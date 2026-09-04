using System.Text;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Scopes one existing PDF font resource around a target text operation and
/// restores the previously active font immediately afterward.
/// </summary>
internal static class PdfFontSwitchTokenEditor
{
    public static byte[] Wrap(
        ReadOnlySpan<byte> streamData,
        long operationStartOffset,
        long operationEndOffset,
        string selectedResourceName,
        string originalResourceName,
        double fontSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedResourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalResourceName);

        if (!double.IsFinite(fontSize) ||
            fontSize == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize),
                fontSize,
                "Font switching requires a finite non-zero font size.");
        }

        if (operationStartOffset < 0 ||
            operationEndOffset < operationStartOffset ||
            operationEndOffset > streamData.Length)
        {
            throw new InvalidOperationException(
                "The font-switch source span lies outside the current content-stream data.");
        }

        byte[] prefix =
            BuildTf(
                selectedResourceName,
                fontSize);

        byte[] suffix =
            CreateRestorationTf(
                originalResourceName,
                fontSize);

        int start =
            checked((int)operationStartOffset);
        int end =
            checked((int)operationEndOffset);

        byte[] result =
            new byte[
                checked(
                    streamData.Length +
                    prefix.Length +
                    suffix.Length)];

        streamData[..start].CopyTo(result);
        prefix.CopyTo(result.AsSpan(start));

        int targetDestination =
            start + prefix.Length;

        streamData[start..end]
            .CopyTo(
                result.AsSpan(
                    targetDestination));

        int suffixDestination =
            targetDestination +
            (end - start);

        suffix.CopyTo(
            result.AsSpan(
                suffixDestination));

        streamData[end..]
            .CopyTo(
                result.AsSpan(
                    suffixDestination +
                    suffix.Length));

        return result;
    }

    private static byte[] CreateRestorationTf(
        string resourceName,
        double fontSize)
    {
        byte[] operation =
            BuildTf(
                resourceName,
                fontSize);

        var result = new byte[operation.Length];
        result[0] = (byte)'\n';
        operation.AsSpan(0, operation.Length - 1)
            .CopyTo(result.AsSpan(1));

        return result;
    }

    private static byte[] BuildTf(
        string resourceName,
        double fontSize)
    {
        using var destination =
            new MemoryStream();

        PdfObjectSerializer.Write(
            destination,
            PdfName.FromLatin1(
                resourceName));

        destination.WriteByte(
            (byte)' ');

        if (fontSize == Math.Truncate(fontSize) &&
            fontSize >= long.MinValue &&
            fontSize <= long.MaxValue)
        {
            PdfObjectSerializer.Write(
                destination,
                new PdfInteger(
                    checked(
                        (long)fontSize)));
        }
        else
        {
            PdfObjectSerializer.Write(
                destination,
                new PdfReal(
                    fontSize));
        }

        destination.Write(
            Encoding.ASCII.GetBytes(
                " Tf\n"));

        return destination.ToArray();
    }
}
