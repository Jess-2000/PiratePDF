#nullable enable

using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Validation;

public sealed class PdfEditingValidationTests
{
    [Fact]
    public void ValidatorRunsBeforePageMutation()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        var validator =
            new RecordingValidator(
                (_, _) =>
                    throw new PdfEditingValidationException(
                        "Page edit rejected."));

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators = [validator]
                });

        var page =
            session.Document.CreatePage(
                612,
                792);

        var exception =
            Assert.Throws<PdfEditingValidationException>(
                () =>
                    session.Document.InsertPage(
                        1,
                        page));

        Assert.Equal(
            "Page edit rejected.",
            exception.Message);
        Assert.Single(validator.Operations);
        Assert.Equal(
            PdfEditScope.Page,
            validator.Operations[0].Scope);
        Assert.Single(session.Document.Pages);
        Assert.False(
            session.Document.Pages.Contains(page));
    }

    [Fact]
    public void ValidatorRunsBeforeContentMutationAndTransactionRecording()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        var rejectContent =
            false;

        var validator =
            new RecordingValidator(
                (_, operation) =>
                {
                    if (rejectContent &&
                        operation.Scope == PdfEditScope.Content)
                    {
                        throw new PdfEditingValidationException(
                            "Content edit rejected.");
                    }
                });

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators = [validator]
                });

        var page =
            session.Document.Pages[0];

        _ = session.Document.ReplacePageContent(
            page,
            Encoding.ASCII.GetBytes("q Q"));

        rejectContent =
            true;

        using var transaction =
            session.BeginTransaction();

        Assert.Throws<PdfEditingValidationException>(
            () =>
                session.Document.ReplacePageContent(
                    page,
                    Encoding.ASCII.GetBytes("BT ET")));

        Assert.Equal(
            "q Q",
            Encoding.ASCII.GetString(
                page.GetContentBytes()));
        Assert.Equal(
            0,
            transaction.ChangeCount);

        transaction.Rollback();
    }

    [Fact]
    public void ValidatorRunsBeforeResourceMutation()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        var validator =
            new RecordingValidator(
                (_, operation) =>
                {
                    if (operation.Scope == PdfEditScope.Resource)
                    {
                        throw new PdfEditingValidationException(
                            "Resource edit rejected.");
                    }
                });

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators = [validator]
                });

        var page =
            session.Document.Pages[0];
        var category =
            PdfName.FromLatin1("Font");
        var name =
            PdfName.FromLatin1("F1");

        Assert.Throws<PdfEditingValidationException>(
            () =>
                session.Document.SetPageResource(
                    page,
                    category,
                    name,
                    new PdfDictionary()));

        Assert.False(
            page.ContainsResource(
                category,
                name));
        Assert.False(page.HasResources);
    }

    [Fact]
    public void ValidatorsRunInConfiguredOrder()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        var order =
            new List<int>();

        var first =
            new RecordingValidator(
                (_, _) => order.Add(1));

        var second =
            new RecordingValidator(
                (_, _) => order.Add(2));

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators =
                    [
                        first,
                        second
                    ]
                });

        _ = session.Document.SetPageRotation(
            session.Document.Pages[0],
            PdfPageRotation.Clockwise90);

        Assert.Equal(
            [1, 2],
            order);
    }

    [Fact]
    public void SessionSnapshotsConfiguredValidators()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        var configured =
            new List<IPdfEditingValidator>
            {
                new RecordingValidator()
            };

        using var session =
            new PdfEditingSession(
                document,
                new PdfEditingSessionOptions
                {
                    Validators = configured
                });

        configured.Clear();

        _ = session.Document.SetPageRotation(
            session.Document.Pages[0],
            PdfPageRotation.Clockwise90);

        Assert.Single(session.Validators);
    }

    [Fact]
    public void NullValidatorEntriesAreRejected()
    {
        var document =
            PdfDocument.CreateEmpty();

        Assert.Throws<ArgumentException>(
            () =>
            {
                using var session =
                    new PdfEditingSession(
                        document,
                        new PdfEditingSessionOptions
                        {
                            Validators =
                            [
                                null!
                            ]
                        });
            });
    }

    private sealed class RecordingValidator :
        IPdfEditingValidator
    {
        private readonly Action<PdfEditingSession, IPdfEditOperation> validate;

        public RecordingValidator(
            Action<PdfEditingSession, IPdfEditOperation>? validate = null)
        {
            this.validate =
                validate ?? ((_, _) => { });
        }

        public List<IPdfEditOperation> Operations { get; } =
            [];

        public void Validate(
            PdfEditingSession session,
            IPdfEditOperation operation)
        {
            Operations.Add(operation);
            validate(
                session,
                operation);
        }
    }
}
