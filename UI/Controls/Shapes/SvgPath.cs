using System.Globalization;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls.Shapes;

public sealed class SvgPath : Shape
{
    public static readonly UiProperty<string> DataProperty = UiProperty<string>.Register(
        nameof(Data),
        typeof(SvgPath),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<string> ViewBoxProperty = UiProperty<string>.Register(
        nameof(ViewBox),
        typeof(SvgPath),
        new UiPropertyMetadata<string>(
            "0 0 1 1",
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: IsValidViewBox));

    private string? _cachedData;
    private string? _cachedViewBox;
    private SvgGeometry? _cachedGeometry;

    public string Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public string ViewBox
    {
        get => GetValue(ViewBoxProperty);
        set => SetValue(ViewBoxProperty, value);
    }

    protected override Geometry? ResolveGeometry(LayoutRect arrangedBounds)
    {
        string data = Data;
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        string viewBox = ViewBox;
        if (_cachedGeometry is null || _cachedData != data || _cachedViewBox != viewBox)
        {
            _cachedData = data;
            _cachedViewBox = viewBox;
            _cachedGeometry = new SvgGeometry(data, ParseViewBox(viewBox));
        }

        return _cachedGeometry;
    }

    private static bool IsValidViewBox(string value)
    {
        return TryParseViewBox(value, out _);
    }

    private static DrawRect ParseViewBox(string value)
    {
        if (!TryParseViewBox(value, out DrawRect viewBox))
        {
            throw new FormatException($"'{value}' is not a valid SVG viewBox.");
        }

        return viewBox;
    }

    private static bool TryParseViewBox(string? value, out DrawRect viewBox)
    {
        viewBox = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split([' ', '\t', '\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4
            || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
            || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
            || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float width)
            || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float height)
            || !float.IsFinite(x)
            || !float.IsFinite(y)
            || !float.IsFinite(width)
            || !float.IsFinite(height)
            || width <= 0
            || height <= 0)
        {
            return false;
        }

        viewBox = new DrawRect(x, y, width, height);
        return true;
    }
}
