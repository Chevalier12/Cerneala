using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

public sealed class Servo
{
    private readonly ServoContext context;
    private readonly ServoQueryEngine queryEngine = new();
    private readonly ServoActionEngine actionEngine;
    private readonly ServoSynchronization synchronization;
    private readonly ServoCaptureEngine captureEngine;

    public Servo(Window window, ServoOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        context = ServoContext.ForWindow(window);
        Options = ServoOptions.Copy(options);
        actionEngine = new ServoActionEngine(context, queryEngine);
        synchronization = new ServoSynchronization(context, queryEngine);
        captureEngine = new ServoCaptureEngine(queryEngine);
    }

    public Servo(UiHost host, ServoOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        context = ServoContext.ForHost(host);
        Options = ServoOptions.Copy(options);
        actionEngine = new ServoActionEngine(context, queryEngine);
        synchronization = new ServoSynchronization(context, queryEngine);
        captureEngine = new ServoCaptureEngine(queryEngine);
    }

    public static readonly UiProperty<string?> IdProperty = UiProperty<string?>.Register(
        "Id",
        typeof(Servo),
        new UiPropertyMetadata<string?>(
            null,
            UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()));

    internal ServoOptions Options { get; }

    internal Window? Window => context.Window;

    internal UiHost? Host => context.Host;

    public static string? GetId(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(IdProperty);
    }

    public static void SetId(UIElement element, string? id)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IdProperty, id);
    }

    // Every operation has a second overload with a required timeout right before the token. That timeout
    // replaces ServoOptions.DefaultTimeout for that call only.

    public Task<ServoElement> FindAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default) =>
        FindAsync(target, Options.DefaultTimeout, cancellationToken);

    public Task<ServoElement> FindAsync(
        ServoTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.InvokeAsync(root => queryEngine.Find(root, target), token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task<IReadOnlyList<ServoElement>> FindAllAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default) =>
        FindAllAsync(target, Options.DefaultTimeout, cancellationToken);

    public Task<IReadOnlyList<ServoElement>> FindAllAsync(
        ServoTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.InvokeAsync(root => queryEngine.FindAll(root, target), token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task<bool> ExistsAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default) =>
        ExistsAsync(target, Options.DefaultTimeout, cancellationToken);

    public Task<bool> ExistsAsync(
        ServoTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.InvokeAsync(root => queryEngine.Exists(root, target), token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task ClickAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default) =>
        ClickAsync(target, Options.DefaultTimeout, cancellationToken);

    public Task ClickAsync(
        ServoTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        ClickAsync(target, 1, timeout, cancellationToken);

    // Sends clickCount press and release pairs at the center of the target. The input system counts them 1, 2, ..., N,
    // as a real fast multi-click, so clickCount 2 raises MouseDoubleClick once and selects the word of a TextBox, and 3
    // selects its line. The first press always starts a new click sequence, so ClickAsync(target) is one click (count 1)
    // however recently Servo clicked before.
    public Task ClickAsync(
        ServoTarget target,
        int clickCount,
        CancellationToken cancellationToken = default) =>
        ClickAsync(target, clickCount, Options.DefaultTimeout, cancellationToken);

    public Task ClickAsync(
        ServoTarget target,
        int clickCount,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfLessThan(clickCount, 1);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                    target, resolve => input.ClickAsync(resolve, clickCount, operationToken), operationToken),
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task HoverAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default) =>
        HoverAsync(target, Options.DefaultTimeout, cancellationToken);

    public Task HoverAsync(
        ServoTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                    target, resolve => input.HoverAsync(resolve, operationToken), operationToken),
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task DragAsync(
        ServoTarget source,
        ServoPoint destination,
        int steps = 12,
        CancellationToken cancellationToken = default) =>
        DragAsync(source, destination, steps, Options.DefaultTimeout, cancellationToken);

    public Task DragAsync(
        ServoTarget source,
        ServoPoint destination,
        int steps,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                    source,
                    resolve => input.DragAsync(resolve, _ => destination, steps, operationToken),
                    operationToken),
                token),
            cancellationToken,
            DescribeTarget(source));
    }

    // The destination's actionable center is resolved in the same attempt as the source, before the
    // pointer moves. A missing or not-actionable destination is retried like the source, so a drop zone
    // that appears or becomes enabled a few frames later still receives the drop.
    public Task DragAsync(
        ServoTarget source,
        ServoTarget destination,
        int steps = 12,
        CancellationToken cancellationToken = default) =>
        DragAsync(source, destination, steps, Options.DefaultTimeout, cancellationToken);

    public Task DragAsync(
        ServoTarget source,
        ServoTarget destination,
        int steps,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                    source,
                    resolve => input.DragAsync(
                        resolve,
                        root =>
                        {
                            ServoActionTarget end = actionEngine.ResolveActionable(root, destination);
                            return new ServoPoint(end.X, end.Y);
                        },
                        steps,
                        operationToken),
                    operationToken),
                token),
            cancellationToken,
            () => $"The Servo operation on targets {source} and {destination}");
    }

    public Task ScrollAsync(
        ServoTarget target,
        int wheelDelta,
        CancellationToken cancellationToken = default) =>
        ScrollAsync(target, wheelDelta, Options.DefaultTimeout, cancellationToken);

    public Task ScrollAsync(
        ServoTarget target,
        int wheelDelta,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                    target, resolve => input.ScrollAsync(resolve, wheelDelta, operationToken), operationToken),
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task PressKeyAsync(
        InputKey key,
        ServoModifiers modifiers = ServoModifiers.None,
        CancellationToken cancellationToken = default) =>
        PressKeyAsync(key, modifiers, Options.DefaultTimeout, cancellationToken);

    public Task PressKeyAsync(
        InputKey key,
        ServoModifiers modifiers,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => input.PressKeyAsync(key, modifiers, operationToken),
                token),
            cancellationToken);
    }

    public Task HoldKeyAsync(
        InputKey key,
        TimeSpan duration,
        ServoModifiers modifiers = ServoModifiers.None,
        CancellationToken cancellationToken = default) =>
        HoldKeyAsync(key, duration, modifiers, Options.DefaultTimeout, cancellationToken);

    // The timeout also bounds the key-release cleanup after a cancellation, timeout, or failure.
    public Task HoldKeyAsync(
        InputKey key,
        TimeSpan duration,
        ServoModifiers modifiers,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        if (context.Window is null)
        {
            return Task.FromException(new NotSupportedException(
                "Held Servo key input requires a Window-owned frame runtime."));
        }

        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => input.HoldKeyAsync(
                    key, duration, modifiers, timeout, operationToken),
                token),
            cancellationToken);
    }

    public Task SendTextAsync(
        string text,
        CancellationToken cancellationToken = default) =>
        SendTextAsync(text, Options.DefaultTimeout, cancellationToken);

    public Task SendTextAsync(
        string text,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => input.SendTextAsync(text, operationToken),
                token),
            cancellationToken);
    }

    public Task TypeIntoAsync(
        ServoTarget target,
        string text,
        CancellationToken cancellationToken = default) =>
        TypeIntoAsync(target, text, Options.DefaultTimeout, cancellationToken);

    public Task TypeIntoAsync(
        ServoTarget target,
        string text,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                [
                    (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                        target, resolve => input.ClickAsync(resolve, 1, operationToken), operationToken),
                    (_, input, operationToken) => input.SendTextAsync(text, operationToken)
                ],
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task ReplaceTextAsync(
        ServoTarget target,
        string text,
        CancellationToken cancellationToken = default) =>
        ReplaceTextAsync(target, text, Options.DefaultTimeout, cancellationToken);

    public Task ReplaceTextAsync(
        ServoTarget target,
        string text,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        // Ctrl+A is the select-all shortcut of the Cerneala text controls on every operating system, and
        // these steps are Cerneala input frames, not platform key events. Selected text is removed by the
        // typed replacement, or by Delete when the replacement is empty. The final step checks the result.
        return ServoOperation.RunAsync(
            timeout,
            token => context.ExecuteInputAsync(
                [
                    // The text-value check runs in each click attempt before input: a target without a
                    // text value fails at once, a missing or not-yet-actionable one is waited for.
                    (_, input, operationToken) => actionEngine.DispatchWhenActionableAsync(
                        target,
                        resolve => input.ClickAsync(resolve, 1, operationToken),
                        operationToken,
                        root => actionEngine.ReadTextValue(root, target)),
                    (_, input, operationToken) => input.PressKeyAsync(InputKey.A, ServoModifiers.Control, operationToken),
                    text.Length == 0
                        ? (_, input, operationToken) => input.PressKeyAsync(InputKey.Delete, ServoModifiers.None, operationToken)
                        : (_, input, operationToken) => input.SendTextAsync(text, operationToken),
                    (root, _, _) =>
                    {
                        actionEngine.VerifyReplacedText(root, target, text);
                        return Task.CompletedTask;
                    }
                ],
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    public Task SendPlatformInputAsync(params ServoPlatformInput[] events) =>
        SendPlatformInputAsync(events, CancellationToken.None);

    public Task SendPlatformInputAsync(
        IReadOnlyList<ServoPlatformInput> events,
        CancellationToken cancellationToken = default) =>
        SendPlatformInputAsync(events, Options.DefaultTimeout, cancellationToken);

    public Task SendPlatformInputAsync(
        IReadOnlyList<ServoPlatformInput> events,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        ServoPlatformInput[] snapshot = [.. events];
        if (snapshot.Length == 0)
        {
            throw new ArgumentException("At least one platform input event is required.", nameof(events));
        }

        if (Array.IndexOf(snapshot, null) >= 0)
        {
            throw new ArgumentException("A platform input event cannot be null.", nameof(events));
        }

        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        if (context.Window is null)
        {
            return Task.FromException(new NotSupportedException(
                "Servo platform input requires a Window-owned frame runtime."));
        }

        return ServoOperation.RunAsync(
            timeout,
            token => context.SendPlatformInputAsync(snapshot, token),
            cancellationToken);
    }

    public Task WaitForAsync(
        ServoTarget target,
        ServoCondition condition,
        CancellationToken cancellationToken = default) =>
        WaitForAsync(target, condition, Options.DefaultTimeout, cancellationToken);

    public Task WaitForAsync(
        ServoTarget target,
        ServoCondition condition,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => synchronization.WaitForAsync(target, condition, token),
            cancellationToken,
            () => condition == ServoCondition.Exists
                ? $"The Servo wait for target {target} to exist"
                : $"The Servo wait for target {target} to be {condition}");
    }

    public Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> predicate,
        CancellationToken cancellationToken = default) =>
        WaitUntilAsync(predicate, Options.DefaultTimeout, cancellationToken);

    public Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => synchronization.WaitUntilAsync(predicate, token),
            cancellationToken);
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default) =>
        WaitForIdleAsync(Options.DefaultTimeout, cancellationToken);

    public Task WaitForIdleAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(timeout, synchronization.WaitForIdleAsync, cancellationToken);
    }

    public Task SaveScreenshotAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        SaveScreenshotAsync(path, Options.DefaultTimeout, cancellationToken);

    public Task SaveScreenshotAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        return ServoOperation.RunAsync(
            timeout,
            token => context.SaveScreenshotAsync(path, resolveRegion: null, token),
            cancellationToken);
    }

    public Task SaveScreenshotAsync(
        ServoTarget target,
        string path,
        CancellationToken cancellationToken = default) =>
        SaveScreenshotAsync(target, path, Options.DefaultTimeout, cancellationToken);

    public Task SaveScreenshotAsync(
        ServoTarget target,
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ServoOptions.ValidateTimeout(timeout, nameof(timeout));
        if (context.Window is null)
        {
            return Task.FromException(new NotSupportedException(
                "Servo screenshots require a Window-owned graphics backend."));
        }

        return ServoOperation.RunAsync(
            timeout,
            token => context.SaveScreenshotAsync(
                path,
                root => captureEngine.ResolveRegion(root, target),
                token),
            cancellationToken,
            DescribeTarget(target));
    }

    // Example: the timeout message of ClickAsync(ServoTarget.ById("save")) starts with
    // The Servo operation on target Id="save".
    private static Func<string> DescribeTarget(ServoTarget target) =>
        () => $"The Servo operation on target {target}";
}
