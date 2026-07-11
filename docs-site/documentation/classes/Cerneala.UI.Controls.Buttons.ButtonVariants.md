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

AspectRuleSet dangerRule = new(
    "app.button.danger",
    AspectLayer.App,
    new AspectTarget(
        typeof(Button),
        conditions: [AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Danger)]),
    [
        new AspectDeclaration(Control.BackgroundProperty, AspectValue<Color>.Literal(new Color(220, 38, 38))),
        new AspectDeclaration(Control.ForegroundProperty, AspectValue<Color>.Literal(Color.White))
    ],
    priority: 0);
```

## Remarks

`ButtonVariants` is a static catalog of strongly typed `AspectVariantKey<Button, TValue>` instances for button aspect selection. The class defines variant identities only; active values live on `Control.AspectVariants` and are changed with `Control.SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue>, TValue)`.

`Kind` uses `ButtonKind` values to distinguish neutral, primary, and danger button styling. `Size` uses `ButtonSize` values to distinguish small, medium, and large button styling. The built-in keys are owned by `Button`, so they are intended for `Button` aspect conditions and button component-template rules.

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
