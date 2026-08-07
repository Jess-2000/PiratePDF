namespace BlackFlag.Engine.Parsing.Resources;

/// <summary>
/// Identifies a resource-resolution failure category.
/// </summary>
public enum PdfResourceResolutionFailure
{
    /// <summary>The requested resource could not be found.</summary>
    MissingResource,

    /// <summary>The resource object has the wrong PDF type.</summary>
    WrongResourceType,

    /// <summary>The resource dictionary is malformed.</summary>
    MalformedResource,

    /// <summary>The resource graph contains a circular reference.</summary>
    CircularReference,

    /// <summary>The resource uses an unsupported subtype or construct.</summary>
    UnsupportedResource,

    /// <summary>A color-space resource is invalid.</summary>
    InvalidColorSpace,

    /// <summary>A font resource is invalid.</summary>
    InvalidFont,

    /// <summary>An XObject resource is invalid.</summary>
    InvalidXObject,

    /// <summary>A configured defensive limit was exceeded.</summary>
    LimitExceeded
}
