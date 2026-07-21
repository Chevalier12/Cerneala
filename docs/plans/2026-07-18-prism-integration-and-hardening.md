# Prism — integrare și hardening

## Scop

Închide matricea de acoperire, probează Prism într-un view Cerneala real și
stabilește prin măsurători limitele, performanța și rezistența la lifecycle.

**Dependențe:** toate celelalte planuri Prism din
`2026-07-18-prism-plan-index.md`.

## Etapa 0 — auditul completitudinii

- [x] Generează raportul final catalog → sintaxă → binder → runtime → graph →
  kernel → Motion → diagnostics → test → documentație.
- [x] Compară raportul cu toate filtrele, styles, blend modes, masks, backdrop,
  color profiles și proprietățile aprobate în cele două documente de design.
- [x] Elimină intrările moarte, dublurile și API-urile adăugate „poate cândva”;
  nu completa golurile prin fallback tăcut.
- [x] Rulează diff-ul API față de baseline-ul fundației și justifică fiecare
  simbol public printr-un scenariu din proposal.

### Gate etapa 0

- [x] Raportul are zero goluri, zero defaults divergente și zero API public
  fără consumator curent.

## Etapa 1 — diagnostics operaționale

- [x] Finalizează diagnostics pentru parse/binding, capabilități, fallback,
  graph build, surface budgets, shader load și backdrop acquisition.
- [x] Expune o vedere diagnostică internă cu compoziții active, passes,
  captures, surfaces, peak, allocations, fallback și Motion activ, fără
  referințe care prelungesc lifetime-ul elementelor.
- [x] Fă dump-ul grafului determinist și redactează identificatorii GPU
  instabili, astfel încât snapshot-urile să fie utile în CI.
- [x] Adaugă teste care verifică faptul că diagnostics dezactivate au overhead
  minim și zero alocări per frame după warmup.

### Gate etapa 1

- [x] Pentru orice failure path important există un diagnostic precis și un
  test, iar diagnostics nu introduc memory leak ori muncă ascunsă.

## Etapa 2 — dogfooding în Presentation

- [x] Aplică Prism prin Cerneala markup unui element natural din
  `CernealaPresentation/SolarSystemChapterView.cui.xml`, preferabil cardului de
  planetă și fundalului lui, fără custom control `OnRender` și fără schimbarea
  logicii orbitelor/selectării.
- [x] Definește compoziția reutilizabilă ca `PrismComposition` și exercită
  minimum layer, group, style/filter, mask, Motion pe layer numit și backdrop.
- [x] Adaugă o stare deterministă de automatizare care fixează planeta,
  animația, viewportul și timpul înainte de captură.
- [x] Extinde raportul existent din
  `CernealaPresentation/PresentationWindow.Automation.cs` cu counters Prism
  utili, fără API de test în view-ul de producție.
- [x] Capturează desktop și viewport mic exclusiv prin API-ul
  `IWindowPlatform.RenderPng`; verifică vizual și automat lipsa overlapului,
  clippingului accidental și textului ilizibil.

### Gate etapa 2

- [x] Exemplul real folosește numai markup și API-uri Prism publice, iar orice
  defect descoperit este reparat în framework și acoperit de regression test.

## Etapa 3 — lifecycle și memorie

- [x] Rulează minimum 10.000 de cicluri attach/detach/reattach pentru instanțe
  Prism cu Motion, bindings, filters, styles, masks și backdrop.
- [x] Automatizează navigarea repetată SolarSystem ↔ Diagnostics și verifică
  zero Motion, passes, leases și suprafețe active după ascundere/detașare.
- [x] Testează hide/unhide, `Collapsed`, resource replacement, template
  recycling, root replacement, resize, device reset și backdrop source
  replacement.
- [x] Folosește `WeakReference`, contoarele poolului și snapshoturi de memorie
  pentru a separa managed leaks de resurse GPU neeliberate.
- [x] Repară orice leak în owner-ul invariantului și adaugă întâi un test RED;
  nu introduce cleanup special în SolarSystem. (Nu a fost necesar: stress-ul,
  `WeakReference`-urile și contoarele GPU nu au detectat niciun leak.)

### Gate etapa 3

- [x] După GC/device cleanup, numărul elementelor, instanțelor, Motion handles,
  leases și resurselor GPU reținute revine la baseline.

## Etapa 4 — performanță și bugete

- [x] Construiește benchmarkuri pentru static, animated parameter, multe
  layere, chains de filtre, styles, nested groups și backdrop împărțit.
- [x] Măsoară CPU build/submit, GPU frame time, passes, captures, allocations,
  peak live surfaces, hit/miss retained și memorie GPU la rezoluții
  reprezentative.
- [x] Confirmă zero alocări managed după warmup pentru Prism static și lipsa
  rebuildului `ElementRenderCache` la animații nonstructurale.
- [x] Confirmă că al doilea frame identic produce hit retained, zero capture și
  zero effect passes acoperite, iar fiecare input pixel-affecting produce miss.
- [x] Stabilește abia acum valorile implicite pentru surface budget și limitele
  cache-ului transient/retained; documentează datele și comportamentul când
  limita este depășită.
- [x] Nu adăuga adaptive quality, async compute sau API de plugin terț;
  înregistrează separat oportunitățile doar dacă datele le cer.

### Gate etapa 4

- [x] Bugetele au benchmark reproductibil și failure behavior determinist, iar
  scena dogfood rămâne în target fără degradare ascunsă.

## Etapa 5 — documentație și compatibilitate

- [x] Actualizează proposal-ul și TDD-ul cu deciziile finale măsurate, păstrând
  clar separate contractul implementat și ideile amânate.
- [x] Folosește skill-ul `writing-api-documentation` pentru toate paginile din
  `docs-site/documentation/classes/` atinse și sincronizează manifestul.
- [x] Adaugă ghiduri concise pentru modelul Photoshop, sursa implicită,
  layer/group/mask/clipping, Motion paths, backdrop și diagnostics; datele
  catalogului rămân generate.
- [x] Verifică backendurile non-Prism: conținutul interior se redă normal, fără
  excepție, schimbare de layout/input sau obligația unui backdrop provider.
- [x] Rulează verificarea de compatibilitate API și marchează explicit orice
  breaking change necesar în documentația release-ului.

### Gate etapa 5

- [x] Documentația descrie exact comportamentul implementat și toate API-urile
  publice au pagină și intrare corectă în manifest.

## Etapa 6 — suită finală

- [x] Rulează reindexarea completă și
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- doctor`.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`.
- [x] Rulează `dotnet test .\Cerneala.slnx` din checkout/build curat, inclusiv
  recompilarea shaderelor.
- [x] Rulează automatizarea Presentation și toate capturile golden prin API pe
  viewporturile stabilite.
- [x] Rulează benchmarkurile și stress tests finale, salvează baseline-urile
  reproductibile și verifică limitele.
- [x] Rulează `git diff --check`, raportul API și raportul catalogului.

## Definiția de gata

- [x] Prism este demonstrat end-to-end în markup real, fără code-behind de
  randare sau workaround la nivel de view.
- [x] Catalogul complet, conformance-ul, lifecycle-ul, memoria, performanța,
  cache-ul retained, compatibilitatea și documentația sunt verzi.
- [x] Toate gate-urile din index și din cele nouă planuri sunt bifate.
