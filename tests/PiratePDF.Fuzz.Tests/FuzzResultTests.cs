using System.Text.Json;

namespace PiratePDF.Fuzz.Tests;

public sealed class FuzzResultTests
{
    [Fact]
    public void SuccessfulResultContainsOnlyStableDigestAndOutcome()
    {
        byte[] input = "abc"u8.ToArray();
        FuzzResult result = FuzzResult.Success(input);
        input[0] = 0;
        Assert.Equal("success", result.Outcome);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", result.Observation);
        Assert.Equal(result, FuzzResult.Success("abc"u8));
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        Assert.Equal(result.Observation, json.RootElement.GetProperty("Observation").GetString());
        Assert.Equal("success", json.RootElement.GetProperty("Outcome").GetString());
    }

    [Fact]
    public void EmptySuccessHasKnownDigest() =>
        Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            FuzzResult.Success([]).Observation);

    [Fact]
    public void RejectionAllowsBoundedCodesAndRejectsLimitPlusOne()
    {
        string code = "Invalid_Data-" + new string('1', 51);
        FuzzResult result = FuzzResult.Rejected(code);
        Assert.Equal("rejected", result.Outcome);
        Assert.Equal(code, result.Observation);
        Assert.Equal(result, FuzzResult.Rejected(code));
        Assert.Equal(64, FuzzResult.Rejected(new string('a', 64)).Observation.Length);
        Assert.Throws<ArgumentException>(() => FuzzResult.Rejected(new string('a', 65)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid data")]
    [InlineData("Invalid\nData")]
    [InlineData("C:\\private\\document.pdf")]
    [InlineData("café")]
    public void MessagesPathsAndNonAsciiTextCannotBecomeRejectionCodes(string code) =>
        Assert.Throws<ArgumentException>(() => FuzzResult.Rejected(code));

    [Fact]
    public void NullRejectionCodeFails() =>
        Assert.Throws<ArgumentNullException>(() => FuzzResult.Rejected(null));
}
