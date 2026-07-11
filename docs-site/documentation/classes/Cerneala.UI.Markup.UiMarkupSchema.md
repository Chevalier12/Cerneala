# UiMarkupSchema Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: [UI/Markup/UiMarkupSchema.cs](../../UI/Markup/UiMarkupSchema.cs)

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
| `TextBlock` | `TextBlock` | Does not accept child elements. | `Text` |

The schema also registers these shared element/control properties for every built-in element above:

| Property | Target type | Value conversion |
| --- | --- | --- |
| `IsEnabled` | `UIElement` | `bool.TryParse`; invalid values throw `FormatException`. |
| `IsVisible` | `UIElement` | `bool.TryParse`; invalid values throw `FormatException`. |
| `Margin` | `UIElement` | One float, or four comma-separated floats, parsed with invariant culture. |
| `Background` | `Control` | Named color or comma-separated byte color. |
| `Foreground` | `Control` | Named color or comma-separated byte color. |
| `BorderColor` | `Control` | Named color or comma-separated byte color. |
| `BorderThickness` | `Control` | One float, or four comma-separated floats, parsed with invariant culture. |
| `Padding` | `Control` | One float, or four comma-separated floats, parsed with invariant culture. |
| `FontFamily` | `Control` | Raw string value. |
| `FontSize` | `Control` | Single-precision float parsed with invariant culture. |

`Button.Content` and `TextBlock.Text` are registered as element-specific properties. Text content is accepted only when the element registration has a matching content property registration.

Color values accept `Transparent`, `White`, or `Black`, using case-insensitive matching against `Color` names. Numeric color values must be three or four comma-separated bytes: red, green, blue, and optional alpha. When alpha is omitted, the schema uses `255`.

Thickness values must use either one value, which creates uniform thickness, or four comma-separated values: left, top, right, and bottom. Floating-point values use `CultureInfo.InvariantCulture`.

Parsing and assignment failures are surfaced by `UiFactory` as markup diagnostics. For example, unknown elements and properties are reported separately from invalid typed values produced by this schema's converters.

## Methods
| Name | Description |
| --- | --- |
| `CreateDefault()` | Creates and returns a new `UiMarkupTypeRegistry` populated with the built-in element and property registrations. |

## Applies to
Cerneala retained UI markup loading through `UiMarkupReader`, `UiMarkupTypeRegistry`, and `UiFactory`.

## See also
- [UiMarkupTypeRegistry.cs](../../UI/Markup/UiMarkupTypeRegistry.cs)
- [UiFactory.cs](../../UI/Markup/UiFactory.cs)
- [UiMarkupReader.cs](../../UI/Markup/UiMarkupReader.cs)
