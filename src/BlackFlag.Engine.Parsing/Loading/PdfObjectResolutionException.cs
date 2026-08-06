using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Reports a document-level indirect-object resolution failure.
/// </summary>
public sealed class PdfObjectResolutionException : IOException
{
    /// <summary>Initializes a resolution exception.</summary>
    public PdfObjectResolutionException(
        PdfObjectId objectId,
        PdfObjectResolutionFailure failure,
        string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ObjectId = objectId;
        Failure = failure;
    }

    /// <summary>
    /// Initializes a resolution exception with the underlying failure.
    /// </summary>
    public PdfObjectResolutionException(
        PdfObjectId objectId,
        PdfObjectResolutionFailure failure,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(innerException);
        ObjectId = objectId;
        Failure = failure;
    }

    /// <summary>Gets the object being resolved.</summary>
    public PdfObjectId ObjectId { get; }

    /// <summary>Gets the failure category.</summary>
    public PdfObjectResolutionFailure Failure { get; }
}
