# code-first-templates Specification

## Purpose
TBD - created by archiving change add-code-first-templates. Update Purpose after archive.
## Requirements
### Requirement: Control templates create retained visual structure
Cerneala SHALL provide code-first `ControlTemplate` and `ControlTemplate<TControl>` APIs that create retained `UIElement` visual structure for controls.

#### Scenario: Typed control template receives owner context
- **WHEN** a typed control template is applied to a matching control
- **THEN** the template factory receives a typed template context for that control

#### Scenario: Template rejects incompatible owner
- **WHEN** a typed control template is applied to a control that is not assignable to the template owner type
- **THEN** template application fails before generating retained children

#### Scenario: Template output is retained child content
- **WHEN** a control template returns a retained element root
- **THEN** the generated root is attached through retained logical and visual child ownership

### Requirement: Template instances are retained and replaceable
Cerneala SHALL provide `TemplateInstance` to own generated template roots and preserve them until the template or owning modeled content changes.

#### Scenario: Same template reuses generated children
- **WHEN** the same template is applied across repeated measure, arrange, render, or frame passes
- **THEN** the previously generated template children are reused instead of rebuilt

#### Scenario: Template replacement detaches old generated children
- **WHEN** a control receives a different template
- **THEN** the old template instance is detached before the new generated children are attached

#### Scenario: Template replacement invalidates once
- **WHEN** a control template changes
- **THEN** retained invalidation is requested for the affected subtree once for that replacement

### Requirement: Template bindings are typed
Cerneala SHALL provide `TemplateBinding<T>` for explicit typed propagation from an owner `UiProperty<T>` to a generated child `UiProperty<T>`.

#### Scenario: Binding copies owner value to generated child
- **WHEN** a template binding connects an owner property to a generated child property
- **THEN** applying the template sets the child property from the owner's effective value

#### Scenario: Binding follows owner property changes
- **WHEN** the bound owner property changes after template application
- **THEN** the generated child property updates through the binding

#### Scenario: Binding rejects mismatched property type
- **WHEN** a template binding is created with source and target properties of different value types
- **THEN** creation fails before template application

#### Scenario: Binding avoids string paths in hot path
- **WHEN** template binding updates a generated child
- **THEN** it uses typed `UiProperty<T>` references rather than string property paths or runtime property-name lookup

### Requirement: Template part metadata is diagnostic only
Cerneala SHALL provide `TemplatePartAttribute` for documenting expected template parts without requiring hidden runtime magic.

#### Scenario: Template part metadata can be inspected
- **WHEN** a control type declares template part attributes
- **THEN** diagnostics can inspect the declared part names and expected part types

#### Scenario: Missing template part does not block application
- **WHEN** a template omits a diagnostic template part
- **THEN** the template can still apply without mandatory runtime part resolution

### Requirement: Content presenters materialize retained content
Cerneala SHALL provide `ContentPresenter` that materializes content into retained child structure using explicit templates.

#### Scenario: UIElement content is retained directly
- **WHEN** `ContentPresenter.Content` is already a `UIElement`
- **THEN** that element becomes the presenter's retained child content

#### Scenario: Data template creates content element
- **WHEN** presenter content is not a `UIElement` and a matching `DataTemplate` exists
- **THEN** the data template creates the retained child element for that content

#### Scenario: Content change replaces presented child
- **WHEN** presenter content changes to a different retained output
- **THEN** the old presented child is detached before the new child is attached

### Requirement: Data templates create typed retained content
Cerneala SHALL provide `DataTemplate` and `DataTemplate<T>` APIs that create retained elements for data values without runtime string binding.

#### Scenario: Typed data template receives data value
- **WHEN** a `DataTemplate<T>` is applied to a value assignable to `T`
- **THEN** its factory receives the typed value and returns retained element content

#### Scenario: Data template rejects incompatible data
- **WHEN** a `DataTemplate<T>` is applied to a value that is not assignable to `T`
- **THEN** template application fails before creating retained content

#### Scenario: Null data can produce no child
- **WHEN** a data template or presenter receives null content
- **THEN** it can produce no retained child without failing

### Requirement: Items presenters compose retained item children
Cerneala SHALL provide `ItemsPresenter` and `ItemsPanelTemplate` foundations for composing item children in a retained panel root.

#### Scenario: Items panel template creates panel root
- **WHEN** an items presenter is given an items panel template
- **THEN** the template creates the retained panel root that owns generated item children

#### Scenario: Items use data template when provided
- **WHEN** an items presenter has item values and a data template
- **THEN** each item is materialized as a retained child through the data template

#### Scenario: Items presenter updates changed item list
- **WHEN** the presented item collection changes by replacement
- **THEN** stale generated item children are detached and current item children are attached in retained order

### Requirement: Template children participate in retained systems
Cerneala SHALL make template-generated children participate in existing retained layout, rendering, hit testing, input routing, styling, and invalidation.

#### Scenario: Template child participates in layout
- **WHEN** a templated control is measured and arranged
- **THEN** generated template children are measured and arranged through the retained layout system

#### Scenario: Template child participates in rendering
- **WHEN** a templated control is rendered
- **THEN** generated template children emit retained render commands through existing rendering traversal

#### Scenario: Template child participates in hit testing and input routing
- **WHEN** pointer input targets a generated template child
- **THEN** hit testing and routed input use the retained visual parent chain containing that child

#### Scenario: Template child can be styled
- **WHEN** styling is applied to a generated template child
- **THEN** style rules and visual-state rules treat it as a normal retained element

### Requirement: Templates remain backend-neutral and tested
Cerneala SHALL keep template APIs independent of concrete rendering backends and include focused tests for the template system.

#### Scenario: Template APIs avoid backend references
- **WHEN** template-related control files are compiled
- **THEN** they do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Required template tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for control templates, template bindings, content presenters, data templates, and items panel templates

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

