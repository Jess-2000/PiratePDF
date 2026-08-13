using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Recovery;
using BlackFlag.Engine.Parsing.Security;

namespace BlackFlag.Engine.Parsing.Tests.Security;

public sealed class PdfEncryptionSecurityHandlerTests
{
    [Fact]
    public void MissingEncryptEntryIsUnencrypted()
    {
        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result =
            handler.Assess(PdfDictionary.Empty);

        Assert.Equal(PdfEncryptionSecurityState.None, result.State);
        Assert.Null(result.Observation);
        Assert.False(result.BlocksProtectedContent);
    }

    [Fact]
    public void ValidStandardDictionaryRequiresPassword()
    {
        PdfDictionary encryption = StandardDictionary();
        PdfDictionary trailer = Dictionary(("Encrypt", encryption));
        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result = handler.Assess(trailer);

        Assert.Equal(
            PdfEncryptionSecurityState.PasswordRequired,
            result.State);
        Assert.True(result.BlocksProtectedContent);
        Assert.Equal("PasswordRequired", result.Observation?.Kind);
    }

    [Fact]
    public void IndirectStandardDictionaryRequiresPassword()
    {
        var id = new PdfObjectId(9, 0);
        var resolver = new StubResolver((id, StandardDictionary()));
        PdfDictionary trailer =
            Dictionary(("Encrypt", new PdfIndirectReference(9, 0)));
        var handler = new PdfEncryptionSecurityHandler(resolver);

        PdfEncryptionSecurityAssessment result = handler.Assess(trailer);

        Assert.Equal(
            PdfEncryptionSecurityState.PasswordRequired,
            result.State);
    }

    [Fact]
    public void MissingEncryptionDictionaryIsDamaged()
    {
        PdfDictionary trailer =
            Dictionary(("Encrypt", new PdfIndirectReference(9, 0)));
        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result = handler.Assess(trailer);

        Assert.Equal(PdfEncryptionSecurityState.Damaged, result.State);
        Assert.Equal("DamagedEncryption", result.Observation?.Kind);
    }

    [Fact]
    public void UnsupportedFilterIsDamaged()
    {
        PdfDictionary encryption = Dictionary(
            ("Filter", new PdfName("Custom")),
            ("R", new PdfInteger(4)),
            ("O", new PdfLiteralString("owner")),
            ("U", new PdfLiteralString("user")),
            ("P", new PdfInteger(-4)));

        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result =
            handler.Assess(Dictionary(("Encrypt", encryption)));

        Assert.Equal(PdfEncryptionSecurityState.Damaged, result.State);
    }

    [Fact]
    public void DamagedRecoveryStateIsTerminal()
    {
        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result =
            handler.Assess(
                PdfDictionary.Empty,
                PdfRecoveredEncryptionState.Damaged);

        Assert.Equal(PdfEncryptionSecurityState.Damaged, result.State);
        Assert.Equal("DamagedEncryption", result.Observation?.Kind);
    }

    [Fact]
    public void RecoveredAuthenticationStateRequiresPasswordWithoutTrailer()
    {
        var handler = new PdfEncryptionSecurityHandler(new StubResolver());

        PdfEncryptionSecurityAssessment result =
            handler.Assess(
                trailer: null,
                PdfRecoveredEncryptionState.AuthenticationRequired);

        Assert.Equal(
            PdfEncryptionSecurityState.PasswordRequired,
            result.State);
    }

    private static PdfDictionary StandardDictionary() =>
        Dictionary(
            ("Filter", StandardName()),
            ("V", new PdfInteger(2)),
            ("R", new PdfInteger(3)),
            ("O", new PdfLiteralString(new string('O', 32))),
            ("U", new PdfLiteralString(new string('U', 32))),
            ("P", new PdfInteger(-4)));

    private static PdfName StandardName() => new("Standard");

    private static PdfDictionary Dictionary(
        params (string Key, PdfObject Value)[] entries) =>
        new(
            entries.Select(
                item =>
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName(item.Key),
                        item.Value)));

    private sealed class StubResolver : IPdfIndirectObjectResolver
    {
        private readonly Dictionary<PdfObjectId, PdfObject> objects;

        public StubResolver(
            params (PdfObjectId Id, PdfObject Value)[] objects)
        {
            this.objects = objects.ToDictionary(
                item => item.Id,
                item => item.Value);
        }

        public PdfObject Resolve(PdfObjectId objectId)
        {
            throw new NotImplementedException();
        }


        public bool TryResolve(
            PdfObjectId objectId,
            out PdfObject value) =>
            objects.TryGetValue(objectId, out value!);
    }
}
