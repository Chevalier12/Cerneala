using Cerneala.UI.Core;

namespace Cerneala.UI.Styling;

public sealed class Setter<T> : Setter
{
    private readonly T? value;
    private readonly ThemeResource<T>? themeResource;

    public Setter(UiProperty<T> property, T value)
        : base(property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        this.value = value;
    }

    public Setter(UiProperty<T> property, ThemeResource<T> themeResource)
        : base(property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        this.themeResource = themeResource ?? throw new ArgumentNullException(nameof(themeResource));
    }

    public new UiProperty<T> Property { get; }

    public override bool IsThemeBacked => themeResource is not null;

    public override object? GetValue(ThemeProvider? themeProvider = null)
    {
        return ResolveValue(themeProvider);
    }

    public override void Apply(UiObject target, UiPropertyValueSource source, ThemeProvider? themeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(Property, ResolveValue(themeProvider), source);
    }

    public override void Clear(UiObject target, UiPropertyValueSource source)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.ClearValue(Property, source);
    }

    private T ResolveValue(ThemeProvider? themeProvider)
    {
        return themeResource is null ? value! : themeResource.Resolve(themeProvider);
    }
}
