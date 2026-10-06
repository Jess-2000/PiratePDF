using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Resources;

/// <summary>Describes one collision-safe page resource projection.</summary>
public sealed class PdfPageResourceMutationProjection
{
    internal PdfPageResourceMutationProjection(
        string resourceName,
        PdfResourceMutationOwnership ownership,
        bool categoryCopiedFromIndirectObject,
        PdfObjectWriteProjection pageProjection)
    {
        ResourceName = resourceName;
        Ownership = ownership;
        CategoryCopiedFromIndirectObject = categoryCopiedFromIndirectObject;
        PageProjection = pageProjection;
    }

    public string ResourceName { get; }
    public PdfResourceMutationOwnership Ownership { get; }
    public bool CategoryCopiedFromIndirectObject { get; }
    public PdfObjectWriteProjection PageProjection { get; }
}
