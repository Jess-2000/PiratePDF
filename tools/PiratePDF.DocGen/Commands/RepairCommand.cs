using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Changes.Application;
using PiratePDF.DocGen.Changes.Reporting;
using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Repair;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Commands;

internal sealed class RepairCommand : IDocGenCommand
{
    private const string ApplyOption = "--apply";

    public string Name => "repair";

    public string Description =>
        "Preview or apply safe documentation repairs.";

    // =========================================================================
    // Command Execution
    // =========================================================================

    public int Execute(
        CommandContext context,
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);

        var optionResult = ParseOptions(arguments);

        if (!optionResult.IsValid)
        {
            Console.Error.WriteLine(optionResult.ErrorMessage);
            Console.Error.WriteLine();
            PrintHelp();

            return 1;
        }

        if (optionResult.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        try
        {
            var library = LoadLibrary(context);

            var repairPlan = DocumentationRepairPlanner.Plan(library);

            var changeSet = repairPlan.ChangeSet;

            if (!optionResult.ApplyChanges)
            {
                WritePreviewReport(
                    context,
                    changeSet);

                PrintPreviewSummary(
                    repairPlan,
                    context.DocumentChangeReportPath);

                return changeSet.IsEmpty
                    ? 0
                    : 1;
            }

            return ApplyRepairs(
                context,
                changeSet);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "Documentation repair failed.");

            Console.Error.WriteLine(exception);

            return 2;
        }
    }

    // =========================================================================
    // Preview
    // =========================================================================

    private static void WritePreviewReport(
        CommandContext context,
        DocumentChangeSet changeSet)
    {
        DocumentChangeReportWriter.Write(
            context.DocumentChangeReportPath,
            context.RepositoryRoot,
            changeSet,
            changesApplied: false);
    }

    private static void PrintPreviewSummary(
        DocumentationRepairPlan repairPlan,
        string reportPath)
    {
        ArgumentNullException.ThrowIfNull(repairPlan);

        Console.WriteLine(
            "Documentation repair preview complete.");

        Console.WriteLine();
        Console.WriteLine(
            $"Changes:     {repairPlan.ChangeSet.Count}");

        Console.WriteLine(
            $"Files:       {repairPlan.ChangeSet.FileCount}");

        Console.WriteLine(
            $"Diagnostics: {repairPlan.DiagnosticCount}");

        Console.WriteLine(
            $"Warnings:    {repairPlan.WarningCount}");

        Console.WriteLine(
            $"Errors:      {repairPlan.ErrorCount}");

        Console.WriteLine("Applied:     No");
        Console.WriteLine();
        Console.WriteLine($"Report: {reportPath}");

        if (repairPlan.HasChanges)
        {
            Console.WriteLine();
            Console.WriteLine(
                "Run 'repair --apply' to apply these changes.");
        }

        if (repairPlan.DiagnosticCount > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "Review repair diagnostics before applying changes.");
        }
    }

    // =========================================================================
    // Application
    // =========================================================================

    private static int ApplyRepairs(
        CommandContext context,
        DocumentChangeSet changeSet)
    {
        if (changeSet.IsEmpty)
        {
            WriteAppliedReport(
                context,
                changeSet);

            Console.WriteLine(
                "No documentation repairs are required.");

            return 0;
        }

        var applicationResult =
            DocumentChangeApplier.Apply(changeSet);

        WriteAppliedReport(
            context,
            changeSet);

        // Reload the library after file modification so validation uses the
        // updated Markdown rather than the cached pre-repair document model.
        var updatedLibrary = DocumentationLibraryLoader.Load(
            context.DocumentationDirectory);

        context.SetDocumentationLibrary(
            updatedLibrary);

        var validationResult =
            ValidateUpdatedLibrary(updatedLibrary);

        PrintApplicationSummary(
            applicationResult,
            validationResult,
            context.DocumentChangeReportPath);

        return validationResult.HasErrors
            ? 1
            : 0;
    }

    private static void WriteAppliedReport(
        CommandContext context,
        DocumentChangeSet changeSet)
    {
        DocumentChangeReportWriter.Write(
            context.DocumentChangeReportPath,
            context.RepositoryRoot,
            changeSet,
            changesApplied: true);
    }

    private static DocumentationValidationResult ValidateUpdatedLibrary(
        DocumentationLibrary library)
    {
        return DocumentationValidator.Validate(
            library);
    }

    private static void PrintApplicationSummary(
        DocumentChangeApplicationResult applicationResult,
        DocumentationValidationResult validationResult,
        string reportPath)
    {
        Console.WriteLine(
            "Documentation repairs applied.");

        Console.WriteLine();
        Console.WriteLine(
            $"Changes: {applicationResult.AppliedChangeCount}");

        Console.WriteLine(
            $"Files:   {applicationResult.ModifiedFileCount}");

        Console.WriteLine("Applied: Yes");
        Console.WriteLine();
        Console.WriteLine(
            $"Validation errors:   {validationResult.ErrorCount}");

        Console.WriteLine(
            $"Validation warnings: {validationResult.WarningCount}");

        Console.WriteLine();
        Console.WriteLine($"Report: {reportPath}");
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
    // Command Options
    // =========================================================================

    private static RepairOptions ParseOptions(
        IReadOnlyList<string> arguments)
    {
        var applyChanges = false;

        foreach (var argument in arguments)
        {
            if (IsHelpRequest(argument))
            {
                if (arguments.Count != 1)
                {
                    return RepairOptions.Invalid(
                        "The help option cannot be combined with other options.");
                }

                return RepairOptions.Help();
            }

            if (string.Equals(
                    argument,
                    ApplyOption,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (applyChanges)
                {
                    return RepairOptions.Invalid(
                        $"The option '{ApplyOption}' was specified more than once.");
                }

                applyChanges = true;
                continue;
            }

            return RepairOptions.Invalid(
                $"Unknown repair option '{argument}'.");
        }

        return RepairOptions.Valid(
            applyChanges);
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

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  dotnet run --project tools/PiratePDF.DocGen -- repair [--apply]");

        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine(
            "  --apply     Apply all planned safe repairs.");

        Console.WriteLine(
            "  -h, --help  Display repair command help.");

        Console.WriteLine();
        Console.WriteLine(
            "Without --apply, the command creates a preview report only.");

        Console.WriteLine();
        Console.WriteLine("Safe repairs currently include:");
        Console.WriteLine("  - Canonical status casing.");
        Console.WriteLine("  - Unambiguous ISO date normalization.");
        Console.WriteLine("  - Missing revision-history sections.");
        Console.WriteLine("  - Missing trailing newlines.");
    }

    private sealed record RepairOptions(
        bool IsValid,
        bool ShowHelp,
        bool ApplyChanges,
        string ErrorMessage)
    {
        public static RepairOptions Valid(
            bool applyChanges)
        {
            return new RepairOptions(
                IsValid: true,
                ShowHelp: false,
                ApplyChanges: applyChanges,
                ErrorMessage: string.Empty);
        }

        public static RepairOptions Help()
        {
            return new RepairOptions(
                IsValid: true,
                ShowHelp: true,
                ApplyChanges: false,
                ErrorMessage: string.Empty);
        }

        public static RepairOptions Invalid(
            string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                errorMessage);

            return new RepairOptions(
                IsValid: false,
                ShowHelp: false,
                ApplyChanges: false,
                ErrorMessage: errorMessage);
        }
    }
}