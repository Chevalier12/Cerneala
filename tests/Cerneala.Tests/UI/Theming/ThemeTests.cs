using Cerneala.Drawing;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.UI.Theming;

public sealed class ThemeTests
{
    [Fact]
    public void ThemeResolvesTypedValues()
    {
        ThemeKey<Color> key = new("Accent");
        Theme theme = new Theme("Test").Set(key, Color.White);

        Assert.True(theme.TryGet(key, out Color value));
        Assert.Equal(Color.White, value);
        Assert.Equal(Color.White, theme.Get(key));
    }

    [Fact]
    public void MissingThemeValueFailsClearly()
    {
        Theme theme = new();
        ThemeKey<Color> key = new("Missing");

        Assert.Throws<KeyNotFoundException>(() => theme.Get(key));
    }

    [Fact]
    public void DefaultThemeProvidesPalette()
    {
        Theme theme = DefaultTheme.Create();

        ThemePalette palette = theme.Get(DefaultTheme.PaletteKey);

        Assert.Equal(theme.Get(DefaultTheme.BackgroundKey), palette.Background);
        Assert.Equal(theme.Get(DefaultTheme.ForegroundKey), palette.Foreground);
    }

    [Fact]
    public void ThemeCanBeChangedBeforeInstallation()
    {
        ThemeKey<Color> key = new("Accent");
        Theme theme = new();

        Assert.Same(theme, theme.Set(key, Color.White));
        Assert.Same(theme, theme.Set(key, Color.Black));
        Assert.Equal(Color.Black, theme.Get(key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProviderInstallationRejectsThemeChangesAndPreservesReads(bool installByAssignment)
    {
        ThemeKey<Color> key = new("Accent");
        ThemeKey<string?> nullableKey = new("Optional");
        Theme theme = new Theme().Set(key, Color.White).Set(nullableKey, null);
        ThemeProvider provider = installByAssignment ? new(new Theme()) : new(theme);
        if (installByAssignment)
        {
            provider.Theme = theme;
        }

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => theme.Set(key, Color.Black));

        Assert.Contains("installed", error.Message);
        Assert.Contains("new Theme", error.Message);
        Assert.Contains("assign", error.Message);
        Assert.Contains("provider", error.Message);
        Assert.Throws<InvalidOperationException>(() => theme.Set(new ThemeKey<Color>("New"), Color.Black));
        Assert.Equal(Color.White, theme.Get(key));
        Assert.True(theme.TryGet(key, out Color value));
        Assert.Equal(Color.White, value);
        Assert.Equal(Color.White, provider.Get(key));
        Assert.True(theme.TryGet(nullableKey, out string? optional));
        Assert.Null(optional);
        Assert.False(theme.TryGet(new ThemeKey<Color>("Missing"), out _));
        Assert.Throws<KeyNotFoundException>(() => theme.Get(new ThemeKey<Color>("Missing")));
    }

    [Fact]
    public void ReplacementFreezesNewThemeBeforeNotificationAndNeverUnfreezesOldTheme()
    {
        ThemeKey<Color> key = new("Accent");
        Theme original = new Theme().Set(key, Color.White);
        Theme replacement = new Theme().Set(key, Color.Black);
        ThemeProvider provider = new(original);
        int notifications = 0;
        provider.ThemeChanged += (_, args) =>
        {
            notifications++;
            Assert.Same(original, args.OldTheme);
            Assert.Same(replacement, args.NewTheme);
            Assert.Same(replacement, provider.Theme);
            Assert.Throws<InvalidOperationException>(() => args.NewTheme.Set(key, Color.White));
            Assert.Throws<InvalidOperationException>(() => args.OldTheme.Set(key, Color.Black));
        };

        provider.Theme = replacement;

        Assert.Equal(1, notifications);
        Assert.Equal(Color.Black, provider.Get(key));
        Assert.Throws<InvalidOperationException>(() => original.Set(key, Color.Black));
        Assert.Throws<InvalidOperationException>(() => replacement.Set(key, Color.White));
    }

    [Fact]
    public void ThemeRemainsFrozenWithoutRetainingItsProvider()
    {
        Theme theme = CreateInstalledTheme();

        Assert.Throws<InvalidOperationException>(() => theme.Set(DefaultTheme.SurfaceKey, Color.White));
    }

    [Fact]
    public void FrozenThemeCanBeSharedAndSameInstanceAssignmentDoesNotNotify()
    {
        Theme theme = DefaultTheme.Create();
        ThemeProvider first = new(theme);
        ThemeProvider second = new(theme);
        int notifications = 0;
        first.ThemeChanged += (_, _) => notifications++;
        second.ThemeChanged += (_, _) => notifications++;

        first.Theme = theme;
        second.Theme = theme;

        Assert.Same(theme, first.Theme);
        Assert.Same(theme, second.Theme);
        Assert.Equal(0, notifications);
        Assert.Throws<InvalidOperationException>(() => theme.Set(DefaultTheme.SurfaceKey, Color.White));
    }

    private static Theme CreateInstalledTheme()
    {
        Theme theme = DefaultTheme.Create();
        _ = new ThemeProvider(theme);
        return theme;
    }
}
