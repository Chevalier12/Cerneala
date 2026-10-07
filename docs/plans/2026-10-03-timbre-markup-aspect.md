# Plan: Timbre — SoundClip, @sound și Aspect reactiv

> Data: 2026-10-07
> Status: finalizat
> Dependențe: [core/runtime](2026-10-03-timbre-core-runtime.md); [decodare](2026-10-03-timbre-decoding-streaming.md) și [SDL3](2026-10-03-timbre-sdl3-backend.md) pentru dogfood/native acceptare
> Scop: aceeași redare C# prin .crn, declanșată explicit de evenimente sau activarea unei condiții, fără autoplay și fără al doilea resolver Aspect.

## 1. Puncte de integrare și limite

Inspectează UiMarkupResourceEmitter și UiMarkupMotionResolver pentru integrarea resurselor. SoundClip nu preia TargetType sau restricția unei rădăcini Motion pentru lanțul DSP.

Verifică parsarea @on și colectarea activărilor în UiMarkupDirectiveParser.MotionTriggers, UiMarkupReactiveEmitter și GeneratedMarkupConditions. Reutilizarea evaluării condițiilor nu transferă lifecycle-ul vizual asupra sunetului.

Inventariază Language, SourceGen, LSP, VS grammar și PreviewHost; nu presupune că parserele/binderii existenți consumă deja un singur model semantic. Contractul audio trebuie să fie comun.

Protejează factory-urile și partial-urile paired din UiMarkupGenerator/UiMarkupUserControlGenerator și helperii GeneratedMarkup. Harness-ul de compilare a output-ului nu substituie execuția consumerului generat.

## 2. Contract nou și owneri

Se implementează sintaxa aprobată în [index](2026-10-03-timbre.md#2-sintaxa-aprobată-ca-țintă-încă-neimplementată): SoundClip resource, Source/Volume/Loop, `@parameter`, `@modifier`, `@sound $Clip(args) [as Handle];`, `@handle`, `@cancel`, `@pause Handle;`, `@resume Handle;`, `@seek Handle to 30s;`, în `@on` și `@when/@if`, generic în orice `{control}.Aspect`/`<Aspect>` aplicabil, inclusiv Scene2D.

Declarația nu pornește reader/device/voice doar prin creare sau referire. Literal paths sunt date runtime cu rezolvare aprobată, nu obligația compilerului să deschidă fișiere audio. Nu se introduce @sound block pe Button cu semantici implicite.

Integrare aprobată ca graniță; forma fizică este delegată: contract audio syntax/schema/binding cu owner build-time unic în Language/shared artifact aprobat, consumat de SourceGen pentru lowering tipat. Nu adăugăm două definiții independente ale validității SoundClip și nu migrăm întregul Motion/Prism ca muncă adiacentă. Language rămâne build-time; core UI nu depinde de el.

AspectEngine rămâne resolverul valorilor. Observațiile condițiilor rămân unice; activările audio sunt sidecar-uri deținute de un scope audio per instanță concretă, inclusiv template/item occurrence. Hide nu este detach și nu trebuie să retrigger-eze sunetul la revenirea vizuală dacă ramura a rămas activă. Înlocuirea Aspect/template anulează numai scope-ul retras.

Într-un body mixt, acțiunile pornesc în ordinea sursei: @sound leagă identitatea/override-urile înainte de Motion-ul următor, fără wait până la EOF. Mai multe execuții Motion continuă să necesite compoziție explicită; @sound nu devine copil MotionExecutionNode doar ca să încapă în parserul vechi. Orchestrarea @sound în @sequence/@parallel nu este parte din primul contract aprobat.

### Lowering către API-ul C# Timbre

Contractul C# din [core §2.1–2.2](2026-10-03-timbre-core-runtime.md) este prerechizitul, nu un API derivat separat în generator. În forma țintă:

| Markup | Rezultat C# aprobat, semnături mecanice formalizate în core |
| --- | --- |
| `<SoundClip>` + Source/Volume/Loop | Definiție SoundClip identică celei construibile manual; nu control vizual și nu execuție audio la declarare. |
| `@parameter` + `@modifier` | Descriptori tipați și recipe LowPass/Delay în ordinea sursei; constantele/defaults și unitățile se convertesc conform catalogului comun. |
| `@handle Playback;` | Slot SoundHandle al instanței audio scope; identificatorul markup nu este obiectul SoundPlayback. |
| `@sound $Clip(args);` | Operația Play a scope-ului, cu snapshot inițial validat, fără handle și cu overlap. |
| `@sound ... as Playback;` | Aceeași operație Play cu slotul local; rezultatul are identitate stabilă înaintea acțiunii următoare, chiar dacă sursa este pending. |
| `@cancel Playback;` | Cancel pe ocupantul slotului, nu anularea tuturor instanțelor unui SoundClip. |
| `@pause Playback;` / `@resume Playback;` | Pause/Resume pe ocupantul capturat; slot gol no-op, Pause Pending împiedică primul PCM, fără pause-device comun. |
| `@seek Playback to 30s;` | SeekAsync absolut în timpul sursei, request nonblocking pe identitatea capturată, latest-request wins; nu așteaptă acțiunea următoare. |
| `Loop = true;` / `@sound $Clip(Loop = true);` | Default/override boolean immutable la start pentru loop întreg, fără schimbare dinamică, regiuni/count/crossfade. |
| `$self.sound.Playback.Parametru` | Captură a ocupantului și descriptorului tipat pentru bridge Motion audio; nu lookup/retarget la fiecare tick. Livrat de planul Motion dependent. |

Codul poate folosi helpers GeneratedMarkup pentru attach, subscriptions, resource resolution și scope retirement; acestea delegă la operațiile core, fără validare DSP/mixer/decoder duplicat. SoundClip este resursă runtime reutilizabilă conform contractului core; nu copiem automat strategia compiler-only/inline de la MotionClip. Nu impunem câmp public named pentru fiecare resursă sau acces C# printr-o proprietate inventată: lookup-ul tipat și scope/shadowing existente sunt aprobate, accesibilitatea/helpers se formalizează mecanic în etapa 0. Resource replacement nu mută definiția redărilor existente.

Toate formele existente de emission afectate trebuie inventariate: factory, paired UserControl/Window/Application și content/template occurrences. Nu schimbăm contractele lor de construcție. Output-ul .g.cs trebuie să compileze într-un consumer normal, fără SDL/audio device la build și fără acces intern acordat numai testelor. Nu este obligatoriu ca textul generat să fie identic cu exemplul fluent manual; este obligatoriu ca operațiile și runtime-ul să fie aceleași.

## 3. Inventarul consumatorilor de migrat aditiv

| Consumator actual | Integrare necesară / comportament păstrat |
| --- | --- |
| `Cerneala.Language/Syntax/Embedded/DirectiveSyntaxParser.cs`, MotionSyntaxParser; `Features/CernealaLanguageFacts.cs` | SoundClip/@sound/@pause/@resume/@seek/Loop/modifier grammar și recovery; keyword/semicolon facts audio comune, fără schimbarea gramaticii Motion valide |
| `Semantics/CernealaSemanticModel.Scopes.cs`, `MotionPrism.cs`, `MotionPrism.Motion.cs` | nou tip de resursă, referințe/parametri/handles/actions tipate și diagnostice; nu trata clipul ca control/property assignment arbitrar |
| `Cerneala.SourceGen/UiMarkupResourceEmitter.cs`, `UiMarkupDirectiveParser*.cs`, `UiMarkupAspectReader.cs` | dispatch/resurse și heterogeneous actions în locurile aprobate; păstrează @default assignments și Motion composition constraints |
| `UiMarkupMotionResolver.cs`, `UiMarkupMotionActivationEmitter.cs`, `UiMarkupReactiveEmitter.cs` | păstrează Motion existent; lower audio prin semantic contract propriu comun, event order și activări reactive unice |
| `UI/Markup/GeneratedMarkupConditions.cs`, `GeneratedMarkupMotion.cs` | audio sidecar/scope; legacy visual calls păstrează lifecycle/documented cancellation |
| `CernealaCompletionService*.cs`, navigation/structure și semantic symbols | resource refs, args/types, params, handles/cancel/pause/resume/seek/Loop, source paths și action context completion/navigation |
| `LanguageServer/Features/DiagnosticService.cs`, workspace snapshots/completion | SoundClip standalone fallback și diagnostics/completion/parity pe același model |
| `VisualStudio/Grammars/cerneala.tmLanguage.json`, VS package | SoundClip/@sound/@pause/@resume/@seek/Loop/@modifier highlighting și handle/resource scopes; artefactul packaged este verificat |
| `PreviewHost/PreviewCompiler.cs`, PreviewMarkupHotReload | unsaved AdditionalFile compiles; modificarea textului directivei nu este interpretată ca attribute-only fast reload; preview default-disabled/enable-explicit, fără success fals și fără restaurarea ocupanților la hot reload |

Scope: fișierele și referințele de verificat din aceste proiecte, testele Motion/Prism/semantic/editor și fixtures existente. Nu se pretinde inventarul tuturor utilizatorilor externi. Înaintea modificării unei funcții comune se reenumeră callerii acelei funcții pe snapshot-ul curent.

## 4. Fișiere estimate

- **Noi:** model/parser/binder audio în Language și lowering Sound în SourceGen, plus runtime helpers/scope în `UI/Timbre/` / `UI/Markup/`, exact split înghețat în etapa 0.
- **Existente:** consumatorii din tabel, actual AdditionalFiles/catalog build inputs numai dacă schema comună aprobată le cere.
- **Teste noi:** UiMarkupGeneratorSoundTests, SoundSemanticTests, SoundAspectIntegrationTests și corpus pozitiv/negativ audio; numele sunt artefacte planificate.
- **Teste existente:** MotionClip/Parameter/Composition/Handle generator, MotionPrismSemanticTests, EmbeddedSyntaxTests, CompletionTests, NavigationTests, StructureTests, LSP protocol, CernealaGrammarTests, PreviewHostTests.
- **Consumer:** fixture .crn/C# în smoke Timbre și un exemplu real de aplicație după gate; fără rescrierea consumerilor existenți.
- **Docs:** ghid conceptual planificat `docs/timbre-guide.md`, actualizare `docs/CernealaMarkupGuide.md`, paginile API canonice și manifest. Ghidul conceptual nu înlocuiește API docs.

## 5. Etapele de implementare

### Etapa 0 — matrice syntax/activation și compatibilitate

- [x] Îngheață corpusul pozitiv pentru declarații/acțiuni audio și body mixt cu Motion vizual existent; negative: clip/parametru/handle necunoscut, duplicate, tip/range greșit, modifier necunoscut, @sound/@pause/@resume/@seek fără semicolon, Loop non-boolean, seek unit/type/negative invalid, context greșit, Source lipsă și conflicting names/scopes. Cazurile cu target `.sound.` din exemplul complet se implementează și acceptă numai în planul Motion dependent, nu blochează acest plan.
- [x] Formalizează handle typing aprobat: mai multe clipuri permise, Volume comun, parametri custom numai cu schema compatibilă în toate clipurile posibile; conflict audio/Motion vizual pentru același identificator diagnostic în același scope. Cancel/transport capturează ocupantul, slot gol no-op; numele rezervate/mechanical diagnostics respectă grammar existentă.
- [x] Definește action AST/binding/emission și common catalog consumer boundaries; inventariază current callers ai parsării/resource/event/reactive emission modificate. Nu introduce două parsere Sound independente sau un resolver Aspect secundar.
- [x] Îngheață maparea din §2 către API-ul core aprobat, accessibility și helpers lifecycle; inventariază factory/paired roots/resource emission/template contexts. Nu generează apeluri SDL/decoder și nu presupune că numele unui @handle este o instanță SoundPlayback.
- [x] Definește hidden-condition observation și activation identity: initially true sub control ascuns pornește, reeval true nu repetă, false→true pornește chiar fără render, hide→show cu true stabil nu repetă. Distinge Window.Hide permis să suspende UI pump de hidden-control în root activ.
- [x] Aplică pending/action-order/erori din index/core: eroare sincronă oprește corpul, asincronă nu inversează acțiunile deja pornite; Seek request nu blochează UI/acțiunea următoare, latest request supersedes, captură stable identity inclusiv replacement. Fără catch-and-ignore/failure ca success.
- [x] Rulează GREEN corpus Motion/Prism/Aspect/editor existent. Adaugă cel mai mic test generator/language care cere @sound în context aprobat și confirmă RED pentru diagnostic-ul current unsupported directive/resource, nu fixture/compiler/runtime SDL lipsă.

**Gate etapa 0** — dovezi: [evidence/2026-10-03-timbre-markup-stage0](evidence/2026-10-03-timbre-markup-stage0/README.md)

- [x] Syntax/actions/handles/lifecycle și inventarul schimbării sunt aprobate; există RED valid pentru capabilitatea de markup lipsă și caracterizare legacy GREEN. Producția audio nu începe prin modificarea așteptărilor vechi.

### Etapa 1 — SoundClip și binding/lowering comun

- [x] Implementează resource recognition/schema/param/modifier parsing și binding audio comune, SourceGen lowering în aceleași definiții C# ca API-ul core. SoundClip nu impune TargetType de control.
- [x] Testează Source simplu, Volume, Loop default/override immutable, params care alimentează unul/mai mulți modifiers, declaration order, override defaults/types și resource scoping/shadowing; valorile instance nu mută recipe-ul comun.
- [x] Pentru fiecare caz valid/invalid, compară Language și SourceGen diagnostics, spans/types și generated compilation; valid-only corpus nu este suficient.
- [x] Testează referirea unui SoundClip în două elemente/ferestre și redefinirea unei resurse conform politicii aprobate; compile/declare/ref nu produce playback sau I/O eager ascuns.
- [x] Adaugă fixtures de compilare pentru definiții/resurse în factory și partial paired, folosind harness-ul existent RunGenerator/RunPairedGenerator: SoundClip simplu/modificat, float descriptors/defaults și constante. Verifică simbolurile bind-uite ale constructorilor/apelurilor emise contra API-ului core, nu numai Contains pe text; protejează roots/templates inventariate fără rescrierea lor. Emission/execuția acțiunilor și override-urile de start aparțin etapei 2.

**Gate etapa 1** — dovezi: [evidence/2026-10-03-timbre-markup-stage1](evidence/2026-10-03-timbre-markup-stage1/README.md)

- [x] Clip/arguments/catalog sunt tipate și identice între hosts; generated code compilează fără reflection/dynamic runtime și fără dispozitiv audio necesar în build.
- [x] Factory/paired partial folosesc suprafața core aprobată sau helpers care delegă la ea; nu există motor audio, semantici de parametri sau stări de redare alternative în SourceGen.

### Etapa 2 — evenimente, reactive și scope runtime

- [x] Înainte de behavior changes, adaugă regressions faithful prin tree/runtime real pentru initial true, reentry, reevaluation stable, false no stop, overlap și keyed replacement/cancel.
- [x] Creează înaintea gate-ului de paritate un harness de emit/load/execute al consumerului generat, cu clock/sink/source-reader deterministe și cleanup al assembly-ului/ownerilor. Fixture-urile care apelează direct GeneratedMarkup nu substituie execuția clasei generate.
- [x] Înainte de transport markup funcțional, confirmă RED pe consumer generat executat, apoi implementează și verifică Pause/Resume/Seek/Loop prin @on/reactive: slot gol no-op, pending pause/seek, paused quota64, terminal rejection, latest-seek/cancel/replacement și două clip schemas. Nici callback/task stale nu mută noul ocupant; seek resetDSP/loop preserveDSP corespund core.
- [x] Implementează @sound/cancel/pause/resume/seek și typed handle dispatch, cu scope per element/Aspect/template, ordonare @sound→Motion vizual existent și cleanup corect la detach/replacement; nu folosi sesiunea vizuală neschimbată drept owner audio. Binding-ul Motion audio este livrat separat de planul dependent.
- [x] Extinde fixtures de factory/paired partial cu Play/Loop, override-uri, SoundHandle/cancel/Pause/Resume/SeekAsync și scopes ale template-urilor; confirmă generated compilation și binding către operațiile API-ului core înainte de execuția runtime. Compile-only nu închide gate-ul de paritate.
- [x] Testează două occurrences ale aceluiași Aspect/template/ItemsControl: cancel local nu traversează identitățile; template swap și Aspect replacement nu lasă old callbacks/voices active.
- [x] Testează control/ancestor Hidden și Collapsed, schimbări de condiție în stare ascunsă și hide→show: transport/observațiile audio continuă fără replay, în root care este încă pompat; visual Motion își păstrează anularea existentă.
- [x] Testează 100 attach/detach/reattach, cancellation pending/open failure și reactive reentrancy cu bariere; toate redările fără handle sunt și ele owner-scoped și eliberate la detach.
- [x] Verifică click/hover și routed events prin Servo/input user-like în fixture real; direct property changes sunt numai testele de condition source, nu probe de routing.
- [x] Măsoară idle frame fără schimbări: zero start-uri audio suplimentare, zero reevaluări necerute și zero invalidări measure/arrange/render cauzate exclusiv de audio control.
- [x] Compară C# manual și factory/partial .crn executat pentru aceeași sursă, schema, block partitions și acțiuni: primul PCM/override-uri, ordinea lanțului, param updates, overlap, replacement, pending cancel/failure, Pause/Resume/Seek/Loop/Position și detach/zero start la declarare. Compară trace-ul de operații și PCM cu toleranțele core; identitățile numerice diferite între rulări nu sunt criteriu fals de egalitate. Înlocuirea unui slot nu permite unei referințe vechi să anuleze ocupantul nou.

**Gate etapa 2** — dovezi: [evidence/2026-10-03-timbre-markup-stage2](evidence/2026-10-03-timbre-markup-stage2/README.md)

- [x] Contractele audio/events/reactive/lifecycle sunt GREEN și legacy visual/cascade/routing rămân GREEN. Hidden-control nu este testat numai prin chemarea unui getter audio.
- [x] Paritatea C#/.crn este demonstrată pe cod generat executat, nu numai prin compilare sau bridge fake; ambele căi folosesc motorul și validarea reale cu sink/reader deterministe.

### Etapa 3 — tooling și preview

- [x] Extinde completion/signature help/navigation/structure pentru SoundClip/@sound/@pause/@resume/@seek/Loop/@modifier/@parameter/handle/cancel și args/types/units; publică aceleași diagnostics în LSP standalone și workspace.
- [x] Actualizează VS grammar keyword/resource/label scopes și packaged assets; testează corpusul actual plus audio, semicolons/recovery și stale/saved AdditionalFiles.
- [x] Testează preview unsaved .crn prin compilation reală, attribute edits și directive body edit requiring recompile; verifică audio disabled implicit/enable explicit/stare vizibilă, fără false success; hot reload anulează scope-urile vechi fără restore și aplică reactive rules noi. Nu porni automat audio în editor printr-un fallback neaprobat.
- [x] Rulează suitele Language/SourceGen/LSP/VS/PreviewHost și native consumer build pe desktop; după orice .crn/AdditionalFile/project edit reîncarcă workspace-ul semantic înaintea validării simbolurilor generate.

**Gate etapa 3** — dovezi: [evidence/2026-10-03-timbre-markup-stage3](evidence/2026-10-03-timbre-markup-stage3/README.md)

- [x] Editorul și generatorul recunosc aceeași limbă audio, inclusiv negative/recovery; previews/builds nu au side effects de audio necontractate.

### Etapa 4 — docs și dogfood

- [x] Scrie ghidul conceptual și integrarea markup folosind numai API/grammar implementate; documentează no autoplay, overlap/replace, initial true/reentry/false no stop, Pause/Resume/SeekAsync/Loop, latest-seek/action-order/errors și preview.
- [x] Folosește writing-api-documentation pentru noile API public/protected și comportamentele helpers comune schimbate; sincronizează manifestul și verifică exemplele compilate.
- [x] Rulează native Timbre smoke pe Windows cu WAV/MP3/Vorbis/Opus, preload/streaming/Pause/Resume/Seek/Loop, user-like events, reactive și multiwindow; PCM tap/output și resource counts sunt probe, nu afirmații despre ce a auzit utilizatorul.
- [x] Rulează proiectele afectate integral, manifest, strict ApiCompat și full solution; review pe sursa/diff-ul final și cleanup înainte de checkpoint.

**Gate etapa 4** — dovezi: [evidence/2026-10-03-timbre-markup-stage4](evidence/2026-10-03-timbre-markup-stage4/README.md)

- [x] C# și .crn sunt aceeași cale runtime; syntax/editor/docs/native și compatibility gates sunt verificate. Motion audio detaliat se acceptă numai prin planul dependent.

## 6. Comenzi și definiția de gata

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~UiMarkupGeneratorTests|FullyQualifiedName~UiMarkupGeneratorSound"
dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj -c Release --filter "FullyQualifiedName~SoundSemantic|FullyQualifiedName~MotionPrismSemanticTests|FullyQualifiedName~EmbeddedSyntaxTests|FullyQualifiedName~CompletionTests"
dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj -c Release
dotnet test .\tests\Cerneala.Tests.VisualStudio\Cerneala.Tests.VisualStudio.csproj -c Release
dotnet test .\tests\Cerneala.Tests.PreviewHost\Cerneala.Tests.PreviewHost.csproj -c Release
```

Probele native/runtime non-Windows sunt N/A conform index §7; publish/asset checks șase RIDs și Windows gates rămân required.

Filtrele Sound sunt planificate; verifică la execuție că au selectat teste, nu zero tests. Fișierele existente UiMarkupGeneratorMotion* declară clasa parțială `UiMarkupGeneratorTests`, nu clase denumite după fișiere. Rulează ulterior proiectele întregi și comenzile comune din index.

- [x] SoundClip/@sound/cancel/pause/resume/seek/Loop/handles și reactivitatea explicită sunt implementate și verificate, fără implicit button playback sau state partajat.
- [x] Output-ul generat pentru factory și partial paired compilează și rulează pe API-ul Timbre comun, cu paritate C# manual/markup pentru PCM, ordine, slot versus instanță și lifecycle.
- [x] Cascade/resolver, visual lifecycle/composition, routing și consumerii existenți sunt compatibili.
- [x] Corpus pozitiv/negativ între hosts, tooling, preview policy, native dogfood, API/docs/manifest și full suite sunt închise cu dovezi.
