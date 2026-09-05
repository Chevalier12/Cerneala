# Servo Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/Servo.cs`

Provides asynchronous, in-process queries and user-like input over a Cerneala window or retained host.

```csharp
public sealed class Servo
```

## Examples

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Servo;

var servo = new Servo(window);
ServoElement save = await servo.FindAsync(ServoTarget.ById("save"));
bool hasMenu = await servo.ExistsAsync(ServoTarget.ByRole(SemanticsRole.Menu));
await servo.ClickAsync(ServoTarget.ById("save"));
await servo.WaitForAsync(ServoTarget.ById("confirmation"), ServoCondition.Visible);
await servo.WaitForIdleAsync();
await servo.SaveScreenshotAsync("artifacts/window.png");
await servo.SaveScreenshotAsync(
    ServoTarget.ById("confirmation"),
    "artifacts/confirmation.png");
```

## Remarks

Every query resolves its `ServoTarget` against the current Servo semantic projection. Returned `ServoElement` instances are snapshots and do not expose or retain a live `UIElement`.

The Servo projection includes the logical scene input subtree of `RenderSurface2D`, in addition to visual children. Scoped selectors follow that ownership. Scene nodes remain outside layout: their client-DIP bounds are the axis-aligned union of their picking geometry and input descendants, transformed through the scene and surface. This includes collider geometry, but not effect padding or ordinary batched tile cells. Unknown geometry has empty bounds. Querying does not arrange nodes or rebuild the collision index. The accessibility projection is unchanged.

`Servo.IdProperty` is the attached identifier property used by `ById`. Values are trimmed; blank values become `null`.

Target actions resolve the target immediately before input and reject hidden, disabled, detached, zero-bounds, or non-hit-testable elements. Pointer and keyboard actions use Cerneala `InputFrame` routing. A window action completes after its final input frame is presented; a retained-host action completes after `UiHost.Update` commits that frame.

The bounds center must hit the target or a descendant on the current input route. A union with a gap at its center can therefore be queryable but not actionable; Servo does not search for a substitute point. Disabled colliders remain queryable, but cannot supply a hit. Scene target screenshots use the same geometric bounds and capture the rendered window pixels there, not a separate rendering of the node.

Input sequences are serialized per window or host. `TypeIntoAsync` composes a click and text input. `ReplaceTextAsync` composes a click, `Control+A`, and text input rather than assigning a control property.

Every operation uses `ServoOptions.DefaultTimeout`. Expiration throws `ServoTimeoutException`; caller cancellation remains `OperationCanceledException`. Waits reevaluate after retained frame boundaries and release their transient subscriptions on every completion path. Async `WaitUntilAsync` predicates may issue Servo queries and do not hold the input serialization gate. `WaitForIdleAsync` requires the relay, scheduler, input context, and Motion system to be idle; continuous Motion therefore times out.

Screenshots are supported only by the `Window` constructor and use the application-owned `Window.SaveScreenshot` pipeline. A target screenshot resolves the target fresh, requires effective visibility and a non-empty framebuffer intersection, and crops the fully rendered window after drawing. Disabled and non-hit-testable targets remain capturable. A `UiHost`-only Servo throws `NotSupportedException` for both screenshot overloads.

## Constructors

| Name | Description |
| --- | --- |
| `Servo(Window, ServoOptions?)` | Creates a Servo façade for a Cerneala window. The window must have a live root when an operation executes. |
| `Servo(UiHost, ServoOptions?)` | Creates a Servo façade for a retained Cerneala host. The host must have a live root when an operation executes. |

## Fields

| Name | Description |
| --- | --- |
| `IdProperty` | Identifies the attached Servo ID property. |

## Methods

| Name | Description |
| --- | --- |
| `GetId(UIElement)` | Gets the normalized attached Servo ID. |
| `SetId(UIElement, string?)` | Sets or clears the attached Servo ID. |
| `FindAsync(ServoTarget, CancellationToken)` | Returns the single current match; throws for zero or multiple matches. |
| `FindAllAsync(ServoTarget, CancellationToken)` | Returns all current matches in stable semantic-tree order. |
| `ExistsAsync(ServoTarget, CancellationToken)` | Returns whether at least one current match exists. |
| `ClickAsync(ServoTarget, CancellationToken)` | Clicks the current target center through routed pointer input. |
| `HoverAsync(ServoTarget, CancellationToken)` | Moves the pointer to the current target center. |
| `DragAsync(ServoTarget, ServoPoint, int, CancellationToken)` | Drags from the current target center to an absolute client-DIP destination. |
| `ScrollAsync(ServoTarget, int, CancellationToken)` | Sends a wheel delta at the current target center. |
| `PressKeyAsync(InputKey, ServoModifiers, CancellationToken)` | Sends a key chord to the currently focused element. |
| `SendTextAsync(string, CancellationToken)` | Sends Unicode text elements to the currently focused element. |
| `TypeIntoAsync(ServoTarget, string, CancellationToken)` | Clicks a target and sends text to it. |
| `ReplaceTextAsync(ServoTarget, string, CancellationToken)` | Clicks a target, selects all with `Control+A`, and sends replacement text. |
| `WaitForAsync(ServoTarget, ServoCondition, CancellationToken)` | Waits for a target cardinality or unique-target state condition at frame boundaries. |
| `WaitUntilAsync(Func<CancellationToken, Task<bool>>, CancellationToken)` | Waits until an asynchronous predicate succeeds; predicate exceptions propagate. |
| `WaitForIdleAsync(CancellationToken)` | Waits until scheduled, relay, input, and Motion work are idle. |
| `SaveScreenshotAsync(string, CancellationToken)` | Saves the complete current window framebuffer through `Window.SaveScreenshot`. |
| `SaveScreenshotAsync(ServoTarget, string, CancellationToken)` | Saves the visible pixel crop for a freshly resolved target through the window screenshot owner. |

## Applies to

In-process automation of Cerneala applications.

## See also

- `ServoTarget`
- `ServoElement`
- `ServoOptions`
