## ADDED Requirements

### Requirement: Touch bridge dispatches retained touch events
Cerneala SHALL provide a touch input bridge that routes touch down, move, and up events from explicit touch frames through the retained input route map.

#### Scenario: Touch down reaches hit target
- **WHEN** a touch frame contains a new touch point over an arranged retained element
- **THEN** preview and bubble touch down events are routed to that target with touch id and position data

#### Scenario: Touch move follows captured target
- **WHEN** a touch point moves after being captured by an element
- **THEN** the touch move event is routed to the captured target

### Requirement: Stylus bridge dispatches retained stylus events
Cerneala SHALL provide a stylus input bridge that routes stylus down, move, up, in-range, out-of-range, and button events from explicit stylus frames.

#### Scenario: Stylus down includes pressure and position
- **WHEN** a stylus down sample is dispatched over a retained element
- **THEN** stylus event args include stylus id, position, pressure, and in-range state

#### Scenario: Stylus button routes typed event args
- **WHEN** a stylus button changes state over a retained element
- **THEN** the corresponding stylus button routed event carries typed stylus event args

### Requirement: Gestures are recognized deterministically
Cerneala SHALL provide a gesture recognizer that recognizes tap and drag gestures from ordered pointer samples.

#### Scenario: Tap recognized within threshold
- **WHEN** a pointer goes down and up without exceeding the movement threshold
- **THEN** the recognizer reports a tap gesture

#### Scenario: Drag recognized after threshold
- **WHEN** a pointer moves beyond the drag threshold while pressed
- **THEN** the recognizer reports drag start and drag delta gestures

### Requirement: Manipulation processor computes translation and scale
Cerneala SHALL provide a manipulation processor that computes translation and scale deltas from active touch points.

#### Scenario: One point translates
- **WHEN** one active touch point moves
- **THEN** manipulation delta reports the position translation

#### Scenario: Two points scale
- **WHEN** two active touch points move farther apart
- **THEN** manipulation delta reports a scale greater than one

### Requirement: Local drag/drop routes retained events with data
Cerneala SHALL provide local drag/drop primitives that route drag enter, over, leave, and drop events with `DataTransfer` payloads.

#### Scenario: Drag over changes target
- **WHEN** a drag session moves from one retained element to another
- **THEN** drag leave is routed from the old target and drag enter is routed to the new target

#### Scenario: Drop delivers data transfer
- **WHEN** a drag session is dropped over a retained element
- **THEN** the drop event args expose the original `DataTransfer` payload

### Requirement: Cursor service resolves retained cursor requests
Cerneala SHALL provide cursor primitives that let retained elements request a cursor and let callers resolve the cursor at a point.

#### Scenario: Cursor resolves from hit element
- **WHEN** a cursor is requested for a point over an element with an assigned cursor
- **THEN** the cursor service returns that cursor

### Requirement: Ink canvas records retained strokes
Cerneala SHALL provide `InkCanvas`, `Stroke`, and `StrokeCollection` for retaining stylus/touch stroke data.

#### Scenario: Ink canvas records stylus stroke
- **WHEN** stylus down, move, and up samples are applied to an ink canvas
- **THEN** the canvas stores one stroke with the sampled points in order

#### Scenario: Stroke collection notifies on mutation
- **WHEN** a stroke is added or removed
- **THEN** the stroke collection reports the change to listeners

### Requirement: Advanced input remains backend-neutral
Cerneala SHALL keep advanced input and ink APIs independent of concrete rendering and platform backends.

#### Scenario: Advanced input core has no backend references
- **WHEN** advanced input source files are inspected
- **THEN** they do not reference MonoGame, Skia, HarfBuzz, SpriteBatch, Texture2D, or platform polling APIs
