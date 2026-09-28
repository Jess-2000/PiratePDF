using Xunit;

namespace BlackFlag.Engine.Objects.Tests;

public sealed class PdfHexStringTests
{
    [Fact]
    public void ConstructorStoresBytesAndReportsType()
    {
        byte[] source = [0x00, 0x7F, 0x80, 0xFF];

        var value = new PdfHexString(source);

        Assert.Equal(source, value.Value.ToArray());
        Assert.Equal(PdfObjectType.HexString, value.Type);
    }

    [Fact]
    public void ConstructorTakesDefensiveCopy()
    {
        byte[] source = [1, 2, 3];
        var value = new PdfHexString(source);

        source[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, value.Value.ToArray());
    }

    [Fact]
    public void ExposedMemoryCannotReplaceInternalStorage()
    {
        byte[] snapshot = new PdfHexString(
            new byte[] { 1, 2, 3 }).Value.ToArray();

        snapshot[0] = 99;

        Assert.Equal(99, snapshot[0]);
    }

    [Fact]
    public void EqualityUsesByteSequence()
    {
        var left = new PdfHexString(new byte[] { 1, 2, 3 });
        var equal = new PdfHexString(new byte[] { 1, 2, 3 });
        var different = new PdfHexString(new byte[] { 1, 2, 4 });

        Assert.Equal(left, equal);
        Assert.True(left == equal);
        Assert.NotEqual(left, different);
        Assert.False(left == different);
        Assert.Equal(left.GetHashCode(), equal.GetHashCode());
    }

    [Fact]
    public void EmptySequenceIsSupported()
    {
        var value = new PdfHexString(ReadOnlyMemory<byte>.Empty);

        Assert.True(value.Value.IsEmpty);
    }

    [Fact]
    public void AcceptDispatchesToHexStringOverload()
    {
        var visitor = new RecordingVisitor();

        Assert.Equal(
            "HexString",
            new PdfHexString(new byte[] { 0xAB }).Accept(visitor));
    }

    private sealed class RecordingVisitor : PdfObjectVisitor<string>
    {
        public override string Visit(PdfObject value) => "Object";

        public override string Visit(PdfHexString value) => "HexString";
    }
}
