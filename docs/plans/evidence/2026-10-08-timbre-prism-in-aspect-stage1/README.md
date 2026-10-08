# Etapa 1 — `PrismComposition` → `PrismClip`

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 1. Baseline: `59c0e257`.

## Schimbare

- Înlocuire mecanică `PrismComposition` → `PrismClip` și `prismCompositions` → `prismClips` în 99 de fișiere (cod, teste, fixture-uri, Playground, gramatica VS + golden, docs, docs-site, manifest, `Tools/PrismAudit`), fără `docs/plans`, rezultate benchmark, `.trx`, `FileTree.md`.
- `git mv`: `UI/Prism/Definitions/PrismClipDefinition.cs`, `docs-site/.../Cerneala.UI.Prism.Definitions.PrismClipDefinition.md`, `docs-site/.../Cerneala.UI.Prism.Runtime.PrismClipState.md`.
- Neatinse: `PrismClipToBelowDiagnostic`, `PrismClippingStyle`, `ClipToBelow`, cuvântul „composition” (de ex. `PrismInstance.Composition`, catalogul `composition/*`).
- `docs/prism-markup-syntax-proposal.md` declara normativ că resursa „nu se numește `PrismClip`” (Clip = clipping). Paragraful consemnează acum redenumirea din 2026-10-08 și motivul (familia `MotionClip`/`PrismClip`/`TimbreClip`); utilizatorul a decis știind de ambiguitatea cu clipping.
- `docs/prism-completeness-report.generated.md` regenerat cu `dotnet run --project Tools/PrismAudit/PrismAudit.csproj -c Release -- --write`.

## Verificare

- `git grep -E 'PrismComposition|prismComposition'` (fără `docs/plans`, rezultate benchmark, `.trx`, `FileTree.md`, `.dll`): doar paragraful istoric din `docs/prism-markup-syntax-proposal.md`.
- `dotnet build Cerneala.slnx -c Release -m:1`: 0 erori.
- `PrismAudit -- --check`: „178 catalog entries, 31 common properties, 216 public Prism types and 8 extended public types, zero gaps”, exit 0.
- Release, `--no-build`: `Cerneala.Tests` 4198 passed / 2 skipped; `Cerneala.Tests.Language` 371 / 1 skipped; `Cerneala.Tests.SourceGen` 641; `Cerneala.Tests.VisualStudio` 48; `Cerneala.Tests.SdlGpu` 601 / 241 skipped (nativ dezactivat; skip-uri preexistente).
- `Cerneala.Tests.LanguageServer`: prima rulare 44/45 (un eșec, test neidentificat — rularea nu avea logger TRX) în timp ce în paralel rulau build-ul worktree-ului baseline și ApiCompat; testele au bugete fixe de timp (`firstCompletionBudget` 5 s, timeout-uri 10–90 s) și durate de minute. Rerulare izolată: 45/45 (`ls-stage1.trx`). Clasificare: eșec intermitent sub încărcare, cauză neprobată; nu apare în rularea izolată.
- Link-uri și fișierele din `docs-site/documentation/manifest.json`: toate există.

## ApiCompat (strict, fără suppression-uri)

`api-compat.proj` față de worktree-ul `C:\Users\lauri\Desktop\Cerneala-baseline-aspect-59c0e25` (`59c0e257`). Compatibil și strict: exit 1, numai redenumiri (`api-compat-strict.log`):

- CP0001: `PrismCompositionDefinition` → `PrismClipDefinition`; `PrismCompositionState` → `PrismClipState`.
- CP0002: membrii care le folosesc în semnătură — `PrismDrawScope.Definition`, `PrismInstance.Definition`, `PrismInstance.Composition`, constructorul `PrismInstance(PrismClipDefinition)`, `PrismInstance.ReplaceDefinition(PrismClipDefinition)`.

Nicio altă adăugare, eliminare sau modificare.
