namespace BlackFlag.Engine.Filters;

internal ref struct PdfLzwBitReader
{
    private readonly ReadOnlySpan<byte> data;
    private int bitOffset;

    public PdfLzwBitReader(
        ReadOnlySpan<byte> data)
    {
        this.data = data;
        bitOffset = 0;
    }

    public bool TryRead(
        int width,
        out int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            width,
            1);

        if (
            bitOffset >
            checked(
                (data.Length * 8) -
                width))
        {
            value = 0;
            return false;
        }

        int result = 0;

        for (int index = 0; index < width; index++)
        {
            int absoluteBit =
                bitOffset + index;

            int byteIndex =
                absoluteBit >> 3;

            int bitInByte =
                7 -
                (absoluteBit & 7);

            result =
                (result << 1) |
                ((data[byteIndex] >> bitInByte) & 1);
        }

        bitOffset += width;
        value = result;
        return true;
    }
}

