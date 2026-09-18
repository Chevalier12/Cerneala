namespace Cerneala.UI.Core;

public static class UiPropertyRegistry
{
    private static readonly Dictionary<(Type OwnerType, string Name), UiProperty> Properties = new();
    private static readonly List<UiProperty> PropertiesById = new();
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
            Publish(property);
            return property;
        }
    }

    // The caller holds SyncRoot across identity allocation, construction and publication.
    private static void Publish(UiProperty property)
    {
        if (!Properties.TryAdd((property.OwnerType, property.Name), property))
        {
            throw new InvalidOperationException($"UI property '{property.OwnerType.FullName}.{property.Name}' is already registered.");
        }

        int index = PropertiesById.Count;
        // Normally an append. A custom owner's metadata can register another
        // property reentrantly during construction, publishing a later ID first.
        while (index > 0 && PropertiesById[index - 1].Id > property.Id)
        {
            index--;
        }
        PropertiesById.Insert(index, property);
        registeredProperties = null;
        PropertiesByOptions.Clear();
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
            return registeredProperties ??= Array.AsReadOnly(PropertiesById.ToArray());
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

            int count = 0;
            foreach (UiProperty property in PropertiesById)
            {
                if ((property.Options & options) == options) { count++; }
            }
            UiProperty[] selected = new UiProperty[count];
            int index = 0;
            foreach (UiProperty property in PropertiesById)
            {
                if ((property.Options & options) == options) { selected[index++] = property; }
            }
            IReadOnlyList<UiProperty> properties = Array.AsReadOnly(selected);
            PropertiesByOptions.Add(options, properties);
            return properties;
        }
    }
}
