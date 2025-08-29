Imports Microsoft.Extensions.Logging
Imports System.Runtime.InteropServices
Imports System.IO

Namespace FileSystems

    ''' <summary>
    ''' NTFS file system parser for analyzing Master File Table (MFT) entries
    ''' Provides deleted file recovery from NTFS metadata structures
    ''' </summary>
    Public Class NtfsParser
        Private ReadOnly _logger As ILogger
        Private ReadOnly _diskAccess As DiskAccess.DiskAccessManager

        ' NTFS Constants
        Private Const NTFS_FILE_SIGNATURE As UInteger = &H454C4946UI ' "FILE"
        Private Const MFT_RECORD_SIZE As Integer = 1024
        Private Const SECTORS_PER_MFT_RECORD As Integer = 2
        Private Const ATTRIBUTE_LIST As Byte = &H20
        Private Const ATTRIBUTE_FILENAME As Byte = &H30
        Private Const ATTRIBUTE_DATA As Byte = &H80
        Private Const ATTRIBUTE_STANDARD_INFO As Byte = &H10

        Public Sub New(diskAccess As DiskAccess.DiskAccessManager, logger As ILogger)
            _diskAccess = diskAccess ?? throw New ArgumentNullException(NameOf(diskAccess))
            _logger = logger ?? throw New ArgumentNullException(NameOf(logger))
        End Sub

        ''' <summary>
        ''' Parse NTFS Master File Table to find deleted file entries
        ''' </summary>
        ''' <param name="maxRecordsToScan">Maximum number of MFT records to examine</param>
        ''' <returns>Collection of found file entries including deleted ones</returns>
        Public Async Function ParseMftAsync(Optional maxRecordsToScan As Integer = 100000) As Task(Of List(Of NtfsFileEntry))
            Dim results As New List(Of NtfsFileEntry)
            
            Try
                _logger.LogInformation("Starting NTFS MFT analysis")
                
                ' Get NTFS boot sector to locate MFT
                Dim bootSector As Byte() = Await _diskAccess.ReadSectorsAsync(0, 1)
                If bootSector Is Nothing OrElse bootSector.Length < 512 Then
                    _logger.LogError("Unable to read NTFS boot sector")
                    Return results
                End If
                
                ' Parse boot sector to get MFT location
                Dim mftCluster As Long = BitConverter.ToUInt64(bootSector, &H30)
                Dim sectorsPerCluster As Byte = bootSector(&HD)
                Dim mftStartSector As Long = mftCluster * sectorsPerCluster
                
                _logger.LogInformation($"MFT located at cluster {mftCluster}, sector {mftStartSector}")
                
                ' Read and parse MFT records
                Dim recordsProcessed As Integer = 0
                Dim currentSector As Long = mftStartSector
                
                While recordsProcessed < maxRecordsToScan
                    ' Read MFT record (typically 2 sectors = 1024 bytes)
                    Dim mftData As Byte() = Await _diskAccess.ReadSectorsAsync(currentSector, SECTORS_PER_MFT_RECORD)
                    If mftData Is Nothing OrElse mftData.Length < MFT_RECORD_SIZE Then
                        Exit While
                    End If
                    
                    ' Process each MFT record in the sector data
                    For offset As Integer = 0 To mftData.Length - MFT_RECORD_SIZE Step MFT_RECORD_SIZE
                        Dim record As Byte() = New Byte(MFT_RECORD_SIZE - 1) {}
                        Array.Copy(mftData, offset, record, 0, MFT_RECORD_SIZE)
                        
                        Dim fileEntry As NtfsFileEntry = ParseMftRecord(record, recordsProcessed)
                        If fileEntry IsNot Nothing Then
                            results.Add(fileEntry)
                            
                            ' Log interesting deleted files
                            If fileEntry.IsDeleted AndAlso Not String.IsNullOrEmpty(fileEntry.FileName) Then
                                _logger.LogDebug($"Found deleted file: {fileEntry.FileName} (Size: {fileEntry.FileSize} bytes)")
                            End If
                        End If
                        
                        recordsProcessed += 1
                        If recordsProcessed >= maxRecordsToScan Then Exit For
                    Next
                    
                    currentSector += SECTORS_PER_MFT_RECORD
                    
                    ' Progress reporting every 1000 records
                    If recordsProcessed Mod 1000 = 0 Then
                        _logger.LogInformation($"Processed {recordsProcessed} MFT records, found {results.Count} file entries")
                    End If
                End While
                
                _logger.LogInformation($"MFT analysis complete. Found {results.Count} file entries ({results.Count(Function(f) f.IsDeleted)} deleted)")
                
            Catch ex As Exception
                _logger.LogError(ex, "Error during NTFS MFT parsing")
            End Try
            
            Return results
        End Function

        ''' <summary>
        ''' Parse individual MFT record to extract file information
        ''' </summary>
        Private Function ParseMftRecord(record As Byte(), recordNumber As Integer) As NtfsFileEntry
            Try
                ' Check for valid FILE signature
                Dim signature As UInteger = BitConverter.ToUInteger(record, 0)
                If signature <> NTFS_FILE_SIGNATURE Then
                    Return Nothing
                End If
                
                ' Parse MFT record header
                Dim flags As UShort = BitConverter.ToUShort(record, &H16)
                Dim isDeleted As Boolean = (flags And &H1) = 0 ' In use flag
                Dim isDirectory As Boolean = (flags And &H2) <> 0
                
                ' Get first attribute offset
                Dim attributeOffset As UShort = BitConverter.ToUShort(record, &H14)
                If attributeOffset >= record.Length Then Return Nothing
                
                Dim fileEntry As New NtfsFileEntry With {
                    .RecordNumber = recordNumber,
                    .IsDeleted = isDeleted,
                    .IsDirectory = isDirectory
                }
                
                ' Parse attributes to extract file information
                Dim currentOffset As Integer = attributeOffset
                
                While currentOffset < record.Length - 16
                    Dim attributeType As UInteger = BitConverter.ToUInteger(record, currentOffset)
                    
                    ' End of attributes
                    If attributeType = &HFFFFFFFFUI Then Exit While
                    
                    Dim attributeLength As UInteger = BitConverter.ToUInteger(record, currentOffset + 4)
                    If attributeLength = 0 OrElse currentOffset + attributeLength > record.Length Then Exit While
                    
                    Select Case attributeType And &HFF
                        Case ATTRIBUTE_STANDARD_INFO
                            ParseStandardInformation(record, currentOffset, fileEntry)
                        Case ATTRIBUTE_FILENAME
                            ParseFileName(record, currentOffset, fileEntry)
                        Case ATTRIBUTE_DATA
                            ParseDataAttribute(record, currentOffset, fileEntry)
                    End Select
                    
                    currentOffset += CInt(attributeLength)
                End While
                
                ' Only return entries with meaningful file information
                If Not String.IsNullOrEmpty(fileEntry.FileName) OrElse fileEntry.FileSize > 0 Then
                    Return fileEntry
                End If
                
            Catch ex As Exception
                _logger.LogDebug($"Error parsing MFT record {recordNumber}: {ex.Message}")
            End Try
            
            Return Nothing
        End Function

        ''' <summary>
        ''' Parse Standard Information attribute for timestamps and basic file info
        ''' </summary>
        Private Sub ParseStandardInformation(record As Byte(), offset As Integer, fileEntry As NtfsFileEntry)
            Try
                ' Standard Information attribute structure
                Dim nonResident As Byte = record(offset + 8)
                If nonResident = 0 Then ' Resident attribute
                    Dim contentOffset As Integer = offset + BitConverter.ToUShort(record, offset + &H14)
                    
                    If contentOffset + 48 <= record.Length Then
                        ' Parse NTFS timestamps (64-bit FILETIME format)
                        Dim createdTime As Long = BitConverter.ToInt64(record, contentOffset)
                        Dim modifiedTime As Long = BitConverter.ToInt64(record, contentOffset + 8)
                        Dim accessedTime As Long = BitConverter.ToInt64(record, contentOffset + 16)
                        
                        fileEntry.CreatedTime = ConvertNtfsTime(createdTime)
                        fileEntry.ModifiedTime = ConvertNtfsTime(modifiedTime)
                        fileEntry.AccessedTime = ConvertNtfsTime(accessedTime)
                        
                        ' File attributes
                        Dim attributes As UInteger = BitConverter.ToUInteger(record, contentOffset + 32)
                        fileEntry.FileAttributes = CType(attributes, FileAttributes)
                    End If
                End If
            Catch ex As Exception
                _logger.LogDebug($"Error parsing Standard Information: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Parse File Name attribute to extract file name and parent directory info
        ''' </summary>
        Private Sub ParseFileName(record As Byte(), offset As Integer, fileEntry As NtfsFileEntry)
            Try
                Dim nonResident As Byte = record(offset + 8)
                If nonResident = 0 Then ' Resident attribute
                    Dim contentOffset As Integer = offset + BitConverter.ToUShort(record, offset + &H14)
                    Dim contentLength As Integer = BitConverter.ToUShort(record, offset + &H10)
                    
                    If contentOffset + 66 <= record.Length Then
                        ' Parent directory reference
                        Dim parentRef As Long = BitConverter.ToInt64(record, contentOffset)
                        fileEntry.ParentDirectory = parentRef And &HFFFFFFFFFFFF ' Lower 48 bits
                        
                        ' File name length (in Unicode characters)
                        Dim nameLength As Byte = record(contentOffset + 64)
                        
                        If nameLength > 0 AndAlso contentOffset + 66 + (nameLength * 2) <= record.Length Then
                            ' Extract Unicode file name
                            Dim nameBytes As Byte() = New Byte((nameLength * 2) - 1) {}
                            Array.Copy(record, contentOffset + 66, nameBytes, 0, nameLength * 2)
                            fileEntry.FileName = System.Text.Encoding.Unicode.GetString(nameBytes)
                            
                            ' Real file size from filename attribute (more reliable than data attribute)
                            If contentOffset + 48 <= record.Length Then
                                fileEntry.FileSize = BitConverter.ToInt64(record, contentOffset + 48)
                                fileEntry.AllocatedSize = BitConverter.ToInt64(record, contentOffset + 56)
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                _logger.LogDebug($"Error parsing File Name: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Parse Data attribute to determine file size and data runs
        ''' </summary>
        Private Sub ParseDataAttribute(record As Byte(), offset As Integer, fileEntry As NtfsFileEntry)
            Try
                Dim nonResident As Byte = record(offset + 8)
                
                If nonResident = 0 Then
                    ' Resident data - file content is stored in MFT record
                    Dim contentOffset As Integer = offset + BitConverter.ToUShort(record, offset + &H14)
                    Dim contentLength As Integer = BitConverter.ToUInteger(record, offset + &H10)
                    
                    fileEntry.FileSize = contentLength
                    fileEntry.IsResident = True
                    
                    ' For small resident files, we could extract the actual data here
                    If contentLength > 0 AndAlso contentLength <= 512 AndAlso contentOffset + contentLength <= record.Length Then
                        fileEntry.ResidentData = New Byte(contentLength - 1) {}
                        Array.Copy(record, contentOffset, fileEntry.ResidentData, 0, contentLength)
                    End If
                    
                Else
                    ' Non-resident data - file content is stored in data runs
                    If offset + 64 <= record.Length Then
                        fileEntry.FileSize = BitConverter.ToInt64(record, offset + &H30) ' Real size
                        fileEntry.AllocatedSize = BitConverter.ToInt64(record, offset + &H28) ' Allocated size
                        fileEntry.IsResident = False
                        
                        ' Parse data runs for cluster locations (complex - simplified here)
                        Dim dataRunOffset As Integer = offset + BitConverter.ToUShort(record, offset + &H20)
                        fileEntry.DataRunOffset = dataRunOffset
                    End If
                End If
                
            Catch ex As Exception
                _logger.LogDebug($"Error parsing Data attribute: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Convert NTFS time (100-nanosecond intervals since 1601-01-01) to DateTime
        ''' </summary>
        Private Function ConvertNtfsTime(ntfsTime As Long) As DateTime
            Try
                If ntfsTime = 0 Then Return DateTime.MinValue
                
                ' NTFS time is 100-nanosecond intervals since January 1, 1601 (UTC)
                Dim baseDate As New DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                Return baseDate.AddTicks(ntfsTime)
            Catch
                Return DateTime.MinValue
            End Try
        End Function

        ''' <summary>
        ''' Represents an NTFS file system entry from MFT analysis
        ''' </summary>
        Public Class NtfsFileEntry
            Public Property RecordNumber As Integer
            Public Property FileName As String
            Public Property FileSize As Long
            Public Property AllocatedSize As Long
            Public Property IsDeleted As Boolean
            Public Property IsDirectory As Boolean
            Public Property IsResident As Boolean
            Public Property ParentDirectory As Long
            Public Property CreatedTime As DateTime
            Public Property ModifiedTime As DateTime
            Public Property AccessedTime As DateTime
            Public Property FileAttributes As FileAttributes
            Public Property ResidentData As Byte()
            Public Property DataRunOffset As Integer
            
            ''' <summary>
            ''' Determine file category based on extension
            ''' </summary>
            Public ReadOnly Property FileCategory As String
                Get
                    If IsDirectory Then Return "Directory"
                    If String.IsNullOrEmpty(FileName) Then Return "Unknown"
                    
                    Dim extension As String = Path.GetExtension(FileName)?.ToLower()
                    Select Case extension
                        Case ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif"
                            Return "Image"
                        Case ".pdf", ".doc", ".docx", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx"
                            Return "Document"
                        Case ".mp3", ".wav", ".flac", ".aac", ".ogg"
                            Return "Audio"
                        Case ".mp4", ".avi", ".mov", ".wmv", ".mkv"
                            Return "Video"
                        Case ".zip", ".rar", ".7z", ".tar", ".gz"
                            Return "Archive"
                        Case ".exe", ".dll", ".sys", ".msi"
                            Return "Executable"
                        Case Else
                            Return "File"
                    End Select
                End Get
            End Property
            
            ''' <summary>
            ''' Check if file matches specified extensions
            ''' </summary>
            Public Function MatchesExtensions(targetExtensions As String()) As Boolean
                If targetExtensions Is Nothing OrElse targetExtensions.Length = 0 Then Return True
                If String.IsNullOrEmpty(FileName) Then Return False
                
                Dim fileExt As String = Path.GetExtension(FileName)?.TrimStart("."c)?.ToLower()
                Return targetExtensions.Any(Function(ext) ext.ToLower() = fileExt)
            End Function
            
            ''' <summary>
            ''' Check if this is a recoverable deleted file
            ''' </summary>
            Public ReadOnly Property IsRecoverable As Boolean
                Get
                    Return IsDeleted AndAlso 
                           Not IsDirectory AndAlso 
                           Not String.IsNullOrEmpty(FileName) AndAlso 
                           FileSize > 0 AndAlso
                           Not FileName.StartsWith("$") ' Skip system files
                End Get
            End Property
            
            Public Overrides Function ToString() As String
                Dim status As String = If(IsDeleted, "[DELETED]", "[ACTIVE]")
                Dim type As String = If(IsDirectory, "DIR", "FILE")
                Return $"{status} {type} {FileName} ({FileSize:N0} bytes) - Modified: {ModifiedTime:yyyy-MM-dd HH:mm:ss}"
            End Function
        End Class
        
    End Class
    
End Namespace