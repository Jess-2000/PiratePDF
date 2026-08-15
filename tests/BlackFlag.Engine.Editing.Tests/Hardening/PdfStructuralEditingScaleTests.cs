using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Hardening;

public sealed class PdfStructuralEditingScaleTests
{
    [Fact]
    public void LargeMixedStructuralTransactionPreservesCountsAndHistory()
    {
        const int ItemCount = 200;

        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        using (var transaction = session.BeginTransaction())
        {
            for (var index = 0; index < ItemCount; index++)
            {
                var field = session.Document.CreateFormField(PdfFormFieldType.Text);
                _ = session.Document.SetFormFieldPartialName(field, $"Field{index}");
                _ = session.Document.AddFormField(field);

                var outline = session.Document.CreateOutlineItem($"Item {index}");
                _ = session.Document.SetOutlineDestination(outline, page);
                _ = session.Document.AddOutlineItem(outline);

                var decoration = session.Document.CreatePageDecoration(
                    PdfPageDecorationKind.Text,
                    PdfPageDecorationRegion.Footer);
                _ = session.Document.SetPageDecorationText(decoration, $"Footer {index}");
                _ = session.Document.AddPageDecoration(decoration);
            }

            _ = transaction.Commit();
        }

        Assert.Equal(ItemCount, session.Document.FormFields.Count);
        Assert.Equal(ItemCount, session.Document.OutlineItems.Count);
        Assert.Equal(ItemCount, session.Document.PageDecorations.Count);
        session.ValidateConsistency();

        _ = session.History.Undo();

        Assert.Empty(session.Document.FormFields);
        Assert.Empty(session.Document.OutlineItems);
        Assert.Empty(session.Document.PageDecorations);
        session.ValidateConsistency();

        _ = session.History.Redo();

        Assert.Equal(ItemCount, session.Document.FormFields.Count);
        Assert.Equal(ItemCount, session.Document.OutlineItems.Count);
        Assert.Equal(ItemCount, session.Document.PageDecorations.Count);
        session.ValidateConsistency();
    }
}
