using System.Buffers.Binary;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Fonts;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExternalExistingTextFontServiceTests
{
    [Fact]
    public void ResourceChangeOperationReportsSupportedStrategy()
    {
        Assert.Equal(
            PdfExistingTextEditability.EditableWithResourceChange,
            PdfExistingTextResourceChangeOperationStrategy());
    }

    [Theory]
    [InlineData("مرحبا")]
    [InlineData("a\u0301")]
    [InlineData("\u0600")]
    [InlineData("\u0700")]
    [InlineData("\u074F")]
    [InlineData("\u0780")]
    [InlineData("\u07BF")]
    [InlineData("\u07C0")]
    [InlineData("\u07FF")]
    [InlineData("\u08FF")]
    [InlineData("👩\u200D💻")]
    public void ShapingDependentTextIsRejected(
        string text)
    {
        Assert.Throws<NotSupportedException>(
            () =>
                PdfExternalTextShapingGuard
                    .EnsureSimpleScalarMapping(
                        text));
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Ω")]
    [InlineData("😀")]
    public void SimpleScalarTextPassesShapingGuard(
        string text)
    {
        PdfExternalTextShapingGuard
            .EnsureSimpleScalarMapping(
                text);
    }

    private static PdfExistingTextEditability
        PdfExistingTextResourceChangeOperationStrategy()
    {
        var owner =
            new PdfEditingOwnershipToken(
                Guid.NewGuid());

        var identity =
            new PdfExistingTextIdentity(
                owner.SessionId,
                Guid.NewGuid(),
                PdfExistingTextSourceKind.PageContent,
                new PdfObjectIdentifier(3),
                new PdfObjectIdentifier(4),
                0,
                0,
                1,
                0,
                0);

        var constructor =
            typeof(PdfExistingTextResourceChangeOperation)
                .GetConstructors(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .Single();
        _ =
            (PdfExistingTextResourceChangeOperation)
            constructor.Invoke(
            [
                owner,
                identity,
                new PdfObjectIdentifier(4),
                "FExt1",
                7,
                10,
                20
            ]);

        return PdfExistingTextResourceChangeOperation.Editability;
    }
}
