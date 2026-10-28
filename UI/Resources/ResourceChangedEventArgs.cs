namespace Cerneala.UI.Resources;

public sealed class ResourceChangedEventArgs : EventArgs
{
    public ResourceChangedEventArgs(Type resourceType, string key, object? oldValue, object? newValue, long version)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Resource key cannot be empty.", nameof(key));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Resource version must be positive.");
        }

        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        Version = version;
    }

    internal ResourceChangedEventArgs(
        Type resourceType,
        string key,
        object? oldValue,
        object? newValue,
        long version,
        bool appliesToEveryResourceType)
        : this(resourceType, key, oldValue, newValue, version)
    {
        AppliesToEveryResourceType = appliesToEveryResourceType;
    }

    public Type ResourceType { get; }

    // True when the provider keeps one entry per key, so the change reaches every type the key is read as.
    internal bool AppliesToEveryResourceType { get; }

    public string Key { get; }

    public object? OldValue { get; }

    public object? NewValue { get; }

    public long Version { get; }

    public bool Matches<T>(ResourceId<T> id)
    {
        return ResourceType == typeof(T) && Key == id.Key;
    }
}
