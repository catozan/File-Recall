' File Recall - Data Recovery Tool Usage Examples
' ================================================
' 
' This file contains examples of how to use the DataRecoveryCore.dll
' in various scenarios for deleted file recovery operations.

Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Logging.Console
Imports System.IO
Imports DataRecoveryCore.Recovery
Imports DataRecoveryCore.DiskAccess
Imports DataRecoveryCore.FileSignatures

Module UsageExamples

    ''' <summary>
    ''' Example 1: Basic file recovery with default settings
    ''' Recovers all file types from the primary drive
    ''' </summary>
    Public Async Function BasicRecoveryExample() As Task
        Console.WriteLine("=== Basic Recovery Example ===")
        
        ' Initialize logging
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            
            Dim logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            ' Create recovery engine
            Using recovery As New RecoveryEngine(logger)
                ' Initialize for physical drive 0 (usually C:)
                If recovery.Initialize(driveNumber:=0) Then
                    
                    ' Set output directory
                    Dim outputPath As String = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop), "BasicRecovery")
                    
                    Console.WriteLine($"Starting recovery to: {outputPath}")
                    
                    ' Recover all file types using combined method
                    Dim result = Await recovery.RecoverFilesAsync(
                        RecoveryEngine.RecoveryMode.Combined,
                        targetExtensions:=Nothing, ' All file types
                        maxScanSizeBytes:=10_000_000_000, ' 10GB limit
                        outputDirectory:=outputPath
                    )
                    
                    ' Display results
                    Console.WriteLine($"Recovery completed!")
                    Console.WriteLine($"Files found: {result.TotalFilesFound}")
                    Console.WriteLine($"Data recovered: {result.TotalBytesRecovered:N0} bytes")
                    Console.WriteLine($"Duration: {result.ScanDuration.TotalMinutes:F1} minutes")
                    
                Else
                    Console.WriteLine("Failed to initialize recovery engine")
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Example 2: Targeted file recovery for specific file types
    ''' Focuses on recovering images and documents only
    ''' </summary>
    Public Async Function TargetedFileRecovery() As Task
        Console.WriteLine("=== Targeted File Recovery Example ===")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            
            Dim logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            Using recovery As New RecoveryEngine(logger)
                If recovery.Initialize(driveNumber:=0) Then
                    
                    ' Target specific file types
                    Dim targetTypes As String() = {
                        "jpg", "jpeg", "png", "gif", "bmp", ' Images
                        "pdf", "doc", "docx", "txt", "rtf"  ' Documents
                    }
                    
                    Dim outputPath As String = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "RecoveredFiles"
                    )
                    
                    Console.WriteLine($"Recovering images and documents to: {outputPath}")
                    Console.WriteLine($"Target extensions: {String.Join(", ", targetTypes)}")
                    
                    ' Use deep scan for maximum recovery
                    Dim result = Await recovery.RecoverFilesAsync(
                        RecoveryEngine.RecoveryMode.DeepScan,
                        targetTypes,
                        maxScanSizeBytes:=5_000_000_000, ' 5GB limit
                        outputDirectory:=outputPath
                    )
                    
                    ' Analyze recovered files by category
                    Dim recoveredFiles = result.RecoveredFiles.Where(Function(f) f.IsSuccessful)
                    Dim imageFiles = recoveredFiles.Where(Function(f) f.FileInfo.FileCategory = "Image").Count()
                    Dim docFiles = recoveredFiles.Where(Function(f) f.FileInfo.FileCategory = "Document").Count()
                    
                    Console.WriteLine($"Images recovered: {imageFiles}")
                    Console.WriteLine($"Documents recovered: {docFiles}")
                    
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Example 3: Fast recovery using file system metadata only
    ''' Best for recently deleted files on healthy drives
    ''' </summary>
    Public Async Function FastFileSystemRecovery() As Task
        Console.WriteLine("=== Fast File System Recovery Example ===")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            
            Dim logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            Using recovery As New RecoveryEngine(logger)
                If recovery.Initialize(driveNumber:=0) Then
                    
                    Dim outputPath As String = "D:\\QuickRecovery" ' Use different drive
                    Directory.CreateDirectory(outputPath)
                    
                    Console.WriteLine("Performing fast NTFS metadata scan...")
                    
                    ' Use file system only mode for speed
                    Dim result = Await recovery.RecoverFilesAsync(
                        RecoveryEngine.RecoveryMode.FileSystemOnly,
                        targetExtensions:=Nothing, ' All types
                        maxScanSizeBytes:=Long.MaxValue, ' No limit
                        outputDirectory:=outputPath
                    )
                    
                    Console.WriteLine($"Fast recovery statistics:")
                    Console.WriteLine($"- Scan time: {result.ScanDuration.TotalSeconds:F1} seconds")
                    Console.WriteLine($"- Files processed: {result.TotalFilesFound:N0}")
                    Console.WriteLine($"- Success rate: {If(result.TotalFilesFound > 0, (result.RecoveredFiles.Count(Function(f) f.IsSuccessful) / result.TotalFilesFound * 100), 0):F1}%")
                    
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Example 4: Advanced recovery with custom filtering and monitoring
    ''' Demonstrates progress tracking and custom file validation
    ''' </summary>
    Public Async Function AdvancedRecoveryWithMonitoring() As Task
        Console.WriteLine("=== Advanced Recovery with Monitoring ===")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
            
            Dim logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            Using recovery As New RecoveryEngine(logger)
                If recovery.Initialize(driveNumber:=0) Then
                    
                    ' Multi-media file recovery
                    Dim mediaExtensions As String() = {
                        "mp4", "avi", "mov", "wmv", "mkv", ' Video
                        "mp3", "wav", "flac", "aac",      ' Audio
                        "jpg", "png", "raw", "tiff"       ' Images
                    }
                    
                    Dim outputPath As String = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                        "RecoveredMedia"
                    )
                    
                    Console.WriteLine($"Starting advanced media recovery...")
                    Console.WriteLine($"Scanning for: {String.Join(", ", mediaExtensions)}")
                    
                    ' Setup progress monitoring
                    Dim startTime As DateTime = DateTime.Now
                    
                    ' Execute recovery with comprehensive scanning
                    Dim result = Await recovery.RecoverFilesAsync(
                        RecoveryEngine.RecoveryMode.Combined,
                        mediaExtensions,
                        maxScanSizeBytes:=20_000_000_000, ' 20GB scan limit
                        outputDirectory:=outputPath
                    )
                    
                    ' Detailed result analysis
                    Console.WriteLine("\n=== Recovery Analysis ===")
                    Console.WriteLine($"Total operation time: {(DateTime.Now - startTime).TotalMinutes:F1} minutes")
                    Console.WriteLine($"Files discovered: {result.TotalFilesFound:N0}")
                    Console.WriteLine($"Successful recoveries: {result.RecoveredFiles.Count(Function(f) f.IsSuccessful):N0}")
                    Console.WriteLine($"Failed recoveries: {result.RecoveredFiles.Count(Function(f) Not f.IsSuccessful):N0}")
                    Console.WriteLine($"Total data recovered: {FormatBytes(result.TotalBytesRecovered)}")
                    
                    ' File type breakdown
                    Dim typeGroups = result.RecoveredFiles.Where(Function(f) f.IsSuccessful).
                        GroupBy(Function(f) f.FileInfo.FileCategory).
                        OrderByDescending(Function(g) g.Count())
                    
                    Console.WriteLine("\nRecovered files by category:")
                    For Each group In typeGroups
                        Dim totalSize = group.Sum(Function(f) If(f.Data?.Length, 0))
                        Console.WriteLine($"- {group.Key}: {group.Count():N0} files ({FormatBytes(totalSize)})")
                    Next
                    
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Example 5: Signature-only recovery for severely damaged file systems
    ''' Uses raw sector scanning when file system metadata is corrupted
    ''' </summary>
    Public Async Function SignatureOnlyRecovery() As Task
        Console.WriteLine("=== Signature-Only Recovery Example ===")
        Console.WriteLine("Use this method when file system is severely damaged")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            
            Dim logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            Using recovery As New RecoveryEngine(logger)
                If recovery.Initialize(driveNumber:=1) Then ' External drive
                    
                    ' Focus on high-value file types
                    Dim criticalFiles As String() = {
                        "pdf", "docx", "xlsx", "pptx", ' Office documents
                        "jpg", "png", "tiff",         ' Images
                        "zip", "rar", "7z"            ' Archives
                    }
                    
                    Dim outputPath As String = "C:\\CriticalFileRecovery"
                    Directory.CreateDirectory(outputPath)
                    
                    Console.WriteLine("WARNING: Signature-only recovery is slow but thorough")
                    Console.WriteLine("Estimated time: 1-4 hours depending on drive size")
                    Console.WriteLine("Starting raw sector analysis...")
                    
                    ' Pure signature-based recovery
                    Dim result = Await recovery.RecoverFilesAsync(
                        RecoveryEngine.RecoveryMode.SignatureOnly,
                        criticalFiles,
                        maxScanSizeBytes:=50_000_000_000, ' 50GB deep scan
                        outputDirectory:=outputPath
                    )
                    
                    Console.WriteLine($"\nSignature scan completed!")
                    Console.WriteLine($"Raw signatures detected: {result.TotalFilesFound}")
                    Console.WriteLine($"Files successfully extracted: {result.RecoveredFiles.Count(Function(f) f.IsSuccessful)}")
                    
                    ' Quality analysis
                    Dim largeFiles = result.RecoveredFiles.Where(
                        Function(f) f.IsSuccessful AndAlso f.Data IsNot Nothing AndAlso f.Data.Length > 1048576 ' > 1MB
                    ).Count()
                    
                    Console.WriteLine($"Large files recovered (>1MB): {largeFiles}")
                    
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Example 6: Custom file signature analysis
    ''' Demonstrates direct use of FileSignatureAnalyzer for file identification
    ''' </summary>
    Public Sub FileSignatureAnalysisExample()
        Console.WriteLine("=== File Signature Analysis Example ===")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole())
            
            Dim logger = loggerFactory.CreateLogger("SignatureAnalysis")
            Dim analyzer As New FileSignatureAnalyzer(logger)
            
            ' Example file signatures for testing
            Dim testSignatures As New Dictionary(Of String, Byte()) From {
                {"JPEG", {&HFF, &HD8, &HFF, &HE0}},
                {"PNG", {&H89, &H50, &H4E, &H47}},
                {"PDF", {&H25, &H50, &H44, &H46}},
                {"ZIP", {&H50, &H4B, &H3, &H4}},
                {"Unknown", {&H99, &H99, &H99, &H99}}
            }
            
            Console.WriteLine("Testing file signature detection:")
            
            For Each test In testSignatures
                Dim result = analyzer.DetectFileType(test.Value)
                
                If result IsNot Nothing Then
                    Console.WriteLine($"✓ {test.Key}: Detected as {result.Description} ({result.PrimaryExtension})")
                    Console.WriteLine($"  Extensions: {String.Join(", ", result.KnownExtensions)}")
                Else
                    Console.WriteLine($"✗ {test.Key}: No signature match found")
                End If
                Console.WriteLine()
            Next
        End Using
    End Sub

    ''' <summary>
    ''' Example 7: Disk access and low-level operations
    ''' Demonstrates direct disk reading capabilities
    ''' </summary>
    Public Async Function LowLevelDiskAccessExample() As Task
        Console.WriteLine("=== Low-Level Disk Access Example ===")
        Console.WriteLine("CAUTION: This demonstrates raw disk access")
        
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder) builder.AddConsole())
            
            Dim logger = loggerFactory.CreateLogger("DiskAccess")
            
            Using diskAccess As New DiskAccessManager(logger)
                If Await diskAccess.InitializeAsync(physicalDriveNumber:=0) Then
                    
                    Console.WriteLine("Successfully opened physical drive 0")
                    
                    ' Read first sector (boot sector)
                    Dim bootSector As Byte() = Await diskAccess.ReadSectorsAsync(0, 1)
                    
                    If bootSector IsNot Nothing AndAlso bootSector.Length >= 512 Then
                        Console.WriteLine($"Boot sector read: {bootSector.Length} bytes")
                        
                        ' Analyze file system type
                        Dim fsType As String = AnalyzeFileSystem(bootSector)
                        Console.WriteLine($"File system: {fsType}")
                        
                        ' Display first 32 bytes of boot sector
                        Console.Write("Boot sector header: ")
                        For i As Integer = 0 To Math.Min(31, bootSector.Length - 1)
                            Console.Write($"{bootSector(i):X2} ")
                        Next
                        Console.WriteLine()
                        
                        ' Try to read file system metadata
                        If fsType.Contains("NTFS") Then
                            Console.WriteLine("\nNTFS file system detected - MFT analysis possible")
                            Dim mftCluster As Long = BitConverter.ToUInt64(bootSector, &H30)
                            Console.WriteLine($"MFT starts at cluster: {mftCluster}")
                        End If
                        
                    Else
                        Console.WriteLine("Failed to read boot sector")
                    End If
                    
                Else
                    Console.WriteLine("Failed to initialize disk access")
                    Console.WriteLine("Make sure you have administrator privileges")
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Helper function to analyze file system type from boot sector
    ''' </summary>
    Private Function AnalyzeFileSystem(bootSector As Byte()) As String
        Try
            ' Check for NTFS
            If bootSector.Length >= 512 Then
                Dim ntfsSignature As String = System.Text.Encoding.ASCII.GetString(bootSector, 3, 8)
                If ntfsSignature = "NTFS    " Then
                    Return "NTFS"
                End If
                
                ' Check for FAT32
                If bootSector.Length >= 90 Then
                    Dim fat32Signature As String = System.Text.Encoding.ASCII.GetString(bootSector, 82, 5)
                    If fat32Signature = "FAT32" Then
                        Return "FAT32"
                    End If
                End If
                
                ' Check for FAT16
                If bootSector.Length >= 62 Then
                    Dim fat16Signature As String = System.Text.Encoding.ASCII.GetString(bootSector, 54, 3)
                    If fat16Signature = "FAT" Then
                        Return "FAT16"
                    End If
                End If
            End If
            
            Return "Unknown"
            
        Catch
            Return "Error analyzing"
        End Try
    End Function

    ''' <summary>
    ''' Helper function to format byte sizes in human-readable format
    ''' </summary>
    Private Function FormatBytes(bytes As Long) As String
        If bytes = 0 Then Return "0 B"
        
        Dim units As String() = {"B", "KB", "MB", "GB", "TB"}
        Dim unitIndex As Integer = 0
        Dim size As Double = bytes
        
        While size >= 1024 AndAlso unitIndex < units.Length - 1
            size /= 1024
            unitIndex += 1
        End While
        
        Return $"{size:F2} {units(unitIndex)}"
    End Function

    ''' <summary>
    ''' Main entry point for running all examples
    ''' </summary>
    Public Async Function RunAllExamples() As Task
        Console.WriteLine("File Recall - Data Recovery Usage Examples")
        Console.WriteLine("=========================================")
        Console.WriteLine()
        
        If Not DiskAccessManager.HasAdministratorPrivileges() Then
            Console.WriteLine("⚠️ WARNING: Administrator privileges required for disk access")
            Console.WriteLine("Please run as Administrator to execute recovery examples")
            Console.WriteLine()
            Return
        End If
        
        Try
            ' Run signature analysis (safe example)
            FileSignatureAnalysisExample()
            Console.WriteLine("Press any key to continue to recovery examples...")
            Console.ReadKey()
            
            ' Run low-level example
            Await LowLevelDiskAccessExample()
            Console.WriteLine("\nPress any key to continue...")
            Console.ReadKey()
            
            Console.WriteLine("\nNOTE: Additional recovery examples are available but commented out")
            Console.WriteLine("to prevent accidental execution. Uncomment specific examples to run them.")
            
            ' Uncomment specific examples as needed:
            ' Await BasicRecoveryExample()
            ' Await TargetedFileRecovery()
            ' Await FastFileSystemRecovery()
            ' Await AdvancedRecoveryWithMonitoring()
            ' Await SignatureOnlyRecovery()
            
        Catch ex As Exception
            Console.WriteLine($"Example execution error: {ex.Message}")
        End Try
        
        Console.WriteLine("\nAll examples completed!")
    End Function
    
End Module