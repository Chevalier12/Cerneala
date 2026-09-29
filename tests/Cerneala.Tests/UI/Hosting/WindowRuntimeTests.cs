using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Cerneala.Drawing;
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
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Hosting;

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
        ServoApi servo = new(window);
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Task rejected = servo.ClickAsync(ServoTarget.ById("target"));
        button.IsEnabled = false;
        PumpUntilCompleted(runtime, rejected);
        await Assert.ThrowsAsync<ServoTargetNotActionableException>(() => rejected);
        Assert.Equal(0, clicks);
        Assert.False(window.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));

        button.IsEnabled = true;
        Task next = servo.ClickAsync(ServoTarget.ById("target"));
        PumpUntilCompleted(runtime, next);
        await next;
        Assert.Equal(1, clicks);
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
    public void ServoHoldCancellationBetweenDrainCheckAndFirstModifierStepCannotDispatchInput()
    {
        using CancellationTokenSource cancellation = new();
        PointerSnapshot pointer = PointerSnapshot.Empty;
        KeyboardSnapshot empty = KeyboardSnapshot.Empty;
        KeyboardSnapshot control = KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl]);
        KeyboardSnapshot chord = KeyboardSnapshot.FromDownKeys([InputKey.LeftCtrl, InputKey.A]);
        static ServoInputStep Step(PointerSnapshot pointer, KeyboardSnapshot before, KeyboardSnapshot after) =>
            new(new InputFrame(pointer, pointer, before, after, []), pointer, after);
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
    public async Task WindowOperationsRejectAnotherThread()
    {
        Install(new FakeWindowPlatform());
        Window window = new();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(window.Show));

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

    private static void CompleteSynchronously(Task operation)
    {
        operation.GetAwaiter().GetResult();
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

        public bool Destroyed { get; private set; }

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
            Destroyed = true;
        }

        public void Dispose()
        {
            Session.Dispose();
        }
    }

    private sealed record FakeWindowSurface(int Id) : IWindowSurface;

    private sealed class MutableInputSource : IInputSource
    {
        private PointerSnapshot previousPointer = PointerSnapshot.Empty;
        private PointerSnapshot currentPointer = PointerSnapshot.Empty;

        public InputFrame GetFrame()
        {
            InputFrame frame = new(
                previousPointer,
                currentPointer,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []);
            previousPointer = currentPointer;
            return frame;
        }

        public void MovePointer(float x, float y)
        {
            currentPointer = currentPointer.WithPosition(x, y);
        }

        public void SetButton(InputMouseButton button, bool down)
        {
            currentPointer = currentPointer.WithButton(button, down);
        }
    }

    private sealed class FakeDrawingBackend : IDrawingBackend
    {
        public int RenderCount { get; private set; }

        public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
        {
            RenderCount++;
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
