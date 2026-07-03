## Why

Runtime markup loading now exists, but source generation is still deferred. This change prototypes build-time UI markup generation so simple markup files can produce code-first factories and avoid runtime parsing/reflection in hot paths.

## What Changes

- Add `Cerneala.SourceGen` as a Roslyn source generator project.
- Add `UiMarkupGenerator` that reads additional XML markup files and emits generated factory classes.
- Add `tests/Cerneala.Tests.SourceGen` with generator tests that compile generated output against Cerneala runtime APIs.
- Keep source generation optional; applications can still use code-first APIs or runtime markup loading.
- Update `ROADMAPv2.md` section 25 and implementation order when source generation is implemented.

## Capabilities

### New Capabilities

### Modified Capabilities

- `markup-serialization`: Generated factories compile supported markup into retained trees using the same typed property APIs and runtime `GeneratedUiFactory` contract as code-created trees.

## Impact

- Adds `Cerneala.SourceGen/Cerneala.SourceGen.csproj` and `Cerneala.SourceGen/UiMarkupGenerator.cs`.
- Adds `tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj` and `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`.
- Updates `Cerneala.slnx`.
- Uses Roslyn packages already available in the local NuGet cache.
