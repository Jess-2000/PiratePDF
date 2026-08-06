namespace PiratePDF.DocGen.Library;

internal sealed record DocumentationSection(
    string Heading,
    int Level,
    int StartLine,
    int EndLine)
{
    public int LineCount =>
        EndLine >= StartLine
            ? EndLine - StartLine + 1
            : 0;
}
