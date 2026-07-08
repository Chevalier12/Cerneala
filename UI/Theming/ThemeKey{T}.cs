namespace Cerneala.UI.Theming;

public readonly record struct ThemeKey<T>
{
    public ThemeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Theme key cannot be empty.", nameof(key));
        }

        Key = key;
    }

    public string Key { get; }

    public Type ValueType => typeof(T);

    public override string ToString()
    {
        return $"{typeof(T).FullName}:{Key}";
    }
}
