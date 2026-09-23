using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.SdlGpu;

public sealed class RenderSurface3DStage3LifecycleTests
{
    [Fact]
    public void Root_cleanup_retires_only_its_surface_target_and_reattach_records_again()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-root", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = NewSurface();
        root.VisualChildren.Add(surface);
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));

        Render(session, commands);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        root.ReleaseDrawingResources();
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        root.VisualChildren.Remove(surface);
        root.VisualChildren.Add(surface);
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.RecordingCount);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
    }

    [Fact]
    public void Removing_last_subscriber_retires_target_without_closing_window()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-unsubscribe", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = new();
        RenderSurface3DDrawEventHandler handler = (_, frame) =>
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
        surface.Draw += handler;
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        surface.Draw -= handler;
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
    }

    [Fact]
    public void Captured_command_after_last_subscriber_removal_cannot_revive_a_target()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-stale-unsubscribe", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = new();
        RenderSurface3DDrawEventHandler handler = (_, frame) =>
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
        surface.Draw += handler;
        DrawCommandList captured = Commands(surface, new DrawRect(0, 0, 32, 24));

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(captured, Frame(captured));
        surface.Draw -= handler;
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        session.DrawingBackend.Render(captured, Frame(captured));
        session.CompleteFrame(false);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Render(session, captured);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
    }

    [Fact]
    public void Skipped_stale_surface_restores_parent_target_for_following_2d_commands()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-stale-following-2d", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = new();
        RenderSurface3DDrawEventHandler handler = (_, _) => { };
        surface.Draw += handler;
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 80, 60), Color.HotPink));
        commands.Add(DrawCommand.RenderSurface3DCommand(surface, new DrawRect(0, 0, 32, 24), Color.White, 1));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(40, 0, 20, 20), Color.LimeGreen));
        surface.Draw -= handler;

        Render(session, commands);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Assert.Contains(api.GpuActions, action => action.StartsWith("draw-indexed:", StringComparison.Ordinal));
    }

    [Fact]
    public void Pending_3d_target_is_not_released_by_another_window_flush_after_detach()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("stage3-pending-a", 80, 60, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("stage3-pending-b", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = NewSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = NewSession(factory, api, secondWindow);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = NewSurface();
        root.VisualChildren.Add(surface);
        DrawCommandList captured = Commands(surface, new DrawRect(0, 0, 32, 24));

        first.BeginFrame(Color.Transparent);
        first.DrawingBackend.Render(captured, Frame(captured));
        nint[] targetHandles = api.GpuTextures
            .Where(pair => pair.Value.CreateInfo.Width == 32 && pair.Value.CreateInfo.Height == 24)
            .Select(pair => pair.Key).ToArray();
        Assert.Equal(3, targetHandles.Length);
        root.VisualChildren.Remove(surface);
        Assert.Equal(0, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        second.BeginFrame(Color.Transparent);
        second.CompleteFrame(false);
        foreach (nint handle in targetHandles)
            Assert.Contains(handle, api.GpuTextures.Keys);
        first.CompleteFrame(false);
        foreach (nint handle in targetHandles)
            Assert.DoesNotContain(handle, api.GpuTextures.Keys);
        Assert.Equal(0, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Render(second, Commands(NewSurface(), new DrawRect(0, 0, 32, 24)));
        Assert.Equal(1, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void Root_cleanup_and_detach_reject_stale_capture_but_allow_a_fresh_acquisition()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-stale-root", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = NewSurface();
        root.VisualChildren.Add(surface);
        DrawCommandList beforeCleanup = Commands(surface, new DrawRect(0, 0, 32, 24));
        Render(session, beforeCleanup);
        root.ReleaseDrawingResources();
        Render(session, beforeCleanup);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        DrawCommandList afterCleanup = Commands(surface, new DrawRect(0, 0, 32, 24));
        Render(session, afterCleanup);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        root.VisualChildren.Remove(surface);
        Render(session, afterCleanup);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        root.VisualChildren.Add(surface);
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
    }

    [Theory]
    [InlineData("translate")]
    [InlineData("opacity")]
    [InlineData("translate-then-opacity")]
    [InlineData("opacity-then-translate")]
    public void Presentation_copies_preserve_a_retired_3d_command_capture(string mapping)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow($"stage3-copy-{mapping}", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = NewSurface();
        root.VisualChildren.Add(surface);
        IRenderSurface3DSource source = surface;
        DrawCommand captured = DrawCommand.RenderSurface3DCommand(
            source, new DrawRect(0, 0, 32, 24), Color.White, source.FrameVersion);
        Render(session, Single(captured));
        root.ReleaseDrawingResources();
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        DrawCommand mapped = Map(captured, mapping);
        Assert.Same(source, mapped.RenderSurface3D);
        Assert.Equal(captured.RetainedVersion, mapped.RetainedVersion);
        Render(session, Single(mapped));
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Assert.Equal(captured.RenderSurface3DResourceEpoch, mapped.RenderSurface3DResourceEpoch);
        DrawCommand recaptured = Map(DrawCommand.RenderSurface3DCommand(
            source, captured.Rect, captured.Color, captured.RetainedVersion), mapping);
        Assert.NotEqual(mapped, recaptured);
        DrawCommandMetadata oldMetadata = DrawCommandMetadata.Create(mapped);
        DrawCommandMetadata newMetadata = DrawCommandMetadata.Create(recaptured, oldMetadata);
        Assert.NotSame(oldMetadata, newMetadata);

        DrawCommand fresh = DrawCommand.RenderSurface3DCommand(
            source, captured.Rect, captured.Color, source.FrameVersion);
        Render(session, Single(Map(fresh, mapping)));
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
    }

    private static DrawCommand Map(DrawCommand command, string mapping) => mapping switch
    {
        "translate" => DrawCommandTransform.Translate(command, 7, 9),
        "opacity" => DrawCommandTransform.ApplyOpacity(command, .5f),
        "translate-then-opacity" => DrawCommandTransform.ApplyOpacity(
            DrawCommandTransform.Translate(command, 7, 9), .5f),
        "opacity-then-translate" => DrawCommandTransform.Translate(
            DrawCommandTransform.ApplyOpacity(command, .5f), 7, 9),
        _ => throw new ArgumentOutOfRangeException(nameof(mapping))
    };

    private static DrawCommandList Single(DrawCommand command)
    {
        DrawCommandList commands = new();
        commands.Add(command);
        return commands;
    }

    [Fact]
    public void Same_recording_replays_in_one_buffer_then_reuses_after_submit()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-replay", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.DrawingBackend.Render(commands, Frame(commands));
        session.CompleteFrame(false);
        Assert.Equal(1, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.UploadCount);
        Render(session, commands);
        Assert.Equal(1, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.UploadCount);
    }

    [Fact]
    public void A_captured_command_replay_does_not_consume_invalidation_raised_by_its_callback()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-callback-replay", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = new();
        int callbacks = 0;
        surface.Draw += (sender, frame) =>
        {
            callbacks++;
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
            if (callbacks == 1) sender.InvalidateFrame();
        };
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.DrawingBackend.Render(commands, Frame(commands));
        session.CompleteFrame(false);
        Assert.Equal(1, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);

        Render(session, commands);
        Assert.Equal(2, callbacks);
    }

    [Fact]
    public void A_camera_change_after_recording_in_the_same_buffer_is_not_a_pending_hit()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-same-buffer-camera", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        surface.ViewMatrix = Matrix4x4.CreateTranslation(0, 0, -1);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.CompleteFrame(false);
        Assert.Equal(2, callbacks);
        Assert.Equal(2, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void A_second_mutation_after_callback_invalidation_is_not_deduplicated()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-callback-then-camera", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = new();
        int callbacks = 0;
        surface.Draw += (sender, frame) =>
        {
            callbacks++;
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
            if (callbacks == 1) sender.InvalidateFrame();
        };
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.Equal(1, callbacks);
        surface.ViewMatrix = Matrix4x4.CreateTranslation(0, 0, -1);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.CompleteFrame(false);
        Assert.Equal(2, callbacks);
        Assert.Equal(2, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void Changed_logical_raster_bounds_with_same_ceiled_pixels_records_again()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-bounds", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        Render(session, Commands(surface, new DrawRect(0, 0, 32.1f, 24.1f)));
        Render(session, Commands(surface, new DrawRect(0, 0, 32.9f, 24.9f)));
        Assert.Equal(2, callbacks);
    }

    [Fact]
    public void Zero_viewport_has_no_target_recording_or_pass()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-zero", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        Render(session, Commands(surface, new DrawRect(0, 0, 0, 24)));
        SdlGpuRenderSurface3DFrameCounters counters = session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters;
        Assert.Equal(0, counters.TargetCreateCount);
        Assert.Equal(0, counters.RecordingCount);
        Assert.Equal(0, counters.PassCount);
    }

    [Fact]
    public void Failed_first_submit_and_failed_recovery_never_publish_a_generation()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-submit", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        api.EnqueueSubmitResults(false, false, true);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            session.BeginFrame(Color.Transparent);
            session.DrawingBackend.Render(commands, Frame(commands));
            Assert.ThrowsAny<Exception>(() => session.CompleteFrame(false));
        }
        Render(session, commands);
        Assert.Equal(3, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Render(session, commands);
        Assert.Equal(3, callbacks);
    }

    [Fact]
    public void A_successful_first_submit_survives_a_failed_recovery_for_a_new_generation()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-recovery", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        Render(session, commands);
        surface.InvalidateFrame();
        api.EnqueueSubmitResults(false, true);
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.ThrowsAny<Exception>(() => session.CompleteFrame(false));
        Render(session, commands);
        Assert.Equal(3, callbacks);
        Render(session, commands);
        Assert.Equal(3, callbacks);
    }

    [Fact]
    public void Callback_throw_and_detach_during_callback_leave_no_reusable_result()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-callback", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = new();
        root.VisualChildren.Add(surface);
        int callbacks = 0;
        surface.Draw += (sender, frame) =>
        {
            callbacks++;
            if (callbacks == 1) throw new InvalidOperationException("callback sentinel");
            if (callbacks == 2) root.VisualChildren.Remove(sender);
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
        };
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));

        session.BeginFrame(Color.Transparent);
        Assert.Throws<InvalidOperationException>(() => session.DrawingBackend.Render(commands, Frame(commands)));
        session.CompleteFrame(false);
        session.BeginFrame(Color.Transparent);
        Assert.Throws<ObjectDisposedException>(() => session.DrawingBackend.Render(commands, Frame(commands)));
        session.CompleteFrame(false);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        root.VisualChildren.Add(surface);
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        Assert.Equal(3, callbacks);
    }

    [Fact]
    public void Two_windows_on_one_device_keep_independent_surface_targets_and_retire_locally()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("stage3-window-a", 80, 60, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("stage3-window-b", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = NewSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = NewSession(factory, api, secondWindow);
        UIRoot firstRoot = new(80, 60);
        UIRoot secondRoot = new(80, 60);
        RenderSurface3D firstSurface = NewSurface();
        RenderSurface3D secondSurface = NewSurface();
        firstRoot.VisualChildren.Add(firstSurface);
        secondRoot.VisualChildren.Add(secondSurface);
        DrawCommandList firstCommands = Commands(firstSurface, new DrawRect(0, 0, 32, 24));
        DrawCommandList secondCommands = Commands(secondSurface, new DrawRect(0, 0, 32, 24));

        Render(first, firstCommands);
        Render(second, secondCommands);
        Render(first, firstCommands);
        Assert.Equal(0, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(1, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Assert.Equal(1, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        firstRoot.ReleaseDrawingResources();
        Assert.Equal(0, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Render(second, secondCommands);
        Assert.Equal(0, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(1, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        firstRoot.VisualChildren.Remove(firstSurface);
        secondRoot.VisualChildren.Remove(secondSurface);
        firstRoot.VisualChildren.Add(secondSurface);
        Render(first, Commands(secondSurface, new DrawRect(0, 0, 32, 24)));
        Assert.Equal(1, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(0, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        first.Dispose();
        second.Dispose();
        factory.Dispose();
        Assert.Empty(api.GpuTextures);
        Assert.Empty(api.GpuPipelines);
        Assert.Empty(api.GpuShaders);
    }

    [Fact]
    public void Failed_submit_recaptures_prism_parent_with_unchanged_child_generation()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedTextureFormats.Add(SdlGpuTextureFormat.R16G16B16A16Float);
        nint window = api.CreateWindow("stage3-prism", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawRect bounds = new(0, 0, 32, 24);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Stage 3 parent", PrismTestData.Layer(1, "Parent")),
            ownerToken: 97003, bounds: bounds);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.RenderSurface3DCommand(surface, bounds, Color.White, 1),
            DrawCommand.EndPrism());
        long version = ((IRenderSurface3DSource)surface).FrameVersion;
        api.EnqueueSubmitResults(false, true);

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.Equal(1, callbacks);
        Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.Equal(1, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.ThrowsAny<Exception>(() => session.CompleteFrame(false));
        Assert.Equal(version, ((IRenderSurface3DSource)surface).FrameVersion);

        Render(session, commands);
        Assert.Equal(2, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
        Render(session, commands);
        Assert.Equal(2, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount);
    }

    [Theory]
    [InlineData("texture")]
    [InlineData("pipeline")]
    [InlineData("upload")]
    [InlineData("render-pass")]
    public void Incomplete_gpu_work_is_retried_after_target_pipeline_or_upload_failure(string failure)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow($"stage3-{failure}", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        switch (failure)
        {
            case "texture": api.FailTextureCreationAt = api.TextureCreationCount + 1; break;
            case "pipeline": api.FailPipelineCreationCount = 1; break;
            case "upload": api.FailBufferUploadCount = 1; break;
        }

        session.BeginFrame(Color.Transparent);
        if (failure == "render-pass") api.FailRenderPassCount = 1;
        Assert.ThrowsAny<Exception>(() => session.DrawingBackend.Render(commands, Frame(commands)));
        session.CompleteFrame(false);
        Render(session, commands);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Render(session, commands);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Theory]
    [InlineData("texture", "create-texture-targeted-failed:D24UnormS8Uint")]
    [InlineData("pipeline", "create-pipeline-targeted-failed:D24UnormS8Uint")]
    [InlineData("upload", "upload-buffer-targeted-failed")]
    [InlineData("render-pass", "begin-depth-render-targeted-failed")]
    public void Targeted_3d_failure_under_prism_recaptures_parent_with_unchanged_versions(
        string failure, string expectedAction)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedTextureFormats.Add(SdlGpuTextureFormat.R16G16B16A16Float);
        nint window = api.CreateWindow($"stage3-prism-target-{failure}", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawRect bounds = new(0, 0, 32, 24);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Stage 3 targeted parent", PrismTestData.Layer(1, "Parent")),
            ownerToken: 97030, bounds: bounds);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.RenderSurface3DCommand(surface, bounds, Color.White, 1),
            DrawCommand.EndPrism());
        long generation = ((IRenderSurface3DSource)surface).FrameVersion;
        switch (failure)
        {
            case "texture":
                api.FailNextTextureMatching = info =>
                    info.Format == SdlGpuTextureFormat.D24UnormS8Uint &&
                    info.Width == 32 && info.Height == 24;
                break;
            case "pipeline":
                api.FailNextPipelineMatching = info =>
                    info.DepthStencilFormat == SdlGpuTextureFormat.D24UnormS8Uint &&
                    info.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual;
                break;
            case "upload":
                api.FailNextBufferUploadMatching = size => size == 4 * 56;
                break;
            case "render-pass":
                api.FailNextDepthPassMatching = (_, depth) =>
                    api.GpuTextures[depth.Texture].CreateInfo.Width == 32;
                break;
        }

        session.BeginFrame(Color.Transparent);
        Exception thrown = Assert.ThrowsAny<Exception>(() =>
            session.DrawingBackend.Render(commands, Frame(commands)));
        Assert.Contains(api.GpuActions, action => action.StartsWith(expectedAction, StringComparison.Ordinal));
        Assert.Equal(generation, ((IRenderSurface3DSource)surface).FrameVersion);
        Assert.Equal(failure == "texture" ? 0 : 1, callbacks);
        session.CompleteFrame(false);
        Render(session, commands);
        Assert.Equal(failure == "texture" ? 1 : 2, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
        Render(session, commands);
        Assert.Equal(failure == "texture" ? 1 : 2, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(0, session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount);
        Assert.Contains(failure == "upload" ? "Configured targeted" : "SDL GPU",
            thrown.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Prism_callback_throw_then_detach_retries_without_reusing_a_retired_acquisition()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedTextureFormats.Add(SdlGpuTextureFormat.R16G16B16A16Float);
        nint window = api.CreateWindow("stage3-prism-callback-detach", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = new();
        root.VisualChildren.Add(surface);
        int callbacks = 0;
        surface.Draw += (sender, frame) =>
        {
            callbacks++;
            if (callbacks == 1) throw new InvalidOperationException("Prism callback sentinel");
            if (callbacks == 2) root.VisualChildren.Remove(sender);
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
        };
        DrawRect bounds = new(0, 0, 32, 24);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Stage 3 callback parent", PrismTestData.Layer(1, "Parent")),
            ownerToken: 97031, bounds: bounds);
        DrawCommandList Captured() => PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.RenderSurface3DCommand(surface, bounds, Color.White,
                ((IRenderSurface3DSource)surface).FrameVersion),
            DrawCommand.EndPrism());
        DrawCommandList firstCapture = Captured();
        long generation = ((IRenderSurface3DSource)surface).FrameVersion;

        session.BeginFrame(Color.Transparent);
        InvalidOperationException first = Assert.Throws<InvalidOperationException>(() =>
            session.DrawingBackend.Render(firstCapture, Frame(firstCapture)));
        Assert.Contains("Prism callback sentinel", first.Message, StringComparison.Ordinal);
        session.CompleteFrame(false);
        Assert.Equal(generation, ((IRenderSurface3DSource)surface).FrameVersion);

        session.BeginFrame(Color.Transparent);
        Assert.Throws<ObjectDisposedException>(() =>
            session.DrawingBackend.Render(firstCapture, Frame(firstCapture)));
        session.CompleteFrame(false);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Render(session, firstCapture);
        Assert.Equal(2, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);

        root.VisualChildren.Add(surface);
        scope = new PrismDrawScope(scope.Instance, scope.CacheOwnerToken, scope.ControlBounds,
            scope.EffectiveTransform, scope.PixelScale, visualContentVersion: 2,
            PrismDrawResources.Empty);
        Render(session, Captured());
        Assert.Equal(3, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
    }

    [Fact]
    public void Prism_warmed_result_survives_two_failed_recovery_submits_and_a_cancelled_present_buffer()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedTextureFormats.Add(SdlGpuTextureFormat.R16G16B16A16Float);
        nint window = api.CreateWindow("stage3-prism-warm-recovery", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawRect bounds = new(0, 0, 32, 24);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Stage 3 warm parent", PrismTestData.Layer(1, "Parent")),
            ownerToken: 97032, bounds: bounds);
        DrawCommandList Captured() => PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.RenderSurface3DCommand(surface, bounds, Color.White,
                ((IRenderSurface3DSource)surface).FrameVersion),
            DrawCommand.EndPrism());
        DrawCommandList commands = Captured();
        Render(session, commands);
        Assert.Equal(1, callbacks);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Render(session, commands);
        Assert.Equal(0, session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount);

        surface.InvalidateFrame();
        long recoveryGeneration = ((IRenderSurface3DSource)surface).FrameVersion;
        scope = new PrismDrawScope(scope.Instance, scope.CacheOwnerToken, scope.ControlBounds,
            scope.EffectiveTransform, scope.PixelScale, visualContentVersion: 2,
            PrismDrawResources.Empty);
        commands = Captured();
        api.EnqueueSubmitResults(false, false, true);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            session.BeginFrame(Color.Transparent);
            session.DrawingBackend.Render(commands, Frame(commands));
            Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
            Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
            Assert.Throws<InvalidOperationException>(() => session.CompleteFrame(false));
            Assert.Equal(recoveryGeneration, ((IRenderSurface3DSource)surface).FrameVersion);
        }
        Render(session, commands);
        Assert.Equal(4, callbacks);
        Assert.True(session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount > 0);
        Render(session, commands);
        Assert.Equal(4, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount);

        api.EnqueueSwapchainAcquireResults(false, false);
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.Throws<InvalidOperationException>(() => session.CompleteFrame(true));
        Assert.Equal(FakeGpuCommandBufferActionKind.Cancel,
            api.GpuCommandBufferActions[^1].Kind);
        Assert.True(api.GpuCommandBufferActions[^1].Succeeded);
        Render(session, commands);
        Assert.Equal(4, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().PrismDiagnostics.Counters.CaptureCount);
    }

    [Fact]
    public void Submitted_3d_target_survives_failed_present_recovery_buffer()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-present-recovery", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        api.EnqueueSwapchainAcquireResults(false, false);
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        Assert.ThrowsAny<Exception>(() => session.CompleteFrame(true));
        Assert.Collection(api.GpuCommandBufferActions,
            submitted => { Assert.Equal(FakeGpuCommandBufferActionKind.Submit, submitted.Kind); Assert.True(submitted.Succeeded); },
            cancelled => { Assert.Equal(FakeGpuCommandBufferActionKind.Cancel, cancelled.Kind); Assert.True(cancelled.Succeeded); });
        Render(session, commands);
        Assert.Equal(1, callbacks);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void Disposing_session_with_pending_3d_work_cancels_and_new_session_recreates_target()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-cancel", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        RenderSurface3D surface = NewSurface();
        int callbacks = 0;
        surface.Draw += (_, _) => callbacks++;
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        using (SdlGpuWindowGraphicsSession first = NewSession(factory, api, window))
        {
            first.BeginFrame(Color.Transparent);
            first.DrawingBackend.Render(commands, Frame(commands));
            first.Dispose();
            Assert.Equal(0, first.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        }
        Assert.Contains(api.GpuCommandBufferActions, action =>
            action.Kind == FakeGpuCommandBufferActionKind.Cancel && action.Succeeded);
        using SdlGpuWindowGraphicsSession second = NewSession(factory, api, window);
        Render(second, commands);
        Assert.Equal(2, callbacks);
        Assert.Equal(1, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void One_hundred_resize_detach_and_reattach_cycles_return_local_targets_to_baseline()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-stress", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        UIRoot root = new(80, 60);
        RenderSurface3D surface = NewSurface();
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        root.VisualChildren.Add(surface);
        Render(session, commands);
        root.VisualChildren.Remove(surface);
        session.DrawingResources.FlushRetired();
        int baselineTextures = api.GpuTextures.Count;
        for (int cycle = 0; cycle < 100; cycle++)
        {
            root.VisualChildren.Add(surface);
            session.Resize(80 + cycle % 3, 60 + cycle % 5, cycle % 2 == 0 ? 1 : 1.25f);
            Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
            Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
            root.VisualChildren.Remove(surface);
            session.DrawingResources.FlushRetired();
            Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
            Assert.Equal(baselineTextures, api.GpuTextures.Count);
        }
    }

    [Fact]
    public void Two_surfaces_in_one_window_keep_distinct_cameras_and_reuse_independently()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureVertexUniformWrites = true };
        nint window = api.CreateWindow("stage3-two-surfaces", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D first = NewSurface();
        RenderSurface3D second = NewSurface();
        first.ViewMatrix = Matrix4x4.Identity;
        second.ViewMatrix = Matrix4x4.CreateTranslation(1, 2, 3);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(first, new DrawRect(0, 0, 32, 24), Color.White, 1));
        commands.Add(DrawCommand.RenderSurface3DCommand(second, new DrawRect(40, 0, 32, 24), Color.White, 1));
        Render(session, commands);
        Assert.Equal(2, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Equal(2, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.LiveTargetCount);
        Matrix4x4[] views = api.VertexUniformWrites.Where(bytes => bytes.Length == 208)
            .Select(bytes => MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(bytes).View).ToArray();
        Assert.Equal(new[] { first.ViewMatrix, second.ViewMatrix }, views);

        Render(session, commands);
        Assert.Equal(0, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        second.InvalidateFrame();
        Render(session, commands);
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
    }

    [Fact]
    public void Positive_zero_positive_window_resize_and_dpi_rebuild_only_valid_raster_sizes()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-resize", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = NewSession(factory, api, window);
        RenderSurface3D surface = NewSurface();
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        session.Resize(0, 0, 1);
        session.BeginFrame(Color.Transparent);
        session.CompleteFrame(false);
        Assert.DoesNotContain(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 0 || texture.CreateInfo.Height == 0);
        session.Resize(100, 75, 1.25f);
        Render(session, Commands(surface, new DrawRect(0, 0, 32, 24)));
        Assert.Equal(1, session.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Contains(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 40 && texture.CreateInfo.Height == 30 &&
            texture.CreateInfo.Format == SdlGpuTextureFormat.D24UnormS8Uint);
    }

    [Fact]
    public void Replacing_a_session_with_a_new_format_does_not_reuse_its_old_3d_target()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("stage3-format", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        RenderSurface3D surface = NewSurface();
        DrawCommandList commands = Commands(surface, new DrawRect(0, 0, 32, 24));
        using (SdlGpuWindowGraphicsSession first = NewSession(factory, api, window))
        {
            Render(first, commands);
            Assert.Contains(api.GpuTextures.Values, texture =>
                texture.CreateInfo.Width == 32 &&
                texture.CreateInfo.Format == SdlGpuTextureFormat.R8G8B8A8Unorm);
        }
        api.SwapchainTextureFormat = SdlGpuTextureFormat.B8G8R8A8Unorm;
        using SdlGpuWindowGraphicsSession second = NewSession(factory, api, window);
        Render(second, commands);
        Assert.Equal(1, second.DrawingBackendAs3D().LastFrameRenderSurface3DCounters.PassCount);
        Assert.Contains(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 32 &&
            texture.CreateInfo.Format == SdlGpuTextureFormat.B8G8R8A8Unorm);
        Assert.DoesNotContain(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 32 &&
            texture.CreateInfo.Format == SdlGpuTextureFormat.R8G8B8A8Unorm);
    }

    private static SdlGpuWindowGraphicsSession NewSession(SdlGpuWindowGraphicsSessionFactory factory,
        FakeSdlApi api, nint window) => Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 80, 60, 1));

    private static RenderSurface3D NewSurface()
    {
        RenderSurface3D surface = new();
        surface.Draw += (_, frame) => frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
        return surface;
    }

    private static DrawCommandList Commands(RenderSurface3D surface, DrawRect bounds)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(surface, bounds, Color.White, 1));
        return commands;
    }

    private static DrawingFrameContext Frame(DrawCommandList commands) => new(
        new Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer().Analyze(commands));

    private static void Render(SdlGpuWindowGraphicsSession session, DrawCommandList commands)
    {
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, Frame(commands));
        session.CompleteFrame(false);
    }
}

internal static class RenderSurface3DTestBackendExtensions
{
    internal static SdlGpuDrawingBackend DrawingBackendAs3D(this SdlGpuWindowGraphicsSession session) =>
        Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);
}
