using System.Buffers.Binary;

namespace BlackFlag.Engine.Fonts.Embedding;

internal static class PdfTrueTypeCompositeClosure
{
    public static void EnsureUsableRootGlyph(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        int glyph)
    {
        uint start = offsets[glyph];
        uint end = offsets[glyph + 1];

        if (start == end)
        {
            throw new NotSupportedException(
                $"Selected glyph {glyph} has no outline data.");
        }

        int length =
            checked((int)(end - start));

        if (length < 10)
        {
            throw new InvalidDataException(
                $"Selected glyph {glyph} is truncated.");
        }

        _ = glyf.Slice(
            checked((int)start),
            length);
    }

    public static void Expand(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        int glyphCount,
        HashSet<int> selectedGlyphs,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken)
    {
        var visitState =
            new Dictionary<int, GlyphVisitState>();

        int[] roots = [.. selectedGlyphs];

        for (int index = 0; index < roots.Length; index++)
        {
            Visit(
                glyf,
                offsets,
                glyphCount,
                roots[index],
                0,
                selectedGlyphs,
                visitState,
                options,
                cancellationToken);
        }
    }

    private static void Visit(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        int glyphCount,
        int glyph,
        int depth,
        HashSet<int> selectedGlyphs,
        Dictionary<int, GlyphVisitState> visitState,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (depth > options.MaximumCompositeDepth)
        {
            throw new InvalidDataException(
                $"Composite glyph dependency depth exceeded the configured limit of {options.MaximumCompositeDepth}.");
        }

        if (glyph < 0 || glyph >= glyphCount)
        {
            throw new InvalidDataException(
                $"Composite glyph reference {glyph} lies outside the font glyph range.");
        }

        if (visitState.TryGetValue(glyph, out GlyphVisitState state))
        {
            if (state == GlyphVisitState.Visiting)
            {
                throw new InvalidDataException(
                    $"Composite glyph dependency cycle detected at glyph {glyph}.");
            }

            if (state == GlyphVisitState.Visited)
            {
                return;
            }
        }

        visitState[glyph] = GlyphVisitState.Visiting;

        if (selectedGlyphs.Add(glyph) &&
            selectedGlyphs.Count > options.MaximumRetainedGlyphs)
        {
            throw new InvalidDataException(
                $"TrueType subset dependency closure exceeded the configured {options.MaximumRetainedGlyphs}-glyph limit.");
        }

        ReadOnlySpan<byte> data =
            GetGlyphData(
                glyf,
                offsets,
                glyph);

        if (data.IsEmpty)
        {
            visitState[glyph] = GlyphVisitState.Visited;
            return;
        }

        if (data.Length < 10)
        {
            throw new InvalidDataException(
                $"Glyph {glyph} is truncated.");
        }

        short contours =
            unchecked(
                (short)BinaryPrimitives.ReadUInt16BigEndian(
                    data[..2]));

        if (contours >= 0)
        {
            visitState[glyph] = GlyphVisitState.Visited;
            return;
        }

        VisitCompositeComponents(
            glyf,
            offsets,
            glyphCount,
            glyph,
            depth,
            data,
            selectedGlyphs,
            visitState,
            options,
            cancellationToken);

        visitState[glyph] = GlyphVisitState.Visited;
    }

    private static void VisitCompositeComponents(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        int glyphCount,
        int glyph,
        int depth,
        ReadOnlySpan<byte> data,
        HashSet<int> selectedGlyphs,
        Dictionary<int, GlyphVisitState> visitState,
        PdfTrueTypeSubsetOptions options,
        CancellationToken cancellationToken)
    {
        int offset = 10;
        int componentCount = 0;
        ushort flags;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            componentCount++;

            if (componentCount > options.MaximumComponentsPerGlyph)
            {
                throw new InvalidDataException(
                    $"Composite glyph {glyph} exceeds the configured {options.MaximumComponentsPerGlyph}-component limit.");
            }

            if (offset > data.Length - 4)
            {
                throw new InvalidDataException(
                    $"Composite glyph {glyph} has a truncated component record.");
            }

            flags =
                BinaryPrimitives.ReadUInt16BigEndian(
                    data.Slice(offset, 2));

            int componentGlyph =
                BinaryPrimitives.ReadUInt16BigEndian(
                    data.Slice(offset + 2, 2));

            offset =
                AdvanceComponentOffset(
                    data,
                    glyph,
                    offset + 4,
                    flags);

            Visit(
                glyf,
                offsets,
                glyphCount,
                componentGlyph,
                checked(depth + 1),
                selectedGlyphs,
                visitState,
                options,
                cancellationToken);
        }
        while ((flags & 0x0020) != 0);

        ValidateInstructions(
            data,
            glyph,
            offset,
            flags);
    }

    private static int AdvanceComponentOffset(
        ReadOnlySpan<byte> data,
        int glyph,
        int offset,
        ushort flags)
    {
        offset =
            checked(
                offset +
                ((flags & 0x0001) != 0 ? 4 : 2));

        if ((flags & 0x0008) != 0)
        {
            offset = checked(offset + 2);
        }
        else if ((flags & 0x0040) != 0)
        {
            offset = checked(offset + 4);
        }
        else if ((flags & 0x0080) != 0)
        {
            offset = checked(offset + 8);
        }

        if (offset > data.Length)
        {
            throw new InvalidDataException(
                $"Composite glyph {glyph} component data exceeds its glyph boundary.");
        }

        return offset;
    }

    private static void ValidateInstructions(
        ReadOnlySpan<byte> data,
        int glyph,
        int offset,
        ushort flags)
    {
        if ((flags & 0x0100) == 0)
        {
            return;
        }

        if (offset > data.Length - 2)
        {
            throw new InvalidDataException(
                $"Composite glyph {glyph} has a truncated instruction length.");
        }

        int instructionLength =
            BinaryPrimitives.ReadUInt16BigEndian(
                data.Slice(offset, 2));

        int end =
            checked(
                offset +
                2 +
                instructionLength);

        if (end > data.Length)
        {
            throw new InvalidDataException(
                $"Composite glyph {glyph} instructions exceed its glyph boundary.");
        }
    }

    private static ReadOnlySpan<byte> GetGlyphData(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        int glyph)
    {
        uint start = offsets[glyph];
        uint end = offsets[glyph + 1];

        return start == end
            ? ReadOnlySpan<byte>.Empty
            : glyf.Slice(
                checked((int)start),
                checked((int)(end - start)));
    }

    private enum GlyphVisitState
    {
        Visiting,
        Visited
    }
}
