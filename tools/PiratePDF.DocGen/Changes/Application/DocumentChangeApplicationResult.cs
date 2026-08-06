namespace PiratePDF.DocGen.Changes.Application;

internal sealed record DocumentChangeApplicationResult(
    int AppliedChangeCount,
    int ModifiedFileCount,
    IReadOnlyList<string> ModifiedFiles);
