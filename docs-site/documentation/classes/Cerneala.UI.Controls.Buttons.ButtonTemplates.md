# ButtonTemplates Class

## Definition
Namespace: `Cerneala.UI.Controls.Buttons`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Buttons/ButtonTemplates.cs`

Provides built-in component templates for `Button` controls.

```csharp
public static class ButtonTemplates
```

Inheritance:
`object` -> `ButtonTemplates`

## Examples

Assign the built-in modern component template directly to a button:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Layout;

Button button = new()
{
    Content = "Save",
    ComponentTemplate = ButtonTemplates.Modern,
    Padding = new Thickness(14, 9, 14, 9)
};
```

Register the template in an aspect package so it can be selected with component-template rules:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;

AspectPackage package = AspectPackage.Create("App")
    .Components(components =>
    {
        components.AddTemplate(new ComponentTemplateDefinition(
            "App.Button.Modern",
            typeof(Button),
            ButtonTemplates.Modern));
    });
```

## Remarks

`ButtonTemplates` is a static catalog for reusable button component templates. The current catalog exposes `Modern`, a `ComponentTemplate<Button>` named `Button.Modern`.

`Modern` creates a `Border` root and a nested `ContentPresenter`. The content presenter receives the owning button's `ResourceProvider` and `FontResourceId`, then the template records `ButtonSlots.Root` for the border and `ButtonSlots.Content` for the content presenter. It also registers the content presenter as the required `PART_Content` template part.

The template binds `Background`, `BorderBrush`, `BorderThickness`, and `Padding` from the owner button to the border with local target value source. It binds `ContentControl.Content` from the button to `ContentPresenter.Content`, binds the button `Foreground` to the content presenter, and binds the button's `HorizontalContentAlignment` and `VerticalContentAlignment` to the presenter's `HorizontalAlignment` and `VerticalAlignment`, so the content is centered by default, as in WPF's default button. These bindings let aspect rules that target the button and local button values drive the visual tree created by the component template. The `Content`, `Foreground`, and alignment bindings use the default `TemplateOwnerBinding` source, so a rule that targets `ButtonSlots.Content` cannot override those presenter properties; it still sets the presenter's other properties. To move the content, set `HorizontalContentAlignment` or `VerticalContentAlignment` on the button.

`DefaultAspectPackage.Create()` registers this template as `Button.Modern`. The playground modern-aspect sample also registers the same template and demonstrates targeting `ButtonSlots.Content` from aspect rules.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `Modern` | `ComponentTemplate<Button>` | Gets the built-in modern button component template. The template creates a `Border` root, a `ContentPresenter` content part, registers button slots, and binds the button chrome, content, and content-alignment properties into the template tree. |

## Applies to

Project: `Cerneala`

UI area: button component templates, default aspect package registration, and aspect slot targeting.

## See also

- `UI/Controls/Buttons/ButtonTemplates.cs`
- `UI/Controls/Button.cs`
- `UI/Controls/Buttons/ButtonSlots.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Aspect/DefaultAspectPackage.cs`
