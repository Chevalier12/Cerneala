## Why

Cerneala now has retained controls, code-first templates, scrolling, and range primitives, but item-heavy controls still rebuild broad child trees and have no shared selection or virtualization model. Section 17 of `ROADMAPv2.md` adds retained item controls so large lists can realize only visible containers, preserve container identity, and update selection without invalidating unrelated items.

## What Changes

- Add retained item collection and item-control infrastructure: `ItemsControl`, `ItemCollection`, `ItemContainerGenerator`, `ItemContainerRecyclePool`, and an upgraded `ItemsPresenter`.
- Add selection infrastructure: `SelectionModel`, `SelectionModel<T>`, and `Primitives/Selector`.
- Add concrete selection controls: `ListBox`, `ListBoxItem`, `ComboBox`, `TabControl`, and `TabItem`.
- Add layout virtualization: `VirtualizingStackPanel`, `VirtualizationContext`, and `RealizationWindow`.
- Integrate items with existing data templates, item panel templates, scrolling, retained layout, hit testing, routed input, styles, visual state, and invalidation.
- Add focused tests for item generation/recycling, selection, selector behavior, list box behavior, virtualization, and scroll-driven realization.
- Update `ROADMAPv2.md` section 17 as files, tests, and behavior are completed.

## Capabilities

### New Capabilities
- `items-selection-virtualization`: Covers retained item controls, container generation and recycling, selection models, selector-derived controls, and scroll-aware virtualization.

### Modified Capabilities

## Impact

- Adds new controls under `UI/Controls` and `UI/Controls/Primitives`.
- Adds virtualization layout APIs under `UI/Layout/Panels` and `UI/Layout/Virtualization`.
- Adds tests under `tests/Cerneala.Tests/Controls` and `tests/Cerneala.Tests/UI/Layout`.
- Reuses existing retained elements, typed properties, templates, panels, scroll viewer contracts, routed input, visual state, styling, and invalidation.
- Does not add data observation/binding APIs from section 18, advanced editable combo box text behavior, multi-window popups, keyboard navigation polish beyond minimal retained behavior, or accessibility APIs.
