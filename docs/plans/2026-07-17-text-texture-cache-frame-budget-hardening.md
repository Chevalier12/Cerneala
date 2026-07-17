# Plan: hardening pentru text texture cache si frame budget in CernealaPresentation

> Data: 2026-07-17
> Status: finalizat
> Dependenta: Motion markup si automatizarea CernealaPresentation existente
> Scop: eliminam rerasterizarea repetata a textului animat si garantam printr-un benchmark nativ ca incarcarea fiecarui view relevant ramane in bugetul de 60 FPS.

## 1. Rezumat

Profilarea nativa a CernealaPresentation a demonstrat ca toate view-urile in afara de
Welcome pot depasi bugetul de `16.6667 ms` la incarcare. Problema nu este `Present()`
sau VSync: `UiFrame.ProcessingTime` este capturat inainte de `Present()`, iar spike-urile
se afla in `Draw`.

Root cause-ul este compus din trei comportamente din pipeline-ul de text:

- `MonoGameDrawingBackend.TextTextureKey` include faza subpixel exacta a pozitiei;
- Motion schimba acea faza aproape la fiecare frame, producand cache misses repetate;
- `PruneInactiveTextTextureCaches()` evacueaza dupa fiecare frame toate texturile care
  nu au fost folosite in acel frame, inclusiv textele view-urilor temporar collapsed.

Un cache miss executa sincron `SkiaTextRasterizer.RasterizeSubpixel()`, creeaza
referintele white/black, trei layere RGB, masca grayscale si patru texturi GPU.
Baseline-ul instrumentat a observat aproximativ `4-6 MB` alocati pe frame-urile
obisnuite, `7-11 MB` pe spike-uri si GC in 99 dintre 102 frame-uri peste buget.

Doua experimente controlate au confirmat cauza:

- neutralizarea fazei subpixel a redus puternic alocarile si spike-urile din timpul Motion;
- neutralizarea fazei plus pastrarea cache-ului intre view-uri a lasat numai primul
  cold load; ciclurile 2 si 3 au avut zero depasiri, cu maxime warm de aproximativ
  `1.1-5 ms`.

Planul inlocuieste politica de cache "ultimul frame sau gunoi" cu retentie bounded,
foloseste faze subpixel canonice si optimizeaza cold rasterization. Benchmark-ul nativ
folosit la diagnostic devine gate permanent.

## 2. Obiective

- Nicio rerasterizare de text doar fiindca un view a lipsit dintr-un singur frame.
- Un numar finit si controlat de variante subpixel pentru text animat.
- Evacuare bounded/LRU cu `Dispose()` corect pentru toate texturile GPU dependente.
- Reducerea alocarilor cold-path, inclusiv eliminarea mastii grayscale cand textul solid
  nu o foloseste.
- Zero frame-uri cu `ProcessingTime > 16.6667 ms` la cold si warm load pentru Retained,
  Markup, Aspect, Motion, Frame Pipeline si Diagnostics pe gate-ul nativ Release.
- Benchmark permanent, machine-readable si cu exit code non-zero la regresie.

## 3. Non-obiective

- Nu schimbam semantica Motion sau markup-ul Presentation doar ca sa ascundem costul.
- Nu eliminam subpixel text rendering si nu acceptam text neclar drept optimizare.
- Nu introducem un atlas global de glyph-uri, rasterizare GPU sau worker pipeline
  asincron daca fixul bounded cache plus cold-path optimization satisface gate-ul.
- Nu transformam `UiFrame` intr-un API public de profiler si nu adaugam phase timings
  publice doar pentru benchmark.
- Nu garantam 60 FPS pentru hardware arbitrar; gate-ul masoara acelasi WindowsDX runtime,
  aceeasi configuratie Release si acelasi mediu de referinta documentat.

## 4. Arhitectura propusa

### 4.1 Faza subpixel canonica

`MonoGameDrawingBackend` va normaliza faza fizica inainte de construirea
`TextTextureKey`. Grid-ul initial trebuie validat la 8 faze pe axa; daca inspectia
pixel-diff arata ca 4 faze sunt indistinguibile la scale-urile suportate, se poate alege
grila mai mica.

Aceeasi faza canonica trebuie folosita atat in cheie, cat si la rasterizare. Nu este
legal ca doua pozitii sa imparta cheia, dar textura sa depinda de prima pozitie exacta
care a ratat cache-ul.

Pozitia finala de draw continua sa foloseasca baseline-ul real si maparea existenta.
Cuantizarea controleaza numai varianta rasterizata, nu geometria logica.

### 4.2 Cache bounded intre frame-uri

Cache-ul de text va retine intrari intre frame-uri si intre view switches. Fiecare
intrare va urmari ultima generatie/frame in care a fost folosita si costul aproximativ
in bytes.

Evacuarea va avea doua limite explicite:

- un plafon de memorie/texturi;
- o perioada minima de retentie sau o politica LRU care nu evacueaza continutul doar
  fiindca a lipsit din frame-ul curent.

Cand o intrare este evacuata, backend-ul trebuie sa elibereze Red, Green, Blue si masca
optionala, apoi sa elimine toate texturile brush dependente de aceeasi cheie.
`CoordinateScale` change, device reset si `Dispose()` continua sa goleasca imediat tot.

### 4.3 Cold rasterization

Textul solid nu va construi textura de masca folosita exclusiv de text brushes
nesolide. Pipeline-ul subpixel va evita copii LINQ si buffers temporare nenecesare si
va folosi pooling numai unde ownership-ul bufferului dupa `Texture2D.SetData()` este
clar.

Prima aparitie a unui text ramane sincrona in aceasta etapa, dar costul agregat al
primului view load trebuie sa satisfaca acelasi gate de `16.6667 ms`. Daca optimizarile
locale nu sunt suficiente, implementarea se opreste si documenteaza profilul ramas
inainte de a extinde scope-ul la prewarm/async rasterization.

### 4.4 Gate nativ permanent

`PresentationWindow.Automation.cs` va primi un mod opt-in separat pentru frame budget.
El va conduce controalele existente prin automation peers, va exclude Welcome si va
captura primele 45 de frame-uri dupa fiecare switch.

Un runner dedicat din `benchmarks/Cerneala.PresentationFrameBudget/` va porni
CernealaPresentation in `Release`, va valida raportul JSON si va esua daca:

- un view nu produce numarul asteptat de frame-uri;
- apare o exceptie sau procesul depaseste timeout-ul;
- orice cold sau warm frame are `ProcessingTime > 16.6667 ms`;
- raportul include Welcome sau omite unul dintre cele sase view-uri;
- warm loads arata rerasterizare/GC churn peste bugetele stabilite in etapa 0.

Benchmark-ul este un gate nativ Windows, nu un test unit cross-platform si nu ruleaza
implicit in `dotnet test`.

## 5. Fisiere estimate

- `Drawing/MonoGame/MonoGameDrawingBackend.cs`
- `Drawing/Text/SkiaTextRasterizer.cs`
- `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`
- `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`
- `CernealaPresentation/PresentationWindow.Automation.cs`
- `benchmarks/Cerneala.PresentationFrameBudget/Cerneala.PresentationFrameBudget.csproj`
- `benchmarks/Cerneala.PresentationFrameBudget/Program.cs`
- `benchmarks/Cerneala.PresentationFrameBudget/README.md`
- `benchmarks/results/<data>-presentation-frame-budget/README.md`
- `Cerneala.slnx`

Nu este planificata o schimbare de API public. Daca implementarea cere membri
public/protected noi in Cerneala, etapa respectiva se opreste pentru review si
documentatie API inainte de continuare.

## 6. Etape de implementare

### Etapa 0 - Benchmark RED permanent si baseline

- [x] Promoveaza harness-ul temporar intr-un mod frame-budget opt-in in `PresentationWindow.Automation.cs`, fara reflection si fara modificari de markup facute numai pentru test.
- [x] Captureaza pentru fiecare sample: ciclu, capitol, indexul frame-ului, `ProcessingTime`, `ElapsedTime`, `FrameStats`, cold/warm si timestamp relativ.
- [x] Exclude explicit Welcome si ruleaza Retained, Markup, Aspect, Motion, Frame Pipeline si Diagnostics in ordinea reala a navigatiei.
- [x] Adauga runner-ul `benchmarks/Cerneala.PresentationFrameBudget` cu 8 cicluri, 45 frame-uri per load, timeout bounded, raport JSON si sumar lizibil.
- [x] Fa runner-ul sa iasa non-zero pentru orice frame peste `16.6667 ms`, raport incomplet, eroare asincrona sau proces blocat.
- [x] Inregistreaza baseline-ul RED in `benchmarks/results/<data>-presentation-frame-budget/README.md`, inclusiv hardware, OS, configuratie, maxime per view, counts peste buget si comanda exacta.
- [x] Confirma ca baseline-ul reproduce alocari/GC asociate draw-ului printr-un profiling run separat; nu introduce phase timings publice permanente.
- [x] Reindexeaza dupa fiecare modificare C# sau project-file.

**Gate etapa 0**

- [x] Comanda de benchmark ruleaza end-to-end pe window-ul WindowsDX real si esueaza RED pentru cauza observata, nu pentru build, timeout sau fixture defect.
- [x] Raportul contine exact cele sase view-uri cerute, separat cold/warm, si pastreaza dovada baseline-ului.

### Etapa 1 - Contracte RED pentru cache si lifecycle GPU

- [x] Inlocuieste testul `CompletingFrameEvictsTextTexturesNotUsedByThatFrame` cu teste RED pentru retentie intre frame-uri si evacuare numai la depasirea politicii bounded.
- [x] Adauga un test care foloseste textul A, apoi B, apoi A si cere cache hit pentru revenirea la A.
- [x] Adauga un test cu suficiente chei pentru a depasi plafonul si verifica evacuarea LRU determinista, nu crestere nelimitata.
- [x] Verifica prin test ca evacuarea elibereaza toate texturile RGB, masca optionala si intrarile `textBrushTextureCache` dependente.
- [x] Pastreaza teste pentru `Dispose()` idempotent, coordinate-scale reset si device reset; fiecare trebuie sa goleasca imediat cache-urile.
- [x] Adauga diagnostics counters interni/test-only pentru hits, misses, evictions si estimated bytes, fara API public.
- [x] Reindexeaza dupa modificarile C#.

**Gate etapa 1**

- [x] Noile teste esueaza RED impotriva politicii actuale de pruning per-frame si descriu exact ownership-ul resurselor GPU.
- [x] Niciun test nu cere cache nelimitat sau omiterea `Dispose()`.

### Etapa 2 - Faze subpixel finite si corecte

- [x] Introdu o functie unica de normalizare/cuantizare pentru faza fizica, cu comportament definit pentru pozitii negative, scale fractional si valori aproape de 0/1.
- [x] Foloseste faza canonica in `TextTextureKey` si aceeasi faza in inputul `RasterizeSubpixel`; elimina dependenta de float-uri animate exacte.
- [x] Adauga teste pentru numarul maxim de chei produse de o translatie lunga la scale 1, 1.25, 1.5 si 2.
- [x] Adauga pixel-diff tests pentru fazele canonice si pozitiile dintre ele; verifica baseline, clipping, culoare/gamma si absenta salturilor mai mari decat toleranta acceptata.
- [x] Verifica text solid si text cu brush, deoarece ambele cache-uri includ cheia textului.
- [x] Pastreaza `TextTextureKey` separat pentru font, size, coordinate scale si rasterization color.
- [x] Reindexeaza dupa modificarile C#.

**Gate etapa 2**

- [x] O animatie de pozitie produce un numar bounded de variante, apoi cache hits, fara rerasterizare continua.
- [x] Pixel-diff-ul confirma ca optimizarea nu transforma textul in marmelada vizuala.

### Etapa 3 - Cache bounded si cold-path optimization

- [x] Implementeaza generatia/frame usage si politica bounded/LRU in `MonoGameDrawingBackend`.
- [x] Evacueaza intrarile numai dupa aplicarea limitelor explicite si dispose-uie resursele in ordine sigura pentru GraphicsDevice.
- [x] Leaga lifecycle-ul `textBrushTextureCache` de cheia textului parinte, astfel incat o evacuare sa nu lase texturi orfane.
- [x] Fa masca grayscale optionala si construieste-o numai cand un text brush nesolid o cere.
- [x] Elimina `Select(...).ToArray()` si alte copii temporare din `CreateGrayscaleMask` si profileaza buffers mari din `RasterizeSubpixelReference`/`CreateSubpixelLayers`.
- [x] Foloseste `ArrayPool<byte>` numai daca bufferul poate fi returnat dupa upload fara ca `RasterizedText` sau `Texture2D` sa-i pastreze referinta.
- [x] Adauga allocation tests dupa warm-up pentru static text, animated text si A-B-A view switching.
- [x] Ruleaza benchmark-ul dupa fiecare subpas si pastreaza comparatia cold/warm fata de baseline.
- [x] Reindexeaza dupa modificarile C#.

**Gate etapa 3**

- [x] Warm view switching are zero cache misses pentru texturi deja retinute, in afara invalidarilor legitime de font/scale/color.
- [x] Animated text ajunge la hits dupa popularea variantelor canonice si nu mai produce GC repetat in draw.
- [x] Cache-ul respecta plafonul si toate resursele evacuate sunt dispose-uite.
- [x] Niciunul dintre cele sase view-uri nu depaseste `16.6667 ms` in benchmark; cold si warm trec separat.

### Etapa 4 - Hardening al benchmark-ului si integrare

- [x] Adauga proiectul benchmark in folderul `/benchmarks/` din `Cerneala.slnx`.
- [x] Documenteaza in README comanda, cerintele WindowsDX, formatul raportului, timeout-ul si faptul ca rezultatele sunt environment-specific.
- [x] Ruleaza benchmark-ul de trei ori in procese Release curate si cere zero depasiri in toate cele trei rulari.
- [x] Pastreaza rezultatul final in `benchmarks/results/<data>-presentation-frame-budget/README.md` langa baseline, cu acelasi hardware si aceiasi parametri.
- [x] Confirma ca benchmark-ul nu forteaza GC, nu scrie in callback-ul de frame si nu include timpul propriei serializari in `ProcessingTime`.
- [x] Confirma ca procesul inchide window-ul pe succes, failure si timeout si nu lasa procese Cerneala/dotnet active.
- [x] Reindexeaza dupa modificarile C# si project-file.

**Gate etapa 4**

- [x] Comanda `dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667` este GREEN de trei ori consecutiv.
- [x] Fiecare raport contine 360 de frame-uri pentru fiecare view, exceptand numai diferente explicit justificate de inchiderea ferestrei.
- [x] Welcome nu apare in samples sau agregate.

### Etapa 5 - Verificare finala si documentatie

- [x] Ruleaza testele focusate pentru `MonoGameDrawingBackendStateTests` si `TextPipelineTests`.
- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`.
- [x] Ruleaza benchmark-ul final exact din gate-ul etapei 4 si compara raportul cu baseline-ul RED.
- [x] Cere validare vizuala umana pentru text static si animat la scale 1, 1.25, 1.5 si 2; agentul nu inventeaza rezultatul acestui gate.
- [x] Ruleaza public API diff; daca este gol, consemneaza ca nu sunt necesare pagini noi in `docs-site/documentation/classes/`. (Diff-ul strict contine doar doua adaugari documentate; verificarea de compatibilitate non-strict este GREEN.)
- [x] Daca exista schimbari publice neasteptate, actualizeaza documentatia prin `writing-api-documentation` si sincronizeaza manifestul unde se adauga sau redenumesc pagini. (Paginile existente pentru `UIRoot` si `UiFrame` sunt sincronizate; manifestul nu necesita schimbare.)
- [x] Ruleaza `git diff --check` si RoslynIndexer `doctor/status` dupa indexarea finala.
- [x] Confirma ca nu exista warnings noi, teste skipped noi, procese ramase sau artefacte temporare de profiling. (Warning-ul indexerului pentru `tmp/presentation-frame-cause.nettrace.etlx` este un artefact preexistent al utilizatorului, nu unul creat de plan.)

**Gate etapa 5**

- [x] Testele focusate, proiectul runtime, intreaga solutie si benchmark-ul nativ sunt GREEN.
- [x] Validarea umana confirma text clar si stabil in Motion, fara jitter, ghosting sau schimbari de culoare/gamma.
- [x] Benchmark-ul demonstreaza zero frame-uri peste buget pentru fiecare load relevant, nu doar o medie frumoasa care ascunde spike-uri jegoase.

## 7. Ordinea recomandata

1. Ingheata benchmark-ul RED si baseline-ul inainte de productie.
2. Scrie contractele RED pentru retentie, bounded eviction si phase cardinality.
3. Canonicalizeaza faza subpixel.
4. Inlocuieste pruning-ul per-frame cu cache bounded/LRU.
5. Optimizeaza cold rasterization pana trece acelasi gate.
6. Intareste runner-ul permanent, documenteaza rezultatele si ruleaza verificarea completa.

## 8. Stop conditions

- [x] Opreste extinderea spre glyph atlas sau rasterizare asincrona daca fixul local trece gate-ul. (Nu au fost introduse glyph atlas sau rasterizare asincrona.)
- [x] Opreste implementarea si cere review daca solutia necesita API public nou in Cerneala. (Utilizatorul a aprobat explicit extinderea Diagnostics; cele doua adaugari sunt documentate si nu expun phase timings.)
- [x] Nu relaxa bugetul, nu exclude frame-uri lente si nu muta serializarea astfel incat benchmark-ul sa cosmetizeze rezultatul.
- [x] Nu rezolva problema ascunzand view-urile, eliminand Motion sau reducand continutul Presentation.

## 9. Definitia de gata

- [x] Cache-ul de text pastreaza continut reutilizabil intre frame-uri si view-uri fara crestere necontrolata.
- [x] Motion nu mai produce o cheie noua pentru fiecare pozitie float si nu mai declanseaza rerasterizare continua.
- [x] Cold text rasterization nu mai aloca si copiaza buffers care nu sunt necesare tipului de text desenat.
- [x] Resursele GPU sunt dispose-uite determinist la eviction, scale change, device reset si backend disposal.
- [x] Toate testele de contract si lifecycle sunt GREEN.
- [x] Benchmark-ul nativ Release este GREEN de trei ori consecutiv si raporteaza zero frame-uri peste `16.6667 ms` pentru toate cele sase view-uri.
- [x] Utilizatorul confirma vizual ca textul static si animat ramane clar; performanta nu a fost cumparata cu pixeli fututi.
