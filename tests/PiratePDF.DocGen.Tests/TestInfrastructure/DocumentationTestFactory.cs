using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;

namespace PiratePDF.DocGen.Tests.TestInfrastructure;

internal static class DocumentationTestFactory
{
    public static string CreateDocument(
        string documentId = "PP-TEST-900",
        string title = "Test Document",
        string category = "Testing Documentation",
        string version = "1.0",
        string status = "Approved",
        string owner = "PiratePDF Project",
        string relatedWorkItems = "BF-1030",
        string lastUpdated = "2026-07-25",
        string nextReview = "2026-10-25",
        string body = "## Purpose\n\nTest document.",
        bool includeRevisionHistory = true,
        bool includeTrailingNewline = true)
    {
        var content =
            $"""
             # {title}

             | Field | Value |
             |---|---|
             | **Document ID** | {documentId} |
             | **Category** | {category} |
             | **Version** | {version} |
             | **Status** | {status} |
             | **Owner** | {owner} |
             | **Related BF Work Items** | {relatedWorkItems} |
             | **Last Updated** | {lastUpdated} |
             | **Next Review** | {nextReview} |

             ---

             {body}
             """;

        if (includeRevisionHistory)
        {
            content +=
                """

                ## Revision History

                | Version | Date | Description |
                |---|---|---|
                | 1.0 | 2026-07-25 | Initial version. |
                """;
        }

        content = content.Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);

        return includeTrailingNewline
            ? content.TrimEnd('\n') + "\n"
            : content.TrimEnd('\n');
    }

    public static DocumentationLibrary LoadLibrary(
        TemporaryDirectory directory,
        params TestDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(documents);

        foreach (var document in documents)
        {
            directory.CreateFile(
                document.RelativePath,
                document.Content);
        }

        return DocumentationLibraryLoader.Load(
            directory.Path);
    }

    internal sealed record TestDocument(
        string RelativePath,
        string Content);
}
