# Intermediate Data Recovery Tool

A production-level deleted data recovery tool built with VB.NET, featuring intermediate capabilities for raw disk analysis, file signature detection, and file system parsing.

## Features

### Core Capabilities
- **Raw Disk Access**: Direct sector-level reading with administrator privileges
- **File Signature Analysis**: Detect files by header/footer patterns even without file system metadata
- **Multi-File System Support**: NTFS, FAT32, and exFAT parsing capabilities
- **Fragmented Recovery**: Reconstruct files scattered across disk sectors
- **Performance Optimized**: Async operations with memory-efficient processing

### Architecture
```
DataRecoveryTool/
├── DataRecoveryCore/          # Core DLL Library
│   ├── DiskAccess/           # Low-level disk operations
│   ├── FileSignatures/       # File type detection
│   ├── FileSystems/          # NTFS/FAT32 parsers
│   └── Recovery/             # File reconstruction logic
├── RecoveryConsole/          # Console Application
└── Tests/                    # Unit and integration tests
```

## Prerequisites
- Windows 10/11 (Administrator privileges required)
- .NET Framework 4.8 or .NET 6+
- Visual Studio 2022 or VS Code with VB.NET support

## Quick Start

### Console Usage
```cmd
RecoveryConsole.exe --drive C: --scan deep --output "C:\Recovery"
RecoveryConsole.exe --drive E: --signature jpg,png,docx --target "E:\RestoreFolder"
```

### Library Usage (VB.NET)
```vb
Dim recovery As New DataRecoveryCore.RecoveryEngine()
Dim results As List(Of RecoveredFile) = Await recovery.ScanDriveAsync("C:\", RecoveryMode.Deep)
```

## Development

### Building
```cmd
dotnet build --configuration Release
```

### Testing
```cmd
dotnet test --logger console --verbosity normal
```

## Version History
- v1.0.0 - Initial release with basic recovery
- v1.1.0 - Added file signature detection
- v1.2.0 - NTFS parsing implementation
- v1.3.0 - Performance optimizations

## License
MIT License - See LICENSE file for details

## Contributing
1. Fork the repository
2. Create feature branch (`git checkout -b feature/enhancement`)
3. Commit changes (`git commit -am 'Add new feature'`)
4. Push to branch (`git push origin feature/enhancement`)
5. Create Pull Request

## Disclaimer
This tool is for legitimate data recovery purposes only. Users are responsible for complying with local laws and regulations.