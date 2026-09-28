using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlackFlag.Engine.Fonts.Tests;

namespace PiratePDF.Fuzz.Tests;

internal sealed record CorpusRecipe(string Id, string Target, string Category, string Purpose,
    string ExpectedOutcome, string SourceRecipe, string[] Related, Func<byte[]> Generate);

/// <summary>Owned fixture recipes and independently chosen expectations; no target execution.</summary>
internal static class CorpusSeedRecipes
{
    private const string TextSource = "tests/BlackFlag.Engine.Filters.Tests/Corpus/PdfTextFilterCorpus.cs";
    private const string FlateSource = "tests/BlackFlag.Engine.Filters.Tests/PdfFlateCompletenessTests.cs";
    private const string TargetSource = "tests/PiratePDF.Fuzz.Tests/ManagedTargetTests.cs";

    internal static IReadOnlyList<CorpusRecipe> All { get; } = Array.AsReadOnly<CorpusRecipe>(
    [
        new("asciihex-empty", "filter-asciihex", "valid-minimal", "Decode an immediate terminator to empty bytes.", "success",
            TextSource + "#AsciiHexValid.Empty", ["BF-1040"], static () => ">"u8.ToArray()),
        new("asciihex-invalid-character", "filter-asciihex", "malformed-structural", "Reject G inside a hexadecimal pair (InvalidData).", "rejected",
            TextSource + "#AsciiHexInvalid.InvalidCharacter", ["BF-1040"], static () => "48G0>"u8.ToArray()),
        new("asciihex-missing-terminator", "filter-asciihex", "truncated", "Reject complete pairs without the required terminator (InvalidData).", "rejected",
            TextSource + "#AsciiHexInvalid.MissingTerminator", ["BF-1040"], static () => "48656C6C6F"u8.ToArray()),
        new("asciihex-odd-nibble", "filter-asciihex", "valid-minimal", "Pad the final low nibble with zero, yielding A0.", "success",
            TextSource + "#AsciiHexValid.OddNibble", ["BF-1040"], static () => "A>"u8.ToArray()),
        new("asciihex-trailing-data", "filter-asciihex", "malformed-structural", "Reject non-whitespace after the terminator (InvalidData).", "rejected",
            TextSource + "#AsciiHexInvalid.TrailingData", ["BF-1040"], static () => "00>X"u8.ToArray()),
        new("asciihex-whitespace", "filter-asciihex", "valid-minimal", "Decode Hello with supported space, CR, LF and tab separators.", "success",
            TextSource + "#AsciiHexValid.Whitespace", ["BF-1040"], static () => "48 65\r\n6C\t6C 6F >"u8.ToArray()),
        new("flate-decoded-exact", "filter-flate", "resource-boundary", "Accept 32768 decoded bytes while remaining below expansion ratio 32.", "success",
            TargetSource + "#FlateUsesDecodedByteLimitAndExpansionRatio", ["POST-P0-001"], static () => Compress(BudgetPayload(32768))),
        new("flate-decoded-over", "filter-flate", "resource-boundary", "Reject 32769 decoded bytes (DecodedByteLimitExceeded), with ratio below 32.", "rejected",
            TargetSource + "#FlateUsesDecodedByteLimitAndExpansionRatio", ["POST-P0-001"], static () => Compress(BudgetPayload(32769))),
        new("flate-empty", "filter-flate", "valid-minimal", "Accept a complete empty zlib member.", "success",
            FlateSource + "#CompleteEmptyMemberRemainsValid", ["F3-OBS-01"], static () => Convert.FromHexString("789C030000000001")),
        new("flate-expansion-over", "filter-flate", "resource-boundary", "Reject 4096 zero bytes expanded beyond ratio 32 (ExpansionRatioExceeded).", "rejected",
            TargetSource + "#FlateUsesDecodedByteLimitAndExpansionRatio", ["POST-P0-001"], static () => Compress(new byte[4096])),
        new("flate-hello", "filter-flate", "valid-minimal", "Decode the complete zlib member for Hello.", "success",
            TargetSource + "#FlateDecodesOwnedInputAndRejectsTruncation", ["F3-OBS-01"], static () => Compress("Hello"u8)),
        new("flate-truncated", "filter-flate", "historical-regression", "Reject the Hello member missing its last checksum byte (InvalidData).", "rejected",
            TargetSource + "#FlateDecodesOwnedInputAndRejectsTruncation", ["F3-OBS-01"], static () => Compress("Hello"u8)[..^1]),
        new("font-minimal", "font-sanitize", "valid-minimal", "Accept the owned one-glyph TrueType fixture with coherent required tables/checksums.", "success",
            "tests/BlackFlag.Engine.Fonts.Tests/TestFontFactory.cs#CreateMinimalTrueType", ["BF-1054"], MinimalFont),
        new("font-too-many-tables", "font-sanitize", "resource-boundary", "Reject an SFNT header declaring 33 tables, above v1 limit 32 (TooManyTables).", "rejected",
            TargetSource + "#FontSanitizerUsesDomainRejectionsAndConfiguredTableCeiling", ["BF-1054"], TooManyTables),
        new("font-truncated-header", "font-sanitize", "truncated", "Reject eleven zero bytes before reading a complete twelve-byte SFNT header (MalformedHeader).", "rejected",
            "tests/PiratePDF.Fuzz.Tests/CorpusSeedRecipes.cs#TruncatedHeader", ["BF-1054"], TruncatedHeader)
    ]);

    internal static byte[] BudgetPayload(int length)
    {
        // Same fixed-seed fixture as the existing permanent budget regression.
        byte[] block = new byte[4096];
        new Random(1049).NextBytes(block);
        byte[] payload = new byte[length];
        for (int index = 0; index < payload.Length; index++) payload[index] = block[index % block.Length];
        return payload;
    }

    private static byte[] Compress(ReadOnlySpan<byte> bytes)
    {
        using var output = new MemoryStream();
        using (var compressor = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) compressor.Write(bytes);
        return output.ToArray();
    }

    private static byte[] MinimalFont()
    {
        using MemoryStream font = TestFontFactory.CreateMinimalTrueType();
        return font.ToArray();
    }

    private static byte[] TooManyTables()
    {
        byte[] header = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(header, 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4), 33);
        return header;
    }

    private static byte[] TruncatedHeader() => new byte[11];

    internal static void WriteNewCorpus(string repositoryRoot)
    {
        string root = Path.Combine(Path.GetFullPath(repositoryRoot), "tests", "FuzzCorpus");
        if (Directory.Exists(root)) throw new InvalidOperationException("Corpus generation never overwrites an existing corpus.");
        Directory.CreateDirectory(root);
        var entries = new List<object>();
        foreach (CorpusRecipe recipe in All)
        {
            byte[] bytes = recipe.Generate();
            string directory = Path.Combine(root, recipe.Target);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, recipe.Id + ".bin"), bytes);
            entries.Add(new
            {
                id = recipe.Id, target = recipe.Target, configurationVersion = 1,
                path = $"tests/FuzzCorpus/{recipe.Target}/{recipe.Id}.bin", length = bytes.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)), sourceRecipe = recipe.SourceRecipe,
                origin = "repository-owned", rights = "Repository-owned synthetic fixture; no external license is asserted.",
                category = recipe.Category, purpose = recipe.Purpose, expectedOutcome = recipe.ExpectedOutcome,
                related = recipe.Related
            });
        }
        string json = JsonSerializer.Serialize(new { schema = "piratepdf-fuzz-corpus", version = 1, seeds = entries },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(root, "manifest.json"), json + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.bin -text\nmanifest.json text eol=lf\n", new UTF8Encoding(false));
    }
}
