using System.Runtime.InteropServices;
using System.Text;

using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Commands;

internal sealed class DoctorCommand : IDocGenCommand
{
    public string Name => "doctor";

    public string Description =>
        "Diagnose the DocGen environment and repository configuration.";

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
            var checks = RunChecks(context);

            PrintReport(checks);

            return checks.Any(check => !check.Succeeded)
                ? 1
                : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "DocGen diagnostics failed.");

            Console.Error.WriteLine(exception);

            return 2;
        }
    }

    // =========================================================================
    // Diagnostics
    // =========================================================================

    private static List<DoctorCheck> RunChecks(
        CommandContext context)
    {
        var checks = new List<DoctorCheck>();

        AddPathCheck(
            checks,
            "Repository root",
            context.RepositoryRoot,
            Directory.Exists);

        AddPathCheck(
            checks,
            "Documentation directory",
            context.DocumentationDirectory,
            Directory.Exists);

        AddWritableDirectoryCheck(
            checks,
            context.DocumentationDirectory);

        AddEncodingCheck(checks);
        AddRuntimeChecks(checks);
        AddDocumentationChecks(
            checks,
            context);

        return checks;
    }

    private static void AddPathCheck(
        List<DoctorCheck> checks,
        string name,
        string path,
        Func<string, bool> exists)
    {
        checks.Add(
            exists(path)
                ? DoctorCheck.Success(
                    name,
                    path)
                : DoctorCheck.Failure(
                    name,
                    $"Path was not found: {path}"));
    }

    private static void AddWritableDirectoryCheck(
        List<DoctorCheck> checks,
        string directory)
    {
        if (!Directory.Exists(directory))
        {
            checks.Add(
                DoctorCheck.Failure(
                    "Documentation output",
                    "The documentation directory does not exist."));

            return;
        }

        var temporaryPath = Path.Combine(
            directory,
            $".docgen-write-test-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                "PiratePDF.DocGen",
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));

            checks.Add(
                DoctorCheck.Success(
                    "Documentation output",
                    "The documentation directory is writable."));
        }
        catch (Exception exception)
            when (
                exception is IOException or
                UnauthorizedAccessException)
        {
            checks.Add(
                DoctorCheck.Failure(
                    "Documentation output",
                    exception.Message));
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void AddEncodingCheck(
        List<DoctorCheck> checks)
    {
        try
        {
            var encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true);

            _ = encoding.GetBytes(
                "PiratePDF");

            checks.Add(
                DoctorCheck.Success(
                    "UTF-8 encoding",
                    "UTF-8 without BOM is available."));
        }
        catch (Exception exception)
        {
            checks.Add(
                DoctorCheck.Failure(
                    "UTF-8 encoding",
                    exception.Message));
        }
    }

    private static void AddRuntimeChecks(
        List<DoctorCheck> checks)
    {
        checks.Add(
            DoctorCheck.Success(
                ".NET runtime",
                RuntimeInformation.FrameworkDescription));

        checks.Add(
            DoctorCheck.Success(
                "Operating system",
                RuntimeInformation.OSDescription));

        checks.Add(
            DoctorCheck.Success(
                "Process architecture",
                RuntimeInformation.ProcessArchitecture.ToString()));
    }

    private static void AddDocumentationChecks(
        List<DoctorCheck> checks,
        CommandContext context)
    {
        try
        {
            var library = LoadLibrary(context);
            var validationResult =
                DocumentationValidator.Validate(library);

            checks.Add(
                DoctorCheck.Success(
                    "Documentation loading",
                    $"{library.Count} authoritative documents loaded."));

            checks.Add(
                validationResult.HasErrors
                    ? DoctorCheck.Failure(
                        "Documentation validation",
                        $"{validationResult.ErrorCount} error(s) detected.")
                    : DoctorCheck.Success(
                        "Documentation validation",
                        "No validation errors detected."));

            var missingRelationships =
                library.Relationships
                    .FindMissingRelatedDocumentIds();

            checks.Add(
                missingRelationships.Length == 0
                    ? DoctorCheck.Success(
                        "Document relationships",
                        "No missing related-document IDs detected.")
                    : DoctorCheck.Failure(
                        "Document relationships",
                        $"{missingRelationships.Length} missing ID(s) detected."));
        }
        catch (Exception exception)
        {
            checks.Add(
                DoctorCheck.Failure(
                    "Documentation loading",
                    exception.Message));
        }
    }

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

    private static void PrintReport(
        IReadOnlyCollection<DoctorCheck> checks)
    {
        Console.WriteLine(
            "PiratePDF.DocGen Diagnostics");

        Console.WriteLine();

        foreach (var check in checks)
        {
            Console.Write(
                check.Succeeded
                    ? "[PASS] "
                    : "[FAIL] ");

            Console.Write(check.Name);
            Console.Write(": ");
            Console.WriteLine(check.Message);
        }

        var failedCount = checks.Count(
            check => !check.Succeeded);

        Console.WriteLine();
        Console.WriteLine(
            failedCount == 0
                ? "Status: Healthy"
                : $"Status: Unhealthy ({failedCount} failed check(s))");
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
            PrintHelp();
            return 0;
        }

        Console.Error.WriteLine(
            $"Unknown doctor option '{arguments[0]}'.");

        Console.Error.WriteLine();
        PrintHelp();

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

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  dotnet run --project tools/PiratePDF.DocGen -- doctor");

        Console.WriteLine();
        Console.WriteLine(
            "Checks the DocGen runtime, repository paths, output access, " +
            "documentation loading, validation, and relationships.");

        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine(
            "  -h, --help  Display doctor command help.");
    }

    private sealed record DoctorCheck(
        string Name,
        bool Succeeded,
        string Message)
    {
        public static DoctorCheck Success(
            string name,
            string message)
        {
            return new DoctorCheck(
                name,
                Succeeded: true,
                message);
        }

        public static DoctorCheck Failure(
            string name,
            string message)
        {
            return new DoctorCheck(
                name,
                Succeeded: false,
                message);
        }
    }
}