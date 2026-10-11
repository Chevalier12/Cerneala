# ButtonVariants Class

## Definition
Namespace: `Cerneala.UI.Controls.Buttons`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Buttons/ButtonVariants.cs`

Provides the built-in aspect variant keys used to select Cerneala `Button` styling by kind and size.

```csharp
public static class ButtonVariants
```

Inheritance:
`object` -> `ButtonVariants`

## Examples

Apply built-in button variants to a button instance:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;

Button button = new()
{
    Content = "Delete"
};

button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Danger);
button.SetAspectVariant(ButtonVariants.Size, ButtonSize.Small);
```

Use button variants in aspect rules:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Media;

AspectRuleSet dangerRule = new(
    "app.button.danger",
    AspectLayer.App,
    new AspectTarget(
        typeof(Button),
        conditions: [AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Danger)]),
    [
        new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(new Color(220, 38, 38)))),
        new AspectDeclaration(Control.ForegroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
    ],
    declarationOrder: 0);
```

## Remarks

`ButtonVariants` is a static catalog of strongly typed `AspectVariantKey<Button, TValue>` instances for button aspect selection. The class defines variant identities only; active values live on `Control.AspectVariants` and are changed with `Control.SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue>, TValue)`.

`Kind` holds a `ButtonKind` value (`Neutral`, `Primary`, `Danger`) and `Size` holds a `ButtonSize` value (`Small`, `Medium`, `Large`). The built-in keys are owned by `Button`, so they are intended for `Button` aspect conditions and button component-template rules.

The keys only name the variants. The default aspect package (`DefaultAspectPackage`) has no rule that reads `Kind` or `Size`, so with only the default package a `Danger` button looks the same as a `Neutral` one. The styling comes from rules you add, as in the second example. No variant value is set automatically: a new `Button` has an empty `AspectVariants` set, and `.crn` markup has no `Kind` or `Size` attribute on `Button`.

Aspect rules can match these keys with `AspectCondition.Variant`. A variant condition matches only when the current `AspectVariantSet` contains the key and the stored value equals the expected value. Changing a button variant through `SetAspectVariant` updates `AspectVariants` and invalidates aspect and render state when the value actually changes.

The key names are `kind` and `size`. Their diagnostic string representation comes from `AspectVariantKey.ToString()`, which formats labels such as `Button.kind` and `Button.size`.

## Fields

| Name | Type | Variant Name | Values |
| --- | --- | --- | --- |
| `Kind` | `AspectVariantKey<Button, ButtonKind>` | `kind` | `ButtonKind.Neutral`, `ButtonKind.Primary`, `ButtonKind.Danger` |
| `Size` | `AspectVariantKey<Button, ButtonSize>` | `size` | `ButtonSize.Small`, `ButtonSize.Medium`, `ButtonSize.Large` |

## Applies to

Cerneala UI aspect packages, button component templates, and button aspect condition matching.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Buttons.ButtonKind`
- `Cerneala.UI.Controls.Buttons.ButtonSize`
- `Cerneala.UI.Aspect.AspectVariantKey<TOwner, TValue>`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectVariantSet`
