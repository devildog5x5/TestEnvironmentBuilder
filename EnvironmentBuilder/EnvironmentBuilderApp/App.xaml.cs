// ============================================================================
// App.xaml.cs - Application Entry Point
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System.Windows;

namespace EnvironmentBuilderApp;

/// <summary>
/// Application entry point for Environment Builder.
/// Handles application-level events and initialization.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Called when the application starts
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
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
}
