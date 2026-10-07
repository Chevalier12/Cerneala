# Plan: Timbre — core runtime, parametri și DSP

> Data: 2026-10-07
> Status: finalizat
> Dependență: contractele comune din [indexul Timbre](2026-10-03-timbre.md)
> Scop: motor C# backend-neutral pentru SoundClip simplu sau modificat, redări independente și lifecycle măsurabil.

## 1. Owneri și integrare de verificat

Verifică sharing-ul Application.Resources între rădăcinile ferestrelor; resursele partajate nu dețin playhead sau memorie Delay mutabilă. Inspectează Application, WindowApplicationRuntime, UiHost și UIRoot înaintea integrării audio.

Păstrează contractele publice IPlatformServices/PlatformServices, inclusiv constructor/deconstruct/equality și implementatorii existenți. Stabilește o integrare aditivă pe inventarul real al consumatorilor, fără a transforma IResourceProvider în service registry.

Verifică ordinea attach/detach și granița dintre OnRenderabilityChanged și Detach. Resource/Prism/Motion/binding/SVG își păstrează politicile proprii; acestea nu se schimbă global pentru audio.

## 2. Arhitectura propusă, nu API existent

Suprafață estimată în `Timbre/`: SoundClip immutable, descriptor de parametru tipat, definiții LowPass/Delay, playback cu identitate stabilă, runtime și output contract. `UI/Timbre/` furnizează adaptorul C# de ownership în acest plan, iar subscriptions/lowering declarativ se adaugă în planul markup; motorul de procesare nu depinde de AspectEngine, DrawingContext, Prism executor sau SDL.

O definiție poate fi partajată; fiecare playback deține valori override, stare DSP, poziție și cititorul sursei. Runtime-ul deține mixajul și comunicarea către output. Output-ul nativ este application/platform-runtime scoped și nu este eliberat de ultimul sample al unui clip. Source assets/preload pot fi partajate numai cu lifetime explicit; streaming readers și starea DSP nu sunt partajate.

Backend-ul consumă audio pregătit și nu evaluează Aspect sau arborele UI. Parametrii sunt validați de schema comună și publicați coerent către procesare; decoder/I/O și callback-ul audio nu execută mutații UI. Callback-urile de completion/error către UI sunt livrate prin owner-thread/Relay, nu direct din callback nativ.

Arhitectura din index: acest motor deține efectele și mixajul tuturor redărilor active și livrează **PCM final mixat** prin output seam. Adaptorul SDL3 core nu instanțiază voices/modifiers și nu deleagă procesarea SDL_mixer. Clipul simplu parcurge aceeași cale fără lanț DSP; nu există motor paralel pentru plain playback. Decoderii existenți sunt selectați în planul dependent, nu implementați de la zero în core.

### 2.1. Suprafața C# cerută și ținta de ergonomie

Extinderea cerută de utilizator: API-ul C# este baza funcțională a Timbre, disponibil fără markup/Aspect, inclusiv pentru codul care controlează o scenă 2D. SourceGen trebuie să emită apeluri către acest API, direct sau prin helpers de lifecycle; nu introduce alt motor audio. Aici „backend C#” înseamnă core/runtime, **nu** adaptorul nativ SDL3.

| Concept C# planificat | Responsabilitate și operații urmărite |
| --- | --- |
| `SoundClip` | Definiție immutable: Source, Volume/Loop/defaults, parametri declarați și lanț LowPass/Delay ordonat. Crearea/referirea nu redă și nu deschide device/reader. |
| `SoundParameter<float>` | Descriptor tipat al unui parametru expus; poate alimenta unul sau mai mulți modificatori. Identitatea/schema nu se reduc la numele textual. Prima livrare nu promite orice `T`. |
| `SoundScope` / variabila `sounds` | Owner al redărilor și sloturilor, conectat la runtime-ul comun. Ținta: `Play(...)`, `CreateHandle()`, teardown al resurselor proprii; tipul SoundScope și accesul element.Sounds/application.Sounds sunt aprobate; standalone primește runtime explicit. |
| `SoundPlayback` | Instanță cu identitate stabilă, disponibilă și pentru start pending. Ținta aprobată: `Volume`, `Set(descriptor, value)`, `Cancel()`, `Pause()`, `Resume()`, `SeekAsync(TimeSpan)`, Position read-only și Duration eventual necunoscută, stare și rezultat final awaitable. Loop este snapshot immutable la start. Nu reprezintă slotul reutilizabil. |
| `SoundHandle` | Slot local unui scope. `Play(..., handle: slot)` înlocuiește numai redarea lui; `slot.Cancel()` țintește ocupantul curent și este no-op dacă slotul este gol. |
| Punte Motion audio | Animează Volume/descriptori pe instanța capturată; este livrată de [planul Motion](2026-10-03-timbre-motion-parameters.md), nu de un engine nou în core. |

Exemplele următoare fixează **forma dorită a utilizării**, nu sunt API existent sau exemple deja compilate. Namespace-ul Cerneala.Timbre, vocabularul/ergonomia/ownership-ul/contractele terminale sunt aprobate în index §1.1. Overload-uri, nullability și tipul configurării se formalizează mecanic în etapa 0; abaterea materială cere oprire, nu alt rezultat mascat ca implementare.

```csharp
var plainSound = new SoundClip("audio/confirm.wav");
var first = sounds.Play(plainSound);
var second = sounds.Play(plainSound); // Overlap: instanțe independente.
first.Cancel(); // Nu anulează second.

var toneCutoff = new SoundParameter<float>(
    name: "ToneCutoff", defaultValue: 1200f);

var filteredSound = new SoundClip(
    source: "audio/confirm.wav",
    parameters: [toneCutoff],
    modifiers: [new LowPass(cutoff: toneCutoff)]);

var slot = sounds.CreateHandle();
var playback = sounds.Play(filteredSound, start =>
{
    start.Volume = 0.2f;
    start.Set(toneCutoff, 800f);
}, handle: slot);

playback.Volume = 0.8f;
playback.Set(toneCutoff, 6000f);
playback.Pause();
playback.Resume();
// Într-o metodă async, fără blocarea UI:
await playback.SeekAsync(TimeSpan.FromSeconds(1));
playback.Cancel(); // Anulează identitatea aceasta, nu un ocupant viitor.
slot.Cancel();     // Anulează ocupantul curent al slotului, dacă există.
```

Configurația de start se validează și se publică înainte de primul PCM și înainte de acțiunea următoare care capturează redarea. Delegate-ul de configurare nu este callback audio și nu poate fi reexecutat de decoder/output. Definiția și valorile altor redări rămân neschimbate. Constructorul unui modifier trebuie să poată reprezenta și o valoare constantă, de exemplu `new LowPass(cutoff: 1200f)`, nu numai un parametru expus.

Loop se configurează prin default-ul immutable al clipului ori `start.Loop = true;`; implicit false, fără setter dinamic. Redarea looping nu încheie rezultatul la fiecare EOF. Fragmentul cu await se compilează într-un consumer async real la gate, nu este API deja disponibil.

Core-ul nu depinde de Language, SourceGen, GeneratedMarkup, UIElement sau SDL. Adaptorul `UI/Timbre/` leagă scope-ul de element/Aspect/template și de schedulerul UI. Codul C# fără Aspect folosește același core, cu owner/disposal și acces la runtime explicite; nu se inventează implicit un singleton global sau o cerință de control vizual fictiv.

### 2.2. Contractul comun pentru C# și cod generat

```text
C# scris de utilizator ──────────────────────┐
.crn → semantic binding → C# generat         ├→ API Timbre → decoder/DSP/mix → output seam → SDL3
                         + adaptor lifecycle┘
```

Schema runtime pentru definiție, descriptor, valoare inițială, validare și actualizare este aceeași. Build-time poate transforma valori/diagnostice în reprezentări statice, dar nu creează a doua implementare a regulilor DSP. Helpers pentru markup gestionează attach/detach, trigger-e, resource lookup și sloturi ale comportamentului; nu dețin un alt mixer, decoder sau motor de parametri.

Core-ul trebuie să poată fi consumat dintr-un assembly de aplicație fără acces la `internal` prin privilegii de test. Nu promitem că partial-ul emite literalmente fluent API-ul: lowering-ul poate apela un bridge generated-markup cu accesibilitate aprobată, care delegă la aceleași operații runtime. Paritatea se probează prin execuție și PCM, nu prin asemănarea textului generat.

### 2.3. Contractul runtime aprobat și formalizarea tehnică

Catalogul de valori, formulele wet/dry, ordinea Volume, formatul float32/stereo/48kHz, bugetele loading/streaming/voices și state/erori sunt canonice în [index §1.1](2026-10-03-timbre.md#11-contracte-și-limitele-autonomiei); pragurile de cost și platformele sunt în index §7. Nu se repropune acel contract la fiecare etapă.

- Surface/ownership: SoundRuntime comun, SoundScope per owner, element.Sounds/application.Sounds; hosted/standalone explicit. Detașat nu poate Play; reattach lifecycle nou. Integrarea hosting este aditivă, fără rupturi PlatformServices/IResourceProvider.
- Pending/Playing/Paused/Completed/Canceled/Failed și rezultat awaitable; primele operații/override-uri se acceptă înainte de primul PCM. Pause Pending nu permite primul PCM până la Resume; Pause păstrează reader/DSP/poziție și consumă slot de voice.
- SeekAsync în timpul sursei, precise decoded PCM, latest pending request wins, acțiunea următoare nu așteaptă; Position nu este DAC. Păstrează paused/active și identitatea, reset DSP numai la seek reușit; Loop păstrează DSP. Resursele/cererile vechi nu publică după cancel/replacement.
- Validarea startului precedă replacement; erorile sincrone păstrează ocupantul vechi. Failure asincron nu îl restaurează. Cancel/dispose sunt idempotente; mutații/transport/animații noi pe terminal sunt respinse.
- Source local/factory seekable, app-base relative, Auto/Preload/Streaming și pregătire async explicită. Cache immutable cu pinning pentru active payloads; fără I/O eager la declarare. La underrun padding contorizat fără salt de conținut; corrupt/truncated/I/O failure nu este EOF reușit.
- Decoder/DSP/mix și output au un singur owner pentru conversie și fiecare coadă; mix final comun. Backend default/lazy și device failure explicit, fără retry automat. PCM deja queued nu este retractabil per voice; nu se închide sau golește output comun la pause/seek/cancel local.
- Algoritmii LowPass/Delay, criteriul numeric al tail-ului în cap-ul 30s, mecanismele de queue/conversie și detaliile service seam sunt delegate pentru alegere/probe înaintea producției. Nu se adaugă API public DSP auxiliar necerut (de exemplu Q/Resonance/Bypass) sub eticheta de implementare internă.

## 3. Fișiere estimate

- **Noi:** `UI/Timbre/` pentru owner adapter C# și `Timbre/` pentru definiții/runtime/DSP/output seam; un catalog de operații și parametri cu owner unic și consumatori core/build-time, forma fizică decisă în etapa 0.
- **Noi:** `tests/Cerneala.Tests.Timbre/Cerneala.Tests.Timbre.csproj` și corpus de teste net8.0; acesta este un artefact viitor, nu comandă existentă acum.
- **Existente, numai dacă seam-ul aprobat le cere:** `UI/Platform/IPlatformServices.cs`, `PlatformServices.cs`, `UI/Hosting/UiHostOptions.cs`, `UiHost.cs`, `UI/Elements/UIRoot.cs`, `UI/Elements/UIElement.cs` pentru accesul aditiv Sounds, `UI/Hosting/Windowing/WindowApplicationRuntime.cs`, `UI/Application.cs`, `Cerneala.csproj`, `Cerneala.slnx`.
- **Teste de compatibilitate:** ServiceRegistrationTests, UiHostPlatformServicesIntegrationTests, ApplicationRuntimeTests, ApplicationResourceIntegrationTests, ElementLifecycleTests și WindowRuntimeTests.
- **API docs:** pagini noi/afectate în `docs-site/documentation/classes/` și manifest.

## 4. Etapele de implementare

### Etapa 0 — contract C#/backend și baseline

- [x] Formalizează semnăturile/nullability/accesibilitatea aprobate în §2.1–2.3 și index: SoundRuntime/Scope, element.Sounds/application.Sounds, hosted standalone explicit, SoundClip/float descriptors, Play/initial Volume+Loop/handle, Cancel/Pause/Resume/SeekAsync/Position/Duration și rezultat final. Compilează exemplele standalone după staging; owner access concret se compilează/execută în etapa1. Lipsa tipurilor nu este RED runtime.
- [x] Îngheață contractul intern output/source-reader și adapter boundary din §2.2; catalogul runtime și reprezentarea build-time au o mapare explicită. Generatorul nu depinde de SDL sau de tipurile bibliotecii decoder.
- [x] Aplică PCM float32/stereo/48kHz aprobat și formalizează unitățile byte/sample/frame și clock-ul de procesare și transferul reader-worker → procesare/mix → output. Decide push/pull, ownership-ul buffers și backpressure cu un singur owner pentru fiecare coadă; DSP/mix rămân în Timbre indiferent de firul pe care rulează. Nu executa fișiere/decoder/UI în callback-ul nativ.
- [x] Definește contractul între PCM produs, queued și consumat: bugetul output ≤40ms, cancel/pause/resume/seek/loop/replacement și publication Motion față de PCM deja mixat/queued, precum și EOF + Delay tail față de completion. Respectă semantica aprobată; nu goli output-ul comun la cancel local și nu marca redarea terminată doar pentru că ultimul bloc a fost produs.
- [x] Formalizează snapshot-ul configurării și validarea înainte de primul PCM: descriptor nedeclarat/al altui clip, tip invalid, callback care aruncă, handle din alt scope sau scope retras. Fixează tranzacția aprobată: invalid sincron păstrează ocupantul, start acceptat îl anulează, failure ulterior nu îl restaurează; ordinea operațiilor nu poate schimba rezultatul. Implementarea și dovada PCM aparțin etapei1.
- [x] Formalizează seam-ul de hosting/servicii aditiv aprobat la application/runtime/root, compatibilitatea IPlatformServices/PlatformServices și ownership-ul disposal inclusiv hosted runtime fără Application. Oprește dacă dovezile cer schimbare materială neaprobată de arhitectură/public API; nu transforma IResourceProvider în service registry.
- [x] Formalizează catalogul aprobat din index §1.1: Volume post-chain 0–1/default1, Cutoff20–20000Hz/default1200, Delay Time1ms–2s/default120ms/Feedback0–0.95/default0.20/Mix0–1/default0.15 wet-dry liniar; float descriptors și sample-rate-domain validation. Alege algoritmii DSP pe surse primare/probe și fixează oracle-urile; fără Q/Resonance/Bypass public necerut sau filtre Prism copiate.
- [x] Rezolvă explicit contractul mix/headroom/clipping, feedback stabil, criteriul/durata tail-ului Delay și deosebirea EOF/completion/cancel. Formalizează respingerea setter/start NaN/Infinity/out-of-range și oprirea numai a animației parametrului la sample invalid, cu ultima valoare validă și diagnostic; nu arunca din callback nativ și nu clamp-ează ascuns. Execuția pe mixer/DSP real și Motion are gates în etapele/planul proprii.
- [x] Formalizează tranzițiile Pending/Playing/Paused/Completed/Canceled/Failed, transportul și rezultatul awaitable aprobate: Pause Pending, Resume fără restart, SeekAsync supersession și cancel, Loop EOF fără completion; identitate imediată, fără blocarea UI. Publicarea stale nu atinge replacement; rezultatul final se închide independent de UI pump, notificările UI pe owner-thread.
- [x] Formalizează Auto/Preload/Streaming/pregătire async și source app-base/factory seekable; aplică Auto1MiB/preload16MiB/cache64MiB/64voices inclusiv paused și pinning cache. Streaming incremental, fără memorie proporțională cu durata și fără plafon numeric implicit obligatoriu; limitele cooperative configurate explicit rămân aplicabile, cu accounting implicit long.MaxValue conform indexului §1.1. Device default lazy/failure și Preview disabled implicit cu enable explicit respectă indexul; nu introdu autoplay sau false success.
- [x] Înregistrează snapshot/worktree, inventariază referințele și implementatorii seam-urilor care chiar vor fi schimbate; păstrează constructorii și callerii existenți ori obține aprobare pentru ruptura exactă. Îngheață baseline binar pentru ApiCompat.
- [x] Creează proiectul de teste net8.0 și un sink/reader determinist de test, cu contoare planned pentru start/pause/resume/seek/loop/cancel/completion, source-position/seek-generation, frames queued/consumed, buffers/cache/voices și owned resources. Verifică observerii separat, fără fake mixer; dovada harness→motor real se închide obligatoriu în etapa1.
- [x] Rulează caracterizările existente relevante și păstrează raw logs/TRX. Sunt GREEN de baseline, nu RED audio.

**Gate etapa 0**

- [x] Contractele aprobate sunt formalizate și reproductibil testabile; alegerile tehnice delegate au dovezi și consumatorii seam-urilor comune sunt acoperiți. Fără gate, nu se implementează runtime funcțional.
- [x] Suprafața poate fi consumată dintr-un assembly C# normal fără Aspect/generator/SDL concret; contractul poate fi lower-uit în cod generat fără privilegii interne sau runtime audio duplicat.
- [x] Staging-ul API nou este distinct de un RED runtime. Testele de compilare pentru contract nou se clasifică separat; lipsa tipurilor sau a unui codec/fișier nu este raportată ca eșec audio runtime.

### Etapa 1 — redări, transport, parametri și anulare

**Delimitarea etapelor:** definițiile LowPass/Delay și validarea parametrilor lor se introduc în etapa 1, dar DSP-ul funcțional rămâne în etapa 2. Până atunci, `Play` cu modificatori aruncă explicit `NotSupportedException` înainte de replacement, păstrând ocupantul existent; nu există bypass tăcut. Etapa 2 elimină această restricție după RED→GREEN pentru DSP real. Aceasta nu reduce gates finale și nu certifică redarea modificată în etapa 1.

**Contract reader:** după zero frames cu `EndOfSource = false`, următorul `ReadAsync` așteaptă asincron date, EOF, eroare sau anulare. Nu se adaugă un seam public de readiness, polling, delay arbitrar sau deschidere prematură a output-ului. Obligația se verifică pe reader/sink și se documentează în API-ul canonic.

- [x] Demonstrează că sink/reader/observerii deterministi observă motorul concret Timbre, nu o implementare fake de Play/mix/transport. Compilează și execută owner access element.Sounds/application.Sounds/hosted; verifică validarea/snapshot/tranzacția configurației înainte de primul PCM.

- [x] Adaugă teste de contract compilabile pentru motorul concret și confirmă eșecul intenționat înaintea implementării comportamentului: două redări ale unui clip au playhead/valori/stare diferite; override-ul nu mută definiția. Include mix PCM plain conform gain/headroom/clipping și cancel/replacement/publication cu queue/consum observabil; fixtures/types lipsă nu constituie RED.
- [x] Înainte de transport funcțional, confirmă RED faithful pe motor/sink pentru Pause/Resume/Pending/terminal și SeekAsync/Loop. Implementează numai după RED; testează păstrarea identității/poziției, pause în pending, paused voice quota, Resume fără restart și no-op idempotent. Tail/DSP pause se acceptă în etapa 2, după existența efectelor reale.
- [x] Implementează definiții immutable și instanțe cu identitate stabilă, parametri tipați, source reader/output seam, mixajul PCM plain și terminal state conform etapei 0. Lanțul LowPass/Delay se implementează numai în etapa 2.
- [x] Implementează adaptorul C# de ownership în UI/Timbre și accesul element.Sounds/application.Sounds/hosted explicit, cu attach/detach/shutdown și scope-uri noi la reattach. Motorul core rămâne fără UIElement/SDL; subscriptions și lowering declarativ rămân în planul markup. Probează C# direct fără Aspect pe lifecycle real.
- [x] Implementează API-ul C# aprobat și ownerul de scope: Play fără handle suprapune; Play cu același SoundHandle înlocuiește numai ocupantul; Cancel pe o referință SoundPlayback veche nu anulează noul ocupant. Testează slot gol, cross-scope, scope disposal și aceeași definiție folosită fără Aspect.
- [x] Implementează SeekAsync/source-position/Duration conform contractului reader comun: absolute/negative/over-duration, latest-request supersession și cancel/replacement în seek, paused-preserving/active-continuing, fără UI block. Readerul PCM determinist probează precise target/discard; adaptoarele comprimate sunt acceptate în planul decoding.
- [x] Implementează Loop boolean snapshot definition/start, fără setter dinamic: PCM reader reia întreaga sursă fără completion per EOF sau pauze introduse; controlează finalizarea/cancel pe corpus finit determinist. Isolation și resursele rămân per playback.
- [x] Probează override-urile inițiale pe primul bloc PCM și starea pending înainte de callback-ul următor; configurarea se evaluează conform contractului, fără retenție/reexecuție pe worker/audio thread. Modificarea colecțiilor furnizate la construire nu poate altera recipe-ul immutable.
- [x] Testează Volume și Set cu descriptor tipat pe două instanțe independente, descriptor incompatibil și instanță terminală; aceeași schemă de validare este folosită la start și la schimbările runtime conform politicilor aprobate.
- [x] Testează Pending → Playing/Paused, pending cancel, Playing/Paused cancel, EOF, failure și dispose repetat; completion/error se livrează o singură dată conform contractului, iar stale callbacks nu afectează noua identitate.
- [x] Testează două scope-uri/ferestre și două redări simultane cu aceeași definiție; un cancel/dispose nu oprește redările altui owner și nu închide output-ul comun.
- [x] Probează mixajul concret, nu un fake mixer: surse PCM plain deterministe independente, partiții neregulate și gain/headroom/clipping conform formulei aprobate. O singură redare păstrează calea comună, iar suprapunerea produce mixul așteptat. Integrarea cu LowPass/Delay se verifică după implementarea lor în etapa 2.
- [x] Cu bariere între producție/queue/consum, testează cancel/pause/resume/seek/loop/replacement și parameter publication în timp ce există PCM queued; old/new voice și un al doilea sunet rămân izolate conform latenței aprobate. Observerul de consumed/drained distinge completion de simplul EOF al readerului.
- [x] Rulează corpusul nou și compatibilitatea serviciilor/resource/lifecycle din etapa 0; repară ownerul invariantului, nu sink-ul testului.

**Gate etapa 1**

- [x] Contract tests și RED→GREEN aplicabile sunt verificate; cancel/failure eliberează cititorul, buffers și abonamentele proprii fără a afecta alt playback. Nu există publicare târzie în instanțe terminale.

### Etapa 2 — LowPass, Delay și procesare pe blocuri

- [x] Înainte de DSP funcțional, adaugă teste cu impuls, DC, sinusoide sub/peste cutoff și canale distincte, cu oracle independent/formule aprobate și toleranțe motivate numeric. Include mixajul plain + LowPass/Delay și două lanțuri independente, fără state partajat sau mixer alternativ; confirmă RED pentru încălcarea așteptată după staging-ul compilabil.
- [x] Implementează LowPass și Delay cu stare per playback; schema/cataloagele nu conțin buffers runtime. Păstrează state între blocuri și izolează canalele conform contractului.
- [x] Compară procesarea aceleiași intrări în bloc unic și partiții neregulate, cu toleranța aprobată: fără reset la frontieră și fără dependență de mărimea blocului.
- [x] Verifică lanțul în ordine, Volume post-chain/override-uri, dry/wet endpoints și actualizări parametrice în timpul redării; pentru Delay verifică timpul/feedback/wet-dry și tail/EOF/cancel exact convenite.
- [x] Confirmă RED pentru DSP reset după seek manual și state preserve după Pause/Resume sau loop natural, apoi implementează pe lanțul real; testează impuls/echo peste granița loop, fără tail separat la fiecare EOF, tail cap30s pe redare non-loop și freeze în pause. Seek respins nu aplică reset DSP.
- [x] Testează rate/channel formats acceptate, date invalide și tranzacția unei schimbări de parametri; fără NaN/Infinity propagat la output și fără fallback/clamp necontractat. (Core canonical stereo/F32/48k și complete-frame/finite contract; formatele sursă/conversia sunt în planul decoding, nu suport codec pretins aici.)

**Gate etapa 2**

- [x] Corpusul PCM și oracle-urile trec pentru sunet simplu și lanțuri; păstrarea stării/per-channel și ordinea sunt demonstrate, nu apreciate auditiv din intuiție.

### Etapa 3 — concurrency, bugete și API/docs

**Scenariu normal de performanță:** fixture-ul consumă independent de mixer și notifică eliberarea capacității suficiente pentru blocul solicitat, fără DSP inline pe consumer. Restanța și padding-ul observat rămân contabilizate; nu se resetează clock-ul, nu se clamp-ează restanța și nu se forțează readiness. Notificarea reținută este control separat de starvation. Matricea este 32 voices/4 streaming/LowPass+Delay, blocuri de 480 frames și 5 procese × (1000 warmup + 10000 măsurate), cu acoperirea completă a costului și alocărilor. Fixture-ul și acoperirea se validează înaintea campaniei; pragurile și regula de variație/repetare se fixează înaintea măsurării conform indexului.

- [x] Adaugă înainte de stress contoare pentru buffers/reader/worker/scope și publicații de parametri; expune prin Detective numai diagnostice justificate, fără a-i transfera ownership audio.
- [x] Rulează scenarii bounded start/pause/resume/seek/loop/cancel/replace/failure concurente cu output-ul push/pull aprobat în etapa 0, pe seed/iterații înghețate; verifică publication order, interdicția UI access pe procesare, resource return-to-baseline și shutdown fără jobs neaccountate.
- [x] Măsoară corpusul de cost din index după warmup, inclusiv multe voices și modificatori, aplică țintele numerice deja aprobate în index §7, fără relaxare post-hoc. Nu optimiza speculativ și nu pretinde zero allocations dintr-un singur bloc.
- [x] Documentează API-ul C#/output/lifetime/erori/range-uri cu writing-api-documentation și exemple compilate; sincronizează manifestul și rulează strict ApiCompat pe baseline-ul etapei 0.
- [x] Adaugă consumer C# extern assembly-ului core pentru exemplele §2.1, fără .crn/Aspect sau acces intern de test; include owner access/disposal, Loop la start, Pause/Resume/SeekAsync/Position/Duration și diferența slot/instanță. Consumerul pentru scenă 2D folosește aceeași cale, fără modificarea API-ului Scene2D sau adăugarea autoplay.
- [x] Rulează întregul proiect Timbre, proiectele core afectate, manifestul și full solution conform indexului. Verifică diff-ul, docs și cleanup înaintea checkpoint-ului.

**Gate etapa 3**

- [x] Core-ul este livrabil prin C# cu sink independent de SDL/decoder concret; noile API-uri sunt documentate și compatibility/performance gates aprobate sunt trecute. Output fizic și codecs nu sunt pretinse aici.
- [x] DSP/mixajul și queue/transport contract folosesc motorul Timbre real, fără SDL_mixer; sink-ul observă ordinea/consumul, nu substituie procesarea sau politica de cancel/completion.

## 5. Comenzi și definiția de gata

Caracterizare existentă, din root:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~ServiceRegistrationTests|FullyQualifiedName~UiHostPlatformServicesIntegrationTests|FullyQualifiedName~ApplicationRuntimeTests|FullyQualifiedName~ApplicationResourceIntegrationTests|FullyQualifiedName~ElementLifecycleTests|FullyQualifiedName~WindowRuntimeTests"
```

După crearea proiectului nou în etapa 0:

```powershell
dotnet test .\tests\Cerneala.Tests.Timbre\Cerneala.Tests.Timbre.csproj -c Release
```

- [x] Definițiile și redările au ownership/semantici aprobate și probe deterministe independente de device.
- [x] Utilizatorul C# poate defini clipuri/modificatori/parametri, porni cu valori inițiale, suprapune/înlocui/anula/pauza/relua/seek/loop și observa Position/Duration/rezultatul fără Aspect; SoundPlayback și SoundHandle nu sunt confundate.
- [x] DSP, cancel/failure/concurrency și bugetele aprobate sunt verificate fără schimbări neaprobate ale UI/resource/services.
- [x] C# API, canonical docs/manifest, compatibility și suitele aplicabile sunt sincronizate; planurile codec/backend/markup/Motion rămân distincte până la gates proprii.
