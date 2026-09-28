using System.Text;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Text;

/// <summary>Preserves nesting when a decorated run's tag begins inside BT/ET.</summary>
internal static class PdfExistingTextDecorationMarkedContent
{
    internal static byte[] PreserveNesting(
        byte[] styled,
        int originalLength,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity,
        double styledAdvance,
        CancellationToken cancellationToken)
    {
        HashSet<int> boundaries = FindBoundaries(source.Operations, identity.OperationIndex, cancellationToken);
        if (boundaries.Count == 0) return styled;

        var runs = source.DisplayList.OfType<PdfDisplayTextRun>()
            .ToDictionary(static run => run.SourceSpan);
        var insertions = new List<(int Offset, byte[] Bytes)>();
        var saved = new Stack<(string? Font, double Size, double Scale, double Leading)>();
        PdfMatrix text = PdfMatrix.Identity;
        PdfMatrix line = PdfMatrix.Identity;
        string? font = null;
        double size = 0, scale = 100, leading = 0;
        int last = boundaries.Max();

        // Replay only matrix positioning and the font/scale needed to restore
        // it. Glyph advances come from interpreted runs and the existing width
        // service, never guessed from bytes. Each intervening run is measured once.
        for (int index = 0; index <= last; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PdfGraphicsOperation operation = source.Operations[index];
            IReadOnlyList<G.PdfObject> values = operation.Operands;
            switch (operation.Operator.Code)
            {
                case "q": saved.Push((font, size, scale, leading)); break;
                case "Q":
                    if (!saved.TryPop(out var state)) throw new InvalidOperationException("Unbalanced decoration source state.");
                    (font, size, scale, leading) = state;
                    break;
                case "BT": text = line = PdfMatrix.Identity; break;
                case "Tf": font = ((G.PdfName)values[0]).Value; size = Number(values[1]); break;
                case "Tz": scale = Number(values[0]); break;
                case "TL": leading = Number(values[0]); break;
                case "Tm":
                    text = line = new PdfMatrix(Number(values[0]), Number(values[1]), Number(values[2]),
                        Number(values[3]), Number(values[4]), Number(values[5]));
                    break;
                case "TD": leading = -Number(values[1]); goto case "Td";
                case "Td":
                    text = line = new PdfMatrix(1, 0, 0, 1, Number(values[0]), Number(values[1])).Multiply(line);
                    break;
                case "T*": text = line = new PdfMatrix(1, 0, 0, 1, 0, -leading).Multiply(line); break;
                case "'": case "\"":
                    text = line = new PdfMatrix(1, 0, 0, 1, 0, -leading).Multiply(line);
                    goto case "Tj";
                case "Tj": case "TJ":
                    if (!runs.TryGetValue(operation.SourceSpan, out PdfDisplayTextRun? run))
                        throw new InvalidOperationException("Decoration marked-content positioning requires complete run provenance.");
                    double advance = index == identity.OperationIndex ? styledAdvance
                        : PdfTextAdvanceMeasurementService.MeasureOriginal(run, cancellationToken);
                    text = new PdfMatrix(1, 0, 0, 1, advance, 0).Multiply(text);
                    break;
            }

            if (!boundaries.Contains(index)) continue;
            PdfContentSourceSpan span = operation.SourceSpan;
            if (span.StreamIndex != identity.StreamIndex)
                throw new NotSupportedException("Decoration marked-content boundaries must belong to the target stream.");
            int shift = span.StartOffset >= identity.EndOffset ? styled.Length - originalLength : 0;
            int start = checked((int)span.StartOffset + shift);
            int end = checked((int)span.EndOffset + shift);
            byte[] keyword = Encoding.ASCII.GetBytes(operation.Operator.Code);
            if (start < 0 || end > styled.Length || end - start < keyword.Length ||
                !styled.AsSpan(end - keyword.Length, keyword.Length).SequenceEqual(keyword))
                throw new InvalidOperationException("The decoration marked-content boundary provenance is stale.");

            using var restart = new MemoryStream();
            PdfExistingTextStyleTokenEditor.WriteContinuation(restart, font,
                new PdfGraphicsStateSnapshot { TextMatrix = text, TextLineMatrix = line, FontSize = size, HorizontalScaling = scale }, 0);
            insertions.Add((start, "ET\n"u8.ToArray()));
            insertions.Add((end, restart.ToArray()));
        }

        int length = styled.Length;
        foreach (var insertion in insertions) length = checked(length + insertion.Bytes.Length);
        byte[] result = new byte[length];
        int copied = 0, destination = 0;
        foreach (var insertion in insertions)
        {
            int count = insertion.Offset - copied;
            styled.AsSpan(copied, count).CopyTo(result.AsSpan(destination));
            destination += count;
            insertion.Bytes.CopyTo(result.AsSpan(destination));
            destination += insertion.Bytes.Length;
            copied = insertion.Offset;
        }
        styled.AsSpan(copied).CopyTo(result.AsSpan(destination));
        return result;
    }

    private static HashSet<int> FindBoundaries(
        IReadOnlyList<PdfGraphicsOperation> operations, int target, CancellationToken cancellationToken)
    {
        var active = new Stack<int>();
        var boundaries = new HashSet<int>();
        int begin = -1;
        int selected = 0;
        for (int index = 0; index < operations.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (operations[index].Operator.Code)
            {
                case "BT": begin = index; break;
                case "BMC": case "BDC": active.Push(index); break;
                case "EMC":
                    if (!active.TryPop(out int start)) throw new InvalidOperationException("Unbalanced marked-content provenance.");
                    if (boundaries.Contains(start)) boundaries.Add(index);
                    break;
            }
            if (index == target)
            {
                foreach (int start in active)
                    if (start > begin) boundaries.Add(start);
                selected = boundaries.Count;
            }
            if (index > target && operations[index].Operator.Code == "ET") break;
        }
        if (boundaries.Count != selected * 2)
            throw new InvalidOperationException("Decoration requires balanced marked-content boundaries within its text object.");
        return boundaries;
    }

    private static double Number(G.PdfObject value) => value switch
    {
        G.PdfInteger integer => integer.Value,
        G.PdfReal real => real.Value,
        _ => throw new InvalidOperationException("Invalid decoration text-position operand.")
    };
}
