
namespace BlackFlag.Engine.Rendering.Functions;

/// <summary>Defines bounded work limits for PDF function evaluation.</summary>
public sealed record PdfFunctionEvaluationLimits
{
    /// <summary>Gets production defaults.</summary>
    public static PdfFunctionEvaluationLimits Default { get; } = new();

    /// <summary>Gets the maximum sampled-function input dimension count.</summary>
    public int MaximumInputDimensions { get; init; } = 8;

    /// <summary>Gets the maximum sampled-function sample values.</summary>
    public int MaximumSampleValues { get; init; } = 4_000_000;

    /// <summary>Gets the maximum calculator-function operations.</summary>
    public int MaximumCalculatorOperations { get; init; } = 100_000;

    /// <summary>Gets the maximum calculator stack depth.</summary>
    public int MaximumCalculatorStackDepth { get; init; } = 1024;

    /// <summary>Gets the maximum nested calculator procedure depth.</summary>
    public int MaximumCalculatorProcedureDepth { get; init; } = 32;

    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumInputDimensions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumSampleValues);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumCalculatorOperations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumCalculatorStackDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumCalculatorProcedureDepth);
    }
}
