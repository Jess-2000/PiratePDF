namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Selects the only two supported meanings of editing text defined by a Form
/// XObject that may have more than one placement.
/// </summary>
public enum PdfSharedFormEditMode
{
    /// <summary>Mutate the authoritative Form source and therefore every placement.</summary>
    EditSharedSource = 0,

    /// <summary>Clone the Form and redirect only one explicitly identified placement.</summary>
    CloneForPlacementOnly = 1
}
