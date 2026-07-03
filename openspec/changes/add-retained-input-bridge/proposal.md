## Why

Cerneala can now run a retained UI root inside a game loop, but `InputFrame` snapshots are only captured, not translated into retained element behavior. This change connects existing input snapshots, retained trees, routed events, hit testing, focus, and visual state so controls can react to mouse, keyboard, and text input.

## What Changes

- Add retained hit testing over arranged visual bounds with filters and deterministic visual-order targeting.
- Add an `ElementInputBridge` that converts `InputFrame` pointer, keyboard, wheel, and text transitions into preview/bubble routed events against retained elements.
- Reuse `UiInputTree`, `ElementInputRouteBuilder`, `RoutedEventRouter`, `InputEvents`, and `UIElement.Handlers` instead of duplicating route logic.
- Add pointer capture, hover tracking, pressed-state tracking, click synthesis, focus management, keyboard navigation MVP, and text input bridging services.
- Add retained visual state properties on `UIElement`: `IsPointerOver`, `IsKeyboardFocusWithin`, and `IsKeyboardFocused`.
- Add the minimal `ButtonBase.IsPressedProperty` primitive needed by pressed-state tracking without implementing full controls from the later controls phase.
- Update `UiHost.Update(...)` so retained input dispatch runs before retained invalidation/layout/render queue processing.
- Add focused tests for hit testing, event bridging, pointer capture, hover, pressed/click behavior, focus, text input, and retained routed event integration.
- Update `ROADMAPv2.md` checkboxes for section 8 as implementation tasks complete.

## Capabilities

### New Capabilities
- `retained-input-bridge`: Defines retained hit testing, input frame to routed event dispatch, pointer capture, hover/pressed/click tracking, focus, text input bridging, and visual input state.

### Modified Capabilities
- `game-loop-host-integration`: Host update now drives retained input dispatch before retained scheduler processing.
- `retained-ui-mvp-foundation`: The retained UI MVP gains concrete input bridge behavior over the retained visual tree.

## Impact

- New production files under `UI/Input`.
- New visual state properties in `UI/Elements/UIElement.cs`.
- New minimal primitive under `UI/Controls/Primitives/ButtonBase.cs` if no existing controls primitive exists yet.
- Updates to `UI/Input/RoutedEventRouter.cs`, `UI/Input/InputEvents.cs`, and `UI/Hosting/UiHost.cs`.
- New tests under `tests/Cerneala.Tests/Input`.
- No new external dependencies.
