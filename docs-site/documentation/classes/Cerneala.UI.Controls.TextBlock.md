# TextBlock Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBlock.cs`

Displays a single text string, measures it through the text layout pipeline, and renders it with the control font and foreground settings.

```csharp
public class TextBlock : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBlock`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

TextBlock label = new()
{
    Text = "Ready",
    FontFamily = "Default",
    FontSize = 18,
    Foreground = Color.White,
    TextWrapping = TextWrapping.NoWrap
};

LayoutSize desired = label.Measure(new MeasureContext(new LayoutSize(200, 40)));
```

## Remarks

`TextBlock` builds a `TextAspect` from `FontFamily`, `FontSize`, `Foreground`, `TextWrapping`, `TextTrimming`, and `FontResourceId`. During measurement it passes the current text, aspect, and available width to a `TextMeasurer`; during rendering it draws the same text at the render bounds origin through a `TextRenderer`.

The `Text` value is never stored as `null`. Assigning `null` through the property or `TextProperty` is coerced to `string.Empty`. Empty text still measures through the text measurer, but `OnRender` exits without emitting a text draw command.

By default the control uses `TextMeasurer.Default` and `TextRenderer.Default`. Supplying `FontResourceId` together with a local `ResourceProvider` or root resource provider makes the control use a resource-backed `FontResolver` and an internal text layout cache. When a font resource dependency is recorded through `ResourceDependencyTracker` or the root tracker, resource changes invalidate measure and render.

`TextWrapping.Wrap` allows measurement to respect the available width. `TextTrimming` is part of the layout key, but the current enum only exposes `TextTrimming.None`; the implementation does not document or claim ellipsis trimming behavior.

Changing text, wrapping, font resource, resource provider, or text measurer invalidates measure and render. Changing the text renderer invalidates render only. Changing inherited `Foreground` invalidates render without changing the text layout identity.

## Constructors

| Name | Description |
| --- | --- |
| `TextBlock()` | Initializes a new `TextBlock` with default control and text property values. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the `Text` property. Defaults to `string.Empty`, coerces `null` to empty text, and affects measure, render, and semantics. |
| `TextWrappingProperty` | `UiProperty<TextWrapping>` | Identifies the `TextWrapping` property. Defaults to `TextWrapping.NoWrap`, validates enum values, and affects measure and render. |
| `TextTrimmingProperty` | `UiProperty<TextTrimming>` | Identifies the `TextTrimming` property. Defaults to `TextTrimming.None`, validates enum values, and affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the displayed text. `null` assignments become `string.Empty`. |
| `TextWrapping` | `TextWrapping` | Gets or sets whether text is measured as `NoWrap` or `Wrap`. |
| `TextTrimming` | `TextTrimming` | Gets or sets trimming behavior. The only current value is `TextTrimming.None`. |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the measurer used when no font resource provider path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates measure and render. |
| `TextRenderer` | `TextRenderer` | Gets or sets the renderer used when no font resource provider path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates render. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource id used for resource-backed measurement and rendering. Changing it clears the internal resource text layout cache and invalidates measure and render. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional local resource provider. If unset, the control can use `Root.ResourceProvider`. Changing it clears the internal resource text layout cache and invalidates measure and render. |
| `ResourceDependencyTracker` | `ResourceDependencyTracker?` | Gets or sets the optional local dependency tracker. If unset, the control can use `Root.ResourceDependencyTracker`. |

## Relevant Inherited Properties

| Name | Declared On | Description |
| --- | --- | --- |
| `FontFamily` | `Control` | Inherited text font family. Defaults to `"Default"`, inherits through the UI property system, and affects measure and render. |
| `FontSize` | `Control` | Inherited text size. Defaults to `16`, must be positive and finite, and affects measure and render. |
| `Foreground` | `Control` | Inherited text color. Defaults to `Color.Black`, inherits through the UI property system, and affects render. |

## Methods

This class does not declare public methods.

## Events

This class does not declare public events.

## Applies to

`Cerneala.UI.Controls.TextBlock` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextRenderer`
- `Cerneala.UI.Text.TextWrapping`
- `Cerneala.UI.Text.TextTrimming`
