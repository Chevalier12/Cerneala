# Adjustment Prism filters

This document describes common mathematical conventions for filters
adjustment. List of filters, properties, default values, domains,
planner, kernel and conformance owners are generated from
`Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`; the document is not a a
two sources of truth for that data.

## Pipe color

A filter receives the premultiplied working surface. The kernel o
depremultiply once, convert color via common Prism pipeline
in linear sRGB, apply the adjustment and blend mode, then convert the result
back into the working profile and premultiplies it with the same alpha. The pixel with
alpha zero remains zero. Gamma conversions, profile matrices and operations
alpha are the common ones from the Prism pipeline; filters do not have local copies of
these formulas.

All adjustments use the exact source bounds. They do not change the layout,
the hitbox or surface size.

## Common primitives

- **Matrix** applies a 3x3 RGB matrix and a constant per channel.
- **Curve** compile points `Composite`, `Red`, `Green` and `Blue` with PCHIP
  shape-preserving in a single LUT RGB `1024x1`. The channel curve is applied
  first, then the composite curve.
- **LUT** reads a canonical Hald CLUT 3D: a square image with side
  `level³`, which represents a cube with side `level²`, with red varying the most
  quickly in row-major order. Supports tetrahedral or trilinear interpolation.
- **Channel mapping** selects the channel before the curve, level or
  transformation.
- **Threshold** calculates a global linear luminance histogram, chooses the Otsu threshold with maximum between-class variance, and uses the cataloged parameter only as a fallback for degenerate or fully transparent images.
- **Levels** remaps input range, gamma and output range.
  With `Auto=true`, build on the GPU a synchronized distribution for
  selected channel, remove 0.1% of each tail and use limits
  results as entry points. The analysis ignores fully transparent pixels
  and doesn't read the texture back to the CPU.

## Semantics of filters

| Filter | Prism Semantics |
| --- | --- |
| `BrightnessContrast` | brightness in taillights and exponential contrast around pivot 0.18; the old linear variant remains selectable via `UseLegacy` |
| `Levels` | remap input/gamma/output on RGB or on one channel; `Auto` chooses robust thresholds from the synchronized image percentile |
| `Curves` | PCHIP cubic C1 for composite dots and per-channel, compiled into a 1024-sample RGB LUT |
| `Exposure` | exposure/contrast transformation with linear, video and logarithmic styles, forward/reverse directions, pivot and configurable log parameters |
| `Vibrance` | polynomial curve in perceptual RGB for positive values, global desaturation for negative values ​​and optional mask for skin tones |
| `HueSaturation` | point Okhsl adjustment, with hue weighted on the color range, saturation normalized to the sRGB gamut and perceptual lightness; `Colorize` also interpolates in the same space |
| `ColorBalance` | weighted corrections for shadows, midtones and highlights, with optional luminance preserved |
| `BlackWhite` | configurable RGB monochrome mixer (`Red·R + Green·G + Blue·B`), with optional normalization via `abs(1 / (Red + Green + Blue))`; default values ​​are `0.333` on each channel |
| `PhotoFilter` | linear blend per channel between source and filter color, controlled by `Density`, with alpha copied unchanged |
| `ChannelMixer` | RGB subset of a 4x5 matrix: three RGB rows plus constants, applied after unpremultiply and followed by clamp/premultiply |
| `ColorLookup` | Hald CLUT 3D from the versioned resource, with canonical row-major indexing and trilinear interpolation as in `HaldClutImage` |
| `Invert` | RGB complement in the linear space |
| `Posterize` | uniform quantization to the cataloged number of levels |
| `Threshold` | black/white by global Otsu threshold of linear luminance; a uniform histogram returns the only occupied level, and one with no visible pixels uses the cataloged fallback |
| `GradientMap` | 1D linear-sRGB LUT transfer versioned by linear CIE luminance, with Bayer 4x4 deterministic interpolation, reverse and dithering |
| `SelectiveColor` | CMYK Photoshop/FFmpeg correction on Reds, Yellows, Greens, Cyans, Blues, Magentas, Whites, Neutrals and Blacks, in Relative or Absolute mode |

`SelectiveColor` keeps the FFmpeg formula per interval and channel: combine
CMY component with `K` through `((-1 - adjustment) * K) - adjustment`, apply
factor `1 - value` in Relative mode, limits the contribution to the range
valid channel and only then weights it with the interval mask.

`GradientMap` requires a valid `PrismGradientMapResource` with strictly ascending points covering `[0, 1]`. The resource is versioned and cached as a 256-sample 1D LUT; its absence produces fallback copy.

`Curves` requires a valid `PrismCurvesResource`. Points use coordinates
normalized, have strictly increasing input and include zero and one ends. the LUT
is versioned along with the resource and executor cache; the shader does how much
one sample for each channel, no additional pass.

`Vibrance` temporarily converts linear color to perceptual sRGB and preserves
alpha. `Amount>0` applies a polynomial response that decays to zero as
the color is already saturated; `Amount<0` uses a separate desaturation pathway.
`AvoidSaturatingSkinTones=true` only dampens the positive momentum in the sector
skin tones. `GrayColorTransform` configures the gray axis again
`Saturation` remains a global adjustment applied after vibrance.

`ColorLookup` requires a valid Hald CLUT image, encoded in linear RGB:
its side must be exactly `level³` for a `level >= 2`. The executor
validate this shape before the GPU, index the texel via
`r + size * (g + size * b)` and depremultiply the LUT texel to samples.
The default interpolation is tetrahedral; `Linear`/`Trilinear` select
trilinear. `Intensity` mixes the LUT output with the source after converting to
linear sRGB. The source alpha remains unchanged and a pixel with zero alpha remains zero.
If the resource is missing, has an invalid form, or cannot be used, the executor
apply the Prism fallback policy and publish a diagnostic; does not replace
The LUT in stealth.

## Compliance

Analytical vectors check opaque and transparent pixels, associated alpha, values
limit, individual channels and all selectable color profiles.
Interactions in this family have sufficient analytical results, so no
a separate golden raster is required for the adjustments stage. Semantics of
above is the Prism contract and does not claim byte-for-byte compatibility with
proprietary implementations.