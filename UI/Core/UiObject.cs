namespace Cerneala.UI.Core;

public class UiObject
{
    private readonly UiPropertyStore propertyStore = new();

    public event EventHandler<UiPropertyChangedEventArgs>? PropertyChanged;

    protected virtual UiPropertyMutationObserver? MutationObserver => null;

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

    public object? GetSourceValue(UiProperty property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        return propertyStore.GetSourceValue(property, source);
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
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        propertyStore.ClearValue(property, source);
        T newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, null, wasCoerced: false);
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, null, wasCoerced: false);
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
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);
        propertyStore.SetValue(property, source, coerced);
        object? newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.AreEqualUntyped(oldValue, newValue))
        {
            NotifyPropertyChangedUntyped(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, coerced, !Equals(value, coerced));
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, coerced, !Equals(value, coerced));
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
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        propertyStore.ClearValue(property, source);
        object? newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.AreEqualUntyped(oldValue, newValue))
        {
            NotifyPropertyChangedUntyped(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, null, wasCoerced: false);
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, null, wasCoerced: false);
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
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);

        propertyStore.SetValue(property, source, coerced);
        T newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, coerced, !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, coerced, !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
        }

        return oldValue;
    }

    private void NotifyPropertyChanged<T>(
        UiProperty<T> property,
        T oldValue,
        T newValue,
        UiPropertyValueSource valueSource,
        UiPropertyValueSource mutatingSource,
        UiPropertyValueSource oldEffectiveSource,
        object? oldSourceValue,
        object? newSourceValue,
        bool wasCoerced)
    {
        UiPropertyChangedEventArgs<T> args = new(this, property, oldValue, newValue, valueSource);
        OnPropertyChanged(args);
        NotifyPropertyMutated(property, mutatingSource, oldValue, oldEffectiveSource, newValue, valueSource, oldSourceValue, newSourceValue, wasCoerced);

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
        UiPropertyValueSource valueSource,
        UiPropertyValueSource mutatingSource,
        UiPropertyValueSource oldEffectiveSource,
        object? oldSourceValue,
        object? newSourceValue,
        bool wasCoerced)
    {
        UiPropertyChangedEventArgs args = new(this, property, oldValue, newValue, valueSource);
        OnPropertyChanged(args);
        NotifyPropertyMutated(property, mutatingSource, oldValue, oldEffectiveSource, newValue, valueSource, oldSourceValue, newSourceValue, wasCoerced);

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

    private void NotifyPropertyMutated(
        UiProperty property,
        UiPropertyValueSource mutatingSource,
        object? oldValue,
        UiPropertyValueSource oldEffectiveSource,
        object? newValue,
        UiPropertyValueSource valueSource,
        object? oldSourceValue,
        object? newSourceValue,
        bool wasCoerced)
    {
        MutationObserver?.OnPropertyMutated(new UiPropertyMutation(
            this,
            property,
            mutatingSource,
            oldValue,
            oldEffectiveSource,
            newValue,
            valueSource,
            oldSourceValue,
            newSourceValue,
            wasCoerced));
    }
}
