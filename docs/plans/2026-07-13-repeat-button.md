# Plan: RepeatButton

> Date: 2026-07-13
> Status: completed
> Purpose: the introduction of a button that activates immediately when pressed and repeats the activation at determined intervals as long as it remains pressed

## 1. Summary

`RepeatButton` will extend the existing button behavior without introducing threads, `Task.Delay`, global timers or commands executed directly from the control. The repetition time will come from the host frame, and the routing of the `Click` event and the execution of the commands will use the same contracts as the rest of the Cerneala input.

The first activation takes place when the left button is pressed. After `Delay`, the control produces at most one activation per frame every `Interval`. Repetition stops upon release, cancellation of pressed status, detach, disable, loss of route or change of root.

Design assumption: `RepeatButton` will derive from `Button`, not directly from `ButtonBase`, to reuse the current visual fallback and content composition. If before the implementation a common default template appears at the level of `ButtonBase`, this decision must be re-evaluated, not blindly copied like a recipe.

## 2. Objectives

- [x] There is `Cerneala.UI.Controls.Primitives.RepeatButton` as a public API.
- [x] `RepeatButton` exposes `Delay` and `Interval` as `UiProperty<int>` expressed in milliseconds.
- [x] `Delay` accepts finite integer values ​​greater than or equal to zero.
- [x] `Interval` accepts only strictly positive integer values.
- [x] The valid press immediately produces a single `Click` and a single command execution.
- [x] The first repetition takes place only after `Delay` expires.
- [x] The following repetitions respect `Interval` and produce at most one activation per frame.
- [x] A delayed frame does not produce an uncontrolled burst of outstanding activations.
- [x] The release no longer produces a click for `RepeatButton`.
- [x] `Button`, `ToggleButton`, keyboard activation and existing commands retain their behavior.
- [x] The implementation is deterministic in tests through `TimeSpan` provided explicitly to the host.

## 3. Non-objectives

- [x] We do not introduce a general UI timer scheduler.
- [x] We do not use `System.Threading.Timer`, `DispatcherTimer`, `Task.Delay` or work on secondary thread.
- [x] We do not add progressive repetition acceleration.
- [x] We do not add WPF properties that are not necessary to this slice.
- [x] We are not modifying the `ScrollBar` templates yet; these are handled by the dependent plane for the scrolling parts.
- [x] We do not change the general semantics of `Click` for regular buttons.

## 4. Proposed contract

Estimated Public API:
```csharp
public class RepeatButton : Button
{
    public static readonly UiProperty<int> DelayProperty;
    public static readonly UiProperty<int> IntervalProperty;

    public int Delay { get; set; }
    public int Interval { get; set; }
}
```
Suggested default values:

- `Delay = 500` ms;
- `Interval = 100` ms.

Proposed internal contract:
```text
UiHost frameTime
    -> ElementInputBridge.Dispatch(..., frameTime)
        -> RepeatButtonController
            -> IInputActivatable.Activate()
            -> IInputCommandSource.ExecuteCommand(...)
```

`RepeatButtonController` must hold the temporary replay session. The control exposes the input configuration and identity, but does not receive global dependencies such as `CommandRouter`.

## 5. Estimated files

New files:

- `UI/Controls/Primitives/RepeatButton.cs`;
- `UI/Input/RepeatButtonController.cs`;
- `UI/Input/IInputRepeatSource.cs`, only if the internal marker simplifies the integration without concrete type checks;
- `tests/Cerneala.Tests/Controls/Primitives/RepeatButtonTests.cs`;
- `docs-site/documentation/classes/Cerneala.UI.Controls.Primitives.RepeatButton.md`.

Probably changed files:

- `UI/Controls/Primitives/ButtonBase.cs`;
- `UI/Input/ElementInputBridge.cs`;
- `UI/Hosting/UiHost.cs`;
- `UI/Hosting/MonoGame/MonoGameUiHost.cs`, only if the wrapper signature requires it;
- `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs`;
- `tests/Cerneala.Tests/UI/Input/ElementInputBridgeTests.cs`;
- `docs-site/documentation/classes/Cerneala.UI.Controls.Primitives.ButtonBase.md` if a new protected hook appears;
- `docs-site/documentation/manifest.json`.

## 6. Implementation stages

### Stage 0 - Baseline and characterization tests

- [x] Generate `FileTree.md` and check the RoslynIndexer index.
- [x] Run the existing tests for `Button`, `ButtonBase`, keyboard activation, commands and `ElementInputBridge`.
- [x] Add characterization tests for the normal click on release of a `Button`.
- [x] Adds characterization tests for executing a command exactly once per click.
- [x] Adds characterization tests for canceling the pressed state at release and detach.
- [x] Note explicitly that these tests must remain unchanged after the introduction of repetition. (The characterization tests in `ElementInputBridgeTests` are regression contracts and are not modified to accommodate `RepeatButton`.)

**Gate Stage 0**

- [x] The baseline is green.
- [x] The normal button contract is covered before the input change.
- [x] No functional changes were introduced.

### Step 1 - RepeatButton API

- [x] Create `RepeatButton` in the `Cerneala.UI.Controls.Primitives` namespace.
- [x] Temporarily derived from `Button` according to documented assumption.
- [x] Register `DelayProperty` with default value `500` and validation `>= 0`.
- [x] Register `IntervalProperty` with default value `100` and validation `> 0`.
- [x] Exposes CLR properties `Delay` and `Interval`.
- [x] Mark the properties with the available markup constraints or extend the markup validation only if necessary. (The extension was not necessary: the existing constraints cover the properties, and the `UiProperty` validation remains authoritative.)
- [x] Add tests for default values, valid values, and rejection of invalid values.
- [x] Checks the generation/parsing of markup properties with integer values.

**Gate stage 1**

- [x] The API compiles and validation is covered.
- [x] The control does not start threads or timers by itself.
- [x] Existing tests for buttons remain green.

### Stage 2 - One way to activate

- [x] Extracts in `ButtonBase` a minimal protected hook that decides whether mouse-up produces a click.
- [x] Keep the default value of the hook so that `Button` and `ToggleButton` continue to activate on release.
- [x] Overwrite the hook in `RepeatButton` to avoid the additional click on release.
- [x] Keep `IInputActivatable.Activate()` as the unique path for raising the event `Click`.
- [x] Do not execute the command directly from `RepeatButton`.
- [x] Add tests that demonstrate that programmatic activation raises a single `Click`.
- [x] Add tests that demonstrate that the release of a `RepeatButton` does not raise an additional click.

**Gate stage 2**

- [x] There is a clear separation between the raising of `Click` and the execution of the order.
- [x] Normal buttons keep their semantics.
- [x] `RepeatButton` does not duplicate activation at release.

### Stage 3 - Deterministic input time

- [x] Add an overload `ElementInputBridge.Dispatch(UIRoot, InputFrame, TimeSpan frameTime)`.
- [x] Keep the existing overload for compatibility and explicitly delegate with a documented neutral value. (`TimeSpan.Zero`.)
- [x] Modify `UiHost.UpdateCore` to transmit the same `frameTime` used by the frame to the input bridge.
- [x] Do not reuse `ITimeSensitiveRenderElement`; repetition is input behavior, not false-whisker rendering invalidation.
- [x] Determines whether `frameTime` represents absolute or delta timestamp and keeps the same semantics in host, controller and tests. (`frameTime` is the delta drained in the current frame.)
- [x] Add host tests that confirm the propagation of explicitly provided time.
- [x] Check MonoGame and Windows wrappers for signatures or alternative update paths. (No changes were needed: both paths already reach `UiHost.UpdateCore` with the same delta.)

**Gate stage 3**

- [x] The input receives deterministic time without access to the global clock.
- [x] No host path advances time twice.
- [x] Tests without explicit time remain compatible.

### Step 4 - RepeatButtonController

- [x] Creates an internal controller owned by `ElementInputBridge`.
- [x] On valid mouse-down, resolve the nearest repeat source in the visual route.
- [x] Starts a single session for the left button and memorizes the source, the root and the next term.
- [x] Immediately produces the initial activation through `IInputActivatable`.
- [x] Execute the initial order through `IInputCommandSource` and `CommandRouter`.
- [x] After `Delay`, produces at most one activation per frame.
- [x] After the first repetition, program the next term using `Interval`.
- [x] At a very late frame, skip the old intervals without the catch-up loop.
- [x] Stops the session on mouse-up, detach, disable, hidden/collapsed, root change or invalid route.
- [x] Stops the session if the source is no longer pressable or the command source is valid.
- [x] Decides and explicitly tests the behavior when the pointer leaves the button while the button remains pressed. (The session is cancelled.)
- [x] For MVP, prefer canceling replay when exiting hit target instead of a new default capture.
- [x] Avoid stale references after cancellation.

**Gate Stage 4**

- [x] The click/command sequence is `1 initial + N repetari`, without accidental final click.
- [x] There is no more than one activation per frame.
- [x] The controller does not retain detached controls.
- [x] Routed commands and simple commands use the same existing path.

### Stage 5 - Interactions and limit cases

- [x] Test `Delay = 0` without duplicating the initial activation.
- [x] Tests the `Delay` change during a session and sets the rule: it only affects the next session.
- [x] Tests the change `Interval` during a session and sets the rule: it applies to the next calculated term.
- [x] Tests a frame that jumps over several intervals.
- [x] Test release exactly at the deadline of a repetition; the release wins and no longer produces repeats.
- [x] Test disable and detach between two frames.
- [x] Tests the command that becomes `CanExecute == false` during repetition.
- [x] Test handler `Click` that modifies the tree or removes the button.
- [x] Tests two `RepeatButton` pressed successively; the old session must be canceled.
- [x] Tests that the right button and the wheel do not start the repetition.
- [x] Tests the activation from the keyboard and documents that the MVP only repeats the pointer, if the repetition for Space is not implemented. (Space activates only once on release; MVP repetition is only for the left pointer.)

**Gate Stage 5**

- [x] The limit cases have deterministic results.
- [x] There are no activations after release or detachment.
- [x] There are no unintentional differences for `Button` and `ToggleButton`.

### Stage 6 - Integration, documentation and verification

- [x] Register `RepeatButton` in the markup scheme/factory if the types are not discovered automatically.
- [x] Add a minimal example to the Playground only if there is already a suitable surface for primitive controls. (It wasn't necessary: the current Playground is a navigation shell, without a showcase surface for primitive controls.)
- [x] Create the API documentation in `docs-site/documentation/classes/` using the `writing-api-documentation` skill.
- [x] Updates `docs-site/documentation/manifest.json` for the new page.
- [x] Updates the `ButtonBase` documentation if the protected hook becomes a public/protected API.
- [x] Re-indexes after each code or project change.
- [x] Run the targeted tests for input, buttons, commands and host.
- [x] Runs `dotnet test Cerneala.slnx`.
- [x] Check the public API diff and confirm that it only includes the intended surface. (`RepeatButton`, its properties, the protected hook from `ButtonBase` and the timed overload from `ElementInputBridge`; the repetition mechanism remains internal.)

**Gate stage 6**

- [x] The whole suite is green.
- [x] The public documentation is synchronized.
- [x] RepeatButton can be used from C# and markup.
- [x] Dependent plane for scrollbar can consume control without workarounds.

## 7. The definition of ready

`RepeatButton` is ready when a valid press immediately produces a click and a command execution, the repetition starts only after `Delay`, continues at most once per frame according to `Interval`, stops safely in all cancellation paths and does not change the behavior of the regular buttons. No hidden timer should be left beating on the walls after the control has disappeared.