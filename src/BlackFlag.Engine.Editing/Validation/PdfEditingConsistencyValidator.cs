using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Editing.Validation;

internal static class PdfEditingConsistencyValidator
{
    internal static void Validate(PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var pages = new HashSet<PdfPage>();
        var pageIds = new HashSet<Guid>();
        var annotations = new HashSet<PdfAnnotation>();

        for (var pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
        {
            var page = document.Pages[pageIndex];

            if (!pages.Add(page) || !pageIds.Add(page.Id))
            {
                throw new PdfEditingValidationException("The page tree contains duplicate page identity.");
            }

            for (var annotationIndex = 0; annotationIndex < page.Annotations.Count; annotationIndex++)
            {
                var annotation = page.Annotations[annotationIndex];

                if (!ReferenceEquals(annotation.Parent, page))
                {
                    throw new PdfEditingValidationException("An annotation is not owned by the page that contains it.");
                }

                if (!annotations.Add(annotation))
                {
                    throw new PdfEditingValidationException("An annotation is attached to more than one page collection.");
                }
            }
        }

        var fields = new HashSet<PdfFormField>();
        var widgetOwners = new Dictionary<PdfAnnotation, PdfFormField>();

        ValidateFormFields(document.AcroForm.Fields, null, pages, fields, widgetOwners);

        foreach (var annotation in annotations)
        {
            if (annotation.FormField is not null && !fields.Contains(annotation.FormField))
            {
                throw new PdfEditingValidationException("A Widget annotation references a form field outside the document AcroForm hierarchy.");
            }
        }

        var outlineItems = new HashSet<PdfOutlineItem>();
        ValidateOutlineItems(document.Outline.Items, null, pageIds, outlineItems);
    }

    private static void ValidateFormFields(
        IReadOnlyList<PdfFormField> source,
        PdfFormField? expectedParent,
        HashSet<PdfPage> pages,
        HashSet<PdfFormField> fields,
        Dictionary<PdfAnnotation, PdfFormField> widgetOwners)
    {
        for (var index = 0; index < source.Count; index++)
        {
            var field = source[index];

            if (!fields.Add(field))
            {
                throw new PdfEditingValidationException("The AcroForm hierarchy contains duplicate or cyclic field identity.");
            }

            if (!ReferenceEquals(field.Parent, expectedParent))
            {
                throw new PdfEditingValidationException("A form field has an inconsistent parent relationship.");
            }

            for (var widgetIndex = 0; widgetIndex < field.Widgets.Count; widgetIndex++)
            {
                var widget = field.Widgets[widgetIndex];

                if (widget.Subtype != PdfAnnotationSubtype.Widget)
                {
                    throw new PdfEditingValidationException("A form field is associated with a non-Widget annotation.");
                }

                if (widget.Parent is null || !pages.Contains(widget.Parent))
                {
                    throw new PdfEditingValidationException("A form-field Widget is not attached to a page in this document.");
                }

                if (!ReferenceEquals(widget.FormField, field))
                {
                    throw new PdfEditingValidationException("A Widget annotation and form field disagree about their association.");
                }

                if (!widgetOwners.TryAdd(widget, field))
                {
                    throw new PdfEditingValidationException("A Widget annotation is associated with more than one form field.");
                }
            }

            ValidateFormFields(field.Children, field, pages, fields, widgetOwners);
        }
    }

    private static void ValidateOutlineItems(
        IReadOnlyList<PdfOutlineItem> source,
        PdfOutlineItem? expectedParent,
        HashSet<Guid> pageIds,
        HashSet<PdfOutlineItem> items)
    {
        for (var index = 0; index < source.Count; index++)
        {
            var item = source[index];

            if (!items.Add(item))
            {
                throw new PdfEditingValidationException("The outline hierarchy contains duplicate or cyclic item identity.");
            }

            if (!ReferenceEquals(item.Parent, expectedParent))
            {
                throw new PdfEditingValidationException("An outline item has an inconsistent parent relationship.");
            }

            if (item.Destination is not null && item.Action is not null)
            {
                throw new PdfEditingValidationException("An outline item cannot contain both a direct destination and a navigation action.");
            }

            if (item.Destination is { } destination && !pageIds.Contains(destination.PageId))
            {
                throw new PdfEditingValidationException("An outline destination references a page outside the document page tree.");
            }

            ValidateOutlineItems(item.Children, item, pageIds, items);
        }
    }
}
