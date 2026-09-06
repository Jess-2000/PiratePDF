using System.Text;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Writing;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Services.Text;

internal static class PdfExistingTextStyleTokenEditor
{
    public static byte[] Apply(
        ReadOnlySpan<byte> streamData,
        PdfExistingTextDescriptor descriptor,
        PdfGraphicsStateSnapshot graphicsState,
        PdfExistingTextStyleUpdate update,
        long operationEndOffset)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(graphicsState);
        ArgumentNullException.ThrowIfNull(update);
        update.Validate();

        long startOffset =
            descriptor.Identity.StartOffset;

        if (startOffset < 0 ||
            operationEndOffset < startOffset ||
            operationEndOffset > streamData.Length)
        {
            throw new InvalidOperationException(
                "The existing-text style target lies outside the decoded stream.");
        }

        byte[] prefix =
            BuildPrefix(
                descriptor,
                graphicsState,
                update);

        byte[] suffix =
            BuildSuffix(
                descriptor,
                graphicsState,
                update);

        int start = checked((int)startOffset);
        int end = checked((int)operationEndOffset);

        byte[] result =
            new byte[
                checked(
                    streamData.Length +
                    prefix.Length +
                    suffix.Length)];

        streamData[..start].CopyTo(result);
        prefix.CopyTo(result.AsSpan(start));

        int target = start + prefix.Length;
        streamData[start..end].CopyTo(result.AsSpan(target));

        int suffixStart = target + (end - start);
        suffix.CopyTo(result.AsSpan(suffixStart));
        streamData[end..].CopyTo(
            result.AsSpan(suffixStart + suffix.Length));

        return result;
    }

    private static byte[] BuildPrefix(
        PdfExistingTextDescriptor descriptor,
        PdfGraphicsStateSnapshot state,
        PdfExistingTextStyleUpdate update)
    {
        using var output = new MemoryStream();

        WriteScript(
            output,
            descriptor,
            update,
            restore: false);

        if (update.FillColor is not null)
        {
            WriteColor(
                output,
                update.FillColor,
                stroking: false);
        }

        if (update.StrokeColor is not null)
        {
            WriteColor(
                output,
                update.StrokeColor,
                stroking: true);
        }

        return output.ToArray();
    }

    private static byte[] BuildSuffix(
        PdfExistingTextDescriptor descriptor,
        PdfGraphicsStateSnapshot state,
        PdfExistingTextStyleUpdate update)
    {
        using var output = new MemoryStream();
        WriteAscii(output, "\n");

        if (update.ScriptStyle.HasValue)
        {
            string resourceName =
                descriptor.FontResourceName ??
                throw new NotSupportedException(
                    "Script styling requires a known font resource name.");

            WriteName(output, resourceName);
            WriteAscii(output, " ");
            WriteNumber(output, descriptor.Typography.FontSize);
            WriteAscii(output, " Tf\n");
            WriteNumber(output, descriptor.Typography.TextRise);
            WriteAscii(output, " Ts\n");
        }

        if (update.FillColor is not null)
        {
            WriteDisplayColor(
                output,
                state.FillColor,
                stroking: false);
        }

        if (update.StrokeColor is not null)
        {
            WriteDisplayColor(
                output,
                state.StrokeColor,
                stroking: true);
        }

        if (update.Decoration is not null)
        {
            WriteDecoration(
                output,
                descriptor,
                state,
                update.Decoration);
        }

        return output.ToArray();
    }

    private static void WriteScript(
        Stream output,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextStyleUpdate update,
        bool restore)
    {
        if (!update.ScriptStyle.HasValue ||
            update.ScriptStyle.Value ==
                PdfExistingTextScriptStyle.Normal)
        {
            return;
        }

        string resourceName =
            descriptor.FontResourceName ??
            throw new NotSupportedException(
                "Superscript/subscript requires a known font resource name.");

        double baseSize =
            descriptor.Typography.FontSize;

        double scale =
            update.ScriptStyle.Value ==
                PdfExistingTextScriptStyle.Superscript
                ? update.SuperscriptScale
                : update.SubscriptScale;

        double rise =
            update.ScriptStyle.Value ==
                PdfExistingTextScriptStyle.Superscript
                ? descriptor.Typography.TextRise +
                  (baseSize * update.SuperscriptRise)
                : descriptor.Typography.TextRise -
                  (baseSize * update.SubscriptDrop);

        WriteName(output, resourceName);
        WriteAscii(output, " ");
        WriteNumber(output, baseSize * scale);
        WriteAscii(output, " Tf\n");
        WriteNumber(output, rise);
        WriteAscii(output, " Ts\n");
    }

    private static void WriteColor(
        Stream output,
        PdfExistingTextColor color,
        bool stroking)
    {
        for (int index = 0;
            index < color.Components.Count;
            index++)
        {
            if (index != 0)
            {
                WriteAscii(output, " ");
            }

            WriteNumber(
                output,
                color.Components[index]);
        }

        string operation =
            (color.ColorSpaceName, stroking) switch
            {
                ("DeviceGray", false) => " g\n",
                ("DeviceGray", true) => " G\n",
                ("DeviceRGB", false) => " rg\n",
                ("DeviceRGB", true) => " RG\n",
                ("DeviceCMYK", false) => " k\n",
                ("DeviceCMYK", true) => " K\n",
                _ => throw new NotSupportedException(
                    $"Direct color '{color.ColorSpaceName}' is not serializable.")
            };

        WriteAscii(output, operation);
    }

    private static void WriteDisplayColor(
        Stream output,
        PdfColorValue color,
        bool stroking)
    {
        if (color.Resource is not null ||
            color.Pattern is not null)
        {
            throw new NotSupportedException(
                "BF-1054S direct color restoration supports device color spaces only.");
        }

        var direct =
            color.ColorSpaceName switch
            {
                "DeviceGray" when color.Components.Count == 1 =>
                    PdfExistingTextColor.Gray(color.Components[0]),
                "DeviceRGB" when color.Components.Count == 3 =>
                    PdfExistingTextColor.Rgb(
                        color.Components[0],
                        color.Components[1],
                        color.Components[2]),
                "DeviceCMYK" when color.Components.Count == 4 =>
                    PdfExistingTextColor.Cmyk(
                        color.Components[0],
                        color.Components[1],
                        color.Components[2],
                        color.Components[3]),
                _ => throw new NotSupportedException(
                    $"Existing device color '{color.ColorSpaceName}' cannot be restored by BF-1054S.")
            };

        WriteColor(output, direct, stroking);
    }

    private static void WriteDecoration(
        Stream output,
        PdfExistingTextDescriptor descriptor,
        PdfGraphicsStateSnapshot state,
        PdfExistingTextDecoration decoration)
    {
        double fontSize =
            descriptor.Typography.FontSize;

        double offset =
            decoration.Offset ??
            (decoration.Kind ==
                PdfExistingTextDecorationKind.Underline
                ? -0.12 * fontSize
                : 0.30 * fontSize);

        double thickness =
            decoration.Thickness ??
            Math.Max(
                0.5,
                0.05 * Math.Abs(fontSize));

        WriteAscii(output, "q\n");
        WriteDisplayColor(
            output,
            state.FillColor,
            stroking: true);
        WriteNumber(output, thickness);
        WriteAscii(output, " w\n0 ");
        WriteNumber(output, offset);
        WriteAscii(output, " m\n");
        WriteNumber(output, decoration.AdvanceWidth);
        WriteAscii(output, " ");
        WriteNumber(output, offset);
        WriteAscii(output, " l\nS\nQ\n");
    }

    private static void WriteName(Stream output, string name) =>
        PdfObjectSerializer.Write(
            output,
            PdfName.FromLatin1(name));

    private static void WriteNumber(Stream output, double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == Math.Truncate(value) &&
            value >= long.MinValue &&
            value <= long.MaxValue)
        {
            PdfObjectSerializer.Write(
                output,
                new PdfInteger(
                    checked((long)value)));
        }
        else
        {
            PdfObjectSerializer.Write(
                output,
                new PdfReal(value));
        }
    }

    private static void WriteAscii(Stream output, string value) =>
        output.Write(Encoding.ASCII.GetBytes(value));
}
