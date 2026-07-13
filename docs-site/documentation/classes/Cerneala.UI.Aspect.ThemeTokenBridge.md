# ThemeTokenBridge Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/ThemeTokenBridge.cs`

Projects selected theme keys into aspect tokens and creates an aspect environment from a `Theme`.

```csharp
public static class ThemeTokenBridge
```

Inheritance:
`object` -> `ThemeTokenBridge`

## Examples

Convert a theme key into a matching aspect token:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Theming;

ThemeKey<Color> accentKey = DefaultTheme.AccentKey;
AspectToken<Color> accentToken = ThemeTokenBridge.ToToken(accentKey);

// accentToken.Name is "theme.Accent".
```

Create an aspect environment from the default theme and read projected color tokens:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Theming;

Theme theme = DefaultTheme.Create();
AspectEnvironment environment = ThemeTokenBridge.CreateEnvironment(theme);

if (environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BackgroundKey), out Color background))
{
    // Use the theme background color in aspect resolution or template binding.
}
```

## Remarks

`ThemeTokenBridge` is the adapter between the theming API in `Cerneala.UI.Theming` and the aspect token environment in `Cerneala.UI.Aspect`. It does not mutate the source `Theme`; it creates aspect tokens and fills a new `AspectEnvironment` with values that can be consumed by component templates and aspect resolution.

`ToToken<T>` preserves the theme key value type and creates an `AspectToken<T>` named with the `theme.` prefix followed by `ThemeKey<T>.Key`. For example, `DefaultTheme.AccentKey` maps to `theme.Accent`.

`CreateEnvironment` creates an environment named after `Theme.Name`, or `theme` when the theme is unnamed. It projects `BackgroundKey`, `ForegroundKey`, `SurfaceKey`, `BorderKey`, and `AccentKey` into their `theme.*` tokens and the matching `DefaultAspectTokens.Color` entries. Background, foreground, surface, and border colors are also converted to `SolidColorBrush` values for the corresponding semantic brush tokens.

Default button tokens are derived from the same palette: surface supplies the normal background, foreground supplies text, border supplies border and pressed background, and accent supplies hover background. Root aspect processing applies this projection over package defaults whenever the active theme changes. Controls and their template token bindings consume the processor's shared runtime environment, so both aspect declarations and templates observe the same palette.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToToken<T>(ThemeKey<T> key)` | `AspectToken<T>` | Creates a typed aspect token named `theme.` plus the theme key text. |
| `CreateEnvironment(Theme theme)` | `AspectEnvironment` | Creates a new aspect environment containing typed theme tokens plus the corresponding semantic color, brush, and default button tokens. Throws `ArgumentNullException` when `theme` is `null`. |

## Applies to

Cerneala UI theme projection, aspect environments, component templates, and controls that resolve themed aspect tokens.

## See also

- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Theming.DefaultTheme`
- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeKey<T>`
