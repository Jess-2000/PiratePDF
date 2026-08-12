namespace BlackFlag.Engine.Security.Containers;

/// <summary>Describes the normalized kind of an embedded payload.</summary>
public enum DocumentEmbeddedObjectKind
{
    Unknown,
    Pdf,
    ZipContainer,
    OoxmlPackage,
    OpenDocumentPackage,
    OleCompoundDocument,
    Rtf,
    Executable,
    Script,
    Image
}
