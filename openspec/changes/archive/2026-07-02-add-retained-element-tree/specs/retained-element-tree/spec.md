## ADDED Requirements

### Requirement: Retained UI element base exists
Cerneala SHALL provide `UIElement` as the retained element base under `UI/Elements`.

#### Scenario: Element uses typed state
- **WHEN** a retained element stores UI state
- **THEN** it uses the typed state model from `UI/Core`

#### Scenario: Element exposes parentage
- **WHEN** an element is in a retained tree
- **THEN** it exposes its logical parent, visual parent, logical children, visual children, root, and attachment state

### Requirement: Logical and visual trees are separate
Cerneala SHALL represent logical and visual child relationships separately.

#### Scenario: Logical child is added
- **WHEN** an element is added as a logical child
- **THEN** its logical parent is set to the owning element

#### Scenario: Visual child is added
- **WHEN** an element is added as a visual child
- **THEN** its visual parent is set to the owning element

#### Scenario: Logical and visual parentage do not overwrite each other
- **WHEN** an element has both a logical parent and a visual parent
- **THEN** changing one parent kind does not silently change the other parent kind

### Requirement: Child collections enforce ownership
Cerneala SHALL provide owned child collections that preserve tree integrity.

#### Scenario: Adding a child sets exactly one parent for the collection role
- **WHEN** a child is added to a logical or visual child collection
- **THEN** exactly the matching parent relationship is assigned

#### Scenario: Removing a child clears matching parent
- **WHEN** a child is removed from a logical or visual child collection
- **THEN** the matching parent relationship is cleared

#### Scenario: Reparenting without removal is rejected
- **WHEN** a child already has a parent for the same collection role
- **THEN** adding it to another collection for that role fails

#### Scenario: Duplicate child add is rejected
- **WHEN** the same child is added twice to the same collection
- **THEN** the second add fails

### Requirement: Root attachment owns element ids
Cerneala SHALL provide `UIRoot` as the attachment root and source of stable `UiElementId` values.

#### Scenario: Attached element gets stable id
- **WHEN** an element is attached to a root
- **THEN** it receives a `UiElementId` that remains stable while it stays attached

#### Scenario: Descendants attach with root
- **WHEN** a subtree is attached to a root
- **THEN** each descendant receives the same root reference and an element id

#### Scenario: Detached element leaves route ownership
- **WHEN** an element is detached from its root
- **THEN** it is no longer included in the retained input route map

### Requirement: Element lifecycle is deterministic
Cerneala SHALL provide deterministic attach, detach, and tree version behavior.

#### Scenario: Attach increments tree version
- **WHEN** an element or subtree is attached
- **THEN** the owning root tree version changes

#### Scenario: Detach increments tree version
- **WHEN** an element or subtree is detached
- **THEN** the owning root tree version changes

#### Scenario: Lifecycle order is deterministic
- **WHEN** a subtree is attached or detached
- **THEN** lifecycle hooks run in a documented deterministic order

### Requirement: Tree walking is deterministic
Cerneala SHALL provide traversal helpers for retained element trees.

#### Scenario: Pre-order traversal visits parent before children
- **WHEN** a retained subtree is walked in pre-order
- **THEN** each parent is returned before its descendants

#### Scenario: Post-order traversal visits children before parent
- **WHEN** a retained subtree is walked in post-order
- **THEN** descendants are returned before their parent

#### Scenario: Ancestor traversal returns route to root
- **WHEN** ancestors are requested for an element
- **THEN** they are returned from nearest parent toward root

### Requirement: Element route bridge uses retained tree
Cerneala SHALL build retained input route data from retained elements.

#### Scenario: Route map associates elements and ids
- **WHEN** an attached retained element is routable
- **THEN** the route map can resolve `UIElement` to `UiElementId` and `UiElementId` to `UIElement`

#### Scenario: Visual ancestry defines pointer route order
- **WHEN** a pointer target is routed through the bridge
- **THEN** route order follows the retained visual ancestor chain

#### Scenario: Disabled or invisible elements can be excluded
- **WHEN** an element is disabled or excluded by visibility/input policy
- **THEN** the route bridge can omit it from hit-test or pointer input routing

### Requirement: Routed event handlers are stored on elements
Cerneala SHALL allow retained elements to store routed event handlers without making `UiInputTree` the retained tree.

#### Scenario: Handler store records element handlers
- **WHEN** a handler is registered on a retained element
- **THEN** the element handler store records it by routed event

#### Scenario: Route builder exports handlers
- **WHEN** a retained route table is built
- **THEN** registered element handlers can be exported to the low-level input router

### Requirement: Retained element tree is tested
Cerneala SHALL include focused tests for retained element tree behavior.

#### Scenario: Required element tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for element tree ownership, child collections, root attachment, lifecycle, tree walking, and input route bridge behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
