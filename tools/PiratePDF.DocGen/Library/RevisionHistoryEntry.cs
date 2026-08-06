namespace PiratePDF.DocGen.Library;

internal sealed record RevisionHistoryEntry(
    string Version,
    string Date,
    string Description);
