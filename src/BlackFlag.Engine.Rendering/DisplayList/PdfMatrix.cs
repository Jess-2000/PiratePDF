namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents an immutable two-dimensional affine transformation matrix.
/// </summary>
public readonly record struct PdfMatrix
{
    /// <summary>Initializes an affine transformation matrix.</summary>
    public PdfMatrix(
        double a,
        double b,
        double c,
        double d,
        double e,
        double f)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        F = f;
    }

    /// <summary>Gets the horizontal scale/rotation component.</summary>
    public double A { get; }

    /// <summary>Gets the vertical rotation component.</summary>
    public double B { get; }

    /// <summary>Gets the horizontal skew component.</summary>
    public double C { get; }

    /// <summary>Gets the vertical scale component.</summary>
    public double D { get; }

    /// <summary>Gets horizontal translation.</summary>
    public double E { get; }

    /// <summary>Gets vertical translation.</summary>
    public double F { get; }

    /// <summary>Gets the identity matrix.</summary>
    public static PdfMatrix Identity { get; } = new(1, 0, 0, 1, 0, 0);

    /// <summary>
    /// Concatenates this matrix with another matrix using PDF affine
    /// transformation semantics.
    /// </summary>
    public PdfMatrix Multiply(PdfMatrix other) =>
        new(
            (A * other.A) + (B * other.C),
            (A * other.B) + (B * other.D),
            (C * other.A) + (D * other.C),
            (C * other.B) + (D * other.D),
            (E * other.A) + (F * other.C) + other.E,
            (E * other.B) + (F * other.D) + other.F);

    /// <summary>Transforms a point through this matrix.</summary>
    public PdfPoint Transform(PdfPoint point) =>
        new(
            (point.X * A) + (point.Y * C) + E,
            (point.X * B) + (point.Y * D) + F);
}
