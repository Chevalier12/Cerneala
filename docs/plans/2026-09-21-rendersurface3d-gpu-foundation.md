# Plan: fundația GPU internă pentru RenderSurface3D

> Data: 2026-09-21
> Status: încheiat; etapele 0–3 acceptate, cu waiver explicit pentru native Linux/macOS
> Dependență: contractele și politica din [index](2026-09-21-rendersurface3d-plan-index.md); niciun alt plan de implementat înainte.
> Deblochează: [controlul RenderSurface3D](2026-09-21-rendersurface3d-control.md)

## 1. Rezultat și non-obiective

Adaptorul SDL acceptă un vertex layout și un depth state explicite. Uploadul comun poate primi un layout diferit de `SdlGpuVertex`. Desenul 2D, stencil-ul, Prism fullscreen, ordinea comenzilor și API-ul public rămân identice. Nu se introduc aici controlul, shaderele 3D, scene sau un API public de GPU.

Nu se dublează `NativeSdlApi.CreateGpuGraphicsPipeline` într-o variantă „3D” și nu se schimbă semnificația `UsesVertexInput=true` printr-un pitch nou global. Acesta este ownerul corect al eliminării layout-ului hardcodat.

## 2. Contract intern propus

- Un descriptor immutable de vertex input: stride și atribute location/format/offset, cu un singur stream în slot 0 și input rate per vertex în MVP. Numai formatele necesare layout-ului existent și layout-ului 3D ulterior; nu se copiază întregul API SDL în core și nu se adaugă instancing anticipat.
- Două valori concrete pentru consumatorii existenți: layout-ul 2D de 32 bytes și input gol pentru fullscreen. `UsesVertexInput` este înlocuit intern, nu păstrat ca a doua sursă contradictorie de adevăr.
- Depth test/write/compare sunt independente de stencil. Default explicit pentru consumatorii existenți: test off, write off; stencil exact ca înainte. Viitorul 3D folosește depth normal 0..1, clear 1, compare LessEqual și writes enabled.
- `ClearDepth=1` și load/store-ul existent sunt suficiente acestui contract. Nu se adaugă reversed-Z, un clear configurabil sau modificări ale `BeginRenderTarget` fără o nevoie demonstrată.
- Uploadul are un singur corp byte-oriented, cu un front-end tipat pentru vertexuri unmanaged. Păstrează index int32, offseturi de binding în bytes, cele trei frame slots și ownership-ul actual. Alignmentul și overflow-ul sunt validate; două layout-uri intercalate nu își suprascriu uploadurile.
- Noile contracte și helperul de conversie nativă rămân în adaptoarele interne. Helperul pentru descriptorii nativi trebuie folosit chiar de `NativeSdlApi`, astfel încât testele să nu valideze o conversie paralelă.

## 3. Call-site-uri care trebuie migrate atomic

Inventar semantic din `refs SdlGpuGraphicsPipelineCreateInfo --exact`, `refs SdlGpuDepthStencilTargetInfo --exact` și căutarea referințelor `UploadGeometry`:

| Consumator | Migrare / contract păstrat | Verificare |
| --- | --- | --- |
| `SdlGpuDrawingResources.GetPipeline` | Layout 2D explicit, depth disabled, stencil și blend neschimbate. | Descriptor + conformance 2D |
| `SdlGpuPrismDeviceResources.GetPipeline` | Input gol explicit, depth disabled, fără target depth. | Fullscreen native pipeline + Prism |
| `ISdlApi.CreateGpuGraphicsPipeline` | Semnătura metodei rămâne; tipul intern create-info este extins/migrat. | Compile + architecture tests |
| `NativeSdlApi.CreateGpuGraphicsPipeline` | Consumă descriptorul; elimină pitch/offset hardcodate. | Teste pe structurile native și pipeline nativ |
| `FakeSdlApi` / colecția `GpuPipelines` | Înregistrează snapshotul întreg, fără a conserva array-uri mutabile ale apelantului. | Asserții structurale, nu string logs |
| `SdlGpuShaderArtifactTests.CreatePipeline` | Alege explicit layout 2D sau gol după shader; nu mai poate trata shaderul fullscreen ca 2D implicit. | Testul nativ existent |
| `Cerberus.Flush` | Același `SdlGpuVertex`, aceeași geometrie și secvență de draw; folosește uploadul comun. | `CerberusTests`, teste backend/allocations |
| `SdlGpuGeometryAllocationTests` | Păstrează probele 2D și adaugă intercalarea a două layout-uri. | Offseturi/bytes/failure tests |

`SdlGpuDepthStencilTargetInfo` are consumatori în `SdlGpuWindowGraphicsSession.BeginRenderTarget`, `NativeSdlApi`, `ISdlApi` și `FakeSdlApi`; aceștia rămân neschimbați semantic. ClearDepth nativ deja 1 nu cere migrare. `BeginRenderTarget` este folosit și de brush captures, layers și `SdlGpuPrismExecutor`: nu se introduce clear implicit suplimentar pe revenirea la părinte.

Inventar de fișiere estimat: `Cerneala.Platforms.Sdl3/Interop/ISdlApi.cs`, `NativeSdlApi.cs`, un fișier intern nou pentru descriere/conversie layout; `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingResources.cs`, `SdlGpuGeometryUploadArena.cs`, eventual call-site-ul `Cerberus.cs`; `Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismDeviceResources.cs`; `tests/Cerneala.Tests.SdlGpu/FakeSdlApi.cs`, `SdlGpuShaderArtifactTests.cs`, `SdlGpuGeometryAllocationTests.cs`, teste noi `SdlGpuPipelineDescriptorTests.cs`; documentația internă `docs/sdl-desktop-backend.md`. Nu se creează fișiere pentru simpla simetrie cu inventarul.

Workflow-ul `.github/workflows/desktop-backends.yml` intră în inventar pentru gate-ul nativ al fundației. În baseline, pasul multiplatformă de unit tests nu setează `CERNEALA_SDL_NATIVE_TESTS`; smoke-urile existente nu înlocuiesc executarea testului nativ de pipeline. Extensia CI de aici este independentă de modul smoke 3D, care apare abia în planul dependent.

## 4. Etape

### Etapa 0 — baseline reproductibil și observabilitate

- [x] Salvează HEAD, diff-ul local relevant și assembly-urile Release folosite drept baseline în `artifacts/rendersurface3d/baseline/` (artefact planificat). Clasifică schimbările străine; dacă suprapunerea împiedică o comparație sigură, oprește.
- [x] Reconfirmă indexul și inventarul complet de call-site-uri de mai sus; orice consumator nou primește o migrare explicită înainte de editare.
- [x] Rulează caracterizarea existentă pentru `SdlGpuShaderArtifactTests`, `SdlGpuDrawingBackendTests`, `SdlGpuCommandRangeStateTests`, `SdlGpuWindowGraphicsSessionTests`, `SdlGpuDeviceOwnerTests`, `CerberusTests` și `SdlGpuGeometryAllocationTests`.
- [x] Arhivează rezultatele și asserțiile de allocations/ordering/lifetime 2D folosite ca baseline în ambele planuri. Nu eticheta simpla trecere a testelor drept măsurătoare CPU/GPU și nu inventa ulterior timing-uri baseline.
- [x] Înregistrează layout-ul 2D, inputul fullscreen gol, depth off și stencil increment/test/decrement din conversia reală. Dacă accesul pentru asserții lipsește, expune conversia ca helper intern fără schimbare de comportament și verifică întâi caracterizarea GREEN.
- [x] Pregătește un proiect ApiCompat parametrizat în artefactele acestei inițiative, după mecanismul `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` din `benchmarks/results/2026-09-04-rendersurface2d-scene-foundation/api-compat.proj`; rezolvă SDK-ul instalat și baseline-ul local, nu copia căile istorice sau suppression-urile altui plan.

**Gate etapa 0**

- [x] Baseline-ul existent este GREEN sau blockerul este explicat și rezolvat înainte de migrare; nicio defecțiune nu este etichetată „probabil unrelated”.
- [x] Descriptorii nativi reali, buffer offsets și API diff-ul pot fi observați de harnessurile identificate; nu există pretins RED 3D în această caracterizare.

Audit părinte acceptat: sursa și hash-urile verificate; caracterizare 78 pass / 4 native opt-in skips, completate de 7/7 native Windows fără skips; descriptori 3/3 rerulați independent; ApiCompat strict exit 0. Dovezi și comenzi în `artifacts/rendersurface3d/baseline/README.md` și `commands.md`. Inventarul depth inițial valid este reutilizat deoarece referințele nu s-au schimbat; query-urile finale cu timeout sunt explicit invalide. Linux/macOS rămân gate-uri ale etapei 3.

### Etapa 1 — vertex layout și depth state independente

- [x] Adaugă întâi teste compile-safe pentru noul contract și apoi RED comportamental pe conversia nativă: layout diferit de 32 bytes, offseturi/formate exacte, depth test/write enabled și stencil disabled. Confirmă că eșecul nu este un fixture sau shader absent.
- [x] Implementează descriptorii immutable, validarea atributelor/stride/offset/enum și conversia lor în structurile native. Refuză layout-uri incoerente înainte de apelul SDL; nu deduce layout-ul din numele shaderului.
- [x] Migrează atomic fiecare consumator din tabel; păstrează separat inputul fullscreen gol și stencil-ul 2D. Testează și combinația depth+stencil, fără a lega unul de celălalt.
- [x] Demonstrează prin teste că două create-info cu layout/depth diferite produc descriptori nativi diferiți. Cache-urile 2D/Prism au layout fix și rămân astfel; testul de cache cu state 3D variabil aparține planului dependent, nu cere un cache general nou în fundație.
- [x] Testează eșecul creării pipeline-ului și disposal-ul shaderelor/pipeline-urilor deja create: niciun handle invalid nu devine intrare de cache.
- [x] Rulează testele focalizate ale etapei.

**Gate etapa 1**

- [x] RED-urile sunt GREEN; descriptorii 2D și fullscreen sunt structural echivalenți cu baseline-ul, iar noul depth state ajunge în descriptorul nativ real.
- [x] Nicio schimbare publică/protejată; `SdlArchitectureTests` și testele de dependency boundaries trec.

Audit părinte acceptat: diff/surse/hash-uri, RED-urile intenționate, teste de descriptori 9/9, native Windows 1/1, architecture 3/3, boundaries 11/11 și ApiCompat strict inspectate. Depth off rămâne echivalent comportamental: CompareOp este explicit Always, nu identitate bit-cu-bit cu vechiul zero/Invalid neconsultat. Comparația controlată Stage0 reconstruit vs Stage1, trei procese fiecare, produce aceleași 12.088 / 7.992.616 / 509.608 bytes; diferența față de observația istorică nu se reproduce ca regresie Stage1. Dovezi: `artifacts/rendersurface3d/foundation-stage1/README.md` și `audit-repair/allocation-attribution.md`.

### Etapa 2 — upload comun pentru layout-uri distincte

- [x] Adaugă RED pentru upload A(2D)–B(vertex unmanaged de test diferit)–A în același frame, verificând bytes, alignment, offseturi, lipsa cycling-ului distructiv și indicii int32.
- [x] Generalizează strict granița de upload; nu modifica stocarea sau batchingul Cerberus într-un motor 3D. Corpul transferului, growth și retirement rămân unice.
- [x] Testează empty/invalid input, overflow, growth în toate cele trei sloturi, eșec map/upload, frame următor și dispose repetat. Un transfer eșuat nu permite reutilizarea unei regiuni încă folosite de alt draw.
- [x] Rulează caracterizarea 2D de allocations după warmup; orice diferență este măsurată și explicată. Nu adăuga conversii array/span alocante pentru apelantul 2D.
- [x] Verifică din nou call-site-urile `UploadGeometry`; lista este limitată la Cerberus, testul existent și noul test până la planul dependent.

**Gate etapa 2**

- [x] Uploadurile cu layout-uri intercalate păstrează exact payload-ul și nu schimbă geometria/ordinea 2D.
- [x] Testele de allocations și lifetime afectate trec; nu există un al doilea transfer arena copiat pentru 3D.

Audit părinte acceptat (2026-09-22): corectat după RED offsetul zero la growth; overflow byte-count/offset și eșec al celui de-al doilea upload acoperite. Rerulare părinte 9/9, native Windows 6/6, SDL 391 pass/191 skips, ApiCompat exit 0. Dovezi în `artifacts/rendersurface3d/foundation-stage2/`. Hash-urile citirilor Roslyn coincid cu toate sursele modificate; status stale/1 identifică numai indexul Markdown editat de părinte, nu C#. Variabilitatea allocations între procese identice este documentată separat; patru din șase procese reproduc exact baseline-ul controlat și trei probe fără tiering sunt stabile. Fără promisiuni CPU/GPU.

### Etapa 3 — gate de livrare a fundației

Amendament utilizator (2026-09-22): execuțiile native Linux/macOS sunt **WAIVED / NEEXECUTATE**, conform indexului, secțiunea 6. Checkpoint-ul blocat de mai jos păstrează istoricul anterior deciziei; lipsa runnerelor nu mai blochează acceptarea, dar nu devine rezultat GREEN. Gate-urile locale și Windows rămân obligatorii.

Checkpoint audit părinte (2026-09-22): implementarea locală a etapei și reparațiile de audit sunt inspectate, dar etapa **nu este acceptată** și checklist-ul rămâne deschis. Pe bytes finali: build Release fără warnings/errors; full suite 5.564 pass, 0 fail, 194 skips (5.758 total, 10 TRX recontorizate independent); Windows native pipeline 1/1 și conformance Drawing/Prism 133/133 fără skips; 133 seturi de capturi; șase shadere verificate; șase RID-uri publicate local; ApiCompat strict fără delta; formatter cumulativ pentru 15 fișiere C# atribuibile, exit 0. Părintele a reprodus și apoi reconfirmat fixul de parsare PowerShell `$Label:`; failure artifacts ajung acum și în root-ul încărcat de CI, cu RED/GREEN permanent 2/2. Dovezi: `artifacts/rendersurface3d/foundation-stage3/audit-repair/README.md`, `commands.md`, loguri/TRX și manifestele finale.

**Blocker:** nu există execuții native pentru Linux x64/Vulkan/lavapipe/Xvfb și macOS arm64/Metal. Workflow-ul modificat nu a fost executat pe hosted runners și nu a fost validat cu un parser YAML complet; blocurile PowerShell au fost parsate și validatorul exact de TRX/capturi a fost exercitat local. Este necesar acces la runner-ele respective sau autorizarea publicării schimbărilor și executării CI. Nu există waiver. Planul controlului rămâne neînceput. Index refresh a trecut și toate sursele C# modificate sunt hash-matched; statusul text stale identifică documentația Markdown, nu o sursă C#.

Worker `/root/foundation_stage_3` este completed/quiescent și rămâne disponibil pentru continuarea aceleiași etape; nu a fost retras ca etapă acceptată. Nu există joburi build/test/index active. Niciun commit/push sau workflow dispatch nu a fost făcut.

- [x] Rulează suitele SDL_GPU și core drawing/RenderSurface2D, conformance-ul 2D/Prism actual și testul nativ de creare pipeline. Păstrează toleranțele existente; nu actualiza golden-uri ca să ascunzi o schimbare de contract.
- [x] Adaugă și rulează în matricea desktop un pas nativ explicit pentru testele de pipeline și conformance-ul 2D/Prism afectat, cu `CERNEALA_SDL_NATIVE_TESTS=1`, TRX și capturi. Pe Linux rulează după configurarea lavapipe, sub `xvfb-run`; asigură restore/build pentru proiectele filtrate. Verifică numărul testelor native executate și absența skip-urilor obligatorii. Publicarea altor RID-uri și smoke-ul singur nu înlocuiesc acest gate.
- [x] Rulează ApiCompat strict față de baseline: delta public/protected trebuie să fie goală pentru core, SDL platform și SDL backend. Nu adăuga suppression pentru o regresie.
- [x] Actualizează documentația internă pentru noul ownership al layout/depth/upload. Dacă apar membri publici, oprește: nu sunt autorizați în acest plan.
- [x] Rulează full suite și build conform comenzilor de mai jos, formatter verification limitat la C# modificat, refresh index și diff-check.

**Gate etapa 3**

- [x] Toate verificările obligatorii sunt GREEN, fără skip nativ prezentat ca pass; numai acum începe planul controlului.

Acceptare părinte (2026-09-22), care înlocuiește checkpoint-ul blocat de mai sus: etapa 3 și fundația sunt **acceptate cu waiver explicit Linux/macOS**, nu conformance multiplatformă GREEN. Auditul independent anterior rămâne valid: 17/17 fișiere relevante sunt identice cu bytes testați, reconfirmate independent de părinte; numai cele trei planuri au fost modificate. Dovezi: `artifacts/rendersurface3d/foundation-stage3/waiver-final-check/`. Pasul CI este livrat și verificat local; execuția hosted și parserul YAML complet rămân nevalidate. Nu mai există gate local nesatisfăcut. Workerul `/root/foundation_stage_3` este retired în ledger, completed/quiescent fără joburi; nu există close tool și nu se pretinde închidere fizică.

## 5. Comenzi de verificare

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~Drawing|FullyQualifiedName~RenderSurface|FullyQualifiedName~Architecture'
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release -- --verify
# Într-un shell de verificare dedicat; nu suprascrie permanent mediul utilizatorului.
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter 'FullyQualifiedName~SdlGpuShaderArtifactTests|FullyQualifiedName~SdlGpuWindowGraphicsSessionTests|FullyQualifiedName~SdlGpuDeviceOwnerTests'
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter 'FullyQualifiedName~SdlGpuDrawingConformanceTests|FullyQualifiedName~PrismSdlGpuPixelConformanceTests'
dotnet build .\Cerneala.slnx -c Release
dotnet test .\Cerneala.slnx -c Release --no-build -m:1
git diff --check
```

Proiectul ApiCompat este creat la etapa 0; comanda sa exactă și exit code-ul intră în raportul etapei 3. Se utilizează aceeași configurație Release și framework pentru assembly-urile comparate.

## 6. Definiția de gata

- [x] Layout/depth sunt contracte interne explicite, fără hardcoding 2D ascuns în adaptor.
- [x] Toți consumatorii existenți au fost migrați și comportamentul lor este păstrat.
- [x] Uploadul comun deservește două layout-uri fără a dubla ownership-ul sau a modifica painter order.
- [x] API public neschimbat, verificările native/2D/Prism/full suite închise și dovezile arhivate.
