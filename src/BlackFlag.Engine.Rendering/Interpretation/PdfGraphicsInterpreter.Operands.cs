using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

public sealed partial class PdfGraphicsInterpreter
{
    private static PdfPathGeometry CreateRectangleGeometry(
        PdfArray rectangle,
        PdfMatrix transform)
    {
        if (rectangle.Count != 4)
        {
            throw new ArgumentException(
                "A PDF rectangle requires exactly four numeric values.",
                nameof(rectangle));
        }

        double x1 =
            ReadNumber(rectangle[0]);

        double y1 =
            ReadNumber(rectangle[1]);

        double x2 =
            ReadNumber(rectangle[2]);

        double y2 =
            ReadNumber(rectangle[3]);

        PdfPoint p0 =
            transform.Transform(
                new PdfPoint(x1, y1));

        PdfPoint p1 =
            transform.Transform(
                new PdfPoint(x2, y1));

        PdfPoint p2 =
            transform.Transform(
                new PdfPoint(x2, y2));

        PdfPoint p3 =
            transform.Transform(
                new PdfPoint(x1, y2));

        return new PdfPathGeometry(
            [
                new PdfPathSegment(
                    PdfPathSegmentKind.MoveTo,
                    p0),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p1),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p2),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p3),

                new PdfPathSegment(
                    PdfPathSegmentKind.ClosePath,
                    p0)
            ]);
    }

    private static double ReadNonNegativeNumber(
        PdfObject value,
        PdfGraphicsOperation operation,
        string description)
    {
        double number =
            ReadNumber(value);

        if (number < 0)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"{description} cannot be negative.");
        }

        return number;
    }

    private static double ReadMiterLimit(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        double number =
            ReadNumber(value);

        if (number < 1)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The miter limit must be at least 1.");
        }

        return number;
    }

    private static double ReadFlatness(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        double number =
            ReadNumber(value);

        if (number is < 0 or > 100)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The flatness tolerance must be between 0 and 100.");
        }

        return number;
    }

    private static int ReadTextRenderingMode(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number =
            ReadInteger(value);

        if (number is < 0 or > 7)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Text rendering mode {number} is invalid.");
        }

        return checked((int)number);
    }

    private static int? TryGetInteger(
        PdfDictionary dictionary,
        string key)
    {
        if (!dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            || !TryReadInteger(
                value,
                out long number)
            || number is < int.MinValue or > int.MaxValue)
        {
            return null;
        }

        return checked((int)number);
    }

    private static PdfArray? TryGetArray(
        PdfDictionary dictionary,
        string key)
    {
        return dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            ? value as PdfArray
            : null;
    }

    private static double? TryGetNumber(
        PdfDictionary? dictionary,
        string key)
    {
        if (dictionary is null
            || !dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            || !TryReadNumber(
                value,
                out double number))
        {
            return null;
        }

        return number;
    }

    private static bool TryReadInteger(
        PdfObject value,
        out long number)
    {
        if (value is PdfInteger integer)
        {
            number = integer.Value;
            return true;
        }

        number = 0;
        return false;
    }

    private static bool TryReadNumber(
        PdfObject value,
        out double number)
    {
        switch (value)
        {
            case PdfInteger integer:
                number = integer.Value;
                return true;

            case PdfReal real:
                number = real.Value;
                return true;

            default:
                number = 0;
                return false;
        }
    }

    private static void EnsureLimit(
        int value,
        int maximum,
        PdfGraphicsOperation operation,
        string message)
    {
        if (value > maximum)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.LimitExceeded,
                operation,
                message);
        }
    }

    private static PdfGraphicsInterpretationException CreateException(
        PdfGraphicsInterpretationFailure failure,
        PdfGraphicsOperation operation,
        string message,
        Exception? innerException = null) =>
        new(failure, operation.SourceSpan, message, innerException);
}
