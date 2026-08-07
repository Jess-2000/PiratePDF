namespace BlackFlag.Engine.Filters;

// Provides shared classification for bytes used by PDF filter encodings.
internal static class PdfFilterByteClassifier
{
    // Determines whether the specified byte is PDF whitespace.
    public static bool IsWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }
}
