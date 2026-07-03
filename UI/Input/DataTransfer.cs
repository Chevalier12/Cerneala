namespace Cerneala.UI.Input;

public sealed class DataTransfer
{
    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Formats => values.Keys;

    public DataTransfer SetData(string format, object? value)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Data format cannot be empty.", nameof(format));
        }

        values[format] = value;
        return this;
    }

    public bool Contains(string format)
    {
        return values.ContainsKey(format);
    }

    public bool TryGetData<T>(string format, out T? value)
    {
        if (!values.TryGetValue(format, out object? raw))
        {
            value = default;
            return false;
        }

        if (raw is null)
        {
            value = default;
            return default(T) is null;
        }

        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}
