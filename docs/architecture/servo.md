# Servo

> Code: `UI/Servo`, `UI/Hosting/Windowing/WindowApplicationRuntime.cs` (Servo input queue and screenshots), `UI/Input/ElementInputBridge.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Servo automates a Cerneala `Window` or `UiHost` from code in the same process. It finds elements through the semantic tree and drives them with synthesized input frames that travel the same path as platform input: hit testing, routed events, focus, commands, and the retained commit. It does not write control state directly, and it does not automate other processes or applications.

Usage is in the guide [servo.md](../guides/servo.md). This document explains how Servo works inside.

Correction to the [architecture diagram](../assets/cerneala-architecture.png): its box reads "Servo — external automation". Servo is **in-process** automation. It has no socket, pipe, HTTP or CLI surface. The only out-of-process input path is Live Preview: `Cerneala.PreviewHost` receives input over its stdio protocol and feeds it into the same input driver (see [language-tooling.md](language-tooling.md#cernealapreviewhost)). The image has no source in the repository, so it is not regenerated.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Servo` | `UI/Servo/Servo.cs` | Public facade: `FindAsync`, `FindAllAsync`, `ExistsAsync`, input actions, waits, screenshots; `Servo.Id` attached property | caller; `new Servo(window)` or `new Servo(host)` |
| `ServoTarget` | `UI/Servo/ServoTarget.cs` | Selector: `ById`, `ByName`, `ByRole`, `WithName`, `Within` | caller (immutable) |
| `ServoElement` | `UI/Servo/ServoElement.cs` | Read-only snapshot of a found element; never the live element | caller |
| `ServoOptions` | `UI/Servo/ServoOptions.cs` | `DefaultTimeout` (5 s by default; zero, negative and infinite are rejected); copied at construction | `Servo` |
| `ServoContext` | `UI/Servo/ServoContext.cs` | Internal; binds to the window or host, marshals to the owning thread, serializes input | `Servo` |
| `ServoQueryEngine` | `UI/Servo/ServoQueryEngine.cs` | Internal; resolves targets against `SemanticsProvider.Build(root, SemanticsProjection.Servo)` | `Servo` |
| `ServoActionEngine` | `UI/Servo/ServoActionEngine.cs` | Internal; checks that a target can receive input and computes its center point | `Servo` |
| `RetainedServoInputDriver` | `UI/Servo/RetainedServoInputDriver.cs` | Internal; builds `InputFrame` sequences (pointer, keyboard, text) and dispatches them | per window (`ServoInputState.Driver`) or per host |
| `ServoInputState` | `UI/Servo/ServoInputState.cs` | Internal; one gate (`SemaphoreSlim(1, 1)`) plus the driver | per window, created by `WindowApplicationRuntime`; per `UiHost` through a weak table |
| `ServoSynchronization` | `UI/Servo/ServoSynchronization.cs` | Internal; `WaitFor`, `WaitUntil`, `WaitForIdle`, woken by frame events | `Servo` |
| `ServoCaptureEngine` | `UI/Servo/ServoCaptureEngine.cs` | Internal; element screenshot region | `Servo` |
| `ServoException` and subclasses | `UI/Servo/ServoException.cs` | `ServoTargetNotFoundException`, `ServoTargetAmbiguousException`, `ServoTargetNotActionableException`, `ServoTimeoutException` | thrown to caller |

## Data Flow

Example: `await servo.ClickAsync(ServoTarget.ById("save"))` on a `Window`.

1. `ServoOperation.RunAsync` links the caller's token with the `DefaultTimeout`. The timeout also covers time spent waiting in the queue.
2. `ServoContext.ExecuteSerializedAsync` gets the window's `ServoInputState` on the UI thread and takes its gate. Two `Servo` instances on the same window therefore run one after the other; different windows do not block each other.
3. `ServoActionEngine.ResolveActionable` resolves exactly one element and checks, in order:
   - it is still attached to this root;
   - it is effectively visible;
   - it is enabled;
   - it has usable arranged bounds;
   - its center hits the element itself or one of its descendants (`root.InputCache.HitTest` plus the route to the root).

   Any failure throws `ServoTargetNotActionableException` before any input is sent.
4. `RetainedServoInputDriver.ClickAsync` builds three `InputFrame` steps: pointer move to the center, left button down, left button up. The steps are built lazily, after scheduled layout, so a target moved by pending layout is clicked at its new position.
5. Dispatch:
   - **Window:** `WindowApplicationRuntime.EnqueueServoInputAsync` queues a `ServoInputOperation` and requests a render. On each render tick, `UpdateServoInput` gives `UiHost.Update` the next Servo frame instead of the platform frame.
   - **UiHost:** the driver calls `host.Update(...)` directly, once per step.
6. `UiHost.UpdateCore` handles the frame like platform input: Relay drain, scheduled layout, then `ElementInputBridge.Dispatch`, which hit-tests, focuses on press, raises the preview and bubble mouse events, and runs the button command on click.
7. The task completes only after the frame that carried the input has been presented. In the window test, `Click` fires at present `before + 2` and the task completes at `before + 3`.

`TypeIntoAsync` is a click followed by text input. `ReplaceTextAsync` is a click, Ctrl+A, then text input. Neither writes `Text` directly.

## Queries

- `Servo.Id` is an attached `UiProperty<string?>` with `AffectsSemantics`; whitespace is trimmed, and a blank value becomes `null`. In markup, the `Servo.Id="save"` attribute is emitted as this property.
- Matching uses ordinal comparison on Id, Name and Role, within an optional `Within` scope.
- Cardinality is explicit:
  - `FindAsync` throws `ServoTargetNotFoundException` for zero matches and `ServoTargetAmbiguousException` for more than one;
  - `FindAllAsync` returns an empty list for zero matches;
  - `ExistsAsync` returns a `bool`.
- The Servo semantic projection keeps hidden and collapsed nodes, so `WaitForAsync(target, ServoCondition.Hidden)` can tell "hidden" apart from "missing".

## Waiting

- `WaitForAsync`, `WaitUntilAsync` and `WaitForIdleAsync` evaluate their predicate on the UI thread. They re-check after each frame event: `Window.FrameRendered` and `Closed`, or `UiHost.FrameUpdated` and `RootChanged`. There is no polling interval.
- Idle means all five are true:
  - no pending Relay work;
  - the scheduler has no work;
  - no active motion;
  - no active pointer repeat;
  - the Servo gate is free.
- Servo does not pump frames. In window mode the application loop does it. In host mode the caller must call `host.Update`. A wait without frames ends with `ServoTimeoutException`.

## Screenshots

- `Servo.SaveScreenshotAsync(path)` on a window uses the application-owned path (`WindowApplicationRuntime.SaveServoScreenshotAsync`). It saves immediately when the retained cache is valid and nothing is rendering; otherwise it saves after the next completed frame.
- `SaveScreenshotAsync(target, path)` crops to the element's bounds with `WindowScreenshotRegion` (floor and ceil at framebuffer scale).
- A `UiHost` has no framebuffer, so both overloads throw `NotSupportedException`. There is no OS screen-capture fallback.

## Lifecycle And Ownership

- `Servo` holds no element references between operations. Each operation resolves targets again, against the current tree.
- A timeout, cancellation or root replacement detaches the wait callbacks and leaves the `Servo` usable for the next operation.
- If a key chord or drag is canceled or fails, the driver presents a reset step that releases pointer buttons and modifiers before the next action.

## Frame Integration

Servo adds no frame phase. Its frames enter through `UiHost.Update` at the same point as platform input: after the Relay drain and scheduled layout. See [input.md](input.md) and [invalidation-and-frame.md](invalidation-and-frame.md).

## Invariants

- Click and hover go through hit testing and routed input; a click focuses and releases the button. `tests/Cerneala.Tests/UI/Servo/ServoInputTests.cs:HoverAndClickUseHitTestingRoutedInputAndRetainedCommit`.
- Text actions compose focus, keys and text input, without direct assignment (`"DEFAULT"` + `"!"` → `"DEFAULT!"`). `ServoInputTests.cs:TextActionsComposeFocusKeyAndTextInputWithoutDirectAssignment`.
- Hidden, disabled, zero-size and not-hit-testable targets throw `ServoTargetNotActionableException`. `ServoInputTests.cs:TargetActionsRejectNonActionableElements`.
- Zero, one and many matches are handled differently. `tests/Cerneala.Tests/UI/Servo/ServoQueryTests.cs:CardinalityContractsDistinguishMissingSingleAndAmbiguousTargets`.
- Idle waits for scheduler work and finite motion, and times out on continuous motion. `tests/Cerneala.Tests/UI/Servo/ServoSynchronizationTests.cs:WaitForIdleObservesSchedulerFiniteMotionAndContinuousMotion`.
- A window action completes only after its input frame was presented. `tests/Cerneala.Tests/UI/Hosting/WindowRuntimeTests.cs:ServoWindowActionCompletesOnlyAfterItsInputFrameWasPresented`.
- Servo instances on one window are serialized; separate windows are independent. `WindowRuntimeTests.cs:ServoSerializesInstancesPerWindowAndKeepsWindowsIndependent`.
- Host screenshots are unsupported, with no OS fallback. `WindowRuntimeTests.cs:ServoHostScreenshotsAreUnsupportedWithoutAnOsFallback`.

## Known Limitations

- Live Preview input (`WindowApplicationRuntime.ClickPreview`, `MovePreviewPointer`, `SendPreviewText`, `PressPreviewKey`) calls the same driver directly and skips the Servo queue and gate. Whether it can interleave with a running Servo operation on the same window is netestat.
- Selectors combine only through `WithName` and `Within`. There is no selector for "this Id and this Role".
- Screenshot tests check the save calls on a fake graphics session; no test decodes the written PNG.
