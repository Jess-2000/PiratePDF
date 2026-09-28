using System.Buffers.Binary;
using System.IO;
using System.Text;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Fonts;
using BlackFlag.Engine.Services.Tests.Text;
using BlackFlag.Engine.Writing;
using Xunit.Abstractions;

namespace BlackFlag.Engine.Services.Tests.Fonts;

public sealed partial class PdfExternalFontEmbeddingRightsTests(ITestOutputHelper output)
{
    [Fact]
    public void VersionThreeNoSubsettingRejectsType0BeforeProjection()
    {
        using var context = new PdfFontCollisionTestDocument("direct");
        var program = Program(3, 0x0100);
        output.WriteLine($"Sanitized: {program.Sha256}; program rights: {PdfExternalTrueTypeInspector.Inspect(program).EmbeddingRights}; provider: {context.Face.EmbeddingRights}");
        var failure = Record.Exception(() => PdfExternalType0TrueTypeResourceProjector.Create(
            PdfWritePlanner.Create(context.Document), PdfFilteredTextTestDocument.PageId,
            "FExt1", context.Face, program, "A"));
        output.WriteLine(failure?.ToString() ?? "Type0 subset projection succeeded.");
        Assert.IsType<NotSupportedException>(failure);
        context.AssertOriginalGraph();
    }

    [Fact]
    public void EditableProgramIsNotMisreportedAsSubsetOnly()
    {
        var program = Program(3, 0x0008);
        var metadata = PdfSanitizedExternalFontMetadataInspector.Inspect(program);
        output.WriteLine($"Metadata: {metadata.EmbeddingRights}; writer: {PdfExternalTrueTypeInspector.Inspect(program).EmbeddingRights}");
        Assert.Equal(PdfExternalFontEmbeddingRights.Editable, metadata.EmbeddingRights);
        Assert.Equal(metadata.EmbeddingRights, PdfExternalTrueTypeInspector.Inspect(program).EmbeddingRights);
    }

    internal static PdfSanitizedExternalFontProgram Program(ushort version, ushort fsType)
    {
        using var input = new MemoryStream(PdfExternalType0TrueTypeResourceProjectorTests.BuildFont(
            fsType, NameTable(), version), writable: false);
        return new PdfOpenTypeFontSanitizer().Sanitize(input);
    }

    private static byte[] NameTable()
    {
        byte[] name = Encoding.BigEndianUnicode.GetBytes("Rights Test");
        byte[] table = new byte[18 + name.Length];
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(4), 18);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(6), 3);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(10), 0x0409);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(14), checked((ushort)name.Length));
        name.CopyTo(table, 18);
        return table;
    }
}
