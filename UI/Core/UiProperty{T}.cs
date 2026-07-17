namespace Cerneala.UI.Core;

public sealed class UiProperty<T> : UiProperty
{
    private readonly object? boxedDefaultValue;

    internal UiProperty(long id, string name, Type ownerType, UiPropertyMetadata<T> metadata)
        : base(id, name, ownerType, typeof(T), metadata.Options)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        boxedDefaultValue = metadata.DefaultValue;
    }

    public UiPropertyMetadata<T> Metadata { get; }

    internal override object? DefaultValueUntyped => boxedDefaultValue;

    public static UiProperty<T> Register(string name, Type ownerType, UiPropertyMetadata<T> metadata)
    {
        return UiPropertyRegistry.Register(name, ownerType, metadata);
    }

    public static UiPropertyKey<T> RegisterReadOnly(string name, Type ownerType, UiPropertyMetadata<T> metadata)
    {
        return UiPropertyRegistry.RegisterReadOnly(name, ownerType, metadata);
    }

    internal override bool AreEqualUntyped(object? left, object? right)
    {
        return Metadata.EqualityComparer.Equals((T?)left!, (T?)right!);
    }

    internal override object? CoerceUntyped(UiObject owner, object? value)
    {
        if (value is not T typedValue)
        {
            if (value is null && default(T) is null)
            {
                typedValue = default!;
            }
            else
            {
                throw new ArgumentException(
                    $"Value for '{DiagnosticName}' must be assignable to '{typeof(T).FullName}'.",
                    nameof(value));
            }
        }

        return Metadata.CoerceValue is null ? typedValue : Metadata.CoerceValue(owner, typedValue);
    }

    internal override void ValidateUntyped(object? value)
    {
        if (value is not T typedValue)
        {
            if (value is null && default(T) is null)
            {
                typedValue = default!;
            }
            else
            {
                throw new ArgumentException(
                    $"Value for '{DiagnosticName}' must be assignable to '{typeof(T).FullName}'.",
                    nameof(value));
            }
        }

        if (Metadata.ValidateValue is not null && !Metadata.ValidateValue(typedValue))
        {
            throw new ArgumentException($"Value for '{DiagnosticName}' failed validation.", nameof(value));
        }
    }
}
