using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>Represents a deterministic graphics interpretation failure.</summary>
public sealed class PdfGraphicsInterpretationException : FormatException
{
    /// <summary>Initializes an interpretation exception.</summary>
    public PdfGraphicsInterpretationException(
        PdfGraphicsInterpretationFailure failure,
        PdfContentSourceSpan sourceSpan,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        SourceSpan = sourceSpan;
    }

    /// <summary>Gets the failure category.</summary>
    public PdfGraphicsInterpretationFailure Failure { get; }

    /// <summary>Gets the source operation range associated with the failure.</summary>
    public PdfContentSourceSpan SourceSpan { get; }
}
