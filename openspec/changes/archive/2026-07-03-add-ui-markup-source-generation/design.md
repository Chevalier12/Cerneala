## Context

Cerneala has a runtime markup document model, reader/writer, explicit schema, runtime `UiFactory`, and `GeneratedUiFactory` seam. Roadmap section 25 intentionally deferred source generation until the runtime markup shape was stable. That is now true enough for a small prototype.

The generator should not become a XAML clone. It should support the same small XML subset used by runtime markup tests and emit plain C# that constructs retained controls with public typed properties.

## Goals / Non-Goals

**Goals:**

- Add a standalone source generator project.
- Generate code from additional `.cui.xml` markup files.
- Emit deterministic factory classes that create retained UI trees.
- Prove generated output compiles against Cerneala runtime APIs and uses code-first typed property assignment.
- Keep generation backend-neutral and optional.

**Non-Goals:**

- No full XAML compatibility.
- No runtime file loading from generated factories.
- No binding expressions, resources, styles, event handlers, templates, or arbitrary reflection.
- No integration into the main runtime project as a required analyzer.
- No broad generated factory registry.

## Decisions

### Additional files are the source input

`UiMarkupGenerator` should read `AdditionalText` files ending in `.cui.xml`. The generated class name is derived from the file name and emitted under a stable namespace.

Rationale: tests can run the generator directly, and applications can opt in by adding markup files as additional files later.

Alternative considered: attributes on C# classes. Rejected because the roadmap specifically describes markup/serialization source generation.

### Emit code-first construction

Generated source should construct supported controls directly and set public properties such as `Text`, `Content`, `Background`, `Padding`, and `FontSize`. Child relationships should use public child/content APIs.

Rationale: generated factories must use the same typed property and retained invalidation paths as hand-written code.

Alternative considered: generated code calls runtime `UiMarkupReader` and `UiFactory`. Rejected because that preserves runtime parsing cost and misses the purpose of source generation.

### Keep supported surface deliberately small

This prototype should support the stable controls already covered by runtime markup: `Panel`, `StackPanel`, `Border`, `Button`, and `TextBlock`, plus simple scalar conversions for strings, booleans, floats, thickness, and draw colors.

Rationale: enough to prove the architecture without making the generator a second markup engine.

Alternative considered: share runtime `UiMarkupSchema` directly. Rejected because source generators run at compile time and should not depend on runtime construction delegates.

## Risks / Trade-offs

- [Risk] Generator and runtime markup parser drift. -> Cover common supported cases in source-gen tests and keep generated surface small.
- [Risk] Diagnostics can be too vague. -> Emit deterministic diagnostics for malformed XML and unsupported elements/properties.
- [Risk] Sourcegen project bloats solution restore. -> Keep Roslyn references limited to the sourcegen/test projects.
- [Risk] Generated code bypasses typed validation. -> Emit public property assignments, not backing field/property-store mutation.
