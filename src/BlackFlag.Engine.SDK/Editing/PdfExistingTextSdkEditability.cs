namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Describes the safe SDK edit strategy available for an existing-text target.</summary>
public enum PdfExistingTextSdkEditability
{
    DirectlyEditable = 0,
    EditableWithResourceChange = 1,
    EditableWithSharedSourceImpact = 2,
    ReadOnlyAmbiguous = 3,
    ReadOnlyUnsupportedEncoding = 4,
    ReadOnlyUnsupportedFont = 5,
    ReadOnlyNonText = 6,
    ReadOnlyMalformedSource = 7,
    EditableWithPositionPreservation = 8
}
