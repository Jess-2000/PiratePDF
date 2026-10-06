namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>Classifies fail-closed external-font sanitization failures.</summary>
public enum PdfExternalFontSanitizationFailure
{
    UnreadableInput = 0,
    InputTooLarge = 1,
    UnsupportedContainer = 2,
    MalformedHeader = 3,
    TooManyTables = 4,
    DuplicateTable = 5,
    InvalidTableRange = 6,
    OverlappingTables = 7,
    InvalidChecksum = 8,
    MissingRequiredTable = 9,
    MalformedRequiredTable = 10,
    ResourceLimitExceeded = 11
}
