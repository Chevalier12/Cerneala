# Prism — retained rendering și graful de compoziție

## Scop

Introduce scope-urile Prism în lista retained de comenzi, analizează un frame o
singură dată și construiește un graf backend-neutral. Etapa nu execută shader-e
și nu deține resurse GPU.

**Dependență:** `2026-07-18-prism-foundation-and-catalog.md`.

## Etapa 0 — contracte RED

- [x] Adaugă teste RED în `tests/Cerneala.Tests/Drawing/Prism/` pentru
  `BeginPrism`/`EndPrism`, nesting, clip, transform, opacity, Presence children
  și fallback-ul backendurilor fără Prism.
- [x] Fixează prin teste ordinea din `DrawCommandListBuilder`: `PushClip`,
  `BeginPrism`, comenzile locale/copiii/exiting children, `EndPrism`, `PopClip`.
- [x] Adaugă teste RED pentru analiză: zero/unu/mai multe scope-uri, scope
  invalid, versiune stale și cererea de backdrop făcută cel mult o dată.
- [x] Adaugă teste RED pentru graful Photoshop: procesare bottom-up, grupuri,
  măști, clipping chain, layer invizibil, `PassThrough` și backdrop separat.

### Gate etapa 0

- [x] Testele descriu semantică backend-neutral și nu fac assertions pe
  SpriteBatch, RenderTarget2D ori numele unor shader-e.

## Etapa 1 — comenzi retained tipate

- [x] Extinde `Drawing/DrawCommandKind.cs` cu `BeginPrism` și `EndPrism`.
- [x] Extinde `Drawing/DrawCommand.cs` cu un payload readonly și tipat
  `PrismDrawScope`, plus fabrici dedicate; nu folosi `object`, dictionary sau
  identificatori string.
- [x] Payload-ul conține numai starea necesară frame-ului: definiție/instanță,
  bounds, transform, pixel scale, versiunile structurală/de valori și generația
  vizuală agregată a subtree-ului capturat.
- [x] Include un `PrismCacheOwnerToken` numeric și fără referință inversă, astfel
  încât backendul să poată indexa/invalida intrări fără să rețină elementul.
- [x] Adaugă în `Drawing/DrawCommandList.cs` o versiune structurală minimă,
  actualizată determinist la `Add`/`Clear`, pentru invalidarea analizei cached.
- [x] Actualizează toate switch-urile de transform/translate și backendurile
  fake astfel încât scope-ul să fie păstrat ori ignorat explicit.

### Gate etapa 1

- [x] Lista de comenzi rămâne reutilizabilă și readonly pentru consumatori,
  fără metadata generică pentru un DAG care nu există încă.

## Etapa 2 — integrarea builderului retained

- [x] Extinde `UI/Rendering/DrawCommandListBuilder.cs` să emită scope-ul exact
  în jurul randării elementului, după clip și înainte de comenzile locale.
- [x] Păstrează transformul și coordonatele scope-ului sincronizate cu aceleași
  operații folosite pentru comenzile interioare.
- [x] Exclude complet scope-ul pentru elemente fără Prism și pentru stări
  nerandabile; un layer intern invizibil rămâne decizia grafului, nu a builderului.
- [x] Verifică nesting-ul unui element Prism în alt element Prism și Presence
  exiting children fără intercalarea greșită a perechilor Begin/End.
- [x] Dacă invalidarea existentă nu poate separa compoziția de layout, adaugă
  cea mai mică categorie presentation-only în scheduler/retained cache și
  dovedește că nu reconstruiește `ElementRenderCache`. (Nu a fost necesar:
  invalidarea Prism existentă este deja presentation-only, iar testele confirmă
  reutilizarea `ElementRenderCache` și a listei structurale.)

### Gate etapa 2

- [x] Snapshot-urile comenzilor sunt stabile, perechile sunt balansate, iar o
  schimbare de parametru Prism nu regenerează comenzile structurale.

## Etapa 3 — analiza unică a frame-ului

- [x] Adaugă `Drawing/Prism/Graph/PrismFrameAnalyzer` și rezultatul imuabil
  `PrismFrameAnalysis`, indexat după poziția comenzilor și versiunea listei.
- [x] Analizorul face o singură trecere, validează nesting-ul și calculează
  scope-urile active, bounds, necesarul de suprafețe, backdrop și capabilități.
- [x] Produce pentru fiecare scope un `PrismDependencyStamp` backend-neutral,
  compact și fără referințe la elemente, folosind versiunile retained propagate
  incremental în locul unei traversări suplimentare a subtree-ului.
- [x] Refolosește aceeași analiză în host și `PrismGraphBuilder`; interzice
  scanarea separată a listei pentru backdrop ori bugete.
- [x] Schimbă `IDrawingBackend.Render` să primească un
  `DrawingFrameContext` tipat care transportă analiza și lease-ul backdrop
  opțional, fără dependență MonoGame.
- [x] Actualizează `RetainedRenderer`, `UiHost`, `IUiBackend`, backendurile,
  test doubles și call site-urile existente pentru noul contract.
- [x] Respinge analiza stale când versiunea listei nu mai corespunde și testează
  reutilizarea sigură a `DrawCommandList`.

### Gate etapa 3

- [x] Un frame este analizat o singură dată, contextul nu deține resurse peste
  frame, iar toate backendurile non-Prism păstrează outputul anterior.

## Etapa 4 — graful semantic

- [x] Adaugă `PrismGraphBuilder` cu noduri/edges tipate pentru capture, layer,
  group, filter, style, mask, clip-to-below, composite, color conversion și
  backdrop input.
- [x] Atribuie fiecărui nod identitate structurală stabilă și dependențe
  pixel-affecting explicite, necesare cache-ului retained cross-frame.
- [x] Capturează imaginea controlului o singură dată și tratează rezultatele
  intermediare ca valori explicite; nu permite layerului o sursă arbitrară.
- [x] Construiește copiii în ordine bottom-up, păstrând ordinea declarată pentru
  naming/diagnostics și izolând grupurile non-`PassThrough`.
- [x] Modelează clipping chain și mask alpha separat; `Opacity` și `Fill` sunt
  operații distincte, conform semanticii Photoshop.
- [x] Include nodul backdrop numai ca plan de input separat, fără achiziție sau
  API de host în această etapă.
- [x] Emite diagnostics cu numele compoziției/nodului și source span-ul păstrat
  de definiție când un graf nu poate fi construit.

### Gate etapa 4

- [x] Golden snapshots ale grafului confirmă ordinea, dependențele și numărul
  de capturi pentru compoziții simple, nested, masked și clipped.

## Etapa 5 — optimizare sigură

- [x] Adaugă `PrismGraphOptimizer` separat de builder; optimizerul nu modifică
  definiția sau instanța.
- [x] Marchează nodurile deterministe cacheable numai când toate resursele și
  valorile lor pot participa la dependency stamp; celelalte rămân explicit
  necacheable.
- [x] Elimină noduri no-op dovedite, layere invizibile și conversii redundante;
  fuzionează pași numai când catalogul declară echivalența.
- [x] Calculează bounds extinse pentru blur, shadow, stroke și transform fără a
  afecta layout/hitbox și fără a tăia pixeli necesari.
- [x] Estimează peak live surfaces prin lifetime analysis și transmite un plan
  explicit executorului, fără alocare GPU.
- [x] Adaugă teste diferențiale builder vs optimizer pentru alpha, blend order,
  masks, clipping și grupuri.

### Gate etapa 5

- [x] Graful optimizat este semantic echivalent cu cel brut și niciun test nu
  depinde de ordinea accidentală a colecțiilor.

## Etapa 6 — documentare și verificare

- [x] Actualizează cu skill-ul `writing-api-documentation` paginile
  `IDrawingBackend`, `IUiBackend` și toate tipurile publice ale frame contextului;
  sincronizează manifestul.
- [x] Rulează reindexarea după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "Prism|DrawCommand|RetainedRenderer"`
  și `dotnet test .\Cerneala.slnx`.
- [x] Rulează `git diff --check` și diff-ul API public.

## Definiția de gata

- [x] Lista retained exprimă scope-uri Prism tipate și rămâne compatibilă cu
  backendurile care nu execută efecte.
- [x] Frame-ul este analizat o singură dată, iar graful brut și optimizat au
  semantică verificată fără GPU.
- [x] Identitățile nodurilor și dependency stamp-urile sunt stabile, complete și
  gata de consumat de cache fără lookup string ori referințe la UI.
- [x] Testele, documentația și toate gate-urile sunt verzi.
