using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Parsing;

/// <summary>
/// Provides buffered, parser-oriented access to tokens produced by a PDF
/// lexer.
/// </summary>
/// <remarks>
/// <para>
/// The reader owns token lookahead but does not own or dispose the supplied
/// lexer. The lexer must not be read independently while the reader is in use.
/// </para>
/// <para>
/// Lookahead is zero-based: <see cref="Peek()"/> and <c>Peek(0)</c> return the
/// next unread token. Reading past the logical end of the source repeatedly
/// returns the same end-of-file token.
/// </para>
/// <para>
/// This type is not thread-safe.
/// </para>
/// </remarks>
public sealed class PdfTokenReader
{
    private const int DefaultBufferCapacity = 8;

    private IPdfLexer lexer;

    private PdfToken[] buffer;
    private int head;
    private int count;
    private PdfToken endOfFileToken;
    private bool endOfFileWasRead;
    private PdfToken current;
    private bool hasCurrent;

    /// <summary>
    /// Initializes a new PDF token reader.
    /// </summary>
    /// <param name="lexer">
    /// The lexer from which tokens will be read.
    /// </param>
    public PdfTokenReader(
        IPdfLexer lexer)
        : this(
            lexer,
            DefaultBufferCapacity)
    {
    }

    /// <summary>
    /// Initializes a new PDF token reader with the specified initial lookahead
    /// capacity.
    /// </summary>
    /// <param name="lexer">
    /// The lexer from which tokens will be read.
    /// </param>
    /// <param name="initialBufferCapacity">
    /// The initial number of tokens that can be buffered without growing the
    /// internal ring buffer.
    /// </param>
    public PdfTokenReader(
        IPdfLexer lexer,
        int initialBufferCapacity)
    {
        ArgumentNullException.ThrowIfNull(lexer);

        if (initialBufferCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialBufferCapacity),
                initialBufferCapacity,
                "The initial buffer capacity must be at least one.");
        }

        this.lexer = lexer;
        buffer = new PdfToken[initialBufferCapacity];
    }

    /// <summary>
    /// Replaces the lexer and clears all buffered reader state.
    /// </summary>
    /// <param name="lexer">
    /// The lexer that will supply tokens after the reset.
    /// </param>
    /// <remarks>
    /// This internal operation is used when a parser consumes raw source bytes
    /// that cannot be represented as ordinary lexical tokens.
    /// </remarks>
    internal void Reset(
        IPdfLexer lexer)
    {
        ArgumentNullException.ThrowIfNull(lexer);

        this.lexer = lexer;

        Array.Clear(buffer);
        head = 0;
        count = 0;
        endOfFileToken = default;
        endOfFileWasRead = false;
        current = default;
        hasCurrent = false;
    }

    /// <summary>
    /// Gets a value indicating whether the next unread token is the logical
    /// end-of-file token.
    /// </summary>
    public bool EndOfFile =>
        Peek().Kind == PdfTokenKind.EndOfFile;

    /// <summary>
    /// Gets a value indicating whether at least one token has been consumed.
    /// </summary>
    public bool HasCurrent =>
        hasCurrent;

    /// <summary>
    /// Gets the most recently consumed token.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No token has been consumed.
    /// </exception>
    public PdfToken Current
    {
        get
        {
            if (!hasCurrent)
            {
                throw new InvalidOperationException(
                    "No token has been consumed by the reader.");
            }

            return current;
        }
    }

    /// <summary>
    /// Gets the source position at which the next unread token begins.
    /// </summary>
    public PdfSourcePosition Position =>
        Peek().Span.Start;

    /// <summary>
    /// Returns the next unread token without consuming it.
    /// </summary>
    /// <returns>
    /// The next unread token.
    /// </returns>
    public PdfToken Peek()
    {
        return Peek(lookahead: 0);
    }

    /// <summary>
    /// Returns a token at the specified zero-based lookahead distance without
    /// consuming any tokens.
    /// </summary>
    /// <param name="lookahead">
    /// The zero-based distance from the next unread token.
    /// </param>
    /// <returns>
    /// The token at the requested lookahead distance, or the stable
    /// end-of-file token when the distance extends beyond the source.
    /// </returns>
    public PdfToken Peek(
        int lookahead)
    {
        if (lookahead < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lookahead),
                lookahead,
                "Lookahead cannot be negative.");
        }

        EnsureBuffered(lookahead + 1);

        if (lookahead >= count)
        {
            return endOfFileToken;
        }

        return buffer[GetPhysicalIndex(lookahead)];
    }

    /// <summary>
    /// Consumes and returns the next unread token.
    /// </summary>
    /// <returns>
    /// The consumed token. Once the source is exhausted, every call returns
    /// the same end-of-file token.
    /// </returns>
    public PdfToken Read()
    {
        EnsureBuffered(requiredCount: 1);

        PdfToken token;

        if (count == 0)
        {
            token = endOfFileToken;
        }
        else
        {
            token = buffer[head];
            buffer[head] = default;
            head = (head + 1) % buffer.Length;
            count--;
        }

        current = token;
        hasCurrent = true;

        return token;
    }

    /// <summary>
    /// Consumes the next token when it has the specified kind.
    /// </summary>
    /// <param name="kind">
    /// The token kind to match.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a matching token was consumed; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Match(
        PdfTokenKind kind)
    {
        ValidateRequestedKind(kind);

        if (Peek().Kind != kind)
        {
            return false;
        }

        Read();
        return true;
    }

    /// <summary>
    /// Consumes and returns the next token when it has the specified kind.
    /// </summary>
    /// <param name="kind">
    /// The token kind required by the caller.
    /// </param>
    /// <returns>
    /// The consumed token.
    /// </returns>
    /// <exception cref="PdfUnexpectedTokenException">
    /// The next token does not have the requested kind.
    /// </exception>
    public PdfToken Expect(
        PdfTokenKind kind)
    {
        ValidateRequestedKind(kind);

        PdfToken token = Peek();

        if (token.Kind != kind)
        {
            throw new PdfUnexpectedTokenException(
                kind,
                token);
        }

        return Read();
    }

    private void EnsureBuffered(
        int requiredCount)
    {
        while (
            count < requiredCount &&
            !endOfFileWasRead)
        {
            PdfToken token = lexer.ReadToken();

            if (token.Kind == PdfTokenKind.EndOfFile)
            {
                endOfFileToken = token;
                endOfFileWasRead = true;
                break;
            }

            EnsureCapacity(count + 1);
            buffer[GetPhysicalIndex(count)] = token;
            count++;
        }
    }

    private void EnsureCapacity(
        int requiredCapacity)
    {
        if (requiredCapacity <= buffer.Length)
        {
            return;
        }

        int newCapacity = checked(buffer.Length * 2);

        if (newCapacity < requiredCapacity)
        {
            newCapacity = requiredCapacity;
        }

        var newBuffer = new PdfToken[newCapacity];

        for (int index = 0; index < count; index++)
        {
            newBuffer[index] = buffer[GetPhysicalIndex(index)];
        }

        buffer = newBuffer;
        head = 0;
    }

    private int GetPhysicalIndex(
        int logicalIndex)
    {
        return (head + logicalIndex) % buffer.Length;
    }

    private static void ValidateRequestedKind(
        PdfTokenKind kind)
    {
        if (kind == PdfTokenKind.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "A requested token kind must be assigned.");
        }
    }
}

