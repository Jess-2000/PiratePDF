using System.Collections.ObjectModel;
using BlackFlag.Engine.Parsing.CrossReferences;

namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Stores the effective newest-revision location for each PDF object number.
/// </summary>
public sealed class PdfObjectLocationTable
{
    private readonly ReadOnlyDictionary<int, PdfObjectLocation> locations;

    /// <summary>
    /// Builds a newest-wins table from revisions ordered oldest to newest.
    /// </summary>
    public PdfObjectLocationTable(
        IEnumerable<IEnumerable<PdfObjectLocation>> revisions)
    {
        ArgumentNullException.ThrowIfNull(revisions);

        var effective = new Dictionary<int, PdfObjectLocation>();
        bool hasRevision = false;

        foreach (IEnumerable<PdfObjectLocation> revision in revisions)
        {
            ArgumentNullException.ThrowIfNull(revision);
            hasRevision = true;

            foreach (PdfObjectLocation location in revision)
            {
                ArgumentNullException.ThrowIfNull(location);
                effective[location.ObjectId.ObjectNumber] = location;
            }
        }

        if (!hasRevision)
        {
            throw new ArgumentException(
                "At least one cross-reference revision is required.",
                nameof(revisions));
        }

        locations = new ReadOnlyDictionary<int, PdfObjectLocation>(effective);
    }

    /// <summary>Gets the effective locations by object number.</summary>
    public IReadOnlyDictionary<int, PdfObjectLocation> Locations => locations;

    /// <summary>
    /// Tries to obtain the effective location for an exact object identifier.
    /// </summary>
    public bool TryGetLocation(
        PdfObjectId objectId,
        out PdfObjectLocation? location)
    {
        if (!locations.TryGetValue(objectId.ObjectNumber, out location))
        {
            return false;
        }

        if (location.ObjectId != objectId)
        {
            location = null;
            return false;
        }

        return true;
    }
}
