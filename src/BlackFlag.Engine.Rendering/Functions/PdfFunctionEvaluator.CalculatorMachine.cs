namespace BlackFlag.Engine.Rendering.Functions;

public sealed partial class PdfFunctionEvaluator
{
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
}
