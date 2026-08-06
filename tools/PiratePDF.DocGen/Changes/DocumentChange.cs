namespace PiratePDF.DocGen.Changes;

internal sealed record DocumentChange
{
    public DocumentChange(
        string filePath,
        DocumentChangeType changeType,
        int startIndex,
        int length,
        string replacementText,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(replacementText);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startIndex),
                startIndex,
                "The change start index cannot be negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "The change length cannot be negative.");
        }

        ValidateChangeShape(
            changeType,
            length,
            replacementText);

        FilePath = Path.GetFullPath(filePath);
        ChangeType = changeType;
        StartIndex = startIndex;
        Length = length;
        ReplacementText = replacementText;
        Reason = reason.Trim();
    }

    // =========================================================================
    // Change Data
    // =========================================================================

    public string FilePath { get; }

    public DocumentChangeType ChangeType { get; }

    public int StartIndex { get; }

    public int Length { get; }

    public string ReplacementText { get; }

    public string Reason { get; }

    public int EndIndex =>
        checked(StartIndex + Length);

    // =========================================================================
    // Factory Methods
    // =========================================================================

    public static DocumentChange Insert(
        string filePath,
        int startIndex,
        string text,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new DocumentChange(
            filePath,
            DocumentChangeType.Insert,
            startIndex,
            length: 0,
            text,
            reason);
    }

    public static DocumentChange Replace(
        string filePath,
        int startIndex,
        int length,
        string replacementText,
        string reason)
    {
        return new DocumentChange(
            filePath,
            DocumentChangeType.Replace,
            startIndex,
            length,
            replacementText,
            reason);
    }

    public static DocumentChange Delete(
        string filePath,
        int startIndex,
        int length,
        string reason)
    {
        return new DocumentChange(
            filePath,
            DocumentChangeType.Delete,
            startIndex,
            length,
            replacementText: string.Empty,
            reason);
    }

    // =========================================================================
    // Validation
    // =========================================================================

    private static void ValidateChangeShape(
        DocumentChangeType changeType,
        int length,
        string replacementText)
    {
        switch (changeType)
        {
            case DocumentChangeType.Insert:
                if (length != 0)
                {
                    throw new ArgumentException(
                        "Insert changes must have a length of zero.",
                        nameof(length));
                }

                break;

            case DocumentChangeType.Replace:
                if (length == 0)
                {
                    throw new ArgumentException(
                        "Replace changes must affect at least one character.",
                        nameof(length));
                }

                break;

            case DocumentChangeType.Delete:
                if (length == 0)
                {
                    throw new ArgumentException(
                        "Delete changes must affect at least one character.",
                        nameof(length));
                }

                if (replacementText.Length != 0)
                {
                    throw new ArgumentException(
                        "Delete changes cannot contain replacement text.",
                        nameof(replacementText));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(changeType),
                    changeType,
                    "Unknown document change type.");
        }
    }
}
