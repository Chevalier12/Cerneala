# Plan: Documentație de arhitectură pentru fiecare sistem Cerneala

> Data: 2026-10-10
> Status: planificat
> Baseline: commit `def7f419` (master).
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

- [ ] Confirmă precondiția: Gate etapa 5 din planul de reorganizare este bifat; `docs/architecture/` conține fișierele mutate.
- [ ] Notează commit-ul de start în acest plan.
- [ ] Rulează `dotnet run --project Tools/PrismAudit -- --check` și `Test-MarkdownLinks.ps1 -Baseline .artifacts/docs-reorg/links-baseline.txt`. Ambele trebuie să treacă înainte de editare.
- [ ] Pentru fiecare sistem din §2, cere unui explorator lista de tipuri publice și interne principale, fișierele lor și testele care îl acoperă. Rezultatul este doar material de lucru, nu se comite.

**Gate etapa 0**
- [ ] Precondiția e confirmată, cele două verificări trec, iar inventarul pe sistem există pentru toate cele 28 de rânduri.

### Etapa 1 — `overview.md` și sistemul de proprietăți

- [ ] `overview.md`: corectează ordinea frame-ului după `FramePhase` și `UIRoot`:
  - include `CommandState`;
  - spune unde rulează proprietățile moștenite de două ori;
  - mută golirea `Relay` în `UIRoot.BeginUpdate`, în afara scheduler-ului.

  Fiecare pas trimite la tipul care îl execută.
- [ ] `overview.md`: tabel cu toate sistemele din §2. Fiecare rând are o frază de responsabilitate și un link spre documentul lui. Secțiunile lungi existente se mută în documentele dedicate din etapele următoare, iar în `overview.md` rămâne un rezumat de 2–4 rânduri cu link.
- [ ] `property-system.md`: `UiProperty<T>`, `UiPropertyStore`, cele 9 surse de precedență în ordinea din cod, invalidarea la schimbarea valorii efective, proprietățile moștenite. Preia din `archive/architecture-v2.md` doar afirmațiile verificate valide din §2.
- [ ] Verificarea din §5 pentru ambele documente.

**Gate etapa 1**
- [ ] Ordinea fazelor din `overview.md` corespunde exact `FramePhase` și apelurilor din `UIRoot`. Fiecare fază are fișier:linie notat în dovadă.
- [ ] Verificatorul de linkuri iese cu 0; dovada §5 există pentru ambele documente.

### Etapa 2 — Pipeline-ul retained

- [ ] `invalidation-and-frame.md`: scheduler-ul, cele 6 cozi construite de `UIRoot` (`RenderQueue`, `AspectQueue`, `HitTestQueue`, `InheritedPropertyQueue`, `CommandStateQueue`, `LayoutQueue`) și nucleul lor comun din Queue Engine 2 (`ElementWorkQueue`, `ElementQueueOrderIndex`), ordinea după `TreeVersion`, curățarea la detașare, recuperarea după excepții, `FrameStats`, `FrameBudget` (amânat: `DefersWork` constant `false`).
- [ ] `diagrams/retained-frame-loop.md`: corectează de la 3 la cozile reale.
- [ ] `layout.md`: măsurare și aranjare, cum invalidarea de layout ajunge în coadă, contractul de idle frame (frame fără schimbări = fără muncă de layout), cu testul care îl verifică.
- [ ] `input.md`: de la sursa platformei la `UiInputTree`, rutare, hit test, focus, capture, comenzi. Rutele vin dintr-un arbore derivat: explică cum și când se reconstruiește.
- [ ] `rendering.md`: `RetainedRenderer`, cache-ul retained, ce invalidează o intrare, legătura cu `DrawingContext`.
- [ ] `drawing.md`: `DrawingContext`, `DrawCommandList`, `IDrawingBackend`, `DrawingFrameContext`, granița spre backend.
- [ ] Verificarea din §5 pentru fiecare document.

**Gate etapa 2**
- [ ] Fiecare document are dovada §5 completă; invarianții fără test sunt marcați „netestat”.
- [ ] Verificatorul de linkuri iese cu 0.

### Etapa 3 — Autorare

- [ ] `markup-and-sourcegen.md`:
  - de la `.crn` la cod generat: generatoarele `UiMarkupApplicationGenerator`, `UiMarkupWindowGenerator`, `UiMarkupUserControlGenerator`, `UiMarkupSceneComponentGenerator`;
  - parserul de directive și emițătoarele;
  - `GeneratedMarkup` ca suprafață runtime;
  - selecția backend-ului;
  - diagnosticele `CERNEALAUI*`.

  Legăturile de date trimit la `reference/markup-data-bindings.md`, fără să-l dubleze.
- [ ] `language-tooling.md`:
  - `Cerneala.Language` (sintaxă, semantică, diagnostice, catalogul de diagnostice);
  - `Cerneala.LanguageServer` (transport, workspace, funcționalități);
  - `Cerneala.PreviewHost` (compilare, hot reload de markup, sesiune de randare, protocolul din `Shared/PreviewProtocol.cs`);
  - `Cerneala.VisualStudio` (`ILanguageClient`, pornirea serverului, preview). Decizia arhivată `archive/visual-studio-community-spike.md` se leagă ca istoric.
- [ ] Verificarea din §5.

**Gate etapa 3**
- [ ] Dovada §5 completă; fiecare proiect din cele 4 de tooling apare în `language-tooling.md` cu cel puțin componente, flux și ownership.

### Etapa 4 — Sisteme cu documente existente

- [ ] `prism-technical-design.md`: înlocuiește arborele de foldere cu cel real; scoate sau corectează `PrismRenderState`; reverifică numele din pipeline. Păstrează cele 8 tokenuri cerute de `PrismAudit`. Apoi rulează `dotnet run --project Tools/PrismAudit -- --write` (diff-ul raportului trebuie să conțină doar hash-ul acestui document) și `--check`.
- [ ] `aspect.md`: reverificare §5. Precedența trimite la `property-system.md`.
- [ ] `motion.md`: extinde de la 39 de linii la forma din §5: `MotionSystem`, timeline-uri, priorități, interacțiunea cu `UiPropertyStore` (sursa de animație), activarea din markup, Motion pe Prism și Timbre.
- [ ] `relay.md`: preia secțiunea din `overview.md` (snapshot plafonat, `VerifyAccess`, `UIRoot.Relay`, golirea în `BeginUpdate`) și leagă `docs/audits/2026-09-02-relay-audit.md` ca istoric.
- [ ] `detective.md`: `UIRoot.Detective`, `DetectiveSnapshot`, ce subsisteme raportează (inclusiv `Detective.Motion`).
- [ ] `servo.md` (arhitectură, distinct de `guides/servo.md`): automatizare în proces, prin aceleași căi de input ca utilizatorul. Corectează eticheta „external automation” din `docs/assets/cerneala-architecture.png` doar în text; imaginea se regenerează doar dacă sursa ei există în repo.
- [ ] Verificarea din §5.

**Gate etapa 4**
- [ ] `PrismAudit --check` trece; dovada §5 completă pentru cele 6 documente.

### Etapa 5 — Sisteme fără document

- [ ] `text.md`: `UI/Text` + `Drawing/Text`: modelare, line breaking (`LineBreakService`), layout de text, fonturi (`IDrawFont`, `IFontSource`), cache de texturi de text, dacă există în cod.
- [ ] `theming-and-resources.md`: `UI/Theming` (inclusiv `ThemeTokenBridge`) și `UI/Resources`: rezolvarea resurselor, urmărirea dependențelor, schimbarea temei și ce invalidează.
- [ ] `accessibility.md`: `UI/Accessibility`: arborele de accesibilitate și cum ajunge la platformă. Dacă nu ajunge la nicio platformă azi, documentul spune asta explicit.
- [ ] `ink.md`: `UI/Ink`.
- [ ] `timbre.md`: `Timbre/` (motor, mixer, decodare, catalog, DSP, buget de memorie: preload 1 MiB, 16 MiB/clip, cache 64 MiB, 64 de voci, reverificate în cod), `UI/Timbre` (atașare din Aspect) și `Cerneala.Platforms.Sdl3/Audio` (ieșirea). Ghidul rămâne pentru utilizare.
- [ ] Verificarea din §5.

**Gate etapa 5**
- [ ] Dovada §5 completă; fiecare dintre cele 5 documente are cel puțin secțiunile Responsabilitate, Componente, Flux de date și Ciclu de viață.

### Etapa 6 — Găzduire, backend-uri, unelte, Scene2D

- [ ] `hosting-and-platform.md`: `UI/Hosting` + `UI/Platform`: `Application`, ferestre, `ApplicationBackendAttribute`, `EnsureRegistered()`, `IUiBackend`, abstracțiile de platformă implementate de `Cerneala.Platforms.Sdl3`.
- [ ] `sdl-desktop-backend.md`: reverificare §5; extinde partea `Cerneala.Platforms.Sdl3` (ferestre, input, audio, interop).
- [ ] `scene2d.md`:
  - formatul de pachet (`Scene2DPackageWriter`, `PackageIndex`, cititoarele de interval);
  - importatorii Tiled și LDtk;
  - compilatorul `Tools/Cerneala.Scene2D.PackageCompiler`;
  - legătura cu controalele `TileMap2D`, `CollisionWorld2D`, `SpriteAnimation`.

  Planul în română `2026-09-24-scene2d-stage0-contract-proposal.md` se leagă ca istoric.
- [ ] `build-tools.md`: `Tools/Cerneala.SdlShaderCompiler` (versiunile reale din `.csproj`: Graphix-CS 3.4.16.1 și Shadercross 3.0.0.9, de reverificat), `VerifySdlShaderArtifacts` din `Cerneala.Backends.SdlGpu.csproj`, `Tools/Cerneala.SvgAssetCompiler`, `Tools/PrismAudit`, `Tools/scripts/New-PrismFilterReference.ps1`, `Tools/RoslynMcp` (legat la README-ul lui, fără să-l dubleze).
- [ ] Verificarea din §5.

**Gate etapa 6**
- [ ] Dovada §5 completă pentru cele 4 documente.

### Etapa 7 — Verificare finală

- [ ] Fiecare rând din §2 are în `overview.md` un link care funcționează spre documentul sau secțiunea lui.
- [ ] `Test-MarkdownLinks.ps1 -Baseline …` iese cu 0; `PrismAudit --check` trece.
- [ ] Recitește complet fiecare document nou sau editat față de sursa curentă. Dacă între timp au intrat commit-uri care ating sistemele documentate, reverifică identificatorii afectați și actualizează commit-ul din subsol.
- [ ] Defectele de cod găsite în timpul documentării sunt raportate prin `github-issue-raise` (draft în `.artifacts/issue-drafts/` dacă utilizatorul lipsește), cu lista lor în raportul final.
- [ ] Testele nu se rulează: planul schimbă doar `.md` și raportul regenerat de `PrismAudit`, care este verificat de `--check`.

**Gate etapa 7**
- [ ] Toate punctele de mai sus trec.

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
