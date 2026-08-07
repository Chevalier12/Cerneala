# PrismCatalog Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Catalog/PrismCatalog.cs`

Provides immutable runtime discovery metadata for the built-in Prism filter and style catalog.

```csharp
public static class PrismCatalog
```

## Examples

```csharp
PrismCatalogOperationInfo blur = PrismCatalog.GetFilter(PrismFilterId.Blur);
foreach (PrismCatalogParameterInfo parameter in blur.Parameters)
{
    Console.WriteLine($"{parameter.Name}: {parameter.ValueKind}");
}
```

## Remarks

The catalog is projected from the same generated descriptors used by Prism markup and rendering. Applications can therefore build editors and diagnostics without copying stable identifiers, parameter slots, defaults, or numeric domains.

Operations with a required resource parameter report `RequiresResource=true`. The catalog does not create or import those resources.

The `Shear` filter reports a signed, unitless `Amount` parameter whose default is `0`. Its magnitude is a fraction of source width; `Curve` selects a centered, shape-preserving piecewise cubic Hermite displacement profile, and `UndefinedAreas` selects boundary sampling.

The `Clouds` and `DifferenceClouds` descriptors expose the complete Wave Noise contract through `DirectionCount`, `SliceThickness`, `FrequencyRange`, `Anisotropy`, and `Spectrum`, in addition to the shared colors, scale, and seed. `Anisotropy.X` is the preferred axis in degrees and `Anisotropy.Y` ranges from `0` for directional noise to `1` for isotropic noise. `Spectrum` accepts `White`, `Blue`, `Pink`, or `Brown`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Version` | `string` | Gets the machine-readable catalog version. |
| `Filters` | `ImmutableArray<PrismCatalogOperationInfo>` | Gets all built-in filter descriptors in stable catalog order. |
| `Styles` | `ImmutableArray<PrismCatalogOperationInfo>` | Gets all built-in style descriptors in stable catalog order. |

## Methods

| Name | Description |
| --- | --- |
| `GetFilter(PrismFilterId)` | Gets metadata for one built-in filter. |
| `GetStyle(PrismStyleId)` | Gets metadata for one built-in style. |

## Applies to

The `Cerneala` project and generated Prism catalog.
