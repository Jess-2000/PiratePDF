using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Resources;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>
/// Constructs a conservative WinAnsi simple TrueType PDF font resource from
/// sanitized external font bytes and projects it into one direct page resource
/// dictionary.
/// </summary>
public static class PdfExternalTrueTypeResourceProjector
{
    private static readonly PdfName TypeName = PdfName.FromLatin1("Type");
    private static readonly PdfName FontName = PdfName.FromLatin1("Font");
    private static readonly PdfName SubtypeName = PdfName.FromLatin1("Subtype");
    private static readonly PdfName TrueTypeName = PdfName.FromLatin1("TrueType");
    private static readonly PdfName BaseFontName = PdfName.FromLatin1("BaseFont");
    private static readonly PdfName FirstCharName = PdfName.FromLatin1("FirstChar");
    private static readonly PdfName LastCharName = PdfName.FromLatin1("LastChar");
    private static readonly PdfName WidthsName = PdfName.FromLatin1("Widths");
    private static readonly PdfName EncodingName = PdfName.FromLatin1("Encoding");
    private static readonly PdfName WinAnsiName = PdfName.FromLatin1("WinAnsiEncoding");
    private static readonly PdfName FontDescriptorName = PdfName.FromLatin1("FontDescriptor");
    private static readonly PdfName FontFile2Name = PdfName.FromLatin1("FontFile2");
    private static readonly PdfName Length1Name = PdfName.FromLatin1("Length1");
    private static readonly PdfName FontBBoxName = PdfName.FromLatin1("FontBBox");
    private static readonly PdfName FlagsName = PdfName.FromLatin1("Flags");
    private static readonly PdfName ItalicAngleName = PdfName.FromLatin1("ItalicAngle");
    private static readonly PdfName AscentName = PdfName.FromLatin1("Ascent");
    private static readonly PdfName DescentName = PdfName.FromLatin1("Descent");
    private static readonly PdfName CapHeightName = PdfName.FromLatin1("CapHeight");
    private static readonly PdfName StemVName = PdfName.FromLatin1("StemV");
    private static readonly PdfName FontCategoryName = PdfName.FromLatin1("Font");

    public static PdfExternalFontResourceProjection Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(program);

        if (face.EmbeddingRights != PdfExternalFontEmbeddingRights.Allowed)
        {
            throw new NotSupportedException(
                "BF-1054J full embedding requires provider embedding rights to be Allowed.");
        }

        PdfExternalTrueTypeMetrics metrics =
            PdfExternalTrueTypeInspector.Inspect(
                program);

        if (metrics.EmbeddingRights !=
            PdfExternalFontEmbeddingRights.Allowed)
        {
            throw new NotSupportedException(
                "BF-1054J full embedding requires the sanitized font program itself to permit embedding.");
        }

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();

        PdfObjectIdentifier programIdentifier =
            allocator.ReserveNext();
        PdfObjectIdentifier descriptorIdentifier =
            allocator.ReserveNext();
        PdfObjectIdentifier fontIdentifier =
            allocator.ReserveNext();

        string baseFont =
            NormalizeBaseFontName(
                face.FaceName);

        ReadOnlyMemory<byte> fontBytes = program.Bytes;
        PdfStream fontProgram =
            new(
                new PdfDictionary
                {
                    {
                        Length1Name,
                        new PdfInteger(
                            fontBytes.Length)
                    }
                },
                fontBytes.Span);

        PdfDictionary descriptor =
            CreateDescriptor(
                baseFont,
                face,
                metrics,
                programIdentifier);

        PdfDictionary font =
            CreateFontDictionary(
                baseFont,
                metrics,
                descriptorIdentifier);

        PdfPageResourceMutationProjection resourceMutation =
            PdfPageResourceMutationProjector.Create(
                writePlan,
                pageIdentifier,
                FontCategoryName,
                resourceName,
                new PdfIndirectReference(
                    fontIdentifier));

        return new PdfExternalFontResourceProjection(
            resourceMutation.ResourceName,
            fontIdentifier,
            descriptorIdentifier,
            programIdentifier,
            null,
            pageIdentifier,
            new PdfObjectWriteProjectionSet(
            [
                resourceMutation.PageProjection,
                new PdfObjectWriteProjection(
                    programIdentifier,
                    fontProgram),
                new PdfObjectWriteProjection(
                    descriptorIdentifier,
                    descriptor),
                new PdfObjectWriteProjection(
                    fontIdentifier,
                    font)
            ]));
    }

    private static PdfDictionary CreateFontDictionary(
        string baseFont,
        PdfExternalTrueTypeMetrics metrics,
        PdfObjectIdentifier descriptorIdentifier)
    {
        var widths = new PdfArray(
            metrics.WinAnsiWidths.Select(
                static width =>
                    (PdfObject)new PdfInteger(width)));

        return new PdfDictionary
        {
            { TypeName, FontName },
            { SubtypeName, TrueTypeName },
            { BaseFontName, PdfName.FromLatin1(baseFont) },
            { FirstCharName, new PdfInteger(32) },
            { LastCharName, new PdfInteger(255) },
            { WidthsName, widths },
            { EncodingName, WinAnsiName },
            {
                FontDescriptorName,
                new PdfIndirectReference(
                    descriptorIdentifier)
            }
        };
    }

    private static PdfDictionary CreateDescriptor(
        string baseFont,
        PdfExternalFontFace face,
        PdfExternalTrueTypeMetrics metrics,
        PdfObjectIdentifier programIdentifier)
    {
        int flags =
            32 |
            (face.Slant == PdfExternalFontSlant.Upright
                ? 0
                : 64);

        double italicAngle =
            face.Slant == PdfExternalFontSlant.Upright
                ? 0
                : -12;

        int stemV =
            face.Weight >= 700
                ? 120
                : 80;

        return new PdfDictionary
        {
            { TypeName, PdfName.FromLatin1("FontDescriptor") },
            { PdfName.FromLatin1("FontName"), PdfName.FromLatin1(baseFont) },
            { FlagsName, new PdfInteger(flags) },
            {
                FontBBoxName,
                new PdfArray(
                [
                    new PdfInteger(metrics.XMin),
                    new PdfInteger(metrics.YMin),
                    new PdfInteger(metrics.XMax),
                    new PdfInteger(metrics.YMax)
                ])
            },
            { ItalicAngleName, new PdfReal(italicAngle) },
            { AscentName, new PdfInteger(metrics.Ascent) },
            { DescentName, new PdfInteger(metrics.Descent) },
            { CapHeightName, new PdfInteger(metrics.Ascent) },
            { StemVName, new PdfInteger(stemV) },
            {
                FontFile2Name,
                new PdfIndirectReference(
                    programIdentifier)
            }
        };
    }

    private static string NormalizeBaseFontName(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder =
            new StringBuilder(
                value.Length);

        foreach (char character in value)
        {
            if (character is >= 'A' and <= 'Z' or
                >= 'a' and <= 'z' or
                >= '0' and <= '9' or '-' or '_')
            {
                _ = builder.Append(
                    character);
            }
            else if (char.IsWhiteSpace(character))
            {
                _ = builder.Append('-');
            }
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException(
                "The selected external font face has no PDF-safe base font name.");
        }

        return builder.ToString();
    }
}
