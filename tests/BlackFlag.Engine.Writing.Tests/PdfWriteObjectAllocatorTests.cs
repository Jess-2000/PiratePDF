using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfWriteObjectAllocatorTests
{
    [Fact]
    public void ReserveNextSkipsObjectAddedAfterPlanning()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan plan = PdfWritePlanner.Create(document);

        document.Objects.Add(
            new PdfObjectIdentifier(plan.NextObjectNumber),
            new PdfDictionary());

        PdfWriteObjectAllocator allocator =
            plan.CreateObjectAllocator();

        PdfObjectIdentifier identifier =
            allocator.ReserveNext();

        Assert.Equal(
            checked(plan.NextObjectNumber + 1),
            identifier.ObjectNumber);
        Assert.Equal(0, identifier.GenerationNumber);
    }

    [Fact]
    public void ReserveNextReturnsUniqueMonotonicIdentifiers()
    {
        PdfWriteObjectAllocator allocator =
            PdfWritePlanner
                .Create(CreateDocument())
                .CreateObjectAllocator();

        PdfObjectIdentifier first = allocator.ReserveNext();
        PdfObjectIdentifier second = allocator.ReserveNext();

        Assert.Equal(
            checked(first.ObjectNumber + 1),
            second.ObjectNumber);
        Assert.Equal(0, first.GenerationNumber);
        Assert.Equal(0, second.GenerationNumber);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        var rootIdentifier =
            new PdfObjectIdentifier(1);

        document.Objects.Add(
            rootIdentifier,
            new PdfDictionary());
        document.Objects.Add(
            new PdfObjectIdentifier(3),
            PdfString.FromLatin1("existing"));
        document.Trailer.SetRootObject(rootIdentifier);

        return document;
    }
}
