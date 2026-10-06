using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>Checks the enclosing context before locally splitting a text object.</summary>
internal static class PdfExistingTextDecorationContext
{
    internal static void Validate(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity,
        ReadOnlySpan<byte> decoded,
        CancellationToken cancellationToken)
    {
        int begin = -1;
        int end = -1;
        bool pendingPath = false;
        for (int index = 0; index < source.Operations.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PdfGraphicsOperation operation = source.Operations[index];
            switch (operation.Operator.Code)
            {
                case "m": case "l": case "c": case "v": case "y": case "h": case "re":
                    pendingPath = true;
                    break;
                case "S": case "s": case "f": case "F": case "f*":
                case "B": case "B*": case "b": case "b*": case "n":
                    pendingPath = false;
                    break;
                case "BT":
                    if (begin >= 0)
                        throw new InvalidOperationException("Decoration source contains nested text objects.");
                    begin = index;
                    break;
                case "ET":
                    if (index > identity.OperationIndex)
                        end = index;
                    else
                        begin = -1;
                    break;
            }
            if (index == identity.OperationIndex && pendingPath)
                throw new NotSupportedException("Decoration cannot consume a surrounding unfinished graphics path.");
            if (end >= 0) break;
        }

        if (begin < 0 || begin >= identity.OperationIndex || end < 0)
            throw new InvalidOperationException("Decoration requires the complete enclosing text-object provenance.");

        PdfContentSourceSpan first = source.Operations[begin].SourceSpan;
        PdfContentSourceSpan last = source.Operations[end].SourceSpan;
        ValidateBoundary(first, identity.StreamIndex, decoded, "BT"u8);
        ValidateBoundary(last, identity.StreamIndex, decoded, "ET"u8);

        // Splitting clipping text changes the union accumulated until ET into
        // successive clip intersections. No such typography policy is defined.
        foreach (PdfDisplayItem item in source.DisplayList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item is PdfDisplayTextRun run && run.SourceSpan.StreamIndex == identity.StreamIndex &&
                run.SourceSpan.StartOffset > first.StartOffset && run.SourceSpan.EndOffset < last.EndOffset &&
                run.GraphicsState.TextRenderingMode >= 4)
            {
                throw new NotSupportedException("Decoration cannot split a text object that accumulates text clipping.");
            }
        }
    }

    private static void ValidateBoundary(
        PdfContentSourceSpan span,
        int streamIndex,
        ReadOnlySpan<byte> decoded,
        ReadOnlySpan<byte> expected)
    {
        if (span.StreamIndex != streamIndex)
            throw new NotSupportedException("Decoration requires its text-object boundaries in the target stream.");
        if (span.StartOffset < 0 || span.EndOffset > decoded.Length ||
            span.EndOffset - span.StartOffset != expected.Length ||
            !decoded.Slice((int)span.StartOffset, expected.Length).SequenceEqual(expected))
        {
            throw new InvalidOperationException("The decoration text-object boundary provenance is stale.");
        }
    }
}
