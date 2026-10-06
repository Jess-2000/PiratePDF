using System.Text;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>Builds a deterministic ToUnicode CMap for a WinAnsi simple font subset.</summary>
internal static class PdfToUnicodeCMapBuilder
{
    public static byte[] Build(
        IReadOnlyDictionary<byte, int> unicodeByCode)
    {
        ArgumentNullException.ThrowIfNull(unicodeByCode);

        var builder =
            new StringBuilder();

        _ = builder.AppendLine("/CIDInit /ProcSet findresource begin");
        _ = builder.AppendLine("12 dict begin");
        _ = builder.AppendLine("begincmap");
        _ = builder.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def");
        _ = builder.AppendLine("/CMapName /PiratePDF-ExternalFont-UCS def");
        _ = builder.AppendLine("/CMapType 2 def");
        _ = builder.AppendLine("1 begincodespacerange");
        _ = builder.AppendLine("<00> <FF>");
        _ = builder.AppendLine("endcodespacerange");

        KeyValuePair<byte, int>[] mappings =
        [
            .. unicodeByCode.OrderBy(
                static pair => pair.Key)
        ];

        for (int offset = 0;
            offset < mappings.Length;
            offset += 100)
        {
            int count =
                Math.Min(
                    100,
                    mappings.Length - offset);

            _ = builder.Append(count)
                .AppendLine(" beginbfchar");

            for (int index = 0;
                index < count;
                index++)
            {
                KeyValuePair<byte, int> mapping =
                    mappings[offset + index];

                _ = builder.Append('<')
                    .Append(mapping.Key.ToString("X2", System.Globalization.CultureInfo.InvariantCulture))
                    .Append("> <")
                    .Append(ToUtf16BeHex(mapping.Value))
                    .AppendLine(">");
            }

            _ = builder.AppendLine("endbfchar");
        }

        _ = builder.AppendLine("endcmap");
        _ = builder.AppendLine("CMapName currentdict /CMap defineresource pop");
        _ = builder.AppendLine("end");
        _ = builder.AppendLine("end");

        return Encoding.ASCII.GetBytes(
            builder.ToString());
    }

    private static string ToUtf16BeHex(
        int unicode)
    {
        if (unicode <= 0xFFFF)
        {
            return unicode.ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        int scalar =
            unicode - 0x10000;

        int high =
            0xD800 +
            (scalar >> 10);

        int low =
            0xDC00 +
            (scalar & 0x3FF);

        return high.ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture) +
            low.ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture);
    }
}
