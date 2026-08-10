
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Represents one parser-validated indirect object discovered physically.</summary>
public sealed record PdfRecoveredObjectCandidate(
    PdfObjectId ObjectId,
    long Offset,
    long EndOffset,
    PdfObject Value);
