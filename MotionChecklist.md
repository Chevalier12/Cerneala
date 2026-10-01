# Motion — checklist fisiere C#

126 fisiere din inventarul implementarii dedicate Motion: 112 in `UI/Motion`
si 14 fisiere de semantica, markup, integrare si diagnostice.

Include fisierul semantic comun Motion/Prism si puntea Motion–Prism.
Nu include teste, exemple, benchmark-uri, documentatie sau fisiere generale
care integreaza Motion alaturi de alte sisteme.

Status: finalizat. O bifa inseamna fisier inspectat, cleanup justificat aplicat
sau pastrare deliberata, apoi verificare focalizata trecuta. Nu inseamna ca
fiecare fisier trebuie modificat. Suita completa ruleaza numai la final,
conform acordului; checkpoint-urile de mai jos pastreaza evidenta loturilor.

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Language\Semantics
- [x] CernealaSemanticModel.MotionPrism.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Language\Syntax\Embedded
- [x] MotionSyntaxParser.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen
- [x] MotionMarkupLanguage.cs
- [x] UiMarkupMotionResolver.cs
- [x] UiMarkupMotionSyntax.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism\Binding
- [x] PrismMotionResolver.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism\Emission
- [x] PrismMotionEmitter.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Aspect
- [x] AspectMotion.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Detective
- [x] MotionDiagnostics.cs
- [x] MotionGraphSnapshot.cs
- [x] MotionTrace.cs
- [x] MotionTraceEvent.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Markup
- [x] GeneratedMarkupMotion.cs
- [x] MarkupMotionExecution.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion
- [x] MotionAnimationBuilder.cs
- [x] MotionDefaults.cs
- [x] MotionElementFacade.cs
- [x] MotionExtensions.cs
- [x] MotionProperty.cs
- [x] MotionPropertyShortcut.cs
- [x] MotionStateBuilder.cs
- [x] MotionStateTargetBuilder.cs
- [x] ObjectMotionAnimationBuilder.cs
- [x] ObjectMotionExpressionPropertyCache.cs
- [x] ObjectMotionFacade.cs
- [x] ObjectMotionRuntime.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Core
- [x] DerivedMotionValue{T}.cs
- [x] IMotionClock.cs
- [x] IReducedMotionSource.cs
- [x] ManualMotionTimeline.cs
- [x] MotionCancellation.cs
- [x] MotionChannel.cs
- [x] MotionCompletionSource.cs
- [x] MotionComposition.cs
- [x] MotionConflictResolver.cs
- [x] MotionFrame.cs
- [x] MotionFrameCoordinator.cs
- [x] MotionFramePhase.cs
- [x] MotionFrameResult.cs
- [x] MotionGraph.cs
- [x] MotionGroup.cs
- [x] MotionGroupHandle.cs
- [x] MotionHandle.cs
- [x] MotionNode.cs
- [x] MotionPriority.cs
- [x] MotionSequence.cs
- [x] MotionStagger.cs
- [x] MotionStartOptions.cs
- [x] MotionSystem.cs
- [x] MotionTimeline.cs
- [x] MotionTimelineRegistry.cs
- [x] MotionValue.cs
- [x] MotionValue{T}.cs
- [x] ReducedMotionMode.cs
- [x] ReducedMotionPolicy.cs
- [x] SystemMotionClock.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Input
- [x] DragMotionController.cs
- [x] GestureMotionController.cs
- [x] MotionRange.cs
- [x] PointerMotionState.cs
- [x] ScrollMotionBinding.cs
- [x] ScrollTimeline.cs
- [x] VelocityTracker.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Interpolation
- [x] BrushMixer.cs
- [x] ColorMixer.cs
- [x] DoubleMixer.cs
- [x] DrawPointMixer.cs
- [x] DrawRectMixer.cs
- [x] DrawSizeMixer.cs
- [x] FloatMixer.cs
- [x] IValueMixer.cs
- [x] IValueMixerDispatcher.cs
- [x] ThicknessMixer.cs
- [x] TransformComponents.cs
- [x] TransformInterpolationMode.cs
- [x] TransformMixer.cs
- [x] ValueMixer.cs
- [x] ValueMixerRegistry.cs
- [x] Vector4Mixer.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Layout
- [x] LayoutMotionBinding.cs
- [x] LayoutMotionCoordinator.cs
- [x] LayoutMotionId.cs
- [x] LayoutMotionOptions.cs
- [x] LayoutSnapshot.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Presence
- [x] PresenceCoordinator.cs
- [x] PresenceOptions.cs
- [x] PresenceState.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Properties
- [x] AnimatablePropertyRegistry.cs
- [x] MotionClearBehavior.cs
- [x] MotionPropertyBinding.cs
- [x] MotionPropertyBinding{T}.cs
- [x] MotionPropertyInvalidationCategory.cs
- [x] MotionPropertyInvalidationClassifier.cs
- [x] MotionPropertyKey.cs
- [x] MotionPropertyOptions.cs
- [x] MotionPropertyStartOptions.cs
- [x] MotionPropertyStore.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Specs
- [x] CubicBezierEasing.cs
- [x] DecaySpec.cs
- [x] Easings.cs
- [x] FillMode.cs
- [x] IEasing.cs
- [x] KeyframesSpec.cs
- [x] Motion.cs
- [x] MotionCompletion.cs
- [x] MotionSampler.cs
- [x] MotionSpec.cs
- [x] MotionSpecContext.cs
- [x] MotionSpec{T}.cs
- [x] MotionVelocity.cs
- [x] PingPongSpec.cs
- [x] RepeatSpec.cs
- [x] RetargetMode.cs
- [x] SpringSpec.cs
- [x] SpringVelocityMode.cs
- [x] StepEasing.cs
- [x] TweenSpec.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\States
- [x] MotionStateRule.cs
- [x] MotionTokens.cs
- [x] MotionVisualStateController.cs
- [x] MotionVisualStateSnapshot.cs
- [x] ThemeMotionTokens.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Motion\Transactions
- [x] MotionTransaction.cs
- [x] MotionTransactionContext.cs
- [x] MotionTransactionOptions.cs
- [x] MotionTransactionScope.cs

## Jurnal cleanup

### Lot 1 — Language: semantica Motion si adapterul de parsare (2026-10-01)

- Scope: primele doua fisiere din inventar; 2/126 rezolvate.
- Inspectie: implementarea completa, partialele semantic-model (resurse,
  binding, completion), consumatorul SourceGen si DiagnosticService LSP,
  testele EmbeddedSyntax/MotionPrismSemantic si testele protocolului afectat.
  Explorator Luna read-only a confirmat referintele; decizia si review-ul sunt
  ale parintelui. Cautarile nu demonstreaza absenta reflectiei arbitrare in
  binare externe; nicio utilizare nominala dinamica a simbolurilor private
  eliminate nu apare in sursele inspectate.
- Schimbare: eliminat `MotionProgram.Text` fara cititori; eliminati cei doi
  parametri din `BindMotionCommand` folositi numai intr-un bloc gol; eliminat
  `ValidateMotionOption`, fara apeluri. Comentariul despre scope-ul clipurilor
  ramane langa `FindResource`. Ordinea validarilor, lookup-ul resurselor,
  simbolurile, span-urile, diagnosticele si API-ul raman neschimbate.
- Pastrare: `MotionSyntaxParser` este adapterul dedicat catre parserul comun;
  nu justifica o alta cale de parsare. Nu s-au unificat contractele Motion si
  Prism si nu s-a modificat agregarea validarilor MotionSpec. Validatorul
  activ al optiunilor ramane in SourceGen; nu s-a introdus comportament nou.
- Baseline: `dotnet test tests/Cerneala.Tests.Language/Cerneala.Tests.Language.csproj
  -c Release --no-build --no-restore --filter
  'FullyQualifiedName~MotionPrismSemanticTests|FullyQualifiedName~EmbeddedSyntaxTests'`:
  71 passed, 0 failed. Aceleasi 71 nume/outcome-uri dupa cleanup, cu rebuild
  (`--no-restore`, fara `--no-build`).
- Gate afectat: intregul proiect Language, `-c Release --no-build --no-restore`:
  259 passed, 0 failed, 1 skip existent. Skip-ul WarmCompletionP95 este cerut
  de maintainer din 2026-09-21; defectul de performanta nu este rezolvat aici.
- Gate protocol: `dotnet test
  tests/Cerneala.Tests.LanguageServer/Cerneala.Tests.LanguageServer.csproj
  -c Release --no-restore --filter
  'FullyQualifiedName~DiagnosticsTests|FullyQualifiedName~CompletionProtocolTests'`:
  11 passed, 0 failed. Include semicolon diagnostics si completarea Motion.
- Dovezi brute: `artifacts/motion-cleanup/language/{language-baseline,
  language-focused,language-suite,language-protocol}.trx`; fiecare comanda
  foloseste `--logger 'trx;LogFileName=<nume>.trx' --results-directory
  artifacts/motion-cleanup/language`. Directorul artifacts este ignorat.
- Review parinte: diff-ul si sursa curenta verificate; seturile de teste
  baseline/focused identice; `git diff --check` trecut. Niciun API public,
  generator output, renderer, frame lifecycle sau documentatie API schimbat;
  fara pretentii de performanta, fara gate vizual aplicabil acestui lot.
  Nicio suita completa de repository rulata in acest checkpoint.
- Modificarile preexistente din `Cerneala.slnx`, `FileTree.md`, InvestorReel si
  Markup_Authoring_Findings sunt pastrate. Fara commit/push.

### Lot 2 — SourceGen: vocabular, model sintactic si resolver Motion (2026-10-01)

- Scope: cele trei fisiere SourceGen din grupul principal; 5/126 rezolvate.
- Inspectie completa a celor trei target-uri, consumatorii din parserul comun,
  resource readers/PrepareAspectBehavior/ApplyAspects si reactive emitter.
  Referintele si testele existente au fost confruntate cu explorarea Luna.
- Cleanup intern: `MotionSpecResource` pastreaza doar `Kind` si `Arguments`;
  `Name` si `Source` nu aveau cititori in payload. Numele din `NamedSymbol`
  si locatiile referintelor sintactice raman intacte. Eliminata colectia
  locala `bindingNames` din `EmitMotionScrolls`, care era doar umpluta, niciodata
  citita. Declaratiile si toate liniile emise attach/detach raman intacte.
- Pastrate fara modificari: tabelul MotionMarkupLanguage (folosit de parser si
  verificat reflectiv de teste) si nodurile UiMarkupMotionSyntax (constructori
  ai parserului, locatii diagnostice si consumatori resolver/emitter). Forma
  similara cu tabelul Prism nu justifica un nou layer comun.
- Baseline si GREEN: `dotnet test
  tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release
  --no-restore --filter 'FullyQualifiedName~Motion'`: 192 passed, 0 failed
  in fiecare run; baseline-ul foloseste `--no-build` dupa build, GREEN face
  rebuild. Seturile de nume/outcome-uri TRX sunt identice. Initialul filtru
  bazat pe numele fisierelor a selectat zero teste; a fost corectat inaintea
  editarii si nu a fost contabilizat drept verificare.
- Gate afectat: acelasi proiect fara filtru, `--no-build --no-restore`:
  614 passed, 0 failed, 0 skipped. Include testele generatie/compilare,
  diagnostice, determinism si executie a factory-urilor generate. Testele
  scroll inspectate verifica doua assignment-uri verticale, varianta
  orizontala/AllowLayout si cleanup-ul emis; nu sunt teste runtime scroll.
- Dovezi brute: `artifacts/motion-cleanup/sourcegen/{sourcegen-baseline,
  sourcegen-focused,sourcegen-suite}.trx`; optiunile logger/results-directory
  ca la lotul anterior, cu acest director si aceste nume.
- Review parinte: sursa curenta, constructorul si toti consumatorii payload-ului,
  diff-ul (2 insertii/10 stergeri) si rezultatele brute verificate;
  `git diff --check` trecut. Fara API public/protected, sintaxa, algoritmi,
  lifecycle sau output statements schimbate; fara documentatie API necesara,
  masuratori de performanta revendicate ori gate vizual aplicabil. Nu s-au
  rulat suita completa de repository sau commit/push.

### Lot 3 — SourceGen: puntea Motion–Prism (2026-10-01)

- Scope: PrismMotionResolver si PrismMotionEmitter; 7/126 rezolvate.
  Implementarile complete, consumatorii comuni Motion si testele relevante
  au fost inspectate de parinte; Luna a confirmat referintele read-only.
- Cleanup: eliminat `AspectResource aspect` din resolverul tintelor Prism si
  helper-ul owner, unde era doar transmis, niciodata citit. Actualizat apelul
  unic din `UiMarkupMotionResolver` ca modificare colaterala obligatorie.
  Rezolutia scope-ului, template owner, namescope-ul, diagnosticele si span-urile
  nu sunt modificate. Parametrul Aspect ramane in resolverul comun unde este
  folosit pentru template-urile tintelor non-Prism.
- Pastrat `PrismMotionEmitter`: payload-ul are consumatori pentru toate
  campurile; getter/setter static, conversiile integer/number si catalog
  slots au responsabilitati distincte. Nu s-au gasit helper-e private fara
  apeluri sau duplicari care justifica un alt layer in aceste doua fisiere.
- Baseline refolosit: `sourcegen-suite.trx` din lotul 2, acelasi cod al puntilor
  inainte de edit; toate cele 7 cazuri PrismMotion passed. Acopera self/owner/
  named, Number/Color, bool/enum discreti, @set, segmente invalide si binding.
- Verificare dupa edit: `dotnet test
  tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release
  --no-restore --logger 'trx;LogFileName=bridge-sourcegen-suite.trx'
  --results-directory artifacts/motion-cleanup/sourcegen`: rebuild si 614
  passed, 0 failed, 0 skipped, inclusiv aceleasi 7 cazuri focalizate. Seturile
  celor 614 nume/outcome-uri baseline/GREEN sunt identice. Acest run reverifica
  si apelul colateral din fisierul bifat in lotul 2; checkpoint-ul lui ramane valid.
- Review parinte: sursa curenta si toate cele 4 linii eliminate, call paths,
  emitterul nemodificat, diff-ul si TRX-ul verificate; `git diff --check` trecut.
  Fara API/documentatie/generator output/backend/frame lifecycle modificat;
  gate vizual/performance nu se aplica eliminarii de parametri privati.
  Suita completa a repository-ului ramane pentru final; fara commit/push.

### Lot 4 — metadata publica Aspect–Motion (2026-10-01)

- Scope: AspectMotion.cs; 8/126 rezolvate. Pastrat fara modificari: model
  public mic, validari explicite, trei proprietati cu consumatori reali si
  flags stabili. Nu exista un cleanup justificat in acest fisier.
- Parintele a citit complet target-ul, AspectDeclaration, ResolvedAspectValue
  si cele doua pagini canonice; a urmarit Resolve/ApplyResolved/GetMotionValue/
  HasApplicableMotion/ApplyMutation in AspectEngine si testele relevante.
  Luna a confirmat read-only responsabilitatile, consumatorii si limitele.
- Contract pastrat: identitate UiProperty, token theme, masca surselor,
  fallback la metadata precedenta pentru exit; State este Interactive,
  celelalte Normal. Fara root/theme se aplica imediat. Nicio mutatie API/docs.
- Gate: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-restore --filter 'FullyQualifiedName~AspectEngineTests'
  --logger 'trx;LogFileName=aspect-engine.trx' --results-directory
  artifacts/motion-cleanup/aspect`: rebuild, 15 passed, 0 failed, 0 skipped.
  TRX: `artifacts/motion-cleanup/aspect/aspect-engine.trx`.
- Acopera tranzitii state in ambele directii, masca necorespunzatoare,
  prioritatea animatiei explicite si clasificarea Base/State/Variant/Data.
  Stimulusul hover este modificarea directa IsPointerOver, nu input real;
  nu revendica verificarea hit testing/routing/focus. Constructor guards si
  combinatiile tuturor flag-urilor nu au teste directe gasite; aici nu se
  modifica comportamentul lor si nu se inventeaza teste pentru un refactor absent.
- Review parinte: target-ul/docs fara diff, consumatorii si TRX verificate,
  `git diff --check` trecut. SCN2D017 din build este informatia existenta
  despre fields editor-only LDtk. Nu este gate vizual/performance al unui
  runtime modificat. Suita completa ramane pentru final; fara commit/push.

### Lot 5 — Detective: diagnostice, snapshot si trace Motion (2026-10-01)

- Scope: cele patru fisiere Detective; 12/126 rezolvate. C# pastrat fara
  modificari: contoare/collections cu ownership clar, trace opt-in cumulativ
  pana la Clear, warning-uri independente de tracing, snapshot positional.
  Nu se adauga limite arbitrare, ring buffer, enum removal sau alte semantici.
- Parintele a citit complet cele patru fisiere si MotionDiagnosticsTests,
  paginile canonice aferente si coordinatorul frame; a inspectat creatorul
  MotionSystem, captura Detective, definitia binding.IsActive si testele
  relevante. Luna a confirmat referintele si discrepantele documentatiei.
- Reparatie documentatie canonică, fara schimbare API/runtime:
  `Cerneala.UI.Detective.MotionDiagnostics.md` descrie accesul public real
  UIRoot.Detective.Motion (MotionSystem.Diagnostics este internal);
  `Cerneala.UI.Detective.MotionGraphSnapshot.md` identifica struct-ul,
  copie contoarele ultimului MotionFrameResult si exclude binding-uri idle.
  Sursa si testele contraziceau vechile afirmatii despre zero/registered.
  Workflow writing-api-documentation si referinta completa au fost citite;
  destinatia canonica docs-site are prioritate fata de path-ul vechi din skill.
- Gate: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-build --no-restore --filter
  'FullyQualifiedName~MotionDiagnosticsTests|FullyQualifiedName~MotionSpecTests|FullyQualifiedName~MotionSystemTests|FullyQualifiedName~MotionCompositionReducedMotionTests|FullyQualifiedName~UI.Detective.DetectiveTests'
  --logger 'trx;LogFileName=motion-detective.trx' --results-directory
  artifacts/motion-cleanup/detective`: 80 passed, 0 failed, 0 skipped.
  Build-ul curent din lotul 4 este refolosit; doar docs/checklist au fost
  modificate ulterior, deci run-ul nu este invalidat.
- Probe: start/sample/completed, tracing disabled, snapshot counters nonzero,
  binding cached idle exclus, warnings reset, faze frame/capture si reduced
  motion skip. Raw TRX: `artifacts/motion-cleanup/detective/motion-detective.trx`.
- Review parinte: diff docs si sursa curenta confruntate cu probele;
  Definition/Examples/Remarks/member tables si absenta placeholder-elor
  verificate, manifest JSON parsat si ambele entries unice/existente confirmate
  (fara pagini noi/renames, deci manifest nemodificat). `git diff --check` trecut.
  Enumerarea tipurilor publice nu autorizeaza stergerea celor fara emit call
  automat: Record este public. Test direct Trace.Clear/ordine absoluta nu a
  fost gasit; contractul nu a fost schimbat. Fara revendicari de performanta,
  gate vizual nou, suita completa de repository sau commit/push.

### Lot 6 — UI/Markup: sesiuni si executii Motion (2026-10-01)

- Scope: GeneratedMarkupMotion si MarkupMotionExecution; 14/126 rezolvate.
  Parintele a citit complet implementarea, testele GeneratedMarkupMotion,
  MarkupMotionExecution si PrismMotionIntegration, paginile canonice si
  referintele relevante; exploratorul Luna a confirmat read-only ownership-ul.
- Cleanup: eliminat doar campul privat PrismMotionBinding<T>.getValue si
  atribuirea lui, fara cititori. Parametrul constructorului si apelul
  getValue(instance), care captureaza baseValue, raman intacte. Nu exista
  utilizare nominala reflectiva gasita in sursele inspectate; cautarea nu
  exclude reflectie arbitrara din binare externe.
- MarkupMotionExecution pastrat: raw groups si executii unificate au API-uri
  publice distincte, documentate si testate. Nu se combina exceptiile din
  Parallel cu CancelAll si nu se modifica ordinea callback-urilor, cancelarile,
  retargeting-ul, lifecycle-ul sesiunilor sau binding-urile Prism.
- Baseline/focused: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj
  -c Release --no-restore --filter
  'FullyQualifiedName~GeneratedMarkupMotionTests|FullyQualifiedName~MarkupMotionExecutionTests|FullyQualifiedName~PrismMotionIntegrationTests'`:
  32 passed, 0 failed, 0 skipped in ambele run-uri. Baseline foloseste
  --no-build; focused reconstruieste. Numele/outcome-urile sunt identice.
- Gate afectat: acelasi proiect, --no-build --no-restore, filtru
  'FullyQualifiedName~UI.Markup|FullyQualifiedName~PrismMotionIntegrationTests':
  80 passed, 0 failed, 0 skipped. SourceGen, -c Release --no-restore, filtru
  'FullyQualifiedName~Motion': 192 passed, 0 failed, 0 skipped.
- Dovezi: `artifacts/motion-cleanup/markup/{markup-baseline,markup-focused,
  markup-suite,markup-sourcegen}.trx`, logger/results-directory ca in loturile
  precedente. Acopera compozitii, restart/cancel, detach/visibility/replacement,
  colectabilitate si invalidari Prism. Testul allocation warm-up (8 frame-uri,
  apoi 32 frame-uri masurate) trece; nu este o afirmatie generala de performanta.
- Review parinte: cele doua linii eliminate, sursa actuala, call paths si TRX
  confruntate; git diff --check trecut. Fara API/docs public schimbat ori
  schimbare a algoritmului de render care necesita un gate pixel nou.
  Niciun job ramas activ; suita completa ramane pentru final; fara commit/push.

### Lot 7 — UI/Motion: facade, builders si runtime pentru obiecte (2026-10-01)

- Scope: cele 12 fisiere directe UI/Motion; 26/126 rezolvate. Toate citite
  complet de parinte, cu testele facade/object/allocation/stress, integrarea
  UiHost si contractele canonice; Luna a confirmat referintele read-only.
- C# pastrat: nu s-au gasit campuri/helper-e private declaration-only.
  Overload-urile publice similare pastreaza selectia UIElement/object si
  params-ul rezervat pentru overload resolution. Nu se introduce un layer
  generic pentru doua linii comune, nu se unifica binding-uri UI si CLR cu
  ownership/lifetime diferite. StateBuilders ramane ConditionalWeakTable,
  cache-ul expression pastreaza member identity, runtime-ul CLR ramane thread-local.
- Docs canonice corectate in MotionExtensions, ObjectMotionFacade (ambele),
  MotionProperty si MotionPropertyShortcut: semnatura reala To(value,spec),
  receiver object explicit, exemple cu OuterGlowStyle existent in loc de
  Player/Marker/pulse/spec nedefinite; shortcut-urile au root atasat si imports.
  Niciun API/runtime schimbat. Workflow writing-api-documentation aplicat;
  cele cinci intrari manifest existente/unice si structura paginilor verificate.
- Gate: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-build --no-restore --filter
  'FullyQualifiedName~MotionFacadeTests|FullyQualifiedName~ObjectMotionTests|FullyQualifiedName~MotionAllocationTests|FullyQualifiedName~MotionStressTests|FullyQualifiedName~MotionInputTimelineTests'
  --logger 'trx;LogFileName=facades-gate.trx' --results-directory
  artifacts/motion-cleanup/facades`: 36 passed, 0 failed, 0 skipped. Build-ul
  lotului 6 este curent; docs-only nu invalideaza probele compilate.
- Cele 7 exemple din paginile editate compilate ca metode independente intr-o
  clasa temporara, imports reunite, folosind `dotnet` cu SDK 10.0.400
  Roslyn/bincore/csc.dll, /target:library /nullable:enable, toate reference DLL
  din cache-ul microsoft.netcore.app.ref/8.0.30/ref/net8.0 si DLL-ul Cerneala
  Release/net8.0 curent: exit 0. Prima cautare a pack-ului net8 in Program Files
  a esuat inainte de compilare; pack-ul real din NuGet a fost folosit ulterior.
  Fisierele temporare .cs/.dll eliminate exact; log si TRX in
  `artifacts/motion-cleanup/facades`. Nu se revendica executia acestor exemple.
- Review parinte: implementarea si docs diff actual confruntate cu signatures,
  TRX 36/36 si compiler exit 0, git diff --check trecut. Testele state includ
  proprietati modificate direct, nu dovada de input real; bugetele allocation
  sunt cele existente, nu masuratori de optimizare. Fara schimbare vizuala/API,
  exploratori/job-uri active sau suita completa de repository; fara commit/push.

### Lot 8 — Motion/Core: graf, valori si orchestrare (2026-10-01)

- Scope: toate cele 30 fisiere Core, citite complet de parinte; 56/126
  rezolvate. Luna a confruntat bounded call path-ul Advance/ValueNode/Graph/
  System, testele reentrante si discrepantele canonice citate; nu a executat
  verificari sau un audit independent exhaustiv al tuturor membrilor privati.
- Cleanup: Advance returneaza direct rezultatul ApplySample. Vechea ramura
  ReferenceEquals returna exact aceeasi variabila ca fallthrough; comparatiile
  pure nu aveau efect. Aplicarea sample-ului, callbacks, counters si out completed
  raman in aceeasi ordine. Celelalte guards de reentrancy, identitate handle/
  sampler, detachment si finalizare raman intacte. Celelalte 29 fisiere pastrate:
  ownership si contracte distincte, fara cleanup justificat gasit.
- Nu se unifica MotionGroup si MarkupMotionExecution: anularea unui copil
  conteaza ca terminal in Core Parallel, pe cand executia markup are alta
  semantica. Nu se elimina enum-uri/API-uri publice fara apeluri nominale,
  thread guards, staged graph mutations sau snapshot-uri reentrante.
- Baseline/GREEN: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj
  -c Release --no-restore --filter 'FullyQualifiedName~UI.Motion.Core'
  --logger 'trx;LogFileName=core-focused.trx' --results-directory
  artifacts/motion-cleanup/core`: 78 passed, 0 failed, 0 skipped; baseline
  --no-build cu core-baseline.trx, GREEN rebuild. Numele/outcome-urile identice.
- Gate afectat: acelasi proiect --no-build --no-restore, filtru
  'FullyQualifiedName~UI.Motion|FullyQualifiedName~UI.Markup|FullyQualifiedName~PrismMotionIntegrationTests|FullyQualifiedName~MotionDiagnosticsTests':
  326 passed, 0 failed, 0 skipped; core-affected.trx in acelasi director.
  Acopera listeners cancel/complete/restart, notificari recursive, exceptii
  terminale, graph remove/re-add, grupuri, prioritati, idle/delta/frame phases,
  colectabilitate, property/lifecycle si bugetele allocation existente.
- Docs canonice: corectat argumentul FrameIndex in MotionGraph, MotionNode,
  MotionFrameResult, MotionValue<T> si ValueNode; override-ul consumatorului
  extern este protected in MotionNode si MotionNodeTickResult. Semnatura
  production protected internal nu este schimbata. Cele sase entries manifest
  existente/unice confirmate; Definition/Examples/Remarks/tables inspectate.
- Opt exemple compilate separat cu csc SDK 10.0.400, /target:library,
  /nullable:enable, ref-pack net8.0 8.0.30, Cerneala Release curent si import
  System implicit, fara executia lor. Prima extractie a confundat o using
  declaration cu un import si a produs un fixture invalid; reparata extractia,
  toate cele opt exit 0. CS0219 in exemplul default-result este warning pentru
  variabila demonstrativa, nu failure. Log core-doc-examples-compile.log
  pastreaza ambele incercari; .cs/.dll temporare eliminate exact.
- Review parinte: diff runtime si docs, sursa curenta/callers, TRX si compiler
  log verificate; git diff --check trecut. Fara public/protected API modificat,
  modificari de renderer ori pretentii de optimizare masurata. Auditul docs
  este focalizat, nu exhaustiv; exemplele Sequence nu au fost executate si
  nu demonstreaza un driver de timp pentru ManualMotionTimeline.
  Niciun job/explorator activ; suita completa ramane pentru final; fara commit/push.

### Lot 9 — Motion/Input: drag, gesturi si scroll (2026-10-01)

- Scope: toate cele sapte fisiere Input, citite complet; 63/126 rezolvate.
  Callers facade/emitter, testele MotionInputTimeline/LifecycleStress si docs
  Drag/Range/Velocity inspectate de parinte. Luna a confirmat field references,
  closures si call paths; cautarea nominala nu este dovada despre reflectie
  arbitrara in binare externe. Celelalte sase fisiere raman fara modificari.
- Eliminat campul privat DragMotionController.element, doar atribuit; actualele
  citiri si closures folosesc parametrul constructorului. Validarea null ramane
  in aceeasi pozitie, prin ThrowIfNull. Ownership-ul valorilor/subscriptiilor,
  disposal, timestamp-uri, velocity, capture-loss si targets nu sunt schimbate.
- Caracterizare permanenta adaugata in MotionInputTimelineTests:
  DragControllerRejectsNullElement verifica tipul exceptiei si ParamName.
  A trecut 1/1 cu productie nemodificata, apoi in GREEN; nu este un RED de bug.
- Baseline reutilizat din core-affected.trx: cele 14 cazuri InputTimeline/
  LifecycleStress passed. Dupa edit, `dotnet test
  tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-restore --filter
  'FullyQualifiedName~MotionInputTimelineTests|FullyQualifiedName~MotionMarkupLifecycleStressTests'
  --logger 'trx;LogFileName=input-focused.trx' --results-directory
  artifacts/motion-cleanup/input`: rebuild, 15 passed, 0 failed, 0 skipped.
  Cele 14 cazuri preexistente au nume/outcome-uri identice cu baseline-ul.
- Gate afectat: acelasi proiect --no-build --no-restore, acelasi filtru de
  4 familii din lotul Core (UI.Motion/UI.Markup/PrismMotionIntegration/
  MotionDiagnostics): 327 passed, 0 failed, 0 skipped, input-affected.trx.
  Run-ul caracterizarii este input-characterization.trx in acelasi director.
- Docs Drag corectat punctual: End valideaza settleSpec in controller inainte
  de State/axes, nu prin AnimateTo. API/runtime ramane identic. Pagina completa,
  structura workflow-ului si intrarea unica/existenta manifest verificate.
- Review parinte: diff sursa/test/docs si TRX actual confruntate; git diff
  --check trecut. Stimulusul testelor este direct pe controller/ScrollInfo,
  nu input nativ sau validare noua hit-test/routing; aceste cai nu sunt editate.
  Fara pretentii de optimizare, API/protected sau algoritmi render schimbati.
  Explorarea read-only a dependintelor ramase continua separat; niciun build/
  test job activ. Suita completa ramane pentru final; fara commit/push.

### Lot 10 — Motion/Interpolation: mixere si dispatcher (2026-10-01)

- Scope: toate cele 16 fisiere, citite complet de parinte; 79/126 rezolvate.
  Luna a confruntat ownership-ul, helpers si direct callers read-only.
- Pastrate deliberat: helpers/state au utilizari; contractele scalar/vector,
  rounding byte RGBA, identitatea endpoint-urilor Brush, stop matching si
  decompozitia Transform nu justifica un layer comun sau alt algoritm.
  API-urile publice/protected fara apel nominal nu sunt cod privat mort.
- Gate curent reutilizat: input-affected.trx, 327 passed/0 failed/0 skipped,
  inclusiv toate cele 18 ValueMixerBuiltInTests passed. Codul compilat este
  neschimbat dupa lotul 9. Comanda reproductibila focalizata: dotnet test
  tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-build --no-restore
  --filter 'FullyQualifiedName~ValueMixerBuiltInTests'. Testele complete citite;
  acopera endpoint-uri, large values, Brush/Color, moduri Transform, registry
  si vector ops. Nu s-a gasit test direct Vector4; comportamentul nu este editat.
- Review parinte: sursa actuala, dispatcher consumers si rezultate TRX
  confruntate; git diff --check trecut. Fara C#/API/docs schimbate, pretentii
  performance sau gate vizual nou. Exploratorii dependintelor sunt read-only;
  niciun build/test activ. Suita completa ramane pentru final; fara commit/push.

### Lot 11 — Motion/Layout: snapshot-uri si correction bindings (2026-10-01)

- Scope: cinci fisiere citite complet; 84/126 rezolvate. Pastrate fara edit:
  toate helpers/state au consumatori; traversarile similare includ elementul
  si layout correction diferit, deci nu reprezinta acelasi contract.
- Parintele a confruntat testele complete, frame coordinator, lifecycle calls
  si docs coordinator/binding cu explorarea Luna read-only. Nu se schimba
  transform arithmetic, reparenting, render-only invalidation ori ownership.
- Gate reutilizat din input-affected.trx: 13 LayoutMotionCoordinatorTests
  passed, 0 failed/skip; gate afectat 327 passed. Build curent, sursa nemodificata.
  Filtru reproductibil: FullyQualifiedName~LayoutMotionCoordinatorTests, acelasi
  proiect Release --no-build --no-restore ca lotul 10. Include continuitate,
  detach, same-id scope, cross-parent si zero measure/arrange la tick; bugetul
  allocation este testul existent (warm-up32, 16 captures), nu optimizare noua.
- Review parinte: sursa/diff/rezultatele brute verificate, git diff --check
  trecut. Fara API/docs/render edit sau gate pixel nou; suita completa ramane
  pentru final. Explorare read-only separata; fara build/test activ sau commit/push.

### Lot 12 — Motion/Presence: intrare, iesire si retained lifecycle (2026-10-01)

- Scope: toate cele trei fisiere citite complet; 87/126 rezolvate. Parintele
  a confruntat testele complete si collection add/remove cu mapping-ul Luna.
- Pastrate: parametrul newOwner este validat, nu eliminat drept nefolosit;
  Enter/Exit au aceeasi secventa scurta cancel/dispose, dar lifetime si callback
  ownership distinct. Un nou base/helper pentru aceasta secventa nu reduce
  suficient complexitatea. Enum-ul public Entering ramane compatibil.
- Gate curent input-affected.trx: 10 PresenceCoordinatorTests passed, fara
  failure/skip; gate afectat 327 passed. Filtru reproductibil
  FullyQualifiedName~PresenceCoordinatorTests, proiectul core Release
  --no-build --no-restore. Cod neschimbat; evidence reutilizata fara rerun inutil.
  Include retained attach/renderqueue, HitTestService, re-add/cancel, 100 cicluri
  lifecycle si coexistenta Layout. Nu revendica input nativ sau pixel parity.
- Review parinte: state/helpers/call paths si TRX confruntate, fara diff C#;
  git diff --check trecut. Fara API/docs/renderer edit ori performance claim.
  Suita completa finala ramane deschisa; explorare dependinte read-only,
  niciun build/test activ; fara commit/push.

### Lot 13 — Motion/Properties: registry, binding si staged writeback (2026-10-01)

- Scope: zece fisiere citite complet; 97/126 rezolvate. C# pastrat:
  pending samples/write snapshot, identity keys, source precedence si callbacks
  au responsabilitati active. Nu se elimina metadata publica si nu se unifica
  snapshot-ul RemoveBindings/CancelBindings pentru o simpla asemanare de sintaxa.
- Parintele a confruntat testele complete, markup default lookup si tranzactii
  cu mapping-ul Luna. Corectate doua pagini canonice: tranzactiile folosesc
  spec-ul tranzactiei, nu registry default/category/safety flags; lista built-in
  nu contine ColorMixer. Pipeline-ul binding clasifica UiProperty direct.
  Aceasta este documentarea sursei existente, nu verdict ca politica este bug
  ori schimbare de comportament. Workflow docs complet si manifest unic/existent
  verificat; exemplele nemodificate nu au fost recompilate aici.
- Gate curent input-affected.trx: 19 MotionPropertyBindingTests si opt
  MotionTransactionTests passed, fara failures/skips; afectat 327 passed.
  Filtru reproductibil FullyQualifiedName~MotionPropertyBindingTests sau
  FullyQualifiedName~MotionTransactionTests, proiect core Release --no-build
  --no-restore. Docs-only nu invalideaza build-ul. Include source masking,
  synchronous completion, hidden/detach cancel, same-system guard si counters.
- Review parinte: sursa, diff docs si TRX actual confruntate; git diff --check
  trecut. Fara API/runtime/renderer edit ori pretentii performance; niciun job
  activ. Suita completa numai la final; fara commit/push.

### Lot 14 — Motion/Specs: sampling, easing si compozitie temporala (2026-10-01)

- Scope: toate cele 20 fisiere citite complet; 117/126 rezolvate. Parintele
  a confruntat toate MotionSpec/Easing/RepeatTimeline tests cu mapping-ul Luna.
- C# pastrat: helpers/state active. Cele doua StaticSampler mici sunt identice,
  dar un nou tip/layer comun pentru reduced-motion shortcuts nu justifica aici
  maintenance cost si runtime type schimbat. Standard/Emphasized raman instante
  distincte chiar daca au aceleasi puncte; enum-urile publice raman compatibile.
  Nu se schimba integrarea spring, decay bounds, duplicate offsets sau parity.
- Gate curent input-affected.trx: 72 cazuri MotionSpecTests/EasingTests/
  MotionRepeatTimelineTests passed, fara failures/skips; afectat 327 passed.
  Filtru reproductibil FullyQualifiedName~MotionSpecTests|FullyQualifiedName~EasingTests|FullyQualifiedName~MotionRepeatTimelineTests,
  core Release --no-build --no-restore. Nicio productie editata dupa build.
- Docs Easings corectat constructorul MotionSpecContext cu toate cele patru
  argumente obligatorii. Workflow docs, pagina completa si manifest entry
  unic/existent verificate. Ambele exemple compilate ca metode separate cu
  csc SDK10.0.400, ref-pack net8.0/8.0.30 si Cerneala Release curent: exit0;
  nu executate. Log artifacts/motion-cleanup/specs/doc-examples-compile.log;
  .cs/.dll temporare eliminate exact, in afara production globs.
- Review parinte: sursa/diff/docs si TRX/compiler actual confruntate, git diff
  --check trecut. Fara API/runtime/render/performance claim; niciun job activ.
  Suita completa numai la final; fara commit/push.

### Lot 15 — Motion/States: token-uri si snapshot-uri publice (2026-10-01)

- Scope: cinci fisiere citite complet; 122/126 rezolvate. Pastrate deliberat:
  dictionary/helpers au utilizari; public StateRule/controller/snapshot fara
  consumatori in repo nu demonstreaza absenta consumatorilor externi.
- Parintele a confruntat theme/AspectEngine resolution, testele relevante si
  paginile Tokens/controller cu explorarea Luna. ThemeMotionTokens este ruta
  efectiva de theme; nu se reinventeaza ownership-ul MotionSystem.Tokens.
- Gate: dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release
  --no-build --no-restore --filter 'FullyQualifiedName~AspectEngineTests'
  --logger 'trx;LogFileName=states-aspect.trx' --results-directory
  artifacts/motion-cleanup/states: 15 passed, 0 failed/skipped, exit0.
  Include token lookup si tranzitii styling. StateRule/Capture nu au teste
  directe gasite; sursa lor simpla/API este nemodificata, nu se revendica
  exercitarea lor de aceasta suita ori input nativ pentru direct flags.
- Review parinte: sursa/call paths/docs si rezultat brut confruntate, fara
  C#/docs diff; git diff --check trecut. Niciun job/explorator activ. Fara
  API/render edit, performance claim sau commit/push; suita completa la final.

### Lot 16 — Motion/Transactions: scopes si mutation dispatch (2026-10-01)

- Scope: ultimele patru fisiere citite complet; 126/126 fisiere rezolvate.
  Parintele a confruntat testele complete si RootPropertyMutationObserver/
  MotionSystem wrappers cu mapping-ul Luna read-only. C# pastrat: stack order,
  disposal recovery si typed adapters au consumatori si contracte distincte.
- Cast seamana cu MotionSpec/ValueMixer, dar exceptiile si mesajele sunt
  diferite; nu se unifica doar acceptarea valorilor. Nu se adauga disposed
  guards sau schimbari ale spec/registry policy necerute de cleanup.
- Gate curent input-affected.trx: opt MotionTransactionTests passed, fara
  failures/skips; afectat 327 passed. Filtru reproductibil
  FullyQualifiedName~MotionTransactionTests, core Release --no-build --no-restore.
  Build curent, nicio productie editata dupa lotul 9. Include nested spec,
  out-of-order scope recovery, disable si Animation-source exclusion.
- Review parinte: sursa/call paths si TRX confruntate, git diff --check trecut.
  Fara API/docs/render edit sau performance claim; niciun job/explorator activ.
  Inventarul este bifat, dar finalizarea ramane conditionata de suita completa
  si review-ul final. Fara commit/push.

### Acceptare finala — repository complet (2026-10-01)

- 126/126 fisiere rezolvate in 16 loturi, niciun item nebifat. Sase fisiere
  productie au cleanup intern justificat; celelalte 120 pastrate deliberat.
  Un test permanent de caracterizare adaugat; 17 pagini API canonice corectate.
  Niciun API public/protected, algoritm de sampling/render, prag, golden sau
  test expectation schimbat. Nu este o afirmatie ca toate bug-urile Motion
  posibile ori toate exemplele din documentatia existenta au fost auditate.
- Windows, SDK10.0.400, Release. Tools/scripts/Cerneala.BuildInputs.Tests.ps1
  trecut: legitimate inputs pastrate, zero archived inputs in cele patru
  categorii; fixture temporar unic curatat de finally cu verificarea path-ului.
  dotnet build Cerneala.slnx -c Release --no-restore: exit0, zero erori, un
  warning CS0108 in InvestorReel preexistent (InkScene.Drop ascunde UIElement.Drop).
  Fara suprimare, excludere proiect ori reparatie a codului strain.
- Suita completa rulata o singura data, numai dupa bifarea inventarului:
  dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1 --logger trx
  --results-directory artifacts/motion-cleanup/final-repository/test-results,
  exit0. CERNEALA_SDL_NATIVE_TESTS=1; CERNEALA_SDL_CONFORMANCE_ARTIFACTS indica
  path-ul absolut artifacts/motion-cleanup/final-repository/full-suite-captures.
  Serializarea pastreaza desktop-ul comun fara competitie intre proiecte.

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

- Numele/outcome-urile brute TRX, nu doar exit0, verificate. Toate baseline-urile
  selectate se regasesc Passed: core Motion/markup327, SourceGen614, Language71.
  Caracterizarea noua DragControllerRejectsNullElement este Passed in run-ul
  final. Nu au aparut failures care sa necesite reparatii dupa suita finala.
- Cele sapte excluderi nu sunt teste trecute: alpha occlusion content1/3/6/7
  dezactivate prin cererea anterioara a userului; foreground ownership/input/
  lifetime si Language CPU P95 dezactivate de maintainer la 2026-09-21;
  Village cadence este opt-in pe masina Windows de referinta, neactivat aici.
  Aceste conditii raman nerezolvate/nevalidate; nu s-au schimbat skips/assertions.
- Conformance actual: 133/133 cazuri passed; 133 rapoarte si 399 PNG negoale
  (reference/SDL/heatmap). Toate sub pragurile existente MAE<=1/P99<=10/max<=49;
  maxime observate 0.3704/10/41. Capturile sunt exclusiv application-owned
  Window.SaveScreenshot. NativeRenderSurface3D27/27 passed in aceeasi suita;
  nu se revendica parity exhaustiv pe resurse/parametri/platforme.
- Review parinte final: sursa curenta, intregul diff propriu, interactiuni si
  jurnal confruntate; git diff --check exit0. Manifest JSON valid; toate cele
  17 pagini editate au intrari unice/existente si Definition/Examples/Remarks,
  fara placeholders. Manifest fara edit, nefiind pagini noi sau renames.
  17 exemple recompilate in loturile7/8/14, nu executate; fisiere temporare
  .cs/.dll eliminate. FileTree regenerat: Unchanged. API/generator contracts
  neschimbate, full build si SourceGen suite trecute; fara shader/catalog edit
  care sa impuna noi gate-uri offline ori smoke-uri adiacente.
- Toate exploratoarele si job-urile proprii terminale. Dovezi brute/loguri si
  rezumate JSON in artifacts/motion-cleanup/final-repository, ignorate de Git.
  Cerneala.slnx, FileTree foreign hunks, InvestorReel si Markup_Authoring_Findings
  pastrate; fara commit/push. Automatizarea Windows este completa; validarea
  umana, alte platforme/RID si masuratori noi de performanta nu sunt revendicate.
