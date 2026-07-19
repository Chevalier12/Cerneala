# Prism — compozitorul MonoGame

## Scop

Execută graful Prism pe MonoGame/WindowsDX, cu ownership clar al stării,
suprafețe transient frame-local și shader-e compilate la build. Etapa livrează
mecanismul GPU și kernelurile de bază; cache-ul retained se construiește separat
peste aceste contracte.

**Dependență:** `2026-07-18-prism-retained-composition-graph.md`.

## Etapa 0 — baseline GPU și pipeline shader

- [x] Extinde testele existente
  `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`
  cu cazuri RED pentru ownership complet, excepții, restore și render consecutiv.
- [x] Adaugă un spike local, mic și șters după decizie, care verifică tool-ul
  MonoGame de compilare a efectelor compatibil cu pachetul `3.8.4.1`.
- [x] Fixează contractul pipeline-ului: surse `.fx` versionate sub
  `Drawing/MonoGame/Prism/Shaders/`, compilare deterministă la build,
  `.mgfxo` embedded în assembly și încărcare din bytes.
- [x] Pin-uiește tool-ul de build în repository și adaugă un check de clean
  build/CI care detectează artefacte lipsă sau stale; interzice compilarea la
  runtime și dependența de `ContentManager` al aplicației.
- [x] Verifică shaderul minim copy/composite printr-un test de integrare
  WindowsDX înainte de a construi registry-ul de kerneluri.

### Gate etapa 0

- [x] Un checkout curat poate produce și încărca determinist shaderul minim,
  fără fișiere generate manual ori tool instalat global implicit.

## Etapa 1 — ownership și restaurarea stării

- [x] Mută ownership-ul top-level `SpriteBatch.Begin/End` în
  `MonoGameDrawingBackend.Render`; actualizează
  `UI/Hosting/MonoGame/MonoGameUiHost.cs` să nu mai deschidă batch-ul extern.
- [x] Actualizează `WindowsDxWindowGraphicsSession` și calea `RenderPng` la
  același contract, fără două implementări divergente.
- [x] Capturează și restaurează în `finally` render targets, viewport, scissor,
  blend/depth/rasterizer/sampler state și orice stare modificată de executor.
- [x] Definește explicit cine deține `SpriteBatch`, `GraphicsDevice` și
  lifetime-ul backendului; validează opțiunile în
  `MonoGameUiHostOptions`.
- [x] Adaugă teste pentru excepție în mijlocul unui pass, device state
  preexistent, două hosturi secvențiale și frame fără Prism.

### Gate etapa 1

- [x] După succes sau excepție, hostul primește exact starea documentată, iar
  UI-ul fără Prism produce același output ca baseline-ul.

## Etapa 2 — suprafețe frame-local

- [x] Adaugă `Drawing/MonoGame/Prism/Surfaces/PrismSurfacePool` cu chei tipate
  pentru dimensiune, format, samples și color space.
- [x] Pool-ul reutilizează numai resurse compatibile, eliberează lease-urile în
  `finally` și evacuează resursele la resize/device reset/dispose.
- [x] Definește explicit contractul prin care o suprafață finalizată poate fi
  promovată ulterior într-un owner retained, fără ca pool-ul transient să îi
  recicleze conținutul.
- [x] Aplică planul de lifetime și peak surfaces calculat backend-neutral;
  executorul nu recalculează graful ori liveness-ul.
- [x] Introdu opțiuni publice numai pentru limite măsurabile necesare acum;
  valorile numerice finale rămân nefixate până la benchmarkurile de hardening.
  (Nu a fost necesar: limita transient este derivată din
  `PeakLiveSurfaces`, fără o valoare publică arbitrară.)
- [x] Adaugă teste pentru reuse, dimensiuni/formate incompatibile, excepții,
  resize, device reset și bounded growth pe mii de frame-uri.

### Gate etapa 2

- [x] Contorul de resurse revine la zero lease-uri active după fiecare frame și
  memoria GPU nu crește necontrolat în testul de stress.

## Etapa 3 — executorul și kernelurile de bază

- [x] Adaugă `Drawing/MonoGame/Prism/Execution/PrismGraphExecutor` care consumă
  exclusiv graful optimizat și planul de suprafețe.
- [x] Implementează pass-urile fundamentale: capture, copy, clear, normal
  composite, mask alpha, clip alpha, color conversion și present.
- [x] Adaugă `PrismKernelRegistry` generat/validat față de catalog, dar
  înregistrează în această etapă numai kernelurile fundamentale.
- [x] Centralizează bind-ul parametrilor tipați și convențiile de alpha/UV/pixel
  size; nu permite string uniforms în bucla per-frame.
- [x] Evită `GetData`, readback CPU, flush-uri ascunse și crearea de
  `Effect`/`RenderTarget2D` în timpul unui pass.
- [x] Leagă erorile de capabilitate de `PrismFallbackPolicy` și diagnostics,
  fără catch care transformă silențios efectul în alt rezultat.

### Gate etapa 3

- [x] O compoziție simplă capturează controlul o singură dată, rulează
  offscreen și se compune corect fără alocări managed după warmup.

## Etapa 4 — diagnostics și conformance de bază

- [x] Expune intern contoare pentru passes, captures, surfaces create/reused,
  peak live surfaces, fallback și timp CPU de submit.
- [x] Adaugă dump determinist al grafului executat și corelare cu
  `PrismFrameAnalysis`, fără a expune obiecte GPU public.
- [x] Construiește scene minime de test pentru normal blend, opacity, fill,
  mask, clip, nested Prism și transform.
- [x] Capturează rezultatul exclusiv prin API-ul
  `IWindowPlatform.RenderPng`/WindowsDX și compară-l cu golden-uri versionate,
  cu profil, alpha și toleranță declarate.
- [x] Adaugă teste pentru device lost/reset și disposal în timpul navigării.

### Gate etapa 4

- [x] Testele semantice și imaginile de bază sunt verzi pe WindowsDX, iar
  diagnostics confirmă lipsa pass-urilor și suprafețelor inutile.

## Etapa 5 — API docs și verificare

- [x] Actualizează cu skill-ul `writing-api-documentation`
  `MonoGameDrawingBackend`, `MonoGameUiHostOptions` și orice contract public
  schimbat; corectează inclusiv vechea afirmație că backendul nu deține
  `Begin/End`.
- [x] Sincronizează `docs-site/documentation/manifest.json`.
- [x] Rulează reindexarea după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "MonoGame|Prism"`,
  `dotnet test .\Cerneala.slnx` și un clean build care recompilă shader-ele.
- [x] Rulează `git diff --check` și verifică să nu existe binare neexplicate ori
  shader-e compilate la runtime.

## Definiția de gata

- [x] Backendul deține și restaurează starea, executorul rulează graful de bază,
  iar pool-ul este bounded și sigur la excepții/reset.
- [x] Pipeline-ul shader este reproducibil din checkout curat.
- [x] Testele GPU de bază, documentația și gate-urile sunt verzi.
