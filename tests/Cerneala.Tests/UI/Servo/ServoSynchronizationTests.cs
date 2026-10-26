using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Servo;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ServoSynchronizationTests
{
    [Fact]
    public async Task WaitForSupportsEveryConditionAndStateCardinality()
    {
        Button button = new() { Content = "Target", Width = 120, Height = 40 };
        ServoApi.SetId(button, "target");
        UIRoot root = new(240, 120);
        root.VisualChildren.Add(button);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("target");

        await servo.WaitForAsync(target, ServoCondition.Exists);
        await servo.WaitForAsync(target, ServoCondition.Visible);
        await servo.WaitForAsync(target, ServoCondition.Enabled);

        Task hidden = servo.WaitForAsync(target, ServoCondition.Hidden);
        Assert.False(hidden.IsCompleted);
        button.Visibility = Visibility.Hidden;
        PumpUntilCompleted(host, hidden);
        await hidden;

        Task disabled = servo.WaitForAsync(target, ServoCondition.Disabled);
        button.IsEnabled = false;
        PumpUntilCompleted(host, disabled);
        await disabled;

        button.Visibility = Visibility.Visible;
        button.IsEnabled = true;
        await servo.ClickAsync(target);
        await servo.WaitForAsync(target, ServoCondition.Focused);

        Task missing = servo.WaitForAsync(target, ServoCondition.Missing);
        root.VisualChildren.Remove(button);
        PumpUntilCompleted(host, missing);
        await missing;

        Button first = new() { Content = "First" };
        Button second = new() { Content = "Second" };
        ServoApi.SetId(first, "duplicate");
        ServoApi.SetId(second, "duplicate");
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        await servo.WaitForAsync(ServoTarget.ById("duplicate"), ServoCondition.Exists);
    }

    [Fact]
    public async Task WaitForTreatsTransientAmbiguityAsNotYetSatisfied()
    {
        UIRoot root = new(240, 120);
        Button outgoing = new() { Content = "Next" };
        Button incoming = new() { Content = "Next" };
        ServoApi.SetId(outgoing, "next");
        ServoApi.SetId(incoming, "next");
        root.VisualChildren.Add(outgoing);
        root.VisualChildren.Add(incoming);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);

        Task wait = servo.WaitForAsync(ServoTarget.ById("next"), ServoCondition.Enabled);
        for (int frame = 0; frame < 3; frame++)
        {
            host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.False(wait.IsCompleted, wait.Exception?.InnerException?.ToString());
        root.VisualChildren.Remove(outgoing);
        PumpUntilCompleted(host, wait);
        await wait;
    }

    [Fact]
    public void WaitForTimeoutStatesTheMatchCountWhenTheTargetStaysAmbiguous()
    {
        UIRoot root = new(240, 120);
        for (int index = 0; index < 2; index++)
        {
            Button duplicate = new() { Content = "Next" };
            ServoApi.SetId(duplicate, "next");
            root.VisualChildren.Add(duplicate);
        }

        UiHost host = CreateHost(root);
        ServoApi servo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(200)
        });

        ServoTimeoutException failure = Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(
            servo.WaitForAsync(ServoTarget.ById("next"), ServoCondition.Enabled)));
        Assert.Contains("The Servo target still matched 2 elements; exactly one was required.", failure.Message);
    }

    [Fact]
    public void WaitForTimeoutOmitsAmbiguityThatResolvedBeforeTheLastEvaluation()
    {
        UIRoot root = new(240, 120);
        Button first = new() { Content = "Next" };
        Button second = new() { Content = "Next" };
        ServoApi.SetId(first, "next");
        ServoApi.SetId(second, "next");
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(1)
        });

        Task wait = servo.WaitForAsync(ServoTarget.ById("next"), ServoCondition.Enabled);
        Assert.False(wait.IsCompleted, wait.Exception?.InnerException?.ToString());
        root.VisualChildren.Remove(first);
        root.VisualChildren.Remove(second);
        for (int frame = 0; frame < 20; frame++)
        {
            host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
            Thread.Sleep(5);
        }

        ServoTimeoutException failure = Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(wait));
        Assert.DoesNotContain("matched", failure.Message);
    }

    [Fact]
    public void CallerCancellationOfAnAmbiguousWaitRemainsCancellation()
    {
        UIRoot root = new(240, 120);
        for (int index = 0; index < 2; index++)
        {
            Button duplicate = new() { Content = "Next" };
            ServoApi.SetId(duplicate, "next");
            root.VisualChildren.Add(duplicate);
        }

        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        using CancellationTokenSource cancellation = new();

        Task wait = servo.WaitForAsync(ServoTarget.ById("next"), ServoCondition.Visible, cancellation.Token);
        Assert.False(wait.IsCompleted, wait.Exception?.InnerException?.ToString());
        cancellation.Cancel();

        Exception failure = Assert.ThrowsAny<OperationCanceledException>(() => CompleteSynchronously(wait));
        Assert.IsNotType<ServoTimeoutException>(failure);
    }

    [Fact]
    public async Task WaitForExistsAndMissingKeepCardinalitySemanticsWithMultipleMatches()
    {
        UIRoot root = new(240, 120);
        Button first = new() { Content = "First" };
        Button second = new() { Content = "Second" };
        ServoApi.SetId(first, "duplicate");
        ServoApi.SetId(second, "duplicate");
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("duplicate");

        await servo.WaitForAsync(target, ServoCondition.Exists);

        Task missing = servo.WaitForAsync(target, ServoCondition.Missing);
        Assert.False(missing.IsCompleted, missing.Exception?.InnerException?.ToString());
        root.VisualChildren.Remove(first);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
        Thread.Sleep(1);
        Assert.False(missing.IsCompleted, missing.Exception?.InnerException?.ToString());
        root.VisualChildren.Remove(second);
        PumpUntilCompleted(host, missing);
        await missing;
    }

    [Fact]
    public async Task WaitUntilAllowsAsyncServoQueriesAndPropagatesPredicateFailure()
    {
        UIRoot root = new(200, 100);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        int evaluations = 0;
        Task wait = servo.WaitUntilAsync(async token =>
        {
            evaluations++;
            return await servo.ExistsAsync(ServoTarget.ById("later"), token);
        });
        Assert.False(wait.IsCompleted);

        Button later = new() { Content = "Later" };
        ServoApi.SetId(later, "later");
        root.VisualChildren.Add(later);
        PumpUntilCompleted(host, wait);
        await wait;

        Assert.True(evaluations >= 2);
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servo.WaitUntilAsync(_ => throw new InvalidOperationException("predicate failure")));
        Assert.Equal("predicate failure", failure.Message);
    }

    [Fact]
    public async Task WaitForIdleObservesSchedulerFiniteMotionAndContinuousMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 100, motionClock: clock);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);

        await servo.WaitForIdleAsync();

        root.Invalidate(InvalidationFlags.Render, "scheduled work");
        Task scheduled = servo.WaitForIdleAsync();
        Assert.False(scheduled.IsCompleted);
        PumpUntilCompleted(host, scheduled);
        await scheduled;

        MotionValue<float> finiteValue = root.Motion.Graph.CreateValue(0f);
        finiteValue.AnimateTo(1, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(20)));
        Task finite = servo.WaitForIdleAsync();
        Assert.False(finite.IsCompleted);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        clock.Advance(TimeSpan.FromMilliseconds(20));
        PumpUntilCompleted(host, finite);
        await finite;

        MotionValue<float> continuousValue = root.Motion.Graph.CreateValue(0f);
        MotionHandle continuous = continuousValue.AnimateTo(
            1,
            new RepeatSpec<float>(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10))));
        ServoApi shortServo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(1)
        });
        Assert.Throws<ServoTimeoutException>(
            () => CompleteSynchronously(shortServo.WaitForIdleAsync()));
        continuous.Cancel();
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        await servo.WaitForIdleAsync();
    }

    [Fact]
    public async Task WaitForIdleIsNotIdleWhileInputIsHeldAndAnInputHeldHandlerExists()
    {
        UIRoot root = new(200, 100);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        ServoApi shortServo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(300)
        });
        KeyboardSnapshot heldD = KeyboardSnapshot.FromDownKeys([InputKey.D]);

        // Held input without any handler needs no frames.
        host.Update(HeldKeyInput(KeyboardSnapshot.Empty, heldD), host.Viewport, TimeSpan.FromMilliseconds(16));
        await servo.WaitForIdleAsync();

        root.InputHeld += (_, _) => { };
        host.Update(HeldKeyInput(heldD, heldD), host.Viewport, TimeSpan.FromMilliseconds(16));
        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(shortServo.WaitForIdleAsync()));

        host.Update(HeldKeyInput(heldD, KeyboardSnapshot.Empty), host.Viewport, TimeSpan.FromMilliseconds(16));
        await servo.WaitForIdleAsync();
    }

    [Fact]
    public async Task WaitForIdleIsNotIdleWhileContactChangesWaitForDelivery()
    {
        BoxCollider2D coin = new() { Width = 10, Height = 10, IsTrigger = true };
        Sprite2D fox = new() { X = 100, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = coin });
        scene.Children.Add(fox);
        List<string> events = [];
        coin.Entered += (_, _) =>
        {
            events.Add("entered");
            fox.X = 100; // the Exited event waits for the next delivery
        };
        coin.Exited += (_, _) => events.Add("exited");
        UIRoot root = new(200, 100);
        root.VisualChildren.Add(new RenderSurface2D { Scene = scene });
        UiHost host = CreateHost(root);
        ServoApi servo = new(host);
        ServoApi shortServo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(300)
        });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
        await servo.WaitForIdleAsync();
        Assert.Empty(events);

        fox.X = 5;
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));

        Assert.Equal(["entered"], events);
        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(shortServo.WaitForIdleAsync()));

        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));

        Assert.Equal(["entered", "exited"], events);
        await servo.WaitForIdleAsync();
    }

    // User decision (#762): Servo IsIdle ignores awake nodes, so WaitForIdle completes while a sprite is awake and nothing else has work.
    [Fact]
    public async Task WaitForIdleCompletesWhileASpriteOnASurfaceIsAwake()
    {
        Sprite2D bee = new();
        Scene2D scene = new();
        scene.Children.Add(bee);
        UIRoot root = new(200, 100);
        root.VisualChildren.Add(new RenderSurface2D { Scene = scene });
        UiHost host = CreateHost(root);
        ServoApi servo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(300)
        });
        int awakeCount = 0;
        bee.Awake += (_, _) => awakeCount++;
        bee.IsAwake = true;
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
        host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
        Assert.Equal(2, awakeCount);

        await servo.WaitForIdleAsync();

        Assert.True(bee.IsAwake);
    }

    [Fact]
    public async Task TimeoutCancellationAndRootReplacementDetachWaitCallbacksAndLeaveServoUsable()
    {
        UIRoot firstRoot = new(200, 100);
        UiHost host = CreateHost(firstRoot);
        ServoApi servo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(1)
        });
        int evaluations = 0;

        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(
            servo.WaitUntilAsync(_ =>
            {
                evaluations++;
                return Task.FromResult(false);
            })));
        int evaluationsAtTimeout = evaluations;
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        Thread.Sleep(10);
        Assert.Equal(evaluationsAtTimeout, evaluations);

        using CancellationTokenSource cancellation = new();
        Task canceled = servo.WaitUntilAsync(_ => Task.FromResult(false), cancellation.Token);
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => CompleteSynchronously(canceled));

        Task replaced = servo.WaitUntilAsync(_ => Task.FromResult(false));
        UIRoot secondRoot = new(200, 100);
        Button replacement = new() { Content = "Replacement" };
        ServoApi.SetId(replacement, "replacement");
        secondRoot.VisualChildren.Add(replacement);
        host.SetRoot(secondRoot);
        Assert.Throws<ServoException>(() => CompleteSynchronously(replaced));
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("replacement")));
    }

    [Fact]
    public async Task QueryTimeoutAndDetachedTargetDoNotPoisonTheNextOperation()
    {
        UIRoot root = new(200, 100);
        Button button = new() { Content = "Target" };
        ServoApi.SetId(button, "target");
        root.VisualChildren.Add(button);
        UiHost host = CreateHost(root);
        ServoApi servo = new(host, new ServoOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(1)
        });

        Task<bool> queuedQuery = Task.Run(
            () => servo.ExistsAsync(ServoTarget.ById("target")));
        Assert.Throws<ServoTimeoutException>(() => CompleteSynchronously(queuedQuery));
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("target")));

        Task missing = servo.WaitForAsync(ServoTarget.ById("target"), ServoCondition.Missing);
        root.VisualChildren.Remove(button);
        PumpUntilCompleted(host, missing);
        await missing;
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("target")));
    }

    private static UiHost CreateHost(UIRoot root)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(root.ViewportWidth, root.ViewportHeight)
        });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        return host;
    }

    private static void PumpUntilCompleted(UiHost host, Task operation)
    {
        for (int frame = 0; frame < 32 && !operation.IsCompleted; frame++)
        {
            host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
            Thread.Sleep(1);
        }

        Assert.True(operation.IsCompleted, "The Servo wait did not complete within 32 deterministic frames.");
    }

    private static InputFrame EmptyInput() =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);

    private static InputFrame HeldKeyInput(KeyboardSnapshot previous, KeyboardSnapshot current) =>
        new(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            previous,
            current,
            []);

    private static void CompleteSynchronously(Task operation)
    {
        operation.GetAwaiter().GetResult();
    }
}
