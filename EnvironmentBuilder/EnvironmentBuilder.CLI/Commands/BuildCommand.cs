using System.CommandLine;
using EnvironmentBuilder.Core.Models;
using EnvironmentBuilder.Core.Services;
using Spectre.Console;

namespace EnvironmentBuilder.CLI.Commands;

public static class BuildCommand
{
    public static Command Create()
    {
        var command = new Command("build", "Build a test environment");

        var presetOption = new Option<string>("--preset", () => "simple", "Complexity preset: simple, medium, complex, brutal");
        var usersOption = new Option<int>("--users", () => 0, "Number of users (overrides preset)");
        var prefixOption = new Option<string>("--prefix", () => "testuser", "Username prefix");
        var configOption = new Option<string>("--config", "Path to JSON config file");
        var ldifOption = new Option<string>("--ldif", "Output LDIF file path");
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Simulate without making changes");
        var parallelOption = new Option<int>("--parallel", () => 4, "Number of parallel operations");

        command.AddOption(presetOption);
        command.AddOption(usersOption);
        command.AddOption(prefixOption);
        command.AddOption(configOption);
        command.AddOption(ldifOption);
        command.AddOption(dryRunOption);
        command.AddOption(parallelOption);

        command.SetHandler(async (context) =>
        {
            var preset = context.ParseResult.GetValueForOption(presetOption)!;
            var users = context.ParseResult.GetValueForOption(usersOption);
            var prefix = context.ParseResult.GetValueForOption(prefixOption)!;
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var ldifPath = context.ParseResult.GetValueForOption(ldifOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var parallel = context.ParseResult.GetValueForOption(parallelOption);

            // Get global options
            var server = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "server") as Option<string>);
            var port = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "port") as Option<int>);
            var bindDn = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "bind-dn") as Option<string>);
            var password = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "password") as Option<string>);
            var baseDn = context.ParseResult.GetValueForOption(
                context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "base-dn") as Option<string>);

            await ExecuteBuild(preset, users, prefix, configPath, ldifPath, dryRun, parallel,
                server ?? "localhost", port, bindDn ?? "cn=admin,o=org", password ?? "", baseDn ?? "o=org");
        });

        return command;
    }

    private static async Task ExecuteBuild(string preset, int users, string prefix, string? configPath,
        string? ldifPath, bool dryRun, int parallel, string server, int port, string bindDn, string password, string baseDn)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Building Environment[/]");
        AnsiConsole.MarkupLine($"  Preset: [cyan]{preset}[/]");
        if (dryRun) AnsiConsole.MarkupLine("  [yellow]DRY RUN MODE[/]");
        AnsiConsole.WriteLine();

        // Build configuration
        var config = new EnvironmentConfig();

        // Load from file if specified
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            config = Newtonsoft.Json.JsonConvert.DeserializeObject<EnvironmentConfig>(json) ?? config;
            AnsiConsole.MarkupLine($"[green]Loaded config from:[/] {configPath}");
        }

        // Apply preset
        var complexityLevel = preset.ToLower() switch
        {
            "simple" => ComplexityLevel.Simple,
            "medium" => ComplexityLevel.Medium,
            "complex" => ComplexityLevel.Complex,
            "brutal" => ComplexityLevel.Brutal,
            _ => ComplexityLevel.Simple
        };
        config.ApplyPreset(ComplexityPreset.FromLevel(complexityLevel));

        // Override with command line options
        config.Connection.Server = server;
        config.Connection.Port = port;
        config.Connection.BindDn = bindDn;
        config.Connection.Password = password;
        config.Connection.BaseDn = baseDn;

        if (users > 0) config.Users.Count = users;
        config.Users.Prefix = prefix;
        config.Execution.DryRun = dryRun;
        config.Execution.ParallelOperations = parallel;

        if (!string.IsNullOrEmpty(ldifPath))
        {
            config.Output.GenerateLdif = true;
            config.Output.LdifPath = ldifPath;
        }

        // Display configuration
        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddRow("Server", $"{config.Connection.Server}:{config.Connection.Port}");
        table.AddRow("Base DN", config.Connection.BaseDn);
        table.AddRow("Users", config.Users.Count.ToString());
        table.AddRow("Prefix", config.Users.Prefix);
        table.AddRow("Parallel Ops", config.Execution.ParallelOperations.ToString());
        table.AddRow("Dry Run", config.Execution.DryRun.ToString());
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Execute build
        using var service = new EnvironmentService(config);

        service.LogMessage += (s, msg) => AnsiConsole.MarkupLine($"[grey]{msg}[/]");

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Building Environment[/]", maxValue: config.Users.Count);

                service.ProgressChanged += (s, update) =>
                {
                    task.Value = update.CurrentItem;
                    task.Description = $"[green]{update.Operation}[/] - {update.CurrentItemName}";
                };

                var result = await service.BuildEnvironmentAsync();

                task.Value = task.MaxValue;

                AnsiConsole.WriteLine();
                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[bold green]✓ Build Complete![/]");
                    AnsiConsole.MarkupLine($"  Created: [cyan]{result.Metrics.SuccessCount}[/] users");
                    AnsiConsole.MarkupLine($"  Failed: [red]{result.Metrics.FailedCount}[/] users");
                    AnsiConsole.MarkupLine($"  Duration: [cyan]{result.Duration.TotalSeconds:F2}s[/]");
                    AnsiConsole.MarkupLine($"  Speed: [cyan]{result.Metrics.ItemsPerSecond:F1}[/] users/sec");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]✗ Build Failed![/]");
                    AnsiConsole.MarkupLine($"  Error: {result.ErrorDetails}");
                }
            });
    }
}

