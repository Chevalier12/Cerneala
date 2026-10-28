using System.Runtime.ExceptionServices;

namespace Cerneala.UI.Core;

public class UiObject
{
    private readonly UiPropertyStore propertyStore = new();

    // Kept on the object itself so hot per-node caches can validate a snapshot
    // without touching the separately allocated store.
    private long propertyValueVersion;

    internal long PropertyValueVersion => propertyValueVersion;

    public event EventHandler<UiPropertyChangedEventArgs>? PropertyChanged;

    protected virtual UiPropertyMutationObserver? MutationObserver => null;

    protected virtual void VerifyMutationAccess()
    {
    }

    internal virtual void ValidatePropertyMutation(UiProperty property, object? value)
    {
    }

    // Validates a value that a write makes the effective value, before the store changes. A write at a source below the
    // effective source is stored and shadowed without this check, as a style or trigger value under a local value.
    internal virtual void ValidateEffectiveValueChange(UiProperty property, object? value)
    {
    }

    // Runs the owner's checks for a write of the coerced value at source, before the store changes.
    internal void ValidatePropertyWrite(UiProperty property, object? coerced, UiPropertyValueSource source)
    {
        ValidatePropertyMutation(property, coerced);
        if (propertyStore.IsEffectiveForWrite(property, source))
        {
            ValidateEffectiveValueChange(property, coerced);
        }
    }

    // Stores the control's starting value below every value source, so markup, Aspect, binding, animation and local
    // values override it, and clearing them shows it again. Protected so a control in another assembly can call it.
    protected internal void SetFrameworkDefault<T>(UiProperty<T> property, T value)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        VerifyMutationAccess();
        T oldValue = GetValue(property);
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);
        ValidatePropertyWrite(property, coerced, UiPropertyValueSource.Default);
        propertyStore.SetFrameworkDefault(property, coerced);
        propertyValueVersion++;
        T newValue = GetValue(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(
                property,
                oldValue,
                newValue,
                UiPropertyValueSource.Default,
                UiPropertyValueSource.Default,
                oldSource,
                oldValue,
                coerced,
                !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
        }
        else
        {
            NotifyPropertyMutated(
                property,
                UiPropertyValueSource.Default,
                oldValue,
                oldSource,
                newValue,
                GetValueSource(property),
                oldValue,
                coerced,
                !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
        }
    }

    internal bool HasFrameworkDefault(UiProperty property)
    {
        return propertyStore.HasFrameworkDefault(property);
    }

    // True when this object sets the property: a value at a concrete source, or a framework default. GetValueSource
    // reports Default for a framework default too, so it alone cannot tell a starting value from no value.
    internal bool IsSet(UiProperty property)
    {
        return GetValueSource(property) != UiPropertyValueSource.Default || HasFrameworkDefault(property);
    }

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

    // The effective value that clearing the property at source would leave, without changing the store.
    internal T GetValueAfterClear<T>(UiProperty<T> property, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(property);
        return (T)propertyStore.GetValueAfterClear(property, source)!;
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

        VerifyMutationAccess();

        T oldValue = GetValue(property);
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        ValidateValueRevealedByClear(property, oldValue, source);
        propertyStore.ClearValue(property, source);
        propertyValueVersion++;
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
        return SetValueUntypedCore(property, value, source, skipInvalidSample: false, out _);
    }

    internal bool TrySetAnimationValueUntyped(UiProperty property, object? value)
    {
        SetValueUntypedCore(property, value, UiPropertyValueSource.Animation, skipInvalidSample: true, out bool applied);
        return applied;
    }

    private object? SetValueUntypedCore(
        UiProperty property, object? value, UiPropertyValueSource source, bool skipInvalidSample, out bool applied)
    {
        applied = false;
        ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{property.DiagnosticName}' is read-only.");
        }

        VerifyMutationAccess();

        object? oldValue = GetValue(property);
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        object? coerced = property.CoerceUntyped(this, value);
        if (skipInvalidSample)
        {
            if (!property.IsValidUntyped(coerced))
            {
                return oldValue;
            }

            // Owner hooks also validate values (for example collider radii).
            // Catch only their value rejection, never coercion or write callbacks.
            try
            {
                ValidatePropertyWrite(property, coerced, source);
            }
            catch (ArgumentException)
            {
                return oldValue;
            }
        }
        else
        {
            property.ValidateUntyped(coerced);
            ValidatePropertyWrite(property, coerced, source);
        }
        propertyStore.SetValue(property, source, coerced);
        applied = true;
        propertyValueVersion++;
        object? newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.AreEqualUntyped(oldValue, newValue))
        {
            NotifyPropertyChangedUntyped(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, coerced, !Equals(value, coerced));
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, coerced, !Equals(value, coerced));
            if (newSource == source)
            {
                NotifyEffectiveValueRewritten(property);
            }
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

        VerifyMutationAccess();

        object? oldValue = GetValue(property);
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        ValidateValueRevealedByClear(property, oldValue, source);
        propertyStore.ClearValue(property, source);
        propertyValueVersion++;
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

    // A clear makes a lower source's value effective. That value was valid when it was written, but the owner's state
    // can have changed since, for example a Content element that got another parent. So the clear checks it, before the
    // store changes, as a write checks the value it stores. The revealed value becomes the effective value, so it also
    // gets the checks of a write that becomes effective.
    private void ValidateValueRevealedByClear(UiProperty property, object? oldValue, UiPropertyValueSource source)
    {
        object? revealedValue = propertyStore.GetValueAfterClear(property, source);
        if (!property.AreEqualUntyped(oldValue, revealedValue))
        {
            ValidatePropertyMutation(property, revealedValue);
            ValidateEffectiveValueChange(property, revealedValue);
        }
    }

    protected virtual void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    private T SetValueCore<T>(UiProperty<T> property, T value, UiPropertyValueSource source)
    {
        VerifyMutationAccess();
        T oldValue = GetValue(property);
        UiPropertyValueSource oldSource = GetValueSource(property);
        object? oldSourceValue = GetSourceValue(property, source);
        object? coerced = property.CoerceUntyped(this, value);
        property.ValidateUntyped(coerced);
        ValidatePropertyWrite(property, coerced, source);

        propertyStore.SetValue(property, source, coerced);
        propertyValueVersion++;
        T newValue = GetValue(property);
        UiPropertyValueSource newSource = GetValueSource(property);
        if (!property.Metadata.EqualityComparer.Equals(oldValue, newValue))
        {
            NotifyPropertyChanged(property, oldValue, newValue, newSource, source, oldSource, oldSourceValue, coerced, !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
        }
        else
        {
            NotifyPropertyMutated(property, source, oldValue, oldSource, newValue, newSource, oldSourceValue, coerced, !EqualityComparer<T>.Default.Equals(value, (T)coerced!));
            if (newSource == source)
            {
                NotifyEffectiveValueRewritten(property);
            }
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
        NotifyPropertyChange(new UiPropertyChange(
            new UiPropertyChangedEventArgs<T>(this, property, oldValue, newValue, valueSource),
            mutatingSource,
            oldEffectiveSource,
            oldSourceValue,
            newSourceValue,
            wasCoerced));
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
        NotifyPropertyChange(new UiPropertyChange(
            new UiPropertyChangedEventArgs(this, property, oldValue, newValue, valueSource),
            mutatingSource,
            oldEffectiveSource,
            oldSourceValue,
            newSourceValue,
            wasCoerced));
    }

    // Notifies a changed effective value. Selector overrides it to notify a SelectedIndex set through SetValue inside
    // the selection change that follows it, as the SelectedIndex setter does.
    internal virtual void NotifyPropertyChange(in UiPropertyChange change)
    {
        RaisePropertyChange(change);
    }

    // Notifies a SetValue whose value became the effective value without changing it. No property change is raised.
    // Selector overrides it so that writing the current SelectedIndex selects that index, as the setter does.
    internal virtual void NotifyEffectiveValueRewritten(UiProperty property)
    {
    }

    // Raises OnPropertyChanged, then reports the mutation, then invalidates what the property affects.
    // The new value is already stored, so a throwing OnPropertyChanged does not skip the report or the
    // invalidation: they still run, then the first failure propagates.
    internal void RaisePropertyChange(in UiPropertyChange change)
    {
        UiPropertyChangedEventArgs args = change.Args;
        UiProperty property = args.Property;
        ExceptionDispatchInfo? failure = null;
        try { OnPropertyChanged(args); }
        catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }

        try
        {
            NotifyPropertyMutated(
                property,
                change.MutatingSource,
                args.OldValue,
                change.OldEffectiveSource,
                args.NewValue,
                args.ValueSource,
                change.OldSourceValue,
                change.NewSourceValue,
                change.WasCoerced);

            UiPropertyOptions invalidationOptions = property.Options & (
                UiPropertyOptions.AffectsMeasure |
                UiPropertyOptions.AffectsArrange |
                UiPropertyOptions.AffectsRender |
                UiPropertyOptions.AffectsHitTest |
                UiPropertyOptions.AffectsAspect |
                UiPropertyOptions.AffectsInputVisual |
                UiPropertyOptions.AffectsSemantics |
                UiPropertyOptions.AffectsParentArrange |
                UiPropertyOptions.Inherits);
            if (invalidationOptions != UiPropertyOptions.None && this is IUiPropertyOwner owner)
            {
                owner.OnPropertyInvalidated(args, invalidationOptions);
            }
        }
        catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }

        failure?.Throw();
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
