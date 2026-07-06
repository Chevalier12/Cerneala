namespace Cerneala.UI.Core;

public class UiObject
{
    private readonly UiPropertyStore propertyStore = new();

    public event EventHandler<UiPropertyChangedEventArgs>? PropertyChanged;

    public T GetValue<T>(UiProperty<T> property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return (T)propertyStore.GetValue(property)!;
    }

    public object? GetValue(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return propertyStore.GetValue(property);
    }

    public UiPropertyValueSource GetValueSource(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return propertyStore.GetValueSource(property);
    }

    public T SetValue<T>(UiProperty<T> property, T value)
    {
        return SetValue(property, value, UiPropertyValueSource.Local);
    }

    public T SetValue<T>(UiProperty<T> property, T value, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        return SetValueCore(property, value, source);
    }

    public T SetValue<T>(UiPropertyKey<T> key, T value)
    {
        return SetValue(key, value, UiPropertyValueSource.Local);
    }

    public T SetValue<T>(UiPropertyKey<T> key, T value, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(key);
        return SetValueCore(key.Property, value, source);
    }

    public T ClearValue<T>(UiProperty<T> property)
    {
        return ClearValue(property, UiPropertyValueSource.Local);
    }

    public T ClearValue<T>(UiProperty<T> property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        T oldValue = GetValue(property);
        propertyStore.ClearValue(property, source);
        T newValue = GetValue(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(property, oldValue, newValue, GetValueSource(property));
        }

        return oldValue;
    }

    internal object? SetValueUntyped(UiProperty property, object? value, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        object? oldValue = GetValue(property);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);
        propertyStore.SetValue(property, source, coerced);
        object? newValue = GetValue(property);
        if (!property.AreEqualUntyped(oldValue, newValue))
        {
            NotifyPropertyChangedUntyped(property, oldValue, newValue, GetValueSource(property));
        }

        return oldValue;
    }

    internal object? ClearValueUntyped(UiProperty property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        object? oldValue = GetValue(property);
        propertyStore.ClearValue(property, source);
        object? newValue = GetValue(property);
        if (!property.AreEqualUntyped(oldValue, newValue))
        {
            NotifyPropertyChangedUntyped(property, oldValue, newValue, GetValueSource(property));
        }

        return oldValue;
    }

    protected virtual void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    private T SetValueCore<T>(UiProperty<T> property, T value, UiPropertyValueSource source)
    {
        T oldValue = GetValue(property);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);

        propertyStore.SetValue(property, source, coerced);
        T newValue = GetValue(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(property, oldValue, newValue, GetValueSource(property));
        }

        return oldValue;
    }

    private void NotifyPropertyChanged<T>(
        UiProperty<T> property,
        T oldValue,
        T newValue,
        UiPropertyValueSource valueSource)
    {
        UiPropertyChangedEventArgs<T> args = new(this, property, oldValue, newValue, valueSource);
        OnPropertyChanged(args);

        UiPropertyOptions invalidationOptions = property.Options & (
            UiPropertyOptions.AffectsMeasure |
            UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender |
            UiPropertyOptions.AffectsHitTest |
            UiPropertyOptions.AffectsStyle |
            UiPropertyOptions.AffectsInputVisual |
            UiPropertyOptions.AffectsSemantics |
            UiPropertyOptions.Inherits);
        if (invalidationOptions != UiPropertyOptions.None && this is IUiPropertyOwner owner)
        {
            owner.OnPropertyInvalidated(args, invalidationOptions);
        }
    }

    private void NotifyPropertyChangedUntyped(
        UiProperty property,
        object? oldValue,
        object? newValue,
        UiPropertyValueSource valueSource)
    {
        UiPropertyChangedEventArgs args = new(this, property, oldValue, newValue, valueSource);
        OnPropertyChanged(args);

        UiPropertyOptions invalidationOptions = property.Options & (
            UiPropertyOptions.AffectsMeasure |
            UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender |
            UiPropertyOptions.AffectsHitTest |
            UiPropertyOptions.AffectsStyle |
            UiPropertyOptions.AffectsInputVisual |
            UiPropertyOptions.AffectsSemantics |
            UiPropertyOptions.Inherits);
        if (invalidationOptions != UiPropertyOptions.None && this is IUiPropertyOwner owner)
        {
            owner.OnPropertyInvalidated(args, invalidationOptions);
        }
    }
}
