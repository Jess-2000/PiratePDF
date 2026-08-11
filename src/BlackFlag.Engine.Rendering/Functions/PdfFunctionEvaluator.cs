
using System.Globalization;
using System.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Functions;

/// <summary>
/// Evaluates bounded PDF FunctionType 0, 2, 3, and 4 functions.
/// </summary>
public sealed class PdfFunctionEvaluator
{
    private const int MaximumStitchingDepth = 32;

    /// <summary>Evaluates a PDF function using production limits.</summary>
    public static double[] Evaluate(
        PdfObject function,
        IReadOnlyList<double> input) =>
        Evaluate(
            function,
            input,
            PdfFunctionEvaluationLimits.Default,
            CancellationToken.None);

    /// <summary>Evaluates a PDF function using explicit limits.</summary>
    public static double[] Evaluate(
        PdfObject function,
        IReadOnlyList<double> input,
        PdfFunctionEvaluationLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();

        PdfDictionary dictionary = GetDictionary(function);
        int type = ReadInteger(dictionary, "FunctionType");

        return type switch
        {
            0 => EvaluateType0(
                RequireStream(function, "A Type 0 sampled function must be a stream."),
                input,
                limits,
                cancellationToken),
            2 => EvaluateType2(dictionary, input),
            3 => EvaluateType3(
                dictionary,
                input,
                limits,
                0,
                cancellationToken),
            4 => EvaluateType4(
                RequireStream(function, "A Type 4 calculator function must be a stream."),
                input,
                limits,
                cancellationToken),
            _ => throw new NotSupportedException(
                $"PDF FunctionType {type} is not supported.")
        };
    }

    private static double[] EvaluateType0(
        PdfStream stream,
        IReadOnlyList<double> input,
        PdfFunctionEvaluationLimits limits,
        CancellationToken cancellationToken)
    {
        PdfDictionary dictionary = stream.Dictionary;

        double[] domain = ReadNumberArray(RequireArray(dictionary, "Domain"));
        double[] range = ReadNumberArray(RequireArray(dictionary, "Range"));
        int[] size = ReadIntegerArray(RequireArray(dictionary, "Size"));

        if (domain.Length % 2 != 0 ||
            range.Length == 0 ||
            range.Length % 2 != 0 ||
            size.Length == 0 ||
            domain.Length / 2 != size.Length ||
            input.Count != size.Length)
        {
            throw new InvalidDataException(
                "The Type 0 function dimensions are structurally inconsistent.");
        }

        if (size.Length > limits.MaximumInputDimensions)
        {
            throw new InvalidDataException(
                "The Type 0 function exceeds the configured input-dimension limit.");
        }

        int outputs = range.Length / 2;
        int bitsPerSample = ReadInteger(dictionary, "BitsPerSample");
        if (bitsPerSample is not (1 or 2 or 4 or 8 or 12 or 16 or 24 or 32))
        {
            throw new NotSupportedException(
                $"Type 0 BitsPerSample={bitsPerSample} is unsupported.");
        }

        int order = dictionary.TryGetValue(
            new PdfName("Order"),
            out PdfObject orderObject)
            ? ReadInteger(orderObject)
            : 1;

        if (order != 1)
        {
            throw new NotSupportedException(
                "BF-1048G sampled functions currently require /Order 1.");
        }

        long latticePoints = 1;
        foreach (int dimension in size)
        {
            if (dimension <= 0)
            {
                throw new InvalidDataException(
                    "Every Type 0 /Size entry must be positive.");
            }

            latticePoints = checked(latticePoints * dimension);
        }

        long sampleValues = checked(latticePoints * outputs);
        if (sampleValues > limits.MaximumSampleValues)
        {
            throw new InvalidDataException(
                "The Type 0 sampled-function lattice exceeds the configured sample limit.");
        }

        double[] encode = dictionary.TryGetValue(
            new PdfName("Encode"),
            out PdfObject encodeObject)
            ? ReadNumberArray(RequireArray(encodeObject, "Encode"))
            : DefaultEncode(size);

        double[] decode = dictionary.TryGetValue(
            new PdfName("Decode"),
            out PdfObject decodeObject)
            ? ReadNumberArray(RequireArray(decodeObject, "Decode"))
            : range.ToArray();

        if (encode.Length != input.Count * 2 ||
            decode.Length != outputs * 2)
        {
            throw new InvalidDataException(
                "The Type 0 /Encode or /Decode array has an invalid length.");
        }

        ReadOnlyMemory<byte> sampleBytes =
            DecodeFunctionStream(stream, cancellationToken);

        long requiredBits = checked(sampleValues * bitsPerSample);
        long requiredBytes = checked((requiredBits + 7) / 8);
        if (sampleBytes.Length < requiredBytes)
        {
            throw new InvalidDataException(
                "The Type 0 sample stream is shorter than required by its lattice.");
        }

        int dimensions = input.Count;
        int[] low = new int[dimensions];
        int[] high = new int[dimensions];
        double[] fractions = new double[dimensions];

        for (int dimension = 0; dimension < dimensions; dimension++)
        {
            double domainMin = domain[dimension * 2];
            double domainMax = domain[dimension * 2 + 1];
            double x = Math.Clamp(input[dimension], domainMin, domainMax);

            double e0 = encode[dimension * 2];
            double e1 = encode[dimension * 2 + 1];
            double mapped = domainMax == domainMin
                ? e0
                : e0 + ((x - domainMin) * (e1 - e0) / (domainMax - domainMin));

            mapped = Math.Clamp(mapped, 0, size[dimension] - 1);
            low[dimension] = (int)Math.Floor(mapped);
            high[dimension] = Math.Min(low[dimension] + 1, size[dimension] - 1);
            fractions[dimension] = mapped - low[dimension];
        }

        double[] result = new double[outputs];
        int corners = 1 << dimensions;
        ulong maximumSample =
            bitsPerSample == 32
                ? uint.MaxValue
                : (1UL << bitsPerSample) - 1UL;

        for (int corner = 0; corner < corners; corner++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double weight = 1;
            int latticeIndex = 0;
            int stride = 1;

            for (int dimension = 0; dimension < dimensions; dimension++)
            {
                bool upper = (corner & (1 << dimension)) != 0;
                int coordinate = upper ? high[dimension] : low[dimension];
                weight *= upper ? fractions[dimension] : 1 - fractions[dimension];
                latticeIndex = checked(latticeIndex + coordinate * stride);
                stride = checked(stride * size[dimension]);
            }

            if (weight == 0)
            {
                continue;
            }

            for (int output = 0; output < outputs; output++)
            {
                long sampleIndex = checked(
                    ((long)latticeIndex * outputs) + output);

                ulong raw = ReadBits(
                    sampleBytes.Span,
                    checked(sampleIndex * bitsPerSample),
                    bitsPerSample);

                double normalized = raw / (double)maximumSample;
                double decoded =
                    decode[output * 2] +
                    normalized *
                    (decode[output * 2 + 1] - decode[output * 2]);

                result[output] += decoded * weight;
            }
        }

        return ClipToRange(range, result);
    }

    private static double[] EvaluateType3(
        PdfDictionary dictionary,
        IReadOnlyList<double> input,
        PdfFunctionEvaluationLimits limits,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth >= MaximumStitchingDepth)
        {
            throw new InvalidDataException(
                "PDF stitching-function depth exceeds the configured limit.");
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

        while (index < bounds.Count &&
            x >= ReadNumber(bounds[index]))
        {
            index++;
        }

        double lower =
            index == 0
                ? domain[0]
                : ReadNumber(bounds[index - 1]);

        double upper =
            index == bounds.Count
                ? domain[1]
                : ReadNumber(bounds[index]);

        double e0 = ReadNumber(encode[index * 2]);
        double e1 = ReadNumber(encode[index * 2 + 1]);

        double mapped =
            upper == lower
                ? e0
                : e0 +
                  ((x - lower) * (e1 - e0) / (upper - lower));

        PdfObject selected = functions[index];

        if (selected is PdfIndirectReference)
        {
            throw new NotSupportedException(
                "Indirect Type 3 function members must be resolved before evaluation.");
        }

        double[] result = EvaluateNested(
            selected,
            [mapped],
            limits,
            checked(depth + 1),
            cancellationToken);

        return ClipIfPresent(GetDictionary(selected), result);
    }

    private static double[] EvaluateNested(
        PdfObject function,
        IReadOnlyList<double> input,
        PdfFunctionEvaluationLimits limits,
        int depth,
        CancellationToken cancellationToken)
    {
        PdfDictionary dictionary = GetDictionary(function);
        int type = ReadInteger(dictionary, "FunctionType");

        return type switch
        {
            0 => EvaluateType0(
                RequireStream(function, "A nested Type 0 function must be a stream."),
                input,
                limits,
                cancellationToken),
            2 => EvaluateType2(dictionary, input),
            3 => EvaluateType3(
                dictionary,
                input,
                limits,
                depth,
                cancellationToken),
            4 => EvaluateType4(
                RequireStream(function, "A nested Type 4 function must be a stream."),
                input,
                limits,
                cancellationToken),
            _ => throw new NotSupportedException(
                $"PDF FunctionType {type} is not supported.")
        };
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
            dictionary.TryGetValue(
                new PdfName("N"),
                out PdfObject n)
                ? n
                : throw new InvalidDataException(
                    "A Type 2 function requires /N."));

        double x = Math.Clamp(input[0], domain[0], domain[1]);
        double factor = Math.Pow(x, exponent);
        double[] result = new double[c0.Length];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] =
                c0[index] +
                factor * (c1[index] - c0[index]);
        }

        return ClipIfPresent(dictionary, result);
    }

    private static double[] EvaluateType4(
        PdfStream stream,
        IReadOnlyList<double> input,
        PdfFunctionEvaluationLimits limits,
        CancellationToken cancellationToken)
    {
        PdfDictionary dictionary = stream.Dictionary;
        double[] domain =
            ReadNumberArray(RequireArray(dictionary, "Domain"));
        double[] range =
            ReadNumberArray(RequireArray(dictionary, "Range"));

        if (domain.Length != input.Count * 2 ||
            range.Length == 0 ||
            range.Length % 2 != 0)
        {
            throw new InvalidDataException(
                "The Type 4 function /Domain or /Range has an invalid length.");
        }

        ReadOnlyMemory<byte> decoded =
            DecodeFunctionStream(stream, cancellationToken);

        string program =
            Encoding.ASCII.GetString(decoded.Span);

        var machine =
            new CalculatorMachine(
                limits,
                cancellationToken);

        for (int index = 0; index < input.Count; index++)
        {
            machine.Push(
                Math.Clamp(
                    input[index],
                    domain[index * 2],
                    domain[index * 2 + 1]));
        }

        machine.Execute(program);

        int outputs = range.Length / 2;
        double[] result = machine.ReadNumericOutputs(outputs);
        return ClipToRange(range, result);
    }

    private static ReadOnlyMemory<byte> DecodeFunctionStream(
        PdfStream stream,
        CancellationToken cancellationToken)
    {
        PdfName[] filters =
            ReadFilters(stream.Dictionary);

        PdfDictionary?[]? parameters =
            ReadDecodeParameters(
                stream.Dictionary,
                filters.Length);

        var pipeline =
            new PdfFilterPipeline(
                PdfFilterRegistrations.CreateDefault());

        return pipeline.Decode(
            stream.Data,
            filters,
            parameters,
            new PdfFilterDecodeLimits(),
            cancellationToken);
    }

    private static ulong ReadBits(
        ReadOnlySpan<byte> bytes,
        long bitOffset,
        int bitCount)
    {
        ulong value = 0;

        for (int index = 0; index < bitCount; index++)
        {
            long absolute = checked(bitOffset + index);
            int byteIndex = checked((int)(absolute / 8));
            int bitIndex = 7 - checked((int)(absolute % 8));

            if ((uint)byteIndex >= (uint)bytes.Length)
            {
                throw new InvalidDataException(
                    "A sampled-function bit read exceeded the decoded stream.");
            }

            value =
                (value << 1) |
                (uint)((bytes[byteIndex] >> bitIndex) & 1);
        }

        return value;
    }

    private static double[] ClipIfPresent(
        PdfDictionary dictionary,
        double[] values)
    {
        if (!dictionary.TryGetValue(
                new PdfName("Range"),
                out PdfObject rangeObject))
        {
            return values;
        }

        return ClipToRange(
            ReadNumberArray(
                RequireArray(rangeObject, "Range")),
            values);
    }

    private static double[] ClipToRange(
        double[] range,
        double[] values)
    {
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

    private static double[] DefaultEncode(
        int[] size)
    {
        double[] result =
            new double[size.Length * 2];

        for (int index = 0; index < size.Length; index++)
        {
            result[index * 2] = 0;
            result[index * 2 + 1] =
                size[index] - 1;
        }

        return result;
    }

    private static PdfDictionary GetDictionary(
        PdfObject function) =>
        function switch
        {
            PdfDictionary value => value,
            PdfStream stream => stream.Dictionary,
            _ => throw new NotSupportedException(
                "A PDF function must be represented by a dictionary or stream.")
        };

    private static PdfStream RequireStream(
        PdfObject value,
        string message) =>
        value as PdfStream ??
        throw new InvalidDataException(message);

    private static PdfArray RequireArray(
        PdfDictionary dictionary,
        string key) =>
        dictionary.TryGetValue(
            new PdfName(key),
            out PdfObject value)
            ? RequireArray(value, key)
            : throw new InvalidDataException(
                $"PDF function /{key} is required.");

    private static PdfArray RequireArray(
        PdfObject value,
        string key) =>
        value as PdfArray ??
        throw new InvalidDataException(
            $"PDF function /{key} must be an array.");

    private static int ReadInteger(
        PdfDictionary dictionary,
        string key) =>
        dictionary.TryGetValue(
            new PdfName(key),
            out PdfObject value)
            ? ReadInteger(value)
            : throw new InvalidDataException(
                $"PDF function /{key} is required.");

    private static int ReadInteger(PdfObject value)
    {
        if (value is not PdfInteger integer ||
            integer.Value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidDataException(
                "A PDF function integer entry is invalid.");
        }

        return (int)integer.Value;
    }

    private static int[] ReadIntegerArray(
        PdfArray array) =>
        array.Select(ReadInteger).ToArray();

    private static double[] ReadNumberArray(
        PdfArray array) =>
        array.Select(ReadNumber).ToArray();

    private static double ReadNumber(
        PdfObject value) =>
        value switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => throw new InvalidDataException(
                "PDF function array contains a non-number.")
        };

    private static PdfName[] ReadFilters(
        PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(
                new PdfName("Filter"),
                out PdfObject value))
        {
            return [];
        }

        return value switch
        {
            PdfName name => [name],
            PdfArray array => array.Select(
                item =>
                    item as PdfName ??
                    throw new InvalidDataException(
                        "Function /Filter entries must be names."))
                .ToArray(),
            _ => throw new InvalidDataException(
                "Function /Filter must be a name or array.")
        };
    }

    private static PdfDictionary?[]? ReadDecodeParameters(
        PdfDictionary dictionary,
        int filterCount)
    {
        if (!dictionary.TryGetValue(
                new PdfName("DecodeParms"),
                out PdfObject value))
        {
            return null;
        }

        if (value is PdfNull)
        {
            return Enumerable.Repeat<PdfDictionary?>(
                null,
                filterCount).ToArray();
        }

        if (value is PdfDictionary single)
        {
            if (filterCount != 1)
            {
                throw new InvalidDataException(
                    "A single function /DecodeParms dictionary requires one filter.");
            }

            return [single];
        }

        if (value is not PdfArray array ||
            array.Count != filterCount)
        {
            throw new InvalidDataException(
                "Function /DecodeParms must align with /Filter.");
        }

        return array.Select(
            item => item switch
            {
                PdfDictionary parameters => parameters,
                PdfNull => null,
                _ => throw new InvalidDataException(
                    "Function /DecodeParms entries must be dictionaries or null.")
            }).ToArray();
    }

    private sealed class CalculatorMachine
    {
        private readonly PdfFunctionEvaluationLimits limits;
        private readonly CancellationToken cancellationToken;
        private readonly List<object> stack = [];
        private int operations;

        public CalculatorMachine(
            PdfFunctionEvaluationLimits limits,
            CancellationToken cancellationToken)
        {
            this.limits = limits;
            this.cancellationToken = cancellationToken;
        }

        public void Push(object value)
        {
            if (stack.Count >= limits.MaximumCalculatorStackDepth)
            {
                throw new InvalidDataException(
                    "The calculator-function operand stack exceeded its configured limit.");
            }

            stack.Add(value);
        }

        public void Execute(string program)
        {
            List<Token> tokens =
                CalculatorParser.Parse(program);

            ExecuteTokens(tokens, depth: 0);
        }

        public double[] ReadNumericOutputs(int count)
        {
            if (stack.Count < count)
            {
                throw new InvalidDataException(
                    "The calculator function did not leave enough output values on the stack.");
            }

            double[] result = new double[count];
            int start = stack.Count - count;

            for (int index = 0; index < count; index++)
            {
                result[index] =
                    ToNumber(stack[start + index]);
            }

            return result;
        }

        private void ExecuteTokens(
            IReadOnlyList<Token> tokens,
            int depth)
        {
            if (depth > limits.MaximumCalculatorProcedureDepth)
            {
                throw new InvalidDataException(
                    "The calculator-function procedure depth exceeded its configured limit.");
            }

            foreach (Token token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();

                operations++;
                if (operations > limits.MaximumCalculatorOperations)
                {
                    throw new InvalidDataException(
                        "The calculator function exceeded its configured operation limit.");
                }

                switch (token.Kind)
                {
                    case TokenKind.Number:
                        Push(token.Number);
                        break;

                    case TokenKind.Boolean:
                        Push(token.Boolean);
                        break;

                    case TokenKind.Procedure:
                        Push(token.Procedure!);
                        break;

                    case TokenKind.Operator:
                        ExecuteOperator(
                            token.Text!,
                            depth);
                        break;
                }
            }
        }

        private void ExecuteOperator(
            string name,
            int depth)
        {
            switch (name)
            {
                case "add": BinaryNumber((a, b) => a + b); break;
                case "sub": BinaryNumber((a, b) => a - b); break;
                case "mul": BinaryNumber((a, b) => a * b); break;
                case "div": BinaryNumber((a, b) => a / b); break;
                case "idiv":
                {
                    double b = PopNumber();
                    double a = PopNumber();
                    Push((double)(long)(a / b));
                    break;
                }
                case "mod":
                {
                    double b = PopNumber();
                    double a = PopNumber();
                    Push(a % b);
                    break;
                }
                case "neg": Push(-PopNumber()); break;
                case "abs": Push(Math.Abs(PopNumber())); break;
                case "ceiling": Push(Math.Ceiling(PopNumber())); break;
                case "floor": Push(Math.Floor(PopNumber())); break;
                case "round": Push(Math.Round(PopNumber(), MidpointRounding.AwayFromZero)); break;
                case "truncate": Push(Math.Truncate(PopNumber())); break;
                case "sqrt": Push(Math.Sqrt(PopNumber())); break;
                case "sin": Push(Math.Sin(PopNumber() * Math.PI / 180)); break;
                case "cos": Push(Math.Cos(PopNumber() * Math.PI / 180)); break;
                case "atan":
                {
                    double denominator = PopNumber();
                    double numerator = PopNumber();
                    double degrees = Math.Atan2(numerator, denominator) * 180 / Math.PI;
                    if (degrees < 0) degrees += 360;
                    Push(degrees);
                    break;
                }
                case "exp":
                {
                    double exponent = PopNumber();
                    double basis = PopNumber();
                    Push(Math.Pow(basis, exponent));
                    break;
                }
                case "ln": Push(Math.Log(PopNumber())); break;
                case "log": Push(Math.Log10(PopNumber())); break;
                case "cvi": Push((double)(long)PopNumber()); break;
                case "cvr": Push(PopNumber()); break;
                case "eq": Compare((a, b) => Equals(a, b)); break;
                case "ne": Compare((a, b) => !Equals(a, b)); break;
                case "gt": NumericCompare((a, b) => a > b); break;
                case "ge": NumericCompare((a, b) => a >= b); break;
                case "lt": NumericCompare((a, b) => a < b); break;
                case "le": NumericCompare((a, b) => a <= b); break;
                case "and": LogicalOrBitwise((a, b) => a & b, (a, b) => a && b); break;
                case "or": LogicalOrBitwise((a, b) => a | b, (a, b) => a || b); break;
                case "xor": LogicalOrBitwise((a, b) => a ^ b, (a, b) => a ^ b); break;
                case "not":
                {
                    object value = Pop();
                    if (value is bool boolean)
                    {
                        Push(!boolean);
                    }
                    else
                    {
                        Push((double)~checked((long)ToNumber(value)));
                    }
                    break;
                }
                case "bitshift":
                {
                    long shift = checked((long)PopNumber());
                    long value = checked((long)PopNumber());
                    Push((double)(shift >= 0 ? value << checked((int)shift) : value >> checked((int)-shift)));
                    break;
                }
                case "dup":
                {
                    object value = Peek();
                    Push(value);
                    break;
                }
                case "exch":
                {
                    object first = Pop();
                    object second = Pop();
                    Push(first);
                    Push(second);
                    break;
                }
                case "pop":
                    Pop();
                    break;
                case "copy":
                {
                    int count = checked((int)PopNumber());
                    if (count < 0 || count > stack.Count)
                    {
                        throw new InvalidDataException("Calculator copy count is invalid.");
                    }

                    object[] copy = stack.Skip(stack.Count - count).ToArray();
                    foreach (object value in copy)
                    {
                        Push(value);
                    }
                    break;
                }
                case "index":
                {
                    int index = checked((int)PopNumber());
                    if (index < 0 || index >= stack.Count)
                    {
                        throw new InvalidDataException("Calculator index is invalid.");
                    }

                    Push(stack[stack.Count - 1 - index]);
                    break;
                }
                case "roll":
                {
                    int amount = checked((int)PopNumber());
                    int count = checked((int)PopNumber());
                    Roll(count, amount);
                    break;
                }
                case "if":
                {
                    List<Token> procedure = PopProcedure();
                    bool condition = PopBoolean();
                    if (condition)
                    {
                        ExecuteTokens(procedure, checked(depth + 1));
                    }
                    break;
                }
                case "ifelse":
                {
                    List<Token> falseProcedure = PopProcedure();
                    List<Token> trueProcedure = PopProcedure();
                    bool condition = PopBoolean();
                    ExecuteTokens(
                        condition ? trueProcedure : falseProcedure,
                        checked(depth + 1));
                    break;
                }
                default:
                    throw new NotSupportedException(
                        $"PDF calculator operator '{name}' is unsupported.");
            }
        }

        private void BinaryNumber(Func<double, double, double> operation)
        {
            double b = PopNumber();
            double a = PopNumber();
            Push(operation(a, b));
        }

        private void NumericCompare(Func<double, double, bool> operation)
        {
            double b = PopNumber();
            double a = PopNumber();
            Push(operation(a, b));
        }

        private void Compare(Func<object, object, bool> operation)
        {
            object b = Pop();
            object a = Pop();
            Push(operation(a, b));
        }

        private void LogicalOrBitwise(
            Func<long, long, long> integerOperation,
            Func<bool, bool, bool> booleanOperation)
        {
            object b = Pop();
            object a = Pop();

            if (a is bool ab && b is bool bb)
            {
                Push(booleanOperation(ab, bb));
                return;
            }

            Push((double)integerOperation(
                checked((long)ToNumber(a)),
                checked((long)ToNumber(b))));
        }

        private void Roll(int count, int amount)
        {
            if (count < 0 || count > stack.Count)
            {
                throw new InvalidDataException(
                    "Calculator roll count is invalid.");
            }

            if (count == 0)
            {
                return;
            }

            int normalized =
                ((amount % count) + count) % count;

            if (normalized == 0)
            {
                return;
            }

            int start = stack.Count - count;
            object[] values =
                stack.Skip(start).ToArray();

            for (int index = 0; index < count; index++)
            {
                stack[start + ((index + normalized) % count)] =
                    values[index];
            }
        }

        private object Peek()
        {
            if (stack.Count == 0)
            {
                throw new InvalidDataException(
                    "Calculator operand stack underflow.");
            }

            return stack[^1];
        }

        private object Pop()
        {
            object value = Peek();
            stack.RemoveAt(stack.Count - 1);
            return value;
        }

        private double PopNumber() =>
            ToNumber(Pop());

        private bool PopBoolean() =>
            Pop() is bool value
                ? value
                : throw new InvalidDataException(
                    "Calculator function expected a boolean.");

        private List<Token> PopProcedure() =>
            Pop() as List<Token> ??
            throw new InvalidDataException(
                "Calculator function expected a procedure.");

        private static double ToNumber(object value) =>
            value switch
            {
                double number => number,
                _ => throw new InvalidDataException(
                    "Calculator function expected a number.")
            };
    }

    private static class CalculatorParser
    {
        public static List<Token> Parse(string source)
        {
            int position = 0;
            List<Token> tokens =
                ParseProcedure(
                    source,
                    ref position,
                    terminatorRequired: false);

            SkipWhitespace(source, ref position);

            if (position != source.Length)
            {
                throw new InvalidDataException(
                    "Unexpected calculator-function input remains after parsing.");
            }

            return tokens;
        }

        private static List<Token> ParseProcedure(
            string source,
            ref int position,
            bool terminatorRequired)
        {
            var tokens = new List<Token>();

            while (position < source.Length)
            {
                SkipWhitespace(source, ref position);

                if (position >= source.Length)
                {
                    break;
                }

                if (source[position] == '%')
                {
                    SkipComment(source, ref position);
                    continue;
                }

                if (source[position] == '}')
                {
                    if (!terminatorRequired)
                    {
                        throw new InvalidDataException(
                            "Unexpected calculator-function procedure terminator.");
                    }

                    position++;
                    return tokens;
                }

                if (source[position] == '{')
                {
                    position++;
                    tokens.Add(
                        Token.ForProcedure(
                            ParseProcedure(
                                source,
                                ref position,
                                terminatorRequired: true)));
                    continue;
                }

                string word =
                    ReadWord(source, ref position);

                if (double.TryParse(
                        word,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number))
                {
                    tokens.Add(Token.ForNumber(number));
                }
                else if (word == "true")
                {
                    tokens.Add(Token.ForBoolean(true));
                }
                else if (word == "false")
                {
                    tokens.Add(Token.ForBoolean(false));
                }
                else
                {
                    tokens.Add(Token.ForOperator(word));
                }
            }

            if (terminatorRequired)
            {
                throw new InvalidDataException(
                    "Calculator-function procedure is unterminated.");
            }

            return tokens;
        }

        private static string ReadWord(
            string source,
            ref int position)
        {
            int start = position;

            while (position < source.Length &&
                !char.IsWhiteSpace(source[position]) &&
                source[position] is not '{' and not '}' and not '%')
            {
                position++;
            }

            if (start == position)
            {
                throw new InvalidDataException(
                    "Calculator function contains an empty token.");
            }

            return source[start..position];
        }

        private static void SkipWhitespace(
            string source,
            ref int position)
        {
            while (position < source.Length &&
                char.IsWhiteSpace(source[position]))
            {
                position++;
            }
        }

        private static void SkipComment(
            string source,
            ref int position)
        {
            while (position < source.Length &&
                source[position] is not '\r' and not '\n')
            {
                position++;
            }
        }
    }

    private enum TokenKind
    {
        Number,
        Boolean,
        Operator,
        Procedure
    }

    private sealed record Token
    {
        private Token(TokenKind kind)
        {
            Kind = kind;
        }

        public TokenKind Kind { get; }

        public double Number { get; private init; }

        public bool Boolean { get; private init; }

        public string? Text { get; private init; }

        public List<Token>? Procedure { get; private init; }

        public static Token ForNumber(double value) =>
            new(TokenKind.Number)
            {
                Number = value
            };

        public static Token ForBoolean(bool value) =>
            new(TokenKind.Boolean)
            {
                Boolean = value
            };

        public static Token ForOperator(string value) =>
            new(TokenKind.Operator)
            {
                Text = value
            };

        public static Token ForProcedure(List<Token> value) =>
            new(TokenKind.Procedure)
            {
                Procedure = value
            };
    }
}
