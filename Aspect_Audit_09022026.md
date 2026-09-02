# Audit și remediere Aspect — 2026-09-02

## Verdict final

**Cele șase defecte confirmate de audit au fost remediate și acoperite cu regresii permanente.** Runtime-ul păstrează acum ownership-ul exclusiv al `AspectBase`, reacționează la dependențele dinamice de `UiProperty`, respectă lifecycle-ul sidecar-urilor locale, expune snapshot-uri stabile și respinge atomic declarațiile incompatibile.

Porțile relevante sunt verzi: cele opt regresii noi, testele Aspect focalizate, suita completă, buildul Release, formatterul limitat la fișierele schimbate, manifestul documentației, benchmarkul determinist, bugetul de cadru și două comparații vizuale exacte.

Nu a fost efectuată validare manuală umană a UI-ului. Aceasta nu este înlocuită de testele automate sau de capturile aplicației.

## Domeniu

Auditul inițial a inspectat:

- contractul din `docs/aspect-system.md` și planul `docs/plans/2026-08-27-unify-aspect-runtime.md`;
- runtime-ul `UI/Aspect/` și integrarea cu `UIElement`, `UIRoot`, property store, invalidation, resources, templates și controale;
- coborârea markup-ului și testele runtime, SourceGen și Language;
- documentația API canonică din `docs-site/documentation/classes/`;
- artefactele istorice de performanță și conformanță.

Auditul inițial a fost read-only. Remedierea ulterioară descrisă în acest raport a modificat producția, testele și documentația.

## Constatări și rezoluții

### A-01 — Rezolvat — ownership multiplu pentru `AspectBase`

**Comportament observat înainte:** după ce o regulă temporară schimba padding-ul unui `TextBox`, eliminarea regulii lăsa `Padding = Thickness.Zero` în loc să restaureze default-ul controlului.

**Cauză confirmată:** controalele și `AspectEngine` scriau în aceeași bandă `AspectBase`; cleanup-ul engine-ului nu putea distinge valorile proprii de default-urile scrise anterior de control.

**Remediere:**

- `UiPropertyStore` deține acum fallback-uri framework per instanță, evaluate în sursa publică existentă `Default`; nu a fost introdusă o bandă publică nouă;
- controalele folosesc `UiObject.SetFrameworkDefault` în loc să scrie direct în `AspectBase`;
- `ItemsControl.ItemContainerAspect` se propagă prin `TemplateBinding`, nu prin `AspectBase`;
- `AspectEngine` rămâne singurul writer al benzii `AspectBase`.

**Probă permanentă:** `RemovingTemporaryRuleRestoresFrameworkAspectDefault` verifică aplicarea regulii, eliminarea ei și restaurarea valorii framework cu sursa `Default`.

### A-02 — Rezolvat — dependențele dinamice de `UiProperty` erau ignorate

**Comportament observat înainte:** o condiție pe `OpacityProperty`, care nu are flag static `AffectsAspect`, captura dependența dar nu programa recomputarea; rezultatul Aspect rămânea stale.

**Cauză confirmată:** graful de dependențe era populat de engine, dar calea root-owned de property mutation nu îl consulta.

**Remediere:**

- `UIRoot` deține un `RootPropertyMutationObserver` comun;
- observerul păstrează notificarea Motion existentă și transmite mutația către `AspectProcessor`;
- procesorul invalidează numai elementul atașat la acel root și numai când valoarea efectivă s-a schimbat și proprietatea apare în dependențele capturate;
- aplicarea și cleanup-ul engine-ului sunt protejate împotriva feedback-ului/reentrancy.

**Probă permanentă:** `RootProcessorRecomputesDynamicPropertyDependencyWithoutStaticAspectFlag` verifică enqueue-ul, recomputarea și valoarea finală.

### A-03 — Rezolvat — lifecycle incorect pentru `ElementAspect.behaviorFactory`

**Comportament observat înainte:** detach-ul nu elimina sidecar-ul, iar reattach-ul nu crea un lifetime nou.

**Cauză confirmată:** sidecar-ul era sincronizat numai când se schimba `AspectProperty`, nu în hook-urile reale de attach/detach.

**Remediere:** `UIElement` atașează behavior-ul local numai cât timp elementul este atașat, îl elimină la detach și îl recreează idempotent la reattach. Replacement și clear folosesc aceiași helperi de lifecycle.

**Probă permanentă:** `ElementAspectBehaviorFollowsAttachDetachAndReattachLifecycle` verifică attach → detach/dispose → reattach și numărul exact de instanțe.

### A-04 — Rezolvat — snapshot-uri publice mutabile

**Comportament observat înainte:** `ResolvedAspect.Values` putea fi convertit la un dictionary mutabil; golirea lui corupea `LastResolved`, iar `AspectEngine.Clear` lăsa valoarea blocată în `AspectBase`.

**Cauză confirmată:** constructorii expuneau colecțiile primite ca `IReadOnly*` fără să le copieze.

**Remediere:**

- `ResolvedAspect` copiază valorile într-un `ReadOnlyDictionary` și listele în snapshot-uri read-only;
- `AspectDependencySet`, `AspectConditionResult` și `AspectDiagnostics.Snapshot` copiază de asemenea toate colecțiile de intrare;
- starea reținută de engine nu mai poate fi alterată prin obiectele publice returnate.

**Probe permanente:** `PublicResolvedValuesCannotCorruptEngineCleanup` și `PublicAspectCollectionsAreStableReadOnlySnapshots` verifică atât protecția la mutare, cât și izolarea față de colecția sursă.

### A-05 — Rezolvat — atribuirea invalidă a `ElementAspect` nu era atomică

**Comportament observat înainte:** atribuirea unui aspect `TargetType = TextBlock` unui `Button` arunca, dar instanța respinsă rămânea în property store și aspectul anterior putea fi deja dezactivat.

**Cauză confirmată:** validarea rula din `OnPropertyChanged`, după commit-ul mutației.

**Remediere:** `UiObject` are acum un hook intern de validare pre-commit, iar `UIElement` validează `AspectProperty` înainte ca store-ul sau lifecycle-ul anterior să fie modificate.

**Probă permanentă:** `RejectedElementAspectAssignmentIsAtomic` verifică atât atribuirea inițială respinsă, cât și replacement-ul valid → invalid.

### A-06 — Rezolvat — proprietăți incompatibile în declarațiile condiționale/code-first

**Comportament observat înainte:** un `ElementAspect` pentru `Button` putea aplica `TextBlock.TextProperty` printr-o condiție; aceeași breșă exista pentru reguli code-first.

**Cauză confirmată:** validarea locală inspecta numai `DefaultValues`, iar engine-ul nu avea gardă defensivă pentru ownerul proprietății câștigătoare.

**Remediere:**

- validarea `ElementAspect` inspectează și fiecare `Condition.Values`;
- `AspectEngine` validează toate valorile rezolvate înainte de prima mutație și respinge `AspectProperty` sau orice proprietate al cărei `OwnerType` nu este compatibil cu elementul țintă;
- eșecul are loc înainte de aplicarea parțială a rezultatului.

**Probe permanente:** `ElementAspectRejectsIncompatibleConditionalProperty` și `EngineRejectsDeclarationIncompatibleWithTargetElement` acoperă căile locală și code-first.

## Schimbări de contract documentate

Au fost sincronizate paginile canonice pentru:

- `ResolvedAspect`, `AspectDependencySet`, `AspectConditionResult` și `AspectDiagnostics.Snapshot` — snapshot-uri defensive;
- `AspectEngine` și `AspectProcessor` — validare înainte de mutație și invalidare dinamică;
- `ElementAspect` și `UIElement` — validare atomică și lifecycle attach/detach;
- `ItemsControl` — propagarea `ItemContainerAspect` prin `TemplateBinding`.

Nu au fost adăugate sau redenumite pagini, deci `docs-site/documentation/manifest.json` nu a necesitat modificare. Testul oficial al manifestului este verde.

## Verificare

### Regresii și suite focalizate

- `AspectAuditRegressionTests`: **8/8 GREEN**;
- controale și templates afectate: **82/82 GREEN**;
- runtime `FullyQualifiedName~Aspect`: **174/174 GREEN**;
- SourceGen `FullyQualifiedName~Aspect`: **43/43 GREEN**;
- Language `FullyQualifiedName~Aspect`: **14/14 GREEN**.

Cele șapte reproduceri inițiale au fost confirmate RED pentru motivul intenționat înaintea modificării producției. A opta regresie acoperă defensiv celelalte DTO-uri read-only identificate de audit.

### Suita completă și build

`dotnet test .\Cerneala.slnx -c Debug --no-restore` a ieșit cu cod 0:

| Proiect | Passed | Skipped | Failed |
| --- | ---: | ---: | ---: |
| `Cerneala.Tests` | 3.167 | 2 | 0 |
| `Cerneala.Tests.SourceGen` | 485 | 0 | 0 |
| `Cerneala.Tests.Language` | 182 | 0 | 0 |
| `Cerneala.Tests.LanguageServer` | 40 | 0 | 0 |
| `Cerneala.Tests.PreviewHost` | 12 | 0 | 0 |
| `Cerneala.Tests.VisualStudio` | 47 | 0 | 0 |
| `Cerneala.Tests.SdlGpu` | 115 | 5 | 0 |
| `Cerneala.Tetris.Tests` | 25 | 0 | 0 |

- `dotnet build .\Cerneala.slnx -c Release --no-restore`: **GREEN**, 0 warnings, 0 errors;
- formatterul limitat la toate fișierele C# modificate: **GREEN**;
- testul `ApiDocumentationManifestIsValidAndReferencesExistingFiles`: **GREEN**;
- comparația strictă ApiCompat între assembly-ul `HEAD` și assembly-ul remediat: **GREEN**, fără schimbări publice/protejate;
- RoslynRepoIndexer force rebuild: **valid**, 3.584 documente, 92.847 simboluri, 377.943 referințe, 0 fișiere dirty; `doctor` este verde, cu cele șapte warnings baseline cunoscute;
- `git diff --check`: **GREEN**; Git raportează numai avertismentele de conversie LF → CRLF ale checkout-ului.

### Performanță

BenchmarkDotNet `ShortRun`, aceeași configurație ca artefactul istoric:

| Scenariu | Mean curent | Allocated/op curent |
| --- | ---: | ---: |
| `CodeFirstCatalogResolve` | 2,877 μs | 4,48 KB |
| `RootRegisteredPackageFrame` | 12,590 μs | 9,55 KB |
| `ElementLocalMutationAndFrame` | 26,611 μs | 18,39 KB |
| `NestedScopeAttachAndFrame` | 241,083 μs | 36,41 KB |

Snapshot-urile defensive cresc explicabil alocările față de rularea istorică în unele scenarii. `ShortRun` are doar trei iterații măsurate, iar pentru această remediere nu a fost definit un prag contractual separat; valorile sunt probe măsurate, nu o afirmație de regresie/improvement statistică.

Contorii deterministici pentru 1.000 de operații/scenariu coincid cu artefactul final istoric:

| Scenariu | Rules | Matched | Conditions | Declarations | Invalidations |
| --- | ---: | ---: | ---: | ---: | ---: |
| code-first | 2.000 | 1.500 | 1.000 | 1.500 | 0 |
| root package | 4.000 | 2.500 | 1.000 | 6.500 | 1.000 |
| nested scope | 3.000 | 2.000 | 0 | 6.000 | 1.000 |
| element-local | 3.000 | 2.000 | 0 | 6.000 | 1.000 |

Presentation frame-budget, 8 cicluri × 45 cadre/capitol, prag warm p99 `16,6667 ms`:

- toate cele șase capitole: **0 cadre warm peste prag**;
- cel mai mare warm p99: **15,406 ms** (`FRAME PIPELINE`);
- Aspect programat: mean **0,001917 ms**, p99 **0,0787 ms**, max **0,1503 ms**;
- 133 elemente Aspect procesate în probele warm;
- alocare medie totală: **545.236 B/cadru**.

### Conformanță vizuală

Capturile au fost create exclusiv prin `Window.SaveScreenshot`. Din cauza DPI 125% și a dimensiunii minime actuale a ferestrei, referințele istorice 1600×900 nu puteau fi comparate fără redimensionare. A fost construit temporar `HEAD` într-un worktree extern și au fost capturate baseline și remediere la aceeași dimensiune SDL3, **1650×1075**. Worktree-ul temporar a fost eliminat.

| Scenariu | Pixeli | Schimbați | MAE | P99 | Max | Rezultat |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Aspect Studio | 1.773.750 | 0 | 0 | 0 | 0 | GREEN |
| Build-Time Markup/templates | 1.773.750 | 0 | 0 | 0 | 0 | GREEN |

## Experimente abandonate și limite

- Prima ipoteză pentru A-01 a mutat default-urile în `DefaultAspectPackage`; suita runtime a expus 134 regresii pentru controale template rootless. Ipoteza a fost abandonată, iar package-ul a fost restaurat fără diff. Soluția finală păstrează comportamentul rootless și ownership-ul engine-ului.
- Prima comparație vizuală cu referințele istorice a fost invalidă din cauza dimensiunilor diferite; nu a fost tratată ca eșec de randare. Comparația validă este cea `HEAD` versus remediere, la aceleași condiții.
- Nu a fost efectuată validare manuală umană. Nu rămâne nicio problemă Aspect cunoscută din cele șase constatate, dar asta nu demonstrează absența oricărui defect posibil în subsistem.
