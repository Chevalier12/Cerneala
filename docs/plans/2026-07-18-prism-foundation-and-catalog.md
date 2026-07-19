# Prism — fundație și catalog unic

## Scop

Construiește modelul backend-neutral, catalogul machine-readable și contractele
de versiune pe care se bazează toate celelalte planuri. Etapa nu desenează pe
GPU și nu modifică parserul CUI.

**Dependențe:** niciuna.

## Etapa 0 — decizii și baseline

- [x] Corectează contradicțiile dintre
  `docs/prism-technical-design.md` și
  `docs/prism-markup-syntax-proposal.md`: prima implementare include cache
  retained cross-frame, dar nu are înregistrare publică de filtre sau shader-e
  încărcate după nume arbitrar; păstrează sintaxa și comportamentul Photoshop.
- [x] Documentează explicit ordinea de randare, sursa implicită, regula de
  frunză pentru layer, `@group`, masca, `ClipToBelow`, `PassThrough`,
  `Visible`, `Fill`, `Opacity`, `BlendIf`, culoarea implicită `LinearSrgb` și
  faptul că Prism nu influențează layout-ul ori inputul.
- [x] Inventariază API-urile publice pe care planurile ulterioare le vor atinge
  (`IDrawingBackend`, `IUiBackend`, `MonoGameUiHostOptions`) și salvează
  baseline-ul API pentru diff-ul final.
- [x] Adaugă teste RED în `tests/Cerneala.Tests/UI/Prism/` pentru definiții
  imuabile, ordine bottom-up, unicitatea numelor și validarea structurii
  layer/group/backdrop, fără să adaugi încă execuție GPU.

### Gate etapa 0

- [x] Cele două documente de design nu se mai contrazic, iar testele RED
  eșuează exclusiv fiindcă modelul Prism lipsește.

## Etapa 1 — catalogul DRY

- [x] Creează o singură sursă machine-readable în
  `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` și schema ei; catalogul
  include identificatorii stabili, tipurile, proprietățile, valorile implicite,
  domeniile, unitățile, capabilitățile, determinismul și cacheability pentru
  filtre, stiluri, blend modes, profiluri de culoare și sampling.
- [x] Include în catalog toate familiile Photoshop aprobate în proposal, fără
  a copia aceleași liste în C#, shader-e și documentație.
- [x] Adaugă un generator/validator determinist care produce artefactele tipate
  necesare SourceGen, runtime și backend; consumatorii folosesc același fișier
  fizic, nu copii sincronizate manual.
- [x] Generează o matrice de acoperire catalog → runtime → kernel → test →
  documentație care poate eșua la build când o intrare nu are proprietar.
- [x] Testează identificatori duplicați, valori implicite incompatibile,
  intervale invalide, proprietăți necunoscute și output nedeterminist.

### Gate etapa 1

- [x] O singură comandă de build regenerează/verifică toate artefactele, iar o
  intrare intenționat incompletă face testul catalogului să pice cu diagnostic
  precis.

## Etapa 2 — modelul de definiții

- [x] Adaugă în `UI/Prism/Definitions/` tipurile imuabile
  `PrismCompositionDefinition`, `PrismNodeDefinition`,
  `PrismLayerDefinition`, `PrismGroupDefinition`,
  `PrismBackdropDefinition`, definițiile de mask/filter/style și cheile tipate
  de parametri generate din catalog.
- [x] Modelează copiii layer-ului drept colecții separate de filtre, stiluri și
  cel mult o mască; nu permite unui layer copii layer/group.
- [x] Modelează ordinea declarată o singură dată și oferă enumerare bottom-up
  fără a muta ori duplica nodurile.
- [x] Validează nume opționale unice în scope-ul compoziției pentru accesul
  Motion și interzice folosirea numelor ca surse arbitrare.
- [x] Păstrează `@backdrop` într-un plan separat logic, dar include în model
  invariantul „maxim unul, ultimul copil direct”.
- [x] Mută politica de degradare într-un singur
  `Drawing/Prism/Catalog/PrismFallbackPolicy`; definițiile nu cunosc MonoGame.
- [x] Fă verzi testele RED și adaugă teste de egalitate structurală, snapshot
  determinist și serializare diagnostică.

### Gate etapa 2

- [x] Modelul nu referă `Microsoft.Xna.Framework.Graphics`, nu conține stare
  mutabilă per control și exprimă toate invariantele structurale fără `object`
  sau lookup-uri string în hot path.

## Etapa 3 — instanța per element

- [x] Adaugă în `UI/Prism/Runtime/` `PrismInstance`, depozitul dens și tipat de
  parametri, `PrismStructuralVersion`, `PrismValueVersion` și starea
  layer/group/backdrop adresabilă după chei generate.
- [x] Separă definiția partajată de valorile instanței; nicio resursă GPU nu
  este deținută de `UIElement` sau `PrismInstance`.
- [x] Implementează `Visible`, `Opacity`, `Fill`, blend mode, advanced blending,
  `BlendIf`, `ClipToBelow`, mask/style/filter values și profilul de culoare ca
  proprietăți tipate, cu defaults exclusiv din catalog.
- [x] Incrementează versiunea structurală doar când se schimbă topologia și
  versiunea valorilor doar când se schimbă datele; scrierile identice sunt
  no-op.
- [x] Adaugă teste pentru izolare între două controale ce folosesc aceeași
  compoziție, replacement, reset la defaults și lipsa alocărilor după warmup
  pentru actualizări tipate repetate.

### Gate etapa 3

- [x] Două instanțe partajează definiția fără a partaja stare, iar benchmarkul
  de actualizare nu face lookup string și nu creează obiecte per frame.

## Etapa 4 — documentare și verificare

- [x] Dacă modelul expune tipuri publice, folosește skill-ul
  `writing-api-documentation` pentru paginile din
  `docs-site/documentation/classes/` și sincronizează manifestul; nu adăuga
  documentație API în `docs/documentation/`.
- [x] Rulează reindexarea RoslynIndexer după fiecare lot C#/proiect și, la
  final, `doctor`.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter Prism` și
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Rulează `git diff --check` și compară diff-ul API cu baseline-ul etapei 0.

## Definiția de gata

- [x] Catalogul este unica sursă de adevăr și validează toate intrările
  aprobate.
- [x] Definițiile sunt imuabile, instanțele sunt izolate și niciun tip din
  fundație nu depinde de backendul GPU.
- [x] Testele țintite, documentația publică și gate-urile etapelor sunt verzi.
