using System.Text;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Writing;
using GraphicsObjects = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Converts the PDF compound quote text-showing operators to their equivalent
/// primitive text-state and Tj operations before scoped style insertion.
/// </summary>
internal static class PdfCompoundTextOperatorNormalizer
{
    public static byte[] Normalize(
        ReadOnlySpan<byte> streamData,
        PdfGraphicsOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation.Operator.Code switch
        {
            "Tj" or "TJ" =>
                streamData.ToArray(),

            "'" =>
                ReplaceOperation(
                    streamData,
                    operation,
                    BuildSingleQuote(operation)),

            "\"" =>
                ReplaceOperation(
                    streamData,
                    operation,
                    BuildDoubleQuote(operation)),

            _ =>
                throw new NotSupportedException(
                    $"Existing-text style editing does not support operator '{operation.Operator.Code}'.")
        };
    }

    private static byte[] BuildSingleQuote(
        PdfGraphicsOperation operation)
    {
        if (operation.Operands.Count != 1)
        {
            throw new InvalidDataException(
                "A canonical single-quote text operation requires one string operand.");
        }

        using var output = new MemoryStream();
        WriteAscii(output, "T*\n");
        WriteString(output, operation.Operands[0]);
        WriteAscii(output, " Tj");
        return output.ToArray();
    }

    private static byte[] BuildDoubleQuote(
        PdfGraphicsOperation operation)
    {
        if (operation.Operands.Count != 3)
        {
            throw new InvalidDataException(
                "A canonical double-quote text operation requires word spacing, character spacing, and a string.");
        }

        using var output = new MemoryStream();

        WriteNumber(output, operation.Operands[0]);
        WriteAscii(output, " Tw\n");
        WriteNumber(output, operation.Operands[1]);
        WriteAscii(output, " Tc\nT*\n");
        WriteString(output, operation.Operands[2]);
        WriteAscii(output, " Tj");

        return output.ToArray();
    }

    private static void WriteString(Stream output, GraphicsObjects.PdfObject operand)
    {
        PdfString text = operand switch
        {
            GraphicsObjects.PdfLiteralString literal =>
                PdfString.FromLatin1(literal.Value),
            GraphicsObjects.PdfHexString hexadecimal =>
                new PdfString(hexadecimal.Value.Span, PdfStringFormat.Hexadecimal),
            _ => throw new InvalidDataException(
                "A compound text-showing operation requires a string operand.")
        };

        PdfObjectSerializer.Write(output, text);
    }

    private static void WriteNumber(Stream output, GraphicsObjects.PdfObject operand)
    {
        Document.Objects.PdfObject number = operand switch
        {
            GraphicsObjects.PdfInteger integer => new PdfInteger(integer.Value),
            GraphicsObjects.PdfReal real => new PdfReal(real.Value),
            _ => throw new InvalidDataException(
                "A double-quote text operation requires numeric spacing operands.")
        };

        PdfObjectSerializer.Write(output, number);
    }

    private static byte[] ReplaceOperation(
        ReadOnlySpan<byte> source,
        PdfGraphicsOperation operation,
        ReadOnlySpan<byte> replacement)
    {
        int start = checked((int)operation.SourceSpan.StartOffset);
        int end = checked((int)operation.SourceSpan.EndOffset);

        if (start < 0 ||
            end < start ||
            end > source.Length)
        {
            throw new InvalidOperationException(
                "The compound text operation lies outside the current decoded stream.");
        }

        byte[] result =
            new byte[
                checked(
                    source.Length -
                    (end - start) +
                    replacement.Length)];

        source[..start].CopyTo(result);
        replacement.CopyTo(result.AsSpan(start));
        source[end..].CopyTo(
            result.AsSpan(start + replacement.Length));

        return result;
    }

    private static void WriteAscii(Stream stream, string value) =>
        stream.Write(Encoding.ASCII.GetBytes(value));
}
