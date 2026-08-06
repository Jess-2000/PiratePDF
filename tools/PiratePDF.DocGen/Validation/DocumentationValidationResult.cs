namespace PiratePDF.DocGen.Validation;

internal sealed class DocumentationValidationResult
{
    private readonly ValidationIssue[] issues;

    public DocumentationValidationResult(
        IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        this.issues = issues
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.DocumentId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.File, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ValidationIssue> Issues => issues;

    public int ErrorCount =>
        issues.Count(issue => issue.Severity == ValidationSeverity.Error);

    public int WarningCount =>
        issues.Count(issue => issue.Severity == ValidationSeverity.Warning);

    public int InformationCount =>
        issues.Count(issue => issue.Severity == ValidationSeverity.Information);

    public bool HasErrors => ErrorCount > 0;
}
