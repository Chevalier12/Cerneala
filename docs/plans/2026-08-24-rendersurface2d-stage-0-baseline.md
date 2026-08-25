# RenderSurface2D complete drawing API — Stage 0 baseline

Date: 2026-08-24

This inventory freezes the pre-expansion command contract and the integration
points that every later stage must update. It is an implementation aid for the
parent plan, not public API documentation.

## Approved contract conventions

- Typed path arcs use the SVG endpoint form: radii, axis rotation in degrees,
  large-arc flag, sweep flag, and endpoint. Convenience shape angles use radians.
- Raw `Push*`/`Pop*` methods remain available. Separate ergonomic methods return
  LIFO-validated `ref struct` scopes.
- Mesh and batch inputs are defensively copied, exposed through immutable views,
  and carry explicit retained versions. Images remain platform-neutral references.
- Text layouts are immutable reusable results built from immutable
  `DrawTextSpan` runs by `DrawTextLayoutBuilder.AddSpan`.

## Current command inventory

`DrawCommand` is a `readonly record struct`. Its synthesized equality and hash
cover every stored property using that property's equality semantics. That means
scalar payload changes are retained misses, while brush, font, image, Prism scope,
and nested-surface identity follow their own equality implementation.

| Kind | Meaningful payload | Current damage bounds | Resources / context |
|---|---|---|---|
| `FillRectangle` | `Rect`; either `Color` or `Brush` + `BrushOpacity` | mapped `Rect` | optional brush |
| `DrawRectangle` | fill payload plus `Thickness` | mapped `Rect` (stroke expansion is not represented) | optional brush |
| `FillEllipse` | `Rect`; either `Color` or `Brush` + `BrushOpacity` | mapped `Rect` | optional brush |
| `DrawEllipse` | fill payload plus `Thickness` | mapped `Rect` (stroke expansion is not represented) | optional brush |
| `DrawLine` | `Position`, `EndPoint`, `Thickness`; color or brush payload | endpoint AABB expanded by half thickness | optional brush |
| `FillPath` | SVG `PathData`, source `SourceRect`, destination `Rect`, `Brush`, `BrushOpacity` | mapped destination `Rect` | brush; SVG is reparsed downstream |
| `DrawText` | `Text`, `TextRun`, `Font`, `Position`; color or brush payload | full surface | font/text run and optional brush |
| `DrawImage` | `Image`, destination `Rect`, optional `ImageSource`, `Color`, `ImageRotation`, `ImageOrigin`, `ImageFlip`, `LayerDepth` | destination only when rotation is zero and origin is default; otherwise full surface | image |
| `RenderSurface2D` | internal `RenderSurface`, destination `Rect`, `Color` | full surface | nested surface |
| `PushClip` | `Rect` | full surface and context-sensitive | clip stack |
| `PopClip` | none | full surface and context-sensitive | clip stack |
| `BeginPrism` | `PrismScope` | full surface and context-sensitive | Prism instance/cache/resources |
| `EndPrism` | none | full surface and context-sensitive | Prism stack |

Unused record fields retain their default value. `DrawCommandList.Version` changes
on mutations; the retained session compares command records by prefix and suffix.
Any clip or Prism command currently promotes damage to the full surface.

## Recording and replay flow

1. `RenderSurface2DFrame` enforces per-frame lifetime, delegates drawing to one
   `DrawingContext`, and records image dependencies before delegation.
2. `DrawingContext` validates through `DrawCommand` factories and appends immutable
   command values to `DrawCommandList`.
3. UI rendering uses `DrawCommandListBuilder` to translate local commands, apply
   ancestor transform/opacity, add rectangular clips, and wrap Prism scopes.
4. `MonoGameRenderSurface2DSession` records a frame, compares it with retained
   commands, calculates damage, creates a damage clip, and invokes analysis/replay.
5. `PrismFrameAnalyzer` interprets rectangular clip and Prism stacks, validates
   nesting, calculates Prism bounds/capabilities, and supplies the backend frame
   analysis.
6. `MonoGameDrawingBackend` switches over the command kind, resolves brush/text/
   image/path resources, maintains the scissor stack, and executes the GPU work.

## Exhaustive switch and integration checklist

- `Drawing/DrawCommand.cs`: factories define every current payload.
- `Drawing/MonoGame/MonoGameDrawingBackend.cs`: primary render switch and nested
  surface command mapping switch.
- `UI/Rendering/DrawCommandListBuilder.cs`: local translation and render-scope
  transform/opacity switches.
- `Drawing/MonoGame/MonoGameRenderSurface2DSession.cs`: context-sensitivity and
  damage-bound classification.
- `Drawing/Prism/Graph/PrismFrameAnalyzer.cs`: clip/Prism stack interpretation.
- `UI/Controls/RenderSurface2DFrame.cs`: public facade delegation, frame lifetime,
  and image dependency tracking.

## Baseline and RED evidence

- Existing primitive behavior is frozen by
  `CompleteDrawingApiBaselineTests`, `DrawingContextTests`,
  `AdvancedDrawCommandTests`, and `RenderSurface2DTests`.
- `CompleteDrawingApiRedTests` is compile-safe before the new API exists. Each
  test carries a `PlanStage` trait and fails on a missing type/member rather than
  a harness failure.
- Test names state the mathematical/lifetime invariants: contour closure and SVG
  arc convention; stroke expansion/style identity; LIFO state and group opacity;
  radians/radius normalization; immutable versioned mesh/batch payloads;
  cluster-safe bidi layout; and centralized retained/damage/resource metadata.
