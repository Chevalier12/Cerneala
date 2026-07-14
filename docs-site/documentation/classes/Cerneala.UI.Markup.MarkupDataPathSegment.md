# MarkupDataPathSegment Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`

Describes one property step in a generated markup data-path observation.

```csharp
public sealed class MarkupDataPathSegment
```

## Examples
```csharp
var segment = new MarkupDataPathSegment(
    "Name",
    owner => ((Person)owner!).Name,
    (owner, value) => ((Person)owner!).Name = (string?)value);
```

## Remarks
The getter is retained by generated markup infrastructure and is used for typed
path traversal without reflection. The overload with a setter is emitted only
for a writable terminal segment. Intermediate segments remain read-only.

A blank property name, `null` getter, or `null` setter is rejected. The
two-argument constructor remains available for read-only observations.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupDataPathSegment(string, Func<object?, object?>)` | Creates one named path segment. |
| `MarkupDataPathSegment(string, Func<object?, object?>, Action<object?, object?>)` | Creates a named path segment with a typed terminal setter for generated write-back. |

## Properties
| Name | Description |
| --- | --- |
| `PropertyName` | Name used for subscription and diagnostics. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Both constructors | `ArgumentNullException` | `propertyName` or `getter` is `null`. |
| Both constructors | `ArgumentException` | `propertyName` is empty or whitespace. |
| `MarkupDataPathSegment(string, Func<object?, object?>, Action<object?, object?>)` | `ArgumentNullException` | `setter` is `null`. |

## Applies to
Source-generated markup data paths.

## See Also
- `Cerneala.UI.Markup.GeneratedMarkup`
- `Cerneala.UI.Markup.MarkupObservation`
- `docs/markup-data-bindings.md`
