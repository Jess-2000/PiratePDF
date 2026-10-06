using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Portable lifecycle contracts use owned bytes and immutable values, never FindingFixture's file store.</summary>
public sealed class FindingPolicyTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static FindingMetadata Discovery => new("piratepdf-fuzz-finding", 1, "owned-run", "case-one",
        FindingFixture.Hash("F0>"u8.ToArray()), 3, 16384, FindingFixture.Source(),
        "repository-owned-no-external-license-asserted", "unattempted", "not-requested", "not-requested");

    private static FindingResult Inspect(byte[] json, byte[] input = null) =>
        FindingValidation.Inspect(json, input ?? "F0>"u8.ToArray(), "owned-run", "case-one");

    [Fact]
    public void ValidDiscoveryHasDeterministicBoundedMetadataOnlyOutput()
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(Discovery, Options);
        FindingResult first = Inspect(json);
        Assert.Equal(FindingExit.Success, first.ExitCode);
        Assert.Equal(Discovery, first.Metadata);
        Assert.Equal(first.Json, Inspect(json).Json);
        Assert.InRange(Encoding.UTF8.GetByteCount(first.Json), 1, 1536);
        Assert.DoesNotContain("F0>", first.Json);
        Assert.DoesNotContain("controlled-owned-recipe", first.Json);
    }

    // Same malformed metadata cases as the retained-file integration test; validation is now executable on every OS.
    [Theory]
    [InlineData("schema", "wrong")]
    [InlineData("version", "2")]
    [InlineData("extra", "PRIVATE_DATA")]
    [InlineData("target", "unknown-target")]
    [InlineData("configurationVersion", "2")]
    [InlineData("sha256", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("length", "4")]
    [InlineData("inputCeiling", "16385")]
    [InlineData("rights", "external")]
    [InlineData("origin", "Unreviewed")]
    [InlineData("originId", "../private")]
    [InlineData("reproduction", "reproduced")]
    [InlineData("minimization", "minimized")]
    [InlineData("promotion", "promoted")]
    [InlineData("runId", "other-run")]
    [InlineData("findingId", "other-finding")]
    [InlineData("toolchain", "https://untrusted.example")]
    [InlineData("kind", "Unknown")]
    [InlineData("observed", "Unknown")]
    [InlineData("related", "private-content\n")]
    public void MetadataContractFailsClosedWithoutFilesystem(string field, string value)
    {
        JsonObject document = JsonSerializer.SerializeToNode(Discovery, Options).AsObject();
        JsonObject container = field is "target" or "configurationVersion" or "origin" or "originId" or "toolchain" or "kind" or "observed" or "related" ?
            document["source"].AsObject() : document;
        container[field] = int.TryParse(value, out int number) ? JsonValue.Create(number) : JsonValue.Create(value);
        FindingResult result = Inspect(Encoding.UTF8.GetBytes(document.ToJsonString()));
        Assert.Equal(FindingExit.IntegrityFailure, result.ExitCode);
        Assert.Null(result.Metadata);
        Assert.DoesNotContain("PRIVATE", result.Json);
    }

    [Theory]
    [InlineData("run", "INVALID")]
    [InlineData("run", "a/escape")]
    [InlineData("run", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("finding", "pending")]
    [InlineData("finding", "proposal")]
    [InlineData("finding", "proposal-work")]
    [InlineData("finding", "a/escape")]
    public void MatchingButUnadmittedIdentitiesFailClosed(string field, string value)
    {
        FindingMetadata metadata = field == "run" ? Discovery with { RunId = value } : Discovery with { FindingId = value };
        FindingResult result = FindingValidation.Inspect(JsonSerializer.SerializeToUtf8Bytes(metadata, Options), "F0>"u8.ToArray(),
            metadata.RunId, metadata.FindingId);
        Assert.Equal(FindingExit.IntegrityFailure, result.ExitCode);
        Assert.Null(result.Metadata);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, 1536);
        Assert.DoesNotContain(value, result.Json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../outside", false)]
    [InlineData("incoming/../outside", false)]
    [InlineData("incoming/file:stream", false)]
    [InlineData("C:/private.bin", false)]
    [InlineData("\\\\server\\private", false)]
    [InlineData("incoming/a/b/c/d", false)]
    [InlineData("incoming/owned.bin", true)]
    public void IncomingGrammarIsPortable(string path, bool expected) => Assert.Equal(expected, FindingValidation.IsIncomingPath(path));

    [Theory]
    [InlineData(15, 196608, 65536, true)]
    [InlineData(16, 0, 0, false)]
    [InlineData(0, 196609, 65536, false)]
    [InlineData(0, 0, 65537, false)]
    [InlineData(0, int.MaxValue, 1, false)]
    [InlineData(-1, 0, 0, false)]
    public void RetentionBoundsAreCheckedWithoutAllocation(int count, int retained, int input, bool expected) =>
        Assert.Equal(expected, FindingValidation.CanRetain(count, retained, input));

    [Theory]
    [InlineData(0, FindingExit.Success)]
    [InlineData(1, FindingExit.IntegrityFailure)]
    public void MetadataByteCeilingIsExact(int extra, FindingExit expected)
    {
        byte[] json = Enumerable.Repeat((byte)' ', FindingStore.MaximumMetadataBytes + extra).ToArray();
        JsonSerializer.SerializeToUtf8Bytes(Discovery, Options).CopyTo(json, 0);
        Assert.Equal(expected, Inspect(json).ExitCode);
    }

    [Theory]
    [InlineData(FindingClass.UnexpectedManagedFailure, FindingExit.ReproducedFailure, true)]
    [InlineData(FindingClass.Success, FindingExit.NonReproducible, false)]
    [InlineData(FindingClass.ExpectedRejection, FindingExit.NonReproducible, false)]
    public void StableReproductionPreservesDiscoveryAndOwnsEvidence(FindingClass outcome, FindingExit exit, bool matches)
    {
        FindingMetadata original = Discovery;
        string code = outcome == FindingClass.UnexpectedManagedFailure ? "UnexpectedTargetFailure" : "TargetResult";
        FindingAttempt[] attempts = [new(1, outcome, code), new(2, outcome, code)];
        FindingResult result = FindingReproduction.Assess(original, attempts);
        attempts[0] = new(1, FindingClass.Timeout, "NoWorkerRecord");
        Assert.Equal(exit, result.ExitCode);
        Assert.True(result.Agrees);
        Assert.Equal(matches, result.MatchesDiscovery);
        Assert.Equal(outcome, result.Attempts[0].Classification);
        Assert.Same(original, result.Metadata);
        Assert.Equal("unattempted", original.Reproduction);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, 1536);
        Assert.Throws<NotSupportedException>(() => ((IList<FindingAttempt>)result.Attempts).Clear());
    }

    [Fact]
    public void DisagreementAndInterruptedExecutionNeverConfirmDiscovery()
    {
        FindingAttempt[] attempts = [new(1, FindingClass.Success, "TargetResult"), new(2, FindingClass.ExpectedRejection, "TargetResult")];
        FindingResult result = FindingReproduction.Assess(Discovery, attempts);
        Assert.Equal(FindingExit.NonReproducible, result.ExitCode);
        Assert.False(result.Agrees);
        Assert.Equal(FindingExit.Cancelled, FindingReproduction.Assess(Discovery, [], FindingClass.Cancellation).ExitCode);
        Assert.Equal(FindingExit.IntegrityFailure, FindingReproduction.Assess(Discovery, [], FindingClass.IntegrityFailure).ExitCode);
        Assert.Throws<ArgumentException>(() => FindingReproduction.Assess(Discovery, [attempts[0], attempts[1], attempts[0]]));
    }

    [Theory]
    [InlineData("long-code")]
    [InlineData("wrong-position")]
    [InlineData("wrong-code")]
    [InlineData("invalid-class")]
    [InlineData("invalid-interruption")]
    [InlineData("invalid-metadata")]
    public void AssessmentRejectsUnboundedOrInconsistentEvidence(string fault)
    {
        FindingAttempt attempt = fault switch
        {
            "long-code" => new(1, FindingClass.Indeterminate, new string('X', 4096)),
            "wrong-position" => new(2, FindingClass.Success, "TargetResult"),
            "wrong-code" => new(1, FindingClass.Timeout, "TargetResult"),
            "invalid-class" => new(1, FindingClass.Duplicate, "NoWorkerRecord"),
            _ => new(1, FindingClass.Success, "TargetResult")
        };
        FindingClass? interruption = fault == "invalid-interruption" ? FindingClass.Success : null;
        FindingMetadata metadata = fault == "invalid-metadata" ? Discovery with { RunId = new string('a', 65) } : Discovery;
        Assert.Throws<ArgumentException>(() => FindingReproduction.Assess(metadata, [attempt], interruption));
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("target", false)]
    [InlineData("config", false)]
    [InlineData("related", false)]
    [InlineData("length", false)]
    [InlineData("parent", false)]
    [InlineData("outcome", false)]
    public void ReductionMetadataRequiresSameCharacterizedFailure(string fault, bool expected)
    {
        FindingMetadata original = Discovery;
        FindingMetadata smaller = original with { Length = 2, Source = original.Source with { ParentSha256 = original.Sha256 } };
        smaller = fault switch
        {
            "target" => smaller with { Source = smaller.Source with { Target = "font-sanitize" } },
            "config" => smaller with { Source = smaller.Source with { ConfigurationVersion = 2 } },
            "related" => smaller with { Source = smaller.Source with { Related = "" } },
            "length" => smaller with { Length = 3 },
            "parent" => smaller with { Source = smaller.Source with { ParentSha256 = new string('A', 64) } },
            "outcome" => smaller with { Source = smaller.Source with { Observed = FindingClass.Timeout } },
            _ => smaller
        };
        Assert.Equal(expected, FindingMinimization.CanCompare(original, smaller));
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("no-prior", false)]
    [InlineData("probe", false)]
    [InlineData("fixture", false)]
    [InlineData("unreviewed", false)]
    [InlineData("untracked", false)]
    [InlineData("timeout", false)]
    [InlineData("success", false)]
    [InlineData("cleanup", false)]
    [InlineData("discovery-success", false)]
    public void CorpusEligibilityRequiresOwnedCharacterizedPriorFailure(string fault, bool expected)
    {
        FindingMetadata metadata = Discovery;
        metadata = metadata with { Source = fault switch
        {
            "probe" => metadata.Source with { Origin = FindingOrigin.ControlledProbe },
            "fixture" => metadata.Source with { Kind = FindingKind.ControlledFixture },
            "unreviewed" => metadata.Source with { Origin = FindingOrigin.Unreviewed },
            "untracked" => metadata.Source with { Related = "" },
            "discovery-success" => metadata.Source with { Observed = FindingClass.Success },
            _ => metadata.Source
        } };
        FindingClass? prior = fault switch
        {
            "no-prior" => null, "timeout" => FindingClass.Timeout, "success" => FindingClass.Success,
            "cleanup" => FindingClass.CleanupFailure, _ => FindingClass.UnexpectedManagedFailure
        };
        Assert.Equal(expected, FindingPromotion.IsEligible(metadata, PromotionDecision.CorpusFixedSuccess, prior));
        Assert.Equal(expected, FindingPromotion.IsEligible(metadata, PromotionDecision.CorpusFixedRejection, prior));
    }

    [Fact]
    public void ControlledTimeoutCanOnlyRecommendAnOrdinaryToolingTest()
    {
        FindingMetadata metadata = Discovery with { Source = Discovery.Source with { Origin = FindingOrigin.ControlledProbe } };
        Assert.True(FindingPromotion.IsEligible(metadata, PromotionDecision.OrdinaryToolingRegression, FindingClass.Timeout));
        Assert.False(FindingPromotion.IsEligible(metadata, PromotionDecision.OrdinaryEngineRegression, FindingClass.Timeout));
    }
}
