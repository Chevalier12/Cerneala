# PrismRendererOptions Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Configures the MonoGame Prism host color profile, surface budgets, and optional
development diagnostics.

```csharp
public sealed class PrismRendererOptions
```

Inheritance:
`object` -> `PrismRendererOptions`

## Examples

```csharp
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;

PrismRendererOptions prismOptions = new()
{
    SurfaceHardByteLimit = 384L * 1024 * 1024,
    RetainedCacheSoftByteLimit = 192L * 1024 * 1024,
    RetainedCacheEntryLimit = 192,
    HostColorProfile = PrismColorProfile.ScRgb,
    EnableDevelopmentDiagnostics = true
};

using MonoGameDrawingBackend backend = new(
    spriteBatch,
    whitePixel,
    textRasterizer: null,
    prismOptions);
```

## Remarks

`SurfaceHardByteLimit` covers all transient and retained Prism GPU surfaces.
`RetainedCacheSoftByteLimit` is the retained subset's eviction target, and
`RetainedCacheEntryLimit` independently bounds workloads made from many small
surfaces. The retained soft limit cannot exceed the hard limit.

The measured defaults are 512 MiB hard, 256 MiB retained soft, and 256 retained
entries. A retained limit or entry limit of zero prevents retained promotion.
The options do not expose the internal cache-off conformance mode.

`HostColorProfile` declares the profile of pixels captured from the host and of
the destination restored for presentation. Its default is `Srgb`. Prism converts
from this profile into each composition's working profile and converts the final
result back into the same host profile exactly once. Nested compositions also
cross this declared host boundary, preventing an implicit sRGB reinterpretation.

When `HostColorProfile` is `ScRgb`, the host must supply linear BT.709/D65 pixels
and a floating-point RGBA destination such as `SurfaceFormat.HalfVector4`.
Prism preserves negative and greater-than-one RGB values and treats alpha as
linear premultiplied coverage. The host remains responsible for configuring its
swapchain color space as scRGB (`1.0` corresponds to 80 nits); selecting the
option cannot upgrade an existing 8-bit destination.

Counters in `PrismRendererDiagnostics` remain available when
`EnableDevelopmentDiagnostics` is `false`. Enabling it additionally classifies
which dependency-key fields changed on the most recent prepared Prism frame and
captures internal composition, pass, capture, surface, and fallback state. The
host's internal operational view combines that state with backdrop counters and
the current Motion state. The internal executed-graph dump uses deterministic
node aliases and redacts runtime GPU identifiers so its output can be compared
in CI.

Development snapshots copy value metadata only; they do not retain UI elements
or Prism instances. The disabled path keeps the primitive operational counters
but does not build dependency diffs, retain scope/pass snapshots, or format a
graph dump.

Validation occurs when the options are consumed by `MonoGameDrawingBackend` or
`MonoGameUiHost`; assigning init-only properties does not validate them by
itself.

## Properties

| Name | Type | Default | Description |
| --- | --- | ---: | --- |
| `SurfaceHardByteLimit` | `long` | 536,870,912 | Gets the maximum bytes owned by all Prism transient and retained surfaces. |
| `RetainedCacheSoftByteLimit` | `long` | 268,435,456 | Gets the retained-surface byte target. The cache evicts unpinned entries toward this limit; promotion is rejected when no entry can be evicted. |
| `RetainedCacheEntryLimit` | `int` | 256 | Gets the maximum number of retained surface entries. |
| `HostColorProfile` | `PrismColorProfile` | `Srgb` | Gets the color profile shared by host capture and presentation. Use `ScRgb` only with a linear floating-point host surface and matching swapchain configuration. |
| `EnableDevelopmentDiagnostics` | `bool` | `false` | Gets whether the renderer captures detailed dependency, execution-graph, and fallback diagnostics in addition to the always-on primitive counters. |

## Exceptions

| Consumer | Exception | Condition |
| --- | --- | --- |
| `MonoGameDrawingBackend` or `MonoGameUiHost` constructor | `ArgumentOutOfRangeException` | A byte or entry limit is negative, or `RetainedCacheSoftByteLimit` exceeds `SurfaceHardByteLimit`. |
| `MonoGameDrawingBackend` or `MonoGameUiHost` constructor | `ArgumentOutOfRangeException` | `HostColorProfile` is not a defined `PrismColorProfile` value. |

## Applies to

Cerneala MonoGame Prism rendering and UI hosting.

## See also

- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions`
