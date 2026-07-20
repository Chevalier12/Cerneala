# PrismGraphDependencyKind Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Identifies a versioned input that can invalidate a Prism graph node or retained result.

```csharp
public enum PrismGraphDependencyKind
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static bool DependsOnPixels(PrismGraphNode node) =>
    node.Dependencies.Any(
        dependency =>
            dependency.Kind == PrismGraphDependencyKind.VisualContent);
```

## Remarks

Every emitted node carries the scope structure, value, and descendant stamps.
Nodes add narrower dependencies for captured pixels, bounds, scale, effective
transform, color profile, catalog entries, resources, and a borrowed backdrop
frame when those inputs affect their output. `Bounds` tracks the already
transformed logical rectangle; `Transform` independently tracks the matrix that
produced it. Every backdrop input that borrows the same frame carries an equal
`BackdropFrame` key and content version.

## Fields

| Name | Description |
| --- | --- |
| `Structure` | Tracks the scope composition structure version. |
| `Values` | Tracks the scope runtime value version. |
| `VisualContent` | Tracks retained control content changes. |
| `Descendants` | Tracks the stable aggregate version of nested Prism scopes. |
| `Bounds` | Tracks the analyzed transformed and clipped logical scope bounds. |
| `PixelScale` | Tracks the scope pixel scale. |
| `Transform` | Tracks the effective logical transform used for control capture. |
| `ColorProfile` | Tracks the composition working color profile. |
| `CatalogEntry` | Tracks the selected generated filter or style catalog entry. |
| `Resource` | Tracks an image or catalog parameter resource identity. |
| `BackdropFrame` | Tracks the shared borrowed backdrop raster contract and its `ContentVersion`. |

## Applies to

Cerneala retained Prism graph invalidation and cache-key construction.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphDependency`
- `Cerneala.Drawing.Prism.Graph.PrismDependencyStamp`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
