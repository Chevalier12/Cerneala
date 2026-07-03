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

    public Type ResourceType { get; }

    public string Key { get; }

    public object? OldValue { get; }

    public object? NewValue { get; }

    public long Version { get; }

    public bool Matches<T>(ResourceId<T> id)
    {
        return ResourceType == typeof(T) && Key == id.Key;
    }
}
