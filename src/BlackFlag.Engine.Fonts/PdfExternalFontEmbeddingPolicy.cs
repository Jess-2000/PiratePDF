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
            PdfExternalFontEmbeddingRights.SubsetOnly;

    public static void EnsureCanEmbed(
        PdfExternalFontFace face)
    {
        ArgumentNullException.ThrowIfNull(face);

        if (CanEmbed(face.EmbeddingRights))
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
}
