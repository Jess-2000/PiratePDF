using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects common authoritative annotation state onto existing bound
/// annotation dictionaries and the owning page's `/Annots` array.
/// </summary>
public static class PdfAnnotationWriteProjector
{
    private static readonly PdfName AnnotsName = PdfName.FromLatin1("Annots");
    private static readonly PdfName TypeName = PdfName.FromLatin1("Type");
    private static readonly PdfName AnnotName = PdfName.FromLatin1("Annot");
    private static readonly PdfName SubtypeName = PdfName.FromLatin1("Subtype");
    private static readonly PdfName RectName = PdfName.FromLatin1("Rect");
    private static readonly PdfName ContentsName = PdfName.FromLatin1("Contents");
    private static readonly PdfName FlagsName = PdfName.FromLatin1("F");

    /// <summary>
    /// Projects the complete authoritative annotation collection for one
    /// bound page. Every attached annotation must already be bound to an
    /// existing indirect dictionary.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfObjectIdentifier pageIdentifier)
    {
        ArgumentNullException.ThrowIfNull(document);

        PdfPage page = FindPage(document, pageIdentifier);
        PdfObjectIdentifier[] targets =
        [
            .. page.Annotations.Select(
                annotation =>
                    annotation.ObjectIdentifier ??
                    throw new InvalidOperationException(
                        $"Annotation '{annotation.Id}' has not been assigned an indirect-object identifier."))
        ];

        return Create(
            document,
            pageIdentifier,
            targets);
    }

    /// <summary>
    /// Projects the owning page's authoritative `/Annots` array and only the
    /// explicitly targeted bound annotation dictionaries.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfObjectIdentifier pageIdentifier,
        IEnumerable<PdfObjectIdentifier> annotationIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(annotationIdentifiers);

        return CreateCore(
            document,
            pageIdentifier,
            new Dictionary<PdfAnnotation, PdfObjectIdentifier>(),
            annotationIdentifiers);
    }

    internal static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        PdfObjectIdentifier pageIdentifier,
        IReadOnlyDictionary<PdfAnnotation, PdfObjectIdentifier>
            pendingAnnotationBindings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(pendingAnnotationBindings);

        return CreateCore(
            document,
            pageIdentifier,
            pendingAnnotationBindings,
            pendingAnnotationBindings.Values);
    }

    private static PdfObjectWriteProjectionSet CreateCore(
        PdfDocument document,
        PdfObjectIdentifier pageIdentifier,
        IReadOnlyDictionary<PdfAnnotation, PdfObjectIdentifier>
            pendingAnnotationBindings,
        IEnumerable<PdfObjectIdentifier> targetedIdentifiers)
    {
        PdfPage page = FindPage(document, pageIdentifier);
        PdfDictionary sourcePage =
            GetDictionary(document, pageIdentifier, "Page");
        var remainingTargets =
            targetedIdentifiers.ToHashSet();

        var projections =
            new List<PdfObjectWriteProjection>(
                remainingTargets.Count + 1);
        var annotationReferences = new PdfArray();

        for (int index = 0; index < page.Annotations.Count; index++)
        {
            PdfAnnotation annotation = page.Annotations[index];
            PdfObjectIdentifier annotationIdentifier =
                ResolveAnnotationIdentifier(
                    annotation,
                    pendingAnnotationBindings);

            annotationReferences.Add(
                new PdfIndirectReference(
                    annotationIdentifier));

            if (!remainingTargets.Remove(annotationIdentifier))
            {
                continue;
            }

            PdfDictionary projectedAnnotation;

            if (document.Objects.TryGet(
                    annotationIdentifier,
                    out PdfObject? current))
            {
                projectedAnnotation =
                    current is PdfDictionary sourceAnnotation
                        ? sourceAnnotation.CreateCopy()
                        : throw new InvalidOperationException(
                            $"Annotation object '{annotationIdentifier}' must be a PDF dictionary.");
            }
            else if (pendingAnnotationBindings.TryGetValue(
                        annotation,
                        out PdfObjectIdentifier pendingIdentifier) &&
                    pendingIdentifier == annotationIdentifier)
            {
                projectedAnnotation = new PdfDictionary();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Annotation object '{annotationIdentifier}' is not present in the object store.");
            }

            ProjectCommonState(
                projectedAnnotation,
                annotation);

            projections.Add(
                new PdfObjectWriteProjection(
                    annotationIdentifier,
                    projectedAnnotation));
        }

        if (remainingTargets.Count != 0)
        {
            PdfObjectIdentifier missing =
                remainingTargets.OrderBy(
                    identifier => identifier.ObjectNumber)
                    .ThenBy(
                        identifier => identifier.GenerationNumber)
                    .First();

            throw new InvalidOperationException(
                $"Targeted annotation object '{missing}' is not attached to page '{pageIdentifier}'.");
        }

        PdfDictionary projectedPage = sourcePage.CreateCopy();

        if (annotationReferences.Count == 0)
        {
            _ = projectedPage.Remove(AnnotsName);
        }
        else
        {
            projectedPage.Set(
                AnnotsName,
                annotationReferences);
        }

        projections.Add(
            new PdfObjectWriteProjection(
                pageIdentifier,
                projectedPage));

        return new PdfObjectWriteProjectionSet(projections);
    }

    private static void ProjectCommonState(
        PdfDictionary dictionary,
        PdfAnnotation annotation)
    {
        dictionary.Set(TypeName, AnnotName);
        dictionary.Set(
            SubtypeName,
            PdfName.FromLatin1(
                GetSubtypeName(annotation.Subtype)));
        dictionary.Set(
            RectName,
            CreateRectangle(annotation.Rectangle));

        if (annotation.Contents is null)
        {
            _ = dictionary.Remove(ContentsName);
        }
        else
        {
            dictionary.Set(
                ContentsName,
                CreateTextString(annotation.Contents));
        }

        dictionary.Set(
            FlagsName,
            new PdfInteger((int)annotation.Flags));
    }

    private static PdfObjectIdentifier ResolveAnnotationIdentifier(
        PdfAnnotation annotation,
        IReadOnlyDictionary<PdfAnnotation, PdfObjectIdentifier>
            pendingAnnotationBindings)
    {
        if (annotation.ObjectIdentifier.HasValue)
        {
            return annotation.ObjectIdentifier.Value;
        }

        if (pendingAnnotationBindings.TryGetValue(
                annotation,
                out PdfObjectIdentifier pendingIdentifier))
        {
            return pendingIdentifier;
        }

        throw new InvalidOperationException(
            $"Annotation '{annotation.Id}' has not been assigned an indirect-object identifier.");
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

    private static PdfDictionary GetDictionary(
        PdfDocument document,
        PdfObjectIdentifier identifier,
        string modelName)
    {
        if (!document.Objects.TryGet(
                identifier,
                out PdfObject? value) ||
            value is null)
        {
            throw new InvalidOperationException(
                $"{modelName} object '{identifier}' is not present in the object store.");
        }

        return value as PdfDictionary ??
            throw new InvalidOperationException(
                $"{modelName} object '{identifier}' must be a PDF dictionary.");
    }

    private static PdfArray CreateRectangle(
        PdfAnnotationRectangle rectangle)
    {
        return new PdfArray(
        [
            CreateNumber(rectangle.Left),
            CreateNumber(rectangle.Bottom),
            CreateNumber(rectangle.Right),
            CreateNumber(rectangle.Top)
        ]);
    }

    private static PdfObject CreateNumber(double value)
    {
        return value == Math.Truncate(value) &&
            value >= int.MinValue &&
            value <= int.MaxValue
            ? new PdfInteger((int)value)
            : new PdfReal(value);
    }

    private static PdfString CreateTextString(string value)
    {
        if (value.All(static character => character <= byte.MaxValue))
        {
            return PdfString.FromLatin1(value);
        }

        byte[] text = Encoding.BigEndianUnicode.GetBytes(value);
        var bytes = new byte[text.Length + 2];
        bytes[0] = 0xFE;
        bytes[1] = 0xFF;
        text.CopyTo(bytes, 2);

        return new PdfString(
            bytes,
            PdfStringFormat.Hexadecimal);
    }

    private static string GetSubtypeName(
        PdfAnnotationSubtype subtype)
    {
        return subtype switch
        {
            PdfAnnotationSubtype.Text => "Text",
            PdfAnnotationSubtype.Link => "Link",
            PdfAnnotationSubtype.FreeText => "FreeText",
            PdfAnnotationSubtype.Line => "Line",
            PdfAnnotationSubtype.Square => "Square",
            PdfAnnotationSubtype.Circle => "Circle",
            PdfAnnotationSubtype.Polygon => "Polygon",
            PdfAnnotationSubtype.PolyLine => "PolyLine",
            PdfAnnotationSubtype.Highlight => "Highlight",
            PdfAnnotationSubtype.Underline => "Underline",
            PdfAnnotationSubtype.Squiggly => "Squiggly",
            PdfAnnotationSubtype.StrikeOut => "StrikeOut",
            PdfAnnotationSubtype.Stamp => "Stamp",
            PdfAnnotationSubtype.Caret => "Caret",
            PdfAnnotationSubtype.Ink => "Ink",
            PdfAnnotationSubtype.Popup => "Popup",
            PdfAnnotationSubtype.FileAttachment => "FileAttachment",
            PdfAnnotationSubtype.Sound => "Sound",
            PdfAnnotationSubtype.Movie => "Movie",
            PdfAnnotationSubtype.Widget => "Widget",
            PdfAnnotationSubtype.Screen => "Screen",
            PdfAnnotationSubtype.PrinterMark => "PrinterMark",
            PdfAnnotationSubtype.TrapNet => "TrapNet",
            PdfAnnotationSubtype.Watermark => "Watermark",
            PdfAnnotationSubtype.ThreeD => "3D",
            PdfAnnotationSubtype.Redact => "Redact",
            _ => throw new ArgumentOutOfRangeException(
                nameof(subtype),
                subtype,
                "The PDF annotation subtype is not supported.")
        };
    }
}
