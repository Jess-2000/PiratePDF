using System.Net;
using System.Net.Sockets;
using System.Text;

internal static class H2bProbe
{
    internal static async Task<int> RunAsync(string mode, string root)
    {
        if (!Path.IsPathFullyQualified(root)) return 64;
        switch (mode)
        {
            case "exit0": return 0;
            case "exit17": return 17;
            case "hang": await Task.Delay(TimeSpan.FromMinutes(10)); return 99;
            case "both":
                byte[] bytes = Encoding.ASCII.GetBytes(new string('X', 4096));
                await Task.WhenAll(Flood(Console.OpenStandardOutput(), bytes), Flood(Console.OpenStandardError(), bytes));
                return 0;
            case "allocate":
                var allocated = new List<byte[]>();
                try { for (int index = 0; index < 512; index++) { allocated.Add(new byte[1048576]); allocated[^1].AsSpan().Fill(1); } }
                catch (OutOfMemoryException) { Console.WriteLine("allocation-refused"); return 23; }
                GC.KeepAlive(allocated);
                return 24;
            case "full-volume":
                try
                {
                    await using var output = new FileStream(Path.Combine(root, "temp", "capacity-probe.bin"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    byte[] block = new byte[1048576];
                    for (int index = 0; index < 4097; index++) await output.WriteAsync(block);
                }
                catch (IOException failure) when ((failure.HResult & 0xffff) is 39 or 112) { Console.WriteLine("filesystem-capacity-refused"); return 25; }
                return 26;
            case "network":
                foreach (AddressFamily family in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
                {
                    using var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
                    try { socket.Bind(new IPEndPoint(family == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback, 0)); socket.Listen(1); return 27; }
                    catch (SocketException failure) when (failure.SocketErrorCode == SocketError.AccessDenied) { }
                }
                Console.WriteLine("socket-assignment-refused-v4-v6");
                return 0;
            case "access":
                await File.WriteAllTextAsync(Path.Combine(root, "temp", "owned-write.txt"), "controlled");
                return 0;
            case "child":
                var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
                foreach (string argument in new[] { typeof(H2bProbe).Assembly.Location, "h2b-probe", "hang", root }) start.ArgumentList.Add(argument);
                using (System.Diagnostics.Process child = System.Diagnostics.Process.Start(start)!)
                { Console.WriteLine("controlled-child=" + child.Id); await Task.Delay(1000); }
                return 0;
            default: return 64;
        }
    }

    private static async Task Flood(Stream stream, byte[] bytes)
    { for (int index = 0; index < 64; index++) await stream.WriteAsync(bytes); await stream.FlushAsync(); }
}
