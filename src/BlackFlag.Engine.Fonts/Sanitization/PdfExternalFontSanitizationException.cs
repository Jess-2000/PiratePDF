namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>Reports a rejected external font without exposing it to trusted processing.</summary>
public sealed class PdfExternalFontSanitizationException : IOException
{
    public PdfExternalFontSanitizationException(
        PdfExternalFontSanitizationFailure failure,
        string message)
        : base(message)
    {
        Failure = failure;
    }

    public PdfExternalFontSanitizationFailure Failure { get; }
}
