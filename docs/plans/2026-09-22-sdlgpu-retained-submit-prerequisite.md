# Plan: publicarea retained la submit-ul command bufferului SDL_GPU

> Data: 2026-09-22
> Status: toate etapele acceptate; prerequisite încheiat cu waiver explicit Linux/macOS
> Dependență: [fundația GPU acceptată](2026-09-21-rendersurface3d-gpu-foundation.md), cu waiver-ul de platformă din [index](2026-09-21-rendersurface3d-plan-index.md).
> Deblochează: [RenderSurface3D, etapa 0](2026-09-21-rendersurface3d-control.md), care rămâne neîncheiată.
> Scop: un rezultat raster retained nu este reutilizabil între buffers dacă producerea lui nu a fost submitted cu succes; fără invalidarea lucrului deja submitted.

## 1. Decizie, dovezi și limite

Utilizatorul a aprobat crearea și executarea acestui prerequisite după reproducerea blocantă din etapa 0 a controlului. Este un defect al infrastructurii existente, nu implementare 3D. Fundația deja acceptată nu se rescrie, iar workerul etapei 0 a controlului rămâne disponibil pentru reluare după închiderea prerequisite-ului.

| Clasificare | Fapt / decizie | Dovadă |
| --- | --- | --- |
| Fapt reprodus | O captură Prism a unor primitive 2D este reutilizată în frame-ul următor după failed submit; `CaptureCount` rămâne 0, deși captura trebuie refăcută. | `SdlGpuFrameTransactionCharacterizationTests.CancelledBufferCannotPublishAParentPrismCaptureForTheRecoveryFrame`; rerulare părinte 6 pass / 1 RED / 0 skips, `artifacts/rendersurface3d/control-stage0/parent-audit/parent-transaction-final-audit.trx`. |
| Fapt din sursă | `SdlGpuPrismExecutor.Execute` promovează lease-ul în timpul execuției; `TryAcquireRetained` poate opri traversarea dependențelor. | `Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismExecutor.cs`, `Execute`, `AcquireRequiredRetainedHits`. |
| Fapt din sursă | Pool-ul și cache-ul Prism sunt per device; lease-urile au identitate proprie, pin count și window identity. | `SdlGpuPrismDeviceResources` citit integral: `Promote`, `TryAcquireRetained`, `Release`, invalidări, bugete și `Dispose`. |
| Fapt din sursă | Sesiunea poate trimite primul buffer înainte de achiziționarea celui de recovery; `CompleteFrame(false)` face submit fără present. | `SdlGpuWindowGraphicsSession` și factory citite integral; `PresentFrame`, `SubmitActiveCommandBuffer`, `CompleteFrame`, `RenderPngCore`, `Dispose`. |
| Fapt din sursă | Suprafața 2D marchează `FrameVersion` și `RetainedEntries` înainte de submit; brush captures folosesc aceeași rută raster. | `SdlGpuDrawingBackend.AddRenderSurface`, `RenderRecordedSurfaceFrame`, `SdlGpuRenderSurfaceState`; partial-ul `BrushCaptures` citit integral. |
| Ipoteză de verificat | Publicarea anticipată poate permite și unui raster 2D/brush ori unui input upload anulat să supraviețuiască recuperării. | Matricea de descoperire din etapa 0; aceste defecte nu sunt declarate reproduse doar din sursă. |
| Decizie | Fără cache bypass, invalidare globală, schimbarea sensului `CompleteFrame(false)`, API public GPU sau refactor general Prism. | Aprobarea prerequisite-ului și limitele indexului. |
| Necunoscut concret | Contractul exact al handle-ului după submit eșuat în Graphix.Native 3.4.16-graphix.6 trebuie reconciliat cu documentația SDL înaintea fixului. | `Cerneala.Platforms.Sdl3.csproj`; adaptorul `NativeSdlApi` delegă direct. SDL documentează handle invalid după submit și interzice cancel după achiziția swapchain. |

Surse native primare consultate: [SDL_SubmitGPUCommandBuffer](https://wiki.libsdl.org/SDL3/SDL_SubmitGPUCommandBuffer) și [SDL_CancelGPUCommandBuffer](https://wiki.libsdl.org/SDL3/SDL_CancelGPUCommandBuffer). Reproducerea inițială surprinde comportamentul actual al fake-ului și sesiunii (failed submit apoi cancel), nu stabilește că acel cancel este legal nativ. Dacă versiunea folosită confirmă consumarea handle-ului chiar la failed submit, testul de caracterizare a secvenței trebuie reclasificat și înlocuit cu RED pentru contractul nativ; se păstrează istoricul și invariantul de nereutilizare, nu apelul invalid.

Nu sunt autorizate în acest prerequisite: RenderSurface3D, shaders noi, schimbări de rig, algoritmi noi de damage, redesign de resource cache general, schimbări de public API, modificări de toleranțe/golden-uri, commit/push/dispatch. Extinderea necesară la cache-urile de input este autorizată prin decizia ulterioară de mai jos, numai pentru încălcări demonstrate ale aceluiași contract; nu se ascunde sub o invalidare globală.

## 2. Contractul de implementat

### Checkpoint de descoperire — 2026-09-22

Workerul `/root/retained_submit_stage_0` este completed/quiescent, disponibil pentru aceeași etapă; etapa 0 rămâne nebifată și neacceptată. Auditul părintelui a verificat delta celor două fișiere de teste, contextul ownerilor și contractul nativ exact, apoi a cerut două reparații ale observațiilor de test. Nu s-a modificat production, Graphix, pachete sau API public.

- Contractul versiunii Graphix folosite consumă handle-ul la submit inclusiv când rezultatul este false. Cancel ulterior este invalid. Caracterizarea istorică a acelui cancel nu reprezenta contractul nativ și a fost înlocuită cu RED separat, fără a condiționa RED-ul Prism de apelul invalid.
- Sunt reproduse publicarea Prism după failed submit, pending cross-session, rasterele OnDemand/brush sub Prism și reutilizarea imaginii cold al cărei upload aparținea unui submit eșuat. Ultimul caz implică independent `SdlGpuDrawingResources.textures` și activează stop condition-ul de scope. Text atlas/alte cache-uri de input nu sunt încă defecte demonstrate.
- Rerularea independentă finală: **18 teste / 12 pass / 6 RED intenționate / 0 skips**, exit 1. Manifestul surselor: **10/10 hash-uri corespund**. Dovezi: `artifacts/rendersurface3d/cache-submit-prerequisite/stage0/parent-audit/parent-final-discovery-audit.trx`, `parent-final-discovery-audit.log`, `source-state/final-source-manifest.json`; raport complet `stage0-report.md`.
- Baseline worker: 68 pass/10 native-gated skips; SDL fără clasa deliberat RED: 391 pass/191 skips, înaintea reparațiilor limitate la observatorii clasei. Aceste rezultate nu sunt full suite GREEN și nici verificare nativă a recuperării.

Decizia necesară înainte de etapa 1: extinderea aceluiași prerequisite la publicarea/recuperarea cache-ului de imagini în funcție de outcome-ul bufferului și la investigarea inputurilor GPU necesare, cu migrarea altor cache-uri numai dacă aceeași încălcare este demonstrată prin RED. Această extindere este propusă, **nu aprobată încă**; nu autorizează redesign general sau schimbări Graphix. Waiver-ul Linux/macOS rămâne neschimbat.

**Decizie ulterioară explicită, 2026-09-22:** utilizatorul aprobă eliminarea blocajelor necesare terminării planului RenderSurface3D („blanket approval”) și cere continuarea fără reconfirmări. Aceasta rezolvă stop condition-ul de scope de mai sus: același prerequisite include cache-ul de imagini confirmat RED și investigarea cache-urilor de input GPU necesare; ceilalți owneri se migrează numai dacă RED demonstrează aceeași încălcare. Etapa 0 reia inventarul semantic și probele de input, iar etapa 1 repară atomic closure-ul confirmat. Aprobarea nu înseamnă gate-uri trecute sau waive-uri noi și nu alege implicit între contracte publice/arhitecturi material diferite. Graphix local se modifică numai dacă dovezile stabilesc că acolo este invariantul încălcat; până acum nu există asemenea dovadă.

### Owner și stări

Sesiunea deține identitatea și rezultatul fiecărui command buffer, inclusiv recovery. Cache-ul deține lease-urile/resursele și eligibilitatea reutilizării. Aceste responsabilități nu se inversează: controlul UI nu află detalii SDL, iar device-ul nu ține o cameră/stare de frame globală.

Rezultatul complet produs într-un buffer este **pending**, reutilizabil numai în ordinea aceluiași buffer/sesiuni. Devine **submitted**, reutilizabil între buffers, numai după submit reușit. Failure/abandon elimină eligibilitatea pending a acelui buffer și a compozițiilor care depind de el. Submit nu înseamnă present sau GPU fence completion; retirement păstrează regulile reale de lifetime.

Un rezultat parțial al unei operații care aruncă nu devine complet doar fiindcă `RenderPngCore` trimite bufferul în `finally`. Rezultatele independente complete pot supraviețui dacă au fost trimise; nu se pretinde rollback al GPU work deja submitted.

Publicarea deferred nu are voie să piardă pin-ul lease-ului în `Execute.finally`, să permită pool-ului să-l suprascrie înainte de replay/submit sau să publice din nou un owner invalidat/disposed în timp ce era pending. Identitățile native pot fi reutilizate: tracking-ul nu confundă buffers din sesiuni/generații diferite. Două ferestre intercalate nu pot consuma pending-ul celeilalte sau anula rezultatele ei submitted.

Se păstrează limitele existente de cache/buget, invalidările owner/key și cleanup idempotent. Nicio promisiune nouă de zero allocations. Scopul determinist este conservarea reuse-ului valid și eliminarea reuse-ului invalid, nu flush continuu.

### Integrare și consumatori

Inventarul semantic de redactare este în `artifacts/rendersurface3d/cache-submit-prerequisite/planning/`. Workerul reconfirmă inventarul înainte de modificarea fiecărui contract comun.

| Contract / apelanți | Migrare sau conservare | Verificare |
| --- | --- | --- |
| `SdlGpuPrismDeviceResources.Promote` — executor + cele 9 apeluri din `PrismSurfaceOwnershipTests` | Publicare compatibilă cu outcome-ul bufferului; testele resource-only păstrează ownership/pins/bugete, nu forțează publicare GPU prematură. Semnătura finală internă se stabilește după etapa 0. | Pending replay, submitted reuse, abandon, invalidare pending, lease copiat/dispose repetat. |
| `TryAcquireRetained` — executor + `PrismSurfaceOwnershipTests` | Lookup distinge pending-ul bufferului curent de rezultate submitted; nu expune pending cross-window. | Identități, dependency closure și două sesiuni. |
| `Invalidate`, `InvalidateStaleOwnerEntries` — backend, executor, teste și `PrismRetainedCacheBenchmarkRunner` | Păstrează invalidarea explicită; nu reînvie intrări invalidate la commit. | Owner/all/stale-key înainte/după submit, pin și evicție. |
| `RenderRecordedSurfaceFrame` — `RenderSurfaceFrame` și `ResolveTileBrushPaint` | Dacă RED confirmă aceeași publicare raster nesigură, migrare atomică în etapa 1 a celor două call-site-uri; fără modificarea producerului UI sau a algoritmului damage. | 2D OnDemand, retained entries, brush capture, reraster după abandon, reuse după succes. |
| `CompleteFrame(bool)` — `Present`, `RenderPngCore`, `WindowApplicationRuntime`, benchmarks și fixture-uri | Fără semnătură/sens public nou. Outcome-ul intern per buffer se comunică înainte ca handle-ul să fie uitat; cleanup nu decide cu un singur bool întregul frame. | Succes/failure, no-present, screenshot exception, recovery split. |
| `CapturePresentedFrame` și native submit/fence/cancel | Readback nu publică rezultate de desen; păstrează separarea, dar respectă contractul de consumare a handle-ului dacă gate-ul nativ dovedește necesitatea corecției. | Failure fence/readback și cleanup; nicio modificare a pixel normalization. |
| Factory/device owner, `EndFrame`, `Dispose`, pool/retirement | Nu schimbă partajarea device-ului; închid numai achizițiile și pending-ul deținut. | Dispose în frame activ, a doua fereastră încă funcțională, zero lease-uri pending după cleanup. |
| UI, Tetris, probe externe | Public API și redraw/layout semantics neschimbate. | Core/SDL/Tetris suites și ApiCompat strict. |

Inventarul `CompleteFrame` din control-stage0 include 88 linii de declarații/apeluri la momentul capturii; numărul nu este prag viitor. Query-ul ulterior pe implementare găsește apelanți concreți, iar cel pe interfață întoarce gol deși runtime-ul are apel: rezultatul gol nu este dovadă de absență. Fallback-ul documentat și caller table-ul se reconfirmă. Query-ul `Invalidate` ambiguu a fost rezolvat prin symbol-id: rezultatul complet este `planning/Invalidate.symbol-refs.json`, inclusiv apelanții suplimentari din testele de budget/dependency/ownership.

Closure de input confirmat: ``SdlGpuDrawingResources.cs`` deține atât dictionary-ul sampled textures (byte/half-vector), cât și entries/pages/dirty uploads ale atlasului. Caller table-ul complet și migrarea fiecăruia sunt în ``stage0/approval-continuation/input-cache-closure.md`` și inventarul JSON asociat. Loaderul și geometry cache sunt CPU-only; geometry arena reîncarcă la fiecare recording și rămâne GREEN; pipeline/sampler/shader/storage caches nu publică aici conținut dependent de submit.

Fișiere estimate: `SdlGpuDrawingResources.cs` (inclusiv tipurile atlas), `SdlGpuWindowGraphicsSession.cs`, `SdlGpuPrismDeviceResources.cs`, `SdlGpuPrismExecutor.cs`, `SdlGpuDrawingBackend.cs`, `SdlGpuDrawingBackend.BrushCaptures.cs` numai dacă gate-ul îl implică; un helper intern per-buffer numai dacă reduce duplicarea reală; `FakeSdlApi.cs`, testul de tranzacții existent, `PrismSurfaceOwnershipTests.cs`, `SdlGpuSurfaceRetainedTests.cs`, teste brush/session/dependency afectate; `docs/sdl-desktop-backend.md`. Nu se creează abstracții doar pentru inventar.

## 3. Etape

### Etapa 0 — baseline, contract nativ și closure de reproducere

- [x] Salvează starea dirty exactă și patch-ul local al etapei, inclusiv fișiere untracked; reconfirmă fundația și RED-ul permanent fără a rescrie munca control-stage0. Rulează/citește FileTree.
- [x] Reconfirmă owner cone și toți apelanții contractelor din tabel. Rezolvă contractul submit/fence/cancel pentru versiunea Graphix efectivă din package docs/source; arhivează sursa și diferența față de caracterizarea existentă. Fără încercarea unui use-after-submit pe GPU real.
- [x] Reexecută cele șapte caracterizări existente. Adaugă înaintea production RED/characterization pentru aceeași scenă în același buffer, submit reușit urmat de recovery eșuat, anulare fără submit, invalidare pending, două sesiuni intercalate și dispose activ. Distinge lipsa observabilității de un defect de fixture.
- [x] Caracterizează cu conținut 2D existent suprafața OnDemand și brush captures sub părinte Prism: înregistrare/render parțial eșuat, failed submit/abandon, apoi același conținut/versiune și frame valid. Identifică exact care retained keys/versions trebuie invalidate, fără a pretinde că un flag de control repară părintele.
- [x] Verifică prin probe minime recuperarea inputurilor cold upload necesare capturilor după abandon/failed submit și izolarea pending între sesiuni. Include cache-ul de imagini confirmat RED; inventariază semantic loaderul/text atlas/alte cache-uri de input necesare și adaugă RED sau caracterizare GREEN pentru ownerii relevanți. Arhivează call-site-uri, migrarea fiecăruia și observatorii. Extinderea necesară este aprobată explicit; nu implementa production în descoperire și nu migra cache-uri pe simplă suspiciune.
- [x] Arhivează baseline pentru reuse valid, pins/handle-uri/retirement și allocations existente relevante; stabilește sursele exacte ale contoarelor și scenariile înainte de fix. Rulează GREEN-urile relevante și clasele RED separat, cu număr exact de teste și failure reasons.

**Gate etapa 0**

- [x] Ownerul și contractul nativ sunt stabilite; fiecare RED este un invariant intenționat, nu compilare/fake greșit. Matricea și lista exactă de consumatori de migrat sunt în evidence; orice extindere independentă este rezolvată explicit. Această etapă de descoperire poate închide cu RED-uri arhivate, nu pretinde full suite GREEN și nu este livrarea fixului.

Checkpoint de acceptare părinte, 2026-09-22: audit al testului complet, ownerului de resurse/atlas, inventarului semantic și dovezilor. Rerulare independentă finală **25 total / 14 pass / 11 RED intenționate / 0 skips**; source/binary/TRX **7/7 hash-uri** corespund. Dovezi: `stage0/parent-audit/parent-expanded-final.trx`, `stage0/parent-expanded-final.log`, `stage0/approval-continuation/input-cache-closure.md` și `source-state/approval-continuation-source-correspondence.json` sub root-ul de artefacte. Baseline input/lifetime/allocations: 140 pass/16 native-gated skips; SDL fără RED: 391 pass/191 skips. Descoperire acceptată, **nu fix GREEN**. Workerul este completed/quiescent și retired logic, fără close tool.

### Etapa 1 — commit și abandon per command buffer

Checkpoint de pauză explicită, 2026-09-22: utilizatorul a cerut pauză inclusiv subagentului. Părintele a întrerupt `/root/retained_submit_stage_1` și a oprit arborele de procese verificat al rulării `core-affected-final-green` (wrapper PID 2740, dotnet test PID 18088); verificarea ulterioară nu a găsit procese dotnet/testhost/vstest/MSBuild/csi active. Modificările sunt păstrate, nu revertate. Workerul raportase 25/25 caracterizări și 33/33 teste noi GREEN, dar acestea nu au fost încă auditate independent, iar rularea core întreruptă nu este dovadă finală GREEN. La reluare se recuperează starea din `artifacts/rendersurface3d/cache-submit-prerequisite/stage1/`, se finalizează verificările lipsă și auditul cu același worker; nu se reia descoperirea deja acceptată.

Reluare explicită ulterioară: utilizatorul a actualizat instrucțiunile/planul și cere continuarea. Același worker reia verificările lipsă și predarea dovezilor, fără editări C#/proiect sau navigare sursă nouă în această continuare. Orice failure nou care cere editare se raportează înaintea modificării.

- [x] Reconfirmă RED-urile etapei 0 pe bytes curenți, apoi implementează tracking intern per buffer în sesiune și publicarea pending/submitted a capturilor Prism. Nu folosi `EndFrame` ca sinonim pentru submit și nu trata `CompleteFrame(false)` drept abort.
- [x] Migrează atomic fiecare owner raster confirmat RED în etapa 0: păstrează pending replay în același buffer, închide metadata/versiunea numai pentru rezultate complete și invalidează eligibilitatea celor neconfirmate, inclusiv părinți. Conservă rezultatele primului buffer submitted la eșecul recovery-ului.
- [x] Migrează cache-ul de imagini `SdlGpuDrawingResources.textures` și ceilalți owneri de input confirmați RED în etapa 0 la același outcome per buffer; respectă ownership-ul device/session, uploads pending, reuse după succes, retry după failure/abandon, cleanup și izolarea între sesiuni. Inventarul exact se închide în etapa 0; nu rescrie loaderul sau atlasul dacă invariantul poate fi reparat în ownerul existent.
- [x] Corectează numai încălcările demonstrate ale contractului native-handle din etapa 0, cu RED înainte de production; păstrează propagarea erorii și diferența submit/present/fence. Nu apela cancel pe un handle consumat sau pe un buffer cu swapchain achiziționat dacă contractul nativ interzice.
- [x] Adaugă și trece teste pentru exception înainte/în timpul producerului, pending invalidated/disposed înainte de submit, evicție/buget cu lease-uri pending, două ferestre și cleanup repetat. Un entry invalidat nu este reînviat la commit; un target pending nu este returnat prematur pool-ului.
- [x] Rulează 100 cicluri deterministe succes/failure/abandon/recovery și secvența intercalată a două sesiuni; handle-uri/lease-uri locale revin la baseline după punctul real de cleanup/retirement, iar cache-urile device-owned sunt eliberate la dispose.
- [x] Rulează toate RED-urile acum GREEN, SDL_GPU suite și core Drawing/RenderSurface/Prism/Architecture afectate, apoi Tetris. Investighează fiecare regresie; nu schimba praguri pentru a ascunde fixul.

**Gate etapa 1**

- [x] Reproducerea originală și closure-ul confirmat sunt GREEN; succesul păstrează reuse, failure-ul reface numai rezultatele nevalide, fără contaminare între buffers/sesiuni sau API public nou. Nu rămâne RED deliberat în production test suite.

Checkpoint de acceptare părinte: implementarea și patru reparații de audit au fost verificate pe delta reală, owneri și teste. Text fast-path, pin-uri Prism/input, starea suprafeței per sesiune cu cleanup în ambele direcții, atlas revision/write footprints și dirty entries evicted sunt acoperite de RED→GREEN. Rerulare independentă finală **100/100 pass, 0 skips** (46 tranzacții + 54 ownership); **11/11 hash-uri** finale match. TRX-urile finale verificate independent: SDL 443 pass/191 native opt-in skips; core afectat 1233 pass/2 native skips; Tetris 30/30. Artefacte autoritative: `stage1/parent-repair-4/` și `stage1/parent-audit/parent-stage1-final.trx`. Etapa 1 acceptată, worker completed/quiescent și retired logic; Windows native, API/docs/full suite rămân gate-uri etapei 2. Nu există claim de raster nativ din replay-ul determinist al uploadurilor.

### Etapa 2 — conformance, compatibilitate și predare controlului

- [x] Rulează corpusul existent Windows nativ: pipeline creation, toate Drawing/Prism conformance (133 cazuri la baseline), plus clasele native retained/dependency/ownership afectate. Verifică numărul real executat și zero mandatory skips. Păstrează toleranțele și capturile prin `Window.SaveScreenshot`; fake-ul nu este dovadă de rasterizare.
- [x] Compară aceleași scenarii de allocations/retained work/lifetime cu baseline-ul etapei 0; reuse-ul valid nu provoacă capture/raster nou pe frame-uri neschimbate. Timing CPU/GPU rămâne nepretins dacă nu este măsurat; variația între procese se investighează, nu se declară automat regresie.
- [x] Actualizează `docs/sdl-desktop-backend.md` cu ownership-ul pending/submitted/abandon și limita submit versus completion. Public API trebuie să rămână identic; dacă behavior docs canonice necesită clarificare, folosește `writing-api-documentation` în `docs-site/documentation/classes/` și sincronizează manifestul numai dacă paginile se adaugă/redenumește.
- [x] Rulează ApiCompat strict core/platform/backend față de baseline-ul fundației: `dotnet msbuild .\artifacts\rendersurface3d\baseline\api-compat.proj -t:Compare -v:minimal` (path și comandă reconfirmate). Delta public/protected goală, fără suppression nou. Rulează shader compiler `--verify`.
- [x] Rulează build Release și full suite `Cerneala.slnx`, formatter verification numai pe C# atribuibil prerequisite-ului, diff-check și verificarea hash-urilor surselor. Arhivează TRX/raw logs/config/commands și starea finală exactă; păstrează modificările străine și elimină numai experimentele proprii.
- [x] Predă contractul verificat workerului existent al controlului etapa 0. Nu bifa acea etapă: acesta reia propria matrice/oracle/observabilitate și revalidează integrarea după prerequisite.

**Gate etapa 2**

- [x] Toate gate-urile locale obligatorii trec pe bytes finali; Linux/macOS native sunt **WAIVED / NEEXECUTATE**, nu pass, conform deciziei din index. Nu există job mutabil rămas activ, skip ascuns sau pretinsă validare umană. Numai acum se reia controlul.

## 4. Verificare și artefacte

Root pentru dovezi: `artifacts/rendersurface3d/cache-submit-prerequisite/stage<N>/`. Copiile de surse au extensia `.cs.txt`, nu `.cs` sub artifacts (SDK globs). Baseline-ul original și RED-ul control-stage0 se păstrează ca istoric separat.

Comenzi existente, din repository root, în shell dedicat pentru variabilele native:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter 'FullyQualifiedName~SdlGpuFrameTransactionCharacterizationTests'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~Drawing|FullyQualifiedName~RenderSurface|FullyQualifiedName~Architecture'
dotnet test .\Tetrisish\Tests\Tetris.Tests.csproj -c Release
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~SdlGpuDrawingConformanceTests|FullyQualifiedName~PrismSdlGpuPixelConformanceTests'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter 'FullyQualifiedName~SdlGpuShaderArtifactTests|FullyQualifiedName~PrismRetainedExecutionTests|FullyQualifiedName~PrismRetainedDependencyMigrationTests|FullyQualifiedName~PrismSurfaceOwnershipTests'
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release -- --verify
dotnet build .\Cerneala.slnx -c Release
dotnet test .\Cerneala.slnx -c Release --no-build -m:1
git diff --check
```

## 5. Audit de redactare și definiția de gata

Au fost inspectate session/factory și Prism resource owner complet, executor publication/dependency lookup, backend raster publication și `EndFrame`, partial-ul brush captures complet, adapterul nativ submit/cancel, caracterizările 7 cazuri și testele native retained existente. Consumatorul extern este documentat prin runtime/screenshot/benchmark callers și docs canonice RenderSurface2D. Nu s-a modificat production la redactare. Ultima rerulare părinte a celor 7 teste precede această redactare; etapa 0 o reface, nu revendică testări noi din simpla citire.

Contradicții tratate: commit per frame ar pierde primul submit la recovery; publicarea deferred fără pin ar returna targetul la pool; invalidarea doar Prism ar putea lăsa rasterele părinte clean; succesele native nu sunt dovedite de fake; cancel după failed submit este observație existentă, nu contract nativ validat. Unknown-ul Graphix și closure-ul inputurilor au gate de descoperire înainte de production.

- [x] Capturile/rasterele din closure-ul confirmat sunt reutilizabile numai în bufferul pending corect sau după submit reușit; failure/abandon nu publică rezultat valid.
- [x] Succesul, no-present, recovery split, invalidare/dispose, buget și două sesiuni sunt testate fără flush global și fără pierderea reuse-ului valid.
- [x] Full suite, Windows native conformance, ApiCompat și documentația internă sunt închise cu dovezi finale; waiver-ul Linux/macOS este explicit.
- [x] Prerequisite-ul este acceptat independent; controlul etapa 0 este reluat separat, nu declarat implicit complet.

Checkpoint final de acceptare părinte, 2026-09-22: delta etapei 2 este documentația internă și un hunk exclusiv whitespace în testul de ownership. Audit independent pe raw logs/TRX, diferențe și corespondență: 11/11 surse, 5/5 assemblies și 16/16 TRX hash-matched; documentația finală corespunde manifestului. Rerulare părinte: 100/100 tranzacții/ownership, 0 skips, stage2/trx/parent-final-audit.trx. Full suite: 5616 pass, 0 fail, 194 opt-in skips; Windows native 1 pipeline + 75 retained/dependency/ownership + 133 conformance, fără skips. Build, ApiCompat strict, shader verify și formatter cumulativ trecute. Baseline-urile allocations/reuse/lifetime păstrează toate outcome-urile; nu există claim de timing sau zero allocations nou. Linux/macOS WAIVED / NEEXECUTATE. Worker completed/quiescent, retired logic fără close tool. Contractul este predat separat control-stage0 prin stage2/control-stage0-handoff.md; controlul nu este implicit complet. Rularea întreruptă rămâne exclusă din dovezi.
