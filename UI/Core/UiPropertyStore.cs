namespace Cerneala.UI.Core;

public sealed class UiPropertyStore
{
    private static readonly UiPropertyValueSource[] EffectiveOrder =
    [
        UiPropertyValueSource.Local,
        UiPropertyValueSource.Animation,
        UiPropertyValueSource.AspectVisualState,
        UiPropertyValueSource.AspectBase,
        UiPropertyValueSource.TemplateBinding,
        UiPropertyValueSource.Inherited
    ];

    private readonly Dictionary<UiProperty, Dictionary<UiPropertyValueSource, object?>> values = new();

    public object? GetValue(UiProperty property)
    {
        return GetEffectiveValue(property).Value;
    }

    public UiPropertyValueSource GetValueSource(UiProperty property)
    {
        return GetEffectiveValue(property).Source;
    }

    public object? GetSourceValue(UiProperty property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        ValidateSource(source);
        return values.TryGetValue(property, out Dictionary<UiPropertyValueSource, object?>? propertyValues) &&
            propertyValues.TryGetValue(source, out object? value)
            ? value
            : null;
    }

    public void SetValue(UiProperty property, UiPropertyValueSource source, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        ValidateSource(source);

        if (!values.TryGetValue(property, out Dictionary<UiPropertyValueSource, object?>? propertyValues))
        {
            propertyValues = new Dictionary<UiPropertyValueSource, object?>();
            values.Add(property, propertyValues);
        }

        propertyValues[source] = value;
    }

    public void ClearValue(UiProperty property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        ValidateSource(source);

        if (!values.TryGetValue(property, out Dictionary<UiPropertyValueSource, object?>? propertyValues))
        {
            return;
        }

        propertyValues.Remove(source);
        if (propertyValues.Count == 0)
        {
            values.Remove(property);
        }
    }

    private (object? Value, UiPropertyValueSource Source) GetEffectiveValue(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (values.TryGetValue(property, out Dictionary<UiPropertyValueSource, object?>? propertyValues))
        {
            foreach (UiPropertyValueSource source in EffectiveOrder)
            {
                if (propertyValues.TryGetValue(source, out object? value))
                {
                    return (value, source);
                }
            }
        }

        return (property.DefaultValueUntyped, UiPropertyValueSource.Default);
    }

    private static void ValidateSource(UiPropertyValueSource source)
    {
        if (!Enum.IsDefined(source) || source == UiPropertyValueSource.Default)
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Only concrete non-default value sources can be stored.");
        }
    }
}
