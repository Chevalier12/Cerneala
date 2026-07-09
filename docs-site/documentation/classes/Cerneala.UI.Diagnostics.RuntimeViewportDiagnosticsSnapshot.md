# RuntimeViewportDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/RuntimeDiagnostics.cs`](../../UI/Diagnostics/RuntimeDiagnostics.cs)

Represents the logical viewport dimensions and scale captured during a runtime diagnostics snapshot.

```csharp
public sealed record RuntimeViewportDiagnosticsSnapshot(
    float LogicalWidth,
    float LogicalHeight,
    float Scale)
```

Inheritance:
`Object` -> `RuntimeViewportDiagnosticsSnapshot`

Implements:
`IEquatable<RuntimeViewportDiagnosticsSnapshot>`

## Examples

Capture viewport diagnostics through `RuntimeDiagnostics.Capture`:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(320, 180, 1.5f);
UiViewport viewport = new(320, 180, 1.5f);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

float width = snapshot.Viewport.LogicalWidth;
float height = snapshot.Viewport.LogicalHeight;
float scale = snapshot.Viewport.Scale;
```

## Remarks

`RuntimeViewportDiagnosticsSnapshot` is the `Viewport` component of `RuntimeDiagnosticsSnapshot`. `RuntimeDiagnostics.Capture` creates it from the supplied `UiViewport` by copying `UiViewport.Width` to `LogicalWidth`, `UiViewport.Height` to `LogicalHeight`, and `UiViewport.Scale` to `Scale`.

The type is a positional record, so its public constructor can also be used directly when a diagnostic snapshot needs explicit viewport values. The constructor does not perform validation; values captured from `UiViewport` have already passed `UiViewport` validation.

`RuntimeDiagnosticsSnapshot.ToString()` includes these values in the formatted runtime line as `runtime viewport={LogicalWidth}x{LogicalHeight}, scale={Scale}`.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimeViewportDiagnosticsSnapshot(float logicalWidth, float logicalHeight, float scale)` | Initializes the snapshot with logical viewport dimensions and scale. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LogicalWidth` | `float` | Gets the logical viewport width copied from `UiViewport.Width`. |
| `LogicalHeight` | `float` | Gets the logical viewport height copied from `UiViewport.Height`. |
| `Scale` | `float` | Gets the viewport scale copied from `UiViewport.Scale`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out float LogicalWidth, out float LogicalHeight, out float Scale)` | `void` | Deconstructs the positional record into its public component values. |
| `ToString()` | `string` | Returns the compiler-generated positional record string for the snapshot. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- [`RuntimeDiagnostics`](Cerneala.UI.Diagnostics.RuntimeDiagnostics.md)
- [`RuntimeDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md)
- [`UiViewport`](Cerneala.UI.Hosting.UiViewport.md)
