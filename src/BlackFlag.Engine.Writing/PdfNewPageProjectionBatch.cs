using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

internal sealed class PdfNewPageProjectionBatch
{
    private readonly IReadOnlyList<PageBinding> bindings;

    private PdfNewPageProjectionBatch(
        PdfObjectWriteProjectionSet projections,
        IReadOnlyList<PageBinding> bindings)
    {
        Projections = projections;
        this.bindings = bindings;
    }

    public PdfObjectWriteProjectionSet Projections { get; }

    public static PdfNewPageProjectionBatch Create(
        PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        PdfDocument document = writePlan.Document;
        PdfPage[] newPages =
        [
            .. document.Pages.Where(
                static page => !page.ObjectIdentifier.HasValue)
        ];

        if (newPages.Length == 0)
        {
            throw new InvalidOperationException(
                "New-page write planning requires at least one unbound page.");
        }

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();
        var pendingBindings =
            new Dictionary<PdfPage, PdfObjectIdentifier>();
        var bindings =
            new List<PageBinding>(newPages.Length);
        var projections =
            new List<PdfObjectWriteProjection>(
                newPages.Length + 1);

        for (int index = 0; index < newPages.Length; index++)
        {
            PdfPage page = newPages[index];
            PdfObjectIdentifier identifier =
                allocator.ReserveNext();
            PdfObject value =
                page.CreateDictionarySnapshot();

            pendingBindings.Add(
                page,
                identifier);
            bindings.Add(
                new PageBinding(
                    page,
                    identifier));
            projections.Add(
                new PdfObjectWriteProjection(
                    identifier,
                    value));
        }

        PdfObjectWriteProjectionSet treeProjection =
            PdfPageTreeWriteProjector.Create(
                document,
                pendingBindings);

        projections.AddRange(
            treeProjection.Projections);

        return new PdfNewPageProjectionBatch(
            new PdfObjectWriteProjectionSet(projections),
            bindings.AsReadOnly());
    }

    public void CommitBindings()
    {
        for (int index = 0; index < bindings.Count; index++)
        {
            PageBinding binding = bindings[index];

            if (binding.Page.ObjectIdentifier.HasValue)
            {
                throw new InvalidOperationException(
                    $"Page '{binding.Page.Id}' was bound unexpectedly while new-page write planning was in progress.");
            }
        }

        for (int index = 0; index < bindings.Count; index++)
        {
            PageBinding binding = bindings[index];
            binding.Page.BindObjectIdentifier(
                binding.Identifier);
        }
    }

    private sealed record PageBinding(
        PdfPage Page,
        PdfObjectIdentifier Identifier);
}
