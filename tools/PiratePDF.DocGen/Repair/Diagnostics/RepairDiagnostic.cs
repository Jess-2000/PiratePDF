namespace PiratePDF.DocGen.Repair.Diagnostics;

internal sealed record RepairDiagnostic
{
    public RepairDiagnostic(
        RepairDiagnosticSeverity severity,
        string ruleName,
        string filePath,
        string documentId,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Severity = severity;
        RuleName = ruleName.Trim();
        FilePath = Path.GetFullPath(filePath);
        DocumentId = documentId.Trim();
        Message = message.Trim();
    }

    public RepairDiagnosticSeverity Severity { get; }

    public string RuleName { get; }

    public string FilePath { get; }

    public string DocumentId { get; }

    public string Message { get; }
}
