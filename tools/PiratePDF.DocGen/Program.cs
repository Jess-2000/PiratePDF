using PiratePDF.DocGen.Commands;
using PiratePDF.DocGen.Infrastructure;

try
{
    var repositoryRoot = RepositoryLocator.FindRepositoryRoot(
        Directory.GetCurrentDirectory());

    var documentationDirectory = Path.Combine(
        repositoryRoot,
        "docs");

    var context = new CommandContext(
        repositoryRoot,
        documentationDirectory);

    var indexCommand = new IndexCommand();
    var validateCommand = new ValidateCommand();
    var healthCommand = new HealthCommand();
    var doctorCommand = new DoctorCommand();
    var repairCommand = new RepairCommand();

    var dispatcher = new CommandDispatcher(
    [
        new AllCommand(
            indexCommand,
            validateCommand),
        healthCommand,
        doctorCommand,
        indexCommand,
        repairCommand,
        validateCommand
    ]);

    var commandArguments = args.Length == 0
        ? ["all"]
        : args;

    return dispatcher.Execute(
        commandArguments,
        context);
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        "PiratePDF.DocGen could not start.");

    Console.Error.WriteLine(exception);

    return 2;
}
