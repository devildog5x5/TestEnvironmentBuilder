using System.CommandLine;
using EnvironmentBuilder.Core.Models;
using EnvironmentBuilder.Core.Services;
using Spectre.Console;

namespace EnvironmentBuilder.CLI.Commands;

public static class HealthCommand
{
    public static Command Create()
    {
        var command = new Command("health", "Check LDAP server health");

        command.SetHandler(async (context) =>
        {
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

            await ExecuteHealthCheck(server ?? "localhost", port, bindDn ?? "cn=admin,o=org", password ?? "", baseDn ?? "o=org");
        });

        return command;
    }

    private static async Task ExecuteHealthCheck(string server, int port, string bindDn, string password, string baseDn)
    {
        AnsiConsole.MarkupLine($"[bold cyan]Health Check[/]");
        AnsiConsole.MarkupLine($"  Server: [cyan]{server}:{port}[/]");
        AnsiConsole.WriteLine();

        var config = new EnvironmentConfig
        {
            Connection = new ConnectionConfig
            {
                Server = server,
                Port = port,
                BindDn = bindDn,
                Password = password,
                BaseDn = baseDn
            }
        };

        using var service = new EnvironmentService(config);

        HealthCheckResult? health = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Checking health...", async ctx =>
            {
                health = await service.HealthCheckAsync();
            });

        if (health == null)
        {
            AnsiConsole.MarkupLine("[red]Health check failed[/]");
            return;
        }

        // Display results
        var panel = new Panel(new Markup(health.IsHealthy 
            ? "[bold green]HEALTHY[/]" 
            : "[bold red]UNHEALTHY[/]"))
        {
            Header = new PanelHeader("Server Status"),
            Padding = new Padding(2, 1)
        };
        AnsiConsole.Write(panel);

        var table = new Table();
        table.AddColumn("Check");
        table.AddColumn("Result");
        table.AddColumn("Details");

        table.AddRow(
            "Connection",
            health.CanConnect ? "[green]✓[/]" : "[red]✗[/]",
            health.CanConnect ? "Connected" : "Failed"
        );

        table.AddRow(
            "Authentication",
            health.CanAuthenticate ? "[green]✓[/]" : "[red]✗[/]",
            health.CanAuthenticate ? "Authenticated" : "Failed"
        );

        table.AddRow(
            "Read Access",
            health.CanRead ? "[green]✓[/]" : "[red]✗[/]",
            health.CanRead ? "Can read" : "No read access"
        );

        table.AddRow(
            "Write Access",
            health.CanWrite ? "[green]✓[/]" : "[yellow]?[/]",
            health.CanWrite ? "Can write" : "Not tested"
        );

        table.AddRow(
            "Response Time",
            health.ResponseTimeMs < 100 ? "[green]✓[/]" : health.ResponseTimeMs < 500 ? "[yellow]~[/]" : "[red]✗[/]",
            $"{health.ResponseTimeMs}ms"
        );

        AnsiConsole.Write(table);

        if (health.Warnings.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach (var warning in health.Warnings)
            {
                AnsiConsole.MarkupLine($"  ⚠ {warning}");
            }
        }

        if (health.Errors.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]Errors:[/]");
            foreach (var error in health.Errors)
            {
                AnsiConsole.MarkupLine($"  ✗ {error}");
            }
        }
    }
}

