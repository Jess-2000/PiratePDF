namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes a directly serializable device-space PDF color for an
/// existing-text style operation.
/// </summary>
public sealed class PdfExistingTextColor
{
    private readonly double[] components;

    private PdfExistingTextColor(
        string colorSpaceName,
        params double[] components)
    {
        ColorSpaceName = colorSpaceName;
        this.components = components;
        Validate();
    }

    public string ColorSpaceName { get; }
    public IReadOnlyList<double> Components => components;

    public static PdfExistingTextColor Gray(double gray) =>
        new("DeviceGray", gray);

    public static PdfExistingTextColor Rgb(
        double red,
        double green,
        double blue) =>
        new("DeviceRGB", red, green, blue);

    public static PdfExistingTextColor Cmyk(
        double cyan,
        double magenta,
        double yellow,
        double black) =>
        new("DeviceCMYK", cyan, magenta, yellow, black);

    private void Validate()
    {
        int expected =
            ColorSpaceName switch
            {
                "DeviceGray" => 1,
                "DeviceRGB" => 3,
                "DeviceCMYK" => 4,
                _ => throw new NotSupportedException(
                    $"Existing-text direct color '{ColorSpaceName}' is not supported.")
            };

        if (components.Length != expected)
        {
            throw new ArgumentException(
                $"Color '{ColorSpaceName}' requires {expected} components.");
        }

        for (int index = 0; index < components.Length; index++)
        {
            double value = components[index];

            if (!double.IsFinite(value) ||
                value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(components),
                    value,
                    "Device color components must be finite values from 0 through 1.");
            }
        }
    }
}
