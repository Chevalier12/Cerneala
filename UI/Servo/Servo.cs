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
        actionEngine = new ServoActionEngine(queryEngine);
        synchronization = new ServoSynchronization(context, queryEngine);
        captureEngine = new ServoCaptureEngine(queryEngine);
    }

    public Servo(UiHost host, ServoOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        context = ServoContext.ForHost(host);
        Options = ServoOptions.Copy(options);
        actionEngine = new ServoActionEngine(queryEngine);
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

    public Task<ServoElement> FindAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(
            token => context.InvokeAsync(root => queryEngine.Find(root, target), token),
            cancellationToken);
    }

    public Task<IReadOnlyList<ServoElement>> FindAllAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(
            token => context.InvokeAsync(root => queryEngine.FindAll(root, target), token),
            cancellationToken);
    }

    public Task<bool> ExistsAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(
            token => context.InvokeAsync(root => queryEngine.Exists(root, target), token),
            cancellationToken);
    }

    public Task ClickAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(token => context.ExecuteInputAsync(
            (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, target);
                return input.ClickAsync(actionTarget.X, actionTarget.Y, operationToken);
            },
            token), cancellationToken);
    }

    public Task HoverAsync(
        ServoTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(token => context.ExecuteInputAsync(
            (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, target);
                return input.HoverAsync(actionTarget.X, actionTarget.Y, operationToken);
            },
            token), cancellationToken);
    }

    public Task DragAsync(
        ServoTarget source,
        ServoPoint destination,
        int steps = 12,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        return RunAsync(token => context.ExecuteInputAsync(
            (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, source);
                return input.DragAsync(
                    actionTarget.X,
                    actionTarget.Y,
                    destination.X,
                    destination.Y,
                    steps,
                    operationToken);
            },
            token), cancellationToken);
    }

    public Task ScrollAsync(
        ServoTarget target,
        int wheelDelta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(token => context.ExecuteInputAsync(
            (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, target);
                return input.ScrollAsync(actionTarget.X, actionTarget.Y, wheelDelta, operationToken);
            },
            token), cancellationToken);
    }

    public Task PressKeyAsync(
        InputKey key,
        ServoModifiers modifiers = ServoModifiers.None,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => input.PressKeyAsync(key, modifiers, operationToken),
                token),
            cancellationToken);
    }

    public Task SendTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return RunAsync(
            token => context.ExecuteInputAsync(
                (_, input, operationToken) => input.SendTextAsync(text, operationToken),
                token),
            cancellationToken);
    }

    public Task TypeIntoAsync(
        ServoTarget target,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        return RunAsync(token => context.ExecuteInputAsync(
            async (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, target);
                await input.ClickAsync(actionTarget.X, actionTarget.Y, operationToken).ConfigureAwait(false);
                await input.SendTextAsync(text, operationToken).ConfigureAwait(false);
            },
            token), cancellationToken);
    }

    public Task ReplaceTextAsync(
        ServoTarget target,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(text);
        return RunAsync(token => context.ExecuteInputAsync(
            async (root, input, operationToken) =>
            {
                ServoActionTarget actionTarget = actionEngine.ResolveActionable(root, target);
                await input.ClickAsync(actionTarget.X, actionTarget.Y, operationToken).ConfigureAwait(false);
                await input.PressKeyAsync(InputKey.A, ServoModifiers.Control, operationToken).ConfigureAwait(false);
                await input.SendTextAsync(text, operationToken).ConfigureAwait(false);
            },
            token), cancellationToken);
    }

    public Task WaitForAsync(
        ServoTarget target,
        ServoCondition condition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RunAsync(
            token => synchronization.WaitForAsync(target, condition, token),
            cancellationToken);
    }

    public Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return RunAsync(
            token => synchronization.WaitUntilAsync(predicate, token),
            cancellationToken);
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync(synchronization.WaitForIdleAsync, cancellationToken);
    }

    public Task SaveScreenshotAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return RunAsync(
            token => context.SaveScreenshotAsync(path, resolveRegion: null, token),
            cancellationToken);
    }

    public Task SaveScreenshotAsync(
        ServoTarget target,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (context.Window is null)
        {
            return Task.FromException(new NotSupportedException(
                "Servo screenshots require a Window-owned graphics backend."));
        }

        return RunAsync(
            token => context.SaveScreenshotAsync(
                path,
                root => captureEngine.ResolveRegion(root, target),
                token),
            cancellationToken);
    }

    private Task RunAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken) =>
        ServoOperation.RunAsync(Options.DefaultTimeout, operation, cancellationToken);

    private Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) =>
        ServoOperation.RunAsync(Options.DefaultTimeout, operation, cancellationToken);
}
