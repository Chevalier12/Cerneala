namespace Cerneala.UI.Text;

public readonly record struct TextCompositionState(bool IsActive, int Start, string Text)
{
    public static TextCompositionState Inactive { get; } = new(false, 0, string.Empty);
}
