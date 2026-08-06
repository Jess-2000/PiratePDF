using System.Text;

namespace PiratePDF.DocGen.Changes.Application;

internal static class DocumentChangeApplier
{
    private static readonly UTF8Encoding Utf8WithoutBom =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentChangeApplicationResult Apply(
        DocumentChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);

        if (changeSet.IsEmpty)
        {
            return new DocumentChangeApplicationResult(
                AppliedChangeCount: 0,
                ModifiedFileCount: 0,
                ModifiedFiles: []);
        }

        var preparedFiles = PrepareChangedFiles(changeSet);

        // All changes are validated and rendered before any original file is
        // replaced, reducing the risk of a partially applied change set.
        foreach (var preparedFile in preparedFiles)
        {
            WriteAtomically(
                preparedFile.FilePath,
                preparedFile.UpdatedContent);
        }

        var modifiedFiles = preparedFiles
            .Select(file => file.FilePath)
            .OrderBy(
                filePath => filePath,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DocumentChangeApplicationResult(
            changeSet.Count,
            modifiedFiles.Length,
            modifiedFiles);
    }

    // =========================================================================
    // Change Preparation
    // =========================================================================

    private static PreparedFileChange[] PrepareChangedFiles(
        DocumentChangeSet changeSet)
    {
        var preparedFiles =
            new List<PreparedFileChange>();

        foreach (var fileGroup in changeSet.Changes.GroupBy(
                     change => change.FilePath,
                     StringComparer.OrdinalIgnoreCase))
        {
            var filePath = fileGroup.Key;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "A document targeted by the change set was not found.",
                    filePath);
            }

            var originalContent = File.ReadAllText(
                filePath,
                Utf8WithoutBom);

            var fileChanges = fileGroup
                .OrderByDescending(change => change.StartIndex)
                .ThenByDescending(change => change.EndIndex)
                .ToArray();

            ValidateRanges(
                filePath,
                originalContent,
                fileChanges);

            var updatedContent = ApplyChanges(
                originalContent,
                fileChanges);

            preparedFiles.Add(
                new PreparedFileChange(
                    filePath,
                    updatedContent));
        }

        return preparedFiles.ToArray();
    }

    private static void ValidateRanges(
        string filePath,
        string content,
        DocumentChange[] changes)
    {
        foreach (var change in changes)
        {
            if (change.StartIndex > content.Length)
            {
                throw new InvalidOperationException(
                    $"Change start index {change.StartIndex} exceeds the " +
                    $"length of '{filePath}' ({content.Length}).");
            }

            if (change.EndIndex > content.Length)
            {
                throw new InvalidOperationException(
                    $"Change end index {change.EndIndex} exceeds the " +
                    $"length of '{filePath}' ({content.Length}).");
            }
        }
    }

    private static string ApplyChanges(
        string originalContent,
        DocumentChange[] changes)
    {
        var builder = new StringBuilder(
            originalContent);

        // Changes are applied from the end of the file toward the beginning so
        // earlier offsets remain valid as later ranges are modified.
        foreach (var change in changes)
        {
            switch (change.ChangeType)
            {
                case DocumentChangeType.Insert:
                    builder.Insert(
                        change.StartIndex,
                        change.ReplacementText);

                    break;

                case DocumentChangeType.Replace:
                    builder.Remove(
                        change.StartIndex,
                        change.Length);

                    builder.Insert(
                        change.StartIndex,
                        change.ReplacementText);

                    break;

                case DocumentChangeType.Delete:
                    builder.Remove(
                        change.StartIndex,
                        change.Length);

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported change type '{change.ChangeType}'.");
            }
        }

        return builder.ToString();
    }

    // =========================================================================
    // Atomic File Output
    // =========================================================================

    private static void WriteAtomically(
        string filePath,
        string content)
    {
        var fullFilePath = Path.GetFullPath(filePath);

        var directory = Path.GetDirectoryName(fullFilePath)
            ?? throw new InvalidOperationException(
                "The target file directory could not be determined.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullFilePath)}." +
            $"{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                content,
                Utf8WithoutBom);

            File.Move(
                temporaryPath,
                fullFilePath,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record PreparedFileChange(
        string FilePath,
        string UpdatedContent);
}
