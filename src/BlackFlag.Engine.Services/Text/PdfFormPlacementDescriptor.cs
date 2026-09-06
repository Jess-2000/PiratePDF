using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Identifies one concrete page-content `Do` invocation that places an
/// indirect Form XObject.
/// </summary>
public sealed class PdfFormPlacementDescriptor
{
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

    /// <summary>
    /// Creates a placement descriptor by matching one page display Form to its
    /// canonical page-content `Do` operation.
    /// </summary>
    public static PdfFormPlacementDescriptor Create(
        PdfObjectIdentifier pageIdentifier,
        PdfExistingTextDiscoverySource pageSource,
        PdfDisplayForm displayForm)
    {
        ArgumentNullException.ThrowIfNull(pageSource);
        ArgumentNullException.ThrowIfNull(displayForm);

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
            operation.SourceSpan.EndOffset);
    }
}
