using Cerneala.UI.Elements;
using Cerneala.UI.Core;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Prism.Runtime;

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

    public static MotionHandle StartPrismMotionProperty<T>(
        IDisposable session,
        UIElement target,
        int propertyId,
        Func<PrismInstance, T> getValue,
        Action<PrismInstance, T> setValue,
        bool discrete,
        bool hasFrom,
        T from,
        bool toCurrent,
        T to,
        MotionSpec<T>? spec,
        MotionPropertyStartOptions options)
    {
        return GetMotionSession(session).StartPrismProperty(
            target,
            propertyId,
            getValue,
            setValue,
            discrete,
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
        private readonly Dictionary<PrismMotionPropertyKey, PrismMotionBinding> prismBindings = [];
        private readonly List<MotionGroupHandle> groups = [];
        private readonly List<MarkupMotionExecution> executions = [];
        private readonly Dictionary<string, MarkupMotionExecution> executionSlots = new(StringComparer.Ordinal);
        private bool attached;
        private bool renderable;
        private bool disposed;

        public MarkupMotionSession(UIElement owner)
        {
            this.owner = owner;
        }

        private bool CanStart =>
            attached &&
            owner.IsAttached &&
            UIElementVisibility.IsEffectivelyVisible(owner);

        public void Attach()
        {
            if (attached || disposed)
            {
                return;
            }

            attached = true;
            SetRenderable(UIElementVisibility.IsEffectivelyVisible(owner));
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            attached = false;
            SetRenderable(false);
        }

        public void OnRenderabilityChanged(bool isRenderable)
        {
            if (!attached || disposed)
            {
                return;
            }

            SetRenderable(isRenderable);
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
            if (renderable)
            {
                attach();
            }
        }

        public MotionGroupHandle Start(Func<IReadOnlyList<MotionHandle>> start)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!CanStart)
            {
                throw new InvalidOperationException(
                    "Motion sessions can start executions only while their owner is attached and renderable.");
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
            if (!CanStart ||
                owner.Root is null ||
                !ReferenceEquals(target.Root, owner.Root))
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

        public MotionHandle StartPrismProperty<T>(
            UIElement target,
            int propertyId,
            Func<PrismInstance, T> getValue,
            Action<PrismInstance, T> setValue,
            bool discrete,
            bool hasFrom,
            T from,
            bool toCurrent,
            T to,
            MotionSpec<T>? spec,
            MotionPropertyStartOptions options)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(getValue);
            ArgumentNullException.ThrowIfNull(setValue);
            ArgumentNullException.ThrowIfNull(options);
            if (!CanStart ||
                owner.Root is null ||
                !ReferenceEquals(target.Root, owner.Root))
            {
                throw new InvalidOperationException(
                    "Prism Motion targets must be attached to the renderable session owner's UIRoot.");
            }

            PrismInstance instance = GetPrismInstance(target);
            PrismMotionPropertyKey key = new(target, propertyId);
            PrismMotionBinding<T> binding;
            if (prismBindings.TryGetValue(key, out PrismMotionBinding? existing))
            {
                if (existing is not PrismMotionBinding<T> typed)
                {
                    throw new InvalidOperationException(
                        "A Prism Motion property id was reused with an incompatible value type.");
                }

                if (ReferenceEquals(typed.Instance, instance))
                {
                    binding = typed;
                }
                else
                {
                    typed.Dispose(restoreBase: false);
                    binding = CreatePrismBinding(
                        owner.Root.Motion,
                        target,
                        instance,
                        getValue,
                        setValue,
                        discrete);
                    prismBindings[key] = binding;
                }
            }
            else
            {
                binding = CreatePrismBinding(
                    owner.Root.Motion,
                    target,
                    instance,
                    getValue,
                    setValue,
                    discrete);
                prismBindings.Add(key, binding);
            }

            if (hasFrom)
            {
                binding.JumpTo(from);
            }

            MotionSpec<T> effectiveSpec = spec ?? PrismDefaultMotionSpec<T>.Value;
            T destination = toCurrent ? binding.Current : to;
            return binding.AnimateTo(destination, effectiveSpec, options);
        }

        public MarkupMotionExecution StartExecution(Func<MarkupMotionExecution> start)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!CanStart)
            {
                throw new InvalidOperationException(
                    "Motion sessions can start executions only while their owner is attached and renderable.");
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

        private void SetRenderable(bool value)
        {
            if (renderable == value)
            {
                return;
            }

            renderable = value;
            if (value)
            {
                foreach ((Action attach, _) in triggers)
                {
                    attach();
                }

                return;
            }

            try
            {
                foreach ((_, Action detach) in triggers)
                {
                    detach();
                }
            }
            finally
            {
                CancelExecutions();
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
            foreach (PrismMotionBinding binding in prismBindings.Values)
            {
                binding.Dispose(restoreBase: false);
            }

            prismBindings.Clear();
        }

        private static PrismMotionBinding<T> CreatePrismBinding<T>(
            MotionSystem motion,
            UIElement target,
            PrismInstance instance,
            Func<PrismInstance, T> getValue,
            Action<PrismInstance, T> setValue,
            bool discrete)
        {
            ValueMixer<T> mixer = discrete
                ? DiscretePrismMixer<T>.Instance
                : motion.Mixers.Resolve<T>("Prism property");
            return new PrismMotionBinding<T>(
                motion,
                target,
                instance,
                getValue,
                setValue,
                mixer);
        }
    }

    private readonly record struct PrismMotionPropertyKey(
        UIElement Target,
        int PropertyId);

    private abstract class PrismMotionBinding
    {
        public abstract void Dispose(bool restoreBase);
    }

    private sealed class PrismMotionBinding<T> : PrismMotionBinding
    {
        private readonly MotionSystem motion;
        private readonly UIElement target;
        private readonly Func<PrismInstance, T> getValue;
        private readonly Action<PrismInstance, T> setValue;
        private readonly T baseValue;
        private readonly MotionValue<T> value;
        private readonly BindingNode node;
        private readonly IDisposable valueSubscription;
        private MotionHandle? activeHandle;
        private T pendingSample = default!;
        private bool hasPendingSample;
        private bool holdOnComplete;
        private bool completedNaturally;
        private bool disposed;

        public PrismMotionBinding(
            MotionSystem motion,
            UIElement target,
            PrismInstance instance,
            Func<PrismInstance, T> getValue,
            Action<PrismInstance, T> setValue,
            ValueMixer<T> mixer)
        {
            this.motion = motion;
            this.target = target;
            Instance = instance;
            this.getValue = getValue;
            this.setValue = setValue;
            baseValue = getValue(instance);
            value = motion.Graph.CreateValue(baseValue, mixer);
            node = new BindingNode(this);
            valueSubscription = value.Subscribe(OnValueChanged);
        }

        public PrismInstance Instance { get; }

        public T Current => value.Current;

        public void JumpTo(T next)
        {
            ThrowIfDisposed();
            value.JumpTo(next);
        }

        public MotionHandle AnimateTo(
            T destination,
            MotionSpec<T> spec,
            MotionPropertyStartOptions options)
        {
            ThrowIfDisposed();
            holdOnComplete = options.HoldOnComplete;
            completedNaturally = false;
            MotionHandle handle = value.AnimateTo(
                destination,
                spec,
                options.ToMotionStartOptions());
            activeHandle = handle;
            handle.Completed += OnMotionCompleted;
            if (!target.IsAttached ||
                !UIElementVisibility.IsEffectivelyVisible(target) ||
                !IsCurrentInstance())
            {
                Clear(restoreBase: false);
                return handle;
            }

            if (handle.IsCompleted)
            {
                activeHandle = null;
                completedNaturally = true;
            }

            StageCurrent();
            motion.Graph.Register(node);
            return handle;
        }

        public override void Dispose(bool restoreBase)
        {
            if (disposed)
            {
                return;
            }

            Clear(restoreBase);
            disposed = true;
            valueSubscription.Dispose();
        }

        private void Clear(bool restoreBase)
        {
            if (disposed)
            {
                return;
            }

            activeHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
            activeHandle = null;
            completedNaturally = false;
            hasPendingSample = false;
            motion.Graph.Unregister(node);
            if (restoreBase &&
                target.IsAttached &&
                UIElementVisibility.IsEffectivelyVisible(target) &&
                IsCurrentInstance())
            {
                setValue(Instance, baseValue);
            }
        }

        private MotionNodeTickResult Tick()
        {
            if (disposed)
            {
                return new MotionNodeTickResult(Completed: true);
            }

            if (!target.IsAttached ||
                !UIElementVisibility.IsEffectivelyVisible(target) ||
                !IsCurrentInstance())
            {
                Clear(restoreBase: false);
                return new MotionNodeTickResult(Completed: true);
            }

            int writes = 0;
            if (hasPendingSample)
            {
                writes += WriteIfChanged(pendingSample);
                hasPendingSample = false;
            }

            if (completedNaturally && !holdOnComplete)
            {
                writes += WriteIfChanged(baseValue);
            }

            bool finished =
                completedNaturally ||
                activeHandle is null ||
                !activeHandle.IsActive;
            return new MotionNodeTickResult(
                PropertyWrites: writes,
                Completed: finished,
                RenderInvalidations: writes);
        }

        private int WriteIfChanged(T next)
        {
            PrismValueVersion before = Instance.ValueVersion;
            setValue(Instance, next);
            return before == Instance.ValueVersion ? 0 : 1;
        }

        private void OnValueChanged(MotionValueChanged<T> change)
        {
            pendingSample = change.NewValue;
            hasPendingSample = true;
        }

        private void OnMotionCompleted(object? sender, MotionCompletedEventArgs args)
        {
            if (!ReferenceEquals(sender, activeHandle))
            {
                return;
            }

            activeHandle = null;
            completedNaturally = args.State == MotionCompletionState.Completed;
        }

        private bool IsCurrentInstance()
        {
            return TryGetPrismInstance(target, out PrismInstance? current) &&
                ReferenceEquals(current, Instance);
        }

        private void StageCurrent()
        {
            pendingSample = value.Current;
            hasPendingSample = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private sealed class BindingNode(PrismMotionBinding<T> owner) : MotionNode
        {
            protected internal override MotionNodeTickResult Tick(MotionFrame frame)
            {
                return owner.Tick();
            }
        }
    }

    private sealed class DiscretePrismMixer<T> : ValueMixer<T>
    {
        public static DiscretePrismMixer<T> Instance { get; } = new();

        public override T Mix(T from, T to, float progress)
        {
            return progress < 1f ? from : to;
        }
    }

    private static class PrismDefaultMotionSpec<T>
    {
        public static MotionSpec<T> Value { get; } = new TweenSpec<T>(
            TimeSpan.FromMilliseconds(180),
            Easings.Standard);
    }
}
