namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>Identifies a graphics interpretation failure category.</summary>
public enum PdfGraphicsInterpretationFailure
{
    /// <summary>The graphics-state stack is invalid.</summary>
    InvalidGraphicsState,

    /// <summary>The current path state is invalid.</summary>
    InvalidPathState,

    /// <summary>The current text state is invalid.</summary>
    InvalidTextState,

    /// <summary>A required resource is missing or has the wrong type.</summary>
    InvalidResource,

    /// <summary>A recursive Form XObject was detected.</summary>
    RecursiveForm,

    /// <summary>Marked-content nesting is invalid.</summary>
    InvalidMarkedContent,

    /// <summary>An operation cannot be interpreted by this milestone.</summary>
    UnsupportedOperation,

    /// <summary>A configured defensive limit was exceeded.</summary>
    LimitExceeded,

    /// <summary>The content is otherwise malformed.</summary>
    MalformedGraphicsContent
}
