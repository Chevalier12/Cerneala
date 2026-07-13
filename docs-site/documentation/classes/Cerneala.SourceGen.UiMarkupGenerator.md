# UiMarkupGenerator Class

## Definition

Namespace: `Cerneala.SourceGen`

Assembly/Project: `Cerneala.SourceGen`

Source: `Cerneala.SourceGen/UiMarkupGenerator.cs`

Implements an incremental Roslyn source generator that converts `.cui.xml` UI markup additional files into typed Cerneala UI factory classes.

```csharp
[Generator]
public sealed class UiMarkupGenerator : IIncrementalGenerator
```

Inheritance:
`object` -> `UiMarkupGenerator`

Implements:
`Microsoft.CodeAnalysis.IIncrementalGenerator`

Attributes:
`Microsoft.CodeAnalysis.GeneratorAttribute`

## Examples

A `.cui.xml` additional file named `Sample.cui.xml` can define a supported UI tree:

```xml
<StackPanel>
  <TextBlock Text="Hello" FontSize="18" />
  <Button Content="Go" />
</StackPanel>
```

The generator emits a factory type in the `Cerneala.GeneratedUi` namespace. The generated `Create` method returns the root `UIElement`:

```csharp
global::Cerneala.UI.Elements.UIElement root =
    Cerneala.GeneratedUi.SampleFactory.Create();
```

For a file named `typed-view.cui.xml`, the generated type name is `TypedViewFactory`.

## Remarks

`UiMarkupGenerator` reads compiler additional text files whose paths end with `.cui.xml`, ignoring case. Each markup file is parsed as XML and emitted as one generated static partial factory class under the `Cerneala.GeneratedUi` namespace.

Generated factories contain:

| Member | Description |
| --- | --- |
| `Create()` | Builds and returns the root `global::Cerneala.UI.Elements.UIElement`. |
| `AsGeneratedFactory()` | Returns a `global::Cerneala.UI.Markup.GeneratedUiFactory` that wraps `Create`. |

The generated factory class name is based on the markup file name without the `.cui.xml` suffix, converted to a valid identifier and suffixed with `Factory`. Duplicate base names are disambiguated with the parent directory name, then with a stable FNV-1a hash if needed.

Reactive directive expressions support the lowercase `and` and `or` operators plus parentheses. Their precedence is `comparison` before `and` before `or`; parentheses override the default order.

```xml
<TextBlock Text="Idle">
  @when IsEnabled and (IsMouseOver or IsKeyboardFocusWithin)
  {
    Text = "Active";
  }
</TextBlock>
```

An `@if` expression may combine typed comparisons and reactive operands:

```xml
@when $DataContext.Value
{
  @if value >= $DataContext.Minimum and value <= $DataContext.Maximum
  {
    Text = "In range";
  }
}
```

Evaluation short-circuits, while every syntactic source is still observed. Compound `@when` expressions require Boolean source leaves, and `value` inside their `@if` blocks is the Boolean result of the complete expression. The directive language does not accept `not`, `&&`, `||`, or arbitrary C# expressions. This is a source-generator language change only and does not add or modify a public runtime API.

Supported root and child elements:

| Markup element | Generated type |
| --- | --- |
| `Panel` | `global::Cerneala.UI.Controls.Panel` |
| `StackPanel` | `global::Cerneala.UI.Controls.StackPanel` |
| `Border` | `global::Cerneala.UI.Controls.Border` |
| `Button` | `global::Cerneala.UI.Controls.Button` |
| `TextBlock` | `global::Cerneala.UI.Controls.TextBlock` |

Supported child relationships:

| Parent element | Generated relationship |
| --- | --- |
| `Panel` | Adds the child to `LogicalChildren` and `VisualChildren`. |
| `StackPanel` | Adds the child to `LogicalChildren` and `VisualChildren`. |
| `Border` | Assigns the child to `Child`. |
| `Button` | Assigns the child to `Content`. |

Supported attributes:

| Attribute | Applies to | Value handling |
| --- | --- | --- |
| `Text` | `TextBlock` | Emits a C# string literal. |
| `Content` | `Button` | Emits a C# string literal. |
| `IsEnabled` | Any supported element | Requires a Boolean value. |
| `IsVisible` | Any supported element | Requires a Boolean value. |
| `Margin` | Any supported element | Requires one float or four comma-separated floats. |
| `Background` | `Border`, `Button`, `TextBlock` | Accepts a color shorthand converted to `SolidColorBrush`, a brush resource, or a composite brush property element. |
| `Foreground` | `Border`, `Button`, `TextBlock` | Requires a supported color value. |
| `BorderBrush` | `Border`, `Button`, `TextBlock` | Accepts a color shorthand converted to `SolidColorBrush`, a brush resource, or a composite brush property element. |
| `BorderThickness` | `Border`, `Button`, `TextBlock` | Requires one non-negative float or four comma-separated non-negative floats. |
| `Padding` | `Border`, `Button`, `TextBlock` | Requires one non-negative float or four comma-separated non-negative floats. |
| `FontFamily` | `Border`, `Button`, `TextBlock` | Requires a non-whitespace string. |
| `FontSize` | `Border`, `Button`, `TextBlock` | Requires a positive finite float. |

Color attributes accept the named values `Transparent`, `White`, and `Black`, ignoring case. They also accept comma-separated byte components in `R, G, B` or `R, G, B, A` form.

Direct text content is supported for `TextBlock` and `Button`. For `TextBlock`, direct text sets `Text`; for `Button`, it sets `Content`. Direct text on other supported elements is reported as an unsupported `#text` property.

The generator reports diagnostics instead of emitting source when markup cannot be processed successfully:

| Diagnostic ID | Condition |
| --- | --- |
| `CERNEALAUI001` | The markup file is malformed XML or has no root element. |
| `CERNEALAUI002` | The markup contains an unsupported element. |
| `CERNEALAUI003` | The markup contains an unsupported property, text content, or child relationship. |
| `CERNEALAUI004` | The markup contains an invalid value for a supported property. |

Diagnostics are created with locations from XML line information when it is available.

## Constructors

| Signature | Description |
| --- | --- |
| `UiMarkupGenerator()` | Initializes a new generator instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Initialize(IncrementalGeneratorInitializationContext context)` | `void` | Registers incremental source output for collected `.cui.xml` additional files. |

## Applies To

Cerneala source generation project targeting `netstandard2.0`.

## See Also

- `Cerneala.SourceGen.UiMarkupGenerator.GenerationScope`
- `Cerneala.UI.Markup.GeneratedUiFactory`
