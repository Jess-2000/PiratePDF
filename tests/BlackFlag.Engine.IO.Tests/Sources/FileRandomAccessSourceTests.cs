using BlackFlag.Engine.IO.Sources;

namespace BlackFlag.Engine.IO.Tests.Sources;

public sealed class FileRandomAccessSourceTests : IDisposable
{
    private readonly string temporaryDirectory;

    public FileRandomAccessSourceTests()
    {
        temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "BlackFlag.Engine.IO.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void ConstructorWithExistingFileSetsProperties()
    {
        var filePath = CreateFile([10, 20, 30, 40]);

        using var source = new FileRandomAccessSource(filePath);

        Assert.Equal(4, source.Length);
        Assert.Equal(Path.GetFullPath(filePath), source.FilePath);
    }

    [Fact]
    public void ConstructorWithMissingFileThrows()
    {
        var filePath = Path.Combine(
            temporaryDirectory,
            "missing.pdf");

        var exception = Assert.Throws<FileNotFoundException>(
            () => new FileRandomAccessSource(filePath));

        Assert.Equal(
            Path.GetFullPath(filePath),
            exception.FileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorWithInvalidPathThrows(string? filePath)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new FileRandomAccessSource(filePath!));
    }

    [Fact]
    public void ReadFromBeginningReturnsRequestedBytes()
    {
        var filePath = CreateFile([10, 20, 30, 40, 50]);
        using var source = new FileRandomAccessSource(filePath);

        Span<byte> destination = stackalloc byte[3];

        var bytesRead = source.Read(
            offset: 0,
            destination);

        Assert.Equal(3, bytesRead);
        Assert.Equal(
            new byte[] { 10, 20, 30 },
            destination.ToArray());
    }

    [Fact]
    public void ReadFromMiddleUsesAbsoluteOffset()
    {
        var filePath = CreateFile([10, 20, 30, 40, 50]);
        using var source = new FileRandomAccessSource(filePath);

        Span<byte> destination = stackalloc byte[2];

        var bytesRead = source.Read(
            offset: 2,
            destination);

        Assert.Equal(2, bytesRead);
        Assert.Equal(
            new byte[] { 30, 40 },
            destination.ToArray());
    }

    [Fact]
    public void ReadNearEndReturnsPartialResult()
    {
        var filePath = CreateFile([10, 20, 30, 40, 50]);
        using var source = new FileRandomAccessSource(filePath);

        Span<byte> destination = stackalloc byte[4];

        var bytesRead = source.Read(
            offset: 3,
            destination);

        Assert.Equal(2, bytesRead);
        Assert.Equal(40, destination[0]);
        Assert.Equal(50, destination[1]);
    }

    [Fact]
    public void ReadAtEndReturnsZero()
    {
        var filePath = CreateFile([10, 20, 30]);
        using var source = new FileRandomAccessSource(filePath);

        Span<byte> destination = stackalloc byte[4];

        var bytesRead = source.Read(
            source.Length,
            destination);

        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void ReadBeyondEndReturnsZero()
    {
        var filePath = CreateFile([10, 20, 30]);
        using var source = new FileRandomAccessSource(filePath);

        Span<byte> destination = stackalloc byte[4];

        var bytesRead = source.Read(
            source.Length + 100,
            destination);

        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void ReadWithEmptyDestinationReturnsZero()
    {
        var filePath = CreateFile([10, 20, 30]);
        using var source = new FileRandomAccessSource(filePath);

        var bytesRead = source.Read(
            offset: 0,
            Span<byte>.Empty);

        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public async Task ReadWithNegativeOffsetThrows()
    {
        var filePath = CreateFile([10, 20, 30]);
        using var source = new FileRandomAccessSource(filePath);

        using var cancellationSource = new CancellationTokenSource();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () =>
            {
                await source.ReadAsync(
                    offset: -1,
                    new byte[2],
                    cancellationSource.Token);
            });
    }

    [Fact]
    public async Task ReadAsyncReturnsRequestedBytes()
    {
        var filePath = CreateFile([10, 20, 30, 40, 50]);
        await using var source =
            new FileRandomAccessSource(filePath);

        var destination = new byte[3];

        var bytesRead = await source.ReadAsync(
            offset: 1,
            destination);

        Assert.Equal(3, bytesRead);
        Assert.Equal(
            new byte[] { 20, 30, 40 },
            destination);
    }

    [Fact]
    public async Task ReadAsyncWithCancelledTokenThrows()
    {
        var filePath = CreateFile([10, 20, 30]);
        await using var source =
            new FileRandomAccessSource(filePath);

        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
            {
                await source.ReadAsync(
                    offset: 0,
                    new byte[2],
                    cancellationSource.Token);
            });
    }

    [Fact]
    public void ReadAfterDisposeThrows()
    {
        var filePath = CreateFile([10, 20, 30]);
        var source = new FileRandomAccessSource(filePath);

        source.Dispose();

        var destination = new byte[1];

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                source.Read(
                    offset: 0,
                    destination);
            });
    }

    [Fact]
    public async Task ReadAsyncAfterDisposeThrows()
    {
        var filePath = CreateFile([10, 20, 30]);
        var source = new FileRandomAccessSource(filePath);

        await source.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () =>
            {
                await source.ReadAsync(
                    offset: 0,
                    new byte[1]);
            });
    }

    [Fact]
    public async Task ConcurrentReadsReturnIndependentResults()
    {
        var bytes = Enumerable
            .Range(0, 200)
            .Select(value => (byte)value)
            .ToArray();

        var filePath = CreateFile(bytes);
        await using var source =
            new FileRandomAccessSource(filePath);

        var firstBuffer = new byte[20];
        var secondBuffer = new byte[20];
        var thirdBuffer = new byte[20];

        var firstRead = source
            .ReadAsync(0, firstBuffer)
            .AsTask();

        var secondRead = source
            .ReadAsync(50, secondBuffer)
            .AsTask();

        var thirdRead = source
            .ReadAsync(100, thirdBuffer)
            .AsTask();

        await Task.WhenAll(
            firstRead,
            secondRead,
            thirdRead);

        Assert.Equal(
            bytes[0..20],
            firstBuffer);

        Assert.Equal(
            bytes[50..70],
            secondBuffer);

        Assert.Equal(
            bytes[100..120],
            thirdBuffer);
    }

    [Fact]
    public void DisposeCanBeCalledMoreThanOnce()
    {
        var filePath = CreateFile([10, 20, 30]);
        var source = new FileRandomAccessSource(filePath);

        source.Dispose();
        source.Dispose();
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(
                temporaryDirectory,
                recursive: true);
        }
    }

    private string CreateFile(ReadOnlySpan<byte> contents)
    {
        var filePath = Path.Combine(
            temporaryDirectory,
            $"{Guid.NewGuid():N}.bin");

        File.WriteAllBytes(
            filePath,
            contents.ToArray());

        return filePath;
    }
}