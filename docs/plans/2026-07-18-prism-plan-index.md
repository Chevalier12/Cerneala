# Prism — indexul planului de implementare

## Scop

Acest index transformă deciziile din
[`docs/prism-technical-design.md`](../prism-technical-design.md) și
[`docs/prism-markup-syntax-proposal.md`](../prism-markup-syntax-proposal.md)
într-o ordine de implementare verificabilă. Prism rămâne procesare vizuală:
nu modifică layout-ul, hitbox-ul sau rutarea inputului.

Planul este împărțit pentru ca SourceGen, runtime-ul retained, executorul GPU și
backdrop-ul să poată avea teste și gate-uri proprii. Nu se implementează „totul
într-o clasă mare”, iar etapele independente nu sunt forțate într-un singur
branch logic.

## Decizii obligatorii

- Sintaxa publică are numai directivele `@prism`, `@parameter`, `@layer`,
  `@group`, `@filter`, `@style`, `@mask` și `@backdrop`.
- Resursa reutilizabilă se numește `PrismComposition`; instanțele sunt create
  per element, iar definițiile compilate sunt imuabile și partajabile.
- Imaginea controlului este sursa implicită. Un `@layer` este frunză, un
  `@group` poate conține layere sau grupuri, iar primul element declarat este
  în față. Evaluarea compoziției se face de jos în sus.
- Un `@backdrop` este opțional, unic și ultimul copil direct al Prism-ului.
- Catalogul de tipuri, proprietăți, valori implicite, identificatori și
  capabilități are o singură sursă machine-readable.
- Prima implementare nu expune SDK public pentru filtre terțe și nu compilează
  shader-e la runtime.
- Rezultatele GPU Prism stabile sunt păstrate și reutilizate între frame-uri pe
  baza unui dependency stamp complet, sub buget explicit și fără referințe la UI.
- Invizibil, `Hidden`, `Collapsed` sau detașat înseamnă zero lucru Prism și
  anularea Motion-ului asociat.
- Back-endurile care nu implementează Prism ignoră scope-ul vizual și redau
  conținutul interior normal.

## Ordine și dependențe

1. [Fundație și catalog](2026-07-18-prism-foundation-and-catalog.md) — fără
   dependențe. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `xhigh`

2. [Markup, Motion și lifecycle](2026-07-18-prism-markup-motion-and-lifecycle.md)
   — depinde de fundație. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

3. [Retained rendering și graful de compoziție](2026-07-18-prism-retained-composition-graph.md)
   — depinde de fundație; poate avansa în paralel cu markup-ul. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `ultra`

4. [Compozitorul MonoGame](2026-07-18-prism-monogame-compositor.md) — depinde
   de graful de compoziție. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

5. [Culoare, blending și stiluri](2026-07-18-prism-color-blend-and-styles.md) —
   depinde de markup și compozitor. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

6. [Catalogul de filtre](2026-07-18-prism-filter-catalog.md) — depinde de
   markup și compozitor; poate avansa în paralel cu stilurile. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `xhigh`

7. [Backdrop și integrarea hostului](2026-07-18-prism-backdrop-hosting.md) —
   depinde de graful de compoziție și compozitor. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

8. [Cache retained GPU](2026-07-18-prism-retained-pixel-cache.md) — depinde de
   graf, compozitor, catalogul vizual complet și backdrop. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `ultra`

9. [Integrare și hardening](2026-07-18-prism-integration-and-hardening.md) —
   depinde de toate planurile precedente. GATA

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

## Gate-uri globale

- [x] Înaintea primului cod Prism, armonizează cele două documente-sursă:
  cache-ul retained cross-frame este obligatoriu, iar extensiile publice terțe
  sunt explicit amânate, fără schimbarea gramaticii aprobate.
- [x] Nu începe un plan dependent până când toate gate-urile planurilor sale
  prealabile sunt bifate și testele lor țintite sunt verzi.
- [x] Pentru fiecare schimbare C# sau de proiect, rulează imediat:
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.
- [x] Pentru fiecare API public nou sau modificat, actualizează în aceeași
  etapă `docs-site/documentation/classes/` cu skill-ul
  `writing-api-documentation` și sincronizează
  `docs-site/documentation/manifest.json`.
- [x] Nicio etapă GPU nu pornește înainte ca testele modelului CPU și ale
  grafului backend-neutral să fie verzi; un screenshot nu înlocuiește un test
  semantic.
- [x] Niciun cache hit cross-frame nu este acceptat doar pe baza unui hash;
  outputul cache-on trebuie să fie identic cu cache-off, iar dependency stamp-ul
  trebuie să includă fiecare input pixel-affecting.
- [x] Toate verificările vizuale folosesc API-ul existent de captură
  `IWindowPlatform.RenderPng`/automatizarea Presentation, nu screenshot-uri
  făcute manual.
- [x] Orice workaround în `CernealaPresentation` pentru o problemă a
  frameworkului blochează gate-ul; invariantul se repară în stratul care îl
  deține.
- [x] La finalul fiecărui plan rulează `git diff --check` și verifică explicit
  să nu existe fișiere generate, binare shader sau schimbări fără proprietar.

## Stop conditions

Implementarea se oprește și decizia se întoarce în documentele de design dacă:

- contractul cerut nu poate fi exprimat prin cele opt directive aprobate;
- o optimizare schimbă ordinea Photoshop, alpha-ul sau rezultatul măștilor;
- un API public terț ar fi necesar doar pentru a evita o extensie internă
  simplă;
- un buget numeric trebuie ghicit înainte să existe măsurători;
- hostul nu poate furniza backdrop-ul fără sincronizare GPU sau readback CPU.

## Definiția de gata

- [x] Toate cele nouă planuri sunt complet bifate, în ordinea dependențelor.
- [x] `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj` și
  `dotnet test .\Cerneala.slnx` sunt verzi.
- [x] Matricea generată catalog → parser → runtime → kernel → test → documentație
  nu conține intrări lipsă.
- [x] Testele de lifecycle, memorie, device reset, performanță și conformance
  vizuală sunt verzi pe configurația suportată WindowsDX.
- [x] Un Prism static produce hit retained fără recapturare ori effect passes,
  iar orice input pixel-affecting schimbat produce miss și output corect.
- [x] RoslynIndexer `doctor` și reindexarea completă sunt verzi, documentația
  publică este sincronizată, iar `git diff --check` nu raportează probleme.
