# Prism catalog filters

The filter catalog is the source of truth for every remaining Stylize,
Pixelate, Render, Video, artistic, texture, morphology, and Cerneala-native
filter. Generated descriptors carry defaults, validation, execution metadata,
coverage ownership, and documentation data. Runtime code consumes those
descriptors instead of maintaining a second filter list.

The generated [filter reference](prism-filter-reference.generated.md) contains
the complete filter/property/default matrix. This handwritten document keeps
only conceptual behavior and implementation conventions.

## Shared execution primitives

Filters are planned through reusable morphology, quantization, procedural,
video, artistic, edge-detection, tiling, texture, convolution, and color
primitives. Public filter identities remain distinct even when they share the
same mathematical primitive. This keeps markup semantics and diagnostics
specific while avoiding duplicate sampling and alpha code.

Maximum and Minimum use one prepared morphology pass. Their CPU conformance
paths decompose the digital footprint into chords and reuse recursively computed
line-window extrema; the GPU path evaluates the same footprint directly so both
paths produce the same component-wise result. Minimum honors `Preserve`:
`Roundness` selects a digital disk and `Squareness` selects a square. Facet and
Diffuse expand their catalog iteration count into explicit passes. The planner
computes sampling radii and conservative bounds once; the shader receives
prepared values and does not reinterpret markup.

Facet uses the generalized anisotropic Kuwahara construction with a Sobel
structure tensor, an edge-aligned elliptical kernel, and eight polynomially
weighted sectors. Every iteration uses a fixed three-pixel base radius; the
edge-aligned major semiaxis can reach six pixels at maximum anisotropy. Sector
means are combined by inverse variance, so flat regions become painterly patches
without averaging directly across strong edges. Transparent samples do not
contribute to the sector statistics, and the filtered color is associated with
the center pixel's alpha. CPU conformance math and the dedicated GPU technique
implement the same constants and weighting model.

Diffuse uses a clean-room coherence-enhancing shock filter based on
[Weickert](https://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf),
with the BSD-2-Clause
[reference implementation](https://github.com/bhoke/Coherence-Enhancing-Shock-Filters/blob/c8adf58d23280caac7b3d598e5891a34141ccd2a/shock_filter.m)
used only as an implementation reference. A Gaussian-weighted 3-by-3 structure
tensor is computed from linear-light luminance gradients. `Normal` selects the
shock sign from directional curvature, `DarkenOnly` performs directional
erosion, `LightenOnly` performs directional dilation, and `Anisotropic` uses
the tensor's dominant eigenvector and coherence. `Seed` changes only
low-coherence direction ties, while `Iterations` expands to explicit stable
passes. The luminance decision selects one straight RGB vector, which is then
associated with the center pixel's unchanged alpha. CPU and GPU paths share
the same tensor weights, eigenanalysis, time step, mode mapping, and hash.

Cutout uses a bounded joint spatial-range mean-shift approximation based on
[Comaniciu and Meer](https://doi.org/10.1109/34.1000236), followed by hard RGB
quantization. `EdgeSimplicity` expands to one through four explicit iterations
and scales a fixed 5-by-5 sample lattice up to an eight-pixel radius.
`EdgeFidelity` tightens the range bandwidth so samples across strong color
boundaries contribute less, while `Levels` selects between 2 and 32 final
channel levels. The iteration count and lattice are deliberately bounded:
there are no global labels, convergence-dependent passes, or hidden temporal
state. Intermediate passes preserve straight linear color and center alpha;
the final pass quantizes once, then applies opacity and blend mode against the
preserved original input. CPU conformance math and the dedicated GPU technique
use the same bandwidth mapping, weights, sampling positions, and quantization
contract.

Poster Edges uses the luminance-guided local linear model from
[He, Sun, and Tang](https://doi.org/10.1007/978-3-642-15549-9_1). Two separable
box-filter passes compute alpha-weighted luminance moments, a coefficient pass
derives the local `a` and `b` terms with `epsilon = 0.01`, and two more box
passes average those coefficients. The reconstructed luminance scales straight
linear RGB without changing hue or source alpha. A final Scharr pass draws
black edge ink and quantizes each channel once. `EdgeThickness` selects the
one- through eight-pixel guided-filter and Scharr radius after device scaling;
`EdgeIntensity` controls only the ink amount; and `Posterization` selects 2
through 32 tonal levels per channel. Intermediate GPU surfaces carry moments
and coefficients in half-float RGBA. The final pass alone applies opacity and
blend mode against the preserved original input. CPU and GPU paths share the
same epsilon, alpha weighting, reconstruction, quantization, edge kernel, and
clamps.

Film Grain uses a clean-room analytic approximation of the filtered Boolean
model statistics described by [Zhang et al.](https://doi.org/10.1145/3592127).
It synthesizes a deterministic unit-variance Gaussian-like field from seeded
procedural lattice samples, then applies the model's signal-dependent standard
deviation in linear light. A normalized 3-by-3 Gaussian radial-basis stencil
introduces spatial correlation without an external noise texture or frame
state. `Grain` controls correlation scale without changing variance,
`Intensity` controls amplitude, `HighlightArea` moves the variance peak from
midtones toward highlights, and `Seed` changes only the realization. The same
bounded distribution, hashes, stencil, luminance envelope, and alpha contract
are implemented by the CPU conformance path and GPU catalog shader.

Grain uses a clean-room, fixed-budget implementation of the inhomogeneous
Boolean film model described by [Newson, Delon, and Galerne](https://www.ipol.im/pub/art/2017/192/).
Each pixel evaluates a deterministic, signal-dependent point process over a
3-by-3 cell neighborhood, gives accepted grains distributed elliptical radii,
and computes their antialiased Boolean union. The expected coverage is removed
before `Intensity`, `Contrast`, and the selected `Type` shape the response, so
the effect preserves mean tone while retaining physical signal-dependent
variance. `Seed` changes only the realization. The CPU implementation lives in
`PrismGrainFilter.cs`; the matching GPU source is `Filters/Catalog/Grain.fx`.
Neither implementation copies or adapts GPL or AGPL source code.

Mezzotint uses clean-room 16-by-16 void-and-cluster threshold rank maps. The
screen preserves the source luminance statistically while avoiding the rigid
grid artifacts of ordered Bayer dithering. `FineDots`, `MediumDots`,
`GrainyDots`, and `CoarseDots` select one-pixel, two-pixel, alternate-grain, and
four-pixel dot screens. `ShortLines`, `MediumLines`, and `LongLines` use
three-, six-, and nine-pixel line footprints. The matching `ShortStrokes`,
`MediumStrokes`, and `LongStrokes` types use the same lengths with a two-pixel
stroke thickness. `Seed` deterministically selects screen phase and
horizontal or vertical orientation for line and stroke patterns. CPU and GPU
paths decode the same packed rank maps and preserve the source alpha.

Mosaic keeps its center-sample behavior when `PreserveEdges` is disabled. When
enabled, each cell uses a fixed three-by-three bilateral sample grid centered on
the cell representative. Spatial weights are normalized to the cell dimensions,
while range weights compare linear straight RGB and alpha with a fixed range
sigma of `0.25`. The associated-color weighted average stays constant across the
cell, rejects samples across strong color or coverage boundaries, and remains a
single deterministic pass without auxiliary textures. CPU and GPU paths use the
same sample positions and weighting constants.

Clouds and Difference Clouds use a clean-room implementation of
[Multi-Dimensional Procedural Wave Noise](https://pascalguehl.github.io/siggraph2025-wave-noise/assets/papers/papers_1206-compressed_low.pdf).
A 64-sample complex periodic table is precomputed
from 32 spectral bins, including the two-dimensional radial Jacobian, and packed
into a cached half-float GPU texture. Runtime evaluation stratifies up to 32
half-circle directions, jitters each direction inside its sector, interpolates
between irregular slices, and normalizes the effective directional energy.
`FrequencyRange` and `Spectrum` select the spectral distribution;
`DirectionCount` controls angular integration quality; `SliceThickness`
controls slice density; and `Anisotropy` stores the preferred axis in degrees
and the isotropy amount. `Clouds` maps the scalar field between its two colors.
`DifferenceClouds` takes the component-wise absolute difference between the
source and that same pattern. The CPU conformance path and dedicated GPU
technique share the table layout, hash, slicing, weighting, and normalization
contract.

Fibers uses five octaves of C2-interpolated gradient noise in a strongly
anisotropic coordinate system. A low-frequency transverse warp bends the
otherwise vertical field, while small fixed rotations on successive octaves
break lattice alignment without destroying longitudinal coherence. `Variance`
controls transverse scale, `Strength` controls normalized contrast, and `Seed`
selects the lattice gradients and warp phase. The generated scalar mixes
`Background` and `Foreground` in linear straight color, then associates the
result with the source alpha. CPU and GPU paths share the octave weights,
coordinate transforms, seed splitting, hash, fade curve, and normalization
constants.

Chalk Charcoal uses a clean-room tonal extended Difference of Gaussians
pipeline based on [Winnemoeller et al.](https://users.cs.northwestern.edu/~sco590/winnemoeller-cag2012.pdf)
and informed by the MIT-licensed
[AcerolaFX implementation](https://github.com/GarrettGunnell/AcerolaFX/blob/c33f779b093fa1e25faf0c77ef22c3fe6902e2fe/Shaders/AcerolaFX_DifferenceOfGaussians.fx).
Two separable passes build narrow and broad alpha-weighted luminance fields;
the final pass applies a soft XDoG threshold, separate dark charcoal and light
chalk tonal masks, and deterministic multiscale grain. `CharcoalArea` and
`ChalkArea` control the two Gaussian scales and tonal coverage,
`StrokePressure` controls threshold contrast and grain strength, and
`Foreground` and `Background` tint the resulting media. CPU and GPU paths
share the same scale mapping, XDoG constants, procedural grain, and associated
alpha contract. The bounded implementation deliberately omits iterative ETF.

Conte Crayon uses a five-pass Flow-XDoG pipeline informed by the MIT-licensed
[AcerolaFX Difference of Gaussians shader](https://github.com/GarrettGunnell/AcerolaFX/blob/c33f779b093fa1e25faf0c77ef22c3fe6902e2fe/Shaders/AcerolaFX_DifferenceOfGaussians.fx).
The first two passes build and refine an edge-tangent flow field, normal and
flow-aligned DoG passes produce coherent contours, and the final pass combines
those contours with four progressively activated hatch layers. `ForegroundLevel`
controls contour coverage, `BackgroundLevel` controls tonal hatching,
`Texture` selects the paper-height model, and `Scaling` changes its physical
frequency after device scaling. `Relief` and `LightDirection` shade the paper
normal, while `Foreground` and `Background` tint the two media endpoints. CPU
and GPU paths preserve source alpha and share the flow analysis, XDoG scale
mapping, hatch thresholds, paper models, and bounded two-temporary execution
contract.

Lighting Effects uses a metallic Cook-Torrance model with a GGX
normal-distribution function, correlated Smith visibility, Schlick Fresnel,
and Lambert diffuse response. `Gloss` maps to squared microfacet roughness
with a minimum of `0.045`, preventing singular highlights and unstable
sparkling at grazing angles. `Metallic` blends dielectric `F0 = 0.04` toward
the source color and suppresses the diffuse lobe; `Ambient` and `Exposure` are
applied in linear light.

The required `Lights` property resolves a `PrismLightingResource` containing
up to eight directional or point lights. Directional vectors point from the
surface toward the light. Point positions use normalized filter coordinates
with the surface in the XY unit square at `Z = 0` and inverse-square
attenuation. When `Texture` is present, four neighboring height samples form a
central-difference normal scaled by `TextureHeight`; without it, the filter
uses the flat `+Z` normal. CPU conformance math and the dedicated GPU technique
share the light packing, roughness floor, BRDF equations, alpha association,
and height-normal convention.

Lens Flare uses a clean-room sparse-polynomial optical transfer model based on
the [ray-transfer formulation of Bodonyi, Csoba, and Kunkli](https://link.springer.com/article/10.1007/s00371-024-03625-7)
and the [tiled ghost rasterization of Bodonyi and Kunkli](https://www.sciencedirect.com/science/article/pii/S0097849323001486),
both published under CC BY 4.0. A reusable `PrismLensProfileResource`
contains piecewise fits for aperture position, sensor position, transmission,
and lens-housing radius for every reflection path. Three wavelength samples
reproduce chromatic separation. At runtime Prism evaluates a coarse pupil grid,
rejects aperture- and housing-blocked rays, bins ghost triangles into 16-by-16
screen tiles, and rasterizes a cached half-float flare texture. The CPU
conformance path consumes the same tiled result, while the GPU catalog shader
composites the cached texture without changing source alpha.

`PrismLensProfileFitter` performs deterministic offline fitting with a shared
sparse basis and ridge-regularized least squares.
`PrismLensProfileJson` persists the validated profile for resource loading; the
`Lens` catalog property is therefore a required typed resource rather than a
symbolic preset.

## Determinism and resources

Procedural filters derive randomness only from the catalog seed property and
pixel coordinates. They do not read the clock or a global random generator, so
identical inputs and resource versions produce identical output.

Lighting Effects binds its typed lights resource and optional height texture as
separate prepared inputs. Texturizer accepts one optional texture, while Custom
Convolution requires its kernel resource. Resource identities participate in
the existing graph dependency and cache contracts.

## Color, alpha, and composition

CPU conformance math and GPU kernels work in the selected working profile with
premultiplied RGBA. Sampling results are clamped back to valid associated
alpha. Filter order is preserved because noncommutative catalog operations are
represented as separate graph nodes.

NtscColors applies the System M gamma of 2.2 to linear-sRGB straight color,
derives the NTSC YIQ chrominance amplitude, and uniformly reduces encoded RGB
until chrominance is at most 50 IRE and the `Y + chrominance` composite peak
is at most 110 IRE. Limit normalization includes the 7.5 IRE System M setup
level. It then removes the gamma encoding and restores the original alpha.
The supported contract is `Standard=NTSC` with
`Method=ReduceLuminance`; both symbols are validated by the planner. CPU and
GPU paths use the same transfer, setup level, YIQ coefficients, and signal
limits.

Catalog filters use the same layer and group graph boundaries as every other
Prism operation. Masks, clipping alpha, isolation, opacity, and blend modes are
applied outside the filter pass in their declared composition order.

## Conformance gallery

The conformance gallery is generated from the same catalog-derived filter set
used by the planner tests. Each entry contains one composition for one filter;
adding a supported catalog filter therefore adds a gallery case without a
hand-written view.
