using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyBinding<T> : MotionPropertyBinding
{
    private readonly MotionSystem motion;
    private readonly MotionPropertyInvalidationCategory invalidationCategory;
    private readonly BindingNode node;
    private readonly IDisposable valueSubscription;
    private MotionHandle? activeHandle;
    private bool hasPendingSample;
    private T pendingSample = default!;
    private bool holdOnComplete;
    private bool completedNaturally;
    private bool disposed;

    public MotionPropertyBinding(MotionSystem motion, UiObject target, UiProperty<T> property, MotionValue<T> value)
    {
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        if (!ReferenceEquals(Value.Graph, motion.Graph))
        {
            throw new InvalidOperationException("MotionPropertyBinding requires a MotionValue created by the same MotionSystem as the binding.");
        }

        invalidationCategory = MotionPropertyInvalidationClassifier.Classify(property);
        node = new BindingNode(this);
        valueSubscription = Value.Subscribe(OnValueChanged);
    }

    internal override MotionSystem Motion => motion;

    public override UiObject Target { get; }

    public UiProperty<T> Property { get; }

    public override UiProperty PropertyUntyped => Property;

    public MotionValue<T> Value { get; }

    public MotionHandle AnimateTo(T to, MotionSpec<T> spec, MotionPropertyStartOptions? options = null)
    {
        ThrowIfDisposed();
        motion.VerifyAccess();
        ArgumentNullException.ThrowIfNull(spec);

        MotionPropertyStartOptions effectiveOptions = options ?? MotionPropertyStartOptions.Default;
        holdOnComplete = effectiveOptions.HoldOnComplete;
        completedNaturally = false;
        MotionHandle handle = Value.AnimateTo(to, spec, effectiveOptions.ToMotionStartOptions());
        activeHandle = handle;
        activeHandle.Completed += OnMotionCompleted;
        if (handle.IsCompleted)
        {
            activeHandle = null;
            completedNaturally = true;
            if (holdOnComplete)
            {
                motion.Properties.StageSet(Target, Property, Value.Current, invalidationCategory);
            }
            else
            {
                motion.Properties.StageClear(Target, Property, invalidationCategory);
            }

            return handle;
        }

        StageCurrent();
        motion.Graph.Register(node);
        return handle;
    }

    public override void Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase)
    {
        if (disposed)
        {
            return;
        }

        motion.VerifyAccess();
        activeHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        activeHandle = null;
        completedNaturally = false;

        if (behavior == MotionClearBehavior.HoldCurrent)
        {
            motion.Properties.StageSet(Target, Property, Value.Current, invalidationCategory);
        }
        else
        {
            motion.Properties.StageClear(Target, Property, invalidationCategory);
        }

        motion.Graph.Unregister(node);
    }

    public override void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Clear();
        disposed = true;
        valueSubscription.Dispose();
    }

    private void OnValueChanged(MotionValueChanged<T> change)
    {
        pendingSample = change.NewValue;
        hasPendingSample = true;
    }

    private void OnMotionCompleted(object? sender, MotionCompletedEventArgs args)
    {
        if (args.State != MotionCompletionState.Completed)
        {
            return;
        }

        completedNaturally = true;
        activeHandle = null;
        if (!holdOnComplete)
        {
            motion.Properties.StageClear(Target, Property, invalidationCategory);
        }
    }

    private MotionNodeTickResult Tick()
    {
        if (disposed)
        {
            return new MotionNodeTickResult(Completed: true);
        }

        if (Target is UIElement element && !element.IsAttached)
        {
            Clear();
            return new MotionNodeTickResult(Completed: true);
        }

        if (hasPendingSample)
        {
            motion.Properties.StageSet(Target, Property, pendingSample, invalidationCategory);
            hasPendingSample = false;
        }

        if (completedNaturally && !holdOnComplete)
        {
            motion.Properties.StageClear(Target, Property, invalidationCategory);
        }

        bool finished = completedNaturally || activeHandle is null || !activeHandle.IsActive;
        return new MotionNodeTickResult(Completed: finished);
    }

    private void StageCurrent()
    {
        pendingSample = Value.Current;
        hasPendingSample = true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    private sealed class BindingNode(MotionPropertyBinding<T> owner) : MotionNode
    {
        protected internal override MotionNodeTickResult Tick(MotionFrame frame)
        {
            return owner.Tick();
        }
    }
}
