# ThemePalette Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/ThemePalette.cs`

Stores the core `DrawColor` values that make up a Cerneala UI theme palette.

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
    new DrawColor(248, 250, 252),
    new DrawColor(28, 35, 48),
    new DrawColor(255, 255, 255),
    new DrawColor(148, 163, 184),
    new DrawColor(37, 99, 235));

Theme theme = new Theme("Custom").Set(DefaultTheme.PaletteKey, palette);
```

Read the built-in palette from the default theme:

```csharp
using Cerneala.UI.Theming;

Theme theme = DefaultTheme.Create();
ThemePalette palette = theme.Get(DefaultTheme.PaletteKey);
```

## Remarks

`ThemePalette` is an immutable container for the five core colors used by the default theme surface: background, foreground, surface, border, and accent. The constructor assigns each supplied `DrawColor` to a read-only property.

`DefaultTheme.Create()` creates a `ThemePalette`, stores it under `DefaultTheme.PaletteKey`, and also stores each palette color under the matching individual theme key. `ThemeTokenBridge.CreateEnvironment` projects those individual color keys into aspect tokens.

## Constructors

| Name | Description |
| --- | --- |
| `ThemePalette(DrawColor background, DrawColor foreground, DrawColor surface, DrawColor border, DrawColor accent)` | Initializes a palette with the supplied background, foreground, surface, border, and accent colors. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Background` | `DrawColor` | Gets the default background color. |
| `Foreground` | `DrawColor` | Gets the default foreground color. |
| `Surface` | `DrawColor` | Gets the default surface color. |
| `Border` | `DrawColor` | Gets the default border color. |
| `Accent` | `DrawColor` | Gets the default accent color. |

## Applies to

Cerneala UI theming and palette-backed default color resources.

## See also

- `Cerneala.Drawing.DrawColor`
- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Aspect.ThemeTokenBridge`
