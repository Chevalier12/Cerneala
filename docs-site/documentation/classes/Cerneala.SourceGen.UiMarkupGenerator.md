# UiMarkupGenerator Class

## Definition

Namespace: `Cerneala.SourceGen`

Assembly/Project: `Cerneala.SourceGen`

Source: `Cerneala.SourceGen/UiMarkupGenerator.cs`

Implements an incremental Roslyn source generator that converts `.crn` UI markup additional files into typed Cerneala UI factory classes.

```csharp
[Generator]
public sealed partial class UiMarkupGenerator : IIncrementalGenerator
```

Inheritance:
`object` -> `UiMarkupGenerator`

Implements:
`Microsoft.CodeAnalysis.IIncrementalGenerator`

Attributes:
`Microsoft.CodeAnalysis.GeneratorAttribute`

## Examples

A `.crn` additional file named `Sample.crn` can define a supported UI tree:

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

For a file named `typed-view.crn`, the generated type name is `TypedViewFactory`.

## Remarks

`UiMarkupGenerator` reads compiler additional text files whose paths end with `.crn`, ignoring case. Other additional files are ignored. Each file is parsed into the shared `Cerneala.Language` document and analyzed in strict `Build` mode; a file with error diagnostics does not emit generated source.

The shared language layer owns lossless parsing, recovery, source spans, binding,
resource, template, Aspect, Motion, and Prism semantics, plus the host-agnostic
`CERNEALAUI*` diagnostics. The generator owns incremental Roslyn orchestration,
converts common diagnostics to Roslyn diagnostics, and lowers validated semantic
results to C#. Recovery support is available to editor-agnostic consumers, but it
does not make incomplete saved markup valid at build and does not itself expose an
LSP service.

An ordinary markup file produces a static partial factory under the `Cerneala.GeneratedUi` namespace. A root `DataType` adds typed `Create(dataContext)` and `AsGeneratedFactory(dataContext)` overloads. All ordinary factories expose:

| Member | Description |
| --- | --- |
| `Create()` | Builds and returns the root `global::Cerneala.UI.Elements.UIElement`. |
| `AsGeneratedFactory()` | Returns a `global::Cerneala.UI.Markup.GeneratedUiFactory` that wraps `Create`. |

The generated factory class name is based on the markup file name without the `.crn` suffix, converted to a valid identifier and suffixed with `Factory`. Duplicate base names are disambiguated with the parent directory name, then with a stable FNV-1a hash if needed. Files paired with compatible `Application`, `Window`, or `UserControl` partial declarations follow their corresponding generated startup or control path instead of the ordinary standalone factory path.

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
`$element.Property`, `$self.Property`, `$root.Property`,
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

`$root.Property` binds to a UI property declared by the document root. It keeps
view dataflow in markup without requiring a `Name` on a paired `UserControl` or
`Window` wrapper, and it reacts to subsequent root-property changes:

```xml
<UserControl>
  <ItemsControl ItemsSource="$root.Rows" />
</UserControl>
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
component-template factory are disposed with the template instance. CLR
`INotifyPropertyChanged` notifications may be raised on worker threads; the
runtime queues refresh work through the target root's Relay and does not read
the path or write the target on the worker. Direct Cerneala UI-property
notifications remain UI-thread-only.

See `docs/markup-data-bindings.md` for the complete grammar, name-scope rules,
null and cascade behavior, diagnostics, and unsupported features.

### Markup Shapes

The generator resolves built-in controls and imported public CLR types rather than using a fixed five-element list. A custom element must be qualified through a `clr-namespace` XML namespace alias; an unqualified custom type is rejected. Container relationships are resolved from the target type: panels receive logical and visual children, decorators receive `Child`, and content controls receive `Content`.

The current source and regression tests cover, among others:

| Markup element | Generated type |
| --- | --- |
| `Panel` and `StackPanel` | Logical and visual child collections |
| `Border` and other decorators | `Child` assignment |
| `Button` and other content controls | `Content` assignment |
| `ItemsControl` and derived controls | Inline `ContentTemplate`, `ItemTemplate`, and `ItemsPanel` markup |
| Imported CLR controls | Type resolution through a `clr-namespace` alias |

Inline content templates have exactly one visual root, may declare `DataType`, `Name`, `Key`, and `Priority`, and use the template item as the `$DataContext` source. Tests cover nested `DataContext` scopes, typed `INotifyPropertyChanged` paths, retargeting after an intermediate object changes, and `TwoWay` write-back from a generated control.

Supported generated value categories include:

| Category | Examples |
| --- | --- |
| Scalar literals | `bool`, integer, `float`, `double`, `decimal`, enum, and finite positive values where the target property requires them |
| Layout values | `Thickness` and `LayoutPoint`, including comma-separated forms |
| Drawing values | Named or hexadecimal colors, byte color components, and brush resources/property elements |
| Generated content | Direct text for content-bearing controls and `ContentTemplate` declarations |
| Reactive values | Typed `$DataContext`, `$element`, `$self`, `$root`, `$control.parts.$part`, and `$owner` paths, including `OneWay` and `TwoWay` modes where the endpoint is writable |

Color values accept the named colors known by the generator, ignoring case. They also accept hexadecimal colors and comma-separated byte components in `R, G, B` or `R, G, B, A` form.

Direct text content is assigned to the target's supported text or content property. Direct text on an element without such a property is reported as an unsupported `#text` property.

The generator reports diagnostics instead of emitting source when markup cannot be processed successfully:

| Diagnostic ID | Condition |
| --- | --- |
| `CERNEALAUI001` | The markup file is malformed XML or has no root element. |
| `CERNEALAUI002` | The markup contains an unsupported element. |
| `CERNEALAUI003` | The markup contains an unsupported property, text content, or child relationship. |
| `CERNEALAUI004` | The markup contains an invalid value for a supported property. |
| `CERNEALAUI005` | The document shape is invalid, including invalid resource or template placement. |
| `CERNEALAUI006` | A markup directive is invalid. |
| `CERNEALAUI007` | A binding or reactive source has invalid syntax, scope, mode, type, accessibility, observability, or writability. |
| `CERNEALAUI008` | A `UserControl` declaration is invalid. |
| `CERNEALAUI009` | A markup event handler is invalid. |
| `CERNEALAUI010` | A `Window` declaration is invalid. |
| `CERNEALAUI011` | Generated `Window` startup is invalid. |
| `CERNEALAUI012` | A component template declaration is invalid. |
| `CERNEALAUI013` | An `Application` declaration is invalid. |
| `CERNEALAUI014` | Application startup is invalid. |
| `CERNEALAUI020`-`CERNEALAUI026` | Motion syntax, target, event, type, composition, lifecycle, or runtime capability is invalid. |

Diagnostics use exact source spans from the shared syntax and semantic model and
are converted to Roslyn source locations by the generator host adapter.

## Constructors

| Signature | Description |
| --- | --- |
| `UiMarkupGenerator()` | Initializes a new generator instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Initialize(IncrementalGeneratorInitializationContext context)` | `void` | Registers incremental source output for collected `.crn` additional files. |

## Applies To

Cerneala source generation project targeting `netstandard2.0`.

## See Also

- `Cerneala.SourceGen.UiMarkupGenerator.GenerationScope`
- `Cerneala.SourceGen.UiMarkupGenerator.MarkupSource`
- `Cerneala.UI.Markup.GeneratedUiFactory`
- `Cerneala.UI.Markup.GeneratedMarkup`
- `docs/markup-data-bindings.md`
