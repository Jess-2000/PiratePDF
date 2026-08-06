namespace PiratePDF.DocGen.Commands;

internal sealed class CommandDispatcher
{
    private readonly Dictionary<string, IDocGenCommand> commands;

    public CommandDispatcher(
        IEnumerable<IDocGenCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        this.commands = new Dictionary<string, IDocGenCommand>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var command in commands)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (!this.commands.TryAdd(
                    command.Name,
                    command))
            {
                throw new ArgumentException(
                    $"Duplicate command name '{command.Name}'.",
                    nameof(commands));
            }
        }

        if (this.commands.Count == 0)
        {
            throw new ArgumentException(
                "At least one DocGen command must be registered.",
                nameof(commands));
        }
    }

    // =========================================================================
    // Command Execution
    // =========================================================================

    public int Execute(
        IReadOnlyList<string> arguments,
        CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(context);

        if (arguments.Count == 0)
        {
            PrintHelp();
            return 0;
        }

        var commandName = arguments[0];

        if (IsHelpRequest(commandName))
        {
            PrintHelp();
            return 0;
        }

        if (!commands.TryGetValue(
                commandName,
                out var command))
        {
            Console.Error.WriteLine(
                $"Unknown command '{commandName}'.");

            Console.Error.WriteLine();
            PrintHelp();

            return 1;
        }

        var commandArguments = arguments
            .Skip(1)
            .ToArray();

        return command.Execute(
            context,
            commandArguments);
    }

    // =========================================================================
    // Help Output
    // =========================================================================

    private void PrintHelp()
    {
        Console.WriteLine("PiratePDF.DocGen");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  dotnet run --project tools/PiratePDF.DocGen -- <command> [options]");

        Console.WriteLine();
        Console.WriteLine("Commands:");

        var commandNameWidth = commands.Keys.Max(
            name => name.Length);

        foreach (var command in commands.Values.OrderBy(
                     command => command.Name,
                     StringComparer.OrdinalIgnoreCase))
        {
            Console.Write("  ");
            Console.Write(
                command.Name.PadRight(commandNameWidth));
            Console.Write("  ");
            Console.WriteLine(command.Description);
        }

        Console.WriteLine();
        Console.WriteLine("Global options:");
        Console.WriteLine("  -h, --help  Display command help.");
        Console.WriteLine();
        Console.WriteLine(
            "Run '<command> --help' for command-specific options.");
    }

    private static bool IsHelpRequest(
        string value)
    {
        return string.Equals(
                   value,
                   "-h",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   value,
                   "--help",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   value,
                   "help",
                   StringComparison.OrdinalIgnoreCase);
    }
}