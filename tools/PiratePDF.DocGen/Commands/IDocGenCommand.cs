namespace PiratePDF.DocGen.Commands;

internal interface IDocGenCommand
{
    string Name { get; }

    string Description { get; }

    int Execute(
        CommandContext context,
        IReadOnlyList<string> arguments);
}