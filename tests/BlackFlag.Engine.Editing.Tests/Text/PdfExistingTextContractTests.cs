using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Editing.Tests.Text;

public sealed class PdfExistingTextContractTests
{
    [Fact]
    public void IdentityIsStableForEquivalentProvenance()
    {
        Guid sessionId = Guid.NewGuid();
        Guid pageId = Guid.NewGuid();

        var first = CreateIdentity(
            sessionId,
            pageId,
            new PdfObjectIdentifier(10));
        var second = CreateIdentity(
            sessionId,
            pageId,
            new PdfObjectIdentifier(10));

        Assert.Equal(first, second);
        Assert.Equal(4, first.SourceLength);
    }

    [Fact]
    public void SameSourceSpanInDifferentOwnersDoesNotShareIdentity()
    {
        Guid sessionId = Guid.NewGuid();
        Guid pageId = Guid.NewGuid();

        PdfExistingTextIdentity first = CreateIdentity(
            sessionId,
            pageId,
            new PdfObjectIdentifier(10));
        PdfExistingTextIdentity second = CreateIdentity(
            sessionId,
            pageId,
            new PdfObjectIdentifier(11));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SameSourceInDifferentSessionsDoesNotShareIdentity()
    {
        Guid pageId = Guid.NewGuid();
        PdfObjectIdentifier owner = new(10);

        PdfExistingTextIdentity first = CreateIdentity(
            Guid.NewGuid(),
            pageId,
            owner);
        PdfExistingTextIdentity second = CreateIdentity(
            Guid.NewGuid(),
            pageId,
            owner);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void FormSourceKindRemainsDistinctFromPageContent()
    {
        Guid sessionId = Guid.NewGuid();
        Guid pageId = Guid.NewGuid();
        PdfObjectIdentifier owner = new(10);

        PdfExistingTextIdentity pageContent = CreateIdentity(
            sessionId,
            pageId,
            owner,
            PdfExistingTextSourceKind.PageContent);
        PdfExistingTextIdentity formContent = CreateIdentity(
            sessionId,
            pageId,
            owner,
            PdfExistingTextSourceKind.FormXObject);

        Assert.NotEqual(pageContent, formContent);
        Assert.Equal(PdfExistingTextSourceKind.FormXObject, formContent.SourceKind);
    }

    [Fact]
    public void DescriptorSnapshotsEncodedBytesAndExposesExplicitEditability()
    {
        byte[] source = [0x41, 0x42];
        var descriptor = new PdfExistingTextDescriptor(
            CreateIdentity(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new PdfObjectIdentifier(10)),
            source,
            "AB",
            "F1",
            new PdfObjectIdentifier(20),
            12,
            PdfExistingTextEditability.DirectlyEditable);

        source[0] = 0x5A;

        Assert.True(descriptor.IsDirectlyEditable);
        Assert.Null(descriptor.EditabilityReason);
        Assert.Equal("AB", descriptor.UnicodeText);
        Assert.Equal("F1", descriptor.FontResourceName);
        Assert.Equal([0x41, 0x42], descriptor.EncodedBytes.ToArray());
    }

    [Fact]
    public void NonDirectDescriptorRequiresReason()
    {
        PdfExistingTextIdentity identity = CreateIdentity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PdfObjectIdentifier(10));

        Assert.Throws<ArgumentException>(
            () => new PdfExistingTextDescriptor(
                identity,
                ReadOnlyMemory<byte>.Empty,
                null,
                null,
                null,
                12,
                PdfExistingTextEditability.ReadOnlyAmbiguous));
    }

    [Fact]
    public void EditingAssemblyDoesNotDependOnRenderingForTextContracts()
    {
        string[] blackFlagReferences =
        [
            .. typeof(PdfExistingTextIdentity)
                .Assembly
                .GetReferencedAssemblies()
                .Select(static reference => reference.Name)
                .Where(static name => name is not null && name.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal))
                .Select(static name => name!)
        ];

        Assert.DoesNotContain(
            "BlackFlag.Engine.Rendering",
            blackFlagReferences);
    }

    [Fact]
    public void IdentityRejectsInvalidSourceCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfExistingTextIdentity(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PdfExistingTextSourceKind.PageContent,
                new PdfObjectIdentifier(10),
                null,
                0,
                5,
                4,
                0,
                0));
    }

    private static PdfExistingTextIdentity CreateIdentity(
        Guid sessionId,
        Guid pageId,
        PdfObjectIdentifier owner,
        PdfExistingTextSourceKind sourceKind = PdfExistingTextSourceKind.PageContent)
    {
        return new PdfExistingTextIdentity(
            sessionId,
            pageId,
            sourceKind,
            owner,
            new PdfObjectIdentifier(12),
            0,
            20,
            24,
            3,
            0);
    }
}
