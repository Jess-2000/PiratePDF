using PiratePDF.DocGen.Changes;

namespace PiratePDF.DocGen.Tests.Changes;

public sealed class DocumentChangeSetTests
{
    [Fact]
    public void Constructor_WithNonOverlappingChanges_SortsChanges()
    {
        var filePath = System.IO.Path.GetFullPath(
            "Document.md");

        var laterChange = DocumentChange.Insert(
            filePath,
            startIndex: 20,
            text: "later",
            reason: "Later change.");

        var earlierChange = DocumentChange.Insert(
            filePath,
            startIndex: 5,
            text: "earlier",
            reason: "Earlier change.");

        var changeSet = new DocumentChangeSet(
        [
            laterChange,
            earlierChange
        ]);

        Assert.Equal(
            2,
            changeSet.Count);

        Assert.Equal(
            5,
            changeSet.Changes[0].StartIndex);

        Assert.Equal(
            20,
            changeSet.Changes[1].StartIndex);
    }

    [Fact]
    public void Constructor_WithOverlappingReplacements_Throws()
    {
        var filePath = System.IO.Path.GetFullPath(
            "Document.md");

        var first = DocumentChange.Replace(
            filePath,
            startIndex: 5,
            length: 10,
            replacementText: "first",
            reason: "First replacement.");

        var second = DocumentChange.Replace(
            filePath,
            startIndex: 10,
            length: 8,
            replacementText: "second",
            reason: "Second replacement.");

        var exception = Assert.Throws<ArgumentException>(
            () => new DocumentChangeSet(
            [
                first,
                second
            ]));

        Assert.Contains(
            "overlap",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithTwoInsertionsAtSameIndex_Throws()
    {
        var filePath = System.IO.Path.GetFullPath(
            "Document.md");

        var first = DocumentChange.Insert(
            filePath,
            startIndex: 5,
            text: "first",
            reason: "First insertion.");

        var second = DocumentChange.Insert(
            filePath,
            startIndex: 5,
            text: "second",
            reason: "Second insertion.");

        Assert.Throws<ArgumentException>(
            () => new DocumentChangeSet(
            [
                first,
                second
            ]));
    }

    [Fact]
    public void GetChangesForFile_MatchesPathsCaseInsensitively()
    {
        var filePath = System.IO.Path.GetFullPath(
            "Document.md");

        var change = DocumentChange.Insert(
            filePath,
            startIndex: 0,
            text: "text",
            reason: "Test insertion.");

        var changeSet = new DocumentChangeSet(
        [
            change
        ]);

        var alternateCasePath =
            filePath.ToUpperInvariant();

        var result = changeSet.GetChangesForFile(
            alternateCasePath);

        Assert.Single(result);
    }
}