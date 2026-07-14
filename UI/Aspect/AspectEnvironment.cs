using Cerneala.UI.Relay;

namespace Cerneala.UI.Aspect;

public sealed class AspectEnvironment
{
    private readonly Dictionary<AspectToken, object?> values = [];
    private readonly AspectEnvironment? parent;
    private readonly IUiThreadAccess threadAccess;

    public AspectEnvironment(string name, AspectEnvironment? parent = null)
        : this(parent?.threadAccess ?? new CapturedUiThreadAccess(), name, parent)
    {
    }

    internal AspectEnvironment(IUiThreadAccess threadAccess, string name, AspectEnvironment? parent = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect environment name cannot be empty.", nameof(name));
        }

        Name = name;
        this.threadAccess = threadAccess ?? throw new ArgumentNullException(nameof(threadAccess));
        this.parent = parent;
    }

    public string Name { get; }

    public int Version { get; private set; }

    internal event Action<AspectToken>? TokenChanged;

    public void Set<T>(AspectToken<T> token, T value)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(token);
        values[token] = value;
        Version++;
        TokenChanged?.Invoke(token);
    }

    public void Set(AspectToken token, object? value)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(token);
        if (value is not null && !token.ValueType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Aspect token '{token.Name}' expects values of type '{token.ValueType.FullName}'.",
                nameof(value));
        }

        values[token] = value;
        Version++;
        TokenChanged?.Invoke(token);
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
        threadAccess.VerifyAccess();
        return new AspectEnvironment(threadAccess, name, this);
    }

    internal void ReplaceWith(AspectEnvironment source)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(source);
        AspectToken[] changedTokens = values.Keys
            .Union(source.values.Keys)
            .Where(token =>
                !values.TryGetValue(token, out object? current) ||
                !source.values.TryGetValue(token, out object? next) ||
                !Equals(current, next))
            .ToArray();

        values.Clear();
        foreach ((AspectToken token, object? value) in source.values)
        {
            values[token] = value;
        }

        Version++;
        foreach (AspectToken token in changedTokens)
        {
            TokenChanged?.Invoke(token);
        }
    }
}
