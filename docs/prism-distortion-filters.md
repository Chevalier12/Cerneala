# Prism distortion, transform, and resampling filters

The Prism distortion family uses one prepared coordinate-mapping pipeline. The
catalog remains the source of truth for property names, defaults, ranges, typed
resources, kernel ownership, tests, and generated reference documentation.

## Execution contract

- The captured control pixels are always the implicit filter input. Distortion
  filters do not expose a generic `Source`, shader filename, or runtime shader
  source.
- The planner resolves typed catalog values once, converts logical distances to
  device distances, prepares radians and skew tangents, selects edge behavior,
  records resource requirements, and emits one or more explicit passes.
- Kernels receive only prepared numeric options and typed image resources. They
  do not parse markup values or repeat catalog defaults.
- Coordinate sampling runs in the composition working profile with associated
  alpha. Filtering converts to linear sRGB for math and converts back through
  the shared color pipeline.
- Source sampling uses the common bilinear sampler and supports clamp,
  transparent, wrap, mirror, and background fill edge behavior. `Liquify`
  uses a Keys bicubic kernel with `A = -0.75` while its local Jacobian remains
  orientation-preserving and bounded, then blends back to bilinear sampling
  across folded or extreme footprints. `Twirl` and `Wave` combine bilinear
  fetches through bounded anisotropic footprints.
  `Displace` samples its map independently with point sampling, so encoded
  channel values cannot be blended with neighboring map pixels.

## Visual transform

`Transform` applies inverse coordinate mapping around the normalized `Origin`,
in this order: translation, rotation, skew, and scale. The optimizer applies the
matching forward transform to visual pixel bounds. Partial opacity or a
non-normal blend conservatively unions source and transformed bounds.

This is a Prism visual operation only. It does not change measure, arrange,
control bounds, input routing, or hit testing. Nested transforms remain graph
passes over the same implicit captured control source.

## Typed resources

| Filter | Resource | Contract |
| --- | --- | --- |
| `Displace` | `Map` | Required raw encoded displacement data; RGBA or luminance channels are selected without color-profile conversion |
| `Glass` | `TextureImage` | Optional raw scalar height surface used only by `TextureImage`; RGB is reduced to luminance without working-profile conversion, and a missing image is a neutral flat surface |
| `Liquify` | `Mesh` | Required two-channel displacement mesh |
| `Liquify` | `Mask` | Optional alpha mask, independently invertible |

Missing required resources follow `PrismFallbackPolicy` and produce observable
diagnostics. Optional resources have an explicit procedural or unmasked
behavior; there is no silent substitution with an unrelated resource.

## Filter semantics

| Filter | Prepared mapping or pass behavior |
| --- | --- |
| `Transform` | Inverse affine coordinate map with prepared visual bounds |
| `AdaptiveWideAngle` | Calibrated inverse fisheye map from normalized focal lengths, principal point, and four radial coefficients; samples outside the source transparently |
| `LensCorrection` | Radial distortion, chromatic offsets, vignette, perspective, rotation, and scale |
| `DiffuseGlow` | Explicit diffuse pass followed by deterministic grain/clear pass |
| `Displace` | Raw selected map channels are clamped to `[0, 1]`, centered at `0.5`, and drive independent horizontal and vertical displacement; `Tile` repeats the map at its native pixel size |
| `Glass` | One-pass scalar height-field displacement. `Frosted`, `TinyLens`, `Blocks`, and `Canvas` generate distinct deterministic surfaces; `Smoothness` controls the gradient footprint, `Scaling` controls feature size, `Distortion` controls the bounded displacement amplitude, and `Invert` reverses the surface gradient |
| `OceanRipple` | One-pass, seeded two-octave coherent domain warp over a triangular gradient lattice. `RippleSize` controls feature size in device pixels (clamped to one pixel), while `RippleMagnitude` controls displacement amplitude |
| `Pinch` | Centered sine-power radial mapping with finite image-filling elliptical support and a bounded signed amount |
| `PolarCoordinates` | Aspect-correct rectangular-to-polar or polar-to-rectangular conversion in pixel space. An analytic Jacobian drives a bounded eight-tap Gaussian EWA footprint with 8:1 maximum anisotropy; angular coordinates wrap, while radial and Cartesian overflow remains transparent |
| `Ripple` | One-pass seeded directional ripple combining two sinusoidal bands with coherent phase modulation. `Size` maps `Small`, `Medium`, and `Large` to stable 8, 16, and 32 device-pixel wavelengths; `Amount` controls device-pixel displacement and `EdgeMode` controls boundary sampling |
| `Shear` | One-pass inverse horizontal warp driven by signed normalized `Amount` and a centered, shape-preserving piecewise cubic Hermite `Curve`; all presets pass through the source center and `UndefinedAreas` controls boundary sampling |
| `Spherize` | One-pass inverse orthographic spherical-cap projection with bilinear sampling. `Amount` is saturated internally to `[-1, 1]`: positive values use the inverse `asin` projection, negative values use its forward `sin` projection, and zero is an exact no-op. `Center` translates a fixed half-surface support ellipse; `HorizontalOnly` and `VerticalOnly` use independent one-dimensional mappings |
| `Twirl` | One-pass centered radius-falloff rotation. An analytic Jacobian selects the mapped footprint's major axis, clamps its support to eight source pixels, and uses fixed one-, four-, or eight-tap Gaussian-weighted sampling to suppress aliasing while preserving deterministic linear-premultiplied color behavior |
| `Wave` | One-pass bank of up to 32 globally seeded plane-wave generators. Each generator derives a stable direction and phase from `Seed`, then draws its wavelength and amplitude from the configured min/max intervals; `Scale` controls the accumulated horizontal and vertical displacement. Energy-normalized accumulation keeps added generators visually stable. Sine is box-filtered analytically in two dimensions, Triangle and Square use footprint-limited odd harmonics, and an analytic Jacobian selects bounded one-, four-, or eight-tap anisotropic source sampling |
| `ZigZag` | One-pass aspect-correct sinusoidal polar warp in device-pixel space. `Ridges` is the exact number of displacement-direction reversals from the center to the farthest corner, while a radial sine envelope makes the displacement continuous at the center and outer support. `Amount` is saturated to `[-1, 1]`, and the amplitude is bounded analytically to keep the maximum coordinate-map slope below `0.85`. `PondRipples` displaces along the upper-left/lower-right diagonal, `OutFromCenter` displaces radially, and `AroundCenter` rotates while preserving radius |
| `Liquify` | One-pass inverse displacement from the required two-channel mesh, attenuated by `Reconstruct` and the optional alpha mask. A finite-difference Jacobian detects orientation reversal and extreme source footprints; stable regions use a 4-by-4 Keys bicubic source kernel, while unsafe regions transition deterministically to bilinear sampling |
| `Offset` | Pixel offset with wrap, clamp, transparent, mirror, or fill behavior |

`OceanRipple`, `Ripple`, and `Wave` use the catalog `Seed`. `DiffuseGlow` uses a
fixed deterministic grain sequence. No distortion filter reads wall-clock time
or a global random-number generator.

## Bounds and optimization

Coordinate-only distortions preserve source bounds except `Displace` and
`Glass`, which conservatively expand each axis by half their maximum prepared
displacement after the effective visual transform, and `Transform`, which
applies its matching forward transform. A pass is removed only when its
prepared parameters prove a mathematical no-op and its opacity and blend
settings are neutral. Resource dependencies and their versions remain attached
to the graph even when an adjacent no-op is elided.
