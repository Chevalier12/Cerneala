# Plan: RenderSurface3D generic și consumatorul de conformance

> Data: 2026-09-21
> Status: încheiat; etapele 0–5 acceptate, cu waiver explicit pentru native Linux/macOS
> Dependență obligatorie: [fundația GPU](2026-09-21-rendersurface3d-gpu-foundation.md), toate etapele încheiate.
> Prerequisite descoperit și aprobat 2026-09-22: [retained/submit](2026-09-22-sdlgpu-retained-submit-prerequisite.md); se închide înainte de reluarea etapei 0. Checkpoint-ul blocat păstrează istoricul anterior aprobării.
> Contract comun: [indexul inițiativei](2026-09-21-rendersurface3d-plan-index.md).

> Amendament utilizator, 2026-09-22: execuțiile native Linux x64/Vulkan și macOS arm64/Metal sunt **WAIVED / NEEXECUTATE** pentru toate etapele acestui plan, conform indexului, secțiunea 6. Referințele de mai jos la runtime/conformance multiplatformă se aplică cu această excepție explicită, nu înseamnă GREEN pe platformele omise. Native Windows, matricea CI livrată, shaderele tuturor formatelor și celelalte verificări rămân obligatorii.

## 1. Contract public propus

Acestea sunt artefacte planificate, nu API-uri existente. Se păstrează namespace-urile actuale: control/frame în `Cerneala.UI.Controls`, datele geometrice și comenzile platform-neutral în `Cerneala.Drawing`. Nu se introduc tipuri SDL în core.

| Element | Contract MVP |
| --- | --- |
| `RenderSurface3D : ContentControl` | Gazdă de randare 3D cu `Content` UI deasupra; background/border/layout/input normale. Nu derivă din `RenderSurface2D`. |
| `ClearColor` | Proprietate UI cu default transparent, invalidează render. Un callback activ fără primitive produce numai clear; fără callback nu există target 3D activ, dar background/border/Content continuă normal. |
| Camera | View `Matrix4x4` și descriptor immutable de proiecție perspective/orthographic. Matricea de proiecție se obține din dimensiunile rasterului curent, fără a muta aspect ratio în aplicație la fiecare resize. |
| Proiecție | FOV vertical în radiani pentru perspective; înălțime world-space pentru orthographic; near/far finite cu `0 < near < far`. Descriptorul nu este o ierarhie extensibilă de pluginuri. |
| `Draw` + `RenderSurface3DFrame` | Callback de înregistrare, writer valabil numai în callback, date de viewport și timp. Complete/abort sunt interne. Fără `Begin/End` GPU public. |
| Primitive | Linie 3D colorată și marker circular centrat într-un punct 3D; variante de batch pentru spanuri de primitive. Transformare model affine `Matrix4x4` explicită per draw/batch; default identity. |
| Dimensiuni vizuale | Grosimea liniei și diametrul markerului sunt DIPs locali pozitivi și finiți, convertiți la pixeli la rasterizare; nu reprezintă cilindri/sfere în world-space. |
| Redraw | `OnDemand` implicit pentru editor, `Continuous` explicit; `InvalidateFrame()` și schimbarea camerei cer render, nu measure/arrange. Enum 3D separat; enum-ul public 2D nu se redenumește. |
| Conversii | World → root și root → world ray, prin același snapshot de cameră și aceeași geometrie viewport ca randarea. Variante `Try` pentru clipping/transform neinversabil/viewport gol. |
| Input | Evenimentele/capture/focus existente; controlul nu consumă automat mouse-ul pentru orbit. Controllerul aplicației alege bindingurile și politica de picking. |

Nu se adaugă API de mesh/triunghiuri/texturi în acest MVP. Triunghiurile folosite intern la rasterizarea liniilor/markerelor nu sunt un mesh editor public.

Scheletul, pose-urile, controllerul orbit/pan/zoom, selecția și rotația demonstrativă există exclusiv în fixture-uri de teste/smoke/benchmark. Nu intră în assembly-urile livrate ale frameworkului, nici ca implementări interne ale controlului. Eliminarea fixture-ului nu trebuie să lase dependențe sau API-uri de rig în control/executor; o scenă de linii și puncte fără schelet verifică separat această limită.

### Convenții matematice și vizuale

- Coordonate right-handed, Y în sus, camera privește pe -Z în view-space. `System.Numerics` row-vector: `local * model * view * projection`; shaderul respectă aceeași ordine și packing explicit, verificat cu o matrice asimetrică, nu doar identity.
- Clip x/y în `[-w,+w]`, z în `[0,w]`; fereastra folosește NDC depth `[0,1]`, clear 1 și LessEqual. La egalitate de depth, ultima primitivă acoperită câștigă; nu se adaugă offset magic pentru articulații.
- View implicit privește din `(0,0,5)` către origine cu up Y. Proiecția implicită este perspective, FOV vertical `π/3`, near `0.01`, far `1000`. Acestea sunt valori propuse ale API-ului nou, nu un comportament dedus din 2D.
- Raster-size urmează rotunjirea existentă DPI în sus. Proiecția folosește aspectul rasterului efectiv, iar conversia în root include arranged origin, raport pixeli/DIPs și transformurile vizuale ale strămoșilor. Lățimea markerului rămâne constantă în proiecție, nu în world-space; transformarea UI a suprafeței scalează imaginea compusă în mod normal.
- Linia are capete butt; un segment degenerat nu desenează. Markerul este un disc billboard cu depth-ul centrului, nu o sferă. Near/far/side clipping sunt obligatorii; un segment care traversează near plane nu dispare integral și nu explodează după împărțirea la w.
- Geometria este opacă în MVP (`Color.A=255`, valori diferite respinse explicit). ClearColor poate fi transparent; opacitatea UI și Prism se aplică imaginii compuse. Nu se promite transparență 3D sortată sau OIT.
- Valori non-finite, matrice view non-affine sau neinversabilă, model non-affine și descriptor invalid sunt respinse înainte de publicarea frame-ului. Un model singular affine este permis: poate produce primitive degenerate, nu inversează camera.
- Proiecția unui punct în afara frustumului și conversia unei poziții root în afara viewportului întorc false. Raza pornește pe near plane și are direcție normalizată către far plane; aceeași convenție funcționează perspective și orthographic. Picking-ul consumatorului o limitează la intervalul vizibil.
- Conversiile publice folosesc starea cerută actuală; înaintea următorului present imaginea poate fi încă cea anterioară. Nu se promite sincronizare cu scanout-ul. Fixture-ul de input așteaptă frame-ul corespunzător înainte de verificarea selecției.

### Oracle și toleranțe, înaintea primului gate vizual

Fixture-ul matematic independent și măștile pixel se fixează în etapa 0, înaintea shaderelor: poziții proiectate ≤1 pixel fizic față de oracle; delta pe canal ≤1 byte în interiorul opac al scenariilor fără efecte. Marginea AA se analizează separat într-o bandă de 2 pixeli: banda nu este exclusă din verificare, ci verificată pentru coverage, extindere și halouri. Scenele Prism/opacity folosesc contractele și toleranțele corpusului existent, nu pragul pentru culoare opacă nefiltrată. Necunoscutele de coverage primesc caracterizare și justificare înainte de acceptarea metodei; un eșec nu autorizează lărgirea pragurilor.

## 2. Înregistrare, compoziție și lifetime

Decizie explicită utilizator, 2026-09-22: batch-urile cu span gol sunt **no-op**, nu aruncă excepție pentru lipsa primitivelor. Aceasta nu relaxează contractul de lifetime al writerului.

Datele spanurilor sunt consumate/copiate la înregistrare într-un snapshot deținut de framework; mutarea array-ului apelantului după callback nu schimbă retrospectiv cadrul. Comenzile au transformări explicite; nu este necesar un stack 3D de state pentru această livrare.

Suprafața emite o nouă comandă de compoziție `RenderSurface3D` în `DrawCommandList`, cu sursă internă separată și generație capturată. Valoarea enum se adaugă fără renumerotarea kind-urilor existente. Metadata cunoaște rect-ul finit al compoziției, resursa-sursă și identitatea generației; nu parcurge oase. Transform/opacity/Prism operează pe imaginea compusă și păstrează această identitate.

Executorul 3D este intern, per sesiune, fără referințe la modelul de schelet. Camera, vertexurile și depth-ul sunt autentice 3D; controlul nu proiectează scena într-o listă de comenzi `DrawLine` 2D. Rasterizarea liniilor groase/markerelor folosește triunghiuri interne, nu depinde de suportul driverului pentru wide lines sau point size.

Fiecare redraw murdar reface întregul target 3D color+depth. Nu se introduce damage tracking pe oase: schimbarea camerei poate modifica tot viewportul. Un cadru `OnDemand` nemodificat reutilizează targetul, fără callback/upload/pass 3D nou; compoziția UI poate avea în continuare lucru.

Ordinea este: flush 2D părinte, record snapshot, upload prin arena sesiunii, pass 3D izolat, revenire la targetul părinte cu `Load`, rebind complet, compoziție. Begin-ul unui copy/render pass nu poate supraviețui ca binding cache valid al altui pass. Uploadul comun deja închide/reia pass-ul; executorul folosește handle-ul curent după upload.

Starea targetului este per **control–sesiune**, cu owner device pentru handle-uri și retirement. Un generation token capturat înainte de callback nu devine automat versiunea cerută după callback: invalidarea/reintrarea între timp rămâne pending. Snapshotul deja început nu amestecă noua cameră cu geometria versiunii vechi. Invalidarea din callback cere un frame ulterior, nu o buclă de reînregistrare în același frame.

Se disting rezultatul **pending în command buffer** și rezultatul **submitted, reutilizabil între frame-uri**. Replay-ul aceleiași înregistrări, cu aceeași identitate de cameră/raster/generație, poate reutiliza rezultatul pending în acel command buffer; nu reapelează callback-ul pentru fiecare captură/compoziție. Commit-ul privește rezultatele bufferului trimis cu succes, nu simpla ieșire din `EndFrame`. `SdlGpuWindowGraphicsSession.PresentFrame` poate trimite primul buffer și achiziționa altul pentru recovery; eșecul celui de-al doilea nu anulează lucrul deja submitted. Anularea/eșecul unui buffer invalidează rezultatele sale neconfirmate. Submit nu înseamnă GPU fence completion sau present reușit. Sesiunea deține tracking-ul și păstrează regulile existente de retirement, fără listă globală.

Invalidarea numai a targetului 3D nu dovedește recuperarea compoziției retained: un rezultat Prism/capture care îl include nu poate rămâne un hit valid dacă producerea sa a fost anulată. Etapa 0 caracterizează această limită a infrastructurii existente; etapa 3 verifică dependency closure pentru noul conținut. Nu se introduce flush global, dezactivare de cache sau invalidare continuă drept remediu. Dacă rezolvarea cere schimbarea contractului comun de commit/cache pentru consumatorii existenți, se oprește pentru un prerequisite separat aprobat; acesta nu este implicit autorizat ca refactor Prism.

Resize/format/session change pregătește resurse noi exception-safe; eșecul nu marchează targetul vechi ca rezultat valid pentru noul viewport. Detach, ultimul subscriber eliminat, root cleanup și session dispose eliberează achizițiile idempotent. Un callback care detașează controlul nu poate publica un snapshot către o stare deja retrasă. Nu se afișează tacit un frame parțial drept rezultat actual și nu se ascund excepțiile cu retry loop.

Nu există `Scene` și nu se copiază infrastructura 2D de streaming, simulation sau loading. `Content` este UI normal; controlul nu are template parts proprii la care să se aboneze.

## 3. Integrare și fișiere estimate

| Zonă | Fișiere existente / artefacte planificate | Obligație |
| --- | --- | --- |
| API core nou | `UI/Controls/RenderSurface3D.cs`, `RenderSurface3DFrame.cs`, `RenderSurface3DRedrawMode.cs`; tipuri geometrice și snapshot-uri sub `Drawing/` | Fără tipuri SDL, fără API de skeleton. |
| Comandă UI | `Drawing/DrawCommand.cs`, `DrawCommandKind.cs`, `DrawCommandMetadata.cs`, `DrawingContext.cs`, `UI/Rendering/DrawCommandTransform.cs` | Nou kind/payload/factory intern; bounds, resurse și versiune păstrate în transform/copy. |
| Audit copy/analyzer | `Drawing/DrawCommand.Images.cs` (`WithMesh`), `Drawing/DrawState.cs`, `Drawing/Prism/Graph/PrismFrameAnalyzer.cs`, `UI/Rendering/DrawCommandListBuilder.cs` | Copierea de comenzi și cache-ul nu pierd payload/version. Se modifică numai unde noul kind cere integrare demonstrată. |
| Lifecycle | `UI/Elements/UIRoot.cs`, `UI/Controls/RenderSurface2D.cs`, un `IRenderSurfaceResourceOwner` intern nou | Root apelează contractul mic comun pentru 2D/3D; metoda 2D existentă delegă aceluiași cleanup, fără redesign. |
| SDL executor | `SdlGpuDrawingBackend.cs`, nou `SdlGpuRenderSurface3DExecutor.cs`, `SdlGpuWindowGraphicsSession.cs`, `SdlGpuDrawingResources.cs` și eventual un owner lazy intern de resurse 3D | Flush/compoziție, tranzacție de frame, ownership per sesiune/device. |
| Audit commit/caches | `SdlGpuPrismExecutor.Execute`, `SdlGpuPrismDeviceResources`, `SdlGpuDrawingBackend.EndFrame` și partial-ul brush captures; testele sesiunii/retained | Caracterizare înainte de implementare; nicio modificare generală Prism fără RED, ownership stabilit și aprobarea extinderii dacă este necesară. |
| Shadere | Fișiere noi în `Cerneala.Backends.SdlGpu/Gpu/Shaders/`, `Shaders/manifest.json`, `artifacts.json`, `SdlGpuShaderArtifacts.cs`, backend `.csproj` | HLSL, SPIR-V/DXIL/MSL, reflection și embedded resources sincronizate. |
| Tests | Noi `RenderSurface3DTests`, `RenderSurface3DGeometryTests`, `RenderSurface3DFrameTests`, `SdlGpuRenderSurface3DTests`, teste native 3D, fixture comun | Contracte, input, failure/lifetime, pixel evidence. |
| Authoring | Test nou `UiMarkupGeneratorRenderSurface3DTests.cs`, fixtures în testele Language/PreviewHost; generatorul și schema numai dacă descoperirea normală nu ajunge | Instanțiere/Content/events/properties prin sintaxa existentă. |
| Consumer nativ | `tests/Cerneala.SdlGpuSmoke/SmokeOptions.cs`, `App.crn.cs`, `MainWindow.crn.cs`, fixture nou `tests/Shared/RenderSurface3DConformanceFixture.cs`, smoke `.csproj` | Mod nou, determinist, `rendersurface3d`. Nu există `Program.cs` în acel proiect. |
| Tooling/CI | `Tools/scripts/Invoke-SdlGpuSmoke.ps1`, `.github/workflows/desktop-backends.yml` | Parser PowerShell și matricea runtime primesc scenariul după implementarea lui. |
| Măsurători | Nou `benchmarks/Cerneala.Benchmarks/RenderSurface3DProbeRunner.cs` și integrare în `Program.cs` | Probe nativ Windows, JSON cu scope-ul exact al contoarelor și parser testat; nu există încă. |
| Docs | Pagini noi în `docs-site/documentation/classes/`, manifest, `docs/sdl-desktop-backend.md` | API canonic separat de notele interne. |

Call-site-uri și contracte păstrate:

- `UIRoot.ReleaseDrawingResources` este apelat din `SetImageResourceCache` și din două căi ale `WindowApplicationRuntime`. Toate continuă același root cleanup; numai dispatch-ul către suprafețe devine comun.
- `IRenderSurface2DFrameSource` și toate testele/benchmarkurile scene/sprite care îl folosesc rămân neschimbate. Nu se redenumește interfața într-una generică cu metode 3D opționale.
- Kind-ul 2D este consumat de backend, metadata și `DrawCommandTransform`; se adaugă cazul 3D alături, nu se schimbă sensul cazului existent. `WithMesh` rămâne exclusiv pentru comenzile mesh 2D.
- `BeginRenderTarget` este consumat de backend, brush captures, Prism executor și testele sesiunii. Semantica sa de `Load`/`Clear` nu se schimbă; noua rută 3D trebuie să respecte acea semantică.
- `TimeSensitiveRenderInvalidator` continuă să trateze `ITimeSensitiveRenderElement` generic. Excepția sa de traversare pentru `Scene2D` nu se extinde la un scene graph 3D inexistent.
- `UiMarkupGenerator.IsRenderSurfaceSceneElement` rămâne numai pentru `.Scene` 2D. `TetrisGameSurface`, showcase-urile și fixture-urile scene/tile/sprite rămân consumatori 2D nemigrați; full suite și testele Tetris protejează lifecycle-ul și callback-urile lor după introducerea cleanup-ului comun.
- `IDrawingBackend.Render` rămâne neschimbat; backendurile externe nu primesc metode abstracte noi. Kind-ul nou este public și necesită documentarea capabilității: un backend care nu îl implementează trebuie să diagnosticheze explicit utilizarea, nu să pretindă randare reușită. Nu se inventează un renderer fallback.
- `CompleteFrame(bool)` păstrează semnătura și sensul: `false` înseamnă submit fără present, nu abort. Consumatorii existenți — `Present`, `RenderPngCore`, `PrismRetainedCacheBenchmarkRunner`, `TextCharacterizationRunner` și fixture-urile SDL_GPU/Prism — rămân nemigrați. Testele noi nu simulează anularea apelând `CompleteFrame(false)`. `RenderPngCore` finalizează în `finally`, inclusiv după excepția callback-ului: succesul submit-ului nu dovedește că o înregistrare 3D întreruptă este validă.

## 4. Etape de implementare

### Etapa 0 — contract testabil, baseline și harness

Checkpoint părinte (2026-09-22): stop condition reprodus și auditat independent. Cu primitive 2D existente sub Prism, primul submit eșuează și același command buffer este anulat; frame-ul următor reutilizează captura promovată anterior submit-ului, în loc să o reconstruiască (`CaptureCount == 0`). Testul permanent `SdlGpuFrameTransactionCharacterizationTests.CancelledBufferCannotPublishAParentPrismCaptureForTheRecoveryFrame` rămâne RED intenționat. Controlul cu submit reușit dovedește reuse valid; încă cinci caracterizări protejează tranzacțiile, inclusiv primul buffer submitted urmat de recovery buffer cancelled și semantica `CompleteFrame(false)` de submit fără present.

Audit final părinte: 2/2 hash-uri sursă coincid cu manifestul; rerulare independentă Release `--no-build`: **6 pass / 1 RED / 0 skips**. Sursa `SdlGpuPrismExecutor.Execute`, sesiunea și `EndFrame` susțin atribuirea la granița comună command-buffer/publicare retained, nu la viitorul control 3D. Dovada este structurală cu fake SDL, nu pixel/native. Artefacte: `artifacts/rendersurface3d/control-stage0/README.md`, patch-uri locale, manifest și `parent-audit/parent-transaction-final-audit.trx`.

Nu s-a modificat production în această etapă. Au fost modificate numai fake-ul și noul test; restul etapei (contract/signatures, matricea completă 2D, oracle matematic și observabilitate 3D) este neîncheiat. Checklist-ul rămâne deschis. Este necesară aprobarea utilizatorului pentru un plan prerequisite separat al publicării resurselor retained după rezultatul fiecărui command buffer, fără flush global sau invalidarea lucrului deja submitted. Workerul `/root/control_stage_0` rămâne disponibil, completed/quiescent fără joburi; nu este retras ca etapă acceptată.


- [x] Reconfirmă închiderea fundației, starea worktree și inventarul semantic. Arhivează contractul numeric/public de mai sus și semnăturile finale în `artifacts/rendersurface3d/contracts.md`; orice schimbare materială față de acest plan revine la utilizator.
- [x] Adaugă caracterizări GREEN pentru surface 2D + overlay, clip/opacity/Prism, root resource replacement și aceeași fereastră cu două suprafețe. Nu prelua rezultate istorice drept rulare actuală.
- [x] Caracterizează înaintea implementării traseele existente de submit/cancel/recovery și Prism retained: render reușit, submit eșuat, primul buffer submitted urmat de recovery eșuat, `CompleteFrame(false)` și excepție în callback-ul screenshot. Fake-ul înregistrează identitatea bufferului, ordinea submit/cancel și injectează eșecuri distincte. Arhivează inventarul semantic complet al apelanților `CompleteFrame`; nu le schimba sensul.
- [x] Verifică recuperarea unui părinte Prism/capture după un buffer anulat folosind conținutul existent 2D. Dacă testul expune un defect comun, păstrează reproducerea RED și oprește etapa pentru decizia asupra prerequisite-ului; nu eticheta comportamentul curent drept GREEN și nu promite că doar executorul 3D îl repară.
- [x] Pregătește fixture-ul matematic independent: puncte world cunoscute, cameră asimetrică, linii la depth diferit care se intersectează în ecran, segment care traversează near plane; rezultatele așteptate se calculează fără helperul de producție testat. Îngheață toleranțele și măștile din secțiunea 1 înaintea primului test nativ 3D.
- [x] Adaugă observabilitate internă structurată pentru frame recordings, passes/uploads/draws 3D și target create/retire/live. Integrează resetul/aggregarea cu frame-ul existent, fără API public de profiler.
- [x] Rulează verificările focalizate ale etapei.

**Gate etapa 0**

- [x] Caracterizarea 2D și infrastructura de failure injection sunt GREEN. Riscul cache/abort este rezolvat sau etapa rămâne blocată explicit; instrumentarea observă bufferul relevant și nu confundă fake SDL cu rasterizarea reală. Nu rămân teste de prezență 3D/markup intenționat roșii între etape.

Checkpoint de reluare părinte, 2026-09-22: prerequisite-ul retained/submit este acceptat integral, cu dovezi finale în `artifacts/rendersurface3d/cache-submit-prerequisite/stage2/`. Același `/root/control_stage_0` a primit reluarea și handoff-ul contractului; RED-ul istoric de mai sus este rezolvat, nu șters din istoric. Workerul finalizează numai matricea/oracle/observabilitatea și verificările propriei etape 0. Checklist-ul controlului rămâne deschis până la audit separat.

Acceptare independentă părinte, 2026-09-22: etapa 0 închisă după două reparații ale oracle-ului. Citite delta diagnostics/backend, toate testele noi, fixture-ul nativ și oracle-ul reutilizabil; măștile au aplicabilitate explicită fractional/pixel-aligned, nu impun AA parțial pentru dreptunghi perfect aliniat și nu pretind acoperire pentru scene clipate/ocludate. API shape din `artifacts/rendersurface3d/contracts.md` acceptată pentru etapa 1, fără API implementat acum. Empty span no-op este decizia utilizatorului. 9/9 surse + 4/4 binare + 9/9 TRX istorice/finale hash-match; rerulare independentă finală 59/59 fără skips. SDL final 456 pass/194 opt-in skips, native Windows 22/22 și core 11/11 păstrate pentru surse neafectate; formatter și ApiCompat trecute. Dovezi `control-stage0/resume-after-prerequisite/parent-final-source-trx-audit.json`, `parent-stage0-final.trx`. Inventarul apelanților este textual/direct-source din cauza ștergerii tooling-ului, nu dovadă semantică pretinsă. Worker completed/quiescent, retired logic fără close tool. Nicio dovadă de raster 3D sau control public implementat la această etapă.

### Etapa 1 — contracte matematice, recorder și invalidare

Excepție explicită utilizator, 2026-09-22: „Da, consemnează excepția și continuă după verificări.” Se acceptă exclusiv abaterea istorică a workerului care a introdus primele teste comportamentale după logica inițială. Aceste rulări GREEN nu sunt redenumite RED; dovezile retrospective sunt etichetate distinct. Auditul independent, verificările comportamentale și RED înainte de fix pentru defectele noi rămân obligatorii. Excepția nu înseamnă acceptarea etapei și nu se extinde la etapele următoare.

- [x] Adaugă întâi RED compile-safe de prezență pentru control/proprietăți/frame/kind; confirmă lipsa API-ului, apoi introdu declarațiile minime pentru testele comportamentale. Aceste RED-uri devin GREEN în această etapă; testele markup apar numai în etapa 4.
- [x] Înaintea logicii, adaugă RED comportamental pentru compunerea model/view/projection, proiecție și ray round-trip perspective/orthographic, DPI 1/1.25/2, arranged offset și transformuri UI imbricate.
- [x] Implementează contractele platform-neutral și validarea din secțiunea 1. Matricea/proiecția și viewportul sunt capturate o singură dată per înregistrare; helperii publici și shader uniforms folosesc aceeași convenție.
- [x] Adaugă RED și implementare pentru lifetime writer după complete/abort, span copiat, batch gol, culori non-opace respinse, dimensiuni invalide, matrice invalidă și overflow de cantitate/bytes.
- [x] Adaugă RED și implementare pentru `OnDemand`, `Continuous`, abonare/dezabonare `Draw`, camera schimbată și `InvalidateFrame` în timpul callback-ului. O invalidare nouă nu este înghițită prin publicarea versiunii curente după callback; camera/viewportul înregistrării începute rămân coerente și nu se reapelează recursiv callback-ul pentru a consuma noua invalidare.
- [x] Introdu controlul și comanda de compoziție, metadata/dependency/retained identity și transform/opacity mapping. Păstrează valorile numerice existente din `DrawCommandKind`; verifică switch-urile exhaustive.
- [x] Testează managed că schimbarea camerei schimbă identitatea de conținut văzută de analiza/cache-ul Prism al părintelui, iar schimbarea numai a poziției/opacității UI păstrează identitatea 3D dacă rasterul/camera nu s-au schimbat. Reuse-ul GPU și imaginea efectivă se verifică la etapele 2–3.
- [x] Rulează testele core, de drawing state și de invalidare.

**Gate etapa 1**

- [x] Toate testele managed ale contractului trec, inclusiv niciun measure/arrange suplimentar pentru simpla rotire a camerei într-un layout stabil.
- [x] Acesta este gate managed, nu dovadă de viewport randat; backendul nu ignoră tacit comanda 3D încă neimplementată.

Acceptare independentă părinte, 2026-09-23: etapa 1 închisă după audit source/API/docs și reparații RED→GREEN pentru transform singular, ray extrem, matrice derivată invalidă și output default la overflow. Excepția istorică test-first de mai sus este aplicată explicit itemilor afectați; bifarea lor nu rescrie cronologia. Snapshot-ul coerent, zero measure/arrange, callback lifetime și integrarea reală a cheilor Prism sunt verificate managed. 26/26 surse/docs, 4/4 binare și 10/10 TRX verificate prin hash în `control-stage1/final-repair/parent-source-trx-audit.json`. Rerulări independente: core focused 50/50 fără skips, SDL reconstruit 457 pass/194 native opt-in skips, `parent-stage1-core-final.trx` și `parent-stage1-sdl-final.trx`; broader core 988 pass/2 native opt-in skips. Formatter verify/docs/manifest/diff-check exit 0. ApiCompat strict exit 1 exclusiv pentru cele 9 tipuri și un enum member aditive aprobate, zero diagnostic neaditiv, fără suppression; nu este pretins exit 0. Aceasta nu dovedește raster 3D: backendul refuză explicit kind-ul până la etapa 2. Worker completed/quiescent, retired logic fără close tool; build servers închise după verificarea părintelui.

### Etapa 2 — executor GPU, primitive și integrare shader

Decizie explicită utilizator, 2026-09-23 — „Varianta 1”: RenderSurface3D necesită un sample count MSAA comun color+depth de minimum 2x; dacă există numai 1x comun (sau niciun mod compatibil), ruta 3D refuză explicit cu NotSupportedException. Nu există fallback 3D cu margini fără antialias. Comportamentul și selecția sample-count pentru 2D rămân neschimbate. Contractul se implementează după RED și se documentează canonic în etapa 2; nu waive-uiește alte gate-uri.

- [x] Adaugă RED pentru ruta backend a noului kind: target izolat color+depth, pipeline 3D, matrices upload, flush 2D înainte, `Load` și rebind la întoarcere. Failure-ul este lipsa rutei, nu lipsa artefactelor necreate.
- [x] Implementează executorul intern și payload-ul GPU propriu; linii/markere sunt rasterizate cu triunghiuri interne. GPU face transform/proiecție/depth; nu se apelează rendererul 2D pentru oase.
- [x] Adaugă HLSL și intrările manifestului, artefactele celor trei formate, loader-ul și embedding-ul. Testează stride/offset/reflection, dimensiunea uniformelor și transpunerea matricelor cu valori nesimetrice.
- [x] Creează testele native `NativeRenderSurface3DTests` și fixture-ul minim cu fereastră înaintea gate-ului vizual. Folosește `SdlNativeFact` și `Window.SaveScreenshot`; extinderea smoke de la etapa 4 va reutiliza scenariile, nu este o dependență ascunsă a acestei etape.
- [x] Creează lazy resursele 3D sub ownerul device-ului; pipeline key include toate diferențele de layout, shader, depth, format și sample count relevante. Fereastra fără suprafețe 3D nu creează resurse 3D.
- [x] Folosește aceeași politică de sample-count/target allocation compatibilă cu color și depth; selecția sau eroarea este explicită. Nu presupune că suportul MSAA al formatului color dovedește și suportul depth.
- [x] Validează în harnessul nativ mic clipping-ul înaintea operațiilor sensibile la w, segmentele aproape paralele cu view direction, capetele butt și conturul markerului. Dacă metoda aleasă nu satisface contractul, se corectează metoda, nu se micșorează corpusul.
- [x] Testează imaginea transparentă rezultată: geometrie opacă, exterior transparent și compoziție fără halouri de alpha. Fragmentele din quad-ul markerului eliminate de shader în afara discului nu scriu depth.
- [x] Rulează compilerul offline în modul generare, apoi `--verify`, testele descriptorilor și testul nativ de creare a pipeline-urilor 3D.

**Gate etapa 2**

- [x] Un target nativ satisface oracle-ul și toleranțele înghețate în etapa 0 pentru occlusion independent de ordinea de desen la depth-uri diferite, near/far clipping și perspective/orthographic; matrix packing nu este dedus doar dintr-un build reușit.
- [x] Desen 2D înainte/după 3D, nested clip/layer și Prism se păstrează. Shaderele 3D sunt compilate pentru toate formatele; runtime-ul celorlalte platforme rămâne obligatoriu la gate-ul final.

Acceptare independentă părinte, 2026-09-23: etapa 2 închisă după audit și reparații RED→GREEN pentru snapshot-ul invalidat în callback, AA/depth dependent de ordinea desenării, clipping-ul primitivelor extinse și politica explicită MSAA minimum 2x. `SV_Coverage` respectă numărul real de samples; nu a fost necesară schimbarea descriptorului platformei. Oracle-urile frozen și paritatea numerică 2D/Prism au rămas active. Părinte native/parity 51/51 înaintea deciziei 1x și route final 14/14 după aceasta; worker final native/parity55/55, SDL471 pass/210 native opt-in skips, core3984 pass/2 native opt-in skips. Native fizic Windows/D3D12 8x; 2/4x și refuz1x sunt testate prin fake capabilities, nu declarate native. Shader generate/verify8/8, formatter/diff-check trecute; paginile canonice și contractul sunt sincronizate, manifestul fără schimbări necesare. Hash-uri independente finale23 surse/docs/artifacts+3binare+6TRX+22PNG corespund, dovezi `control-stage2/one-x-decision/parent-final-hash-audit.json`, `parent-one-x-final.trx`, `final-handoff.md`; rundele anterioare păstrează RED și native51. Linux/macOS waived/neexecutate. Worker completed/quiescent, retired logic fără close tool. Lifecycle-ul complet și closure-ul retained al noii rute rămân gate-urile etapei3, nu sunt pretinse verificate aici.

### Etapa 3 — lifecycle, failure recovery și retained composition

- [x] Adaugă RED pentru detach/reattach, ultimul subscriber eliminat, schimbarea root/device/session, două suprafețe și două ferestre intercalate. Testează aceeași suprafață mutată într-o altă fereastră a aceluiași device.
- [x] Introdu contractul intern mic `IRenderSurfaceResourceOwner` în dispatch-ul `UIRoot`; adaptează `RenderSurface2D` fără a modifica semantica sa. Verifică toți apelanții `ReleaseDrawingResources` din inventar.
- [x] După RED-urile de lifecycle, implementează retirement/dispose idempotent pentru achizițiile control–sesiune. Închiderea unei ferestre nu invalidează resursele altei ferestre. Commit/cancel pending se implementează după RED-urile de replay/failure de mai jos, conform secțiunii 2.
- [x] Adaugă RED și implementare pentru replay al aceleiași înregistrări 3D în compoziție și captură în același buffer: un singur callback/upload/pass pentru aceeași cheie, apoi reutilizare după submit. O schimbare efectivă de raster/cameră/generație nu este deduplicată greșit.
- [x] Adaugă RED și implementare pentru excepție în callback, detach în callback, eșec texture/pipeline/upload/render-pass/submit și anularea bufferului după înregistrare. Verifică separat eșecul primului submit și primul submit reușit urmat de recovery eșuat. Numai rezultate complete și submitted sunt reutilizabile; următorul frame reface rezultatele invalidate, fără a pretinde rollback pentru GPU work deja trimis.
- [x] Repetă failure/recovery cu 3D sub Prism și captură retained, păstrând aceleași versiuni cerute în următorul frame. Contoarele și pixelii trebuie să dovedească refacerea compoziției invalidate, nu doar un flag dirty pe control. Refolosește contractul comun verificat la etapa 0; dacă este insuficient, oprește pentru prerequisite, nu introduce bypass de cache.
- [x] Testează resize pozitiv→zero→pozitiv, DPI și format change, transform UI neinversabil și clip integral. La viewport gol nu se alocă target 0×0 și nu se emite pass 3D.
- [x] Rulează 100 cicluri deterministe de resize/detach/reattach și o secvență intercalată de două ferestre; verifică handle-uri live/retired după punctul real de retirement. Cache-urile immutable per device pot rămâne până la dispose, nu sunt confundate cu leak-uri per control.
- [x] Rulează suitele SDL_GPU/core afectate și `Tetrisish/Tests/Tetris.Tests.csproj`, inclusiv cele 2D după migrarea root cleanup.

**Gate etapa 3**

- [x] Nicio versiune eșuată nu devine clean; resursele locale revin la baseline după cleanup, cele device-owned dispar la dispose-ul device-ului.
- [x] Frames reușite după fiecare failure și multiple instanțe păstrează camera, color/depth și starea UI fără contaminare între owneri.

Acceptare independentă părinte, 2026-09-23: etapa3 închisă după două runde de audit. RED→GREEN pentru cleanup/replay/raster gol, capturi stale după retirement, pinning-ul handle-urilor încă pending între două sesiuni shared-device, rebind 2D pe skip și conservarea epoch-ului prin translate/opacity/compoziția lor. Testele native folosesc două ferestre pe același owner și verifică pixeli/camere/move/close; failure matrix are injectare fake țintită și recuperare pixel nativă reprezentativă, nu fault injection fizic de driver. Ipoteza Prism după cleanup fără schimbare de conținut a fost respinsă: pixelii submitted încă valizi se reutilizează legitim; invalidarea vizuală reală recapturează. Nicio schimbare Prism sau bypass. Părinte final native/focused79/79, fără skips; SDL508 pass/214 opt-in skips, core3984 pass/2 skips, native2D/Prism133/133, Tetris30/30; formatter/diff/docs verificate. Hash-uri independente16 surse+5binare+11TRX+13PNG match. Dovezi finale `control-stage3/audit-round2/parent-hash-audit.json`, `parent-stage3-final.trx`, `final-handoff.md`. Handle lifetime este verificat cu handle-uri reale ale fake-ului; native confirmă pixeli/counters, nu inventar de handle-uri nativ. Linux/macOS waived/neexecutate. Worker completed/quiescent, retired logic fără close tool.
### Etapa 4 — authoring și consumatorul de schelet

- [x] Adaugă întâi teste SourceGen pentru `<RenderSurface3D>` cu `Content` overlay, proprietățile scalare/redraw și handler `Draw`; compilează și instanțiază fabrica generată. Dacă descoperirea existentă le satisface deja, sunt caracterizare GREEN și generatorul rămâne nemodificat; numai un contract încă nesatisfăcut justifică RED și implementare. Folosește binding/code pentru valori compuse, fără un parser de matrici inventat.
- [x] Integrează numai mecanismele existente de descoperire/schema necesare. Testează completion/diagnostic Language și instanțierea în preview cu geometrie de probă, fără acces GPU public.
- [x] Demonstrează Aspect/Motion pentru proprietățile UI deja suportate și Prism pe imaginea controlului. Nu adăuga un mixer `Quaternion` sau animație a oaselor în Motion în această livrare. Un binding care înlocuiește descriptorul camerei invalidează corect.
- [x] Creează fixture-ul cu 20 de articulații, o ierarhie fixă fără cicluri, grid/axe și minimum două pose-uri deterministe; transformările ierarhice sunt cod de consumator, nu API al frameworkului. Verifică referințele de proiect și suprafața publică: frameworkul/backendul nu depind de fixture sau de tipuri de schelet/controller; include o scenă generică fără ierarhie în corpus.
- [x] Implementează în fixture controllerul orbit/pan/zoom, toggle perspective/orthographic, selectare articulație vizibilă și o rotație de articulație demonstrativă. Picking-ul folosește conversiile controlului și regula explicită de depth/tie a fixture-ului, nu mută selecția în renderer.
- [x] Testează înlocuirea Content/template-ului, detach/reattach și overlay focus/capture fără abonamente duplicate. Nu se introduc template parts 3D; testul protejează lifecycle-ul moștenit al gazdei.
- [x] Extinde `SmokeOptions`, hostul smoke, `Invoke-SdlGpuSmoke.ps1` și CI cu modul planificat `rendersurface3d`. Reutilizează hostul existent; nu crea o a doua buclă de prezentare. Adaugă testele native 3D la pasul opt-in multiplatformă livrat de fundație; actualizează trigger-ele CI pentru control, fixture-ul comun și probe, nu numai lista de moduri smoke.
- [x] Exercită input prin Servo/driverul disponibil: drag orbit, pan, wheel zoom, click de selecție, interacțiune cu overlay-ul. Pointer capture este eliberat la detach/cancel; click pe overlay nu pornește orbit. Dacă bindingul ales cere input nou neacoperit, adaugă harnessul înainte de gate.
- [x] Capturează toate cele opt direcții prestabilite prin `Window.SaveScreenshot`. Acesta este corpus de conformance, nu un API de export sprite-sheet sau promisiune că PixelLab va interpreta pose-urile.
- [x] Construiește probe-ul de cost în proiectul benchmarks existent și testează noua sa ramură de argumente înainte de folosire. Colectează timing CPU cu un scope care exclude așteptarea VSync, allocations pe thread-ul de render și contoarele structurale introduse la etapa 0; raportează separat costul total al frame-ului. Refolosește datele fixture-ului, nu implementa un al doilea renderer. Proiectul este `net8.0-windows`: probe-ul de timing este Windows-only, în timp ce gate-urile deterministe/native rămân pe toate platformele din index.
- [x] Rulează suitele SourceGen/Language/PreviewHost și testele de input/conversii ale fixture-ului.

**Gate etapa 4**

- [x] Fixture-ul rulează într-o fereastră reală cu 3D și UI overlay; inputul schimbă camera/pose conform traseului de evenimente, nu prin setări directe folosite drept dovadă de interacțiune.
- [x] Markup/preview funcționează fără sintaxă nouă și fără a transforma primitivele 3D în UIElement-uri sau noduri de rig.

Acceptare independentă părinte, 2026-09-23: etapa4 închisă după audit sursă/delta/raw TRX și reparații RED→GREEN pentru zoom orthographic și picking în afara footprint-ului markerului. SourceGen existent nemodificat, callback generat executat; preview rasterizează geometria prin framebuffer-ul aplicației. Fixture-ul rămâne exclusiv consumator; niciun API public de rig. Părinte13/13 fixture,3/3 SourceGen,1/1 preview și script smoke publicat Windows cu8 capturi Window.SaveScreenshot. Worker final SourceGen610/610,Language227 pass/1 skip explicit anterior al maintainerului pentru performanță,Preview14/14,native3D20/20; build0 warnings/errors,formatter/diff trecute. Probe parser2valid/7invalid, două rulări scurte numai de funcționalitate; CPU separat de present/VSync,totalframe și measure/arrange raportate. Hash-uri independente17surse+37artefacte+11binare match în control-stage4/final/parent-round1-hash-audit.json; handoff-round1.md și command-ledger-round1.md sunt autoritative. Copiile experimentale .crn au fost redenumite .crn.txt pentru a nu intra în generator; build failure-ul inițial nu este RED comportamental. Hosted CI/YAML parser neexecutate, Linux/macOS waived/neexecutate; fără validare umană sau verdict vizual dedus din capturi. Worker completed/quiescent, retired logic fără close tool.

### Etapa 5 — conformance, costuri, documentație și închidere

- [x] Execută corpusul nativ pe matricea din index: occlusion în ambele ordini, near/far/side clipping, DPI 1/1.25/2, camera la poziții asimetrice, disc/line overlap, resize, transparent clear, 2D overlay, UI transform, opacity și Prism. Oracolele includ coordonate/depth/culori cunoscute; acordul dintre drivere singur nu stabilește adevărul.
- [x] Aplică aceleași oracole și toleranțe din etapa 0 pe întreaga matrice; raportul separă interiorul opac, coverage AA și compoziția Prism/opacity. Nu redefinește succesul după rezultate.
- [x] Măsoară cu probe-ul etapei 4 scenariile cu 20 și 200 de articulații, viewport 800×600 DIPs la DPI 1 și 2, static și camera în mișcare: warmup 120 frames, 600 măsurate, trei procese Release. Raportează CPU median/P95, allocations/frame, recordings, uploads/bytes, passes/draws, target churn și measure/arrange. GPU time se raportează numai dacă există query real; altfel indisponibil.
- [x] Verifică gate-ul determinist: 300 frame-uri UI în `OnDemand`, după primul present reușit și fără mutații, produc zero recordings/uploads/passes **3D** noi. Nu se pretinde zero lucru al întregii ferestre. Mișcarea camerei cu dimensiuni fixe nu recreează targetul și nu declanșează layout.
- [x] Compară aceleași caracterizări 2D de allocations/ordering/lifetime cu baseline-ul fundației, fără control 3D; orice regresie deterministă se investighează. Nu pretinde comparație de timing 2D cu un baseline nemăsurat. Timingul 3D cu variație mare este inconcludent și se rerulează în mediu stabil, fără afirmație de performanță nesusținută.
- [x] Folosește skillul `writing-api-documentation` pentru fiecare tip/membru public nou și pentru `DrawCommandKind`/alte contracte publice afectate. Sursa canonică este exclusiv `docs-site/documentation/classes/`; sincronizează `docs-site/documentation/manifest.json`. Exemplele folosesc API-ul implementat și testat, nu numele estimative din plan.
- [x] Documentează coordonate, DPI, lifetime frame/span, convențiile ray, defaulturile, invalidarea, opaque-only, backend capability și ce nu face controlul. Actualizează separat `docs/sdl-desktop-backend.md` pentru traseul intern/resurse.
- [x] Rulează ApiCompat față de assembly-urile baseline: fără removals/breaks; strict diff revizuit cu lista exactă de adăugiri aprobate, fără suppression global. `SdlArchitectureTests`, `SdlDependencyBoundaryTests` și `DesktopBackendDependencyBoundaryTests` trebuie să păstreze backendul intern și core-ul independent de SDL.
- [x] Rulează full suite, build Release, compiler shader `--verify`, corpusul 2D/Prism complet și corpusul 3D. Verifică explicit numărul testelor native executate, nu doar exit code-ul unui filtru gol sau skip.
- [x] Verifică formatarea numai pentru C# modificat, diff-check, index final și inventarul documentației. Curăță experimentele proprii; păstrează artifactele necesare reproducerii, fără a șterge outputuri străine.

**Gate etapa 5**

- [x] Toate porțile obligatorii și matricea native sunt închise; nicio platformă lipsă nu este raportată GREEN. Documentația și manifestul sunt complete în aceeași schimbare.

Acceptare finală independentă părinte, 2026-09-23: etapa5 și planul controlului sunt închise cu waiver-ul explicit Linux/macOS, nu conformance multiplatformă GREEN. Audit sursă/delta/owner/teste/docs și raw evidence; 211/211 hash-uri finale și 15/15 copii sursă corespund. Părinte88/88 native3D/Prism budget/ownership și39/39 layout/scheduler/fixture, fără skips. Cele10TRX finale au fost recontorizate independent:6245 pass,6skip explicite anterior aprobate,0fail; native3D27/27, separat2D/Prism133/133. Build0warnings/errors,shader8/8,cumulativeformatter64C#,diff-check,docs/manifest și6RID publish trecute. ApiCompat strict exit1 exclusiv9tipuri+1enummember aditive aprobate; reflecție fără diferențe față de semnăturile acceptate, fără suppression. Cele24procese probe și300frame-uri idle au fost auditate: idle0record/upload/pass3D; moving0targetchurn/measure/arrange; timpii CPU/P95 rămân observații variabile, fără buget/FPS garantat, GPUtime indisponibil. RED→GREEN pentru resize în schedulerul shared (root arrange înainte de copii) și allocations Prism în ownerul comun; candidatul LayoutManager care regresa44teste a fost eliminat integral. Nicio corecție simptomatică în renderer. Canonical docs includ depth/AA/lifetime/MSAA/empty-frame și manifestul este sincronizat. Dovezi autoritative: control-stage5/final-handoff.md,conformance-and-cost-report.md,full-audit-trx/,parent-final-hash-audit.json,parent-probe-audit.json și cele două parent-final TRX. Indexer indisponibil prin ștergerea utilizatorului: inventar textual/hash, fără claim de index semantic. HostedCI și parserYAML neexecutate; validare umană nepretinsă. Eșecul shader pipeline din prima rulare nu s-a repetat izolat, în SDL complet sau în cele două full suite ulterioare; cauza exactă rămâne necunoscută. Worker completed/quiescent, retired logic fără close tool. Niciun commit/push/dispatch extern.

## 5. Comenzi și artefacte

Comenzi actuale; filtrele pentru clase noi se folosesc numai după ce testele au fost create și numărul lor a fost verificat:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~RenderSurface3D|FullyQualifiedName~RenderSurface2D|FullyQualifiedName~Drawing'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj -c Release
dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj -c Release
dotnet test .\tests\Cerneala.Tests.PreviewHost\Cerneala.Tests.PreviewHost.csproj -c Release
# Fără --verify: generează artefactele după modificarea shaderelor.
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release -- --manifest .\Cerneala.Backends.SdlGpu\Shaders\manifest.json
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release -- --verify
# Mod NOU, disponibil numai după etapa 4:
dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release -- --mode rendersurface3d --artifacts artifacts/rendersurface3d/native
# Shell dedicat verificării native:
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet build .\Cerneala.slnx -c Release
dotnet test .\Cerneala.slnx -c Release --no-build -m:1
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~SdlGpuDrawingConformanceTests|FullyQualifiedName~PrismSdlGpuPixelConformanceTests'
git diff --check
```

Artefactele planificate sub `artifacts/rendersurface3d/` includ contractul, baseline/final, TRX, descriere GPU/driver/DPI/format, capturi și diff-uri, contoare structurate, raportul ApiCompat și comenzile exacte. Runnerul 3D și parserul sunt implementate și verificate în etapa4; comenzile finale și rezultatele sunt în control-stage5/final-handoff.md și conformance-and-cost-report.md.

## 6. Definiția de gata

- [x] `RenderSurface3D` este public, platform-neutral și thin; poate randa un schelet fără a cunoaște conceptul de os.
- [x] GPU proiecție/depth/clipping și compoziția UI sunt verificate; defaulturile și limitările sunt documentate.
- [x] Camera și selecția sunt demonstrate prin input real/user-like; fixture-ul nu este confundat cu un editor de rig complet.
- [x] OnDemand/Continuous, resize/detach/root cleanup, multi-window și toate failure paths au teste și dovezi observabile.
- [x] Shaderele offline, API compatibility aditivă revizuită, markup/preview, documentația/manifestul, full suite și conformance-ul Windows sunt verificate; native Linux/macOS sunt WAIVED / NEEXECUTATE, nu GREEN.
- [x] Nu există mesh/skinning/IK/timeline sau infrastructură de extensie speculativă strecurată în MVP.
