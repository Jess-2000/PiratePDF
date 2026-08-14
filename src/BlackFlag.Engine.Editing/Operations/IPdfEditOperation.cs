using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Operations;

/// <summary>
/// Describes a controlled logical mutation performed by the editing subsystem.
/// </summary>
public interface IPdfEditOperation
{
    /// <summary>
    /// Gets the session ownership token associated with the operation.
    /// </summary>
    PdfEditingOwnershipToken Owner { get; }

    /// <summary>
    /// Gets the logical scope directly targeted by the operation.
    /// </summary>
    PdfEditScope Scope { get; }
}
