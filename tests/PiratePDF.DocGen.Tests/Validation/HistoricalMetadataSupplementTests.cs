using System.Security.Cryptography;
using System.Text.Json;
using PiratePDF.DocGen.Infrastructure;
using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Repair;
using PiratePDF.DocGen.Tests.TestInfrastructure;
using PiratePDF.DocGen.Validation;

namespace PiratePDF.DocGen.Tests.Validation;

public sealed class HistoricalMetadataSupplementTests
{
    [Fact]
    public void ValidRegistrationSatisfiesRequirementsWithoutRewritingOriginal()
    {
        using var fixture = new Fixture();
        byte[] before = File.ReadAllBytes(fixture.OriginalPath);
        fixture.Write(fixture.Record);
        DocumentationLibrary library = fixture.Load();
        Assert.False(DocumentationValidator.Validate(library).HasErrors);
        Assert.False(DocumentationRepairPlanner.Plan(library).HasChanges);
        Assert.Equal(before, File.ReadAllBytes(fixture.OriginalPath));
        Assert.Contains(library.AllDocuments, doc => doc.MetadataSupplementPath is not null &&
            doc.Title.Contains("metadata registered 2026-10-24", StringComparison.Ordinal) && doc.ReferencesWorkItem("BF-1054"));
    }

    [Theory]
    [InlineData("path")]
    [InlineData("hash")]
    [InlineData("policy")]
    [InlineData("claims")]
    [InlineData("date")]
    [InlineData("id")]
    [InlineData("history")]
    public void IncorrectBindingOrUnsupportedClaimsFailClosed(string fault)
    {
        using var fixture = new Fixture();
        var record = fault switch
        {
            "path" => fixture.Record with { OriginalPath = "docs/reviews/Unapproved.md" },
            "hash" => fixture.Record with { OriginalSha256 = new string('0', 64) },
            "policy" => fixture.Record with { PolicySha256 = new string('0', 64) },
            "claims" => fixture.Record with { HistoricalMetadata = "Historically approved and qualified" },
            "date" => fixture.Record with { Registered = "2026-10-10" },
            "id" => fixture.Record with { DocumentId = "PP-HIST-B9-002" },
            _ => fixture.Record with { History = "Original report accepted in 2026" }
        };
        fixture.Write(record);
        Assert.Throws<InvalidDataException>(() => fixture.Load());
    }

    [Fact]
    public void ConflictingRegistrationsFailClosed()
    {
        using var fixture = new Fixture();
        fixture.Write(fixture.Record, fixture.Record);
        Assert.Throws<InvalidDataException>(() => fixture.Load());
    }

    [Fact]
    public void UnknownFieldsCannotSmuggleHistoricalClaims()
    {
        using var fixture = new Fixture();
        fixture.Write(fixture.Record);
        string json = File.ReadAllText(fixture.SupplementPath).Replace("\"Version\":1", "\"OriginalCreated\":\"2020-01-01\",\"Version\":1", StringComparison.Ordinal);
        File.WriteAllText(fixture.SupplementPath, json);
        Assert.Throws<JsonException>(() => fixture.Load());
    }

    [Fact]
    public void DuplicatePropertiesAndMalformedJsonFailClosed()
    {
        using var fixture = new Fixture();
        File.WriteAllText(fixture.SupplementPath, "[{\"Version\":1,\"Version\":2}]");
        Assert.Throws<InvalidDataException>(() => fixture.Load());
        File.WriteAllText(fixture.SupplementPath, "[");
        // The reader throws an internal JsonException subtype for truncated JSON.
        Assert.ThrowsAny<JsonException>(() => fixture.Load());
    }

    [Fact]
    public void InvalidGoverningPolicyCannotAuthorizeSupplementation()
    {
        using var fixture = new Fixture();
        File.WriteAllText(fixture.PolicyPath, "# Policy\n\n" + HistoricalMetadataSupplements.PolicyMarker);
        fixture.Write(fixture.Record with { PolicySha256 = Fixture.Hash(fixture.PolicyPath) });
        Assert.Throws<InvalidDataException>(() => fixture.Load());
    }

    [Fact]
    public void OriginalStructureAndUnrelatedCurrentRequirementsRemainStrict()
    {
        using var fixture = new Fixture();
        File.AppendAllText(fixture.OriginalPath, "\n```\nunclosed fence\n");
        fixture.Write(fixture.Record with { OriginalSha256 = Fixture.Hash(fixture.OriginalPath) });
        Assert.True(DocumentationValidator.Validate(fixture.Load()).HasErrors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurrentOrNewQualificationReportStillRequiresMetadataAndHistory(bool metadata)
    {
        using var fixture = new Fixture();
        fixture.Write(fixture.Record);
        File.WriteAllText(Path.Combine(fixture.Directory.Path, "New-Qualification.md"), metadata
            ? DocumentationTestFactory.CreateDocument(documentId: "PP-TEST-901", includeRevisionHistory: false)
            : "# Current document\n");
        Assert.True(DocumentationValidator.Validate(fixture.Load()).HasErrors);
    }

    [Fact]
    public void MissingSupplementNeverRepairsImmutableOriginal()
    {
        using var fixture = new Fixture();
        DocumentationLibrary library = fixture.Load();
        Assert.True(DocumentationValidator.Validate(library).HasErrors);
        Assert.False(DocumentationRepairPlanner.Plan(library).HasChanges);
        Assert.NotEmpty(DocumentationRepairPlanner.Plan(library).Diagnostics);
    }

    private sealed class Fixture : IDisposable
    {
        internal TemporaryDirectory Directory { get; } = new();
        internal string OriginalPath { get; }
        internal string PolicyPath { get; }
        internal string SupplementPath { get; }
        internal HistoricalMetadataSupplements.Registration Record { get; }
        internal Fixture()
        {
            OriginalPath = Directory.CreateFile(HistoricalMetadataSupplements.Originals[0], "# Historical report\n\nEvidence unchanged.\n");
            PolicyPath = Directory.CreateFile("05-Governance/Documentation-Standards.md",
                DocumentationTestFactory.CreateDocument(body: "## Supplementation\n\n" + HistoricalMetadataSupplements.PolicyMarker));
            SupplementPath = Path.Combine(Directory.Path, "reviews", HistoricalMetadataSupplements.FileName);
            Record = new(1, "docs/" + HistoricalMetadataSupplements.Originals[0], Hash(OriginalPath), "PP-HIST-B9-001",
                "2026-10-24", "2027-01-24", "Unknown; not reconstructed.",
                "2026-10-24: Initial corrective registration; original report unchanged.", Hash(PolicyPath));
        }
        internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        internal void Write(params HistoricalMetadataSupplements.Registration[] records) => File.WriteAllText(SupplementPath, JsonSerializer.Serialize(records));
        internal DocumentationLibrary Load() => DocumentationLibraryLoader.Load(Directory.Path);
        public void Dispose() => Directory.Dispose();
    }
}
