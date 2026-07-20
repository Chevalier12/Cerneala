# PrismGraphBuilder Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphBuilder.cs`

Builds a deterministic retained composition graph from one current Prism frame analysis.

```csharp
public sealed class PrismGraphBuilder
```

Inheritance:
`object` -> `PrismGraphBuilder`

## Examples

```csharp
using Cerneala.Drawing;
using System.Numerics;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

PrismGraphBuilder builder = new();
BackdropFrameMetadata metadata = new(
    1920,
    1080,
    1,
    PrismColorProfile.Srgb,
    BackdropPixelFormat.Rgba8Unorm,
    BackdropAlphaMode.Premultiplied,
    Matrix3x2.Identity,
    42);
PrismGraph graph = builder.Build(analysis, metadata);
```

## Remarks

`Build` consumes `PrismFrameAnalysis` directly and does not rescan the draw command list. It verifies that the analysis is current both before and after construction.

Each non-empty analyzed scope receives one control-capture branch and a working-color conversion. Visible content is processed bottom-up; hidden or zero-opacity contributions are omitted. Composition-level settings are captured on every graph scope, and advanced layer settings are captured on each emitted layer node. Non-pass-through groups are marked as isolation boundaries. Masks, clipping-base alpha, and backdrop input use distinct graph branches and edge kinds.

When frame metadata is supplied, every visible backdrop branch depends on the
same `BackdropFrame` key and `ContentVersion`. The builder maps each analyzed
control rectangle through `CoordinateTransform`, clamps it to the source raster,
and emits `BackdropInput` -> `BackdropCrop` -> `ColorConversion`. Raster
metadata is attached only to the conversion node. Filters, styles, mask, and
opacity then run on that branch before its result becomes the background of the
control-content composite. Control capture and later UI nodes never feed the
backdrop branch.

The central backdrop-frame policy rejects incomplete metadata and
non-invertible coordinate transforms before graph construction. All declared
`BackdropPixelFormat` and `BackdropAlphaMode` values remain explicit in the
conversion node for the backend.

A pass-through group is built over the incoming stack background rather than an isolated transparent surface. Its explicit `PassThroughComposite` boundary receives the original background and the group's local result, then consumes that background so the parent stack does not composite it a second time. When the boundary becomes a clipping base, `ClipBaseAlpha` selects the group's local alpha instead of alpha already present in the original background.

Node IDs are derived from the retained scope owner, definition node ID, operation kind, and ordinal, so value-only changes update snapshots without changing graph identity.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphBuilder()` | Initializes a graph builder. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Build(PrismFrameAnalysis analysis)` | `PrismGraph` | Validates `analysis` and builds immutable nodes, edges, and scope outputs. |
| `Build(PrismFrameAnalysis analysis, in BackdropFrameMetadata backdropMetadata)` | `PrismGraph` | Builds the graph with an explicit shared backdrop-frame dependency, per-scope crop, and raster normalization metadata. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Build` | `ArgumentNullException` | `analysis` is `null`. |
| `Build` | `ArgumentException` | Supplied backdrop metadata is incomplete, unsupported, or has a non-invertible coordinate transform. |
| `Build` | `PrismGraphBuildException` | The analysis is stale or a composition/runtime value cannot be represented as a valid graph. |

## Applies to

Cerneala retained Prism graph construction between frame analysis and backend execution.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.Graph.PrismGraphCompositionSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphLayerSettings`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuildException`
- `Cerneala.Drawing.Prism.BackdropFrameMetadata`
