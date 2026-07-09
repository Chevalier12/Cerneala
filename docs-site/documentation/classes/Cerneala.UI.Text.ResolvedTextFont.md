# ResolvedTextFont Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/ResolvedTextFont.cs`

Represents a resolved draw font together with a stable identity string.

```csharp
public sealed class ResolvedTextFont
```

Inheritance:
`object` -> `ResolvedTextFont`

## Examples

Create a resolved text font from an `IDrawFont`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Text;

IDrawFont font = GetFont();
ResolvedTextFont resolved = new(font);

IDrawFont drawFont = resolved.Font;
string identity = resolved.Identity;
```

Use an explicit identity:

```csharp
ResolvedTextFont resourceFont = new(font, "resource:heading:3");
```

## Remarks

`ResolvedTextFont` stores a non-null `IDrawFont`. The constructor that accepts only a font builds `Identity` from the font family name and size.

The constructor with an explicit identity throws `ArgumentException` when `identity` is `null`, empty, or whitespace. Both constructors throw `ArgumentNullException` when `font` is `null`.

## Constructors

| Signature | Description |
| --- | --- |
| `ResolvedTextFont(IDrawFont font)` | Initializes the resolved font and derives identity from the font family name and size. |
| `ResolvedTextFont(IDrawFont font, string identity)` | Initializes the resolved font with an explicit non-empty identity. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Font` | `IDrawFont` | Gets the resolved draw font. |
| `Identity` | `string` | Gets the identity string used to distinguish resolved font instances. |

## Applies To

Cerneala UI text measurement and rendering APIs.

## See Also

- `Cerneala.UI.Text.FontResolver`
- `Cerneala.Drawing.IDrawFont`
- `Cerneala.UI.Resources.FontResource`
