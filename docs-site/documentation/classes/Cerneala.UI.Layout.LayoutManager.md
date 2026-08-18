# LayoutManager Class

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/LayoutManager.cs`

Coordinates the measurement and arrangement of UI elements for a `UIRoot`, with cache for results and rendering invalidation when the arranged boundaries change.
```csharp
public sealed class LayoutManager
```
Inheritance:
`Object` -> `LayoutManager`

## Examples
Measuring and arranging the root UI for a fixed viewport:
```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(viewportWidth: 800, viewportHeight: 600);
LayoutManager layout = root.LayoutManager;

LayoutResult measure = layout.Measure(root, new LayoutSize(800, 600));
LayoutResult arrange = layout.Arrange(root, new LayoutRect(0, 0, 800, 600));

bool boundsChanged = arrange.BoundsChanged;
```
Creating the processors used by the frame scheduler for the layout phases:
```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(800, 600);
FramePhaseProcessors processors = root.LayoutManager.CreatePhaseProcessors();

processors.Measure?.Invoke(root);
processors.Arrange?.Invoke(root);
```
## Remarks
`LayoutManager` is linked to a single `UIRoot`. The constructor gets the root, and the public methods operate on `UIElement` elements in that tree.

`CreatePhaseProcessors` builds a `FramePhaseProcessors` for `Measure` and `Arrange` and also exposes incremental measure processing for the scheduler. The generated actions automatically choose the available size and final rectangle based on the root, the visual parent, the position in `Canvas`, and the last known arrangement slot.

`Measure` avoids measurement when the element does not have the `InvalidationFlags.Measure` flag, the available size is the same as the last measurement, and `LayoutVersion` has not changed. When the measurement runs, the method calls `UIElement.Measure(new MeasureContext(availableSize))`, updates the element's internal cache, and returns a `LayoutResult`.

`Arrange` avoids arranging in the same cache conditions for `InvalidationFlags.Arrange`, the final rectangle and `LayoutVersion`. When a dirty element is arranged, the manager clears the cached arrangement slot and calls `UIElement.Arrange(new ArrangeContext(finalRect, LayoutRounding.ForScale(root.Scale)))`, then updates the internal cache and checks if `ArrangedBounds` has changed. If the limits have changed for an attached element, the element is invalidated for `Render` and `HitTest`.

For attached descendants that have a valid render cache, the same render dependencies, and the same content size, but a different position, the manager removes the render job from `RenderQueue` and invalidates the retained cache root. This behavior allows the reuse of rendered content when only the translation has changed.

For nested elements, a later measure reuses the last constraint received from the parent before falling back to the arranged bounds. This preserves infinite or partial panel constraints and prevents a resize from accidentally turning an unconstrained measure into a constrained one.

The default space rules are:

| Case | Used space |
| --- | --- |
| The element is `UIRoot` | The root viewport. |
| The visual parent is `UIRoot` | The root viewport. |
| The parent has `ArrangedBounds` with positive width and height | The size or rectangle of the parent. |
| Child in a `Canvas` | Parent position plus `Canvas.GetLeft(element)` and `Canvas.GetTop(element)`, with size `DesiredSize`. |
| There is no usable parent | The viewport is used for measurement; for arrangement, `DesiredSize` is used at the origin `(0, 0)`. |

## Constructors
| Name | Description |
| --- | --- |
| `LayoutManager(UIRoot root)` | Create a layout manager for `root`. Throw `ArgumentNullException` if `root` is `null`. |

## Methods
| Name | Description |
| --- | --- |
| `FramePhaseProcessors CreatePhaseProcessors()` | Returns processors for phases `Measure` and `Arrange`, related to internal rules of available size and final rectangle. |
| `LayoutResult Measure(UIElement element, LayoutSize availableSize)` | Measure `element` for `availableSize` or return the result from the cache when the layout data is still valid. |
| `LayoutResult Arrange(UIElement element, LayoutRect finalRect)` | Arrange `element` into `finalRect` or return the cached result when the layout data is still valid. Invalidates rendering and hit testing when bounds change for an attached element. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `LayoutManager(UIRoot root)` | `ArgumentNullException` | `root` is `null`. |
| `Measure(UIElement element, LayoutSize availableSize)` | `ArgumentNullException` | `element` is `null`. |
| `Arrange(UIElement element, LayoutRect finalRect)` | `ArgumentNullException` | `element` is `null`. |

## Return Value Details
| Member | `LayoutResult` fields |
| --- | --- |
| `Measure` cache hit | `DesiredSize` and `ArrangedBounds` come from the element, `UsedMeasureCache` is `true`, `UsedArrangeCache` and `BoundsChanged` are `false`. |
| `Measure` executed | `DesiredSize` is the result of the measurement, `ArrangedBounds` is the current value of the element, and the cache and change flags are `false`. |
| `Arrange` cache hit | `DesiredSize` and `ArrangedBounds` come from the element, `UsedArrangeCache` is `true`, `UsedMeasureCache` and `BoundsChanged` are `false`. |
| `Arrange` executed | `DesiredSize` comes from the element, `ArrangedBounds` is the result of the arrangement, and `BoundsChanged` indicates if the previous rectangle differs from the new one. |

## Applies to
Project: `Cerneala`

Runtime context: the UI system of the project, especially the frame processing by `UIRoot.ProcessFrame` and `UiFrameScheduler`.

## See also
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.LayoutResult`
- `Cerneala.UI.Invalidation.FramePhaseProcessors`
