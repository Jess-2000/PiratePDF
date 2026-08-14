namespace BlackFlag.Engine.Editing.Structural;

/// <summary>
/// Captures an immutable structural value together with the way that value is
/// represented in the logical document.
/// </summary>
/// <typeparam name="T">The structural value type.</typeparam>
/// <remarks>
/// Origin is part of the state. An inherited value and an explicit value with
/// identical payloads are intentionally different states so rollback, undo,
/// and redo can restore exact logical representation rather than only an
/// equivalent effective value.
/// </remarks>
public readonly struct PdfStructuralValueState<T> :
    IEquatable<PdfStructuralValueState<T>>
    where T : notnull
{
    private readonly T? value;

    internal PdfStructuralValueState(
        PdfStructuralValueOrigin origin,
        T? value)
    {
        Origin = origin;
        this.value = value;
    }

    /// <summary>Gets the representation origin captured by this state.</summary>
    public PdfStructuralValueOrigin Origin { get; }

    /// <summary>Gets whether this state carries an effective value.</summary>
    public bool HasValue =>
        Origin != PdfStructuralValueOrigin.Absent;

    /// <summary>
    /// Gets the captured value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the structural value is absent.
    /// </exception>
    public T Value =>
        HasValue
            ? value!
            : throw new InvalidOperationException(
                "The structural value is absent.");

    /// <inheritdoc />
    public bool Equals(
        PdfStructuralValueState<T> other)
    {
        return Origin == other.Origin &&
            EqualityComparer<T?>.Default.Equals(
                value,
                other.value);
    }

    /// <inheritdoc />
    public override bool Equals(
        object? obj)
    {
        return obj is PdfStructuralValueState<T> other &&
            Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Origin,
            value);
    }

    public static bool operator ==(
        PdfStructuralValueState<T> left,
        PdfStructuralValueState<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        PdfStructuralValueState<T> left,
        PdfStructuralValueState<T> right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Creates exact structural value states without placing static factory
/// members on the generic state type.
/// </summary>
public static class PdfStructuralValueState
{
    /// <summary>
    /// Creates a state representing a value that is absent at the targeted
    /// location.
    /// </summary>
    public static PdfStructuralValueState<T> Absent<T>()
        where T : notnull
    {
        return new PdfStructuralValueState<T>(
            PdfStructuralValueOrigin.Absent,
            default);
    }

    /// <summary>Creates a state for an explicitly declared value.</summary>
    public static PdfStructuralValueState<T> Explicit<T>(
        T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PdfStructuralValueState<T>(
            PdfStructuralValueOrigin.Explicit,
            value);
    }

    /// <summary>Creates a state for an inherited effective value.</summary>
    public static PdfStructuralValueState<T> Inherited<T>(
        T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PdfStructuralValueState<T>(
            PdfStructuralValueOrigin.Inherited,
            value);
    }

    /// <summary>Creates a state for a PDF-defined fallback value.</summary>
    public static PdfStructuralValueState<T> Defaulted<T>(
        T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PdfStructuralValueState<T>(
            PdfStructuralValueOrigin.Defaulted,
            value);
    }
}
