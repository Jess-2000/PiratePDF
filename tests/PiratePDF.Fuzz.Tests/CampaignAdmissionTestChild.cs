using PiratePDF.Fuzz.Runner;
using System.Threading.Tasks;

namespace PiratePDF.Fuzz.Tests;

internal static class CampaignAdmissionTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var store = new CampaignTicketStore(args[1], args[2]);
            string binding = Path.Combine(args[1], "artifacts", "fuzz", "invocations", args[2], "child.json");
            if (args[4] != "direct")
                while (!File.Exists(binding)) await Task.Delay(10, deadline.Token);
            CampaignInvocationIdentity identity = await store.ConsumeAsync(args[3], deadline.Token);
            Console.WriteLine("admitted:" + identity.Digest());
            if (args[4] == "replay")
            {
                try { _ = await store.ConsumeAsync(args[3], deadline.Token); return 95; }
                catch (IOException) { Console.WriteLine("replay-rejected"); }
            }
            return 0;
        }
        catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException or
            System.Text.Json.JsonException or OperationCanceledException)
        {
            Console.WriteLine("admission-rejected");
            return 94;
        }
    }
}
