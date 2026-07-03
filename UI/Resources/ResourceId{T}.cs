namespace Cerneala.UI.Resources;

public readonly record struct ResourceId<T>
{
    public ResourceId(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Resource key cannot be empty.", nameof(key));
        }

        Key = key;
    }

    public string Key { get; }

    public Type ResourceType => typeof(T);

    public override string ToString()
    {
        return $"{typeof(T).FullName}:{Key}";
    }
}
