using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using BlackFlag.Engine.Security.Formats.Ole;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Formats.Ole;

public sealed class LegacyWordBinarySecurityAnalyzerTests
{
    [Fact]
    public void NonDocFileNameIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new LegacyWordBinarySecurityAnalyzer()
                .Analyze(
                    new byte[512],
                    "legacy.xls"));
    }

    [Fact]
    public void InvalidCompoundFileIsRejected()
    {
        byte[] content = new byte[512];

        byte[] signature =
        [
            0xD0, 0xCF, 0x11, 0xE0,
            0xA1, 0xB1, 0x1A, 0xE1
        ];

        signature.CopyTo(content, 0);

        Assert.Throws<InvalidDataException>(
            () => new LegacyWordBinarySecurityAnalyzer()
                .Analyze(
                    content,
                    "invalid.doc"));
    }

    [Fact]
    public void MacroStorageIsDetectedInValidCompoundFile()
    {
        byte[] document = CreateCompoundFileWithMacroStorage();

        LegacyWordBinarySecurityAnalysisResult result =
            new LegacyWordBinarySecurityAnalyzer()
                .Analyze(
                    document,
                    "macro.doc");

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "LegacyMacroContent" &&
                item.Category ==
                    DocumentSecurityObservationCategory.ScriptContent);
    }

    private static byte[] CreateCompoundFileWithMacroStorage()
    {
        const uint FreeSector = 0xFFFFFFFF;
        const uint EndOfChain = 0xFFFFFFFE;
        const uint FatSector = 0xFFFFFFFD;

        byte[] bytes = new byte[1536];
        Span<byte> header = bytes.AsSpan(0, 512);

        byte[] signature =
            [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        signature.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(24, 2), 0x003E);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(26, 2), 0x0003);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(28, 2), 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(30, 2), 9);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(32, 2), 6);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(44, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(48, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(56, 4), 4096);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(60, 4), EndOfChain);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(68, 4), EndOfChain);

        for (int index = 0; index < 109; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                header.Slice(76 + (index * 4), 4),
                index == 0 ? 0U : FreeSector);
        }

        Span<byte> fat = bytes.AsSpan(512, 512);
        for (int offset = 0; offset < fat.Length; offset += 4)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                fat.Slice(offset, 4),
                FreeSector);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(fat.Slice(0, 4), FatSector);
        BinaryPrimitives.WriteUInt32LittleEndian(fat.Slice(4, 4), EndOfChain);

        Span<byte> directory = bytes.AsSpan(1024, 512);
        WriteDirectoryEntry(
            directory.Slice(0, 128),
            "Root Entry",
            type: 5,
            startSector: EndOfChain,
            streamSize: 0);

        WriteDirectoryEntry(
            directory.Slice(128, 128),
            "Macros",
            type: 1,
            startSector: EndOfChain,
            streamSize: 0);

        return bytes;
    }

    private static void WriteDirectoryEntry(
        Span<byte> entry,
        string name,
        byte type,
        uint startSector,
        long streamSize)
    {
        byte[] nameBytes =
            Encoding.Unicode.GetBytes(
                name + "\0");

        nameBytes.CopyTo(entry);

        BinaryPrimitives.WriteUInt16LittleEndian(
            entry.Slice(64, 2),
            checked((ushort)nameBytes.Length));

        entry[66] = type;

        BinaryPrimitives.WriteUInt32LittleEndian(
            entry.Slice(116, 4),
            startSector);

        BinaryPrimitives.WriteInt64LittleEndian(
            entry.Slice(120, 8),
            streamSize);
    }
}
