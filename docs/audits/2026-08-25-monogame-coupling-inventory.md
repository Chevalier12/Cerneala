# Inventar și remediere a cuplării cu MonoGame

Data auditului: 2026-08-25

Status: **remediat** în aceeași schimbare. Inventarul de mai jos păstrează fotografia inițială, iar această secțiune documentează limita finală.

## Rezultat după remediere

- `Cerneala.csproj` nu mai referă `MonoGame.Framework.WindowsDX`, nu mai compilează sursele MonoGame/WindowsDX și nu mai construiește sau încorporează shader-ele MGFX.
- `Cerneala.csproj` țintește `net8.0`; TFM-ul Windows rămâne numai în proiectele adaptoare și în consumatorii Windows.
- `Cerneala.Backends.MonoGame/Cerneala.Backends.MonoGame.csproj` deține pachetul WindowsDX, implementările din `Drawing/MonoGame`, hosting/input/resources MonoGame, sesiunea WindowsDX și artefactele Prism MGFX.
- `Cerneala.Platforms.Win32/Cerneala.Platforms.Win32.csproj` deține separat P/Invoke-ul Win32, fereastra nativă, input-ul, cursorul, DPI awareness și preferința GPU Windows.
- Soluția construiește separat `Cerneala.dll` și `Cerneala.Backends.MonoGame.dll`; core-ul poate fi referit fără o dependență tranzitivă MonoGame.
- `RenderSurface2D` folosește numai `IRenderSurface2DFrameSource` și stare opacă `IRenderSurface2DBackendState`; sesiunea/target-ul MonoGame sunt create și deținute exclusiv de backend.
- `WindowApplicationRuntime` primește diagnostice Prism și numărul de lease-uri prin `IWindowGraphicsSession`; nu mai importă și nu mai face cast la `MonoGameDrawingBackend`.
- `GameBootstrap.CreateDefaultClearColor()` returnează `Cerneala.Drawing.Color`.
- Core-ul rezolvă atomic un `IWindowingBackend` prin `WindowingBackendRegistry`, iar `IWindowSurface` păstrează handle-urile native în adaptor. `WindowsDxApplicationBackend.EnsureRegistered()` compune adaptorul `Cerneala.Platforms.Win32` cu rendererul `Cerneala.Backends.MonoGame` pentru startup-ul generat și preview host.
- Tessellarea path/stroke produce `DrawTriangleMesh`/`DrawStrokeRenderMesh` cu puncte și indici Cerneala. Wrapper-ele MonoGame fac numai împachetarea finală în vertex-uri XNA.
- `PrismExecutionDiagnostics`, `PrismGraphFallbackTracker`, `PrismSurfaceBudget`, `PrismSurfaceMemoryAccountant`, `PrismSurfaceAllocationException`, `PrismKernelKind` și `PrismOperationalDiagnostics` au ownership și namespace-uri agnostice.
- Serviciile reale de font/text/imagine sunt `DrawingContentServices` în fundația de drawing din core. `MonoGameContentServices` este doar un nume de compatibilitate peste serviciul independent de MonoGame, iar `MonoGameUiHostOptions.ContentServices` acceptă tipul core.

### Dovada limitei

`MonoGameDependencyBoundaryTests` verifică explicit că:

1. `Cerneala.csproj` nu conține pachetul MonoGame sau build-ul shaderelor și exclude toate sursele backend;
2. proiectul backend deține pachetul, shader-ele și sursele MonoGame;
3. nicio sursă de producție din afara backend-ului sau a consumatorilor expliciți nu poate importa `Microsoft.Xna.Framework`.

Build-ul complet al soluției și suitele focalizate pentru path/stroke, surface accounting, content services, RenderSurface2D, source generation și limita de arhitectură fac parte din verificarea finală a schimbării.

### Limita acestei remedieri

Această schimbare izolează **MonoGame/WindowsDX** și permite proiectului
principal să țintească `net8.0`, dar nu transformă încă întregul stack de
aplicație într-o distribuție complet multi-platformă. Core-ul continuă să
dețină integrarea Skia/HarfBuzz pentru text și SVG, iar fiecare platformă are
nevoie de assets native și de un adaptor de windowing/rendering compatibil.
Startup-ul generat și preview host-ul aleg încă explicit WindowsDX; alegerea
backend-ului trebuie generalizată înainte ca o aplicație generată să poată
folosi un adaptor non-Windows.

## Actualizare finală: limita SDL3 + SDL_GPU

Paragraful anterior păstrează fotografia istorică de la închiderea remedierii MonoGame. Livrarea SDL ulterioară a generalizat selecția fără a schimba sau a șterge acel istoric:

- `Cerneala.Platforms.Sdl3` este singurul owner pentru bindingul SDL3, ferestre, event pump, input, DPI și cursor.
- `Cerneala.Backends.SdlGpu` este singurul owner pentru device, swapchain-uri, drawing, resurse, `RenderSurface2D`, Prism și composition root-ul public `SdlGpuApplicationBackend`.
- `Tools/Cerneala.SdlShaderCompiler` este singura zonă suplimentară SDL și folosește ShaderCross exclusiv la build/verificare offline; aplicațiile publicate nu îl cer la runtime.
- Core-ul, `Cerneala.SourceGen`, Win32 și backendul MonoGame nu au primit package references SDL, handle-uri SDL sau ramuri de platformă pentru SDL.
- API-ul public nou este limitat la `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`; bindingul și toate tipurile native rămân interne.
- Startup-ul generat nu mai alege WindowsDX. Executabilul selectează explicit fie `WindowsDxApplicationBackend`, fie `SdlGpuApplicationBackend` prin `ApplicationBackendAttribute`, iar registry-ul respinge amestecarea lor în același proces.
- Matematica HLSL Prism este comună sub `Drawing/Prism/Shaders/Hlsl`; adaptoarele păstrează numai wrapper-ele, artefactele și execuția specifice backendului.

`SdlDependencyBoundaryTests` și scanarea public API verifică această limită, iar publish matrix verifică separat asseturile native pentru cele șase RIDs desktop.

## Scop

Acest document inventariază tipurile de producție care sunt legate de MonoGame, direct sau prin responsabilitatea lor curentă. Sunt incluse:

- tipurile care folosesc direct namespace-uri sau tipuri `Microsoft.Xna.Framework`;
- tipurile care depind de alte implementări MonoGame, chiar dacă nu importă direct XNA;
- tipurile agnostice aflate într-un owner MonoGame, dacă poziționarea lor curentă le face parte din acel backend;
- scurgerile prin care detalii MonoGame apar în controale, hosting sau contracte care ar trebui să rămână backend-agnostic.

Nu sunt incluse testele, benchmark-urile, Playground-ul, uneltele sau codul generat. „Clasă” este folosit în sens larg în inventar; interfețele, structurile, record-urile și enum-urile asociate sunt enumerate lângă clasele pe care le susțin, deoarece fac parte din aceeași suprafață de cuplare.

## Rezumatul constatării inițiale

- La începutul auditului, proiectul principal `Cerneala.csproj` referea direct pachetul `MonoGame.Framework.WindowsDX` versiunea `3.8.4.1`.
- Am găsit **48 de fișiere de producție** în amprenta MonoGame.
- **39 de fișiere** folosesc direct `Microsoft.Xna.Framework`.
- Alte **9 fișiere** sunt legate tranzitiv, nominal sau prin ownership de backend.
- În cele 48 de fișiere există 51 de declarații class/record-class, plus 11 interfețe, 37 de structuri/record-structs și 6 enum-uri. Unele tipuri nested sunt helper-e pure, dar nu au în prezent un owner independent de clasa MonoGame care le conține.

Cea mai importantă constatare inițială a fost la nivel de assembly: API-ul agnostic și backend-ul MonoGame erau compilate în același proiect. Separarea descrisă în „Rezultat după remediere” elimină această problemă.

## Legendă

| Marcaj | Semnificație |
| --- | --- |
| **Scurgere** | Un tip dintr-o zonă care ar trebui să fie agnostică cunoaște un tip sau o implementare MonoGame. |
| **Backend** | Cuplare justificată pentru adapterul sau rendererul MonoGame. Tipul ar trebui să rămână în proiectul backend. |
| **Mixt** | Tipul combină logică agnostică cu materializare sau resurse MonoGame și merită separat. |
| **Extractabil** | Tipul nu folosește direct MonoGame și poate fi mutat sau redenumit fără să transporte backend-ul. |

## Scurgeri în suprafața agnostică

| Fișier | Tipuri relevante | Cuplare | Evaluare |
| --- | --- | --- | --- |
| `Cerneala.csproj` | assembly-ul principal | Referință directă la `MonoGame.Framework.WindowsDX`. | **Scurgere structurală**: abstracțiile și implementarea nu pot fi consumate separat. |
| `Drawing/MonoGame/RenderSurface2D.MonoGame.cs` | `RenderSurface2D` | Controlul public din `Cerneala.UI.Controls` implementează `IMonoGameRenderSurface2DSource`, păstrează `MonoGameRenderSurface2DSession` și rezolvă un `Texture2D` folosind `GraphicsDevice`. | **Scurgere**: controlul agnostic este parțial implementat de backend. |
| `UI/Hosting/Windows/WindowApplicationRuntime.cs` | `WindowApplicationRuntime` | Importă `Drawing.MonoGame`, face cast de la backend la `MonoGameDrawingBackend` și capturează `PrismExecutionDiagnostics`/`PrismOperationalDiagnostics`. | **Scurgere**: runtime-ul general cunoaște implementarea concretă pentru diagnostic. Tipurile nested `WindowCallbacks`, `WindowContext`, `AutomationFrameRequest` și `ReferenceEqualityComparer` sunt helper-e și nu folosesc separat MonoGame. |
| `UI/Hosting/Windows/IWindowPlatform.cs` | `IWindowPrismScreenshotDiagnosticsSource` | Contractul expune `PrismExecutionDiagnostics`, definit sub `Drawing.MonoGame.Prism.Execution`. | **Scurgere**: o interfață de platformă transportă un tip concret al backend-ului. Celelalte interfețe din fișier sunt agnostice și nu fac parte din problemă. |
| `GameBootstrap.cs` | `GameBootstrap` | Expune/returnează `Microsoft.Xna.Framework.Color`. | **Scurgere**: bootstrap-ul comun cunoaște direct reprezentarea de culoare XNA. |

## Drawing: backend-ul MonoGame

### Nucleul de desenare

| Fișier | Tipuri declarate | Motivul cuplării | Evaluare |
| --- | --- | --- | --- |
| `Drawing/MonoGame/IMonoGameRenderSurface2DSource.cs` | `IMonoGameRenderSurface2DSource` | Contract bazat pe `GraphicsDevice` și `Texture2D`. | **Backend**, dar devine scurgere când este implementat direct de controlul public. |
| `Drawing/MonoGame/MonoGameClipStack.cs` | `MonoGameClipStack` | Transformă clip-uri în `Microsoft.Xna.Framework.Rectangle`. | **Backend**. |
| `Drawing/MonoGame/MonoGameDrawingBackend.cs` | `MonoGameDrawingBackend`; nested: `TextTextureCacheDiagnosticSnapshot`, `TextTextureCacheMetadata`, `TextRasterizationRequest`, `TextTextureKey`, `TextTexture`, `TextBrushTextureKey`, `BrushTextureKey`, `PathMeshKey`, `StrokeMeshKey`, `DrawingLayerScope` | Rendererul concret deține `GraphicsDevice`, `SpriteBatch`, texturi, efecte, stări GPU și cache-uri de vertex/textură. | **Backend**. |
| `Drawing/MonoGame/MonoGameDrawMapper.cs` | `MonoGameDrawMapper` | Convertește primitivele Cerneala în `Color`, `Rectangle`, `Vector2` și alte reprezentări XNA. | **Backend**; adapter de limită corect. |
| `Drawing/MonoGame/MonoGameGraphicsDeviceStateSnapshot.cs` | `MonoGameGraphicsDeviceStateSnapshot` | Capturează și restaurează starea `GraphicsDevice`. | **Backend**. |
| `Drawing/MonoGame/MonoGameImage.cs` | `MonoGameImage` | Înfășoară și deține un `Texture2D`. | **Backend**. |
| `Drawing/MonoGame/MonoGamePathMeshBuilder.cs` | `MonoGamePathMeshBuilder`, `MonoGamePathMesh` | Teselează path-uri și produce direct vertex/index buffers în tipuri XNA. | **Mixt**: geometria și împachetarea vertex-urilor MonoGame pot fi separate. |
| `Drawing/MonoGame/MonoGameRenderSurface2DSession.cs` | `MonoGameRenderSurface2DSession`, `RenderSurface2DRetainedMissReason` | Gestionează `RenderTarget2D`, `GraphicsDevice`, `SpriteBatch` și prezentarea retained. | **Backend**. |
| `Drawing/MonoGame/MonoGameStrokeMeshBuilder.cs` | `MonoGameStrokeMeshBuilder`, `MonoGameStrokeMesh` | Generează stroke geometry direct în `VertexPositionColorTexture`. | **Mixt**: algoritmul de stroke poate produce geometrie agnostică, urmată de un packer MonoGame. |

### Prism: execuție și resurse GPU

| Fișier | Tipuri declarate | Motivul cuplării | Evaluare |
| --- | --- | --- | --- |
| `Drawing/MonoGame/Prism/IMonoGameBackdropFrameLease.cs` | `IMonoGameBackdropFrameLease` | Extinde lease-ul agnostic cu o proprietate publică `Texture2D`. | **Backend**; suprafață publică specifică backend-ului. |
| `Drawing/MonoGame/Prism/MonoGameBackdropFrameValidation.cs` | `MonoGameBackdropFrameValidation` | Validează texturi și resurse grafice MonoGame pentru backdrop. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/IPrismCommandRenderer.cs` | `IPrismCommandRenderer` | Contract intern construit în jurul `GraphicsDevice`, target-uri și resurse Prism MonoGame. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismCurveTextureCache.cs` | `PrismCurveTextureCache`, nested `Entry` | Creează și păstrează `Texture2D` pentru curbe. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismExecutionColdStartWarmup.cs` | `PrismExecutionColdStartWarmup` | Orchestrează încălzirea executorului și a resurselor Prism MonoGame, fără import XNA direct. | **Backend** tranzitiv. |
| `Drawing/MonoGame/Prism/Execution/PrismExecutionDiagnostics.cs` | `PrismExecutionDiagnostics`, `PrismExecutionDiagnostic`, `PrismExecutionCounters`, `PrismExecutionPass`, `PrismExecutionScope`, `PrismExecutionDiagnosticStage`, `PrismExecutionPassKind` | Nu folosește XNA, dar este definit în namespace-ul backend-ului și consumat de runtime. | **Extractabil** într-un contract de diagnostic agnostic. |
| `Drawing/MonoGame/Prism/Execution/PrismGradientDitherTexture.cs` | `PrismGradientDitherTexture` | Creează textura de dithering pe `GraphicsDevice`. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismGradientMapTextureCache.cs` | `PrismGradientMapTextureCache`, nested `Entry` | Materializează gradient maps ca `Texture2D`. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismGradientOverlayTextureCache.cs` | `PrismGradientOverlayTextureCache`, nested `Key`, `Entry` | Cache de texturi și resurse grafice MonoGame. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismGraphExecutionCache.cs` | `PrismGraphExecutionCache` | Cache-ul de execuție păstrează resurse/target-uri concrete MonoGame. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismGraphExecutor.cs` | `PrismGraphExecutor`; `PrismMaskKernelSettings`, `PrismBackdropCropKernelSettings`, `PrismBackdropColorKernelSettings`, `PrismStyleKernelSettings`, `PrismFilterKernelSettings`, `PrismLightingKernelSettings` | Execută graful Prism folosind `GraphicsDevice`, efecte, stări și suprafețe MonoGame. | **Backend**; settings-urile pur valorice pot fi separate dacă devin contract comun. |
| `Drawing/MonoGame/Prism/Execution/PrismGraphFallbackTracker.cs` | `PrismGraphFallbackTracker` | Nu folosește XNA direct; urmărește fallback-uri pentru executorul concret. | **Extractabil** ca diagnostic/telemetrie generică Prism. |
| `Drawing/MonoGame/Prism/Execution/PrismGraphFilterResources.cs` | `PrismGraphFilterResources` | Deține resurse GPU pentru filtre. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismGraphPresentation.cs` | `PrismGraphPresentation`, `PrismPresentationRegion` | Prezintă suprafețe Prism prin `SpriteBatch`, `Texture2D` și dreptunghiuri XNA. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismLensProfileTextureCache.cs` | `PrismLensProfileTextureCache`, nested `Entry` | Cache de profiluri de lentilă materializate în `Texture2D`. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismSpatterPointTextureCache.cs` | `PrismSpatterPointTextureCache` | Cache de texturi GPU pentru punctele de spatter. | **Backend**. |
| `Drawing/MonoGame/Prism/Execution/PrismWaveNoiseTextureCache.cs` | `PrismWaveNoiseTextureCache`, nested `Entry` | Cache de texturi noise pe `GraphicsDevice`. | **Backend**. |
| `Drawing/MonoGame/Prism/Kernels/PrismKernelRegistry.cs` | `PrismKernelRegistry`, `PrismShaderUnavailableException`, `PrismKernel`, `PrismKernelParameters`, `PrismKernelKind` | Amestecă descrierea logică a kernel-urilor cu `Effect`, blend/sampler state și alte resurse XNA. | **Mixt**: descriptorii pot fi agnostici, iar realizarea GPU să rămână în backend. |
| `Drawing/MonoGame/Prism/Shaders/PrismShaderResources.cs` | `PrismShaderResources`, `PrismShaderId` | Încarcă, validează și deține `Effect`-uri MonoGame. | **Backend**. |

### Prism: suprafețe și memorie

| Fișier | Tipuri declarate | Motivul cuplării | Evaluare |
| --- | --- | --- | --- |
| `Drawing/MonoGame/Prism/Surfaces/PrismRetainedSurface.cs` | `PrismRetainedSurface` | Deține o suprafață retained bazată pe `RenderTarget2D`. | **Backend**. |
| `Drawing/MonoGame/Prism/Surfaces/PrismRetainedSurfaceCache.cs` | `PrismRetainedSurfaceCache`, `PrismRetainedSurfaceLease`, nested `CacheEntry` | Cache și lease lifecycle pentru suprafețe MonoGame. | **Backend**. |
| `Drawing/MonoGame/Prism/Surfaces/PrismScratchSurfaceLease.cs` | `PrismScratchSurfaceLease` | Lease peste o suprafață scratch MonoGame. | **Backend**. |
| `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceAllocationException.cs` | `PrismSurfaceAllocationException` | Excepție fără dependență XNA directă, definită pentru allocatorul backend-ului. | **Extractabil** într-un contract generic de alocare Prism. |
| `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceFrame.cs` | `PrismSurfaceFrame` | Transportă target-uri/texturi MonoGame între pașii Prism. | **Backend**. |
| `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceKey.cs` | `PrismSurfaceKey` | Cheia include formate și proprietăți de suprafață XNA. | **Backend**; ar deveni agnostică doar cu formate Cerneala proprii. |
| `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceMemoryAccountant.cs` | `PrismSurfaceMemoryAccountant`, `PrismSurfaceBudget` | Contabilitate pur numerică, fără XNA direct, dar deținută de pool-ul MonoGame. | **Extractabil**. |
| `Drawing/MonoGame/Prism/Surfaces/PrismSurfacePool.cs` | `PrismSurfacePool`, nested `SurfaceEntry` | Alocă, reciclează și distruge `RenderTarget2D`. | **Backend**. |

## Hosting, input și resurse

| Fișier | Tipuri declarate | Motivul cuplării | Evaluare |
| --- | --- | --- | --- |
| `UI/Hosting/MonoGame/MonoGameContentServices.cs` | `MonoGameContentServices` | Nu folosește XNA; compune font, text rasterizer, image loader și cache, dar este numit și amplasat ca serviciu MonoGame. | **Extractabil** ca serviciu de conținut agnostic. |
| `UI/Hosting/MonoGame/MonoGameUiHost.cs` | `MonoGameUiHost`, nested `MonoGameUiBackend` | Compune `SpriteBatch`, `MonoGameDrawingBackend`, input-ul și resursele concrete într-un `IUiBackend`. | **Backend**; composition root corect. |
| `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs` | `MonoGameUiHostOptions` | API-ul public cere `SpriteBatch` și `Texture2D` și acceptă servicii concrete MonoGame. | **Backend**; contract public specific adapterului. |
| `UI/Hosting/MonoGame/PrismOperationalDiagnostics.cs` | `PrismOperationalDiagnostics` | Nu folosește XNA; agregă diagnostice Prism MonoGame pentru hosting. | **Extractabil** sau înlocuibil cu un snapshot agnostic. |
| `UI/Hosting/Windows/WindowsDxWindowGraphicsSession.cs` | `WindowsDxWindowGraphicsSessionFactory`, `WindowsDxWindowGraphicsSession`, nested `BackdropFrameLease`, `FrameKind` | Creează `GraphicsDevice`, `SpriteBatch`, target-uri, texturi și host-ul MonoGame pentru WindowsDX. | **Backend/platform adapter**. |
| `UI/Input/MonoGame/MonoGameInputMapper.cs` | `MonoGameInputMapper` | Mapează `Keys`, `ButtonState` și stările XNA la evenimentele Cerneala. | **Backend**; adapter de limită corect. |
| `UI/Input/MonoGame/MonoGameInputSource.cs` | `MonoGameInputSource` | Citește direct `Mouse`, `Keyboard`, `MouseState` și `KeyboardState`. | **Backend**. |
| `UI/Resources/MonoGame/MonoGameImageLoader.cs` | `MonoGameImageLoader` | Încarcă `Texture2D` prin `GraphicsDevice` și produce `MonoGameImage`. | **Backend**. |

## Fișiere fără import XNA direct, dar aflate în amprenta MonoGame

Acestea sunt cele 9 fișiere care explică diferența dintre cele 48 de fișiere inventariate și cele 39 cu referințe XNA directe:

1. `Drawing/MonoGame/Prism/Execution/PrismExecutionColdStartWarmup.cs`
2. `Drawing/MonoGame/Prism/Execution/PrismExecutionDiagnostics.cs`
3. `Drawing/MonoGame/Prism/Execution/PrismGraphFallbackTracker.cs`
4. `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceAllocationException.cs`
5. `Drawing/MonoGame/Prism/Surfaces/PrismSurfaceMemoryAccountant.cs`
6. `UI/Hosting/MonoGame/MonoGameContentServices.cs`
7. `UI/Hosting/MonoGame/PrismOperationalDiagnostics.cs`
8. `UI/Hosting/Windows/IWindowPlatform.cs` — numai `IWindowPrismScreenshotDiagnosticsSource`
9. `UI/Hosting/Windows/WindowApplicationRuntime.cs`

Primele șapte sunt fie orchestratoare tranzitive, fie candidați buni de extras. Ultimele două reprezintă scurgeri ale diagnosticului concret în hosting-ul general.

## Limita arhitecturală implementată

Separarea implementată păstrează următoarele niveluri:

1. `Cerneala.Core`/`Cerneala` — geometrie, culori, path-uri, comenzi de desen, controale, input și contracte de resurse, fără referință MonoGame.
2. `Cerneala.Backends.MonoGame` — toate adapterele, resursele GPU, host-ul MonoGame și implementarea WindowsDX.
3. Composition WindowsDX — `WindowsDxApplicationBackend` și `WindowsDxWindowGraphicsSession`, compilate în proiectul backend și dependente de contractele de hosting core.

Schimbarea a aplicat toate tăieturile identificate:

- [x] înlocuirea implementării MonoGame din `RenderSurface2D` cu un contract de surface/session furnizat de backend;
- [x] eliminarea cast-ului la `MonoGameDrawingBackend` din `WindowApplicationRuntime` printr-un contract agnostic de diagnostic;
- [x] mutarea geometriei de path/stroke înaintea etapei de împachetare în vertex-uri XNA;
- [x] separarea descriptorului de kernel Prism de efectele și resursele GPU materializate;
- [x] mutarea tipurilor pur numerice/de diagnostic în stratul agnostic;
- [x] mutarea referinței `MonoGame.Framework.WindowsDX` din proiectul principal în proiectul backend.

## Metodă de verificare

Inventarul a fost construit din indexul Roslyn al soluției și verificat printr-o căutare exactă după `Microsoft.Xna.Framework` în fișierele de producție. Regula finală din `MonoGameDependencyBoundaryTests` interzice pachetul, build-ul shaderelor și sursele MonoGame în proiectul core și permite importuri XNA numai în proiectul backend sau în consumatorii expliciți care îl testează ori îl compun.
