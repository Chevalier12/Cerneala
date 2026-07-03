namespace Cerneala.UI.Text;

public readonly record struct TextLayoutKey(
    string Text,
    string FontIdentity,
    float FontSize,
    TextWrapping Wrapping,
    float WrappingWidth,
    TextTrimming Trimming,
    float Scale);
