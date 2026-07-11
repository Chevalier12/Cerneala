# DefaultAspectTokens Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Provides the built-in semantic aspect tokens used by the default Cerneala UI aspect package.

```csharp
public static class DefaultAspectTokens
```

Inheritance:
`object` -> `DefaultAspectTokens`

## Examples

Use a default token as the value source for an aspect declaration:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectRuleSet borderRule = new(
    "app.border",
    AspectLayer.App,
    new AspectTarget(typeof(Border)),
    [new AspectDeclaration(Control.BorderColorProperty, DefaultAspectTokens.Color.Border.Ref())],
    priority: 0);
```

Read a default token value from the default environment:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Color.Accent, out Color accent))
{
    // accent is the default accent color, Color(37, 99, 235).
}
```

Override one of the built-in tokens in an application package:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(DefaultAspectTokens.Color.Accent, new Color(79, 70, 229)))
    .Build();
```

## Remarks

`DefaultAspectTokens` is a static catalog of named, strongly typed `AspectToken<T>` instances. It does not store values by itself. Values are provided by an `AspectPackage`, an `AspectEnvironment`, or another token source.

`DefaultAspectPackage.Create()` registers default values for these tokens in the built-in package. `DefaultAspectPackage.CreateEnvironment()` creates an environment named `default` and sets matching values for runtime resolution.

The token names are stable semantic names such as `color.background`, `spacing.control-padding`, and `motion.normal`. Rules can refer to the tokens through `AspectToken<T>.Ref()` so the final value is read from the active `AspectEnvironment` during aspect resolution.

The default package uses several of these tokens directly: button rules use `Stroke.ControlBorderThickness`, border rules use `Color.Surface` and `Color.Border`, and the default environment provides values for all tokens listed below.

## Nested Classes

| Name | Description |
| --- | --- |
| `Color` | Groups semantic color tokens backed by `Color` values. |
| `Typography` | Groups text styling tokens for font family and font size. |
| `Spacing` | Groups spacing tokens backed by `Thickness` values. |
| `Stroke` | Groups border and stroke sizing tokens backed by `Thickness` values. |
| `Motion` | Groups motion timing tokens backed by `MotionSpec` values. |

## Fields

### Color

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Color.Background` | `AspectToken<Color>` | `color.background` | `new Color(248, 250, 252)` |
| `Color.Foreground` | `AspectToken<Color>` | `color.foreground` | `new Color(28, 35, 48)` |
| `Color.Surface` | `AspectToken<Color>` | `color.surface` | `new Color(255, 255, 255)` |
| `Color.Border` | `AspectToken<Color>` | `color.border` | `new Color(148, 163, 184)` |
| `Color.Accent` | `AspectToken<Color>` | `color.accent` | `new Color(37, 99, 235)` |

### Typography

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Typography.FontFamily` | `AspectToken<string>` | `typography.font-family` | `"Default"` |
| `Typography.FontSize` | `AspectToken<float>` | `typography.font-size` | `16f` |

### Spacing

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Spacing.ControlPadding` | `AspectToken<Thickness>` | `spacing.control-padding` | `new Thickness(8)` |

### Stroke

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Stroke.ControlBorderThickness` | `AspectToken<Thickness>` | `stroke.control-border-thickness` | `new Thickness(1)` |

### Motion

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Motion.Fast` | `AspectToken<MotionSpec>` | `motion.fast` | `new TweenSpec<float>(TimeSpan.FromMilliseconds(120))` |
| `Motion.Normal` | `AspectToken<MotionSpec>` | `motion.normal` | `new TweenSpec<float>(TimeSpan.FromMilliseconds(200))` |

## Applies to

Cerneala UI aspect packages, aspect environments, component rules, and theme-style token resolution.

## See also

- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Controls.Control`
