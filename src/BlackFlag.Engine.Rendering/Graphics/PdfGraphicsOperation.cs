using System.Collections.ObjectModel;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Graphics;

/// <summary>Represents one immutable canonical graphics operation.</summary>
public sealed class PdfGraphicsOperation
{
    private readonly ReadOnlyCollection<PdfObject> operands;

    /// <summary>Initializes a canonical graphics operation.</summary>
    public PdfGraphicsOperation(
        PdfGraphicsOperator @operator,
        IEnumerable<PdfObject> operands,
        PdfContentSourceSpan sourceSpan,
        PdfInlineImage? inlineImage = null)
    {
        ArgumentNullException.ThrowIfNull(@operator);
        ArgumentNullException.ThrowIfNull(operands);

        Operator = @operator;
        this.operands = Array.AsReadOnly(operands.ToArray());
        SourceSpan = sourceSpan;
        InlineImage = inlineImage;
    }

    /// <summary>Gets the operation identifier.</summary>
    public PdfGraphicsOperator Operator { get; }

    /// <summary>Gets the immutable operands in source order.</summary>
    public IReadOnlyList<PdfObject> Operands => operands;

    /// <summary>Gets the source range occupied by the operation and operands.</summary>
    public PdfContentSourceSpan SourceSpan { get; }

    /// <summary>Gets inline-image data when this is a <c>BI</c> operation.</summary>
    public PdfInlineImage? InlineImage { get; }
}
