using System.Globalization;
using System.Text;

using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Serializes logical PDF objects into deterministic PDF object syntax.
/// </summary>
public static class PdfObjectSerializer
{
    private const int MaximumCompositeDepth = 256;

    /// <summary>Serializes a PDF object to a new byte array.</summary>
    public static byte[] Serialize(PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var destination = new MemoryStream();
        Write(destination, value);
        return destination.ToArray();
    }

    /// <summary>Writes a PDF object to the supplied destination.</summary>
    public static void Write(
        Stream destination,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(value);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The PDF object destination must be writable.",
                nameof(destination));
        }

        var activeCompositeObjects =
            new HashSet<PdfObject>(ReferenceEqualityComparer.Instance);

        WriteObject(
            destination,
            value,
            activeCompositeObjects,
            depth: 0);
    }

    private static void WriteObject(
        Stream destination,
        PdfObject value,
        HashSet<PdfObject> activeCompositeObjects,
        int depth)
    {
        switch (value)
        {
            case PdfNull:
                WriteAscii(destination, "null");
                break;
            case PdfBoolean boolean:
                WriteAscii(destination, boolean.Value ? "true" : "false");
                break;
            case PdfInteger integer:
                WriteAscii(
                    destination,
                    integer.Value.ToString(CultureInfo.InvariantCulture));
                break;
            case PdfReal real:
                WriteAscii(destination, FormatReal(real.Value));
                break;
            case PdfName name:
                WriteName(destination, name);
                break;
            case PdfString text:
                WriteString(destination, text);
                break;
            case PdfIndirectReference reference:
                WriteAscii(
                    destination,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{reference.ObjectNumber} {reference.GenerationNumber} R"));
                break;
            case PdfArray array:
                WriteComposite(
                    destination,
                    array,
                    activeCompositeObjects,
                    depth,
                    WriteArray);
                break;
            case PdfDictionary dictionary:
                WriteComposite(
                    destination,
                    dictionary,
                    activeCompositeObjects,
                    depth,
                    WriteDictionary);
                break;
            case PdfStream stream:
                WriteComposite(
                    destination,
                    stream,
                    activeCompositeObjects,
                    depth,
                    WriteStream);
                break;
            default:
                throw new NotSupportedException(
                    $"PDF object type '{value.GetType().FullName}' is not supported by the object serializer.");
        }
    }

    private static void WriteComposite<T>(
        Stream destination,
        T value,
        HashSet<PdfObject> activeCompositeObjects,
        int depth,
        Action<Stream, T, HashSet<PdfObject>, int> writer)
        where T : PdfObject
    {
        if (depth >= MaximumCompositeDepth)
        {
            throw new InvalidOperationException(
                $"PDF object nesting exceeds the supported depth of {MaximumCompositeDepth}.");
        }

        if (!activeCompositeObjects.Add(value))
        {
            throw new InvalidOperationException(
                "A direct PDF object cycle cannot be serialized. Use an indirect reference to represent cyclic document structure.");
        }

        try
        {
            writer(
                destination,
                value,
                activeCompositeObjects,
                depth + 1);
        }
        finally
        {
            activeCompositeObjects.Remove(value);
        }
    }

    private static void WriteArray(
        Stream destination,
        PdfArray array,
        HashSet<PdfObject> activeCompositeObjects,
        int depth)
    {
        destination.WriteByte((byte)'[');

        for (int index = 0; index < array.Count; index++)
        {
            if (index > 0)
            {
                destination.WriteByte((byte)' ');
            }

            WriteObject(
                destination,
                array[index],
                activeCompositeObjects,
                depth);
        }

        destination.WriteByte((byte)']');
    }

    private static void WriteDictionary(
        Stream destination,
        PdfDictionary dictionary,
        HashSet<PdfObject> activeCompositeObjects,
        int depth)
    {
        WriteAscii(destination, "<<");

        foreach (var entry in dictionary)
        {
            destination.WriteByte((byte)' ');
            WriteName(destination, entry.Key);
            destination.WriteByte((byte)' ');
            WriteObject(
                destination,
                entry.Value,
                activeCompositeObjects,
                depth);
        }

        if (dictionary.Count > 0)
        {
            destination.WriteByte((byte)' ');
        }

        WriteAscii(destination, ">>");
    }

    private static void WriteStream(
        Stream destination,
        PdfStream stream,
        HashSet<PdfObject> activeCompositeObjects,
        int depth)
    {
        byte[] data = stream.ToByteArray();
        PdfDictionary dictionary = stream.Dictionary.CreateCopy();
        dictionary.Set(
            PdfNames.Length,
            new PdfInteger(data.Length));

        WriteObject(
            destination,
            dictionary,
            activeCompositeObjects,
            depth);
        WriteAscii(destination, "\nstream\n");
        destination.Write(data);
        WriteAscii(destination, "\nendstream");
    }

    private static void WriteName(
        Stream destination,
        PdfName name)
    {
        destination.WriteByte((byte)'/');

        foreach (byte value in name.ToByteArray())
        {
            if (IsRegularNameByte(value))
            {
                destination.WriteByte(value);
                continue;
            }

            destination.WriteByte((byte)'#');
            WriteHexByte(destination, value);
        }
    }

    private static bool IsRegularNameByte(byte value)
    {
        return value is >= 33 and <= 126 &&
            value != (byte)'(' && value != (byte)')' &&
            value != (byte)'<' && value != (byte)'>' &&
            value != (byte)'[' && value != (byte)']' &&
            value != (byte)'{' && value != (byte)'}' &&
            value != (byte)'/' && value != (byte)'%' &&
            value != (byte)'#';
    }

    private static void WriteString(
        Stream destination,
        PdfString value)
    {
        if (value.Format == PdfStringFormat.Hexadecimal)
        {
            destination.WriteByte((byte)'<');

            foreach (byte item in value.ToByteArray())
            {
                WriteHexByte(destination, item);
            }

            destination.WriteByte((byte)'>');
            return;
        }

        destination.WriteByte((byte)'(');

        foreach (byte item in value.ToByteArray())
        {
            switch (item)
            {
                case (byte)'\n':
                    WriteAscii(destination, "\\n");
                    break;
                case (byte)'\r':
                    WriteAscii(destination, "\\r");
                    break;
                case (byte)'\t':
                    WriteAscii(destination, "\\t");
                    break;
                case 8:
                    WriteAscii(destination, "\\b");
                    break;
                case 12:
                    WriteAscii(destination, "\\f");
                    break;
                case (byte)'(':
                case (byte)')':
                case (byte)'\\':
                    destination.WriteByte((byte)'\\');
                    destination.WriteByte(item);
                    break;
                default:
                    if (item is >= 32 and <= 126)
                    {
                        destination.WriteByte(item);
                    }
                    else
                    {
                        WriteOctalByte(destination, item);
                    }

                    break;
            }
        }

        destination.WriteByte((byte)')');
    }

    private static string FormatReal(double value)
    {
        if (value == 0)
        {
            return "0.0";
        }

        string roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
        int exponentMarker = roundTrip.IndexOfAny(['E', 'e']);

        if (exponentMarker < 0)
        {
            return roundTrip.Contains('.')
                ? roundTrip
                : string.Concat(roundTrip, ".0");
        }

        string mantissa = roundTrip[..exponentMarker];
        int exponent = int.Parse(
            roundTrip[(exponentMarker + 1)..],
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);

        bool negative = mantissa.Length > 0 && mantissa[0] == '-';
        if (negative)
        {
            mantissa = mantissa[1..];
        }

        int decimalPoint = mantissa.IndexOf('.');
        int originalIntegerDigits = decimalPoint < 0
            ? mantissa.Length
            : decimalPoint;
        string digits = mantissa.Replace(".", string.Empty, StringComparison.Ordinal);
        int decimalPosition = originalIntegerDigits + exponent;

        string expanded = decimalPosition switch
        {
            <= 0 => string.Concat(
                "0.",
                new string('0', -decimalPosition),
                digits),
            _ when decimalPosition >= digits.Length => string.Concat(
                digits,
                new string('0', decimalPosition - digits.Length),
                ".0"),
            _ => string.Concat(
                digits[..decimalPosition],
                ".",
                digits[decimalPosition..])
        };

        return negative
            ? string.Concat("-", expanded)
            : expanded;
    }

    private static void WriteHexByte(
        Stream destination,
        byte value)
    {
        const string Hex = "0123456789ABCDEF";
        destination.WriteByte((byte)Hex[value >> 4]);
        destination.WriteByte((byte)Hex[value & 0x0F]);
    }

    private static void WriteOctalByte(
        Stream destination,
        byte value)
    {
        destination.WriteByte((byte)'\\');
        destination.WriteByte((byte)('0' + ((value >> 6) & 0x07)));
        destination.WriteByte((byte)('0' + ((value >> 3) & 0x07)));
        destination.WriteByte((byte)('0' + (value & 0x07)));
    }

    private static void WriteAscii(
        Stream destination,
        string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Write(bytes);
    }
}
