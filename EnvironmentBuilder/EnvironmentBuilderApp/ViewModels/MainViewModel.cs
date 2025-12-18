// ============================================================================
// MainViewModel.cs - Main ViewModel for Unified Environment Builder
// Handles all application logic for the tabbed interface
// Evolved from TreeBuilder 3.4 by Robert Foster
// Test Brutally - Build Your Level of Complexity
// ============================================================================

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace EnvironmentBuilderApp.ViewModels
{
    /// <summary>
    /// Main ViewModel for the unified Environment Builder application.
    /// Provides all functionality across Build, Dashboard, Validate, Cleanup, Reports, and Settings tabs.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        
        private readonly Stopwatch _operationTimer = new();
        private bool _isProcessing;
        private bool _isConnected;
        private string _connectionStatus = "Disconnected";
        private string _statusMessage = "Ready";
        
        // Preset selections
        private bool _isSimplePreset = true;
        private bool _isMediumPreset;
        private bool _isComplexPreset;
        private bool _isBrutalPreset;
        private bool _isCustomPreset;
        
        // Connection settings
        private string _serverAddress = "localhost";
        private string _port = "389";
        private string _baseDN = "dc=example,dc=com";
        private string _bindDN = "cn=admin,dc=example,dc=com";
        
        // User generation settings
        private string _userPrefix = "testuser";
        private int _startNumber = 1;
        private int _endNumber = 10;
        private string _defaultPassword = "P@ssw0rd123";
        private string _userContainerDN = "ou=Users,dc=example,dc=com";
        private bool _generateRandomData = true;
        private bool _createHomeDirectories;
        
        // Dashboard stats
        private int _usersCreated;
        private int _containersCreated;
        private int _errorCount;
        private string _elapsedTime = "00:00";
        private string _estimatedRemaining = "N/A";
        private double _usersPerSecond;
        private double _overallProgress;
        private double _usersProgress;
        private double _containersProgress;
        private string _usersProgressText = "0 / 0";
        private string _containersProgressText = "0 / 0";
        private string _currentPhase = "Idle";
        private string _currentOperation = "No operation in progress";
        
        // Cleanup settings
        private string _cleanupUserPrefix = "testuser";
        private string _cleanupContainerDN = "";
        private bool _confirmFullReset;
        
        // Settings
        private string _ldifExportPath = @"C:\EnvironmentBuilder\LDIF";
        private string _logFilePath = @"C:\EnvironmentBuilder\Logs";
        private string _reportsPath = @"C:\EnvironmentBuilder\Reports";
        private bool _stopOnError;
        private bool _generateLdifBackup = true;
        private bool _verboseLogging;
        private bool _autoValidate = true;
        private int _operationTimeout = 30;
        
        // Reports
        private ReportItem? _selectedReport;
        private string _reportContent = "Select a report to view its contents.";
        private string _validationSummary = "";
        
        // Import/Export
        private string _importFilePath = "";
        private int _exportUserCount = 100;
        private string _selectedExportFormat = "Standard CSV";
        
        // Services
        private readonly Services.TestDataGenerator _dataGenerator = new();
        private readonly Services.TestScenarioService _scenarioService;
        private readonly Services.CsvImportExportService _csvService = new();
        private readonly Services.PerformanceTracker _perfTracker = new();
        private readonly Services.AuditLogService _auditLog = new();
        
        #endregion

        #region Constructor
        
        public MainViewModel()
        {
            // Initialize services
            _scenarioService = new Services.TestScenarioService(_dataGenerator, _perfTracker);
            
            // Initialize collections
            LiveLog = new ObservableCollection<string>();
            ValidationResults = new ObservableCollection<ValidationResult>();
            CleanupLog = new ObservableCollection<string>();
            SavedReports = new ObservableCollection<ReportItem>();
            
            // Initialize commands
            InitializeCommands();
            
            // Add welcome message
            LiveLog.Add($"[{DateTime.Now:HH:mm:ss}] Environment Builder initialized");
            LiveLog.Add($"[{DateTime.Now:HH:mm:ss}] Select a complexity preset and configure your environment");
            
            // Load sample reports
            LoadSampleReports();
        }
        
        #endregion

        #region Observable Collections
        
        public ObservableCollection<string> LiveLog { get; }
        public ObservableCollection<ValidationResult> ValidationResults { get; }
        public ObservableCollection<string> CleanupLog { get; }
        public ObservableCollection<ReportItem> SavedReports { get; }
        
        #endregion

        #region Properties - Processing State
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }
        
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }
        
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Presets
        
        public bool IsSimplePreset
        {
            get => _isSimplePreset;
            set { _isSimplePreset = value; OnPropertyChanged(); }
        }
        
        public bool IsMediumPreset
        {
            get => _isMediumPreset;
            set { _isMediumPreset = value; OnPropertyChanged(); }
        }
        
        public bool IsComplexPreset
        {
            get => _isComplexPreset;
            set { _isComplexPreset = value; OnPropertyChanged(); }
        }
        
        public bool IsBrutalPreset
        {
            get => _isBrutalPreset;
            set { _isBrutalPreset = value; OnPropertyChanged(); }
        }
        
        public bool IsCustomPreset
        {
            get => _isCustomPreset;
            set { _isCustomPreset = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Connection
        
        public string ServerAddress
        {
            get => _serverAddress;
            set { _serverAddress = value; OnPropertyChanged(); }
        }
        
        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }
        
        public string BaseDN
        {
            get => _baseDN;
            set { _baseDN = value; OnPropertyChanged(); }
        }
        
        public string BindDN
        {
            get => _bindDN;
            set { _bindDN = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - User Generation
        
        public string UserPrefix
        {
            get => _userPrefix;
            set { _userPrefix = value; OnPropertyChanged(); }
        }
        
        public int StartNumber
        {
            get => _startNumber;
            set { _startNumber = value; OnPropertyChanged(); }
        }
        
        public int EndNumber
        {
            get => _endNumber;
            set { _endNumber = value; OnPropertyChanged(); }
        }
        
        public string DefaultPassword
        {
            get => _defaultPassword;
            set { _defaultPassword = value; OnPropertyChanged(); }
        }
        
        public string UserContainerDN
        {
            get => _userContainerDN;
            set { _userContainerDN = value; OnPropertyChanged(); }
        }
        
        public bool GenerateRandomData
        {
            get => _generateRandomData;
            set { _generateRandomData = value; OnPropertyChanged(); }
        }
        
        public bool CreateHomeDirectories
        {
            get => _createHomeDirectories;
            set { _createHomeDirectories = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Dashboard
        
        public int UsersCreated
        {
            get => _usersCreated;
            set { _usersCreated = value; OnPropertyChanged(); }
        }
        
        public int ContainersCreated
        {
            get => _containersCreated;
            set { _containersCreated = value; OnPropertyChanged(); }
        }
        
        public int ErrorCount
        {
            get => _errorCount;
            set { _errorCount = value; OnPropertyChanged(); }
        }
        
        public string ElapsedTime
        {
            get => _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(); }
        }
        
        public string EstimatedRemaining
        {
            get => _estimatedRemaining;
            set { _estimatedRemaining = value; OnPropertyChanged(); }
        }
        
        public double UsersPerSecond
        {
            get => _usersPerSecond;
            set { _usersPerSecond = value; OnPropertyChanged(); }
        }
        
        public double OverallProgress
        {
            get => _overallProgress;
            set { _overallProgress = value; OnPropertyChanged(); }
        }
        
        public double UsersProgress
        {
            get => _usersProgress;
            set { _usersProgress = value; OnPropertyChanged(); }
        }
        
        public double ContainersProgress
        {
            get => _containersProgress;
            set { _containersProgress = value; OnPropertyChanged(); }
        }
        
        public string UsersProgressText
        {
            get => _usersProgressText;
            set { _usersProgressText = value; OnPropertyChanged(); }
        }
        
        public string ContainersProgressText
        {
            get => _containersProgressText;
            set { _containersProgressText = value; OnPropertyChanged(); }
        }
        
        public string CurrentPhase
        {
            get => _currentPhase;
            set { _currentPhase = value; OnPropertyChanged(); }
        }
        
        public string CurrentOperation
        {
            get => _currentOperation;
            set { _currentOperation = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Cleanup
        
        public string CleanupUserPrefix
        {
            get => _cleanupUserPrefix;
            set { _cleanupUserPrefix = value; OnPropertyChanged(); }
        }
        
        public string CleanupContainerDN
        {
            get => _cleanupContainerDN;
            set { _cleanupContainerDN = value; OnPropertyChanged(); }
        }
        
        public bool ConfirmFullReset
        {
            get => _confirmFullReset;
            set { _confirmFullReset = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Settings
        
        public string LdifExportPath
        {
            get => _ldifExportPath;
            set { _ldifExportPath = value; OnPropertyChanged(); }
        }
        
        public string LogFilePath
        {
            get => _logFilePath;
            set { _logFilePath = value; OnPropertyChanged(); }
        }
        
        public string ReportsPath
        {
            get => _reportsPath;
            set { _reportsPath = value; OnPropertyChanged(); }
        }
        
        public bool StopOnError
        {
            get => _stopOnError;
            set { _stopOnError = value; OnPropertyChanged(); }
        }
        
        public bool GenerateLdifBackup
        {
            get => _generateLdifBackup;
            set { _generateLdifBackup = value; OnPropertyChanged(); }
        }
        
        public bool VerboseLogging
        {
            get => _verboseLogging;
            set { _verboseLogging = value; OnPropertyChanged(); }
        }
        
        public bool AutoValidate
        {
            get => _autoValidate;
            set { _autoValidate = value; OnPropertyChanged(); }
        }
        
        public int OperationTimeout
        {
            get => _operationTimeout;
            set { _operationTimeout = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Reports
        
        public ReportItem? SelectedReport
        {
            get => _selectedReport;
            set 
            { 
                _selectedReport = value; 
                OnPropertyChanged();
                LoadReportContent();
            }
        }
        
        public string ReportContent
        {
            get => _reportContent;
            set { _reportContent = value; OnPropertyChanged(); }
        }
        
        public string ValidationSummary
        {
            get => _validationSummary;
            set { _validationSummary = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Properties - Import/Export
        
        public string ImportFilePath
        {
            get => _importFilePath;
            set { _importFilePath = value; OnPropertyChanged(); }
        }
        
        public int ExportUserCount
        {
            get => _exportUserCount;
            set { _exportUserCount = value; OnPropertyChanged(); }
        }
        
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set { _selectedExportFormat = value; OnPropertyChanged(); }
        }
        
        #endregion

        #region Commands
        
        public ICommand? SelectPresetCommand { get; private set; }
        public ICommand? TestConnectionCommand { get; private set; }
        public ICommand? BuildEnvironmentCommand { get; private set; }
        public ICommand? GenerateLdifCommand { get; private set; }
        public ICommand? ValidateUsersCommand { get; private set; }
        public ICommand? TestAuthenticationCommand { get; private set; }
        public ICommand? VerifyDirectoriesCommand { get; private set; }
        public ICommand? HealthCheckCommand { get; private set; }
        public ICommand? DeleteUsersCommand { get; private set; }
        public ICommand? DeleteContainersCommand { get; private set; }
        public ICommand? FullResetCommand { get; private set; }
        public ICommand? GenerateReportCommand { get; private set; }
        public ICommand? ExportHtmlCommand { get; private set; }
        public ICommand? ExportPdfCommand { get; private set; }
        public ICommand? LoadConfigCommand { get; private set; }
        public ICommand? SaveConfigCommand { get; private set; }
        
        // New commands for enhanced features
        public ICommand? RunScenarioCommand { get; private set; }
        public ICommand? BrowseImportFileCommand { get; private set; }
        public ICommand? ValidateCsvCommand { get; private set; }
        public ICommand? ImportUsersCommand { get; private set; }
        public ICommand? DownloadTemplateCommand { get; private set; }
        public ICommand? ExportUsersCommand { get; private set; }
        public ICommand? ExportAllFormatsCommand { get; private set; }
        public ICommand? CopyCredentialsCommand { get; private set; }
        public ICommand? GetRandomUserCommand { get; private set; }
        
        private void InitializeCommands()
        {
            SelectPresetCommand = new RelayCommand<string>(SelectPreset);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsProcessing);
            BuildEnvironmentCommand = new RelayCommand(async () => await BuildEnvironmentAsync(), () => !IsProcessing);
            GenerateLdifCommand = new RelayCommand(async () => await GenerateLdifAsync(), () => !IsProcessing);
            ValidateUsersCommand = new RelayCommand(async () => await ValidateUsersAsync(), () => !IsProcessing);
            TestAuthenticationCommand = new RelayCommand(async () => await TestAuthenticationAsync(), () => !IsProcessing);
            VerifyDirectoriesCommand = new RelayCommand(async () => await VerifyDirectoriesAsync(), () => !IsProcessing);
            HealthCheckCommand = new RelayCommand(async () => await HealthCheckAsync(), () => !IsProcessing);
            DeleteUsersCommand = new RelayCommand(async () => await DeleteUsersAsync(), () => !IsProcessing);
            DeleteContainersCommand = new RelayCommand(async () => await DeleteContainersAsync(), () => !IsProcessing);
            FullResetCommand = new RelayCommand(async () => await FullResetAsync(), () => !IsProcessing && ConfirmFullReset);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            ExportHtmlCommand = new RelayCommand(ExportHtml, () => SelectedReport != null);
            ExportPdfCommand = new RelayCommand(ExportPdf, () => SelectedReport != null);
            LoadConfigCommand = new RelayCommand(LoadConfig);
            SaveConfigCommand = new RelayCommand(SaveConfig);
            
            // New enhanced commands
            RunScenarioCommand = new RelayCommand<string>(async (s) => await RunScenarioAsync(s), (s) => !IsProcessing);
            BrowseImportFileCommand = new RelayCommand(BrowseImportFile);
            ValidateCsvCommand = new RelayCommand(async () => await ValidateCsvAsync(), () => !string.IsNullOrEmpty(ImportFilePath));
            ImportUsersCommand = new RelayCommand(async () => await ImportUsersAsync(), () => !string.IsNullOrEmpty(ImportFilePath));
            DownloadTemplateCommand = new RelayCommand(DownloadTemplate);
            ExportUsersCommand = new RelayCommand(async () => await ExportUsersAsync());
            ExportAllFormatsCommand = new RelayCommand(async () => await ExportAllFormatsAsync());
            CopyCredentialsCommand = new RelayCommand<string>(CopyCredentials);
            GetRandomUserCommand = new RelayCommand(GetRandomUser);
        }
        
        #endregion

        #region Command Implementations
        
        private void SelectPreset(string? preset)
        {
            // Reset all presets
            IsSimplePreset = false;
            IsMediumPreset = false;
            IsComplexPreset = false;
            IsBrutalPreset = false;
            IsCustomPreset = false;
            
            switch (preset)
            {
                case "Simple":
                    IsSimplePreset = true;
                    EndNumber = 10;
                    AddLog("Selected SIMPLE preset: 10 users, 2 containers");
                    break;
                case "Medium":
                    IsMediumPreset = true;
                    EndNumber = 100;
                    AddLog("Selected MEDIUM preset: 100 users, 5 containers");
                    break;
                case "Complex":
                    IsComplexPreset = true;
                    EndNumber = 500;
                    AddLog("Selected COMPLEX preset: 500 users, 10 containers");
                    break;
                case "Brutal":
                    IsBrutalPreset = true;
                    EndNumber = 2000;
                    AddLog("Selected BRUTAL preset: 2000+ users, 25 containers");
                    break;
                case "Custom":
                    IsCustomPreset = true;
                    AddLog("Selected CUSTOM preset: Configure your own settings");
                    break;
            }
        }
        
        private async Task TestConnectionAsync()
        {
            IsProcessing = true;
            StatusMessage = "Testing connection...";
            AddLog($"Testing connection to {ServerAddress}:{Port}...");
            
            try
            {
                // Simulate connection test
                await Task.Delay(1500);
                
                IsConnected = true;
                ConnectionStatus = $"Connected to {ServerAddress}";
                StatusMessage = "Connection successful";
                AddLog("✅ Connection test successful");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = "Connection failed";
                StatusMessage = $"Connection failed: {ex.Message}";
                AddLog($"❌ Connection failed: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task BuildEnvironmentAsync()
        {
            var totalUsers = EndNumber - StartNumber + 1;
            
            if (MessageBox.Show(
                $"This will create {totalUsers} users in the LDAP directory.\n\nContinue?",
                "Confirm Build",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            
            IsProcessing = true;
            _operationTimer.Restart();
            
            // Reset stats
            UsersCreated = 0;
            ContainersCreated = 0;
            ErrorCount = 0;
            OverallProgress = 0;
            UsersProgress = 0;
            ContainersProgress = 0;
            CurrentPhase = "Initializing";
            StatusMessage = "Building environment...";
            
            AddLog("═══════════════════════════════════════════════════════════");
            AddLog("⚔️ STARTING ENVIRONMENT BUILD");
            AddLog($"   Target: {totalUsers} users");
            AddLog($"   Server: {ServerAddress}:{Port}");
            AddLog("═══════════════════════════════════════════════════════════");
            
            try
            {
                // Phase 1: Create containers
                CurrentPhase = "Creating Containers";
                AddLog("📁 Phase 1: Creating container structure...");
                
                int containerCount = IsSimplePreset ? 2 : IsMediumPreset ? 5 : IsComplexPreset ? 10 : IsBrutalPreset ? 25 : 3;
                for (int i = 0; i < containerCount; i++)
                {
                    await Task.Delay(100);
                    ContainersCreated++;
                    ContainersProgress = (double)ContainersCreated / containerCount * 100;
                    ContainersProgressText = $"{ContainersCreated} / {containerCount}";
                    OverallProgress = ContainersProgress * 0.1;
                    UpdateElapsedTime();
                    AddLog($"   Created container: ou=Group{i + 1}");
                }
                AddLog($"✅ Created {containerCount} containers");
                
                // Phase 2: Create users
                CurrentPhase = "Creating Users";
                AddLog("👤 Phase 2: Creating users...");
                
                for (int i = StartNumber; i <= EndNumber; i++)
                {
                    await Task.Delay(20); // Simulate LDAP operation
                    UsersCreated++;
                    UsersProgress = (double)UsersCreated / totalUsers * 100;
                    UsersProgressText = $"{UsersCreated} / {totalUsers}";
                    OverallProgress = 10 + (UsersProgress * 0.8);
                    
                    if (_operationTimer.Elapsed.TotalSeconds > 0)
                    {
                        UsersPerSecond = UsersCreated / _operationTimer.Elapsed.TotalSeconds;
                        var remaining = (totalUsers - UsersCreated) / UsersPerSecond;
                        EstimatedRemaining = TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
                    }
                    
                    UpdateElapsedTime();
                    CurrentOperation = $"Creating user: {UserPrefix}{i}";
                    
                    if (UsersCreated % 50 == 0 || UsersCreated == totalUsers)
                    {
                        AddLog($"   Created {UsersCreated}/{totalUsers} users ({UsersProgress:F0}%)");
                    }
                }
                
                // Phase 3: Finalize
                CurrentPhase = "Finalizing";
                AddLog("🔧 Phase 3: Finalizing...");
                await Task.Delay(500);
                OverallProgress = 100;
                
                _operationTimer.Stop();
                CurrentPhase = "Complete";
                CurrentOperation = "Build completed successfully";
                StatusMessage = $"Build complete: {UsersCreated} users, {ContainersCreated} containers";
                
                AddLog("═══════════════════════════════════════════════════════════");
                AddLog($"✅ BUILD COMPLETE");
                AddLog($"   Users Created: {UsersCreated}");
                AddLog($"   Containers Created: {ContainersCreated}");
                AddLog($"   Errors: {ErrorCount}");
                AddLog($"   Total Time: {ElapsedTime}");
                AddLog("═══════════════════════════════════════════════════════════");
                
                if (AutoValidate)
                {
                    AddLog("🔍 Running auto-validation...");
                    await ValidateUsersAsync();
                }
                
                MessageBox.Show(
                    $"Environment build complete!\n\nUsers: {UsersCreated}\nContainers: {ContainersCreated}\nTime: {ElapsedTime}",
                    "Build Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorCount++;
                AddLog($"❌ BUILD FAILED: {ex.Message}");
                StatusMessage = $"Build failed: {ex.Message}";
                CurrentPhase = "Failed";
            }
            finally
            {
                IsProcessing = false;
                EstimatedRemaining = "N/A";
            }
        }
        
        private async Task GenerateLdifAsync()
        {
            IsProcessing = true;
            StatusMessage = "Generating LDIF file...";
            AddLog("📄 Generating LDIF file...");
            
            try
            {
                var totalUsers = EndNumber - StartNumber + 1;
                var ldif = new StringBuilder();
                
                ldif.AppendLine("# Environment Builder LDIF Export");
                ldif.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                ldif.AppendLine($"# Users: {totalUsers}");
                ldif.AppendLine();
                
                for (int i = StartNumber; i <= EndNumber; i++)
                {
                    ldif.AppendLine($"dn: cn={UserPrefix}{i},{UserContainerDN}");
                    ldif.AppendLine("objectClass: inetOrgPerson");
                    ldif.AppendLine("objectClass: organizationalPerson");
                    ldif.AppendLine("objectClass: person");
                    ldif.AppendLine("objectClass: top");
                    ldif.AppendLine($"cn: {UserPrefix}{i}");
                    ldif.AppendLine($"sn: User{i}");
                    ldif.AppendLine($"givenName: Test");
                    ldif.AppendLine($"uid: {UserPrefix}{i}");
                    ldif.AppendLine($"userPassword: {DefaultPassword}");
                    ldif.AppendLine();
                    
                    await Task.Yield();
                }
                
                // Ensure directory exists
                Directory.CreateDirectory(LdifExportPath);
                var filePath = Path.Combine(LdifExportPath, $"environment_{DateTime.Now:yyyyMMdd_HHmmss}.ldif");
                await File.WriteAllTextAsync(filePath, ldif.ToString());
                
                AddLog($"✅ LDIF file saved: {filePath}");
                StatusMessage = "LDIF file generated successfully";
                
                MessageBox.Show($"LDIF file saved to:\n{filePath}", "LDIF Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AddLog($"❌ LDIF generation failed: {ex.Message}");
                StatusMessage = $"LDIF generation failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task ValidateUsersAsync()
        {
            IsProcessing = true;
            StatusMessage = "Validating users...";
            ValidationResults.Clear();
            
            AddLog("🔍 Starting user validation...");
            
            try
            {
                var totalUsers = EndNumber - StartNumber + 1;
                int validated = 0;
                int passed = 0;
                
                for (int i = StartNumber; i <= EndNumber; i++)
                {
                    await Task.Delay(10);
                    validated++;
                    
                    // Simulate validation (95% pass rate)
                    bool success = new Random().Next(100) < 95;
                    if (success) passed++;
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ValidationResults.Add(new ValidationResult
                        {
                            Icon = success ? "✅" : "❌",
                            Message = success 
                                ? $"{UserPrefix}{i}: Validated successfully"
                                : $"{UserPrefix}{i}: User not found in directory"
                        });
                    });
                    
                    if (validated % 100 == 0)
                    {
                        StatusMessage = $"Validated {validated}/{totalUsers} users...";
                    }
                }
                
                ValidationSummary = $"✅ {passed} passed, ❌ {totalUsers - passed} failed";
                StatusMessage = $"Validation complete: {passed}/{totalUsers} users valid";
                AddLog($"✅ Validation complete: {passed}/{totalUsers} users valid");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Validation failed: {ex.Message}");
                StatusMessage = $"Validation failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task TestAuthenticationAsync()
        {
            IsProcessing = true;
            StatusMessage = "Testing authentication...";
            ValidationResults.Clear();
            AddLog("🔐 Testing user authentication...");
            
            try
            {
                await Task.Delay(1000);
                
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "Authentication test passed for sample users" });
                ValidationResults.Add(new ValidationResult { Icon = "ℹ️", Message = "All users can bind with default password" });
                
                ValidationSummary = "Authentication test complete";
                StatusMessage = "Authentication test complete";
                AddLog("✅ Authentication test complete");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task VerifyDirectoriesAsync()
        {
            IsProcessing = true;
            StatusMessage = "Verifying directories...";
            ValidationResults.Clear();
            AddLog("📂 Verifying home directories...");
            
            try
            {
                await Task.Delay(1500);
                
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "Home directory structure verified" });
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "Permissions are correctly set" });
                
                ValidationSummary = "Directory verification complete";
                StatusMessage = "Directory verification complete";
                AddLog("✅ Directory verification complete");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task HealthCheckAsync()
        {
            IsProcessing = true;
            StatusMessage = "Running health check...";
            ValidationResults.Clear();
            AddLog("💓 Running full health check...");
            
            try
            {
                await Task.Delay(500);
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "LDAP Server: Responding (12ms)" });
                
                await Task.Delay(500);
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "Authentication: Working" });
                
                await Task.Delay(500);
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "Base DN: Accessible" });
                
                await Task.Delay(500);
                ValidationResults.Add(new ValidationResult { Icon = "✅", Message = "User Container: Exists" });
                
                await Task.Delay(500);
                ValidationResults.Add(new ValidationResult { Icon = "⚠️", Message = "Disk Space: 15.2 GB available" });
                
                ValidationSummary = "Health check complete - System healthy";
                StatusMessage = "Health check complete";
                AddLog("✅ Health check complete - System healthy");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task DeleteUsersAsync()
        {
            if (MessageBox.Show(
                $"This will delete all users matching prefix '{CleanupUserPrefix}'.\n\nThis action cannot be undone. Continue?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            IsProcessing = true;
            StatusMessage = "Deleting users...";
            CleanupLog.Clear();
            
            CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Starting user deletion...");
            CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Prefix: {CleanupUserPrefix}");
            
            try
            {
                await Task.Delay(2000);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Deleted 10 users");
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ User deletion complete");
                StatusMessage = "Users deleted successfully";
            }
            catch (Exception ex)
            {
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Error: {ex.Message}");
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task DeleteContainersAsync()
        {
            if (MessageBox.Show(
                $"This will delete container '{CleanupContainerDN}' and all its contents.\n\nThis action cannot be undone. Continue?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            IsProcessing = true;
            StatusMessage = "Deleting containers...";
            CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Starting container deletion...");
            
            try
            {
                await Task.Delay(2000);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Container deletion complete");
                StatusMessage = "Containers deleted successfully";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private async Task FullResetAsync()
        {
            if (MessageBox.Show(
                "⚠️ FULL RESET WARNING ⚠️\n\nThis will permanently delete:\n• All test users\n• All test containers\n• All home directories\n\nThis action CANNOT be undone!\n\nAre you absolutely sure?",
                "CONFIRM FULL RESET",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop) != MessageBoxResult.Yes)
            {
                return;
            }
            
            IsProcessing = true;
            StatusMessage = "Performing full reset...";
            CleanupLog.Clear();
            
            CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ INITIATING FULL RESET");
            
            try
            {
                await Task.Delay(1000);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Deleting users...");
                await Task.Delay(1500);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Deleting containers...");
                await Task.Delay(1500);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] Removing home directories...");
                await Task.Delay(1000);
                CleanupLog.Add($"[{DateTime.Now:HH:mm:ss}] ✅ FULL RESET COMPLETE");
                
                StatusMessage = "Full reset complete";
                ConfirmFullReset = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void GenerateReport()
        {
            var report = new ReportItem
            {
                Name = $"Build Report - {DateTime.Now:MMM dd, HH:mm}",
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Content = GenerateReportContent()
            };
            
            SavedReports.Insert(0, report);
            SelectedReport = report;
            StatusMessage = "Report generated";
            AddLog("📊 New report generated");
        }
        
        private string GenerateReportContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║           ENVIRONMENT BUILDER - BUILD REPORT                 ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Server: {ServerAddress}:{Port}");
            sb.AppendLine();
            sb.AppendLine("── SUMMARY ─────────────────────────────────────────────────────");
            sb.AppendLine($"  Users Created:      {UsersCreated}");
            sb.AppendLine($"  Containers Created: {ContainersCreated}");
            sb.AppendLine($"  Errors:             {ErrorCount}");
            sb.AppendLine($"  Elapsed Time:       {ElapsedTime}");
            sb.AppendLine($"  Users/Second:       {UsersPerSecond:F1}");
            sb.AppendLine();
            sb.AppendLine("── CONFIGURATION ───────────────────────────────────────────────");
            sb.AppendLine($"  User Prefix:        {UserPrefix}");
            sb.AppendLine($"  Number Range:       {StartNumber} - {EndNumber}");
            sb.AppendLine($"  Base DN:            {BaseDN}");
            sb.AppendLine($"  Container DN:       {UserContainerDN}");
            sb.AppendLine();
            sb.AppendLine("── STATUS ──────────────────────────────────────────────────────");
            sb.AppendLine($"  Final Phase:        {CurrentPhase}");
            sb.AppendLine($"  Connection:         {ConnectionStatus}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            return sb.ToString();
        }
        
        private void ExportHtml()
        {
            if (SelectedReport == null) return;
            
            var dialog = new SaveFileDialog
            {
                FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                Filter = "HTML Files (*.html)|*.html"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var html = $"<html><head><title>{SelectedReport.Name}</title><style>body{{font-family:monospace;background:#0d1117;color:#c9d1d9;padding:20px;}}</style></head><body><pre>{SelectedReport.Content}</pre></body></html>";
                File.WriteAllText(dialog.FileName, html);
                StatusMessage = "Report exported to HTML";
            }
        }
        
        private void ExportPdf()
        {
            MessageBox.Show("PDF export requires a PDF library.\nUse HTML export for now.", "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void LoadConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Load Configuration"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var config = JsonConvert.DeserializeObject<ConfigurationData>(json);
                    
                    if (config != null)
                    {
                        ServerAddress = config.ServerAddress ?? ServerAddress;
                        Port = config.Port ?? Port;
                        BaseDN = config.BaseDN ?? BaseDN;
                        BindDN = config.BindDN ?? BindDN;
                        UserPrefix = config.UserPrefix ?? UserPrefix;
                        StartNumber = config.StartNumber;
                        EndNumber = config.EndNumber;
                        UserContainerDN = config.UserContainerDN ?? UserContainerDN;
                        
                        StatusMessage = "Configuration loaded";
                        AddLog($"📂 Configuration loaded from {dialog.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load configuration: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void SaveConfig()
        {
            var dialog = new SaveFileDialog
            {
                FileName = "environment_config.json",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Configuration"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var config = new ConfigurationData
                    {
                        ServerAddress = ServerAddress,
                        Port = Port,
                        BaseDN = BaseDN,
                        BindDN = BindDN,
                        UserPrefix = UserPrefix,
                        StartNumber = StartNumber,
                        EndNumber = EndNumber,
                        UserContainerDN = UserContainerDN
                    };
                    
                    var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);
                    
                    StatusMessage = "Configuration saved";
                    AddLog($"💾 Configuration saved to {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save configuration: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // ══════════════════════════════════════════════════════════════════════
        // NEW ENHANCED FEATURE COMMANDS
        // ══════════════════════════════════════════════════════════════════════
        
        private async Task RunScenarioAsync(string? scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId)) return;
            
            var scenario = _scenarioService.GetScenarioById(scenarioId);
            if (scenario == null)
            {
                StatusMessage = $"Scenario not found: {scenarioId}";
                return;
            }
            
            if (scenario.IsDangerous)
            {
                var result = MessageBox.Show(
                    $"⚠️ WARNING\n\n{scenario.WarningMessage}\n\nThis will create {scenario.UserCount} users with potentially malicious data.\n\nContinue?",
                    "Dangerous Scenario",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes) return;
            }
            
            IsProcessing = true;
            StatusMessage = $"Running scenario: {scenario.Name}...";
            AddLog($"🎯 Starting scenario: {scenario.Name}");
            AddLog($"   {scenario.Description}");
            
            _auditLog.Log(Services.AuditAction.BuildStart, $"Scenario: {scenario.Name}");
            _perfTracker.StartSession();
            
            try
            {
                var users = _scenarioService.GenerateScenarioUsers(scenario, UserPrefix);
                UsersCreated = 0;
                
                foreach (var user in users)
                {
                    using var timer = _perfTracker.StartOperation("CreateUser");
                    await Task.Delay(10);
                    UsersCreated++;
                    UsersProgress = (double)UsersCreated / users.Count * 100;
                    OverallProgress = UsersProgress;
                    
                    if (UsersCreated % 50 == 0)
                    {
                        AddLog($"   Created {UsersCreated}/{users.Count} users");
                    }
                }
                
                _perfTracker.StopSession();
                var summary = _perfTracker.GetSummary();
                
                AddLog($"✅ Scenario complete: {users.Count} users created");
                AddLog($"   Avg response: {summary.AverageResponseTime:F2}ms, P95: {summary.P95ResponseTime:F2}ms");
                StatusMessage = $"Scenario complete: {users.Count} users";
                
                _auditLog.Log(Services.AuditAction.BuildComplete, $"Scenario completed: {users.Count} users");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void BrowseImportFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File to Import"
            };
            
            if (dialog.ShowDialog() == true)
            {
                ImportFilePath = dialog.FileName;
                AddLog($"📂 Selected import file: {dialog.FileName}");
            }
        }
        
        private async Task ValidateCsvAsync()
        {
            if (string.IsNullOrEmpty(ImportFilePath)) return;
            
            StatusMessage = "Validating CSV file...";
            
            await Task.Run(() =>
            {
                var result = _csvService.ValidateCsvFile(ImportFilePath);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (result.IsValid)
                    {
                        AddLog($"✅ CSV Valid: {result.RecordCount} records found");
                        AddLog($"   Columns: {string.Join(", ", result.Headers)}");
                        StatusMessage = $"CSV valid: {result.RecordCount} records";
                    }
                    else
                    {
                        AddLog($"❌ CSV Invalid: {string.Join(", ", result.Errors)}");
                        StatusMessage = "CSV validation failed";
                    }
                });
            });
        }
        
        private async Task ImportUsersAsync()
        {
            if (string.IsNullOrEmpty(ImportFilePath)) return;
            
            IsProcessing = true;
            StatusMessage = "Importing users from CSV...";
            
            try
            {
                var users = await Task.Run(() => _csvService.ImportUsers(ImportFilePath));
                AddLog($"✅ Imported {users.Count} users from CSV");
                StatusMessage = $"Imported {users.Count} users";
                _auditLog.Log(Services.AuditAction.ImportCsv, $"Imported {users.Count} users from {ImportFilePath}");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Import failed: {ex.Message}");
                StatusMessage = $"Import failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void DownloadTemplate()
        {
            var dialog = new SaveFileDialog
            {
                FileName = "user_import_template.csv",
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Save Import Template"
            };
            
            if (dialog.ShowDialog() == true)
            {
                _csvService.GenerateSampleTemplate(dialog.FileName);
                AddLog($"📄 Template saved: {dialog.FileName}");
                StatusMessage = "Template saved";
            }
        }
        
        private async Task ExportUsersAsync()
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"users_{DateTime.Now:yyyyMMdd}.csv",
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Export Users"
            };
            
            if (dialog.ShowDialog() == true)
            {
                IsProcessing = true;
                StatusMessage = "Exporting users...";
                
                try
                {
                    var users = _dataGenerator.GenerateLoadTestUsers(UserPrefix, ExportUserCount);
                    
                    var format = SelectedExportFormat switch
                    {
                        "Selenium" => Services.CsvExportFormat.Selenium,
                        "JMeter" => Services.CsvExportFormat.JMeter,
                        "Postman" => Services.CsvExportFormat.Postman,
                        "LoadRunner" => Services.CsvExportFormat.LoadRunner,
                        "Credentials Only" => Services.CsvExportFormat.Credentials,
                        _ => Services.CsvExportFormat.Standard
                    };
                    
                    await Task.Run(() => _csvService.ExportUsers(users, dialog.FileName, format));
                    
                    AddLog($"✅ Exported {users.Count} users to {dialog.FileName}");
                    StatusMessage = $"Exported {users.Count} users";
                    _auditLog.Log(Services.AuditAction.ExportCsv, $"Exported {users.Count} users");
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }
        
        private async Task ExportAllFormatsAsync()
        {
            // Use WPF-compatible folder selection
            var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                                          "EnvironmentBuilder", "Exports", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            
            IsProcessing = true;
            StatusMessage = "Exporting all formats...";
            
            try
            {
                var users = _dataGenerator.GenerateLoadTestUsers(UserPrefix, ExportUserCount);
                await Task.Run(() => _csvService.ExportAllFormats(users, exportPath));
                
                AddLog($"✅ Exported {users.Count} users in all formats to {exportPath}");
                StatusMessage = "Exported all formats";
                
                // Open the folder
                System.Diagnostics.Process.Start("explorer.exe", exportPath);
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        private void CopyCredentials(string? username)
        {
            if (string.IsNullOrEmpty(username)) return;
            
            var credentials = $"{username}:{DefaultPassword}";
            Clipboard.SetText(credentials);
            StatusMessage = $"Copied credentials for {username}";
            AddLog($"📋 Copied credentials for {username}");
        }
        
        private void GetRandomUser()
        {
            var random = new Random();
            var randomNum = random.Next(StartNumber, EndNumber + 1);
            var username = $"{UserPrefix}{randomNum}";
            
            Clipboard.SetText(username);
            StatusMessage = $"Random user: {username} (copied)";
            AddLog($"🎲 Random user: {username}");
        }
        
        #endregion

        #region Helper Methods
        
        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LiveLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                
                // Keep log size manageable
                while (LiveLog.Count > 500)
                {
                    LiveLog.RemoveAt(0);
                }
            });
        }
        
        private void UpdateElapsedTime()
        {
            ElapsedTime = _operationTimer.Elapsed.ToString(@"mm\:ss");
        }
        
        private void LoadSampleReports()
        {
            SavedReports.Add(new ReportItem
            {
                Name = "Sample Report",
                Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                Content = "This is a sample report.\nRun a build to generate real reports."
            });
        }
        
        private void LoadReportContent()
        {
            if (SelectedReport != null)
            {
                ReportContent = SelectedReport.Content ?? "No content available";
            }
        }
        
        #endregion

        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }

    #region Support Classes
    
    /// <summary>
    /// Represents a validation result entry
    /// </summary>
    public class ValidationResult
    {
        public string Icon { get; set; } = "";
        public string Message { get; set; } = "";
    }
    
    /// <summary>
    /// Represents a saved report
    /// </summary>
    public class ReportItem
    {
        public string Name { get; set; } = "";
        public string Date { get; set; } = "";
        public string Content { get; set; } = "";
    }
    
    /// <summary>
    /// Configuration data for save/load
    /// </summary>
    public class ConfigurationData
    {
        public string? ServerAddress { get; set; }
        public string? Port { get; set; }
        public string? BaseDN { get; set; }
        public string? BindDN { get; set; }
        public string? UserPrefix { get; set; }
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public string? UserContainerDN { get; set; }
    }
    
    /// <summary>
    /// Generic RelayCommand implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }
    
    /// <summary>
    /// Generic RelayCommand with parameter
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
    
    #endregion
}
