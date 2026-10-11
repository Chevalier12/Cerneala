# ButtonSize Enum

## Definition
Namespace: `Cerneala.UI.Controls.Buttons`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Buttons/ButtonSize.cs`

Names the size of a `Button`. It is the value type of the `ButtonVariants.Size` aspect variant key.

```csharp
public enum ButtonSize
```

## Examples

Mark a button as small and give small buttons less padding with an app rule:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Layout;

Button button = new() { Content = "OK" };
button.SetAspectVariant(ButtonVariants.Size, ButtonSize.Small);

AspectRuleSet smallRule = new(
    "app.button.small",
    AspectLayer.App,
    new AspectTarget(
        typeof(Button),
        conditions: [AspectCondition.Variant(ButtonVariants.Size, ButtonSize.Small)]),
    [new AspectDeclaration(Control.PaddingProperty, AspectValue<Thickness>.Literal(new Thickness(4)))],
    declarationOrder: 0);
```

## Remarks

`ButtonSize` is a plain value. It does not change a button by itself. A button gets a size only when code calls `Control.SetAspectVariant(ButtonVariants.Size, value)`, and the size changes the look only when an aspect rule matches it with `AspectCondition.Variant(ButtonVariants.Size, value)`.

The default aspect package (`DefaultAspectPackage`) has no rule that reads `ButtonVariants.Size`. With only the default package, `Small`, `Medium`, and `Large` buttons look the same.

No size is applied automatically. A new `Button` has an empty `AspectVariants` set, so a rule for `ButtonSize.Medium` does not match it until `SetAspectVariant` sets `Medium` explicitly.

`.crn` markup has no `Size` attribute on `Button`. Set the variant from C#.

## Values

| Name | Value | Description |
| --- | --- | --- |
| `Small` | `0` | Small button size. |
| `Medium` | `1` | Medium button size. |
| `Large` | `2` | Large button size. |

## Applies to

`Cerneala.UI.Controls.Button` aspect variants.

## See also

- `Cerneala.UI.Controls.Buttons.ButtonVariants`
- `Cerneala.UI.Controls.Buttons.ButtonKind`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Controls.Button`
