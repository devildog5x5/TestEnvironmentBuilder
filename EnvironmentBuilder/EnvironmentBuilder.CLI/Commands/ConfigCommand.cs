using System.CommandLine;
using EnvironmentBuilder.Core.Models;
using Newtonsoft.Json;
using Spectre.Console;

namespace EnvironmentBuilder.CLI.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Manage configuration files");

        var initCommand = new Command("init", "Create a new configuration file");
        var presetOption = new Option<string>("--preset", () => "simple", "Preset to use: simple, medium, complex, brutal");
        var outputOption = new Option<string>("--output", () => "envbuilder.json", "Output file path");
        
        initCommand.AddOption(presetOption);
        initCommand.AddOption(outputOption);

        initCommand.SetHandler(async (preset, output) =>
        {
            var level = preset.ToLower() switch
            {
                "simple" => ComplexityLevel.Simple,
                "medium" => ComplexityLevel.Medium,
                "complex" => ComplexityLevel.Complex,
                "brutal" => ComplexityLevel.Brutal,
                _ => ComplexityLevel.Simple
            };

            var config = new EnvironmentConfig { Name = $"{preset} Test Environment" };
            config.ApplyPreset(ComplexityPreset.FromLevel(level));

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(output, json);

            AnsiConsole.MarkupLine($"[green]✓ Created configuration file:[/] {output}");
            AnsiConsole.MarkupLine($"  Preset: [cyan]{preset}[/]");
            AnsiConsole.MarkupLine($"  Users: [cyan]{config.Users.Count}[/]");
        }, presetOption, outputOption);

        var showCommand = new Command("show", "Display a configuration file");
        var fileOption = new Option<string>("--file", "Configuration file to display");
        showCommand.AddOption(fileOption);

        showCommand.SetHandler(async (file) =>
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                AnsiConsole.MarkupLine("[red]File not found[/]");
                return;
            }

            var json = await File.ReadAllTextAsync(file);
            var config = JsonConvert.DeserializeObject<EnvironmentConfig>(json);

            if (config == null)
            {
                AnsiConsole.MarkupLine("[red]Invalid configuration file[/]");
                return;
            }

            var table = new Table().Title($"[bold]{config.Name}[/]");
            table.AddColumn("Section");
            table.AddColumn("Setting");
            table.AddColumn("Value");

            table.AddRow("Connection", "Server", $"{config.Connection.Server}:{config.Connection.Port}");
            table.AddRow("Connection", "Base DN", config.Connection.BaseDn);
            table.AddRow("Connection", "SSL", config.Connection.UseSsl.ToString());
            
            table.AddRow("Users", "Count", config.Users.Count.ToString());
            table.AddRow("Users", "Prefix", config.Users.Prefix);
            table.AddRow("Users", "Randomize", config.Users.RandomizeData.ToString());
            
            table.AddRow("Execution", "Batch Size", config.Execution.BatchSize.ToString());
            table.AddRow("Execution", "Parallel Ops", config.Execution.ParallelOperations.ToString());
            table.AddRow("Execution", "Dry Run", config.Execution.DryRun.ToString());

            AnsiConsole.Write(table);
        }, fileOption);

        command.AddCommand(initCommand);
        command.AddCommand(showCommand);

        return command;
    }
}

