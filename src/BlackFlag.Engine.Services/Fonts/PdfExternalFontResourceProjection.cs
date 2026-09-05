using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>
/// Describes the newly allocated objects and page-resource projection required
/// to embed one external TrueType font.
/// </summary>
public sealed class PdfExternalFontResourceProjection
{
    internal PdfExternalFontResourceProjection(
        string resourceName,
        PdfObjectIdentifier fontIdentifier,
        PdfObjectIdentifier descriptorIdentifier,
        PdfObjectIdentifier programIdentifier,
        PdfObjectIdentifier pageIdentifier,
        PdfObjectWriteProjectionSet projections)
    {
        ResourceName = resourceName;
        FontIdentifier = fontIdentifier;
        DescriptorIdentifier = descriptorIdentifier;
        ProgramIdentifier = programIdentifier;
        PageIdentifier = pageIdentifier;
        Projections = projections;
    }

    public string ResourceName { get; }
    public PdfObjectIdentifier FontIdentifier { get; }
    public PdfObjectIdentifier DescriptorIdentifier { get; }
    public PdfObjectIdentifier ProgramIdentifier { get; }
    public PdfObjectIdentifier PageIdentifier { get; }
    public PdfObjectWriteProjectionSet Projections { get; }
}
