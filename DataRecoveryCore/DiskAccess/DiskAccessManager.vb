Imports System.Runtime.InteropServices
Imports System.IO
Imports System.ComponentModel
Imports Microsoft.Extensions.Logging

Namespace DiskAccess

    ''' <summary>
    ''' Low-level disk access manager for reading raw sectors and bypassing file system
    ''' Requires administrator privileges for direct hardware access
    ''' </summary>
    Public Class DiskAccessManager
        Implements IDisposable

        Private ReadOnly _logger As ILogger(Of DiskAccessManager)
        Private _diskHandle As IntPtr = IntPtr.Zero
        Private _isDisposed As Boolean = False

        ' Win32 API Constants
        Private Const GENERIC_READ As UInteger = &H80000000UI
        Private Const FILE_SHARE_READ As UInteger = 1
        Private Const FILE_SHARE_WRITE As UInteger = 2
        Private Const OPEN_EXISTING As UInteger = 3
        Private Const FILE_FLAG_NO_BUFFERING As UInteger = &H20000000UI
        Private Shared ReadOnly INVALID_HANDLE_VALUE As IntPtr = New IntPtr(-1)

        ' IOCTL codes for disk operations
        Private Const IOCTL_DISK_GET_DRIVE_GEOMETRY As UInteger = &H70000UI
        Private Const IOCTL_DISK_GET_PARTITION_INFO As UInteger = &H74004UI

        <StructLayout(LayoutKind.Sequential)>
        Public Structure DISK_GEOMETRY
            Public Cylinders As Long
            Public MediaType As UInteger
            Public TracksPerCylinder As UInteger
            Public SectorsPerTrack As UInteger
            Public BytesPerSector As UInteger
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Public Structure PARTITION_INFORMATION
            Public StartingOffset As Long
            Public PartitionLength As Long
            Public HiddenSectors As UInteger
            Public PartitionNumber As UInteger
            Public PartitionType As Byte
            Public BootIndicator As Boolean
            Public RecognizedPartition As Boolean
            Public RewritePartition As Boolean
        End Structure

        ' Win32 API Declarations
        <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
        Private Shared Function CreateFile(
            lpFileName As String,
            dwDesiredAccess As UInteger,
            dwShareMode As UInteger,
            lpSecurityAttributes As IntPtr,
            dwCreationDisposition As UInteger,
            dwFlagsAndAttributes As UInteger,
            hTemplateFile As IntPtr) As IntPtr
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function ReadFile(
            hFile As IntPtr,
            lpBuffer As Byte(),
            nNumberOfBytesToRead As UInteger,
            ByRef lpNumberOfBytesRead As UInteger,
            lpOverlapped As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function SetFilePointer(
            hFile As IntPtr,
            lDistanceToMove As Integer,
            lpDistanceToMoveHigh As IntPtr,
            dwMoveMethod As UInteger) As UInteger
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function SetFilePointerEx(
            hFile As IntPtr,
            liDistanceToMove As Long,
            ByRef lpNewFilePointer As Long,
            dwMoveMethod As UInteger) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function DeviceIoControl(
            hDevice As IntPtr,
            dwIoControlCode As UInteger,
            lpInBuffer As IntPtr,
            nInBufferSize As UInteger,
            lpOutBuffer As IntPtr,
            nOutBufferSize As UInteger,
            ByRef lpBytesReturned As UInteger,
            lpOverlapped As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
        End Function

        Public Sub New(logger As ILogger(Of DiskAccessManager))
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))
            _logger = logger
        End Sub

        ''' <summary>
        ''' Opens direct access to physical drive (requires admin privileges)
        ''' </summary>
        ''' <param name="driveNumber">Physical drive number (0 = first drive)</param>
        ''' <returns>True if successful</returns>
        Public Function OpenPhysicalDrive(driveNumber As Integer) As Boolean
            Try
                If _diskHandle <> IntPtr.Zero Then
                    CloseHandle(_diskHandle)
                End If

                Dim drivePath As String = $"\\.\PhysicalDrive{driveNumber}"
                _logger.LogInformation($"Attempting to open physical drive: {drivePath}")

                _diskHandle = CreateFile(
                    drivePath,
                    GENERIC_READ,
                    FILE_SHARE_READ Or FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_FLAG_NO_BUFFERING,
                    IntPtr.Zero)

                If _diskHandle = INVALID_HANDLE_VALUE Then
                    Dim errorCode = Marshal.GetLastWin32Error()
                    _logger.LogError($"Failed to open drive {drivePath}. Error: {errorCode}")
                    Return False
                End If

                _logger.LogInformation($"Successfully opened drive: {drivePath}")
                Return True

            Catch ex As Exception
                _logger.LogError(ex, $"Exception opening physical drive {driveNumber}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Gets disk geometry information including sector size and total size
        ''' </summary>
        Public Function GetDiskGeometry() As DISK_GEOMETRY
            If _diskHandle = IntPtr.Zero Then
                _logger.LogWarning("Attempted to get geometry on closed disk handle")
                Return New DISK_GEOMETRY() ' Return empty structure instead of null
            End If

            Try
                Dim geometry As New DISK_GEOMETRY()
                Dim bytesReturned As UInteger = 0
                Dim bufferPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(geometry))

                Try
                    Dim result As Boolean = DeviceIoControl(
                        _diskHandle,
                        IOCTL_DISK_GET_DRIVE_GEOMETRY,
                        IntPtr.Zero,
                        0,
                        bufferPtr,
                        CUInt(Marshal.SizeOf(geometry)),
                        bytesReturned,
                        IntPtr.Zero)

                    If result Then
                        geometry = Marshal.PtrToStructure(Of DISK_GEOMETRY)(bufferPtr)
                        _logger.LogInformation($"Disk geometry - Cylinders: {geometry.Cylinders}, Sectors/Track: {geometry.SectorsPerTrack}, Bytes/Sector: {geometry.BytesPerSector}")
                        Return geometry
                    Else
                        Dim errorCode = Marshal.GetLastWin32Error()
                        _logger.LogError($"Failed to get disk geometry. Error: {errorCode}")
                        Return New DISK_GEOMETRY()
                    End If

                Finally
                    Marshal.FreeHGlobal(bufferPtr)
                End Try

            Catch ex As Exception
                _logger.LogError(ex, "Exception getting disk geometry")
                Return New DISK_GEOMETRY()
            End Try
        End Function

        ''' <summary>
        ''' Reads raw sectors from disk starting at specified sector offset
        ''' </summary>
        ''' <param name="sectorOffset">Starting sector number</param>
        ''' <param name="sectorCount">Number of sectors to read</param>
        ''' <param name="bytesPerSector">Bytes per sector (usually 512)</param>
        ''' <returns>Raw sector data or Nothing on error</returns>
        Public Function ReadSectors(sectorOffset As Long, sectorCount As Integer, bytesPerSector As Integer) As Byte()
            If _diskHandle = IntPtr.Zero Then
                _logger.LogWarning("Attempted to read sectors on closed disk handle")
                Return Nothing
            End If

            Try
                ' Calculate byte offset and buffer size
                Dim byteOffset As Long = sectorOffset * bytesPerSector
                Dim bufferSize As Integer = sectorCount * bytesPerSector
                Dim buffer(bufferSize - 1) As Byte

                ' Seek to the specified location
                Dim newPosition As Long = 0
                If Not SetFilePointerEx(_diskHandle, byteOffset, newPosition, 0) Then
                    Dim errorCode = Marshal.GetLastWin32Error()
                    _logger.LogError($"Failed to seek to sector {sectorOffset}. Error: {errorCode}")
                    Return Nothing
                End If

                ' Read the sectors
                Dim bytesRead As UInteger = 0
                If Not ReadFile(_diskHandle, buffer, CUInt(bufferSize), bytesRead, IntPtr.Zero) Then
                    Dim errorCode = Marshal.GetLastWin32Error()
                    _logger.LogError($"Failed to read sectors. Error: {errorCode}")
                    Return Nothing
                End If

                If bytesRead <> bufferSize Then
                    _logger.LogWarning($"Partial read: requested {bufferSize} bytes, got {bytesRead} bytes")
                End If

                _logger.LogDebug($"Successfully read {sectorCount} sectors starting at sector {sectorOffset}")
                Return buffer

            Catch ex As Exception
                _logger.LogError(ex, $"Exception reading sectors {sectorOffset}-{sectorOffset + sectorCount - 1}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Reads a single sector from disk
        ''' </summary>
        Public Function ReadSector(sectorOffset As Long, bytesPerSector As Integer) As Byte()
            Return ReadSectors(sectorOffset, 1, bytesPerSector)
        End Function

        ''' <summary>
        ''' Validates administrator privileges for disk access
        ''' </summary>
        Public Shared Function HasAdministratorPrivileges() As Boolean
            Try
                Dim identity = System.Security.Principal.WindowsIdentity.GetCurrent()
                Dim principal = New System.Security.Principal.WindowsPrincipal(identity)
                Return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)
            Catch
                Return False
            End Try
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _isDisposed Then
                If disposing Then
                    ' Dispose managed resources
                End If

                ' Close disk handle
                If _diskHandle <> IntPtr.Zero Then
                    CloseHandle(_diskHandle)
                    _diskHandle = IntPtr.Zero
                    _logger?.LogInformation("Disk handle closed")
                End If

                _isDisposed = True
            End If
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub

    End Class

End Namespace