using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Pages;

/// <summary>
/// Represents a stable editing-session handle for one high-level PDF page.
/// </summary>
/// <remarks>
/// The handle does not expose the mutable source <see cref="PdfPage"/>. Later
/// mutation slices operate through editing operations so page-tree changes
/// cannot bypass ownership, validation, or transaction boundaries.
/// </remarks>
public sealed class PdfEditablePage
{
    private readonly PdfPage page;

    internal PdfEditablePage(
        PdfPage page,
        PdfEditingOwnershipToken owner,
        PdfEditablePageOrigin origin,
        int? sourceIndex)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!Enum.IsDefined(origin))
        {
            throw new ArgumentOutOfRangeException(
                nameof(origin),
                origin,
                "The editable-page origin is not supported.");
        }

        if (sourceIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceIndex));
        }

        if (
            origin == PdfEditablePageOrigin.SourceDocument &&
            sourceIndex is null)
        {
            throw new ArgumentException(
                "A source-document page requires its opening page index.",
                nameof(sourceIndex));
        }

        if (
            origin == PdfEditablePageOrigin.SessionCreated &&
            sourceIndex is not null)
        {
            throw new ArgumentException(
                "A session-created page cannot have a source-document index.",
                nameof(sourceIndex));
        }

        this.page = page;
        Owner = owner;
        Origin = origin;
        SourceIndex = sourceIndex;
        Annotations = new PdfEditableAnnotationCollection(page, owner);
    }

    /// <summary>
    /// Gets the stable high-level page identity.
    /// </summary>
    public Guid Id =>
        page.Id;

    /// <summary>
    /// Gets the width of the page in PDF user-space units.
    /// </summary>
    public double Width =>
        page.Width;

    /// <summary>
    /// Gets the height of the page in PDF user-space units.
    /// </summary>
    public double Height =>
        page.Height;

    /// <summary>
    /// Gets the current high-level page rotation.
    /// </summary>
    public PdfPageRotation Rotation =>
        page.Rotation;

    /// <summary>
    /// Gets whether the current page orientation is landscape.
    /// </summary>
    public bool IsLandscape =>
        page.IsLandscape;

    /// <summary>
    /// Gets whether the page currently owns content-stream data.
    /// </summary>
    public bool HasContent =>
        page.HasContent;

    /// <summary>
    /// Gets whether the page currently owns any direct resource entries.
    /// </summary>
    public bool HasResources =>
        page.HasResources;

    /// <summary>Gets the editable annotations currently attached to this page.</summary>
    public PdfEditableAnnotationCollection Annotations { get; }

    /// <summary>
    /// Returns a defensive copy of the current page content-stream bytes.
    /// </summary>
    public byte[] GetContentBytes() =>
        page.GetContentBytes();

    /// <summary>
    /// Determines whether a named resource exists in a direct page resource
    /// category.
    /// </summary>
    public bool ContainsResource(
        PdfName category,
        PdfName name)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        return page.ContainsResource(
            category,
            name);
    }

    /// <summary>
    /// Gets how the page entered the editing session.
    /// </summary>
    public PdfEditablePageOrigin Origin { get; }

    /// <summary>
    /// Gets the page index captured when the editing session opened, or
    /// <see langword="null"/> for a page created by the session.
    /// </summary>
    public int? SourceIndex { get; }

    /// <summary>
    /// Gets whether this page belonged to the source document when the editing
    /// session opened.
    /// </summary>
    public bool IsSourcePage =>
        Origin == PdfEditablePageOrigin.SourceDocument;


    /// <summary>Gets the effective state of a standard page boundary box.</summary>
    public PdfStructuralValueState<PdfPageBox> GetBoxState(
        PdfPageBoxKind kind)
    {
        return MapState(page.GetBox(kind));
    }

    /// <summary>Gets the effective state of the page rotation.</summary>
    public PdfStructuralValueState<PdfPageRotation> RotationState =>
        MapState(page.GetRotationState());

    private static PdfStructuralValueState<T> MapState<T>(
        PdfEffectiveValue<T> state)
        where T : notnull
    {
        return state.Origin switch
        {
            PdfPageValueOrigin.Explicit => PdfStructuralValueState.Explicit(state.Value),
            PdfPageValueOrigin.Inherited => PdfStructuralValueState.Inherited(state.Value),
            PdfPageValueOrigin.Defaulted => PdfStructuralValueState.Defaulted(state.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state.Origin, "The page-value origin is not supported.")
        };
    }

    internal PdfEditingOwnershipToken Owner { get; }

    internal PdfObjectIdentifier? ObjectIdentifier =>
        page.ObjectIdentifier;

    internal void BindObjectIdentifier(
        PdfObjectIdentifier identifier)
    {
        page.BindObjectIdentifier(identifier);
    }

    internal PdfDictionary CreateDictionarySnapshot() =>
        page.CreateDictionarySnapshot();

    internal PdfPage GetSourcePage() =>
        page;
}
