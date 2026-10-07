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
    public void ElementSoundsRequiresAnAttachedElementAndARootRuntime()
    {
        PlainElement detached = new();
        Assert.Throws<InvalidOperationException>(() => detached.Sounds);

        UIRoot root = new();
        PlainElement element = new();
        root.VisualChildren.Add(element);
        Assert.Null(root.SoundRuntime);
        Assert.Throws<InvalidOperationException>(() => element.Sounds);
    }

    [Fact]
    public void ElementScopeIsPerLifecycleAndDetachCancelsItsPlaybacks()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetSoundRuntime(rig.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);
        SoundClip clip = TimbreRig.Clip(new DeterministicSoundSourceFactory(48000));

        SoundScope scope = element.Sounds;
        Assert.Same(scope, element.Sounds);
        Assert.Same(rig.Runtime, scope.Runtime);
        SoundPlayback playback = scope.Play(clip);
        SoundPlayback rootPlayback = root.Sounds.Play(clip);

        root.VisualChildren.Remove(element);

        Assert.Equal(SoundPlaybackState.Canceled, playback.State);
        Assert.True(scope.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => scope.Play(clip));
        Assert.Throws<InvalidOperationException>(() => element.Sounds);
        Assert.Equal(SoundPlaybackState.Pending, rootPlayback.State);

        root.VisualChildren.Add(element);
        SoundScope reattached = element.Sounds;
        Assert.NotSame(scope, reattached);
        Assert.False(reattached.IsDisposed);
        Assert.Equal(SoundPlaybackState.Canceled, playback.State); // not resurrected
    }

    [Fact]
    public void HidingAnElementOrAncestorDoesNotCancelItsAudio()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetSoundRuntime(rig.Runtime);
        PlainElement parent = new();
        PlainElement child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        SoundPlayback playback = child.Sounds.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));

        parent.Visibility = Visibility.Collapsed;
        child.IsVisible = false;

        Assert.Equal(SoundPlaybackState.Pending, playback.State);
        Assert.False(child.Sounds.IsDisposed);
    }

    [Fact]
    public void ChangingTheRootRuntimeRetiresElementScopes()
    {
        using TimbreRig first = new();
        using TimbreRig second = new();
        UIRoot root = new();
        root.SetSoundRuntime(first.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);
        SoundScope oldScope = element.Sounds;
        SoundPlayback playback = oldScope.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));

        root.SetSoundRuntime(first.Runtime);
        Assert.False(oldScope.IsDisposed);
        root.SetSoundRuntime(second.Runtime);

        Assert.True(oldScope.IsDisposed);
        Assert.Equal(SoundPlaybackState.Canceled, playback.State);
        Assert.Same(second.Runtime, element.Sounds.Runtime);
        Assert.False(first.Runtime.IsDisposed); // the root never owns a runtime

        root.SetSoundRuntime(null);
        Assert.Null(root.SoundRuntime);
        Assert.Throws<InvalidOperationException>(() => element.Sounds);
    }

    [Fact]
    public async Task ElementSoundsRequiresTheOwnerThread()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetSoundRuntime(rig.Runtime);
        PlainElement element = new();
        root.VisualChildren.Add(element);

        Exception? failure = await Task.Run(() => Record.Exception(() => element.Sounds));

        Assert.IsType<InvalidOperationException>(failure);
    }

    [Fact]
    public void SceneNodesUseTheSameElementPath()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        root.SetSoundRuntime(rig.Runtime);
        RenderSurface2D surface = new();
        Scene2D scene = new();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);

        SoundPlayback playback = scene.Sounds.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));

        Assert.Same(rig.Runtime, scene.Sounds.Runtime);
        Assert.Equal(SoundPlaybackState.Pending, playback.State);
    }

    [Fact]
    public void HostedRootReceivesTheRuntimeFromUiHostOptionsWithoutAnApplication()
    {
        using TimbreRig rig = new();
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root, SoundRuntime = rig.Runtime });
        UIRoot replacement = new();
        replacement.SetSoundRuntime(null);
        host.SetRoot(replacement);

        Assert.Same(rig.Runtime, root.SoundRuntime);
        Assert.Same(rig.Runtime, replacement.SoundRuntime);
        Assert.Null(Application.Current);

        UIRoot untouched = new();
        using TimbreRig other = new();
        untouched.SetSoundRuntime(other.Runtime);
        _ = new UiHost(new UiHostOptions { Root = untouched });
        Assert.Same(other.Runtime, untouched.SoundRuntime);
    }

    [Fact]
    public void ApplicationCreatesItsRuntimeLazilyAndDisposesItOnExit()
    {
        Application app = new();
        SoundRuntime runtime = app.SoundRuntime;
        SoundScope sounds = app.Sounds;
        Assert.Same(runtime, app.SoundRuntime);
        Assert.Same(sounds, app.Sounds);
        Assert.Same(runtime, sounds.Runtime);
        Assert.Throws<InvalidOperationException>(() => app.SoundRuntime = new SoundRuntime());
        SoundPlayback playback = sounds.Play(TimbreRig.Clip(new DeterministicSoundSourceFactory(48000)));

        app.CompleteExit();

        Assert.True(sounds.IsDisposed);
        Assert.True(runtime.IsDisposed);
        // The default runtime has no output: the playback either already failed
        // with DeviceUnavailable or was canceled by the exit; it never survives it.
        Assert.True(playback.Completion.IsCompleted);
        Assert.Contains(playback.State, new[] { SoundPlaybackState.Canceled, SoundPlaybackState.Failed });
        Assert.Throws<ObjectDisposedException>(() => app.Sounds);
        Assert.Throws<ObjectDisposedException>(() => app.SoundRuntime);
    }

    [Fact]
    public async Task DetectiveCapturesARootsSoundRuntimeWithoutDrivingIt()
    {
        UIRoot root = new();
        Assert.Null(root.Detective.CaptureSound());

        using TimbreRig rig = new();
        root.SetSoundRuntime(rig.Runtime);
        SoundDiagnosticsSnapshot idle = root.Detective.CaptureSound()!;
        Assert.Equal(new SoundDiagnosticsSnapshot(false, 0, 0, 0, 0, 0, 0, 0), idle);
        Assert.Equal(0, rig.Output.OpenCount);

        SoundPlayback playback = rig.Scope.Play(new SoundClip(
            SoundSource.FromReader(new DeterministicSoundSourceFactory(48000).Open),
            loading: SoundLoading.Preload,
            modifiers: [new Delay(time: 0.01f)]));
        await rig.StartAsync(playback);
        SoundDiagnosticsSnapshot active = root.Detective.CaptureSound()!;

        Assert.True(active.OutputOpen);
        Assert.Equal(1, active.ActivePlaybacks);
        Assert.Equal(48000L * 8, active.CacheBytes);
        Assert.Equal(2L * 480 * sizeof(float), active.DspStateBytes);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    [Fact]
    public void ApplicationKeepsAnAssignedRuntimeCallerOwned()
    {
        using TimbreRig rig = new();
        Application app = new() { SoundRuntime = rig.Runtime };
        SoundScope sounds = app.Sounds;

        app.CompleteExit();

        Assert.True(sounds.IsDisposed);
        Assert.False(rig.Runtime.IsDisposed);
        Assert.Throws<ArgumentNullException>(() => new Application().SoundRuntime = null!);
    }

    private sealed class PlainElement : UIElement
    {
    }
}
