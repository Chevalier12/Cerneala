# Prism — cache retained GPU cross-frame

**Status:** finalizat

## Scop

Păstrează și reutilizează între frame-uri capturi, rezultate intermediare și
compoziții finale Prism atunci când fiecare input pixel-affecting este neschimbat.
Cache-ul este obligatoriu pentru arhitectura retained, backend-owned, GPU-only și
limitat explicit ca memorie.

**Dependențe:** `2026-07-18-prism-retained-composition-graph.md`,
`2026-07-18-prism-monogame-compositor.md`,
`2026-07-18-prism-color-blend-and-styles.md`,
`2026-07-18-prism-filter-catalog.md` și
`2026-07-18-prism-backdrop-hosting.md`.

## Etapa 0 — contracte RED și baseline cache-off

- [x] Adaugă teste RED în `tests/Cerneala.Tests/Drawing/MonoGame/Prism/Cache/`
  care compară fiecare rezultat cache-on cu o execuție fresh cache-off.
- [x] Fixează prin teste contractul: al doilea frame static identic produce hit
  final, sare capture și effect passes acoperite, dar desenează rezultatul cached.
- [x] Adaugă matrice RED de miss pentru conținut, structură, parametri, Motion,
  resurse, lower UI, backdrop `ContentVersion`, bounds, pixel scale, profil,
  format, capability set și shader package schimbate.
- [x] Adaugă teste RED pentru hit intermediar, două controale cu aceeași
  definiție, hash collision, buget depășit, intrare pin-uită, excepție, detach,
  Hidden/Collapsed, replacement și device reset.
- [x] Înregistrează baseline-ul fără cache: captures, passes, CPU submit, GPU
  time, alocări și peak transient surfaces pentru scenele standard.

### Gate etapa 0

- [x] Testele eșuează exclusiv fiindcă versionarea/cache-ul retained lipsesc, iar
  oracle-ul cache-off este determinist și verificat de golden-urile existente.

## Etapa 1 — versionare retained incrementală

- [x] Adaugă cea mai mică versiune vizuală agregată în stratul retained care
  deține invalidarea randării; nu calcula cheia traversând subtree-ul per frame.
- [x] Incrementează versiunea la orice schimbare render-affecting: proprietate,
  Motion, comandă locală, copil, imagine/text/resource content ori Presence.
- [x] Propagă generația minim spre scope-ul Prism părinte fără invalidare de
  measure/arrange sau hit testing.
- [x] Păstrează separat `PrismStructuralVersion` și `PrismValueVersion`; o
  scriere cu aceeași valoare este no-op și nu produce miss.
- [x] Cere versiuni monotone pentru imagini, măști, LUT-uri, patterns și resurse
  auxiliare; o resursă fără versiune stabilă face nodul necacheable.
- [x] Pentru backdrop, compune `ContentVersion` cu versiunile nodurilor UI
  inferioare în paint order, fără referințe la owner.

### Gate etapa 1

- [x] Fiecare mutație pixel-affecting testată schimbă exact stamp-ul necesar,
  mutațiile de layout/input fără efect vizual nu îl schimbă inutil, iar analiza
  rămâne o singură trecere.

## Etapa 2 — chei complete și cacheability

- [x] Adaugă backend-neutral `PrismDependencyStamp` și
  `PrismRetainedCacheKey`, formate numai din identificatori/versiuni/value
  fingerprints imuabile.
- [x] Include în cheie: hash structural verificabil, stable node id, versiuni
  Prism, owner/source/resource identities plus versions, lower UI, raster bounds,
  pixel scale, transform relevant, color profiles, format, sampling, capability
  set și shader package version.
- [x] Fă `PrismCacheOwnerToken` unic și nerefolosit pe durata backendului;
  versiunea numerică a conținutului nu este niciodată comparată fără identitate.
- [x] Nu accepta un hit numai pentru că hash-ul corespunde; verifică identitatea
  structurală și întregul dependency stamp.
- [x] Generează din catalog determinismul și dependențele fiecărei operații;
  timpul implicit, randomness fără seed ori resursa neversionată interzic cache-ul.
- [x] Permite cache pentru capture, noduri intermediare scumpe și rezultat final;
  optimizerul decide eligibilitatea, executorul nu ghicește.
- [x] Adaugă teste unitare pentru egalitate, fingerprint determinist, coliziuni,
  chei aproape identice și lipsa stringurilor/referințelor UI.

### Gate etapa 2

- [x] Matricea catalogului raportează cacheability pentru fiecare operație, iar
  nicio dependență pixel-affecting cunoscută nu lipsește din key/stamp.

## Etapa 3 — owner-ul suprafețelor retained

- [x] Adaugă `Drawing/MonoGame/Prism/Surfaces/PrismRetainedSurfaceCache`,
  separat de `PrismSurfacePool`; numai cache-ul retained păstrează conținut.
- [x] Definește promovarea atomică transient → retained după succes și revenirea
  retained → pool/dispose la eviction, fără dublu ownership.
- [x] Ține intrările pin-uite cât timp sunt folosite în draw și interzice
  eviction/dispose înaintea eliberării ultimului lease.
- [x] Implementează LRU determinist sub byte budget și maximum-entry budget;
  eviction-ul preferă intrările nepin-uite cele mai vechi.
- [x] Folosește un accountant comun: hard cap pentru toate suprafețele Prism și
  soft cap pentru retained; evacuează retained înainte să refuzi memoria
  transient necesară unui frame corect.
- [x] Păstrează cache-ul pe UI/render thread-ul backendului MonoGame; nu adăuga
  lock-uri, task graph, async compute sau abstracție generică de GPU fences.
- [x] Gestionează alocarea eșuată prin `PrismFallbackPolicy`: randare fresh/bypass
  sigur, diagnostic și zero intrare parțială.

### Gate etapa 3

- [x] Ownership-ul fiecărei suprafețe este unic și observabil, byte accounting-ul
  este exact, iar excepțiile lasă zero lease-uri orfane.

## Etapa 4 — lookup, pruning și promotion

- [x] Verifică mai întâi cheia rezultatului final; la hit, sari comenzile
  interioare ale scope-ului și toate passes acoperite, apoi compune suprafața.
- [x] La miss final, caută numai nodurile intermediare marcate cacheable și
  prune-uiește subgraful acoperit de hit fără a schimba ordinea Photoshop.
- [x] La miss complet, execută graful normal și promovează numai rezultate
  finalizate cu succes și utile conform planului optimizerului.
- [x] Nu reține textura backdrop furnizată de host; reține numai rezultate
  procesate deținute de Cerneala și validate prin `ContentVersion`.
- [x] Pentru nodurile bazate pe control, include owner tokenul și interzice
  sharing cross-owner; permite sharing numai pentru inputuri externe cu aceeași
  identitate/versionare completă. Numele markup nu participă la cheie.
- [x] Adaugă teste diferențiale hit final/intermediar/miss pentru alpha, masks,
  clipping, groups, blend modes, styles, filters, nested Prism și backdrop.

### Gate etapa 4

- [x] Outputul cache-on este identic cu cache-off în toleranța declarată, iar
  diagnostics confirmă că passes/captures sunt sărite exact cât acoperă hit-ul.

## Etapa 5 — invalidare și lifecycle

- [x] Invalidează intrările afectate la composition/resource replacement,
  viewport/pixel-scale/output-profile/shader-package change și device reset/loss.
- [x] La detach/dispose, invalidează generația ownerului și elimină orice index
  auxiliar fără a scana întregul cache ori a păstra ownerul viu.
- [x] Transportează invalidarea printr-un `PrismCacheOwnerToken` numeric și o
  coadă backend-neutrală consumată la submit; UI-ul nu eliberează direct GPU.
- [x] La `Hidden`/`Collapsed`, fă zero lookup și zero promotion; marchează
  intrările asociate imediat evictable și anulează Motion prin lifecycle-ul comun.
- [x] La reattach/unhide, permite hit numai dacă dependency stamp-ul complet
  rămâne valid; altfel recalculează fără pixeli vechi.
- [x] Testează 10.000 attach/detach, navigation, hide/unhide, replacement,
  resize și device reset cu `WeakReference`, bytes și lease counters.
- [x] Verifică faptul că nicio intrare nu conține `UIElement`, binding, delegate,
  Motion handle sau backdrop source lease.

### Gate etapa 5

- [x] După drain/GC/reset, cache-ul respectă bugetul și nu păstrează elemente,
  instanțe, resurse expirate ori suprafețe inaccesibile.

## Etapa 6 — diagnostics, bugete și performanță

- [x] Expune counters pentru final/intermediate hit, miss reason, promotion,
  eviction reason, bytes/entries, pinned entries, saved captures și saved passes.
- [x] Include dependency-stamp diff în diagnostics de dezvoltare fără a serializa
  resurse GPU sau a aloca per frame când diagnostics sunt oprite.
- [x] Benchmarkuiește static control, static backdrop, game backdrop animat,
  parametru Motion, resursă schimbată, multe instanțe comune și buget mic.
- [x] Stabilește valorile implicite ale byte/entry budgets numai după măsurători
  pe hardware-ul de referință și păstrează opțiunile configurabile.
- [x] Confirmă `0 B` managed după warmup pe hit static și demonstrează câștigul
  net față de lookup/fingerprint cost.
- [x] Adaugă un mod intern cache-off pentru conformance și diagnostic, nu ca
  dialect markup sau proprietate per layer.

### Gate etapa 6

- [x] Cache-ul are câștig măsurat în scenele stabile, cost bounded în scenele
  dinamice și niciun prag numeric inventat fără baseline.

## Etapa 7 — API docs și verificare

- [x] Actualizează cu skill-ul `writing-api-documentation`
  `PrismRendererOptions` și orice API public de buget/diagnostics; sincronizează
  `docs-site/documentation/manifest.json`.
- [x] Actualizează TDD-ul/proposal-ul numai cu detalii confirmate prin
  implementare și benchmark, fără a modifica gramatica.
- [x] Rulează reindexarea obligatorie după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "PrismRetainedCache|PrismDependencyStamp|PrismBackdrop"`.
- [x] Rulează toate golden-urile Prism în mod cache-on și cache-off, apoi
  `dotnet test .\Cerneala.slnx`.
- [x] Rulează stress/benchmarkurile cache și `git diff --check`.

## Definiția de gata

- [x] Un Prism static reutilizează efectiv rezultatul GPU între frame-uri și
  sare munca acoperită, nu doar structura CPU.
- [x] Orice schimbare pixel-affecting invalidează corect; outputul cache-on este
  identic cu cache-off, fără pixeli vechi sau sharing între controale incompatibile.
- [x] Cache-ul este GPU-only, budgeted, leak-free, sigur la lifecycle/device
  reset și complet observabil prin diagnostics.
