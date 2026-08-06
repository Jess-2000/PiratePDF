using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Rendering.Graphics;

/// <summary>Describes a recognized PDF content-stream operator.</summary>
public sealed class PdfGraphicsOperator : IEquatable<PdfGraphicsOperator>
{
    private static readonly ReadOnlyDictionary<string, PdfGraphicsOperator> KnownByCode =
        new(new Dictionary<string, PdfGraphicsOperator>(StringComparer.Ordinal)
        {
            ["q"] = new("q", 0, 0), ["Q"] = new("Q", 0, 0),
            ["cm"] = new("cm", 6, 6), ["w"] = new("w", 1, 1),
            ["J"] = new("J", 1, 1), ["j"] = new("j", 1, 1),
            ["M"] = new("M", 1, 1), ["d"] = new("d", 2, 2),
            ["ri"] = new("ri", 1, 1), ["i"] = new("i", 1, 1),
            ["gs"] = new("gs", 1, 1), ["m"] = new("m", 2, 2),
            ["l"] = new("l", 2, 2), ["c"] = new("c", 6, 6),
            ["v"] = new("v", 4, 4), ["y"] = new("y", 4, 4),
            ["h"] = new("h", 0, 0), ["re"] = new("re", 4, 4),
            ["S"] = new("S", 0, 0), ["s"] = new("s", 0, 0),
            ["f"] = new("f", 0, 0), ["F"] = new("F", 0, 0),
            ["f*"] = new("f*", 0, 0), ["B"] = new("B", 0, 0),
            ["B*"] = new("B*", 0, 0), ["b"] = new("b", 0, 0),
            ["b*"] = new("b*", 0, 0), ["n"] = new("n", 0, 0),
            ["W"] = new("W", 0, 0), ["W*"] = new("W*", 0, 0),
            ["BT"] = new("BT", 0, 0), ["ET"] = new("ET", 0, 0),
            ["Tc"] = new("Tc", 1, 1), ["Tw"] = new("Tw", 1, 1),
            ["Tz"] = new("Tz", 1, 1), ["TL"] = new("TL", 1, 1),
            ["Tf"] = new("Tf", 2, 2), ["Tr"] = new("Tr", 1, 1),
            ["Ts"] = new("Ts", 1, 1), ["Td"] = new("Td", 2, 2),
            ["TD"] = new("TD", 2, 2), ["Tm"] = new("Tm", 6, 6),
            ["T*"] = new("T*", 0, 0), ["Tj"] = new("Tj", 1, 1),
            ["TJ"] = new("TJ", 1, 1), ["'"] = new("'", 1, 1),
            ["\""] = new("\"", 3, 3), ["CS"] = new("CS", 1, 1),
            ["cs"] = new("cs", 1, 1), ["SC"] = new("SC", 1, int.MaxValue),
            ["SCN"] = new("SCN", 1, int.MaxValue), ["sc"] = new("sc", 1, int.MaxValue),
            ["scn"] = new("scn", 1, int.MaxValue), ["G"] = new("G", 1, 1),
            ["g"] = new("g", 1, 1), ["RG"] = new("RG", 3, 3),
            ["rg"] = new("rg", 3, 3), ["K"] = new("K", 4, 4),
            ["k"] = new("k", 4, 4), ["Do"] = new("Do", 1, 1),
            ["MP"] = new("MP", 1, 1), ["DP"] = new("DP", 2, 2),
            ["BMC"] = new("BMC", 1, 1), ["BDC"] = new("BDC", 2, 2),
            ["EMC"] = new("EMC", 0, 0), ["BX"] = new("BX", 0, 0),
            ["EX"] = new("EX", 0, 0), ["sh"] = new("sh", 1, 1),
            ["d0"] = new("d0", 2, 2), ["d1"] = new("d1", 6, 6),
            ["BI"] = new("BI", 0, 0)
        });

    private PdfGraphicsOperator(string code, int minimumOperandCount, int maximumOperandCount)
    {
        Code = code;
        MinimumOperandCount = minimumOperandCount;
        MaximumOperandCount = maximumOperandCount;
    }

    /// <summary>Gets the exact PDF operator code.</summary>
    public string Code { get; }

    /// <summary>Gets the minimum accepted operand count.</summary>
    public int MinimumOperandCount { get; }

    /// <summary>Gets the maximum accepted operand count.</summary>
    public int MaximumOperandCount { get; }

    /// <summary>Gets all standard operators recognized by BF-1045.</summary>
    public static IReadOnlyDictionary<string, PdfGraphicsOperator> Known => KnownByCode;

    /// <summary>Tries to obtain a recognized operator.</summary>
    public static bool TryGet(string code, out PdfGraphicsOperator? value)
    {
        ArgumentNullException.ThrowIfNull(code);
        return KnownByCode.TryGetValue(code, out value);
    }

    /// <summary>Creates an operator descriptor for tolerated unknown syntax.</summary>
    public static PdfGraphicsOperator CreateUnknown(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new PdfGraphicsOperator(code, 0, int.MaxValue);
    }

    /// <inheritdoc />
    public bool Equals(PdfGraphicsOperator? other) =>
        other is not null && string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PdfGraphicsOperator other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Code);

    /// <inheritdoc />
    public override string ToString() => Code;
}
