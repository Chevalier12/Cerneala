using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class ElementAspect
{
    public ElementAspect(IReadOnlyList<ElementAspectValue> defaultValues, bool isConditional = false)
    {
        DefaultValues = defaultValues?.ToArray() ?? throw new ArgumentNullException(nameof(defaultValues));
        if (DefaultValues.Select(value => value.Property).Distinct(ReferenceEqualityComparer.Instance).Count() != DefaultValues.Count)
        {
            throw new ArgumentException("A local aspect cannot assign the same UI property more than once.", nameof(defaultValues));
        }

        IsConditional = isConditional;
    }

    public IReadOnlyList<ElementAspectValue> DefaultValues { get; }

    public bool IsConditional { get; }
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
