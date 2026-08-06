using PiratePDF.DocGen.Commands;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Commands;

public sealed class CommandDispatcherTests
{
    [Fact]
    public void Execute_WithRegisteredCommand_ForwardsCommandArguments()
    {
        using var directory = new TemporaryDirectory();

        var context = new CommandContext(
            directory.Path,
            directory.Path);

        var command = new RecordingCommand(
            "test",
            exitCode: 7);

        var dispatcher = new CommandDispatcher(
        [
            command
        ]);

        var exitCode = dispatcher.Execute(
        [
            "test",
            "--first",
            "second"
        ],
            context);

        Assert.Equal(
            7,
            exitCode);

        Assert.Equal(
        [
            "--first",
            "second"
        ],
            command.ReceivedArguments);
    }

    [Fact]
    public void Execute_WithUnknownCommand_ReturnsOne()
    {
        using var directory = new TemporaryDirectory();

        var context = new CommandContext(
            directory.Path,
            directory.Path);

        var dispatcher = new CommandDispatcher(
        [
            new RecordingCommand(
                "known",
                exitCode: 0)
        ]);

        using var errorWriter =
            new StringWriter();

        var originalError = Console.Error;

        try
        {
            Console.SetError(errorWriter);

            var exitCode = dispatcher.Execute(
            [
                "unknown"
            ],
                context);

            Assert.Equal(
                1,
                exitCode);

            Assert.Contains(
                "Unknown command",
                errorWriter.ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Constructor_WithDuplicateCommandNames_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new CommandDispatcher(
            [
                new RecordingCommand(
                    "same",
                    exitCode: 0),

                new RecordingCommand(
                    "SAME",
                    exitCode: 0)
            ]));
    }

    private sealed class RecordingCommand : IDocGenCommand
    {
        private readonly int exitCode;

        public RecordingCommand(
            string name,
            int exitCode)
        {
            Name = name;
            this.exitCode = exitCode;
        }

        public string Name { get; }

        public string Description =>
            "Test command.";

        public IReadOnlyList<string> ReceivedArguments { get; private set; } =
            Array.Empty<string>();

        public int Execute(
            CommandContext context,
            IReadOnlyList<string> arguments)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(arguments);

            ReceivedArguments = arguments.ToArray();

            return exitCode;
        }
    }
}
