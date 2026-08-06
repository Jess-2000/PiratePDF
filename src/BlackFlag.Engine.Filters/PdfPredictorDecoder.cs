using System.Buffers;

namespace BlackFlag.Engine.Filters;

internal static class PdfPredictorDecoder
{
    public static void Decode(
        ReadOnlySpan<byte> data,
        IBufferWriter<byte> destination,
        PdfFlateDecodeParameters parameters,
        IPdfFilterDecodeBudget budget)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(budget);

        switch (parameters.Predictor)
        {
            case 1:
                Write(
                    destination,
                    data,
                    budget);
                return;

            case 2:
                DecodeTiff(
                    data,
                    destination,
                    parameters,
                    budget);
                return;

            default:
                DecodePng(
                    data,
                    destination,
                    parameters,
                    budget);
                return;
        }
    }

    private static void DecodeTiff(
        ReadOnlySpan<byte> data,
        IBufferWriter<byte> destination,
        PdfFlateDecodeParameters parameters,
        IPdfFilterDecodeBudget budget)
    {
        if (
            parameters.BitsPerComponent != 8 &&
            parameters.BitsPerComponent != 16)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "TIFF Predictor 2 currently requires 8- or 16-bit components.");
        }

        int rowByteCount =
            parameters.GetRowByteCount();

        if (data.Length % rowByteCount != 0)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "TIFF predictor data does not contain complete rows.");
        }

        int bytesPerPixel =
            parameters.GetBytesPerPixel();

        byte[] row =
            ArrayPool<byte>.Shared.Rent(
                rowByteCount);

        try
        {
            for (
                int offset = 0;
                offset < data.Length;
                offset += rowByteCount)
            {
                budget.ThrowIfCancellationRequested();

                data.Slice(
                        offset,
                        rowByteCount)
                    .CopyTo(row);

                if (parameters.BitsPerComponent == 8)
                {
                    for (
                        int index = bytesPerPixel;
                        index < rowByteCount;
                        index++)
                    {
                        row[index] =
                            unchecked(
                                (byte)(
                                    row[index] +
                                    row[index - bytesPerPixel]));
                    }
                }
                else
                {
                    DecodeTiff16BitRow(
                        row.AsSpan(
                            0,
                            rowByteCount),
                        bytesPerPixel);
                }

                Write(
                    destination,
                    row.AsSpan(
                        0,
                        rowByteCount),
                    budget);
            }
        }
        finally
        {
            row.AsSpan(
                    0,
                    rowByteCount)
                .Clear();

            ArrayPool<byte>.Shared.Return(row);
        }
    }

    private static void DecodeTiff16BitRow(
        Span<byte> row,
        int bytesPerPixel)
    {
        for (
            int index = bytesPerPixel;
            index + 1 < row.Length;
            index += 2)
        {
            int priorIndex =
                index -
                bytesPerPixel;

            ushort current =
                (ushort)(
                    (row[index] << 8) |
                    row[index + 1]);

            ushort prior =
                (ushort)(
                    (row[priorIndex] << 8) |
                    row[priorIndex + 1]);

            ushort decoded =
                unchecked(
                    (ushort)(
                        current +
                        prior));

            row[index] =
                (byte)(
                    decoded >> 8);

            row[index + 1] =
                (byte)decoded;
        }
    }

    private static void DecodePng(
        ReadOnlySpan<byte> data,
        IBufferWriter<byte> destination,
        PdfFlateDecodeParameters parameters,
        IPdfFilterDecodeBudget budget)
    {
        int rowByteCount =
            parameters.GetRowByteCount();

        int encodedRowByteCount =
            checked(
                rowByteCount + 1);

        if (data.Length % encodedRowByteCount != 0)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "PNG predictor data does not contain complete prefixed rows.");
        }

        int bytesPerPixel =
            parameters.GetBytesPerPixel();

        byte[] previous =
            ArrayPool<byte>.Shared.Rent(
                rowByteCount);

        byte[] current =
            ArrayPool<byte>.Shared.Rent(
                rowByteCount);

        previous.AsSpan(
                0,
                rowByteCount)
            .Clear();

        try
        {
            for (
                int offset = 0;
                offset < data.Length;
                offset += encodedRowByteCount)
            {
                budget.ThrowIfCancellationRequested();

                byte filter =
                    data[offset];

                ValidatePngFilter(
                    filter,
                    parameters.Predictor);

                data.Slice(
                        offset + 1,
                        rowByteCount)
                    .CopyTo(current);

                ApplyPngFilter(
                    filter,
                    current.AsSpan(
                        0,
                        rowByteCount),
                    previous.AsSpan(
                        0,
                        rowByteCount),
                    bytesPerPixel);

                Write(
                    destination,
                    current.AsSpan(
                        0,
                        rowByteCount),
                    budget);

                (previous, current) =
                    (current, previous);
            }
        }
        finally
        {
            previous.AsSpan(
                    0,
                    rowByteCount)
                .Clear();

            current.AsSpan(
                    0,
                    rowByteCount)
                .Clear();

            ArrayPool<byte>.Shared.Return(previous);
            ArrayPool<byte>.Shared.Return(current);
        }
    }

    private static void ValidatePngFilter(
        byte filter,
        int predictor)
    {
        if (filter > 4)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                $"PNG predictor row uses unsupported filter byte {filter}.");
        }

        if (
            predictor >= 10 &&
            predictor <= 14 &&
            filter != predictor - 10)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                $"PNG predictor {predictor} expected row filter {predictor - 10}, but found {filter}.");
        }
    }

    private static void ApplyPngFilter(
        byte filter,
        Span<byte> current,
        ReadOnlySpan<byte> previous,
        int bytesPerPixel)
    {
        switch (filter)
        {
            case 0:
                return;

            case 1:
                for (
                    int index = bytesPerPixel;
                    index < current.Length;
                    index++)
                {
                    current[index] =
                        unchecked(
                            (byte)(
                                current[index] +
                                current[index - bytesPerPixel]));
                }

                return;

            case 2:
                for (
                    int index = 0;
                    index < current.Length;
                    index++)
                {
                    current[index] =
                        unchecked(
                            (byte)(
                                current[index] +
                                previous[index]));
                }

                return;

            case 3:
                for (
                    int index = 0;
                    index < current.Length;
                    index++)
                {
                    int left =
                        index >= bytesPerPixel
                            ? current[index - bytesPerPixel]
                            : 0;

                    int above =
                        previous[index];

                    current[index] =
                        unchecked(
                            (byte)(
                                current[index] +
                                ((left + above) / 2)));
                }

                return;

            case 4:
                for (
                    int index = 0;
                    index < current.Length;
                    index++)
                {
                    byte left =
                        index >= bytesPerPixel
                            ? current[index - bytesPerPixel]
                            : (byte)0;

                    byte above =
                        previous[index];

                    byte upperLeft =
                        index >= bytesPerPixel
                            ? previous[index - bytesPerPixel]
                            : (byte)0;

                    current[index] =
                        unchecked(
                            (byte)(
                                current[index] +
                                Paeth(
                                    left,
                                    above,
                                    upperLeft)));
                }

                return;

            default:
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    $"PNG predictor row uses unsupported filter byte {filter}.");
        }
    }

    private static byte Paeth(
        byte left,
        byte above,
        byte upperLeft)
    {
        int estimate =
            left +
            above -
            upperLeft;

        int leftDistance =
            Math.Abs(
                estimate -
                left);

        int aboveDistance =
            Math.Abs(
                estimate -
                above);

        int upperLeftDistance =
            Math.Abs(
                estimate -
                upperLeft);

        if (
            leftDistance <= aboveDistance &&
            leftDistance <= upperLeftDistance)
        {
            return left;
        }

        return aboveDistance <= upperLeftDistance
            ? above
            : upperLeft;
    }

    private static void Write(
        IBufferWriter<byte> destination,
        ReadOnlySpan<byte> data,
        IPdfFilterDecodeBudget budget)
    {
        if (data.IsEmpty)
        {
            return;
        }

        budget.ThrowIfCancellationRequested();

        Span<byte> output =
            destination.GetSpan(
                data.Length);

        data.CopyTo(output);
        destination.Advance(
            data.Length);
    }
}

