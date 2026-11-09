using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

internal static class CampaignGateTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args[0] == "gate-root")
        {
            var start = new ProcessStartInfo(ReplayFixture.Host) { UseShellExecute = false, CreateNoWindow = true };
            foreach (string argument in new[] { typeof(SupervisorTestChild).Assembly.Location, "gate-worker", args[1], args[2], args[3], args[4] })
                start.ArgumentList.Add(argument);
            using Process worker = Process.Start(start) ?? throw new InvalidOperationException("Controlled worker start failed.");
            CampaignProcessIdentity stamp = CampaignProcessIdentity.Observe(worker);
            Console.WriteLine(FormattableString.Invariant($"{stamp.ProcessId}:{stamp.StartTicks}"));
            return 0;
        }
        using var deadline = new CancellationTokenSource(args[4] == "short" ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromSeconds(5));
        int caseIndex = args[4] == "wrong-index" ? 1 : int.Parse(args[3], CultureInfo.InvariantCulture);
        if (!await CampaignWorkerGate.AwaitAcknowledgementAsync(args[1], args[2], caseIndex, deadline.Token))
            return 94;
        // Represents the first possible input access, after the real cross-process ACK boundary.
        Console.WriteLine("executed");
        if (args[4] == "hold") await Task.Delay(TimeSpan.FromSeconds(60));
        return 0;
    }
}
