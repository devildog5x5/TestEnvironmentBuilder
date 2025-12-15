// ============================================================================
// MainWindow.xaml.cs - Main Window Code-Behind
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnvironmentBuilderApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Contains minimal code-behind - most logic is in the ViewModel
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Converts boolean to connection status string
/// </summary>
public class BoolToConnectionConverter : IValueConverter
{
    public static readonly BoolToConnectionConverter Instance = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool connected && connected ? "Connected" : "Disconnected";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}
