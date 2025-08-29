using System;
using System.IO;

namespace RecoveryConsoleExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Example of using DataRecoveryCore.dll from a C# application
            // This demonstrates cross-language compatibility
            
            Console.WriteLine("File Recall - C# Integration Example");
            Console.WriteLine("=====================================\n");
            
            try
            {
                // Initialize logging
                var logger = Microsoft.Extensions.Logging.LoggerFactory
                    .Create(builder => builder.AddConsole())
                    .CreateLogger<DataRecoveryCore.Recovery.RecoveryEngine>();
                
                // Create recovery engine
                using (var recovery = new DataRecoveryCore.Recovery.RecoveryEngine(logger))
                {
                    Console.WriteLine("✓ Recovery engine created successfully");
                    
                    // Example: Initialize for drive 0 (C: drive)
                    if (recovery.Initialize(0))
                    {
                        Console.WriteLine("✓ Successfully connected to physical drive 0");
                        
                        // Example recovery parameters
                        var outputPath = Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.Desktop), "CSharp_Recovery");
                        
                        string[] targetExtensions = { "jpg", "png", "pdf", "docx" };
                        
                        Console.WriteLine($"Output directory: {outputPath}");
                        Console.WriteLine($"Target file types: {string.Join(", ", targetExtensions)}");
                        Console.WriteLine("\nNote: This is a demonstration. Actual recovery would require:");
                        Console.WriteLine("- Administrator privileges");
                        Console.WriteLine("- Proper error handling");
                        Console.WriteLine("- Progress monitoring");
                        Console.WriteLine("- User interface for file selection");
                        
                        // In a real application, you would call:
                        // var result = await recovery.RecoverFilesAsync(
                        //     DataRecoveryCore.Recovery.RecoveryEngine.RecoveryMode.Combined,
                        //     targetExtensions, 
                        //     long.MaxValue, 
                        //     outputPath
                        // );
                        
                    }
                    else
                    {
                        Console.WriteLine("✗ Failed to initialize recovery engine");
                        Console.WriteLine("  Make sure you have administrator privileges");
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}