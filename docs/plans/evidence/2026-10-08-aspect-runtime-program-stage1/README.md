# Etapa 1 — `$Name` rezolvat în namescope-ul de declarare

Plan: [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md), Etapa 1.

## Schimbare

- `Cerneala.SourceGen/UiMarkupAspectReader.cs`: `AspectResource.DeclaringElement` — elementul care deține secțiunea `Resources` (`scope.Owner`) sau Aspect-ul inline (`owner`). Necesar pentru că `<X.Resources>` și `<X.Aspect>` sunt scoase din arbore după citire, deci `AspectResource.Source` nu mai poate localiza namescope-ul. `CloneAspectForApplication` îl copiază.
- `Cerneala.SourceGen/UiMarkupMotionResolver.cs`: `FindMotionNamedElement(applicationElement, aspect, name)`:
  1. Aspect declarat în alt document (de ex. `App.crn`) → nimic (doar `$self`/`$owner`);
  2. descendent al elementului de aplicare (regula de până acum, păstrată pentru cazurile din template-uri);
  3. altfel, element din același namescope ca locul de declarare (`FindContainingTemplateRoot` egal) — frați ca `$Speaker` devin valizi.
  Folosit de țintele Motion, `@scroll` și `@stagger`.
- Language nu a avut nevoie de schimbare: `CernealaSemanticModel.FindNamedElement(source, name)` caută deja în namescope; într-o resursă din `App.crn` raportează CERNEALAUI021.

## Teste

- `tests/Cerneala.Tests.Language/MotionPrismSemanticTests.cs`: `NamedMotionTargetsResolveInTheDeclaringNameScope` (inline și resursă, frate `$Speaker`) — zero diagnostice semantice **și** zero diagnostice SourceGen prin `LanguagePipelineHarness`; `ApplicationAspectCannotTargetANamedElement` — CERNEALAUI021 în `App.crn`. Paritatea SourceGen pentru `App.crn` nu e acoperită de harness (un singur document); regula SourceGen pentru Aspect-uri din alt document este pasul 1 de mai sus.
- `AspectRuntimeProgramTests.InlineAspectAssignedToAnotherElementKeepsItsNamedReferences`: compilează acum (înainte: CERNEALAUI021 „not available at this Aspect application site”); eșuează la runtime (`Opacity` 1 în loc de 0.25), partea Etapei 2.

## Inventar Etapa 0 — migrare

Cazurile cu `$Name` (`ForwardNamedMotion` → `$Child`, `HostMotion` → `$Target`, `StructureTests` `Interactive` → `$Action`) rezolvă același element ca înainte; nimic de migrat. `CernealaPresentation` (Aspect-uri cu `PipelineRelay`, `MotionStatusText`, `VisualStage`) compilează.

## Verificare

- `dotnet build Cerneala.slnx -c Release -m:1`: 0 erori.
- TRX în acest director: `Cerneala.Tests.Language` 374 / 1 skipped; `Cerneala.Tests.SourceGen` 641; `Cerneala.Tests.Timbre` 370 passed + 5 failed = exact RED-urile de runtime ale Etapei 2 (`ReplacementAtRuntime…`, `ReplacingBackAndForth…`, `OneResourceAppliedAtRuntime…`, `DefaultAspectBrings…`, `InlineAspectAssigned…`).
