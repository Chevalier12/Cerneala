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
using Cerneala.UI.Controls.Templates;

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
`ContentControl` stores content in the `Content` UI property. The value can be any `object`; when the value is a `UIElement` and the control has neither a `ComponentTemplate` nor a `ComponentTemplateKey`, the element is added to the control's logical and visual child collections.

The hosted element follows the effective value of `ContentProperty`, whatever wrote it: the `Content` setter, or `SetValue` and `ClearValue` with any `UiPropertyValueSource`. For example, on an untemplated `host`, `host.SetValue(ContentControl.ContentProperty, (object?)child)` makes `child.LogicalParent` and `child.VisualParent` equal to `host`. A later `SetValue` with another element, or `host.ClearValue(ContentControl.ContentProperty)`, removes `child` from both collections again. When clearing one value source makes the element of a lower value source the effective `Content`, that element becomes the hosted child.

When `ComponentTemplate` or `ComponentTemplateKey` is set, `ContentControl` releases any directly owned content element from its subtree. A template can then decide how to present the content, commonly by binding or assigning it to a `ContentPresenter`.

A `ContentControl` can itself be the element that a `ContentPresenter`'s content template creates around `UIElement` content. When the control hosts that element directly and the presenter releases it, for example because its `ContentTemplate` changes, it sets the control's `Content` to `null` instead of only removing the child, so `Content` and the control's children stay in step. See `ContentPresenter` for details.

Changing `Content` affects measure, render, and semantics. For `UIElement` content, replacement uses reference-oriented equality so two distinct elements are treated as different even if their `Equals` implementations return `true`. For non-element content, the default object equality comparer is used.

`ContentControl` validates `UIElement` content before ownership changes. It rejects self-parenting, adding an ancestor as a child, reparenting an element that already has a logical or visual parent, and attaching an element from another root. If a replacement fails, the previous content ownership is restored. This also holds when the new element passes validation but hosting it fails, for example because its attach throws, whatever wrote the value: the `Content` setter, `SetValue`, or a `ClearValue` that makes a lower source's element the effective `Content`. The call throws, `Content` and its value source are again the previous ones, the previous element is again the logical and visual child, and the failed element has no parent. For example, on an untemplated `host` attached to a `UIRoot` with `Content = oldChild`, `host.SetValue(ContentControl.ContentProperty, (object?)failing)` with an element whose `OnAttached` throws rethrows that exception, and afterwards `host.Content` is `oldChild`, `host.LogicalChildren` and `host.VisualChildren` hold only `oldChild`, and `failing.LogicalParent` is `null`. The same validation runs for `SetValue(ContentControl.ContentProperty, value, source)`, whether the control hosts the element itself or not (for example when a `ComponentTemplate` or `ComponentTemplateKey` is set or the control is a `TabItem`), when the write becomes the effective `Content`, that is when no value source with higher precedence than `source` holds a value. It runs before `Content` changes: the call throws `InvalidOperationException` and `Content` keeps its previous value. For example, when `child` is already the child of a `Border`, `host.SetValue(ContentControl.ContentProperty, (object?)child)` on an untemplated `host` throws `InvalidOperationException`, `host.Content` keeps its previous value, and `child` stays in the `Border`. It also runs for `ClearValue(ContentControl.ContentProperty, source)` when the clear makes a value of a lower source the new `Content`: when that element got another parent after it was stored, for example `control.SetValue(ContentControl.ContentProperty, (object?)child, UiPropertyValueSource.AspectBase)` under a local `Content = "text"` and then `holder.Child = child`, `control.ClearValue(ContentControl.ContentProperty)` throws `InvalidOperationException`, and `Content` stays `"text"` with source `Local`. A write at a lower value source, for example `UiPropertyValueSource.AspectBase` or `UiPropertyValueSource.MarkupConditional` under a `Local` value, does not change `Content`, so it is stored without this validation, as a WPF style or trigger value shadowed by a local value.

```csharp
TabItem tab = new() { Content = "text" };      // Local value
Border element = new();
Border holder = new() { Child = element };     // element now has a parent

tab.SetValue(ContentControl.ContentProperty, (object?)element, UiPropertyValueSource.AspectBase);
// No exception: tab.Content is still "text" and element stays in holder.

tab.SetValue(ContentControl.ContentProperty, (object?)element);
// Throws InvalidOperationException: a Local write becomes the effective Content.
```

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
