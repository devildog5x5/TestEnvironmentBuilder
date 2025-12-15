// ============================================================================
// MainViewModel.cs - Main Application ViewModel
// Environment Builder - Modern test environment creation tool
// Evolved from TreeBuilder 3.4 by Robert Foster
// ============================================================================

using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnvironmentBuilderApp.Models;
using EnvironmentBuilderApp.Services;
using Microsoft.Win32;
using Serilog;

namespace EnvironmentBuilderApp.ViewModels;

/// <summary>
/// Main ViewModel for the Environment Builder application.
/// Coordinates all operations and manages the application state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ----------------------------------------------------------------------------
    // Private Fields
    // ----------------------------------------------------------------------------
    
    private readonly ILogger _logger;
    private LdapService? _ldapService;
    private LdifService? _ldifService;
    
    // ----------------------------------------------------------------------------
    // Observable Properties
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// The current environment configuration
    /// </summary>
    [ObservableProperty]
    private EnvironmentConfiguration _configuration = EnvironmentConfiguration.CreateDefault();
    
    /// <summary>
    /// Status message displayed in the UI
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    /// <summary>
    /// Progress value (0-100)
    /// </summary>
    [ObservableProperty]
    private int _progressValue = 0;
    
    /// <summary>
    /// Whether an operation is in progress
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing = false;
    
    /// <summary>
    /// Whether the LDAP connection is active
    /// </summary>
    [ObservableProperty]
    private bool _isConnected = false;
    
    /// <summary>
    /// Log messages for display
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();
    
    /// <summary>
    /// Currently selected tab index
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex = 0;
    
    /// <summary>
    /// List of user configurations
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<UserConfiguration> _userConfigs = new();
    
    // ----------------------------------------------------------------------------
    // Constructor
    // ----------------------------------------------------------------------------
    
    public MainViewModel()
    {
        // Initialize Serilog logger
        _logger = new LoggerConfiguration()
            .WriteTo.File("logs/environment_builder_.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        // Initialize services
        _ldifService = new LdifService(_logger);
        
        // Initialize user configs from configuration
        foreach (var userConfig in Configuration.UserConfigs)
        {
            UserConfigs.Add(userConfig);
        }
        
        // Add default user config if empty
        if (UserConfigs.Count == 0)
        {
            var defaultConfig = new UserConfiguration { SetName = "Test Users" };
            UserConfigs.Add(defaultConfig);
            Configuration.UserConfigs.Add(defaultConfig);
        }
        
        AddLogMessage("Environment Builder initialized");
    }
    
    // ----------------------------------------------------------------------------
    // Connection Commands
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Tests the LDAP connection with current settings
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        StatusMessage = "Testing connection...";
        IsProcessing = true;
        
        try
        {
            using var service = new LdapService(Configuration.Connection, _logger);
            var (success, message) = await service.TestConnectionAsync();
            
            StatusMessage = message;
            IsConnected = success;
            AddLogMessage($"Connection test: {message}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
            AddLogMessage($"ERROR: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    // ----------------------------------------------------------------------------
    // Environment Build Commands
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Runs the full environment update/creation
    /// </summary>
    [RelayCommand]
    private async Task RunUpdateAsync()
    {
        StatusMessage = "Building environment...";
        IsProcessing = true;
        ProgressValue = 0;
        
        try
        {
            AddLogMessage("Starting environment build...");
            
            // Generate LDIF first
            await GenerateLdifAsync();
            
            // If not write-only mode, execute against LDAP
            if (!Configuration.WriteOnlyMode)
            {
                AddLogMessage("Connecting to LDAP server...");
                _ldapService = new LdapService(Configuration.Connection, _logger);
                
                if (await _ldapService.ConnectAsync())
                {
                    IsConnected = true;
                    await ExecuteEnvironmentBuildAsync();
                    _ldapService.Disconnect();
                    IsConnected = false;
                }
                else
                {
                    AddLogMessage("ERROR: Failed to connect to LDAP server");
                    StatusMessage = "Connection failed";
                }
            }
            
            ProgressValue = 100;
            StatusMessage = "Environment build complete!";
            AddLogMessage("Environment build completed successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Build failed: {ex.Message}";
            AddLogMessage($"ERROR: {ex.Message}");
            _logger.Error(ex, "Environment build failed");
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    /// <summary>
    /// Generates LDIF file without executing
    /// </summary>
    [RelayCommand]
    private async Task GenerateLdifAsync()
    {
        try
        {
            AddLogMessage("Generating LDIF file...");
            
            // Sync user configs to configuration
            Configuration.UserConfigs.Clear();
            foreach (var config in UserConfigs)
            {
                Configuration.UserConfigs.Add(config);
            }
            
            var ldifContent = _ldifService!.GenerateLdif(Configuration);
            await _ldifService.SaveToFileAsync(ldifContent, Configuration.LdifExportPath);
            
            AddLogMessage($"LDIF file saved to: {Configuration.LdifExportPath}");
            StatusMessage = "LDIF file generated";
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR generating LDIF: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Executes the environment build against LDAP
    /// </summary>
    private async Task ExecuteEnvironmentBuildAsync()
    {
        if (_ldapService == null) return;
        
        int totalOperations = CalculateTotalOperations();
        int currentOperation = 0;
        
        // Create tree structure
        if (Configuration.TreeConfig.CreateTreeStructure)
        {
            AddLogMessage("Creating tree structure...");
            currentOperation = await CreateTreeStructureAsync(Configuration.TreeConfig.RootNodes, "", 
                currentOperation, totalOperations);
        }
        
        // Create users
        foreach (var userConfig in Configuration.UserConfigs.Where(u => u.IsEnabled))
        {
            AddLogMessage($"Creating users for set: {userConfig.SetName}");
            
            for (int i = userConfig.StartNumber; i <= userConfig.EndNumber; i++)
            {
                var username = userConfig.GenerateUsername(i);
                var dn = $"cn={username},{userConfig.UserContext}";
                
                if (Configuration.DeleteMode)
                {
                    await _ldapService.DeleteObjectAsync(dn);
                }
                else
                {
                    await _ldapService.CreateUserAsync(dn, userConfig, i);
                }
                
                currentOperation++;
                ProgressValue = (currentOperation * 100) / totalOperations;
            }
        }
    }
    
    /// <summary>
    /// Recursively creates tree structure
    /// </summary>
    private async Task<int> CreateTreeStructureAsync(List<TreeNode> nodes, string parentDn, 
        int currentOp, int totalOps)
    {
        if (_ldapService == null) return currentOp;
        
        foreach (var node in nodes)
        {
            var rdn = node.GetRDN();
            var dn = string.IsNullOrEmpty(parentDn) ? rdn : $"{rdn},{parentDn}";
            
            await _ldapService.CreateContainerAsync(dn, node.NodeType, node.Description);
            currentOp++;
            ProgressValue = (currentOp * 100) / totalOps;
            
            if (node.Children.Count > 0)
            {
                currentOp = await CreateTreeStructureAsync(node.Children, dn, currentOp, totalOps);
            }
        }
        
        return currentOp;
    }
    
    // ----------------------------------------------------------------------------
    // Configuration Commands
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Saves current configuration to file
    /// </summary>
    [RelayCommand]
    private void SaveConfiguration()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Configuration|*.json",
                Title = "Save Environment Configuration",
                FileName = $"{Configuration.Name}.json"
            };
            
            if (dialog.ShowDialog() == true)
            {
                // Sync user configs
                Configuration.UserConfigs.Clear();
                foreach (var config in UserConfigs)
                {
                    Configuration.UserConfigs.Add(config);
                }
                
                Configuration.SaveToFile(dialog.FileName);
                AddLogMessage($"Configuration saved to: {dialog.FileName}");
                StatusMessage = "Configuration saved";
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR saving configuration: {ex.Message}");
            StatusMessage = "Save failed";
        }
    }
    
    /// <summary>
    /// Loads configuration from file
    /// </summary>
    [RelayCommand]
    private void LoadConfiguration()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Configuration|*.json",
                Title = "Load Environment Configuration"
            };
            
            if (dialog.ShowDialog() == true)
            {
                Configuration = EnvironmentConfiguration.LoadFromFile(dialog.FileName);
                
                // Sync to observable collection
                UserConfigs.Clear();
                foreach (var config in Configuration.UserConfigs)
                {
                    UserConfigs.Add(config);
                }
                
                AddLogMessage($"Configuration loaded from: {dialog.FileName}");
                StatusMessage = "Configuration loaded";
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR loading configuration: {ex.Message}");
            StatusMessage = "Load failed";
        }
    }
    
    /// <summary>
    /// Resets all settings to defaults
    /// </summary>
    [RelayCommand]
    private void ResetAll()
    {
        Configuration = EnvironmentConfiguration.CreateDefault();
        UserConfigs.Clear();
        foreach (var config in Configuration.UserConfigs)
        {
            UserConfigs.Add(config);
        }
        AddLogMessage("All settings reset to defaults");
        StatusMessage = "Settings reset";
    }
    
    // ----------------------------------------------------------------------------
    // User Set Commands
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Adds a new user configuration set
    /// </summary>
    [RelayCommand]
    private void AddUserSet()
    {
        var newSet = new UserConfiguration
        {
            SetName = $"User Set {UserConfigs.Count + 1}",
            StartNumber = 1,
            EndNumber = 10
        };
        UserConfigs.Add(newSet);
        AddLogMessage($"Added new user set: {newSet.SetName}");
    }
    
    /// <summary>
    /// Removes a user configuration set
    /// </summary>
    [RelayCommand]
    private void RemoveUserSet(UserConfiguration? config)
    {
        if (config != null && UserConfigs.Contains(config))
        {
            UserConfigs.Remove(config);
            AddLogMessage($"Removed user set: {config.SetName}");
        }
    }
    
    // ----------------------------------------------------------------------------
    // Helper Methods
    // ----------------------------------------------------------------------------
    
    /// <summary>
    /// Calculates total number of operations for progress tracking
    /// </summary>
    private int CalculateTotalOperations()
    {
        int total = 0;
        
        // Count tree nodes
        total += CountTreeNodes(Configuration.TreeConfig.RootNodes);
        
        // Count users
        foreach (var config in Configuration.UserConfigs.Where(u => u.IsEnabled))
        {
            total += config.GetUserCount();
        }
        
        return Math.Max(1, total);
    }
    
    /// <summary>
    /// Recursively counts tree nodes
    /// </summary>
    private int CountTreeNodes(List<TreeNode> nodes)
    {
        int count = nodes.Count;
        foreach (var node in nodes)
        {
            count += CountTreeNodes(node.Children);
        }
        return count;
    }
    
    /// <summary>
    /// Adds a message to the log display
    /// </summary>
    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages.Insert(0, $"[{timestamp}] {message}");
        
        // Keep only last 100 messages
        while (LogMessages.Count > 100)
        {
            LogMessages.RemoveAt(LogMessages.Count - 1);
        }
        
        _logger.Information(message);
    }
}

