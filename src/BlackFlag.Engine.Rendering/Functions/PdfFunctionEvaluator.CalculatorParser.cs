
using System.Globalization;
using System.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Functions;

public sealed partial class PdfFunctionEvaluator
{
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
}
