using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

internal interface IElementAspectConsumer
{
    void ValidateAspectValue(UiProperty property);

    void ApplyAspectValue(UiProperty property, object? value);
}

public sealed class ElementAspect
{
    private readonly List<ElementAspectValue> defaultValues;
    private readonly List<WeakReference<IElementAspectConsumer>> consumers = [];

    public ElementAspect(IReadOnlyList<ElementAspectValue> defaultValues, bool isConditional = false)
    {
        this.defaultValues = defaultValues?.ToList() ?? throw new ArgumentNullException(nameof(defaultValues));
        if (this.defaultValues.Select(value => value.Property).Distinct(ReferenceEqualityComparer.Instance).Count() != this.defaultValues.Count)
        {
            throw new ArgumentException("A local aspect cannot assign the same UI property more than once.", nameof(defaultValues));
        }

        DefaultValues = this.defaultValues.AsReadOnly();
        IsConditional = isConditional;
    }

    public IReadOnlyList<ElementAspectValue> DefaultValues { get; }

    public bool IsConditional { get; }

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

        foreach (WeakReference<IElementAspectConsumer> reference in consumers)
        {
            if (reference.TryGetTarget(out IElementAspectConsumer? consumer))
            {
                consumer.ApplyAspectValue(property, value);
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
}

public sealed class ElementAspectValue
{
    public ElementAspectValue(UiProperty property, object? value)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value;
    }

    public UiProperty Property { get; }

    public object? Value { get; }
}
