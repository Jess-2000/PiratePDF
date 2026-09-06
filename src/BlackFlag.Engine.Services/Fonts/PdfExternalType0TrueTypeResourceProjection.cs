using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>
/// Describes a newly embedded Type0/CIDFontType2 resource and its supporting
/// indirect objects.
/// </summary>
public sealed class PdfExternalType0TrueTypeResourceProjection
{
    internal PdfExternalType0TrueTypeResourceProjection(
        string resourceName,
        PdfObjectIdentifier type0FontIdentifier,
        PdfObjectIdentifier descendantFontIdentifier,
        PdfObjectIdentifier descriptorIdentifier,
        PdfObjectIdentifier programIdentifier,
        PdfObjectIdentifier cidToGidIdentifier,
        PdfObjectIdentifier toUnicodeIdentifier,
        PdfObjectIdentifier pageIdentifier,
        PdfUnicodeTrueTypeSubsetResult subset,
        PdfObjectWriteProjectionSet projections)
    {
        ResourceName = resourceName;
        Type0FontIdentifier = type0FontIdentifier;
        DescendantFontIdentifier = descendantFontIdentifier;
        DescriptorIdentifier = descriptorIdentifier;
        ProgramIdentifier = programIdentifier;
        CidToGidIdentifier = cidToGidIdentifier;
        ToUnicodeIdentifier = toUnicodeIdentifier;
        PageIdentifier = pageIdentifier;
        Subset = subset;
        Projections = projections;
    }

    public string ResourceName { get; }
    public PdfObjectIdentifier Type0FontIdentifier { get; }
    public PdfObjectIdentifier DescendantFontIdentifier { get; }
    public PdfObjectIdentifier DescriptorIdentifier { get; }
    public PdfObjectIdentifier ProgramIdentifier { get; }
    public PdfObjectIdentifier CidToGidIdentifier { get; }
    public PdfObjectIdentifier ToUnicodeIdentifier { get; }
    public PdfObjectIdentifier PageIdentifier { get; }
    public PdfUnicodeTrueTypeSubsetResult Subset { get; }
    public PdfObjectWriteProjectionSet Projections { get; }
}
