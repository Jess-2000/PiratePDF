namespace BlackFlag.Engine.Editing.Ownership;

/// <summary>
/// Represents a violation of an editing-session ownership boundary.
/// </summary>
public sealed class PdfEditingOwnershipException :
    InvalidOperationException
{
    public PdfEditingOwnershipException(string message)
        : base(message)
    {
    }
}
