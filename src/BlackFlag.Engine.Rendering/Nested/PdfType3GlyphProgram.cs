
using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Rendering.Nested;

/// <summary>Represents one interpreted Type 3 CharProc.</summary>
public sealed record PdfType3GlyphProgram(
    string GlyphName,
    PdfDisplayList Content,
    PdfMatrix FontMatrix);
