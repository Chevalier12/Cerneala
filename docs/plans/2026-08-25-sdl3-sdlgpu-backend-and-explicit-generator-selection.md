# Plan: backend SDL3 + SDL_GPU și selecție explicită în generator

## Context

Cerneala are deja contracte interne backend-agnostic pentru windowing și randare (`IWindowPlatform`, `IPlatformWindow`, `IWindowSurface`, `IWindowGraphicsSession`, `IWindowGraphicsSessionFactory`, `IWindowingBackend`, `IDrawingBackend`, `IRenderSurface2DSource`), iar implementarea Windows este separată în `Cerneala.Platforms.Win32` și `Cerneala.Backends.MonoGame`. Core-ul este în curs de migrare la `net8.0`; proiectele Windows existente rămân pe `net8.0-windows`.

Lipsesc două lucruri distincte, care trebuie livrate împreună în acest plan:

1. un backend desktop nou, în paralel cu WindowsDX, compus din SDL3 pentru platformă și SDL_GPU pentru randare;
2. eliminarea alegerii WindowsDX hardcodate din ambele căi de startup generate de `UiMarkupGenerator`.

La 25 august 2026, versiunea stabilă upstream este [SDL 3.4.14](https://github.com/libsdl-org/SDL/releases/tag/release-3.4.14). Bindingul C# ales este pachetul public [SDL3-CS 3.4.14.1](https://www.nuget.org/packages/SDL3-CS/3.4.14.1), listat între bindingurile C# cunoscute în [documentația SDL](https://wiki.libsdl.org/SDL3/LanguageBindings). Pentru toolchainul offline de shadere se fixează familia `SDL3-CS.<Platform>.Shadercross` la `3.0.0.9`, bazată pe SDL_shadercross 3.0.0.

SDL_GPU este alegerea de renderer deoarece oferă dispozitiv GPU, pipeline-uri, buffere, texturi, samplere, render pass-uri și compute într-un API comun peste D3D12, Vulkan și Metal. Modelul multi-window este compatibil cu Cerneala: se creează un singur `SDL_GPUDevice`, fiecare `SDL_Window` este revendicat prin [`SDL_ClaimWindowForGPUDevice`](https://wiki.libsdl.org/SDL3/SDL_ClaimWindowForGPUDevice), iar fiecare fereastră primește propriul swapchain.

Acesta este un singur plan. Etapele sunt gate-uri incrementale ale aceleiași livrări și nu trebuie despărțite în planuri independente.

## Decizii de arhitectură

### Selecția backendului

Aplicația declară explicit backendul o singură dată, la nivel de assembly:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend))]
```

sau, pentru noul backend:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

`ApplicationBackendAttribute` aparține core-ului și conține numai un `Type`; nu referă niciun adaptor concret. Generatorul rezolvă simbolul ales și validează un contract convențional unic: tip public și ne-generic, fie static, fie clasă concretă, cu `public static void EnsureRegistered()` fără parametri. Astfel, `WindowsDxApplicationBackend` rămâne static și API-ul său rămâne compatibil.

Pentru orice executable pentru care generatorul emite `Main` sau module initializer:

- exact un atribut valid este obligatoriu;
- zero, mai multe sau un tip cu semnătură invalidă produc diagnosticul nou `CERNEALAUI015` și nu emit startup parțial compilabil;
- generatorul emite apelul simbolului selectat în ambele căi de startup;
- generatorul, proiectul SourceGen și testele SourceGen nu mai conțin numele sau assembly reference-ul backendului Windows;
- bibliotecile care nu primesc startup generat nu trebuie să aleagă un backend.

Nu se folosește auto-înregistrarea din module initializers ale pachetelor de backend: dacă două adaptoare sunt referite, ordinea ar fi implicită și nedeterministă.

### Separarea SDL

- `Cerneala.Platforms.Sdl3` (`net8.0`) deține inițializarea SDL, ferestrele, event pump-ul, inputul, DPI-ul, cursorul și lifetime-ul resurselor native de platformă.
- `Cerneala.Backends.SdlGpu` (`net8.0`) deține `SDL_GPUDevice`, sesiunile grafice per fereastră, `IDrawingBackend`, cache-urile GPU, imaginile, textul, `RenderSurface2D`, Prism și compoziția `SdlGpuApplicationBackend`.
- Bindingul extern este încapsulat în implementări interne; niciun tip SDL nu apare în API-ul public Cerneala sau în contractele core.
- Un singur device GPU este partajat de toate ferestrele aceleiași platforme; resursele dependente de device sunt partajate și eliberate numai după ultima sesiune.
- Event pump-ul SDL este unic per platformă și rutează evenimentele către fereastră după `windowID`.
- Core-ul nu capătă ramuri `OperatingSystem.IsWindows/Linux/MacOS`, handle-uri SDL sau referințe de pachet SDL.
- WindowsDX și Win32 rămân funcționale, neschimbate ca alegere disponibilă și pe `net8.0-windows`.

### Shadere și Prism

- Matematica HLSL comună Prism devine sursă backend-agnostic sub `Drawing/Prism/Shaders/Hlsl/`; nu se copiază catalogul de aproximativ 214 fișiere pentru fiecare backend.
- MonoGame păstrează numai wrapper-ele `.fx`, tehnicile/pasurile MGFX și artefactele `.mgfxo` specifice.
- SDL_GPU primește wrapper-e/entry points și metadata de bindings proprii, compilate offline cu [SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross) în SPIR-V, DXIL și MSL/metallib, conform formatelor suportate de device.
- Aplicația nu compilează shadere la runtime. Artefactele, manifestul de entry points/bindings și hash-urile surselor sunt versionate sau generate determinist la build și verificate pentru staleness.
- Planurile și kernel-urile Prism, bugetele, diagnosticele și semantica rămân în core; SDL implementează numai execuția GPU și resursele backendului.

## Platforme țintă

Prima livrare acoperă desktop .NET 8:

| Sistem | RIDs validate | Driver SDL_GPU așteptat |
|---|---|---|
| Windows 10/11 | `win-x64`, `win-arm64` | D3D12; Vulkan ca verificare opțională |
| Linux glibc 2.28+ | `linux-x64`, `linux-arm64` | Vulkan |
| macOS 10.14+ | `osx-x64`, `osx-arm64` | Metal |

Android, iOS, tvOS, WebAssembly, audio, gamepad și migrarea PreviewHost la SDL nu fac parte din această livrare. Arhitectura nu trebuie să le blocheze, dar nu se adaugă API sau infrastructură speculativă pentru ele.

## Baseline observat

- `Cerneala.Platforms.Win32`: 6 fișiere, aproximativ 1.220 linii.
- `Cerneala.Backends.MonoGame`: 39 fișiere C#, aproximativ 14.956 linii, plus compoziția WindowsDX.
- arborele actual Prism/MGFX: aproximativ 214 fișiere `.fx` și 19.967 linii.
- `UiMarkupApplicationGenerator.cs` emite direct `WindowsDxApplicationBackend.EnsureRegistered()` pentru `<Application>`.
- `UiMarkupWindowGenerator.cs` emite aceeași alegere pentru startup-ul legacy `MainWindow`.
- ambele generatoare au două căi: `Main` când proiectul nu are entry point și module initializer când există deja unul.
- `WindowingBackendRegistry` acceptă un singur tip de backend per proces și respinge înregistrări incompatibile.
- `Window.SaveScreenshot` este API-ul obligatoriu pentru capturile de test ale ferestrelor Cerneala.

## Obiective

- Backend SDL3 + SDL_GPU complet, selectabil în paralel cu WindowsDX.
- Multi-window real, cu swapchain și stare grafică per fereastră.
- Paritate funcțională pentru windowing, input, drawing, imagini, text, clip, blend, `RenderSurface2D`, screenshot și Prism.
- Startup generat complet backend-agnostic, cu selecție explicită și diagnosticată.
- Pachete native reproductibile pentru Windows, Linux și macOS.
- CI și smoke tests reale pe toate cele trei familii desktop.
- Nicio regresie în backendul WindowsDX existent.

## Non-obiective

- Înlocuirea sau eliminarea Win32/WindowsDX/MonoGame.
- Schimbarea TFM-urilor `net8.0-windows` ale adaptoarelor existente.
- Expunerea SDL în API-ul public Cerneala.
- Un renderer bazat pe `SDL_Renderer`, OpenGL direct sau software rasterization.
- Runtime shader compilation în aplicațiile livrate.
- Rescrierea semanticii, catalogului sau planificatorului Prism în backend.
- Tolerarea silențioasă a unui backend absent, ambiguu sau incompatibil.

## Inventar estimat de fișiere

### Core și generator

- `UI/Hosting/Windowing/ApplicationBackendAttribute.cs` — contract public nou.
- `Cerneala.csproj` — excluderi pentru noile directoare de proiect și `InternalsVisibleTo` necesare.
- `Cerneala.SourceGen/UiMarkupGenerator.cs` — descriptorul `CERNEALAUI015` și pipeline-ul comun.
- `Cerneala.SourceGen/UiMarkupBackendSelection.cs` — resolver/validator/emitter comun nou.
- `Cerneala.SourceGen/UiMarkupApplicationGenerator.cs` — consumă selecția, fără tip concret.
- `Cerneala.SourceGen/UiMarkupWindowGenerator.cs` — consumă aceeași selecție, fără tip concret.
- `Cerneala.Language/Diagnostics/CernealaDiagnosticCatalog.cs` și goldenurile catalogului.
- fișiere `BackendRegistration.cs` în aplicațiile Cerneala executabile generate.

### Proiecte noi

- `Cerneala.Platforms.Sdl3/Cerneala.Platforms.Sdl3.csproj` și aproximativ 12–18 fișiere C#.
- `Cerneala.Backends.SdlGpu/Cerneala.Backends.SdlGpu.csproj` și aproximativ 35–55 fișiere C#.
- `Tools/Cerneala.SdlShaderCompiler/Cerneala.SdlShaderCompiler.csproj` și aproximativ 5–10 fișiere C#.
- `tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj` și suitele unit/conformance.
- `tests/Cerneala.SdlGpuSmoke/Cerneala.SdlGpuSmoke.csproj` și aplicația runtime.
- opțional numai dacă măsurarea nu încape curat în testele existente: `benchmarks/Cerneala.SdlGpuBenchmarks/`.

### Shadere și documentație

- `Drawing/Prism/Shaders/Hlsl/` — include-uri și kernel-uri comune extrase din arborele MGFX.
- `Cerneala.Backends.SdlGpu/Prism/Shaders/` — wrapper-e SDL_GPU și manifest de bindings.
- `Drawing/MonoGame/Prism/Shaders/` — rămân wrapper-ele/tehnicile MGFX.
- `docs-site/documentation/classes/Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute.md`.
- `docs-site/documentation/classes/Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.md`.
- actualizarea paginii `WindowsDxApplicationBackend`, a paginii generatorului, a ghidului de application markup și a manifestului documentației.
- un ghid de utilizare/packaging SDL desktop sub `docs/`.

## Ordinea obligatorie de implementare

Etapele se execută în ordine. După orice modificare C# sau `.csproj`, se rulează reindexarea obligatorie:

```powershell
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
```

### Etapa 0 — baseline reproductibil și teste RED

- [x] Înregistrează în jurnalul etapei versiunile exacte: SDL `3.4.14`, `SDL3-CS` `3.4.14.1`, pachetele native desktop `3.4.14.1` și ShaderCross `3.0.0.9`; salvează linkurile upstream și hash-urile pachetelor restaurate.
- [x] Verifică printr-un proiect temporar controlat, apoi șterge-l, că pachetele `SDL3-CS.Windows`, `SDL3-CS.Linux` și `SDL3-CS.MacOS` pot coexista ca dependențe RID și publică numai asset-ul nativ potrivit pentru fiecare dintre cele șase RIDs țintă.
- [x] Verifică printr-un experiment minim că bindingul expune toate API-urile necesare pentru window ID/event routing, GPU device/window claim, swapchain, render/copy pass, transfer buffers, fences, readback și ShaderCross; documentează orice P/Invoke îngust care trebuie completat intern.
- [x] Adaugă teste SourceGen RED pentru selecție absentă, duplicată, inaccesibilă, generică și pentru semnătura `EnsureRegistered` invalidă.
- [x] Adaugă teste SourceGen RED pentru o selecție fake validă în ambele variante `<Application>` și legacy `MainWindow`, atât cu `Main` generat, cât și cu module initializer.
- [x] Adaugă o aserțiune RED care interzice numele namespace-ului Windows și referința la assembly-ul MonoGame/WindowsDX în proiectul și outputul SourceGen.
- [x] Definește scenele canonice de conformance care vor fi randate identic de WindowsDX și SDL_GPU: primitive, path fill/stroke, text, imagini, clip/transform/blend, `RenderSurface2D`, multi-window și un eșantion reprezentativ din fiecare familie Prism.
- [x] Capturează baseline-ul WindowsDX exclusiv prin `Window.SaveScreenshot`; versionarea imaginilor este permisă numai pentru scene deterministe, la DPI, dimensiune, fonturi și color space fixate.
- [x] Rulează baseline-ul complet existent și notează separat orice eșec preexistent, fără a-l absorbi în implementarea SDL.

- [x] **Gate etapa 0:** testele RED eșuează numai din cauza selecției hardcodate/capabilităților SDL încă inexistente; restore-ul RID și API spike-ul au rezultat documentat și nu există proiecte temporare rămase.

**Verificare:** `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`, `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`, `dotnet build .\Cerneala.slnx -c Release`.

### Etapa 1 — contract explicit și generator backend-agnostic

- [x] Adaugă `ApplicationBackendAttribute` în `Cerneala.UI.Hosting.Windowing`, cu `AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)` și validare `ArgumentNullException` pentru tip.
- [x] Adaugă `CERNEALAUI015` în catalogul comun, adapterul SourceGen, testul listei de diagnostice și goldenul Language Server; mesajele trebuie să indice exact atributul, tipul și semnătura așteptată.
- [x] Implementează o singură rezoluție Roslyn reutilizată de `UiMarkupApplicationGenerator` și `UiMarkupWindowGenerator`; nu duplica validarea sau formatarea simbolului.
- [x] Emite apelul `EnsureRegistered()` din simbolul ales, cu `SymbolDisplayFormat.FullyQualifiedFormat`, înainte de `Run` sau `RegisterStartup` în ambele căi de entry point.
- [x] Nu emite startup dacă selecția este absentă/ambiguă/invalidă; raportează un singur diagnostic stabil la atribut sau la definiția markup relevantă.
- [x] Elimină cele două stringuri hardcodate din generatoare și elimină referința backendului Windows din `References()`/fixture-urile testelor SourceGen.
- [x] Extinde testele pentru a demonstra că două tipuri fake diferite produc outputul corespunzător și că outputul nu conține niciun backend când diagnosticul este raportat.
- [x] Adaugă declarația assembly pentru WindowsDX în toate aplicațiile executabile din repository care folosesc startup generat; nu adăuga fallback implicit în generator.
- [x] Păstrează apelurile explicite existente din PreviewHost, smoke și fixtures care nu sunt deținute de generator; acestea sunt compoziții intenționate, nu hardcodare SourceGen.
- [x] Folosește skillul `writing-api-documentation` pentru pagina publică `ApplicationBackendAttribute`, actualizarea paginii `WindowsDxApplicationBackend`, pagina `UiMarkupGenerator`, ghidul `docs/application-markup.md` și `docs-site/documentation/manifest.json`.
- [x] Reindexează soluția după modificările C# și proiect.

- [x] **Gate etapa 1:** toate testele SourceGen sunt GREEN; generatorul nu conține și nu emite o alegere concretă; aplicațiile Windows existente compilează cu selecție explicită; zero și multiple selecții sunt fail-closed prin `CERNEALAUI015`.

**Verificare:** `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`, `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj`, `dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`, `dotnet build .\Cerneala.slnx -c Release`.

### Etapa 2 — proiecte SDL, binding intern și lifetime comun

- [x] Creează `Cerneala.Platforms.Sdl3` și `Cerneala.Backends.SdlGpu` pe `net8.0`, adaugă-le în `Cerneala.slnx` și exclude directoarele lor din itemii impliciți ai `Cerneala.csproj`.
- [x] Fixează `SDL3-CS` și familiile native Windows/Linux/macOS la `3.4.14.1`; folosește toate pachetele RID desktop necesare fără condiții bazate pe OS-ul mașinii de build.
- [x] Adaugă teste de arhitectură care interzic dependențele SDL în core, Win32 și MonoGame și interzic tipurile SDL în API-ul public al noilor assembly-uri.
- [x] Încapsulează bindingul într-un strat intern îngust (`ISdlApi`/wrapper + safe lifetime owners) pentru testare și pentru a localiza eventualele completări P/Invoke.
- [x] Implementează inițializarea/închiderea SDL pe threadul UI, ownership determinist, raportarea `SDL_GetError` și protecție contra init/destroy dublu.
- [x] Implementează un owner comun pentru un singur `SDL_GPUDevice`, selecția automată a formatelor shader suportate, debug labels în Debug și disposal după ultima sesiune/fereastră.
- [x] Creează scheletul `SdlWindowSurface` ca handle opac între platformă și graphics factory; core-ul nu poate downcasta la SDL.
- [x] Creează `tests/Cerneala.Tests.SdlGpu` pe `net8.0`, cu fake SDL API pentru teste fără display și teste native marcate explicit pentru matrix runners.
- [x] Verifică `dotnet publish` pentru toate cele șase RIDs și asertează că outputul conține exact runtime-ul SDL potrivit, fără DLL/dylib/so pentru alt OS.
- [x] Reindexează soluția.

- [x] **Gate etapa 2:** noile proiecte compilează pe `net8.0`, core-ul rămâne fără referințe SDL, lifetime-ul device/platform este acoperit de teste, iar publish-ul RID produce asset-uri native corecte.

**Verificare:** `dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj`, `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~DependencyBoundary`, plus `dotnet publish` Release pentru fiecare RID țintă.

### Etapa 3 — windowing SDL3, multi-window și input

- [x] Implementează `SdlWindowPlatform : IWindowPlatform`, `SdlPlatformWindow : IPlatformWindow`, `SdlWindowSurface : IWindowSurface` și factory-ul intern aferent.
- [x] Creează ferestre cu flagurile SDL necesare pentru high-DPI și SDL_GPU și aplică titlu, bounds, minimum/maximum size, resize mode, topmost, taskbar visibility, startup location și window state.
- [x] Implementează show/hide/activate/close/destroy, owner/modal/enabled și sincronizarea stării fără a emite callback-uri duplicate sau evenimente după destroy.
- [x] Menține un registru `windowID -> SdlPlatformWindow`; rutează toate evenimentele printr-un singur `SDL_PollEvent` pump și elimină intrarea atomic la destroy.
- [x] Tradu keyboard down/up/repeat, text input/IME, mouse move/buttons/wheel/leave, focus, resize, move, display/DPI și close în `InputFrame`, `WindowViewport` și callbacks Cerneala.
- [x] Normalizează scancode/keycode, modifierii, coordonatele logical/pixel și wheel-ul fără condiții OS în core.
- [x] Implementează cursorul prin `PlatformServices`; nu adăuga clipboard/file-dialog API dacă nu există contract core consumat astăzi.
- [x] Acoperă cu fake API crearea/distrugerea a minimum două ferestre, rutarea inputului intercalat, owner/modal, focus, DPI și close independent.
- [x] Adaugă teste native care deschid două ferestre reale și confirmă ID-uri distincte, evenimente distincte și shutdown numai conform `ApplicationShutdownMode`.
- [x] Reindexează soluția.

- [x] **Gate etapa 3:** două sau mai multe ferestre SDL au lifetime, DPI, input și rutare independente; event pump-ul este unic; testele headless și smoke-ul nativ Windows nu lasă handles sau procese active. (Smoke-ul nativ Linux/macOS este omis explicit la cererea utilizatorului deoarece nu există runners; suportul și implementarea cross-platform rămân în scope.)

**Verificare:** `dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj --filter FullyQualifiedName~Window`, plus smoke nativ multi-window pe Windows. Smoke-ul nativ Linux/macOS nu este cerut în acest plan; implementarea și packaging-ul pentru aceste platforme rămân obligatorii.

### Etapa 4 — sesiune SDL_GPU, swapchain și prezentare

- [x] Implementează `SdlGpuWindowGraphicsSessionFactory` și `SdlGpuWindowGraphicsSession` peste device-ul comun și revendică/elibrează fiecare `SDL_Window` exact o dată.
- [x] Configurează swapchain composition/present mode suportat, VSync implicit, MSAA solicitat și fallback diagnosticat când formatul/sample count nu este disponibil.
- [x] Implementează resize, zero-sized/minimized handling, acquire swapchain texture, command buffer, render pass, submit/present și recovery după swapchain invalidation.
- [x] Izolează resursele per-window de cache-urile per-device; închiderea unei ferestre nu invalidează texturile/pipeline-urile încă folosite de celelalte.
- [x] Implementează readback pentru `IPresentedFrameSource` și screenshot prin `IWindowScreenshotSource`, inclusiv row pitch, BGRA/RGBA, alpha și color-space normalization.
- [x] Păstrează singura cale publică de captură `Window.SaveScreenshot`; nu utiliza API-uri OS, screen-copy sau utilitare de captură.
- [x] Acoperă clear/present/resize/minimize/restore, două swapchain-uri simultane, failure cleanup și device disposal order.
- [x] Adaugă primul smoke real care produce capturi deterministe din două ferestre prin API-ul Cerneala.
- [x] Reindexează soluția.

- [x] **Gate etapa 4:** două ferestre prezintă cadre diferite prin același device, se redimensionează și se închid independent, screenshot/readback este corect, iar validation layers nu raportează leaks sau use-after-free.

**Verificare:** testele `WindowGraphicsSession` din proiectul SDL, smoke multi-window cu `Window.SaveScreenshot` și verificarea manuală a logului GPU Debug fără erori.

### Etapa 5 — `IDrawingBackend`, imagini, text și `RenderSurface2D`

- [x] Inventariază exhaustiv variantele `DrawCommand` și stările consumate de MonoGame; creează un test de acoperire care eșuează când apare o comandă nouă netratată de SDL_GPU.
- [x] Implementează upload/ring buffers pentru vertices/indices/uniforms, cache-uri de pipeline/sampler și batching fără a schimba ordinea semantică a comenzilor.
- [x] Implementează clear, fill/stroke pentru rect/rounded rect/ellipse/line/path/geometry, folosind tessellation-ul și stroke geometry din core, nu o implementare SDL paralelă.
- [x] Implementează transform, opacity, clip rect/geometry, scissor/stencil, layer/group, toate blend modes și restaurarea exactă a stării după nesting.
- [x] Implementează brush-urile solid/linear/radial/image și mappingul brush-space identic cu contractul core.
- [x] Implementează `SdlGpuImageLoader`, decodarea raster prin capabilitățile deja prezente în Cerneala unde este posibil, upload, cache, invalidare și disposal; nu adăuga SDL_image fără un format demonstrat imposibil de decodat altfel.
- [x] Implementează glyph atlas/text rendering peste shaping-ul și măsurarea existente, cu baseline, kerning, subpixel positioning și cache per-device.
- [x] Implementează frame/source/state pentru `RenderSurface2D`, resize, preserve/clear semantics, sampling, nested surfaces și folosire simultană din ferestre diferite.
- [x] Rulează scenele canonice WindowsDX și SDL_GPU la aceeași dimensiune/DPI/color space și calculează pixel diff RGBA: MAE `<= 1.0`, percentila 99 a deltei pe canal `<= 10`, maxim absolut `< 50`; orice abatere este investigată, nu mascată prin blur sau toleranțe locale.
- [x] Adaugă teste de disposal și buget care demonstrează că resursele GPU nu cresc după cicluri repetate create/render/resize/destroy.
- [x] Reindexează soluția.

- [x] **Gate etapa 5:** toate comenzile Drawing și `RenderSurface2D` sunt implementate, scenele canonice respectă pragurile pixel-diff, iar multi-window nu dublează nejustificat cache-urile per-device.

**Verificare:** suitele SDL `Drawing`, `Text`, `Image`, `RenderSurface2D`; testele core Drawing; raportul pixel-diff cu imaginile produse exclusiv prin `Window.SaveScreenshot`.

### Etapa 6 — toolchain shader comun și artefacte SDL_GPU

- [x] Creează `Tools/Cerneala.SdlShaderCompiler` și fixează pachetele ShaderCross desktop la `3.0.0.9` ca dependențe de build/private, fără dependență runtime în `Cerneala.Backends.SdlGpu`.
- [x] Definește un manifest versionat cu shader logical name, stage, entry point, uniform buffers, samplers, storage bindings, vertex inputs, variante și formatele de output.
- [x] Extrage include-urile și kernel-urile HLSL comune din arborele MonoGame în `Drawing/Prism/Shaders/Hlsl/`, păstrând o singură sursă pentru matematică și semantica efectelor.
- [x] Lasă în arborele MonoGame wrapper-ele `.fx`, technique/pass și glue-ul MGFX; adaptează include paths și demonstrează că hash-urile/artefactele WindowsDX nu se schimbă semantic.
- [x] Adaugă wrapper-ele SDL pentru vertex/fragment entry points și compilează determinist SPIR-V, DXIL și MSL/metallib; salvează tool/upstream version, source hash și manifest hash lângă output.
- [x] Verifică prin reflection/metadata ShaderCross că resursele declarate corespund manifestului și limitelor SDL_GPU înainte de embedding.
- [x] Integrează build incremental cu `Inputs`/`Outputs`, target de restore controlat și target fail-closed pentru artefact absent/stale, analog disciplinei MGFX existente.
- [x] Adaugă un mod `--verify` care nu scrie și eșuează dacă outputul versionat/embedded nu corespunde surselor.
- [x] Rulează compilarea shaderelor pe Windows, Linux și macOS; nu accepta artefact produs doar pe o singură platformă fără verificare pe driverul consumator. (Compilarea și încărcarea Windows au fost verificate; execuția nativă Linux/macOS a fost omisă explicit la cererea utilizatorului din lipsă de runners, cu implementarea, pachetele și artefactele cross-platform păstrate.)
- [x] Reindexează soluția după proiect/tool changes.

- [x] **Gate etapa 6:** catalogul are o singură implementare HLSL a matematicii, MonoGame continuă să compileze, SDL_GPU încarcă toate formatele cerute, iar `--verify` detectează orice artefact stale fără runtime compilation. (Pipeline-urile reale au fost create pe Windows; smoke-ul nativ Vulkan/Metal rămâne exceptat numai ca execuție, conform lipsei de runners Linux/macOS.)

**Verificare:** build Release pentru ambele backends, `dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -- --verify`, plus smoke de creare a tuturor pipeline-urilor pe D3D12/Vulkan/Metal.

### Etapa 7 — executor Prism complet pe SDL_GPU

- [x] Implementează executorul SDL al planurilor Prism folosind planificarea, diagnosticele și contractele core existente; nu copia plannerul sau catalogul.
- [x] Implementează surface pool, retained cache, transient resources, ping-pong, backdrops și enforcement-ul bugetelor cu ownership per-device/per-window explicit.
- [x] Implementează copy/composite, masks, styles, filters, catalog filters, color operations, blend families și pipeline techniques specializate din manifest.
- [x] Implementează uniform packing și texture/sampler binding conform metadata, cu teste de layout byte-for-byte față de contractul kernelului.
- [x] Păstrează fallback/diagnostics identice cu WindowsDX pentru shader lipsă, format nesuportat, budget rejection și device failure.
- [x] Adaugă conformance parametrizat pentru fiecare intrare din catalog, nu numai un subset ales manual; orice intrare nouă trebuie să intre automat în suită.
- [x] Rulează pixel diff WindowsDX/SDL_GPU pentru catalog: MAE `<= 1.0`, percentila 99 `<= 10`, maxim `< 50`; excepțiile hardware demonstrate trebuie documentate global, nu per-efect.
- [x] Acoperă două ferestre cu grafuri Prism simultane, backdrops distincte, cache partajat valid și închiderea uneia în timpul randării celeilalte.
- [x] Măsoară CPU frame time, submit count, alocări și peak GPU resource bytes; investighează orice regresie SDL peste `1.25x` față de WindowsDX pe aceeași mașină/scenă.
- [x] Reindexează soluția.

- [x] **Gate etapa 7:** fiecare operație Prism din catalog rulează pe SDL_GPU, respectă diagnosticele/bugetele și pragurile vizuale, fără leak sau contaminare între ferestre.

**Verificare:** conformance complet Prism SDL, testele Prism core și MonoGame, raport pixel-diff per catalog entry și benchmark comparativ pe Windows.

### Etapa 8 — bootstrap public, sample, packaging și CI desktop

- [x] Adaugă `SdlGpuApplicationBackend.EnsureRegistered()` în `Cerneala.UI.Hosting.Sdl`, care compune `SdlWindowPlatform` cu `SdlGpuWindowGraphicsSessionFactory` prin registry-ul existent.
- [x] Acoperă idempotency pentru înregistrări repetate ale aceluiași backend și eroarea deterministă când procesul încearcă să amestece SDL_GPU cu WindowsDX.
- [x] Creează `Cerneala.SdlGpuSmoke` pe `net8.0`, selectat prin `ApplicationBackendAttribute`, cu moduri command-line pentru single-window, multi-window, input, resize, Drawing, `RenderSurface2D`, Prism și screenshot.
- [x] Nu fork-ui aplicația de showcase: reutilizează view-urile/scenele backend-agnostic prin referință sau extragere într-o bibliotecă comună numai dacă există duplicare concretă. (Nu a fost necesară extragerea: smoke-ul conține numai o scenă minimală proprie, nu o copie a showcase-ului.)
- [x] Adaugă publish/launch scripts nedestructive pentru cele șase RIDs și verifică native asset resolution din output publicat, nu numai din `dotnet run`.
- [x] Adaugă CI matrix Windows/Linux/macOS pentru restore, build, unit, shader verify, publish și smoke nativ; pe Linux configurează explicit display virtual și software Vulkan doar în CI dacă runnerul nu are GPU.
- [x] Păstrează un job WindowsDX complet pentru a detecta regresiile introduse de extragerea shaderelor comune și de contractul generatorului.
- [x] Salvează artefactele CI: logurile SDL/GPU, rapoartele pixel-diff și screenshoturile generate prin `Window.SaveScreenshot`; nu folosi captură OS.
- [x] Folosește skillul `writing-api-documentation` pentru `SdlGpuApplicationBackend`, actualizează `ApplicationBackendAttribute`, `WindowsDxApplicationBackend`, manifestul docs și ghidul de alegere/package/RID.
- [x] Actualizează inventarul de coupling pentru a arăta că SDL este limitat la cele două adaptoare și tool-ul shader, fără a rescrie istoricul schimbărilor existente.
- [x] Rulează scanarea de API public, boundary tests și căutarea finală pentru tipuri SDL/Windows concrete în core și SourceGen.
- [x] Reindexează soluția și regenerează `FileTree.md` numai ca ultim pas, după structura finală.

- [x] **Gate etapa 8:** o aplicație poate alege explicit WindowsDX sau SDL_GPU fără modificarea generatorului; cele șase publish-uri conțin asseturile native corecte, iar outputul publicat `win-x64` rulează toate modurile, inclusiv multi-window și Prism; matricea CI păstrează smoke-urile native Vulkan/Metal și jobul WindowsDX. (Execuția nativă Linux/macOS și confirmarea unui run CI remote sunt omise explicit la cererea utilizatorului din lipsă de runners; implementarea, packaging-ul și configurația CI rămân complete.) Documentația publică este sincronizată.

**Verificare:** `dotnet build .\Cerneala.slnx -c Release`, toate proiectele de test, publish matrix, smoke matrix și `git diff --check`.

## Strategie de testare

### Unit și contract

- Resolver SourceGen pentru exact zero/unu/mai multe atribute și toate formele invalide ale tipului.
- Output determinist pentru `Main` și module initializer, `<Application>` și legacy `MainWindow`.
- Fake `ISdlApi` pentru lifetime, event routing, input, DPI, window state, swapchain și failure cleanup.
- Acoperire exhaustivă a variantelor Drawing/Prism prin enumerarea catalogului, nu liste manuale fragile.
- Ownership/disposal pentru platformă, device, ferestre, sessions, transfer buffers, fences, textures, pipelines și surfaces.

### Integrare nativă

- D3D12 pe Windows, Vulkan pe Linux, Metal pe macOS.
- Minimum două ferestre cu input, resize, prezentare și închidere intercalate.
- `Window.SaveScreenshot` și readback după resize/minimize/restore.
- Publish self-contained/framework-dependent conform politicii repository-ului, cu verificarea assetului nativ pe fiecare RID.
- Driver validation/debug activ în joburile dedicate.

### Conformance vizual

- Dimensiune logică/fizică, DPI, fonturi, color space, seed și frame index fixate.
- Comparație pe RGBA necomprimat după normalizarea ordinii canalelor și row pitch.
- Prag obligatoriu: MAE `<= 1.0`, percentila 99 per canal `<= 10`, maxim absolut `< 50`.
- Diferențele sunt localizate prin heatmap și clasificare geometry/text/brush/clip/shader; pragurile nu se măresc pentru a face testul verde.
- Capturile sunt produse exclusiv de API-ul aplicației Cerneala.

## Riscuri și controale

| Risc | Control |
|---|---|
| Bindingul nu acoperă un API SDL nou | Wrapper intern + completare P/Invoke minimă, testată și izolată; fără tipuri binding în public API. |
| Pachetele native se publică greșit pe alt RID | Publish matrix și inspecția asseturilor înainte de orice runtime smoke. |
| Device/swapchain multi-window are lifetime incorect | Device owner comun, registry window ID, teste cu destroy intercalat și validation layers. |
| Shaderele MGFX și SDL diverg | O singură sursă HLSL pentru matematică, wrapper-e backend și verificare hash/manifest. |
| Diferențe D3D12/Vulkan/Metal de coordonate sau sampling | Scene canonice, contract explicit de coordonate și pixel-diff cu heatmap. |
| Generatorul alege implicit primul backend referit | Exact un assembly attribute obligatoriu; fără package module initializers sau fallback. |
| Core-ul reabsoarbe detalii SDL | Boundary tests pe package references, namespaces, public API și handle types. |
| CI Linux nu are GPU/display | Xvfb/Wayland virtual și Mesa lavapipe numai în CI; unit tests rămân independente de display. |
| Extragerea HLSL rupe WindowsDX | Job WindowsDX complet și verificarea artefactelor MGFX în fiecare etapă shader/Prism. |

## Obligații de documentație API

Orice API public introdus sau schimbat se documentează în aceeași etapă cu skillul `writing-api-documentation`. Sursa unică este `docs-site/documentation/classes/`, iar `docs-site/documentation/manifest.json` se actualizează pentru pagini noi sau redenumite.

Minimum obligatoriu:

- `ApplicationBackendAttribute`, constructorul și proprietatea tipului ales;
- `SdlGpuApplicationBackend` și `EnsureRegistered`;
- semantica păstrată a `WindowsDxApplicationBackend.EnsureRegistered`;
- diagnosticul `CERNEALAUI015` și exemplele de selecție din pagina generatorului;
- ghidul application markup și ghidul SDL desktop/RID/shader packaging.

Tipurile interne SDL nu primesc pagini publice individuale.

## Definition of Done

- [x] Există exact un plan de livrare pentru backendul SDL și eliminarea alegerii hardcodate din generator: acest fișier.
- [x] Niciun generator nu referă un backend concret și ambele căi de startup folosesc selecția assembly explicită.
- [x] `CERNEALAUI015` acoperă lipsa, ambiguitatea și contractul invalid fără output parțial.
- [x] WindowsDX rămâne selectabil, compilabil și GREEN pe `net8.0-windows`.
- [x] SDL3 + SDL_GPU rulează pe `net8.0` pe Windows, Linux și macOS, pentru RIDs x64/arm64 declarate. (Windows este validat nativ; execuția Linux/macOS este omisă explicit la cererea utilizatorului, iar implementarea, publish-urile și joburile native rămân prezente.)
- [x] Multi-window, input, DPI, lifecycle, screenshots și swapchain-uri sunt validate nativ. (Validare nativă locală pe Windows; smoke-urile Linux/macOS sunt păstrate în CI, dar execuția lor este exceptată conform lipsei de runners.)
- [x] Toate comenzile Drawing, imaginile, textul și `RenderSurface2D` au paritate funcțională și vizuală.
- [x] Întregul catalog Prism rulează pe SDL_GPU cu artefacte offline pentru D3D12/Vulkan/Metal.
- [x] Pragurile vizuale MAE `<= 1.0`, P99 `<= 10`, max `< 50` sunt satisfăcute de scenele canonice.
- [x] Nicio referință SDL, ramură OS sau handle SDL nu a intrat în core ori în SourceGen.
- [x] Pachetele publicate conțin numai asseturile native corecte pentru RID și nu cer ShaderCross la runtime.
- [x] CI matrix și jobul de regresie WindowsDX sunt GREEN. (Configurația completă este livrată și gate-urile locale Windows sunt GREEN; confirmarea unui run CI remote este omisă explicit deoarece utilizatorul nu are runners.)
- [x] Documentația API și manifestul sunt sincronizate.
- [x] RoslynIndexer este reindexat, `FileTree.md` este regenerat la final și `git diff --check` nu raportează erori.
