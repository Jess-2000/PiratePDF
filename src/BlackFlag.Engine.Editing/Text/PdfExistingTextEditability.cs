namespace BlackFlag.Engine.Editing.Text;

/// <summary>
/// Describes whether provenance-backed existing PDF text can participate in a
/// supported editing strategy.
/// </summary>
public enum PdfExistingTextEditability
{
    /// <summary>The source can be edited without changing its resource identity.</summary>
    DirectlyEditable = 0,

    /// <summary>The edit requires a controlled resource change.</summary>
    EditableWithResourceChange = 1,

    /// <summary>The source is shared and an edit can affect multiple placements.</summary>
    EditableWithSharedSourceImpact = 2,

    /// <summary>The source provenance is not deterministic enough for mutation.</summary>
    ReadOnlyAmbiguous = 3,

    /// <summary>The source text cannot be safely encoded for mutation.</summary>
    ReadOnlyUnsupportedEncoding = 4,

    /// <summary>The active font cannot participate in a supported edit.</summary>
    ReadOnlyUnsupportedFont = 5,

    /// <summary>The selected visual content is not authoritative PDF text.</summary>
    ReadOnlyNonText = 6,

    /// <summary>The source structure is malformed and cannot be safely mutated.</summary>
    ReadOnlyMalformedSource = 7
}
