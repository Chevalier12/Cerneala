# FontResolver.FallbackDrawFont Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/FontResolver.cs`

Provides an internal fallback `IDrawFont` when no external font source is configured.

```csharp
private sealed class FallbackDrawFont : IDrawFont
```

Inheritance:
`object` -> `FallbackDrawFont`

Implements:
`IDrawFont`

## Examples

`FallbackDrawFont` is private to `FontResolver`. Public code gets it indirectly when resolving by family and size without a font source:

```csharp
using Cerneala.UI.Text;

FontResolver resolver = new();
ResolvedTextFont resolved = resolver.Resolve("Arial", 16);
```

## Remarks

`FallbackDrawFont` stores the requested font family name and size. It is created by `FontResolver.Resolve(string familyName, float size)` when the resolver does not have an `IFontSource`.

The class does not load platform font data; it is a simple `IDrawFont` implementation carrying the requested values.

## Constructors

| Signature | Description |
| --- | --- |
| `FallbackDrawFont(string familyName, float size)` | Initializes the fallback font with the requested family name and size. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FamilyName` | `string` | Gets the requested font family name. |
| `Size` | `float` | Gets the requested font size. |

## Applies To

Cerneala UI text font resolution internals.

## See Also

- `Cerneala.UI.Text.FontResolver`
- `Cerneala.Drawing.IDrawFont`
- `Cerneala.UI.Text.ResolvedTextFont`
