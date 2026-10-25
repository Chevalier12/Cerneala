using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

// Headless host of one RenderSurface2D stress scene: a UIRoot with the surface attached, the cells realized and the child
// index built, so that frames can be driven without a window. Shared by the benchmark runners and the stress tests.
internal sealed class Rs2dHeadlessRig : IDisposable
{
    private Rs2dHeadlessRig(UIRoot root, RenderSurface2D surface, Rs2dStressScene scene, double attachMilliseconds, int realized)
    {
        Root = root;
        Surface = surface;
        Scene = scene;
        AttachMilliseconds = attachMilliseconds;
        Realized = realized;
    }

    internal UIRoot Root { get; }
    internal RenderSurface2D Surface { get; }
    internal Rs2dStressScene Scene { get; }
    internal double AttachMilliseconds { get; }
    internal int Realized { get; }

    public void Dispose()
    {
        Root.VisualChildren.Remove(Surface);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    internal static Rs2dHeadlessRig Create(Rs2dStressSpec spec, int n)
    {
        Rs2dStressScene scene = Rs2dStressScene.Create(spec, n, new ImageReference(new StubImage()));
        Stopwatch stopwatch = Stopwatch.StartNew();
        UIRoot root = new(Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight);
        RenderSurface2D surface = new()
        {
            ViewBox = new DrawRect(scene.CameraX, scene.CameraY, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight),
            RedrawMode = spec.Redraw
        };
        surface.Scene = scene.Scene;
        if (spec.Kind == Rs2dStressKind.TileMaps)
        {
            root.SetImageLoader(new MapLoader());
            surface.Resources.SetResource(new ResourceId<ImageResource>("Atlas"), new ImageResource("atlas.png"));
        }
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        int realized = 0;
        if (scene.Items is { } items)
        {
            for (int guard = 0; guard < 2000 && items.RealizedItemCount < n; guard++)
            {
                TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
                root.ProcessFrame();
            }
            realized = items.RealizedItemCount;
            if (realized != n) { throw new InvalidOperationException($"{spec.Name} N={n}: realized {realized} items."); }
        }
        if (scene.SingleItems.Count > 0)
        {
            long total = 0;
            for (int guard = 0; guard < 2000; guard++)
            {
                total = 0;
                foreach (SceneItems2D one in scene.SingleItems) { total += one.RealizedItemCount; }
                if (total >= n) { break; }
                TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
                root.ProcessFrame();
            }
            realized = (int)total;
            if (total != n) { throw new InvalidOperationException($"{spec.Name} N={n}: realized {total} items."); }
        }
        if (spec.Kind == Rs2dStressKind.TileMaps)
        {
            bool ready = false;
            for (int guard = 0; guard < 5000 && !ready; guard++)
            {
                TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
                root.ProcessFrame();
                DrawCommandList probe = new();
                ((IRenderSurface2DFrameSource)surface).RecordFrame(probe,
                    new DrawRect(0, 0, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight));
                ready = surface.PresentationState == RenderSurface2DPresentationState.Ready &&
                    Rs2dStressScene.CountSprites(probe) > 0;
                if (!ready) { Thread.Sleep(1); }
            }
            if (!ready) { throw new InvalidOperationException($"{spec.Name} N={n}: tile maps never became ready."); }
        }
        // The first record builds the child index; it belongs to attach, not to the measured frames.
        DrawCommandList first = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(first,
            new DrawRect(0, 0, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight));
        double attach = stopwatch.Elapsed.TotalMilliseconds;
        if (surface.ArrangedBounds.Width != Rs2dStressScene.ViewWidth || surface.ArrangedBounds.Height != Rs2dStressScene.ViewHeight)
        {
            throw new InvalidOperationException($"{spec.Name} N={n}: the surface was not arranged at the camera size.");
        }
        return new Rs2dHeadlessRig(root, surface, scene, attach, realized);
    }

    private sealed class StubImage : IDrawImage
    {
        public int Width => 64;
        public int Height => 16;
    }

    private sealed class MapLoader : IAsyncImageLoader
    {
        public IDrawImage Load(string path) => new MapImage();
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
    }

    private sealed class MapImage : IDrawImage
    {
        public int Width => 64;
        public int Height => 64;
    }
}
