# ThemeProvider Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemeProvider.cs`

Stores the active `Theme`, raises change notifications when it is replaced, and resolves typed theme values.

```csharp
public sealed class ThemeProvider
```

Inheritance:
`object` -> `ThemeProvider`

## Examples

Create a provider from the default theme and assign it to a root:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

UIRoot root = new(800, 600);
ThemeProvider provider = new(DefaultTheme.Create());

root.SetThemeProvider(provider);
```

Read and update themed values:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemeProvider provider = new(DefaultTheme.Create());

Color accent = provider.Get(DefaultTheme.AccentKey);

provider.ThemeChanged += (_, args) =>
{
    Theme oldTheme = args.OldTheme;
    Theme newTheme = args.NewTheme;
};

provider.Theme = new Theme("High contrast")
    .Set(DefaultTheme.AccentKey, Color.White);
```

## Remarks

`ThemeProvider` is the mutable holder for the current `Theme`. The constructor and `Theme` setter require non-null themes. Setting `Theme` to the same instance is a no-op; setting it to a different instance stores the new theme and raises `ThemeChanged` with the old and new values.

`Get<T>` and `TryGet<T>` delegate to the current `Theme`. `Get<T>` propagates the theme lookup failure when the key is missing, while `TryGet<T>` returns `false` and assigns the default value for `T`.

`UIRoot.SetThemeProvider` subscribes to `ThemeChanged`. When the provider raises the event, the root invalidates aspect state for the subtree so controls and templates can resolve theme-backed aspect values again.

## Constructors

| Name | Description |
| --- | --- |
| `ThemeProvider(Theme theme)` | Initializes a provider with the active theme. Throws `ArgumentNullException` when `theme` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Theme` | `Theme` | Gets or sets the active theme. Setting a different non-null instance raises `ThemeChanged`; setting the same instance does nothing. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Get<T>(ThemeKey<T> key)` | `T` | Gets the typed value for `key` from the active theme, or throws when the theme does not contain a compatible value. |
| `TryGet<T>(ThemeKey<T> key, out T value)` | `bool` | Attempts to get the typed value for `key` from the active theme. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `ThemeChanged` | `EventHandler<ThemeChangedEventArgs>?` | Raised after the active theme is replaced with a different instance. |

## Applies to

Cerneala UI theming, root theme assignment, aspect resolution, theme resources, and motion token resolution.

## See also

- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeChangedEventArgs`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Theming.ThemeResource<T>`
- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Elements.UIRoot`
