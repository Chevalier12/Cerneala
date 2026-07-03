## 1. Visual State Primitives

- [x] 1.1 Add `UI/Elements/UIElement.IsPointerOverProperty`.
- [x] 1.2 Add `UI/Elements/UIElement.IsKeyboardFocusedProperty`.
- [x] 1.3 Add `UI/Elements/UIElement.IsKeyboardFocusWithinProperty`.
- [x] 1.4 Ensure input visual state property changes schedule render invalidation only when values actually change.
- [x] 1.5 Add `UI/Controls/Primitives/ButtonBase.cs` with minimal `IsPressedProperty`.

## 2. Hit Testing

- [x] 2.1 Create `UI/Input/HitTestResult.cs`.
- [x] 2.2 Create `UI/Input/HitTestFilter.cs`.
- [x] 2.3 Create `UI/Input/HitTestService.cs` using retained visual tree arranged bounds.
- [x] 2.4 Ensure hit testing skips invisible and collapsed elements.
- [x] 2.5 Ensure hit testing skips disabled elements.
- [x] 2.6 Ensure hit testing uses reverse visual order so topmost retained child wins.
- [x] 2.7 Add `tests/Cerneala.Tests/Input/HitTestServiceTests.cs`.

## 3. Retained Routing Bridge

- [x] 3.1 Create `UI/Input/ElementRoutedEventStore.cs` only if the existing `ElementHandlerStore` needs an input-facing wrapper.
- [x] 3.2 Update or extend `UI/Input/RoutedEventRouter.cs` so retained bridge scenarios can raise preview and bubble pairs without duplicating route traversal.
- [x] 3.3 Update or extend `UI/Input/InputEvents.cs` only for missing MVP mouse/key/text events; leave stylus/touch/drag behavior deferred.
- [x] 3.4 Create route-map helpers needed by `ElementInputBridge` while reusing `ElementInputRouteBuilder`.
- [x] 3.5 Add `tests/Cerneala.Tests/Input/RetainedRoutedEventIntegrationTests.cs`.

## 4. Pointer State Services

- [x] 4.1 Create `UI/Input/PointerCaptureManager.cs`.
- [x] 4.2 Create `UI/Input/HoverTracker.cs`.
- [x] 4.3 Create `UI/Input/PressedStateTracker.cs`.
- [x] 4.4 Create `UI/Input/ClickTracker.cs`.
- [x] 4.5 Ensure pointer capture overrides hit-tested pointer targets for move and release.
- [x] 4.6 Ensure hover changes update `IsPointerOver` and raise mouse enter/leave.
- [x] 4.7 Ensure pressed state updates `ButtonBase.IsPressed` and invalidates render.
- [x] 4.8 Ensure click tracking reports click count only for matching press/release targets.
- [x] 4.9 Add `tests/Cerneala.Tests/Input/PointerCaptureManagerTests.cs`.
- [x] 4.10 Add `tests/Cerneala.Tests/Input/HoverTrackerTests.cs`.
- [x] 4.11 Add `tests/Cerneala.Tests/Input/PressedStateTrackerTests.cs`.
- [x] 4.12 Add `tests/Cerneala.Tests/Input/ClickTrackerTests.cs`.

## 5. Focus And Keyboard Services

- [x] 5.1 Create `UI/Input/FocusManager.cs` as an explicit service.
- [x] 5.2 Create `UI/Input/FocusScope.cs` as an MVP shell if nested scopes are not implemented yet.
- [x] 5.3 Create `UI/Input/KeyboardNavigation.cs` for direct focus MVP behavior.
- [x] 5.4 Ensure focus changes update `IsKeyboardFocused` and `IsKeyboardFocusWithin`.
- [x] 5.5 Ensure focus changes raise existing preview/bubble focus routed events.
- [x] 5.6 Ensure keyboard events target the focused retained element.
- [x] 5.7 Add `tests/Cerneala.Tests/Input/FocusManagerTests.cs`.

## 6. Text Input Bridge

- [x] 6.1 Create `UI/Input/TextInputBridge.cs`.
- [x] 6.2 Map `TextInputSnapshotEvent` to preview and bubble text input routed events.
- [x] 6.3 Preserve text payload in `TextCompositionEventArgs.Text`.
- [x] 6.4 Ignore text input when no retained element has focus.
- [x] 6.5 Add `tests/Cerneala.Tests/Input/TextInputBridgeTests.cs`.

## 7. Element Input Bridge And Host Integration

- [x] 7.1 Create `UI/Input/ElementInputBridge.cs`.
- [x] 7.2 Convert pointer down/up/move/wheel transitions from `InputFrame` into retained routed events.
- [x] 7.3 Dispatch pointer events in preview-before-bubble order.
- [x] 7.4 Dispatch keyboard down/up transitions to the focused retained element.
- [x] 7.5 Dispatch text input through `TextInputBridge`.
- [x] 7.6 Ensure disabled retained elements do not receive input handlers.
- [x] 7.7 Update `UI/Hosting/UiHost.cs` to run `ElementInputBridge` before scheduler processing.
- [x] 7.8 Ensure input visual invalidation can be processed in the same update frame.
- [x] 7.9 Add `tests/Cerneala.Tests/Input/ElementInputBridgeTests.cs`.

## 8. Roadmap And Validation

- [x] 8.1 Update `ROADMAPv2.md` section 8 checkboxes immediately as each file and acceptance contract is completed.
- [x] 8.2 Run focused input bridge tests.
- [x] 8.3 Run focused hosting tests.
- [x] 8.4 Run `dotnet test`.
- [x] 8.5 Run `openspec validate add-retained-input-bridge --strict`.
- [x] 8.6 Run `openspec validate --all --strict`.
