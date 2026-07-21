# PrismSourceSpan Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismSourceSpan.cs`

Identifies an optional source location attached to a Prism composition or node definition.

```csharp
public readonly record struct PrismSourceSpan
```

## Examples

```csharp
using Cerneala.UI.Prism.Definitions;

PrismSourceSpan span = new(
    start: 24,
    length: 7,
    sourceName: "Card.cui.xml");

Console.WriteLine(span); // Card.cui.xml@24+7
```

## Remarks

`Start` and `Length` are non-negative source coordinates supplied by the caller. `SourceName` is optional but cannot be empty or whitespace when present.

`ToString()` uses `sourceName@start+length`. When no source name is available, it uses `<source>` as the name. Framework diagnostics include this form when reporting an authoring location.

The span is diagnostic metadata. Definition constructors store it without making it part of Prism structural equality or hashing.

## Constructors

| Name | Description |
| --- | --- |
| `PrismSourceSpan(int start, int length, string? sourceName = null)` | Creates and validates an immutable source span. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Start` | `int` | Gets the non-negative source start coordinate. |
| `Length` | `int` | Gets the non-negative span length. |
| `SourceName` | `string?` | Gets the optional source document name. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns `sourceName@start+length`, using `<source>` when unnamed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismSourceSpan(...)` | `ArgumentOutOfRangeException` | `start` or `length` is negative. |
| `PrismSourceSpan(...)` | `ArgumentException` | A non-null `sourceName` is empty or whitespace. |

## Applies to

Cerneala Prism definition authoring and retained graph diagnostics.

## See also

- `Cerneala.UI.Prism.Definitions.PrismNodeDefinition`
- `Cerneala.UI.Prism.Definitions.PrismCompositionDefinition`
