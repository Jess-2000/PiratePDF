
namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Controls whether structural recovery may be attempted.</summary>
public enum PdfRecoveryMode
{
    /// <summary>Do not attempt structural recovery.</summary>
    None,

    /// <summary>Use bounded structural recovery after normal xref loading fails.</summary>
    Automatic,

    /// <summary>Allow deeper bounded recovery heuristics for damaged documents.</summary>
    Aggressive
}
