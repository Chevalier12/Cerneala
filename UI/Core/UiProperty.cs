namespace Cerneala.UI.Core;

public abstract class UiProperty
{
    internal UiProperty(long id, string name, Type ownerType, Type valueType, UiPropertyOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("UI property name cannot be empty.", nameof(name));
        }

        Id = id;
        Name = name;
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Options = options;
        DiagnosticName = $"{OwnerType.FullName}.{Name}";
    }

    public long Id { get; }

    public string Name { get; }

    public Type OwnerType { get; }

    public Type ValueType { get; }

    public UiPropertyOptions Options { get; }

    public string DiagnosticName { get; }

    public bool IsReadOnly => Options.HasFlag(UiPropertyOptions.ReadOnly);

    internal abstract object? DefaultValueUntyped { get; }

    internal abstract bool AreEqualUntyped(object? left, object? right);

    internal abstract object? CoerceUntyped(UiObject owner, object? value);

    internal abstract void ValidateUntyped(object? value);

    public override string ToString()
    {
        return DiagnosticName;
    }
}
