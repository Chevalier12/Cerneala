# UiMarkupSchema Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: [UI/Markup/UiMarkupSchema.cs](https://github.com/Chevalier12/Cerneala/blob/master/UI/Markup/UiMarkupSchema.cs)

Creates the default runtime markup type registry for the built-in retained UI elements supported by `UiFactory`.

```csharp
public static class UiMarkupSchema
```

Inheritance:
`object` -> `UiMarkupSchema`

## Examples
The default schema is typically passed to `UiFactory` so parsed markup can be converted into a retained UI tree.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

MarkupResult<UiMarkupDocument> documentResult = new UiMarkupReader().Read(
    "<StackPanel><TextBlock Text=\"One\" /><Button>Two</Button></StackPanel>");

UiFactory factory = new(UiMarkupSchema.CreateDefault());
MarkupResult<UIElement> result = factory.Create(documentResult.Value!);

StackPanel panel = (StackPanel)result.Value!;
Button button = (Button)panel.VisualChildren[1];
object? content = button.Content; // "Two"
```

## Remarks
`UiMarkupSchema` is a small bootstrap class. `CreateDefault` returns a new `UiMarkupTypeRegistry` and registers the element names, factories, child handling, content properties, and property converters used by the runtime markup loader.

The default schema registers these markup elements:

| Markup element | Created type | Child handling | Content property |
| --- | --- | --- | --- |
| `Panel` | `Panel` | Adds child elements to `LogicalChildren` and `VisualChildren`. | None |
| `StackPanel` | `StackPanel` | Adds child elements to `LogicalChildren` and `VisualChildren`. | None |
| `Border` | `Border` | Assigns the child to `Border.Child`. | `Child` |
| `Button` | `Button` | Assigns the child to `Button.Content`. | `Content` |
| `RepeatButton` | `RepeatButton` | Assigns the child to `RepeatButton.Content`. | `Content` |
| `TextBlock` | `TextBlock` | Does not accept child elements. | `Text` |

The schema also registers these shared element/control properties. The `Elements` column lists the built-in elements that accept each property:

| Property | Target type | Elements | Value conversion |
| --- | --- | --- | --- |
| `IsEnabled` | `UIElement` | All elements above | `bool.TryParse`; invalid values throw `FormatException`. |
| `IsVisible` | `UIElement` | All elements above | `bool.TryParse`; invalid values throw `FormatException`. |
| `Margin` | `UIElement` | All elements above | One float, or four comma-separated floats, parsed with invariant culture. |
| `Background` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | Color shorthand converted to `SolidColorBrush`, a brush resource, or a composite brush property element. |
| `Foreground` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | A color converted to `SolidColorBrush`, or `$Name` for a brush resource. |
| `BorderBrush` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | A color converted to `SolidColorBrush`, or `$Name` for a brush resource. |
| `BorderThickness` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | One float, or four comma-separated floats, parsed with invariant culture. |
| `Padding` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | One float, or four comma-separated floats, parsed with invariant culture. |
| `FontFamily` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | Raw string value. |
| `FontSize` | `Control` | `Border`, `Button`, `RepeatButton`, `TextBlock` | Single-precision float parsed with invariant culture. |

`Panel` and `StackPanel` are not `Control` types, so the schema does not register the `Control` properties for them. `UiFactory.Create` reports such an attribute as an unknown property (`MARKUP023`). For example, `<StackPanel Background="Red" />` creates no element in strict mode and reports `MARKUP023` with the message `Unknown markup property 'StackPanel.Background'.`. A `<StackPanel.Background>` property element is reported as `MARKUP028`.

The schema registers these element-specific properties. Each one is accepted only on the element that the `Property` column names:

| Property | Value conversion |
| --- | --- |
| `Border.CornerRadius` | One float, which creates a uniform `CornerRadius`, or four comma-separated floats: top-left, top-right, bottom-right, and bottom-left. Floats use invariant culture. For example, `<Border CornerRadius="8,0,0,8" />` rounds only the left corners. |
| `Button.Content` | Raw string value assigned to `Button.Content`. |
| `RepeatButton.Content` | Raw string value assigned to `RepeatButton.Content`. |
| `RepeatButton.Delay` | `int` parsed with invariant culture, in milliseconds. `RepeatButton.Delay` rejects negative values. |
| `RepeatButton.Interval` | `int` parsed with invariant culture, in milliseconds. `RepeatButton.Interval` rejects `0` and negative values. |
| `TextBlock.Text` | Raw string value assigned to `TextBlock.Text`. |

For example, `<RepeatButton Content="x" Delay="300" Interval="50" />` creates a `RepeatButton` with `Delay == 300` and `Interval == 50`. A value that does not convert, such as `Delay="1.5"`, or that the property rejects, such as `Interval="0"`, is reported as `MARKUP027`, and so is a `CornerRadius` with two or three values. `<Button CornerRadius="8" />` is reported as an unknown property (`MARKUP023`).

Text content is accepted only when the element registration has a matching content property registration.

Color values use `Color.TryParse`: any WPF named color, matched without regard to case (for example `Red` or `cornflowerblue`), `#RRGGBB`, `#AARRGGBB`, or three or four comma-separated bytes: red, green, blue, and optional alpha. When alpha is omitted, the schema uses `255`. A brush value that starts with `$` looks up the brush resource with the rest of the name; a missing resource throws `FormatException`, which `UiFactory.Create` reports as `MARKUP027`.

Thickness values must use either one value, which creates uniform thickness, or four comma-separated values: left, top, right, and bottom. Floating-point values use `CultureInfo.InvariantCulture`.

Parsing and assignment failures are surfaced by `UiFactory` as markup diagnostics. For example, unknown elements and properties are reported separately from invalid typed values produced by this schema's converters.

## Methods
| Name | Description |
| --- | --- |
| `CreateDefault()` | Creates and returns a new `UiMarkupTypeRegistry` populated with the built-in element and property registrations. |

## Applies to
Cerneala retained UI markup loading through `UiMarkupReader`, `UiMarkupTypeRegistry`, and `UiFactory`.

## See also
- [UiMarkupTypeRegistry.cs](https://github.com/Chevalier12/Cerneala/blob/master/UI/Markup/UiMarkupTypeRegistry.cs)
- [UiFactory.cs](https://github.com/Chevalier12/Cerneala/blob/master/UI/Markup/UiFactory.cs)
- [UiMarkupReader.cs](https://github.com/Chevalier12/Cerneala/blob/master/UI/Markup/UiMarkupReader.cs)
