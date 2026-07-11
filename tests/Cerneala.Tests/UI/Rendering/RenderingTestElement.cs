using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

internal sealed class RenderingTestElement : UIElement
{
    private readonly Color color;
    private readonly bool throwOnRender;

    public RenderingTestElement(Color color, bool throwOnRender = false)
    {
        this.color = color;
        this.throwOnRender = throwOnRender;
        Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
    }

    public int RenderCount { get; private set; }

    public void ChangeDependencies(RenderDependency dependencies)
    {
        SetRenderDependencies(dependencies);
    }

    protected override void OnRender(RenderContext context)
    {
        RenderCount++;
        if (throwOnRender)
        {
            throw new InvalidOperationException("Render failed.");
        }

        context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), color);
    }
}
