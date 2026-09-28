using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class DeterministicMutationTests
{
    [Theory]
    [InlineData(0, "3C")]
    [InlineData(1, "6D")]
    [InlineData(2, "E83E")]
    [InlineData(3, "")]
    [InlineData(4, "7F")]
    [InlineData(5, "")]
    [InlineData(6, "3E4A4A4A4A4A4A")]
    public void VersionOneMatchesIndependentlyCalculatedVectors(int index, string hex) =>
        Assert.Equal(hex, Convert.ToHexString(DeterministicMutation.Generate("filter-asciihex", "a-success", 1, index, [62])));

    [Theory]
    [InlineData("filter-asciihex", 0)]
    [InlineData("filter-asciihex", 1)]
    [InlineData("filter-asciihex", 64)]
    [InlineData("filter-asciihex", 16384)]
    [InlineData("filter-flate", 16384)]
    [InlineData("font-sanitize", 65536)]
    public void EveryOperatorIsRepeatableBoundedAndOwnsItsOutput(string target, int length)
    {
        byte[] source = Enumerable.Range(0, length).Select(value => (byte)(value % 251)).ToArray();
        byte[] before = [.. source];
        for (int index = 0; index < DeterministicMutation.MutationsPerSeed; index++)
        {
            byte[] first = DeterministicMutation.Generate(target, "owned", 1, index, source);
            byte[] second = DeterministicMutation.Generate(target, "owned", 1, index, source);
            Assert.Equal(first, second);
            Assert.NotSame(source, first);
            Assert.NotSame(first, second);
            Assert.InRange(first.Length, 0, TargetRegistry.Get(target).MaximumInputBytes);
            Assert.Equal(before, source);
        }
    }

    [Fact]
    public void OperatorMappingAndStructuralEffectsAreFixed()
    {
        Assert.Equal("deterministic-v1", DeterministicMutation.Algorithm);
        Assert.Equal(1049, DeterministicMutation.FixedSeed);
        Assert.Equal(7, DeterministicMutation.MutationsPerSeed);
        Assert.Equal(new[] { MutationOperator.BitFlip, MutationOperator.Replace, MutationOperator.Insert,
            MutationOperator.Delete, MutationOperator.Overwrite, MutationOperator.Truncate, MutationOperator.Extend },
            Enumerable.Range(0, 7).Select(DeterministicMutation.Operator));
        byte[] source = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        byte[][] cases = Enumerable.Range(0, 7).Select(index => Generate(source, index)).ToArray();
        Assert.Equal(1, cases[0].Zip(source).Count(pair => pair.First != pair.Second));
        int flipped = cases[0].Zip(source).Where(pair => pair.First != pair.Second).Single().First ^
            cases[0].Zip(source).Where(pair => pair.First != pair.Second).Single().Second;
        Assert.Equal(0, flipped & (flipped - 1));
        Assert.Equal(1, cases[1].Zip(source).Count(pair => pair.First != pair.Second));
        Assert.Equal(33, cases[2].Length);
        Assert.InRange(cases[3].Length, 24, 31);
        Assert.Equal(32, cases[4].Length);
        Assert.InRange(cases[5].Length, 0, 31);
        Assert.Equal(source.Take(cases[5].Length), cases[5]);
        Assert.InRange(cases[6].Length, 33, 40);
        Assert.Equal(source, cases[6].Take(32));
    }

    [Fact]
    public void EmptyAndOneByteRulesAreExplicit()
    {
        for (int index = 0; index < 7; index++)
        {
            byte[] empty = Generate([], index);
            if (index == 6) Assert.InRange(empty.Length, 1, 8);
            else Assert.Equal(index is 3 or 5 ? 0 : 1, empty.Length);
            byte[] one = Generate([62], index);
            if (index is 3 or 5) Assert.Empty(one);
            else Assert.NotEmpty(one);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void GrowthAtCeilingFallsBackToOneByteReplacement(int index)
    {
        byte[] source = new byte[TargetRegistry.Get("filter-asciihex").MaximumInputBytes];
        byte[] result = Generate(source, index);
        Assert.Equal(source.Length, result.Length);
        Assert.Equal(1, result.Zip(source).Count(pair => pair.First != pair.Second));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void IndexCannotExtendTheAlgorithm(int index) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate([], index));

    [Fact]
    public void InputAndIdentityAdmissionPrecedesAllocation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(new byte[16385], 0));
        Assert.Throws<ArgumentException>(() => DeterministicMutation.Generate("unknown", "owned", 1, 0, []));
        Assert.Throws<ArgumentException>(() => DeterministicMutation.Generate("filter-asciihex", "owned", 2, 0, []));
        Assert.Throws<ArgumentException>(() => DeterministicMutation.Generate("filter-asciihex", "../PRIVATE", 1, 0, []));
    }

    private static byte[] Generate(byte[] source, int index) => DeterministicMutation.Generate("filter-asciihex", "owned", 1, index, source);
}
