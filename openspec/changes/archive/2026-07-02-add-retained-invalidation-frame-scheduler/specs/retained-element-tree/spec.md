## ADDED Requirements

### Requirement: Retained elements own dirty state
Cerneala SHALL make retained elements the owners of per-element dirty state and invalidation entry points.

#### Scenario: Element exposes dirty state
- **WHEN** retained invalidation marks an element dirty
- **THEN** the element exposes dirty state that identifies active retained invalidation flags

#### Scenario: Element can be invalidated manually
- **WHEN** retained code requests manual invalidation for an element
- **THEN** the element records the requested dirty flags and diagnostic reason

#### Scenario: Attached element invalidation reaches root
- **WHEN** an attached element is invalidated
- **THEN** its root receives the invalidation request for queueing and frame scheduling

#### Scenario: Detached element invalidation does not enqueue root work
- **WHEN** a detached element is invalidated
- **THEN** no root queue receives work from that request

### Requirement: Retained root owns invalidation scheduling
Cerneala SHALL make `UIRoot` the scheduling owner for attached retained element invalidation.

#### Scenario: Root exposes scheduler
- **WHEN** a retained root exists
- **THEN** it exposes the frame scheduler or scheduling entry point used to process dirty work

#### Scenario: Root queues attached invalidation
- **WHEN** an attached element submits an invalidation request
- **THEN** the root queues layout, render, or hit-test work according to propagation rules

#### Scenario: Root frame processing returns stats
- **WHEN** root frame work is processed
- **THEN** frame stats are returned for diagnostics and tests
