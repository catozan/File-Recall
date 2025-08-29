Imports System.IO
Imports DataRecoveryCore.Recovery

Namespace UserInterface

    ''' <summary>
    ''' User-friendly console interface with visual guides and step-by-step recovery process
    ''' </summary>
    Public Class ConsoleUI

        Private Const BorderChar As String = "═"
        Private Const VerticalChar As String = "║"
        Private Const TopLeftCorner As String = "╔"
        Private Const TopRightCorner As String = "╗"
        Private Const BottomLeftCorner As String = "╚"
        Private Const BottomRightCorner As String = "╝"
        Private Const HorizontalSeparator As String = "╠═══════════════════════════════════════════════════════════════════════════╣"

        Public Shared Sub ShowWelcomeScreen()
            Console.Clear()
            SetConsoleColors()
            
            DrawBorder("File Recall - Data Recovery Tool v1.0")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.WriteLine("   🔄 Professional Data Recovery Solution")
            Console.WriteLine("   💾 Raw Disk Access & File Signature Detection")
            Console.WriteLine("   🛡️ NTFS File System Analysis")
            Console.WriteLine("   📁 Multiple Recovery Modes Available")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine("   ⚠️  ADMINISTRATOR PRIVILEGES REQUIRED")
            Console.WriteLine("   ⚠️  USE ONLY ON AUTHORIZED SYSTEMS")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("   Press any key to begin recovery wizard...")
            Console.ReadKey(True)
        End Sub

        Public Shared Function ShowMainMenu() As Integer
            Console.Clear()
            DrawBorder("Recovery Mode Selection")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("   Select your recovery approach:")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("   [1] 🚀 Quick Recovery")
            Console.WriteLine("       └─ Fast scan using file system metadata (NTFS/FAT)")
            Console.WriteLine("       └─ Best for recently deleted files")
            Console.WriteLine("       └─ Estimated time: 2-10 minutes")
            Console.WriteLine()
            
            Console.WriteLine("   [2] 🔍 Deep Signature Scan")
            Console.WriteLine("       └─ Sector-by-sector analysis with file signatures")
            Console.WriteLine("       └─ Recovers files even after format/corruption")
            Console.WriteLine("       └─ Estimated time: 30 minutes - 2 hours")
            Console.WriteLine()
            
            Console.WriteLine("   [3] 🎯 Smart Combined Recovery")
            Console.WriteLine("       └─ File system scan + signature analysis")
            Console.WriteLine("       └─ Maximum recovery rate for all scenarios")
            Console.WriteLine("       └─ Estimated time: 45 minutes - 3 hours")
            Console.WriteLine()
            
            Console.WriteLine("   [4] ⚙️  Custom Recovery Settings")
            Console.WriteLine("       └─ Advanced options and file type filtering")
            Console.WriteLine()
            
            Console.WriteLine("   [5] ❌ Exit")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("   Enter your choice (1-5): ")
            
            Dim input As String = Console.ReadLine()
            Dim choice As Integer
            If Integer.TryParse(input, choice) AndAlso choice >= 1 AndAlso choice <= 5 Then
                Return choice
            Else
                ShowError("Invalid selection. Please enter a number between 1-5.")
                Threading.Thread.Sleep(2000)
                Return ShowMainMenu()
            End If
        End Function

        Public Shared Function SelectTargetLocation() As DriveSelectionResult
            Console.Clear()
            DrawBorder("Target Location Selection")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine("   📍 Select the location where your lost files were stored:")
            Console.WriteLine()
            
            ' Get available drives
            Dim drives As DriveInfo() = DriveInfo.GetDrives()
            Dim validDrives As New List(Of DriveInfo)
            
            Console.ForegroundColor = ConsoleColor.White
            Dim driveIndex As Integer = 1
            
            For Each drive As DriveInfo In drives
                If drive.DriveType = DriveType.Fixed OrElse drive.DriveType = DriveType.Removable Then
                    validDrives.Add(drive)
                    
                    Console.WriteLine($"   [{driveIndex}] 💽 Drive {drive.Name}")
                    Console.WriteLine($"       └─ Type: {GetDriveTypeDescription(drive.DriveType)}")
                    
                    If drive.IsReady Then
                        Dim totalGB As Double = drive.TotalSize / (1024.0 * 1024.0 * 1024.0)
                        Dim freeGB As Double = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0)
                        Console.WriteLine($"       └─ Size: {totalGB:F1} GB ({freeGB:F1} GB free)")
                        Console.WriteLine($"       └─ Label: {If(String.IsNullOrEmpty(drive.VolumeLabel), "No label", drive.VolumeLabel)}")
                    Else
                        Console.WriteLine("       └─ Status: Not ready")
                    End If
                    
                    Console.WriteLine()
                    driveIndex += 1
                End If
            Next
            
            If validDrives.Count = 0 Then
                ShowError("No valid drives found for recovery.")
                Return New DriveSelectionResult With {.Success = False}
            End If
            
            Console.WriteLine("   [0] 🔙 Back to main menu")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write($"   Select drive (0-{validDrives.Count}): ")
            
            Dim input As String = Console.ReadLine()
            Dim choice As Integer
            
            If Integer.TryParse(input, choice) Then
                If choice = 0 Then
                    Return New DriveSelectionResult With {.Success = False, .GoBack = True}
                ElseIf choice > 0 AndAlso choice <= validDrives.Count Then
                    Dim selectedDrive As DriveInfo = validDrives(choice - 1)
                    
                    ' Convert drive letter to physical drive number (simplified)
                    Dim driveNumber As Integer = Asc(selectedDrive.Name.ToUpper()(0)) - Asc("C"c)
                    If selectedDrive.Name.ToUpper().StartsWith("C") Then driveNumber = 0
                    
                    Return New DriveSelectionResult With {
                        .Success = True,
                        .SelectedDrive = selectedDrive,
                        .PhysicalDriveNumber = driveNumber,
                        .DrivePath = selectedDrive.Name
                    }
                End If
            End If
            
            ShowError("Invalid selection. Please try again.")
            Threading.Thread.Sleep(2000)
            Return SelectTargetLocation()
        End Function

        Public Shared Function SelectFileTypes() As String()
            Console.Clear()
            DrawBorder("File Type Selection")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("   📄 Choose file types to recover (or select All):")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("   [1] 📸 Images (jpg, png, gif, bmp, tiff)")
            Console.WriteLine("   [2] 📝 Documents (pdf, doc, docx, xls, xlsx, ppt, pptx)")
            Console.WriteLine("   [3] 🎵 Audio Files (mp3, wav, flac)")
            Console.WriteLine("   [4] 🎬 Video Files (mp4, avi, mov, wmv)")
            Console.WriteLine("   [5] 📦 Archives (zip, rar, 7z)")
            Console.WriteLine("   [6] 💾 All File Types")
            Console.WriteLine("   [7] 🎯 Custom Selection")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("   Enter your choice (1-7): ")
            
            Dim input As String = Console.ReadLine()
            Dim choice As Integer
            
            If Integer.TryParse(input, choice) Then
                Select Case choice
                    Case 1
                        Return {"jpg", "jpeg", "png", "gif", "bmp", "tiff", "tif"}
                    Case 2
                        Return {"pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx"}
                    Case 3
                        Return {"mp3", "wav", "flac"}
                    Case 4
                        Return {"mp4", "avi", "mov", "wmv"}
                    Case 5
                        Return {"zip", "rar", "7z"}
                    Case 6
                        Return Nothing ' All file types
                    Case 7
                        Return GetCustomFileTypes()
                End Select
            End If
            
            ShowError("Invalid selection. Please try again.")
            Threading.Thread.Sleep(2000)
            Return SelectFileTypes()
        End Function

        Private Shared Function GetCustomFileTypes() As String()
            Console.Clear()
            DrawBorder("Custom File Types")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine("   🎯 Enter file extensions separated by commas:")
            Console.WriteLine("   Example: jpg,png,pdf,docx,mp3")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("   File extensions: ")
            
            Dim input As String = Console.ReadLine()
            If String.IsNullOrWhiteSpace(input) Then
                ShowError("No file types specified. Returning to selection menu.")
                Threading.Thread.Sleep(2000)
                Return SelectFileTypes()
            End If
            
            Dim extensions As String() = input.Split(","c).Select(Function(ext) ext.Trim().ToLower().TrimStart("."c)).Where(Function(ext) Not String.IsNullOrWhiteSpace(ext)).ToArray()
            
            If extensions.Length = 0 Then
                ShowError("No valid file extensions found. Returning to selection menu.")
                Threading.Thread.Sleep(2000)
                Return SelectFileTypes()
            End If
            
            Return extensions
        End Function

        Public Shared Function SelectOutputDirectory(defaultPath As String) As String
            Console.Clear()
            DrawBorder("Recovery Output Location")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("   📁 Where would you like to save recovered files?")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine("   ⚠️  Choose a different drive than the recovery source!")
            Console.WriteLine("   ⚠️  Ensure you have enough free space for recovered files.")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine($"   Default location: {defaultPath}")
            Console.WriteLine()
            Console.WriteLine("   [1] Use default location")
            Console.WriteLine("   [2] Choose custom location")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("   Enter your choice (1-2): ")
            
            Dim choice As String = Console.ReadLine()
            
            If choice = "1" Then
                If Not Directory.Exists(defaultPath) Then
                    Try
                        Directory.CreateDirectory(defaultPath)
                    Catch ex As Exception
                        ShowError($"Cannot create default directory: {ex.Message}")
                        Threading.Thread.Sleep(3000)
                        Return SelectOutputDirectory(defaultPath)
                    End Try
                End If
                Return defaultPath
                
            ElseIf choice = "2" Then
                Console.Write("   Enter custom path: ")
                Dim customPath As String = Console.ReadLine()
                
                If String.IsNullOrWhiteSpace(customPath) Then
                    ShowError("Invalid path specified.")
                    Threading.Thread.Sleep(2000)
                    Return SelectOutputDirectory(defaultPath)
                End If
                
                Try
                    If Not Directory.Exists(customPath) Then
                        Directory.CreateDirectory(customPath)
                    End If
                    Return customPath
                Catch ex As Exception
                    ShowError($"Cannot access/create directory: {ex.Message}")
                    Threading.Thread.Sleep(3000)
                    Return SelectOutputDirectory(defaultPath)
                End Try
            Else
                ShowError("Invalid choice. Please select 1 or 2.")
                Threading.Thread.Sleep(2000)
                Return SelectOutputDirectory(defaultPath)
            End If
        End Function

        Public Shared Sub ShowRecoveryProgress(current As Integer, total As Integer, currentFile As String, Optional bytesProcessed As Long = 0)
            Console.SetCursorPosition(0, Console.CursorTop)
            
            Dim progressWidth As Integer = 50
            Dim progress As Double = If(total > 0, CDbl(current) / total, 0)
            Dim filledWidth As Integer = CInt(progress * progressWidth)
            
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("   Progress: [")
            Console.Write(New String("█"c, filledWidth))
            Console.ForegroundColor = ConsoleColor.DarkGray
            Console.Write(New String("░"c, progressWidth - filledWidth))
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write($"] {progress:P1}")
            
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine($"   Files: {current:N0} / {total:N0}")
            
            If bytesProcessed > 0 Then
                Console.WriteLine($"   Data processed: {FormatBytes(bytesProcessed)}")
            End If
            
            If Not String.IsNullOrEmpty(currentFile) Then
                Console.ForegroundColor = ConsoleColor.Cyan
                Dim displayFile As String = If(currentFile.Length > 60, "..." & currentFile.Substring(currentFile.Length - 57), currentFile)
                Console.WriteLine($"   Current: {displayFile}")
            End If
        End Sub

        Public Shared Sub ShowRecoveryResults(result As RecoveryEngine.RecoveryResult, outputPath As String)
            Console.Clear()
            DrawBorder("Recovery Complete!")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("   ✅ File recovery operation completed successfully!")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine($"   📊 Recovery Statistics:")
            Console.WriteLine($"   ├─ Files Found: {result.TotalFilesFound:N0}")
            Console.WriteLine($"   ├─ Data Recovered: {FormatBytes(result.TotalBytesRecovered)}")
            Console.WriteLine($"   ├─ Scan Duration: {result.ScanDuration.TotalMinutes:F1} minutes")
            Console.WriteLine($"   ├─ Recovery Mode: {result.ScanMode}")
            Console.WriteLine($"   └─ Errors: {result.ErrorCount:N0}")
            Console.WriteLine()
            
            If result.RecoveredFiles.Count > 0 Then
                Console.WriteLine($"   📁 Files saved to: {outputPath}")
                Console.WriteLine()
                
                ' Show file breakdown by category
                Dim categories = result.RecoveredFiles.Where(Function(f) f.IsSuccessful).
                    GroupBy(Function(f) f.FileInfo.FileCategory).
                    OrderByDescending(Function(g) g.Count())
                
                Console.WriteLine("   📈 Recovery Breakdown:")
                For Each category In categories
                    Dim categoryName As String = category.Key.ToString()
                    Dim count As Integer = category.Count()
                    Dim totalSize As Long = category.Sum(Function(f) If(f.Data?.Length, 0))
                    Console.WriteLine($"   ├─ {categoryName}: {count:N0} files ({FormatBytes(totalSize)})")
                Next
            Else
                Console.ForegroundColor = ConsoleColor.Yellow
                Console.WriteLine("   ⚠️  No files were successfully recovered.")
                Console.WriteLine("   💡 Try using a different recovery mode or check if the drive")
                Console.WriteLine("      contains the file types you're looking for.")
            End If
            
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.WriteLine("   Press any key to continue...")
            Console.ReadKey(True)
        End Sub

        Public Shared Sub ShowError(message As String)
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine()
            Console.WriteLine($"   ❌ Error: {message}")
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.White
        End Sub

        Public Shared Sub ShowWarning(message As String)
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine()
            Console.WriteLine($"   ⚠️  Warning: {message}")
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.White
        End Sub

        Public Shared Sub ShowInfo(message As String)
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.WriteLine()
            Console.WriteLine($"   ℹ️  {message}")
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.White
        End Sub

        Public Shared Function ConfirmRecovery(driveInfo As DriveSelectionResult, fileTypes As String(), mode As String) As Boolean
            Console.Clear()
            DrawBorder("Confirm Recovery Settings")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.WriteLine("   🔍 Please confirm your recovery settings:")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine($"   Target Drive: {driveInfo.DrivePath}")
            Console.WriteLine($"   Recovery Mode: {mode}")
            Console.WriteLine($"   File Types: {If(fileTypes Is Nothing, "All types", String.Join(", ", fileTypes))}")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("   ⚠️  IMPORTANT WARNINGS:")
            Console.WriteLine("   • This operation requires administrator privileges")
            Console.WriteLine("   • Recovery process may take significant time")
            Console.WriteLine("   • Do not interrupt the process once started")
            Console.WriteLine("   • Ensure target drive is not being used by other processes")
            Console.WriteLine()
            
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("   Do you want to proceed? (y/n): ")
            
            Dim response As String = Console.ReadLine()?.ToLower()
            Return response = "y" OrElse response = "yes"
        End Function

        Public Shared Sub DrawBorder(title As String)
            Console.ForegroundColor = ConsoleColor.Green
            
            ' Top border
            Console.WriteLine(TopLeftCorner & New String(BorderChar(0), 75) & TopRightCorner)
            
            ' Title line
            Dim padding As Integer = (75 - title.Length) \ 2
            Console.WriteLine(VerticalChar & New String(" "c, padding) & title & New String(" "c, 75 - padding - title.Length) & VerticalChar)
            
            ' Bottom border
            Console.WriteLine(BottomLeftCorner & New String(BorderChar(0), 75) & BottomRightCorner)
            
            Console.ForegroundColor = ConsoleColor.White
        End Sub

        Private Shared Sub SetConsoleColors()
            Console.BackgroundColor = ConsoleColor.Black
            Console.ForegroundColor = ConsoleColor.White
            Console.Clear()
        End Sub

        Private Shared Function GetDriveTypeDescription(driveType As DriveType) As String
            Select Case driveType
                Case DriveType.Fixed
                    Return "Fixed Hard Drive"
                Case DriveType.Removable
                    Return "Removable Drive"
                Case DriveType.Network
                    Return "Network Drive"
                Case DriveType.CDRom
                    Return "CD/DVD Drive"
                Case Else
                    Return "Unknown"
            End Select
        End Function

        Private Shared Function FormatBytes(bytes As Long) As String
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

        Public Class DriveSelectionResult
            Public Property Success As Boolean
            Public Property GoBack As Boolean
            Public Property SelectedDrive As DriveInfo
            Public Property PhysicalDriveNumber As Integer
            Public Property DrivePath As String
        End Class

    End Class

End Namespace