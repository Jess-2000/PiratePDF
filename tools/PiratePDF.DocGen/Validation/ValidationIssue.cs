namespace PiratePDF.DocGen.Validation;

internal sealed record ValidationIssue(
    ValidationSeverity Severity,
    string DocumentId,
    string File,
    string Message);