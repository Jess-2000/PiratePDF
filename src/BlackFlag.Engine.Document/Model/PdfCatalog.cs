using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Model;

/// <summary>
/// Represents the high-level PDF document catalog.
/// </summary>
public sealed class PdfCatalog :
    IPdfDictionaryBackedModel
{
    private readonly PdfObjectBinding binding =
        new();

    public PdfCatalog(PdfPageTree pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        Pages = pages;
    }

    public PdfPageTree Pages { get; }

    public string? Language { get; private set; }

    internal PdfObjectIdentifier? ObjectIdentifier =>
        binding.Identifier;

    public void SetLanguage(string? language)
    {
        Language = string.IsNullOrWhiteSpace(language)
            ? null
            : language.Trim();
    }

    internal void BindObjectIdentifier(
        PdfObjectIdentifier identifier)
    {
        binding.Bind(identifier);
    }

    internal PdfObjectIdentifier GetRequiredObjectIdentifier()
    {
        return binding.GetRequiredIdentifier(
            nameof(PdfCatalog));
    }

    PdfDictionary
        IPdfDictionaryBackedModel.CreateDictionarySnapshot()
    {
        return CreateDictionarySnapshot();
    }

    internal PdfDictionary CreateDictionarySnapshot()
    {
        var dictionary = new PdfDictionary();

        dictionary.Add(
            PdfNames.Type,
            PdfNames.Catalog);

        dictionary.Add(
            PdfNames.Pages,
            new PdfIndirectReference(
                Pages.GetRequiredObjectIdentifier()));

        if (Language is not null)
        {
            dictionary.Add(
                PdfNames.Language,
                PdfString.FromLatin1(Language));
        }

        return dictionary;
    }
}
