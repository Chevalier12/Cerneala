namespace Cerneala.UI.Invalidation;

[Flags]
public enum InvalidationFlags
{
    None = 0,
    Measure = 1 << 0,
    Arrange = 1 << 1,
    Render = 1 << 2,
    Text = 1 << 3,
    Image = 1 << 4,
    Resource = 1 << 5,
    Style = 1 << 6,
    InputVisual = 1 << 7,
    HitTest = 1 << 8,
    Subtree = 1 << 9,
    Inherited = 1 << 10
}
