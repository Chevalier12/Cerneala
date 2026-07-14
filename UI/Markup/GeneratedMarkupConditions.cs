using System.ComponentModel;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Markup;

public sealed class MarkupDataPathSegment
{
    public MarkupDataPathSegment(string propertyName, Func<object?, object?> getter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PropertyName = propertyName;
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    public MarkupDataPathSegment(
        string propertyName,
        Func<object?, object?> getter,
        Action<object?, object?> setter)
        : this(propertyName, getter)
    {
        Setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public string PropertyName { get; }

    internal Func<object?, object?> Getter { get; }

    internal Action<object?, object?>? Setter { get; }
}

public sealed class MarkupConditionalValue
{
    public MarkupConditionalValue(UiObject target, UiProperty property, object? value, UiPropertyValueSource source)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value;
        Source = source;
    }

    internal MarkupConditionalValue(
        UiObject target,
        UiProperty property,
        IMarkupConditionalValueProvider provider)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Source = UiPropertyValueSource.MarkupConditional;
    }

    internal UiObject Target { get; }

    internal UiProperty Property { get; }

    internal object? Value { get; }

    internal UiPropertyValueSource Source { get; }

    internal IMarkupConditionalValueProvider? Provider { get; }
}

public sealed class MarkupConditionalContent
{
    private readonly Func<IReadOnlyList<UIElement>> factory;
    private readonly Action? activated;
    private readonly Action? deactivated;
    private IReadOnlyList<UIElement>? cached;

    public MarkupConditionalContent(int order, Func<IReadOnlyList<UIElement>> factory)
        : this(order, factory, null, null)
    {
    }

    public MarkupConditionalContent(
        int order,
        Func<IReadOnlyList<UIElement>> factory,
        Action? activated,
        Action? deactivated)
    {
        Order = order;
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.activated = activated;
        this.deactivated = deactivated;
    }

    public int Order { get; }

    internal IReadOnlyList<UIElement> GetChildren()
    {
        return cached ??= factory() ?? throw new InvalidOperationException("Conditional markup factory returned null.");
    }

    internal void Activate()
    {
        activated?.Invoke();
    }

    internal void Deactivate()
    {
        deactivated?.Invoke();
    }
}

public sealed class MarkupConditionRule
{
    public MarkupConditionRule(
        int order,
        Func<bool> predicate,
        IReadOnlyList<MarkupConditionalValue>? values = null,
        MarkupConditionalContent? content = null)
    {
        Order = order;
        Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Values = values ?? Array.Empty<MarkupConditionalValue>();
        Content = content;
    }

    public int Order { get; }

    internal Func<bool> Predicate { get; }

    internal IReadOnlyList<MarkupConditionalValue> Values { get; }

    internal MarkupConditionalContent? Content { get; }
}

public abstract class MarkupObservation
{
    internal event EventHandler? Changed;

    public object? Value { get; protected set; }

    internal bool IsResolved { get; private set; }

    internal virtual bool IsWritable => false;

    internal virtual bool CanWrite => IsResolved && IsWritable;

    internal Func<bool>? CallbackGuard { get; set; }

    internal abstract void Start();

    internal abstract void Stop();

    internal abstract void RefreshValue();

    internal virtual bool TryWrite(object? value)
    {
        return false;
    }

    protected void SetResolvedValue(object? value)
    {
        Value = value;
        IsResolved = true;
    }

    protected void SetUnresolved()
    {
        Value = null;
        IsResolved = false;
    }

    protected bool ShouldProcessCallback()
    {
        return CallbackGuard?.Invoke() ?? true;
    }

    protected void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

public static partial class GeneratedMarkup
{
    public static MarkupObservation ObserveProperty(UiObject source, UiProperty property)
    {
        return new UiPropertyObservation(source, property);
    }

    public static MarkupObservation ObserveTemplatePartProperty(Control owner, string partName, UiProperty property)
    {
        return new TemplatePartPropertyObservation(owner, partName, property);
    }

    public static MarkupObservation ObserveObject(Func<object?> getter)
    {
        return new ObjectObservation(getter);
    }

    public static MarkupObservation ObserveDataPath(UIElement owner, params MarkupDataPathSegment[] segments)
    {
        return new DataPathObservation(owner, segments);
    }

    public static IDisposable AttachConditions(
        UIElement owner,
        IReadOnlyList<MarkupObservation> observations,
        IReadOnlyList<MarkupConditionRule> rules)
    {
        MarkupConditionController controller = new(owner, observations, rules);
        owner.AddLifecycleBehavior(controller);
        return controller;
    }

    private sealed class UiPropertyObservation : MarkupObservation
    {
        private readonly UiObject source;
        private readonly UiProperty property;
        private bool started;

        public UiPropertyObservation(UiObject source, UiProperty property)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.property = property ?? throw new ArgumentNullException(nameof(property));
            SetResolvedValue(source.GetValue(property));
        }

        internal override bool IsWritable => !property.IsReadOnly;

        internal override void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            SetResolvedValue(source.GetValue(property));
            source.PropertyChanged += OnPropertyChanged;
        }

        internal override void Stop()
        {
            if (!started)
            {
                return;
            }

            started = false;
            source.PropertyChanged -= OnPropertyChanged;
        }

        internal override void RefreshValue()
        {
            SetResolvedValue(source.GetValue(property));
        }

        internal override bool TryWrite(object? value)
        {
            if (!CanWrite)
            {
                return false;
            }

            source.SetValueUntyped(property, value, UiPropertyValueSource.Local);
            return true;
        }

        private void OnPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Property, property))
            {
                return;
            }

            if (!ShouldProcessCallback())
            {
                return;
            }

            SetResolvedValue(args.NewValue);
            RaiseChanged();
        }
    }

    private sealed class TemplatePartPropertyObservation : MarkupObservation
    {
        private readonly Control owner;
        private readonly string partName;
        private readonly UiProperty property;
        private UiObject? part;
        private bool started;

        public TemplatePartPropertyObservation(Control owner, string partName, UiProperty property)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.partName = string.IsNullOrWhiteSpace(partName) ? throw new ArgumentException("A template part name is required.", nameof(partName)) : partName;
            this.property = property ?? throw new ArgumentNullException(nameof(property));
            Reconnect();
        }

        internal override bool IsWritable => !property.IsReadOnly;

        internal override bool CanWrite => base.CanWrite && part is not null;

        internal override void Start()
        {
            if (started) return;
            started = true;
            owner.PropertyChanged += OnOwnerPropertyChanged;
            Reconnect();
        }

        internal override void Stop()
        {
            if (!started) return;
            started = false;
            owner.PropertyChanged -= OnOwnerPropertyChanged;
            DisconnectPart();
        }

        internal override void RefreshValue()
        {
            Reconnect();
        }

        internal override bool TryWrite(object? value)
        {
            if (!CanWrite)
            {
                return false;
            }

            part!.SetValueUntyped(property, value, UiPropertyValueSource.Local);
            return true;
        }

        private void OnOwnerPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Property, Control.ComponentTemplateProperty) || !ShouldProcessCallback())
            {
                return;
            }

            ReconnectAndRaise();
        }

        private void OnPartPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Property, property) || !ShouldProcessCallback()) return;
            SetResolvedValue(args.NewValue);
            RaiseChanged();
        }

        private void ReconnectAndRaise()
        {
            Reconnect();
            RaiseChanged();
        }

        private void Reconnect()
        {
            DisconnectPart();
            owner.ApplyTemplate();
            if (owner.ComponentTemplateInstance is null || !owner.ComponentTemplateInstance.Parts.TryGetValue(partName, out UIElement? resolved))
            {
                SetUnresolved();
                return;
            }

            part = resolved!;
            SetResolvedValue(part.GetValue(property));
            if (started) part.PropertyChanged += OnPartPropertyChanged;
        }

        private void DisconnectPart()
        {
            if (part is not null) part.PropertyChanged -= OnPartPropertyChanged;
            part = null;
        }
    }

    private sealed class ObjectObservation : MarkupObservation
    {
        private readonly Func<object?> getter;
        private Action? unsubscribe;
        private bool started;

        public ObjectObservation(Func<object?> getter)
        {
            this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
            SetResolvedValue(getter());
        }

        internal override void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            SetResolvedValue(getter());
            unsubscribe = Subscribe(Value, null, Refresh);
        }

        internal override void Stop()
        {
            started = false;
            unsubscribe?.Invoke();
            unsubscribe = null;
        }

        internal override void RefreshValue()
        {
            SetResolvedValue(getter());
        }

        private void Refresh()
        {
            if (!ShouldProcessCallback())
            {
                return;
            }

            SetResolvedValue(getter());
            RaiseChanged();
        }
    }

    private sealed class DataPathObservation : MarkupObservation
    {
        private readonly UIElement owner;
        private readonly IReadOnlyList<MarkupDataPathSegment> segments;
        private readonly List<Action> unsubscribeContext = [];
        private readonly List<Action> unsubscribePath = [];
        private object? terminalOwner;
        private MarkupDataPathSegment? terminalSegment;
        private bool started;

        public DataPathObservation(UIElement owner, IReadOnlyList<MarkupDataPathSegment> segments)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.segments = segments?.ToArray() ?? throw new ArgumentNullException(nameof(segments));
            Rebuild(raiseChanged: false);
        }

        internal override void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            Rebuild(raiseChanged: false);
        }

        internal override void Stop()
        {
            if (!started)
            {
                return;
            }

            started = false;
            ClearContextSubscriptions();
            ClearPathSubscriptions();
            terminalOwner = null;
            terminalSegment = null;
        }

        internal override void RefreshValue()
        {
            Rebuild(raiseChanged: false);
        }

        internal override bool IsWritable => segments.Count > 0 && segments[^1].Setter is not null;

        internal override bool CanWrite => base.CanWrite && terminalOwner is not null;

        internal override bool TryWrite(object? value)
        {
            if (!CanWrite)
            {
                return false;
            }

            terminalSegment!.Setter!(terminalOwner, value);
            return true;
        }

        private void OnContextPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (ReferenceEquals(args.Property, UIElement.DataContextProperty) && ShouldProcessCallback())
            {
                Rebuild(raiseChanged: true);
            }
        }

        private void OnPathPropertyChanged()
        {
            if (ShouldProcessCallback())
            {
                Rebuild(raiseChanged: true);
            }
        }

        private void Rebuild(bool raiseChanged)
        {
            ClearContextSubscriptions();
            ClearPathSubscriptions();
            terminalOwner = null;
            terminalSegment = null;
            object? current = ResolveDataContext();
            bool resolved = current is not null;
            for (int index = 0; index < segments.Count; index++)
            {
                if (current is null)
                {
                    resolved = false;
                    break;
                }

                MarkupDataPathSegment segment = segments[index];
                if (started)
                {
                    Action? unsubscribe = Subscribe(current, segment.PropertyName, OnPathPropertyChanged);
                    if (unsubscribe is not null)
                    {
                        unsubscribePath.Add(unsubscribe);
                    }
                }

                bool terminal = index == segments.Count - 1;
                if (terminal)
                {
                    terminalOwner = current;
                    terminalSegment = segment;
                }

                current = segment.Getter(current);
            }

            if (resolved)
            {
                SetResolvedValue(current);
            }
            else
            {
                SetUnresolved();
            }

            if (raiseChanged)
            {
                RaiseChanged();
            }
        }

        private object? ResolveDataContext()
        {
            UIElement? current = owner;
            object? inherited = owner.DataContext;
            HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
            while (current is not null && visited.Add(current))
            {
                UIElement observed = current;
                if (started)
                {
                    observed.PropertyChanged += OnContextPropertyChanged;
                    unsubscribeContext.Add(() => observed.PropertyChanged -= OnContextPropertyChanged);
                }

                UiPropertyValueSource source = observed.GetValueSource(UIElement.DataContextProperty);
                if (source is not UiPropertyValueSource.Default and not UiPropertyValueSource.Inherited)
                {
                    return observed.DataContext;
                }

                if (source == UiPropertyValueSource.Inherited)
                {
                    inherited = observed.DataContext;
                }

                current = observed.LogicalParent ?? observed.VisualParent;
            }

            return inherited;
        }

        private void ClearContextSubscriptions()
        {
            foreach (Action unsubscribe in unsubscribeContext)
            {
                unsubscribe();
            }

            unsubscribeContext.Clear();
        }

        private void ClearPathSubscriptions()
        {
            foreach (Action unsubscribe in unsubscribePath)
            {
                unsubscribe();
            }

            unsubscribePath.Clear();
        }
    }

    private static Action? Subscribe(object? source, string? propertyName, Action changed)
    {
        if (source is UiObject uiObject)
        {
            EventHandler<UiPropertyChangedEventArgs> handler = (_, args) =>
            {
                if (propertyName is null || string.Equals(args.Property.Name, propertyName, StringComparison.Ordinal))
                {
                    changed();
                }
            };
            uiObject.PropertyChanged += handler;
            return () => uiObject.PropertyChanged -= handler;
        }

        if (source is INotifyPropertyChanged notify)
        {
            PropertyChangedEventHandler handler = (_, args) =>
            {
                if (propertyName is null || string.IsNullOrEmpty(args.PropertyName) || string.Equals(args.PropertyName, propertyName, StringComparison.Ordinal))
                {
                    changed();
                }
            };
            notify.PropertyChanged += handler;
            return () => notify.PropertyChanged -= handler;
        }

        return null;
    }
}

internal sealed class MarkupConditionController : IElementLifecycleBehavior, IDisposable
{
    private readonly UIElement owner;
    private readonly IReadOnlyList<MarkupObservation> observations;
    private readonly IReadOnlyList<MarkupConditionRule> rules;
    private readonly List<MarkupConditionalValue> appliedValues = [];
    private readonly List<MarkupConditionalContent> activeContent = [];
    private bool started;
    private bool disposed;
    private bool evaluating;
    private bool reevaluate;
    private Func<bool>? callbackGuard;
    private readonly UiRelayRefreshDispatcher refreshDispatcher;

    public MarkupConditionController(
        UIElement owner,
        IReadOnlyList<MarkupObservation> observations,
        IReadOnlyList<MarkupConditionRule> rules)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.observations = observations?.ToArray() ?? throw new ArgumentNullException(nameof(observations));
        this.rules = rules?.OrderBy(rule => rule.Order).ToArray() ?? throw new ArgumentNullException(nameof(rules));
        refreshDispatcher = new UiRelayRefreshDispatcher(() => owner.Root?.Relay, RefreshFromRelay, "markup condition");
        Start();
    }

    public void Attach()
    {
        if (started)
        {
            Stop();
        }

        Start();
    }

    public void Detach()
    {
        Stop();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stop();
        ClearAppliedValues();
        foreach (IMarkupConditionalValueProvider provider in rules
            .SelectMany(rule => rule.Values)
            .Select(value => value.Provider)
            .Where(provider => provider is not null)
            .Cast<IMarkupConditionalValueProvider>()
            .Distinct())
        {
            provider.Dispose();
        }

        owner.RemoveLifecycleBehavior(this);
    }

    private void Start()
    {
        if (started || disposed)
        {
            return;
        }

        started = true;
        callbackGuard = refreshDispatcher.Activate();
        foreach (MarkupObservation observation in observations)
        {
            observation.CallbackGuard = callbackGuard;
            observation.Changed += OnObservationChanged;
            observation.Start();
        }

        Evaluate();
    }

    private void Stop()
    {
        if (!started)
        {
            return;
        }

        started = false;
        refreshDispatcher.Deactivate();
        foreach (MarkupObservation observation in observations)
        {
            observation.Changed -= OnObservationChanged;
            if (ReferenceEquals(observation.CallbackGuard, callbackGuard))
            {
                observation.CallbackGuard = null;
            }

            observation.Stop();
        }

        callbackGuard = null;

        foreach (MarkupConditionalValue value in appliedValues.Where(value => value.Provider is not null).ToArray())
        {
            value.Provider!.Deactivate();
            appliedValues.Remove(value);
        }

        foreach (MarkupConditionalContent content in activeContent)
        {
            content.Deactivate();
        }

        activeContent.Clear();
    }

    private void OnObservationChanged(object? sender, EventArgs args)
    {
        Evaluate();
    }

    private void RefreshFromRelay()
    {
        if (!started || disposed)
        {
            return;
        }

        foreach (MarkupObservation observation in observations)
        {
            observation.RefreshValue();
        }

        Evaluate();
    }

    private void Evaluate()
    {
        if (!started)
        {
            return;
        }

        if (evaluating)
        {
            reevaluate = true;
            return;
        }

        evaluating = true;
        try
        {
            do
            {
                reevaluate = false;
                IReadOnlyList<MarkupConditionRule> active = rules.Where(rule => rule.Predicate()).ToArray();
                ApplyValues(active);
                ApplyContent(active);
            }
            while (reevaluate);
        }
        finally
        {
            evaluating = false;
        }
    }

    private void ApplyValues(IReadOnlyList<MarkupConditionRule> active)
    {
        List<MarkupConditionalValue> desired = [];
        foreach (MarkupConditionRule rule in active)
        {
            foreach (MarkupConditionalValue value in rule.Values)
            {
                int existing = desired.FindIndex(candidate => SameSlot(candidate, value));
                if (existing >= 0)
                {
                    desired[existing] = value;
                }
                else
                {
                    desired.Add(value);
                }
            }
        }

        foreach (MarkupConditionalValue previous in appliedValues.ToArray())
        {
            MarkupConditionalValue? replacement = desired.FirstOrDefault(candidate => SameSlot(previous, candidate));
            if (replacement is null || !ReferenceEquals(previous, replacement))
            {
                previous.Provider?.Deactivate();
            }
        }

        foreach (MarkupConditionalValue value in desired)
        {
            if (value.Provider is not null)
            {
                if (!appliedValues.Contains(value))
                {
                    value.Provider.Activate(owner);
                }
            }
            else
            {
                value.Target.SetValueUntyped(value.Property, value.Value, value.Source);
            }
        }

        foreach (MarkupConditionalValue value in appliedValues.Where(old => !desired.Any(candidate => SameSlot(old, candidate))).ToArray())
        {
            value.Target.ClearValueUntyped(value.Property, value.Source);
        }

        appliedValues.Clear();
        appliedValues.AddRange(desired);
    }

    private void ApplyContent(IReadOnlyList<MarkupConditionRule> active)
    {
        MarkupConditionalContent[] content = active
            .Select(rule => rule.Content)
            .Where(item => item is not null)
            .Cast<MarkupConditionalContent>()
            .OrderBy(item => item.Order)
            .ToArray();
        if (content.Length == 0 && !rules.Any(rule => rule.Content is not null))
        {
            return;
        }

        UIElement[] desired = content.SelectMany(item => item.GetChildren()).ToArray();
        switch (owner)
        {
            case Border border:
                border.Child = desired.LastOrDefault();
                break;
            case Button button:
                if (desired.LastOrDefault() is UIElement buttonContent)
                {
                    button.SetValue(
                        ContentControl.ContentProperty,
                        (object?)buttonContent,
                        UiPropertyValueSource.MarkupConditional);
                }
                else
                {
                    button.ClearValue(ContentControl.ContentProperty, UiPropertyValueSource.MarkupConditional);
                }

                break;
            case global::Cerneala.UI.Layout.Panels.Panel:
                Reconcile(owner.LogicalChildren, desired);
                Reconcile(owner.VisualChildren, desired);
                break;
            default:
                throw new InvalidOperationException($"Element '{owner.GetType().Name}' does not accept conditional child content.");
        }

        foreach (MarkupConditionalContent previous in activeContent.Where(item => !content.Contains(item)).ToArray())
        {
            previous.Deactivate();
        }

        foreach (MarkupConditionalContent current in content.Where(item => !activeContent.Contains(item)))
        {
            current.Activate();
        }

        activeContent.Clear();
        activeContent.AddRange(content);
    }

    private static void Reconcile(UIElementCollection collection, IReadOnlyList<UIElement> desired)
    {
        foreach (UIElement child in collection.Where(child => !desired.Any(candidate => ReferenceEquals(candidate, child))).ToArray())
        {
            collection.Remove(child);
        }

        for (int index = 0; index < desired.Count; index++)
        {
            UIElement child = desired[index];
            int current = IndexOf(collection, child);
            if (current < 0)
            {
                collection.Insert(index, child);
            }
            else if (current != index)
            {
                collection.Move(current, index);
            }
        }
    }

    private static int IndexOf(UIElementCollection collection, UIElement child)
    {
        for (int index = 0; index < collection.Count; index++)
        {
            if (ReferenceEquals(collection[index], child))
            {
                return index;
            }
        }

        return -1;
    }

    private void ClearAppliedValues()
    {
        foreach (MarkupConditionalValue value in appliedValues)
        {
            if (value.Provider is not null)
            {
                value.Provider.Dispose();
            }
            else
            {
                value.Target.ClearValueUntyped(value.Property, value.Source);
            }
        }

        appliedValues.Clear();
    }

    private static bool SameSlot(MarkupConditionalValue left, MarkupConditionalValue right)
    {
        return ReferenceEquals(left.Target, right.Target) &&
            ReferenceEquals(left.Property, right.Property) &&
            left.Source == right.Source;
    }
}
