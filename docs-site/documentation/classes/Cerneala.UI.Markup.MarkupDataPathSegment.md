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
var segment = new MarkupDataPathSegment("Name", value => ((Person)value!).Name);
```

## Remarks
The getter is retained by generated markup infrastructure. A blank property name or `null` getter is rejected.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupDataPathSegment(string, Func<object?, object?>)` | Creates one named path segment. |

## Properties
| Name | Description |
| --- | --- |
| `PropertyName` | Name used for subscription and diagnostics. |

## Applies to
Compiled conditional markup.
