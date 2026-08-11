
namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Describes decoded source traits used to choose a best-fit representation.</summary>
public sealed record PdfImageSourceTraits
{
    public bool IsVector { get; init; }

    public bool IsBilevel { get; init; }

    public bool IsIndexed { get; init; }

    public bool IsGray { get; init; }

    public bool HasAlpha { get; init; }

    public bool IsCmyk { get; init; }

    public bool IsPhotographic { get; init; }

    public bool SourceIsLossy { get; init; }

    public int BitsPerComponent { get; init; } = 8;
}
