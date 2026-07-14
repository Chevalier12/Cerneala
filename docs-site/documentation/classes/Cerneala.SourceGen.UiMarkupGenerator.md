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

### Source-Generated Data Bindings

Property attributes accept a typed source path with an optional final mode:

```text
source-path[:OneWay|TwoWay]
```

`OneWay` is the default. Supported sources are `$DataContext.Path`,
`$element.Property`, `$self.Property`,
`$control.parts.$part.Property`, and `$owner.Property` inside a component
template. The generator resolves every segment and endpoint through Roslyn and
emits typed access; it does not evaluate string property paths or use reflection
at runtime.

```xml
<StackPanel DataType="EditorViewModel">
  <TextBlock Text="$DataContext.Name" />
  <TextBlock Text="User: $DataContext.Name, count: $DataContext.Count" />
  <TextBox Text="$DataContext.Name:TwoWay" />
</StackPanel>
```

`$DataContext` paths require a root `DataType`, except on paired generic
`Window<TViewModel>` and `UserControl<TViewModel>` documents, which infer the
type. Every CLR owner along a reactive path must implement
`INotifyPropertyChanged`. UI-property sources use Cerneala property change
notifications instead.

CLR `INotifyPropertyChanged` notifications may arrive from worker threads once
the generated target is attached. The runtime coalesces them per generated
binding or condition controller and reevaluates the complete typed path on the
target root's Relay; no path getter or target property is touched on the worker.
Direct `UiObject.PropertyChanged` notifications remain UI-thread-only because
attached Cerneala property mutations enforce root affinity before raising them.

A `OneWay` binding to a string target converts the source with the current
culture and maps `null` to an empty string. String attributes and quoted
directive strings may interpolate multiple paths; interpolation is always
`OneWay`, rejects fragment modes, deduplicates repeated paths, and uses `\$` as
the literal-dollar escape.

Directive assignment bindings are written unquoted and end with `;`:

```text
Text = $DataContext.Name;
Text = $DataContext.Name:TwoWay;
Text = "Hello, $DataContext.Name";
```

Only the provider selected by the winning conditional rule remains active.
Reactive condition sources themselves are read-only and reject mode suffixes.
All syntactic leaves stay observed even though generated Boolean evaluation
short-circuits.

Bindings stop on detach and refresh on reattach. Bindings created by a
component-template factory are disposed with the template instance. Source
notifications consumed by a binding must be raised on the binding's captured
UI/update thread; the runtime fails fast instead of implicitly marshaling them.

See `docs/markup-data-bindings.md` for the complete grammar, name-scope rules,
null and cascade behavior, diagnostics, and unsupported features.

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
| `CERNEALAUI007` | A binding or reactive source has invalid syntax, scope, mode, type, accessibility, observability, or writability. |

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
- `Cerneala.UI.Markup.GeneratedMarkup`
- `docs/markup-data-bindings.md`
