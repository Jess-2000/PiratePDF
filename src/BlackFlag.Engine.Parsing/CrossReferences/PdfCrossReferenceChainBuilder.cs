namespace BlackFlag.Engine.Parsing.CrossReferences;

internal sealed class PdfCrossReferenceChainBuilder
{
    private readonly List<PdfCrossReferenceTable> tables = [];
    private readonly Dictionary<PdfObjectId, PdfCrossReferenceEntry> entries = [];

    public void AddTable(
        PdfCrossReferenceTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        tables.Add(table);

        foreach (PdfCrossReferenceSection section in table.Sections)
        {
            foreach (PdfCrossReferenceEntry entry in section.Entries)
            {
                entries[entry.ObjectId] = entry;
            }
        }
    }

    public PdfCrossReferenceChain Build()
    {
        if (tables.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one table is required to build a cross-reference chain.");
        }

        return new PdfCrossReferenceChain(
            tables,
            entries);
    }
}
