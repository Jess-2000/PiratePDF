using System.ComponentModel;
using System.Diagnostics;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Exact process instance metadata; inspection failure is never proof of exit.</summary>
public readonly record struct CampaignProcessIdentity(int ProcessId, long StartTicks)
{
    public static CampaignProcessIdentity Current()
    {
        using Process process = Process.GetCurrentProcess();
        return Observe(process);
    }

    public static CampaignProcessIdentity Observe(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (process.HasExited) throw new InvalidOperationException("Process instance has exited.");
        return new(process.Id, process.StartTime.ToUniversalTime().Ticks);
    }

    public bool IsLive()
    {
        if (ProcessId <= 0 || StartTicks <= 0) return false;
        try
        {
            using Process process = Process.GetProcessById(ProcessId);
            return Observe(process) == this;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or Win32Exception or NotSupportedException)
        { return false; }
    }
}
