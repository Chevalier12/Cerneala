# ButtonSlots Class

## Definition
Namespace: `Cerneala.UI.Controls.Buttons`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Buttons/ButtonSlots.cs`

Provides the named aspect slots used by the built-in button component template.

```csharp
public static class ButtonSlots
```

Inheritance:
`object` -> `ButtonSlots`

## Examples

Target the button content slot from an aspect rule:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;

AspectRuleSet focusedContentForeground = new(
    "example.button.focus-content",
    AspectLayer.Runtime,
    new AspectTarget(
        typeof(ContentPresenter),
        ButtonSlots.Content,
        [AspectCondition.State(AspectState.Focus)]),
    [new AspectDeclaration(Control.ForegroundProperty, AspectValue<Color>.Literal(Color.White))],
    declarationOrder: 0);
```

Register the built-in button slots in a component template:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;

ComponentTemplate<Button> template = new("Button.Custom", context =>
{
    ContentPresenter presenter = new();
    Border border = new() { Child = presenter };

    context.RegisterSlot(ButtonSlots.Root, border);
    context.RegisterSlot(ButtonSlots.Content, presenter);

    return border;
});
```

## Remarks

`ButtonSlots` centralizes the slot identifiers used by `ButtonTemplates.Modern`. The template registers `Root` for the outer `Border` and `Content` for the inner `ContentPresenter`.

Aspect rules can use these slots through `AspectTarget` to address template elements without depending on local variables from the template factory. For example, a rule whose target is `new AspectTarget(typeof(Button), ButtonSlots.Content)` applies its declarations to the `ContentPresenter` that the default `ButtonTemplates` registers for that slot. Properties that the template binds from the button keep the button's value: `Content` and `Foreground` on the presenter, and `Background`, `BorderBrush`, `BorderThickness`, and `Padding` on the root border. Set those on the button instead.

Each field is an `AspectSlot<Button, TTarget>`, so the slot carries both the owning control type and the expected target element type. Registration is performed through `ComponentTemplateContext.RegisterSlot`, which stores the slot and element in the template instance's `TemplateSlotMap`.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `Root` | `AspectSlot<Button, Border>` | Identifies the root `Border` slot of the built-in button template. |
| `Content` | `AspectSlot<Button, ContentPresenter>` | Identifies the content presenter slot of the built-in button template. |

## Applies to

Cerneala UI button component templates, aspect targets, and template slot maps.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Buttons.ButtonTemplates`
- `Cerneala.UI.Aspect.AspectSlot`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Controls.Templates.ComponentTemplateContext`
