# Aspect — checklist fisiere C#

61 fisiere din inventarul implementarii dedicate sistemului Aspect: 53 in
`UI/Aspect` si opt fisiere de integrare cu template-uri, diagnostice si invalidare.

Include puntea Aspect–Motion (`AspectMotion.cs`), deja prezenta si in MotionChecklist.
Nu include teste, exemple, benchmark-uri, documentatie sau fisiere generale
care integreaza Aspect alaturi de alte sisteme (inclusiv Language/SourceGen).
`UI/Text/TextAspect.cs` este un descriptor de text, nu implementarea sistemului Aspect.

Status: finalizat. Goal persistent creat la 2026-10-01; 61/61 fisiere rezolvate,
acceptare automata Windows verificata. Limitele si skips sunt consemnate mai jos.
O bifa va insemna fisier inspectat prin repo-cleanup, cleanup justificat aplicat
sau pastrare deliberata, apoi verificarea aplicabila trecuta. Nu inseamna ca
fiecare fisier trebuie modificat. Suita completa ramane pentru finalul intregului
target, conform acordului anterior. Inventarul si jurnalul urmaresc cleanup-ul;
ordinea loturilor nu prescrie modificari fara dovezi.

## Ordinea executiei

Loturile sunt etape atomice; fiecare target apare o singura data in inventar.
Nu se incepe implementarea lotului urmator inainte de verificarea si checkpoint-ul
lotului curent. Pastrarea deliberata este un rezultat valid, nu o cota de editari.

1. Conditii si dependinte: AspectCondition, AspectConditionDependency,
   AspectConditionKey, AspectConditionNode, AspectConditionResult, AspectDataContext,
   AspectDataDependency, AspectDependencySet, AspectMatchContext (9 fisiere).
2. Token-uri si valori: AspectEnvironment, AspectToken, AspectToken{T},
   AspectTokenBuilder, AspectTokenDefinition, AspectValue, AspectValue{T},
   AspectResolutionContext, ThemeTokenBridge, DefaultAspectTokens (10 fisiere).
3. Reguli si selectie: AspectDeclaration, AspectLayer, AspectMotion, AspectOrigin,
   AspectRef, AspectRuleSet, AspectRuleSetBuilder, AspectSpecificity, AspectState,
   AspectStateSet, AspectTarget, AspectVariantKey, AspectVariantKey{TOwner,TValue},
   AspectVariantSet (14 fisiere).
4. Pachete si compozitie: AspectBehavior, AspectCatalog, AspectPackage,
   AspectPackageBuilder, AspectRegistry, DefaultAspectPackage,
   ComponentAspectBuilder, ContentTemplateBuilder (8 fisiere).
5. Runtime si invalidare: AspectEngine, AspectEngineElementState,
   AspectInvalidation, AspectInvalidationGraph, AspectProcessor (5 fisiere).
6. Slot-uri si rezultate: AspectSlot, AspectSlot{TOwner,TTarget}, AspectSlotPath,
   ElementAspect, RejectedAspectDeclaration, ResolvedAspect, ResolvedAspectValue,
   TemplateAspectContext (8 fisiere).
7. Diagnostice si coada: cele sase fisiere UI/Detective din inventar si
   AspectQueue (7 fisiere).
8. Acceptare finala: build, suita completa, gate-uri aplicabile si review final.

Gate comun fiecarui lot: inspectie completa a target-urilor si caller/test/docs
relevante; baseline/characterization si GREEN pentru refactor-uri aplicate;
verificari focalizate/afectate relevante, fara suita completa repetata;
review parinte al sursei/diff-ului/dovezilor; jurnal si bife sincronizate imediat.
Nu se schimba API/semantica/ownership, praguri sau goldens pentru cleanup.
Orice astfel de schimbare necesita decizie separata. Gate-urile inapplicabile
se justifica in jurnal; gate indisponibil nu este GREEN.

## C:\Users\lauri\Desktop\Cerneala\UI\Aspect
- [x] AspectBehavior.cs
- [x] AspectCatalog.cs
- [x] AspectCondition.cs
- [x] AspectConditionDependency.cs
- [x] AspectConditionKey.cs
- [x] AspectConditionNode.cs
- [x] AspectConditionResult.cs
- [x] AspectDataContext.cs
- [x] AspectDataDependency.cs
- [x] AspectDeclaration.cs
- [x] AspectDependencySet.cs
- [x] AspectEngine.cs
- [x] AspectEngineElementState.cs
- [x] AspectEnvironment.cs
- [x] AspectInvalidation.cs
- [x] AspectInvalidationGraph.cs
- [x] AspectLayer.cs
- [x] AspectMatchContext.cs
- [x] AspectMotion.cs
- [x] AspectOrigin.cs
- [x] AspectPackage.cs
- [x] AspectPackageBuilder.cs
- [x] AspectProcessor.cs
- [x] AspectRef.cs
- [x] AspectRegistry.cs
- [x] AspectResolutionContext.cs
- [x] AspectRuleSet.cs
- [x] AspectRuleSetBuilder.cs
- [x] AspectSlot.cs
- [x] AspectSlot{TOwner,TTarget}.cs
- [x] AspectSlotPath.cs
- [x] AspectSpecificity.cs
- [x] AspectState.cs
- [x] AspectStateSet.cs
- [x] AspectTarget.cs
- [x] AspectToken.cs
- [x] AspectToken{T}.cs
- [x] AspectTokenBuilder.cs
- [x] AspectTokenDefinition.cs
- [x] AspectValue.cs
- [x] AspectValue{T}.cs
- [x] AspectVariantKey.cs
- [x] AspectVariantKey{TOwner,TValue}.cs
- [x] AspectVariantSet.cs
- [x] ComponentAspectBuilder.cs
- [x] ContentTemplateBuilder.cs
- [x] DefaultAspectPackage.cs
- [x] DefaultAspectTokens.cs
- [x] ElementAspect.cs
- [x] RejectedAspectDeclaration.cs
- [x] ResolvedAspect.cs
- [x] ResolvedAspectValue.cs
- [x] ThemeTokenBridge.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Controls\Templates
- [x] TemplateAspectContext.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Detective
- [x] AspectConditionTrace.cs
- [x] AspectDiagnostics.cs
- [x] AspectEngineCounters.cs
- [x] AspectResolutionStep.cs
- [x] AspectTokenTrace.cs
- [x] AspectTrace.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Invalidation
- [x] AspectQueue.cs

## Jurnal cleanup

### Lot 1 — conditii si dependinte (2026-10-01)

- Scope: cele noua target-uri din etapa 1, citite complet de parinte; 9/61
  rezolvate. Pastrate deliberat fara modificari C#: toate campurile/helper-ele
  private inspectate au utilizari. API-urile publice fara cititori in repo,
  stamp-urile de versiune, enum-ul Token si proiectiile contextului nu sunt
  cod privat mort si nu sunt eliminate. Luna a confirmat mapping-ul read-only
  si a precizat limitele cautarilor; verdictul si verificarea apartin parintelui.
- Contracte pastrate: All/Any evalueaza toti copiii in ordine, retin rezultatele
  si dependintele, apoi agregheaza Matches diferit. Un nou layer pentru cele
  doua secvente scurte nu are beneficiu de mentenanta demonstrat. Nu se schimba
  evaluarea eager in short-circuit, exception order, specificity sau snapshots.
  Data condition deja foloseste baza comuna; guard-urile de nume au mesaje
  distincte. Contextul public ramane disponibil pentru predicate externe.
- Parintele a inspectat engine resolution/dependency projection, raw declaration
  selection, invalidation consumers, template data si emiterea SourceGen a
  key-urilor/callback-urilor SetActive. Raw selection si runtime resolution au
  rezultate/ownership diferite; nu se unifica superficial. Weak ownership si
  invalidarea condition key doar la schimbare raman intacte.
- Gate: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-restore --filter 'FullyQualifiedName~Cerneala.Tests.UI.Aspect'
  --logger 'trx;LogFileName=conditions-baseline.trx' --results-directory
  artifacts/aspect-cleanup/conditions`: rebuild, exit0, 112 passed, 0 failed,
  0 skipped. Parintele a parsat TRX si confruntat toate cele 112 outcome-uri.
  Include 15 RuleSet cases, typed data/dependencies, 20 unification cases,
  snapshot immutability, dynamic property invalidation si existente stress budgets.
  Nu este suita completa de repository. Niciun C# modificat, deci aceasta
  verificare ramane gate curent; nu se inventeaza RED pentru refactor absent.
- Review: sursa curenta, callers, testele relevante si contractele canonice
  Condition/ConditionKey/All/Any confruntate, git diff --check trecut. Fara
  API/docs/renderer schimbat, generator output ori pretentii performance noi.
  Nu s-a gasit test runtime direct SetActive; generarea sursei nu dovedeste
  executia callback-urilor. Stimulusul testelor este direct pe proprietati,
  nu input nativ/hit-test/routing. Afirmatiile de reflectie dinamica/external
  consumer coverage nu sunt revendicate. Discrepanta de lista Derived din
  pagina interna AspectConditionNode este datorie docs separata, necorectata aici.
- Niciun job/explorator propriu activ. Cerneala.slnx, hunks-urile straine
  FileTree, InvestorReel, SolarSystem si raportul Markup raman user-owned.
  Suita completa si acceptarea finala raman deschise; fara commit/push.

### Lot 2 — token-uri, valori si mediu (2026-10-01)

- Scope: cele zece target-uri din etapa 2 citite complet de parinte; 19/61
  rezolvate. C# pastrat deliberat: private fields/helpers au consumatori;
  token-uri publice fara built-in rule consumer nu sunt cod privat mort.
  Luna a confirmat bounded source/caller/test/docs mapping, fara verificari.
- Contracte pastrate: identitatea token-ului este nume ordinal plus tip;
  typed/untyped Set au validari diferite, incrementeaza Version si notifica
  in aceeasi ordine. Child fallback, weak references, local overrides si
  snapshot-ul children pentru notificari raman neschimbate. Pruning-ul la
  registration si colectarea live pentru notificare au costuri/roluri diferite;
  nu se unifica printr-un helper cu alocari in plus. Literal/token/computed
  values si dependency copy raman cu aceleasi null/exception semantics.
- Parintele a inspectat processor default-resolution/theme projection/
  ReplaceWith, TemplateTokenBinding attach/detach, tests Token/Bridge/Breaker,
  default package/environment equivalence si contractele canonice. Nu se
  schimba ownership thread, theme mapping ori politica default-value.
- Reparatie docs canonică focalizata: DefaultAspectTokens.md foloseste
  declarationOrder (nu inexistentul priority) si descrie token-urile reale
  ale button.base/border.base, conform DefaultAspectPackage.Create. Runtime
  si API neschimbate. Skill writing-api-documentation si referinta completa
  citite; destinatia docs-site are prioritate fata de path-ul vechi din skill.
  Pagina completa, Definition/Examples/Remarks/tables si manifest entry
  unic/existent verificate; fara rename/pagina noua, manifest nemodificat.
- Gate reutilizat: conditions-baseline.trx, 112 passed/0 failed/0 skipped,
  inclusiv Token5, Bridge3, Breaker4 si DefaultPackage6. Niciun input de
  productie modificat. Gate suplimentar: dotnet test acelasi proiect core
  -c Release --no-build --no-restore --filter
  'FullyQualifiedName~UiThreadAffinityTests' --logger
  'trx;LogFileName=tokens-thread-affinity.trx' --results-directory
  artifacts/aspect-cleanup/tokens: exit0, 8 passed/0 failed/0 skipped;
  TRX parsat. Acopera respingerea worker mutation inainte de schimbarea state.
- Cele trei exemple din pagina editata compilate ca metode independente cu
  imports reunite, csc SDK10.0.400, /target:library /nullable:enable,
  ref-pack net8.0/8.0.30 si Cerneala Release curent: exit0. Nu executate.
  Prima extractie a inclus type definition in numar si a fost oprita inainte
  de scriere/compilare. Prima invocare csc a avut /out: gol din array grouping
  PowerShell (CS2005), nu o eroare a exemplului; argumentele parenthesized au
  reparat fixture-ul. Logul pastreaza CS2005 si COMPILE_EXIT=0. Fisierele .cs/
  .dll temporare unice eliminate exact, in afara globs de productie.
- Review parinte: sursa actuala, diff-ul celor doua linii docs, semnaturile,
  TRX/compiler output si manifest confruntate; git diff --check trecut.
  Nu s-a gasit test direct Computed in cautarea bounded, API-ul nu este editat.
  Fara renderer/API edit, gate pixel nou sau masuratori/pretentii performance.
  Niciun job/explorator activ; suita completa numai la final; fara commit/push.

### Lot 3 — reguli, cascade, stari si variante (2026-10-01)

- Scope: toate cele 14 target-uri din etapa 3 citite complet de parinte;
  33/61 rezolvate. C# pastrat: private state/helpers active, public aliases
  (AspectRef.To/Token.Ref) si Checked/Expanded nu sunt eliminate pentru lipsa
  apelurilor locale. Luna a confirmat bounded declaration/caller/test facts.
- Contracte pastrate: layer ordering difera de layer equality; key identity
  include nume ordinal/owner/value type; StateSet are hash/to-string ordonate,
  VariantSet hash order-independent prin XOR. Cascade compara layer/source/
  specificity/declaration, iar egalitatea completa nu inlocuieste primul winner.
  Nu se introduc arrays/comparers generice pentru comparatii lexicografice mici.
  Cele doua Set overload-uri au validari distincte; cele cateva linii copy-on-write
  comune nu justifica un nou layer. Constructor guards, builder snapshots si
  atribuirea catalog-owned origin/scope raman intacte.
- Parintele a urmarit engine winner path, WithOrigin consumer in catalog,
  source-order cascade prin root/application/scopes/element si Control.SetAspectVariant.
  Tests StateSet3/Variant4/RuleSet15/Engine15/Unification20 din gate112 trecute
  si relevante; reutilizate fara rerun, intrucat niciun C# nu s-a schimbat.
  Include hash insertion-order si scope fallback/idle. Input este property-based,
  nu o dovada noua de routing/focus/hit testing prin input real.
- Corectata pagina canonica AspectVariantSet: importul real Controls.Buttons
  in cele doua exemple si descrierea actuala XOR a hash-ului, nu sorting dupa
  key text. Workflow docs aplicat; pagina completa/structura/manifest entry
  unic verificate. API/runtime/manifest neschimbate. Doua exemple compilate
  ca metode separate, csc SDK10.0.400, ref-pack net8.0/8.0.30 si Cerneala Release
  curent: exit0, fara executie; log artifacts/aspect-cleanup/rules/doc-examples-compile.log.
  Fisierele .cs/.dll unice curatate exact din artifacts, fara production globs.
- Review parinte al sursei curente, docs diff, callers/tests, log/manifest;
  git diff --check trecut. Teste directe pentru toate coordonatele specificity,
  custom layers cu acelasi Order ori API-ul AspectRef nu au fost gasite in
  cautarea bounded; contractele lor nu se modifica. Fara backend/render edit,
  gate vizual nou ori pretentii performance. Niciun job/explorator activ;
  suita completa numai la final; fara commit/push.

### Lot 4 — pachete si compozitie (2026-10-01)

- Scope: opt target-uri din etapa 4 citite complet; 41/61 rezolvate. Pastrate
  fara editari C#/docs: storage si helper-ele private au consumatori concreti.
  Parintele a confruntat bounded mapping-ul Luna cu source/callers/tests actuale.
- PackageBuilder detine liste mutabile; Build foloseste Snapshot cu respingerea
  null entries. CatalogAccumulator detine materializarea, proiecteaza rule origin
  fara mutarea source rule si pastreaza registration/scope order. Dictionary index
  pentru identitate si indexul nume/tip au roluri distincte, nu sunt duplicate.
  Registry detine live read-only package view, Version/callback si cached snapshot.
  Nu se transfera aceste invariants intr-un layer generic si nu se elimina cache.
- Default package foloseste deja AddTokens comun cu doi adaptoare typed; testul
  echivalentei verifica fiecare default. Builder-ele publice scurte au roluri
  component/content/token diferite; nu se unifica API-urile pentru o linie Add.
  Behavior lifetime ramane al processorului, nu mutat in catalog/package.
- Gate curent reutilizat: conditions-baseline.trx 112/112 passed, fara fail/skip;
  Package7, RootRegistry2, DefaultPackage6, snapshots/projection/behavior lifecycle
  in Unification20. Thread-affinity8 passed din lotul 2 ramane curent. Nicio
  productie modificata, fara RED/characterization nou sau rebuild repetat.
  Commands si raw TRX sunt in loturile 1/2; reproducere focalizata este acelasi
  core Release filter 'FullyQualifiedName~Cerneala.Tests.UI.Aspect'.
- Review parinte: complete targets, root registry callback/construction,
  processor scope/behavior consumers, teste relevante si pagini canonice
  Catalog/Registry/DefaultPackage confruntate; git diff --check trecut.
  Fara API/generator/render edit sau nou gate vizual/performance. Nu revendica
  audit exhaustiv al codului shared ori al reflectiei externe. Niciun job/
  explorator activ; acceptarea/suita completa numai la final; fara commit/push.

### Lot 5 — runtime si invalidare (2026-10-01)

- Scope: cinci target-uri citite complet; 46/61 rezolvate. Eliminata numai
  proprietatea interna AspectProcessor.Environment, fara consumatori in sursele
  repo inspectate, inclusiv friend projects. Control/ContentPresenter folosesc
  GetEnvironment(element); SourceGen nu mentioneaza AspectProcessor, iar testul
  reflection inspectat enumera membri publici, nu aceasta proprietate interna.
  Luna a raportat aceleasi fapte si limite; parintele a verificat independent.
  Nu revendica absenta reflectiei construite dinamic/externe. API public intact.
- Celelalte patru fisiere pastrate: Engine detine resolution/cascade/dependency
  tracking si snapshot diagnostics; state fields sunt folosite. Processor si
  standalone Invalidation au subscription/trigger ownership diferit; guard-urile
  reentrante pastreaza valoarea anterioara in finally. Cache identity/version,
  weak ownership, behavior attach rollback/reuse/disposal raman neschimbate.
  Nu se unifica invariants diferite sau optimizeaza alocari fara masuratori.
- Baseline: 112 Aspect si 8 thread-affinity passed, consemnate in loturile 1/2.
  GREEN cu rebuild: dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj
  -c Release --no-restore --filter 'FullyQualifiedName~Cerneala.Tests.UI.Aspect'
  --logger 'trx;LogFileName=runtime-focused.trx' --results-directory
  artifacts/aspect-cleanup/runtime: exit0, 112 passed/0 failed/0 skipped.
  Numele celor 112 teste corespund baseline-ului. Gate afectat pe acelasi proiect
  -c Release --no-build --no-restore, filter
  'FullyQualifiedName~Cerneala.Tests.UI.Invalidation|FullyQualifiedName~Cerneala.Tests.UI.Detective|FullyQualifiedName~UiThreadAffinityTests|FullyQualifiedName~ModernAspectArchitectureTests|FullyQualifiedName~Template':
  runtime-affected.trx, exit0, 272 passed/0 failed/0 skipped. TRX counters si
  outcome-uri individuale confruntate. Nicio asteptare schimbata; nu este bug fix.
- Review parinte al actual source/callers/test/docs, diff doua linii si raw
  evidence; git diff --check trecut. Fara API/docs/generator/render semantics
  schimbate sau pretentii performance noi; gate-uri vizuale noi inapplicabile.
  Doua citiri auxiliare au avut First numeric scris gresit; corectate, nu failures
  de produs. Niciun job/explorator activ; full suite si acceptarea raman la final.

### Lot 6 — slot-uri, rezultate si ElementAspect (2026-10-01)

- Opt target-uri citite complet, pastrate deliberat; 54/61 rezolvate. Slot
  identity este nume ordinal/owner/target, iar matching valideaza toate acestea.
  Weak registration apartine TemplateAspectContext; ComponentTemplateInstance
  o ataseaza/detaseaza, Processor transmite owner/path. Nu se muta ownership.
- ElementAspect detine defaults mutabile cu read-only view, conditions snapshots,
  weak consumers si package/version. SetValue valideaza toti consumatorii inainte
  de editare, apoi reconstruieste package si invalideaza; UIElement detine behavior
  lifecycle. AttachBehavior are apel concret; nu este cod mort. Public ConditionKeys,
  DynamicValue, MatchedRules si WinningDeclaration raman chiar daca bounded scan
  nu gaseste cititori locali. Public extensibility nu se reduce pentru cleanup.
- Rezultatele copiaza dictionary/list inputs, conserva reference identity si
  metadata cascade/Motion. DTO-urile scurte nu justifica noi abstracții/helper-e.
  Luna facts confruntate direct de parinte cu consumers/targets/lifetime/tests.
- Gate reutilizat: runtime-focused112 si runtime-affected272 passed, 0fail/skip,
  din lotul 5, productie neschimbata. Acopera slot validation/dependency/template
  replacement, shared consumers/reattach, atomic rejection si read-only results.
  Gate suplimentar dotnet test proiect core -c Release --no-build --no-restore
  --filter 'FullyQualifiedName~ElementAspectTests' --logger
  'trx;LogFileName=slots-element.trx' --results-directory artifacts/aspect-cleanup/slots:
  exit0, 5 passed/0failed/0skipped; counters/outcomes TRX inspectate.
- Review parinte source/callers/tests si contracte canonice; fara C#/API/docs edit,
  RED inventat, gate vizual nou sau claims performance. git diff --check trecut.
  Datorie docs separata: ElementAspect.DefaultValues este live read-only view,
  nu snapshot imuabil dupa SetValue; AspectSlot examples au import Buttons lipsa.
  Nu sunt schimbari runtime. O cale de test presupusa initial inexistenta a fost
  corectata dupa rg --files; testul real este tests/Cerneala.Tests/Controls.
  Niciun job/explorator activ; acceptarea finala ramane deschisa.

### Lot 7 — diagnostice si coada (2026-10-01)

- Ultimele sapte target-uri citite complet; 61/61 rezolvate. Pastrate deliberat:
  helper-ele trace au utilizari recursive/directe, DTO snapshots pastreaza
  contractele proprii, counter copy este explicit. CacheHits/CacheMisses publice
  nu sunt eliminate pentru lipsa incrementarii; docs descriu aceasta limita.
  AspectTraceSnapshot pastreaza lista furnizata, conform contractului canonic;
  nu se transforma unilateral intr-un nou contract immutable.
- AspectQueue foloseste deja owner-ul comun ElementWorkQueue pentru identity,
  dedup/order/pruning. Nu se dubleaza implementarea sau adauga abstractions.
  Scheduler elimina intrarea si curata flags inainte de callback pentru requeue,
  restaureaza pe exception; DirtyPropagation/lifecycle apartin owner-ilor actuali.
  Luna facts confruntate de parinte cu complete source/callers/testele inspectate.
- Gate curent reutilizat din lotul 5: runtime-focused112 si runtime-affected272
  passed, 0failed/0skipped, cu tests Detective/Invalidation. Include trace winner,
  rejection/token/slot/variant/dependencies, snapshot read-only, queue order,
  requeue pe failure/nested invalidation si detach. Niciun input compiled schimbat.
  Fara RED inventat sau rerun de reasigurare.
- Review parinte source/docs/test/evidence si git diff --check trecut. Datorie
  docs separata: AspectQueue descrie clear flags dupa callback, desi scheduler-ul
  le curata inainte si restaureaza pe failure. Nu este schimbare runtime aici.
  Fara API/generator/render edit ori noi performance claims; full suite finala
  obligatorie urmeaza. Niciun job/explorator activ; fara commit/push.

### Acceptare finala — repository complet (2026-10-01)

- 61/61 target-uri rezolvate in sapte loturi. Un fisier productie are cleanup
  intern justificat (AspectProcessor.Environment eliminata), 60 pastrate deliberat.
  Doua pagini API canonice corectate; cinci exemple compilate, nu executate.
  Niciun API public/protected, ownership, algoritm, prag, golden sau expectation
  schimbat. Nu este audit exhaustiv al tuturor bug-urilor ori documentatiei Aspect.
- Windows, SDK10.0.400, Release. Tools/scripts/Cerneala.BuildInputs.Tests.ps1
  trecut: legitimate inputs pastrate, archived inputs=0 in cele patru categorii;
  fixture unic eliminat prin finally cu verificarea path-ului.
  Primul dotnet build Cerneala.slnx -c Release --no-restore a esuat cu zece
  syntax errors pentru string-uri neinchise in proiectul strain SolarSystem,
  MainWindow.Automation.cs:55-60. Sursa s-a schimbat extern in timpul build-ului;
  parintele a confruntat sursa actuala si timestamp-ul, fara sa o editeze.
  Rebuild justificat pe starea actuala: exit0, zero warnings/erori. Logurile
  build.log si build-current.log pastreaza ambele rezultate. InvestorReel CS0108
  a fost warning in primul run, nu suprimat; build incremental final are zero.
- Suita completa rulata o singura data, dupa inventar si build curent:
  dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1 --logger trx
  --results-directory artifacts/aspect-cleanup/final-repository/test-results:
  exit0. CERNEALA_SDL_NATIVE_TESTS=1; CERNEALA_SDL_CONFORMANCE_ARTIFACTS setat
  la path absolut artifacts/aspect-cleanup/final-repository/full-suite-captures.
  Nu se revendica artefacte pixel in acel path, care nu a fost creat de suite.
  Nu s-a repetat suita sau modificat proiectul strain pentru a obtine GREEN.

  | Proiect | Passed | Failed | Skipped |
  | --- | ---: | ---: | ---: |
  | Cerneala.Tests.Language | 259 | 0 | 1 |
  | Cerneala.Tests.LanguageServer | 40 | 0 | 0 |
  | Cerneala.Tests.PreviewHost | 17 | 0 | 0 |
  | Cerneala.Tests.Scene2DImporters | 173 | 0 | 0 |
  | Cerneala.Tests.Scene2DPackages | 104 | 0 | 0 |
  | Cerneala.Tests.SceneVillage | 43 | 0 | 1 |
  | Cerneala.Tests.SdlGpu | 961 | 0 | 5 |
  | Cerneala.Tests.SourceGen | 614 | 0 | 0 |
  | Cerneala.Tests.VisualStudio | 47 | 0 | 0 |
  | Cerneala.Tests | 4258 | 0 | 0 |
  | Cerneala.Tetris.Tests | 31 | 0 | 0 |
  | Total, 6554 cazuri / 11 proiecte | 6547 | 0 | 7 |

- Parintele a parsat toate TRX/outcome-urile: 6547 Passed, sapte NotExecuted,
  niciun alt outcome. Toate runtime-focused112, runtime-affected272 si
  slots-element5 se regasesc Passed in final. Rezumatul JSON foloseste outcome-uri
  individuale pentru skips, nu counters.notExecuted (acesta era zero in aceste TRX).
  Native SDL961 trecut; nicio reparatie Aspect necesara dupa full suite.
- Sapte skips existente, nu teste trecute: alpha occlusion content1/3/6/7
  dezactivate prin cererea anterioara a userului (driver cause neconfirmata);
  native foreground/input/lifetime si Language completion CPU P95 dezactivate
  de maintainer; Village cadence este opt-in de masina de referinta neactivat.
  Sursele skip attributes confruntate; nu s-au modificat aceste excluderi.
- Review parinte final: sursa actuala/diff-ul propriu/callers/tests/jurnal si raw
  results confruntate; git diff --check trecut. Manifest JSON valid, intrari
  existente/unice si Definition/Examples/Remarks pentru cele doua pagini editate,
  fara placeholders; manifest nemodificat, fara pagini noi/renames. Temporarele
  .cs/.dll nu mai exista in artifacts/aspect-cleanup. FileTree regenerat si
  hunks inspectate: inventarul include acum si celelalte fisiere SolarSystem
  create extern. Modificarile straine raman intacte; fara staging/commit/push.
- Toate exploratoarele si job-urile proprii terminale. Logs/TRX/JSON ignorate de
  Git in artifacts/aspect-cleanup/final-repository. Erori ale comenzilor auxiliare
  de afisare (First/Tail numeric) corectate prin citirea rezultatelor brute, nu
  product failures sau gate-uri mascate. Nu revendica validare umana, alte
  platforme/RID, masuratori performance noi sau pixel parity exhaustiv. Datoriile
  docs separate din loturile 1/6/7 raman consemnate, fara redesign runtime.

### A doua trecere — cleanup nou (2026-10-01)

Aceasta este o inspectie noua, ceruta explicit dupa prima trecere, nu reutilizarea
bifelor 61/61 ca dovada a unui nou audit exhaustiv. Acceptarea de mai sus apartine
primei treceri; a doua trecere este finalizata cu acceptare automata Windows.

- Scope inspectat: engine/processor, invalidare, ElementAspect, contexte/slot-uri,
  rezultate, condition nodes, environment, registry/catalog, token/value paths,
  consumers de template/UIElement, scheduler si testele/documentatia relevante.
  Luna a explorat read-only authoring/builders/packages/conditions/dependencies
  si a raportat referinte exacte si limite; parintele a confruntat candidatii cu
  sursa actuala. Nu se revendica reinspectarea exhaustiva a tuturor celor 61 de
  fisiere, backend-urilor, generatorului sau consumatorilor externi/reflection.
- Cleanup runtime: eliminat campul privat ElementAspect.conditions care pastra
  aceeasi referinta ca proprietatea publica get-only Conditions. Constructorul,
  ConditionKeys, IsConditional si rebuilding-ul package folosesc un singur snapshot.
  Nu sunt schimbate API public/protected, exception order, collection identity,
  defaults mutabile, cascade, condition order, behavior sau lifetime ownership.
- Doua characterization tests permanente au trecut pe implementarea nemodificata,
  apoi din nou dupa cleanup: default view ramane live si read-only, conditional
  snapshots supravietuiesc reconstruirii package, Signal active/inactive aplica
  valorile corecte; scheduler callback vede flag-ul Aspect deja curatat si poate
  reprograma acelasi element pentru frame-ul urmator. Nu este bug fix si nu se
  inventeaza RED. Stimuli sunt public property/key/callback APIs, nu input nativ.
- Corectate trei pagini API canonice: ElementAspect.DefaultValues live view,
  derived hierarchy/signal facts in AspectConditionNode, clear-before-callback
  si exception restoration in AspectQueue. Workflow writing-api-documentation
  complet aplicat; manifest entries existente/unice, fara rename/pagini noi.
  Datoria anterioara privind importuri lipsa in AspectSlot era incorecta:
  Button este in Cerneala.UI.Controls, iar exemplele importa deja acel namespace.
  Pagina AspectSlot ramane nemodificata.
- Candidati pastrati deliberat: All/Any au reducer/diagnostic diferite; un nou
  layer pentru cele doua secvente mici nu are beneficiu proportional. Typed si
  untyped Set au validari distincte; snapshot helpers au null policies diferite;
  FromPackages si Compose au seed/source contracts diferite. Culorile default din
  Theme si Aspect nu sunt legate printr-un owner comun fara contract aprobat.
  AspectRef alias si Token dependency payload sunt public extensibility, nu cod
  privat mort. Computed dependencies sunt un copied array documentat; schimbarea
  contractului de expunere nu este cleanup behavior-preserving.
- Dovezi noi sub artifacts/aspect-cleanup-pass2 (ignorate de Git):
  baseline.trx 131 passed; characterization.trx 2 passed inainte de productie;
  focused-green.trx 133 passed; affected-green.trx 273 passed, toate 0 fail/skip.
  Focused: dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-restore --filter 'FullyQualifiedName~Cerneala.Tests.UI.Aspect|FullyQualifiedName~ElementAspectTests|FullyQualifiedName~UiFrameSchedulerTests|FullyQualifiedName~ModernAspectTraceTests'.
  Affected: acelasi proiect -c Release --no-build --no-restore, filter
  'FullyQualifiedName~Cerneala.Tests.UI.Invalidation|FullyQualifiedName~Cerneala.Tests.UI.Detective|FullyQualifiedName~UiThreadAffinityTests|FullyQualifiedName~ModernAspectArchitectureTests|FullyQualifiedName~Template'.
  Fiecare TRX/outcome a fost parsat de parinte, nu numai consola rezumata.
- BuildInputs.Tests trecut: archived inputs=0 in toate cele patru categorii.
  dotnet build Cerneala.slnx -c Release --no-restore -m:1 trecut cu zero erori,
  un warning CS0108 existent in generated InvestorReel InkScene.Drop (markup
  InkScene.crn:72); nu a fost suprimat ori reparat in afara scope-ului.
- Cele trei exemple C# din paginile editate compilate, nu executate, cu csc
  SDK10.0.400/ref-pack net8.0/8.0.30 si Cerneala Release curent: exit0, fara
  diagnostics. Prima compilare pe ref-pack net10 a dat CS1701; rezultatul este
  pastrat separat, apoi ref-pack-ul corect net8 a fost folosit. Fisierele .cs/.dll
  temporare unice din artifacts au fost eliminate exact, fara production globs.
- Review intermediar parinte al sursei/diff-ului/testelor/raw logs/docs trecut.
  Fara pretentii noi performance, pixel parity sau validare umana; runtime edit
  schimba numai referinta privata redundanta, nu algoritmul/rendering/input.
  Exploratorul este terminal, fara job-uri proprii in afara suitei complete.
- [x] Acceptare finala noua: full solution tests, formatter scoped, review si
  checkpoint complet, fara excluderi noi.
- Suita noua: dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1
  --logger trx --results-directory artifacts/aspect-cleanup-pass2/full-suite,
  CERNEALA_SDL_NATIVE_TESTS=1 si CERNEALA_SDL_CONFORMANCE_ARTIFACTS=path absolut
  artifacts/aspect-cleanup-pass2/full-suite-captures: exit0. Parintele a parsat
  cele 11 TRX: 6556 cazuri, 6549 Passed, 0 failed, 7 NotExecuted. Core 4260,
  SDL961 si SourceGen614 passed; manifest test trecut in VisualStudio47.
  Toate cele 133 focused outcomes sunt Passed si in suita completa.
  Setul celor sapte skips este identic cu prima trecere (comparatie de nume),
  nu gate-uri trecute ori waivers noi. Capturi de conformance au fost generate
  de suite; nu se revendica inspectie vizuala manuala sau parity exhaustiv nou.
- Formatter: dotnet format whitespace Cerneala.slnx --verify-no-changes
  --no-restore --include UI/Aspect/ElementAspect.cs
  tests/Cerneala.Tests/Controls/ElementAspectTests.cs
  tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs: exit0, zero edits.
  Workspace-loading warning investigat prin --verbosity diagnostic: doua
  project references fara metadata pentru Cerneala.Language; proiectele core
  si tests sunt incarcate. Rerun diagnostic exit0, zero fisiere formatate.
  Nu se revendica verificarea formatterului unrestricted sau analiza semantica
  completa dintr-un workspace cu warnings; gate-ul cerut este whitespace scoped.
- Review final parinte: sursa actuala, fiecare hunk propriu, characterization
  pre/post, raw full results, build warning, docs/manifest si limitele scope-ului
  confruntate. git diff --check trecut; FileTree regenerat fara delta noua fata
  de hunks-urile straine deja prezente. Nicio schimbare generator/API public,
  benchmark/algorithm sau golden; fara staging/commit/push. Temporarele eliminate,
  toate job-urile/exploratorul terminale; fara validare umana/cross-platform noua.

### A treia trecere — lookup de dependente (2026-10-01)

- Inspectie noua, limitata: authoring/builders, packages/catalog, token/default
  values, condition nodes, environment/variants/state, engine/processor si
  diagnostic projections. Luna read-only a explorat runtime/cache/behavior
  lifecycle si testele relevante; parintele a confruntat sursa/callers/docs.
  Nu este un audit exhaustiv nou al celor 61 de fisiere sau al generatorului.
- Candidat concret: AspectInvalidationGraph.TryGetDependencies produce deja un
  set gol nou la miss; AspectEngine.GetDependencies ignora acel out value si
  construieste alt set gol. Acelasi fallback este implementat in doi owners.
  Lot implementat: engine returneaza rezultatul grafului in ambele cazuri; graful
  pastreaza ownership-ul lookup-ului si al fallback-ului, fara helper nou.
  Contract pastrat: owner-thread/null guards, hit identity, fresh miss identity,
  read-only collections, versions zero, Resolve fara tracking si Clear untrack.
  Nu sunt schimbate API public/protected, cascade, cache keys sau lifetimes.
- Baseline nou: focused 133 Passed, 0 fail/skip; baseline.trx/log in
  artifacts/aspect-cleanup-pass3. Characterization permanent adaugat pentru
  miss/Resolve/Apply/hit/Clear si read-only output: 1 Passed pe productia
  nemodificata, apoi Passed si in focused-green dupa schimbare. Nu este RED.
- Experiment CSI izolat: runtime10.0.11, 20000 warmup si 100000 lookups/round,
  trei rounds, GC.GetAllocatedBytesForCurrentThread. Engine miss=14400000 bytes
  fiecare round; graph miss=7200056/7200000/7200000; engine hit=0 in cele trei
  rounds. Rezultat local, nu frame-time/all-platform/zero-allocation general.
  Script temporar exclus prin artifacts, timeout30s; proces terminat normal.
- Candidati respinsi: dependencies pentru invalidare/motion/trace au scope
  diferit; raw/resolved token trace values sunt contract documentat; cache-key
  compare/store sunt operatii distincte; disposal cu reuse filter si disposal
  integral au contracte diferite. Nu se inventeaza abstractii sau bug fixes.
- [x] Caracterizare pre/post si focused/affected gates: focused-green 134 Passed,
  affected-green 273 Passed, 0 fail/skip; counters si outcomes TRX confruntate.
  Comenzile focused/affected sunt identice cu trecerea2, rezultate in pass3.
  Review parinte: hunk-ul runtime foloseste out fallback-ul non-null deja
  contractat de graph; guards, instanta noua la miss si identity la hit pastrate.
  Dupa schimbare, CSI engine miss=7200000 bytes/100000 calls in toate cele trei
  rounds (72 bytes/call fata de 144 inainte), graph miss=7200000, engine hit=0.
  Masurare local runtime10.0.11, nu buget frame/runtime8 sau gain CPU/GPU.
  Scriptul .csx eliminat exact; sursa reproductibila pastrata ca .txt si loguri.
- [x] Build, BuildInputs, full solution tests, scoped formatter, cleanup si
  review/checkpoint parinte; fara excluderi noi.
  BuildInputs.Tests: legitimate inputs preserved, archived=0 in toate cele
  patru categorii. Build rulat separat dupa corectarea verificarii auxiliare
  LASTEXITCODE (scriptul foloseste Process API, nu native exit state):
  dotnet build Cerneala.slnx -c Release --no-restore -m:1, exit0, 0 warnings/errors.
  Formatter whitespace cu --verify-no-changes --no-restore --include numai
  AspectEngine.cs si AspectEngineTests.cs, --verbosity diagnostic: exit0,
  Formatted0/2282. Aceleasi doua Language metadata-reference workspace warnings;
  proiectele core/tests incarcate. Nu este gate unrestricted de analiza semantica.
- Full nou: dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1
  --logger trx --results-directory artifacts/aspect-cleanup-pass3/full-suite,
  CERNEALA_SDL_NATIVE_TESTS=1, CERNEALA_SDL_CONFORMANCE_ARTIFACTS=path absolut
  artifacts/aspect-cleanup-pass3/full-suite-captures: exit0. Cele 11 TRX/outcomes
  parsate: 6557 cazuri, 6550 Passed, 0 failed, 7 NotExecuted; core4261,
  SDL961, SourceGen614. Toate134 focused outcomes sunt Passed si in full.
  Setul celor sapte skips identic cu pass2: patru opaque/translucent cases,
  native ownership/input/lifetimes, Language completion P95 si Village cadence.
  Nu au fost activate alte opt-ins/dezactivate teste sau introduse waivers.
  Full-suite-summary.json, log si TRX pastrate; capturi native conformance exista.
- Review final parinte: sursa actuala, hunk-ul engine, testul de caracterizare,
  call paths Processor/standalone invalidation, contractul grafului, docs,
  raw pre/post allocations si TRX, interactiunea cu lotul2 confruntate.
  API public/protected si comportamentul documentat neschimbate; nu sunt necesare
  pagini/manifest noi. git diff --check trecut; FileTree regenerat neschimbat.
  Temporarul .csx eliminat; CSI, suitele, build-ul, formatter-ul si exploratorul
  terminale. Parserul de rezultate auxiliare, al carui handle nu fusese afisat,
  confirmat terminat prin process inventory si verificarea focused/full repetata
  cu lookup HashSet. Fara job-uri proprii ramase, staging, commit sau push.
  Modificarile anterioare si straine intacte. Nu se revendica validare umana,
  pixel parity manuala/cross-platform sau masuratori noi de frame-time/CPU/GPU.

### A patra trecere — consumer/lifecycle si signal atomicity (2026-10-01)

- Inspectie noua limitata: ElementAspect consumers, environment children,
  standalone invalidation, condition keys, processor behavior/cache si template
  registrations/lifetimes, cu direct callers/tests/docs. Luna read-only a
  explorat consumers/environment/invalidation/signals; parintele a confruntat
  sursa si a detinut experimentele/concluziile. Fara audit exhaustiv nou.
- Nu este justificata o abstractie noua pentru weak refs: validarea consumerilor
  precede mutatia, invalidarea urmeaza publicarea package; environment notification
  necesita snapshot puternic inainte de callbacks, registration pruning nu.
  Filtered behavior retirement si disposal integral au contracte diferite;
  cache-key comparison/storage nu sunt aceeasi operatie. Fara refactor fortat.
- Defect observat separat de cleanup: AspectConditionKey.SetActive scrie
  State.Active la linia38, apoi cere invalidare la linia39. Pentru un element
  rooted, UIElement.Invalidate delega la UIRoot.Invalidate, unde Relay.VerifyAccess
  respinge worker-ul inainte de queue/dirty propagation. Key nu are preflight
  owner-access sau rollback. Invariant owner al starii este key, guard-ul queue
  este root; cauza demonstrata este ordinea write-before-rejected-invalidation,
  nu resolverul/cascade sau weak-reference collection.
- Ipoteza falsificabila: worker SetActive(true) pe element rooted cu key false
  arunca, dar retine true fara queue work; retry pe owner devine no-op. Falsifier:
  key ramane false dupa exceptie sau exista Aspect work programat. Experiment
  izolat cu public APIs, root+UIElement, default opacity0.4, conditional0.8,
  settle maxim10 frames, Thread.Join10s, proces timeout30s. CSI/runtime10.0.11
  si harness compilat cu csc/net8 refs, runtime8.0.30: fiecare 5/5 trials a dat
  InvalidOperationException, active=true, queued=0, AspectDirty=false,
  ownerRetryChanged=false, nextAspectElements=0, opacity0.4. Invalidare explicita
  pe owner rezolva opacity0.8 in fiecare trial, distingand key/scheduling failure
  de engine failure. Detached worker control: fara exceptie, active=true,
  AspectDirty=true. Sustinut in aceste conditii, fara pretentie de toate races.
- Raw evidence/reproducer sources ca .txt in artifacts/aspect-cleanup-pass4:
  condition-signal-thread.log/err, condition-signal-thread-source.txt;
  condition-signal-net8.log/err, condition-signal-net8-compile.log,
  condition-signal-net8-source.txt.
  Ambele procese exit0; net8 compile fara diagnostics. .csx/.cs/.dll/runtimeconfig
  temporare eliminate exact, numai in artifacts exclus din production globs.
- Nu s-a modificat productie/test API/docs si nu s-a adaugat un regression test
  permanent care sa canonizeze defectul. Thread affinity/failure atomicity pentru
  key nu sunt explicite in pagina canonica; cerere de acord separat pentru
  respingerea off-thread inainte de mutatie, pastrand detached configuration.
  Testele existente inspectate nu acopera acest caz; suita completa pass3 nu
  este prezentata drept verificare noua pass4. Fara build/full suite noi, fiind
  numai investigatie, nu patch. Reparatia si RED/verification raman neautorizate.
- Review parinte: source path, guards, caller contracts, raw outputs, input
  conditions si limite confruntate. git diff --check trecut, schimbarile pass2/3
  intacte; FileTree reflecta arhiva InvestorReel creata extern si disparitia
  arhivei SolarSystem externe, fara operatii ale agentului asupra acestor fisiere.
  Explorator/procese terminale, fara temporare executabile, staging/commit/push.
