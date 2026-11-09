using System.Buffers.Binary;
using System.IO.Pipes;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;

namespace PiratePDF.Fuzz.Tests;

// Ordinary local pipes exercise real framing/cancellation, not privileged peer
// admission or Windows USER objects. No station/desktop APIs are invoked.
public sealed class H2bStationTransportTests
{
    private const string Id="00112233445566778899aabbccddeeff";
    private static async Task<(NamedPipeServerStream Server, NamedPipeClientStream Client)> ConnectAsync()
    {
        string name="pph2b-frame-test-"+Guid.NewGuid().ToString("N");
        var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
        var client=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous);
        try
        {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Task.WhenAll(server.WaitForConnectionAsync(deadline.Token),client.ConnectAsync(deadline.Token));
            return(server,client);
        }
        catch { server.Dispose(); client.Dispose(); throw; }
    }
    private static Task Receive(object transport, CancellationToken token) => (Task)transport.GetType().GetMethod("ReceiveAsync",BindingFlags.Instance|BindingFlags.NonPublic)!
        .Invoke(transport,[H2bTestChild.New("StationAuthorization",1,"StationLeaseOnlyV1",Id,"0123abcd","station-lease-v1",0x20002u,0x20081u,new string('a',64),
            DateTimeOffset.UtcNow,DateTimeOffset.UtcNow.AddSeconds(20),null!,null!),"Request",token])!;

    [Fact]
    public async Task RealFramingReassemblesFragmentedRequest()
    {
        var pair=await ConnectAsync(); using var server=pair.Server; using var client=pair.Client;
        using var transport=(IDisposable)H2bTestChild.New("NativeStationTransport",server);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        byte[] body=System.Text.Encoding.UTF8.GetBytes("{\"Version\":1,\"Operation\":\"StationLeaseOnlyV1\",\"Id\":\""+Id+"\",\"Kind\":\"Request\",\"State\":\"\"}");
        byte[] prefix=new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(prefix,body.Length);
        Task receive=Receive(transport,deadline.Token);
        foreach(byte value in prefix.Concat(body)) await client.WriteAsync(new byte[] { value },deadline.Token);
        await receive;
    }
    [Fact]
    public async Task RealFramingRejectsOversizedPrefixBeforeReadingPayload()
    {
        var pair=await ConnectAsync(); using var server=pair.Server; using var client=pair.Client;
        using var transport=(IDisposable)H2bTestChild.New("NativeStationTransport",server);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Task receive=Receive(transport,deadline.Token);
        byte[] prefix=new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(prefix,4097); await client.WriteAsync(prefix,deadline.Token);
        await Assert.ThrowsAsync<SecurityException>(()=>receive);
    }
    [Fact]
    public async Task RealFramingRejectsDisconnectDuringPayload()
    {
        var pair=await ConnectAsync(); using var server=pair.Server; using var client=pair.Client;
        using var transport=(IDisposable)H2bTestChild.New("NativeStationTransport",server);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Task receive=Receive(transport,deadline.Token);
        byte[] prefix=new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(prefix,100); await client.WriteAsync(prefix,deadline.Token);
        await client.WriteAsync(new byte[] { 123,125 },deadline.Token); client.Dispose();
        await Assert.ThrowsAsync<EndOfStreamException>(()=>receive);
    }
    [Fact]
    public async Task RealFramingHonorsCancellationWhileAwaitingPeer()
    {
        var pair=await ConnectAsync(); using var server=pair.Server; using var client=pair.Client;
        using var transport=(IDisposable)H2bTestChild.New("NativeStationTransport",server);
        using var cancellation=new CancellationTokenSource(); Task receive=Receive(transport,cancellation.Token); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>receive);
    }
    [Theory]
    [InlineData("StationCollision",183,false)]
    [InlineData("SecondDuplicationFailure",5,true)]
    [InlineData("UNKNOWN/BLOCKED:CleanupFailure",0,true)]
    public void PrePipeExitEvidenceRetainsFailureAndOwnership(string stage,int error,bool unknown)
    {
        int code=(int)H2bTestChild.Call("StationBootstrapInterop","FailureExit",stage,error,unknown)!;
        string[] outcome=(string[])H2bTestChild.Call("StationBootstrapInterop","DecodeFailure",unchecked((uint)code))!;
        Assert.Equal(error.ToString(System.Globalization.CultureInfo.InvariantCulture),outcome[1]);
        Assert.Equal(unknown?"UNKNOWN/BLOCKED":"Refused",outcome[2]);
        Assert.Equal(stage.StartsWith("UNKNOWN",StringComparison.Ordinal)?"HelperCleanupFailure":stage,outcome[0]);
    }
}
