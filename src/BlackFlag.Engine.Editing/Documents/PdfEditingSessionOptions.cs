using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Documents;

/// <summary>
/// Configures the architectural behavior of a PDF editing session.
/// </summary>
public sealed class PdfEditingSessionOptions
{
    /// <summary>
    /// Gets or initializes the preservation policy used by the session.
    /// </summary>
    public PdfEditingPreservationMode PreservationMode { get; init; } =
        PdfEditingPreservationMode.PreserveUntargetedStructure;

    /// <summary>
    /// Gets or initializes validators that must approve each controlled
    /// operation before document state is mutated.
    /// </summary>
    public IReadOnlyList<IPdfEditingValidator> Validators { get; init; } =
        [];
}
