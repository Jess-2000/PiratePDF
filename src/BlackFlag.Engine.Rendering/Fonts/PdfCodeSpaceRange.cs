namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Represents one CMap codespace range.</summary>
public sealed class PdfCodeSpaceRange
{
    private readonly byte[] lower;
    private readonly byte[] upper;

    /// <summary>Initializes a codespace range.</summary>
    public PdfCodeSpaceRange(ReadOnlySpan<byte> lower, ReadOnlySpan<byte> upper)
    {
        if (lower.IsEmpty || lower.Length != upper.Length)
        {
            throw new ArgumentException("CMap codespace bounds must have equal nonzero lengths.");
        }

        this.lower = lower.ToArray();
        this.upper = upper.ToArray();
    }

    /// <summary>Gets the code length in bytes.</summary>
    public int Length => lower.Length;

    /// <summary>Determines whether a candidate code is contained in the range.</summary>
    public bool Contains(ReadOnlySpan<byte> candidate)
    {
        if (candidate.Length != lower.Length)
        {
            return false;
        }

        return Compare(candidate, lower) >= 0 && Compare(candidate, upper) <= 0;
    }

    private static int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        for (int index = 0; index < left.Length; index++)
        {
            int comparison = left[index].CompareTo(right[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }
}
