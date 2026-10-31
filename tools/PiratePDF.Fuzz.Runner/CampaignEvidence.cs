using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Stage a bounded ledger/summary together; one directory rename commits the immutable snapshot.</summary>
internal sealed class CampaignEvidence : IDisposable
{
    private readonly string root;
    private readonly string destination;
    private readonly CampaignTelemetryBudget budget;
    private FileStream? writer;
    private FileStream? lease;
    internal bool CleanupSucceeded { get; private set; } = true;
    internal bool CanPublish => writer is not null;
    private bool published;

    internal CampaignEvidence(string repository, string run, int maximum)
    {
        destination = Path.Combine(repository, "artifacts", "fuzz", "campaigns", run);
        // The leading dot cannot collide with an admitted run ID. Never reuse a previous partial invocation.
        root = Path.Combine(repository, "artifacts", "fuzz", "campaigns", "." + run + "-pending");
        budget = new(maximum);
    }

    internal void Open()
    {
        if (Path.Exists(root) || Path.Exists(destination)) throw new IOException("Campaign evidence already exists.");
        FindingFiles.CreateDirectory(root);
        lease = new FileStream(Path.Combine(root, "active.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        writer = new FileStream(Path.Combine(root, "ledger.pending"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        ValidateInventory();
    }

    internal bool Reserve() => budget.Reserve(CampaignTelemetryBudget.MaximumRecordBytes);

    internal void AppendCaseReserved(string record)
    {
        ValidateInventory();
        byte[] bytes = Encoding.UTF8.GetBytes(record);
        if (bytes.Length + 1 > CampaignTelemetryBudget.MaximumRecordBytes) throw new IOException("Campaign record ceiling reached.");
        writer!.Write(bytes);
        writer.WriteByte(10);
        writer.Flush();
    }

    internal void AppendReserved<T>(T record)
    {
        ValidateInventory();
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(record);
        if (bytes.Length + 1 > CampaignTelemetryBudget.MaximumRecordBytes) throw new IOException("Campaign record ceiling reached.");
        writer!.Write(bytes);
        writer.WriteByte(10);
        writer.Flush();
    }

    internal void Publish(CampaignResult result, CancellationToken deadline)
    {
        deadline.ThrowIfCancellationRequested();
        byte[] bytes = Encoding.UTF8.GetBytes(result.Json);
        if (bytes.Length + 1 > CampaignTelemetryBudget.MaximumRecordBytes ||
            !budget.ReserveTerminal(bytes.Length * 2 + 1)) throw new IOException("Campaign terminal ceiling reached.");
        ValidateInventory();
        writer!.Write(bytes);
        writer.WriteByte(10);
        writer.Flush();
        writer.Dispose();
        writer = null;
        File.Move(Path.Combine(root, "ledger.pending"), Path.Combine(root, "ledger.jsonl"));
        using (var summary = new FileStream(Path.Combine(root, "summary.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            summary.Write(bytes);
            summary.Flush();
        }
        // Finalize owned handles/lease before the commit; no cleanup may revise a committed terminal record.
        CloseLease();
        ValidateInventory();
        FindingFiles.CheckDirectory(Path.GetDirectoryName(destination)!, false);
        deadline.ThrowIfCancellationRequested();
        Directory.Move(root, destination);
        published = true;
    }

    private void ValidateInventory()
    {
        FindingFiles.CheckDirectory(root, false);
        int count = 0;
        foreach (string path in Directory.EnumerateFileSystemEntries(root))
        {
            if (++count > 3 || Path.GetFileName(path) is not ("active.lock" or "ledger.pending" or "ledger.jsonl" or "summary.json") ||
                (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new IOException("Invalid campaign evidence inventory.");
        }
    }

    public void Dispose()
    {
        if (published) return;
        try
        {
            try { writer?.Dispose(); }
            finally
            {
                writer = null;
                CloseLease();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FindingException)
        { CleanupSucceeded = false; }
    }

    private void CloseLease()
    {
        if (lease is null) return;
        // Validate first; foreign/reparse contents are preserved and reported, not deleted around.
        FileStream ownedLease = lease;
        lease = null;
        try { ValidateInventory(); }
        finally { ownedLease.Dispose(); }
        File.Delete(Path.Combine(root, "active.lock"));
    }
}
