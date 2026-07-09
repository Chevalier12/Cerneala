# DefaultAspectTokens.Color Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Provides the built-in `DrawColor` aspect tokens used by Cerneala UI default styling.

```csharp
public static class DefaultAspectTokens
{
    public static class Color
}
```

Inheritance:
`object` -> `DefaultAspectTokens.Color`

## Examples

Read the default accent color from the environment created by `DefaultAspectPackage`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Color.Accent, out DrawColor accent))
{
    // accent is DrawColor(37, 99, 235).
}
```

Use a default color token as an aspect value reference:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    DefaultAspectTokens.Color.Surface.Ref());
```

## Remarks

`DefaultAspectTokens.Color` groups the framework's built-in color tokens. Each field is an `AspectToken<DrawColor>` created with `AspectToken.Color(string)`, so the token value type is `DrawColor`.

The class defines token identities only. Default values are assigned by `DefaultAspectPackage.Create()` when building the package token set and by `DefaultAspectPackage.CreateEnvironment()` when creating an `AspectEnvironment`.

The default package sets these values:

| Token | Token name | Default value |
| --- | --- | --- |
| `Background` | `color.background` | `new DrawColor(248, 250, 252)` |
| `Foreground` | `color.foreground` | `new DrawColor(28, 35, 48)` |
| `Surface` | `color.surface` | `new DrawColor(255, 255, 255)` |
| `Border` | `color.border` | `new DrawColor(148, 163, 184)` |
| `Accent` | `color.accent` | `new DrawColor(37, 99, 235)` |

In the default package rules, `DefaultAspectTokens.Color.Surface` is used for `Border` background values and `DefaultAspectTokens.Color.Border` is used for `Border` border color values.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `Background` | `AspectToken<DrawColor>` | Identifies the `color.background` token for general background color values. |
| `Foreground` | `AspectToken<DrawColor>` | Identifies the `color.foreground` token for general foreground color values. |
| `Surface` | `AspectToken<DrawColor>` | Identifies the `color.surface` token for surface color values. |
| `Border` | `AspectToken<DrawColor>` | Identifies the `color.border` token for border color values. |
| `Accent` | `AspectToken<DrawColor>` | Identifies the `color.accent` token for accent color values. |

## Applies to

Cerneala UI default aspect packages, aspect environments, token references, and theme-layer color resolution.

## See also

- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.Drawing.DrawColor`
