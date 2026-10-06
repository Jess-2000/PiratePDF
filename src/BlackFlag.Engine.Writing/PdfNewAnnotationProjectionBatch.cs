using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

internal sealed class PdfNewAnnotationProjectionBatch
{
    private readonly IReadOnlyList<AnnotationBinding> bindings;

    private PdfNewAnnotationProjectionBatch(
        PdfObjectWriteProjectionSet projections,
        IReadOnlyList<AnnotationBinding> bindings)
    {
        Projections = projections;
        this.bindings = bindings;
    }

    public PdfObjectWriteProjectionSet Projections { get; }

    public static PdfNewAnnotationProjectionBatch Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        PdfDocument document = writePlan.Document;
        PdfPage page = FindPage(document, pageIdentifier);

        PdfAnnotation[] newAnnotations =
        [
            .. page.Annotations.Where(
                static annotation =>
                    !annotation.ObjectIdentifier.HasValue)
        ];

        if (newAnnotations.Length == 0)
        {
            throw new InvalidOperationException(
                "New-annotation write planning requires at least one unbound annotation.");
        }

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();
        var pending =
            new Dictionary<PdfAnnotation, PdfObjectIdentifier>();
        var bindings =
            new List<AnnotationBinding>(
                newAnnotations.Length);

        for (int index = 0; index < newAnnotations.Length; index++)
        {
            PdfAnnotation annotation = newAnnotations[index];
            PdfObjectIdentifier identifier =
                allocator.ReserveNext();

            pending.Add(annotation, identifier);
            bindings.Add(
                new AnnotationBinding(
                    annotation,
                    identifier));
        }

        PdfObjectWriteProjectionSet projections =
            PdfAnnotationWriteProjector.Create(
                document,
                pageIdentifier,
                pending);

        return new PdfNewAnnotationProjectionBatch(
            projections,
            bindings.AsReadOnly());
    }

    public void CommitBindings()
    {
        for (int index = 0; index < bindings.Count; index++)
        {
            AnnotationBinding binding = bindings[index];

            if (binding.Annotation.ObjectIdentifier.HasValue)
            {
                throw new InvalidOperationException(
                    $"Annotation '{binding.Annotation.Id}' was bound unexpectedly while new-annotation write planning was in progress.");
            }
        }

        for (int index = 0; index < bindings.Count; index++)
        {
            AnnotationBinding binding = bindings[index];
            binding.Annotation.BindObjectIdentifier(
                binding.Identifier);
        }
    }

    private static PdfPage FindPage(
        PdfDocument document,
        PdfObjectIdentifier identifier)
    {
        foreach (PdfPage page in document.Pages)
        {
            if (page.ObjectIdentifier == identifier)
            {
                return page;
            }
        }

        throw new InvalidOperationException(
            $"Page object '{identifier}' is not bound to an authoritative page.");
    }

    private sealed record AnnotationBinding(
        PdfAnnotation Annotation,
        PdfObjectIdentifier Identifier);
}
