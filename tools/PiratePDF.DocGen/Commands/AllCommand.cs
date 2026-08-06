namespace PiratePDF.DocGen.Commands;

internal sealed class AllCommand : IDocGenCommand
{
    private readonly IndexCommand indexCommand;
    private readonly ValidateCommand validateCommand;

    public AllCommand(
        IndexCommand indexCommand,
        ValidateCommand validateCommand)
    {
        ArgumentNullException.ThrowIfNull(indexCommand);
        ArgumentNullException.ThrowIfNull(validateCommand);

        this.indexCommand = indexCommand;
        this.validateCommand = validateCommand;
    }

    public string Name => "all";

    public string Description =>
        "Generate the index and validate all documentation.";

    // =========================================================================
    // Command Execution
    // =========================================================================

    public int Execute(
        CommandContext context,
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count > 0)
        {
            return HandleArguments(arguments);
        }

        var indexExitCode = indexCommand.Execute(
            context,
            []);

        if (indexExitCode != 0)
        {
            return indexExitCode;
        }

        Console.WriteLine();

        return validateCommand.Execute(
            context,
            []);
    }

    // =========================================================================
    // Command Arguments
    // =========================================================================

    private static int HandleArguments(
        IReadOnlyList<string> arguments)
    {
        if (
            arguments.Count == 1 &&
            IsHelpRequest(arguments[0]))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine(
                "  dotnet run --project tools/PiratePDF.DocGen -- all");

            Console.WriteLine();
            Console.WriteLine(
                "Generates the documentation index and validation report.");

            return 0;
        }

        Console.Error.WriteLine(
            $"Unknown all option '{arguments[0]}'.");

        return 1;
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
                StringComparison.OrdinalIgnoreCase);
    }
}