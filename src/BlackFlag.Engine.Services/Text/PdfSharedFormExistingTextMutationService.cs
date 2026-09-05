using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Performs an explicitly authorized mutation of the authoritative Form
/// XObject stream. Because the source is shared, every placement is affected.
/// </summary>
public static class PdfSharedFormExistingTextMutationService
{
    public static PdfExistingTextEditOperation ReplaceStream(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        ReadOnlySpan<byte> replacementStreamData)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(descriptor);

        session.EnsureActive();
        session.Document.EnsureOwnership(displayedPage);

        if (source.SourceKind !=
                PdfExistingTextSourceKind.FormXObject ||
            !source.HasSharedSourceImpact)
        {
            throw new NotSupportedException(
                "This operation is reserved for a Form XObject explicitly classified as having shared-source impact.");
        }

        if (descriptor.Editability !=
            PdfExistingTextEditability.EditableWithSharedSourceImpact)
        {
            throw new NotSupportedException(
                descriptor.EditabilityReason ??
                "The selected existing text is not classified for shared-source Form editing.");
        }

        PdfExistingTextIdentity identity =
            descriptor.Identity;

        if (identity.SessionId != session.Id ||
            identity.PageId != displayedPage.Id ||
            identity.SourceKind != source.SourceKind ||
            identity.ContentOwnerIdentifier !=
                source.ContentOwnerIdentifier)
        {
            throw new InvalidOperationException(
                "The shared-Form text identity does not belong to the supplied editing context.");
        }

        if (source.GetContentStreamIdentifier(
                identity.StreamIndex) !=
            identity.ContentStreamIdentifier)
        {
            throw new InvalidOperationException(
                "The shared-Form target no longer maps to the supplied authoritative stream.");
        }

        return session.Document.ReplaceExistingTextStream(
            identity,
            replacementStreamData,
            allowFormXObjectSource: true);
    }
}
