# DrawingFrameContext Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawingFrameContext.cs`

Carries the public, backend-neutral resources borrowed for one drawing submission.

```csharp
public readonly struct DrawingFrameContext
```

## Examples

```csharp
using Cerneala.Drawing;

public sealed class InspectingBackend : IDrawingBackend
{
    public void Render(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        bool hasBackdrop = frameContext.BackdropLease is not null;
        Console.WriteLine($"Commands: {commands.Count}; backdrop: {hasBackdrop}");
    }
}
```

## Remarks

`UiHost` creates the context and passes it with the matching `DrawCommandList` to `IDrawingBackend.Render`. `StateAnalysis` is the shared, version-bound interpretation used for stack validation, world bounds, damage tracking, backend submission, and Prism analysis.

`BackdropLease` is optional. The context borrows the lease but does not dispose it; the host that acquired the lease ends the borrow after submission, including exceptional exits. Backends should not retain the context or lease beyond `Render`.

The default struct value has no backdrop lease and is not a substitute for a host-created frame submission.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BackdropLease` | `IBackdropFrameLease?` | Gets the optional backend-neutral backdrop lease borrowed for this submission. |
| `StateAnalysis` | `DrawCommandStateAnalysis` | Gets the common state-stack analysis for the submitted command list. |

## Applies to

Cerneala retained frame submission and drawing backend integration.

## See also

- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.Prism.IBackdropFrameLease`
