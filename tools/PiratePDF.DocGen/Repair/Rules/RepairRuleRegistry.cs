namespace PiratePDF.DocGen.Repair.Rules;

internal static class RepairRuleRegistry
{
    private static readonly IDocumentRepairRule[] Rules =
    [
        new CanonicalStatusRepairRule(),
        new IsoDateRepairRule(),
        new BrokenDocumentReferenceRepairRule(),
        new MissingRevisionHistoryRepairRule(),
        new MissingTrailingNewlineRepairRule()
    ];

    public static IReadOnlyList<IDocumentRepairRule> Create()
    {
        return Rules;
    }
}