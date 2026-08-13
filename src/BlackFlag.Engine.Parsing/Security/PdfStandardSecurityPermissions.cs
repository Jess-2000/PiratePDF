namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Exposes the Standard security-handler permission flags after authentication.</summary>
public sealed class PdfStandardSecurityPermissions
{
    internal PdfStandardSecurityPermissions(int rawValue, bool ownerAuthenticated)
    {
        RawValue = rawValue;
        OwnerAuthenticated = ownerAuthenticated;
    }

    /// <summary>Gets the raw signed /P permission value.</summary>
    public int RawValue { get; }

    /// <summary>Gets whether the owner credential authenticated and therefore overrides user restrictions.</summary>
    public bool OwnerAuthenticated { get; }

    /// <summary>Gets whether printing is permitted.</summary>
    public bool CanPrint => OwnerAuthenticated || HasBit(3);

    /// <summary>Gets whether document contents may be modified.</summary>
    public bool CanModify => OwnerAuthenticated || HasBit(4);

    /// <summary>Gets whether text and graphics may be copied or extracted.</summary>
    public bool CanCopy => OwnerAuthenticated || HasBit(5);

    /// <summary>Gets whether annotations and interactive form fields may be added or modified.</summary>
    public bool CanModifyAnnotations => OwnerAuthenticated || HasBit(6);

    /// <summary>Gets whether form fields may be filled in.</summary>
    public bool CanFillForms => OwnerAuthenticated || HasBit(9);

    /// <summary>Gets whether content may be extracted for accessibility.</summary>
    public bool CanExtractForAccessibility => OwnerAuthenticated || HasBit(10);

    /// <summary>Gets whether the document may be assembled.</summary>
    public bool CanAssemble => OwnerAuthenticated || HasBit(11);

    /// <summary>Gets whether high-quality printing is permitted.</summary>
    public bool CanPrintHighQuality => OwnerAuthenticated || HasBit(12);

    private bool HasBit(int bitPosition) =>
        (unchecked((uint)RawValue) & (1u << (bitPosition - 1))) != 0;
}
