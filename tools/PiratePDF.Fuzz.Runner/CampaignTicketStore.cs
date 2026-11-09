using System.Security.Cryptography;
using System.Text;

namespace PiratePDF.Fuzz.Runner;

/// <summary>One-use local Windows admission. Consumed and failed artifacts are retained, never recycled.</summary>
public sealed class CampaignTicketStore
{
    private readonly string repository;
    private readonly string directory;
    private readonly string run;

    public CampaignTicketStore(string repository, string run)
    {
        this.repository = FindingFiles.Root(repository);
        if (!FindingFiles.Id(run) || run is "pending" or "proposal" or "proposal-work")
            throw new ArgumentException("Invalid ticket run.");
        this.run = run;
        directory = Path.Combine(this.repository, "artifacts", "fuzz", "invocations", run);
    }

    public CampaignInvocationIdentity CreateIdentity(string revision, string target, CampaignConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        CampaignProcessIdentity parent = CampaignProcessIdentity.Current();
        var identity = new CampaignInvocationIdentity(run, revision, target, configuration.Cases, configuration.Duration.Ticks,
            configuration.ProcessedBytes, configuration.PoolInputs, configuration.PoolBytes, configuration.TelemetryBytes,
            RepositoryIdentity(repository), RunnerIdentity(), WorkerSupervisor.TargetIdentity,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), parent.ProcessId, parent.StartTicks);
        _ = identity.Encode();
        return identity;
    }

    public async Task IssueAsync(CampaignInvocationIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ValidateIdentity(identity);
        if (new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks) != CampaignProcessIdentity.Current())
            throw new ArgumentException("Only the issuing parent may publish a ticket.");
        cancellationToken.ThrowIfCancellationRequested();
        string pending = Path.Combine(Path.GetDirectoryName(directory)!, "." + run + "-ticket-pending");
        if (Path.Exists(directory) || Path.Exists(pending)) throw new IOException("Invocation storage already exists.");
        FindingFiles.CreateDirectory(pending);
        await FindingFiles.WriteNewAsync(Path.Combine(pending, "ticket.json"), identity.Encode(), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        FindingFiles.CheckDirectory(Path.GetDirectoryName(directory)!, false);
        Directory.Move(pending, directory);
    }

    public async Task BindChildAsync(CampaignInvocationIdentity identity, CampaignProcessIdentity child,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ValidateIdentity(identity);
        if (new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks) != CampaignProcessIdentity.Current() || !child.IsLive())
            throw new ArgumentException("Binding requires the issuing parent and exact live child.");
        Inventory(consumed: false, bindingRequired: false);
        var stored = CampaignInvocationIdentity.Decode((await InputFile.ReadAsync(Path.Combine(directory, "ticket.json"),
            CampaignInvocationIdentity.MaximumBytes, cancellationToken).ConfigureAwait(false)).Span);
        if (stored != identity) throw new ArgumentException("Ticket identity changed.");
        await FindingFiles.WriteNewAsync(Path.Combine(directory, "child.pending"), CampaignTicketBinding.Create(identity, child).Encode(),
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        FindingFiles.CheckDirectory(directory, false);
        File.Move(Path.Combine(directory, "child.pending"), Path.Combine(directory, "child.json"));
    }

    public async Task<CampaignInvocationIdentity> ConsumeAsync(string expectedHash, CancellationToken cancellationToken = default)
    {
        if (!FindingFiles.Hash(expectedHash)) throw new ArgumentException("Invalid invocation digest.");
        Inventory(consumed: false, bindingRequired: true);
        cancellationToken.ThrowIfCancellationRequested();
        // The non-overwriting same-directory rename arbitrates competing consumers. Invalid consumed tickets remain forensic evidence.
        File.Move(Path.Combine(directory, "ticket.json"), Path.Combine(directory, "consumed.json"));
        var identity = CampaignInvocationIdentity.Decode((await InputFile.ReadAsync(Path.Combine(directory, "consumed.json"),
            CampaignInvocationIdentity.MaximumBytes, cancellationToken).ConfigureAwait(false)).Span);
        ValidateIdentity(identity);
        if (identity.Digest() != expectedHash || !new CampaignProcessIdentity(identity.ParentId, identity.ParentStartTicks).IsLive())
            throw new ArgumentException("Stale or mismatched invocation.");
        _ = CampaignTicketBinding.Decode((await InputFile.ReadAsync(Path.Combine(directory, "child.json"),
            CampaignTicketBinding.MaximumBytes, cancellationToken).ConfigureAwait(false)).Span, identity, CampaignProcessIdentity.Current());
        Inventory(consumed: true, bindingRequired: true);
        return identity;
    }

    private void ValidateIdentity(CampaignInvocationIdentity identity)
    {
        _ = identity.Encode();
        if (identity.Run != run || identity.RepositoryHash != RepositoryIdentity(repository) ||
            identity.RunnerHash != RunnerIdentity() || identity.TargetsHash != WorkerSupervisor.TargetIdentity)
            throw new ArgumentException("Invocation workspace or build mismatch.");
    }

    private void Inventory(bool consumed, bool bindingRequired)
    {
        FindingFiles.CheckDirectory(directory, false);
        int count = 0;
        string ticket = consumed ? "consumed.json" : "ticket.json";
        bool foundTicket = false, foundBinding = false;
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            string name = Path.GetFileName(path);
            if (++count > 2 || (name != ticket && name != "child.json") ||
                (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new IOException("Unsafe invocation inventory.");
            foundTicket |= name == ticket;
            foundBinding |= name == "child.json";
        }
        if (!foundTicket || bindingRequired && !foundBinding) throw new IOException("Incomplete invocation admission.");
    }

    private static string RepositoryIdentity(string root) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(root).ToUpperInvariant())));

    private static string RunnerIdentity()
    {
        using FileStream stream = File.OpenRead(typeof(CampaignTicketStore).Assembly.Location);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
