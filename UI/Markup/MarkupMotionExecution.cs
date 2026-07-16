using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Markup;

public sealed class MarkupMotionExecution
{
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Action? cancel;
    private Action? detach;
    private EventHandler? completed;

    private MarkupMotionExecution(Action? cancel = null, Action? detach = null)
    {
        this.cancel = cancel;
        this.detach = detach;
    }

    public bool IsCompleted { get; private set; }

    public bool IsCanceled { get; private set; }

    public ValueTask Completion => new(completion.Task);

    public event EventHandler? Completed
    {
        add => completed += value;
        remove => completed -= value;
    }

    public static MarkupMotionExecution From(MotionHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        EventHandler<MotionCompletedEventArgs>? handler = null;
        MarkupMotionExecution execution = new(
            () => handle.Cancel(MotionCancelBehavior.KeepCurrent),
            () => handle.Completed -= handler);
        handler = (_, args) =>
        {
            if (args.IsCanceled)
            {
                execution.SetCanceled(invokeCancel: false);
            }
            else
            {
                execution.SetCompleted();
            }
        };
        handle.Completed += handler;
        if (handle.IsCompleted)
        {
            execution.SetCompleted();
        }
        else if (handle.IsCanceled)
        {
            execution.SetCanceled(invokeCancel: false);
        }

        return execution;
    }

    public static MarkupMotionExecution From(MotionGroupHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        MarkupMotionExecution execution = new(handle.Cancel);
        if (handle.IsCompleted)
        {
            execution.SetCompleted();
        }
        else if (handle.IsCanceled)
        {
            execution.SetCanceled(invokeCancel: false);
        }
        else
        {
            _ = ObserveGroupAsync(execution, handle);
        }

        return execution;
    }

    public static MarkupMotionExecution Parallel(params Func<MarkupMotionExecution>[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        List<MarkupMotionExecution> started = new(children.Length);
        try
        {
            foreach (Func<MarkupMotionExecution> child in children)
            {
                ArgumentNullException.ThrowIfNull(child);
                started.Add(child() ?? throw new InvalidOperationException("A Motion execution factory returned null."));
            }
        }
        catch
        {
            foreach (MarkupMotionExecution child in started)
            {
                child.Cancel();
            }

            throw;
        }

        EventHandler? handler = null;
        MarkupMotionExecution execution = new(
            () =>
            {
                foreach (MarkupMotionExecution child in started)
                {
                    child.Cancel();
                }
            },
            () =>
            {
                foreach (MarkupMotionExecution child in started)
                {
                    child.Completed -= handler;
                }
            });
        handler = (_, _) => EvaluateParallel(execution, started);
        foreach (MarkupMotionExecution child in started)
        {
            child.Completed += handler;
        }

        EvaluateParallel(execution, started);
        return execution;
    }

    public static MarkupMotionExecution Sequence(params Func<MarkupMotionExecution>[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (Func<MarkupMotionExecution> child in children)
        {
            ArgumentNullException.ThrowIfNull(child);
        }

        int index = 0;
        MarkupMotionExecution? current = null;
        MarkupMotionExecution? execution = null;
        EventHandler? handler = null;
        Action startNext = () => { };
        startNext = () =>
        {
            if (execution!.IsCompleted || execution.IsCanceled)
            {
                return;
            }

            if (current is not null)
            {
                current.Completed -= handler;
                current = null;
            }

            if (index == children.Length)
            {
                execution.SetCompleted();
                return;
            }

            try
            {
                current = children[index++]() ?? throw new InvalidOperationException("A Motion execution factory returned null.");
            }
            catch
            {
                execution.SetCanceled(invokeCancel: false);
                throw;
            }

            current.Completed += handler;
            if (current.IsCanceled)
            {
                execution.SetCanceled(invokeCancel: false);
            }
            else if (current.IsCompleted)
            {
                startNext();
            }
        };
        handler = (_, _) =>
        {
            if (current!.IsCanceled)
            {
                execution!.SetCanceled(invokeCancel: false);
            }
            else
            {
                startNext();
            }
        };
        execution = new(
            () => current?.Cancel(),
            () =>
            {
                if (current is not null)
                {
                    current.Completed -= handler;
                }

                current = null;
                children = [];
            });
        startNext();
        return execution;
    }

    public void Cancel()
    {
        SetCanceled(invokeCancel: true);
    }

    private static async Task ObserveGroupAsync(MarkupMotionExecution execution, MotionGroupHandle handle)
    {
        try
        {
            await handle.Completion.ConfigureAwait(false);
            execution.SetCompleted();
        }
        catch (OperationCanceledException)
        {
            execution.SetCanceled(invokeCancel: false);
        }
    }

    private static void EvaluateParallel(
        MarkupMotionExecution execution,
        IReadOnlyList<MarkupMotionExecution> children)
    {
        if (execution.IsCompleted || execution.IsCanceled)
        {
            return;
        }

        if (children.Any(child => child.IsCanceled))
        {
            execution.SetCanceled(invokeCancel: true);
        }
        else if (children.All(child => child.IsCompleted))
        {
            execution.SetCompleted();
        }
    }

    private void SetCompleted()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCompleted = true;
        completion.TrySetResult();
        FinishTerminal();
    }

    private void SetCanceled(bool invokeCancel)
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
        Action? cancelAction = invokeCancel ? cancel : null;
        cancelAction?.Invoke();
        completion.TrySetCanceled();
        FinishTerminal();
    }

    private void FinishTerminal()
    {
        detach?.Invoke();
        detach = null;
        cancel = null;
        EventHandler? handler = completed;
        completed = null;
        handler?.Invoke(this, EventArgs.Empty);
    }
}
