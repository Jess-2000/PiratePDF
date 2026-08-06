using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Resolves immutable PDF objects through document cross-reference data.
/// </summary>
public interface IPdfIndirectObjectResolver
{
    /// <summary>Resolves the exact indirect object identifier.</summary>
    PdfObject Resolve(PdfObjectId objectId);

    /// <summary>Attempts to resolve the exact indirect object identifier.</summary>
    bool TryResolve(PdfObjectId objectId, out PdfObject? value);
}
