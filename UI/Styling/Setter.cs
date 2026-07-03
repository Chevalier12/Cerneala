using Cerneala.UI.Core;

namespace Cerneala.UI.Styling;

public abstract class Setter
{
    private protected Setter(UiProperty property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public UiProperty Property { get; }

    public Type ValueType => Property.ValueType;

    public abstract bool IsThemeBacked { get; }

    public abstract object? GetValue(ThemeProvider? themeProvider = null);

    public abstract void Apply(UiObject target, UiPropertyValueSource source, ThemeProvider? themeProvider = null);

    public abstract void Clear(UiObject target, UiPropertyValueSource source);

    public static Setter Create(UiProperty property, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (value is null && property.ValueType.IsValueType && Nullable.GetUnderlyingType(property.ValueType) is null)
        {
            throw new ArgumentException(
                $"Value for property '{property.DiagnosticName}' cannot be null because '{property.ValueType.FullName}' is a non-nullable value type.",
                nameof(value));
        }

        if (value is not null && !property.ValueType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Value type '{value.GetType().FullName}' is not assignable to property '{property.DiagnosticName}' of type '{property.ValueType.FullName}'.",
                nameof(value));
        }

        Type setterType = typeof(Setter<>).MakeGenericType(property.ValueType);
        return (Setter)Activator.CreateInstance(setterType, property, value)!;
    }
}
