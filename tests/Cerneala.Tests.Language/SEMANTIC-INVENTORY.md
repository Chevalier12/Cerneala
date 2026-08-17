# Cerneala language semantic inventory

This stage-0 inventory is executable through `Corpus/constructs.json`. Every row records the implementation owner, a valid sample, an invalid sample, and the existing source-generator suite that protects the behavior.

## Owners

| Area | Current owner | Existing evidence |
| --- | --- | --- |
| XML document shape, root and locations | `UiMarkupGenerator.ParseDocument` | `UiMarkupGeneratorTests` |
| Elements, properties, content, events and literal conversion | `UiMarkupGenerator.GenerationScope` | `UiMarkupGeneratorTests`, application tests |
| Binding tokens, interpolation, modes and source resolution | `UiMarkupBindingResolver` | binding stages zero, three, four and five |
| `@template`, `@when`, `@if`, `@default` and assignments | `UiMarkupDirectiveParser` | `UiMarkupGeneratorTests` |
| Aspect resources, target validation and application | `GenerationScope` partials | `UiMarkupGeneratorTests` and presentation tests |
| Motion grammar and resolution | `UiMarkupDirectiveParser`, `UiMarkupMotionSyntax`, `MotionMarkupLanguage` and motion resolvers | all `UiMarkupGeneratorMotion*Tests` suites |
| Prism grammar, catalog and binding | `Prism/Syntax`, `Prism/Catalog` and `Prism/Binding` | `PrismMarkupContractTests` |

## Corpus contract

- `constructs.json` is the versioned valid/invalid semantic matrix.
- `repository-documents.txt` freezes all repository `.crn` files plus the documentation and sourcegen test sources represented by the matrix.
- `sourcegen-diagnostics.json` freezes invalid-corpus diagnostics by id, severity, message and UTF-16 line/character span.
- `LanguagePipelineHarness` runs `Cerneala.Language` syntax first, suppresses the provisional semantic lane below unrecoverable syntax, and runs the current source generator independently for parity snapshots.
