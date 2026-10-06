using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed class PdfExternalFontProgramRightsTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (ushort version in new ushort[] { 0, 1, 2, 3, 4, 5 })
        foreach (var item in new (ushort Flags, PdfExternalFontEmbeddingRights Level, bool NoSubset, bool Bitmap)[]
        {
            (0, PdfExternalFontEmbeddingRights.Allowed, false, false),
            (2, PdfExternalFontEmbeddingRights.Restricted, false, false),
            (4, PdfExternalFontEmbeddingRights.PreviewAndPrint, false, false),
            (8, PdfExternalFontEmbeddingRights.Editable, false, false),
            (0x100, PdfExternalFontEmbeddingRights.Allowed, true, false),
            (0x200, PdfExternalFontEmbeddingRights.Allowed, false, true),
            (0x102, PdfExternalFontEmbeddingRights.Restricted, true, false),
            (0x104, PdfExternalFontEmbeddingRights.PreviewAndPrint, true, false),
            (0x108, PdfExternalFontEmbeddingRights.Editable, true, false),
            (0x204, PdfExternalFontEmbeddingRights.PreviewAndPrint, false, true),
            (0x208, PdfExternalFontEmbeddingRights.Editable, false, true),
            (0x308, PdfExternalFontEmbeddingRights.Editable, true, true),
            (0x8000, PdfExternalFontEmbeddingRights.Allowed, false, false),
            (1, PdfExternalFontEmbeddingRights.Allowed, false, false),
            (6, version < 3 ? PdfExternalFontEmbeddingRights.PreviewAndPrint : PdfExternalFontEmbeddingRights.Unknown, false, false),
            (10, version < 3 ? PdfExternalFontEmbeddingRights.Editable : PdfExternalFontEmbeddingRights.Unknown, false, false),
            (12, version < 3 ? PdfExternalFontEmbeddingRights.Editable : PdfExternalFontEmbeddingRights.Unknown, false, false),
            (14, version < 3 ? PdfExternalFontEmbeddingRights.Editable : PdfExternalFontEmbeddingRights.Unknown, false, false)
        })
            yield return [version, item.Flags, item.Level, version >= 2 && item.NoSubset, version >= 2 && item.Bitmap];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void VersionedProgramRestrictionsAuthorizeOnlyCompatibleOperations(
        ushort version, ushort flags, PdfExternalFontEmbeddingRights level, bool noSubset, bool bitmap)
    {
        var program = Create(version, flags);
        var metrics = PdfExternalTrueTypeInspector.Inspect(program);
        Assert.Equal(level, metrics.EmbeddingRights);
        Assert.Equal(noSubset, metrics.NoSubsetting);
        Assert.Equal(bitmap, metrics.BitmapEmbeddingOnly);
        var face = Face(PdfExternalFontEmbeddingRights.Allowed);
        bool editable = level is PdfExternalFontEmbeddingRights.Allowed or PdfExternalFontEmbeddingRights.Editable;
        foreach (bool subset in new[] { false, true })
        {
            Exception failure = Record.Exception(() => PdfExternalFontEmbeddingPolicy.EnsureCanEmbed(face, program, subset));
            if (editable && !bitmap && !(subset && noSubset)) Assert.Null(failure);
            else Assert.IsType<NotSupportedException>(failure);
        }
    }

    [Fact]
    public void MissingAndUnknownVersionRemainUnknownWithoutInventingPermission()
    {
        using var input = TestFontFactory.CreateMinimalTrueType();
        var missing = new PdfOpenTypeFontSanitizer().Sanitize(input);
        foreach (var program in new[] { missing, Create(6, 0) })
        {
            Assert.Equal(PdfExternalFontEmbeddingRights.Unknown, PdfExternalTrueTypeInspector.Inspect(program).EmbeddingRights);
            Assert.Throws<NotSupportedException>(() => PdfExternalFontEmbeddingPolicy.EnsureCanEmbed(Face(PdfExternalFontEmbeddingRights.Allowed), program, false));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(77)]
    public void TruncatedOs2IsStructuralDomainFailure(int length)
    {
        using var input = TestFontFactory.CreateMinimalTrueType(new byte[length]);
        var failure = Assert.Throws<PdfExternalFontSanitizationException>(() => new PdfOpenTypeFontSanitizer().Sanitize(input));
        Assert.Equal(PdfExternalFontSanitizationFailure.MalformedRequiredTable, failure.Failure);
    }

    [Fact]
    public void PublicBytesCannotUpgradeCachedRestrictions()
    {
        var program = Create(3, 0x108);
        MemoryMarshal.AsMemory(program.Bytes).Span.Clear();
        using Stream reader = program.OpenRead();
        Assert.Throws<NotSupportedException>(() => reader.Write(new byte[16]));
        Assert.True(PdfExternalTrueTypeInspector.Inspect(program).NoSubsetting);
        Assert.Throws<NotSupportedException>(() => new PdfUnicodeTrueTypeGlyphSubsetter().CreateSubset(program, ""));
    }

    [Fact]
    public void ExistingEnumNumbersAndProviderConstructorRemainCompatible()
    {
        Assert.Equal(0, (int)PdfExternalFontEmbeddingRights.Unknown);
        Assert.Equal(1, (int)PdfExternalFontEmbeddingRights.Allowed);
        Assert.Equal(2, (int)PdfExternalFontEmbeddingRights.SubsetOnly);
        Assert.Equal(3, (int)PdfExternalFontEmbeddingRights.Restricted);
        Assert.False(Face(PdfExternalFontEmbeddingRights.Allowed).NoSubsetting);
        Assert.Throws<ArgumentOutOfRangeException>(() => Face((PdfExternalFontEmbeddingRights)99));
    }

    private static PdfExternalFontFace Face(PdfExternalFontEmbeddingRights rights) => new(
        "test", "face", "Test", "Test", 400, 5, PdfExternalFontSlant.Upright, PdfExternalFontTechnology.TrueType, rights);

    private static PdfSanitizedExternalFontProgram Create(ushort version, ushort flags)
    {
        byte[] os2 = new byte[version switch { 0 => 78, 1 => 86, 5 => 100, _ => 96 }];
        BinaryPrimitives.WriteUInt16BigEndian(os2, version);
        BinaryPrimitives.WriteUInt16BigEndian(os2.AsSpan(8), flags);
        using var input = TestFontFactory.CreateMinimalTrueType(os2);
        return new PdfOpenTypeFontSanitizer().Sanitize(input);
    }
}
