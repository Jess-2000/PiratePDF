using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

/// <summary>
/// Reports a deterministic PDF resource-resolution failure.
/// </summary>
public sealed class PdfResourceResolutionException : FormatException
{
    /// <summary>
    /// Initializes a resource-resolution exception.
    /// </summary>
    public PdfResourceResolutionException(
        PdfResourceResolutionFailure failure,
        PdfResourceKind resourceKind,
        string? resourceName,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        ResourceKind = resourceKind;
        ResourceName = resourceName;
    }

    /// <summary>Gets the failure category.</summary>
    public PdfResourceResolutionFailure Failure { get; }

    /// <summary>Gets the resource category associated with the failure.</summary>
    public PdfResourceKind ResourceKind { get; }

    /// <summary>Gets the requested resource name when one is available.</summary>
    public string? ResourceName { get; }
}
