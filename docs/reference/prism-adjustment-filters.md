# Adjustment Prism filters

This document describes the shared mathematical conventions of the adjustment
filters. The list of filters, their properties, default values, domains, and their
planner, kernel, and conformance owners come from
`Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`; this document is not a
second source of truth for that data.

## Color pipeline

A filter receives the premultiplied working surface. The kernel unpremultiplies
once, converts the color to linear sRGB through the shared Prism pipeline, applies
the adjustment and the blend mode, then converts the result back into the working
profile and premultiplies it again with the same alpha. A pixel with zero alpha
stays zero. Gamma conversions, profile matrices, and alpha operations come from the
shared Prism pipeline.

All adjustments use the exact source bounds. They do not change layout, hit
testing, or surface size.

## Common primitives

- **Matrix** applies a 3x3 RGB matrix and a constant per channel.
- **Curve** compiles the `Composite`, `Red`, `Green`, and `Blue` points with
  shape-preserving PCHIP into a single `1024x1` RGB LUT. The channel curve is
  applied first, then the composite curve.
- **LUT** reads a canonical 3D Hald CLUT: a square image with side `level³` that
  represents a cube with side `level²`, with red varying fastest in row-major
  order. Sampling is trilinear; there is no other interpolation mode.
- **Channel mapping** selects the channel before the curve, level, or
  transformation.
- **Threshold** builds a linear-luminance histogram from a fixed 32x32 sample grid
  of the image, chooses the Otsu threshold with the maximum between-class variance,
  and uses the catalog `Level` value only as a fallback for degenerate or fully
  transparent images.
- **Levels** remaps the input range, gamma, and output range. `Auto=true` is meant
  to derive the input black and white points from the image histogram of the
  selected channel, clipping 0.1% at each tail and ignoring fully transparent
  pixels. That analysis exists only in the CPU reference
  (`PrismLevelsAnalysis`); the SDL_GPU executor does not run a Levels analysis
  pass.

## Filter semantics

| Filter | Prism semantics |
| --- | --- |
| `BrightnessContrast` | brightness in stops (`2^Brightness`) and exponential contrast around the pivot 0.18; the old linear variant stays available through `UseLegacy` |
| `Levels` | remaps input/gamma/output on RGB or on one channel; see `Auto` above |
| `Curves` | C1 PCHIP cubic for the composite and per-channel points, compiled into a 1024-sample RGB LUT |
| `Exposure` | exposure/contrast transform with `Linear`, `Video`, and `Logarithmic` styles, forward/reverse directions, a pivot, and configurable log parameters |
| `Vibrance` | polynomial curve in perceptual RGB for positive values, global desaturation for negative values, and an optional skin-tone mask |
| `HueSaturation` | per-pixel Okhsl adjustment, with hue weighted by the color range, saturation normalized to the sRGB gamut, and perceptual lightness; `Colorize` also works in the same space |
| `ColorBalance` | weighted corrections for shadows, midtones, and highlights, with optional luminance preservation |
| `BlackWhite` | configurable RGB monochrome mixer (`Red·R + Green·G + Blue·B`), with optional normalization by `abs(1 / (Red + Green + Blue))`; the default is `0.333` on each channel |
| `PhotoFilter` | linear per-channel blend between the source and the filter color, controlled by `Density`, with alpha copied unchanged |
| `ChannelMixer` | RGB subset of a 4x5 matrix: three RGB rows plus constants, applied after unpremultiply and followed by clamp/premultiply |
| `ColorLookup` | 3D Hald CLUT from the versioned resource, with canonical row-major indexing and trilinear interpolation (`PrismHaldLut`) |
| `Invert` | RGB complement in linear space |
| `Posterize` | uniform quantization to the catalog number of levels |
| `Threshold` | black/white by the Otsu threshold of linear luminance; a histogram with one occupied level returns that level, and one with no visible pixels uses the catalog fallback |
| `GradientMap` | maps linear CIE luminance through the gradient into linear sRGB; `Reverse` flips the gradient and `Dither` adds a deterministic 4x4 ordered (Bayer) dither offset |
| `SelectiveColor` | Photoshop/FFmpeg CMYK correction on Reds, Yellows, Greens, Cyans, Blues, Magentas, Whites, Neutrals, and Blacks; `Method` is `Perceptual`, `Relative`, or `Absolute`, and `Perceptual` uses the same math as `Relative` |

`SelectiveColor` keeps the FFmpeg formula per range and channel: it combines the
CMY component with `K` through `((-1 - adjustment) * K) - adjustment`, applies the
factor `1 - value` in relative mode, limits the contribution to the valid channel
range, and only then weights it with the range mask.

`GradientMap` requires a valid `PrismGradientMapResource` whose points cover
`[0, 1]` with nondecreasing offsets; two points may share an offset, but a
decreasing offset throws `ArgumentException`. The resource is versioned, and a
missing resource produces the fallback copy.

`Curves` requires a valid `PrismCurvesResource`. Points use normalized coordinates,
have strictly increasing inputs, and include the zero and one end points. The LUT is
versioned together with the resource and the executor cache; the shader reads one
sample per channel, with no additional pass.

`Vibrance` temporarily converts linear color to perceptual sRGB and preserves alpha.
`Amount>0` applies a polynomial response that falls to zero as the color is already
saturated; `Amount<0` uses a separate desaturation path.
`AvoidSaturatingSkinTones=true` only damps the positive boost in the skin-tone
sector. `GrayColorTransform` configures the gray axis, and `Saturation` stays a
global adjustment applied after vibrance.

`ColorLookup` requires a valid Hald CLUT image, encoded in linear RGB: its side must
be exactly `level³` for some `level >= 2`. The executor validates this shape before
the GPU pass, indexes the texel with `r + size * (g + size * b)`, and unpremultiplies
the LUT texel when sampling. `ColorLookup` has no `Interpolation` or `Intensity`
property; to mix the result with the source, use the filter's common `Opacity`.
The source alpha stays unchanged and a pixel with zero alpha stays zero. If the
resource is missing, has an invalid shape, or cannot be used, the executor applies
the Prism fallback policy and records a diagnostic; it never silently substitutes
another LUT.

## Conformance

Analytical vectors check opaque and transparent pixels, associated alpha, limit
values, individual channels, and all selectable color profiles. The results in this
family are analytical, so the adjustments stage needs no separate golden raster.
The semantics above are the Prism contract and do not claim byte-for-byte
compatibility with proprietary implementations.
