# Label Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Label.cs`

Represents a concrete `ContentControl` type used for label content in the retained UI control tree.

```csharp
public class Label : ContentControl
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `Label`

## Examples

Create a label that directly hosts a `TextBlock` as its content:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Label label = new()
{
    Padding = new Thickness(4, 2, 4, 2),
    Content = new TextBlock
    {
        Text = "Username"
    }
};

LayoutSize desired = label.Measure(new MeasureContext(new LayoutSize(200, 40)));
label.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
```

## Remarks

`Label` does not declare additional behavior beyond `ContentControl`. It exists as a distinct control type while using the inherited content storage, ownership, layout, templating, and resource-forwarding behavior from `ContentControl` and `Control`.

When `Content` is a `UIElement` and the label has no classic `Template`, the content element is owned directly as a logical and visual child. The inherited `ContentControl` layout path measures that element inside the inherited `Padding + BorderThickness` insets and arranges it inside the deflated final bounds.

When `Template` is set, the label releases directly owned content from its subtree and the template is responsible for presenting the content, commonly with a `ContentPresenter`.

`Label` does not implement WPF-style target, access-key, or mnemonic behavior in its source. Treat it as a named content host unless a template or external behavior adds more semantics.

## Constructors

| Name | Description |
| --- | --- |
| `Label()` | Initializes a new instance of `Label`. |

## Fields

This class does not declare additional public fields.

## Properties

This class does not declare additional public properties.

## Methods

This class does not declare additional public methods.

## Events

This class does not declare additional public events.

## Important Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | `ContentControl` | Identifies the inherited `Content` UI property. The default value is `null`; metadata affects measure, render, and semantics. |

## Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `ContentControl` | Gets or sets the label content value. `UIElement` values may become direct logical and visual children while the label is untemplated. |
| `FontResourceId` | `ResourceId<FontResource>?` | `ContentControl` | Gets or sets the optional font resource identifier used by templates that forward text rendering resources to presenters. |
| `ResourceProvider` | `IResourceProvider?` | `ContentControl` | Gets or sets the optional resource provider used by templates that forward content rendering resources to presenters. |
| `Padding` | `Thickness` | `Control` | Gets or sets the inset between the label bounds and directly hosted content. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets border thickness that contributes to inherited content insets. |
| `Template` | `ControlTemplate?` | `Control` | Gets or sets the classic control template. When present, the template child handles layout and content presentation. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template, when component-template rendering is used. |

## Important Inherited Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `ApplyTemplate()` | `void` | `Control` | Creates or refreshes the active template instance for the current template properties. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Content` | `ContentProperty` | `null` | `AffectsMeasure`, `AffectsRender`, `AffectsSemantics` |

## Applies to

Project: `Cerneala`

UI area: retained controls, layout, templating, and content presentation.

## See also

- `UI/Controls/Label.cs`
- `UI/Controls/ContentControl.cs`
- `UI/Controls/ContentPresenter.cs`
- `UI/Controls/TextBlock.cs`
