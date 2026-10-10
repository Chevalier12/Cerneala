# Plan: Documentație de arhitectură pentru fiecare sistem Cerneala

> Data: 2026-10-10
> Status: finalizat
> Baseline: commit `def7f419` (master).
> Start implementare: commit `0cb32300` (branch `session/repo-docs-reorganization`), 2026-10-10.
> Dependență: [reorganizarea documentației](2026-10-10-repo-docs-reorganization.md). Gate etapa 5 este precondiție: folderul `docs/architecture/` există, documentele vechi sunt mutate acolo, iar `Tools/scripts/Test-MarkdownLinks.ps1` există.
> Scop: fiecare sistem din repo are un document care explică cum merge pe dinăuntru (componente, flux de date, ownership, ciclu de viață, invarianți), scris din codul de azi și verificat identificator cu identificator.

## 1. Rezumat

Azi, 17 din 28 de sisteme au text de arhitectură, multe subțiri. 4 au doar ghid de utilizare, 7 nu au nimic. Două documente existente au afirmații dovedit false. Acest plan scrie documentele lipsă, le corectează pe cele greșite și face din `docs/architecture/overview.md` harta de intrare.

Un document de arhitectură nu este un ghid („cum folosești”) și nu este referință API (aceea stă doar în `docs-site/documentation/classes/`). El răspunde la: **ce componente există, cine deține ce stare, cum curg datele într-un frame, ce invarianți păstrează sistemul și ce teste îi dovedesc**.

## 2. Baseline (fapte observate)

Sursa: inventarul exploratorului de arhitectură din 2026-10-10. Căile sunt cele de după planul de reorganizare.

| Sistem | Document azi | Stare |
|---|---|---|
| UI/Aspect | `architecture/aspect.md` | verificat corect |
| UI/Motion | `architecture/motion.md` (39 de linii) | corect, subțire |
| UI/Prism + Drawing/Prism | `architecture/prism-technical-design.md` | **greșit**: arborele de foldere listează 6 foldere inexistente (ex. `UI/Prism/Diagnostics`); tipul `PrismRenderState` nu există |
| UI/Relay | secțiune în `overview.md` | corect |
| UI/Servo | `guides/servo.md` | doar ghid |
| UI/Detective | secțiune scurtă în `overview.md` | corect, scurt |
| UI/Layout | secțiune în `overview.md` | doar nume de tipuri |
| UI/Invalidation | secțiune în `overview.md` + `architecture/diagrams/retained-frame-loop.md` | **diagrama e greșită**: arată 3 cozi, `UIRoot` construiește scheduler-ul cu 6 |
| UI/Rendering | secțiune în `overview.md` | corect |
| UI/Input | secțiune în `overview.md` + `diagrams/ui-layer-boundaries.md` | corect, cu nuanța că rutele vin dintr-un `UiInputTree` derivat |
| UI/Data | `reference/markup-data-bindings.md` | contract corect |
| UI/Markup + SourceGen | secțiune scurtă în `overview.md` + `guides/application-markup.md` | scurt |
| UI/Text + Drawing/Text | — | lipsă |
| UI/Theming | un paragraf în `aspect.md` | lipsă |
| UI/Resources | doar o specificație superpowers înlocuită | lipsă |
| UI/Accessibility | — | lipsă |
| UI/Ink | — | lipsă |
| UI/Hosting + UI/Platform | secțiune în `overview.md` | UI/Platform lipsă |
| Drawing/ | secțiune în `overview.md` | corect |
| Timbre/ + UI/Timbre | `guides/timbre-guide.md` §1, §6–7 | doar modelul runtime din ghid |
| Cerneala.Backends.SdlGpu, Cerneala.Platforms.Sdl3 | `architecture/sdl-desktop-backend.md` | corect (Platforms scurt) |
| Cerneala.Language | secțiune scurtă în `overview.md` | scurt |
| Cerneala.LanguageServer | `guides/language-server.md` | transport și operare |
| Cerneala.PreviewHost | — | lipsă |
| Cerneala.VisualStudio | ghid + decizie arhivată | fără arhitectură |
| Cerneala.Scene2D.Importers/Packages | doar un plan în română | lipsă |
| Tools/ (shader, SVG, pachete, PrismAudit) | README pentru pachete; audit vechi pentru shadere (versiuni SDL3-CS depășite) | parțial, învechit |

Defecte în `overview.md`, verificate de explorator:
- ordinea frame-ului omite faza `CommandState` din `FramePhase`;
- proprietățile moștenite rulează de două ori, nu o dată;
- `Relay` se golește în `UIRoot.BeginUpdate`, în afara scheduler-ului, nu ca primă fază a lui.

Din `archive/architecture-v2.md`, după verificare:
- **valid:** contorii `FrameStats`, nucleul UI independent de backend, retragerea `.cui.xml`, `FrameBudget` amânat (`DefersWork` constant `false`, `MaxWorkItems` necitit);
- **fals:** `UseSdlGpu` (contractul real este `EnsureRegistered()`); precedența cu 6 surse (`UiPropertyStore` are 9: plus `MarkupBase`, `MarkupConditional`, `TemplateBinding`, `TemplateOwnerBinding`); cele 6 tipuri marcate „planificat” există toate.

`Tools/PrismAudit/Program.cs:291-295` cere în `prism-technical-design.md` tokenurile `catalog`, `graph`, `kernel`, `Motion`, `diagnostic`, `DrawingFrameContext`, `IBackdropFrameSource`, `LinearSrgb`, iar hash-ul documentului intră în raportul generat (`:322`).

## 3. Obiective

- Fiecare rând din tabelul §2 are un document de arhitectură în `docs/architecture/` sau o secțiune numită într-unul, legat din `overview.md`.
- Afirmațiile false din §2 sunt corectate.
- Fiecare identificator (tip, membru, cale, diagnostic, comandă) dintr-un document nou sau editat este verificat în codul de la commit-ul notat în subsolul documentului.
- Fiecare invariant afirmat trimite la un test care îl verifică. Dacă nu există un astfel de test, invariantul e marcat „netestat”.

## 4. Non-obiective

- Referință API per membru: stă doar în `docs-site/documentation/classes/`.
- Ghiduri noi de utilizare și secțiuni de arhitectură în `docs-site`.
- Schimbări de cod, chiar dacă documentarea scoate la iveală defecte. Defectele găsite merg prin skill-ul `github-issue-raise`, nu se repară aici.
- Diagrame noi decorative. Se corectează cele existente. O diagramă nouă se adaugă doar când fluxul nu se poate descrie clar în maximum 10 rânduri de text.
- Arhitectura proiectelor din `Playground/`, `Tetrisish/`, `benchmarks/`.
- Afirmații de performanță fără măsurătoare. Unde un document pomenește cost, trimite la rezultatul de benchmark existent sau scrie „nemăsurat”.

## 5. Forma unui document de arhitectură

Fiecare document din `docs/architecture/` are aceste secțiuni. Cele care nu se aplică se omit, nu se umplu.

```markdown
# <Sistem>

> Cod: `<foldere>` · Verificat la commit `<sha>` (<data>)

## Responsabilitate
O frază: ce face sistemul și ce NU face.

## Componente
Tabel: tip | fișier | rol | cine îl deține (proces / aplicație / fereastră / UIRoot / element / frame).

## Flux de date
Pașii, în ordine, de la intrare la efect. Exemplu concret: ce se întâmplă când <acțiune reală, ex. „setezi `Button.Content`”>.

## Ciclu de viață și ownership
Creare, atașare, detașare, dispose; cine eliberează resursele.

## Integrare în frame
Faza din `FramePhase` și ce cozi/invalidări folosește.

## Invarianți
Listă; fiecare cu testul care îl verifică (`tests/...:Test`) sau „netestat”.

## Diagnostic
Ce expune prin Detective, dacă expune ceva.

## Limitări cunoscute
Fapte, cu sursa.
```

**Metoda de verificare,** aceeași pentru fiecare document:
1. Lista identificatorilor scriși între backticks se extrage din document.
2. Fiecare simbol C# se verifică cu Roslyn MCP: `workspace_symbol`, apoi `goto_definition`/`hover` pe `symbolId`. Fiecare cale se verifică cu citire directă. Dacă Roslyn MCP nu e disponibil, verificarea se deleagă unui explorator Haiku conform `CLAUDE.md`.
3. Fiecare test citat trebuie să existe și să verifice afirmația; se citește corpul testului, nu doar numele.
4. Lista verificată se păstrează în `.artifacts/architecture-docs/<document>.txt` (dovadă de lucru, nu se comite).

## 6. Fișiere estimate

În `docs/architecture/`. Estimare; două sisteme mici pot fi unite într-un document dacă au același owner.

- **Editate:** `overview.md`, `aspect.md`, `motion.md`, `prism-technical-design.md`, `sdl-desktop-backend.md`, `diagrams/retained-frame-loop.md`, `diagrams/ui-layer-boundaries.md`.
- **Noi:** `property-system.md` (UI/Core: `UiProperty`, `UiPropertyStore`, precedență), `invalidation-and-frame.md`, `layout.md`, `input.md`, `rendering.md`, `drawing.md`, `markup-and-sourcegen.md`, `language-tooling.md` (Language, LanguageServer, PreviewHost, VisualStudio), `relay.md`, `detective.md`, `servo.md`, `text.md`, `theming-and-resources.md`, `accessibility.md`, `ink.md`, `timbre.md`, `hosting-and-platform.md`, `scene2d.md`, `build-tools.md`.
- **Altele:** `docs/README.md` (link spre harta arhitecturii); `docs/reference/prism-completeness-report.generated.md` (regenerat dacă se editează `prism-technical-design.md`).

## 7. Etape de implementare

Fiecare etapă: întâi citirea codului (descoperirea se deleagă exploratorilor Haiku, conform `CLAUDE.md`), apoi scrierea, apoi verificarea din §5.

### Etapa 0 — Harta și baseline

- [x] Confirmă precondiția: Gate etapa 5 din planul de reorganizare este bifat; `docs/architecture/` conține fișierele mutate. (Planul de reorganizare e `finalizat`, toate gate-urile bifate, commit `0cb32300`.)
- [x] Notează commit-ul de start în acest plan.
- [x] Rulează `dotnet run --project Tools/PrismAudit -- --check` și `Test-MarkdownLinks.ps1 -Baseline .artifacts/docs-reorg/links-baseline.txt`. Ambele trebuie să treacă înainte de editare. (PrismAudit `--check` trece; linkuri: exit 0, 4 linkuri rupte spre 1 țintă, toate în baseline.)
- [x] Pentru fiecare sistem din §2, cere unui explorator lista de tipuri publice și interne principale, fișierele lor și testele care îl acoperă. Rezultatul este doar material de lucru, nu se comite. (7 exploratori Haiku; Roslyn MCP nu era conectat, deci inventarul vine din căutare text + citire directă. Material în `.artifacts/architecture-docs/inventory-*.md`.)

**Gate etapa 0**
- [x] Precondiția e confirmată, cele două verificări trec, iar inventarul pe sistem există pentru toate cele 28 de rânduri. (Cele 27 de rânduri din tabelul §2 plus UI/Core pentru `property-system.md`.)

### Etapa 1 — `overview.md` și sistemul de proprietăți

- [x] `overview.md`: corectează ordinea frame-ului după `FramePhase` și `UIRoot`:
  - include `CommandState`;
  - spune unde rulează proprietățile moștenite de două ori;
  - mută golirea `Relay` în `UIRoot.BeginUpdate`, în afara scheduler-ului.

  Fiecare pas trimite la tipul care îl execută.
- [x] `overview.md`: tabel cu toate sistemele din §2. Fiecare rând are o frază de responsabilitate și un link spre documentul lui. Secțiunile lungi existente se mută în documentele dedicate din etapele următoare, iar în `overview.md` rămâne un rezumat de 2–4 rânduri cu link. (Tabelul „Systems Map” are 28 de rânduri. Cele 7 sisteme fără text de arhitectură azi — Preview host, Text, Resources, Accessibility, Ink, Scene2D, Build tools — scriu „No architecture text yet” și primesc linkul în etapa care le scrie documentul, ca verificatorul de linkuri să rămână la 0; Etapa 7 verifică toate rândurile. „Typed State” e redus la rezumat cu link; celelalte secțiuni se reduc în etapa documentului lor.)
- [x] `property-system.md`: `UiProperty<T>`, `UiPropertyStore`, cele 9 surse de precedență în ordinea din cod, invalidarea la schimbarea valorii efective, proprietățile moștenite. Preia din `archive/architecture-v2.md` doar afirmațiile verificate valide din §2. (Nicio afirmație din arhivă nu era necesară aici; lista ei de precedență cu 6 surse e falsă.)
- [x] Verificarea din §5 pentru ambele documente. (Roslyn MCP neconectat: identificatorii verificați prin citire directă a fișierelor; testele găsite de un explorator Haiku, corpurile citite de părinte. Dovadă: `.artifacts/architecture-docs/overview.txt`, `property-system.txt`.)

**Gate etapa 1**
- [x] Ordinea fazelor din `overview.md` corespunde exact `FramePhase` și apelurilor din `UIRoot`. Fiecare fază are fișier:linie notat în dovadă. (`UiFrameScheduler.cs:132-166`, `UIRoot.cs:483-503`, `UIRoot.cs:417-438`, `UiHost.cs:145-244`.)
- [x] Verificatorul de linkuri iese cu 0; dovada §5 există pentru ambele documente. (Exit 0, 1486 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline.)

### Etapa 2 — Pipeline-ul retained

- [x] `invalidation-and-frame.md`: scheduler-ul, cele 6 cozi construite de `UIRoot` (`RenderQueue`, `AspectQueue`, `HitTestQueue`, `InheritedPropertyQueue`, `CommandStateQueue`, `LayoutQueue`) și nucleul lor comun din Queue Engine 2 (`ElementWorkQueue`, `ElementQueueOrderIndex`), ordinea după `TreeVersion`, curățarea la detașare, recuperarea după excepții, `FrameStats`, `FrameBudget` (amânat: `DefersWork` constant `false`). (Conține și bucla `UiHost` mutată din `overview.md`, secțiunea „Where The Scheduler Runs”.)
- [x] `diagrams/retained-frame-loop.md`: corectează de la 3 la cozile reale.
- [x] `layout.md`: măsurare și aranjare, cum invalidarea de layout ajunge în coadă, contractul de idle frame (frame fără schimbări = fără muncă de layout), cu testul care îl verifică.
- [x] `input.md`: de la sursa platformei la `UiInputTree`, rutare, hit test, focus, capture, comenzi. Rutele vin dintr-un arbore derivat: explică cum și când se reconstruiește.
- [x] `rendering.md`: `RetainedRenderer`, cache-ul retained, ce invalidează o intrare, legătura cu `DrawingContext`.
- [x] `drawing.md`: `DrawingContext`, `DrawCommandList`, `IDrawingBackend`, `DrawingFrameContext`, granița spre backend.
- [x] Verificarea din §5 pentru fiecare document. (Roslyn MCP neconectat în această sesiune: identificatorii verificați prin citire directă; testele găsite de exploratori Haiku, corpurile citite de părinte. Dovadă: `.artifacts/architecture-docs/{invalidation-and-frame,input,layout,rendering,drawing}.txt`. Secțiunile lungi din `overview.md` pentru aceste 5 sisteme sunt reduse la rezumate cu link; ancorele lor rămân.)

**Gate etapa 2**
- [x] Fiecare document are dovada §5 completă; invarianții fără test sunt marcați „netestat”.
- [x] Verificatorul de linkuri iese cu 0. (Exit 0, 1491 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline; ancorele noi verificate manual.)

### Etapa 3 — Autorare

- [x] `markup-and-sourcegen.md`:
  - de la `.crn` la cod generat: generatoarele `UiMarkupApplicationGenerator`, `UiMarkupWindowGenerator`, `UiMarkupUserControlGenerator`, `UiMarkupSceneComponentGenerator`;
  - parserul de directive și emițătoarele;
  - `GeneratedMarkup` ca suprafață runtime;
  - selecția backend-ului;
  - diagnosticele `CERNEALAUI*`.

  Legăturile de date trimit la `reference/markup-data-bindings.md`, fără să-l dubleze. (Premisa planului era greșită: cele 4 nume nu sunt generatoare separate, ci fișiere `partial` ale singurului generator de markup, `UiMarkupGenerator`; al doilea generator este `PrismCatalogGenerator`. Documentul spune asta. Secțiunea „Build-Time Authoring” din `overview.md` e redusă la rezumat cu link; ancora rămâne.)
- [x] `language-tooling.md`:
  - `Cerneala.Language` (sintaxă, semantică, diagnostice, catalogul de diagnostice);
  - `Cerneala.LanguageServer` (transport, workspace, funcționalități);
  - `Cerneala.PreviewHost` (compilare, hot reload de markup, sesiune de randare, protocolul din `Shared/PreviewProtocol.cs`);
  - `Cerneala.VisualStudio` (`ILanguageClient`, pornirea serverului, preview). Decizia arhivată `archive/visual-studio-community-spike.md` se leagă ca istoric.

  (Rândurile Language, Language server, Preview host și Visual Studio din „Systems Map” trimit acum la secțiunile documentului. Un defect găsit — frame-ul PreviewHost e RGBA, clientul VS îl afișează ca `Bgra32` — e dovedit la runtime pe partea host-ului și are draft de issue în `.artifacts/issue-drafts/`, în așteptarea aprobării. Documentul descrie neutru comportamentul real.)
- [x] Verificarea din §5. (Roslyn MCP neconectat: identificatorii verificați prin citire directă; testele găsite de exploratori Haiku, corpurile citite de părinte; o sondă runtime pentru ordinea canalelor în preview. Dovadă: `.artifacts/architecture-docs/markup-and-sourcegen.txt`, `language-tooling.txt`.)

**Gate etapa 3**
- [x] Dovada §5 completă; fiecare proiect din cele 4 de tooling apare în `language-tooling.md` cu cel puțin componente, flux și ownership. (Fiecare dintre cele 4 proiecte are secțiunile Components, Data Flow și Lifecycle And Ownership. Verificatorul de linkuri: exit 0, 1493 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline; ancorele noi verificate manual, niciun link spre ancora `#crn` eliminată.)

### Etapa 4 — Sisteme cu documente existente

- [x] `prism-technical-design.md`: înlocuiește arborele de foldere cu cel real; scoate sau corectează `PrismRenderState`; reverifică numele din pipeline. Păstrează cele 8 tokenuri cerute de `PrismAudit`. Apoi rulează `dotnet run --project Tools/PrismAudit -- --write` (diff-ul raportului trebuie să conțină doar hash-ul acestui document) și `--check`. (`PrismRenderState`, `PrismPropertyKey` și placeholderul au fost scoase. Premisa avea încă două erori: nu există categorie „Composition” și nici `ResourceVersion`, iar tabelul de invalidare e rescris după cod. Tokenurile sunt toate prezente. Diff-ul raportului regenerat conține doar linia de hash; `--check` trece: 178 de intrări, 0 goluri.)
- [x] `aspect.md`: reverificare §5. Precedența trimite la `property-system.md`. (Afirmații false corectate: `GetDiagnostics` e intern, diagnosticul trece acum prin `root.Detective`; registrul nu e snapshot; Motion nu e în afara `AspectEngine`. Au fost adăugate secțiunile „Frame integration”, „Invariants” și „Known limitations”. Testele din cele 3 fișiere ale altei sesiuni nu sunt citate.)
- [x] `motion.md`: extinde de la 39 de linii la forma din §5: `MotionSystem`, timeline-uri, priorități, interacțiunea cu `UiPropertyStore` (sursa de animație), activarea din markup, Motion pe Prism și Timbre.
- [x] `relay.md`: preia secțiunea din `overview.md` (snapshot plafonat, `VerifyAccess`, `UIRoot.Relay`, golirea în `BeginUpdate`) și leagă `docs/audits/2026-09-02-relay-audit.md` ca istoric. (Constatarea auditului despre ordinea viewport-ului nu mai e valabilă, iar documentul o notează.)
- [x] `detective.md`: `UIRoot.Detective`, `DetectiveSnapshot`, ce subsisteme raportează (inclusiv `Detective.Motion`). (`Detective` are doar 4 proprietăți: `Invalidation`, `Motion`, `AspectCounters` și `RenderingCounters`. Documentul explică diferența dintre `Detective.Motion` și `DetectiveSnapshot.Motion`.)
- [x] `servo.md` (arhitectură, distinct de `guides/servo.md`): automatizare în proces, prin aceleași căi de input ca utilizatorul. Corectează eticheta „external automation” din `docs/assets/cerneala-architecture.png` doar în text; imaginea se regenerează doar dacă sursa ei există în repo. (Imaginea nu are sursă în repo, deci nu a fost regenerată. Corecția apare în `servo.md` și în `overview.md`, după linkul spre imagine.)
- [x] Verificarea din §5. (Roslyn MCP neconectat: identificatorii au fost verificați prin citire directă. Testele au fost găsite de exploratori Haiku, iar corpurile lor au fost citite de părinte. Dovadă: `.artifacts/architecture-docs/{prism-technical-design,aspect,motion,relay,detective,servo}.txt`. Secțiunile Relay, Aspect, Motion, Prism și Detective din `overview.md` sunt reduse la rezumate cu link, iar ancorele lor rămân.)

**Gate etapa 4**
- [x] `PrismAudit --check` trece; dovada §5 completă pentru cele 6 documente. (Verificatorul de linkuri: exit 0, 1496 de fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline. Ancorele noi au fost verificate manual. `git diff --check` nu raportează nimic, iar cele 3 documente noi nu au spații la final de rând.)

### Etapa 5 — Sisteme fără document

- [x] `text.md`: `UI/Text` + `Drawing/Text`: modelare, line breaking (`LineBreakService`), layout de text, fonturi (`IDrawFont`, `IFontSource`), cache de texturi de text, dacă există în cod. (Line breaking-ul nu este UAX #14: rupe doar după spațiu sau `- / \ , ; :`. Există două motoare de layout: calea UI și `DrawTextLayout`. Atlasul GPU de text există: pagini de 1024 px, maxim 8.)
- [x] `theming-and-resources.md`: `UI/Theming` (inclusiv `ThemeTokenBridge`) și `UI/Resources`: rezolvarea resurselor, urmărirea dependențelor, schimbarea temei și ce invalidează. (`Theme.Set` aruncă după instalare, nu e ignorat. `ThemeTokenBridge` proiectează doar 5 culori. Un posibil defect, dedus doar din sursă — controllerul de resursă din markup nu se reabonează când se schimbă providerul rădăcinii — e trecut la limitări ca netestat, fără draft de issue.)
- [x] `accessibility.md`: `UI/Accessibility`: arborele de accesibilitate și cum ajunge la platformă. Dacă nu ajunge la nicio platformă azi, documentul spune asta explicit. (Nu ajunge la nicio platformă: `IAccessibilityPlatform` nu are implementare de producție; documentul o spune în Responsibility.)
- [x] `ink.md`: `UI/Ink`. (`InkCanvas` nu desenează și nu primește input de la platformă; documentul o spune.)
- [x] `timbre.md`: `Timbre/` (motor, mixer, decodare, catalog, DSP, buget de memorie: preload 1 MiB, 16 MiB/clip, cache 64 MiB, 64 de voci, reverificate în cod), `UI/Timbre` (atașare din Aspect) și `Cerneala.Platforms.Sdl3/Audio` (ieșirea). Ghidul rămâne pentru utilizare. (Valorile sunt confirmate în `TimbreCatalog.cs:50-53`. În plus: `StreamingMemoryLimit` implicit este `long.MaxValue`, deci pool-ul doar numără.)
- [x] Verificarea din §5. (Roslyn MCP neconectat pentru exploratori: identificatorii verificați prin citire directă; testele găsite de exploratori Haiku, corpurile citite de părinte. Dovadă: `.artifacts/architecture-docs/{text,theming-and-resources,accessibility,ink,timbre}.txt`. Rândurile Text, Theming, Resources, Accessibility, Ink și Timbre din „Systems Map” trimit acum la documentele noi.)

**Gate etapa 5**
- [x] Dovada §5 completă; fiecare dintre cele 5 documente are cel puțin secțiunile Responsabilitate, Componente, Flux de date și Ciclu de viață. (Toate 5 au Responsibility, Components, Data Flow și Lifecycle And Ownership; la `accessibility.md` secțiunea a fost adăugată la gate. Verificatorul de linkuri: exit 0, 1501 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline. Documentele noi nu au linkuri cu ancore. `git diff --check` curat; documentele noi nu au spații la final de rând.)

### Etapa 6 — Găzduire, backend-uri, unelte, Scene2D

- [x] `hosting-and-platform.md`: `UI/Hosting` + `UI/Platform`: `Application`, ferestre, `ApplicationBackendAttribute`, `EnsureRegistered()`, `IUiBackend`, abstracțiile de platformă implementate de `Cerneala.Platforms.Sdl3`. (`IUiBackend` nu are implementare de producție; SDL completează doar 2 din cele 7 servicii de platformă: cursor și text input. Secțiunea „Application And Window Hosting” din `overview.md` e redusă la rezumat cu link, ancora rămâne, iar afirmația despre clipboard e scoasă.)
- [x] `sdl-desktop-backend.md`: reverificare §5; extinde partea `Cerneala.Platforms.Sdl3` (ferestre, input, audio, interop). (Afirmații greșite corectate: atlasul de text eliberează intrări, nu pagini întregi; driverul SDL_GPU nu e impus și se cere și MetalLib; job-ul CI nu alege driverul audio dummy, testele îl aleg singure; numele upstream SDL3-CS 3.4.16. Secțiune nouă „SDL3 platform”: ferestre, tabelul de evenimente, harta de taste, DPI, fire, granița de interop. Istoricul de verificare Graphix e păstrat neschimbat.)
- [x] `scene2d.md`:
  - formatul de pachet (`Scene2DPackageWriter`, `PackageIndex`, cititoarele de interval);
  - importatorii Tiled și LDtk;
  - compilatorul `Tools/Cerneala.Scene2D.PackageCompiler`;
  - legătura cu controalele `TileMap2D`, `CollisionWorld2D`, `SpriteAnimation`.

  Planul în română `2026-09-24-scene2d-stage0-contract-proposal.md` se leagă ca istoric. (Legat ca istoric, cu afirmațiile lui învechite notate: puntea internă „planificată” și `TileMap2D.DisposeAsync` „selectat” există acum. Posibil defect dedus doar din sursă: compilatorul de pachete prinde doar 4 tipuri de excepții; nereprodus, deci trecut la limitări fără draft de issue.)
- [x] `build-tools.md`: `Tools/Cerneala.SdlShaderCompiler` (versiunile reale din `.csproj`: Graphix-CS 3.4.16.1 și Shadercross 3.0.0.9, de reverificat), `VerifySdlShaderArtifacts` din `Cerneala.Backends.SdlGpu.csproj`, `Tools/Cerneala.SvgAssetCompiler`, `Tools/PrismAudit`, `Tools/scripts/New-PrismFilterReference.ps1`, `Tools/RoslynMcp` (legat la README-ul lui, fără să-l dubleze). (Versiuni confirmate: Graphix-CS 3.4.16.1, Graphix.Native 3.4.16-graphix.6, `SDL3-CS.{Windows,Linux,MacOS}.Shadercross` 3.0.0.9.)
- [x] Verificarea din §5. (Roslyn MCP neconectat pentru exploratori: identificatorii verificați prin citire directă; testele găsite de exploratori Haiku, corpurile citite de părinte. Dovadă: `.artifacts/architecture-docs/{hosting-and-platform,sdl-desktop-backend,scene2d,build-tools}.txt`. Rândurile Hosting, Scene2D și Build tools din „Systems Map” trimit acum la documentele lor.)

**Gate etapa 6**
- [x] Dovada §5 completă pentru cele 4 documente. (Verificatorul de linkuri: exit 0, 1504 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline. Ancorele noi verificate manual. `git diff --check` curat; documentele noi nu au spații la final de rând.)

### Etapa 7 — Verificare finală

- [x] Fiecare rând din §2 are în `overview.md` un link care funcționează spre documentul sau secțiunea lui. (Toate cele 28 de rânduri din „Systems Map” au link; nu mai există niciun „No architecture text yet”. Ancorele verificate manual.)
- [x] `Test-MarkdownLinks.ps1 -Baseline …` iese cu 0; `PrismAudit --check` trece. (Linkuri: exit 0, 1504 fișiere, 4 linkuri rupte spre 1 țintă, toate în baseline. PrismAudit: 178 de intrări, 31 de proprietăți comune, 216 tipuri publice Prism, 0 goluri.)
- [x] Recitește complet fiecare document nou sau editat față de sursa curentă. Dacă între timp au intrat commit-uri care ating sistemele documentate, reverifică identificatorii afectați și actualizează commit-ul din subsol. (`HEAD` este încă `0cb32300`, deci nu au intrat commit-uri noi. Toate cele 24 de documente și cele 2 diagrame au fost recitite, iar referințele fișier:linie și constantele au fost verificate prin sondaj în cod. Singura afirmație falsă găsită: `diagrams/ui-layer-boundaries.md` lista folderul `UI/Styling`, șters în 2026-07. Acum listează `UI/Aspect`, iar regula despre rute trimite la `UiInputTree`. Dovadă: `.artifacts/architecture-docs/ui-layer-boundaries.txt`.)
- [x] Defectele de cod găsite în timpul documentării sunt raportate prin `github-issue-raise` (draft în `.artifacts/issue-drafts/` dacă utilizatorul lipsește), cu lista lor în raportul final. (Un defect dovedit are draft care așteaptă aprobarea: `.artifacts/issue-drafts/2026-10-10-vs-preview-red-blue-swap.md`. Celelalte defecte posibile au fost deduse doar din sursă, fără reproducere, și sunt trecute la „Known Limitations” cu mențiunea „netestat”, fără issue. Lista completă e în raportul final.)
- [x] Testele nu se rulează: planul schimbă doar `.md` și raportul regenerat de `PrismAudit`, care este verificat de `--check`. (În diff-ul planului nu există fișiere de cod. Cele 3 fișiere de test modificate din worktree aparțin altei sesiuni și nu fac parte din acest plan.)

**Gate etapa 7**
- [x] Toate punctele de mai sus trec.

## 8. Ordinea recomandată

0 → 1 → 2 → 3 → 4 → 5 → 6 → 7. Etapa 1 e prima, pentru că `property-system.md` și ordinea frame-ului sunt citate de aproape toate celelalte. După Etapa 1, Etapele 2–6 sunt independente ca fișiere, dar se fac pe rând, una pe lot.

## 9. Condiții de oprire

- Precondiția din antet nu este îndeplinită → nu se începe.
- Un comportament din cod pare greșit → se raportează prin `github-issue-raise`. Documentul descrie comportamentul real și trimite la issue, fără să-l prezinte ca intenționat sau ca bug confirmat.
- Un sistem nu are owner clar în cod (ex. două tipuri par să dețină aceeași stare) → documentul numește ambii candidați și marchează „owner nerezolvat”; nu inventează unul.

## 10. Definiția de gata

- `docs/architecture/` conține documentele din §6, fiecare în forma din §5, cu commit-ul de verificare în subsol.
- `overview.md` are ordinea frame-ului corectă și leagă toate cele 28 de rânduri din §2.
- Afirmațiile false din §2 nu mai apar în documentele vii.
- Fiecare document are dovada §5 în `.artifacts/architecture-docs/`; invarianții fără test sunt marcați „netestat”.
- `PrismAudit --check` și verificatorul de linkuri trec.
