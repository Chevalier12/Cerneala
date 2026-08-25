# DrawSprite2D Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawBatches.cs`

Describes one destination and image-option set within a `DrawSpriteBatch`.

```csharp
public sealed record DrawSprite2D
```

## Examples

```csharp
DrawSprite2D sprite = new(
    new DrawRect(16, 24, 32, 32),
    new DrawImageOptions(source: new DrawRect(0, 0, 16, 16)));
```

## Remarks

The destination is interpreted with the shared image supplied to the batch. Rotation, source selection, tint, opacity, origin, flip, and depth come from `Options`. Every sprite in one batch must use the same sampling and address modes.

## Constructors

| Name | Description |
| --- | --- |
| `DrawSprite2D(DrawRect destination, DrawImageOptions? options = null)` | Creates a sprite description and uses default options when none are supplied. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Destination` | `DrawRect` | Gets the destination rectangle. |
| `Options` | `DrawImageOptions` | Gets immutable source, appearance, transform, and sampling options. |

## Applies To

`DrawSpriteBatch`.
