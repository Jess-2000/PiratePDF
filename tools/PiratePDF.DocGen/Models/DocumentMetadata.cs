namespace PiratePDF.DocGen.Models;

internal sealed record DocumentMetadata(
    string FilePath,
    string Title,
    string DocumentId,
    string Category,
    string Version,
    string Status,
    string Owner,
    string RelatedWorkItems,
    string LastUpdated,
    string NextReview);