using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using PdfStream = BlackFlag.Engine.Document.Objects.PdfStream;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Identifies one concrete page-content `Do` invocation that places an
/// indirect Form XObject.
/// </summary>
public sealed class PdfFormPlacementDescriptor
{
    private PdfFormPlacementProvenance? provenance;

    private PdfFormPlacementDescriptor(
        PdfObjectIdentifier pageIdentifier,
        PdfObjectIdentifier contentStreamIdentifier,
        PdfObjectIdentifier formIdentifier,
        string resourceName,
        long startOffset,
        long endOffset)
    {
        PageIdentifier = pageIdentifier;
        ContentStreamIdentifier = contentStreamIdentifier;
        FormIdentifier = formIdentifier;
        ResourceName = resourceName;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public PdfObjectIdentifier PageIdentifier { get; }
    public PdfObjectIdentifier ContentStreamIdentifier { get; }
    public PdfObjectIdentifier FormIdentifier { get; }
    public string ResourceName { get; }
    public long StartOffset { get; }
    public long EndOffset { get; }

    internal int StreamIndex { get; private init; }

    /// <summary>
    /// Captures authoritative placement provenance during discovery. Descriptors
    /// created without a document cannot authorize clone projection.
    /// </summary>
    public static PdfFormPlacementDescriptor Create(
        PdfDocument document,
        PdfObjectIdentifier pageIdentifier,
        PdfExistingTextDiscoverySource pageSource,
        PdfDisplayForm displayForm)
    {
        ArgumentNullException.ThrowIfNull(document);
        PdfFormPlacementDescriptor descriptor = Create(pageIdentifier, pageSource, displayForm);
        descriptor.provenance = PdfFormPlacementProvenance.Capture(document, descriptor);
        return descriptor;
    }

    internal (PdfStream Form, PdfStream Content, byte[] DecodedContent) Validate(
        PdfDocument document) =>
        (provenance ?? throw new InvalidOperationException(
            "The Form placement has no authoritative source snapshot; rediscover it with the document."))
        .Validate(document, this);

    /// <summary>
    /// Creates a placement descriptor by matching one page display Form to its
    /// canonical page-content `Do` operation.
    /// </summary>
    /// <remarks>
    /// This compatibility factory retains identity metadata only. Clone
    /// projection requires the document-aware overload to capture source bytes
    /// and current owner/resource identity during discovery.
    /// </remarks>
    public static PdfFormPlacementDescriptor Create(
        PdfObjectIdentifier pageIdentifier,
        PdfExistingTextDiscoverySource pageSource,
        PdfDisplayForm displayForm)
    {
        ArgumentNullException.ThrowIfNull(pageSource);
        ArgumentNullException.ThrowIfNull(displayForm);

        if (pageIdentifier != pageSource.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The Form placement discovery source belongs to another page.");
        }

        if (pageSource.SourceKind !=
            PdfExistingTextSourceKind.PageContent)
        {
            throw new ArgumentException(
                "A Form placement descriptor requires a page-content discovery source.",
                nameof(pageSource));
        }

        PdfGraphicsOperation operation =
            pageSource.Operations.SingleOrDefault(
                candidate =>
                    candidate.SourceSpan ==
                    displayForm.SourceSpan)
            ?? throw new InvalidOperationException(
                "The displayed Form placement does not map to a unique page-content operation.");

        if (operation.Operator.Code != "Do" ||
            operation.Operands.Count != 1 ||
            operation.Operands[0] is not PdfName resourceName)
        {
            throw new InvalidOperationException(
                "The displayed Form placement does not map to a canonical `Do` operation.");
        }

        PdfObjectIdentifier contentStreamIdentifier =
            pageSource.GetContentStreamIdentifier(
                operation.SourceSpan.StreamIndex)
            ?? throw new NotSupportedException(
                "Placement-only Form cloning requires an indirect page content stream.");

        if (!displayForm.Form.Identity.IsIndirect)
        {
            throw new NotSupportedException(
                "Placement-only cloning requires an indirect source Form XObject.");
        }

        var formIdentifier =
            new PdfObjectIdentifier(
                displayForm.Form.Identity.ObjectNumber!.Value,
                displayForm.Form.Identity.GenerationNumber!.Value);

        return new PdfFormPlacementDescriptor(
            pageIdentifier,
            contentStreamIdentifier,
            formIdentifier,
            resourceName.Value,
            operation.SourceSpan.StartOffset,
            operation.SourceSpan.EndOffset)
        {
            StreamIndex = operation.SourceSpan.StreamIndex
        };
    }
}
