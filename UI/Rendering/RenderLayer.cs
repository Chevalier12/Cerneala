namespace Cerneala.UI.Rendering;

public readonly record struct RenderLayer(float Opacity = 1)
{
    public static RenderLayer Default { get; } = new(Opacity: 1);

    public bool IsVisible => Opacity > 0;
}
