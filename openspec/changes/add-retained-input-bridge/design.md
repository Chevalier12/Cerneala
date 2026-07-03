## Context

Cerneala already has:

- `InputFrame` snapshots for pointer, keyboard, wheel, and text input.
- WPF-familiar routed event names in `InputEvents`.
- `RoutedEventRouter`, `UiInputTree`, `ElementInputRouteBuilder`, and `UIElement.Handlers`.
- Retained `UIRoot`, visual tree, arranged bounds, layout, render invalidation, and host update/draw integration.

The missing piece is the retained bridge that turns snapshots into routed behavior on hit-tested retained elements, updates input-driven visual state, and gives keyboard/text input a focused target.

## Goals / Non-Goals

**Goals:**

- Convert `InputFrame` transitions into retained routed input events.
- Hit test retained visual bounds in deterministic visual order.
- Reuse the existing routed event/router/tree primitives.
- Add hover, pressed, click, pointer capture, focus, keyboard, and text input MVP services.
- Add retained visual state properties needed by controls: pointer-over and keyboard focus state on `UIElement`, plus a minimal pressed primitive for `ButtonBase`.
- Integrate retained input dispatch into `UiHost.Update(...)` before scheduler processing.
- Keep input state services explicit and testable, with no global static focus manager.

**Non-Goals:**

- No full WPF input manager clone.
- No stylus, touch, drag/drop, manipulation, IME composition, access keys, or advanced keyboard navigation in this phase.
- No complete `Button`, `Control`, templates, styling, or command routing.
- No global static `FocusManager` dependency.
- No replacement of `RoutedEventRouter`, `InputEvents`, or `UiInputTree`.

## Decisions

### `ElementInputBridge` coordinates existing services

`ElementInputBridge` SHALL be the host-facing input coordinator. It reads an `InputFrame`, builds or receives the retained input route map, hit tests pointer position, updates pointer capture, hover, pressed, click, focus, keyboard, and text services, then raises existing routed events.

Alternative considered: make `UiHost` directly handle pointer transitions. That would cram too much behavior into hosting and make input tests harder to isolate.

### Hit testing uses retained visual tree and arranged bounds

`HitTestService` SHALL traverse retained visual descendants in reverse visual order so later siblings are tested first. It SHALL use `UIElement.ArrangedBounds`, visibility, enabled state, and a `HitTestFilter` to decide candidates.

Alternative considered: reuse render command bounds. That couples input to rendering output too early and breaks when visual content does not match layout bounds.

### Routing reuses `RoutedEventRouter`

Input bridge SHALL build a `UiInputTree` from retained elements and call `RoutedEventRouter.Raise(...)` for preview and bubble events. It SHALL not duplicate route traversal logic.

Alternative considered: route directly through `UIElement.VisualParent` for each event. That bypasses existing routed-event tests and would create two subtly different routing models.

### Visual input state is typed property state

`IsPointerOver`, `IsKeyboardFocused`, and `IsKeyboardFocusWithin` SHALL be `UIElement` typed properties with render/input-visual invalidation effects. `ButtonBase.IsPressed` SHALL be a typed property on a minimal controls primitive so later controls can reuse it.

Alternative considered: keep hover/focus/pressed only inside tracker fields. That makes styling/render invalidation impossible to observe through retained state.

### Focus is an explicit service

`FocusManager` SHALL be instantiated and owned by the host/input bridge. It SHALL track the currently focused retained element, raise existing focus routed events, and update focus visual state.

Alternative considered: static global focus. That is harder to test and unsuitable for multiple roots or future embedded UI surfaces.

### Keyboard navigation MVP is direct focus only

`KeyboardNavigation` SHALL provide the minimum direct-focus behavior needed by tests and future controls. Tab traversal and focus scopes can be represented as MVP shell types, but complex navigation policy is deferred.

Alternative considered: implement full WPF-style tab order now. That belongs later, after controls exist.

### Text input targets focus

`TextInputBridge` SHALL translate `TextInputSnapshotEvent` into preview and bubble text input events against the focused element. If no element has focus, text input is ignored for MVP.

Alternative considered: send text input to hovered element. That is surprising and diverges from keyboard focus behavior.

## Risks / Trade-offs

- [Risk] Hit testing arranged bounds may not match future complex visuals. -> Mitigation: MVP defines bounds-based hit testing; future controls can add hit-test metadata if needed.
- [Risk] Button pressed state exists before full controls. -> Mitigation: implement only the typed primitive and keep full button behavior for the controls phase.
- [Risk] Focus-within updates can be tricky across ancestor chains. -> Mitigation: add focused tests for old and new focus paths.
- [Risk] Pointer capture can make hover and click behavior subtle. -> Mitigation: pointer capture gets focused unit tests and explicit precedence over hit test for pointer-up/move targeting.
- [Risk] Source-string architecture tests are weaker than behavioral tests. -> Mitigation: use behavior tests for routing and state; keep source scans only for boundary checks if needed.

## Migration Plan

1. Add visual state properties on `UIElement` and minimal `ButtonBase`.
2. Add hit-test primitives and tests.
3. Add element route store/bridge helpers that reuse existing handler storage and router.
4. Add hover, pressed, click, pointer capture, focus, keyboard, and text services with focused tests.
5. Integrate `ElementInputBridge` into `UiHost.Update(...)`.
6. Update `ROADMAPv2.md` section 8 checkboxes as each file/contract is completed.
7. Run focused input tests, full `dotnet test`, and OpenSpec validation.

No data migration is required.

## Open Questions

- Full `FocusScope` and tab navigation policy are intentionally MVP-minimal until real controls exist.
- Pointer capture ownership for multi-pointer input is deferred because `InputFrame` currently models one pointer.
