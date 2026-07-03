## 1. Touch And Stylus

- [x] 1.1 Create typed touch sample/frame/event args and `UI/Input/TouchInputBridge.cs`.
- [x] 1.2 Create typed stylus sample/frame/event args and `UI/Input/StylusInputBridge.cs`.
- [x] 1.3 Add `tests/Cerneala.Tests/Input/TouchInputBridgeTests.cs`.
- [x] 1.4 Add `tests/Cerneala.Tests/Input/StylusInputBridgeTests.cs`.

## 2. Gestures And Manipulation

- [x] 2.1 Create `UI/Input/GestureRecognizer.cs` with deterministic tap and drag recognition.
- [x] 2.2 Create `UI/Input/ManipulationProcessor.cs` with translation and scale deltas.
- [x] 2.3 Add `tests/Cerneala.Tests/Input/GestureRecognizerTests.cs`.
- [x] 2.4 Add `tests/Cerneala.Tests/Input/ManipulationProcessorTests.cs`.

## 3. Drag Drop And Cursor

- [x] 3.1 Create `UI/Input/DataTransfer.cs`.
- [x] 3.2 Create `UI/Input/DragDropController.cs` with local enter/over/leave/drop routing.
- [x] 3.3 Create `UI/Input/Cursor.cs` and `UI/Input/CursorService.cs`.
- [x] 3.4 Add `tests/Cerneala.Tests/Input/DragDropControllerTests.cs`.
- [x] 3.5 Add focused cursor service coverage.

## 4. Ink

- [x] 4.1 Create `UI/Ink/Stroke.cs`.
- [x] 4.2 Create `UI/Ink/StrokeCollection.cs`.
- [x] 4.3 Create `UI/Controls/InkCanvas.cs`.
- [x] 4.4 Add `tests/Cerneala.Tests/Controls/InkCanvasTests.cs`.

## 5. Integration And Verification

- [x] 5.1 Prove advanced input reuses `InputEvents` event identities and retained route maps.
- [x] 5.2 Prove advanced input and ink files remain backend-neutral.
- [x] 5.3 Run focused advanced input tests.
- [x] 5.4 Run full test suite.
- [x] 5.5 Validate OpenSpec strictly.
- [x] 5.6 Update `ROADMAPv2.md` section 26 checkboxes for completed files, tests, and existing metadata.
