using System.Collections.ObjectModel;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Parsing;

/// <summary>
/// Represents a parser-level error caused by encountering a token of an
/// unexpected lexical kind.
/// </summary>
public sealed class PdfUnexpectedTokenException : Exception
{
    private readonly ReadOnlyCollection<PdfTokenKind> expectedKinds;

    /// <summary>
    /// Initializes a new unexpected-token exception.
    /// </summary>
    /// <param name="expectedKind">
    /// The token kind required by the caller.
    /// </param>
    /// <param name="actualToken">
    /// The token that was encountered.
    /// </param>
    public PdfUnexpectedTokenException(
        PdfTokenKind expectedKind,
        PdfToken actualToken)
        : base(CreateMessage(expectedKind, actualToken))
    {
        ValidateExpectedKind(expectedKind);

        expectedKinds =
            Array.AsReadOnly(
                [expectedKind]);

        ActualToken = actualToken;
    }

    /// <summary>
    /// Initializes a new unexpected-token exception with an underlying
    /// exception.
    /// </summary>
    /// <param name="expectedKind">
    /// The token kind required by the caller.
    /// </param>
    /// <param name="actualToken">
    /// The token that was encountered.
    /// </param>
    /// <param name="innerException">
    /// The exception that caused the parser operation to fail.
    /// </param>
    public PdfUnexpectedTokenException(
        PdfTokenKind expectedKind,
        PdfToken actualToken,
        Exception innerException)
        : base(
            CreateMessage(expectedKind, actualToken),
            innerException)
    {
        ValidateExpectedKind(expectedKind);
        ArgumentNullException.ThrowIfNull(innerException);

        expectedKinds =
            Array.AsReadOnly(
                [expectedKind]);

        ActualToken = actualToken;
    }

    /// <summary>
    /// Initializes a new unexpected-token exception for a set of accepted
    /// token kinds.
    /// </summary>
    /// <param name="expectedKinds">
    /// The token kinds accepted by the caller.
    /// </param>
    /// <param name="actualToken">
    /// The token that was encountered.
    /// </param>
    public PdfUnexpectedTokenException(
        IReadOnlyCollection<PdfTokenKind> expectedKinds,
        PdfToken actualToken)
        : base(CreateMessage(expectedKinds, actualToken))
    {
        this.expectedKinds =
            CopyAndValidateExpectedKinds(expectedKinds);

        ActualToken = actualToken;
    }

    /// <summary>
    /// Gets the first token kind required by the caller.
    /// </summary>
    /// <remarks>
    /// For exceptions created with multiple accepted kinds, use
    /// <see cref="ExpectedKinds"/> to inspect the complete set.
    /// </remarks>
    public PdfTokenKind ExpectedKind =>
        expectedKinds[0];

    /// <summary>
    /// Gets all token kinds accepted by the caller.
    /// </summary>
    public IReadOnlyList<PdfTokenKind> ExpectedKinds =>
        expectedKinds;

    /// <summary>
    /// Gets the token that was encountered.
    /// </summary>
    public PdfToken ActualToken { get; }

    /// <summary>
    /// Gets the source position at which the unexpected token begins.
    /// </summary>
    public PdfSourcePosition Position =>
        ActualToken.Span.Start;

    private static string CreateMessage(
        PdfTokenKind expectedKind,
        PdfToken actualToken)
    {
        ValidateExpectedKind(expectedKind);

        return string.Concat(
            "Expected token kind ",
            expectedKind,
            " but encountered ",
            actualToken.Kind,
            ". Offset: ",
            actualToken.Span.Start.Offset,
            ", line: ",
            actualToken.Span.Start.Line,
            ", column: ",
            actualToken.Span.Start.Column,
            ".");
    }

    private static string CreateMessage(
        IReadOnlyCollection<PdfTokenKind> expectedKinds,
        PdfToken actualToken)
    {
        ReadOnlyCollection<PdfTokenKind> validatedKinds =
            CopyAndValidateExpectedKinds(expectedKinds);

        return string.Concat(
            "Expected one of the token kinds ",
            string.Join(", ", validatedKinds),
            " but encountered ",
            actualToken.Kind,
            ". Offset: ",
            actualToken.Span.Start.Offset,
            ", line: ",
            actualToken.Span.Start.Line,
            ", column: ",
            actualToken.Span.Start.Column,
            ".");
    }

    private static ReadOnlyCollection<PdfTokenKind>
        CopyAndValidateExpectedKinds(
            IReadOnlyCollection<PdfTokenKind> expectedKinds)
    {
        ArgumentNullException.ThrowIfNull(expectedKinds);

        if (expectedKinds.Count == 0)
        {
            throw new ArgumentException(
                "At least one expected token kind must be supplied.",
                nameof(expectedKinds));
        }

        PdfTokenKind[] copiedKinds =
            new PdfTokenKind[expectedKinds.Count];

        int index = 0;

        foreach (PdfTokenKind kind in expectedKinds)
        {
            ValidateExpectedKind(kind);
            copiedKinds[index] = kind;
            index++;
        }

        return Array.AsReadOnly(copiedKinds);
    }

    private static void ValidateExpectedKind(
        PdfTokenKind expectedKind)
    {
        if (expectedKind == PdfTokenKind.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedKind),
                expectedKind,
                "An expected token kind must be assigned.");
        }
    }
}
