# MonoGameDrawMapper Struct

## Definition
Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala.Backends.MonoGame`

Source: `Drawing/MonoGame/MonoGameDrawMapper.cs`

Provides the `Cerneala.Drawing.MonoGame.MonoGameDrawMapper` API surface.

```csharp
internal readonly struct MonoGameDrawMapper
```

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameDrawMapper(float coordinateScale)` | Creates a mapper for the supplied positive coordinate scale. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CoordinateScale` | `float` | Gets the validated logical-to-physical coordinate scale. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MapRectangle(DrawRect rect)` | `Rectangle` | Maps a logical rectangle to a MonoGame rectangle. |
| `MapVector(DrawPoint point)` | `Vector2` | Maps a logical point to physical coordinates. |
| `MapThickness(float thickness)` | `int` | Maps a logical thickness to a physical pixel width, with a minimum of one pixel. |
| `MapTextRun(DrawTextRun textRun)` | `DrawTextRun` | Maps text size to physical pixels and preserves the original run at scale one. |

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
