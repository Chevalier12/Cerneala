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

ThemeKey<DrawColor> accentKey = DefaultTheme.AccentKey;
AspectToken<DrawColor> accentToken = ThemeTokenBridge.ToToken(accentKey);

// accentToken.Name is "theme.Accent".
```

Create an aspect environment from the default theme and read projected color tokens:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Theming;

Theme theme = DefaultTheme.Create();
AspectEnvironment environment = ThemeTokenBridge.CreateEnvironment(theme);

if (environment.TryGet(ThemeTokenBridge.ToToken(DefaultTheme.BackgroundKey), out DrawColor background))
{
    // Use the theme background color in aspect resolution or template binding.
}
```

## Remarks

`ThemeTokenBridge` is the adapter between the theming API in `Cerneala.UI.Theming` and the aspect token environment in `Cerneala.UI.Aspect`. It does not mutate the source `Theme`; it creates aspect tokens and fills a new `AspectEnvironment` with values that can be consumed by component templates and aspect resolution.

`ToToken<T>` preserves the theme key value type and creates an `AspectToken<T>` named with the `theme.` prefix followed by `ThemeKey<T>.Key`. For example, `DefaultTheme.AccentKey` maps to `theme.Accent`.

`CreateEnvironment` creates an environment named after `Theme.Name`, or `theme` when the theme is unnamed. It projects the default color theme keys: `BackgroundKey`, `ForegroundKey`, `SurfaceKey`, `BorderKey`, and `AccentKey`. Values are added only when the source theme contains a compatible value for that key.

`Control.ApplyTemplate` uses this bridge when a control has a root theme provider, passing the resulting environment into `ComponentTemplateContext`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToToken<T>(ThemeKey<T> key)` | `AspectToken<T>` | Creates a typed aspect token named `theme.` plus the theme key text. |
| `CreateEnvironment(Theme theme)` | `AspectEnvironment` | Creates a new aspect environment from the supplied theme and projects the default color theme values into it. Throws `ArgumentNullException` when `theme` is `null`. |

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
