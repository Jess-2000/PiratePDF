using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

public sealed partial class PdfGraphicsInterpreter
{
    private static PdfGraphicsStateSnapshot ApplyExtGState(
        PdfGraphicsStateSnapshot state,
        PdfResolvedExtGState extGState,
        PdfGraphicsOperation operation)
    {
        double strokingAlpha = extGState.StrokingAlpha ?? state.StrokingAlpha;
        double nonstrokingAlpha = extGState.NonstrokingAlpha ?? state.NonstrokingAlpha;

        if (strokingAlpha is < 0 or > 1 || nonstrokingAlpha is < 0 or > 1)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "An ExtGState alpha value is outside the valid zero-to-one range.");
        }

        return state with
        {
            StrokingAlpha = strokingAlpha,
            NonstrokingAlpha = nonstrokingAlpha,
            OverprintStroke = extGState.OverprintStroke ?? state.OverprintStroke,
            OverprintFill = extGState.OverprintFill ?? state.OverprintFill,
            OverprintMode = extGState.OverprintMode ?? state.OverprintMode,
            BlendMode = extGState.BlendMode ?? state.BlendMode,
            SoftMask = extGState.SoftMask ?? state.SoftMask
        };
    }

    private static PdfGraphicsStateSnapshot MoveTextLine(
        PdfGraphicsStateSnapshot state,
        double x,
        double y)
    {
        PdfMatrix translation = new(1, 0, 0, 1, x, y);
        PdfMatrix line = state.TextLineMatrix.Multiply(translation);
        return state with
        {
            TextLineMatrix = line,
            TextMatrix = line
        };
    }

    private static PdfResolvedFont RequireFont(
        PdfGraphicsStateSnapshot state,
        PdfGraphicsOperation operation) =>
        state.Font
        ?? throw CreateException(
            PdfGraphicsInterpretationFailure.InvalidTextState,
            operation,
            "Text showing requires an active font selected by Tf.");

    private static void RequireTextObject(
        bool inTextObject,
        PdfGraphicsOperation operation)
    {
        if (!inTextObject)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Text operator '{operation.Operator.Code}' occurred outside BT/ET.");
        }
    }

    private static PdfColorValue ResolveColorSpace(
        PdfResourceScope resources,
        PdfName name)
    {
        return name.Value switch
        {
            "DeviceGray" => new PdfColorValue("DeviceGray", [0]),
            "DeviceRGB" => new PdfColorValue("DeviceRGB", [0, 0, 0]),
            "DeviceCMYK" => new PdfColorValue("DeviceCMYK", [0, 0, 0, 1]),
            _ => CreateResolvedColorSpace(resources.ResolveColorSpace(name))
        };
    }

    private static PdfColorValue CreateResolvedColorSpace(
        PdfResolvedColorSpace colorSpace) =>
        new(
            colorSpace.ColorSpaceKind.ToString(),
            new double[Math.Max(0, colorSpace.ComponentCount)],
            colorSpace);

    private static PdfColorValue CreateDeviceColor(
        string name,
        IReadOnlyList<PdfObject> operands) =>
        new(name, operands.Select(ReadNumber));

    private static PdfColorValue ReplaceColorComponents(
        PdfColorValue current,
        IReadOnlyList<PdfObject> operands,
        PdfResourceScope resources)
    {
        PdfObject? lastOperand = operands.Count > 0 ? operands[operands.Count - 1] : null;
        PdfResolvedPattern? pattern = lastOperand is PdfName patternName
            ? resources.ResolvePattern(patternName)
            : current.Pattern;

        IEnumerable<PdfObject> numericOperands = operands.Where(IsNumber);
        return new PdfColorValue(
            current.ColorSpaceName,
            numericOperands.Select(ReadNumber),
            current.Resource,
            pattern);
    }

    private static bool IsNumber(PdfObject value) =>
        value is PdfInteger or PdfReal;

    private static PdfLineCapStyle ReadLineCap(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number = ReadInteger(value);
        return number switch
        {
            0 => PdfLineCapStyle.Butt,
            1 => PdfLineCapStyle.Round,
            2 => PdfLineCapStyle.ProjectingSquare,
            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"Line-cap value {number} is invalid.")
        };
    }

    private static PdfLineJoinStyle ReadLineJoin(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number = ReadInteger(value);
        return number switch
        {
            0 => PdfLineJoinStyle.Miter,
            1 => PdfLineJoinStyle.Round,
            2 => PdfLineJoinStyle.Bevel,
            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"Line-join value {number} is invalid.")
        };
    }

    private static PdfDashPattern ReadDashPattern(
        IReadOnlyList<PdfObject> operands,
        PdfGraphicsOperation operation)
    {
        if (operands[0] is not PdfArray array)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The d operator requires an array of dash lengths.");
        }

        double[] elements = array.Select(ReadNumber).ToArray();
        if (elements.Any(static value => value < 0))
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "Dash lengths cannot be negative.");
        }

        double phase = ReadNumber(operands[1]);
        return new PdfDashPattern(elements, phase);
    }

    private static void AddRectangle(
        PathBuilder path,
        PdfGraphicsStateSnapshot state,
        PdfGraphicsOperation operation)
    {
        double x = ReadNumber(operation.Operands[0]);
        double y = ReadNumber(operation.Operands[1]);
        double width = ReadNumber(operation.Operands[2]);
        double height = ReadNumber(operation.Operands[3]);

        PdfPoint p0 = state.Transform.Transform(new PdfPoint(x, y));
        PdfPoint p1 = state.Transform.Transform(new PdfPoint(x + width, y));
        PdfPoint p2 = state.Transform.Transform(new PdfPoint(x + width, y + height));
        PdfPoint p3 = state.Transform.Transform(new PdfPoint(x, y + height));

        path.MoveTo(p0);
        path.LineTo(p1, operation);
        path.LineTo(p2, operation);
        path.LineTo(p3, operation);
        path.Close(operation);
    }

    private static PdfPoint TransformPoint(
        PdfGraphicsStateSnapshot state,
        IReadOnlyList<PdfObject> operands,
        int offset) =>
        state.Transform.Transform(
            new PdfPoint(
                ReadNumber(operands[offset]),
                ReadNumber(operands[offset + 1])));

    private static PdfMatrix ReadMatrix(IReadOnlyList<PdfObject> operands)
    {
        if (operands.Count < 6)
        {
            throw new ArgumentException("A PDF matrix requires six numeric values.");
        }

        return new PdfMatrix(
            ReadNumber(operands[0]),
            ReadNumber(operands[1]),
            ReadNumber(operands[2]),
            ReadNumber(operands[3]),
            ReadNumber(operands[4]),
            ReadNumber(operands[5]));
    }

    private static double ReadNumber(PdfObject value) =>
        value switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => throw new ArgumentException("A numeric PDF operand was required.", nameof(value))
        };

    private static long ReadInteger(PdfObject value) =>
        value is PdfInteger integer
            ? integer.Value
            : throw new ArgumentException("An integer PDF operand was required.", nameof(value));

    private static PdfName ReadName(PdfObject value) =>
        value as PdfName
        ?? throw new ArgumentException("A PDF name operand was required.", nameof(value));
}
