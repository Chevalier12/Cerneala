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

        return WaitForConditionAsync(target, condition, cancellationToken);
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

    private async Task WaitForConditionAsync(
        ServoTarget target,
        ServoCondition condition,
        CancellationToken cancellationToken)
    {
        int ambiguousMatchCount = 0;
        try
        {
            await WaitAsync(
                (root, _) => Task.FromResult(
                    IsConditionSatisfied(root, target, condition, out ambiguousMatchCount)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (
            ambiguousMatchCount > 1 && cancellationToken.IsCancellationRequested)
        {
            throw new ServoTimeoutDiagnosticException(
                $"The Servo target still matched {ambiguousMatchCount} elements; exactly one was required.",
                exception,
                cancellationToken);
        }
    }

    private bool IsConditionSatisfied(
        UIRoot root,
        ServoTarget target,
        ServoCondition condition,
        out int ambiguousMatchCount)
    {
        ambiguousMatchCount = 0;
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
            // Transitions can briefly keep an outgoing and an incoming copy of the target;
            // ambiguity is "not yet satisfied", and the count is reported if it lasts until timeout.
            ambiguousMatchCount = matches.Count;
            return false;
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
}
