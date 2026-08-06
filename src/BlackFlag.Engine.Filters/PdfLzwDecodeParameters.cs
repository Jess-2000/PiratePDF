using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters;

internal readonly record struct PdfLzwDecodeParameters(
    int EarlyChange,
    PdfFlateDecodeParameters PredictorParameters)
{
    private static readonly PdfName EarlyChangeName =
        new("EarlyChange");

    public static PdfLzwDecodeParameters FromDictionary(
        PdfDictionary? dictionary)
    {
        int earlyChange = 1;

        if (
            dictionary is not null &&
            dictionary.TryGetValue(
                EarlyChangeName,
                out PdfObject value))
        {
            if (value is not PdfInteger integer)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    "The LZW decode parameter /EarlyChange must be an integer.");
            }

            if (
                integer.Value != 0 &&
                integer.Value != 1)
            {
                throw new PdfFilterDecodeException(
                    PdfFilterDecodeFailure.InvalidData,
                    "The LZW decode parameter /EarlyChange must be 0 or 1.");
            }

            earlyChange =
                (int)integer.Value;
        }

        return new PdfLzwDecodeParameters(
            earlyChange,
            PdfFlateDecodeParameters.FromDictionary(
                dictionary));
    }
}

