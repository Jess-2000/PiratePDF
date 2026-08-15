namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Restricts a decoration target to all, odd, or even physical pages.</summary>
public enum PdfPageSelectionParity
{
    /// <summary>Target every page in the configured range.</summary>
    All = 0,

    /// <summary>Target physical pages whose one-based ordinal is odd.</summary>
    Odd = 1,

    /// <summary>Target physical pages whose one-based ordinal is even.</summary>
    Even = 2
}
