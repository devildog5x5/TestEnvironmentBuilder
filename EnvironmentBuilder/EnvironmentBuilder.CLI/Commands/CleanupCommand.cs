using System.CommandLine;
using EnvironmentBuilder.Core.Models;
using EnvironmentBuilder.Core.Services;
using Spectre.Console;

namespace EnvironmentBuilder.CLI.Commands;

public static class CleanupCommand
{
    public static Command Create()
    {
        var command = new Command("cleanup", "Remove test users from environment");

        var prefixOption = new Option<string>("--prefix", () => "testuser", "Username prefix to match for deletion");
        var dryRunOption = new Option<bool>("--dry-run", () => false, "Show what would be deleted without deleting");
        var forceOption = new Option<bool>("--force", () => false, "Skip confirmation prompt");

        command.AddOption(prefixOption);
        command.AddOption(dryRunOption);
        command.AddOption(forceOption);

        command.SetHandler(async (context) =>
        {
            var prefix = context.ParseResult.GetValueForOption(prefixOption)!;
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);

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

            await ExecuteCleanup(prefix, dryRun, force,
                server ?? "localhost", port, bindDn ?? "cn=admin,o=org", password ?? "", baseDn ?? "o=org");
        });

        return command;
    }

    private static async Task ExecuteCleanup(string prefix, bool dryRun, bool force,
        string server, int port, string bindDn, string password, string baseDn)
    {
        AnsiConsole.MarkupLine($"[bold red]Cleanup Environment[/]");
        AnsiConsole.MarkupLine($"  Server: [cyan]{server}:{port}[/]");
        AnsiConsole.MarkupLine($"  Prefix: [yellow]{prefix}*[/]");
        if (dryRun) AnsiConsole.MarkupLine("  [yellow]DRY RUN MODE[/]");
        AnsiConsole.WriteLine();

        if (!dryRun && !force)
        {
            if (!AnsiConsole.Confirm($"Delete all users matching [yellow]{prefix}*[/]?", false))
            {
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                return;
            }
        }

        var config = new EnvironmentConfig
        {
            Connection = new ConnectionConfig
            {
                Server = server,
                Port = port,
                BindDn = bindDn,
                Password = password,
                BaseDn = baseDn
            },
            Users = new UserGenerationConfig { Prefix = prefix },
            Execution = new ExecutionConfig { DryRun = dryRun }
        };

        using var service = new EnvironmentService(config);
        service.LogMessage += (s, msg) => AnsiConsole.MarkupLine($"[grey]{msg}[/]");

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[red]Cleaning up...[/]");

                service.ProgressChanged += (s, update) =>
                {
                    task.MaxValue = update.TotalItems;
                    task.Value = update.CurrentItem;
                };

                var result = await service.CleanupAsync(prefix);

                task.Value = task.MaxValue;

                AnsiConsole.WriteLine();
                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[bold green]✓ Cleanup Complete![/]");
                    AnsiConsole.MarkupLine($"  Deleted: [cyan]{result.Metrics.SuccessCount}[/] users");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[bold red]✗ Cleanup had errors[/]");
                    AnsiConsole.MarkupLine($"  Deleted: {result.Metrics.SuccessCount}");
                    AnsiConsole.MarkupLine($"  Failed: {result.Metrics.FailedCount}");
                }
            });
    }
}

