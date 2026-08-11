
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Nested;

namespace BlackFlag.Engine.Rendering.Patterns;

/// <summary>
/// Plans the finite Type 1 tiling-pattern cells intersecting page-space bounds.
/// </summary>
public static class PdfPatternTilePlanner
{
    /// <summary>Creates a bounded tile plan.</summary>
    public static PdfPatternTilePlan Plan(
        PdfTilingPatternCell cell,
        PdfMatrix patternToPage,
        PdfDisplayBounds pageBounds,
        int maximumCells)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCells);

        if (cell.XStep == 0 || cell.YStep == 0)
        {
            throw new InvalidDataException(
                "A tiling pattern requires nonzero /XStep and /YStep.");
        }

        PdfMatrix inverse =
            Invert(patternToPage);

        PdfPoint[] corners =
        [
            inverse.Transform(new PdfPoint(pageBounds.Left, pageBounds.Bottom)),
            inverse.Transform(new PdfPoint(pageBounds.Left, pageBounds.Top)),
            inverse.Transform(new PdfPoint(pageBounds.Right, pageBounds.Bottom)),
            inverse.Transform(new PdfPoint(pageBounds.Right, pageBounds.Top))
        ];

        double left = corners.Min(static point => point.X);
        double right = corners.Max(static point => point.X);
        double bottom = corners.Min(static point => point.Y);
        double top = corners.Max(static point => point.Y);

        (int minX, int maxX) =
            GetIndexRange(
                left,
                right,
                cell.Bounds.Left,
                cell.Bounds.Right,
                cell.XStep);

        (int minY, int maxY) =
            GetIndexRange(
                bottom,
                top,
                cell.Bounds.Bottom,
                cell.Bounds.Top,
                cell.YStep);

        long width =
            checked((long)maxX - minX + 1);

        long height =
            checked((long)maxY - minY + 1);

        long count =
            checked(width * height);

        if (count <= 0 || count > maximumCells)
        {
            throw new InvalidDataException(
                $"The tiling pattern requires {count} cells, exceeding the configured maximum of {maximumCells}.");
        }

        return new PdfPatternTilePlan(
            minX,
            maxX,
            minY,
            maxY,
            checked((int)count));
    }

    private static (int Minimum, int Maximum) GetIndexRange(
        double viewMinimum,
        double viewMaximum,
        double cellMinimum,
        double cellMaximum,
        double step)
    {
        double first =
            (viewMinimum - cellMaximum) / step;

        double second =
            (viewMaximum - cellMinimum) / step;

        int minimum =
            checked((int)Math.Ceiling(Math.Min(first, second)));

        int maximum =
            checked((int)Math.Floor(Math.Max(first, second)));

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        return (minimum, maximum);
    }

    private static PdfMatrix Invert(PdfMatrix matrix)
    {
        double determinant =
            (matrix.A * matrix.D) -
            (matrix.B * matrix.C);

        if (Math.Abs(determinant) < 1e-12)
        {
            throw new InvalidDataException(
                "The tiling-pattern transform is singular.");
        }

        double a = matrix.D / determinant;
        double b = -matrix.B / determinant;
        double c = -matrix.C / determinant;
        double d = matrix.A / determinant;
        double e =
            -((matrix.E * a) + (matrix.F * c));
        double f =
            -((matrix.E * b) + (matrix.F * d));

        return new PdfMatrix(
            a,
            b,
            c,
            d,
            e,
            f);
    }
}
