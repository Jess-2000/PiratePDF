using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Defines a high-level model capable of producing its low-level PDF
/// dictionary representation.
/// </summary>
internal interface IPdfDictionaryBackedModel
{
    PdfDictionary CreateDictionarySnapshot();
}