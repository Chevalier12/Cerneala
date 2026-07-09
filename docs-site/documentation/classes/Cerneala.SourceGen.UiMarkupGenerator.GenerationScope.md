# UiMarkupGenerator.GenerationScope Class

## Definition
Namespace: `Cerneala.SourceGen`

Assembly/Project: `Cerneala.SourceGen`

Source: `Cerneala.SourceGen/UiMarkupGenerator.cs`

Holds per-file source generation state while converting UI markup XML into generated C# statements.

```csharp
private sealed class GenerationScope
```

Inheritance:
`object` -> `GenerationScope`

## Examples

`GenerationScope` is private to `UiMarkupGenerator`. Public generator execution creates it internally while processing markup files:

```csharp
// Generated internally by UiMarkupGenerator while handling an additional .ui.xml file.
// The scope accumulates generated source lines and reports diagnostics for invalid markup.
```

## Remarks

`GenerationScope` stores the active `SourceProductionContext`, the markup source being processed, a generated variable id counter, emitted source `Lines`, and whether diagnostics have been reported.

`EmitElement` creates C# statements for supported markup elements such as `Panel`, `StackPanel`, `Border`, `Button`, and `TextBlock`. It emits supported properties, direct text content, and child relationships. Unsupported elements, unsupported properties, and invalid values are reported as diagnostics.

The class also parses literals for booleans, floats, thickness values, non-negative thickness values, and colors. String literal emission escapes control characters and quotes for generated C#.

## Constructors

| Signature | Description |
| --- | --- |
| `GenerationScope(SourceProductionContext context, MarkupSource file)` | Initializes per-file generation state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Lines` | `List<string>` | Gets the generated source lines accumulated for the markup file. |
| `HasErrors` | `bool` | Gets whether the scope has reported at least one diagnostic error. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EmitElement(XElement element)` | `string` | Emits C# construction and assignment lines for an element and returns its generated variable name. |

## Applies To

Cerneala UI markup source generation internals.

## See Also

- `Cerneala.SourceGen.UiMarkupGenerator`
