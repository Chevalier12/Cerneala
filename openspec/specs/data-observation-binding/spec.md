# data-observation-binding Specification

## Purpose
TBD - created by archiving change add-data-observation-binding. Update Purpose after archive.
## Requirements
### Requirement: Observable values expose typed change notifications
Cerneala SHALL provide `ObservableValue<T>` for explicit typed scalar observation.

#### Scenario: Value changes notify with old and new values
- **WHEN** an observable value changes to a non-equal value
- **THEN** observers receive typed old and new values

#### Scenario: Equal values do not notify
- **WHEN** an observable value is assigned an equal value
- **THEN** no change notification is emitted

### Requirement: Observable lists expose ordered collection changes
Cerneala SHALL provide `IObservableList<T>` and `ObservableList<T>` for typed ordered list observation.

#### Scenario: Add and remove operations notify with indexes and items
- **WHEN** items are added to or removed from an observable list
- **THEN** observers receive the change kind, index, and affected item data

#### Scenario: Reset operations replace list contents
- **WHEN** an observable list is replaced with new contents
- **THEN** observers receive a reset notification and enumeration reflects the new order

### Requirement: Typed property adapters connect model state without string paths
Cerneala SHALL provide `PropertyAdapter<TOwner,TValue>` for typed owner/value access.

#### Scenario: Adapter reads and writes typed values
- **WHEN** an adapter is created from typed getter and setter delegates
- **THEN** callers can read and write the owner value without casts or string paths

#### Scenario: Adapter can update a retained UI property
- **WHEN** an adapter writes to a `UiObject` typed property
- **THEN** the existing typed property invalidation behavior is used

### Requirement: Binding-light APIs connect observable sources to explicit targets
Cerneala SHALL provide `Binding`, `Binding<T>`, `BindingMode`, and `IValueConverter<TIn,TOut>` for explicit typed data flow.

#### Scenario: One-way binding updates target from source
- **WHEN** a one-way binding observes a source value change
- **THEN** the target setter is called with the converted typed value

#### Scenario: Binding disposal stops updates
- **WHEN** a binding is disposed
- **THEN** later source changes do not update the target

#### Scenario: Two-way binding can push target changes back to source
- **WHEN** a two-way binding target update is committed
- **THEN** the source value is updated through the typed writer

### Requirement: Collection views provide typed filtering and sorting
Cerneala SHALL provide `CollectionView<T>`, `SortDescription<T>`, and `FilterPredicate<T>` for derived typed views.

#### Scenario: Collection view filters source items
- **WHEN** a filter predicate is configured
- **THEN** the view contains only matching items in source order

#### Scenario: Collection view sorts source items
- **WHEN** sort descriptions are configured
- **THEN** the view enumerates matching items in deterministic sorted order

#### Scenario: Source changes refresh the derived view
- **WHEN** an observable source list changes
- **THEN** the collection view refreshes and emits a view reset notification

### Requirement: String property paths remain deferred
Cerneala SHALL keep string-path binding out of the hot-path core for this phase.

#### Scenario: StringPropertyPath reports unsupported behavior
- **WHEN** code attempts to create or evaluate a string property path
- **THEN** the API reports that string-path binding is deferred and unsupported in core

### Requirement: Data observation and binding remains backend-neutral and tested
Cerneala SHALL keep data observation APIs independent of concrete rendering/input backends and include focused tests.

#### Scenario: Data APIs avoid concrete backend references
- **WHEN** section 18 data files are compiled
- **THEN** they do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Required section 18 tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for observable values, observable lists, typed binding, collection views, and string path deferral

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

