namespace Cerneala.UI.Text;

[Flags]
public enum TextDecorations
{
    None = 0,
    Underline = 1,
    Strikethrough = 2,
    Overline = 4,
    Baseline = 8
}

internal static class TextDecorationsValidation
{
    private const TextDecorations All =
        TextDecorations.Underline | TextDecorations.Strikethrough | TextDecorations.Overline | TextDecorations.Baseline;

    // A flags value is valid when it only holds the defined decorations.
    internal static bool IsDefined(TextDecorations value) => (value & ~All) == TextDecorations.None;
}
