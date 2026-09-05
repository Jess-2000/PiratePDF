using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>Describes one placement-only Form clone and page redirection.</summary>
public sealed class PdfFormPlacementCloneProjection
{
    internal PdfFormPlacementCloneProjection(
        string resourceName,
        PdfObjectIdentifier clonedFormIdentifier,
        PdfObjectWriteProjectionSet projections)
    {
        ResourceName = resourceName;
        ClonedFormIdentifier = clonedFormIdentifier;
        Projections = projections;
    }

    public string ResourceName { get; }
    public PdfObjectIdentifier ClonedFormIdentifier { get; }
    public PdfObjectWriteProjectionSet Projections { get; }
}
