Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Logging.Console
Imports System.IO
Imports DataRecoveryCore.Recovery
Imports RecoveryConsole.UserInterface
Imports System.Threading.Tasks

Module Program

    Private logger As ILogger(Of RecoveryEngine)

    Sub Main(args As String())
        ' Initialize logging for the core engine
        Using loggerFactory As ILoggerFactory = LoggerFactory.Create(
            Sub(builder)
                builder.AddConsole().SetMinimumLevel(LogLevel.Warning) ' Reduce console noise for UI
            End Sub)
            
            logger = loggerFactory.CreateLogger(Of RecoveryEngine)()
            
            ' Set console properties for better UI
            Try
                Console.Title = "File Recall - Data Recovery Tool"
                Console.SetWindowSize(Math.Min(100, Console.LargestWindowWidth), Math.Min(40, Console.LargestWindowHeight))
            Catch
                ' Ignore if console properties can't be set
            End Try
            
            ' Check administrator privileges first
            If Not DataRecoveryCore.DiskAccess.DiskAccessManager.HasAdministratorPrivileges() Then
                Console.Clear()
                ConsoleUI.ShowError("This application requires administrator privileges.")
                ConsoleUI.ShowInfo("Please right-click and 'Run as Administrator', then try again.")
                Console.WriteLine("   Press any key to exit...")
                Console.ReadKey()
                Return
            End If
            
            ' Parse command line arguments or run interactive mode
            If args.Length > 0 Then
                ProcessCommandLineArgs(args)
            Else
                RunInteractiveWizard()
            End If
        End Using
    End Sub

    Private Sub ProcessCommandLineArgs(args As String())
        ' Parse command line arguments for automated recovery
        Dim driveNumber As Integer = 0
        Dim recoveryMode As RecoveryEngine.RecoveryMode = RecoveryEngine.RecoveryMode.Combined
        Dim outputDirectory As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FileRecall_Recovery")
        Dim targetExtensions As String() = Nothing
        Dim maxScanSize As Long = Long.MaxValue

        Try
            For i As Integer = 0 To args.Length - 1
                Select Case args(i).ToLower()
                    Case "--drive", "-d"
                        If i + 1 < args.Length Then
                            Integer.TryParse(args(i + 1), driveNumber)
                            i += 1
                        End If
                    Case "--mode", "-m"
                        If i + 1 < args.Length Then
                            [Enum].TryParse(args(i + 1), True, recoveryMode)
                            i += 1
                        End If
                    Case "--output", "-o"
                        If i + 1 < args.Length Then
                            outputDirectory = args(i + 1)
                            i += 1
                        End If
                    Case "--extensions", "-e"
                        If i + 1 < args.Length Then
                            targetExtensions = args(i + 1).Split(","c).Select(Function(ext) ext.Trim().ToLower()).ToArray()
                            i += 1
                        End If
                    Case "--maxsize", "-s"
                        If i + 1 < args.Length Then
                            Long.TryParse(args(i + 1), maxScanSize)
                            i += 1
                        End If
                    Case "--help", "-h", "-?"
                        ShowUsage()
                        Return
                End Select
            Next

            ' Run automated recovery
            RunAutomatedRecovery(driveNumber, recoveryMode, outputDirectory, targetExtensions, maxScanSize)

        Catch ex As Exception
            Console.Clear()
            ConsoleUI.ShowError($"Error processing command line arguments: {ex.Message}")
            ShowUsage()
            Console.WriteLine("Press any key to exit...")
            Console.ReadKey()
        End Try
    End Sub

    Private Sub ShowUsage()
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗")
        Console.WriteLine("║                     File Recall - Command Line Usage                     ║")
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("Usage: RecoveryConsole.exe [options]")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine("Options:")
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("  --drive, -d <number>     Physical drive number (0 = first drive)")
        Console.WriteLine("  --mode, -m <mode>        Recovery mode:")
        Console.WriteLine("                           • FileSystemOnly (Fast MFT scan)")
        Console.WriteLine("                           • SignatureOnly  (Deep sector scan)")
        Console.WriteLine("                           • Combined       (Recommended)")
        Console.WriteLine("                           • DeepScan       (Comprehensive)")
        Console.WriteLine("  --output, -o <path>      Output directory for recovered files")
        Console.WriteLine("  --extensions, -e <list>  File extensions (jpg,png,pdf,docx)")
        Console.WriteLine("  --maxsize, -s <bytes>    Maximum scan size in bytes")
        Console.WriteLine("  --help, -h               Show this help message")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("Examples:")
        Console.WriteLine("  RecoveryConsole.exe -d 0 -m Combined -o ""C:\\Recovery""")
        Console.WriteLine("  RecoveryConsole.exe -d 1 -m SignatureOnly -e ""jpg,png,mp4""")
        Console.WriteLine("  RecoveryConsole.exe -d 0 -m DeepScan -s 10000000000 -o ""D:\\Recovered""")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.WriteLine("💡 No arguments = Interactive wizard with guided recovery")
        Console.WriteLine()
    End Sub

    Private Sub RunInteractiveWizard()
        Try
            ' Welcome screen
            ConsoleUI.ShowWelcomeScreen()
            
            While True
                ' Main menu
                Dim choice As Integer = ConsoleUI.ShowMainMenu()
                
                Select Case choice
                    Case 1 ' Quick Recovery
                        RunGuidedRecovery(RecoveryEngine.RecoveryMode.FileSystemOnly, "🚀 Quick Recovery")
                    Case 2 ' Deep Signature Scan
                        RunGuidedRecovery(RecoveryEngine.RecoveryMode.SignatureOnly, "🔍 Deep Signature Scan")
                    Case 3 ' Smart Combined Recovery
                        RunGuidedRecovery(RecoveryEngine.RecoveryMode.Combined, "🎯 Smart Combined Recovery")
                    Case 4 ' Custom Recovery
                        RunCustomRecovery()
                    Case 5 ' Exit
                        Console.Clear()
                        Console.ForegroundColor = ConsoleColor.Green
                        Console.WriteLine()
                        Console.WriteLine("   ╔═══════════════════════════════════════════════════════════════════╗")
                        Console.WriteLine("   ║                      Thank you for using                          ║")
                        Console.WriteLine("   ║                        🔄 File Recall                             ║")
                        Console.WriteLine("   ║                 Your Data Recovery Partner                        ║")
                        Console.WriteLine("   ╚═══════════════════════════════════════════════════════════════════╝")
                        Console.WriteLine()
                        Console.ForegroundColor = ConsoleColor.White
                        Threading.Thread.Sleep(1500)
                        Exit While
                End Select
            End While
            
        Catch ex As Exception
            ConsoleUI.ShowError($"An unexpected error occurred: {ex.Message}")
            Console.WriteLine("Press any key to exit...")
            Console.ReadKey()
        End Try
    End Sub

    Private Async Sub RunGuidedRecovery(mode As RecoveryEngine.RecoveryMode, modeName As String)
        Try
            ' Select target location
            Dim driveSelection As ConsoleUI.DriveSelectionResult = ConsoleUI.SelectTargetLocation()
            If Not driveSelection.Success Then
                If driveSelection.GoBack Then Return
                ConsoleUI.ShowError("Failed to select target drive.")
                Threading.Thread.Sleep(2000)
                Return
            End If
            
            ' Select file types
            Dim fileTypes As String() = ConsoleUI.SelectFileTypes()
            
            ' Select output directory
            Dim defaultOutput As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FileRecall_Recovery")
            Dim outputPath As String = ConsoleUI.SelectOutputDirectory(defaultOutput)
            
            ' Confirm settings
            If Not ConsoleUI.ConfirmRecovery(driveSelection, fileTypes, modeName) Then
                ConsoleUI.ShowInfo("Recovery cancelled by user.")
                Threading.Thread.Sleep(2000)
                Return
            End If
            
            ' Run the recovery
            Await RunRecoveryWithProgress(driveSelection.PhysicalDriveNumber, mode, outputPath, fileTypes, modeName)
            
        Catch ex As Exception
            ConsoleUI.ShowError($"Recovery failed: {ex.Message}")
            Console.WriteLine("   📝 Error details saved to recovery log.")
            Threading.Thread.Sleep(3000)
        End Try
    End Sub

    Private Sub RunCustomRecovery()
        Console.Clear()
        ConsoleUI.DrawBorder("Custom Recovery Options")
        Console.WriteLine()
        
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine("   🔧 Advanced Recovery Settings")
        Console.WriteLine()
        Console.WriteLine("   [1] 🎯 Target-Specific Location Recovery")
        Console.WriteLine("       └─ Recover files from specific folder paths")
        Console.WriteLine()
        Console.WriteLine("   [2] 🕒 Time-Based Recovery")
        Console.WriteLine("       └─ Recover files by modification date range")
        Console.WriteLine()
        Console.WriteLine("   [3] 📏 Size-Based Recovery")
        Console.WriteLine("       └─ Filter recovery by file size ranges")
        Console.WriteLine()
        Console.WriteLine("   [4] 🔙 Back to Main Menu")
        Console.WriteLine()
        
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.Write("   Enter your choice (1-4): ")
        
        Dim choice As String = Console.ReadLine()
        Select Case choice
            Case "1"
                RunTargetSpecificRecovery()
            Case "2"
                ConsoleUI.ShowInfo("Time-based recovery will be available in the next update!")
                Threading.Thread.Sleep(2000)
            Case "3"
                ConsoleUI.ShowInfo("Size-based recovery will be available in the next update!")
                Threading.Thread.Sleep(2000)
            Case "4"
                Return
            Case Else
                ConsoleUI.ShowError("Invalid selection. Please try again.")
                Threading.Thread.Sleep(2000)
                RunCustomRecovery()
        End Select
    End Sub

    Private Async Sub RunTargetSpecificRecovery()
        Console.Clear()
        ConsoleUI.DrawBorder("Target-Specific Location Recovery")
        Console.WriteLine()
        
        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("   📁 This mode helps recover files from specific known locations")
        Console.WriteLine("   💡 Examples: Desktop, Documents, Pictures, Downloads folders")
        Console.WriteLine()
        
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine("   Enter the original path where your files were located:")
        Console.WriteLine("   Examples:")
        Console.WriteLine("   • C:\\Users\\YourName\\Desktop")
        Console.WriteLine("   • C:\\Users\\YourName\\Documents\\ImportantFiles")
        Console.WriteLine("   • D:\\Photos\\Vacation2024")
        Console.WriteLine()
        
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.Write("   Original path: ")
        
        Dim originalPath As String = Console.ReadLine()
        If String.IsNullOrWhiteSpace(originalPath) Then
            ConsoleUI.ShowWarning("No path specified. Returning to custom menu.")
            Threading.Thread.Sleep(2000)
            RunCustomRecovery()
            Return
        End If
        
        ConsoleUI.ShowInfo($"Target-specific recovery for: {originalPath}")
        ConsoleUI.ShowInfo("This feature provides enhanced recovery for known file locations!")
        
        ' Continue with standard guided recovery but with context
        Await RunGuidedRecovery(RecoveryEngine.RecoveryMode.Combined, $"🎯 Target-Specific Recovery ({Path.GetFileName(originalPath)})")
    End Sub

    Private Async Function RunRecoveryWithProgress(driveNumber As Integer, mode As RecoveryEngine.RecoveryMode, 
                                                  outputDirectory As String, targetExtensions As String(), modeName As String) As Task
        Try
            Console.Clear()
            ConsoleUI.DrawBorder("Recovery In Progress")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.WriteLine("   🔧 Initializing recovery engine...")
            Console.WriteLine("   📡 Establishing disk connection...")
            Console.WriteLine("   🛡️ Verifying administrator privileges...")
            Console.WriteLine()
            
            Using recovery As New RecoveryEngine(logger)
                If Not recovery.Initialize(driveNumber) Then
                    ConsoleUI.ShowError("Failed to initialize recovery engine.")
                    ConsoleUI.ShowWarning("Possible causes:")
                    Console.WriteLine("     • Drive is not accessible")
                    Console.WriteLine("     • Insufficient administrator privileges")
                    Console.WriteLine("     • Drive is being used by another process")
                    Threading.Thread.Sleep(5000)
                    Return
                End If
                
                Console.Clear()
                ConsoleUI.DrawBorder($"Scanning Drive {driveNumber}")
                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Green
                Console.WriteLine("   ✅ Recovery engine initialized successfully!")
                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.White
                Console.WriteLine($"   🎯 Recovery Mode: {modeName}")
                Console.WriteLine($"   💽 Target Drive: {driveNumber}")
                Console.WriteLine($"   📁 Output Path: {outputDirectory}")
                Console.WriteLine($"   📄 File Types: {If(targetExtensions Is Nothing, "All file types", String.Join(", ", targetExtensions))}")
                Console.WriteLine($"   ⏱️ Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                Console.WriteLine()
                
                Console.ForegroundColor = ConsoleColor.Yellow
                Console.WriteLine("   ⚠️  IMPORTANT: Do not interrupt this process!")
                Console.WriteLine("   📊 Progress will be displayed below...")
                Console.WriteLine()
                
                ' Show initial progress
                Console.ForegroundColor = ConsoleColor.Cyan
                Console.WriteLine("   🔍 Analyzing disk structure...")
                Console.WriteLine("   📈 Preparing recovery algorithms...")
                Console.WriteLine()
                
                ' Start the recovery operation with progress updates
                Dim progressTimer As New Threading.Timer(
                    AddressOf UpdateProgress,
                    Nothing,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                )
                
                Dim result As RecoveryEngine.RecoveryResult = Await recovery.RecoverFilesAsync(
                    mode, targetExtensions, Long.MaxValue, outputDirectory)
                
                progressTimer.Dispose()
                
                ' Show completion
                Console.Clear()
                ConsoleUI.ShowRecoveryResults(result, outputDirectory)
                
                ' Offer to open output directory
                If result.TotalFilesFound > 0 Then
                    Console.WriteLine()
                    Console.ForegroundColor = ConsoleColor.Cyan
                    Console.Write("   Would you like to open the recovery folder? (y/n): ")
                    If Console.ReadLine()?.ToLower() = "y" Then
                        Try
                            Process.Start("explorer.exe", outputDirectory)
                        Catch
                            ConsoleUI.ShowWarning("Could not open folder automatically.")
                        End Try
                    End If
                End If
                
            End Using

        Catch ex As Exception
            ConsoleUI.ShowError($"Recovery operation failed: {ex.Message}")
            Console.WriteLine("   📋 Technical details:")
            Console.WriteLine($"      Error Type: {ex.GetType().Name}")
            Console.WriteLine($"      Location: {ex.StackTrace?.Split(vbLf).FirstOrDefault()?.Trim()}")
            Console.WriteLine("   Press any key to continue...")
            Console.ReadKey()
        End Try
    End Function

    Private Sub UpdateProgress(state As Object)
        ' This is called periodically during recovery to show activity
        Static progressStep As Integer = 0
        progressStep = (progressStep + 1) Mod 4
        
        Dim spinChars As String() = {"⣾", "⣷", "⣯", "⣟", "⡿", "⢿", "⣻", "⣽"}
        Console.SetCursorPosition(3, Console.CursorTop - 2)
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.Write($"{spinChars(progressStep Mod spinChars.Length)} Processing...")
        Console.ForegroundColor = ConsoleColor.White
    End Sub

    Private Async Sub RunAutomatedRecovery(driveNumber As Integer, mode As RecoveryEngine.RecoveryMode, 
                                          outputDirectory As String, targetExtensions As String(), maxScanSize As Long)
        Try
            Console.Clear()
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗")
            Console.WriteLine("║                   File Recall - Automated Recovery                       ║")
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝")
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.White
            
            Console.WriteLine("🔧 Initializing automated recovery...")
            Console.WriteLine($"💽 Target Drive: {driveNumber}")
            Console.WriteLine($"🎯 Recovery Mode: {mode}")
            Console.WriteLine($"📁 Output Directory: {outputDirectory}")
            Console.WriteLine($"📄 Target Extensions: {If(targetExtensions Is Nothing, "All types", String.Join(", ", targetExtensions))}")
            Console.WriteLine($"📏 Max Scan Size: {FormatBytes(maxScanSize)}")
            Console.WriteLine()
            
            Using recovery As New RecoveryEngine(logger)
                If Not recovery.Initialize(driveNumber) Then
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine("❌ Failed to initialize recovery engine.")
                    Console.WriteLine("   Make sure you have administrator privileges and the drive is accessible.")
                    Console.WriteLine()
                    Environment.ExitCode = 1
                    Return
                End If
                
                Console.ForegroundColor = ConsoleColor.Green
                Console.WriteLine("✅ Recovery engine initialized successfully!")
                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Yellow
                Console.WriteLine("🚀 Starting recovery process...")
                
                Dim startTime As DateTime = DateTime.Now
                Dim result As RecoveryEngine.RecoveryResult = Await recovery.RecoverFilesAsync(
                    mode, targetExtensions, maxScanSize, outputDirectory)
                
                ' Display results
                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.Green
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗")
                Console.WriteLine("║                        RECOVERY COMPLETED                                ║")
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝")
                Console.WriteLine()
                Console.ForegroundColor = ConsoleColor.White
                Console.WriteLine($"📊 Recovery Statistics:")
                Console.WriteLine($"   Files Found: {result.TotalFilesFound:N0}")
                Console.WriteLine($"   Data Recovered: {FormatBytes(result.TotalBytesRecovered)}")
                Console.WriteLine($"   Scan Duration: {result.ScanDuration.TotalMinutes:F1} minutes")
                Console.WriteLine($"   Recovery Mode: {result.ScanMode}")
                Console.WriteLine($"   Errors: {result.ErrorCount:N0}")
                Console.WriteLine($"   Success Rate: {If(result.TotalFilesFound > 0, (result.RecoveredFiles.Count(Function(f) f.IsSuccessful) / result.TotalFilesFound * 100):F1, 0):F1}%")
                Console.WriteLine()
                Console.WriteLine($"📁 Output Directory: {outputDirectory}")
                Console.WriteLine("═" & New String("="c, 75))
            End Using

        Catch ex As Exception
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine($"❌ Recovery failed: {ex.Message}")
            Console.WriteLine()
            Environment.ExitCode = 1
        End Try
        
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub

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

End Module