Imports Microsoft.Extensions.Logging
Imports DataRecoveryCore.DiskAccess
Imports DataRecoveryCore.FileSignatures
Imports DataRecoveryCore.FileSystems
Imports System.Threading.Tasks
Imports System.IO

Namespace Recovery

    ''' <summary>
    ''' Main recovery engine that coordinates disk access, file system parsing, and signature analysis
    ''' </summary>
    Public Class RecoveryEngine
        Implements IDisposable

        Private ReadOnly _logger As ILogger(Of RecoveryEngine)
        Private ReadOnly _diskAccess As DiskAccessManager
        Private ReadOnly _signatureAnalyzer As FileSignatureAnalyzer
        Private _ntfsParser As NtfsParser
        Private _isDisposed As Boolean = False

        Public Enum RecoveryMode
            FileSystemOnly      ' Use MFT/FAT records only
            SignatureOnly       ' Use file signatures only
            Combined            ' Combine both methods
            DeepScan           ' Sector-by-sector analysis
        End Enum

        Public Class RecoveryResult
            Public Property RecoveredFiles As List(Of RecoveredFileInfo)
            Public Property TotalFilesFound As Integer
            Public Property TotalBytesRecovered As Long
            Public Property ScanDuration As TimeSpan
            Public Property ErrorCount As Integer
            Public Property ScanMode As RecoveryMode

            Public Sub New()
                RecoveredFiles = New List(Of RecoveredFileInfo)
            End Sub
        End Class

        Public Class RecoveryFileInfo
            Public Property FileName As String
            Public Property OriginalPath As String
            Public Property FileSize As Long
            Public Property FileExtension As String
            Public Property CreatedTime As DateTime
            Public Property ModifiedTime As DateTime
            Public Property RecoveryMethod As String
            Public Property ConfidenceLevel As Double
            Public Property IsComplete As Boolean
            Public Property FileCategory As FileCategory
            Public Property StartOffset As Long
            Public Property DataRuns As List(Of FileSystems.NtfsParser.DataRun)

            Public Sub New()
                DataRuns = New List(Of FileSystems.NtfsParser.DataRun)
            End Sub
        End Class

        Public Class RecoveredFileInfo
            Public Property FileInfo As RecoveryFileInfo
            Public Property Data As Byte()
            Public Property IsSuccessful As Boolean
            Public Property ErrorMessage As String

            Public Sub New(fileInfo As RecoveryFileInfo)
                Me.FileInfo = fileInfo
                Me.IsSuccessful = False
            End Sub
        End Class

        Public Sub New(logger As ILogger(Of RecoveryEngine))
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))
            _logger = logger
            _diskAccess = New DiskAccessManager(_logger)
            _signatureAnalyzer = New FileSignatureAnalyzer(_logger)
        End Sub

        ''' <summary>
        ''' Initializes recovery engine for specified drive
        ''' </summary>
        Public Function Initialize(driveNumber As Integer) As Boolean
            Try
                _logger.LogInformation($"Initializing recovery engine for drive {driveNumber}")

                ' Validate administrator privileges
                If Not DiskAccessManager.HasAdministratorPrivileges() Then
                    _logger.LogError("Administrator privileges required for disk access")
                    Return False
                End If

                ' Open disk access
                If Not _diskAccess.OpenPhysicalDrive(driveNumber) Then
                    _logger.LogError($"Failed to open physical drive {driveNumber}")
                    Return False
                End If

                ' Initialize NTFS parser
                _ntfsParser = New NtfsParser(_logger, _diskAccess)
                If Not _ntfsParser.Initialize() Then
                    _logger.LogWarning("NTFS parser initialization failed - will use signature-only recovery")
                    _ntfsParser = Nothing
                End If

                _logger.LogInformation("Recovery engine initialized successfully")
                Return True

            Catch ex As Exception
                _logger.LogError(ex, "Failed to initialize recovery engine")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Performs file recovery using specified mode
        ''' </summary>
        Public Async Function RecoverFilesAsync(mode As RecoveryMode, 
                                               Optional targetExtensions As String() = Nothing,
                                               Optional maxScanSize As Long = Long.MaxValue,
                                               Optional outputDirectory As String = Nothing) As Task(Of RecoveryResult)
            
            Dim result As New RecoveryResult With {
                .ScanMode = mode
            }
            
            Dim stopwatch = System.Diagnostics.Stopwatch.StartNew()

            Try
                _logger.LogInformation($"Starting recovery with mode: {mode}")

                Select Case mode
                    Case RecoveryMode.FileSystemOnly
                        Await RecoverUsingFileSystem(result, targetExtensions)
                    Case RecoveryMode.SignatureOnly
                        Await RecoverUsingSignatures(result, targetExtensions, maxScanSize)
                    Case RecoveryMode.Combined
                        Await RecoverUsingCombined(result, targetExtensions, maxScanSize)
                    Case RecoveryMode.DeepScan
                        Await RecoverUsingDeepScan(result, targetExtensions, maxScanSize)
                End Select

                ' Save recovered files if output directory specified
                If Not String.IsNullOrEmpty(outputDirectory) Then
                    Await SaveRecoveredFiles(result, outputDirectory)
                End If

            Catch ex As Exception
                _logger.LogError(ex, "Error during file recovery")
                result.ErrorCount += 1
            Finally
                stopwatch.Stop()
                result.ScanDuration = stopwatch.Elapsed
                _logger.LogInformation($"Recovery completed in {result.ScanDuration.TotalSeconds:F1} seconds. Found {result.TotalFilesFound} files")
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Recovers files using file system metadata (MFT records)
        ''' </summary>
        Private Async Function RecoverUsingFileSystem(result As RecoveryResult, targetExtensions As String()) As Task
            If _ntfsParser Is Nothing Then
                _logger.LogWarning("NTFS parser not available for file system recovery")
                Return
            End If

            _logger.LogInformation("Starting file system-based recovery...")

            Await Task.Run(Sub()
                Try
                    Dim deletedFiles = _ntfsParser.ScanDeletedFiles()
                    _logger.LogInformation($"Found {deletedFiles.Count} deleted file records in MFT")

                    For Each ntfsFile In deletedFiles
                        ' Filter by extensions if specified
                        If targetExtensions IsNot Nothing AndAlso targetExtensions.Length > 0 Then
                            Dim fileExt = IO.Path.GetExtension(ntfsFile.FileName).TrimStart("."c).ToLower()
                            If Not targetExtensions.Any(Function(ext) ext.ToLower() = fileExt) Then
                                Continue For
                            End If
                        End If

                        ' Convert to recovery file info
                        Dim recoveryInfo As New RecoveryFileInfo With {
                            .FileName = ntfsFile.FileName,
                            .OriginalPath = ntfsFile.FileName,
                            .FileSize = ntfsFile.FileSize,
                            .FileExtension = IO.Path.GetExtension(ntfsFile.FileName),
                            .CreatedTime = ntfsFile.CreatedTime,
                            .ModifiedTime = ntfsFile.ModifiedTime,
                            .RecoveryMethod = "NTFS MFT",
                            .ConfidenceLevel = ntfsFile.Confidence,
                            .IsComplete = ntfsFile.DataRuns.Count > 0,
                            .DataRuns = ntfsFile.DataRuns
                        }

                        ' Attempt to recover file data
                        Dim recoveredFile As New RecoveredFileInfo(recoveryInfo)
                        Try
                            recoveredFile.Data = _ntfsParser.RecoverFileData(ntfsFile)
                            If recoveredFile.Data IsNot Nothing Then
                                recoveredFile.IsSuccessful = True
                                result.TotalBytesRecovered += recoveredFile.Data.Length
                            Else
                                recoveredFile.ErrorMessage = "Failed to recover file data"
                            End If
                        Catch ex As Exception
                            recoveredFile.ErrorMessage = ex.Message
                            result.ErrorCount += 1
                        End Try

                        result.RecoveredFiles.Add(recoveredFile)
                        result.TotalFilesFound += 1

                        ' Log progress every 100 files
                        If result.TotalFilesFound Mod 100 = 0 Then
                            _logger.LogInformation($"Processed {result.TotalFilesFound} files...")
                        End If
                    Next

                Catch ex As Exception
                    _logger.LogError(ex, "Error in file system recovery")
                    result.ErrorCount += 1
                End Try
            End Sub)
        End Function

        ''' <summary>
        ''' Recovers files using signature analysis
        ''' </summary>
        Private Async Function RecoverUsingSignatures(result As RecoveryResult, targetExtensions As String(), maxScanSize As Long) As Task
            _logger.LogInformation("Starting signature-based recovery...")

            Await Task.Run(Sub()
                Try
                    ' Get disk geometry for sector size
                    Dim geometry = _diskAccess.GetDiskGeometry()
                    If geometry.BytesPerSector = 0 Then
                        _logger.LogError("Failed to get disk geometry")
                        Return
                    End If

                    Dim bytesPerSector As Integer = CInt(geometry.BytesPerSector)
                    Dim sectorsPerRead As Integer = 1024 ' Read 512KB chunks
                    Dim bytesPerRead As Integer = sectorsPerRead * bytesPerSector
                    
                    Dim currentSector As Long = 0
                    Dim maxSectors As Long = maxScanSize \ bytesPerSector
                    Dim totalBytesScanned As Long = 0

                    ' Filter signatures by target extensions
                    Dim targetSignatures = If(targetExtensions IsNot Nothing, 
                                            _signatureAnalyzer.GetSignaturesByExtension(targetExtensions),
                                            _signatureAnalyzer.Signatures.ToList())

                    _logger.LogInformation($"Scanning with {targetSignatures.Count} file signatures")

                    While currentSector < maxSectors
                        Try
                            ' Read sector chunk
                            Dim data = _diskAccess.ReadSectors(currentSector, sectorsPerRead, bytesPerSector)
                            If data Is Nothing Then
                                currentSector += sectorsPerRead
                                Continue While
                            End If

                            ' Analyze for file signatures
                            Dim detectedFiles = _signatureAnalyzer.AnalyzeDataBlock(data, currentSector * bytesPerSector)
                            
                            For Each detected In detectedFiles
                                ' Skip if not in target extensions
                                If targetExtensions IsNot Nothing AndAlso 
                                   Not targetExtensions.Any(Function(ext) ext.ToLower() = detected.Signature.Extension.ToLower()) Then
                                    Continue For
                                End If

                                ' Create recovery info
                                Dim recoveryInfo As New RecoveryFileInfo With {
                                    .FileName = detected.FileName,
                                    .OriginalPath = $"Sector_{detected.StartOffset:X8}",
                                    .FileSize = detected.EstimatedSize,
                                    .FileExtension = "." & detected.Signature.Extension,
                                    .RecoveryMethod = "File Signature",
                                    .ConfidenceLevel = detected.ConfidenceLevel,
                                    .FileCategory = detected.Signature.Category,
                                    .StartOffset = detected.StartOffset,
                                    .IsComplete = detected.IsComplete
                                }

                                ' Attempt to extract file data
                                Dim recoveredFile As New RecoveredFileInfo(recoveryInfo)
                                Try
                                    Dim fileStartInChunk As Integer = CInt(detected.StartOffset - (currentSector * bytesPerSector))
                                    If fileStartInChunk >= 0 AndAlso fileStartInChunk < data.Length Then
                                        Dim extractSize As Integer = CInt(Math.Min(detected.EstimatedSize, data.Length - fileStartInChunk))
                                        
                                        ReDim recoveredFile.Data(extractSize - 1)
                                        Array.Copy(data, fileStartInChunk, recoveredFile.Data, 0, extractSize)
                                        
                                        recoveredFile.IsSuccessful = True
                                        result.TotalBytesRecovered += recoveredFile.Data.Length
                                    End If
                                Catch ex As Exception
                                    recoveredFile.ErrorMessage = ex.Message
                                    result.ErrorCount += 1
                                End Try

                                result.RecoveredFiles.Add(recoveredFile)
                                result.TotalFilesFound += 1
                            Next

                            totalBytesScanned += data.Length
                            currentSector += sectorsPerRead

                            ' Log progress every 100MB
                            If totalBytesScanned Mod (100 * 1024 * 1024) = 0 Then
                                _logger.LogInformation($"Scanned {totalBytesScanned / (1024 * 1024):F0} MB, found {result.TotalFilesFound} files")
                            End If

                        Catch ex As Exception
                            _logger.LogWarning(ex, $"Error reading sector {currentSector}")
                            currentSector += sectorsPerRead
                            result.ErrorCount += 1
                        End Try
                    End While

                Catch ex As Exception
                    _logger.LogError(ex, "Error in signature-based recovery")
                    result.ErrorCount += 1
                End Try
            End Sub)
        End Function

        ''' <summary>
        ''' Combines file system and signature recovery methods
        ''' </summary>
        Private Async Function RecoverUsingCombined(result As RecoveryResult, targetExtensions As String(), maxScanSize As Long) As Task
            _logger.LogInformation("Starting combined recovery (file system + signatures)...")

            ' First try file system recovery
            Await RecoverUsingFileSystem(result, targetExtensions)
            Dim fsFileCount = result.TotalFilesFound

            ' Then supplement with signature recovery
            Await RecoverUsingSignatures(result, targetExtensions, maxScanSize)
            
            _logger.LogInformation($"Combined recovery: {fsFileCount} from file system, {result.TotalFilesFound - fsFileCount} from signatures")
        End Function

        ''' <summary>
        ''' Performs deep sector-by-sector scan
        ''' </summary>
        Private Async Function RecoverUsingDeepScan(result As RecoveryResult, targetExtensions As String(), maxScanSize As Long) As Task
            _logger.LogInformation("Starting deep scan recovery...")
            
            ' Deep scan is essentially signature recovery with smaller chunks for better accuracy
            Await RecoverUsingSignatures(result, targetExtensions, maxScanSize)
        End Function

        ''' <summary>
        ''' Saves recovered files to disk
        ''' </summary>
        Private Async Function SaveRecoveredFiles(result As RecoveryResult, outputDirectory As String) As Task
            If Not Directory.Exists(outputDirectory) Then
                Directory.CreateDirectory(outputDirectory)
            End If

            _logger.LogInformation($"Saving {result.RecoveredFiles.Count} recovered files to {outputDirectory}")

            For Each recovered In result.RecoveredFiles
                If Not recovered.IsSuccessful OrElse recovered.Data Is Nothing Then
                    Continue For
                End If

                Try
                    ' Create category subdirectory
                    Dim categoryDir As String = Path.Combine(outputDirectory, recovered.FileInfo.FileCategory.ToString())
                    If Not Directory.Exists(categoryDir) Then
                        Directory.CreateDirectory(categoryDir)
                    End If

                    ' Generate unique filename
                    Dim fileName As String = If(String.IsNullOrEmpty(recovered.FileInfo.FileName), 
                                               $"recovered_{recovered.FileInfo.StartOffset:X8}", 
                                               recovered.FileInfo.FileName)
                    
                    Dim filePath As String = Path.Combine(categoryDir, fileName)
                    Dim counter As Integer = 1
                    
                    While File.Exists(filePath)
                        Dim nameWithoutExt = Path.GetFileNameWithoutExtension(fileName)
                        Dim extension = Path.GetExtension(fileName)
                        filePath = Path.Combine(categoryDir, $"{nameWithoutExt}_{counter}{extension}")
                        counter += 1
                    End While

                    ' Save file
                    Await File.WriteAllBytesAsync(filePath, recovered.Data)
                    
                    ' Set file timestamps if available
                    If recovered.FileInfo.CreatedTime <> DateTime.MinValue Then
                        File.SetCreationTime(filePath, recovered.FileInfo.CreatedTime)
                    End If
                    If recovered.FileInfo.ModifiedTime <> DateTime.MinValue Then
                        File.SetLastWriteTime(filePath, recovered.FileInfo.ModifiedTime)
                    End If

                    _logger.LogDebug($"Saved: {filePath} ({recovered.Data.Length:N0} bytes)")

                Catch ex As Exception
                    _logger.LogWarning(ex, $"Failed to save file: {recovered.FileInfo.FileName}")
                End Try
            Next

            _logger.LogInformation($"File saving completed. Check {outputDirectory}")
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _isDisposed Then
                If disposing Then
                    _diskAccess?.Dispose()
                End If
                _isDisposed = True
            End If
        End Sub

    End Class

End Namespace