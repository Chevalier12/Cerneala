namespace Cerneala.UI.Theming;

public sealed class ThemeProvider
{
    private Theme theme;

    public ThemeProvider(Theme theme)
    {
        this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public Theme Theme
    {
        get => theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(theme, value))
            {
                return;
            }

            Theme oldTheme = theme;
            theme = value;
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, value));
        }
    }

    public bool TryGet<T>(ThemeKey<T> key, out T value)
    {
        return theme.TryGet(key, out value);
    }

    public T Get<T>(ThemeKey<T> key)
    {
        return theme.Get(key);
    }
}

public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme)
    {
        OldTheme = oldTheme ?? throw new ArgumentNullException(nameof(oldTheme));
        NewTheme = newTheme ?? throw new ArgumentNullException(nameof(newTheme));
    }

    public Theme OldTheme { get; }

    public Theme NewTheme { get; }
}
