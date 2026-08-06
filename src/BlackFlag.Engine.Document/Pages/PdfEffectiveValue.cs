namespace BlackFlag.Engine.Document.Pages;

/// <summary>Pairs an effective page value with its resolution origin.</summary>
public readonly record struct PdfEffectiveValue<T>(
    T Value,
    PdfPageValueOrigin Origin)
{
    /// <summary>Gets whether the value was explicitly declared on the page.</summary>
    public bool IsExplicit => Origin == PdfPageValueOrigin.Explicit;
}
