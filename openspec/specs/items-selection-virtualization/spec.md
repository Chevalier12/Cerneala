# items-selection-virtualization Specification

## Purpose
TBD - created by archiving change add-items-selection-virtualization. Update Purpose after archive.
## Requirements
### Requirement: ItemsControl owns retained item data and presentation policy
Cerneala SHALL provide `ItemsControl` and `ItemCollection` for retained item controls.

#### Scenario: ItemsControl exposes item collection state
- **WHEN** an items control is assigned item data
- **THEN** the control exposes the items through an `ItemCollection` in retained order

#### Scenario: ItemsControl uses data templates
- **WHEN** an item is not already a `UIElement` and an item template exists
- **THEN** generated item content is created through the existing data template APIs

#### Scenario: ItemsControl uses item panel templates
- **WHEN** an items control is assigned an items panel template
- **THEN** its presenter creates the retained panel root from that template

### Requirement: Item containers are generated and recycled
Cerneala SHALL provide `ItemContainerGenerator` and `ItemContainerRecyclePool` for retained item container lifetime.

#### Scenario: Generator creates containers for realized indexes
- **WHEN** a realization window includes item indexes
- **THEN** the generator creates or reuses containers for those indexes

#### Scenario: Realized container identity is stable
- **WHEN** the realization window is unchanged across layout passes
- **THEN** previously realized containers are reused instead of rebuilt

#### Scenario: Unrealized containers are detached and pooled
- **WHEN** an item index leaves the realization window
- **THEN** its container is detached from the retained tree and placed in the recycle pool when compatible

#### Scenario: Recycled containers are prepared with current item state
- **WHEN** a recycled container is reused for a different item
- **THEN** stale item, index, and selection visual state is cleared or replaced before the container is attached

### Requirement: ItemsPresenter realizes retained item containers
Cerneala SHALL upgrade `ItemsPresenter` so it can present generated containers from an item generator while preserving existing template behavior.

#### Scenario: Presenter hosts generated containers
- **WHEN** an items presenter is measured for an items control
- **THEN** it hosts retained generated containers under its panel root

#### Scenario: Presenter preserves unrelated realized containers
- **WHEN** item data changes outside the realized range
- **THEN** already realized unrelated containers are not rebuilt

#### Scenario: Presenter supports non-virtualized fallback
- **WHEN** no virtualization context is active
- **THEN** the presenter can realize all items in retained order using the existing item panel behavior

### Requirement: Selection models track selection independently from containers
Cerneala SHALL provide `SelectionModel` and `SelectionModel<T>` for index/item selection independent of realized visual containers.

#### Scenario: Selection model selects an index
- **WHEN** an index is selected
- **THEN** the selection model reports that index as selected

#### Scenario: Selection model changes current selection
- **WHEN** single selection moves from one index to another
- **THEN** the old index is unselected and the new index is selected

#### Scenario: Typed selection model exposes typed selected item
- **WHEN** a typed selection model selects an item
- **THEN** callers can read the selected item with the declared item type

#### Scenario: Selection survives virtualization
- **WHEN** a selected item scrolls out of the realization window and later returns
- **THEN** its newly realized container reflects selected state from the selection model

### Requirement: Selector bridges retained input to selection state
Cerneala SHALL provide `Primitives/Selector` as the retained base class for selectable item controls.

#### Scenario: Selector click selects item container
- **WHEN** retained pointer input clicks a selectable item container
- **THEN** the selector updates its selection model for that item index

#### Scenario: Selection invalidates affected containers only
- **WHEN** selection changes from one realized item to another
- **THEN** only the old and new selected containers require visual-state invalidation

#### Scenario: Selector preserves selected state across container recycling
- **WHEN** a selected container is recycled and later reused for an unselected item
- **THEN** the reused container does not retain stale selected state

### Requirement: ListBox provides retained selectable list behavior
Cerneala SHALL provide `ListBox` and `ListBoxItem` built on `Selector`, retained item containers, and virtualization-aware presentation.

#### Scenario: ListBox creates ListBoxItem containers
- **WHEN** a list box realizes item indexes
- **THEN** generated containers are `ListBoxItem` instances unless the item is already a compatible container

#### Scenario: ListBoxItem exposes selected state
- **WHEN** a list box item represents a selected index
- **THEN** the item exposes selected visual state through typed property state

#### Scenario: ListBox selection follows retained click input
- **WHEN** a retained click targets a list box item
- **THEN** the list box selection model selects that item index

### Requirement: ComboBox and TabControl reuse retained selection primitives
Cerneala SHALL provide retained `ComboBox`, `TabControl`, and `TabItem` controls backed by item containers and selection state.

#### Scenario: ComboBox exposes selected item
- **WHEN** combo box selection changes
- **THEN** the combo box exposes the selected item and selected index from its selection model

#### Scenario: ComboBox uses retained item containers
- **WHEN** combo box items are realized
- **THEN** item containers participate in retained layout, rendering, hit testing, and input routing

#### Scenario: TabControl selects tab item
- **WHEN** retained input selects a tab item
- **THEN** the tab control updates selected index and selected tab item state

#### Scenario: TabItem hosts retained content
- **WHEN** a tab item has content
- **THEN** the selected tab content can participate in retained layout and rendering

### Requirement: VirtualizingStackPanel realizes visible item ranges
Cerneala SHALL provide `VirtualizingStackPanel`, `VirtualizationContext`, and `RealizationWindow` for stack-based item virtualization.

#### Scenario: Realization window is computed from scroll viewport
- **WHEN** a virtualization context has item count, estimated item size, viewport size, scroll offset, and cache length
- **THEN** it produces a deterministic realized index range

#### Scenario: Virtualizing panel measures realized children only
- **WHEN** a virtualizing stack panel is measured with a realization window
- **THEN** only children in that realization window are measured

#### Scenario: Virtualizing panel arranges realized children at scroll-adjusted positions
- **WHEN** a virtualizing stack panel arranges realized children
- **THEN** each child receives a deterministic arranged position based on its item index and scroll offset

#### Scenario: Virtualizing panel reports extent
- **WHEN** item count and item extent are known
- **THEN** the virtualizing panel reports total extent for scroll integration

### Requirement: Scrolling changes realization without rebuilding unrelated items
Cerneala SHALL connect retained scrolling to item realization so scrolling updates only affected realized item containers.

#### Scenario: Scroll offset changes realization window
- **WHEN** scroll offset changes enough to move the realization window
- **THEN** containers that leave the window are recycled and containers that enter the window are realized

#### Scenario: Scroll within same realization window reuses containers
- **WHEN** scroll offset changes but the realized index range remains the same
- **THEN** realized containers are reused and only arrange/render work needed for offset changes is invalidated

#### Scenario: Large data set avoids full realization
- **WHEN** an items control is given a large item count and a constrained viewport
- **THEN** the retained tree contains only realized containers plus configured cache, not every item

### Requirement: Items selection virtualization remains backend-neutral and tested
Cerneala SHALL keep items, selection, and virtualization APIs independent of concrete rendering backends and include focused tests.

#### Scenario: New section 17 files avoid concrete backend references
- **WHEN** section 17 control and layout files are compiled
- **THEN** they do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Required section 17 tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for items control, item container generation, item container recycling, selection model, selector, list box, combo box, tab control, tab item, virtualizing stack panel, and virtualization behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

