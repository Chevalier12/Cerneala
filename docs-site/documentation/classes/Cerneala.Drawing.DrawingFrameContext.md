# DrawingFrameContext Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawingFrameContext.cs`

Carries the typed, backend-neutral analysis and optional backdrop lease for one drawing submission.

```csharp
public readonly struct DrawingFrameContext
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
DrawingFrameContext frameContext = new(analysis);

frameContext.EnsureCurrent(commands);
```

## Remarks

`DrawingFrameContext` binds a `PrismFrameAnalysis` to the command list that was analyzed for the current frame. Pass the same context and command-list instance to `IDrawingBackend.Render`.

`BackdropLease` is optional and uses the backend-neutral `IBackdropFrameLease` contract. The context borrows the lease but does not dispose it. The host that acquired the lease ends the borrow after submission, including exceptional exits.

The default struct value is not initialized. Calling `EnsureCurrent` on it throws `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `DrawingFrameContext(PrismFrameAnalysis prismAnalysis, IBackdropFrameLease? backdropLease = null)` | Creates a frame context from a non-null Prism analysis and an optional backdrop lease. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PrismAnalysis` | `PrismFrameAnalysis` | Gets the Prism analysis created for the submitted command list. |
| `BackdropLease` | `IBackdropFrameLease?` | Gets the optional backend-neutral lease for backdrop input. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnsureCurrent(DrawCommandList commands)` | `void` | Verifies that the context is initialized and that its Prism analysis still matches `commands`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `prismAnalysis` is `null`. |
| `EnsureCurrent` | `ArgumentNullException` | `commands` is `null`. |
| `EnsureCurrent` | `InvalidOperationException` | The context is uninitialized, the command list does not match the analyzed instance or version, or a Prism scope has changed since analysis. |

## Applies to

Cerneala retained frame submission and drawing backend integration.

## See also

- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.Prism.IBackdropFrameLease`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
