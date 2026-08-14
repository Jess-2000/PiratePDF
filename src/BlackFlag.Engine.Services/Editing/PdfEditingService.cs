using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Services.Editing;

/// <summary>
/// Creates controlled editing sessions for higher-level application services.
/// </summary>
public sealed class PdfEditingService
{
    public static PdfEditingSession OpenSession(
        PdfDocument document,
        PdfEditingSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new PdfEditingSession(
            document,
            options);
    }
}
