# Prism blur, sharpen, and noise filters

The generated Prism catalog is the source of truth for property names, value
types, defaults, domains, capabilities, determinism, and coverage ownership.
This document describes the runtime semantics shared by the blur, sharpen, and
noise families; it does not duplicate the generated property table.

## Execution model

`PrismNeighborhoodPlanner` reads the typed parameter snapshot once while the
graph is built. It converts DIP radii and distances to device pixels, prepares
logical bounds radii separately, resolves symbolic modes, and stores immutable
pass settings on each filter node. The executor binds only those prepared
settings. The shader never reads markup defaults or converts public units.

Gaussian and box filters use horizontal and vertical graph passes. Filters
whose sampling path is directional, radial, resource-driven, or edge-aware use
a direct pass. A dimension with one pixel is omitted from a separable plan; a
one-by-one source becomes an exact no-op when the operation cannot change it.
Box `Iterations` widens the prepared convolution support explicitly rather than
introducing a device-dependent pass threshold.

Sampling quality is fixed by the catalog symbol:

| Quality | Samples |
| --- | ---: |
| `Draft` / `Low` | 5 |
| `Good` / `Medium` | 9 |
| `Best` / `High` | 17 |

There is no adaptive quality reduction. Image size only removes mathematically
empty axes; it does not change the selected sample count.

## Color and alpha

Every neighborhood sample is converted from the composition's working profile
to linear sRGB through the same conversion helpers used by adjustment filters.
Convolution operates on associated RGBA, so transparent colored pixels cannot
create halos. The result is blended at the filter opacity and converted back to
the working profile once.

The edge modes are `Clamp`, `Transparent`, `Wrap`, and `Mirror` (with
`Reflect` mapped to `Mirror`). Transparent addressing contributes zero
associated RGBA outside the source; the other modes remap the coordinate before
sampling.

## Implemented families

The classic and specialized blur set is:

- `Average`, `Blur`, `BlurMore`, `BoxBlur`, and `GaussianBlur`
- `LensBlur`, `MotionBlur`, `RadialBlur`, `ShapeBlur`, `SmartBlur`, and
  `SurfaceBlur`
- `FieldBlur`, `IrisBlur`, `TiltShift`, `PathBlur`, and `SpinBlur`

The sharpening set is `Sharpen`, `SharpenMore`, `SharpenEdges`, `UnsharpMask`,
`SmartSharpen`, and `HighPass`.

The noise and cleanup set is `AddNoise`, `Despeckle`, `DustScratches`, `Median`,
and `ReduceNoise`. `AddNoise` reconstructs its explicit 32-bit catalog seed
from prepared halves. It never reads time or global random state, and
monochromatic mode applies the same noise delta to all color channels.

## Auxiliary resources

`LensBlur.DepthMap` is optional. `ShapeBlur.Kernel`, `FieldBlur.Pins`, and
`PathBlur.Path` are required typed image resources. Their resource identifiers
participate in graph dependencies and versioning. A specified resource that is
missing, disposed, from another graphics device, or otherwise unavailable
causes the configured `PrismFallbackPolicy` action and an observable diagnostic;
the executor does not silently substitute another filter.

## Bounds and optimization

Prepared passes carry device sampling radii and logical bounds radii as distinct
values. Separable expansions accumulate along the graph, so the final surface
covers every sampled pixel. Document-space effects whose samples remain inside
the source keep source bounds.

The optimizer removes a neighborhood node only when its prepared pass is an
exact no-op and its opacity/blend state is neutral. Zero radius, zero amount,
and a degenerate one-pixel axis are evaluated in the planner. Nonzero filters,
resource-driven filters, and non-normal blend modes retain their ordering.
