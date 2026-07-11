# ThemePalette Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemePalette.cs`

Stores the core `Color` values that make up a Cerneala UI theme palette.

```csharp
public sealed class ThemePalette
```

Inheritance:
`object` -> `ThemePalette`

## Examples

Create a palette and store it in a theme:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

ThemePalette palette = new(
    new Color(248, 250, 252),
    new Color(28, 35, 48),
    new Color(255, 255, 255),
    new Color(148, 163, 184),
    new Color(37, 99, 235));

Theme theme = new Theme("Custom").Set(DefaultTheme.PaletteKey, palette);
```

Read the built-in palette from the default theme:

```csharp
using Cerneala.UI.Theming;

Theme theme = DefaultTheme.Create();
ThemePalette palette = theme.Get(DefaultTheme.PaletteKey);
```

## Remarks

`ThemePalette` is an immutable container for the five core colors used by the default theme surface: background, foreground, surface, border, and accent. The constructor assigns each supplied `Color` to a read-only property.

`DefaultTheme.Create()` creates a `ThemePalette`, stores it under `DefaultTheme.PaletteKey`, and also stores each palette color under the matching individual theme key. `ThemeTokenBridge.CreateEnvironment` projects those individual color keys into aspect tokens.

## Constructors

| Name | Description |
| --- | --- |
| `ThemePalette(Color background, Color foreground, Color surface, Color border, Color accent)` | Initializes a palette with the supplied background, foreground, surface, border, and accent colors. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Background` | `Color` | Gets the default background color. |
| `Foreground` | `Color` | Gets the default foreground color. |
| `Surface` | `Color` | Gets the default surface color. |
| `Border` | `Color` | Gets the default border color. |
| `Accent` | `Color` | Gets the default accent color. |

## Applies to

Cerneala UI theming and palette-backed default color resources.

## See also

- `Cerneala.Drawing.Color`
- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Aspect.ThemeTokenBridge`
