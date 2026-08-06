namespace BlackFlag.Engine.Filters.Tests.Corpus;

public sealed record PdfBinaryFilterCorpusCase(
    string Name,
    byte[] Encoded,
    byte[]? Expected,
    PdfFilterDecodeFailure? ExpectedFailure);

public static class PdfBinaryFilterCorpus
{
    public static IReadOnlyList<PdfBinaryFilterCorpusCase> RunLengthValid { get; } =
    [
        new(
            "Empty",
            [128],
            [],
            null),
        new(
            "Literal",
            [2, (byte)'A', (byte)'B', (byte)'C', 128],
            "ABC"u8.ToArray(),
            null),
        new(
            "Repeated",
            [254, (byte)'Z', 128],
            "ZZZ"u8.ToArray(),
            null),
        new(
            "Mixed",
            [
                1,
                (byte)'A',
                (byte)'B',
                252,
                (byte)'C',
                128
            ],
            "ABCCCCC"u8.ToArray(),
            null)
    ];

    public static IReadOnlyList<PdfBinaryFilterCorpusCase> RunLengthInvalid { get; } =
    [
        new(
            "MissingMarker",
            [0, (byte)'A'],
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "IncompleteLiteral",
            [2, (byte)'A', 128],
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "IncompleteRepeated",
            [255],
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "TrailingData",
            [128, 0],
            null,
            PdfFilterDecodeFailure.InvalidData)
    ];
}
