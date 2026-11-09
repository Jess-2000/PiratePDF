using System.Buffers.Binary;
using BlackFlag.Engine.Fonts.Tests;

namespace BlackFlag.Engine.Services.Tests.Conformance;

/// <summary>Owned, unhinted rectangle outlines; no system-font lookup or external asset.</summary>
internal static class ConformanceFont
{
    internal static byte[] Create()
    {
        byte[] head = new byte[54];
        U32(head, 0, 0x00010000);
        U32(head, 12, 0x5F0F3CF5);
        U16(head, 18, 1000);
        U16(head, 40, 500);
        U16(head, 42, 700);
        U16(head, 50, 1);
        byte[] hhea = new byte[36];
        U32(hhea, 0, 0x00010000);
        U16(hhea, 4, 800);
        U16(hhea, 6, unchecked((ushort)-200));
        U16(hhea, 34, 3);
        byte[] maxp = new byte[32];
        U32(maxp, 0, 0x00010000);
        U16(maxp, 4, 3);
        U16(maxp, 6, 4);
        U16(maxp, 8, 1);
        byte[] hmtx = new byte[12];
        for (int index = 0; index < 3; index++) U16(hmtx, index * 4, 600);
        byte[] cmap = new byte[274];
        U16(cmap, 2, 1);
        U16(cmap, 4, 3);
        U16(cmap, 6, 1);
        U32(cmap, 8, 12);
        U16(cmap, 14, 262);
        cmap[18 + 65] = 1;
        cmap[18 + 66] = 2;
        byte[] a = Rectangle(500);
        byte[] b = Rectangle(250);
        byte[] glyf = [.. a, .. b];
        byte[] loca = new byte[16];
        U32(loca, 8, (uint)a.Length); // GID 0 empty, GID 1 wide, GID 2 narrow.
        U32(loca, 12, (uint)glyf.Length);
        return TestFontFactory.BuildSfnt(0x00010000, new Dictionary<string, byte[]>
        {
            ["head"] = head, ["hhea"] = hhea, ["maxp"] = maxp,
            ["hmtx"] = hmtx, ["cmap"] = cmap, ["glyf"] = glyf, ["loca"] = loca,
            ["name"] = [0, 0, 0, 0, 0, 6], ["OS/2"] = new byte[78]
        });
    }

    private static byte[] Rectangle(ushort width)
    {
        byte[] glyph = new byte[36];
        U16(glyph, 0, 1);
        U16(glyph, 6, width);
        U16(glyph, 8, 700);
        U16(glyph, 10, 3); // One four-point, on-curve contour, no instructions.
        glyph.AsSpan(14, 4).Fill(1);
        U16(glyph, 20, width);
        U16(glyph, 24, unchecked((ushort)-width));
        U16(glyph, 30, 700);
        return glyph;
    }

    private static void U16(byte[] bytes, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset), value);
    private static void U32(byte[] bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), value);
}
