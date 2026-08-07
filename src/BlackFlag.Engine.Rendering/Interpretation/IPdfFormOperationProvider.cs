using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>
/// Provides parsed Form XObject operations without introducing a Rendering to
/// Parsing project dependency.
/// </summary>
public interface IPdfFormOperationProvider
{
    /// <summary>Gets canonical graphics operations for a resolved Form XObject.</summary>
    IEnumerable<PdfGraphicsOperation> GetOperations(
        PdfResolvedFormXObject form,
        PdfResourceScope scope);
}
