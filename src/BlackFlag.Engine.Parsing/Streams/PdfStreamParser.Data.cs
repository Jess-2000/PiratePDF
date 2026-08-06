namespace BlackFlag.Engine.Parsing.Streams;

internal sealed partial class PdfStreamParser
{
    private byte[] ReadData(
        long offset,
        int length)
    {
        if (length == 0)
        {
            return [];
        }

        long endOffset;

        try
        {
            endOffset =
                checked(offset + length);
        }
        catch (OverflowException)
        {
            throw new FormatException(
                "The PDF stream data range exceeds the supported source range.");
        }

        if (endOffset > source.Length)
        {
            throw new EndOfStreamException(
                "The source ends before the declared PDF stream data is complete.");
        }

        byte[] data =
            GC.AllocateUninitializedArray<byte>(
                length);

        ReadExactly(
            offset,
            data);

        return data;
    }

    private void ReadExactly(
        long offset,
        Span<byte> destination)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < destination.Length)
        {
            int bytesRead =
                source.Read(
                    checked(offset + totalBytesRead),
                    destination[totalBytesRead..]);

            if (
                bytesRead < 0 ||
                bytesRead > destination.Length - totalBytesRead)
            {
                throw new InvalidDataException(
                    "The random-access source returned an invalid byte count.");
            }

            if (bytesRead == 0)
            {
                throw new EndOfStreamException(
                    "The source ended before the declared PDF stream data was complete.");
            }

            totalBytesRead += bytesRead;
        }
    }
}
