# Contributing to Data Recovery Tool

## Development Setup

### Prerequisites
- Windows 10/11
- Visual Studio 2022 or VS Code with VB.NET support
- .NET 6.0 SDK or later
- Administrator privileges (for testing disk access)

### Building the Solution
```cmd
dotnet restore
dotnet build --configuration Release
```

### Running Tests
```cmd
dotnet test --logger console --verbosity normal
```

### Code Style Guidelines
- Use PascalCase for public members
- Use camelCase for private fields with underscore prefix (_field)
- Add XML documentation for public APIs
- Handle exceptions gracefully with proper logging
- Use async/await for I/O operations
- Dispose resources properly

### Testing Guidelines
- Write unit tests for all new functionality
- Mock disk access for unit tests (don't use real drives)
- Include integration tests for end-to-end scenarios
- Test edge cases and error conditions

### Pull Request Process
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Make your changes and add tests
4. Ensure all tests pass
5. Update documentation if needed
6. Commit your changes (`git commit -am 'Add new feature'`)
7. Push to your branch (`git push origin feature/new-feature`)
8. Create a Pull Request

### Security Considerations
- Never commit real disk data or personal files
- Be careful with test data that could contain sensitive information
- Validate all user inputs
- Use secure coding practices for low-level disk operations

## Bug Reports
When reporting bugs, please include:
- Operating system version
- .NET version
- Exact command used
- Error message (if any)
- Expected vs actual behavior
- Steps to reproduce