using System.Buffers;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Filters.Tests;

internal delegate void TestDecodeAction(
    ReadOnlySpan<byte> encodedData,
    IBufferWriter<byte> destination,
    PdfFilterDecodeContext context);

internal sealed class TestFilterDecoder : IPdfFilterDecoder
{
    private readonly TestDecodeAction decode;

    public TestFilterDecoder(
        string name,
        TestDecodeAction decode)
    {
        Name = new PdfName(name);
        this.decode = decode;
    }

    public PdfName Name { get; }

    public void Decode(
        ReadOnlySpan<byte> encodedData,
        IBufferWriter<byte> destination,
        PdfFilterDecodeContext context)
    {
        decode(
            encodedData,
            destination,
            context);
    }

    public static void Write(
        IBufferWriter<byte> destination,
        ReadOnlySpan<byte> data)
    {
        Span<byte> output =
            destination.GetSpan(
                data.Length);

        data.CopyTo(output);
        destination.Advance(
            data.Length);
    }
}

