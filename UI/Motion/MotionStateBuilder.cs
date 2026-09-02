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

    private abstract class StateProperty
    {
        public abstract void Evaluate(AspectStateSet states, bool force);
    }

    private sealed class StateProperty<T> : StateProperty
    {
        private readonly MotionElementFacade facade;
        private readonly UiProperty<T> property;
        private readonly T baseline;
        private readonly List<StateTarget> targets = [];
        private T resolvedTarget;
        private MotionSpec<T>? lastSpec;

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
                (!force && property.Metadata.EqualityComparer.Equals(resolvedTarget, target)))
            {
                return;
            }

            MotionSystem motion = facade.ResolveMotion();
            MotionPropertyBinding<T> binding = motion.Properties.GetOrCreateBinding(
                motion,
                facade.Element,
                property);
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
                return;
            }

            resolvedTarget = target;
            lastSpec = spec;
        }

        private sealed record StateTarget(AspectState State, T Value, MotionSpec<T> Spec);
    }
}
