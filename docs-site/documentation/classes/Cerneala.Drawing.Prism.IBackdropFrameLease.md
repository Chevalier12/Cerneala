# IBackdropFrameLease Interface

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/IBackdropFrameLease.cs`

Marks a backend-neutral lease that can supply backdrop input for a drawing frame.

```csharp
public interface IBackdropFrameLease
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;

IBackdropFrameLease lease = new FrameBackdropLease();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(new DrawCommandList());
DrawingFrameContext context = new(analysis, lease);

sealed class FrameBackdropLease : IBackdropFrameLease
{
}
```

## Remarks

The interface intentionally exposes no graphics API or resource members. A hosting integration can attach a concrete lease to `DrawingFrameContext` without making the drawing contract depend on MonoGame or another graphics backend.

`IBackdropFrameLease` does not define disposal semantics. Concrete lease ownership and lifetime remain the responsibility of the provider.

## Applies to

Cerneala backdrop-aware frame hosting and backend composition.

## See also

- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.Prism.Graph.PrismBackdropRequirement`
