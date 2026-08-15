using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Tests.Structural;

public sealed class PdfStructuralValueStateTests
{
    [Fact]
    public void AbsentStateDoesNotExposeValue()
    {
        var state =
            PdfStructuralValueState.Absent<string>();

        Assert.Equal(
            PdfStructuralValueOrigin.Absent,
            state.Origin);
        Assert.False(state.HasValue);
        Assert.Throws<InvalidOperationException>(
            () => _ = state.Value);
    }

    [Theory]
    [InlineData(PdfStructuralValueOrigin.Explicit)]
    [InlineData(PdfStructuralValueOrigin.Inherited)]
    [InlineData(PdfStructuralValueOrigin.Defaulted)]
    public void ValueStatesPreserveOrigin(
        PdfStructuralValueOrigin origin)
    {
        var state =
            origin switch
            {
                PdfStructuralValueOrigin.Explicit =>
                    PdfStructuralValueState.Explicit("value"),
                PdfStructuralValueOrigin.Inherited =>
                    PdfStructuralValueState.Inherited("value"),
                PdfStructuralValueOrigin.Defaulted =>
                    PdfStructuralValueState.Defaulted("value"),
                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(origin),
                        origin,
                        "The test origin must carry a value.")
            };

        Assert.Equal(origin, state.Origin);
        Assert.True(state.HasValue);
        Assert.Equal("value", state.Value);
    }

    [Fact]
    public void EqualPayloadWithDifferentOriginIsDifferentState()
    {
        var explicitState =
            PdfStructuralValueState.Explicit(90);
        var inheritedState =
            PdfStructuralValueState.Inherited(90);

        Assert.NotEqual(
            explicitState,
            inheritedState);
    }

    [Fact]
    public void NullReferenceValuesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => PdfStructuralValueState.Explicit<string>(null!));
        Assert.Throws<ArgumentNullException>(
            () => PdfStructuralValueState.Inherited<string>(null!));
        Assert.Throws<ArgumentNullException>(
            () => PdfStructuralValueState.Defaulted<string>(null!));
    }
}
