# markup-serialization Specification

## Purpose
TBD - created by archiving change add-markup-serialization. Update Purpose after archive.
## Requirements
### Requirement: Markup documents preserve retained tree shape
Cerneala SHALL provide a markup document model that represents a single retained UI root node, ordered attributes, ordered child nodes, optional text content, and diagnostics/source information.

#### Scenario: Document stores root node
- **WHEN** a markup document is created with a root node
- **THEN** the document exposes that root node and its ordered attributes and children without reordering them

#### Scenario: Missing root is rejected
- **WHEN** markup is read without a root element
- **THEN** the reader reports a diagnostic error and no valid document root is produced

### Requirement: Markup reader parses a deterministic XML subset
Cerneala SHALL provide a markup reader that parses a small XML subset into `UiMarkupDocument` without constructing UI controls during parsing.

#### Scenario: Reader captures element attributes and children
- **WHEN** markup contains nested elements with attributes and text
- **THEN** the reader produces markup nodes that preserve element names, attribute values, text content, and child order

#### Scenario: Reader reports malformed XML
- **WHEN** markup cannot be parsed as XML
- **THEN** the reader returns a diagnostic error instead of throwing an unhandled parser exception

### Requirement: Markup writer serializes deterministically
Cerneala SHALL provide a markup writer that serializes `UiMarkupDocument` instances into deterministic XML output for supported nodes and attributes.

#### Scenario: Writer preserves stable ordering
- **WHEN** the same document is written twice
- **THEN** the serialized markup has the same element, attribute, text, and child ordering

#### Scenario: Writer rejects missing document root
- **WHEN** a document has no valid root node
- **THEN** the writer reports a diagnostic error instead of emitting invalid markup

### Requirement: Markup schema registers explicit element and property factories
Cerneala SHALL provide a markup schema and type registry where element names, constructors, content properties, child handling, property setters, and value converters are registered explicitly.

#### Scenario: Registry resolves known element
- **WHEN** a registered markup element name is requested
- **THEN** the registry returns the constructor and property metadata for that element

#### Scenario: Registry rejects unknown property
- **WHEN** a markup attribute names an unregistered property for a registered element
- **THEN** the factory reports a diagnostic error and does not silently ignore the unsupported attribute

### Requirement: UI factory creates retained trees from markup
Cerneala SHALL provide a `UiFactory` that creates retained `UIElement` trees from markup documents through the registered schema.

#### Scenario: Factory creates nested retained controls
- **WHEN** markup describes a registered panel with registered child controls
- **THEN** the factory returns a retained tree with the same logical child order

#### Scenario: Factory reports unknown element
- **WHEN** markup references an unregistered element name
- **THEN** the factory reports a diagnostic error instead of creating a placeholder control

### Requirement: Generated factories share the runtime contract
Cerneala SHALL provide a `GeneratedUiFactory` runtime seam for precompiled factories that produce retained trees and diagnostics without requiring runtime markup parsing.

#### Scenario: Generated factory creates retained tree
- **WHEN** a generated factory delegate is invoked
- **THEN** it returns a retained root element using the same `UIElement` and typed property APIs as code-created trees

#### Scenario: Generated factory reports diagnostics
- **WHEN** a generated factory cannot create a valid tree
- **THEN** it returns markup diagnostics through the same diagnostic result model as runtime factories

### Requirement: Markup remains optional
Cerneala SHALL keep markup optional so applications can continue creating every supported retained control directly in code.

#### Scenario: Code-first control creation does not depend on markup
- **WHEN** a retained control is created and configured directly in code
- **THEN** no markup reader, writer, schema, or factory is required for the control to work

### Requirement: Markup diagnostics are structured
Cerneala SHALL provide structured markup diagnostics with severity, message, code, and optional source location.

#### Scenario: Diagnostic includes source location when available
- **WHEN** a reader or factory error occurs for markup with line information
- **THEN** the diagnostic includes the source line and column when the parser provides them

### Requirement: Markup source generator emits code-first factories
Cerneala SHALL provide an optional source generator that turns supported markup files into generated code-first UI factories.

#### Scenario: Supported markup generates retained tree factory
- **WHEN** a `.cui.xml` additional file describes supported retained controls
- **THEN** the source generator emits a C# factory class whose `Create` method returns the retained root element

#### Scenario: Generated factory uses typed public properties
- **WHEN** generated code assigns supported control properties
- **THEN** it uses public control properties rather than runtime property-store mutation

### Requirement: Markup source generator reports diagnostics
Cerneala SHALL report source generator diagnostics for malformed XML and unsupported markup.

#### Scenario: Malformed markup reports diagnostic
- **WHEN** a `.cui.xml` additional file cannot be parsed as XML
- **THEN** the source generator reports a diagnostic tied to that file

#### Scenario: Unsupported element reports diagnostic
- **WHEN** markup references an unsupported element
- **THEN** the source generator reports a diagnostic instead of emitting a placeholder

### Requirement: Source generation remains optional
Cerneala SHALL keep runtime and code-first UI creation independent of the source generator.

#### Scenario: Runtime tests do not require source generator
- **WHEN** Cerneala runtime tests build and run
- **THEN** they do not require the source generator project as an analyzer dependency

