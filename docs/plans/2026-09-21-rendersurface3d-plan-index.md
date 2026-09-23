# Plan: RenderSurface3D generic, minimal și extensibil

> Data: 2026-09-21
> Status: încheiat; fundație, prerequisite retained/submit și control acceptate integral, cu waiver explicit pentru native Linux/macOS
> Scop: suprafață generică de randare GPU 3D, compusă în UI-ul retained Cerneala; scheletul este numai primul consumator extern controlului.

## 1. Decizia și limita livrării

Decizia utilizatorului: un `RenderSurface3D` public, thin, care să poată fi extins ulterior; primul consumator este un schelet. Nu se folosește `RenderSurface2D` drept renderer 3D prin proiecția CPU a întregii scene.

Contractul propus de acest plan este **cameră perspective/orthographic + depth + linii și markere de puncte 3D + integrare UI**. Numele tipurilor noi sunt estimări, nu API-uri deja existente. Camera interactivă și selecția sunt demonstrate de un consumator; controlul nu cunoaște oase, anatomie, PixelLab sau un format de rig.

Nu se implementează editorul complet. Modelul de schelet din fixture este numai date de probă pentru ierarhie, pozare și selecție. Scheletul și controllerul rămân în proiectele de verificare, nu în assembly-urile livrate ale frameworkului/backendului. Eliminarea lor nu schimbă contractul controlului; corpusul include și geometrie fără schelet. IK, timeline, undo/redo și persistența proiectului cer o livrare separată.

## 2. Împărțire și dependențe

| Ordine | Plan | Rezultat independent |
| --- | --- | --- |
| 1 | [Fundația GPU](2026-09-21-rendersurface3d-gpu-foundation.md) | Contract intern explicit pentru vertex layout/depth și upload comun, fără schimbarea API-ului public sau a imaginii 2D. |
| 1a | [Prerequisite retained/submit](2026-09-22-sdlgpu-retained-submit-prerequisite.md) | Aprobat explicit 2026-09-22 după RED-ul controlului etapa 0; închide publicarea retained per command buffer înainte de reluarea controlului. |
| 2 | [Controlul și conformance-ul](2026-09-21-rendersurface3d-control.md) | Suprafața publică, executorul 3D, shaderele, lifecycle-ul și demonstrația cu schelet. Depinde de planul 1 complet. |

Separarea evită amestecarea migrării unui adaptor partajat cu introducerea unui control. Shaderele specifice 3D rămân în planul 2: nu sunt o capabilitate independentă de comenzile și contractul vizual pe care îl implementează.

## 3. Baseline verificat și clasificarea dovezilor

Audit pe HEAD `d9a82068ba06e996417c8ed14f1c2e51710982a3`, într-un worktree dirty. HEAD singur nu reproduce schimbările locale. La începutul implementării se arhivează starea exactă; modificările altor activități nu se includ și nu se șterg. `AGENTS.md` era șters în worktree; nu s-a restaurat. `FileTree.md` a fost regenerat conform procedurii. Indexul Roslyn era valid la inspecție.

| Clasificare | Constatare / decizie | Sursă exactă |
| --- | --- | --- |
| Fapt observat | `RenderSurface2D` este `ContentControl`, înregistrează la `Draw`, se invalidează prin coada UI și eliberează state la detach. | `UI/Controls/RenderSurface2D.cs`: `OnRender`, `RecordFrame`, `OnDetached`, `DisposeManagedSession` |
| Fapt observat | Frame-ul este valabil numai în callback; suprafața deține targetul și prezentarea. | `UI/Controls/RenderSurface2DFrame.cs`; pagina canonică `docs-site/documentation/classes/Cerneala.UI.Controls.RenderSurface2DFrame.md` |
| Fapt observat | Suprafața 2D se randează offscreen și este compusă ca quad în targetul părinte. | `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.cs`: `AddRenderSurface`, `RenderSurfaceFrame` |
| Fapt observat | Vertex layout nativ este hardcodat: stride 32, Float2/Float2/Float4, offseturi 0/8/16. | `Cerneala.Platforms.Sdl3/Interop/NativeSdlApi.cs`: `CreateGpuGraphicsPipeline` |
| Fapt observat | Pentru descrierea vertex input, pipeline-ul expune numai `UsesVertexInput`; helperul depth/stencil configurează stencil, nu depth test/write. ClearDepth este deja 1. | `Cerneala.Platforms.Sdl3/Interop/ISdlApi.cs`: `SdlGpuGraphicsPipelineCreateInfo`; `NativeSdlApi.CreateDepthStencilState`, `BeginGpuRenderPass` |
| Fapt observat | Shaderul 2D emite z=0, w=1. Nu este un shader de perspectivă. | `Cerneala.Backends.SdlGpu/Gpu/Shaders/Drawing.vert.hlsl` |
| Fapt observat | Prism fullscreen folosește același descriptor nativ, dar fără vertex input și fără depth target. | `Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismDeviceResources.cs`: `GetPipeline` |
| Fapt observat | Device-ul și `SdlGpuDrawingResources` sunt partajate; backendul și upload arena sunt per sesiune/fereastră. | `SdlGpuDeviceOwner.cs`; `SdlGpuWindowGraphicsSession.cs`: factory, constructor, dispose |
| Fapt observat | Recovery de swapchain poate face submit unui buffer, apoi achiziționa și trimite altul; `CompleteFrame(false)` face submit fără present. | `SdlGpuWindowGraphicsSession.PresentFrame`, `SubmitActiveCommandBuffer`, `CompleteFrame` |
| Fapt observat / risc de integrare | Prism promovează lease-uri retained în `Execute`, înainte de submit-ul sesiunii. Recuperarea compoziției după abort nu este demonstrată doar de un flag dirty 3D. | `Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismExecutor.cs`: `Execute`; `SdlGpuDrawingBackend.EndFrame`; caracterizare obligatorie în planul controlului, etapa 0 |
| Fapt observat | Uploadul comun este tipat `ReadOnlySpan<SdlGpuVertex>`, dar corpul transferă bytes. | `Cerneala.Backends.SdlGpu/Gpu/SdlGpuGeometryUploadArena.cs`: `UploadGeometry` |
| Fapt observat | Shutdown-ul/root replacement are un caz explicit pentru `RenderSurface2D`. Doar detach nu acoperă tot lifecycle-ul. | `UI/Elements/UIRoot.cs`: `ReleaseDrawingResources`; `UI/Hosting/Windowing/WindowApplicationRuntime.cs`: apelurile de la înlocuirea/închiderea contextului |
| Fapt observat | Bounds, resursele și identitatea retained sunt centralizate; kind-urile necunoscute produc excepții. | `Drawing/DrawCommandMetadata.cs`; `Drawing/DrawState.cs`; `UI/Rendering/DrawCommandTransform.cs` |
| Fapt observat | Există shader compiler offline, manifest cu SPIR-V/DXIL/MSL, fake SDL, teste native opt-in și smoke multiplatformă. | `Tools/Cerneala.SdlShaderCompiler/Program.cs`; `Cerneala.Backends.SdlGpu/Shaders/manifest.json`; `tests/Cerneala.Tests.SdlGpu/FakeSdlApi.cs`; `.github/workflows/desktop-backends.yml` |
| Decizie utilizator | 3D real, thin, extensibil; nevoie imediată: schelet. | Discuția care a cerut acest plan |
| Decizie utilizator | Scheletul și funcțiile editorului nu aparțin controlului. | Clarificarea explicită ulterioară cererii de plan |
| Artefact planificat | Contractele 3D, shaderele, testele 3D, contoarele 3D și modul smoke `rendersurface3d` nu există încă. | Planul 2, inventar și etape |
| Necunoscut verificabil | Rasterizarea și costurile 3D pe drivere reale, corectitudinea near-plane și formatelor generate. | Gate-uri native în planul 2; nu se deduc din trecerea testelor 2D |

Planurile istorice cu WindowsDX/MonoGame sunt convenții și dovezi istorice, nu dovadă de existență a unui backend 3D de referință. Se păstrează harnessul actual de referințe 2D; pentru 3D se construiesc oracole geometrice independente.

## 4. Arhitectura țintă și extensibilitate

```text
Aplicație / fixture de schelet
  pose + ierarhie + interacțiuni + selecție
                   |
                   v
RenderSurface3D : ContentControl
  cameră / invalidare / Draw / Content overlay / conversii coordonate
                   |
                   v
snapshot de comenzi 3D platform-neutral
                   |
                   v
SdlGpuDrawingBackend -> executor intern 3D per sesiune
  flush părinte -> target color + depth -> restore părinte -> compoziție 2D
                   |
          SDL_GPU + shadere offline
```

Controlul nu moștenește `RenderSurface2D` și nu primește acces public la device sau render pass. Un singur ciclu de frame, dispatcher și presentation rămâne cel al Cerneala. `IDrawingBackend` nu primește noi metode obligatorii.

| Owner | Stare / responsabilitate |
| --- | --- |
| Aplicație | Schelet, transformări părinte–copil, pose, controller cameră, picking de oase. |
| Control | Camera și versiunea cerută, callback-uri, invalidare; achiziții backend eliberabile la detach/root cleanup. |
| Înregistrare frame | Snapshot coerent de cameră, viewport, comenzi și versiune. Writer-ul devine invalid la complete/abort. |
| Sesiune/fereastră | Executor, upload arena, scratch buffers, tracking submit/cancel per command buffer și state pentru suprafețele acestei sesiuni. Un frame poate avea mai multe buffers la recovery. |
| Pereche control–sesiune | Target color/depth, dimensiuni/format, rezultat pending reutilizabil în buffer și generație submitted reutilizabilă între frame-uri. Nu se partajează doar pentru că două ferestre au același device. |
| Device | Shadere/pipeline-uri immutable, create lazy și eliberate prin ownerul existent; fără cameră sau liste de oase partajate. |

Extensibil înseamnă limite de responsabilitate și contracte aditive, nu un registry de pluginuri anticipat:

- Comenzile 3D nu reutilizează `DrawMesh2D` sau `LayerDepth` cu alt sens.
- Viitoarele mesh-uri pot adăuga payload-uri, vertex layouts și pipeline-uri fără a transforma oasele în responsabilitatea controlului.
- Scene graph, import, skinning și materiale pot fi consumatori/subsisteme ulterioare; nu se rezervă câmpuri, enums goale sau interfețe fictive pentru ele.
- Abstracțiile existente 2D nu sunt redenumite în bloc și nu se extrage o bază generică mare pentru suprafețe.
- Singura partajare nouă de lifecycle este un contract intern mic folosit efectiv de cele două suprafețe și `UIRoot`, descris în planul 2.

## 5. Non-obiective și stop conditions

În afara livrării: `Scene3D`, mesh import GLB/FBX/OBJ, mesh skinned, rig/anatomie publică, IK, timeline, PBR, lumini, umbre, texturi 3D, transparență de geometrie 3D, physics, GPU picking, export de sprite sheet și integrare PixelLab/Meshy. Triunghiurile pentru markere/linii sunt detaliu intern; nu justifică un API public de mesh în acest MVP.

Se oprește pentru o decizie explicită dacă implementarea cere schimbarea contractelor 2D, expunerea SDL, fallback CPU în loc de GPU 3D, un nou backend public, API general de shaders, suport de mesh sau eliminarea unui gate de platformă. Nu se repară oportunist `LayerDepth`, Prism sau codul modificat de alte activități.

Un prerequisite de descoperire în etapa 0 a controlului verifică invalidarea cache-urilor părinte după abort/submit failure. Dacă infrastructura comună cere reparație, se păstrează reproducerea și se cere aprobarea unui plan dependent separat; nu se ascunde problema într-un flush global sau într-o rescriere Prism sub eticheta RenderSurface3D. Inspecția sursei identifică riscul, nu constituie singură reproducerea unui bug existent.

Decizie ulterioară 2026-09-22: după RED-ul permanent și auditul independent din control etapa 0, utilizatorul a răspuns „Yes.” la aprobarea creării și executării planului prerequisite separat, apoi reluării RenderSurface3D. Ordinea devine fundație acceptată → prerequisite retained/submit → reluare aceeași etapă 0 a controlului. Nu autorizează o rescriere generală de cache sau API public nou.

Decizie suplimentară explicită 2026-09-22: utilizatorul cere continuarea până la terminarea RenderSurface3D și aprobă anticipat eliminarea blocajelor necesare („blanket approval”). Extinderea identificată la cache-ul de imagini și la celelalte input cache-uri demonstrate ca afectate este aprobată în același prerequisite; nu mai necesită reconfirmare. Se păstrează RED înainte de fix, ownerul invariantului, auditul per etapă și toate gate-urile ne-waived. Nu se deduc funcții noi, alegeri între contracte publice/arhitecturi ambigue, ștergeri distructive sau publicare externă din această aprobare.

## 6. Politica de verificare comună

Toate căile din comenzile planului sunt relative la rădăcina repository-ului. Fișierele se citesc direct, iar căutarea textuală folosește `rg` / `rg --files`.

Se implementează o etapă odată; checklist-ul etapei se bifează numai după gate și se recitește restul planului. Nu se intercalează etape din planul dependent.

Politica de platformă moștenește matricea desktop actuală: runtime nativ obligatoriu pe Windows x64, Linux x64/Vulkan și macOS arm64/Metal; publish/build pentru celelalte RID-uri din workflow. La redactare nu s-a executat 3D pe nicio platformă. Dacă un runner lipsește, gate-ul este **required but blocked**, nu GREEN și nu waiver implicit. Nu se publică portabilitate din simpla generare a shaderelor.

**Waiver explicit utilizator, 2026-09-22:** „I have no way to provide those runners. Skip them.” Execuțiile native Linux x64/Vulkan și macOS arm64/Metal sunt omise autorizat pentru această inițiativă, inclusiv fundația și gate-ul final al controlului. Sunt **WAIVED / NEEXECUTATE**, nu GREEN și nu dovadă de portabilitate. Această decizie înlocuiește obligația de execuție pe cele două platforme din planurile dependente; nu elimină matricea CI, generarea/verificarea shaderelor sau publicările cross-RID. Native Windows și toate celelalte gate-uri locale rămân obligatorii. Nu autorizează commit, push sau dispatch extern.

Teste RED: caracterizarea existentă trebuie să fie GREEN. Pentru API absent se poate folosi un test compile-safe de prezență/diagnostic; acesta dovedește numai absența API-ului. Testele comportamentale se introduc înaintea logicii respective, cu fixture compilabil; o eroare C# sau lipsa SDL nu este un RED comportamental. O declarație minimă necesară compilării testului nu se raportează drept implementare a contractului.

Capturile native se fac exclusiv cu `Window.SaveScreenshot`. Imaginile rezultate pot fi analizate numeric; nu se substituie capturi OS. Interacțiunile sunt automate prin input, nu declarate manual validate. Un test care setează camera direct verifică randarea, nu orbitarea prin mouse.

Nu există un prag de CPU/GPU cerut de utilizator. Gate-urile obligatorii de performanță sunt comportamentale: lipsa reconstrucției/uploadului 3D în idle și resurse cu lifecycle bounded. Timingurile sunt măsurători, nu promisiuni de FPS sau zero allocations.

## 7. Audit efectuat la redactare

Au fost citite integral `RenderSurface2D` și partial-ul de presentation, frame-ul principal, interfețele sursei, metadata, upload arena și ownerul device-ului. Au fost urmărite integrarea offscreen, adaptorul nativ, creatorii de pipeline 2D/Prism, root cleanup, time invalidation, state analysis, consumerul SourceGen și infrastructura smoke/CI. Call-site-urile care migrează sunt inventariate în planurile dependente.

Auditul de tip a întâlnit o limitare a CLI: `refs` pentru numele complet al `RenderSurface2D` a raportat candidați multipli pentru cele două declarații partial. Inventarul de utilizări al tipului a fost completat prin căutare textuală după acel eșec; inventarele metodelor/descriptorilor care migrează au fost obținute semantic. Consumatorul extern `Tetrisish/TetrisGameSurface.cs` a fost citit integral: moștenește suprafața 2D, se abonează la `Draw` și are propriul lifecycle; nu necesită migrare. Cazul hardcodat SourceGen `IsRenderSurfaceSceneElement` se referă la `.Scene` 2D și rămâne deliberat neschimbat, deoarece MVP-ul 3D nu are scene graph.

Contradicții rezolvate înainte de checklist:

- „Adăugăm doar un shader” contrazice layout-ul nativ fix și uploadul tipat 2D: există planul de fundație, cu migrarea Prism fullscreen inclusă.
- „Target cu depth înseamnă suport 3D” este fals: depth test/write trebuie configurate explicit; stencil-ul 2D rămâne neschimbat.
- „Dispose numai la detach” omite root shutdown/resource replacement: se include integrarea `UIRoot`.
- „State per device pentru fiecare suprafață” poate captura o sesiune greșită: state-ul 3D este per control–sesiune.
- „FrameVersion devine clean după draw” nu dovedește că GPU work a fost submitted: planul separă pending replay de commit per buffer și nu echivalează submit cu present sau fence completion.
- „Smoke 3D există deja” este fals: parserele C# și PowerShell trebuie extinse înainte de folosirea noului mod.
- Gate-ul de cache cu layout/depth variabil aparține executorului 3D, nu fundației: cache-urile actuale 2D/Prism au layout fix. Fundația verifică descriptorii, fără un cache general speculativ.
- Probe-ul de cost 3D și fixture-ul nativ de pixel tests trebuie create înaintea măsurătorilor, nu presupuse existente. Runnerul de benchmarks actual țintește `net8.0-windows`; măsurătorile lui nu constituie dovadă de performanță Linux/macOS.

Comenzi executate în timpul planificării:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter 'FullyQualifiedName~SdlGpuShaderArtifactTests|FullyQualifiedName~SdlGpuCommandRangeStateTests|FullyQualifiedName~SdlGpuWindowGraphicsSessionTests|FullyQualifiedName~SdlGpuDeviceOwnerTests' --logger 'console;verbosity=minimal'
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release -- --verify
```

Rezultat: 37 teste trecute, 1 nativ skipped, 0 eșecuri; cele 6 definiții shader existente verificate, exit 0. Nu s-au executat full suite, teste native opt-in, screenshot 3D, benchmark 3D sau prototip 3D. Verificarea de shader confirmă artefactele existente, nu pipeline-ul viitor. Nu există rezultate RED/GREEN 3D înainte de implementare.

### Audit ulterior la cererea utilizatorului

Corecții ale planului, nu defecte runtime declarate reproduse:

| Defect de plan | Dovadă / consecință | Corecție |
| --- | --- | --- |
| Commit definit global per frame; replay pending nespecificat | `PresentFrame` poate face două submit-uri; repetarea callback-ului în capturi poate produce conținut diferit pentru aceeași identitate. | Commit/cancel per buffer, reuse pending pentru aceeași înregistrare, teste distincte pentru eșec înainte/după primul submit. |
| Recuperarea targetului 3D confundată cu recuperarea întregii compoziții | `SdlGpuPrismExecutor.Execute` promovează rezultate înainte de submit. Un cache părinte trebuie verificat, nu presupus invalidat. | Caracterizare blocantă înainte de implementare, teste 3D sub Prism/capture și stop pentru prerequisite comun dacă este necesar. |
| Semantica de abort neancorată în API-ul sesiunii | `CompleteFrame(false)` trimite bufferul; `RenderPngCore` îl apelează în `finally`. | Contract explicit, inventar de apelanți și teste care nu confundă no-present cu abort sau submit cu înregistrare completă. |
| Toleranțe definite după primul gate vizual; prag opac aplicabil ambiguu efectelor | Gate-ul nativ era în etapa 2, toleranțele în etapa 5. | Oracole/toleranțe în etapa 0, separare interior opac/AA/Prism și reutilizare la toate gate-urile. |
| RED markup prea devreme și scope de fixture insuficient protejat | API-ul apărea în etapa 1, authoring-ul abia în etapa 4; titlurile sugerau un control specializat pe schelet. | RED de prezență în etapa 1, markup în etapa 4 (caracterizare GREEN dacă descoperirea existentă ajunge), limite de dependență și scenă generică fără schelet. |
| Gate nativ multiplatformă fără pas CI opt-in explicit | Workflow-ul setează variabila native în full regression Windows, nu în unit tests din matrice. | Fundația livrează pasul nativ cu display/driver/număr de teste; controlul îl extinde ulterior cu 3D. |
| Comparație de timing cu baseline care nu era cerut | Fundația înregistra teste, nu timing de probe 2D. | Comparație numai pe aceleași caracterizări de allocations/ordering/lifetime; măsurători 3D separate. |

În audit s-au recitit planurile și traseele concrete de sesiune/submit/cache, s-au reconfirmat referințele `CompleteFrame` și configurația CI. Indexul Roslyn era valid, cu 0 fișiere dirty în index. Rezultatele 37/1 și verificarea celor 6 shadere de mai sus aparțin redactării inițiale; nu au fost rerulate în auditul documentelor. Nu s-a implementat cod 3D. Planul poate începe numai cu gate-urile de descoperire și baseline, nu presupunând că riscul de cache comun este deja rezolvat.

## 8. Definiția de gata

- [x] Planul de fundație este încheiat cu migrarea tuturor consumatorilor și fără schimbare vizuală 2D/Prism.
- [x] Planul controlului este încheiat; un schelet poate fi inspectat cu orbit/pan/zoom și selectat prin input, fără proiectarea CPU a scenei în `RenderSurface2D`.
- [x] Depth, clipping, DPI, invalidare, failure recovery și multi-window au dovezi automate și native conform matricei.
- [x] Suprafața publică și documentația canonică/manifestul sunt sincronizate; nu există API public de rig sau detalii SDL scurse în core.
- [x] Full suite, build, conformance 2D și corpusul 3D aplicabil au trecut; un gate obligatoriu blocat împiedică închiderea inițiativei.
- [x] Limitele MVP-ului sunt consemnate; viitoarea extensie la mesh/skinning nu este prezentată ca implementată.

## 9. Jurnal de orchestrare

| Plan / etapă | Worker | Stare | Dovezi / audit | Ultima verificare acceptată |
| --- | --- | --- | --- | --- |
| `2026-09-21-rendersurface3d-gpu-foundation.md`, Etapa 0 — baseline reproductibil și observabilitate | `/root/foundation_stage_0` (gpt-5.6-sol, high, context nou) | accepted; retired (completed, fără joburi; nu există close tool) | Audit independent sursă/diff/hash/loguri; reparațiile de evidență acceptate în `artifacts/rendersurface3d/baseline/README.md`, `commands.md`, `final-workspace-manifest.json`. Parent `New-FileTree` este delta generată explicit separată. | Caracterizare 78 pass + 4 skips opt-in; native Windows 7/7 fără skips; descriptori 3/3 rerulați de părinte; ApiCompat strict exit 0. |
| `2026-09-21-rendersurface3d-gpu-foundation.md`, Etapa 1 — vertex layout și depth state independente | `/root/foundation_stage_1` (gpt-5.6-sol, high, context nou) | accepted; retired (completed, fără joburi) | Audit independent și două reparații de dovezi/teste în `artifacts/rendersurface3d/foundation-stage1/`; A/B reconstruit nu arată regresie de allocations. | Descriptori 9/9; focused 16 pass + 1 skip opt-in acoperit separat native 1/1; SDL 380 pass/191 skips înainte de cele două teste noi; architecture 3/3, boundaries 11/11, ApiCompat exit 0. |
| `2026-09-21-rendersurface3d-gpu-foundation.md`, Etapa 2 — upload comun pentru layout-uri distincte | `/root/foundation_stage_2` (gpt-5.6-sol, high, context nou) | accepted; retired (completed, fără joburi) | Audit independent, growth RED→GREEN și probe de failure/overflow; source/hash/raw logs în `artifacts/rendersurface3d/foundation-stage2/`. Stale index este doar Markdown; sursele C# hash-matched. | Rerulare părinte upload 9/9; native Windows 6/6; SDL 391 pass/191 skips; ApiCompat exit 0. |
| `2026-09-21-rendersurface3d-gpu-foundation.md`, Etapa 3 — gate de livrare a fundației | `/root/foundation_stage_3` (gpt-5.6-sol, high, context nou) | accepted cu waiver explicit; retired (completed, fără joburi) | Reparații auditate: PowerShell, failure-artifacts, formatter cumulativ. Dovezi finale `artifacts/rendersurface3d/foundation-stage3/audit-repair/`. Native Linux/macOS neexecutate, waived explicit 2026-09-22; CI nu a fost dispatch-uit. | Local: full suite 5.564 pass/0 fail/194 skips, build 0 warnings/errors; native Windows 1+133 fără skips; ApiCompat și formatter pass. 17/17 hash-uri reconfirmate independent; etapa 3 acceptată conform waiver-ului din secțiunea 6. Dovezi `foundation-stage3/waiver-final-check/`. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 0 — contract testabil, baseline și harness | `/root/control_stage_0` (gpt-5.6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Prerequisite închis separat. Audit independent source/tests/docs/oracle și două reparații măști. `control-stage0/resume-after-prerequisite/`; contract API shape acceptat, nu implementat. | Părinte 59/59; SDL456 pass/194 opt-in skips; native22/22; core11/11; formatter/API GREEN. 9 surse+4 assemblies+9TRX hash-matched. |
| `2026-09-22-sdlgpu-retained-submit-prerequisite.md`, Etapa 0 — baseline, contract nativ și closure de reproducere | `/root/retained_submit_stage_0` (gpt-5.6-sol, high, context nou) | accepted; retired (completed/quiescent fără joburi; fără close tool) | Audit sursă/teste/inventar și reparații observatori; closure confirmat sesiune, Prism, 2D/brush, sampled textures și atlas text. Scope aprobat explicit. Fără fix production/Graphix. Dovezi `artifacts/rendersurface3d/cache-submit-prerequisite/stage0/approval-continuation/`. | Rerulare părinte 25 total: 14 pass / 11 RED intenționate / 0 skips, `stage0/parent-audit/parent-expanded-final.trx`; source/binary/TRX 7/7 hash-uri match. Descoperire acceptată, nu fix GREEN. |
| `2026-09-22-sdlgpu-retained-submit-prerequisite.md`, Etapa 1 — commit și abandon per command buffer | `/root/retained_submit_stage_1` (gpt-5.6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit independent + patru reparații RED→GREEN: fast-path text, pins, surface/session lifecycle, atlas write footprints/dirty eviction. Autoritativ `stage1/parent-repair-4/`; toate 11 hash-uri finale corespund. | Părinte 100/100 focused; SDL 443 pass/191 native skips; core 1233 pass/2 native skips; Tetris 30/30. Gate-uri native/final în etapa 2, nu implicit trecute. |
| `2026-09-22-sdlgpu-retained-submit-prerequisite.md`, Etapa 2 — conformance, compatibilitate și predare controlului | `/root/retained_submit_stage_2` (gpt-5.6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit docs/diff/raw logs, 11 surse + 5 assemblies + 16 TRX hash-matched; `stage2/parent-audit-source-and-trx.json`. | Părinte 100/100; full 5616 pass/194 opt-in skips; Windows native 1+75+133 fără skips; build/API/shaders/formatter trecute. Linux/macOS waived, neexecutate. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 1 — contracte matematice, recorder și invalidare | `/root/control_stage_1` (gpt-5.6-sol, high, context nou) | accepted cu excepția istorică test-first explicită; retired (completed/quiescent; fără close tool) | Audit independent source/API/docs și reparații RED→GREEN; `control-stage1/final-repair/`, 26 surse+4 binare+10 TRX hash-matched. | Părinte core50/50, SDL reconstruit457 pass/194 opt-in skips; core afectat988 pass/2 skips. Formatter/docs/diff-check0; strict API1 doar 10 adăugiri aprobate, fără breaks/suppressions. GPU rămâne etapa2. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 2 — executor GPU, primitive și integrare shader | `/root/control_stage_2` (gpt-6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit independent și reparații RED→GREEN; decizia 1x explicită implementată/documentată. `control-stage2/one-x-decision/`; 23surse+3binare+6TRX+22PNG hash-matched. | Părinte native51/51 și route final14/14; worker native55/55, SDL471 pass/210 skips, core3984 pass/2 skips, shader8/8. Windows8x fizic, 2/4/refuz1x fake; Linux/macOS waived. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 3 — lifecycle, failure recovery și retained composition | `/root/control_stage_3` (gpt-6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit independent și două runde de reparații RED→GREEN; `control-stage3/audit-round2/`, 16surse+5binare+11TRX+13PNG hash-matched. | Părinte79/79 native/focused; SDL508+214opt-in skips, core3984+2skips, parity133/133, Tetris30/30; formatter/docs/diff pass. Native handle counts/fault injection nepretinse. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 4 — authoring și consumatorul de schelet | `/root/control_stage_4` (gpt-6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit independent și reparații zoom/picking RED→GREEN; `control-stage4/final/handoff-round1.md`,17surse+37artefacte+11binare hash-matched. | Părinte13fixture+3SourceGen+1preview și smoke publicat8capturi; worker610SourceGen,227Language+1skip explicit,14Preview,20native; build/format/diff pass. Probe verificat funcțional, nu măsurători etapa5. |
| `2026-09-21-rendersurface3d-control.md`, Etapa 5 — conformance, costuri, documentație și închidere | `/root/control_stage_5` (gpt-6-sol, high, context nou) | accepted; retired (completed/quiescent; fără close tool) | Audit final independent și reparații: resize shared-scheduler, Prism allocations, DPI/AA/docs/cost report; `control-stage5/final-handoff.md`,211evidence+15sourcecopies hash-matched. | Full6245pass/6skip explicite/0fail; părinte88native+39layout; native3D27,2DPrism133; build/shader8/APIaditiv/docs/formatter64/6RID verificate. Linux/mac waived, timing inconcludent, shader intermittent inițial rootcause necunoscut. |

Execuția din 2026-09-21 folosește un singur worker per etapă; părintele deține checkpoint-urile, workerul implementarea. Modificările preexistente, inclusiv ștergerea `AGENTS.md`, nu se restaurează și nu se atribuie inițiativei. Politica inițială fără waiver este amendată exclusiv de decizia explicită din 2026-09-22, secțiunea 6.

Checkpoint de reluare, 2026-09-22: utilizatorul a cerut oprire completă, apoi a actualizat instrucțiunile locale/tooling-ul și a cerut explicit continuarea. Același worker al etapei 2 reia din artefacte; rularea full-suite-final întreruptă (wrapper PID 7448 și descendenți opriți) nu este gate GREEN. Nicio etapă suplimentară nu este acceptată prin această reluare.

Checkpoint de reluare control etapa 0, 2026-09-22: după pauza globală cerută explicit, utilizatorul a autorizat continuarea. Același worker /root/control_stage_0 reia din artefacte. Tooling-ul RoslynRepoIndexer și RoslynBro a fost șters de utilizator; absența proiectului CLI și README-ului a fost verificată. Navigarea continuă prin citire directă și căutare textuală conform fallback-ului pentru tooling indisponibil; nu se revendică query semantic sau index nou. Modificările/ștergerile utilizatorului sunt păstrate. Niciun gate nou nu este bifat prin reluare.

Audit control etapa 0: caracterizările și observabilitatea internă au fost inspectate; gate-ul măștilor vizuale rămâne incomplet. Testul inițial verifică numai constantele toleranței, nu clasificarea interior/bandă/exterior. Același worker trebuie să predea oracle reutilizabil și măști analitice testate înainte de acceptare; pragurile nu se schimbă. Batch gol no-op este decizia explicită a utilizatorului.

Audit control etapa 1: sursele/API/docs/testele au fost citite independent. Returnat aceluiași worker pentru reproducerea și corectarea conversiilor la transform singular și ray cu valori finite extreme, dovezi reale de zero layout și integrare Prism, oracle independent și gate formatter verify. Workerul a raportat că primele teste comportamentale au fost scrise după logică și au fost deja GREEN: aceasta este o abatere test-first, nu RED valid; istoricul este păstrat și dovezile retrospective nu îl rescriu. Etapa rămâne neacceptată.

Decizie utilizator privind orchestrarea, 2026-09-22: workerii noi folosesc GPT-6 Sol High (model gpt-6-sol, reasoning_effort high, fork_turns none). Skillul personal cerneala-orchestrate-plan a fost actualizat; istoricul workerilor GPT-5.6 Sol rămâne factual. Workerul curent control_stage_1 continuă reparația aceleiași etape, deoarece instrumentele nu permit schimbarea modelului în loc. Regulile de audit și gate-urile nu se schimbă.


Checkpoint audit etapa2, 2026-09-23: metoda per-sample SV_Coverage a eliminat 56 pixeli dependenți de ordinea desenării în scenariul overlap; rerulare independentă parent-round1-native-audit.trx 51/51. Nicio extindere a descriptorului platformei nu a fost necesară. La numai1x, metoda oferă margine binară, incompatibilă cu cerința fractional-AA; utilizatorul a fost întrebat explicit între refuz3D sau mod1x limitat documentat. Nicio alegere implicită, waiver nou sau bifă etapa2. Workerul este quiescent și nu se trece la etapa3 până la decizie.

Decizie explicită utilizator, 2026-09-23 — „Varianta 1”: RenderSurface3D necesită un sample count MSAA comun color+depth de minimum 2x; dacă există numai 1x comun (sau niciun mod compatibil), ruta 3D refuză explicit cu NotSupportedException. Nu există fallback 3D cu margini fără antialias. Comportamentul și selecția sample-count pentru 2D rămân neschimbate. Contractul se implementează după RED și se documentează canonic în etapa 2; nu waive-uiește alte gate-uri.

Audit etapa3 runda2: reparațiile epoch/pinning și matricea shared-owner/Prism au fost citite. A rămas o cale de copiere neverificată: DrawCommandTransform.Translate/ApplyOpacity reconstruiesc comanda prin factory, iar constructorul recapturează epoch-ul live în locul celui înregistrat. Același worker primește reproducerea stale-command după transform; etapa rămâne deschisă.


Checkpoint final, 2026-09-23: toate etapele și obiectivele aplicabile sunt acceptate după audit independent. Detaliile și limitele sunt în acceptarea etapei5 și artifacts/rendersurface3d/control-stage5/final-handoff.md. Waiver-ul Linux/macOS, skip-urile explicite preexistente, CI hosted neexecutat și incertitudinea eșecului inițial de pipeline rămân vizibile; închiderea nu le transformă în rezultate GREEN. Nu există job de implementare/verificare rămas activ sau publicare externă.
