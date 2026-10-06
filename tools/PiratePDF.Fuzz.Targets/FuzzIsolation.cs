namespace PiratePDF.Fuzz.Targets;

/// <summary>Required execution context; the neutral adapter does not start or supervise processes.</summary>
public enum FuzzIsolation
{
    /// <summary>Managed bounded work eligible for reuse inside a supervised worker process.</summary>
    SupervisedManagedWorker,

    /// <summary>Each input requires a fresh supervised process, including native or unproven bounds.</summary>
    DedicatedProcess
}
