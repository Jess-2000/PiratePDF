namespace BlackFlag.Engine.Filters.Tests.Corpus;

internal sealed record PdfTextFilterCorpusCase(
    string Name,
    byte[] Encoded,
    byte[]? Expected,
    PdfFilterDecodeFailure? ExpectedFailure);

internal static class PdfTextFilterCorpus
{
    public static IReadOnlyList<PdfTextFilterCorpusCase> AsciiHexValid { get; } =
    [
        new(
            "Empty",
            ">"u8.ToArray(),
            [],
            null),
        new(
            "Uppercase",
            "48656C6C6F>"u8.ToArray(),
            "Hello"u8.ToArray(),
            null),
        new(
            "Lowercase",
            "68656c6c6f>"u8.ToArray(),
            "hello"u8.ToArray(),
            null),
        new(
            "Whitespace",
            "48 65\r\n6C\t6C 6F >"u8.ToArray(),
            "Hello"u8.ToArray(),
            null),
        new(
            "OddNibble",
            "A>"u8.ToArray(),
            [0xA0],
            null)
    ];

    public static IReadOnlyList<PdfTextFilterCorpusCase> AsciiHexInvalid { get; } =
    [
        new(
            "MissingTerminator",
            "48656C6C6F"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "InvalidCharacter",
            "48G0>"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "TrailingData",
            "00>X"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData)
    ];

    public static IReadOnlyList<PdfTextFilterCorpusCase> Ascii85Valid { get; } =
    [
        new(
            "Empty",
            "~>"u8.ToArray(),
            [],
            null),
        new(
            "HelloWorld",
            "87cURD]i,\"Ebo7~>"u8.ToArray(),
            "Hello World"u8.ToArray(),
            null),
        new(
            "ZeroTuple",
            "z~>"u8.ToArray(),
            [0, 0, 0, 0],
            null),
        new(
            "Whitespace",
            "87cU R\r\nD]i,\" Ebo7 ~>"u8.ToArray(),
            "Hello World"u8.ToArray(),
            null),
        new(
            "PartialTwo",
            "!!~>"u8.ToArray(),
            [0],
            null),
        new(
            "PartialThree",
            "!!!~>"u8.ToArray(),
            [0, 0],
            null),
        new(
            "PartialFour",
            "!!!!~>"u8.ToArray(),
            [0, 0, 0],
            null)
    ];

    public static IReadOnlyList<PdfTextFilterCorpusCase> Ascii85Invalid { get; } =
    [
        new(
            "MissingTerminator",
            "87cURD]i,\"Ebo7"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "MalformedZ",
            "!z~>"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "SingleFinalCharacter",
            "!~>"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "TupleOverflow",
            "uuuuu~>"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "InvalidCharacter",
            "{~>"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData),
        new(
            "TrailingData",
            "z~>X"u8.ToArray(),
            null,
            PdfFilterDecodeFailure.InvalidData)
    ];
}

