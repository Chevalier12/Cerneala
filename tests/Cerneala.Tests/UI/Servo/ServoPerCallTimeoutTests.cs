using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoPerCallTimeoutTests
{
    [Fact]
    public void EveryPublicOperationHasAnOverloadWithARequiredTimeoutBeforeTheToken()
    {
        MethodInfo[] operations = typeof(ServoApi)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .Where(method => method.GetParameters() is [.., ParameterInfo last] &&
                last.ParameterType == typeof(CancellationToken))
            .Where(method => method.GetParameters().All(parameter => parameter.Name != "timeout"))
            .ToArray();

        Assert.Equal(
            new[]
            {
                "ClickAsync", "ClickAsync", "DragAsync", "DragAsync", "ExistsAsync", "FindAllAsync", "FindAsync",
                "HoldKeyAsync", "HoverAsync", "PressKeyAsync", "ReplaceTextAsync", "SaveScreenshotAsync",
                "SaveScreenshotAsync", "ScrollAsync", "SendPlatformInputAsync", "SendTextAsync", "TypeIntoAsync",
                "WaitForAsync", "WaitForIdleAsync", "WaitUntilAsync"
            },
            operations.Select(method => method.Name).Order(StringComparer.Ordinal));
        foreach (MethodInfo operation in operations)
        {
            Type[] expected =
            [
                .. operation.GetParameters()[..^1].Select(parameter => parameter.ParameterType),
                typeof(TimeSpan),
                typeof(CancellationToken)
            ];
            MethodInfo? overload = typeof(ServoApi).GetMethod(operation.Name, expected);
            Assert.True(overload is not null, $"{operation} has no overload with a TimeSpan timeout before the token.");
            ParameterInfo[] parameters = overload.GetParameters();
            Assert.Equal("timeout", parameters[^2].Name);
            Assert.False(parameters[^2].IsOptional);
            Assert.True(parameters[^1].IsOptional);
            Assert.Equal(operation.ReturnType, overload.ReturnType);
        }
    }

    public static TheoryData<string> QueuedOperations => new()
    {
        "find", "find-all", "exists", "click", "hover", "drag-to-point", "drag-to-target", "scroll",
        "press-key", "send-text", "type-into", "replace-text", "wait-for", "wait-until", "wait-for-idle"
    };

    // The operation is started on a thread pool thread, so Servo queues it to the host's Relay, which no frame
    // pumps. It can only end through its timeout; the message says which timeout applied.
    [Theory]
    [MemberData(nameof(QueuedOperations))]
    public void TimeoutOverloadReplacesTheDefaultTimeoutOfThatOperation(string operation)
    {
        (UiHost host, _) = CreateScene();
        ServoApi servo = new(host, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(30) });
        TimeSpan timeout = TimeSpan.FromMilliseconds(100);

        ServoTimeoutException failure = Assert.Throws<ServoTimeoutException>(
            () => Task.Run(() => Start(servo, operation, timeout)).GetAwaiter().GetResult());

        Assert.Contains("exceeded its 00:00:00.1000000 timeout.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TimeoutOverloadDoesNotChangeTheTimeoutOfLaterOperations()
    {
        (UiHost host, _) = CreateScene();
        ServoApi servo = new(host, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(400) });
        ServoTarget target = ServoTarget.ById("bin");

        ServoTimeoutException overridden = Assert.Throws<ServoTimeoutException>(() => Task.Run(
            () => servo.ExistsAsync(target, TimeSpan.FromMilliseconds(100))).GetAwaiter().GetResult());
        ServoTimeoutException standard = Assert.Throws<ServoTimeoutException>(() => Task.Run(
            () => servo.ExistsAsync(target)).GetAwaiter().GetResult());

        Assert.Contains("00:00:00.1000000", overridden.Message, StringComparison.Ordinal);
        Assert.Contains("00:00:00.4000000", standard.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LongerTimeoutLetsOneSlowWaitSucceedWhereTheDefaultTimesOut()
    {
        (UiHost host, Canvas canvas) = CreateScene();
        ServoApi servo = new(host, new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(100) });
        ServoTarget late = ServoTarget.ById("late");

        Task standard = servo.WaitForAsync(late, ServoCondition.Exists);
        Task overridden = servo.WaitForAsync(late, ServoCondition.Exists, TimeSpan.FromSeconds(10));
        Thread.Sleep(400);
        Button button = new() { Content = "Late", Width = 40, Height = 20 };
        ServoApi.SetId(button, "late");
        canvas.VisualChildren.Add(button);
        PumpUntilCompleted(host, overridden);

        await overridden;
        await Assert.ThrowsAsync<ServoTimeoutException>(() => standard);
    }

    [Fact]
    public void TimeoutOverloadsRejectZeroNegativeAndInfiniteTimeoutsBeforeAnyInput()
    {
        (UiHost host, _) = CreateScene();
        ServoApi servo = new(host);
        ServoTarget card = ServoTarget.ById("card");
        UiFrame before = host.LastFrame!;

        foreach (TimeSpan invalid in new[] { TimeSpan.Zero, TimeSpan.FromMilliseconds(-1), Timeout.InfiniteTimeSpan })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.FindAsync(card, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.ClickAsync(card, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.DragAsync(card, ServoTarget.ById("bin"), 4, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.PressKeyAsync(InputKey.A, ServoModifiers.None, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.HoldKeyAsync(InputKey.A, TimeSpan.FromMilliseconds(10), ServoModifiers.None, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.SendPlatformInputAsync([ServoPlatformInput.KeyDown(InputKey.A)], invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.WaitForIdleAsync(invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.SaveScreenshotAsync("shot.png", invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.SaveScreenshotAsync(card, "shot.png", invalid); });
        }

        Assert.Same(before, host.LastFrame);
    }

    // 4294967 s is just below the largest supported timeout of 4294967294 ms (about 49.7 days).
    [Theory]
    [InlineData(5)]
    [InlineData(4294967)]
    [InlineData(60 * 24 * 3600)]
    public void AnAcceptedDefaultTimeoutCanRunAnOperation(int seconds)
    {
        (UiHost host, _) = CreateScene();
        ServoOptions options = new();
        Exception? rejected = Record.Exception(() => options.DefaultTimeout = TimeSpan.FromSeconds(seconds));
        if (rejected is not null)
        {
            Assert.IsType<ArgumentOutOfRangeException>(rejected);
            return;
        }

        ServoApi servo = new(host, options);
        Task<bool>? exists = null;
        Exception? failure = Record.Exception(() =>
        {
            exists = servo.ExistsAsync(ServoTarget.ById("card"));
            for (int frame = 0; frame < 32 && !exists.IsCompleted; frame++)
            {
                host.Update(EmptyInput(), host.Viewport, TimeSpan.FromMilliseconds(16));
                Thread.Sleep(1);
            }
        });
        Exception? faulted = failure ?? (exists is { IsFaulted: true } ? exists.Exception!.GetBaseException() : null);

        Assert.True(
            faulted is null && exists is { IsCompletedSuccessfully: true },
            $"DefaultTimeout={seconds} s was accepted by ServoOptions; ExistsAsync status={exists?.Status.ToString() ?? "not created"}, exception={faulted?.GetType().Name}: {faulted?.Message}");
    }

    [Fact]
    public void TimeoutsAboveTheLargestSupportedTimeoutAreRejectedWhenSetAndPerCall()
    {
        (UiHost host, _) = CreateScene();
        ServoTarget card = ServoTarget.ById("card");
        TimeSpan largest = TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        UiFrame before = host.LastFrame!;

        ServoOptions options = new() { DefaultTimeout = largest };
        Assert.Equal(largest, options.DefaultTimeout);
        ServoApi servo = new(host);
        foreach (TimeSpan invalid in new[] { largest + TimeSpan.FromTicks(1), TimeSpan.FromDays(60), TimeSpan.MaxValue })
        {
            ArgumentOutOfRangeException setter = Assert.Throws<ArgumentOutOfRangeException>(
                () => options.DefaultTimeout = invalid);
            Assert.Equal("value", setter.ParamName);
            ArgumentOutOfRangeException perCall = Assert.Throws<ArgumentOutOfRangeException>(
                () => { _ = servo.ExistsAsync(card, invalid); });
            Assert.Equal("timeout", perCall.ParamName);
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.ClickAsync(card, invalid); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servo.WaitForIdleAsync(invalid); });
        }

        Assert.Equal(largest, options.DefaultTimeout);
        Assert.Same(before, host.LastFrame);
    }

    private static Task Start(ServoApi servo, string operation, TimeSpan timeout)
    {
        ServoTarget card = ServoTarget.ById("card");
        ServoTarget bin = ServoTarget.ById("bin");
        return operation switch
        {
            "find" => servo.FindAsync(card, timeout),
            "find-all" => servo.FindAllAsync(card, timeout),
            "exists" => servo.ExistsAsync(card, timeout),
            "click" => servo.ClickAsync(bin, timeout),
            "hover" => servo.HoverAsync(bin, timeout),
            "drag-to-point" => servo.DragAsync(card, new ServoPoint(200, 50), 4, timeout),
            "drag-to-target" => servo.DragAsync(card, bin, 4, timeout),
            "scroll" => servo.ScrollAsync(bin, -120, timeout),
            "press-key" => servo.PressKeyAsync(InputKey.A, ServoModifiers.None, timeout),
            "send-text" => servo.SendTextAsync("a", timeout),
            "type-into" => servo.TypeIntoAsync(bin, "a", timeout),
            "replace-text" => servo.ReplaceTextAsync(bin, "a", timeout),
            "wait-for" => servo.WaitForAsync(card, ServoCondition.Exists, timeout),
            "wait-until" => servo.WaitUntilAsync(_ => Task.FromResult(true), timeout),
            "wait-for-idle" => servo.WaitForIdleAsync(timeout),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static (UiHost Host, Canvas Canvas) CreateScene()
    {
        Thumb card = new() { Width = 60, Height = 40 };
        ServoApi.SetId(card, "card");
        Canvas.SetLeft(card, 20);
        Canvas.SetTop(card, 60);
        Button bin = new() { Content = "Bin", Width = 80, Height = 60 };
        ServoApi.SetId(bin, "bin");
        Canvas.SetLeft(bin, 220);
        Canvas.SetTop(bin, 20);
        Canvas canvas = new() { Width = 320, Height = 160 };
        canvas.VisualChildren.Add(card);
        canvas.VisualChildren.Add(bin);
        UIRoot root = new(320, 160);
        root.VisualChildren.Add(canvas);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(320, 160) });
        host.Update(EmptyInput(), host.Viewport, TimeSpan.Zero);
        return (host, canvas);
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
        new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
}
