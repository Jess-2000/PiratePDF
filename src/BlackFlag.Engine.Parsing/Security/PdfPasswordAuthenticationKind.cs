namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Identifies the credential that authenticated a Standard security handler.</summary>
public enum PdfPasswordAuthenticationKind
{
    /// <summary>The supplied credential did not authenticate.</summary>
    Failed,

    /// <summary>The supplied credential authenticated as the document user.</summary>
    User,

    /// <summary>The supplied credential authenticated as the document owner.</summary>
    Owner
}
