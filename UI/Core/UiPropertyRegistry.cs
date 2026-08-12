using System.Collections.Concurrent;

namespace Cerneala.UI.Core;

public static class UiPropertyRegistry
{
    private static readonly ConcurrentDictionary<(Type OwnerType, string Name), UiProperty> Properties = new();
    private static readonly Dictionary<UiPropertyOptions, IReadOnlyList<UiProperty>> PropertiesByOptions = new();
    private static readonly object SyncRoot = new();
    private static IReadOnlyList<UiProperty>? registeredProperties;
    private static long nextId;

    public static UiProperty<T> Register<T>(string name, Type ownerType, UiPropertyMetadata<T> metadata)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentNullException.ThrowIfNull(metadata);

        lock (SyncRoot)
        {
            UiProperty<T> property = new(Interlocked.Increment(ref nextId), name, ownerType, metadata);
            if (!Properties.TryAdd((ownerType, name), property))
            {
                throw new InvalidOperationException($"UI property '{ownerType.FullName}.{name}' is already registered.");
            }

            registeredProperties = null;
            PropertiesByOptions.Clear();
            return property;
        }
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

    public static IReadOnlyList<UiProperty> GetRegisteredProperties()
    {
        lock (SyncRoot)
        {
            return registeredProperties ??= Array.AsReadOnly(
                Properties.Values.OrderBy(property => property.Id).ToArray());
        }
    }

    public static IReadOnlyList<UiProperty> GetPropertiesWithOptions(UiPropertyOptions options)
    {
        lock (SyncRoot)
        {
            if (PropertiesByOptions.TryGetValue(options, out IReadOnlyList<UiProperty>? cached))
            {
                return cached;
            }

            IReadOnlyList<UiProperty> properties = Array.AsReadOnly(Properties.Values
                .Where(property => (property.Options & options) == options)
                .OrderBy(property => property.Id)
                .ToArray());
            PropertiesByOptions.Add(options, properties);
            return properties;
        }
    }
}
