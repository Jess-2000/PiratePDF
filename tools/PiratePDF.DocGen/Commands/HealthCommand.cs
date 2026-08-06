using System.Globalization;

using PiratePDF.DocGen.Health;
using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Reporting;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Commands;

internal sealed class HealthCommand : IDocGenCommand
{
    public string Name => "health";

    public string Description =>
        "Generate a documentation-library health report.";

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

            var validationResult = DocumentationValidator.Validate(library);

            var snapshot =
                DocumentationHealthCalculator.Calculate(
                    library,
                    validationResult);

            DocumentationHealthReportWriter.Write(
                context.DocumentationHealthReportPath,
                snapshot);

            PrintSummary(
                snapshot,
                context.DocumentationHealthReportPath);

            return validationResult.HasErrors
                ? 1
                : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "Documentation health analysis failed.");

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
                "  dotnet run --project tools/PiratePDF.DocGen -- health");

            Console.WriteLine();
            Console.WriteLine(
                "Generates docs/Documentation-Health.md.");

            return 0;
        }

        Console.Error.WriteLine(
            $"Unknown health option '{arguments[0]}'.");

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
        DocumentationHealthSnapshot snapshot,
        string outputPath)
    {
        Console.WriteLine(
            "Documentation health analysis complete.");

        Console.WriteLine();
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Score:     {snapshot.OverallScore:0.0}%"));

        Console.WriteLine($"Grade:     {snapshot.Grade}");
        Console.WriteLine($"Documents: {snapshot.DocumentCount}");
        Console.WriteLine(
            $"Errors:    {snapshot.ValidationErrorCount}");

        Console.WriteLine(
            $"Warnings:  {snapshot.ValidationWarningCount}");

        Console.WriteLine();
        Console.WriteLine($"Report: {outputPath}");
    }
}
