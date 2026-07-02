namespace Cerneala.UI.Core;

public sealed class UiPropertyChangedEventArgs<T> : UiPropertyChangedEventArgs
{
    public UiPropertyChangedEventArgs(
        UiObject owner,
        UiProperty<T> property,
        T oldValue,
        T newValue,
        UiPropertyValueSource valueSource)
        : base(owner, property, oldValue, newValue, valueSource)
    {
        Property = property;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public new UiProperty<T> Property { get; }

    public new T OldValue { get; }

    public new T NewValue { get; }
}
