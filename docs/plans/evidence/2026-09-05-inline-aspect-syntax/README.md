# Inline Aspect: redundant wrapper rejected

This is the user's requested follow-up after the import/debug plan, not a
reopening of that completed plan. The breaking markup change is intentional.

## Contract and owner

`<Type.Aspect>` already declares the inline Aspect. Its defaults, conditions,
Motion and component template belong directly in that property element. A
nested `<Aspect>` or `<Aspect />` is illegal. Named and default
`<Aspect TargetType="...">` resources are unchanged.

The direct form already worked before this change. The shared language model
and generator both also accepted and unwrapped the redundant form. The fix is
owned by `CernealaSemanticModel.Scopes.cs`: it reports `CERNEALAUI005` on the
redundant element's name and always models the property element as the body.
`UiMarkupGenerator.ReadInlineAspects` no longer has an unwrapping branch. The
existing shared-error gate prevents source emission; no parallel generator-only
diagnostic or runtime compatibility path was added.

## RED to GREEN

- `language-valid-red.trx`: three failures because the required diagnostic was
  absent, plus one passing direct-body control.
- `sourcegen-valid-red.trx`: two failures for the same missing diagnostic, plus
  one passing direct-body compilation/runtime control.
- `language-green.trx`: all four pass. Filled, empty and comment-preceded
  wrappers have one exact-span diagnostic shared by Build, Editor and the
  generator adapter. The direct declaration exposes its default, condition,
  Motion event and template-part symbols.
- `sourcegen-green.trx`: all three pass. Invalid documents emit no source;
  the valid direct form compiles, applies its default, emits its Motion trigger
  and updates a live template `$owner.IsEnabled:OneWay` binding at runtime.

The focused commands used the new `InlineAspectRejectsRedundantWrapper*` and
`DirectInlineAspect*` tests in the Language and SourceGen projects, with
`--no-restore --logger trx`. Tests were RED before production changes.

## Migration and documentation

49 wrappers were removed without changing their bodies: 22 in seven SourceGen
test files, ten in Playground/Tetris markup, and 17 in documentation examples.
The test assertions and application content were compared before/after after
removing only wrapper tokens and whitespace. Negative regression strings keep
the forbidden syntax deliberately. Resource declarations are not unwrapped.
Historical plans and captured evidence were not rewritten.

Canonical ElementAspect/generator documentation explains the rejection and
migration. Nine affected canonical control pages, the markup guide and the
Motion language reference use the direct syntax. No canonical page was added
or renamed, so this follow-up requires no manifest entry change. The complete
VisualStudio suite passes `ApiDocumentationManifestIsValidAndReferencesExistingFiles`.
There is no public/protected C# signature change in this follow-up.

## Application and native verification

- `language-suite.trx`: 190 pass.
- `sourcegen-suite.trx`: 518 pass, including named/default resource and migrated
  Motion/Prism/scene regressions.
- `world-runtime.trx`: the actual compiled Scene World effects matrix passes.
- `tetris.trx`: all 29 tests pass after migrating its current-piece Aspect.
- `sdl-build.log`: restored Playground SDL build, zero warnings and errors.
- `native/results.json`: success on
  `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`, 364 observed frames.
  Nine full captures, nine target captures and corresponding state snapshots
  were produced through the existing Window-owned screenshot path.

The same existing bounded Servo scenario verifies closed-door travel -32,
open-door travel -44, fence travel +36, retained NPC identity, pan from 40 to 48
visible chunks, a local mutation rebuilding one batch while reusing 34, and
debug collision/index/picking invariance. Wall-clock animation phases and frame
counts are not claimed pixel-identical to the earlier run.

To rerun the native scenario, build
`Playground/Cerneala.Playground/Cerneala.Playground.csproj` with
`-p:CernealaDesktopBackend=SDL3`, set `CERNEALA_SCENE_WORLD_CAPTURE` to an explicit
output directory and `CERNEALA_CONFORMANCE_CAPTURE=1`, then run its generated
executable. The existing opt-in scenario closes the window. Both environment
variables were removed from the launching process afterward; no probe process
or temporary source remains.

## Complete solution

```powershell
dotnet test .\Cerneala.slnx --logger trx --results-directory docs/plans/evidence/2026-09-05-inline-aspect-syntax/full-suite -v:minimal
git diff --check
```

The solution command restores and rebuilds; it does not use `--no-build`.
All nine test projects have results in `full-suite/`, with their log in
`full-suite.log` and an outcome-based inventory in `suite-summary.json`.

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Core | 3482 | 2 | 2 |
| Scene2D importers | 151 | 0 | 0 |
| Language | 190 | 0 | 0 |
| SourceGen | 518 | 0 | 0 |
| LanguageServer | 40 | 0 | 0 |
| PreviewHost | 13 | 0 | 0 |
| VisualStudio | 47 | 0 | 0 |
| SDL GPU | 121 | 0 | 5 |
| Tetris | 29 | 0 | 0 |
| **Total** | **4591** | **2** | **7** |

The full command exits **1**, not zero. Failure names and messages were compared
against the [pre-change final suite](../2026-09-04-scene-import-stage6/README.md)
and match exactly: the two previously isolated and explicitly waived MonoGame
MSAA `OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent` cases measure
47/255 for content 1 and 25/255 for content 2. No new failure appeared. No failing
assertion or skip was changed to accommodate this syntax change. The seven
opt-in tests were not executed; the separate Scene World SDL run above did run.

`index.json` records the refreshed index after the last C# modification, with
no index warnings/errors. `git diff --check` passes.

## Remaining human validation

Human visual validation is still awaiting confirmation. Run Playground on SDL,
select Scene World, click the player and use arrows/Space; click the door and
exercise Pan/Home, Plant, NPC +, Debug and Tiled/LDtk. Automated captures and
runtime assertions are not a claim that a human performed those interactions.
