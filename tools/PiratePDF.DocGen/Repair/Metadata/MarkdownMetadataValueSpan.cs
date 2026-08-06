namespace PiratePDF.DocGen.Repair.Metadata;

internal readonly record struct MarkdownMetadataValueSpan(
    int StartIndex,
    int Length,
    string Value)
{
    public int EndIndex =>
        checked(StartIndex + Length);
}