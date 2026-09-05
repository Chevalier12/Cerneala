# Etapa 4 — checkpoint verificat pe SDL GPU

Data: 2026-09-05. Etapa 4 este verificata automat pentru tinta SDL GPU aprobata explicit de utilizator. Etapele 5/6 nu sunt acoperite de acest checkpoint. Validarea vizuala umana nu a fost efectuata.

Decizie ulterioara explicita a utilizatorului: planul continua cu SDL GPU drept tinta; defectul MonoGame ramane documentat si nu mai blocheaza livrarea. Sectiunile de diagnostic de mai jos sunt istoric verificat, nu cerinte de reparare MonoGame. Corpusul nativ final SDL este arhivat in `SdlGpu/`. Nu se declara MonoGame reparat si nu se sterg regresiile lui.

## Gate final SDL — 2026-09-05

```powershell
dotnet build tests/Cerneala.SdlGpuSmoke/Cerneala.SdlGpuSmoke.csproj --no-restore -v minimal
dotnet run --no-build --no-restore --project tests/Cerneala.SdlGpuSmoke/Cerneala.SdlGpuSmoke.csproj -- --mode scene-debug --artifacts docs/plans/evidence/2026-09-04-scene-import-stage4/SdlGpu
```

Build: zero warnings/errors. Rularea nativa: exit 0, `SDL_GPU_SMOKE_OK mode=scene-debug flags=7 effects=Aspect,Motion,Prism`. Se foloseste configuratia SDL normala, fara switch-ul temporar MSAA al investigatiei comparative.

- 12 capturi reale 800x525, exclusiv `Window.SaveScreenshot`: off, fiecare dintre cele sapte flags separat, all, Aspect/Motion/Prism, pan/zoom si off restaurat. `SdlGpu/debug-backend.json` contine hash-uri, numarul de pixeli schimbati si observatiile scenei.
- Fiecare flag separat produce categoria sa de comenzi prin testele core si intre 1.737 si 13.322 pixeli schimbati in captura sa nativa. Oprirea overlay-ului restaureaza exact imaginea initiala: zero pixeli diferiti, acelasi SHA256.
- In toate cele 12 scenarii, picking-ul geometric si rezultatele raycast sunt identice. Modelul hartii isi pastreaza identitatea; indexul ramane la 6 intrari, un rebuild initial si zero actualizari incrementale.
- Cu toate flags active sunt vizitate numai 3 chunks, 36 tile-uri, 5 collidere, un tile promovat si 24 celule de navigatie; cele 1.024 chunks indepartate nu sunt enumerate. Pan/zoom reduce vizitele la 33 tile-uri si 22 celule de navigatie.
- Aspect este verificat prin sursa efectiva a UiProperty. Motion este verificat la endpoint-uri explicite in proba nativa si la interpolare cu ceas determinist in testul core. Prism este atasat exclusiv overlay-ului. Capturile `debug-all`, `debug-effects`, `debug-zoom` au fost inspectate de agent; nu sunt prezentate drept validare umana.
- Bounds-ul frame-ului este exact 800x525 pixeli, identic capturii, nu dimensiunea logica 640x420. Grosimea/fontul sunt in unitati locale de scena, scalate de zoom conform documentatiei canonice.
- Rezultatele core/SourceGen/manifest si masurarea CPU deja verzi de mai jos sunt reutilizate: nu a fost modificat codul lor ulterior. Verificarea surogat prin MonoGame nu mai este gate, conform deciziei utilizatorului.

Nu s-au relaxat asertiuni SDL si nu s-au eliminat teste. Costul overlay-ului oprit este verificat la zero comenzi/alocari proprii dupa warmup; costul pornit ramane cel masurat mai jos, nu o promisiune de cost GPU sau zero alocari pentru intreaga scena.

Actualizare dupa autorizarea investigatiei GPU/driver: [probe de profil, matrice MSAA, mostre individuale si instalarea Graphics Tools](gpu-driver/README.md). Setarile globale interogate sunt neschimbate. Proba Direct3D11 separata trece, deci defectul nu este atribuit definitiv driverului. Graphics Tools a fost instalat cu aprobare explicita, fara restart necesar; debug layer-ul este confirmat activ in toate cele 24 cazuri ale matricei. Nu raporteaza utilizare invalida a API-ului in proba, dar defectul hardware RGBA8/8x persista. Regresia permanenta ramane 1 PASS / 2 FAIL.

## Implementare si regresii

- `Scene2DDebugOverlay`: flags independente, post-pass separat de gameplay, culling, navigatie externa, contoare si proprietati Aspect/Motion/Prism. Testele core de overlay au trecut (12 teste); documentatia canonica si manifestul sunt sincronizate, iar testul manifestului este verde. Auditul API si documentatia integrata a etapei 6 raman separate.
- Contururile folosesc `DrawPen` centrat, cu identitate reutilizata; opacitatea 1 nu introduce un layer de compunere inutil. Testul dedicat a fost RED, apoi GREEN.
- Capturile native au expus un defect SDL: `RenderSurface2DFrame.Bounds` era 640x420 unitati logice pentru o suprafata 800x525 pixeli la DPI 1.25. Contractul canonic al frame-ului cere pixeli locali. Backend-ul SDL inregistreaza acum dimensiunile fizice si foloseste temporar scala 1 in callback, restaurand scala parinte ulterior. Asertiunea permanenta din fixture a fost RED, apoi GREEN pe ambele backend-uri. Proiectul SDL: 119 PASS, 5 SKIP; raportul este arhivat aici.

## Istoric: defect MonoGame hardware, neblocant prin decizia utilizatorului

Observatie, nu presupunere de ownership: la doua contururi opace identice, conturul anterior influenteaza pixelii finali cand intre ele se deseneaza text sau un dreptunghi semitransparent. Fara continut intercalat, conturul anterior este ascuns complet.

Regresia permanenta este `AlphaBlendRenderingTests.OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent`. Scenariul foloseste doua suprafete independente 180x90, contururi centrate de 0.75, rotatie 0.2 radiani, scala 1.5 si translatie (20,25). Compara G/continut/M cu continut/M. Diferenta RGB maxima permisa este 1/255. Masurat: control fara continut PASS; text 47/255; dreptunghi semitransparent 25/255. Nu s-a relaxat toleranta.

Rerularea dupa eliminarea probelor temporare: `scene-import-stage4-checkpoint.trx`, 13 PASS (12 overlay + control fara continut), 2 FAIL pentru cele doua cazuri de occlusion de mai sus. Indexul a fost actualizat (11 warnings existente); `git diff --check` pentru fisierele checkpoint-ului nu raporteaza erori de whitespace. App-ul SDL nu mai are diff pentru schimbarea temporara MSAA.

Izolarea suplimentara a eliminat integral desenarea Cerneala: un `RenderTarget2D` MonoGame cu 8 mostre, `BasicEffect`, doua triunghiuri opace identice si un dreptunghi semitransparent intre ele reproduc aceeasi diferenta de 25/255. Varianta numai BasicEffect si varianta cu SpriteBatch intercalat esueaza la fel. Nu este necesar text, Scene2D, retenție, overlay sau schimbare de shader.

Aceeasi proba minima a fost rulata pe:

| Cale | Rezultat |
| --- | --- |
| NVIDIA GeForce RTX 2060, driver 32.0.15.9159, D3D feature level 11_0, sample description {8,0} | FAIL, delta maxima 25/255 |
| MonoGame `GraphicsAdapter.DriverType.FastSoftware` (WARP) | PASS, delta cel mult 1/255 |

`scene-import-stage4-native-msaa-warp-probe.trx` contine cele doua rezultate. Aceasta comparatie izoleaza diferenta pe calea hardware; nu identifica singura daca responsabilitatea exacta este driverul, configuratia lui sau o interactiune MonoGame/D3D. Nu s-a modificat driverul si nu s-a introdus un workaround in renderer. Contractul Direct3D specifica blending independent pe fiecare mostra: [Microsoft — Configuring Blending Functionality](https://learn.microsoft.com/en-us/windows/win32/direct3d11/d3d10-graphics-programming-guide-blend-state).

Proba temporara `NativeMsaaProbe`, footer-ul `CERNEALA_DEBUG_STROKE_PROBE` si schimbarea temporara globala `UseMultisampling` a aplicatiei SDL au fost eliminate. Snapshotul local al probei este pastrat numai in `.artifacts/scene-import-stage4/red/AlphaBlendRenderingTests.native-msaa-probe.cs.txt`, in afara sursei compilate. Nu exista un switch permanent care ascunde defectul prin WARP.

Capturile locale `.artifacts/scene-import-stage4/native-dpi-green/WindowsDx` si `.artifacts/scene-import-stage4/native-msaa/SdlGpu` au fost produse exclusiv prin `Window.SaveScreenshot`. Cu MSAA aliniat temporar, 11 din 12 capturi au delta maxima 0 sau 1; `debug-all` pastreaza 95 pixeli nerezolvati dupa toleranta existenta (3/255, vecinatate 1 pixel), delta maxima 38. Acestea sunt dovezi de diagnostic, nu corpus final acceptat. WARP a fost verificat numai pentru proba minima, nu pentru intregul corpus.

## Reproducere permanenta

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter FullyQualifiedName~OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent --logger "trx;LogFileName=occlusion.trx"
```

## Rezolvarea cerintelor ramase din checkpoint-ul initial

- Rezolvat prin decizie explicita: MonoGame nu mai este tinta obligatorie; testul WARP nu este folosit pentru a declara hardware-ul MonoGame verde.
- Cerinta de aliniere MSAA intre doua backend-uri a fost retrasa odata cu tinta WindowsDX. Configuratia normala SDL este folosita reproductibil, fara schimbare globala temporara.
- Dovezile de cost sunt integrate mai jos si raman aplicabile codului neschimbat.
- Corpusul SDL final si checkpoint-ul etapei 4 sunt inchise; cele doua regresii MonoGame raman explicit rosii, neblocante.
- Demonstratia integrata, API Compat si suita completa aplicabila apartin etapelor 5/6. Nu au fost executate anticipat.

Validarea vizuala umana nu a fost efectuata.

## Regresii afectate (verificate)

- `scene-import-stage4-core.trx`: 188 PASS, 0 FAIL, 0 SKIP, incluzand testele scene/tilemap/collision/animation si overlay. Filtrul nu include cele doua regresii MSAA din `AlphaBlendRenderingTests`; acestea raman explicit RED in raportul checkpoint.
- `scene-import-stage4-sourcegen-verified.trx`: 2 PASS; markup real Aspect/Motion/Prism pe overlay si binding de model fara parser in generator. Un filtru initial dupa numele fisierului nu a gasit teste (clasa este partiala `UiMarkupGeneratorTests`); s-a corectat filtrul la trait-ul real `SceneImportStage=0`. Raportul gol nu este folosit ca dovada.
- `scene-import-stage4-docs.trx`: testul `ApiDocumentationManifestIsValidAndReferencesExistingFiles` PASS.

Comenzi: testele core cu filtrul scene/collision/tilemap din etapa 3 extins cu `FullyQualifiedName~SceneDebugOverlay`, SourceGen cu `--filter SceneImportStage=0`, VisualStudio cu `--filter ApiDocumentationManifestIsValidAndReferencesExistingFiles`. Toate rapoartele sunt arhivate in acest director. Succesul acestor subseturi nu inseamna ca suita completa este verde.

## Cost CPU de inregistrare (verificat)

Runner permanent: `SceneDebugOverlayBenchmarkRunner`, intrarea `--scene-debug-overlay` din proiectul de benchmark. Build Release: 0 warnings, 0 errors. Pentru fiecare scenariu: 512 iteratii warmup, apoi 2048 masuratori. Nu exista un prag de timp inventat; gate-urile executabile verifica egalitatea alocarilor/comenzilor absent-oprit, zero alocari/comenzi proprii oprit, culling-ul si zero rebuild-uri statice.

Rularea initiala `overlay-cost.json` foloseste setarile JIT implicite si prezinta diferente mari intre scenariile executate devreme/tarziu. Nu este folosita pentru comparatia de cost. Rularea `overlay-cost-stable.json` fixeaza `DOTNET_TieredCompilation=0` doar pentru procesul benchmark, apoi restaureaza mediul. Astfel comparatia nu depinde de tranzitii tiered-JIT; acestea nu au fost instrumentate individual si nu sunt declarate cauza unica a variatiei initiale. Zgomotul scheduler-ului ramane posibil.

```powershell
dotnet build benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj -c Release --no-restore
$previousTieredCompilation = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --no-build --no-restore -c Release --project benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj -- --scene-debug-overlay docs/plans/evidence/2026-09-04-scene-import-stage4/overlay-cost-stable.json
} finally {
    $env:DOTNET_TieredCompilation = $previousTieredCompilation
}
```

Viewport: 256x192, 192 tile-uri vizibile in 12 chunks, un tile promovat, 8 collidere vizibile, 32 collidere remote, 192 celule de navigatie vizibile. Variantele contin fie 0, fie 4096 chunks suplimentare in afara viewport-ului.

| Chunks remote | Scenariu | P50 (us) | P95 (us) | B/op | Comenzi/frame |
| ---: | --- | ---: | ---: | ---: | ---: |
| 0 | scena fara overlay | 35.3 | 56.4 | 23164.22 | 20 |
| 0 | scena cu overlay oprit | 35.4 | 44.9 | 23164.22 | 20 |
| 0 | scena, toate flags | 531.4 | 856.6 | 432450.53 | 472 |
| 4096 | scena fara overlay | 35.7 | 55.1 | 23164.22 | 20 |
| 4096 | scena cu overlay oprit | 35.8 | 48.9 | 23164.22 | 20 |
| 4096 | scena, toate flags | 530.1 | 709.5 | 432450.53 | 472 |

Apelul izolat `Overlay.Record` oprit: 0 B/op, 0 comenzi in ambele variante; P50/P95 0.1 us, la rezolutia timerului folosit. Nu se afirma ca intreaga scena are zero alocari: aceasta continua sa aloce aproximativ 23 KB/frame, identic cu/fara overlay oprit.

Pornit: in ambele variante exact 12 chunks candidate, 192 tile-uri vizitate, 8 collidere, un tile promovat, 192 celule de navigatie si 428 primitive debug. Inregistrarea scenei reutilizeaza 13 segmente statice si reconstruiește 0. Adaugarea celor 4096 chunks remote nu mareste contoarele debug sau alocarile masurate. Contoarele hartii din scenariul `overlay-only-disabled` sunt ultimul snapshot al scenei anterioare, nu lucru executat de acel apel izolat.

Aceste cifre masoara numai inregistrarea comenzilor CPU, nu timpul GPU, submission-ul backend, draw calls native, timpul total de frame sau invalidarile/layout-ul unei ferestre. Costul tuturor etichetelor pornite este substantial in alocari (aproximativ 432 KB/frame pentru fixture); nu exista o pretentie de zero alocari cand debug este pornit. Nu s-a introdus o optimizare speculativa in afara cerintei de masurare.
