using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

// A UI root holding one 200 x 200 surface with a scene, and a host that runs real updates over it.
// An update advances animation time, then Motion, then delivers the scene's contact events.
internal static class VisionHostRig
{
    internal static UIRoot CreateRoot(out RenderSurface2D surface, out Scene2D scene, IMotionClock? motionClock = null)
    {
        UIRoot root = new(200, 200, motionClock: motionClock);
        surface = new RenderSurface2D { Width = 200, Height = 200 };
        surface.Measure(new MeasureContext(new LayoutSize(200, 200)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        scene = new Scene2D();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        return root;
    }

    internal static UiHost CreateHost(UIRoot root)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(200, 200)
        });
        Update(host, TimeSpan.Zero);
        return host;
    }

    // One host update that reports this much elapsed time and no input.
    internal static void Update(UiHost host, TimeSpan elapsed) =>
        host.Update(
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            elapsed);
}

internal sealed class FixedImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
