// ============================================================================
// App.xaml.cs - Application Entry Point
// Environment Builder - Modern test environment creation tool
// Supports both GUI and CLI modes
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows;

namespace EnvironmentBuilderApp;

/// <summary>
/// Application entry point for Environment Builder.
/// Handles both GUI and CLI modes for maximum flexibility.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Check for CLI mode
        if (e.Args.Length > 0 && CliProgram.IsCliMode(e.Args))
        {
            // Run in console mode
            AttachConsole(-1); // Attach to parent console
            AllocConsole();    // Or create new console
            
            try
            {
                var exitCode = await CliProgram.RunAsync(e.Args);
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
            return;
        }

        // Normal GUI startup
        base.OnStartup(e);
        
        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }
    
    /// <summary>
    /// Handles unhandled exceptions in the AppDomain
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Message}\n\nThe application will now close.",
                "Environment Builder - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handles unhandled exceptions on the dispatcher thread
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, 
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An error occurred:\n\n{e.Exception.Message}",
            "Environment Builder - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        
        e.Handled = true;
    }

    // P/Invoke for console support
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}
