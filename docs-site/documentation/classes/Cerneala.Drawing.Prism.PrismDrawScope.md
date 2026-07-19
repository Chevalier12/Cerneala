# PrismDrawScope Struct

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismDrawScope.cs`

Carries the typed, backend-neutral frame state for one retained Prism capture scope.

```csharp
public readonly record struct PrismDrawScope
```

## Examples

```csharp
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Prism.Runtime;

PrismDrawScope scope = new(
    prismInstance,
    new PrismCacheOwnerToken(42),
    new DrawRect(0, 0, 120, 48),
    Matrix3x2.Identity,
    pixelScale: 2,
    visualContentVersion: 7);

DrawCommand begin = DrawCommand.BeginPrism(scope);
DrawCommand end = DrawCommand.EndPrism();
```

## Remarks

The scope contains only data needed to analyze and compose the current retained frame. `Instance` supplies the immutable definition and current typed Prism values. `StructuralVersion` and `ValueVersion` are read from that instance, while `VisualContentVersion` identifies changes to the captured retained subtree.

`EffectiveTransform` remains in logical drawing coordinates. `PixelScale` is carried separately so a backend does not apply viewport scaling twice. `CacheOwnerToken` is numeric and has no reference back to a UI element.

## Constructors

| Name | Description |
| --- | --- |
| `PrismDrawScope(PrismInstance, PrismCacheOwnerToken, DrawRect, Matrix3x2, float, long)` | Creates a typed retained Prism scope. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Instance` | `PrismInstance` | Gets the per-element typed Prism instance. |
| `Definition` | `PrismCompositionDefinition` | Gets the immutable composition definition owned by `Instance`. |
| `CacheOwnerToken` | `PrismCacheOwnerToken` | Gets the numeric retained cache identity. |
| `ControlBounds` | `DrawRect` | Gets the captured control bounds in logical coordinates. |
| `EffectiveTransform` | `System.Numerics.Matrix3x2` | Gets the effective logical transform for the scope. |
| `PixelScale` | `float` | Gets the logical-to-physical coordinate scale. |
| `StructuralVersion` | `PrismStructuralVersion` | Gets the current topology version from `Instance`. |
| `ValueVersion` | `PrismValueVersion` | Gets the current typed-value version from `Instance`. |
| `VisualContentVersion` | `long` | Gets the aggregated retained visual generation of the captured subtree. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `instance` is `null`. |
| Constructor | `ArgumentOutOfRangeException` | `pixelScale` is non-finite or not positive. |
| Constructor | `ArgumentOutOfRangeException` | `visualContentVersion` is negative. |

## Applies to

Cerneala retained Prism command recording, frame analysis, and backend composition.

## See also

- `Cerneala.Drawing.Prism.PrismCacheOwnerToken`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.UI.Prism.Runtime.PrismInstance`
