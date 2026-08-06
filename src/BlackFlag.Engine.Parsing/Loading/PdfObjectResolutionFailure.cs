namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Identifies why indirect-object resolution failed.
/// </summary>
public enum PdfObjectResolutionFailure
{
    Missing,
    Free,
    IdentifierMismatch,
    ExpectedObjectStream,
    CompressedObjectMismatch,
    CircularDependency,
    MalformedObject
}
