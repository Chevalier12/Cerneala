# ButtonKind Enum

## Definition
Namespace: `Cerneala.UI.Controls.Buttons`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Buttons/ButtonKind.cs`

Names the kind of a `Button`. It is the value type of the `ButtonVariants.Kind` aspect variant key.

```csharp
public enum ButtonKind
```

## Examples

Mark a button as a danger button and style that kind with an app rule:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Media;

Button deleteButton = new() { Content = "Delete" };
deleteButton.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Danger);

AspectRuleSet dangerRule = new(
    "app.button.danger",
    AspectLayer.App,
    new AspectTarget(
        typeof(Button),
        conditions: [AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Danger)]),
    [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(new Color(220, 38, 38))))],
    declarationOrder: 0);
```

## Remarks

`ButtonKind` is a plain value. It does not change a button by itself. A button gets a kind only when code calls `Control.SetAspectVariant(ButtonVariants.Kind, value)`, and the kind changes the look only when an aspect rule matches it with `AspectCondition.Variant(ButtonVariants.Kind, value)`.

The default aspect package (`DefaultAspectPackage`) has no rule that reads `ButtonVariants.Kind`. With only the default package, `Neutral`, `Primary`, and `Danger` buttons look the same. Add your own rules, as in the example, to give each kind its own style.

No kind is applied automatically. A new `Button` has an empty `AspectVariants` set, so a rule with `AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Neutral)` does not match it until `SetAspectVariant` sets `Neutral` explicitly.

`.crn` markup has no `Kind` attribute on `Button`. Set the variant from C#.

## Values

| Name | Value | Description |
| --- | --- | --- |
| `Neutral` | `0` | Neutral button kind. |
| `Primary` | `1` | Primary button kind, for the main action. |
| `Danger` | `2` | Danger button kind, for destructive actions. |

## Applies to

`Cerneala.UI.Controls.Button` aspect variants.

## See also

- `Cerneala.UI.Controls.Buttons.ButtonVariants`
- `Cerneala.UI.Controls.Buttons.ButtonSize`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Controls.Button`
