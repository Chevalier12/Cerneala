# ThemeMotionTokens Class

## Definition
Namespace: `Cerneala.UI.Motion.States`

Assembly/Project: `Cerneala`

Source: `UI/Motion/States/ThemeMotionTokens.cs`

Defines the built-in theme keys and token names used to resolve shared motion specifications from a `ThemeProvider`.

```csharp
public static class ThemeMotionTokens
```

Inheritance:
`object` -> `ThemeMotionTokens`

## Examples

Resolve one of the default motion tokens from the default theme:

```csharp
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.States;
using Cerneala.UI.Theming;

ThemeProvider provider = new(DefaultTheme.Create());

MotionSpec enter = ThemeMotionTokens.Resolve(provider, ThemeMotionTokens.Enter);
MotionSpec layout = ThemeMotionTokens.Resolve(provider, ThemeMotionTokens.LayoutSpring);
```

Create a theme that overrides a built-in motion token:

```csharp
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.States;
using Cerneala.UI.Theming;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

MotionTokens motion = ThemeMotionTokens.CreateDefault()
    .Set(ThemeMotionTokens.FastOut, MotionFactory.Tween(TimeSpan.FromMilliseconds(90), Easings.Standard));

Theme theme = DefaultTheme.Create()
    .Set(ThemeMotionTokens.Key, motion);
```

## Remarks

`ThemeMotionTokens` is a static catalog for theme-level motion specifications. It does not store the active theme value itself; `DefaultTheme.Create()` registers the `MotionTokens` instance returned by `CreateDefault()` under `ThemeMotionTokens.Key`.

The string constants are token names used inside a `MotionTokens` registry. `Resolve` first reads the current `MotionTokens` value from the supplied `ThemeProvider`, then returns the `MotionSpec` registered for the requested token name.

`CreateDefault()` returns a mutable `MotionTokens` instance populated with untyped tween and spring specifications. The caller can use `Set` to replace individual token values before storing the instance in a `Theme`.

`Resolve` throws `ArgumentNullException` when `provider` is null. It also propagates lookup errors from `ThemeProvider.Get` when the theme does not contain `Key`, and from `MotionTokens.Get` when the token name is invalid or missing.

## Fields

| Name | Type | Value | Description |
| --- | --- | --- | --- |
| `Instant` | `string` | `"Instant"` | Names the near-instant linear tween token. |
| `FastOut` | `string` | `"FastOut"` | Names the fast outward tween token. |
| `FastIn` | `string` | `"FastIn"` | Names the fast inward tween token. |
| `Standard` | `string` | `"Standard"` | Names the standard tween token. |
| `Emphasized` | `string` | `"Emphasized"` | Names the emphasized tween token. |
| `GentleSpring` | `string` | `"GentleSpring"` | Names the gentle spring token. |
| `SnappySpring` | `string` | `"SnappySpring"` | Names the higher-stiffness spring token. |
| `LayoutSpring` | `string` | `"LayoutSpring"` | Names the spring token intended for layout motion. |
| `Enter` | `string` | `"Enter"` | Names the default entrance transition token. |
| `Exit` | `string` | `"Exit"` | Names the default exit transition token. |
| `Key` | `ThemeKey<MotionTokens>` | `new("MotionTokens")` | Identifies the `MotionTokens` value stored in a `Theme`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateDefault()` | `MotionTokens` | Creates a mutable token registry populated with the built-in motion specifications. |
| `Resolve(ThemeProvider provider, string tokenName)` | `MotionSpec` | Gets the theme's `MotionTokens` registry and returns the specification registered for `tokenName`. |

## Default Tokens

| Token | Default Specification |
| --- | --- |
| `Instant` | `Motion.Tween(TimeSpan.FromMilliseconds(1), Easings.Linear)` |
| `FastOut` | `Motion.Tween(TimeSpan.FromMilliseconds(120), Easings.Standard)` |
| `FastIn` | `Motion.Tween(TimeSpan.FromMilliseconds(120), Easings.EaseIn)` |
| `Standard` | `Motion.Tween(TimeSpan.FromMilliseconds(180), Easings.Standard)` |
| `Emphasized` | `Motion.Tween(TimeSpan.FromMilliseconds(240), Easings.Emphasized)` |
| `GentleSpring` | `Motion.Spring(stiffness: 420, damping: 36)` |
| `SnappySpring` | `Motion.Spring(stiffness: 700, damping: 44)` |
| `LayoutSpring` | `Motion.Spring(stiffness: 520, damping: 38)` |
| `Enter` | `Motion.Tween(TimeSpan.FromMilliseconds(180), Easings.EaseOut)` |
| `Exit` | `Motion.Tween(TimeSpan.FromMilliseconds(140), Easings.EaseIn)` |

## Applies to

Cerneala UI themes, default theme creation, and motion APIs that resolve named `MotionSpec` instances from theme state.

## See also

- `Cerneala.UI.Motion.States.MotionTokens`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Theming.Theme`
- `Cerneala.UI.Theming.ThemeProvider`
- `Cerneala.UI.Theming.DefaultTheme`
