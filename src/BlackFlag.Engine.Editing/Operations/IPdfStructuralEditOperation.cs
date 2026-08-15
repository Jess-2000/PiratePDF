using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Identifies an editing operation that targets a structured PDF feature.
/// </summary>
/// <remarks>
/// <see cref="IPdfEditOperation.Scope"/> identifies where the operation
/// applies. <see cref="StructureKind"/> identifies what kind of structured
/// feature is being edited. Keeping those concepts separate prevents the
/// editing scope from becoming a feature taxonomy as structural editing grows.
/// </remarks>
public interface IPdfStructuralEditOperation :
    IPdfEditOperation
{
    /// <summary>
    /// Gets the kind of structured PDF feature targeted by the operation.
    /// </summary>
    PdfStructureKind StructureKind { get; }
}
