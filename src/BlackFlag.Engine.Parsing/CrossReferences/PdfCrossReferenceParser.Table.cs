using System.Globalization;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.CrossReferences;

public sealed partial class PdfCrossReferenceParser
{
    private PdfCrossReferenceTable ParseTable(
        long offset)
    {
        if (
            offset < 0 ||
            offset >= source.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "A cross-reference table offset must identify a source byte.");
        }

        using var cursor =
            new BufferedRandomAccessCursor(
                source,
                initialPosition: offset);

        SkipWhiteSpace(cursor);
        ExpectAscii(cursor, "xref");
        RequireLineEnd(cursor);

        var sections =
            new List<PdfCrossReferenceSection>();

        long trailerDictionaryOffset;

        while (true)
        {
            string line =
                ReadNextNonEmptyLine(
                    cursor,
                    out long lineOffset);

            if (string.Equals(
                    line,
                    "trailer",
                    StringComparison.Ordinal))
            {
                trailerDictionaryOffset =
                    cursor.Position;

                break;
            }

            (
                int firstObjectNumber,
                int entryCount) =
                ParseSubsectionHeader(
                    line,
                    lineOffset);

            var entries =
                new PdfCrossReferenceEntry[entryCount];

            for (int index = 0; index < entryCount; index++)
            {
                string entryLine =
                    ReadLine(
                        cursor,
                        out long entryOffset);

                entries[index] =
                    ParseEntry(
                        entryLine,
                        firstObjectNumber,
                        index,
                        entryOffset);
            }

            sections.Add(
                new PdfCrossReferenceSection(
                    firstObjectNumber,
                    entries));
        }

        if (sections.Count == 0)
        {
            throw new FormatException(
                $"The cross-reference table at offset {offset} contains no "
                + "subsections.");
        }

        PdfDictionary trailer =
            ParseTrailerDictionary(
                trailerDictionaryOffset);

        return new PdfCrossReferenceTable(
            offset,
            sections,
            trailer);
    }

    private static (int FirstObjectNumber, int EntryCount)
        ParseSubsectionHeader(
            string line,
            long lineOffset)
    {
        string[] fields =
            line.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 2)
        {
            throw new FormatException(
                $"The subsection header at offset {lineOffset} must contain "
                + "two unsigned integers.");
        }

        if (!int.TryParse(
                fields[0],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int firstObjectNumber))
        {
            throw new FormatException(
                $"The subsection at offset {lineOffset} has an invalid first "
                + "object number.");
        }

        if (!int.TryParse(
                fields[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int entryCount))
        {
            throw new FormatException(
                $"The subsection at offset {lineOffset} has an invalid entry "
                + "count.");
        }

        if (
            firstObjectNumber < 0 ||
            entryCount < 0)
        {
            throw new FormatException(
                $"The subsection at offset {lineOffset} contains a negative "
                + "value.");
        }

        _ = checked(firstObjectNumber + entryCount);

        return (
            firstObjectNumber,
            entryCount);
    }

    private static PdfCrossReferenceEntry ParseEntry(
        string line,
        int firstObjectNumber,
        int entryIndex,
        long lineOffset)
    {
        string[] fields =
            line.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        if (
            fields.Length != 3 ||
            fields[0].Length != 10 ||
            fields[1].Length != 5 ||
            fields[2].Length != 1)
        {
            throw new FormatException(
                $"The entry at offset {lineOffset} must use the form "
                + "'0000000000 00000 n'.");
        }

        if (!long.TryParse(
                fields[0],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long entryOffset))
        {
            throw new FormatException(
                $"The entry at offset {lineOffset} has an invalid offset.");
        }

        if (!int.TryParse(
                fields[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int generationNumber))
        {
            throw new FormatException(
                $"The entry at offset {lineOffset} has an invalid generation.");
        }

        bool isInUse =
            fields[2] switch
            {
                "n" => true,
                "f" => false,
                _ => throw new FormatException(
                    $"The entry at offset {lineOffset} must end with 'n' or "
                    + "'f'.")
            };

        int objectNumber =
            checked(firstObjectNumber + entryIndex);

        return new PdfCrossReferenceEntry(
            new PdfObjectId(
                objectNumber,
                generationNumber),
            entryOffset,
            isInUse);
    }
}

