namespace Cerneala.UI.Styling;

public sealed class Theme
{
    private readonly Dictionary<EntryKey, object?> values = [];

    public Theme(string? name = null)
    {
        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Theme name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public string? Name { get; }

    public Theme Set<T>(ThemeKey<T> key, T value)
    {
        values[EntryKey.From(key)] = value;
        return this;
    }

    public bool TryGet<T>(ThemeKey<T> key, out T value)
    {
        if (values.TryGetValue(EntryKey.From(key), out object? raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        if (values.TryGetValue(EntryKey.From(key), out raw) && raw is null && default(T) is null)
        {
            value = default!;
            return true;
        }

        value = default!;
        return false;
    }

    public T Get<T>(ThemeKey<T> key)
    {
        return TryGet(key, out T? value)
            ? value
            : throw new KeyNotFoundException($"Theme value '{key}' was not found.");
    }

    private readonly record struct EntryKey(Type ValueType, string Key)
    {
        public static EntryKey From<T>(ThemeKey<T> key)
        {
            return new EntryKey(typeof(T), key.Key);
        }
    }
}
