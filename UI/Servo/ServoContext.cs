using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Servo;

internal sealed class ServoContext
{
    private static readonly ConditionalWeakTable<UiHost, ServoInputState> HostInputStates = new();
    private readonly Func<UIRoot?> rootProvider;

    private ServoContext(Func<UIRoot?> rootProvider, Window? window, UiHost? host)
    {
        this.rootProvider = rootProvider;
        Window = window;
        Host = host;
    }

    internal Window? Window { get; }

    internal UiHost? Host { get; }

    internal static ServoContext ForWindow(Window window)
    {
        return new ServoContext(() => window.Root, window, null);
    }

    internal static ServoContext ForHost(UiHost host)
    {
        return new ServoContext(() => host.Root, null, host);
    }

    internal Task<T> InvokeAsync<T>(Func<UIRoot, T> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        UIRoot root;
        try
        {
            root = RequireRoot();
        }
        catch (Exception exception)
        {
            return Task.FromException<T>(exception);
        }

        UiRelay relay = root.Relay;
        if (relay.CheckAccess())
        {
            try
            {
                return Task.FromResult(operation(root));
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        return relay.InvokeAsync(
            () =>
            {
                UIRoot currentRoot = RequireRoot();
                if (!ReferenceEquals(root, currentRoot))
                {
                    throw new ServoException("The Servo root changed before the queued operation could run.");
                }

                return operation(currentRoot);
            },
            cancellationToken);
    }

    internal IDisposable SubscribeToFrames(Action frame, Action<Exception> invalidated)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(invalidated);
        if (Window is Window window)
        {
            EventHandler frameHandler = (_, _) => frame();
            EventHandler closedHandler = (_, _) => invalidated(
                new ServoException("The Servo Window closed while an operation was waiting."));
            window.FrameRendered += frameHandler;
            window.Closed += closedHandler;
            return new CallbackSubscription(() =>
            {
                window.FrameRendered -= frameHandler;
                window.Closed -= closedHandler;
            });
        }

        UiHost host = Host ?? throw new ServoException("The Servo context has no frame owner.");
        EventHandler hostFrameHandler = (_, _) => frame();
        EventHandler rootChangedHandler = (_, _) => invalidated(
            new ServoException("The Servo UiHost root changed while an operation was waiting."));
        host.FrameUpdated += hostFrameHandler;
        host.RootChanged += rootChangedHandler;
        return new CallbackSubscription(() =>
        {
            host.FrameUpdated -= hostFrameHandler;
            host.RootChanged -= rootChangedHandler;
        });
    }

    internal bool IsIdle(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ServoInputState state = RequireInputState();
        return !root.Relay.HasPendingWork &&
            !root.Scheduler.HasWork &&
            !root.Motion.HasActiveMotion &&
            !state.Driver.HasActivePointerRepeat &&
            state.Gate.CurrentCount != 0;
    }

    internal Task SaveScreenshotAsync(
        string path,
        Func<UIRoot, WindowScreenshotRegion?>? resolveRegion,
        CancellationToken cancellationToken)
    {
        if (Window is not Window window)
        {
            return Task.FromException(new NotSupportedException(
                "Servo screenshots require a Window-owned graphics backend."));
        }

        return ExecuteSerializedAsync(
            (_, _, token) =>
            {
                token.ThrowIfCancellationRequested();
                WindowApplicationRuntime runtime = window.RuntimeOwner ?? throw new ServoException(
                    "The Servo Window is not attached to a live runtime context.");
                return runtime.SaveServoScreenshotAsync(
                    window,
                    path,
                    resolveRegion,
                    token);
            },
            cancellationToken);
    }

    internal async Task ExecuteInputAsync(
        Func<UIRoot, IServoInputDriver, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await ExecuteSerializedAsync(
            (root, state, token) => operation(root, state.Driver, token),
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task ExecuteSerializedAsync(
        Func<UIRoot, ServoInputState, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ServoInputState state = await InvokeAsync(
            _ => RequireInputState(),
            cancellationToken).ConfigureAwait(false);
        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InvokeAsync(
                root =>
                {
                    ServoInputState currentState = RequireInputState();
                    if (!ReferenceEquals(state, currentState))
                    {
                        throw new ServoException("The Servo input context changed before the operation could run.");
                    }

                    return operation(root, currentState, cancellationToken);
                },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private sealed class CallbackSubscription : IDisposable
    {
        private Action? unsubscribe;

        internal CallbackSubscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref unsubscribe, null)?.Invoke();
        }
    }

    private Task InvokeAsync(
        Func<UIRoot, Task> operation,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        UIRoot root;
        try
        {
            root = RequireRoot();
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }

        UiRelay relay = root.Relay;
        if (relay.CheckAccess())
        {
            try
            {
                return operation(root);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        return relay.InvokeAsync(
            () =>
            {
                UIRoot currentRoot = RequireRoot();
                if (!ReferenceEquals(root, currentRoot))
                {
                    throw new ServoException("The Servo root changed before the queued operation could run.");
                }

                return operation(currentRoot);
            },
            cancellationToken);
    }

    private ServoInputState RequireInputState()
    {
        if (Window is not null)
        {
            WindowApplicationRuntime runtime = Window.RuntimeOwner ?? throw new ServoException(
                "The Servo Window is not attached to a live runtime context.");
            return runtime.GetServoInputState(Window);
        }

        UiHost host = Host ?? throw new ServoException("The Servo context has no input owner.");
        return HostInputStates.GetValue(host, static current => new ServoInputState(current));
    }

    private UIRoot RequireRoot()
    {
        return rootProvider() ?? throw new ServoException(
            Window is null
                ? "The Servo UiHost does not currently have a root."
                : "The Servo Window is not attached to a live UI root.");
    }
}
