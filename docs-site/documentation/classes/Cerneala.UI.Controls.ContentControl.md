# ContentControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ContentControl.cs`

Represents a control that stores a single content value and can host `UIElement` content as its logical and visual child.

```csharp
public class ContentControl : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl`

Derived:
`ButtonBase`, `Label`, `ListBoxItem`, `PopupRoot`, `ScrollContentPresenter`, `TabItem`

## Examples
Create an untemplated `ContentControl` that directly hosts a child element and measures it inside the inherited `Padding` and `BorderThickness` insets.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

ContentControl host = new()
{
    Padding = new Thickness(4),
    BorderThickness = new Thickness(1),
    Content = new UIElement()
};

LayoutSize desired = host.Measure(new MeasureContext(new LayoutSize(200, 100)));
host.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
```

Use a template when the content should be presented by another element, such as a `ContentPresenter`.

```csharp
using Cerneala.UI.Controls;

ContentControl host = new()
{
    Content = "Play"
};

host.ComponentTemplate = new ComponentTemplate<ContentControl>("ContentHost", context =>
{
    return new ContentPresenter
    {
        Content = context.Owner.Content
    };
});
```

## Remarks
`ContentControl` stores content in the `Content` UI property. The value can be any `object`; when the value is a `UIElement` and the control has no `ComponentTemplate`, the element is added to the control's logical and visual child collections.

When `ComponentTemplate` is set, `ContentControl` releases any directly owned content element from its subtree. A template can then decide how to present the content, commonly by binding or assigning it to a `ContentPresenter`.

Changing `Content` affects measure, render, and semantics. For `UIElement` content, replacement uses reference-oriented equality so two distinct elements are treated as different even if their `Equals` implementations return `true`. For non-element content, the default object equality comparer is used.

`ContentControl` validates `UIElement` content before ownership changes. It rejects self-parenting, adding an ancestor as a child, reparenting an element that already has a logical or visual parent, and attaching an element from another root. If a replacement fails, the previous content ownership is restored.

Without a template child, layout measures the hosted content inside `Padding + BorderThickness` and returns the content size inflated by those insets. Arrangement deflates the final bounds by the same insets before arranging the content element.

## Constructors
| Name | Description |
| --- | --- |
| `ContentControl()` | Initializes a new instance of `ContentControl`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | Identifies the `Content` UI property. The default value is `null`; metadata affects measure, render, and semantics. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Content` | `object?` | Gets or sets the stored content value. `UIElement` values may become direct logical and visual children when the control is untemplated. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource identifier used by templates that forward text rendering resources to presenters. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional resource provider used by templates that forward content rendering resources to presenters. |

## Methods
This class does not declare additional public methods.

## Events
This class does not declare additional public events.

## Property Information
| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Content` | `ContentProperty` | `null` | `AffectsMeasure`, `AffectsRender`, `AffectsSemantics` |

## Applies To
Project: `Cerneala`

UI area: retained controls, layout, templating, and content presentation.

## See Also
- `Control`
- `ContentPresenter`
- `ComponentTemplate`
- `UIElement`
