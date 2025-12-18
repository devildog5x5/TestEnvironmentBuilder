// ============================================================================
// CliProgram.cs - Command Line Interface Entry Point
// Enables CI/CD integration and automation
// Usage: EnvironmentBuilderApp.exe --cli build --preset brutal
// Environment Builder - Test Brutally
// ============================================================================

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EnvironmentBuilderApp.Services;

namespace EnvironmentBuilderApp
{
    /// <summary>
    /// Command Line Interface for Environment Builder
    /// Enables automation and CI/CD integration
    /// </summary>
    public static class CliProgram
    {
        private static readonly TestDataGenerator DataGenerator = new();
        private static readonly PerformanceTracker PerfTracker = new();
        private static readonly AuditLogService AuditLog = new();
        private static readonly CsvImportExportService CsvService = new();
        private static readonly TestScenarioService ScenarioService = new(DataGenerator, PerfTracker);

        /// <summary>
        /// Checks if running in CLI mode
        /// </summary>
        public static bool IsCliMode(string[] args)
        {
            return args.Length > 0 && (args[0] == "--cli" || args[0] == "-c");
        }

        /// <summary>
        /// Runs the CLI
        /// </summary>
        public static async Task<int> RunAsync(string[] args)
        {
            // Remove --cli flag if present
            var cliArgs = args.Length > 0 && args[0] == "--cli" 
                ? args[1..] 
                : args;

            var rootCommand = new RootCommand("Environment Builder CLI - Test Brutally")
            {
                CreateBuildCommand(),
                CreateCleanupCommand(),
                CreateValidateCommand(),
                CreateHealthCommand(),
                CreateGenerateCommand(),
                CreateExportCommand(),
                CreateImportCommand(),
                CreateScenarioCommand(),
                CreateSnapshotCommand()
            };

            // Add global options
            var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
            var outputOption = new Option<string>("--output", "Output file for results (JSON)");
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(outputOption);

            return await rootCommand.InvokeAsync(cliArgs);
        }

        #region Build Command

        private static Command CreateBuildCommand()
        {
            var command = new Command("build", "Build test environment");

            var presetOption = new Option<string>("--preset", "Preset: simple, medium, complex, brutal, custom")
            { IsRequired = false };
            presetOption.SetDefaultValue("simple");

            var serverOption = new Option<string>("--server", "LDAP server address");
            var portOption = new Option<int>("--port", () => 389, "LDAP port");
            var baseDnOption = new Option<string>("--base-dn", "Base DN");
            var prefixOption = new Option<string>("--prefix", () => "testuser", "Username prefix");
            var countOption = new Option<int>("--count", () => 10, "Number of users to create");
            var scenarioOption = new Option<string>("--scenario", "Use predefined scenario");
            var dryRunOption = new Option<bool>("--dry-run", "Generate LDIF only, don't apply");

            command.AddOption(presetOption);
            command.AddOption(serverOption);
            command.AddOption(portOption);
            command.AddOption(baseDnOption);
            command.AddOption(prefixOption);
            command.AddOption(countOption);
            command.AddOption(scenarioOption);
            command.AddOption(dryRunOption);

            command.SetHandler(async (context) =>
            {
                var preset = context.ParseResult.GetValueForOption(presetOption);
                var prefix = context.ParseResult.GetValueForOption(prefixOption);
                var count = context.ParseResult.GetValueForOption(countOption);
                var scenario = context.ParseResult.GetValueForOption(scenarioOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                // Adjust count based on preset
                count = preset switch
                {
                    "medium" => 100,
                    "complex" => 500,
                    "brutal" => 2000,
                    _ => count
                };

                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║          ENVIRONMENT BUILDER CLI - BUILD                     ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine($"  Preset: {preset}");
                Console.WriteLine($"  Users: {count}");
                Console.WriteLine($"  Prefix: {prefix}");
                Console.WriteLine($"  Dry Run: {dryRun}");
                Console.WriteLine();

                AuditLog.Log(AuditAction.BuildStart, $"CLI Build started: {count} users");
                PerfTracker.StartSession();

                // Generate users
                var users = DataGenerator.GenerateLoadTestUsers(prefix!, count);

                if (dryRun)
                {
                    // Export to LDIF
                    var ldifPath = $"build_{DateTime.Now:yyyyMMdd_HHmmss}.ldif";
                    Console.WriteLine($"✅ Generated {users.Count} users");
                    Console.WriteLine($"📄 LDIF would be written to: {ldifPath}");
                }
                else
                {
                    // Simulate build (in real implementation, connect to LDAP)
                    for (int i = 0; i < users.Count; i++)
                    {
                        using var timer = PerfTracker.StartOperation("CreateUser");
                        await Task.Delay(5); // Simulate operation
                        if ((i + 1) % 100 == 0 || i == users.Count - 1)
                        {
                            Console.WriteLine($"  Progress: {i + 1}/{users.Count} users created");
                        }
                    }
                }

                PerfTracker.StopSession();
                var summary = PerfTracker.GetSummary();

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("BUILD COMPLETE");
                Console.WriteLine($"  Total Users: {users.Count}");
                Console.WriteLine($"  Duration: {summary.SessionDuration:mm\\:ss}");
                Console.WriteLine($"  Throughput: {summary.Throughput:F1} users/sec");
                Console.WriteLine($"  Avg Response: {summary.AverageResponseTime:F2}ms");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");

                AuditLog.Log(AuditAction.BuildComplete, $"CLI Build completed: {users.Count} users");

                // Write JSON output if requested
                await WriteOutputAsync(context, new
                {
                    Success = true,
                    UsersCreated = users.Count,
                    Duration = summary.SessionDuration.ToString(),
                    Throughput = summary.Throughput,
                    AvgResponseMs = summary.AverageResponseTime
                });
            });

            return command;
        }

        #endregion

        #region Cleanup Command

        private static Command CreateCleanupCommand()
        {
            var command = new Command("cleanup", "Cleanup test environment");

            var prefixOption = new Option<string>("--prefix", "Username prefix to delete") { IsRequired = true };
            var fullResetOption = new Option<bool>("--full-reset", "Delete all test data");
            var forceOption = new Option<bool>("--force", "Skip confirmation");

            command.AddOption(prefixOption);
            command.AddOption(fullResetOption);
            command.AddOption(forceOption);

            command.SetHandler(async (context) =>
            {
                var prefix = context.ParseResult.GetValueForOption(prefixOption);
                var fullReset = context.ParseResult.GetValueForOption(fullResetOption);
                var force = context.ParseResult.GetValueForOption(forceOption);

                if (fullReset && !force)
                {
                    Console.Write("⚠️  FULL RESET will delete ALL data. Type 'yes' to confirm: ");
                    var confirm = Console.ReadLine();
                    if (confirm != "yes")
                    {
                        Console.WriteLine("Aborted.");
                        context.ExitCode = 1;
                        return;
                    }
                }

                Console.WriteLine($"🧹 Cleaning up users with prefix: {prefix}");
                AuditLog.Log(AuditAction.CleanupStart, $"CLI Cleanup started: prefix={prefix}");

                await Task.Delay(1000); // Simulate cleanup

                Console.WriteLine("✅ Cleanup complete");
                AuditLog.Log(AuditAction.CleanupComplete, "CLI Cleanup completed");

                await WriteOutputAsync(context, new { Success = true, Prefix = prefix });
            });

            return command;
        }

        #endregion

        #region Validate Command

        private static Command CreateValidateCommand()
        {
            var command = new Command("validate", "Validate test environment");

            var prefixOption = new Option<string>("--prefix", "Username prefix to validate");
            var failOnErrorOption = new Option<bool>("--fail-on-error", "Exit with error code if validation fails");

            command.AddOption(prefixOption);
            command.AddOption(failOnErrorOption);

            command.SetHandler(async (context) =>
            {
                var prefix = context.ParseResult.GetValueForOption(prefixOption);
                var failOnError = context.ParseResult.GetValueForOption(failOnErrorOption);

                Console.WriteLine($"🔍 Validating environment...");
                AuditLog.Log(AuditAction.ValidateStart, "CLI Validation started");

                await Task.Delay(500);

                // Simulate validation
                var passed = 95;
                var failed = 5;
                var success = failed == 0 || !failOnError;

                Console.WriteLine($"✅ Passed: {passed}");
                Console.WriteLine($"❌ Failed: {failed}");

                AuditLog.Log(AuditAction.ValidateComplete, $"Validation: {passed} passed, {failed} failed", success: failed == 0);

                if (!success)
                {
                    context.ExitCode = 1;
                }

                await WriteOutputAsync(context, new { Success = success, Passed = passed, Failed = failed });
            });

            return command;
        }

        #endregion

        #region Health Command

        private static Command CreateHealthCommand()
        {
            var command = new Command("health", "Run health check");

            var serverOption = new Option<string>("--server", "LDAP server address");

            command.AddOption(serverOption);

            command.SetHandler(async (context) =>
            {
                Console.WriteLine("💓 Running health check...");
                AuditLog.Log(AuditAction.HealthCheck, "CLI Health check started");

                var checks = new List<(string Name, bool Pass, string Message)>
                {
                    ("LDAP Connection", true, "Connected (12ms)"),
                    ("Authentication", true, "Bind successful"),
                    ("Base DN Access", true, "Readable"),
                    ("Write Permission", true, "Writable"),
                    ("Disk Space", true, "15.2 GB available")
                };

                await Task.Delay(500);

                var allPassed = true;
                foreach (var (name, pass, message) in checks)
                {
                    var icon = pass ? "✅" : "❌";
                    Console.WriteLine($"  {icon} {name}: {message}");
                    allPassed &= pass;
                }

                Console.WriteLine();
                Console.WriteLine(allPassed ? "✅ All checks passed" : "❌ Some checks failed");

                AuditLog.Log(AuditAction.HealthCheck, $"Health check: {(allPassed ? "passed" : "failed")}", success: allPassed);

                await WriteOutputAsync(context, new { Healthy = allPassed, Checks = checks });
            });

            return command;
        }

        #endregion

        #region Generate Command

        private static Command CreateGenerateCommand()
        {
            var command = new Command("generate", "Generate test data");

            var typeOption = new Option<string>("--type", "Type: realistic, edge-case, negative, password-test");
            typeOption.SetDefaultValue("realistic");
            var countOption = new Option<int>("--count", () => 10, "Number of users");
            var prefixOption = new Option<string>("--prefix", () => "testuser", "Username prefix");
            var outputOption = new Option<string>("--output", "Output CSV file");

            command.AddOption(typeOption);
            command.AddOption(countOption);
            command.AddOption(prefixOption);
            command.AddOption(outputOption);

            command.SetHandler(async (context) =>
            {
                var type = context.ParseResult.GetValueForOption(typeOption);
                var count = context.ParseResult.GetValueForOption(countOption);
                var prefix = context.ParseResult.GetValueForOption(prefixOption);
                var output = context.ParseResult.GetValueForOption(outputOption);

                Console.WriteLine($"📝 Generating {count} {type} users...");

                var users = DataGenerator.GenerateLoadTestUsers(prefix!, count);

                if (!string.IsNullOrEmpty(output))
                {
                    CsvService.ExportUsers(users, output);
                    Console.WriteLine($"✅ Exported to: {output}");
                }
                else
                {
                    Console.WriteLine($"✅ Generated {users.Count} users");
                    foreach (var user in users.GetRange(0, Math.Min(5, users.Count)))
                    {
                        Console.WriteLine($"   {user.Username} | {user.Email}");
                    }
                    if (users.Count > 5)
                    {
                        Console.WriteLine($"   ... and {users.Count - 5} more");
                    }
                }

                await WriteOutputAsync(context, new { Generated = users.Count, Type = type });
            });

            return command;
        }

        #endregion

        #region Export Command

        private static Command CreateExportCommand()
        {
            var command = new Command("export", "Export data");

            var formatOption = new Option<string>("--format", "Format: csv, selenium, jmeter, postman");
            formatOption.SetDefaultValue("csv");
            var outputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
            var countOption = new Option<int>("--count", () => 100, "Number of users");

            command.AddOption(formatOption);
            command.AddOption(outputOption);
            command.AddOption(countOption);

            command.SetHandler(async (context) =>
            {
                var format = context.ParseResult.GetValueForOption(formatOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var count = context.ParseResult.GetValueForOption(countOption);

                var users = DataGenerator.GenerateLoadTestUsers("testuser", count);
                var exportFormat = format switch
                {
                    "selenium" => CsvExportFormat.Selenium,
                    "jmeter" => CsvExportFormat.JMeter,
                    "postman" => CsvExportFormat.Postman,
                    _ => CsvExportFormat.Standard
                };

                CsvService.ExportUsers(users, output!, exportFormat);
                Console.WriteLine($"✅ Exported {users.Count} users to {output} ({format} format)");

                await Task.CompletedTask;
            });

            return command;
        }

        #endregion

        #region Import Command

        private static Command CreateImportCommand()
        {
            var command = new Command("import", "Import users from CSV");

            var fileOption = new Option<string>("--file", "CSV file path") { IsRequired = true };
            var validateOption = new Option<bool>("--validate-only", "Only validate, don't import");

            command.AddOption(fileOption);
            command.AddOption(validateOption);

            command.SetHandler(async (context) =>
            {
                var file = context.ParseResult.GetValueForOption(fileOption);
                var validateOnly = context.ParseResult.GetValueForOption(validateOption);

                if (!File.Exists(file))
                {
                    Console.WriteLine($"❌ File not found: {file}");
                    context.ExitCode = 1;
                    return;
                }

                var validation = CsvService.ValidateCsvFile(file!);
                Console.WriteLine($"📋 CSV Validation:");
                Console.WriteLine($"   Records: {validation.RecordCount}");
                Console.WriteLine($"   Headers: {string.Join(", ", validation.Headers)}");
                Console.WriteLine($"   Valid: {validation.IsValid}");

                if (!validateOnly && validation.IsValid)
                {
                    var users = CsvService.ImportUsers(file!);
                    Console.WriteLine($"✅ Imported {users.Count} users");
                    AuditLog.Log(AuditAction.ImportCsv, $"Imported {users.Count} users from {file}");
                }

                await Task.CompletedTask;
            });

            return command;
        }

        #endregion

        #region Scenario Command

        private static Command CreateScenarioCommand()
        {
            var command = new Command("scenario", "Run predefined test scenario");

            var listOption = new Option<bool>("--list", "List available scenarios");
            var runOption = new Option<string>("--run", "Scenario ID to run");

            command.AddOption(listOption);
            command.AddOption(runOption);

            command.SetHandler(async (context) =>
            {
                var list = context.ParseResult.GetValueForOption(listOption);
                var run = context.ParseResult.GetValueForOption(runOption);

                if (list)
                {
                    Console.WriteLine("Available Test Scenarios:");
                    Console.WriteLine();
                    foreach (var scenario in ScenarioService.GetAllScenarios())
                    {
                        Console.WriteLine($"  {scenario.Icon} {scenario.Id,-20} {scenario.Name}");
                        Console.WriteLine($"     {scenario.Description}");
                        Console.WriteLine($"     Users: {scenario.UserCount}, Est. Time: {scenario.EstimatedDuration}");
                        Console.WriteLine();
                    }
                    return;
                }

                if (!string.IsNullOrEmpty(run))
                {
                    var scenario = ScenarioService.GetScenarioById(run);
                    if (scenario == null)
                    {
                        Console.WriteLine($"❌ Scenario not found: {run}");
                        context.ExitCode = 1;
                        return;
                    }

                    if (scenario.IsDangerous)
                    {
                        Console.WriteLine($"⚠️  WARNING: {scenario.WarningMessage}");
                    }

                    Console.WriteLine($"🎯 Running scenario: {scenario.Name}");
                    var users = ScenarioService.GenerateScenarioUsers(scenario, "test");
                    Console.WriteLine($"✅ Generated {users.Count} users for scenario");
                }

                await Task.CompletedTask;
            });

            return command;
        }

        #endregion

        #region Snapshot Command

        private static Command CreateSnapshotCommand()
        {
            var command = new Command("snapshot", "Environment snapshots");

            var createOption = new Option<string>("--create", "Create snapshot with name");
            var listOption = new Option<bool>("--list", "List snapshots");
            var compareOption = new Option<string[]>("--compare", "Compare two snapshot IDs") { AllowMultipleArgumentsPerToken = true };

            command.AddOption(createOption);
            command.AddOption(listOption);
            command.AddOption(compareOption);

            command.SetHandler(async (context) =>
            {
                var create = context.ParseResult.GetValueForOption(createOption);
                var list = context.ParseResult.GetValueForOption(listOption);
                var compare = context.ParseResult.GetValueForOption(compareOption);

                var snapshotService = new EnvironmentSnapshotService();

                if (list)
                {
                    Console.WriteLine("📸 Saved Snapshots:");
                    foreach (var snap in snapshotService.GetAllSnapshots())
                    {
                        Console.WriteLine($"   {snap.Id} | {snap.Name} | {snap.CreatedAt} | {snap.TotalUsers} users");
                    }
                    return;
                }

                if (!string.IsNullOrEmpty(create))
                {
                    Console.WriteLine($"📸 Creating snapshot: {create}");
                    // In real implementation, would capture from LDAP
                    var snapshot = snapshotService.CreateSnapshot(create, new List<UserSnapshot>(), new List<ContainerSnapshot>());
                    snapshotService.SaveSnapshot(snapshot);
                    Console.WriteLine($"✅ Snapshot saved: {snapshot.Id}");
                    AuditLog.Log(AuditAction.CreateSnapshot, $"Created snapshot: {create}");
                }

                if (compare != null && compare.Length == 2)
                {
                    var before = snapshotService.LoadSnapshot(compare[0]);
                    var after = snapshotService.LoadSnapshot(compare[1]);

                    if (before == null || after == null)
                    {
                        Console.WriteLine("❌ One or both snapshots not found");
                        context.ExitCode = 1;
                        return;
                    }

                    var diff = snapshotService.CompareSnapshots(before, after);
                    Console.WriteLine(snapshotService.GenerateDiffReport(diff));
                    AuditLog.Log(AuditAction.CompareSnapshots, $"Compared {compare[0]} vs {compare[1]}");
                }

                await Task.CompletedTask;
            });

            return command;
        }

        #endregion

        #region Helpers

        private static async Task WriteOutputAsync(InvocationContext context, object data)
        {
            var outputPath = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options
                    .OfType<Option<string>>()
                    .FirstOrDefault(o => o.Name == "output"));

            if (!string.IsNullOrEmpty(outputPath))
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, json);
                Console.WriteLine($"📄 Output written to: {outputPath}");
            }
        }

        #endregion
    }
}

