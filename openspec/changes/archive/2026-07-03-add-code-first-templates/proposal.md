## Why

Cerneala has retained controls, styling, layout, input, and rendering, but reusable control composition is still hand-coded per control. Section 15 of `ROADMAPv2.md` adds code-first templates so controls can generate retained visual structure without string-path binding, hidden markup magic, or per-frame rebuilding.

## What Changes

- Add typed `ControlTemplate` primitives that build retained child trees for controls.
- Add `TemplateContext`, `TemplateInstance`, and typed `TemplateBinding<T>` so generated children can read owner state without reflection-heavy string paths in the hot path.
- Add diagnostic-only template part metadata through `TemplatePartAttribute`.
- Add `ContentPresenter`, `ItemsPresenter`, `DataTemplate`, and `ItemsPanelTemplate` foundations for content and item composition.
- Ensure template-generated children are retained across frames and replaced only when the relevant template changes.
- Ensure template children participate in the existing layout, rendering, hit testing, input routing, invalidation, and retained tree ownership rules.
- Update `ROADMAPv2.md` section 15 as files, tests, and acceptance criteria are completed.

## Capabilities

### New Capabilities
- `code-first-templates`: Covers typed control templates, template instances, typed template bindings, template presenters, and retained composition behavior.

### Modified Capabilities

## Impact

- Adds new APIs under `UI/Controls` for templates, presenters, bindings, and template diagnostics.
- Adds tests under `tests/Cerneala.Tests/Controls`.
- Uses existing retained element tree, typed property system, styling, layout, rendering, hit testing, and input routing contracts.
- Does not add markup, XAML, runtime property paths, template animations, virtualization, or a broad data-binding system.
