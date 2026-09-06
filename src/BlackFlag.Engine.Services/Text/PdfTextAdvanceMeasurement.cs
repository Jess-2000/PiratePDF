namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Reports original/replacement text advances in user-space units after
/// horizontal scaling.
/// </summary>
public sealed class PdfTextAdvanceMeasurement
{
    internal PdfTextAdvanceMeasurement(
        double originalAdvance,
        double replacementAdvance,
        PdfTextAdvanceFit fit)
    {
        OriginalAdvance = originalAdvance;
        ReplacementAdvance = replacementAdvance;
        Delta = replacementAdvance - originalAdvance;
        Fit = fit;
    }

    public double OriginalAdvance { get; }
    public double ReplacementAdvance { get; }
    public double Delta { get; }
    public double AbsoluteDelta => Math.Abs(Delta);
    public PdfTextAdvanceFit Fit { get; }
    public bool PreservesAdvance => Fit == PdfTextAdvanceFit.Exact;
}
