using System.Collections.Concurrent;

namespace Cerneala.UI.Core;

public static class UiPropertyRegistry
{
    private static readonly ConcurrentDictionary<(Type OwnerType, string Name), UiProperty> Properties = new();
    private static long nextId;

    public static UiProperty<T> Register<T>(string name, Type ownerType, UiPropertyMetadata<T> metadata)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentNullException.ThrowIfNull(metadata);

        UiProperty<T> property = new(Interlocked.Increment(ref nextId), name, ownerType, metadata);
        if (!Properties.TryAdd((ownerType, name), property))
        {
            throw new InvalidOperationException($"UI property '{ownerType.FullName}.{name}' is already registered.");
        }

        return property;
    }

    public static UiPropertyKey<T> RegisterReadOnly<T>(string name, Type ownerType, UiPropertyMetadata<T> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        UiPropertyOptions options = metadata.Options | UiPropertyOptions.ReadOnly;
        UiPropertyMetadata<T> readOnlyMetadata = new(
            metadata.DefaultValue,
            options,
            metadata.EqualityComparer,
            metadata.ValidateValue,
            metadata.CoerceValue);

        return new UiPropertyKey<T>(Register(name, ownerType, readOnlyMetadata));
    }
}
