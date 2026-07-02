namespace Cerneala.UI.Core;

public class UiPropertyChangedEventArgs : EventArgs
{
    public UiPropertyChangedEventArgs(
        UiObject owner,
        UiProperty property,
        object? oldValue,
        object? newValue,
        UiPropertyValueSource valueSource)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Property = property ?? throw new ArgumentNullException(nameof(property));
        OldValue = oldValue;
        NewValue = newValue;
        ValueSource = valueSource;
    }

    public UiObject Owner { get; }

    public UiProperty Property { get; }

    public object? OldValue { get; }

    public object? NewValue { get; }

    public UiPropertyValueSource ValueSource { get; }
}
