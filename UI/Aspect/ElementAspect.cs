using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

internal interface IElementAspectConsumer
{
    void ValidateAspectValue(UiProperty property);

    void InvalidateAspect();
}

public sealed class ElementAspect
{
    private readonly List<ElementAspectValue> defaultValues;
    private readonly IReadOnlyList<ElementAspectCondition> conditions;
    private readonly List<WeakReference<IElementAspectConsumer>> consumers = [];
    private readonly Func<UIElement, IDisposable?>? behaviorFactory;
    private AspectPackage package;

    public ElementAspect(IReadOnlyList<ElementAspectValue> defaultValues, bool isConditional = false)
        : this(name: null, typeof(UIElement), defaultValues, isConditional)
    {
    }

    public ElementAspect(
        string? name,
        Type targetType,
        IReadOnlyList<ElementAspectValue> defaultValues,
        bool isConditional = false)
        : this(name, targetType, defaultValues, [], behaviorFactory: null, isConditional: isConditional)
    {
    }

    public ElementAspect(
        string? name,
        Type targetType,
        IReadOnlyList<ElementAspectValue> defaultValues,
        IReadOnlyList<ElementAspectCondition> conditions,
        Func<UIElement, IDisposable?>? behaviorFactory = null,
        bool isConditional = false,
        AspectOrigin? origin = null)
    {
        if (targetType is null || !typeof(UIElement).IsAssignableFrom(targetType))
        {
            throw new ArgumentException("Element aspect target type must derive from UIElement.", nameof(targetType));
        }

        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        Origin = origin ?? AspectOrigin.Code(Name);
        TargetType = targetType;
        this.defaultValues = defaultValues?.ToList() ?? throw new ArgumentNullException(nameof(defaultValues));
        if (this.defaultValues.Select(value => value.Property).Distinct(ReferenceEqualityComparer.Instance).Count() != this.defaultValues.Count)
        {
            throw new ArgumentException("A local aspect cannot assign the same UI property more than once.", nameof(defaultValues));
        }

        DefaultValues = this.defaultValues.AsReadOnly();
        this.conditions = Array.AsReadOnly((conditions ?? throw new ArgumentNullException(nameof(conditions))).Select(
            condition => condition ?? throw new ArgumentException("Element aspect conditions cannot contain null.", nameof(conditions))).ToArray());
        Conditions = this.conditions;
        ConditionKeys = Array.AsReadOnly(this.conditions.Select(condition => condition.Key).ToArray());
        this.behaviorFactory = behaviorFactory;
        IsConditional = isConditional || this.conditions.Count > 0;
        package = CreatePackage();
    }

    public string? Name { get; }

    public Type TargetType { get; }

    public AspectOrigin Origin { get; }

    public IReadOnlyList<ElementAspectValue> DefaultValues { get; }

    public bool IsConditional { get; }

    public IReadOnlyList<ElementAspectCondition> Conditions { get; }

    public IReadOnlyList<AspectConditionKey> ConditionKeys { get; }

    internal int Version { get; private set; }

    internal AspectPackage Package => package;

    internal IDisposable? AttachBehavior(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return behaviorFactory?.Invoke(element);
    }

    public bool SetValue(UiProperty property, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        property.ValidateUntyped(value);

        int assignmentIndex = -1;
        for (int index = 0; index < defaultValues.Count; index++)
        {
            ElementAspectValue current = defaultValues[index];
            if (!ReferenceEquals(current.Property, property))
            {
                continue;
            }

            if (property.AreEqualUntyped(current.Value, value))
            {
                return false;
            }

            assignmentIndex = index;
            break;
        }

        RemoveDeadConsumers();
        foreach (WeakReference<IElementAspectConsumer> reference in consumers)
        {
            if (reference.TryGetTarget(out IElementAspectConsumer? consumer))
            {
                consumer.ValidateAspectValue(property);
            }
        }

        ElementAspectValue assignment = new(property, value);
        if (assignmentIndex >= 0)
        {
            defaultValues[assignmentIndex] = assignment;
        }
        else
        {
            defaultValues.Add(assignment);
        }

        Version++;
        package = CreatePackage();
        foreach (WeakReference<IElementAspectConsumer> reference in consumers)
        {
            if (reference.TryGetTarget(out IElementAspectConsumer? consumer))
            {
                consumer.InvalidateAspect();
            }
        }

        return true;
    }

    internal void Attach(IElementAspectConsumer consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        RemoveDeadConsumers();
        if (consumers.Any(reference =>
            reference.TryGetTarget(out IElementAspectConsumer? existing) &&
            ReferenceEquals(existing, consumer)))
        {
            return;
        }

        consumers.Add(new WeakReference<IElementAspectConsumer>(consumer));
    }

    internal void Detach(IElementAspectConsumer consumer)
    {
        for (int index = consumers.Count - 1; index >= 0; index--)
        {
            if (!consumers[index].TryGetTarget(out IElementAspectConsumer? existing) ||
                ReferenceEquals(existing, consumer))
            {
                consumers.RemoveAt(index);
            }
        }
    }

    private void RemoveDeadConsumers()
    {
        for (int index = consumers.Count - 1; index >= 0; index--)
        {
            if (!consumers[index].TryGetTarget(out _))
            {
                consumers.RemoveAt(index);
            }
        }
    }

    private AspectPackage CreatePackage()
    {
        AspectPackageBuilder builder = AspectPackage.Create(Name ?? "ElementAspect").Origin(Origin);
        if (defaultValues.Count == 0 && conditions.Count == 0)
        {
            return builder;
        }

        return builder.Components(components =>
        {
            if (defaultValues.Count > 0)
            {
                components.AddRule(new AspectRuleSet(
                    (Name ?? "ElementAspect") + ".default",
                    AspectLayer.Runtime,
                    new AspectTarget(TargetType),
                    CreateDeclarations(defaultValues),
                    declarationOrder: 0));
            }

            foreach (ElementAspectCondition condition in conditions)
            {
                components.AddRule(new AspectRuleSet(
                    (Name ?? "ElementAspect") + ".condition." + condition.Order,
                    AspectLayer.Runtime,
                    new AspectTarget(
                        TargetType,
                        conditions: [AspectCondition.Signal(condition.Key)]),
                    CreateDeclarations(condition.Values),
                    declarationOrder: condition.Order + 1));
            }
        });
    }

    private static AspectDeclaration[] CreateDeclarations(IReadOnlyList<ElementAspectValue> values) =>
        values.Select(value => new AspectDeclaration(
            value.Property,
            value.AspectValue ?? new ElementAspectLiteralValue(value.Property.ValueType, value.Value),
            diagnosticName: value.Property.DiagnosticName)).ToArray();

    private sealed class ElementAspectLiteralValue(Type valueType, object? value) : AspectValue
    {
        public override Type ValueType { get; } = valueType;

        public override IReadOnlyList<AspectToken> Dependencies { get; } = [];

        public override object? Resolve(AspectResolutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return value;
        }
    }
}

public sealed class ElementAspectCondition
{
    public ElementAspectCondition(
        AspectConditionKey key,
        IReadOnlyList<ElementAspectValue> values,
        int order)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(values);
        ElementAspectValue[] snapshot = values.Select(
            value => value ?? throw new ArgumentException("Conditional aspect values cannot contain null.", nameof(values))).ToArray();
        if (snapshot.Select(value => value.Property).Distinct(ReferenceEqualityComparer.Instance).Count() != snapshot.Length)
        {
            throw new ArgumentException("An aspect condition cannot assign the same UI property more than once.", nameof(values));
        }

        Values = Array.AsReadOnly(snapshot);
        Order = order;
    }

    public AspectConditionKey Key { get; }

    public IReadOnlyList<ElementAspectValue> Values { get; }

    public int Order { get; }
}

public sealed class ElementAspectValue
{
    public ElementAspectValue(UiProperty property, object? value)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Property.ValidateUntyped(value);
        Value = value;
    }

    public ElementAspectValue(UiProperty property, AspectValue value)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        AspectValue = value ?? throw new ArgumentNullException(nameof(value));
        if (Property.ValueType != AspectValue.ValueType)
        {
            throw new ArgumentException("Element aspect value type must match the UI property value type.", nameof(value));
        }
    }

    public UiProperty Property { get; }

    public object? Value { get; }

    public AspectValue? DynamicValue => AspectValue;

    internal AspectValue? AspectValue { get; }
}
