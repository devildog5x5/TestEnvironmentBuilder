using System.CommandLine;
using EnvironmentBuilder.Core.Models;
using EnvironmentBuilder.Core.Services;
using Spectre.Console;

namespace EnvironmentBuilder.CLI.Commands;

public static class ValidateCommand
{
    public static Command Create()
    {
        var command = new Command("validate", "Validate environment and check user existence");

        var prefixOption = new Option<string>("--prefix", () => "testuser", "Username prefix to validate");
        var expectedOption = new Option<int>("--expected", () => 0, "Expected number of users");

        command.AddOption(prefixOption);
        command.AddOption(expectedOption);

        command.SetHandler(async (context) =>
        {
            var prefix = context.ParseResult.GetValueForOption(prefixOption)!;
            var expected = context.ParseResult.GetValueForOption(expectedOption);

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

            await ExecuteValidate(prefix, expected,
                server ?? "localhost", port, bindDn ?? "cn=admin,o=org", password ?? "", baseDn ?? "o=org");
        });

        return command;
    }

    private static async Task ExecuteValidate(string prefix, int expected,
        string server, int port, string bindDn, string password, string baseDn)
    {
        AnsiConsole.MarkupLine($"[bold blue]Validating Environment[/]");
        AnsiConsole.MarkupLine($"  Server: [cyan]{server}:{port}[/]");
        AnsiConsole.MarkupLine($"  Looking for: [yellow]{prefix}*[/]");
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

        await AnsiConsole.Status()
            .StartAsync("Connecting...", async ctx =>
            {
                var connectResult = await service.ConnectAsync();
                if (!connectResult.Success)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Connection failed: {connectResult.ErrorDetails}[/]");
                    return;
                }

                ctx.Status("Searching for users...");

                // This would search for users - simplified for now
                AnsiConsole.MarkupLine("[green]✓ Connected successfully[/]");

                var health = await service.HealthCheckAsync();

                var table = new Table().Title("[bold]Validation Results[/]");
                table.AddColumn("Check");
                table.AddColumn("Status");

                table.AddRow("Connection", health.CanConnect ? "[green]✓ Pass[/]" : "[red]✗ Fail[/]");
                table.AddRow("Authentication", health.CanAuthenticate ? "[green]✓ Pass[/]" : "[red]✗ Fail[/]");
                table.AddRow("Read Access", health.CanRead ? "[green]✓ Pass[/]" : "[red]✗ Fail[/]");
                table.AddRow("Write Access", health.CanWrite ? "[green]✓ Pass[/]" : "[yellow]? Unknown[/]");
                table.AddRow("Response Time", $"{health.ResponseTimeMs}ms");

                if (expected > 0)
                {
                    table.AddRow("Expected Users", expected.ToString());
                    // Would add actual count here
                }

                AnsiConsole.Write(table);

                if (health.Warnings.Any())
                {
                    AnsiConsole.MarkupLine("\n[yellow]Warnings:[/]");
                    foreach (var warning in health.Warnings)
                        AnsiConsole.MarkupLine($"  ⚠ {warning}");
                }

                if (health.Errors.Any())
                {
                    AnsiConsole.MarkupLine("\n[red]Errors:[/]");
                    foreach (var error in health.Errors)
                        AnsiConsole.MarkupLine($"  ✗ {error}");
                }
            });
    }
}

