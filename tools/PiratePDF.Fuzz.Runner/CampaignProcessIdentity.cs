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
        // Linux Process.StartTime is reconstructed from boot-time clocks and can
        // differ by milliseconds between observers. /proc stat field 22 is
        // the kernel's stable, per-process start tick count.
        if (OperatingSystem.IsLinux())
        {
            string stat = File.ReadAllText($"/proc/{process.Id}/stat");
            int end = stat.LastIndexOf(')');
            if (end < 0) throw new IOException("Invalid Linux process stat.");
            string[] fields = stat[(end + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length <= 19 || !long.TryParse(fields[19], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out long start) || start <= 0)
                throw new IOException("Invalid Linux process start ticks.");
            return new(process.Id, start);
        }
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
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or Win32Exception or NotSupportedException or IOException or UnauthorizedAccessException)
        { return false; }
    }
}
