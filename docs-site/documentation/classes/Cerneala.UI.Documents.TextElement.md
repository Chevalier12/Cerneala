# TextElement Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/TextElement.cs`

Defines the base class of the inline content of a `TextBlock`, with the font family, size, weight, style, stretch and foreground that an inline can set for its text.

```csharp
public abstract class TextElement : UiObject, IUiPropertyOwner
```

Inheritance:
`object` -> `UiObject` -> `TextElement`

Derived:
`Inline` -> `Run`, `Span` (-> `Bold`, `Italic`), `LineBreak`

Implements:
`IUiPropertyOwner`

## Examples

A property that an inline does not set comes from the `Span` that holds it, or from the `TextBlock`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new() { FontFamily = "Segoe UI", FontSize = 16 };
text.Inlines.Add(new Run("top "));

Span big = new() { FontSize = 30 };
big.Inlines.Add(new Run("big and bold") { FontWeight = FontWeight.Bold });
text.Inlines.Add(big);

// "top "          : Segoe UI, 16, Normal (from the TextBlock)
// "big and bold"  : Segoe UI, 30, Bold   (size from the Span, weight from the Run)
```

The same in markup:

```xml
<TextBlock FontFamily="Segoe UI" FontSize="16">top <Span FontSize="30"><Run Text="big and bold" FontWeight="Bold"/></Span></TextBlock>
```

## Remarks

`TextElement` does not draw. It holds the values; `TextBlock` reads them when it builds the text spans for measure and render.

Each property is resolved down the tree: the effective value of an inline is the value that it sets, or else the value of the `Span` above it, or else the value of the `TextBlock`. The default value of a property on the element itself (`"Default"`, `16`, `Normal`, and `null` for `Foreground`) is only what the getter returns; it does not override the parent. An element counts as setting a property when it has a local value, a style or trigger value, or a framework default. The framework default is how `Bold` has `FontWeight.Bold` and `Italic` has `FontStyle.Italic`.

Writing a value that equals the default, such as `run.FontSize = 16`, is a local value: from then on the run keeps size 16 even if the `TextBlock` has size 20. `ClearValue(TextElement.FontSizeProperty)` makes the run inherit again.

A change of `FontFamily`, `FontSize`, `FontWeight`, `FontStyle` or `FontStretch` invalidates measure and render of the owning `TextBlock`. A change of `Foreground` invalidates render only. An element that is not in the inline tree of a `TextBlock` changes nothing but its own value.

Validation: `FontFamily` rejects `null`, empty and white-space values, `FontSize` must be finite and greater than zero, and the three enum properties reject undefined values. Each of them throws `ArgumentException`.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `FontFamilyProperty` | `UiProperty<string>` | Identifies `FontFamily`. Defaults to `"Default"`; affects measure and render. |
| `FontSizeProperty` | `UiProperty<float>` | Identifies `FontSize`. Defaults to `16`; affects measure and render. |
| `FontWeightProperty` | `UiProperty<FontWeight>` | Identifies `FontWeight`. Defaults to `FontWeight.Normal`; affects measure and render. |
| `FontStyleProperty` | `UiProperty<FontStyle>` | Identifies `FontStyle`. Defaults to `FontStyle.Normal`; affects measure and render. |
| `FontStretchProperty` | `UiProperty<FontStretch>` | Identifies `FontStretch`. Defaults to `FontStretch.Normal`; affects measure and render. |
| `ForegroundProperty` | `UiProperty<Brush?>` | Identifies `Foreground`. Defaults to `null`; affects render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FontFamily` | `string` | Gets or sets the font family of the text of this element. |
| `FontSize` | `float` | Gets or sets the font size of the text of this element. |
| `FontWeight` | `FontWeight` | Gets or sets the weight of the font. |
| `FontStyle` | `FontStyle` | Gets or sets the slant of the font. |
| `FontStretch` | `FontStretch` | Gets or sets the width of the font. |
| `Foreground` | `Brush?` | Gets or sets the brush of the text and of its decorations. When the element does not set it, the brush of the parent is used. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnPropertyInvalidated(UiPropertyChangedEventArgs args, UiPropertyOptions options)` | `void` | Implements `IUiPropertyOwner`. Tells the owning `TextBlock` that a property changed, so it invalidates itself. Throws `ArgumentNullException` when `args` is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Inline`
- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.Run`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Core.IUiPropertyOwner`
