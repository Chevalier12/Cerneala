using Cerneala.UI.Core;

namespace Cerneala.UI.Data;

public static class BindingOperations
{
    public static UiPropertyBinding<T> BindOneWay<T>(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source)
    {
        return Bind(target, targetProperty, source, BindingMode.OneWay);
    }

    public static UiPropertyBinding<T> BindTwoWay<T>(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source)
    {
        return Bind(target, targetProperty, source, BindingMode.TwoWay);
    }

    public static UiPropertyBinding<T> Bind<T>(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source,
        BindingMode mode)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        ArgumentNullException.ThrowIfNull(source);

        if (targetProperty.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{targetProperty.DiagnosticName}' is read-only.");
        }

        return new UiPropertyBinding<T>(target, targetProperty, source, mode);
    }
}
