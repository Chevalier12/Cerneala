# TimeSensitiveRenderInvalidator Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/TimeSensitiveRenderInvalidator.cs`

Traverses a `UIElement` tree and gives time-sensitive render elements the current frame time before frame processing continues.

```csharp
public static class TimeSensitiveRenderInvalidator
```

Inheritance:
`object` -> `TimeSensitiveRenderInvalidator`

## Examples

Advance time-sensitive render state for a retained UI tree:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

UIRoot root = GetRoot();
TimeSpan frameTime = TimeSpan.FromMilliseconds(16);

TimeSensitiveRenderInvalidator.Invalidate(root, frameTime);
```

`UiHost.Update` calls the invalidator after applying the current viewport and priming the initial frame:

```csharp
using Cerneala.UI.Hosting;

UiHost host = GetHost();
_ = host.Update(elapsedTime: TimeSpan.FromMilliseconds(16));
```

## Remarks

`TimeSensitiveRenderInvalidator` is used by the hosting update path to keep render-only time state in sync with the frame clock. It starts at the supplied root and visits each element in the retained UI tree. When an element implements `ITimeSensitiveRenderElement`, the invalidator calls `UpdateRenderTime(frameTime)`.

The invalidator does not inspect the return value from `UpdateRenderTime`. Time-sensitive elements are responsible for invalidating themselves when their time state changes. For example, `TextBoxBase` uses this hook to update caret blink visibility and invalidates render state when the blink phase changes.

Traversal follows visual children when an element has any visual children. If the element has no visual children, traversal falls back to logical children. This keeps template-generated visual trees preferred when they exist while still supporting elements that only expose logical children.

`Invalidate` validates the root argument and throws `ArgumentNullException` when it is `null`. A zero or negative `frameTime` is still passed to participating elements; each implementation decides how to interpret it.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Invalidate(UIElement root, TimeSpan frameTime)` | `void` | Traverses `root` and its descendants, calling `ITimeSensitiveRenderElement.UpdateRenderTime` on each time-sensitive render element. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Invalidate(UIElement root, TimeSpan frameTime)` | `ArgumentNullException` | `root` is `null`. |

## Applies To

Cerneala retained UI rendering and hosting frame updates.

## See Also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Rendering.ITimeSensitiveRenderElement`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Controls.TextBoxBase`
