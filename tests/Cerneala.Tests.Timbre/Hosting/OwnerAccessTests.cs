using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Timbre.Hosting;

// Owner access runs on the test thread that owns the UI roots, so these tests
// stay synchronous and block only on engine tasks completed by the mixer.
public sealed class OwnerAccessTests
{
    [Fact]
    public void ElementTimbreRequiresAnAttachedElementAndARootRuntime()
    {
        PlainElement detached = new();
        Assert.Throws<InvalidOperationException>(() => detached.Timbre);

        UIRoot root = new();
        PlainElement element = new();
        root.VisualChildren.Add(element);
        Assert.Null(root.TimbreRuntime);
        Assert.Throws<InvalidOperationException>(() => element.Timbre);
    }

    [Fact]
    public void ElementScopeIsPerLifecycleAndDetachCancelsItsPlaybacks()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetTimbreRuntime(rig.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000));

        TimbreScope scope = element.Timbre;
        Assert.Same(scope, element.Timbre);
        Assert.Same(rig.Runtime, scope.Runtime);
        TimbrePlayback playback = scope.Play(clip);
        TimbrePlayback rootPlayback = root.Timbre.Play(clip);

        root.VisualChildren.Remove(element);

        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        Assert.True(scope.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => scope.Play(clip));
        Assert.Throws<InvalidOperationException>(() => element.Timbre);
        Assert.Equal(TimbrePlaybackState.Pending, rootPlayback.State);

        root.VisualChildren.Add(element);
        TimbreScope reattached = element.Timbre;
        Assert.NotSame(scope, reattached);
        Assert.False(reattached.IsDisposed);
        Assert.Equal(TimbrePlaybackState.Canceled, playback.State); // not resurrected
    }

    [Fact]
    public void HidingAnElementOrAncestorDoesNotCancelItsAudio()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetTimbreRuntime(rig.Runtime);
        PlainElement parent = new();
        PlainElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        TimbrePlayback playback = child.Timbre.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));

        parent.Visibility = Visibility.Collapsed;
        child.IsVisible = false;

        Assert.Equal(TimbrePlaybackState.Pending, playback.State);
        Assert.False(child.Timbre.IsDisposed);
    }

    [Fact]
    public void ChangingTheRootRuntimeRetiresElementScopes()
    {
        using TimbreRig first = new();
        using TimbreRig second = new();
        UIRoot root = new();
        root.SetTimbreRuntime(first.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);
        TimbreScope oldScope = element.Timbre;
        TimbrePlayback playback = oldScope.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));

        root.SetTimbreRuntime(first.Runtime);
        Assert.False(oldScope.IsDisposed);
        root.SetTimbreRuntime(second.Runtime);

        Assert.True(oldScope.IsDisposed);
        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        Assert.Same(second.Runtime, element.Timbre.Runtime);
        Assert.False(first.Runtime.IsDisposed); // the root never owns a runtime

        root.SetTimbreRuntime(null);
        Assert.Null(root.TimbreRuntime);
        Assert.Throws<InvalidOperationException>(() => element.Timbre);
    }

    [Fact]
    public async Task ElementTimbreRequiresTheOwnerThread()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetTimbreRuntime(rig.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);

        Exception? failure = await Task.Run(() => Record.Exception(() => element.Timbre));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public void SceneNodesUseTheSameElementPath()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetTimbreRuntime(rig.Runtime);
        RenderSurface2D surface = new();
        Scene2D scene = new();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);

        TimbrePlayback playback = scene.Timbre.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));

        Assert.Same(rig.Runtime, scene.Timbre.Runtime);
        Assert.Equal(TimbrePlaybackState.Pending, playback.State);
    }

    [Fact]
    public void HostedRootReceivesTheRuntimeFromUiHostOptionsWithoutAnApplication()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root, TimbreRuntime = rig.Runtime });
        UIRoot replacement = new();
        replacement.SetTimbreRuntime(null);
        host.SetRoot(replacement);

        Assert.Same(rig.Runtime, root.TimbreRuntime);
        Assert.Same(rig.Runtime, replacement.TimbreRuntime);
        Assert.Null(Application.Current);

        UIRoot untouched = new();
        using TimbreRig other = new();
        untouched.SetTimbreRuntime(other.Runtime);
        _ = new UiHost(new UiHostOptions { Root = untouched });
        Assert.Same(other.Runtime, untouched.TimbreRuntime);
    }

    [Fact]
    public void ApplicationCreatesItsRuntimeLazilyAndDisposesItOnExit()
    {
        Application app = new();
        TimbreRuntime runtime = app.TimbreRuntime;
        TimbreScope sounds = app.Timbre;
        Assert.Same(runtime, app.TimbreRuntime);
        Assert.Same(sounds, app.Timbre);
        Assert.Same(runtime, sounds.Runtime);
        Assert.Throws<InvalidOperationException>(() => app.TimbreRuntime = new TimbreRuntime());
        TimbrePlayback playback = sounds.Play(TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000)));

        app.CompleteExit();

        Assert.True(sounds.IsDisposed);
        Assert.True(runtime.IsDisposed);
        // The default runtime has no output: the playback either already failed
        // with DeviceUnavailable or was canceled by the exit; it never survives it.
        Assert.True(playback.Completion.IsCompleted);
        Assert.Contains(playback.State, new[] { TimbrePlaybackState.Canceled, TimbrePlaybackState.Failed });
        Assert.Throws<ObjectDisposedException>(() => app.Timbre);
        Assert.Throws<ObjectDisposedException>(() => app.TimbreRuntime);
    }

    [Fact]
    public async Task DetectiveCapturesARootsTimbreRuntimeWithoutDrivingIt()
    {
        UIRoot root = new();
        Assert.Null(root.Detective.CaptureTimbre());

        using TimbreRig rig = new();
        root.SetTimbreRuntime(rig.Runtime);
        TimbreDiagnosticsSnapshot idle = root.Detective.CaptureTimbre()!;
        Assert.Equal(new TimbreDiagnosticsSnapshot(false, 0, 0, 0, 0, 0, 0, 0), idle);
        Assert.Equal(0, rig.Output.OpenCount);

        TimbrePlayback playback = rig.Scope.Play(new TimbreSound(
            TimbreSource.FromReader(new DeterministicTimbreSourceFactory(48000).Open),
            loading: TimbreLoading.Preload,
            modifiers: [new Delay(time: 0.01f)]));
        await rig.StartAsync(playback);
        TimbreDiagnosticsSnapshot active = root.Detective.CaptureTimbre()!;

        Assert.True(active.OutputOpen);
        Assert.Equal(1, active.ActivePlaybacks);
        Assert.Equal(48000L * 8, active.CacheBytes);
        Assert.Equal(2L * 480 * sizeof(float), active.DspStateBytes);
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }

    [Fact]
    public void ApplicationKeepsAnAssignedRuntimeCallerOwned()
    {
        using TimbreRig rig = new();
        Application app = new() { TimbreRuntime = rig.Runtime };
        TimbreScope sounds = app.Timbre;

        app.CompleteExit();

        Assert.True(sounds.IsDisposed);
        Assert.False(rig.Runtime.IsDisposed);
        Assert.Throws<ArgumentNullException>(() => new Application().TimbreRuntime = null!);
    }

    private sealed class PlainElement : UIElement
    {
    }
}
