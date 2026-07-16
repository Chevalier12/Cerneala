using Cerneala.UI.Elements;
using Cerneala.UI.Core;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static IDisposable AttachMotionSession(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        MarkupMotionSession session = new(owner);
        owner.AddLifecycleBehavior(session);
        if (owner.IsAttached)
        {
            session.Attach();
        }

        return session;
    }

    public static IDisposable AttachMotionTriggers(UIElement owner, Action attach, Action detach)
    {
        IDisposable lifetime = AttachMotionSession(owner);
        AddMotionTrigger(lifetime, attach, detach);
        return lifetime;
    }

    public static void AddMotionTrigger(IDisposable session, Action attach, Action detach)
    {
        GetMotionSession(session).AddTrigger(attach, detach);
    }

    public static MotionGroupHandle StartMotion(
        IDisposable session,
        Func<IReadOnlyList<MotionHandle>> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return GetMotionSession(session).Start(start);
    }

    public static MarkupMotionExecution StartMotionExecution(
        IDisposable session,
        Func<MarkupMotionExecution> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return GetMotionSession(session).StartExecution(start);
    }

    public static MarkupMotionExecution StartMotionExecution(
        IDisposable session,
        string handleName,
        Func<MarkupMotionExecution> start)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        ArgumentNullException.ThrowIfNull(start);
        return GetMotionSession(session).StartExecution(handleName, start);
    }

    public static void CancelMotionExecution(IDisposable session, string handleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        GetMotionSession(session).CancelExecution(handleName);
    }

    public static MotionHandle StartMotionProperty<T>(
        IDisposable session,
        UIElement target,
        UiProperty<T> property,
        bool hasFrom,
        T from,
        bool toCurrent,
        T to,
        MotionSpec<T>? spec,
        MotionPropertyStartOptions options)
    {
        return GetMotionSession(session).StartProperty(
            target,
            property,
            hasFrom,
            from,
            toCurrent,
            to,
            spec,
            options);
    }

    private static MarkupMotionSession GetMotionSession(IDisposable session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session as MarkupMotionSession
            ?? throw new ArgumentException("The lifetime was not created by AttachMotionSession.", nameof(session));
    }

    private sealed class MarkupMotionSession : IElementLifecycleBehavior, IDisposable
    {
        private readonly UIElement owner;
        private readonly List<(Action Attach, Action Detach)> triggers = [];
        private readonly HashSet<MotionPropertyBinding> bindings = [];
        private readonly List<MotionGroupHandle> groups = [];
        private readonly List<MarkupMotionExecution> executions = [];
        private readonly Dictionary<string, MarkupMotionExecution> executionSlots = new(StringComparer.Ordinal);
        private bool attached;
        private bool disposed;

        public MarkupMotionSession(UIElement owner)
        {
            this.owner = owner;
        }

        public void Attach()
        {
            if (attached || disposed)
            {
                return;
            }

            attached = true;
            foreach ((Action attach, _) in triggers)
            {
                attach();
            }
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            attached = false;
            foreach ((_, Action detach) in triggers)
            {
                detach();
            }

            CancelExecutions();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Detach();
            owner.RemoveLifecycleBehavior(this);
        }

        public void AddTrigger(Action attach, Action detach)
        {
            ArgumentNullException.ThrowIfNull(attach);
            ArgumentNullException.ThrowIfNull(detach);
            ObjectDisposedException.ThrowIf(disposed, this);
            triggers.Add((attach, detach));
            if (attached)
            {
                attach();
            }
        }

        public MotionGroupHandle Start(Func<IReadOnlyList<MotionHandle>> start)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!attached || !owner.IsAttached)
            {
                throw new InvalidOperationException("Motion sessions can start executions only while their owner is attached.");
            }

            IReadOnlyList<MotionHandle> handles = start() ?? throw new InvalidOperationException("A Motion execution returned null handles.");
            MotionHandle[] executionHandles = handles.ToArray();
            MotionGroupHandle group = new(() =>
            {
                foreach (MotionHandle handle in executionHandles)
                {
                    handle.Cancel(MotionCancelBehavior.KeepCurrent);
                }
            });
            groups.RemoveAll(candidate => candidate.IsCompleted || candidate.IsCanceled);
            groups.Add(group);

            int remaining = executionHandles.Count(handle => !handle.IsCompleted && !handle.IsCanceled);
            if (remaining == 0)
            {
                group.Complete();
                return group;
            }

            foreach (MotionHandle handle in executionHandles.Where(handle => !handle.IsCompleted && !handle.IsCanceled))
            {
                handle.Completed += (_, _) =>
                {
                    remaining--;
                    if (remaining == 0)
                    {
                        group.Complete();
                    }
                };
            }

            return group;
        }

        public MotionHandle StartProperty<T>(
            UIElement target,
            UiProperty<T> property,
            bool hasFrom,
            T from,
            bool toCurrent,
            T to,
            MotionSpec<T>? spec,
            MotionPropertyStartOptions options)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(options);
            if (!attached || owner.Root is null || !ReferenceEquals(target.Root, owner.Root))
            {
                throw new InvalidOperationException("Motion targets must be attached to the session owner's UIRoot.");
            }

            MotionSystem motion = owner.Root.Motion;
            MotionPropertyBinding<T> binding = motion.Properties.GetOrCreateBinding(motion, target, property);
            bindings.Add(binding);
            if (hasFrom)
            {
                binding.Value.JumpTo(from);
            }

            MotionSpec<T> effectiveSpec = spec ?? (MotionSpec<T>)motion.AnimatableProperties.Get(property).DefaultSpec;
            T destination = toCurrent ? binding.Value.Current : to;
            return binding.AnimateTo(destination, effectiveSpec, options);
        }

        public MarkupMotionExecution StartExecution(Func<MarkupMotionExecution> start)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!attached || !owner.IsAttached)
            {
                throw new InvalidOperationException("Motion sessions can start executions only while their owner is attached.");
            }

            MarkupMotionExecution execution = start()
                ?? throw new InvalidOperationException("A Motion execution returned null.");
            executions.RemoveAll(candidate => candidate.IsCompleted || candidate.IsCanceled);
            if (!execution.IsCompleted && !execution.IsCanceled)
            {
                executions.Add(execution);
                EventHandler? completed = null;
                completed = (_, _) =>
                {
                    execution.Completed -= completed;
                    executions.Remove(execution);
                };
                execution.Completed += completed;
            }

            return execution;
        }

        public MarkupMotionExecution StartExecution(string handleName, Func<MarkupMotionExecution> start)
        {
            if (executionSlots.Remove(handleName, out MarkupMotionExecution? previous) &&
                !previous.IsCompleted && !previous.IsCanceled)
            {
                previous.Cancel();
            }

            MarkupMotionExecution execution = StartExecution(start);
            if (!execution.IsCompleted && !execution.IsCanceled)
            {
                executionSlots.Add(handleName, execution);
                EventHandler? completed = null;
                completed = (_, _) =>
                {
                    execution.Completed -= completed;
                    if (executionSlots.TryGetValue(handleName, out MarkupMotionExecution? current) &&
                        ReferenceEquals(current, execution))
                    {
                        executionSlots.Remove(handleName);
                    }
                };
                execution.Completed += completed;
            }

            return execution;
        }

        public void CancelExecution(string handleName)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (executionSlots.Remove(handleName, out MarkupMotionExecution? execution) &&
                !execution.IsCompleted && !execution.IsCanceled)
            {
                execution.Cancel();
            }
        }

        private void CancelExecutions()
        {
            foreach (MotionGroupHandle group in groups.Where(group => !group.IsCompleted && !group.IsCanceled))
            {
                group.Cancel();
            }

            groups.Clear();
            MarkupMotionExecution[] activeExecutions = executions
                .Where(execution => !execution.IsCompleted && !execution.IsCanceled)
                .ToArray();
            executions.Clear();
            executionSlots.Clear();
            foreach (MarkupMotionExecution execution in activeExecutions)
            {
                execution.Cancel();
            }
            foreach (MotionPropertyBinding binding in bindings)
            {
                binding.Clear(MotionClearBehavior.RestoreBase);
            }

            bindings.Clear();
        }
    }
}
