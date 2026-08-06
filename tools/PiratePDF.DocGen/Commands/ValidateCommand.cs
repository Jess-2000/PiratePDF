using PiratePDF.DocGen.Generation;
using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Commands;

internal sealed class ValidateCommand : IDocGenCommand
{
    public string Name => "validate";

    public string Description =>
        "Validate the documentation library and generate a report.";

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

        try
        {
            var library = LoadLibrary(context);

            // The validator currently consumes metadata records. Keeping this
            // conversion here isolates the temporary compatibility boundary.
            var result = DocumentationValidator.Validate(library);

            ValidationReportWriter.Write(
                context.ValidationReportPath,
                context.RepositoryRoot,
                library.Count,
                result);

            PrintSummary(
                library.Count,
                result,
                context.ValidationReportPath);

            return result.HasErrors
                ? 1
                : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "Documentation validation failed.");

            Console.Error.WriteLine(exception);

            return 2;
        }
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
                "  dotnet run --project tools/PiratePDF.DocGen -- validate");

            Console.WriteLine();
            Console.WriteLine(
                "Validates the documentation library and generates " +
                "docs/ValidationReport.md.");

            return 0;
        }

        Console.Error.WriteLine(
            $"Unknown validate option '{arguments[0]}'.");

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

    // =========================================================================
    // Documentation Library
    // =========================================================================

    private static DocumentationLibrary LoadLibrary(
        CommandContext context)
    {
        if (context.IsDocumentationLibraryLoaded)
        {
            return context.DocumentationLibrary;
        }

        var library = DocumentationLibraryLoader.Load(
            context.DocumentationDirectory);

        context.SetDocumentationLibrary(library);

        return library;
    }

    // =========================================================================
    // Console Output
    // =========================================================================

    private static void PrintSummary(
        int documentCount,
        DocumentationValidationResult result,
        string reportPath)
    {
        Console.WriteLine(
            "Documentation validation complete.");

        Console.WriteLine();
        Console.WriteLine($"Documents:   {documentCount}");
        Console.WriteLine($"Errors:      {result.ErrorCount}");
        Console.WriteLine($"Warnings:    {result.WarningCount}");
        Console.WriteLine(
            $"Information: {result.InformationCount}");

        Console.WriteLine();
        Console.WriteLine($"Report: {reportPath}");
    }
}
