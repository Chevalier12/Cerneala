# Prism blur, sharpen, and noise filters

The generated Prism catalog is the source of truth for property names, value
types, defaults, domains, capabilities, determinism, and coverage ownership.
This document describes the runtime semantics shared by the blur, sharpen, and
noise families; it does not duplicate the generated property table.

## Execution model

`PrismNeighborhoodPlanner` reads the typed parameter snapshot once while the
graph is built. It converts DIP radii and distances to device pixels, prepares
logical bounds radiate separately, resolves symbolic modes, and stores immutable
pass settings on each filter node. The executor binds only those prepared
settings. The shader never reads markup defaults or converts public units.

Gaussian and box filters use horizontal and vertical graph passes. Filters
whose sampling path is directional, radial, resource-driven, or edge-aware use
a direct pass. A dimension with one pixel is omitted from a separable plane; a
one-by-one source becomes an exact no-op when the operation cannot change it.
Box `Iterations` widens the prepared convolution support explicitly rather than
introducing a device-dependent pass threshold. The CPU reference uses a
summed-area table; the shader uses the equivalent separable form and evaluates
all `2 * radius + 1` taps, no hidden ceiling quality.

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

`SmartBlur` and `SurfaceBlur` use a 2D disc-cut bilateral media,
not a sparse row of spiral dots. `Quality` choose grid resolution 5x5, 9x9 or
17x17. Both multiply the spatial and range Gaussian weights; SmartBlur
measures RGB straight distance and SurfaceBlur measures luminance difference
linear. CPU and GPU normalize the same grid.

`FieldBlur` reads normalized depth from `BlurField`, applies inversion
optional and calculate circle of confusion from distance to
`FocalDistance`. The radius of the aperture is `Blur * CoC`; samples are distributed
on disk and optionally luminance-weighted for highlights. CPU and GPU
they use the same formula, same support and associated alpha.

`IrisBlur` builds an elliptical focus mask rotated around it
ZZZ BLACK 10ZZZ. The interior of the rays remains clear, `Feather` produces the smoothstep transition,
and outside the circle of confusion reaches the radius `Blur`. Decorative parameters
without effect were removed; The CPU and GPU rotate the coordinate before scaling
the ellipse.

`TiltShift` uses the signed distance to a plane oriented by `Angle`.
`FocusWidth` band remains clear, `Feather` produces smoothstep transition, and
range reaches `Blur` out of band. Options without effect have been removed,
and CPU/GPU use the same mask.

`SpinBlur` integrates an arc centered on the pixel position, in coordinates of
pixel to preserve circular trajectory on rectangular surfaces.
`Center` and `Radius` are normalized and `Feather` is the inner fraction
`0..1` of the radius of the ellipse. The number of taps is odd, increasing by approx
arc length and is capped at 65; close to the center is automatically reduced.
The usual way uses a trigonometric recurrence and bilinear sampling.
`StrobeStrength` combines continuous exposure with periodic windows defined by
`StrobeFlashes` and `StrobeDuration` and `Noise` deterministically disrupt only
the inner taps. CPU and GPU accumulate and normalize RGBA associated in
the same direct pass, without auxiliary resources.

The sharpening set is `Sharpen`, `SharpenMore`, `SharpenEdges`, `UnsharpMask`,
`SmartSharpen`, and `HighPass`.

`Sharpen` uses a radial kernel with five cross taps. The negative lobe
is calculated separately on each channel from the minimum, maximum and local headroom,
and `Amount` monotonically controls the intensity in the range `0..1`. The filter
run in one pass on RGB linear straight, preserve pixel alpha
central and remultiplies the result; fully transparent neighbors inherit
the central color for the calculation, avoiding the fringes of transparent black.
The CPU and GPU use the same formula, edge clamp and the same five taps.

`SharpenMore` uses a 3x3 high-boost with binomial blur
`[1 2 1; 2 4 2; 1 2 1] / 16`. At the default value of `Amount = 0.5`,
the formula is `2 * centru - blur`; the interval `0..1` monotonically scales the residual
up to double intensity. The nine taps are explicitly evaluated in a
single pass, with edge clamp, on RGB linear straight. Central Alpha remains
unchanged, fully transparent neighbors use the center color in the calculation,
and the CPU and GPU remultiply the same result.

`SharpenEdges` evaluates a normalized Sobel gradient on a 3x3 neighborhood and
use its magnitude as a gate for the same sharpen limited by
local contrast as `Sharpen`. `Threshold` sets the center of a transition
threshold-width smoothstep, instead of a binary break,
and `Amount` controls the maximum intensity. CPU/GPU implementation is a
deterministic single pass with nine taps, edge clamp and RGB linear straight.
The central alpha remains unchanged and the fully transparent neighbors inherit
the central color in the calculation so as not to produce false fringing or edges.

`UnsharpMask` constructs the mask from a separable Gaussian with finite support at
`Radius` and sigma `Radius / 3`, then recombine the original texture with the residue
`original - blur`. `Amount` explicitly scales the residual, and `Threshold`
controls a smoothstep gate centered on the luminance difference with one strip
minimum of one 8-bit level to avoid visible clipping. The two passes
Gaussian uses 17 taps each and are followed by a pass without sampling de
neighborhood that reads both the blur and the original preserved by the Prism graph.
The high-boost calculation runs on RGB linear straight, preserves the original alpha and
remultiplies the result. CPU and GPU use the same formula, and `Amount = 0`,
`Radius = 0` and 1x1 source are exact no-ops.

`SmartSharpen` uses Richardson-Lucy deconvolution with four fixed iterations,
which also works as regularization. Each iteration executes the convolution
estimates, the ratio to the original, back-projection with the mirrored PSF and
multiplicative updating; a seventeenth pass applies `Amount` and
tonal protections. `Remove` select Gaussian PSF, disk for lens blur or
segment oriented by `Angle` for motion blur. `ReduceNoise` amortizes
multiplicative correction to identity, and shadow/highlight controls
reduce the effect by using local luminance and their configured radii. CPU and GPU
works on RGB linear straight, keeps original alpha and repremultiplies
the result. The graph keeps the iteration observation and estimation separate, and the GPU
use ping-pong surfaces; the correction values are reversibly encoded on
intermediate normalized surfaces. Sampling passes do not extend the limits
logic of the image.

The noise and cleanup set is `AddNoise`, `Despeckle`, `DustScratches`, `Median`,
and `ReduceNoise`. `AddNoise` preserves its explicit 32-bit catalog seed as two
prepares 16-bit halves and combines them with pixel coordinates and channel in
a stateless integer permutation. `Uniform` maps one permuted sample to `[-1, 1]`;
`Gaussian` transforms paired samples into a zero-mean, unit-variance normal
deviated instead of averaging uniforms. Both CPU and GPU run
in one direct pass, apply the delta to linear straight RGB, clamp, repremultiply,
and preserve source alpha. The implementation never reads time or global random
state, and monochromatic mode applies the same deviation to all color channels.

`Despeckle` uses switched median detection and progressive restoration. Three
detection passes compare the straight line luminance of the center with the sample
median and marks only differences greater than `Threshold`. Three passes
restoration replace the marked pixels with the median calculated exclusively from
neighbors already considered good; a final pass resolves any remaining markup and
applies opacity/blend to the original input. `Radius` actually controls
isotropic support: the small radius uses the 3x3 neighborhood, and the larger radii use the
21 positions distributed on a scaled disk. The intermediate state preserves RGB
linear straight and binary mask on surfaces `HalfVector4`; the final pass
restores original center alpha and remultiplies RGB. CPU and GPU use
same order of samples, same three iterations and edge clamp;
`Radius = 0` is exact identity.

`DustScratches` uses a single-pass switched adaptive median.
Planner converts `Radius` to pixels, rounds it up and limits the support
GPU at radius 3, i.e. 7x7 maximum. For each pixel, the filter tries in order
the 3x3, 5x5, and 7x7 windows until the median is strictly between the minimum and maximum
local luminance. In that window, the center is replaced only if it is one
extreme local and its difference from the median exceeds `Threshold`; if none
window does not separate the impulse signal, the median of the largest is used
windows, also through the same threshold gate. Sorting uses RGB luminance
linear straight, but the replacement keeps the center alpha and remultiplies
the middle color. The CPU and GPU use the same expansion, the same edge clamp
and the same radius limit; `Radius = 0` is exact identity.

`Median` uses a fixed compare-exchange network for the nine samples
of the 3x3 window, with edge clamp and no data-dependent branches in
shaders. The rank is the RGB linear straight luminance, and the result is the pixel
Associated RGBA located on the median position, not a color synthesized separately on
channels. The CPU and GPU execute the same order of comparisons and keep it that way
same semantics for translucent pixels. `Radius` is an integer selector:
`0` is exact identity, `1` enables 3x3 kernel, and other values are
rejected by the catalog domain.

`ReduceNoise` uses normalized convolution from the Domain Transform family
described by Gastal and Oliveira, adapted to the pixel-based Prism backend
shaders. The planner outputs three iterations, each with a horizontal pass and one
vertically, and the sigma of each iteration follows the programming in the paper. The distance
transform combines luma variation with chroma variation in YCoCg; `Strength` and
`SharpenDetails` controls luma, `ReduceColorNoise` controls chroma, and
`PreserveDetails` narrows the range domain and re-adds the detail layer to
recombination. `RemoveJpegArtifact` adds two block-aware passes that operate
only on the borders of the JPEG 8x8 grid and are followed by the same recombination with
the original. CPU and GPU use the same formulas, edge clamp and max
eight taps on each side. All passes filter RGB linear straight,
mask alpha difference contributions, keep central alpha
unfiltered and remultiplies the result. When all checks are zero and
JPEG artifact removal is disabled, the entire plan is identity
exact.

## Auxiliary resources

`LensBlur.DepthMap` is optional. `ShapeBlur.Shape`, `FieldBlur.BlurField`, and
`PathBlur.Path` has required typed image resources. Their resource identifiers
participated in graph dependencies and versioning. A specified resource that is
missing, disposed, from another graphics device, or otherwise unavailable
causes the configured `PrismFallbackPolicy` action and an observable diagnosis;
the executor does not silently substitute another filter.

## Bounds and optimization

Prepared passes carry device sampling radii and logical bounds radii as distinct
values. Separable expansions accumulate along the graph, so the final surface
covers every sampled pixel. Document-space effects whose samples remain inside
the source keep source bounds.

The optimizer removes a neighborhood node only when its prepared pass is an
exactly no-op and its opacity/blend state is neutral. Zero radius, zero amount,
and a degenerate one-pixel axis are evaluated in the planner. nonzero filters,
resource-driven filters, and non-normal blend modes retain their ordering.