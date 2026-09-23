using System;
using System.IO;
using System.Threading;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed class PdfFontCatalogBuildBudgetTests
{
    [Fact]
    public void LongMaximumBudgetRejectsOverflowWithoutLargeAllocation()
    {
        var budget = new PdfFontCatalogBuildBudget(new PdfExternalFontCatalogLimits { MaximumSourceBytes = long.MaxValue });
        budget.RecordRead(long.MaxValue - 1);
        Assert.Equal(1, budget.RemainingSourceBytes);
        budget.RecordRead(1);
        Assert.Equal(0, budget.RemainingSourceBytes);
        budget.RecordRead(0);
        var failure = Assert.Throws<PdfExternalFontCatalogLimitException>(() => budget.RecordRead(1));
        Assert.Equal(long.MaxValue, failure.Limit);
        Assert.Same(failure, Assert.Throws<PdfExternalFontCatalogLimitException>(budget.ThrowIfExceeded));
        Assert.Equal(0, budget.RemainingSourceBytes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ActualReadEnforcesBudgetDespiteUnderreportedLength(int bytes)
    {
        var budget = new PdfFontCatalogBuildBudget(new PdfExternalFontCatalogLimits { MaximumSourceBytes = 2 });
        using var source = new UnderreportedStream(new byte[bytes]);
        budget.CheckSourceLength(source.Length);
        using var stream = new PdfFontCatalogReadStream(source, budget, default);
        byte[] result = new byte[4];
        if (bytes <= 2)
        {
            Assert.Equal(bytes, stream.Read(result));
            Assert.Equal(-1, stream.ReadByte());
            Assert.Equal(2 - bytes, budget.RemainingSourceBytes);
        }
        else
        {
            Assert.Throws<PdfExternalFontCatalogLimitException>(() => stream.Read(result));
            Assert.Equal(3, source.Position); // At most one undelivered EOF-probe byte.
            Assert.Throws<PdfExternalFontCatalogLimitException>(() => stream.ReadByte());
            Assert.Equal(3, source.Position);
        }
    }

    [Fact]
    public void SeekingAndArrayReadsCannotBypassAggregateAccounting()
    {
        var budget = new PdfFontCatalogBuildBudget(new PdfExternalFontCatalogLimits { MaximumSourceBytes = 2 });
        using var source = new MemoryStream([1, 2]);
        using var stream = new PdfFontCatalogReadStream(source, budget, default);
        byte[] result = new byte[2];
        Assert.Equal(2, stream.Read(result, 0, 2));
        Assert.Equal(0, stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<PdfExternalFontCatalogLimitException>(() => stream.ReadByte());
    }

    [Fact]
    public void ReadChecksCancellationBeforeAndAfterUnderlyingIo()
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new PdfFontCatalogBuildBudget(new PdfExternalFontCatalogLimits());
        using var source = new CancellingStream(cancellation);
        using var stream = new PdfFontCatalogReadStream(source, budget, cancellation.Token);
        Assert.Equal(cancellation.Token,
            Assert.Throws<OperationCanceledException>(() => stream.ReadByte()).CancellationToken);
        Assert.Equal(1, source.Position);
        Assert.Throws<OperationCanceledException>(() => stream.ReadByte());
        Assert.Equal(1, source.Position);
    }

    private sealed class UnderreportedStream(byte[] data) : MemoryStream(data)
    {
        public override long Length => 0;
    }

    private sealed class CancellingStream(CancellationTokenSource cancellation) : MemoryStream(new byte[2])
    {
        public override int Read(Span<byte> buffer)
        {
            int read = base.Read(buffer);
            cancellation.Cancel();
            return read;
        }
    }
}
