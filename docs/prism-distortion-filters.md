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
- `Linear` is the approved sampling mode. The common sampler performs bilinear
  interpolation and supports clamp, transparent, wrap, mirror, and background
  fill edge behavior.

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
| `AdaptiveWideAngle` | `Constraints` | Required coordinate constraint map |
| `Displace` | `Map` | Required channel-addressed displacement map |
| `Glass` | `TextureImage` | Optional texture when the selected texture mode uses it |
| `Liquify` | `Mesh` | Required two-channel displacement mesh |
| `Liquify` | `Mask` | Optional alpha mask, independently invertible |

Missing required resources follow `PrismFallbackPolicy` and produce observable
diagnostics. Optional resources have an explicit procedural or unmasked
behavior; there is no silent substitution with an unrelated resource.

## Filter semantics

| Filter | Prepared mapping or pass behavior |
| --- | --- |
| `Transform` | Inverse affine coordinate map with prepared visual bounds |
| `AdaptiveWideAngle` | Projection, focal/crop correction, constraint map, rotation, translation, and scale |
| `LensCorrection` | Radial distortion, chromatic offsets, vignette, perspective, rotation, and scale |
| `DiffuseGlow` | Explicit diffuse pass followed by deterministic grain/clear pass |
| `Displace` | Selected map channels drive independent horizontal and vertical displacement |
| `Glass` | Procedural or typed texture displacement with scaling and inversion |
| `OceanRipple` | Seeded two-axis ripple field |
| `Pinch` | Centered radial power mapping |
| `PolarCoordinates` | Rectangular-to-polar or polar-to-rectangular conversion |
| `Ripple` | Seeded directional ripple with catalog edge behavior |
| `Shear` | Catalog curve mapping with explicit undefined-area handling |
| `Spherize` | Radial sphere mapping, optionally restricted to one axis |
| `Twirl` | Centered radius-falloff rotation |
| `Wave` | Seeded sine, triangle, or square displacement generators |
| `ZigZag` | Radial or angular ridge mapping around the selected center |
| `Liquify` | Typed mesh displacement blended by reconstruction and optional mask |
| `Offset` | Pixel offset with wrap, clamp, transparent, mirror, or fill behavior |

`OceanRipple`, `Ripple`, and `Wave` use the catalog `Seed`. `DiffuseGlow` uses a
fixed deterministic grain sequence. No distortion filter reads wall-clock time
or a global random-number generator.

## Bounds and optimization

Coordinate-only distortions preserve source bounds. `Transform` is the only
filter in this family that changes visual bounds. A pass is removed only when
its prepared parameters prove a mathematical no-op and its opacity and blend
settings are neutral. Resource dependencies and their versions remain attached
to the graph even when an adjacent no-op is elided.
