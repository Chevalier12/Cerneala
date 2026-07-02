using System.Collections.Generic;

namespace Cerneala.UI.Core;

public sealed class UiPropertyMetadata<T>
{
    public UiPropertyMetadata(
        T defaultValue,
        UiPropertyOptions options = UiPropertyOptions.None,
        IEqualityComparer<T>? equalityComparer = null,
        ValidateValue<T>? validateValue = null,
        CoerceValue<T>? coerceValue = null)
    {
        DefaultValue = defaultValue;
        Options = options;
        EqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        ValidateValue = validateValue;
        CoerceValue = coerceValue;
    }

    public T DefaultValue { get; }

    public UiPropertyOptions Options { get; }

    public IEqualityComparer<T> EqualityComparer { get; }

    public ValidateValue<T>? ValidateValue { get; }

    public CoerceValue<T>? CoerceValue { get; }
}
