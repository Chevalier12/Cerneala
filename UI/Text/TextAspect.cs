using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Text;

public readonly record struct TextAspect
{
    private const float MaxTextSize = 16_384f;

    public TextAspect(
        string fontFamily,
        float fontSize,
        TextWrapping wrapping = TextWrapping.NoWrap,
        TextTrimming trimming = TextTrimming.None,
        float scale = 1,
        Brush? foreground = null,
        ResourceId<FontResource>? fontResourceId = null)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException("Font family cannot be empty.", nameof(fontFamily));
        }

        ThrowIfNotPositiveFinite(fontSize, nameof(fontSize));
        ThrowIfNotPositiveFinite(scale, nameof(scale));
        ThrowIfNotValidEffectiveTextSize(fontSize, scale, nameof(scale));
        if (!Enum.IsDefined(wrapping))
        {
            throw new ArgumentOutOfRangeException(nameof(wrapping), "Text wrapping is not supported.");
        }

        if (!Enum.IsDefined(trimming))
        {
            throw new ArgumentOutOfRangeException(nameof(trimming), "Text trimming is not supported.");
        }

        FontFamily = fontFamily;
        FontSize = fontSize;
        Foreground = foreground;
        FontResourceId = fontResourceId;
        Wrapping = wrapping;
        Trimming = trimming;
        Scale = scale;
    }

    public string FontFamily { get; }

    public float FontSize { get; }

    public Brush? Foreground { get; }

    public ResourceId<FontResource>? FontResourceId { get; }

    public TextWrapping Wrapping { get; }

    public TextTrimming Trimming { get; }

    public float Scale { get; }

    public DrawTextRun ToDrawTextRun(ResolvedTextFont font, string text)
    {
        ArgumentNullException.ThrowIfNull(font);
        return new DrawTextRun(font.Font, text ?? throw new ArgumentNullException(nameof(text)), FontSize * Scale);
    }

    private static void ThrowIfNotPositiveFinite(float value, string parameterName)
    {
        if (value <= 0 || !float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be positive and finite.");
        }

        if (value > MaxTextSize)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value exceeds the maximum supported text size.");
        }
    }

    private static void ThrowIfNotValidEffectiveTextSize(float fontSize, float scale, string parameterName)
    {
        float effectiveSize = fontSize * scale;
        if (!float.IsFinite(effectiveSize) || effectiveSize > MaxTextSize)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Effective text size exceeds the maximum supported text size.");
        }
    }
}
