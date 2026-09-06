using System.Collections.ObjectModel;

namespace BlackFlag.Engine.SDK.Editing;

/// <summary>SDK-owned immutable description of one registered existing-text target.</summary>
public sealed class PdfExistingTextSdkTarget
{
    private readonly ReadOnlyCollection<string> textSegments;

    public PdfExistingTextSdkTarget(
        Guid id,
        Guid pageId,
        Guid sourceId,
        PdfExistingTextSdkSourceKind sourceKind,
        IEnumerable<string> textSegments,
        string? fontResourceName,
        double fontSize,
        PdfExistingTextSdkTypography typography,
        PdfExistingTextSdkEditability editability,
        string? editabilityReason)
    {
        if (id == Guid.Empty) throw new ArgumentException("A target id is required.", nameof(id));
        if (pageId == Guid.Empty) throw new ArgumentException("A page id is required.", nameof(pageId));
        if (sourceId == Guid.Empty) throw new ArgumentException("A source id is required.", nameof(sourceId));
        ArgumentNullException.ThrowIfNull(textSegments);
        ArgumentNullException.ThrowIfNull(typography);

        Id = id;
        PageId = pageId;
        SourceId = sourceId;
        SourceKind = sourceKind;
        this.textSegments = Array.AsReadOnly(textSegments.ToArray());
        FontResourceName = fontResourceName;
        FontSize = fontSize;
        Typography = typography;
        Editability = editability;
        EditabilityReason = editabilityReason;
    }

    public Guid Id { get; }
    public Guid PageId { get; }
    public Guid SourceId { get; }
    public PdfExistingTextSdkSourceKind SourceKind { get; }
    public IReadOnlyList<string> TextSegments => textSegments;
    public string Text => string.Concat(textSegments);
    public string? FontResourceName { get; }
    public double FontSize { get; }
    public PdfExistingTextSdkTypography Typography { get; }
    public PdfExistingTextSdkEditability Editability { get; }
    public string? EditabilityReason { get; }
}
