using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>Contains a sanitized outline-reduced TrueType program and its WinAnsi code map.</summary>
public sealed class PdfTrueTypeSubsetResult
{
    internal PdfTrueTypeSubsetResult(
        PdfSanitizedExternalFontProgram program,
        IReadOnlyDictionary<byte, int> unicodeByCode)
    {
        Program = program;
        UnicodeByCode =
            new System.Collections.ObjectModel.ReadOnlyDictionary<byte, int>(
                new Dictionary<byte, int>(unicodeByCode));
    }

    public PdfSanitizedExternalFontProgram Program { get; }
    public IReadOnlyDictionary<byte, int> UnicodeByCode { get; }
}
