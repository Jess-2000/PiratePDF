using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>
/// Interprets canonical PDF graphics operations into an immutable semantic
/// display list without rasterizing paths, glyphs, images, or colors.
/// </summary>
public sealed partial class PdfGraphicsInterpreter
{
    private readonly PdfGraphicsInterpretationOptions options;
    private readonly IPdfFormOperationProvider? formOperationProvider;

    /// <summary>Initializes a graphics interpreter.</summary>
    public PdfGraphicsInterpreter(
        PdfGraphicsInterpretationOptions? options = null,
        IPdfFormOperationProvider? formOperationProvider = null)
    {
        this.options = options ?? new PdfGraphicsInterpretationOptions();
        this.options.Validate();
        this.formOperationProvider = formOperationProvider;
    }

    /// <summary>
    /// Interprets canonical operations using the supplied active resource scope.
    /// </summary>
    public PdfDisplayList Interpret(
        IEnumerable<PdfGraphicsOperation> operations,
        PdfResourceScope resources)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(resources);

        var activeForms = new HashSet<PdfResourceIdentity>();
        return InterpretCore(
            operations,
            resources,
            new PdfGraphicsStateSnapshot(),
            activeForms,
            formDepth: 0);
    }

}
