namespace BlackFlag.Engine.Parsing.ContentStreams;

/// <summary>Identifies a content-stream parsing failure category.</summary>
public enum PdfContentParseFailure
{
    UnexpectedToken,
    InvalidOperand,
    OperandCountMismatch,
    UnsupportedOperator,
    UnterminatedInlineImage,
    LimitExceeded,
    UnbalancedGraphicsState,
    MalformedContent
}
