namespace BlackFlag.Engine.Objects;

/// <summary>
/// Defines operations that can be performed on PDF objects.
/// </summary>
/// <typeparam name="TResult">The result produced by the visitor.</typeparam>
public abstract class PdfObjectVisitor<TResult>
{
    /// <summary>
    /// Visits a PDF object for which no more-specific overload is available.
    /// </summary>
    /// <param name="value">The PDF object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public abstract TResult Visit(PdfObject value);

    /// <summary>
    /// Visits a PDF null object.
    /// </summary>
    /// <param name="value">The PDF null object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfNull value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF Boolean object.
    /// </summary>
    /// <param name="value">The PDF Boolean object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfBoolean value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF integer object.
    /// </summary>
    /// <param name="value">The PDF integer object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfInteger value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF real-number object.
    /// </summary>
    /// <param name="value">The PDF real-number object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfReal value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF name object.
    /// </summary>
    /// <param name="value">The PDF name object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfName value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF literal-string object.
    /// </summary>
    /// <param name="value">The PDF literal-string object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfLiteralString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF hexadecimal-string object.
    /// </summary>
    /// <param name="value">The PDF hexadecimal-string object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfHexString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF array object.
    /// </summary>
    /// <param name="value">The PDF array object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfArray value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF dictionary object.
    /// </summary>
    /// <param name="value">The PDF dictionary object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfDictionary value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF indirect reference.
    /// </summary>
    /// <param name="value">The PDF indirect reference being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfIndirectReference value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }

    /// <summary>
    /// Visits a PDF stream object.
    /// </summary>
    /// <param name="value">The PDF stream object being visited.</param>
    /// <returns>The result of visiting <paramref name="value"/>.</returns>
    public virtual TResult Visit(PdfStream value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Visit((PdfObject)value);
    }
}
