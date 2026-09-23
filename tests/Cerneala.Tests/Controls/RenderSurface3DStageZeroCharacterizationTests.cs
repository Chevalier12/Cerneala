using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface3DStageZeroCharacterizationTests
{
    [Fact]
    public void RootImageResourceReplacementReleasesSurfaceBackendStateAndAllowsRecovery()
    {
        UIRoot root = new(32, 16);
        RenderSurface2D surface = new();
        surface.Draw += static (_, frame) => frame.FillRectangle(frame.Bounds, Color.CornflowerBlue);
        root.VisualChildren.Add(surface);
        using ImageResourceCache firstCache = new(null);
        using ImageResourceCache replacementCache = new(null);
        root.SetImageResourceCache(null, firstCache);
        object owner = new();
        RecordingBackendState firstState = new();
        IRenderSurface2DFrameSource source = surface;
        source.SetBackendState(owner, firstState);

        root.SetImageResourceCache(null, replacementCache);

        Assert.True(firstState.IsDisposed);
        Assert.Null(source.GetBackendState(owner));
        RecordingBackendState recoveredState = new();
        source.SetBackendState(owner, recoveredState);
        DrawCommandList commands = new();
        source.RecordFrame(commands, new DrawRect(0, 0, 32, 16));
        Assert.Same(recoveredState, source.GetBackendState(owner));
        Assert.Equal(DrawCommandKind.FillRectangle, Assert.Single(commands).Kind);
    }

    private sealed class RecordingBackendState : IRenderSurface2DBackendState
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
