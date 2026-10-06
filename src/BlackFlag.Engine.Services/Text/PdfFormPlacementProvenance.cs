using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Services.Text;

/// <summary>Owns the narrow discovery snapshot used by Form clone projection.</summary>
internal sealed class PdfFormPlacementProvenance
{
    private readonly PdfDocument document;
    private readonly PlacementState state;
    private readonly long dataRevision;
    private readonly bool filtered;
    private readonly byte[] sourceSpan;

    private PdfFormPlacementProvenance(
        PdfDocument document, PlacementState state, byte[] sourceSpan)
    {
        this.document = document;
        this.state = state;
        this.sourceSpan = sourceSpan;
        dataRevision = state.Content.DataRevision;
        filtered = state.Content.Dictionary.ContainsKey(PdfName.FromLatin1("Filter"));
    }

    internal static PdfFormPlacementProvenance Capture(
        PdfDocument document, PdfFormPlacementDescriptor descriptor)
    {
        PlacementState state = Resolve(document, descriptor);
        byte[] decoded = PdfContentStreamMutationCodec.Decode(state.Content);
        ValidateSpan(descriptor, decoded.Length);

        PdfFormPlacementSyntax.Validate(decoded, descriptor);

        return new PdfFormPlacementProvenance(document, state,
            decoded.AsSpan((int)descriptor.StartOffset, (int)(descriptor.EndOffset - descriptor.StartOffset)).ToArray());
    }

    internal (PdfStream Form, PdfStream Content, byte[] DecodedContent) Validate(
        PdfDocument currentDocument, PdfFormPlacementDescriptor descriptor)
    {
        if (!ReferenceEquals(document, currentDocument))
        {
            throw Stale();
        }

        PlacementState current = Resolve(currentDocument, descriptor);
        if (current != state)
        {
            throw Stale();
        }

        // Keep codec limits and malformed-Flate failures authoritative. Its one
        // decoded snapshot is also the input to successful redirection.
        byte[] decoded = PdfContentStreamMutationCodec.Decode(current.Content);
        ValidateSpan(descriptor, decoded.Length);
        if (current.Content.DataRevision != dataRevision ||
            current.Content.Dictionary.ContainsKey(PdfName.FromLatin1("Filter")) != filtered ||
            !decoded.AsSpan((int)descriptor.StartOffset, sourceSpan.Length).SequenceEqual(sourceSpan))
        {
            throw Stale();
        }

        return (current.Form, current.Content, decoded);
    }

    private static PlacementState Resolve(PdfDocument document, PdfFormPlacementDescriptor descriptor)
    {
        PdfDictionary page = Get<PdfDictionary>(document, descriptor.PageIdentifier);
        if (!page.TryGetValue(PdfName.FromLatin1("Type"), out PdfObject? type) ||
            type is not PdfName pageType || pageType != PdfName.FromLatin1("Page"))
        {
            throw Stale();
        }

        if (!page.TryGetValue(PdfName.FromLatin1("Contents"), out PdfObject? contents))
        {
            throw Stale();
        }

        PdfObject container = Dereference(document, contents);
        PdfObject selected;
        if (container is PdfArray array)
        {
            if ((uint)descriptor.StreamIndex >= (uint)array.Count)
            {
                throw Stale();
            }
            selected = array[descriptor.StreamIndex];
        }
        else
        {
            if (descriptor.StreamIndex != 0)
            {
                throw Stale();
            }
            selected = contents;
        }

        if (selected is not PdfIndirectReference contentReference ||
            contentReference.Identifier != descriptor.ContentStreamIdentifier)
        {
            throw Stale();
        }

        PdfStream content = Get<PdfStream>(document, descriptor.ContentStreamIdentifier);
        PdfDictionary resourceOwner = page;
        var visited = new HashSet<PdfObjectIdentifier> { descriptor.PageIdentifier };
        while (!resourceOwner.ContainsKey(PdfName.FromLatin1("Resources")))
        {
            if (!resourceOwner.TryGetValue(PdfName.FromLatin1("Parent"), out PdfObject? parent) ||
                parent is not PdfIndirectReference reference || !visited.Add(reference.Identifier))
            {
                throw Stale();
            }
            resourceOwner = Get<PdfDictionary>(document, reference.Identifier);
        }

        PdfDictionary resources = Dereference(document, resourceOwner[PdfName.FromLatin1("Resources")])
            as PdfDictionary ?? throw Stale();
        if (!resources.TryGetValue(PdfName.FromLatin1("XObject"), out PdfObject? categoryValue))
        {
            throw Stale();
        }
        PdfDictionary category = Dereference(document, categoryValue) as PdfDictionary ?? throw Stale();
        if (!category.TryGetValue(PdfName.FromLatin1(descriptor.ResourceName), out PdfObject? binding) ||
            binding is not PdfIndirectReference formReference || formReference.Identifier != descriptor.FormIdentifier)
        {
            throw Stale();
        }

        PdfStream form = Get<PdfStream>(document, descriptor.FormIdentifier);
        if (!form.Dictionary.TryGetValue(PdfName.FromLatin1("Subtype"), out PdfObject? subtype) ||
            subtype is not PdfName formType || formType != PdfName.FromLatin1("Form"))
        {
            throw Stale();
        }

        return new PlacementState(page, container, content, resourceOwner, resources, category, form);
    }

    private static PdfObject Dereference(PdfDocument document, PdfObject value) =>
        value is PdfIndirectReference reference ? Get<PdfObject>(document, reference.Identifier) : value;

    private static T Get<T>(PdfDocument document, PdfObjectIdentifier identifier) where T : PdfObject =>
        document.Objects.TryGet(identifier, out PdfObject? value) && value is T typed ? typed : throw Stale();

    private static void ValidateSpan(PdfFormPlacementDescriptor descriptor, int length)
    {
        if (descriptor.StartOffset < 0 || descriptor.StartOffset >= length ||
            descriptor.EndOffset <= descriptor.StartOffset || descriptor.EndOffset > length)
        {
            throw Stale();
        }
    }

    private static InvalidOperationException Stale() =>
        new("The Form placement provenance is stale or invalid; rediscover the placement before cloning.");

    private sealed record PlacementState(
        PdfDictionary Page, PdfObject Contents, PdfStream Content, PdfDictionary ResourceOwner,
        PdfDictionary Resources, PdfDictionary XObjects, PdfStream Form);
}
