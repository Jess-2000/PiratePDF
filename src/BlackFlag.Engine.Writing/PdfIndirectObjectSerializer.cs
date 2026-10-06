using System.Globalization;
using System.Text;

using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Serializes one indirect PDF object with its object header and terminator.
/// </summary>
public static class PdfIndirectObjectSerializer
{
    /// <summary>Serializes an indirect object to a new byte array.</summary>
    public static byte[] Serialize(
        PdfObjectIdentifier identifier,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var destination = new MemoryStream();
        Write(
            destination,
            identifier,
            value);
        return destination.ToArray();
    }

    /// <summary>Writes an indirect object to the supplied destination.</summary>
    public static void Write(
        Stream destination,
        PdfObjectIdentifier identifier,
        PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(value);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The indirect-object destination must be writable.",
                nameof(destination));
        }

        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{identifier.ObjectNumber} {identifier.GenerationNumber} obj\n"));
        PdfObjectSerializer.Write(destination, value);
        WriteAscii(destination, "\nendobj\n");
    }

    private static void WriteAscii(
        Stream destination,
        string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Write(bytes);
    }
}
