
namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Describes how directly a recovered document structure was established.</summary>
public enum PdfRecoveryConfidence
{
    /// <summary>Recovery corrected a narrowly identifiable structural defect.</summary>
    Exact,

    /// <summary>Recovered objects and trailer form a coherent document structure.</summary>
    High,

    /// <summary>Useful structure was recovered but some information remains uncertain.</summary>
    Partial
}
