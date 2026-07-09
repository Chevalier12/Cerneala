# MarkupResult&lt;T&gt; Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents the value produced by a markup operation together with diagnostics collected during that operation.

```csharp
public sealed class MarkupResult<T>
```

Inheritance:
`object` -> `MarkupResult<T>`

## Type Parameters
| Name | Description |
| --- | --- |
| `T` | The type of value produced by the markup operation. |

## Examples
The following example reads markup, checks whether the operation produced any error diagnostics, and then uses the parsed document value.

```csharp
using Cerneala.UI.Markup;

UiMarkupReader reader = new();
MarkupResult<UiMarkupDocument> result = reader.Read("<StackPanel><TextBlock Text=\"Hello\" /></StackPanel>");

if (!result.HasErrors && result.Value is not null)
{
    UiMarkupDocument document = result.Value;
}
```

The following example creates a failed result with an error diagnostic.

```csharp
using Cerneala.UI.Markup;

MarkupResult<string> result = new(
    null,
    [MarkupDiagnostic.Error("MARKUP010", "Markup document must contain a root node.")]);
```

## Remarks
`MarkupResult<T>` is used by markup APIs such as `UiMarkupReader.Read`, `UiMarkupWriter.Write`, `UiFactory.Create`, and `GeneratedUiFactory.Create` to return both an operation result and any diagnostics produced while building that result.

`Value` is nullable. A result can contain a `null` value when an operation fails or when a recovery path intentionally returns diagnostics without a usable value. Callers should check `HasErrors` and handle a `null` `Value` before using the result.

The constructor copies the supplied diagnostics into a read-only collection. Passing `null` for `diagnostics` produces an empty diagnostics list.

`HasErrors` is `true` when at least one diagnostic has `MarkupDiagnosticSeverity.Error`. Informational and warning diagnostics do not make `HasErrors` return `true`.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupResult(T? value, IEnumerable<MarkupDiagnostic>? diagnostics = null)` | Initializes a result with an optional value and optional diagnostics. When diagnostics are omitted, `Diagnostics` is empty. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Value` | `T?` | Gets the value produced by the markup operation. The value may be `null`. |
| `Diagnostics` | `IReadOnlyList<MarkupDiagnostic>` | Gets the diagnostics collected for the operation. |
| `HasErrors` | `bool` | Gets whether `Diagnostics` contains at least one diagnostic whose severity is `MarkupDiagnosticSeverity.Error`. |

## Applies to
Project: `Cerneala`

Markup namespace: `Cerneala.UI.Markup`

## See also
- `UiMarkupReader`
- `UiMarkupWriter`
- `UiFactory`
- `GeneratedUiFactory`
- `MarkupDiagnostic`
