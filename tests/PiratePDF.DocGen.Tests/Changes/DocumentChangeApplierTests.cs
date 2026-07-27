using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Changes.Application;
using PiratePDF.DocGen.Tests.TestInfrastructure;

namespace PiratePDF.DocGen.Tests.Changes;

public sealed class DocumentChangeApplierTests
{
    [Fact]
    public void Apply_WithInsertReplaceAndDelete_UpdatesFileCorrectly()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Document.md",
            "ABCDEFGHIJ");

        var changeSet = new DocumentChangeSet(
        [
            DocumentChange.Insert(
                filePath,
                startIndex: 0,
                text: "START-",
                reason: "Add prefix."),

            DocumentChange.Replace(
                filePath,
                startIndex: 2,
                length: 3,
                replacementText: "xyz",
                reason: "Replace middle characters."),

            DocumentChange.Delete(
                filePath,
                startIndex: 8,
                length: 2,
                reason: "Delete final characters.")
        ]);

        var result = DocumentChangeApplier.Apply(
            changeSet);

        Assert.Equal(
            3,
            result.AppliedChangeCount);

        Assert.Equal(
            1,
            result.ModifiedFileCount);

        Assert.Equal(
            "START-ABxyzFGH",
            directory.ReadFile("Document.md"));
    }

    [Fact]
    public void Apply_WithOutOfRangeChange_DoesNotModifyFile()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "Document.md",
            "PiratePDF");

        var changeSet = new DocumentChangeSet(
        [
            DocumentChange.Replace(
                filePath,
                startIndex: 50,
                length: 2,
                replacementText: "XX",
                reason: "Invalid test range.")
        ]);

        Assert.Throws<InvalidOperationException>(
            () => DocumentChangeApplier.Apply(
                changeSet));

        Assert.Equal(
            "PiratePDF",
            directory.ReadFile("Document.md"));
    }

    [Fact]
    public void Apply_WithMissingFile_DoesNotModifyExistingFiles()
    {
        using var directory = new TemporaryDirectory();

        var existingFile = directory.CreateFile(
            "Existing.md",
            "Original");

        var missingFile = System.IO.Path.Combine(
            directory.Path,
            "Missing.md");

        var changeSet = new DocumentChangeSet(
        [
            DocumentChange.Insert(
                existingFile,
                startIndex: 0,
                text: "Modified-",
                reason: "Modify existing file."),

            DocumentChange.Insert(
                missingFile,
                startIndex: 0,
                text: "Missing",
                reason: "Target missing file.")
        ]);

        Assert.Throws<FileNotFoundException>(
            () => DocumentChangeApplier.Apply(
                changeSet));

        Assert.Equal(
            "Original",
            directory.ReadFile("Existing.md"));
    }
}