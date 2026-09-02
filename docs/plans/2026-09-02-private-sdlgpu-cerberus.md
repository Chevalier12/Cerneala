# Cerberus privat și independent în backendul SDL_GPU

## Statut

Plan de implementare finalizat. Toate etapele sunt executate și verificate. Suita completă are o excepție explicit autorizată de utilizator: 12 teste adăugate/modificate de audituri paralele eșuează în `Cerneala.Tests`; rezultatul este consemnat mai jos și nu este raportat drept GREEN.

## Obiectiv

Cerberus devine subsistemul intern de batching și emitere a geometriei pentru `Cerneala.Backends.SdlGpu`, separat arhitectural de `SdlGpuDrawingBackend`, dar intenționat dependent de SDL_GPU. Utilizatorii Cerneala nu primesc API Cerberus, opțiuni Cerberus sau un ciclu public `Begin`/`End`.

Rezultatul urmărit nu este o clonă `SpriteBatch`. Cerberus trebuie să păstreze avantajele modelului Cerneala — comenzi eterogene, geometrie indexată arbitrară, text, clip/stencil, render targets și Prism — și să elimine cu dovezi costurile redundante din executorul SDL_GPU, fără reordonarea semantică a desenării.

## Decizii aprobate

- Cerberus rămâne în assembly-ul `Cerneala.Backends.SdlGpu` și poate folosi direct tipuri și resurse SDL_GPU interne.
- Cerberus este `internal`; nu se adaugă API public sau protejat.
- Cerberus devine tip top-level separat, nu clasă privată imbricată în `SdlGpuDrawingBackend`.
- `SdlGpuDrawingBackend` deține câte o instanță Cerberus pentru fiecare `SdlGpuWindowGraphicsSession` și îi coordonează ciclul de viață.
- Instanțele și cozile mutabile nu se partajează între ferestre. Resursele GPU care sunt deja partajate per device rămân în `SdlGpuDrawingResources`.
- Cerberus nu păstrează o referință la `SdlGpuDrawingBackend` și nu îl apelează prin callback-uri generice.
- Backendul traduce `DrawCommand` și starea Cerneala în geometrie și stare SDL; Cerberus deține stocarea, fuziunea adiacentă, rebazarea indicilor, uploadul geometriei și emiterea draw-urilor.
- Ordinea painter-style a comenzilor rămâne contract. Nu se introduc `SpriteSortMode`, sortare după textură sau depth și nici reordonare „inteligentă” neverificată.
- `IDrawingBackend` rămâne punctul public de substituție pentru utilizatorii care vor un renderer diferit. Cerberus nu devine al doilea punct public de extensie.

## Baza de dovezi

### Repository Cerneala

Auditul a fost făcut pe commitul `d009162ecb74164fcc7490b29de2d2588e9b0d3a`, cu worktree deja dirty. Schimbările preexistente sau cu proprietate incertă din `.vscode`, `AGENTS.md`, `.codex`, `AGENTS_DEPRECATED.md` și `FileTree.md` nu fac parte din plan și nu trebuie suprascrise.

- `Cerneala.Backends.SdlGpu/Gpu/Cerberus.cs` conține astăzi o clasă privată imbricată care primește întregul `SdlGpuDrawingBackend` ca `owner`.
- Cerberus păstrează persistent trei array-uri redimensionabile: vertexuri, indici `int` și draw-uri GPU. Capacitățile inițiale sunt 1.024 vertexuri, 1.536 indici și 256 draw-uri, cu creștere 1,5x.
- `Allocate` unește numai draw-uri `TriangleList` imediat adiacente și numai când întregul `BatchKey` coincide. Aceasta păstrează ordinea; nu există sortare.
- `Flush` apelează prin `owner` patru responsabilități străine: uploadul atlasului text, agregarea contoarelor, `SdlGpuGeometryUploadArena` și accesul la sesiune/resurse.
- `BatchKey` și `CpuDrawBatch` sunt încă tipuri imbricate în `SdlGpuDrawingBackend`, deși sunt contracte de intrare ale Cerberus.
- Executorul leagă pipeline-ul, samplerul, scissor-ul și stencil reference pentru fiecare `GpuDraw`, chiar dacă numai una dintre aceste stări s-a schimbat.
- `SdlGpuGeometryUploadArena` este per sesiune/fereastră, folosește trei frame slots și transferă vertexurile/indicii în buffere append-only fără cycling în același frame.
- `SdlGpuWindowGraphicsSessionFactory` partajează device-ul și `SdlGpuDrawingResources`, dar fiecare sesiune de fereastră creează propriul backend și propriul geometry arena. Aceasta fixează proprietarul corect al stării mutabile Cerberus.
- `SdlGpuDrawingFrameCounters` măsoară astăzi numai flush-uri, draw calls, vertexuri și indici; nu poate demonstra fuziunile sau bind-urile SDL eliminate.
- `tests/Cerneala.Tests.SdlGpu/SdlGpuDrawingBackendTests.cs` acoperă batching adiacent, 4.096 alternări de texturi, reutilizarea stocării, geometry arena, recuperarea după excepție, stencil/scissor, text atlas, `RenderSurface2D`, Prism și eliberarea resurselor.
- `tests/Cerneala.Tests/Drawing/SdlGpuDrawingConformanceTests.cs` compară capturi produse prin `Window.SaveScreenshot` cu WindowsDX la pragurile existente: MAE `<= 1,0`, P99 `<= 10`, delta maximă `<= 49`.
- Benchmarkul existent `DrawingBatchBenchmarks` măsoară doar înregistrarea comenzilor core, nu planificarea sau emiterea Cerberus. Nu există astăzi o măsurătoare Cerberus versus MonoGame care să justifice afirmații de viteză.

Baseline-ul focalizat observat înaintea planului este GREEN: 24/24 teste SDL drawing backend și 13/13 teste `DrawingImageMeshBatchTests`. Aceste rezultate sunt baseline de audit, nu înlocuiesc rulările cerute după implementare.

### Sursa MonoGame citită

Comparația este fixată la MonoGame commit `5cebc1e12a2c57f789f37e863f4edb1c65280bf3` (2026-08-26 UTC):

- [`SpriteBatch.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Graphics/SpriteBatch.cs)
- [`SpriteBatcher.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Graphics/SpriteBatcher.cs)
- [`SpriteBatchItem.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Graphics/SpriteBatchItem.cs)
- [`SpriteSortMode.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Graphics/SpriteSortMode.cs)
- [`SpriteEffect.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Graphics/Effect/SpriteEffect.cs) și [`SpriteEffect.fx`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/MonoGame.Framework/Platform/Graphics/Effect/Resources/SpriteEffect.fx)
- testul oficial [`SpriteBatchTest.cs`](https://github.com/MonoGame/MonoGame/blob/5cebc1e12a2c57f789f37e863f4edb1c65280bf3/Tests/Framework/Graphics/SpriteBatchTest.cs)

Nu se folosește o descriere secundară a API-ului drept substitut pentru implementare.

### Contracte SDL_GPU citite

Documentația SDL 3 fixează limitele optimizării de stare:

- [`SDL_BeginGPURenderPass`](https://wiki.libsdl.org/SDL3/SDL_BeginGPURenderPass) cere ca operațiile grafice să fie în interiorul unui render pass și stabilește viewportul/scissor-ul inițial; un copy pass nu poate începe până când render pass-ul nu s-a încheiat.
- [`SDL_BindGPUGraphicsPipeline`](https://wiki.libsdl.org/SDL3/SDL_BindGPUGraphicsPipeline), [`SDL_BindGPUFragmentSamplers`](https://wiki.libsdl.org/SDL3/SDL_BindGPUFragmentSamplers) și [`SDL_SetGPUScissor`](https://wiki.libsdl.org/SDL3/SDL_SetGPUScissor) modifică starea curentă pentru draw-urile următoare din pass.

Planul nu presupune persistența unei legături peste restartul render pass-ului și nu presupune că schimbarea pipeline-ului păstrează în mod portabil samplerul decât dacă documentația SDL sau un test nativ pe backendurile aplicabile o confirmă.

## Matrice de capabilități și decizii

| Problemă | MonoGame `SpriteBatch` | Cerberus actual | Decizie pentru Cerberus |
| --- | --- | --- | --- |
| Vizibilitate | Tip public, creat de utilizator | Detaliu privat al backendului | Tip top-level `internal`, fără API public |
| Ciclu de viață | `Begin`/`Draw`/`End` explicit | Automat în `Render` și la bariere | Automat; backendul coordonează, Cerberus validează starea internă |
| Intrare | Quad-uri sprite și text `SpriteFont` | Geometrie indexată pentru forme, imagini, text, mesh, clips și compoziție | Păstrează modelul general; nu reduce Cerberus la sprite-uri |
| Sortare | Deferred, Immediate, Texture, FrontToBack, BackToFront | Numai fuziune adiacentă; ordinea este păstrată | Fără sortare/reordonare; ordinea este invariantă explicită |
| Stare | Blend/sampler/depth/rasterizer/effect/transform la `Begin` | Stare derivată din stack-urile `DrawingContext` și `BatchKey` | Fără configurare Cerberus; backendul traduce starea existentă |
| Texture grouping | Sortare opțională după textură | Numai texturi adiacente identice | Păstrează numai fuziunea sigură adiacentă |
| Geometrie | Patru vertexuri per sprite, batching specializat | Vertexuri/indici arbitrar, `TriangleList` și `TriangleStrip` | Păstrează 32-bit indices și geometria generală |
| Render targets și bariere | Controlate extern prin `GraphicsDevice` | Integrate cu layers, Prism, text upload și `RenderSurface2D` | Cerberus respectă bariere declarate de backend; nu le deduce |
| Emitere stare GPU | Flush specializat pe sprite-uri și texturi | Rebind complet pentru fiecare draw logic | Emite numai diferențele de stare documentat sigure în interiorul aceluiași render pass |
| Diagnostic | Fără contract Cerneala | Contoare interne limitate | Contoare interne pentru submissions, merges, uploads, draws și state binds |
| Recuperare | `Begin`/`End` validează starea | `Discard` curăță coada după excepție | Păstrează resetarea deterministă și o testează direct |

### Ce înseamnă „mai bun” în această livrare

- Cerberus deservește întreaga geometrie 2D SDL_GPU, nu numai sprite-uri.
- Utilizatorul nu trebuie să fragmenteze manual desenarea în secvențe `Begin`/`End` și nu poate alege accidental o sortare care strică painter order.
- Uploadul de geometrie rămâne comun pentru toate draw-urile dintr-un flush.
- Fuziunea compatibilă rămâne automată și stabilă.
- Bind-urile SDL_GPU demonstrat redundante sunt eliminate determinist fără schimbarea ordinii draw-urilor.
- Metricile fac diferența dintre „mai puține draw calls”, „mai puține state changes” și „aceeași geometrie”; nu se declară performanță din intuiție.

Această definiție nu promite că Cerberus este mai rapid decât `SpriteBatch` pe orice hardware. O asemenea afirmație cere un harness comparabil și măsurători pe aceleași workload-uri, care nu există încă.

## Arhitectura țintă

```text
DrawCommandList
      |
      v
SdlGpuDrawingBackend
  - interpretează comenzile și stack-urile de stare
  - produce vertexuri + CerberusBatchKey
  - declară barierele de flush
  - pregătește uploadurile text/Prism/layer/surface
      |
      v
internal Cerberus
  - Begin target
  - Allocate/Add
  - stocare persistentă
  - rebazare indici
  - merge adiacent sigur
  - Flush(context) / Discard
  - emitere incrementală de stare SDL_GPU
      |
      +--> SdlGpuGeometryUploadArena (per sesiune)
      +--> SdlGpuDrawingResources (partajat per device)
      +--> ISdlApi + active command/render pass (per frame)
```

### Contracte interne estimate

Numele exacte pot fi ajustate mecanic în implementare, dar responsabilitățile nu se mută fără revizuirea planului:

- `internal sealed class Cerberus`: deține numai coada, storage-ul reutilizabil, plannerul adiacent și executorul SDL_GPU.
- `internal readonly record struct CerberusBatchKey`: deține topologia, textura, sampling/address, blend, stencil mode/reference, scissor și color write mask.
- `internal readonly record struct CerberusBatch`: adaptor pentru geometria deja materializată folosită de clip/stencil; căile calde continuă să scrie direct în spanul rezervat.
- `internal readonly record struct CerberusExecutionContext`: transportă explicit `SdlGpuWindowGraphicsSession` și `SdlGpuDrawingResources`, colaboratorii SDL necesari uploadului/emiterii; nu conține `SdlGpuDrawingBackend`.
- `internal readonly record struct CerberusFlushMetrics`: returnează rezultatul flush/reset pentru agregare în contoarele frame-ului.

Cerberus nu trebuie să primească delegate care îl recheamă generic pe owner. Dacă `SdlGpuWindowGraphicsSession` rămâne în execution context, utilizarea sa trebuie limitată la geometry arena, active command buffer/render pass și API-ul SDL; textul, Prism, analiza comenzilor și timingul backendului rămân în backend.

### Proprietate și lifetime

- Factory: un device și `SdlGpuDrawingResources` partajate, conform contractului existent.
- Fereastră/sesiune: un `SdlGpuGeometryUploadArena`, un `SdlGpuDrawingBackend` și o instanță Cerberus.
- Frame: backendul începe Cerberus pe targetul activ; fiecare flush golește coada dar păstrează capacitatea array-urilor.
- Eșec: orice excepție în traducere, upload sau emitere lasă Cerberus gol și utilizabil în frame-ul următor.
- Dispose: Cerberus nu deține resurse GPU persistente separate; nu dublează dispose-ul sesiunii sau al resurselor partajate.

### Bariere obligatorii

Backendul, nu Cerberus, decide când ordinea și lifetime-ul resurselor cer flush sau restart de pass:

1. înaintea unui copy pass fără legătură cu flush-ul, dacă există draw-uri anterioare care trebuie emise înaintea copiei;
2. înaintea și după execuția Prism;
3. la schimbarea render targetului pentru layer sau `RenderSurface2D`;
4. înaintea compoziției unui target copil în targetul părinte;
5. la tranzițiile de clip/stencil care cer geometria anterioară materializată;
6. la finalul range-ului/frame-ului;
7. înainte de orice operație SDL care invalidează starea render pass-ului curent.

Helperul de coordonare păstrează ordinea reală a flush-ului curent: finalizează mai întâi uploadurile pending de resurse, inclusiv atlasul text chiar când coada geometrică este goală; Cerberus încarcă apoi geometria prin copy pass și emite draw-urile în render pass-ul reluat. Orice draw deja înregistrat înaintea unui copy pass separat este emis mai întâi când dependența de ordine/resursă o cere.

Toate call-site-urile trebuie să treacă printr-un singur helper privat al backendului care construiește contextul Cerberus, apelează `Flush`, agregă metricile și măsoară timingul din exterior. Apelurile directe dispersate la `batches.Flush()` nu rămân.

Cache-ul de stare al executorului se resetează la fiecare flush/render pass. Nu se presupune că bindingurile SDL supraviețuiesc unui copy pass, unui target switch sau reluării render pass-ului.

## Contradicție cunoscută, în afara scope-ului

Documentația publică pentru `DrawImageOptions.LayerDepth` afirmă că depth participă la ordering. Core păstrează valoarea, iar backendul MonoGame o consumă, însă calea SDL_GPU folosește `SdlGpuVertex` fără Z, shaderul scrie Z=0, iar `DrawSpriteBatch` pierde depth-ul per sprite când reconstruiește opțiunile. Aceasta este o neconcordanță reală specifică traseului SDL_GPU, dar planul de față nu inventează semantica lipsă și nu introduce sortare. Problema cere reproducere și decizie separată asupra contractului public.

## Fișiere estimate

- `Cerneala.Backends.SdlGpu/Gpu/Cerberus.cs`
- `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.cs`
- `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingFrameCounters.cs`
- `tests/Cerneala.Tests.SdlGpu/SdlArchitectureTests.cs`
- `tests/Cerneala.Tests.SdlGpu/CerberusTests.cs`
- `tests/Cerneala.Tests.SdlGpu/SdlGpuDrawingBackendTests.cs`
- `tests/Cerneala.Tests.SdlGpu/SdlGpuDrawingFrameCountersTests.cs`
- `benchmarks/Cerneala.Benchmarks/CerberusPlanningBenchmarks.cs`
- `docs/sdl-desktop-backend.md`
- `docs/plans/2026-09-02-private-sdlgpu-cerberus.md`

Nu se modifică proiectele core, MonoGame sau WindowsDX decât dacă o verificare demonstrează o dependență reală; o asemenea extindere cere explicarea dovezii înainte de editare.

## Etapa 0 — Baseline, contracte RED și măsurători

- [x] Salvează statusul worktree-ului și separă explicit schimbările utilizatorului de fișierele planului.
- [x] Rulează baseline-ul focalizat pentru `SdlGpuDrawingBackendTests`, `SdlGpuDrawingFrameCountersTests` și `DrawingImageMeshBatchTests`; înregistrează numărul de teste, durata și orice skip.
- [x] Extinde `SdlArchitectureTests` cu un test RED care caută tipul top-level `Cerneala.Backends.SdlGpu.Cerberus`, cere vizibilitate non-publică și interzice `SdlGpuDrawingBackend` în semnăturile constructorilor, câmpurilor, proprietăților și metodelor Cerberus. Testul trebuie să fie RED numai pentru că Cerberus este încă imbricat și owner-coupled; gate-ul semantic final verifică separat și corpul implementării prin RoslynIndexer.
- [x] Caracterizează în același test de arhitectură suprafața publică exactă a assembly-ului SDL GPU: singurul tip exportat rămâne `SdlGpuApplicationBackend`, iar singurul entry point intenționat rămâne `public static void EnsureRegistered()`.
- [x] Adaugă teste de caracterizare GREEN pentru ordinea draw-urilor, merge numai adiacent, target switch, flush la text/Prism/layer/`RenderSurface2D`, reset după eșec și instanțe independente pentru două ferestre.
- [x] Extinde scenariul de 4.096 texturi alternante ca baseline pentru numărul actual de draw calls, pipeline binds, sampler binds, scissor sets și stencil-reference sets. Nu schimba încă așteptările către valorile optimizate.
- [x] Măsoară cu același harness, după minimum trei frame-uri de warmup, alocările pentru: 1.000 quad-uri compatibile, 4.096 texturi alternante și depășirea tuturor capacităților inițiale. Salvează comenzile, mediul, warmupul și valorile efective în `benchmarks/results/2026-09-02-cerberus/baseline.md`; nu declara un JSON automat dacă niciun runner nu îl produce.
- [x] Confirmă prin test că ordinea observable a texturilor din fake SDL API este identică ordinii comenzilor; acesta este invariantul care va bloca orice sortare accidentală.
- [x] **Gate etapa 0:** testul arhitectural este RED pentru motivul intenționat, caracterizările sunt GREEN, iar baseline-ul conține draw/state counts și alocări măsurate — nu estimări.

**Verificare:**

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~SdlGpuDrawingBackendTests|FullyQualifiedName~SdlGpuDrawingFrameCountersTests|FullyQualifiedName~SdlArchitectureTests"
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter FullyQualifiedName~DrawingImageMeshBatchTests
```

## Etapa 1 — Separarea Cerberus fără schimbare de comportament

- [x] Transformă `Cerberus` într-un tip top-level `internal sealed` din `Cerneala.Backends.SdlGpu`; nu îl face `public`, `protected` sau consumabil din core.
- [x] Mută `BatchKey`, `CpuDrawBatch` și `GpuDraw` sub proprietatea Cerberus, cu nume interne care descriu rolul. `SdlGpuDrawingBackend` nu mai este containerul tipurilor plannerului.
- [x] Elimină parametrul/referința `owner: SdlGpuDrawingBackend` din Cerberus.
- [x] Introdu un execution context explicit cu numai colaboratorii SDL necesari flush-ului. Nu introduce o interfață publică și nu abstractiza SDL în afara backendului său.
- [x] Mută uploadul pending al atlasului text înaintea apelului Cerberus, în helperul de coordonare al backendului. Păstrează faptul că uploadul se poate executa chiar dacă nu există geometrie în coadă.
- [x] Fă `Flush` să returneze metricile necesare în loc să modifice câmpurile backendului prin owner. Backendul măsoară din exterior timpul pentru `Flush`/`Discard`; Cerberus nu primește și nu deține timingul backendului.
- [x] Înlocuiește toate apelurile directe de flush cu helperul unic și auditează fiecare cale de layer, clip, Prism și `RenderSurface2D`.
- [x] Păstrează o instanță Cerberus per backend/sesiune; adaugă test care dovedește că două ferestre nu partajează coada sau targetul.
- [x] Adaugă teste directe pentru `Begin`, target invalid, empty add, `Discard`, reset după excepție și reutilizarea capacității după flush.
- [x] Confirmă că testul arhitectural din etapa 0 devine GREEN fără slăbirea lui.
- [x] Reîmprospătează RoslynIndexer imediat după fiecare modificare C# sau de proiect și nu continua analiza pe index stale.
- [x] **Gate etapa 1:** Cerberus este top-level internal, nu referă `SdlGpuDrawingBackend`, toate caracterizările păstrează exact ordinea/draw counts din baseline și suita SDL GPU este GREEN.

**Verificare:**

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~Cerberus|FullyQualifiedName~SdlGpuDrawingBackend"
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- refs SdlGpuDrawingBackend --exact --json
```

## Etapa 2 — Planner intern robust și testabil

- [x] Adaugă teste unitare directe pentru rebazarea indicilor când mai multe geometrii sunt unite într-un singur draw.
- [x] Testează separat cheile identice, fiecare câmp diferit al cheii și secvența A-B-A; A-B-A trebuie să rămână trei draw-uri, nu două.
- [x] Păstrează merge numai pentru `TriangleList`; nu uni `TriangleStrip` fără un contract și o transformare explicită care previn conectarea primitivelor.
- [x] Testează overflowul aritmetic, geometria goală și schimbarea targetului cu coadă neflush-uită. Auditează validarea indicilor la toți producătorii înainte de a adăuga verificări per-index în calea caldă; adaugă o asemenea validare numai dacă este demonstrat un producător nevalidat. Excepțiile trebuie să lase o stare recuperabilă prin `Discard`. (Nu s-a adăugat validare per-index: toți producătorii auditați validează deja indicii.)
- [x] Testează creșterea peste capacitățile inițiale și reutilizarea storage-ului la frame-ul următor fără retenția referințelor externe.
- [x] Schimbă intrările `IReadOnlyList` în `ReadOnlySpan` numai dacă auditul tuturor call-site-urilor confirmă storage contiguu fără copii sau adaptoare alocante. Altfel păstrează contractul existent. (Auditul a confirmat exclusiv storage `int[]` contiguu.)
- [x] Adaugă `CerberusPlanningBenchmarks` pentru enqueue/merge/index rebasing și discard pe workload-uri omogene, alternante și peste capacitatea inițială. Benchmarkul nu pretinde că măsoară GPU time.
- [x] Rulează benchmarkul cu warmup și salvează artifactul final lângă baseline; raportează throughput și alocări, fără prag procentual inventat.
- [x] **Gate etapa 2:** plannerul are teste directe pentru toate invariantele, stressul determinist trece, iar aceleași scenarii de integrare warm nu depășesc baseline-ul lor de alocări. Benchmarkul izolat al plannerului este raportat separat și nu este comparat artificial cu alocările întregului frame. Orice regresie trebuie explicată și reparată, nu etichetată „noise”.

**Verificare:**

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter FullyQualifiedName~CerberusTests
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*CerberusPlanningBenchmarks*" --exporters JSON --artifacts .\benchmarks\results\2026-09-02-cerberus\final
```

## Etapa 3 — Emitere SDL_GPU incrementală și diagnostic măsurabil

- [x] Adaugă mai întâi teste RED pentru eliminările susținute de contractul SDL în interiorul aceluiași render pass: pipeline numai când cheia pipeline se schimbă; scissor și stencil reference numai când valorile se schimbă. Pentru sampler/texture, caracterizează separat secvențele cu pipeline constant și schimbat.
- [x] Implementează un cache local de stare în executorul Cerberus. Prima comandă a fiecărui render pass reluat emite starea completă; cache-ul se invalidează la fiecare restart de pass, inclusiv cel produs de uploadul geometriei, copy pass sau target switch.
- [x] Elimină rebindul sampler/texture peste o schimbare de pipeline numai dacă documentația SDL sau conformance-ul nativ pe backendurile aplicabile confirmă că bindingul rămâne valid. În lipsa dovezii, reemite samplerul la schimbarea pipeline-ului; nu transforma o presupunere de driver în contract Cerberus. (Rebindul conservator la schimbarea pipeline-ului a fost păstrat.)
- [x] Nu elimina draw calls doar pentru că starea este asemănătoare. Draw-urile se unesc numai prin regula de merge adiacent deja testată.
- [x] Păstrează ordinea exactă a `DrawGpuIndexedPrimitives`; testul celor 4.096 texturi trebuie să observe aceeași secvență de texturi ca înainte.
- [x] Extinde `CerberusFlushMetrics`/`SdlGpuDrawingFrameCounters` cu submissions, merged submissions, bytes/vertexuri/indici uploadați, draw calls și numărul fiecărei categorii de state bind.
- [x] Agregarea contoarelor trebuie să fie checked, resetată la `BeginFrame` și acoperită cu teste de overflow/mai multe flush-uri.
- [x] Compară artifactele baseline/final. Gate-ul determinist este reducerea bind-urilor la tranzițiile documentat sigure; CPU time este raportat, dar nu declarat îmbunătățit fără semnal stabil.
- [x] Confirmă că secvențele cu text, clips/stencil, blend changes și target switches reemit starea completă după barieră.
- [x] **Gate etapa 3:** testele RED devin GREEN, draw order și pixel semantics sunt neschimbate, iar contoarele demonstrează exact ce bind-uri au fost eliminate.

**Verificare:**

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~Cerberus|FullyQualifiedName~SdlGpuDrawingFrameCounters|FullyQualifiedName~AlternatingTextures|FullyQualifiedName~NestedState"
```

## Etapa 4 — Integrare, conformance, documentație și gate final

- [x] Rulează întreaga suită `Cerneala.Tests.SdlGpu`, nu numai testele Cerberus. (115 trecute, 5 teste native sărite în rularea fără opt-in, 0 eșecuri.)
- [x] Rulează suitele core drawing pentru imagini, mesh, batches, state, text și `RenderSurface2D`. (1.048 trecute, 2 teste native sărite, 0 eșecuri.)
- [x] Rulează conformance-ul nativ SDL_GPU/WindowsDX prin scenariul existent și `Window.SaveScreenshot`; nu folosi captură OS. Păstrează pragurile existente și arhivează artifactele numai la eșec conform harnessului. (Scenariul nativ existent a trecut 1/1 la pragurile păstrate; capturile au fost produse de API-ul aplicației.)
- [x] Rulează native lifetime/multi-window smoke pentru a verifica instanțele independente și dispose-ul resurselor. (3/3 trecute cu `CERNEALA_SDL_NATIVE_TESTS=1`.)
- [x] Actualizează `docs/sdl-desktop-backend.md` cu data flow-ul intern, proprietatea per sesiune, barierele și faptul că Cerberus nu este API public.
- [x] Nu adăuga pagini în `docs-site/documentation/classes/` și nu schimba manifestul dacă API diff-ul este gol. Dacă apare orice schimbare publică/protejată, oprește etapa: aceasta contrazice decizia aprobată și cere revizuire, nu documentare automată. (Diff-ul API este gol; paginile canonice și manifestul au rămas nemodificate.)
- [x] Rulează `SdlArchitectureTests` și comparația strictă ApiCompat prin SDK `ValidateAssembliesTask` între assembly-urile Release din commitul baseline `d009162ecb74164fcc7490b29de2d2588e9b0d3a` și worktree; construiește baseline-ul într-un worktree temporar separat, fără checkout/reset în worktree-ul dirty. Păstrează în `benchmarks/results/2026-09-02-cerberus/api-compat.md` calea assembly-urilor, proiectul MSBuild temporar care invocă taskul, comanda exactă, exit code-ul și outputul, apoi confirmă diff public/protected gol pentru `Cerneala.Backends.SdlGpu` și core. (`SdlArchitectureTests`: 3/3; ApiCompat strict: exit 0 și diff public/protected gol; proiectul și raportul sunt arhivate.)
- [x] Reîmprospătează indexul final și verifică toate referințele Cerberus. Regenerează `FileTree.md` numai după ce diff-ul existent al fișierului a fost clasificat; dacă se suprapune peste schimbări neatribuite sigur planului, oprește și cere direcție. (Index final reușit; 50 de referințe exacte, numai în backendul SDL_GPU, benchmark și testele SDL_GPU. `FileTree.md` a fost regenerat după autorizarea explicită a utilizatorului de a include și fișierele auditului paralel.)
- [x] Rulează suita completă a soluției o singură dată pe starea finală, apoi build Release, formatter verification limitat la fișierele C# modificate și `git diff --check`. Rularea formatterului pe întregul repository este diagnostică; debt-ul baseline din fișiere nemodificate nu autorizează cleanup în afara scope-ului. (Pe starea C# finală, toate proiectele raportate au trecut cu excepția `Cerneala.Tests`: 12 eșecuri, 3.158 treceri și 2 skip-uri. Cele 12 sunt exclusiv testele auditului paralel pentru Aspect, Motion, Prism și Relay, pe care utilizatorul a cerut explicit să le ignorăm și să le rezolve separat; această suită nu este raportată drept GREEN. Build Release: 0 warnings/0 errors; formatterul limitat și `git diff --check` au trecut.)
- [x] Revizuiește diff-ul complet pentru debug code, API public accidental, callback-uri către owner, cache de stare păstrat peste pass, fișiere temporare și modificări în afara scope-ului. (Nu s-au găsit markeri debug, API public Cerberus, owner/callback generic sau fișiere temporare ale planului; schimbările auditului paralel au rămas neatinse.)
- [x] Marchează checklistul fiecărei etape imediat după ce gate-ul ei este satisfăcut; nu bifa retroactiv cod neverificat.
- [x] **Gate etapa 4:** toate porțile Cerberus — SDL/core focalizat, conformance nativ, lifetime nativ, build, formatter, API diff, index și diff-check — sunt GREEN. Suita completă a fost executată pe starea finală, dar nu este GREEN din cauza celor 12 teste ale auditului paralel exceptate explicit de utilizator; nu pretindem contrariul.

**Verificare:**

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~Drawing|FullyQualifiedName~RenderSurface"
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter FullyQualifiedName~DrawingApiShowcaseMatchesWindowsDxPixelThresholds
dotnet test .\Cerneala.slnx -c Release
dotnet build .\Cerneala.slnx -c Release
dotnet format .\Cerneala.slnx --verify-no-changes --include .\Cerneala.Backends.SdlGpu\Gpu\Cerberus.cs .\Cerneala.Backends.SdlGpu\Gpu\SdlGpuDrawingBackend.cs .\Cerneala.Backends.SdlGpu\Gpu\SdlGpuDrawingFrameCounters.cs .\tests\Cerneala.Tests.SdlGpu\SdlArchitectureTests.cs .\tests\Cerneala.Tests.SdlGpu\CerberusTests.cs .\tests\Cerneala.Tests.SdlGpu\SdlGpuDrawingBackendTests.cs .\tests\Cerneala.Tests.SdlGpu\SdlGpuDrawingFrameCountersTests.cs .\benchmarks\Cerneala.Benchmarks\CerberusPlanningBenchmarks.cs
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
.\Tools\scripts\New-FileTree.ps1
git diff --check
```

## Riscuri și protecții

| Risc | Protecție |
| --- | --- |
| Extracția schimbă ordinea fără intenție | Caracterizare înainte de editare și verificarea secvenței fake SDL pentru mii de draw-uri |
| Uploadul text nu mai precedă samplingul atlasului | Helper unic care execută uploadurile pending înaintea uploadului geometriei/emiterii și testul existent pentru un singur upload atlas |
| Cache-ul de stare traversează un render pass și reutilizează bindinguri invalide | Cache local per render pass, reset obligatoriu la orice restart și teste cu target/copy pass |
| Optimizarea presupune că samplerul supraviețuiește schimbării pipeline-ului pe toate driverele | Rebind conservator până când documentația SDL sau conformance-ul nativ aplicabil demonstrează contrariul |
| Cerberus devine public accidental | Test de arhitectură, API diff gol și lipsa documentației API Cerberus |
| O abstracție generică scoate SDL din owner doar formal | Execution context intern SDL-specific; interdicție explicită pentru backend owner/callback generic |
| Stare mutabilă partajată între ferestre | Instanță per sesiune și test multi-window intercalat |
| Benchmarkul confundă planning CPU cu GPU performance | Nume/scenarii separate, metrici declarate și fără concluzie GPU din benchmarkul plannerului |
| „Optimizarea” sortează draw-uri transparente | Interdicție de reordering și test A-B-A + ordine texturi |
| Se repară oportunist `LayerDepth` | Contradicția este documentată separat și nu se schimbă fără reproducere/decizie de contract |

## Obligații de documentație API

Planul nu autorizează nicio schimbare publică sau protejată. În mod normal se actualizează numai documentația internă `docs/sdl-desktop-backend.md`.

Dacă implementarea dovedește că este necesar un API public, lucrul se oprește și se cere aprobarea utilizatorului. Numai după aprobare se aplică skillul `writing-api-documentation`, se actualizează sursa canonică `docs-site/documentation/classes/` și, dacă este cazul, `docs-site/documentation/manifest.json`.

## Definition of done

- [x] Cerberus este top-level `internal`, SDL_GPU-specific și nu referă `SdlGpuDrawingBackend`.
- [x] Fiecare sesiune/fereastră are propria instanță și propriul storage mutabil Cerberus.
- [x] Backendul este ownerul traducerii comenzilor, textului, Prism, layers și barierelor; Cerberus este ownerul batchingului și emiterii SDL_GPU.
- [x] Ordinea comenzilor și semantica vizuală sunt neschimbate.
- [x] Fuziunea adiacentă, rebazarea indicilor, growth/reuse, failure reset și target lifecycle au teste directe.
- [x] Bind-urile demonstrat redundante sunt reduse la tranzițiile documentat sigure și demonstrate de contoare/teste.
- [x] Baseline-ul și rezultatul final conțin măsurători reproductibile; nu există afirmații de performanță nemăsurate.
- [x] Nu există API public Cerberus, opțiuni Cerberus, `Begin`/`End` public sau dependență Cerberus în core/MonoGame/WindowsDX.
- [x] Documentația internă descrie proprietatea, data flow-ul și barierele reale.
- [x] API diff-ul public/protected este gol.
- [x] Testele focalizate, suitele afectate, buildul Release, formatterul, conformance-ul aplicabil, RoslynIndexer și `git diff --check` sunt GREEN. Suita completă a fost rulată pe starea finală, dar are cele 12 eșecuri din auditul paralel exceptate explicit de utilizator; nu este raportată drept GREEN.
- [x] Nu există gate nativ neexecutat: conformance-ul SDL_GPU/WindowsDX a trecut 1/1, iar lifetime/multi-window a trecut 3/3.
- [x] Schimbările utilizatorului și fișierele din afara scope-ului rămân neatinse, cu singura regenerare mixtă a `FileTree.md` autorizată explicit de utilizator.
