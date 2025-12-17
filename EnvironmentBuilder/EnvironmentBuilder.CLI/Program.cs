using System.CommandLine;
using EnvironmentBuilder.CLI.Commands;
using Spectre.Console;

namespace EnvironmentBuilder.CLI;

/// <summary>
/// Environment Builder CLI
/// Test Brutally - Build Your Level of Complexity
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Display banner
        AnsiConsole.Write(new FigletText("EnvBuilder").Color(Color.Gold1));
        AnsiConsole.MarkupLine("[grey]Test Brutally - Build Your Level of Complexity[/]");
        AnsiConsole.WriteLine();

        var rootCommand = new RootCommand("Environment Builder CLI - Create and manage test environments")
        {
            BuildCommand.Create(),
            CleanupCommand.Create(),
            ValidateCommand.Create(),
            ConfigCommand.Create(),
            HealthCommand.Create()
        };

        // Global options
        var serverOption = new Option<string>("--server", () => "localhost", "LDAP server address");
        var portOption = new Option<int>("--port", () => 389, "LDAP port");
        var bindDnOption = new Option<string>("--bind-dn", () => "cn=admin,o=org", "Bind DN for authentication");
        var passwordOption = new Option<string>("--password", "Bind password");
        var baseDnOption = new Option<string>("--base-dn", () => "o=org", "Base DN for operations");
        var verboseOption = new Option<bool>("--verbose", () => false, "Enable verbose output");

        rootCommand.AddGlobalOption(serverOption);
        rootCommand.AddGlobalOption(portOption);
        rootCommand.AddGlobalOption(bindDnOption);
        rootCommand.AddGlobalOption(passwordOption);
        rootCommand.AddGlobalOption(baseDnOption);
        rootCommand.AddGlobalOption(verboseOption);

        return await rootCommand.InvokeAsync(args);
    }
}

