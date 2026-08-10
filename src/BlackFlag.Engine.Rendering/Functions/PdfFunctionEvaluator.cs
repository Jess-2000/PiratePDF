
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Functions;

/// <summary>
/// Evaluates bounded PDF function types required by color, shading, and
/// pattern rendering.
/// </summary>
public sealed class PdfFunctionEvaluator
{
    private const int MaximumStitchingDepth = 32;

    /// <summary>Evaluates a function for the supplied inputs.</summary>
    public static double[] Evaluate(PdfObject function, IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(input);

        PdfDictionary dictionary = function switch
        {
            PdfDictionary value => value,
            PdfStream stream => stream.Dictionary,
            _ => throw new NotSupportedException(
                "A PDF function must be represented by a dictionary or stream.")
        };

        int type = ReadInteger(dictionary, "FunctionType");
        return type switch
        {
            2 => EvaluateType2(dictionary, input),
            3 => EvaluateType3(dictionary, input, depth: 0),
            _ => throw new NotSupportedException(
                $"PDF FunctionType {type} is not supported by the BF-1048F evaluator.")
        };
    }

    private static double[] EvaluateType3(
        PdfDictionary dictionary,
        IReadOnlyList<double> input,
        int depth)
    {
        if (depth >= MaximumStitchingDepth)
        {
            throw new InvalidDataException(
                "PDF stitching-function depth exceeds the configured BF-1048F limit.");
        }

        if (input.Count != 1)
        {
            throw new InvalidDataException(
                "A Type 3 stitching function requires exactly one input.");
        }

        PdfArray functions = RequireArray(dictionary, "Functions");
        PdfArray bounds = RequireArray(dictionary, "Bounds");
        PdfArray encode = RequireArray(dictionary, "Encode");
        double[] domain = ReadNumberArray(RequireArray(dictionary, "Domain"));

        if (domain.Length != 2 ||
            bounds.Count != Math.Max(0, functions.Count - 1) ||
            encode.Count != functions.Count * 2)
        {
            throw new InvalidDataException(
                "The Type 3 function arrays are structurally inconsistent.");
        }

        double x = Math.Clamp(input[0], domain[0], domain[1]);
        int index = 0;
        while (index < bounds.Count && x >= ReadNumber(bounds[index]))
        {
            index++;
        }

        double lower = index == 0 ? domain[0] : ReadNumber(bounds[index - 1]);
        double upper = index == bounds.Count ? domain[1] : ReadNumber(bounds[index]);
        double e0 = ReadNumber(encode[index * 2]);
        double e1 = ReadNumber(encode[index * 2 + 1]);
        double mapped = upper == lower
            ? e0
            : e0 + ((x - lower) * (e1 - e0) / (upper - lower));

        PdfObject selected = functions[index];
        if (selected is PdfIndirectReference)
        {
            throw new NotSupportedException(
                "Indirect stitching-function members must be resolved before evaluation.");
        }

        PdfDictionary selectedDictionary = selected switch
        {
            PdfDictionary value => value,
            PdfStream stream => stream.Dictionary,
            _ => throw new InvalidDataException(
                "A Type 3 stitching function member must be a function dictionary or stream.")
        };

        int type = ReadInteger(selectedDictionary, "FunctionType");
        double[] result = type switch
        {
            2 => EvaluateType2(selectedDictionary, [mapped]),
            3 => EvaluateType3(selectedDictionary, [mapped], checked(depth + 1)),
            _ => throw new NotSupportedException(
                $"Nested PDF FunctionType {type} is not supported.")
        };

        return ClipIfPresent(selectedDictionary, result);
    }

    private static double[] EvaluateType2(
        PdfDictionary dictionary,
        IReadOnlyList<double> input)
    {
        if (input.Count != 1)
        {
            throw new InvalidDataException(
                "A Type 2 exponential interpolation function requires one input.");
        }

        double[] domain = dictionary.TryGetValue(
            new PdfName("Domain"),
            out PdfObject domainObject)
            ? ReadNumberArray(RequireArray(domainObject, "Domain"))
            : [0, 1];

        if (domain.Length != 2)
        {
            throw new InvalidDataException(
                "A Type 2 function /Domain must contain two numbers.");
        }

        double[] c0 = dictionary.TryGetValue(
            new PdfName("C0"),
            out PdfObject c0Object)
            ? ReadNumberArray(RequireArray(c0Object, "C0"))
            : [0];

        double[] c1 = dictionary.TryGetValue(
            new PdfName("C1"),
            out PdfObject c1Object)
            ? ReadNumberArray(RequireArray(c1Object, "C1"))
            : [1];

        if (c0.Length != c1.Length)
        {
            throw new InvalidDataException(
                "A Type 2 function /C0 and /C1 must contain the same number of outputs.");
        }

        double exponent = ReadNumber(
            dictionary.TryGetValue(new PdfName("N"), out PdfObject n)
                ? n
                : throw new InvalidDataException("A Type 2 function requires /N."));

        double x = Math.Clamp(input[0], domain[0], domain[1]);
        double factor = Math.Pow(x, exponent);
        double[] result = new double[c0.Length];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] = c0[index] + factor * (c1[index] - c0[index]);
        }

        return ClipIfPresent(dictionary, result);
    }

    private static double[] ClipIfPresent(
        PdfDictionary dictionary,
        double[] values)
    {
        if (!dictionary.TryGetValue(new PdfName("Range"), out PdfObject rangeObject))
        {
            return values;
        }

        double[] range = ReadNumberArray(RequireArray(rangeObject, "Range"));
        if (range.Length != values.Length * 2)
        {
            throw new InvalidDataException(
                "PDF function /Range must contain two values per output.");
        }

        for (int index = 0; index < values.Length; index++)
        {
            values[index] = Math.Clamp(
                values[index],
                range[index * 2],
                range[index * 2 + 1]);
        }

        return values;
    }

    private static PdfArray RequireArray(PdfDictionary dictionary, string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? RequireArray(value, key)
            : throw new InvalidDataException($"PDF function /{key} is required.");

    private static PdfArray RequireArray(PdfObject value, string key) =>
        value as PdfArray
        ?? throw new InvalidDataException($"PDF function /{key} must be an array.");

    private static int ReadInteger(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value) ||
            value is not PdfInteger integer ||
            integer.Value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidDataException($"PDF function /{key} must be an integer.");
        }

        return (int)integer.Value;
    }

    private static double[] ReadNumberArray(PdfArray array) =>
        array.Select(ReadNumber).ToArray();

    private static double ReadNumber(PdfObject value) => value switch
    {
        PdfInteger integer => integer.Value,
        PdfReal real => real.Value,
        _ => throw new InvalidDataException("PDF function array contains a non-number.")
    };
}
