using System.Text;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Recognizes only a provenance-selected name/Do pair. Prefix inspection at
/// discovery excludes strings, comments and composite operands; it never chooses
/// a different target. Mutation relies on the captured stream data revision.
/// </summary>
internal static class PdfFormPlacementSyntax
{
    internal static void Validate(ReadOnlySpan<byte> bytes, PdfFormPlacementDescriptor descriptor)
    {
        int start = (int)descriptor.StartOffset;
        int end = (int)descriptor.EndOffset;
        ValidatePrefix(bytes[..start]);
        ReadOnlySpan<byte> target = bytes[start..end];
        if (target.IsEmpty || target[0] != '/' ||
            (end < bytes.Length && !IsDelimiter(bytes[end])))
        {
            throw Invalid();
        }

        int index = 1;
        int nameIndex = 0;
        while (index < target.Length && !IsDelimiter(target[index]))
        {
            byte value = target[index++];
            if (value == '#')
            {
                if (target.Length - index < 2)
                {
                    throw Invalid();
                }
                value = (byte)((Hex(target[index]) << 4) | Hex(target[index + 1]));
                index += 2;
            }
            if (nameIndex >= descriptor.ResourceName.Length ||
                descriptor.ResourceName[nameIndex++] != value)
            {
                throw Invalid();
            }
        }
        if (nameIndex != descriptor.ResourceName.Length || nameIndex == 0)
        {
            throw Invalid();
        }
        SkipTrivia(target, ref index);
        if (!target[index..].SequenceEqual("Do"u8))
        {
            throw Invalid();
        }
    }

    private static void ValidatePrefix(ReadOnlySpan<byte> prefix)
    {
        int index = 0;
        int compositeDepth = 0;
        bool pendingOperand = false;
        while (index < prefix.Length)
        {
            SkipTrivia(prefix, ref index);
            if (index == prefix.Length)
            {
                break;
            }
            byte value = prefix[index];
            if (value == '(')
            {
                index = PdfTextStringTokenCodec.FindLiteralStringEnd(prefix, index);
                pendingOperand = true;
                continue;
            }
            if (value == '<' && (index + 1 == prefix.Length || prefix[index + 1] != '<'))
            {
                index = PdfTextStringTokenCodec.FindHexadecimalStringEnd(prefix, index);
                pendingOperand = true;
                continue;
            }
            if (value is (byte)'[' or (byte)'<' or (byte)']' or (byte)'>')
            {
                bool opening = value is (byte)'[' or (byte)'<';
                compositeDepth += opening ? 1 : -1;
                if (compositeDepth < 0 || (value is (byte)'<' or (byte)'>' &&
                    (index + 1 == prefix.Length || prefix[index + 1] != value)))
                {
                    throw Invalid();
                }
                index += value is (byte)'<' or (byte)'>' ? 2 : 1;
                pendingOperand = true;
                continue;
            }
            bool name = value == '/';
            int tokenStart = index++;
            while (index < prefix.Length && !IsDelimiter(prefix[index]))
            {
                index++;
            }
            if (compositeDepth != 0 || name || value is >= (byte)'0' and <= (byte)'9' or (byte)'+' or (byte)'-' or (byte)'.')
            {
                pendingOperand = true;
                continue;
            }
            ReadOnlySpan<byte> token = prefix[tokenStart..index];
            if (token.SequenceEqual("BI"u8))
            {
                // Services has no Parsing dependency. Guessing an EI boundary
                // in binary image data would make forged placement spans unsafe.
                throw new NotSupportedException("Form placement capture after inline image data is not supported.");
            }
            if (!PdfGraphicsOperator.TryGet(Encoding.ASCII.GetString(token), out _))
            {
                throw Invalid();
            }
            pendingOperand = false;
        }
        if (compositeDepth != 0 || pendingOperand)
        {
            throw Invalid();
        }
    }

    private static void SkipTrivia(ReadOnlySpan<byte> bytes, ref int index)
    {
        while (index < bytes.Length)
        {
            if (IsWhiteSpace(bytes[index]))
            {
                index++;
            }
            else if (bytes[index] == '%')
            {
                index = PdfTextStringTokenCodec.SkipComment(bytes, index);
                if (index == bytes.Length)
                {
                    throw Invalid();
                }
            }
            else
            {
                break;
            }
        }
    }

    private static bool IsWhiteSpace(byte value) => value is 0 or 9 or 10 or 12 or 13 or 32;
    private static bool IsDelimiter(byte value) => IsWhiteSpace(value) ||
        value is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';
    private static int Hex(byte value) => value switch
    {
        >= (byte)'0' and <= (byte)'9' => value - '0',
        >= (byte)'A' and <= (byte)'F' => value - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => value - 'a' + 10,
        _ => throw Invalid()
    };
    private static InvalidOperationException Invalid() =>
        new("The recorded Form span is not a canonical name/Do placement in the current content.");
}
