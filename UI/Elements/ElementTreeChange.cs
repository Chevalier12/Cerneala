namespace Cerneala.UI.Elements;

public sealed class ElementTreeChange
{
    public ElementTreeChange(
        UIElement parent,
        UIElement child,
        ElementChildRole role,
        ElementTreeChangeKind kind)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Role = role;
        Kind = kind;
    }

    public UIElement Parent { get; }

    public UIElement Child { get; }

    public ElementChildRole Role { get; }

    public ElementTreeChangeKind Kind { get; }
}
