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
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;

// window is the application's open Cerneala Window.
static async Task DriveAsync(Window window)
{
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

    var walkingServo = new Servo(window, new ServoOptions
    {
        DefaultTimeout = TimeSpan.FromSeconds(10)
    });
    await walkingServo.HoldKeyAsync(InputKey.D, TimeSpan.FromSeconds(2));

    // One slow step gets its own timeout; the next call uses DefaultTimeout (5 s) again.
    await servo.ClickAsync(ServoTarget.ById("open-large-file"));
    await servo.WaitForIdleAsync(TimeSpan.FromSeconds(30));
    await servo.WaitForAsync(ServoTarget.ById("document"), ServoCondition.Visible);

    // Drag a card onto the center of the trash bin, wherever the bin is when the drag starts.
    await servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("trash"));
}

// A window on the SDL platform: send raw platform events, including auto-repeat.
static async Task RepeatBackspaceAsync(Window window)
{
    var servo = new Servo(window);
    await servo.SendPlatformInputAsync(
        ServoPlatformInput.KeyDown(InputKey.Back),
        ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
        ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
        ServoPlatformInput.KeyUp(InputKey.Back),
        ServoPlatformInput.Text("abc"));
}
```

## Remarks

Every query resolves its `ServoTarget` against the current Servo semantic projection. Returned `ServoElement` instances are snapshots and do not expose or retain a live `UIElement`.

The Servo projection includes the logical scene input subtree of `RenderSurface2D`, in addition to visual children. Scoped selectors follow that ownership. Scene nodes remain outside layout: their client-DIP bounds are the axis-aligned union of their picking geometry and input descendants, transformed through the scene and surface. This includes the shape of every `Collider2D`, also disabled colliders and colliders with `CollisionLayer` 0, but not effect padding or ordinary batched tile cells. Unknown geometry has empty bounds. Querying does not arrange nodes or rebuild the collision index. The accessibility projection is unchanged.

`Servo.IdProperty` is the attached identifier property used by `ById`. Values are trimmed; blank values become `null`.

Target actions resolve the target immediately before input, after the owning frame has processed scheduled layout, and reject hidden, disabled, detached, zero-bounds, or non-hit-testable elements. A target's coordinates and actionable state are not frozen when the action enters the queue. Pointer and keyboard actions use Cerneala `InputFrame` routing. A window action completes after its final input frame is presented; a retained-host action completes after `UiHost.Update` commits that frame.

In a `Window`, the pointer stays where the last Servo input frame put it. Frames without a Servo step still read the platform input, but while the platform pointer reports the same position as before, they are routed at the Servo pointer position. For example, after `await servo.HoverAsync(ServoTarget.ById("save"))` the button keeps `IsPointerOver == true` and gets no `MouseLeave` while the physical mouse rests elsewhere, and a later `PressKeyAsync` raises no `MouseEnter` or `MouseLeave`. The first platform frame with a new pointer position, from a physical mouse move or a `SendPlatformInputAsync` `PointerMove`, moves the pointer there. Servo input frames always carry Servo's own last pointer position, so the next Servo operation, also a keyboard-only one, moves the pointer back to it. Before Servo's first pointer operation in the `Window` there is no Servo pointer position, so `PressKeyAsync`, `HoldKeyAsync`, and `SendTextAsync` leave the pointer where it is. For example, while the physical mouse rests on a button and no Servo operation has moved the pointer yet, `await servo.PressKeyAsync(InputKey.F2)` raises no `MouseLeave` on the button, and `Window.LastFrame.Input.Pointer` keeps reporting the mouse position.

On a retained `UiHost`, Servo input frames also carry Servo's own last pointer position. Before Servo's first pointer operation on the host, `PressKeyAsync` and `SendTextAsync` frames are routed at the pointer position of the host's last frame (`UiHost.LastFrame.Input.Pointer`), so they move nothing. For example, while the application's own `InputFrame`s keep the pointer on a button at (160, 80), `await new Servo(host).PressKeyAsync(InputKey.F2)` raises no `MouseLeave` on the button, and `host.LastFrame.Input.Pointer` still reports (160, 80). After `HoverAsync` or another pointer operation, the key and text frames carry the Servo pointer position instead.

Target resolution does not wait for global idleness or add a settling frame. The resolved coordinates define the gesture; Servo does not continuously follow a target that moves after the gesture starts.

`DragAsync(ServoTarget, ServoTarget, int, CancellationToken)` resolves the destination exactly like the source: at dispatch time, after scheduled layout, with the same actionability checks, and aims at the destination's center. Both ends are resolved before the pointer moves. For example, a 60 x 40 `Thumb` with Servo ID `card` at (20, 60) and an 80 x 60 `Border` with ID `bin` at (220, 20): `await servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"))` presses at (50, 80), moves in 12 steps, and releases at (260, 50), so the thumb reports `TotalHorizontalChange == 210` and `TotalVerticalChange == -30`. When the bin is disabled or missing, the call throws `ServoTargetNotActionableException` or `ServoTargetNotFoundException` and no pointer input is sent. A destination is therefore a hit-testable element; to drop on a point without an element, use the `ServoPoint` overload.

Layout elements use their drawn bounds: the axis-aligned box of `ArrangedBounds` mapped through the render and Motion transforms of the element and every ancestor, the same transforms that hit testing applies. For example, a 100 x 40 `Thumb` arranged at (0, 40) inside a `StackPanel` whose `RenderTransform` scales 2x around `RenderTransformOrigin` (0, 0) has bounds (0, 80, 200, 80), and target actions aim at (100, 120). Without transforms, the bounds equal `ArrangedBounds`.

The center of these bounds must hit the target or a descendant on the current input route. A union with a gap at its center can therefore be queryable but not actionable; Servo does not search for a substitute point. Disabled colliders remain queryable, but cannot supply a hit. Target screenshots of layout elements and scene nodes use the same bounds and capture the rendered window pixels there, not a separate rendering of the node.

Input sequences are serialized per window or host. `TypeIntoAsync` composes a click and text input. `ReplaceTextAsync` composes a click, `Control+A`, and text input rather than assigning a control property. When the replacement text is empty, it presses `Delete` instead of sending text.

`ReplaceTextAsync` behaves the same on every operating system: its steps are Cerneala `InputFrame`s, not platform key events, and `Control+A` is the select-all shortcut of `TextBox` and `PasswordBox` on every platform. Cerneala has no Command key.

`ReplaceTextAsync` accepts only a target that exposes a text value: the semantic `Value` property, as a `TextBox` does, or a `PasswordBox`. Any other target, for example a `Button`, throws `ServoException` before any input is sent, so nothing is clicked and no key reaches the focused element. After the input, the target's value must equal the requested text exactly, compared ordinally; otherwise the call throws `ServoException` with the requested and the actual value. A `PasswordBox` is checked through its `Password`, and its error message contains neither password. Examples, for a `TextBox` holding `abc`:

```csharp
await servo.ReplaceTextAsync(ServoTarget.ById("name"), "xyz"); // Text == "xyz"
await servo.ReplaceTextAsync(ServoTarget.ById("name"), "");    // Text == ""
// TextBox is single-line and drops "\n": Text becomes "xy", and the call throws ServoException.
await servo.ReplaceTextAsync(ServoTarget.ById("name"), "x\ny");
```

A read-only `TextBox`, or a control whose handler takes `Control+A` for something else, also throws, because its value does not become the requested text.

Every operation uses `ServoOptions.DefaultTimeout`, unless the call uses its overload with a `TimeSpan timeout` parameter right before the `CancellationToken`. That timeout replaces `DefaultTimeout` for that call only: with the default 5 s, `await servo.WaitForIdleAsync(TimeSpan.FromSeconds(30))` may wait 30 s, and the next `await servo.WaitForIdleAsync()` times out after 5 s again. Optional parameters before the timeout become required in these overloads, for example `DragAsync(source, destination, 12, TimeSpan.FromSeconds(30))` and `PressKeyAsync(InputKey.F5, ServoModifiers.None, TimeSpan.FromSeconds(30))`. The timeout must be positive; zero, negative values, and `Timeout.InfiniteTimeSpan` throw `ArgumentOutOfRangeException` before any input. Expiration throws `ServoTimeoutException`; caller cancellation remains `OperationCanceledException`.

Target failures name the target with `ServoTarget.ToString()`. For example, `await servo.ClickAsync(ServoTarget.ById("save"))` on a disabled button throws `ServoTargetNotActionableException` with the message `The Servo target Id="save" is disabled.`, and two matches throw `ServoTargetAmbiguousException` with `The Servo target Id="save" matched 2 elements; exactly one was required.` A `ServoTargetNotFoundException` for a target with two or more criteria also counts near misses, the elements that fail exactly one criterion: `The Servo target Role=Button, Name="Save" within (Id="toolbar") did not match any element. Near misses: 2 matched every criterion except within (Id="toolbar"); 1 matched every criterion except Name="Save".` A target with a position also reports matches that the position skips, for example `Near misses: 2 matched every criterion, but Nth(5) selects none of them.` Timeouts of operations that take a target name it too: `The Servo operation on target Id="save" exceeded its 00:00:05 timeout.`, `The Servo operation on targets Id="card" and Id="bin" exceeded its 00:00:05 timeout.` for a drag to a target, and `The Servo wait for target Id="dialog" to be Visible exceeded its 00:00:05 timeout.` for `WaitForAsync` (`to exist` for `ServoCondition.Exists`). Operations without a target keep `The Servo operation exceeded its 00:00:05 timeout.` Waits reevaluate after retained frame boundaries and release their transient subscriptions on every completion path. `WaitForAsync` with a unique-target condition treats multiple matches as not yet satisfied, the same as no match, so an outgoing and an incoming copy of a target during a transition do not fail the wait; if the last evaluation before the timeout still found multiple matches, the `ServoTimeoutException` message ends with the match count, for example `The Servo wait for target Id="save" to be Visible exceeded its 00:00:05 timeout. The Servo target still matched 2 elements; exactly one was required.` Queries such as `FindAsync` and target actions still throw `ServoTargetAmbiguousException` immediately. Async `WaitUntilAsync` predicates may issue Servo queries and do not hold the input serialization gate. `WaitForIdleAsync` requires the relay, scheduler, and Motion system to be idle, no active Servo pointer repeat, and no Servo input sequence in progress; continuous Motion therefore times out.

`HoldKeyAsync` requires a `Window`-owned Servo; a direct `UiHost` Servo rejects it with `NotSupportedException` before sending input. The duration must be positive. The key and any requested left-side Shift, Control, or Alt modifiers follow the same focused-element routing as `PressKeyAsync`; `PressKeyAsync` remains a tap. The monotonic hold interval starts when the Window runtime completes Servo input for the key-down frame, **after** `Window.FrameRendered` callbacks have returned. It is not a physical display/scanout timestamp. Subsequent Servo-owned window frames keep the same down state without synthesizing repeated `KeyDown` events; to send auto-repeat `KeyDown` events, use `SendPlatformInputAsync` (see "Input paths"). Key-up is delivered in the first input frame begun at or after the requested minimum **monotonic wall-clock** duration, followed by modifier release; frame scheduling can make the actual interval longer.

The operation timeout (`DefaultTimeout`, or the `timeout` argument of `HoldKeyAsync(InputKey, TimeSpan, ServoModifiers, TimeSpan, CancellationToken)`) applies to the whole operation, including serialization wait, key-down delivery, hold interval, and ordinary release. Configure it longer than the requested duration with margin for frame scheduling. Cancellation or timeout can shorten the hold. If the owner processes cleanup before an additional cleanup deadline of the same length, Servo routes the release before the task settles. If the Window is hidden or its viewport becomes non-renderable during a hold, Servo faults the hold and routes a release through the retained input host without presenting another frame; an already requested cancellation or timeout keeps its result. During that offscreen release, reentrant Show or Close requests from input callbacks are deferred until the retained update finishes. If the owner pump stops or is delayed beyond that deadline after input started, cancellation, timeout, or failure initiates cleanup but cannot dispatch it from a background thread. The task settles after at most one additional cleanup interval of the operation timeout, with the reset left ahead of subsequent Servo input for the next owner update. A closed/destroyed or persistently faulted owner cannot promise delivery of a release; close discards its pending Servo input. Servo does not reserve or alter input supplied independently to a direct `UiHost`.

### Input paths

Servo has two input paths. Pick by what the test needs to prove.

| Operation | Path | What it exercises |
| --- | --- | --- |
| `ClickAsync`, `HoverAsync`, `DragAsync`, `ScrollAsync`, `PressKeyAsync`, `HoldKeyAsync`, `SendTextAsync`, `TypeIntoAsync`, `ReplaceTextAsync` | Servo-built `InputFrame` | Hit testing, routing, focus, commands. Works for `Window` and `UiHost`. Never goes through the platform. |
| `SendPlatformInputAsync` | The platform's own event queue | Everything above, plus the platform layer: for SDL, `SdlWindowPlatform` event pumping and `SdlInputSource` frame building. |

Use `SendPlatformInputAsync` when the bug or behavior lives below the `InputFrame`: key auto-repeat (`KeyDown` with `repeat: true`), a key press and release that arrive in one event pump, or text that arrives with the key events. Use the other operations for everything else; they also work without a platform.

`SendPlatformInputAsync` limits:

- It needs a `Window` whose platform can inject events. Today that is the SDL platform. A `UiHost` Servo, a preview window, or a platform without injection throws `NotSupportedException`.
- Positions are client DIPs, like `ServoPoint` everywhere else. They are not hit-tested against a target.
- A wheel delta must be a non-zero multiple of 120, the unit of `ScrollAsync`. SDL reports whole wheel notches, so `Wheel(position, 240)` is two notches and `Wheel(position, 100)` throws `ArgumentException` without pushing anything.
- Events are pushed all at once, before the next event pump. The task completes on the UI thread after a pump that started after the push has run, the resulting frame was presented, and the input source holds no undelivered input. It also completes when the platform drops the events, for example `Text` while text input is inactive.
- Cancellation and timeout do not recall events that were already pushed.
- It does not move or release anything for you: a `KeyDown` without a later `KeyUp` leaves the key down.

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
| `DragAsync(ServoTarget, ServoTarget, int, CancellationToken)` | Drags from the current source center to the current destination center; both are resolved at dispatch time. |
| `ScrollAsync(ServoTarget, int, CancellationToken)` | Sends a wheel delta at the current target center. |
| `PressKeyAsync(InputKey, ServoModifiers, CancellationToken)` | Sends a key chord to the currently focused element. |
| `HoldKeyAsync(InputKey, TimeSpan, ServoModifiers, CancellationToken)` | Holds a key chord across Window frames for at least the specified wall-clock duration, then releases it. |
| `SendTextAsync(string, CancellationToken)` | Sends Unicode text elements to the currently focused element. |
| `SendPlatformInputAsync(IReadOnlyList<ServoPlatformInput>, CancellationToken)` | Pushes the events through the Window platform's own event queue (also available as `SendPlatformInputAsync(params ServoPlatformInput[])`), so they go through the real platform input source. |
| `TypeIntoAsync(ServoTarget, string, CancellationToken)` | Clicks a target and sends text to it. |
| `ReplaceTextAsync(ServoTarget, string, CancellationToken)` | Clicks a target that exposes a text value, selects all with `Control+A`, sends the replacement text (or `Delete` for empty text), and throws `ServoException` unless the value then equals the text. |
| `WaitForAsync(ServoTarget, ServoCondition, CancellationToken)` | Waits for a target cardinality or unique-target state condition at frame boundaries. |
| `WaitUntilAsync(Func<CancellationToken, Task<bool>>, CancellationToken)` | Waits until an asynchronous predicate succeeds; predicate exceptions propagate. |
| `WaitForIdleAsync(CancellationToken)` | Waits until scheduled, relay, input, and Motion work are idle. |
| `SaveScreenshotAsync(string, CancellationToken)` | Saves the complete current window framebuffer through `Window.SaveScreenshot`. |
| `SaveScreenshotAsync(ServoTarget, string, CancellationToken)` | Saves the visible pixel crop for a freshly resolved target through the window screenshot owner. |

### Overloads with a per-call timeout

Each overload behaves like the method above with the same name, but uses `timeout` instead of `ServoOptions.DefaultTimeout` for that call only.

| Name | Description |
| --- | --- |
| `FindAsync(ServoTarget, TimeSpan, CancellationToken)` | `FindAsync` with its own timeout. |
| `FindAllAsync(ServoTarget, TimeSpan, CancellationToken)` | `FindAllAsync` with its own timeout. |
| `ExistsAsync(ServoTarget, TimeSpan, CancellationToken)` | `ExistsAsync` with its own timeout. |
| `ClickAsync(ServoTarget, TimeSpan, CancellationToken)` | `ClickAsync` with its own timeout. |
| `HoverAsync(ServoTarget, TimeSpan, CancellationToken)` | `HoverAsync` with its own timeout. |
| `DragAsync(ServoTarget, ServoPoint, int, TimeSpan, CancellationToken)` | `DragAsync` to a point with its own timeout; `steps` is required. |
| `DragAsync(ServoTarget, ServoTarget, int, TimeSpan, CancellationToken)` | `DragAsync` to a target with its own timeout; `steps` is required. |
| `ScrollAsync(ServoTarget, int, TimeSpan, CancellationToken)` | `ScrollAsync` with its own timeout. |
| `PressKeyAsync(InputKey, ServoModifiers, TimeSpan, CancellationToken)` | `PressKeyAsync` with its own timeout; `modifiers` is required. |
| `HoldKeyAsync(InputKey, TimeSpan, ServoModifiers, TimeSpan, CancellationToken)` | `HoldKeyAsync` with its own operation and cleanup timeout; the first `TimeSpan` is the hold duration, `modifiers` is required. |
| `SendTextAsync(string, TimeSpan, CancellationToken)` | `SendTextAsync` with its own timeout. |
| `SendPlatformInputAsync(IReadOnlyList<ServoPlatformInput>, TimeSpan, CancellationToken)` | `SendPlatformInputAsync` with its own timeout. |
| `TypeIntoAsync(ServoTarget, string, TimeSpan, CancellationToken)` | `TypeIntoAsync` with its own timeout. |
| `ReplaceTextAsync(ServoTarget, string, TimeSpan, CancellationToken)` | `ReplaceTextAsync` with its own timeout. |
| `WaitForAsync(ServoTarget, ServoCondition, TimeSpan, CancellationToken)` | `WaitForAsync` with its own timeout. |
| `WaitUntilAsync(Func<CancellationToken, Task<bool>>, TimeSpan, CancellationToken)` | `WaitUntilAsync` with its own timeout. |
| `WaitForIdleAsync(TimeSpan, CancellationToken)` | `WaitForIdleAsync` with its own timeout. |
| `SaveScreenshotAsync(string, TimeSpan, CancellationToken)` | Window screenshot with its own timeout. |
| `SaveScreenshotAsync(ServoTarget, string, TimeSpan, CancellationToken)` | Target screenshot with its own timeout. |

## Applies to

In-process automation of Cerneala applications.

## See also

- `ServoTarget`
- `ServoPlatformInput`
- `ServoElement`
- `ServoOptions`
