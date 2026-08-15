
using System.Globalization;
using System.Text;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Rendering.Functions;

public sealed partial class PdfFunctionEvaluator
{
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
