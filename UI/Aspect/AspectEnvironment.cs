namespace Cerneala.UI.Aspect;

public sealed class AspectEnvironment
{
    private readonly Dictionary<AspectToken, object?> values = [];
    private readonly AspectEnvironment? parent;

    public AspectEnvironment(string name, AspectEnvironment? parent = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect environment name cannot be empty.", nameof(name));
        }

        Name = name;
        this.parent = parent;
    }

    public string Name { get; }

    public int Version { get; private set; }

    public void Set<T>(AspectToken<T> token, T value)
    {
        ArgumentNullException.ThrowIfNull(token);
        values[token] = value;
        Version++;
    }

    public void Set(AspectToken token, object? value)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (value is not null && !token.ValueType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Aspect token '{token.Name}' expects values of type '{token.ValueType.FullName}'.",
                nameof(value));
        }

        values[token] = value;
        Version++;
    }

    public bool TryGet<T>(AspectToken<T> token, out T value)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (values.TryGetValue(token, out object? raw))
        {
            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            if (raw is null && default(T) is null)
            {
                value = default!;
                return true;
            }

            value = default!;
            return false;
        }

        if (parent is not null)
        {
            return parent.TryGet(token, out value);
        }

        value = default!;
        return false;
    }

    public AspectEnvironment CreateChildScope(string name)
    {
        return new AspectEnvironment(name, this);
    }
}
