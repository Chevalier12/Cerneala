using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
namespace Cerneala.Tests.Controls;
public sealed class SpriteResourceProviderCompatibilityTests
{
    [Fact]
    public void ExplicitFrameInvalidationResolvesChangedNonObservableImageProvider()
    {
        ResourceId<ImageResource> id = new("atlas");
        TestImage first = new(), second = new();
        Provider provider = new() { Current = new ImageResource(first) };
        Sprite2D sprite = new() { Image = new(id), Width = 16, Height = 16 };
        global::Cerneala.UI.Controls.Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new(100, 100);
        root.SetResourceProvider(provider);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Assert.Same(first, RecordImage());
            provider.Current = new ImageResource(second);
            surface.InvalidateFrame();
            Assert.Same(second, RecordImage());
        }
        finally { root.VisualChildren.Remove(surface); }
        IDrawImage? RecordImage()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new(0, 0, 100, 100));
            return Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawImage)).Image;
        }
    }
    private sealed class Provider : IResourceProvider
    {
        internal required ImageResource Current { get; set; }
        public bool TryGetResource<T>(ResourceId<T> id, out T resource)
        {
            if (id.Key == "atlas" && Current is T value) { resource = value; return true; }
            resource = default!;
            return false;
        }
    }
    private sealed class TestImage : IDrawImage { public int Width => 16; public int Height => 16; }
}
