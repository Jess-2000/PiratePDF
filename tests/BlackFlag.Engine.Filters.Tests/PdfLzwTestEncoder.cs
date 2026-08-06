namespace BlackFlag.Engine.Filters.Tests;

internal static class PdfLzwTestEncoder
{
    public static byte[] EncodeLiteralCodes(
        ReadOnlySpan<byte> data,
        int earlyChange = 1)
    {
        var codes =
            new List<int>
            {
                256
            };

        foreach (byte value in data)
        {
            codes.Add(value);
        }

        codes.Add(257);

        return PackCodes(
            codes,
            earlyChange);
    }

    public static byte[] PackCodes(
        IReadOnlyList<int> codes,
        int earlyChange = 1)
    {
        var bytes =
            new List<byte>();

        int codeWidth = 9;
        int nextCode = 258;
        int previousCode = -1;
        int currentByte = 0;
        int bitCount = 0;

        foreach (int code in codes)
        {
            for (int bit = codeWidth - 1; bit >= 0; bit--)
            {
                currentByte =
                    (currentByte << 1) |
                    ((code >> bit) & 1);

                bitCount++;

                if (bitCount == 8)
                {
                    bytes.Add(
                        (byte)currentByte);

                    currentByte = 0;
                    bitCount = 0;
                }
            }

            if (code == 256)
            {
                codeWidth = 9;
                nextCode = 258;
                previousCode = -1;
                continue;
            }

            if (code == 257)
            {
                break;
            }

            if (
                previousCode >= 0 &&
                nextCode <= 4095)
            {
                nextCode++;

                if (
                    codeWidth < 12 &&
                    nextCode ==
                    (1 << codeWidth) -
                    earlyChange)
                {
                    codeWidth++;
                }
            }

            previousCode = code;
        }

        if (bitCount != 0)
        {
            bytes.Add(
                (byte)(
                    currentByte <<
                    (8 - bitCount)));
        }

        return bytes.ToArray();
    }
}

