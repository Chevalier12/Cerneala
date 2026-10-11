# Prism blur, sharpen, and noise filters

The generated Prism catalog is the source of truth for property names, value
types, defaults, domains, capabilities, determinism, and coverage ownership.
This document describes the runtime semantics shared by the blur, sharpen, and
noise families; it does not duplicate the generated property table.

## Execution model

`PrismNeighborhoodPlanner` reads the typed parameter snapshot once while the
graph is built. It converts DIP radii and distances to device pixels, prepares
logical-bounds radii separately, resolves symbolic modes, and stores immutable
pass settings on each filter node. The executor binds only those prepared
settings. The shader never reads markup defaults or converts public units.

Gaussian and box filters use horizontal and vertical graph passes. Filters
whose sampling path is directional, radial, resource-driven, or edge-aware use
a direct pass. A dimension with one pixel is omitted from a separable plane; a
one-by-one source becomes an exact no-op when the operation cannot change it.
Box `Iterations` widens the prepared convolution support explicitly rather than
introducing a device-dependent pass threshold. The CPU reference uses a
summed-area table; the shader uses the equivalent separable form and evaluates
all `2 * radius + 1` taps, with no hidden quality cap.

Sampling quality is fixed by the catalog symbol:

| Quality | Samples |
| --- | ---: |
| `Draft` / `Low` | 5|
| `Good` / `Medium` | 9|
| `Best` / `High` | 17 |

The fixed-quality filters do not reduce quality adaptively. Image size only
removes mathematically empty axes for those filters. `SpinBlur` is the
intentional exception: its planner prepares an odd maximum tap budget from the
rotation angle and the largest masked pixel radius, while each pixel reduces
that budget again from its own arc length.

## Color and alpha

Each neighborhood sample is converted from the composition's working profile
to linear sRGB through the same conversion helpers used by adjustment filters.
Convolution operates on associated RGBA, so transparent colored pixels cannot
create halos. The result is blended at the filter opacity and converted back to
the working profile once.

The edge modes are `Clamp`, `Transparent`, `Wrap`, and `Mirror` (with
`Reflect` mapped to `Mirror`). Transparent addressing contributes zero
associated RGBA outside the source; the other modes remap the coordinates before
sampling.

## Implemented families

The classic and specialized blur set is:

- `Average`, `Blur`, `BlurMore`, `BoxBlur`, and `GaussianBlur`
- `LensBlur`, `MotionBlur`, `RadialBlur`, `ShapeBlur`, `SmartBlur`, and
  `SurfaceBlur`
- `FieldBlur`, `IrisBlur`, `TiltShift`, `PathBlur`, and `SpinBlur`
`Average` is a distinct fixed 3x3 box convolution: all nine taps have weight
`1/9`, edges clamp to the source, and bounds do not expand. CPU reference and
GPU shader both evaluate the same kernel in linear premultiplied RGBA.

`SmartBlur` and `SurfaceBlur` use a 2D bilateral mean over a square grid cut to
a disc, not a sparse row of spiral dots. `Quality` chooses a 5x5, 9x9, or
17x17 grid. Both multiply the spatial and range Gaussian weights; `SmartBlur`
measures the distance between straight linear RGB colors and `SurfaceBlur`
measures the linear luminance difference. CPU and GPU normalize the same grid.

`FieldBlur` reads normalized depth from `BlurField`, optionally inverts it, and
calculates the circle of confusion from the distance to `FocalDistance`. The
aperture radius is `Blur * CoC`; samples are distributed on a disc and
optionally luminance-weighted for highlights. CPU and GPU use the same formula,
the same support, and associated alpha.

`IrisBlur` builds an elliptical focus mask centered on `Center`, with semi-axes
`Radius`, rotated about `Center` by `Rotation` degrees. The inside of the
ellipse stays sharp, `Feather` sets the width of the smoothstep transition in
normalized ellipse units, and outside the transition the blur reaches the disc
radius `Blur`. Decorative parameters without effect were removed. CPU and GPU
rotate the coordinate before scaling the ellipse and compute the same mask.

`TiltShift` uses the absolute distance to a line through `Center` oriented by
`Angle`; `Angle = 0` gives a horizontal band. The band of half-width
`FocusWidth` stays sharp, `Feather` sets the smoothstep transition, and outside
the band the blur reaches `Blur`. Options without effect have been removed, and
CPU and GPU use the same mask.

Both masks are computed in normalized UV coordinates, so on a non-square surface
the ellipse and the band angle are stretched with the surface. In the transition
band, the CPU reference shrinks the blur radius by the mask amount before mixing,
while the GPU samples at the full radius and only mixes by that amount.

`SpinBlur` integrates an arc centered on the pixel position, in pixel
coordinates, to keep a circular trajectory on rectangular surfaces.
`Center` and `Radius` are normalized and `Feather` is the inner fraction
`0..1` of the radius of the ellipse. The number of taps is odd, grows roughly
with the arc length, and is capped at 65; it drops automatically near the
center, down to one tap at the center itself.
The common path uses a trigonometric recurrence and bilinear sampling.
`StrobeStrength` combines continuous exposure with periodic windows defined by
`StrobeFlashes` and `StrobeDuration` and `Noise` deterministically disrupt only
the inner taps. CPU and GPU accumulate and normalize RGBA associated in
the same direct pass, without auxiliary resources.

The sharpening set is `Sharpen`, `SharpenMore`, `SharpenEdges`, `UnsharpMask`,
`SmartSharpen`, and `HighPass`.

`Sharpen` uses a radial kernel with five cross taps. The negative lobe
is calculated separately on each channel from the local minimum, maximum, and
headroom, and `Amount` monotonically controls the intensity in the range `0..1`.
The filter runs in one pass on straight linear RGB, preserves the center
pixel's alpha, and premultiplies the result again; fully transparent neighbors
use the center color in the calculation, which avoids transparent-black fringes.
CPU and GPU use the same formula, edge clamp, and the same five taps.

`SharpenMore` uses a 3x3 high-boost with the binomial blur
`[1 2 1; 2 4 2; 1 2 1] / 16`. At the default `Amount = 0.5` the formula is
`2 * center - blur`; the range `0..1` monotonically scales the residual up to
double intensity. The nine taps are evaluated explicitly in a single pass, with
edge clamp, on straight linear RGB. The center alpha stays unchanged, fully
transparent neighbors use the center color in the calculation, and CPU and GPU
premultiply the same result.

`SharpenEdges` evaluates a normalized Sobel gradient on a 3x3 neighborhood and
uses its magnitude as a gate for the same local-contrast-limited sharpen as
`Sharpen`. `Threshold` sets the center of a smoothstep transition instead of a
hard cut, and `Amount` controls the maximum intensity. The CPU and GPU
implementation is a deterministic single pass with nine taps, edge clamp, and
straight linear RGB. The center alpha stays unchanged and fully transparent
neighbors use the center color in the calculation, so no false fringes or
edges appear.

`UnsharpMask` builds the mask from a separable Gaussian with finite support of
`Radius` and sigma `Radius / 3`, then recombines the original texture with the
residual `original - blur`. `Amount` scales the residual, and `Threshold`
controls a smoothstep gate centered on the luminance difference, with a minimum
width of one 8-bit level to avoid visible clipping. The two Gaussian passes use
17 taps each and are followed by a pass with no neighborhood sampling that reads
both the blur and the original preserved by the Prism graph. The high-boost
calculation runs on straight linear RGB, preserves the original alpha, and
premultiplies the result again. CPU and GPU use the same formula, and `Amount = 0`,
`Radius = 0` and 1x1 source are exact no-ops.

`SmartSharpen` uses Richardson-Lucy deconvolution with four fixed iterations,
which also acts as regularization. Each iteration runs the estimate
convolution, the ratio to the original, back-projection with the mirrored PSF,
and the multiplicative update; a seventeenth pass applies `Amount` and the
tonal protections. `Remove` selects a Gaussian PSF, a disc for lens blur, or a
segment oriented by `Angle` for motion blur. `ReduceNoise` damps the
multiplicative correction toward identity, and the shadow/highlight controls
reduce the effect using local luminance and their configured radii. CPU and GPU
work on straight linear RGB, keep the original alpha, and premultiply the result
again. The graph keeps the observation and the estimate of each iteration
separate, and the GPU uses ping-pong surfaces; the correction values are
reversibly encoded on intermediate normalized surfaces. Sampling passes do not
extend the image's logical bounds.

The noise and cleanup set is `AddNoise`, `Despeckle`, `DustScratches`, `Median`,
and `ReduceNoise`. `AddNoise` keeps its explicit catalog seed (an integer from
`0` to `2147483647`) as two prepared 16-bit halves and combines them with the
pixel coordinates and channel in a stateless integer permutation. `Uniform` maps
one permuted sample to `[-1, 1]`; `Gaussian` transforms paired samples into a
zero-mean, unit-variance normal deviate instead of averaging uniforms. Both CPU and GPU run
in one direct pass, apply the delta to straight linear RGB, clamp, premultiply again,
and preserve source alpha. The implementation never reads time or global random
state, and monochromatic mode applies the same deviation to all color channels.

`Despeckle` uses switched median detection and progressive restoration. Three
detection passes compare the straight linear luminance of the center with the
sample median and mark only differences greater than `Threshold`. Three
restoration passes replace the marked pixels with the median calculated only
from neighbors already considered good; a final pass resolves any remaining
marks and applies opacity/blend to the original input. `Radius` controls the
isotropic support: a small radius uses the 3x3 neighborhood, and larger radii
use 21 positions distributed on a scaled disc. The intermediate state keeps
straight linear RGB and a binary mask on `HalfVector4` surfaces; the final pass
restores the original center alpha and premultiplies RGB again. CPU and GPU use
the same sample order, the same three iterations, and edge clamp;
`Radius = 0` is an exact identity.

`DustScratches` uses a single-pass switched adaptive median. The planner
converts `Radius` to pixels, rounds it up, and limits the GPU support to radius
3, that is a 7x7 maximum. For each pixel, the filter tries the 3x3, 5x5, and 7x7
windows in order until the median lies strictly between the local minimum and
maximum luminance. In that window, the center is replaced only if it is a local
extreme and its difference from the median exceeds `Threshold`. If no window
separates the impulse, the median of the largest window is used, through the
same threshold gate. Ranking uses straight linear RGB luminance; the
replacement keeps the center alpha and premultiplies the median color by it.
CPU and GPU use the same window growth, the same edge clamp, and the same radius
limit; `Radius = 0` is an exact identity.

`Median` uses a fixed compare-exchange network for the nine samples of the 3x3
window, with edge clamp and no data-dependent branches in shaders. The rank is
the straight linear RGB luminance, and the result is the associated RGBA pixel
at the median position, not a color synthesized separately per channel. CPU and
GPU run the same comparison order and keep the same semantics for translucent
pixels. `Radius` is an integer selector:
`0` is exact identity, `1` enables 3x3 kernel, and other values are
rejected by the catalog domain.

`ReduceNoise` uses normalized convolution from the Domain Transform family
described by Gastal and Oliveira, adapted to the pixel-based Prism backend
shaders. The planner emits three iterations, each with a horizontal and a
vertical pass, and the sigma of each iteration follows the paper's schedule. The distance
transform combines luma variation with chroma variation in YCoCg; `Strength` and
`SharpenDetails` controls luma, `ReduceColorNoise` controls chroma, and
`PreserveDetails` narrows the range domain and re-adds the detail layer to
recombination. `RemoveJpegArtifact` adds two block-aware passes that operate
only on the borders of the JPEG 8x8 grid. The final recombination with the
original then uses at least `0.65` for the luma mix weight and at least `0.5`
for the chroma mix weight. CPU and GPU use the same formulas, edge clamp and max
eight taps on each side. All passes filter straight linear RGB, mask the
contributions of alpha differences, keep the center alpha unfiltered, and
premultiply the result again. When all controls are zero and JPEG artifact
removal is disabled, the whole plan is an exact identity.

## Auxiliary resources

`LensBlur.DepthMap` is optional. `ShapeBlur.Shape`, `FieldBlur.BlurField`, and
`PathBlur.Path` have required typed image resources. Their resource identifiers
participate in graph dependencies and versioning. A specified resource that is
missing, disposed, from another graphics device, or otherwise unavailable
causes the configured `PrismFallbackPolicy` action and an observable diagnosis;
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