using System.Globalization;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Represents a declared PDF specification version.
/// </summary>
public readonly record struct PdfVersion :
    IComparable<PdfVersion>
{
    public PdfVersion(
        int major,
        int minor)
    {
        if (major is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(major),
                major,
                "The PDF major version must be between 1 and 9.");
        }

        if (minor is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minor),
                minor,
                "The PDF minor version must be between 0 and 9.");
        }

        Major = major;
        Minor = minor;
    }

    public int Major { get; }

    public int Minor { get; }

    public static PdfVersion Pdf10 { get; } =
        new(1, 0);

    public static PdfVersion Pdf14 { get; } =
        new(1, 4);

    public static PdfVersion Pdf17 { get; } =
        new(1, 7);

    public static PdfVersion Pdf20 { get; } =
        new(2, 0);

    public int CompareTo(PdfVersion other)
    {
        var majorComparison =
            Major.CompareTo(other.Major);

        return majorComparison != 0
            ? majorComparison
            : Minor.CompareTo(other.Minor);
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Major}.{Minor}");
    }

    public static bool operator <(
        PdfVersion left,
        PdfVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(
        PdfVersion left,
        PdfVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(
        PdfVersion left,
        PdfVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(
        PdfVersion left,
        PdfVersion right)
    {
        return left.CompareTo(right) >= 0;
    }
}