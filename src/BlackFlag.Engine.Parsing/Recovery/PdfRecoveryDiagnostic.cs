
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Describes one structural recovery observation or action.</summary>
public sealed record PdfRecoveryDiagnostic(
    string Code,
    string Message,
    long? Offset = null,
    PdfObjectId? ObjectId = null);
