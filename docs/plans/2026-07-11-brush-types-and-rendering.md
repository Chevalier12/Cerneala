# Plan: completing the Brush model and rendering

## Summary

We complete the `Brush` system so that Cerneala can represent and render solid colors, gradients, images, drawings and visuals. The implementation must cover both the data model and the translation to the WindowsDX/MonoGame backend; it is not enough to add classes that remain simple containers.

Finally, each type of brush supported by the API must have:

- own validation and deterministic equality;
- support in markup and source generator where it makes sense;
- correct cache/resource lifetime for each `GraphicsDevice`;
- a testable rendering path through `IDrawingBackend`;
- regression tests for content, clipping, opacity and transformations.

## Current status

There are already:

- `Brush` with optional `SolidColor`;
- `SolidColorBrush`;
- `LinearGradientBrush`;
- `RadialGradientBrush`;
- `Pen` which retains a `Brush`.

Missing or incomplete:

- `ImageBrush`;
- `DrawingBrush`;
- `VisualBrush`;
- a common abstraction of type `TileBrush` for stretch, alignment, viewport and tile mode;
- rendering of gradients and non-solid brushes;
- GPU caches per window for the resources used by the brushes;
- markup support for compound brushes.

The current renderer draws primitives with `Color`; the existing gradient brushes do not reach the backend.

## Architectural decisions

- `Brush` remains the public semantic API; the backend receives an internal representation prepared for the device.
- `SolidColorBrush` is the fast path and does not create additional GPU resources.
- Gradients are rendered by GPU resources per `GraphicsDevice`, not by CPU sampling at each pixel.
- `ImageBrush` reuses the loader and image cache of the window; no `Texture2D` is shared between windows.
- `DrawingBrush` uses a rasterizable command list, not a second general renderer.
- `VisualBrush` uses an offscreen render target and has explicit protection against visual cycles.
- We do not introduce dependencies on WPF or MonoGame internals.
- The API keeps coordinates Cerneala and applies the DPI transformation only once, in the same place as the rest of the rendering.

## Phase 1: contract and common model

1. We define the `Brush` contract for:
   - type identification;
   - opacity;
   - validation of values;
   - conversion to an internal sampling description.
2. Enter `TileBrush` if the properties are common:
   - `Stretch`;
   - `AlignmentX` and `AlignmentY`;
   - `Viewport` and `Viewbox`;
   - `TileMode`;
   - `Opacity`.
3. We set enums and default values ​​without automatically copying all non-renderable WPF properties to Cerneala.
4. We keep `SolidColor` only as a shortcut for `SolidColorBrush`; compound brushes return `null`.
5. We are adding tests for the validation of stops, radii, coordinates and structural equality.

## Phase 2: gradients

1. We define the internal format for the gradient:
   - sorted list of stops;
   - alpha premultiplication;
   - interpolation in the space decided by the renderer;
   - clamp for offsets and extension for heads.
2. We implement in `MonoGameDrawingBackend` a common GPU representation for linear and radial:
   - stop texture or equivalent buffer;
   - quad/mesh with gradient parameters;
   - alpha blending compatible with existing text and primitives.
3. We correctly apply `Clip`, `Opacity`, `RenderTransform` and `CoordinateScale`.
4. We treat degenerate gradients predictably: single stop, zero length, invalid radii or duplicate stops.
5. We add reference images and pixel diffs for linear/radial at scales 1.0, 1.25 and 1.5.

## Phase 3: ImageBrush

1. Enter `ImageBrush` with:
   - source (`IDrawImage` or URI/path resolved by loader);
   - `Stretch`, alignment, viewport and tile mode;
   - opacity;
   - explicit behavior for missing or invalid image.
2. We separate the CPU data from the GPU texture:
   - decoded image/sharable CPU cache;
   - `Texture2D` created per session/window;
   - invalidation when the source changes.
3. We implement sampling and tiling in the backend, including clipping and DPI.
4. We are adding tests for aspect ratio, crop, repeat, mirror and device isolation.

## Phase 4: DrawingBrush

1. We establish the public form of the content: the list of `DrawCommand` or an immutable drawing object.
2. We define clear limits:
   - no access to `UIElement` from `DrawingBrush`;
   - no effects that require layout cycles;
   - the content must be safe to re-render.
3. We rasterize the content in a cached render target/texture per device.
4. We apply tile, transform, opacity and clip over the rasterized content.
5. We test that changing a command invalidates only the affected brush.

## Phase 5: VisualBrush

1. We define if the source is an existing `UIElement` or a separate template; the first version uses an existing element.
2. We introduce an explicit render pass offscreen, separated from the main frame.
3. We detect cycles of type `VisualBrush -> element -> brush` and fail controlled, without infinite recursion.
4. We establish the policy for detached elements, resources, input and focus: `VisualBrush` is only visual.
5. We cache the render target on the device and invalidate it when layout, properties or content changes.
6. We add tests for own source, parent source, cycles and size changes.

## Phase 6: markup and resources

1. We extend the runtime scheme and the source generator for:
   - `<SolidColorBrush ... />`;
   - `<LinearGradientBrush ...>`;
   - `<RadialGradientBrush ...>`;
   - `<ImageBrush ...>`;
- `<DrawingBrush ...>`;
   - `<VisualBrush ...>`.
2. We allow simple brush properties (`Color`, `Opacity`, stops, source) and element-property where the content is composed.
3. We make resource references type-safe: `$Accent` must produce `Brush`, not be forced into `Color`.
4. Keep diagnostics for incompatible types, missing stops, unknown sources and impossible combinations.
5. We document which syntax is compiled and which syntax remains runtime-only.

## Phase 7: integration with backends and lifetime

1. We extend `IDrawingBackend` with internal operations for the brush, without breaking the public API of `DrawingContext`.
2. We move all the GPU brush resources into the ownership of the graphic session of the window.
3. We release render targets, textures and caches to `Dispose`, resize and device reset.
4. We verify that two windows can use the same Brush description without sharing GPU objects.
5. We keep the solid fallback for backends that do not yet support compound brushes and we report clear diagnosis.

## Testing and acceptance

- unit tests for each Brush model and common properties;
- source generator and runtime markup tests for all types;
- render tests for color, alpha, clipping, transform and DPI;
- pixel diffs for linear, radial and image brush;
- lifetime tests per `GraphicsDevice` and device reset;
- cycle tests and invalidation for `VisualBrush`;
- build without warnings and completely green runtime/sourcegen suites;
- updated API documentation and no Brush type declared but unrenderable without an explicit diagnosis.

## Non-objectives

- binary compatibility with a WPF implementation;
- WPF effects that require a separate composer;
- arbitrary shaders exposed to the user in the first version;
- sharing GPU resources between windows;
- the change of properties `Background` and `Foreground` in this plan.