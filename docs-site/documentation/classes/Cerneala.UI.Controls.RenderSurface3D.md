# RenderSurface3D Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/RenderSurface3D.cs`

Hosts callback-recorded 3D lines and markers behind ordinary retained `Content`.

```csharp
public class RenderSurface3D : ContentControl, ITimeSensitiveRenderElement
```

Inheritance: `object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `RenderSurface3D`

## Examples
```csharp
RenderSurface3D surface = new();
surface.Draw += (_, frame) =>
    frame.DrawLine(Vector3.Zero, Vector3.UnitY, Color.White, 2);
```

## Remarks
The default right-handed camera looks from `(0,0,5)` toward the origin with Y up. `OnDemand` is the default redraw mode. Camera changes and `InvalidateFrame()` request rendering without requesting measure or arrange. The callback-scoped frame captures one camera, viewport, time, DPI, clear color, and generation snapshot; changes made during a callback apply to a later frame and do not recursively re-record the active callback.

`ViewMatrix`, `Projection`, `ClearColor`, and the frame generation describe requested recording state. Changing requested state does not state that a backend has presented it. A `Draw` callback observes the snapshot selected for that recording; backend submission is later than recording.

The SDL_GPU backend renders callback-recorded lines and markers into an isolated color-and-depth target, then composites that image with 2D drawing, clipping, layers, opacity, and Prism. The color and depth formats must support a common multisample count of at least two; the backend chooses eight, four, or two samples in that order. If no such common count exists, rendering a `RenderSurface3D` command throws `NotSupportedException` rather than silently using 1x. This requirement is specific to 3D; it does not change `RenderSurface2D` or ordinary 2D drawing.

Homogeneous clip-space z is `[0, w]`; after perspective division, normalized depth is `[0, 1]`. The 3D pass clears depth to `1` and uses read/write LessOrEqual depth testing: nearer covered samples win regardless of recording order, while the last covered primitive wins at equal depth. The fragment shader discards samples outside a marker's circular footprint before writing depth. This is geometry depth within the isolated 3D pass, not a replacement for 2D painter order around the composed surface.

With no `Draw` subscriber, the control emits no 3D command or target work; its ordinary background, border and `Content` still render. An active callback that records no primitives still emits a surface whose pixels come only from `ClearColor`, including a transparent clear. A transparent clear color is permitted, but lines and markers themselves must be opaque.

The backend reuses an unchanged submitted 3D raster without re-invoking `Draw`. A target produced only by a failed or cancelled command buffer is recorded again before reuse. Removing the final `Draw` subscriber, detaching the control, or releasing its root's drawing resources retires that control's session-local target. A retained command captured before that retirement cannot reacquire it; a fresh render after attachment, subscription, or root resource replacement can record a new target. These operations do not dispose device-wide immutable pipelines or another window's target. A target referenced by an unfinished command buffer stays alive until that buffer submits or is cancelled, even when another window on the same device completes a frame.

World/root conversion includes the arranged origin, physical raster dimensions rounded up from logical dimensions and root scale, and ancestor visual transforms. A successful root-to-world conversion returns an origin on the near plane and a finite unit direction toward the far plane. Conversion returns `false` and writes the default output for non-finite input, an empty or unrepresentable viewport, a root point outside the viewport, a world point outside the visible frustum, a singular or non-finite visual transform, or non-finite/non-invertible derived camera matrices. If otherwise-valid finite projection values cannot produce finite invertible matrices for the current raster aspect ratio, recording rejects the request with `ArgumentOutOfRangeException` before invoking `Draw`. 3D primitive colors must be opaque; `ClearColor` may be transparent. The control exposes no GPU device, render pass, scene graph, mesh, rig, picking policy, or orbit controller.

## Constructors
| Signature | Description |
| --- | --- |
| `RenderSurface3D()` | Creates a surface with transparent clear color, the default right-handed view, a 60-degree perspective projection with near plane `0.01` and far plane `1000`, and `OnDemand` redraw mode. |

## Fields
| Name | Description |
| --- | --- |
| `ClearColorProperty` | Identifies `ClearColor`; default transparent. |
| `ViewMatrixProperty` | Identifies the finite, affine, invertible view matrix. |
| `ProjectionProperty` | Identifies the validated projection descriptor. |
| `RedrawModeProperty` | Identifies the redraw mode; default `OnDemand`. |

## Properties
| Name | Description |
| --- | --- |
| `ClearColor` | Gets or sets the target clear color. |
| `ViewMatrix` | Gets or sets the requested finite, affine view matrix whose inverse is finite. Setting an invalid matrix throws `ArgumentException`. |
| `Projection` | Gets or sets the requested validated projection descriptor. Setting `default` or another invalid descriptor throws `ArgumentException`. |
| `RedrawMode` | Gets or sets on-demand or continuous recording. Setting an undefined enum value throws `ArgumentException`. |

## Methods
| Name | Description |
| --- | --- |
| `void InvalidateFrame()` | Advances the requested generation and schedules a later render without measure or arrange. |
| `bool TryWorldToRoot(Vector3 worldPosition, out Vector2 rootPosition)` | Projects a visible finite world position into logical root coordinates. Returns `false` and writes `default` when conversion is not defined. |
| `bool TryRootToWorldRay(Vector2 rootPosition, out DrawRay3D ray)` | Creates a finite normalized near-to-far world ray for a logical root position. Returns `false` and writes `default` when conversion is not defined. |

## Events
| Name | Description |
| --- | --- |
| `Draw` | Records one frame while its `RenderSurface3DFrame` is active. Adding or removing a handler requests a later frame; removing the final handler also releases session-local drawing resources. Handler-list mutations during dispatch take effect on later dispatches. |

## Applies to
`Cerneala`
