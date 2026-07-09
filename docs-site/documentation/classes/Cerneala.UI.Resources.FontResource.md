# FontResource Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/FontResource.cs`

Wraps an `IDrawFont` so it can be resolved through UI resource APIs.

```csharp
public sealed class FontResource
```

Inheritance:
`object` -> `FontResource`

## Examples

Create and resolve a font resource:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;

IDrawFont font = GetFont();
FontResource resource = new(font);

IDrawFont resolved = resource.Resolve();
```

## Remarks

`FontResource` stores a non-null `IDrawFont` instance. The constructor throws `ArgumentNullException` when `font` is `null`.

`Resolve` returns the stored font instance directly; the class does not load, clone, cache, or dispose the font.

## Constructors

| Signature | Description |
| --- | --- |
| `FontResource(IDrawFont font)` | Initializes a font resource with the supplied draw font. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve()` | `IDrawFont` | Returns the stored font instance. |

## Applies To

Cerneala UI resource and text rendering APIs.

## See Also

- `Cerneala.Drawing.IDrawFont`
- `Cerneala.UI.Resources.ResourceStore`
- `Cerneala.UI.Resources.ResourceId<T>`
