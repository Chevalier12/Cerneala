# UiMarkupAttribute Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents a parsed markup attribute with name, value, and optional source location.

```csharp
public sealed record UiMarkupAttribute
```

Inheritance:
`Object` -> `UiMarkupAttribute`

## Examples

Create an attribute and attach it to a markup node.

```csharp
using Cerneala.UI.Markup;

UiMarkupAttribute width = new("Width", "120", line: 3, column: 15);
UiMarkupNode node = new("Button", attributes: new[] { width });
```

## Remarks

`UiMarkupAttribute` stores the parsed name and value for an attribute, plus optional line and column information from the source markup.

The constructor rejects null, empty, or whitespace-only names. `Value` is stored as supplied by the caller.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupAttribute(string, string, int?, int?)` | Initializes a markup attribute with name, value, and optional source location. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int?` | Gets the source column, when available. |
| `Line` | `int?` | Gets the source line, when available. |
| `Name` | `string` | Gets the attribute name. |
| `Value` | `string` | Gets the attribute value. |

## Applies to

- `Cerneala.UI.Markup.UiMarkupAttribute`

## See also

- `Cerneala.UI.Markup.UiMarkupNode`
- `Cerneala.UI.Markup.UiMarkupDocument`
