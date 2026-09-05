using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Services.Resources;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Clones an indirect Form XObject and redirects exactly one page-content `Do`
/// placement to the clone.
/// </summary>
public static class PdfFormPlacementCloneProjector
{
    private static readonly PdfName FilterName =
        PdfName.FromLatin1("Filter");

    private static readonly PdfName XObjectName =
        PdfName.FromLatin1("XObject");

    public static PdfFormPlacementCloneProjection Create(
        PdfWritePlan writePlan,
        PdfFormPlacementDescriptor placement,
        ReadOnlySpan<byte> replacementFormData,
        string? preferredCloneResourceName = null)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentNullException.ThrowIfNull(placement);

        PdfStream sourceForm =
            GetStream(
                writePlan,
                placement.FormIdentifier,
                "source Form XObject");

        if (sourceForm.Dictionary.ContainsKey(FilterName))
        {
            throw new NotSupportedException(
                "BF-1054O placement-only cloning is limited to unfiltered Form streams until BF-1054P re-encoding is available.");
        }

        PdfStream pageContent =
            GetStream(
                writePlan,
                placement.ContentStreamIdentifier,
                "page content");

        if (pageContent.Dictionary.ContainsKey(FilterName))
        {
            throw new NotSupportedException(
                "BF-1054O placement-only cloning is limited to unfiltered page content streams until BF-1054P re-encoding is available.");
        }

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();

        PdfObjectIdentifier cloneIdentifier =
            allocator.ReserveNext();

        var clonedForm =
            new PdfStream(
                sourceForm.Dictionary,
                replacementFormData);

        string preferred =
            preferredCloneResourceName ??
            placement.ResourceName;

        PdfPageResourceMutationProjection resourceProjection =
            PdfPageResourceMutationProjector.Create(
                writePlan,
                placement.PageIdentifier,
                XObjectName,
                preferred,
                new PdfIndirectReference(
                    cloneIdentifier));

        byte[] projectedPageContent =
            RedirectPlacement(
                pageContent.ToByteArray(),
                placement,
                resourceProjection.ResourceName);

        var projectedContentStream =
            new PdfStream(
                pageContent.Dictionary,
                projectedPageContent);

        return new PdfFormPlacementCloneProjection(
            resourceProjection.ResourceName,
            cloneIdentifier,
            new PdfObjectWriteProjectionSet(
            [
                resourceProjection.PageProjection,
                new PdfObjectWriteProjection(
                    placement.ContentStreamIdentifier,
                    projectedContentStream),
                new PdfObjectWriteProjection(
                    cloneIdentifier,
                    clonedForm)
            ]));
    }

    private static byte[] RedirectPlacement(
        ReadOnlySpan<byte> source,
        PdfFormPlacementDescriptor placement,
        string resourceName)
    {
        if (placement.StartOffset < 0 ||
            placement.EndOffset < placement.StartOffset ||
            placement.EndOffset > source.Length)
        {
            throw new InvalidOperationException(
                "The Form placement source span lies outside the current page content stream.");
        }

        int start =
            checked((int)placement.StartOffset);

        int end =
            checked((int)placement.EndOffset);

        byte[] replacement =
            BuildDoOperation(
                resourceName);

        byte[] result =
            new byte[
                checked(
                    source.Length -
                    (end - start) +
                    replacement.Length)];

        source[..start].CopyTo(result);
        replacement.CopyTo(
            result.AsSpan(start));

        source[end..].CopyTo(
            result.AsSpan(
                start +
                replacement.Length));

        return result;
    }

    private static byte[] BuildDoOperation(
        string resourceName)
    {
        using var destination =
            new MemoryStream();

        PdfObjectSerializer.Write(
            destination,
            PdfName.FromLatin1(
                resourceName));

        destination.WriteByte(
            (byte)' ');

        destination.Write(
            Encoding.ASCII.GetBytes("Do"));

        return destination.ToArray();
    }

    private static PdfStream GetStream(
        PdfWritePlan plan,
        PdfObjectIdentifier identifier,
        string description)
    {
        if (!plan.Document.Objects.TryGet(
                identifier,
                out PdfObject? value) ||
            value is not PdfStream stream)
        {
            throw new InvalidOperationException(
                $"{description} object '{identifier}' is not an indirect stream.");
        }

        return stream;
    }
}
