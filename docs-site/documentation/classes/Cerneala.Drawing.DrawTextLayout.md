# DrawTextLayout Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Represents an immutable, measured collection of positioned text lines and runs.

```csharp
public sealed class DrawTextLayout
```

## Examples

```csharp
DrawTextLayout layout = builder.Build(options);
drawing.DrawTextLayout(layout, new DrawPoint(24, 40));
```

## Remarks

The layout stores completed shaping and reflow results. Recording it creates one logical command; the backend renders its positioned runs. The common transform stack supplies rotation and scaling, and the common clip stack clips text. Reusing the same layout preserves `StableId`, retained equality, and text raster caches.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Lines` | `IReadOnlyList<DrawTextLayoutLine>` | Gets immutable positioned lines. |
| `Bounds` | `DrawRect` | Gets layout-local conservative bounds. |
| `Options` | `DrawTextLayoutOptions` | Gets the options used to build the layout. |
| `StableId` | `long` | Gets the stable identity of this immutable result. |

## Applies To

Retained, `OnDemand`, transformed, clipped, and Prism-captured text drawing.
