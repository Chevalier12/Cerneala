using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public sealed class MotionStateBuilder
{
    private readonly Dictionary<UiProperty, StateProperty> properties = new(ReferenceEqualityComparer.Instance);
    private bool subscribed;
    private AspectStateSet observedStates = AspectStateSet.Empty;

    internal MotionStateBuilder(MotionElementFacade facade)
    {
        Facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    internal MotionElementFacade Facade { get; }

    public MotionStateTargetBuilder When(AspectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new MotionStateTargetBuilder(this, state);
    }

    internal MotionStateBuilder Set<T>(
        AspectState state,
        UiProperty<T> property,
        T value,
        MotionSpec<T> spec)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(spec);
        Facade.ResolveMotion();
        EnsureSubscribed();

        if (!properties.TryGetValue(property, out StateProperty? stateProperty))
        {
            stateProperty = new StateProperty<T>(
                Facade,
                property,
                Facade.Element.GetValue(property));
            properties.Add(property, stateProperty);
        }

        if (stateProperty is not StateProperty<T> typed)
        {
            throw new InvalidOperationException(
                $"Motion state property '{property.DiagnosticName}' has an incompatible value type.");
        }

        typed.Set(state, value, spec);
        typed.Evaluate(observedStates, force: false);
        return this;
    }

    private void EnsureSubscribed()
    {
        if (subscribed)
        {
            return;
        }

        subscribed = true;
        observedStates = AspectStateSet.FromElement(Facade.Element);
        Facade.Element.PropertyChanged += OnPropertyChanged;
        Facade.Element.Loaded += OnLoaded;
        Facade.Element.Unloaded += OnUnloaded;
    }

    private void OnPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        AspectStateSet current = AspectStateSet.FromElement(Facade.Element);
        if (current.Equals(observedStates))
        {
            return;
        }

        observedStates = current;
        EvaluateAll(force: false);
    }

    private void OnLoaded(UiElementId sender, RoutedEventArgs args)
    {
        observedStates = AspectStateSet.FromElement(Facade.Element);
        EvaluateAll(force: true);
    }

    private void OnUnloaded(UiElementId sender, RoutedEventArgs args)
    {
        foreach (StateProperty stateProperty in properties.Values)
        {
            stateProperty.StopWaiting();
        }
    }

    private void EvaluateAll(bool force)
    {
        if (Facade.Element.Root is null && Facade.Element is not UIRoot)
        {
            return;
        }

        foreach (StateProperty stateProperty in properties.Values)
        {
            stateProperty.Evaluate(observedStates, force);
        }
    }

    private abstract class StateProperty : MotionNode
    {
        public abstract void Evaluate(AspectStateSet states, bool force);

        public abstract void StopWaiting();
    }

    private sealed class StateProperty<T> : StateProperty
    {
        private readonly MotionElementFacade facade;
        private readonly UiProperty<T> property;
        private readonly T baseline;
        private readonly List<StateTarget> targets = [];
        private T resolvedTarget;
        private MotionSpec<T>? lastSpec;
        private MotionPropertyBinding<T>? pendingBinding;
        private MotionSystem? pendingMotion;

        public StateProperty(MotionElementFacade facade, UiProperty<T> property, T baseline)
        {
            this.facade = facade;
            this.property = property;
            this.baseline = baseline;
            resolvedTarget = baseline;
        }

        public void Set(AspectState state, T value, MotionSpec<T> spec)
        {
            int index = targets.FindIndex(target => target.State.Equals(state));
            StateTarget target = new(state, value, spec);
            if (index >= 0)
            {
                targets[index] = target;
            }
            else
            {
                targets.Add(target);
            }
        }

        public override void Evaluate(AspectStateSet states, bool force)
        {
            StateTarget? winner = null;
            for (int index = targets.Count - 1; index >= 0; index--)
            {
                if (states.Contains(targets[index].State))
                {
                    winner = targets[index];
                    break;
                }
            }

            T target = winner is null ? baseline : winner.Value;
            MotionSpec<T>? spec = winner is null ? lastSpec : winner.Spec;
            if (spec is null ||
                (!force && pendingBinding is null &&
                    property.Metadata.EqualityComparer.Equals(resolvedTarget, target)))
            {
                return;
            }

            MotionSystem motion = facade.ResolveMotion();
            MotionPropertyBinding<T> binding = motion.Properties.GetOrCreateBinding(
                motion,
                facade.Element,
                property);
            StopWaiting();
            MotionHandle handle = binding.AnimateTo(
                target,
                spec,
                new MotionPropertyStartOptions
                {
                    HoldOnComplete = true,
                    Priority = MotionPriority.Interactive,
                    RetargetMode = RetargetMode.Restart
                });
            if (handle.IsCanceled)
            {
                if (!binding.Value.CanStart(MotionPriority.Interactive))
                {
                    // Retain the logical state's spec even if it leaves before the
                    // blocker ends, so the pending baseline can still be animated.
                    lastSpec = spec;
                    pendingBinding = binding;
                    pendingMotion = motion;
                    motion.Graph.Register(this);
                }

                return;
            }

            resolvedTarget = target;
            lastSpec = spec;
        }

        protected internal override MotionNodeTickResult Tick(MotionFrame frame)
        {
            UIElement element = facade.Element;
            if (!ReferenceEquals(element.Root ?? element as UIRoot, pendingMotion?.Root) ||
                !UIElementVisibility.IsEffectivelyVisible(element))
            {
                StopWaiting();
                return new MotionNodeTickResult(Completed: true);
            }

            if (pendingBinding?.Value.CanStart(MotionPriority.Interactive) == true)
            {
                // Resolve the current state, not the target that was first rejected.
                // Waiting in the graph avoids starting inside a replacement/cancel
                // callback and also observes disposal, which suppresses Completed.
                Evaluate(AspectStateSet.FromElement(element), force: true);
            }

            return new MotionNodeTickResult(Completed: pendingBinding is null);
        }

        public override void StopWaiting()
        {
            pendingMotion?.Graph.Unregister(this);
            pendingBinding = null;
            pendingMotion = null;
        }

        private sealed record StateTarget(AspectState State, T Value, MotionSpec<T> Spec);
    }
}
