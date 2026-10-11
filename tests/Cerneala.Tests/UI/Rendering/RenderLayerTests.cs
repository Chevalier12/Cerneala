using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderLayerTests
{
    [Fact]
    public void DefaultLayerIsFullyOpaqueAndVisible()
    {
        Assert.Equal(1f, RenderLayer.Default.Opacity);
        Assert.True(RenderLayer.Default.IsVisible);
        Assert.Equal(new RenderLayer(1), RenderLayer.Default);
    }

    [Fact]
    public void ElementRenderCachePassesTheDefaultLayerToRender()
    {
        LayerCapturingElement element = new();
        using ElementRenderCache cache = new();

        cache.Ensure(element, new RenderCounters());

        Assert.Equal(1f, element.Layer?.Opacity);
        Assert.True(element.Layer?.IsVisible);
    }

    private sealed class LayerCapturingElement : UIElement
    {
        internal RenderLayer? Layer { get; private set; }

        protected override void OnRender(RenderContext context)
        {
            Layer = context.Layer;
        }
    }
}
