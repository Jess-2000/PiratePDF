using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Applies the conservative embedding policy used before any external font
/// program is admitted to PDF Writing.
/// </summary>
public static class PdfExternalFontEmbeddingPolicy
{
    public static bool CanEmbed(
        PdfExternalFontEmbeddingRights rights) =>
        rights is
            PdfExternalFontEmbeddingRights.Allowed or
            PdfExternalFontEmbeddingRights.SubsetOnly or
            PdfExternalFontEmbeddingRights.Editable;

    public static void EnsureCanEmbed(
        PdfExternalFontFace face)
    {
        ArgumentNullException.ThrowIfNull(face);

        if (CanEmbedFace(face))
        {
            return;
        }

        throw new InvalidOperationException(
            face.EmbeddingRights switch
            {
                PdfExternalFontEmbeddingRights.Restricted =>
                    $"Font face '{face.FaceName}' prohibits embedding.",

                PdfExternalFontEmbeddingRights.Unknown =>
                    $"Font face '{face.FaceName}' has unknown embedding rights and cannot be embedded conservatively.",

                _ =>
                    $"Font face '{face.FaceName}' is not eligible for embedding."
            });
    }

    /// <summary>Whether provider policy permits any supported outline embedding operation.</summary>
    internal static bool CanEmbedFace(PdfExternalFontFace face)
    {
        ArgumentNullException.ThrowIfNull(face);
        return CanEmbed(face.EmbeddingRights) && !face.BitmapEmbeddingOnly &&
            !(face.NoSubsetting && face.EmbeddingRights == PdfExternalFontEmbeddingRights.SubsetOnly);
    }

    /// <summary>
    /// Requires both provider and trusted program permission for outline embedding
    /// into the supported editable-document workflow.
    /// </summary>
    public static void EnsureCanEmbed(
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        bool subset)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(program);
        if (!CanEmbed(face.EmbeddingRights) || face.BitmapEmbeddingOnly ||
            (subset && face.NoSubsetting) ||
            (!subset && face.EmbeddingRights == PdfExternalFontEmbeddingRights.SubsetOnly))
            throw new NotSupportedException("Provider restrictions prohibit the requested outline embedding operation.");
        program.EmbeddingRestrictions.EnsureCanEmbed(subset);
    }
}
