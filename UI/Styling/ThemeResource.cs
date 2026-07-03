namespace Cerneala.UI.Styling;

public sealed class ThemeResource<T>
{
    public ThemeResource(ThemeKey<T> key)
    {
        Key = key;
    }

    public ThemeKey<T> Key { get; }

    public T Resolve(ThemeProvider? provider)
    {
        if (provider is null)
        {
            throw new InvalidOperationException($"A theme provider is required to resolve theme resource '{Key}'.");
        }

        return provider.Get(Key);
    }
}
