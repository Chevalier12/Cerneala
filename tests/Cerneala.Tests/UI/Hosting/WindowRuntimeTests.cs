using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Microsoft.Extensions.DependencyInjection;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using Cerneala.UI.Text;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Hosting;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class WindowRuntimeTests : IDisposable
{
    public WindowRuntimeTests()
    {
        GeneratedWindowApplication.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        GeneratedWindowApplication.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    [Fact]
    public void ShowBuildsHostBeforeLifecycleAndHidePreservesTheWindow()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Hello" } };
        List<string> events = [];
        window.SourceInitialized += (_, _) => events.Add("source");
        window.Initialized += (_, _) => events.Add("initialized");
        window.Loaded += (_, _) => events.Add("loaded");
        window.ContentRendered += (_, _) => events.Add("rendered");

        window.Show();

        Assert.Equal(new[] { "source", "initialized", "loaded", "rendered" }, events);
        Assert.True(window.IsShown);
        Assert.True(window.IsLoaded);
        Assert.Single(runtime.Windows);
        Assert.Equal(1, platform.Windows[0].Backend.RenderCount);

        window.Hide();
        Assert.False(window.IsShown);
        Assert.True(window.IsLoaded);
        Assert.Equal(0, platform.Windows[0].Session.DisposeCount);

        window.Show();
        Assert.True(window.IsShown);
        Assert.Single(events, value => value == "rendered");
    }

    [Fact]
    public void NativeWindowIsShownOnlyAfterItsFirstFrameWasPresented()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Ready" } };

        window.Show();

        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        Assert.Equal(1, nativeWindow.ShowCount);
        Assert.True(nativeWindow.HadPresentedFrameWhenShown);
    }

    [Fact]
    public void NativeActivationFocusesAFocusableWindowWhenNoElementIsFocused()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Focusable = true };

        window.Show();
        Assert.False(window.IsKeyboardFocused);

        Assert.Single(platform.Windows).Activate();

        Assert.True(window.IsActive);
        Assert.True(window.IsKeyboardFocused);
    }

    [Fact]
    public void NativeActivationPreservesAnExistingFocusedChild()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Focused" };
        Window window = new() { Content = button, Focusable = true };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        LayoutRect bounds = button.ArrangedBounds;
        nativeWindow.Input.MovePointer(
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));
        nativeWindow.Input.SetButton(InputMouseButton.Left, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(button.IsKeyboardFocused);

        nativeWindow.Activate();

        Assert.True(button.IsKeyboardFocused);
        Assert.False(window.IsKeyboardFocused);
    }

    [Fact]
    public void RenderLifecycleCallbacksRunInsideTheWindowRelaySynchronizationContext()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new();
        SynchronizationContext? frameContext = null;
        SynchronizationContext? contentContext = null;
        window.FrameRendered += (_, _) => frameContext = SynchronizationContext.Current;
        window.ContentRendered += (_, _) => contentContext = SynchronizationContext.Current;

        window.Show();

        Assert.NotNull(frameContext);
        Assert.NotNull(contentContext);
        Assert.Equal("UiRelaySynchronizationContext", frameContext.GetType().Name);
        Assert.Same(frameContext, contentContext);
    }

    [Fact]
    public void PresentedFrameUpdatesWindowDiagnosticsBeforeRaisingFrameRendered()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Diagnostics" } };
        List<UiFrame> observedFrames = [];
        window.FrameRendered += (_, _) => observedFrames.Add(Assert.IsType<UiFrame>(window.LastFrame));

        window.Show();
        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "test frame");
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, observedFrames.Count);
        Assert.Same(window.LastFrame, observedFrames[^1]);
        Assert.Equal(TimeSpan.FromMilliseconds(16), window.LastFrame!.ElapsedTime);
    }

    [Fact]
    public void PumpOnceReportsWhetherItPresentedAFrame()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Pacing" } };
        window.Show();

        object? idleResult = typeof(WindowApplicationRuntime)
            .GetMethod(nameof(WindowApplicationRuntime.PumpOnce))!
            .Invoke(runtime, [TimeSpan.FromMilliseconds(16)]);

        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "pacing test");
        object? renderedResult = typeof(WindowApplicationRuntime)
            .GetMethod(nameof(WindowApplicationRuntime.PumpOnce))!
            .Invoke(runtime, [TimeSpan.FromMilliseconds(16)]);

        Assert.False(Assert.IsType<bool>(idleResult));
        Assert.True(Assert.IsType<bool>(renderedResult));
    }

    [Fact]
    public void PumpOnceDoesNotAddASecondCompositorWaitAfterPresentation()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Pacing" } };
        window.Show();

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, platform.PresentedFrameWaitCount);

        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "pacing test");
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, platform.PresentedFrameWaitCount);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, platform.PresentedFrameWaitCount);
    }

    [Fact]
    public void DesignPreviewRendersOffscreenWithoutPresentingOrWaitingForTheCompositor()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Preview" } };

        runtime.StartPreviewWindow(window);
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        Assert.Equal(1, session.BeginFrameCount);
        Assert.Equal(0, session.PresentCount);

        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "preview frame");
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, session.BeginFrameCount);
        Assert.Equal(0, session.PresentCount);
        Assert.Equal(0, platform.PresentedFrameWaitCount);
    }

    [Fact]
    public void DesignPreviewForwardsPointerHoverAndButtonTransitions()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Continue", Width = 180, Height = 48 };
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        Window window = new() { Content = button };
        runtime.StartPreviewWindow(window);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);

        runtime.MovePreviewPointer(window, x, y);

        Assert.True(button.IsMouseOver);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(button.IsMouseOver);

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);

        Assert.Equal(1, clickCount);

        runtime.LeavePreviewPointer(window);

        Assert.False(button.IsMouseOver);
    }

    // Preview clicks are real user input, so their click count follows the real time of the window runtime: the frames
    // that the runtime pumps between two clicks advance the input clock of the same ElementInputBridge (#682).
    [Theory]
    [InlineData(2000, 1, false)]
    [InlineData(0, 2, true)]
    public void DesignPreviewClickCountFollowsTheTimeThePumpedFramesAdvance(
        int millisecondsBetweenClicks, int secondClickCount, bool wordSelected)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Text = "hello world", Width = 200, Height = 40 };
        Window window = new() { Content = editor };
        runtime.StartPreviewWindow(window);
        List<int> downCounts = [];
        editor.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        LayoutRect bounds = editor.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);
        if (millisecondsBetweenClicks > 0)
        {
            // The runtime advances its clock only by the frames that it pumps: pump 250 ms frames up to the gap.
            for (int elapsed = 0; elapsed < millisecondsBetweenClicks; elapsed += 250)
            {
                window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "preview time");
                runtime.PumpOnce(TimeSpan.FromMilliseconds(250));
            }
        }

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);

        Assert.Equal([1, secondClickCount], downCounts);
        Assert.Equal(wordSelected, editor.SelectedText.Length > 0);
    }

    // What the VS preview does while the user is idle: its animation timer calls Capture, which pumps the runtime with
    // the real time since the previous pump, but nothing invalidates, so the pumps render no frame (#682).
    [Fact]
    public void DesignPreviewClickCountFollowsRealTimeThroughIdlePumps()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 160, Height = 40 };
        Window window = new() { Content = button };
        runtime.StartPreviewWindow(window);
        List<int> downCounts = [];
        button.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        for (int elapsed = 0; elapsed < 2000; elapsed += 250)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(250));
        }

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);

        Assert.Equal([1, 1], downCounts);
    }

    // The time of a pump counts for the double-click time even when the window is hidden or has no viewport for it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DesignPreviewClickCountFollowsRealTimeWhileTheWindowIsHiddenOrHasNoViewport(bool hide)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 160, Height = 40 };
        Window window = new() { Content = button };
        runtime.StartPreviewWindow(window);
        List<int> downCounts = [];
        button.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        LayoutRect bounds = button.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);

        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        if (hide) window.Hide();
        else platform.Windows[0].ReportBounds(new UiViewport(0, 0), 0, 0, WindowState.Normal);
        for (int elapsed = 0; elapsed < 2000; elapsed += 250)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(250));
        }

        if (hide) window.Show();
        else platform.Windows[0].ReportBounds(new UiViewport(800, 600), 0, 0, WindowState.Normal);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: true);
        runtime.SetPreviewPointerButton(window, x, y, InputMouseButton.Left, isDown: false);

        Assert.Equal([1, 1], downCounts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DesignPreviewTextReachesTheFocusedTextBoxWithoutAPump(bool startAsPreview)
    {
        (string? afterInput, string? afterPump) = RunPreviewTextBoxInput(
            startAsPreview,
            initialText: "",
            (runtime, window) => runtime.SendPreviewText(window, "ab"));

        Assert.Equal("ab", afterInput);
        Assert.Equal("ab", afterPump);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DesignPreviewKeyPressReachesTheFocusedTextBoxWithoutAPump(bool startAsPreview)
    {
        (string? afterInput, string? afterPump) = RunPreviewTextBoxInput(
            startAsPreview,
            initialText: "ab",
            (runtime, window) =>
            {
                runtime.PressPreviewKey(window, InputKey.End, ServoModifiers.None);
                runtime.PressPreviewKey(window, InputKey.Back, ServoModifiers.None);
            });

        Assert.Equal("a", afterInput);
        Assert.Equal("a", afterPump);
    }

    [Fact]
    public void FrameProcessingTimeIncludesThePresentationWait()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Diagnostics" } };
        TimeSpan presentationWait = TimeSpan.FromMilliseconds(80);

        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        session.PresentDelay = presentationWait;
        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "timed frame");

        Stopwatch pumpTime = Stopwatch.StartNew();
        WindowApplicationRuntime.Current!.PumpOnce(TimeSpan.FromMilliseconds(16));
        pumpTime.Stop();

        Assert.True(window.LastFrame!.ProcessingTime >= presentationWait);
        Assert.True(window.LastFrame.DiagnosticsTiming.CompleteFrame >= presentationWait);
        Assert.True(
            pumpTime.Elapsed - window.LastFrame.ProcessingTime < TimeSpan.FromMilliseconds(40),
            $"Processing time {window.LastFrame.ProcessingTime} omitted the {presentationWait} presentation wait from a {pumpTime.Elapsed} pump.");
    }

    [Fact]
    public void EachWindowUsesAndDisposesItsOwnGraphicsSession()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window first = new();
        Window second = new();

        first.Show();
        second.Show();

        Assert.NotSame(platform.Windows[0].Session, platform.Windows[1].Session);
        Assert.Equal(1, platform.Windows[0].Session.BeginFrameCount);
        Assert.Equal(1, platform.Windows[0].Session.PresentCount);
        Assert.Equal(1, platform.Windows[1].Session.BeginFrameCount);
        Assert.Equal(1, platform.Windows[1].Session.PresentCount);

        first.Close();

        Assert.Equal(1, platform.Windows[0].Session.DisposeCount);
        Assert.Equal(0, platform.Windows[1].Session.DisposeCount);
        runtimePump(second);
        Assert.Equal(2, platform.Windows[1].Session.PresentCount);

        static void runtimePump(Window window)
        {
            window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "test");
            WindowApplicationRuntime.Current!.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
    }

    [Fact]
    public void CaretBlinkSchedulesAWindowFrameWithoutAnExternalRenderRequest()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox textBox = new()
        {
            Text = "blink",
            IsKeyboardFocused = true
        };
        Window window = new() { Content = textBox };

        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        Assert.Equal(1, session.PresentCount);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(500));

        Assert.Equal(2, session.PresentCount);
    }

    [Fact]
    public void HeldRepeatButtonSchedulesFramesWithoutExternalRenderRequests()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        RepeatButton repeatButton = new() { Content = "Hold" };
        int clickCount = 0;
        repeatButton.Click += (_, _) => clickCount++;
        Window window = new() { Content = repeatButton };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        float x = repeatButton.ArrangedBounds.X + (repeatButton.ArrangedBounds.Width / 2);
        float y = repeatButton.ArrangedBounds.Y + (repeatButton.ArrangedBounds.Height / 2);
        nativeWindow.Input.MovePointer(x, y);
        nativeWindow.Input.SetButton(InputMouseButton.Left, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        int framesAfterPress = nativeWindow.Session.PresentCount;

        Assert.Equal(1, clickCount);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(repeatButton.Delay));

        Assert.Equal(2, clickCount);
        Assert.Equal(framesAfterPress + 1, nativeWindow.Session.PresentCount);
    }

    [Fact]
    public void HeldKeyWithAnInputHeldHandlerSchedulesFramesWithoutExternalRenderRequestsUntilReleased()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Hold" } };
        List<TimeSpan> elapsed = [];
        window.Show();
        window.Root!.InputHeld += (_, args) => elapsed.Add(args.Elapsed);
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        // Adding the handler may itself request one frame; let it settle.
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        nativeWindow.Input.SetKey(InputKey.D, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        int framesAfterPress = nativeWindow.Session.PresentCount;

        Assert.Equal([TimeSpan.Zero], elapsed);

        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));

        Assert.Equal(framesAfterPress + 2, nativeWindow.Session.PresentCount);
        Assert.Equal(
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)],
            elapsed);

        nativeWindow.Input.SetKey(InputKey.D, false);
        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        int framesAfterRelease = nativeWindow.Session.PresentCount;

        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.Equal(framesAfterRelease, nativeWindow.Session.PresentCount);
        Assert.Equal(3, elapsed.Count);
    }

    [Fact]
    public void HeldKeyRaisesInputHeldOnTheWindowItselfWithNothingFocused()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Hold" } };
        List<TimeSpan> elapsed = [];
        List<bool> fromTheWindow = [];
        window.InputHeld += (sender, args) =>
        {
            elapsed.Add(args.Elapsed);
            fromTheWindow.Add(ReferenceEquals(sender, window) && ReferenceEquals(args.OriginalSource, window));
        };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        nativeWindow.Input.SetKey(InputKey.D, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));

        Assert.Equal(
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)],
            elapsed);
        Assert.All(fromTheWindow, isFromTheWindow => Assert.True(isFromTheWindow));
    }

    [Fact]
    public void HeldKeyWithoutAnInputHeldHandlerDoesNotScheduleFrames()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Hold" } };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        nativeWindow.Input.SetKey(InputKey.D, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        int framesAfterPress = nativeWindow.Session.PresentCount;

        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));

        Assert.Equal(framesAfterPress, nativeWindow.Session.PresentCount);
    }

    // Characterization (Scene2D awake, Stage 0): a shown window with no work still advances the render time on every pump
    // (WindowApplicationRuntime.PumpOnce calls AdvanceRenderTime first), and renders no frame.
    [Fact]
    public void PumpOnceWithNothingToDoStillAdvancesRenderTimeForAShownWindowAndRendersNoFrame()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        RenderTimeProbe probe = new();
        Window window = new() { Content = probe };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        probe.Times.Clear();
        int presentsBefore = nativeWindow.Session.PresentCount;

        bool rendered = runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.False(rendered);
        Assert.Equal(presentsBefore, nativeWindow.Session.PresentCount);
        Assert.Equal([TimeSpan.FromMilliseconds(16)], probe.Times);
    }

    // Characterization (Scene2D awake, Stage 0): a hidden window does not advance the render time, and the time of the
    // hidden pumps is lost; the first pump after showing it again delivers only its own 16 ms, not 5 s + 16 ms.
    [Fact]
    public void PumpOnceDoesNotAdvanceRenderTimeForAHiddenWindowAndTheTimeIsNotCarriedOver()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        RenderTimeProbe probe = new();
        Window window = new() { Content = probe };
        window.Show();
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        window.Hide();
        probe.Times.Clear();

        runtime.PumpOnce(TimeSpan.FromSeconds(5));

        Assert.Empty(probe.Times);

        window.Show();
        probe.Times.Clear();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal([TimeSpan.FromMilliseconds(16)], probe.Times);
    }

    // Scene2D awake, Stage 2: an awake sprite gets Awake on every real PumpOnce of a shown window; Awake alone, and setting
    // IsAwake, request no frame.
    [Fact]
    public void AnAwakeSpriteGetsAwakeOnEveryPumpWithoutRequestingFrames()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Sprite2D sprite = new();
        Scene2D scene = new();
        scene.Children.Add(sprite);
        Window window = new()
        {
            Content = new RenderSurface2D
            {
                Width = 200,
                Height = 200,
                Scene = scene,
                RedrawMode = RenderSurface2DRedrawMode.OnDemand
            }
        };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        List<TimeSpan> elapsed = [];
        sprite.Awake += (_, args) => elapsed.Add(args.Elapsed);
        int presentsBefore = nativeWindow.Session.PresentCount;

        sprite.IsAwake = true;
        bool first = runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        bool second = runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        bool third = runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal([false, false, false], [first, second, third]);
        Assert.Equal(presentsBefore, nativeWindow.Session.PresentCount);
        Assert.Equal([TimeSpan.Zero, TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)], elapsed);
    }

    // Contract: like every other user callback of an update, an Awake handler raised by PumpOnce runs inside the UI relay
    // synchronization context, so an async handler continues on the UI thread after an await.
    [Fact]
    public void AnAwakeHandlerRunsInTheRelaySynchronizationContextDuringPumpOnce()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Sprite2D sprite = new();
        Scene2D scene = new();
        scene.Children.Add(sprite);
        Window window = new()
        {
            Content = new RenderSurface2D
            {
                Width = 200,
                Height = 200,
                Scene = scene,
                RedrawMode = RenderSurface2DRedrawMode.OnDemand
            }
        };
        window.Show();
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        List<SynchronizationContext?> contexts = [];
        sprite.Awake += (_, _) => contexts.Add(SynchronizationContext.Current);

        sprite.IsAwake = true;
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        SynchronizationContext? recorded = Assert.Single(contexts);
        Assert.True(
            recorded is Cerneala.UI.Relay.UiRelaySynchronizationContext,
            $"The Awake handler ran with SynchronizationContext.Current = {recorded?.GetType().FullName ?? "null"}.");
    }

    // Scene2D awake, Stage 2: a handler that changes the scene makes PumpOnce render in the same pump; once the last awake
    // sprite sleeps, further pumps render nothing and raise no Awake.
    [Fact]
    public void AfterTheLastAwakeObjectSleepsPumpOnceRendersNoMoreFrames()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Sprite2D sprite = new();
        Scene2D scene = new();
        scene.Children.Add(sprite);
        Window window = new()
        {
            Content = new RenderSurface2D
            {
                Width = 200,
                Height = 200,
                Scene = scene,
                RedrawMode = RenderSurface2DRedrawMode.OnDemand
            }
        };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        List<TimeSpan> elapsed = [];
        sprite.Awake += (_, args) =>
        {
            elapsed.Add(args.Elapsed);
            sprite.X += 1;
            if (elapsed.Count == 3)
            {
                sprite.IsAwake = false;
            }
        };
        sprite.IsAwake = true;
        List<(bool Rendered, int Presents, int Awakes)> pumps = [];

        for (int pump = 0; pump < 6; pump++)
        {
            bool rendered = runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            pumps.Add((rendered, nativeWindow.Session.PresentCount, elapsed.Count));
        }

        int presentsBefore = pumps[0].Presents - 1;
        Assert.Equal(
            [
                (true, presentsBefore + 1, 1),
                (true, presentsBefore + 2, 2),
                (true, presentsBefore + 3, 3),
                (false, presentsBefore + 3, 3),
                (false, presentsBefore + 3, 3),
                (false, presentsBefore + 3, 3)
            ],
            pumps);
        Assert.Equal([TimeSpan.Zero, TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)], elapsed);
        Assert.Equal(3f, sprite.X);
    }

    private sealed class RenderTimeProbe : UIElement, Cerneala.UI.Rendering.ITimeSensitiveRenderElement
    {
        public List<TimeSpan> Times { get; } = [];

        public bool UpdateRenderTime(TimeSpan frameTime)
        {
            Times.Add(frameTime);
            return false;
        }
    }

    // A contact handler moves the fox away in the middle of a frame, after the delivery point. The Exited event waits
    // for the next delivery, and nothing else asks for that frame, so the pump has to.
    [Fact]
    public void ContactChangesMadeByAContactHandlerKeepPumpOnceRenderingUntilTheyAreDelivered()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        Sprite2D fox = new() { X = 100, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = coin });
        scene.Children.Add(fox);
        List<string> events = [];
        coin.Entered += (_, _) =>
        {
            events.Add("entered");
            fox.X = 100;
        };
        coin.Exited += (_, _) => events.Add("exited");
        // A Continuous surface (the default) asks for a frame on every pump, so only an OnDemand one can go idle.
        Window window = new() { Content = new RenderSurface2D { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand } };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        for (int settle = 0; settle < 8 && runtime.PumpOnce(TimeSpan.FromMilliseconds(16)); settle++) { }
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.Empty(events);

        fox.X = 5;

        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.Equal(["entered"], events);
        Assert.True(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.Equal(["entered", "exited"], events);
        int framesAfterDelivery = nativeWindow.Session.PresentCount;
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        Assert.Equal(framesAfterDelivery, nativeWindow.Session.PresentCount);
    }

    [Fact]
    public async Task ServoFirstFrameClickResolvesAfterLayoutQueuedByTheSameFrameCallback()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Border header = new() { Height = 16 };
        Button button = new() { Content = "Target", Width = 160, Height = 32 };
        ServoApi.SetId(button, "target");
        StackPanel panel = new();
        panel.VisualChildren.Add(header);
        panel.VisualChildren.Add(button);
        Window window = new() { Content = panel };
        ServoApi servo = new(window);
        Task? action = null;
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        window.FrameRendered += (_, _) =>
        {
            if (action is not null) return;
            action = servo.ClickAsync(ServoTarget.ById("target"));
            header.Height = 80;
        };

        window.Show();
        Assert.NotNull(action);
        PumpUntilCompleted(runtime, action);
        await action;

        Assert.Equal(1, clicks);
        Assert.Equal(4, Assert.Single(platform.Windows).Session.PresentCount);
        Assert.False(window.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task ServoQueuedTargetActionRejectsADisabledTargetBeforeDispatchAndRemainsUsable()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 160, Height = 32 };
        ServoApi.SetId(button, "target");
        Window window = new() { Content = button };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(300) });
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Task rejected = servo.ClickAsync(ServoTarget.ById("target"));
        button.IsEnabled = false;
        PumpUntilCompletedWithin(runtime, rejected, TimeSpan.FromSeconds(5));
        ServoTimeoutException timeout = await Assert.ThrowsAsync<ServoTimeoutException>(() => rejected);
        Assert.IsType<ServoTargetNotActionableException>(timeout.InnerException);
        Assert.Equal(0, clicks);
        Assert.False(window.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));

        button.IsEnabled = true;
        Task next = servo.ClickAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.Equal(1, clicks);
    }

    [Theory]
    [InlineData("click")]
    [InlineData("hover")]
    [InlineData("drag")]
    [InlineData("scroll")]
    [InlineData("type")]
    [InlineData("replace")]
    public async Task ServoTargetActionsWaitForATargetThatBecomesEnabledFramesLater(string action)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Text = "old", Width = 160, Height = 32, IsEnabled = false };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        ServoTarget target = ServoTarget.ById("editor");

        Task operation = action switch
        {
            "click" => servo.ClickAsync(target),
            "hover" => servo.HoverAsync(target),
            "drag" => servo.DragAsync(target, new ServoPoint(5, 5), steps: 2),
            "scroll" => servo.ScrollAsync(target, -120),
            "type" => servo.TypeIntoAsync(target, "!"),
            "replace" => servo.ReplaceTextAsync(target, "new"),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        PumpFrames(runtime, 3);
        Assert.False(operation.IsCompleted);

        editor.IsEnabled = true;
        PumpUntilCompleted(runtime, operation);
        await operation;

        var pointer = window.LastFrame!.Input.Pointer;
        switch (action)
        {
            case "click":
                Assert.True(editor.IsKeyboardFocused);
                break;
            case "hover":
                Assert.True(editor.IsMouseOver);
                break;
            case "drag":
                Assert.Equal((5f, 5f), (pointer.X, pointer.Y));
                Assert.False(pointer.IsDown(InputMouseButton.Left));
                break;
            case "scroll":
                Assert.Equal(-120, pointer.WheelValue);
                break;
            case "type":
                Assert.Equal("old!", editor.Text);
                break;
            case "replace":
                Assert.Equal("new", editor.Text);
                break;
        }
    }

    [Fact]
    public async Task ServoClickWaitsForACoveringOverlayToDisappear()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Save", Width = 160, Height = 32 };
        ServoApi.SetId(button, "save");
        Border overlay = new() { Background = new Cerneala.UI.Media.SolidColorBrush(new Color(0, 0, 0)) };
        Cerneala.UI.Layout.Panels.Grid grid = new();
        grid.Children.Add(button);
        grid.Children.Add(overlay);
        Window window = new() { Content = grid };
        window.Show();
        ServoApi servo = new(window);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Task click = servo.ClickAsync(ServoTarget.ById("save"));
        PumpFrames(runtime, 3);
        Assert.False(click.IsCompleted);
        Assert.Equal(0, clicks);

        grid.Children.Remove(overlay);
        PumpUntilCompleted(runtime, click);
        await click;
        Assert.Equal(1, clicks);
    }

    [Fact]
    public async Task ServoClickWaitsForATargetThatAppearsFramesLater()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        StackPanel panel = new();
        Window window = new() { Content = panel };
        window.Show();
        ServoApi servo = new(window);
        Button button = new() { Content = "Later", Width = 160, Height = 32 };
        ServoApi.SetId(button, "later");
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Task click = servo.ClickAsync(ServoTarget.ById("later"));
        PumpFrames(runtime, 3);
        Assert.False(click.IsCompleted);

        panel.VisualChildren.Add(button);
        PumpUntilCompleted(runtime, click);
        await click;
        Assert.Equal(1, clicks);
    }

    // The window runtime dispatches Servo steps with the real frame time (#682). ClickAsync is one click even when the
    // frames of two calls are far inside the double-click time, and a multi-click stays one sequence when its frames are
    // far outside it.
    [Fact]
    public async Task ServoClickCountsAreIndependentOfTheFrameTimeOfTheWindowRuntime()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 160, Height = 32 };
        ServoApi.SetId(button, "target");
        Window window = new() { Content = button };
        window.Show();
        ServoApi servo = new(window);
        List<int> downCounts = [];
        int doubleClicks = 0;
        button.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        button.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++, handledEventsToo: true);

        Task first = servo.ClickAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, first);
        await first;
        Task second = servo.ClickAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, second);
        await second;

        Assert.Equal([1, 1], downCounts);
        Assert.Equal(0, doubleClicks);

        Task multi = servo.ClickAsync(ServoTarget.ById("target"), clickCount: 2);
        for (int frame = 0; frame < 64 && !multi.IsCompleted; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromSeconds(1));
            Thread.Sleep(1);
        }

        Assert.True(multi.IsCompleted);
        await multi;
        Assert.Equal([1, 1, 1, 2], downCounts);
        Assert.Equal(1, doubleClicks);
    }

    [Fact]
    public async Task ServoReplaceTextWaitsForATextBoxThatAppearsFramesLater()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        StackPanel panel = new();
        Window window = new() { Content = panel };
        window.Show();
        ServoApi servo = new(window);
        TextBox editor = new() { Text = "old", Width = 160, Height = 32 };
        ServoApi.SetId(editor, "editor");

        Task replace = servo.ReplaceTextAsync(ServoTarget.ById("editor"), "new");
        PumpFrames(runtime, 3);
        Assert.False(replace.IsCompleted);

        panel.VisualChildren.Add(editor);
        PumpUntilCompleted(runtime, replace);
        await replace;
        Assert.Equal("new", editor.Text);
    }

    [Fact]
    public async Task ServoClickOnATargetThatNeverBecomesActionableTimesOutNamingTheLastReason()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Save", Width = 160, Height = 32, IsEnabled = false };
        ServoApi.SetId(button, "save");
        Window window = new() { Content = button };
        window.Show();
        TimeSpan timeoutDuration = TimeSpan.FromMilliseconds(300);
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = timeoutDuration });
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Stopwatch elapsed = Stopwatch.StartNew();
        Task click = servo.ClickAsync(ServoTarget.ById("save"));
        PumpUntilCompletedWithin(runtime, click, TimeSpan.FromSeconds(5));
        elapsed.Stop();

        ServoTimeoutException timeout = await Assert.ThrowsAsync<ServoTimeoutException>(() => click);
        Assert.True(elapsed.Elapsed >= timeoutDuration, $"Timed out early after {elapsed.Elapsed}.");
        Assert.True(elapsed.Elapsed < timeoutDuration + TimeSpan.FromSeconds(2), $"Timed out late after {elapsed.Elapsed}.");
        Assert.Equal(
            "The Servo operation on target Id=\"save\" exceeded its 00:00:00.3000000 timeout. " +
            "Last failure: The Servo target Id=\"save\" is disabled.",
            timeout.Message);
        ServoTargetNotActionableException lastFailure =
            Assert.IsType<ServoTargetNotActionableException>(timeout.InnerException);
        Assert.Equal("The Servo target Id=\"save\" is disabled.", lastFailure.Message);
        Assert.Equal(0, clicks);
        Assert.False(window.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));

        button.IsEnabled = true;
        Task next = servo.ClickAsync(ServoTarget.ById("save"));
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.Equal(1, clicks);
    }

    [Fact]
    public async Task ServoClickOnAnAmbiguousTargetStillFailsWithoutWaiting()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button first = new() { Content = "First", Width = 160, Height = 32 };
        Button second = new() { Content = "Second", Width = 160, Height = 32 };
        ServoApi.SetId(first, "duplicate");
        ServoApi.SetId(second, "duplicate");
        StackPanel panel = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        Window window = new() { Content = panel };
        window.Show();
        ServoApi servo = new(window);
        int clicks = 0;
        first.Click += (_, _) => clicks++;
        second.Click += (_, _) => clicks++;

        Stopwatch elapsed = Stopwatch.StartNew();
        Task click = servo.ClickAsync(ServoTarget.ById("duplicate"));
        PumpUntilCompleted(runtime, click);

        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(() => click);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(2), $"The ambiguous click waited {elapsed.Elapsed}.");
        Assert.Equal(0, clicks);
    }

    [Fact]
    public async Task ServoWindowActionCompletesOnlyAfterItsInputFrameWasPresented()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "target");
        Window window = new() { Content = button };
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        int presentsBefore = session.PresentCount;
        int presentCountDuringClick = -1;
        button.Click += (_, _) => presentCountDuringClick = session.PresentCount;
        ServoApi servo = new(window);

        Task action = servo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("target"));

        Assert.False(action.IsCompleted);
        Assert.Equal(-1, presentCountDuringClick);
        while (!action.IsCompleted)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        await action;
        Assert.Equal(presentsBefore + 2, presentCountDuringClick);
        Assert.Equal(presentsBefore + 3, session.PresentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServoPointerStaysOnItsTargetAcrossFramesWithoutAServoStep(bool click)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        (Window window, Button button) = CreateHeaderAndTargetWindow();
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        // The physical mouse rests on the header, away from the target, and never moves.
        nativeWindow.Input.MovePointer(5, 5);
        PumpFrames(runtime, nativeWindow, 1);
        int enters = 0;
        int leaves = 0;
        button.AddHandler(UIElement.MouseEnterEvent, (_, _) => enters++, handledEventsToo: true);
        button.AddHandler(UIElement.MouseLeaveEvent, (_, _) => leaves++, handledEventsToo: true);
        ServoApi servo = new(window);

        Task action = click
            ? servo.ClickAsync(ServoTarget.ById("target"))
            : servo.HoverAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, action);
        await action;
        PumpFrames(runtime, nativeWindow, 10);

        Assert.True(button.IsPointerOver);
        Assert.Equal((1, 0), (enters, leaves));

        for (int press = 0; press < 3; press++)
        {
            Task key = servo.PressKeyAsync(InputKey.F2);
            PumpUntilCompleted(runtime, key);
            await key;
            PumpFrames(runtime, nativeWindow, 2);
        }

        Assert.True(button.IsPointerOver);
        Assert.Equal((1, 0), (enters, leaves));
    }

    [Fact]
    public async Task PhysicalPointerMoveAfterAServoHoverTakesThePointerBack()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        (Window window, Button button) = CreateHeaderAndTargetWindow();
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        nativeWindow.Input.MovePointer(5, 5);
        PumpFrames(runtime, nativeWindow, 1);
        ServoApi servo = new(window);
        Task hover = servo.HoverAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, hover);
        await hover;
        PumpFrames(runtime, nativeWindow, 2);
        Assert.True(button.IsPointerOver);

        nativeWindow.Input.MovePointer(6, 5);
        PumpFrames(runtime, nativeWindow, 1);

        Assert.False(button.IsPointerOver);
        Assert.Equal(6, window.LastFrame!.Input.Pointer.X);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyboardOnlyServoInputBeforeAnyServoPointerActionLeavesThePhysicalPointerInPlace(bool text)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        (Window window, Button button) = CreateHeaderAndTargetWindow();
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        // The physical mouse rests on the target (x 320..480, y 40..72) and never moves.
        nativeWindow.Input.MovePointer(400, 56);
        PumpFrames(runtime, nativeWindow, 3);
        Assert.True(button.IsPointerOver);
        int enters = 0;
        int leaves = 0;
        button.AddHandler(UIElement.MouseEnterEvent, (_, _) => enters++, handledEventsToo: true);
        button.AddHandler(UIElement.MouseLeaveEvent, (_, _) => leaves++, handledEventsToo: true);
        ServoApi servo = new(window);

        Task input = text ? servo.SendTextAsync("ab") : servo.PressKeyAsync(InputKey.F2);
        PumpUntilCompleted(runtime, input);
        await input;
        PumpFrames(runtime, nativeWindow, 10);

        Assert.True(button.IsPointerOver);
        Assert.Equal((0, 0), (enters, leaves));
        Assert.Equal((400f, 56f), (window.LastFrame!.Input.Pointer.X, window.LastFrame.Input.Pointer.Y));
    }

    private static (Window Window, Button Button) CreateHeaderAndTargetWindow()
    {
        Border header = new() { Height = 40 };
        Button button = new() { Content = "Target", Width = 160, Height = 32 };
        ServoApi.SetId(button, "target");
        StackPanel panel = new();
        panel.VisualChildren.Add(header);
        panel.VisualChildren.Add(button);
        return (new Window { Content = panel }, button);
    }

    private static void PumpFrames(WindowApplicationRuntime runtime, FakePlatformWindow nativeWindow, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            nativeWindow.RequestRender();
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
    }

    [Fact]
    public async Task ServoSerializesInstancesPerWindowAndKeepsWindowsIndependent()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        List<string> clicks = [];
        Button firstButton = new() { Content = "First" };
        Button secondButton = new() { Content = "Second" };
        ServoApi.SetId(firstButton, "first");
        ServoApi.SetId(secondButton, "second");
        firstButton.Click += (_, _) => clicks.Add("first");
        secondButton.Click += (_, _) => clicks.Add("second");
        Window first = new() { Content = firstButton };
        Window second = new() { Content = secondButton };
        first.Show();
        second.Show();
        ServoApi firstServo = new(first);
        ServoApi sameWindowServo = new(first);
        ServoApi secondServo = new(second);

        Task firstAction = firstServo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("first"));
        Task serializedAction = sameWindowServo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("first"));
        Task independentAction = secondServo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("second"));

        while (!Task.WhenAll(firstAction, serializedAction, independentAction).IsCompleted)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        await Task.WhenAll(firstAction, serializedAction, independentAction);
        Assert.Equal(2, clicks.Count(value => value == "first"));
        Assert.Equal(1, clicks.Count(value => value == "second"));
        Assert.True(platform.Windows[1].Session.PresentCount < platform.Windows[0].Session.PresentCount);
    }

    [Fact]
    public async Task ServoWindowChordFailurePresentsResetBeforeTheNextAction()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        Task focus = servo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;
        bool throwOnce = true;
        KeyEventArgs? nextKey = null;
        editor.AddHandler(
            InputEvents.KeyDownEvent,
            (_, args) =>
            {
                KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
                if (throwOnce && key.Key == InputKey.A)
                {
                    throwOnce = false;
                    throw new InvalidOperationException("Injected window key failure.");
                }

                if (key.Key == InputKey.B)
                {
                    nextKey = key;
                }
            },
            handledEventsToo: true);

        Task failed = servo.PressKeyAsync(
            InputKey.A,
            Cerneala.UI.Servo.ServoModifiers.Control | Cerneala.UI.Servo.ServoModifiers.Shift);
        PumpUntilCompleted(runtime, failed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed);

        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.NotNull(nextKey);
        Assert.False(nextKey.IsControlDown);
        Assert.False(nextKey.IsShiftDown);
        Assert.False(nextKey.IsAltDown);
    }

    [Fact]
    public async Task ServoWindowComposedTextActionsDispatchEveryStepOnTheOwningThread()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Text = "DEFAULT", Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);

        Task typed = servo.TypeIntoAsync(ServoTarget.ById("editor"), "!");
        PumpUntilCompleted(runtime, typed);
        await typed;
        Assert.True(editor.IsKeyboardFocused);
        Assert.Equal("DEFAULT!", editor.Text);

        Task replaced = servo.ReplaceTextAsync(ServoTarget.ById("editor"), "replacement");
        PumpUntilCompleted(runtime, replaced);
        await replaced;
        Assert.Equal("replacement", editor.Text);
    }

    [Fact]
    public void ServoHoldCancellationBetweenDrainCheckAndFirstModifierStepCannotDispatchInput()
    {
        using CancellationTokenSource cancellation = new();
        PointerSnapshot pointer = PointerSnapshot.Empty;
        KeyboardSnapshot empty = KeyboardSnapshot.Empty;
        KeyboardSnapshot control = KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl]);
        KeyboardSnapshot chord = KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl, InputKey.A]);
        static ServoInputStep Step(PointerSnapshot pointer, KeyboardSnapshot before, KeyboardSnapshot after) =>
            new(new InputFrame(pointer, pointer, before, after, []), pointer, after, HasPointerPosition: false);
        InterruptingServoSteps steps = new([
            Step(pointer, empty, control),
            Step(pointer, control, chord),
            Step(pointer, chord, control),
            Step(pointer, control, empty)
        ]);
        ServoInputSequence sequence = new(
            steps, heldStepIndex: 1, holdDuration: TimeSpan.FromSeconds(1),
            cleanupTimeout: TimeSpan.FromMilliseconds(250));
        Type operationType = typeof(WindowApplicationRuntime).GetNestedType(
            "ServoInputOperation", BindingFlags.NonPublic)!;
        object operation = Activator.CreateInstance(
            operationType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: [sequence, cancellation.Token], culture: null)!;
        Task completion = (Task)operationType.GetProperty("Task")!.GetValue(operation)!;

        // Count is read after GetNextFrame's drain/cancel checks but before
        // the first modifier-down step claims ownership.
        steps.InterruptOnNextCount(cancellation.Cancel);
        object? request = operationType.GetMethod("GetNextFrame")!.Invoke(operation, null);

        Assert.True(completion.IsCanceled);
        Assert.Null(request);
    }

    [Fact]
    public void ServoWindowTapPersistentResetFailureDoesNotKeepSerializationGate()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(500) });
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        CompleteSynchronously(focus);

        bool failReset = true;
        int resetAttempts = 0;
        bool nextSawCleanModifiers = false;
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
            if (key.Key == InputKey.A) throw new InvalidOperationException("Injected tap failure.");
            if (key.Key == InputKey.B) nextSawCleanModifiers = !key.IsControlDown;
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.PreviewKeyUpEvent, (_, _) =>
        {
            if (failReset)
            {
                resetAttempts++;
                throw new InvalidOperationException("Injected persistent tap cleanup failure.");
            }
        }, handledEventsToo: true);

        Task failed = servo.PressKeyAsync(InputKey.A, ServoModifiers.Control);
        Stopwatch deadline = Stopwatch.StartNew();
        while (!failed.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(failed.IsCompleted, "A persistent tap reset failure retained the Servo serialization gate.");
        Assert.Throws<InvalidOperationException>(() => CompleteSynchronously(failed));
        Assert.True(resetAttempts >= 1);
        failReset = false;
        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        CompleteSynchronously(next);
        Assert.True(nextSawCleanModifiers);
    }

    [Fact]
    public async Task ServoWindowHoldKeepsOneChordDownAcrossPresentedFramesThenReleases()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        int keyDownCount = 0;
        int keyUpCount = 0;
        int heldFrames = 0;
        long firstHeldFrame = 0;
        long firstReleasedFrame = 0;
        bool nextKeySawRelease = false;
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
            if (key.Key == InputKey.A)
            {
                keyDownCount++;
                Assert.True(key.IsControlDown);
                Assert.True(key.IsShiftDown);
            }
            else if (key.Key == InputKey.B)
            {
                nextKeySawRelease = firstReleasedFrame != 0;
                Assert.False(key.IsControlDown);
                Assert.False(key.IsShiftDown);
            }
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A) keyUpCount++;
        }, handledEventsToo: true);
        window.FrameRendered += (_, _) =>
        {
            if (window.LastFrame!.Input.Keyboard.IsDown(InputKey.A))
            {
                heldFrames++;
                if (firstHeldFrame == 0) firstHeldFrame = Stopwatch.GetTimestamp();
            }
            else if (firstHeldFrame != 0 && firstReleasedFrame == 0)
            {
                firstReleasedFrame = Stopwatch.GetTimestamp();
            }
        };

        TimeSpan duration = TimeSpan.FromMilliseconds(120);
        Task hold = servo.HoldKeyAsync(InputKey.A, duration, ServoModifiers.Control | ServoModifiers.Shift);
        Task next = servo.PressKeyAsync(InputKey.B);
        Stopwatch deadline = Stopwatch.StartNew();
        while (!Task.WhenAll(hold, next).IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(3))
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(Task.WhenAll(hold, next).IsCompleted, "The held key or serialized next action did not finish.");
        await Task.WhenAll(hold, next);
        Assert.True(heldFrames >= 3, $"Expected multiple held input frames, got {heldFrames}.");
        Assert.True(firstReleasedFrame != 0);
        Assert.True(Stopwatch.GetElapsedTime(firstHeldFrame, firstReleasedFrame) >= duration);
        Assert.Equal(1, keyDownCount);
        Assert.Equal(1, keyUpCount);
        Assert.True(nextKeySawRelease);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.False(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftCtrl));
        Assert.False(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftShift));
    }

    [Fact]
    public async Task ServoWindowHoldCancellationReleasesBeforeTaskSettles()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        using CancellationTokenSource beforeStart = new();
        beforeStart.Cancel();
        int presentsBefore = Assert.Single(platform.Windows).Session.PresentCount;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(1), cancellationToken: beforeStart.Token));
        Assert.Equal(presentsBefore, platform.Windows[0].Session.PresentCount);

        using CancellationTokenSource cancellation = new();
        Task hold = servo.HoldKeyAsync(
            InputKey.A, TimeSpan.FromSeconds(2), ServoModifiers.Control, cancellation.Token);
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.True(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftCtrl));

        cancellation.Cancel();
        Assert.False(hold.IsCompleted);
        PumpUntilCompleted(runtime, hold);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hold);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.False(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftCtrl));

        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.B));
    }

    [Fact]
    public async Task ServoWindowHoldFailureResetsModifiersAndValidationDoesNotPresentInput()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        int presentsBefore = Assert.Single(platform.Windows).Session.PresentCount;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            servo.HoldKeyAsync(InputKey.A, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            servo.HoldKeyAsync(InputKey.A, TimeSpan.FromMilliseconds(-1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            servo.HoldKeyAsync(InputKey.None, TimeSpan.FromMilliseconds(10)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            servo.HoldKeyAsync(InputKey.A, TimeSpan.FromMilliseconds(10), (ServoModifiers)int.MaxValue));
        Assert.Equal(presentsBefore, platform.Windows[0].Session.PresentCount);

        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;
        int heldFrames = 0;
        KeyEventArgs? nextKey = null;
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
            if (key.Key == InputKey.B) nextKey = key;
        }, handledEventsToo: true);
        window.FrameRendered += (_, _) =>
        {
            if (window.LastFrame!.Input.Keyboard.IsDown(InputKey.A) && ++heldFrames == 2)
            {
                throw new InvalidOperationException("Injected held-frame failure.");
            }
        };

        Task failed = servo.HoldKeyAsync(
            InputKey.A, TimeSpan.FromSeconds(1), ServoModifiers.Control | ServoModifiers.Shift);
        PumpUntilCompleted(runtime, failed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
        Assert.True(heldFrames >= 2);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Assert.False(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftCtrl));
        Assert.False(window.LastFrame.Input.Keyboard.IsDown(InputKey.LeftShift));

        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.NotNull(nextKey);
        Assert.False(nextKey.IsControlDown);
        Assert.False(nextKey.IsShiftDown);
    }

    [Fact]
    public async Task ServoWindowHoldTimeoutReleasesAndCloseFailsWithoutWaitingForAnotherFrame()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBox { Width = 200, Height = 40 } };
        window.Show();
        ServoApi servo = new(window, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(250)
        });

        Task timedOut = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2));
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        Thread.Sleep(300);
        Assert.False(timedOut.IsCompleted);
        PumpUntilCompleted(runtime, timedOut);
        await Assert.ThrowsAsync<ServoTimeoutException>(() => timedOut);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        Task closing = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2));
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        window.Close();
        Assert.True(window.IsClosed);
        Assert.True(platform.Windows[0].Destroyed);
        Assert.False(runtime.PumpOnce(TimeSpan.FromMilliseconds(16)));
        await Assert.ThrowsAsync<ServoException>(() => closing);
        await Assert.ThrowsAsync<ServoException>(() => servo.PressKeyAsync(InputKey.B));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServoWindowHoldCannotRemainOwnedAfterVisibilityOrViewportLoss(bool hide)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        int released = 0;
        bool nextSawRelease = false;
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A) released++;
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.B)
            {
                nextSawRelease = released == 1 &&
                    !Assert.IsType<KeyEventArgs>(args).IsControlDown;
            }
        }, handledEventsToo: true);

        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2), ServoModifiers.Control);
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        if (hide) window.Hide();
        else platform.Windows[0].ReportBounds(new UiViewport(0, 0), 0, 0, WindowState.Normal);
        Stopwatch completionDeadline = Stopwatch.StartNew();
        while (!hold.IsCompleted && completionDeadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(hold.IsCompleted, "Visibility/viewport loss left a started Servo hold owned by a non-rendering Window.");
        await Assert.ThrowsAsync<ServoException>(() => hold);
        Assert.Equal(1, released);

        if (hide) window.Show();
        else platform.Windows[0].ReportBounds(new UiViewport(800, 600), 0, 0, WindowState.Normal);
        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.True(nextSawRelease);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServoWindowHoldCancellationKeepsItsResultThroughVisibilityOrViewportLoss(bool hide)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBox { Width = 200, Height = 40 } };
        window.Show();
        ServoApi servo = new(window);
        using CancellationTokenSource cancellation = new();
        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2),
            cancellationToken: cancellation.Token);
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        cancellation.Cancel();
        if (hide) window.Hide();
        else platform.Windows[0].ReportBounds(new UiViewport(0, 0), 0, 0, WindowState.Normal);
        Stopwatch completionDeadline = Stopwatch.StartNew();
        while (!hold.IsCompleted && completionDeadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }
        Assert.True(hold.IsCompleted);
        Assert.ThrowsAny<OperationCanceledException>(() => CompleteSynchronously(hold));

        if (hide) window.Show();
        else platform.Windows[0].ReportBounds(new UiViewport(800, 600), 0, 0, WindowState.Normal);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public void ServoWindowHoldTimeoutKeepsItsResultWhenViewportCollapsesBeforeReset()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBox { Width = 200, Height = 40 } };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(250) });
        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2));
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        Thread.Sleep(300);
        platform.Windows[0].ReportBounds(new UiViewport(0, 0), 0, 0, WindowState.Normal);
        Stopwatch completionDeadline = Stopwatch.StartNew();
        while (!hold.IsCompleted && completionDeadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }
        Assert.True(hold.IsCompleted);
        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(hold));
        platform.Windows[0].ReportBounds(new UiViewport(800, 600), 0, 0, WindowState.Normal);
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public async Task ServoWindowHoldCancellationWithoutPumpSettlesAndResetsBeforeNextAction()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(250) });
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        int released = 0;
        bool nextSawRelease = false;
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A) released++;
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.B) nextSawRelease = released == 1;
        }, handledEventsToo: true);
        using CancellationTokenSource cancellation = new();
        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2), cancellationToken: cancellation.Token);
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        cancellation.Cancel();
        Assert.True(SpinWait.SpinUntil(() => hold.IsCompleted, TimeSpan.FromSeconds(2)),
            "Cancellation did not settle after the owner stopped pumping.");
        Assert.ThrowsAny<OperationCanceledException>(() => CompleteSynchronously(hold));
        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.True(nextSawRelease);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public async Task ServoWindowHoldTimeoutWithoutPumpSettlesAndResetsBeforeNextAction()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(250) });
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        int released = 0;
        bool nextSawRelease = false;
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A) released++;
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.B) nextSawRelease = released == 1;
        }, handledEventsToo: true);

        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2));
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        Assert.True(SpinWait.SpinUntil(() => hold.IsCompleted, TimeSpan.FromSeconds(2)),
            "Timeout did not settle after the owner stopped pumping.");
        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(hold));
        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.True(nextSawRelease);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public async Task ServoWindowHoldCleanupFailureRetainsResetBeforeNextAction()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        await focus;

        int releaseAttempts = 0;
        int released = 0;
        bool nextSawRelease = false;
        editor.AddHandler(InputEvents.PreviewKeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A && ++releaseAttempts == 1)
            {
                throw new InvalidOperationException("Injected one-shot cleanup failure.");
            }
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.A) released++;
        }, handledEventsToo: true);
        editor.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key == InputKey.B) nextSawRelease = released == 1;
        }, handledEventsToo: true);

        using CancellationTokenSource cancellation = new();
        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(2), cancellationToken: cancellation.Token);
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
        cancellation.Cancel();
        PumpUntilCompleted(runtime, hold);
        await Assert.ThrowsAsync<InvalidOperationException>(() => hold);
        Assert.True(releaseAttempts >= 2);
        Assert.Equal(1, released);

        Task next = servo.PressKeyAsync(InputKey.B);
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.True(nextSawRelease);
        Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Theory]
    [InlineData("hide")]
    [InlineData("close")]
    [InlineData("reshow")]
    public void ServoHoldCleanupKeyUpLifecycleReentryDoesNotRepeatReleaseOrUpdateClosedHost(string action)
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(2) });
        Task focus = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, focus);
        CompleteSynchronously(focus);

        UiHost host = GetRuntimeHost(runtime, window);
        int updatesAfterClose = 0;
        host.FrameUpdated += (_, _) =>
        {
            if (window.IsClosed) updatesAfterClose++;
        };
        int keyUpCalls = 0;
        editor.AddHandler(InputEvents.KeyUpEvent, (_, args) =>
        {
            if (Assert.IsType<KeyEventArgs>(args).Key != InputKey.A) return;
            if (++keyUpCalls > 3)
            {
                throw new InvalidOperationException("Bounded recursive cleanup in test.");
            }

            if (action == "close") window.Close();
            else
            {
                window.Hide();
                if (action == "reshow") window.Show();
            }
        }, handledEventsToo: true);

        Task hold = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromSeconds(1));
        for (int frame = 0; frame < 4 && !window.LastFrame!.Input.Keyboard.IsDown(InputKey.A); frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
        Assert.True(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));

        window.Hide();
        Assert.Equal(1, keyUpCalls);
        Assert.Equal(0, updatesAfterClose);
        Assert.True(SpinWait.SpinUntil(() => hold.IsCompleted, TimeSpan.FromSeconds(2)));
        Assert.Throws<ServoException>(() => CompleteSynchronously(hold));
        if (action == "close")
        {
            Assert.True(window.IsClosed);
        }
        else
        {
            if (!window.IsShown) window.Show();
            Task next = servo.PressKeyAsync(InputKey.B);
            PumpUntilCompleted(runtime, next);
            CompleteSynchronously(next);
            Assert.False(window.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
            Assert.Equal(1, keyUpCalls);
        }
    }

    [Fact]
    public void RuntimeUsesNativePlatformCursorWhenNoServicesOverrideIsProvided()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new()
        {
            Content = "Hover",
            Cursor = Cursor.Crosshair
        };
        Window window = new() { Content = button };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        LayoutRect bounds = button.ArrangedBounds;
        nativeWindow.Input.MovePointer(
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));
        nativeWindow.RequestRender();

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(CursorShape.Crosshair, platform.Cursor.Current);
    }

    [Fact]
    public void PendingRelayWorkWakesAnOtherwiseIdleStandaloneWindow()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new();
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        int framesBefore = session.PresentCount;
        int executions = 0;
        window.Root!.Relay.Post(() => executions++);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, executions);
        Assert.Equal(framesBefore + 1, session.PresentCount);
        Assert.Equal(1, window.LastFrame!.Stats.RelayExecutedCallbacks);
    }

    [Fact]
    public void SaveScreenshotDrawsACompleteCurrentFrameWithoutReplacingThePresentedBackBuffer()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Current frame" } };
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");

        try
        {
            window.SaveScreenshot(path);

            Assert.Equal(1, session.BeginFrameCount);
            Assert.Equal(2, session.Backend.RenderCount);
            Assert.Equal(1, session.PresentCount);
            Assert.Equal(1, session.SavePngCount);
            Assert.Equal(2, session.RenderCountAtSave);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveScreenshotFromFrameRenderedAfterAChangeCapturesThePresentedFrame()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBlock status = new() { Text = "initial" };
        Window window = new() { Content = status };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");
        string laterPath = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");
        bool handled = false;
        List<string>? textsDrawnForHandlerCapture = null;
        window.FrameRendered += (_, _) =>
        {
            if (handled)
            {
                return;
            }

            handled = true;
            status.Text = "changed";
            window.SaveScreenshot(path);
            textsDrawnForHandlerCapture = nativeWindow.Backend.RenderedTexts[^1];
        };

        try
        {
            nativeWindow.RequestRender();
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            Assert.True(handled);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            Assert.NotNull(textsDrawnForHandlerCapture);
            Assert.Contains("initial", textsDrawnForHandlerCapture);
            Assert.DoesNotContain("changed", textsDrawnForHandlerCapture);

            nativeWindow.RequestRender();
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            window.SaveScreenshot(laterPath);

            Assert.True(File.Exists(laterPath));
            List<string> textsDrawnForLaterCapture = nativeWindow.Backend.RenderedTexts[^1];
            Assert.Contains("changed", textsDrawnForLaterCapture);
            Assert.DoesNotContain("initial", textsDrawnForLaterCapture);
        }
        finally
        {
            File.Delete(path);
            File.Delete(laterPath);
        }
    }

    [Fact]
    public void SaveScreenshotFromFrameRenderedLeavesPrismInvalidationsForTheNextFrame()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBlock status = new() { Text = "initial" };
        Window window = new() { Content = status };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");
        PrismCacheOwnerToken owner = new(4242);
        bool handled = false;
        int captureFirstRender = -1;
        int captureEndRender = -1;
        window.FrameRendered += (_, _) =>
        {
            if (handled)
            {
                return;
            }

            handled = true;
            // Queued after the presented frame: it belongs to the next commit.
            window.Root!.PrismCacheInvalidations.EnqueueOwner(owner);
            status.Text = "changed";
            captureFirstRender = nativeWindow.Backend.RenderCount;
            window.SaveScreenshot(path);
            captureEndRender = nativeWindow.Backend.RenderCount;
        };

        try
        {
            nativeWindow.RequestRender();
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            Assert.True(handled);
            Assert.True(captureEndRender > captureFirstRender);
            List<List<PrismCacheInvalidation>> drained = nativeWindow.Backend.DrainedPrismInvalidations;
            for (int render = captureFirstRender; render < captureEndRender; render++)
            {
                Assert.Empty(drained[render]);
            }

            nativeWindow.RequestRender();
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

            Assert.True(nativeWindow.Backend.RenderCount > captureEndRender);
            PrismCacheInvalidation invalidation = Assert.Single(drained[captureEndRender]);
            Assert.Equal(PrismCacheInvalidationKind.Owner, invalidation.Kind);
            Assert.Equal(owner, invalidation.OwnerToken);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveScreenshotThatFailsToDrawLeavesNoFileAtThePath()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Current frame" } };
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");
        string existingPath = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");
        byte[] existingBytes = [1, 2, 3, 4, 5];
        File.WriteAllBytes(existingPath, existingBytes);
        session.RenderPngFailure = new IOException("encoder failed");

        try
        {
            IOException thrown = Assert.Throws<IOException>(() => window.SaveScreenshot(path));
            Assert.Same(session.RenderPngFailure, thrown);
            Assert.False(File.Exists(path));

            Assert.Throws<IOException>(() => window.SaveScreenshot(existingPath));
            Assert.Equal(existingBytes, File.ReadAllBytes(existingPath));
        }
        finally
        {
            File.Delete(path);
            File.Delete(existingPath);
        }
    }

    [Fact]
    public async Task ServoScreenshotUsesWindowOwnerForFullAndFreshTargetCapture()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Border target = new()
        {
            Width = 40,
            Height = 20,
            IsEnabled = false,
            IsHitTestVisible = false
        };
        ServoApi.SetId(target, "capture");
        Window window = new() { Content = target, Width = 120, Height = 80 };
        window.Show();
        ServoApi servo = new(window);
        string directory = Path.Combine(Path.GetTempPath(), $"cerneala-servo-capture-{Guid.NewGuid():N}");
        string fullPath = Path.Combine(directory, "full.png");
        string targetPath = Path.Combine(directory, "target.png");

        try
        {
            await servo.SaveScreenshotAsync(fullPath);
            Assert.Null(Assert.Single(platform.Windows).Session.LastRegion);

            target.Arrange(new ArrangeContext(new LayoutRect(10.25f, 11.5f, 20.5f, 12.25f)));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            await servo.SaveScreenshotAsync(ServoTarget.ById("capture"), targetPath);

            Assert.Equal(new WindowScreenshotRegion(10, 11, 21, 13), platform.Windows[0].Session.LastRegion);
            Assert.Equal(2, platform.Windows[0].Session.SavePngCount);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ServoScreenshotAwaitedFromWindowRelayRunsAfterRetainedCommit()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Capture", Width = 80, Height = 30 };
        ServoApi.SetId(button, "capture");
        Window window = new() { Content = button, Width = 120, Height = 80 };
        ServoApi servo = new(window);
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-servo-relay-{Guid.NewGuid():N}.png");
        Task? flow = null;
        window.FrameRendered += (_, _) => window.Invalidate(
            Cerneala.UI.Invalidation.InvalidationFlags.Render,
            "keep the next relay phase ahead of retained commit");
        window.ContentRendered += (_, _) => flow = ClickThenCaptureAsync();

        try
        {
            window.Show();
            Assert.NotNull(flow);
            PumpUntilCompleted(runtime, flow);
            await flow;

            Assert.Equal(1, Assert.Single(platform.Windows).Session.SavePngCount);
        }
        finally
        {
            File.Delete(path);
        }

        async Task ClickThenCaptureAsync()
        {
            await servo.ClickAsync(ServoTarget.ById("capture"));
            await servo.SaveScreenshotAsync(path);
        }
    }

    [Fact]
    public async Task ServoTargetCaptureRejectsMissingAmbiguousHiddenZeroAndOutsideTargets()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        UIElement container = new();
        Border first = new() { Width = 20, Height = 20 };
        Border second = new() { Width = 20, Height = 20 };
        ServoApi.SetId(first, "target");
        ServoApi.SetId(second, "other");
        container.VisualChildren.Add(first);
        container.VisualChildren.Add(second);
        Window window = new() { Content = container, Width = 100, Height = 60 };
        window.Show();
        ServoApi servo = new(window);
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-servo-target-{Guid.NewGuid():N}.png");

        try
        {
            await Assert.ThrowsAsync<ServoTargetNotFoundException>(
                () => servo.SaveScreenshotAsync(ServoTarget.ById("missing"), path));

            ServoApi.SetId(second, "target");
            await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
                () => servo.SaveScreenshotAsync(ServoTarget.ById("target"), path));

            ServoApi.SetId(second, "other");
            first.Visibility = Visibility.Hidden;
            await Assert.ThrowsAsync<ServoTargetNotActionableException>(
                () => servo.SaveScreenshotAsync(ServoTarget.ById("target"), path));

            first.Visibility = Visibility.Visible;
            first.Arrange(new ArrangeContext(new LayoutRect(0, 0, 0, 10)));
            await Assert.ThrowsAsync<ServoTargetNotActionableException>(
                () => servo.SaveScreenshotAsync(ServoTarget.ById("target"), path));

            first.Arrange(new ArrangeContext(new LayoutRect(200, 200, 10, 10)));
            await Assert.ThrowsAsync<ServoTargetNotActionableException>(
                () => servo.SaveScreenshotAsync(ServoTarget.ById("target"), path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ServoHostScreenshotsAreUnsupportedWithoutAnOsFallback()
    {
        UIRoot root = new(100, 60);
        Border target = new() { Width = 20, Height = 20 };
        ServoApi.SetId(target, "target");
        root.VisualChildren.Add(target);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(100, 60) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        ServoApi servo = new(host);

        await Assert.ThrowsAsync<NotSupportedException>(() => servo.SaveScreenshotAsync("full.png"));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => servo.SaveScreenshotAsync(ServoTarget.ById("target"), "target.png"));
    }

    [Fact]
    public async Task ServoWindowTimeoutCloseAndNextActionHaveExplicitLifecycleResults()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Target", Width = 80, Height = 30 };
        ServoApi.SetId(button, "target");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Window window = new() { Content = button };
        window.Show();
        ServoApi servo = new(window, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(1)
        });

        Assert.Throws<ServoTimeoutException>(
            () => CompleteSynchronously(servo.ClickAsync(ServoTarget.ById("target"))));
        Task next = servo.ClickAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.Equal(1, clicks);

        Task waiting = servo.WaitUntilAsync(_ => Task.FromResult(false));
        window.Close();
        Assert.Throws<ServoException>(() => CompleteSynchronously(waiting));
    }

    [Fact]
    public void RelayCallbackCanCloseWindowWithoutRenderingItsDisposedGraphicsSession()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new();
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        window.Root!.Relay.Post(window.Close);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(window.IsClosed);
        Assert.Equal(1, session.BeginFrameCount);
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public void NativeBoundsNotificationsDoNotEchoPropertiesBackToThePlatform()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Width = 800, Height = 600 };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        int appliedBeforeNotification = nativeWindow.ApplyPropertiesCount;
        UiViewport nativeViewport = new(620, 440, 1.25f);
        int locationChangedCount = 0;
        int stateChangedCount = 0;
        window.LocationChanged += (_, _) => locationChangedCount++;
        window.StateChanged += (_, _) => stateChangedCount++;

        nativeWindow.ReportBounds(nativeViewport, 120, 75, WindowState.Maximized);

        Assert.Equal(120, window.Left);
        Assert.Equal(75, window.Top);
        Assert.Equal(WindowState.Maximized, window.WindowState);
        Assert.Equal(nativeViewport, nativeWindow.Viewport);
        Assert.Equal(appliedBeforeNotification, nativeWindow.ApplyPropertiesCount);
        Assert.Equal(2, locationChangedCount);
        Assert.Equal(1, stateChangedCount);

        window.Title = "Updated after native notification";
        Assert.Equal(appliedBeforeNotification + 1, nativeWindow.ApplyPropertiesCount);
    }

    [Fact]
    public void VisualPropertyChangesDoNotReapplyNativeWindowGeometry()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new();
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        int appliedBeforeVisualChange = nativeWindow.ApplyPropertiesCount;

        window.Background = new Cerneala.UI.Media.SolidColorBrush(new Color(10, 20, 30));

        Assert.Equal(appliedBeforeVisualChange, nativeWindow.ApplyPropertiesCount);
    }

    [Fact]
    public async Task DialogInfersActiveOwnerAndRestoresItWhenResultClosesDialog()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window owner = new();
        owner.Show();
        owner.Activate();
        Window dialog = new();

        Task<bool?> result = dialog.ShowDialogAsync();

        Assert.Same(owner, dialog.Owner);
        Assert.False(platform.Windows[0].Enabled);
        dialog.DialogResult = true;

        Assert.True(await result);
        Assert.True(platform.Windows[0].Enabled);
        Assert.True(dialog.IsClosed);
        Assert.Empty(owner.OwnedWindows);
    }

    [Fact]
    public void DisablingAnOwnerDuringATextBoxPressReleasesTheSelectionCaptureBeforeTheDialogCloses()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Width = 300, Height = 40 };
        Button ok = new() { Content = "OK", Width = 120, Height = 40 };
        StackPanel panel = new();
        panel.VisualChildren.Add(editor);
        panel.VisualChildren.Add(ok);
        Window owner = new() { Content = panel, Width = 400, Height = 200 };
        int okClicks = 0;
        int lostCaptures = 0;
        ok.Click += (_, _) => okClicks++;
        editor.AddHandler(UIElement.LostMouseCaptureEvent, (_, _) => lostCaptures++, handledEventsToo: true);
        Window? dialog = null;
        editor.AddHandler(UIElement.PreviewMouseDownEvent, (_, _) =>
        {
            if (dialog is not null) return;
            dialog = new Window { Owner = owner, Width = 220, Height = 120 };
            _ = dialog.ShowDialogAsync();
        }, handledEventsToo: true);
        owner.Show();
        FakePlatformWindow nativeOwner = platform.Windows[0];
        PumpFrames(runtime, nativeOwner, 1);
        LayoutRect editorBounds = editor.ArrangedBounds;
        LayoutRect okBounds = ok.ArrangedBounds;
        float editorY = editorBounds.Y + editorBounds.Height / 2;

        nativeOwner.Input.MovePointer(editorBounds.X + 20, editorY);
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.SetButton(InputMouseButton.Left, down: true);
        PumpFrames(runtime, nativeOwner, 1);

        Assert.False(nativeOwner.Enabled);
        TextSelection afterPress = editor.Selection;
        Assert.Equal(afterPress.Anchor, afterPress.Active);
        Assert.Equal(1, lostCaptures);

        // The release is lost while the owner is disabled; the dialog then closes.
        nativeOwner.Input.ForgetButton(InputMouseButton.Left);
        dialog!.Close();
        PumpFrames(runtime, nativeOwner, 1);

        nativeOwner.Input.MovePointer(editorBounds.X + editorBounds.Width - 10, editorY);
        PumpFrames(runtime, nativeOwner, 1);
        Assert.Equal(afterPress, editor.Selection);

        nativeOwner.Input.MovePointer(okBounds.X + okBounds.Width / 2, okBounds.Y + okBounds.Height / 2);
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.SetButton(InputMouseButton.Left, down: true);
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.SetButton(InputMouseButton.Left, down: false);
        PumpFrames(runtime, nativeOwner, 1);

        Assert.Equal(1, okClicks);
        Assert.Equal(afterPress, editor.Selection);
    }

    [Fact]
    public void DisablingAnOwnerDuringASliderThumbPressCancelsTheThumbDrag()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Slider slider = new() { Minimum = 0, Maximum = 100, Value = 50, Width = 300, Height = 30 };
        Window owner = new() { Content = slider, Width = 400, Height = 200 };
        owner.Show();
        FakePlatformWindow nativeOwner = platform.Windows[0];
        PumpFrames(runtime, nativeOwner, 1);
        Thumb thumb = slider.Track.Thumb;
        List<bool> dragCompletions = [];
        thumb.DragCompleted += (_, args) => dragCompletions.Add(args.Canceled);
        Window? dialog = null;
        thumb.AddHandler(UIElement.PreviewMouseDownEvent, (_, _) =>
        {
            if (dialog is not null) return;
            dialog = new Window { Owner = owner, Width = 220, Height = 120 };
            _ = dialog.ShowDialogAsync();
        }, handledEventsToo: true);
        LayoutRect thumbBounds = thumb.ArrangedBounds;
        float thumbY = thumbBounds.Y + thumbBounds.Height / 2;

        nativeOwner.Input.MovePointer(thumbBounds.X + thumbBounds.Width / 2, thumbY);
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.SetButton(InputMouseButton.Left, down: true);
        PumpFrames(runtime, nativeOwner, 1);

        Assert.False(nativeOwner.Enabled);
        Assert.False(thumb.IsDragging);
        Assert.Equal([true], dragCompletions);

        nativeOwner.Input.ForgetButton(InputMouseButton.Left);
        dialog!.Close();
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.MovePointer(thumbBounds.X + thumbBounds.Width / 2 + 100, thumbY);
        PumpFrames(runtime, nativeOwner, 1);

        Assert.Equal(50, slider.Value);
        Assert.False(thumb.IsDragging);
    }

    [Fact]
    public void DisablingAnOwnerDuringAButtonPressCancelsThePressedStateAndTheClick()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Button button = new() { Content = "Open", Width = 120, Height = 40 };
        Window owner = new() { Content = button, Width = 400, Height = 200 };
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Window? dialog = null;
        button.AddHandler(UIElement.PreviewMouseDownEvent, (_, _) =>
        {
            if (dialog is not null) return;
            dialog = new Window { Owner = owner, Width = 220, Height = 120 };
            _ = dialog.ShowDialogAsync();
        }, handledEventsToo: true);
        owner.Show();
        FakePlatformWindow nativeOwner = platform.Windows[0];
        PumpFrames(runtime, nativeOwner, 1);
        LayoutRect bounds = button.ArrangedBounds;

        nativeOwner.Input.MovePointer(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        PumpFrames(runtime, nativeOwner, 1);
        nativeOwner.Input.SetButton(InputMouseButton.Left, down: true);
        PumpFrames(runtime, nativeOwner, 1);

        Assert.False(nativeOwner.Enabled);
        Assert.False(button.IsPressed);

        nativeOwner.Input.ForgetButton(InputMouseButton.Left);
        dialog!.Close();
        PumpFrames(runtime, nativeOwner, 1);

        Assert.False(button.IsPressed);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public async Task CloseFinishesClosingTheWindowWhenContentDetachThrowsThenPropagatesTheFailure()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window owner = new();
        owner.Show();
        owner.Activate();
        ElementAspect aspect = new(
            "ThrowsOnDetach",
            typeof(UIElement),
            [],
            [],
            _ => new CallbackLifetime(() => throw new InvalidOperationException("Dispose failed")));
        Window dialog = new() { Content = new Border { Aspect = aspect } };
        int closedCount = 0;
        dialog.Closed += (_, _) => closedCount++;
        Task<bool?> result = dialog.ShowDialogAsync();
        FakePlatformWindow nativeDialog = platform.Windows[1];

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(dialog.Close);

        Assert.Equal("Dispose failed", failure.Message);
        Assert.False(dialog.IsShown);
        Assert.True(dialog.IsClosed);
        Assert.Equal(1, closedCount);
        Assert.Equal(1, nativeDialog.DestroyCount);
        Assert.Equal(1, nativeDialog.Session.DisposeCount);
        Assert.DoesNotContain(dialog, runtime.Windows);
        Assert.True(platform.Windows[0].Enabled);
        Assert.Empty(owner.OwnedWindows);
        Assert.True(result.IsCompleted);
        Assert.Null(await result);

        dialog.Close();

        Assert.Equal(1, closedCount);
        Assert.Equal(1, nativeDialog.DestroyCount);
    }

    [Fact]
    public void OwnerClosesWhenClosingAnOwnedWindowThrowsThenPropagatesTheFailure()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window owner = new();
        int ownerClosedCount = 0;
        owner.Closed += (_, _) => ownerClosedCount++;
        owner.Show();
        Window throwing = new() { Owner = owner, Content = CreateContentThatThrowsOnDetach() };
        Window second = new() { Owner = owner };
        throwing.Show();
        second.Show();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(owner.Close);

        Assert.Equal("Dispose failed", failure.Message);
        Assert.True(throwing.IsClosed);
        Assert.True(second.IsClosed);
        Assert.False(owner.IsShown);
        Assert.True(owner.IsClosed);
        Assert.Equal(1, ownerClosedCount);
        Assert.All(platform.Windows, window => Assert.Equal(1, window.DestroyCount));
        Assert.Equal(1, platform.Windows[0].Session.DisposeCount);
        Assert.Empty(runtime.Windows);
    }

    [Fact]
    public void ClosingMainWindowClosesEveryWindowWhenClosingOneThrows()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window main = new();
        Window throwing = new() { Content = CreateContentThatThrowsOnDetach() };
        Window secondary = new();
        runtime.StartMainWindow(main);
        throwing.Show();
        secondary.Show();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(main.Close);

        Assert.Equal("Dispose failed", failure.Message);
        Assert.True(main.IsClosed);
        Assert.True(throwing.IsClosed);
        Assert.True(secondary.IsClosed);
        Assert.Empty(runtime.Windows);
        Assert.All(platform.Windows, window => Assert.Equal(1, window.DestroyCount));
    }

    [Fact]
    public void ShutdownClosesEveryWindowAndExitsWhenClosingOneThrows()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        app.Install(runtime);
        int exitCount = 0;
        app.Exit += (_, _) => exitCount++;
        Window throwing = new() { Content = CreateContentThatThrowsOnDetach() };
        Window secondary = new();
        throwing.Show();
        secondary.Show();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(app.Shutdown);

        Assert.Equal("Dispose failed", failure.Message);
        Assert.True(throwing.IsClosed);
        Assert.True(secondary.IsClosed);
        Assert.Empty(runtime.Windows);
        Assert.All(platform.Windows, window => Assert.Equal(1, window.DestroyCount));
        Assert.Equal(1, exitCount);
        Assert.Null(Application.Current);
    }

    [Fact]
    public void RuntimeDisposeClosesEveryWindowAndExitsWhenClosingOneThrows()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        app.Install(runtime);
        int exitCount = 0;
        app.Exit += (_, _) => exitCount++;
        Window throwing = new() { Content = CreateContentThatThrowsOnDetach() };
        Window secondary = new();
        throwing.Show();
        secondary.Show();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(runtime.Dispose);

        Assert.Equal("Dispose failed", failure.Message);
        Assert.True(throwing.IsClosed);
        Assert.True(secondary.IsClosed);
        Assert.Empty(runtime.Windows);
        Assert.All(platform.Windows, window => Assert.Equal(1, window.DestroyCount));
        Assert.Null(WindowApplicationRuntime.Current);
        Assert.Equal(1, exitCount);
        Assert.Null(Application.Current);
    }

    private static Border CreateContentThatThrowsOnDetach()
    {
        ElementAspect aspect = new(
            "ThrowsOnDetach",
            typeof(UIElement),
            [],
            [],
            _ => new CallbackLifetime(() => throw new InvalidOperationException("Dispose failed")));
        return new Border { Aspect = aspect };
    }

    [Fact]
    public void ClosingCanCancelAndClosedWindowCannotBeShownAgain()
    {
        Install(new FakeWindowPlatform());
        Window window = new();
        bool cancel = true;
        window.Closing += (_, args) => args.Cancel = cancel;
        window.Show();

        window.Close();
        Assert.False(window.IsClosed);

        cancel = false;
        window.Close();
        Assert.True(window.IsClosed);
        Assert.Throws<InvalidOperationException>(window.Show);
    }

    [Fact]
    public void ClosingMainWindowClosesEveryWindowInTheRuntime()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window main = new();
        Window secondary = new();
        runtime.StartMainWindow(main);
        secondary.Show();

        main.Close();

        Assert.True(main.IsClosed);
        Assert.True(secondary.IsClosed);
        Assert.Empty(runtime.Windows);
        Assert.All(platform.Windows, window => Assert.True(window.Destroyed));
    }

    [Fact]
    public void WindowOperationsRejectAnotherThread()
    {
        Install(new FakeWindowPlatform());
        Window window = new();

        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(RecordOnAnotherThread(window.Show));

        Assert.Contains("owning UI thread", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OwnershipRejectsSelfAndCyclesBeforeWindowsAreShown()
    {
        Window first = new();
        Window second = new() { Owner = first };

        Assert.Throws<InvalidOperationException>(() => first.Owner = first);
        Assert.Throws<InvalidOperationException>(() => first.Owner = second);
    }

    [Fact]
    public void GeneratedHostedStartupUsesDiOnceAndLeavesTheExternalHostInControl()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        int factoryCalls = 0;
        GeneratedWindowStartupDescriptor descriptor = new(
            services => services.AddTransient<Window>(_ =>
            {
                factoryCalls++;
                return new Window { Title = "Generated" };
            }),
            provider => provider.GetRequiredService<Window>());
        GeneratedWindowApplication.RegisterStartup(descriptor);

        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));
        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, factoryCalls);
        Assert.Single(runtime.Windows);
        Assert.Equal("Generated", runtime.Windows[0].Title);
        Assert.Equal(2, platform.PumpCount);

        GeneratedWindowApplication.StopHosted();
        Assert.Empty(runtime.Windows);
    }

    [Fact]
    public async Task ServoPlatformInputIsRejectedByAPlatformWithoutInputInjection()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        Window window = new() { Content = editor };
        window.Show();
        ServoApi servo = new(window);
        int presentsBefore = Assert.Single(platform.Windows).Session.PresentCount;

        NotSupportedException failure = await Assert.ThrowsAsync<NotSupportedException>(() =>
            servo.SendPlatformInputAsync(ServoPlatformInput.KeyDown(InputKey.A)));

        Assert.Contains("inject", failure.Message);
        Assert.Equal(presentsBefore, platform.Windows[0].Session.PresentCount);
        Task click = servo.ClickAsync(ServoTarget.ById("editor"));
        PumpUntilCompleted(runtime, click);
        await click;
        Assert.True(editor.IsKeyboardFocused);
    }

    private static WindowApplicationRuntime Install(FakeWindowPlatform platform)
    {
        WindowApplicationRuntime runtime = new(platform);
        WindowApplicationRuntime.Install(runtime);
        return runtime;
    }

    private static void PumpUntilCompleted(WindowApplicationRuntime runtime, Task operation)
    {
        for (int frame = 0; frame < 64 && !operation.IsCompleted; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(operation.IsCompleted, "The Servo operation did not complete within 64 deterministic frames.");
    }

    private static void PumpUntilCompletedWithin(
        WindowApplicationRuntime runtime,
        Task operation,
        TimeSpan maximumDuration)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (!operation.IsCompleted && elapsed.Elapsed < maximumDuration)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(operation.IsCompleted, $"The Servo operation did not complete within {maximumDuration}.");
    }

    private static void PumpFrames(WindowApplicationRuntime runtime, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            Thread.Sleep(5);
        }
    }

    private static void CompleteSynchronously(Task operation)
    {
        operation.GetAwaiter().GetResult();
    }

    // Task.Run can run its work on the calling thread, so an owner-thread check needs a dedicated thread.
    private static Exception? RecordOnAnotherThread(Action action)
    {
        Exception? exception = null;
        Thread thread = new(() => exception = Record.Exception(action));
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Worker thread did not finish.");
        return exception;
    }

    private static (string? AfterInput, string? AfterPump) RunPreviewTextBoxInput(
        bool startAsPreview,
        string initialText,
        Action<WindowApplicationRuntime, Window> input)
    {
        // The runtime runs on its own thread so that a preview call that never returns fails the test instead of hanging it.
        string stage = "setup";
        string? afterInput = null;
        string? afterPump = null;
        Exception? failure = null;
        Thread uiThread = new(() =>
        {
            try
            {
                FakeWindowPlatform platform = new();
                WindowApplicationRuntime runtime = Install(platform);
                TextBox editor = new() { Text = initialText, Width = 200, Height = 40 };
                Window window = new() { Content = editor };
                if (startAsPreview)
                {
                    runtime.StartPreviewWindow(window);
                }
                else
                {
                    window.Show();
                    runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
                }

                LayoutRect bounds = editor.ArrangedBounds;
                runtime.ClickPreview(window, bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));

                Volatile.Write(ref stage, "the preview input call");
                input(runtime, window);
                afterInput = editor.Text;

                Volatile.Write(ref stage, "PumpOnce");
                runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
                afterPump = editor.Text;
                Volatile.Write(ref stage, "done");
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        {
            IsBackground = true
        };

        uiThread.Start();
        bool returned = uiThread.Join(TimeSpan.FromSeconds(10));

        Assert.True(returned, $"Preview input did not return within 10 s; blocked in {Volatile.Read(ref stage)}.");
        Assert.Null(failure);
        return (afterInput, afterPump);
    }

    private static UiHost GetRuntimeHost(WindowApplicationRuntime runtime, Window window)
    {
        IDictionary contexts = (IDictionary)typeof(WindowApplicationRuntime)
            .GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(runtime)!;
        object context = contexts[window]!;
        return (UiHost)context.GetType().GetProperty("Host")!.GetValue(context)!;
    }

    private sealed class InterruptingServoSteps(IReadOnlyList<ServoInputStep> steps)
        : IReadOnlyList<ServoInputStep>
    {
        private Action? onNextCount;

        public int Count
        {
            get
            {
                Interlocked.Exchange(ref onNextCount, null)?.Invoke();
                return steps.Count;
            }
        }

        public ServoInputStep this[int index] => steps[index];

        public void InterruptOnNextCount(Action interrupt) => onNextCount = interrupt;

        public IEnumerator<ServoInputStep> GetEnumerator() => steps.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class FakeWindowPlatform : IWindowPlatform
    {
        public List<FakePlatformWindow> Windows { get; } = [];

        public FakeCursorService Cursor { get; } = new();

        public IPlatformServices PlatformServices => new PlatformServices(Cursor: Cursor);

        public int PumpCount { get; private set; }

        public int PresentedFrameWaitCount { get; private set; }

        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
        {
            FakePlatformWindow created = new(window, callbacks, Windows.Count + 1);
            Windows.Add(created);
            return created;
        }

        public void PumpEvents()
        {
            PumpCount++;
        }

        public void WaitForPresentedFrames()
        {
            PresentedFrameWaitCount++;
        }

        public void Dispose()
        {
            foreach (FakePlatformWindow window in Windows)
            {
                window.Dispose();
            }
        }
    }

    private sealed class FakeCursorService : ICursorService
    {
        public CursorShape Current { get; private set; } = CursorShape.Arrow;

        public void SetCursor(CursorShape shape)
        {
            Current = shape;
        }
    }

    private sealed class FakePlatformWindow : IPlatformWindow
    {
        private readonly Window window;
        private readonly IWindowPlatformCallbacks callbacks;

        public FakePlatformWindow(Window window, IWindowPlatformCallbacks callbacks, int handle)
        {
            this.window = window;
            this.callbacks = callbacks;
            Surface = new FakeWindowSurface(handle);
        }

        public IWindowSurface Surface { get; }

        public UiViewport Viewport { get; private set; } = new(800, 600);

        public MutableInputSource Input { get; } = new();

        public IInputSource InputSource => Input;

        public FakeGraphicsSession Session { get; } = new();

        public FakeDrawingBackend Backend => Session.Backend;

        public IWindowGraphicsSession GraphicsSession => Session;

        public bool Enabled { get; private set; } = true;

        public bool Destroyed => DestroyCount > 0;

        public int DestroyCount { get; private set; }

        public int ApplyPropertiesCount { get; private set; }

        public int ShowCount { get; private set; }

        public bool HadPresentedFrameWhenShown { get; private set; }

        public void ApplyProperties(Window source)
        {
            ApplyPropertiesCount++;
            Viewport = new UiViewport(source.Width, source.Height);
        }

        public void ReportBounds(UiViewport viewport, float left, float top, WindowState state)
        {
            Viewport = viewport;
            callbacks.BoundsChanged(viewport, left, top, state);
        }

        public void RequestRender()
        {
            callbacks.RenderRequested();
        }

        public void SetOwner(IPlatformWindow? owner)
        {
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public void Show()
        {
            ShowCount++;
            HadPresentedFrameWhenShown = Session.PresentCount > 0;
        }

        public void Hide()
        {
        }

        public void Activate()
        {
            callbacks.ActivationChanged(true);
        }

        public void Destroy()
        {
            DestroyCount++;
        }

        public void Dispose()
        {
            Session.Dispose();
        }
    }

    private sealed record FakeWindowSurface(int Id) : IWindowSurface;

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }

    private sealed class MutableInputSource : IInputSource
    {
        private readonly HashSet<InputKey> downKeys = [];
        private PointerSnapshot previousPointer = PointerSnapshot.Empty;
        private PointerSnapshot currentPointer = PointerSnapshot.Empty;
        private KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;

        public InputFrame GetFrame()
        {
            KeyboardSnapshot currentKeyboard = KeyboardSnapshot.FromDownKeys(downKeys);
            InputFrame frame = new(
                previousPointer,
                currentPointer,
                previousKeyboard,
                currentKeyboard,
                []);
            previousPointer = currentPointer;
            previousKeyboard = currentKeyboard;
            return frame;
        }

        public void SetKey(InputKey key, bool down)
        {
            if (down)
            {
                downKeys.Add(key);
            }
            else
            {
                downKeys.Remove(key);
            }
        }

        public void MovePointer(float x, float y)
        {
            currentPointer = currentPointer.WithPosition(x, y);
        }

        public void SetButton(InputMouseButton button, bool down)
        {
            currentPointer = currentPointer.WithButton(button, down);
        }

        // A disabled native window drops held buttons without a release edge, as SdlInputSource does.
        public void ForgetButton(InputMouseButton button)
        {
            currentPointer = currentPointer.WithButton(button, false);
            previousPointer = previousPointer.WithButton(button, false);
        }
    }

    private sealed class FakeDrawingBackend : IDrawingBackend
    {
        public int RenderCount { get; private set; }

        // The text strings of the text commands of each Render call, in call order.
        public List<List<string>> RenderedTexts { get; } = [];

        // The Prism cache invalidations each Render call drained from its frame context,
        // the way SdlGpuDrawingBackend consumes them, in call order.
        public List<List<PrismCacheInvalidation>> DrainedPrismInvalidations { get; } = [];

        public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
        {
            RenderCount++;
            List<PrismCacheInvalidation> drained = [];
            if (frameContext.PrismCacheInvalidations is { } invalidations)
            {
                while (invalidations.TryDequeue(out PrismCacheInvalidation invalidation))
                {
                    drained.Add(invalidation);
                }
            }

            DrainedPrismInvalidations.Add(drained);
            List<string> texts = [];
            for (int index = 0; index < commands.Count; index++)
            {
                DrawCommand command = commands[index];
                if (command.Text is not null)
                {
                    texts.Add(command.Text);
                }

                if (command.TextLayout is not null)
                {
                    foreach (DrawTextLayoutLine line in command.TextLayout.Lines)
                    {
                        texts.Add(line.Text);
                    }
                }
            }

            RenderedTexts.Add(texts);
        }
    }

    private sealed class FakeGraphicsSession : IWindowGraphicsSession, IWindowScreenshotSource
    {
        private bool disposed;

        public FakeDrawingBackend Backend { get; } = new();

        public IDrawingBackend DrawingBackend => Backend;

        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

        public int BeginFrameCount { get; private set; }

        public int PresentCount { get; private set; }

        public int DisposeCount { get; private set; }

        public int SavePngCount { get; private set; }

        public int RenderCountAtSave { get; private set; }

        public WindowScreenshotRegion? LastRegion { get; private set; }

        public TimeSpan PresentDelay { get; set; }

        // When set, RenderPng writes a byte to the output and then throws it.
        public Exception? RenderPngFailure { get; set; }

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
        }

        public void BeginFrame(Color clearColor)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            BeginFrameCount++;
        }

        public void Present()
        {
            Thread.Sleep(PresentDelay);
            PresentCount++;
        }

        public void RenderPng(Stream output, Color clearColor, Action<IDrawingBackend> draw)
        {
            RenderPngCore(output, draw, null);
        }

        public void RenderPng(
            Stream output,
            Color clearColor,
            WindowScreenshotRegion region,
            Action<IDrawingBackend> draw)
        {
            RenderPngCore(output, draw, region);
        }

        private void RenderPngCore(
            Stream output,
            Action<IDrawingBackend> draw,
            WindowScreenshotRegion? region)
        {
            SavePngCount++;
            LastRegion = region;
            draw(Backend);
            RenderCountAtSave = Backend.RenderCount;
            output.WriteByte(0);
            if (RenderPngFailure is not null)
            {
                throw RenderPngFailure;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DisposeCount++;
            disposed = true;
        }
    }
}
