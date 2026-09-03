using Cerneala.UI.Elements;

namespace Cerneala.UI.Servo;

internal sealed class ServoSynchronization
{
    private readonly ServoContext context;
    private readonly ServoQueryEngine queryEngine;

    internal ServoSynchronization(ServoContext context, ServoQueryEngine queryEngine)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
    }

    internal Task WaitForAsync(
        ServoTarget target,
        ServoCondition condition,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(condition))
        {
            throw new ArgumentOutOfRangeException(nameof(condition));
        }

        return WaitAsync(
            (root, _) => Task.FromResult(IsConditionSatisfied(root, target, condition)),
            cancellationToken);
    }

    internal Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> predicate,
        CancellationToken cancellationToken) =>
        WaitAsync((_, token) => predicate(token), cancellationToken);

    internal Task WaitForIdleAsync(CancellationToken cancellationToken) =>
        WaitAsync((root, _) => Task.FromResult(context.IsIdle(root)), cancellationToken);

    private async Task WaitAsync(
        Func<UIRoot, CancellationToken, Task<bool>> predicate,
        CancellationToken cancellationToken)
    {
        using ServoFrameSignal signal = new(context);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            signal.ThrowIfInvalidated();
            Task<bool> predicateTask = await context.InvokeAsync(
                root => predicate(root, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (await predicateTask.ConfigureAwait(false))
            {
                return;
            }

            await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private bool IsConditionSatisfied(UIRoot root, ServoTarget target, ServoCondition condition)
    {
        IReadOnlyList<ServoResolvedElement> matches = queryEngine.ResolveAll(root, target);
        if (condition == ServoCondition.Exists)
        {
            return matches.Count > 0;
        }

        if (condition == ServoCondition.Missing)
        {
            return matches.Count == 0;
        }

        if (matches.Count > 1)
        {
            throw new ServoTargetAmbiguousException(
                $"The Servo target matched {matches.Count} elements; exactly one was required.");
        }

        if (matches.Count == 0)
        {
            return false;
        }

        UIElement element = matches[0].Element;
        return condition switch
        {
            ServoCondition.Visible => UIElementVisibility.IsEffectivelyVisible(element),
            ServoCondition.Hidden => !UIElementVisibility.IsEffectivelyVisible(element),
            ServoCondition.Enabled => element.IsEnabled,
            ServoCondition.Disabled => !element.IsEnabled,
            ServoCondition.Focused => element.IsKeyboardFocused,
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
    }

    private sealed class ServoFrameSignal : IDisposable
    {
        private readonly object sync = new();
        private readonly SemaphoreSlim pulse = new(0, 1);
        private readonly IDisposable subscription;
        private Exception? invalidation;
        private bool disposed;

        internal ServoFrameSignal(ServoContext context)
        {
            subscription = context.SubscribeToFrames(Signal, Invalidate);
        }

        internal async Task WaitAsync(CancellationToken cancellationToken)
        {
            ThrowIfInvalidated();
            await pulse.WaitAsync(cancellationToken).ConfigureAwait(false);
            ThrowIfInvalidated();
        }

        internal void ThrowIfInvalidated()
        {
            Exception? failure;
            lock (sync)
            {
                failure = invalidation;
            }

            if (failure is not null)
            {
                throw failure;
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
            }

            subscription.Dispose();
            pulse.Dispose();
        }

        private void Signal()
        {
            lock (sync)
            {
                if (!disposed && pulse.CurrentCount == 0)
                {
                    pulse.Release();
                }
            }
        }

        private void Invalidate(Exception exception)
        {
            lock (sync)
            {
                if (disposed)
                {
                    return;
                }

                invalidation ??= exception;
                if (pulse.CurrentCount == 0)
                {
                    pulse.Release();
                }
            }
        }
    }
}
