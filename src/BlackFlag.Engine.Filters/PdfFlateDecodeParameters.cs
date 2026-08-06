using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

/// <summary>
/// Represents validated parameters used after Flate decompression.
/// </summary>
internal readonly record struct PdfFlateDecodeParameters(
    int Predictor,
    int Colors,
    int BitsPerComponent,
    int Columns)
{
    private static readonly PdfName PredictorName = new("Predictor");
    private static readonly PdfName ColorsName = new("Colors");
    private static readonly PdfName BitsPerComponentName =
        new("BitsPerComponent");
    private static readonly PdfName ColumnsName = new("Columns");

    public static PdfFlateDecodeParameters FromDictionary(
        PdfDictionary? dictionary)
    {
        int predictor =
            GetInteger(
                dictionary,
                PredictorName,
                defaultValue: 1);

        int colors =
            GetInteger(
                dictionary,
                ColorsName,
                defaultValue: 1);

        int bitsPerComponent =
            GetInteger(
                dictionary,
                BitsPerComponentName,
                defaultValue: 8);

        int columns =
            GetInteger(
                dictionary,
                ColumnsName,
                defaultValue: 1);

        if (
            predictor != 1 &&
            predictor != 2 &&
            (predictor < 10 || predictor > 15))
        {
            throw InvalidParameter(
                "Predictor",
                predictor,
                "1, 2, or a value from 10 through 15");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(colors);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

        if (
            bitsPerComponent != 1 &&
            bitsPerComponent != 2 &&
            bitsPerComponent != 4 &&
            bitsPerComponent != 8 &&
            bitsPerComponent != 16)
        {
            throw InvalidParameter(
                "BitsPerComponent",
                bitsPerComponent,
                "1, 2, 4, 8, or 16");
        }

        return new PdfFlateDecodeParameters(
            predictor,
            colors,
            bitsPerComponent,
            columns);
    }

    public int GetRowByteCount()
    {
        long bitsPerRow =
            checked(
                (long)Colors *
                BitsPerComponent *
                Columns);

        long rowByteCount =
            checked(
                (bitsPerRow + 7) /
                8);

        if (
            rowByteCount <= 0 ||
            rowByteCount > int.MaxValue)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "The Flate predictor row size exceeds the supported range.");
        }

        return (int)rowByteCount;
    }

    public int GetBytesPerPixel()
    {
        long bitsPerPixel =
            checked(
                (long)Colors *
                BitsPerComponent);

        long byteCount =
            checked(
                (bitsPerPixel + 7) /
                8);

        if (
            byteCount <= 0 ||
            byteCount > int.MaxValue)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                "The Flate predictor pixel size exceeds the supported range.");
        }

        return (int)byteCount;
    }

    private static int GetInteger(
        PdfDictionary? dictionary,
        PdfName name,
        int defaultValue)
    {
        if (
            dictionary is null ||
            !dictionary.TryGetValue(
                name,
                out PdfObject value))
        {
            return defaultValue;
        }

        if (value is not PdfInteger integer)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                $"The Flate decode parameter /{name.Value} must be an integer.");
        }

        if (
            integer.Value < int.MinValue ||
            integer.Value > int.MaxValue)
        {
            throw new PdfFilterDecodeException(
                PdfFilterDecodeFailure.InvalidData,
                $"The Flate decode parameter /{name.Value} exceeds the supported integer range.");
        }

        return (int)integer.Value;
    }

    private static PdfFilterDecodeException InvalidParameter(
        string parameter,
        int value,
        string expected)
    {
        return new PdfFilterDecodeException(
            PdfFilterDecodeFailure.InvalidData,
            $"The Flate decode parameter /{parameter} has value {value}; expected {expected}.");
    }
}
