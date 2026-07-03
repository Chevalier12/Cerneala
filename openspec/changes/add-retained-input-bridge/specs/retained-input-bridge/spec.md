## ADDED Requirements

### Requirement: Retained hit testing targets visual elements
Cerneala SHALL provide retained hit testing over the retained visual tree using arranged bounds, visibility, enabled state, and deterministic visual order.

#### Scenario: Topmost visual child wins
- **WHEN** multiple visible retained elements contain the pointer position
- **THEN** hit testing returns the later visual sibling or deeper visual descendant that is topmost in retained visual order

#### Scenario: Invisible element is skipped
- **WHEN** a retained element is not visible for input
- **THEN** hit testing does not return that element as the target

#### Scenario: Disabled element is skipped
- **WHEN** a retained element is disabled
- **THEN** hit testing does not return that element as the target

#### Scenario: Filter can reject subtree
- **WHEN** a `HitTestFilter` rejects an element or subtree
- **THEN** hit testing excludes the rejected element or subtree from the result

### Requirement: Input bridge raises retained pointer events
Cerneala SHALL provide an `ElementInputBridge` that converts `InputFrame` pointer transitions into existing preview and bubble mouse routed events against retained elements.

#### Scenario: Mouse down raises preview then bubble
- **WHEN** the primary pointer button is pressed over a retained element
- **THEN** the bridge raises preview mouse down before mouse down on the hit-tested retained route

#### Scenario: Mouse up raises preview then bubble
- **WHEN** the primary pointer button is released for a retained element
- **THEN** the bridge raises preview mouse up before mouse up on the retained route

#### Scenario: Mouse move raises preview then bubble
- **WHEN** pointer position changes over a retained element
- **THEN** the bridge raises preview mouse move before mouse move on the retained route

#### Scenario: Mouse wheel raises preview then bubble
- **WHEN** the input frame reports a wheel delta over a retained element
- **THEN** the bridge raises preview mouse wheel before mouse wheel on the retained route

#### Scenario: Disabled element does not receive pointer handlers
- **WHEN** a disabled retained element is under the pointer
- **THEN** routed pointer handlers on that disabled element are not invoked

### Requirement: Retained routing follows retained parent chain
Cerneala SHALL route retained input events through the retained visual parent chain by reusing `UiInputTree`, `ElementInputRouteBuilder`, and `RoutedEventRouter`.

#### Scenario: Bubble route follows visual parents
- **WHEN** a bubble input event is raised on a retained child
- **THEN** handlers run from the child toward retained visual ancestors

#### Scenario: Preview route follows visual ancestors
- **WHEN** a preview input event is raised on a retained child
- **THEN** handlers run from retained visual ancestors toward the child

#### Scenario: Existing element handler store is used
- **WHEN** retained elements register routed handlers through `UIElement.Handlers`
- **THEN** the retained input bridge invokes those handlers through the existing routed event router

### Requirement: Pointer capture overrides pointer target
Cerneala SHALL provide a `PointerCaptureManager` that lets a retained element receive pointer move and release events while capture is active.

#### Scenario: Captured element receives move
- **WHEN** pointer capture is held by an element and the pointer moves outside that element
- **THEN** pointer move events target the captured element

#### Scenario: Captured element receives release
- **WHEN** pointer capture is held by an element and the pointer button is released
- **THEN** pointer release events target the captured element

#### Scenario: Capture change raises events
- **WHEN** pointer capture changes from one retained element to another
- **THEN** lost and got mouse capture routed events are raised for the affected elements

### Requirement: Hover tracker updates pointer-over state
Cerneala SHALL provide a `HoverTracker` that maintains `UIElement.IsPointerOver` and raises mouse enter/leave routed events when the hover target changes.

#### Scenario: Hover enters element
- **WHEN** pointer movement changes the hit-tested target from none to an element
- **THEN** that element has `IsPointerOver` set to true and receives mouse enter

#### Scenario: Hover leaves element
- **WHEN** pointer movement changes the hit-tested target away from an element
- **THEN** the old element has `IsPointerOver` set to false and receives mouse leave

#### Scenario: Same hover target does not invalidate
- **WHEN** pointer movement stays within the same hover target
- **THEN** hover tracking does not reapply pointer-over state or schedule render invalidation again

### Requirement: Pressed and click tracking is retained
Cerneala SHALL provide `PressedStateTracker` and `ClickTracker` services that synthesize retained pressed state and click counts from pointer transitions.

#### Scenario: Press sets pressed state
- **WHEN** the primary pointer button is pressed over a pressable retained element
- **THEN** that element's pressed state is set and render invalidation is scheduled

#### Scenario: Release clears pressed state
- **WHEN** the primary pointer button is released after a retained press
- **THEN** the pressed state is cleared

#### Scenario: Click is synthesized on matching release
- **WHEN** pointer press and release occur on the same click target
- **THEN** click tracking reports a click count for the resulting mouse button event

#### Scenario: Release outside press target cancels click
- **WHEN** pointer release targets a different retained element without capture
- **THEN** click tracking does not synthesize a click for the original press target

### Requirement: Focus manager targets keyboard input
Cerneala SHALL provide an explicit `FocusManager` service that tracks focused retained elements, updates focus visual state, and raises existing focus routed events.

#### Scenario: Focus changes element
- **WHEN** focus moves from one retained element to another
- **THEN** lost focus events are raised for the old element and got focus events are raised for the new element

#### Scenario: Focused element receives keyboard events
- **WHEN** a key is pressed and a retained element has keyboard focus
- **THEN** preview and bubble key events target the focused retained element

#### Scenario: No focused element ignores keyboard input
- **WHEN** a key is pressed and no retained element has keyboard focus
- **THEN** no retained keyboard routed event is raised

#### Scenario: Focus within follows ancestor chain
- **WHEN** a retained child receives keyboard focus
- **THEN** the focused child and retained ancestors expose keyboard-focus-within state

### Requirement: Text input bridge targets focused element
Cerneala SHALL provide a `TextInputBridge` that maps `TextInputSnapshotEvent` values to existing preview and bubble text input routed events.

#### Scenario: Focused element receives text input
- **WHEN** the input frame contains a text input event and a retained element has focus
- **THEN** preview and bubble text input events target the focused retained element

#### Scenario: Text input preserves text payload
- **WHEN** a text input routed event is raised
- **THEN** `TextCompositionEventArgs.Text` contains the text from the `TextInputSnapshotEvent`

#### Scenario: No focused element ignores text input
- **WHEN** the input frame contains text input and no retained element has focus
- **THEN** no retained text input routed event is raised

### Requirement: Visual input state is typed property state
Cerneala SHALL expose retained input visual state through typed properties that participate in invalidation.

#### Scenario: Pointer over property exists
- **WHEN** retained input visual state is implemented
- **THEN** `UIElement.IsPointerOverProperty` exists and can affect render output

#### Scenario: Keyboard focus properties exist
- **WHEN** retained focus state is implemented
- **THEN** `UIElement.IsKeyboardFocusedProperty` and `UIElement.IsKeyboardFocusWithinProperty` exist and can affect render output

#### Scenario: Pressed primitive exists
- **WHEN** retained pressed state is implemented
- **THEN** `ButtonBase.IsPressedProperty` exists as the minimal pressable control primitive

### Requirement: Retained input bridge is tested
Cerneala SHALL include focused tests for hit testing, retained input dispatch, pointer capture, hover, pressed/click tracking, focus, text input, and routed-event integration.

#### Scenario: Required input tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/Input` for `ElementInputBridge`, `HitTestService`, `PointerCaptureManager`, `HoverTracker`, `PressedStateTracker`, `ClickTracker`, `FocusManager`, `TextInputBridge`, and retained routed event integration

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
