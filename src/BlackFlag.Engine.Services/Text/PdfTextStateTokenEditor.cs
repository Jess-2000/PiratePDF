using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Inserts scoped text-state operators around one Tj/TJ operation while
/// preserving the target operation and all unrelated source bytes.
/// </summary>
internal static class PdfTextStateTokenEditor
{
    public static byte[] Apply(
        ReadOnlySpan<byte> streamData,
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextTypographyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(update);

        update.Validate();

        if (!update.HasChanges)
        {
            throw new ArgumentException(
                "At least one typography value must change.",
                nameof(update));
        }

        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.EndOffset > streamData.Length)
        {
            throw new InvalidOperationException(
                "The existing-text source span lies outside the current content-stream data.");
        }

        byte[] prefix =
            BuildStateOperators(
                descriptor,
                update,
                useRequestedValues: true);

        byte[] suffix =
            CreateRestorationOperators(
                descriptor,
                update);

        int start =
            checked((int)identity.StartOffset);
        int end =
            checked((int)identity.EndOffset);

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
            .CopyTo(result.AsSpan(targetDestination));

        int suffixDestination =
            targetDestination + (end - start);

        suffix.CopyTo(result.AsSpan(suffixDestination));

        streamData[end..]
            .CopyTo(
                result.AsSpan(
                    suffixDestination +
                    suffix.Length));

        return result;
    }

    private static byte[] CreateRestorationOperators(
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextTypographyUpdate update)
    {
        byte[] operators =
            BuildStateOperators(
                descriptor,
                update,
                useRequestedValues: false);

        var result = new byte[operators.Length];
        result[0] = (byte)'\n';
        operators.AsSpan(0, operators.Length - 1)
            .CopyTo(result.AsSpan(1));

        return result;
    }

    private static byte[] BuildStateOperators(
        PdfExistingTextDescriptor descriptor,
        PdfExistingTextTypographyUpdate update,
        bool useRequestedValues)
    {
        PdfExistingTextTypography current =
            descriptor.Typography;

        using var destination =
            new MemoryStream();

        if (update.FontSize.HasValue)
        {
            string resourceName =
                descriptor.FontResourceName ??
                throw new NotSupportedException(
                    "Scoped font-size editing requires a known font resource name.");

            WriteObject(destination, PdfName.FromLatin1(resourceName));
            destination.WriteByte((byte)' ');
            WriteNumber(
                destination,
                useRequestedValues
                    ? update.FontSize.Value
                    : current.FontSize);
            WriteAscii(destination, " Tf\n");
        }

        WriteOptionalNumberOperator(
            destination,
            "Tc",
            update.CharacterSpacing,
            current.CharacterSpacing,
            useRequestedValues);

        WriteOptionalNumberOperator(
            destination,
            "Tw",
            update.WordSpacing,
            current.WordSpacing,
            useRequestedValues);

        WriteOptionalNumberOperator(
            destination,
            "Tz",
            update.HorizontalScaling,
            current.HorizontalScaling,
            useRequestedValues);

        if (update.TextRenderingMode.HasValue)
        {
            WriteAscii(
                destination,
                (useRequestedValues
                    ? update.TextRenderingMode.Value
                    : current.TextRenderingMode)
                .ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            WriteAscii(destination, " Tr\n");
        }

        WriteOptionalNumberOperator(
            destination,
            "Ts",
            update.TextRise,
            current.TextRise,
            useRequestedValues);

        return destination.ToArray();
    }

    private static void WriteOptionalNumberOperator(
        Stream destination,
        string operation,
        double? requested,
        double current,
        bool useRequestedValues)
    {
        if (!requested.HasValue)
        {
            return;
        }

        WriteNumber(
            destination,
            useRequestedValues
                ? requested.Value
                : current);

        WriteAscii(
            destination,
            $" {operation}\n");
    }

    private static void WriteNumber(
        Stream destination,
        double value)
    {
        if (value == Math.Truncate(value) &&
            value >= long.MinValue &&
            value <= long.MaxValue)
        {
            WriteObject(
                destination,
                new PdfInteger(
                    checked((long)value)));
            return;
        }

        WriteObject(
            destination,
            new PdfReal(value));
    }

    private static void WriteObject(
        Stream destination,
        PdfObject value) =>
        PdfObjectSerializer.Write(
            destination,
            value);

    private static void WriteAscii(
        Stream destination,
        string value) =>
        destination.Write(
            Encoding.ASCII.GetBytes(
                value));
}
