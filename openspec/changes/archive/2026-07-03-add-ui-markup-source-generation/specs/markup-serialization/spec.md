## ADDED Requirements

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
