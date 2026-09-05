# Scene2DDebugOverlayDiagnostics Struct

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDebugOverlay.cs`

```csharp
public readonly record struct Scene2DDebugOverlayDiagnostics(
    int CandidateChunks, int VisitedTiles, int Colliders, int PromotedTiles,
    int NavigationCells, int Primitives);
```

## Remarks

Immutable counters for the last [Scene2DDebugOverlay](Cerneala.UI.Controls.Scene2DDebugOverlay.md) recording. They reset each recording and are zero for a disabled overlay. These are debug-observation counters, not gameplay query counts, GPU draw-call counts, or CPU timing measurements.

## Properties

| Name | Description |
| --- | --- |
| `CandidateChunks` | Chunks returned by map spatial queries before exact viewport filtering. |
| `VisitedTiles` | Viewport-intersecting cells inspected for coordinate/ID labels, including empty cells. |
| `Colliders` | Active viewport-intersecting collider geometries drawn in the observed subtree. |
| `PromotedTiles` | Promoted instances whose original slot or current bounds intersects the viewport. |
| `NavigationCells` | External grid coordinate queries, including missing cells. |
| `Primitives` | Emitted debug rectangles, ellipses, lines, and text runs; excludes transform, opacity, and Prism scope commands. |
