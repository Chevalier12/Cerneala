# ButtonTokens Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ButtonAspects.cs`

Provides the built-in aspect tokens used to style Cerneala `Button` controls.

```csharp
public static class ButtonTokens
```

Inheritance:
`object` -> `ButtonTokens`

## Examples

Use a button token as an aspect declaration value source:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectRuleSet buttonRule = new(
    "app.button",
    AspectLayer.App,
    new AspectTarget(typeof(Button)),
    [new AspectDeclaration(Control.BackgroundProperty, ButtonTokens.Background.Ref())],
    priority: 0);
```

Read a button token value from the default environment:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(ButtonTokens.HoverBackground, out DrawColor hoverBackground))
{
    // hoverBackground is the default hover background, DrawColor(37, 99, 235).
}
```

Override button tokens in an application package:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens =>
    {
        tokens.Set(ButtonTokens.Background, new DrawColor(24, 24, 27));
        tokens.Set(ButtonTokens.Foreground, DrawColor.White);
        tokens.Set(ButtonTokens.Padding, new Thickness(12, 6, 12, 6));
    })
    .Build();
```

## Remarks

`ButtonTokens` is a static catalog of strongly typed `AspectToken<T>` instances for button-specific colors, opacity, and padding. The class defines token identities only; values are supplied by an `AspectPackage`, an `AspectEnvironment`, or another token source.

`DefaultAspectPackage.Create()` registers default values for these tokens. `DefaultAspectPackage.CreateEnvironment()` creates a matching environment named `default` and sets the same values for runtime aspect resolution.

The default button aspect rule resolves `Background`, `Foreground`, `BorderColor`, and `Padding` into `Control` properties through `AspectToken<T>.Ref()`. `HoverBackground`, `PressedBackground`, and `DisabledOpacity` are available as named button state tokens in the default token set.

The token names use the `button.*` prefix, such as `button.background`, `button.hover-background`, and `button.disabled-opacity`.

## Fields

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Background` | `AspectToken<DrawColor>` | `button.background` | `new DrawColor(255, 255, 255)` |
| `Foreground` | `AspectToken<DrawColor>` | `button.foreground` | `new DrawColor(28, 35, 48)` |
| `BorderColor` | `AspectToken<DrawColor>` | `button.border` | `new DrawColor(148, 163, 184)` |
| `HoverBackground` | `AspectToken<DrawColor>` | `button.hover-background` | `new DrawColor(37, 99, 235)` |
| `PressedBackground` | `AspectToken<DrawColor>` | `button.pressed-background` | `new DrawColor(148, 163, 184)` |
| `DisabledOpacity` | `AspectToken<float>` | `button.disabled-opacity` | `0.5f` |
| `Padding` | `AspectToken<Thickness>` | `button.padding` | `new Thickness(8)` |

## Applies to

Cerneala UI aspect packages, aspect environments, component rules, and button token resolution.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.ButtonTemplates`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
