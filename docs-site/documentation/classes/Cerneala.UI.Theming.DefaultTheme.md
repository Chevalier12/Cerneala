# DefaultTheme Class

## Definition
Namespace: `Cerneala.UI.Theming`

Assembly/Project: `Cerneala`

Source: `UI/Theming/DefaultTheme.cs`

Provides built-in theme keys, creates the default theme values, and creates the default button template.

```csharp
public static class DefaultTheme
```

Inheritance:
`object` -> `DefaultTheme`

## Examples

Create the default theme and read its palette-backed colors:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Theming;

Theme theme = DefaultTheme.Create();
ThemePalette palette = theme.Get(DefaultTheme.PaletteKey);

Color background = theme.Get(DefaultTheme.BackgroundKey);
Color accent = theme.Get(DefaultTheme.AccentKey);
```

Assign the default button template:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Theming;

Button button = new()
{
    Content = "Save",
    ComponentTemplate = DefaultTheme.CreateButtonTemplate()
};
```

## Remarks

`DefaultTheme` is a convenience entry point for the built-in theme surface. `Create()` returns a theme named `Default` with a `ThemePalette`, individual color entries for background, foreground, surface, border, and accent, and the default theme motion tokens from `ThemeMotionTokens.CreateDefault()`.

The default palette uses these color values:

| Theme key | Value |
| --- | --- |
| `BackgroundKey` | `Color(248, 250, 252)` |
| `ForegroundKey` | `Color(28, 35, 48)` |
| `SurfaceKey` | `Color(255, 255, 255)` |
| `BorderKey` | `Color(148, 163, 184)` |
| `AccentKey` | `Color(37, 99, 235)` |

`PaletteKey` stores the full `ThemePalette`. The individual color keys are also set from that palette so callers can read either the complete palette or one typed color value.

`CreateButtonTemplate()` creates a `ComponentTemplate<Button>` with a `Border` root and a nested `ContentPresenter`. The template binds the button's background, border color, border thickness, padding, content, foreground, font family, and font size into those template elements.

`ThemeTokenBridge.CreateEnvironment` projects the default color keys into aspect tokens when a theme is converted to an `AspectEnvironment`.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `PaletteKey` | `ThemeKey<ThemePalette>` | Identifies the complete default theme palette. |
| `BackgroundKey` | `ThemeKey<Color>` | Identifies the default background color. |
| `ForegroundKey` | `ThemeKey<Color>` | Identifies the default foreground color. |
| `SurfaceKey` | `ThemeKey<Color>` | Identifies the default surface color. |
| `BorderKey` | `ThemeKey<Color>` | Identifies the default border color. |
| `AccentKey` | `ThemeKey<Color>` | Identifies the default accent color. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create()` | `Theme` | Creates the built-in theme named `Default`, including palette, individual color values, and default theme motion tokens. |
| `CreateButtonTemplate()` | `ComponentTemplate<Button>` | Creates a button template whose root is a `Border` containing a `ContentPresenter` bound to button chrome, content, foreground, and font properties. |

## Applies to

Cerneala UI theming, default palette values, theme-to-aspect projection, and button templates.

## See also

- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
- `Cerneala.UI.Theming.ThemePalette`
- `Cerneala.UI.Aspect.ThemeTokenBridge`
- `Cerneala.UI.Motion.States.ThemeMotionTokens`
- `Cerneala.UI.Controls.Button`
