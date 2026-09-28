using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CorpusManifestTests : IDisposable
{
    private readonly string repository = Path.Combine(Path.GetTempPath(), "PiratePDF Corpus " + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(repository, "tests", "FuzzCorpus");
    private string ManifestPath => Path.Combine(Root, "manifest.json");
    private string SeedPath => Path.Combine(Root, "filter-asciihex", "asciihex-empty.bin");
    private readonly JsonObject manifest;
    private JsonObject Seed => (JsonObject)manifest["seeds"][0];

    public CorpusManifestTests()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SeedPath));
        File.WriteAllBytes(SeedPath, [62]);
        manifest = new JsonObject
        {
            ["schema"] = "piratepdf-fuzz-corpus", ["version"] = 1,
            ["seeds"] = new JsonArray(new JsonObject
            {
                ["id"] = "asciihex-empty", ["target"] = "filter-asciihex", ["configurationVersion"] = 1,
                ["path"] = "tests/FuzzCorpus/filter-asciihex/asciihex-empty.bin", ["length"] = 1,
                ["sha256"] = Convert.ToHexString(SHA256.HashData(new byte[] { 62 })),
                ["sourceRecipe"] = "tests/PiratePDF.Fuzz.Tests/CorpusSeedRecipes.cs", ["origin"] = "repository-owned",
                ["rights"] = "Repository-owned fixture.", ["category"] = "valid-minimal", ["purpose"] = "Empty decode.",
                ["expectedOutcome"] = "success", ["related"] = new JsonArray("BF-1040")
            })
        };
        Save();
    }

    [Fact]
    public async Task CommittedManifestAndInventoryValidateWithoutExecutingTargets()
    {
        string root = FindRepository();
        CorpusValidationResult result = await CorpusValidator.ValidateAsync(root);
        if (Unsupported(result)) return;
        Assert.Equal(CorpusFailure.None, result.Failure);
        Assert.Equal(15, result.Seeds.Count);
        Assert.Equal(new[] { 6, 6, 3 }, result.Seeds.GroupBy(seed => seed.Target).Select(group => group.Count()));
        string[] files = Directory.GetFiles(Path.Combine(root, "tests", "FuzzCorpus"), "*.bin", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(files, result.Seeds.Select(seed => seed.Path));
        Assert.All(result.Seeds, seed =>
        {
            Assert.Equal("repository-owned", seed.Origin);
            Assert.Equal(1, seed.ConfigurationVersion);
            Assert.InRange(seed.Length, 0, TargetRegistry.Get(seed.Target).MaximumInputBytes);
        });
        CorpusValidationResult repeated = await CorpusValidator.ValidateAsync(root);
        Assert.Equal(result.ToJson(), repeated.ToJson());
        Assert.Equal(JsonSerializer.Serialize(result.Seeds), JsonSerializer.Serialize(repeated.Seeds));
        var exposed = Assert.IsAssignableFrom<IList<CorpusSeed>>(result.Seeds);
        Assert.Throws<NotSupportedException>(() => exposed.Clear());
    }

    [Fact]
    public void OwnedRecipesReproducePersistedBytesAndPreselectedOraclesWithoutRewriting()
    {
        string root = FindRepository();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tests", "FuzzCorpus", "manifest.json")));
        JsonElement[] entries = document.RootElement.GetProperty("seeds").EnumerateArray().ToArray();
        Assert.Equal(CorpusSeedRecipes.All.Count, entries.Length);
        for (int index = 0; index < entries.Length; index++)
        {
            CorpusRecipe recipe = CorpusSeedRecipes.All[index];
            JsonElement entry = entries[index];
            byte[] generated = recipe.Generate();
            Assert.Equal(generated, recipe.Generate());
            Assert.Equal(recipe.Id, entry.GetProperty("id").GetString());
            Assert.Equal(recipe.ExpectedOutcome, entry.GetProperty("expectedOutcome").GetString());
            Assert.Equal(recipe.Category, entry.GetProperty("category").GetString());
            Assert.Equal(recipe.SourceRecipe, entry.GetProperty("sourceRecipe").GetString());
            Assert.Equal(generated, File.ReadAllBytes(Path.Combine(root, entry.GetProperty("path").GetString())));
            Assert.Equal(Convert.ToHexString(SHA256.HashData(generated)), entry.GetProperty("sha256").GetString());
            Assert.Equal(generated.Length, entry.GetProperty("length").GetInt32());
        }
        // Independent budget arithmetic preserves fixture intent; no production decoder is called.
        Assert.All(CorpusSeedRecipes.All.Where(recipe => recipe.Id.StartsWith("flate-decoded-", StringComparison.Ordinal)),
            recipe => Assert.InRange(recipe.Generate().Length, 1025, 16384));
        Assert.True(4096 > 32 * CorpusSeedRecipes.All.Single(recipe => recipe.Id == "flate-expansion-over").Generate().Length);
    }

    [Theory]
    [InlineData("schema", "unsupported", CorpusFailure.UnsupportedSchema)]
    [InlineData("target", "unknown", CorpusFailure.UnknownTarget)]
    [InlineData("category", "anything", CorpusFailure.InvalidVocabulary)]
    [InlineData("origin", "external", CorpusFailure.InvalidVocabulary)]
    [InlineData("expectedOutcome", "crash", CorpusFailure.InvalidVocabulary)]
    [InlineData("id", "Uppercase", CorpusFailure.InvalidId)]
    [InlineData("id", "a/b", CorpusFailure.InvalidId)]
    [InlineData("sha256", "not-a-hash", CorpusFailure.InvalidMetadata)]
    [InlineData("sourceRecipe", "C:/private/file", CorpusFailure.InvalidMetadata)]
    public async Task InvalidMetadataAndVocabulariesReject(string field, string value, CorpusFailure expected)
    {
        if (field == "schema") manifest[field] = value;
        else Seed[field] = value;
        Save();
        await Expect(expected);
    }

    [Theory]
    [InlineData("/private/file.bin")]
    [InlineData("C:\\private\\file.bin")]
    [InlineData("\\\\server\\share\\file.bin")]
    [InlineData("../outside.bin")]
    [InlineData("tests/FuzzCorpus/../outside.bin")]
    [InlineData("tests\\FuzzCorpus\\filter-asciihex\\asciihex-empty.bin")]
    [InlineData("tests/FuzzCorpus//filter-asciihex/asciihex-empty.bin")]
    [InlineData("tests/FuzzCorpus/filter-asciihex/./asciihex-empty.bin")]
    [InlineData("tests/FuzzCorpus/filter-asciihex/asciihex-empty.bin:stream")]
    [InlineData("tests/FuzzCorpus/filter-asciihex/ASCIIHEX-EMPTY.bin")]
    [InlineData("tests/FuzzCorpus/filter-asciihex/asciihex-empty.bin.")]
    [InlineData("tests/FuzzCorpus/filter-asciihex/%2e%2e/outside.bin")]
    public async Task PathsMustHaveTheSingleCanonicalConfinedForm(string path)
    {
        Seed["path"] = path;
        Save();
        await Expect(CorpusFailure.InvalidPath);
    }

    [Fact]
    public async Task UnknownSchemaVersionRejects()
    {
        manifest["version"] = 2;
        Save();
        await Expect(CorpusFailure.UnsupportedSchema);
    }

    [Fact]
    public async Task ExactTargetConfigurationIsRequired()
    {
        Seed["configurationVersion"] = 2;
        Save();
        await Expect(CorpusFailure.ConfigurationMismatch);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DuplicateIdsAndPathsReject(bool duplicateId)
    {
        JsonObject other = (JsonObject)Seed.DeepClone();
        if (!duplicateId) other["id"] = "another-seed";
        ((JsonArray)manifest["seeds"]).Add(other);
        Save();
        await Expect(duplicateId ? CorpusFailure.DuplicateId : CorpusFailure.DuplicatePath);
    }

    [Fact]
    public async Task ManifestOrderMustBeOrdinal()
    {
        JsonObject other = (JsonObject)Seed.DeepClone();
        other["id"] = "asciihex-a";
        other["path"] = "tests/FuzzCorpus/filter-asciihex/asciihex-a.bin";
        ((JsonArray)manifest["seeds"]).Add(other);
        Save();
        await Expect(CorpusFailure.InvalidOrdering);
    }

    [Fact]
    public async Task HashMismatchRejects()
    {
        File.WriteAllBytes(SeedPath, [63]);
        await Expect(CorpusFailure.HashMismatch);
    }

    [Fact]
    public async Task LengthMismatchRejects()
    {
        Seed["length"] = 2;
        Save();
        await Expect(CorpusFailure.LengthMismatch);
    }

    [Fact]
    public async Task MissingSeedRejects()
    {
        File.Delete(SeedPath);
        await Expect(CorpusFailure.MissingSeed);
    }

    [Fact]
    public async Task UnlistedSeedRejects()
    {
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(SeedPath), "unlisted.bin"), [1]);
        await Expect(CorpusFailure.UnlistedFile);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeclaredAndActualOversizedSeedsReject(bool declared)
    {
        int length = TargetRegistry.Get("filter-asciihex").MaximumInputBytes + 1;
        if (declared) { Seed["length"] = length; Save(); }
        else File.WriteAllBytes(SeedPath, new byte[length]);
        await Expect(CorpusFailure.SeedTooLarge);
    }

    [Theory]
    [InlineData(0, CorpusFailure.None)]
    [InlineData(1, CorpusFailure.ManifestTooLarge)]
    public async Task ManifestHasAnExactByteCeiling(int extra, CorpusFailure expected)
    {
        byte[] original = File.ReadAllBytes(ManifestPath);
        byte[] padded = Enumerable.Repeat((byte)' ', CorpusValidator.MaximumManifestBytes + extra).ToArray();
        original.CopyTo(padded, 0);
        File.WriteAllBytes(ManifestPath, padded);
        await Expect(expected);
    }

    [Fact]
    public async Task ExcessiveEntryCountRejectsBeforeFilesAreNeeded()
    {
        JsonObject template = (JsonObject)Seed.DeepClone();
        var entries = new JsonArray();
        for (int index = 0; index <= CorpusValidator.MaximumEntries; index++) entries.Add(template.DeepClone());
        manifest["seeds"] = entries;
        Save();
        Assert.InRange(new FileInfo(ManifestPath).Length, 1, CorpusValidator.MaximumManifestBytes);
        await Expect(CorpusFailure.TooManyEntries);
    }

    [Fact]
    public async Task ExcessiveFilesystemEnumerationRejectsBeforeHashing()
    {
        for (int index = 0; index <= CorpusValidator.MaximumFileSystemEntries; index++)
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(SeedPath), "unlisted-" + index + ".bin"), []);
        await Expect(CorpusFailure.TooManyFiles);
    }

    [Theory]
    [InlineData("{", CorpusFailure.InvalidManifest)]
    [InlineData("{\"schema\":\"piratepdf-fuzz-corpus\",\"schema\":\"private\",\"version\":1,\"seeds\":[]}", CorpusFailure.InvalidFields)]
    [InlineData("{\"schema\":\"piratepdf-fuzz-corpus\",\"version\":1,\"seeds\":[[[[[]]]]]}", CorpusFailure.InvalidManifest)]
    [InlineData("{\"schema\":\"piratepdf-fuzz-corpus\",\"version\":1,\"seeds\":[]}", CorpusFailure.InvalidFields)]
    public async Task MalformedDeepDuplicateAndEmptyManifestsReject(string json, CorpusFailure expected)
    {
        File.WriteAllText(ManifestPath, json);
        await Expect(expected);
    }

    [Fact]
    public async Task DuplicateEntryPropertyRejectsRatherThanLastValueWinning()
    {
        string json = manifest.ToJsonString().Replace("\"length\":1", "\"length\":2,\"length\":1", StringComparison.Ordinal);
        File.WriteAllText(ManifestPath, json);
        await Expect(CorpusFailure.InvalidFields);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("type")]
    public async Task SchemaIsClosedAndFieldsAreRequiredAndTyped(string mode)
    {
        if (mode == "missing") Seed.Remove("rights");
        else if (mode == "extra") Seed["extension"] = "PRIVATE_METADATA";
        else Seed["length"] = "1";
        Save();
        await Expect(CorpusFailure.InvalidFields);
    }

    [Theory]
    [InlineData("purpose", 257)]
    [InlineData("sourceRecipe", 257)]
    [InlineData("rights", 161)]
    [InlineData("id", 65)]
    public async Task TextLengthsAreBounded(string field, int length)
    {
        Seed[field] = new string('a', length);
        Save();
        await Expect(CorpusFailure.InvalidMetadata);
    }

    [Fact]
    public async Task MissingManifestIsDistinctAndCancellationEscapes()
    {
        File.Delete(ManifestPath);
        await Expect(CorpusFailure.MissingManifest);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CorpusValidator.ValidateAsync(repository, cancellation.Token));
    }

    [Fact]
    public async Task SeedLinkEscapeIsRejected()
    {
        string outside = Path.Combine(repository, "outside.bin");
        File.WriteAllBytes(outside, [62]);
        File.Delete(SeedPath);
        File.CreateSymbolicLink(SeedPath, outside);
        try { await Expect(CorpusFailure.UnsafePath); }
        finally { File.Delete(SeedPath); }
    }

    [Fact]
    public void ExplicitGenerationRefusesToOverwriteAnExistingCorpus() =>
        Assert.Throws<InvalidOperationException>(() => CorpusSeedRecipes.WriteNewCorpus(repository));

    private void Save() => File.WriteAllText(ManifestPath, manifest.ToJsonString(), new UTF8Encoding(false));

    private async Task Expect(CorpusFailure expected)
    {
        CorpusValidationResult result = await CorpusValidator.ValidateAsync(repository);
        if (Unsupported(result)) return;
        Assert.Equal(expected, result.Failure);
        if (expected != CorpusFailure.None) Assert.Empty(result.Seeds);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.ToJson()), 1, 100);
        Assert.DoesNotContain(repository, result.ToJson());
        Assert.DoesNotContain("PRIVATE", result.ToJson());
        Assert.DoesNotContain("Exception", result.ToJson());
    }

    private static bool Unsupported(CorpusValidationResult result)
    {
        if (OperatingSystem.IsWindows()) return false;
        Assert.Equal(CorpusFailure.UnsupportedPlatform, result.Failure);
        return true;
    }

    private static string FindRepository()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    public void Dispose() => Directory.Delete(repository, recursive: true);
}
