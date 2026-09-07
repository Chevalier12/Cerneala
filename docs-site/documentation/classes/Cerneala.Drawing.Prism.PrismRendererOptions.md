# PrismRendererOptions Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismRendererOptions.cs`

Represents Prism surface budgets, a host color-profile value, and a development-diagnostics preference.

```csharp
public sealed class PrismRendererOptions
```

Inheritance: `object` -> `PrismRendererOptions`

## Examples

```csharp
using Cerneala.Drawing.Prism;

PrismRendererOptions defaults = new();
Console.WriteLine(defaults.SurfaceHardByteLimit);
```

Creating this object does not configure the SDL application backend.

## Remarks

The shipped SDL backend creates its options internally. Application code cannot
supply a `PrismRendererOptions` instance through `SdlGpuApplicationBackend`.
The old adapter's public configuration entry points have been removed; there is
no replacement configuration facade.

The type's default limits are 512 MiB hard, 256 MiB retained soft, and 256 retained
entries. SDL currently overrides the retained soft limit internally to 32 MiB.
These object defaults must not be mistaken for application-configurable SDL
settings. `HostColorProfile` and `EnableDevelopmentDiagnostics` do not provide
an application-level way to change SDL's host profile or diagnostics.

The internal consumer validates that limits are nonnegative, the retained soft
limit does not exceed the hard limit, and the color-profile enum value is valid.
Assigning an init-only property does not perform that validation itself.

## Properties

| Name | Type | Default | Description |
| --- | --- | ---: | --- |
| `SurfaceHardByteLimit` | `long` | 536,870,912 | Gets the combined transient and retained Prism GPU-surface byte limit. |
| `RetainedCacheSoftByteLimit` | `long` | 268,435,456 | Gets the retained-surface byte target. |
| `RetainedCacheEntryLimit` | `int` | 256 | Gets the retained-surface entry limit. |
| `HostColorProfile` | `PrismColorProfile` | `Srgb` | Gets the stored host color-profile value. |
| `EnableDevelopmentDiagnostics` | `bool` | `false` | Gets the stored development-diagnostics preference. |

## Applies to

Cerneala Prism option values and internal renderer resource configuration.

## See also

- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
- `Cerneala.UI.Detective.PrismRendererDiagnostics`
