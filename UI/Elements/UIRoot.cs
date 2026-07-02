namespace Cerneala.UI.Elements;

public sealed class UIRoot : UIElement, IElementHost
{
    public UIRoot(float viewportWidth = 0, float viewportHeight = 0, float scale = 1)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        ElementIds = new ElementIdProvider();
        ElementLifecycle.AttachSubtree(this, this);
    }

    UIRoot IElementHost.Root => this;

    public float ViewportWidth { get; private set; }

    public float ViewportHeight { get; private set; }

    public float Scale { get; private set; }

    public int TreeVersion { get; private set; }

    public ElementIdProvider ElementIds { get; }

    public void SetViewport(float width, float height, float scale)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        Scale = scale;
        IncrementTreeVersion();
    }

    internal void IncrementTreeVersion()
    {
        TreeVersion++;
    }
}
