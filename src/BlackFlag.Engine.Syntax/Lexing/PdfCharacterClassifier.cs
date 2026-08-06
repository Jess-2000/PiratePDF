using System.Runtime.CompilerServices;

namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Classifies bytes according to PDF lexical syntax.
/// </summary>
public static class PdfCharacterClassifier
{
    /// <summary>
    /// Determines whether a byte is PDF whitespace.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the byte is PDF whitespace; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// PDF whitespace consists of null, horizontal tab, line feed, form feed,
    /// carriage return, and space.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }

    /// <summary>
    /// Determines whether a byte is a PDF delimiter.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the byte is a PDF delimiter; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDelimiter(
        byte value)
    {
        return value is
            (byte)'(' or
            (byte)')' or
            (byte)'<' or
            (byte)'>' or
            (byte)'[' or
            (byte)']' or
            (byte)'{' or
            (byte)'}' or
            (byte)'/' or
            (byte)'%';
    }

    /// <summary>
    /// Determines whether a byte is a decimal digit.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> for an ASCII digit from zero through nine;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDecimalDigit(
        byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9';
    }

    /// <summary>
    /// Determines whether a byte is a hexadecimal digit.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> for an ASCII hexadecimal digit; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHexadecimalDigit(
        byte value)
    {
        return IsDecimalDigit(value) ||
            value is >= (byte)'A' and <= (byte)'F' ||
            value is >= (byte)'a' and <= (byte)'f';
    }

    /// <summary>
    /// Determines whether a byte terminates a regular PDF token.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the byte is whitespace or a delimiter;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTokenBoundary(
        byte value)
    {
        return IsWhiteSpace(value) ||
            IsDelimiter(value);
    }

    /// <summary>
    /// Determines whether a byte may belong to a regular PDF token.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the byte is neither whitespace nor a
    /// delimiter; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRegular(
        byte value)
    {
        return !IsTokenBoundary(value);
    }

    /// <summary>
    /// Determines whether a byte represents an end-of-line marker.
    /// </summary>
    /// <param name="value">
    /// The byte to classify.
    /// </param>
    /// <returns>
    /// <see langword="true"/> for carriage return or line feed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEndOfLine(
        byte value)
    {
        return value is
            (byte)'\r' or
            (byte)'\n';
    }

    /// <summary>
    /// Converts an ASCII hexadecimal digit to its numeric value.
    /// </summary>
    /// <param name="value">
    /// The hexadecimal digit to convert.
    /// </param>
    /// <returns>
    /// A value from zero through fifteen.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not an ASCII hexadecimal
    /// digit.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHexadecimalValue(
        byte value)
    {
        if (value is >= (byte)'0' and <= (byte)'9')
        {
            return value - (byte)'0';
        }

        if (value is >= (byte)'A' and <= (byte)'F')
        {
            return value - (byte)'A' + 10;
        }

        if (value is >= (byte)'a' and <= (byte)'f')
        {
            return value - (byte)'a' + 10;
        }

        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            "The byte is not an ASCII hexadecimal digit.");
    }
}
