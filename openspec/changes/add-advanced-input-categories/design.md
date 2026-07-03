## Context

The retained input stack already has routed event routing, hit testing, focus, mouse/keyboard/text bridges, pointer capture, and metadata for advanced categories in `InputEvents`. The missing piece is category-specific data and deterministic dispatch for touch, stylus, gestures, manipulation, drag/drop, cursor queries, and ink.

This phase is optional/experimental, so the implementation should be useful but intentionally small. Platform adapters can feed explicit snapshots later; this change must not poll OS or MonoGame APIs directly.

## Goals / Non-Goals

**Goals:**

- Provide typed event args and bridges for touch and stylus routed events.
- Keep gesture/manipulation logic deterministic and testable from explicit input samples.
- Provide minimal drag/drop and cursor services that use retained routing and hit testing.
- Provide an `InkCanvas` that records strokes from stylus or touch samples without depending on a renderer-specific ink engine.
- Keep all advanced input APIs backend-neutral.

**Non-Goals:**

- No OS clipboard/drag integration.
- No multi-window drag/drop.
- No high-fidelity handwriting recognition, pressure curves, smoothing, palm rejection, or inertia physics.
- No platform cursor mutation; `CursorService` resolves requested cursors only.
- No replacement for the existing mouse/keyboard `ElementInputBridge`.

## Decisions

### Use explicit snapshots

Touch and stylus bridges should accept explicit frame/sample objects and route events through `ElementInputRouteBuilder`, `HitTestService`, and `RoutedEventRouter`.

Rationale: this mirrors the existing retained input design and keeps platform adapters outside UI core.

Alternative considered: integrate advanced input into `InputFrame`. Rejected for now because section 26 is experimental and mouse/keyboard frame stability matters.

### Add typed args beside existing metadata

`InputEvents` can keep the same routed event names, but advanced dispatch should use typed args such as `TouchEventArgs`, `StylusEventArgs`, `ManipulationEventArgs`, and `DragEventArgs`.

Rationale: routed events already validate only by event identity at runtime; typed args make handlers useful without changing event names.

Alternative considered: leave everything as bare `RoutedEventArgs`. Rejected because handlers would have no position/device/data payload.

### Gesture and manipulation are small state machines

`GestureRecognizer` should recognize tap and drag from ordered pointer samples. `ManipulationProcessor` should track two active points and report translation/scale deltas.

Rationale: deterministic state machines give useful tests and avoid pretending to implement a full OS gesture stack.

Alternative considered: defer recognizers until platform data arrives. Rejected because bridge-level tests need behavior now.

### Drag/drop stays local and explicit

`DataTransfer` should be an in-memory typed/string payload container. `DragDropController` should begin, update, and complete local drag sessions by routing enter/over/leave/drop events.

Rationale: this gives controls a stable contract without promising OS drag/drop.

Alternative considered: bind to OS data objects. Rejected because platform boundaries are not in scope.

### Ink is retained data, not immediate drawing magic

`Stroke` and `StrokeCollection` should store points and metadata. `InkCanvas` should add strokes from stylus/touch samples and invalidate measure/render when strokes change. Rendering can stay minimal or deferred; correctness is retained data ownership.

Rationale: ink must be editable/testable data first.

Alternative considered: render strokes directly through backend-specific drawing. Rejected because it violates backend neutrality.

## Risks / Trade-offs

- [Risk] The scope can explode into OS-grade input. -> Keep only deterministic MVP APIs and tests.
- [Risk] Advanced event args can drift from existing routed events. -> Reuse `InputEvents` identities and route through existing router.
- [Risk] Drag/drop can imply platform integration. -> Name behavior as local retained drag/drop.
- [Risk] InkCanvas can become a drawing engine. -> Store retained strokes and keep rendering minimal.
- [Risk] Cursor service can imply OS mutation. -> Resolve requested cursor values only.
