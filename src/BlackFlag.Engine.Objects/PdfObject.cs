namespace BlackFlag.Engine.Objects;

/// <summary>
/// Provides the parser-independent base abstraction for every PDF object.
/// </summary>
public abstract class PdfObject
{
    /// <summary>
    /// Gets the concrete kind of this PDF object.
    /// </summary>
    public abstract PdfObjectType Type { get; }

    /// <summary>
    /// Dispatches this object to a PDF object visitor.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor that receives this object.</param>
    /// <returns>The result produced by <paramref name="visitor"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Implementations throw when <paramref name="visitor"/> is
    /// <see langword="null"/>.
    /// </exception>
    public abstract TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor);
}
