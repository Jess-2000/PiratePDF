using System.Text;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Writing;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Converts one single-string text-showing target into a TJ array carrying a
/// trailing compensation adjustment so following text starts at the original
/// horizontal position.
/// </summary>
internal static class PdfTextAdvanceCompensationTokenEditor
{
    public static byte[] Apply(
        ReadOnlySpan<byte> streamData,
        PdfExistingTextIdentity identity,
        PdfGraphicsOperation operation,
        double advanceDelta,
        double fontSize,
        double horizontalScaling)
    {
        if (operation.Operator.Code != "Tj")
        {
            throw new NotSupportedException(
                "BF-1054T local advance compensation currently requires a primitive Tj target.");
        }

        if (!double.IsFinite(advanceDelta) ||
            !double.IsFinite(fontSize) ||
            !double.IsFinite(horizontalScaling) ||
            fontSize == 0 ||
            horizontalScaling == 0)
        {
            throw new InvalidOperationException(
                "The text state cannot represent a stable local advance compensation.");
        }

        double scale =
            horizontalScaling / 100.0;

        double adjustment =
            advanceDelta *
            1000.0 /
            (fontSize * scale);

        if (!double.IsFinite(adjustment))
        {
            throw new InvalidOperationException(
                "The calculated TJ compensation is not finite.");
        }

        int start =
            checked((int)identity.StartOffset);

        int end =
            checked((int)identity.EndOffset);

        if (start < 0 ||
            end < start ||
            end > streamData.Length)
        {
            throw new InvalidOperationException(
                "The compensation target lies outside the decoded content stream.");
        }

        ReadOnlySpan<byte> target =
            streamData[start..end];

        int operatorStart =
            FindTrailingOperatorStart(
                target);

        int operandEnd =
            operatorStart;

        while (operandEnd > 0 &&
            IsWhiteSpace(target[operandEnd - 1]))
        {
            operandEnd--;
        }

        ReadOnlySpan<byte> operand =
            target[..operandEnd];

        if (operand.IsEmpty)
        {
            throw new InvalidOperationException(
                "The Tj target has no string operand.");
        }

        using var replacement =
            new MemoryStream();

        replacement.WriteByte((byte)'[');
        replacement.Write(operand);
        replacement.WriteByte((byte)' ');
        WriteNumber(replacement, adjustment);
        replacement.Write(
            Encoding.ASCII.GetBytes("] TJ"));

        byte[] replacementBytes =
            replacement.ToArray();

        byte[] result =
            new byte[
                checked(
                    streamData.Length -
                    (end - start) +
                    replacementBytes.Length)];

        streamData[..start].CopyTo(result);
        replacementBytes.CopyTo(
            result.AsSpan(start));
        streamData[end..].CopyTo(
            result.AsSpan(
                start +
                replacementBytes.Length));

        return result;
    }

    private static int FindTrailingOperatorStart(
        ReadOnlySpan<byte> target)
    {
        int index = target.Length - 1;

        while (index >= 0 &&
            IsWhiteSpace(target[index]))
        {
            index--;
        }

        if (index < 1 ||
            target[index] != (byte)'j' ||
            target[index - 1] != (byte)'T')
        {
            throw new InvalidOperationException(
                "The compensation target is not a canonical Tj operation.");
        }

        return index - 1;
    }

    private static bool IsWhiteSpace(byte value) =>
        value is 0 or 9 or 10 or 12 or 13 or 32;

    private static void WriteNumber(
        Stream output,
        double value)
    {
        if (value == Math.Truncate(value) &&
            value >= long.MinValue &&
            value <= long.MaxValue)
        {
            PdfObjectSerializer.Write(
                output,
                new PdfInteger(
                    checked((long)value)));
            return;
        }

        PdfObjectSerializer.Write(
            output,
            new PdfReal(value));
    }
}
