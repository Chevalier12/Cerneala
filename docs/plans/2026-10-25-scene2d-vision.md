# Plan: `ConeCollider2D` și `Vision` pe `Sprite2D` și `Scene2D`

> Data: 2026-10-25
> Status: finalizat
> Index: [scene2d-autonomie-index](2026-10-25-scene2d-autonomie-index.md), planul 2
> Depinde de: [scene2d-contacte](2026-10-25-scene2d-contacte.md), finalizat: `Collider2D.Entered`/`Exited` și livrarea contactelor
> Blocat de: nimic. D4, D16, D17 și D18 au fost decise pe 2026-10-25.

## Rezumat

Un obiect poate avea o vedere: un collider în formă de con, separat de corp, care înseamnă „ce vede”. Implicit e `null`. Conul se poate modifica: cât de departe vede, cât de larg vede și încotro privește.

```csharp
bee.Vision = new ConeCollider2D
{
    Radius = 150,      // vede 150 px
    Angle = 90,        // un con de 90°
    Direction = 0,     // privește spre dreapta
};
bee.Vision.Entered += (_, e) => seen.Add(e.Other);
bee.Vision.Exited += (_, e) => seen.Remove(e.Other);

CollisionHit2D[] around = scene.CollisionWorld.Overlap(bee.Vision);   // căutarea, fără API nou
```

Formele conului:

| Pentru | Valori |
| --- | --- |
| Paznic: îngust, dar departe | `Radius = 300`, `Angle = 30` |
| Albină: larg, dar aproape | `Radius = 150`, `Angle = 120` |
| Bufniță: vede în jur | `Radius = 200`, `Angle = 360` |
| Pac-Man: tot, în afară de spate | `Radius = 100`, `Angle = 300` |

Un dragon făcut din mai multe sprite-uri primește `Vision` pe `Scene2D`-ul lui. Dacă și capul (un `Sprite2D` copil) are `Vision`, fiecare anunță separat.

Decizia utilizatorului (2026-10-25): `Vision` este **numai** `ConeCollider2D`. Varianta „orice tip de collider” este înlocuită.

## Baseline (verificat pe master `55c4ea20`)

- Formele de coliziune de azi sunt `Box`, `Circle`, `Polygon` și `Segment` (`UI/Controls/ColliderGeometry2D.cs:6-12`). Nu există con.
- Testul exact folosește GJK (`UI/Controls/CollisionNarrowPhase2D.cs:9,595-661`, cu EPA la `:819`). Fiecare formă dă doar funcția de suport, într-un `switch` pe `ColliderShapeKind2D` (`:902-926`). Unele perechi au teste rapide separate: `TryFastContact`, `TryPolygonContact`, `TryCirclePolygonContact` (`:332-543`).
- GJK merge numai pe forme convexe. Un con de cel mult 180° este convex. Un con mai larg nu este.
- `PolygonCollider2D` respinge deja poligoanele concave, cu eroarea SCN2D008 (`UI/Controls/PolygonCollider2D.cs:51-97`).
- `Flip` nu schimbă azi geometria de coliziune: comentariul de la `UI/Controls/Sprite2D.cs:69` spune „crop, Origin and Flip change presentation, not collision geometry”.
- `Rotation` intră în transformul local în radiani: `Matrix3x2.CreateRotation(Rotation)` (`Sprite2D.cs:810`).
- `Sprite2D.Collider` este un singur slot logic (`UI/Controls/Sprite2D.cs:17,53,56`, `UI/Controls/ColliderSlot2D.cs`):
  - `Replace()` scoate colliderul vechi cu `RemoveOwned` și pune colliderul nou cu `InsertOwned` și `AttachSurface`;
  - colliderul este în `LogicalChildren`, nu în `VisualChildren`.
- `Collider2D.ValidateParentChange` (`UI/Controls/Collider2D.cs:13-24`) aruncă excepție dacă:
  - rolul nu este Logical;
  - părintele nu trece de `AllowsCollisionParent` (azi `parent is Sprite2D`, `:26`);
  - schimbarea nu este făcută de proprietar (`ownerManaged`).

  `Scene2D` nu poate deține azi un collider.
- `CollisionWorld2D.EnumerateColliders` (`UI/Controls/CollisionWorld2D.cs:489-505`) parcurge `LogicalChildren` ale nodurilor `SceneNode2D` și ia orice `Collider2D` găsit. Un al doilea slot ar fi deci găsit automat.
- `CollisionHit2D.Entity` este cel mai apropiat strămoș `SceneNode2D` care nu este collider (`CollisionWorld2D.cs:535-548`).
- Picking-ul folosește aceeași lume: `SceneHitTest2D.cs:19` cheamă `CollisionWorld.CollectPointHits`.
- Overlay-ul de debug desenează colliderele prin `CollectDebugGeometry` (`CollisionWorld2D.cs:301`, `Scene2DDebugOverlay.cs:124`).
- Markup: `[ContentProperty(nameof(Collider))]` pe `Sprite2D` (`Sprite2D.cs:13`) și `[ContentProperty(nameof(Children))]` pe `Scene2D` (`Scene2D.cs:10`).

## Obiective

### `ConeCollider2D`

- Clasă publică, sealed, `ConeCollider2D : Collider2D`, lângă `CircleCollider2D`.
- **Proprietățile** (D16, decis):
  - `Radius`: px, finit și > 0, implicit 100;
  - `Angle`: cât de larg vede, în grade, în intervalul (0, 360], implicit 90. 360 înseamnă cerc întreg;
  - `Direction`: unde privește, în grade, 0 = dreapta (+X), crește în sensul acelor de ceasornic pe ecran, implicit 0. Orice valoare finită se normalizează;
  - `OffsetX`/`OffsetY`, moștenite: mută vârful conului față de proprietar.
- **Forma:** vârful este în originea colliderului. Marginea este un arc de cerc. Conul este simetric față de `Direction`.
- **Transformul:**
  - conul urmează `Rotation` și poziția proprietarului;
  - `Flip` oglindește `Direction` (D18, decis). Excepția se aplică numai conului.
- **Testul exact:**
  - un con de cel mult 180° este o formă convexă nouă în GJK, cu funcție de suport proprie;
  - un con mai larg de 180° se testează ca reuniunea a două conuri convexe;
  - 360° se tratează exact ca un cerc.
- **Bounds-urile** pentru `SparseCollisionGrid2D` sunt strânse în jurul arcului și al vârfului, nu ale cercului întreg.
- **Unde poate sta** (D17, decis): numai în `Vision`. Pus în `Sprite2D.Collider` aruncă excepție cu mesaj clar. De aceea nu are nevoie de `Raycast` sau `ShapeCast`, pentru că un senzor nu este ținta lor (D4). `Overlap`, `Intersects` și contactele funcționează.

### `Vision`

- `public ConeCollider2D? Vision` pe `Sprite2D` și pe `Scene2D`. Implicit `null`.
- Același con nu poate fi `Vision` pe două obiecte deodată.
- `Vision` se mișcă împreună cu proprietarul. Pe `Scene2D`, folosește transformul grupului: `X`, `Y` și `TransformOrigin`.
- **Comportamentul de senzor** (D4, decis 2026-10-25). `Vision` este doar senzor:

  | Ce | `Vision` | Exemplu |
  | --- | --- | --- |
  | Blocare | nu | Vulpea trece prin conul albinei fără să se oprească. |
  | Click | nu | Un click în con, dar în afara corpului, nu nimerește albina. |
  | Atingere | da, detectată | Vulpea intră în con: contactul există, dar nu oprește nimic. |

  - **Atingerea o află numai proprietarul conului**, prin `Vision.Entered`/`Exited` și prin `Overlap(vision)`.
  - Corpul atins nu primește nimic: `fox.Collider.Entered` nu vine pentru un con, iar `Other` nu este niciodată un `Vision`.
  - `MoveAndCollide` (inclusiv `TriggerHits`), `Raycast`, `Overlap` și picking-ul altor collidere nu văd conul.
  - Două `Vision` nu se văd între ele.
  - `IsTrigger` nu contează pe un `Vision`.
- `Vision`-urile imbricate (un `Scene2D` cu `Vision` și un copil cu `Vision`) sunt independente și anunță fiecare.
- Overlay-ul de debug desenează conul (arc și două raze), distinct de corp.
- Markup: `<Sprite2D.Vision><ConeCollider2D Radius="150" Angle="90" Direction="0" /></Sprite2D.Vision>` funcționează. La fel pentru `Scene2D.Vision`.

## Non-obiective

- `ConeCollider2D` ca collider de corp, ținta lui `Raycast` sau formă mișcată cu `MoveAndCollide` (D17). Poate veni mai târziu, cu plan separat.
- `Vision` de alt tip decât con (decizie a utilizatorului).
- `Vision` pe `SceneNode2D`, `Collider2D` sau `TileMap2D` (decizie a utilizatorului).
- Linie de vedere blocată de ziduri. Utilizatorul o poate face cu `Raycast` în handler.
- Mai multe `Vision` pe același obiect.
- Rază internă (inel tăiat) sau vedere care se estompează cu distanța.
- Un API de căutare (`Search`).

## Arhitectura

- **Forma nouă:**
  - `ColliderShapeKind2D.Cone` și un factory `ColliderLocalShape2D.Cone(radius, startAngle, sweep)`, în radiani interni;
  - `ConeCollider2D.GetLocalShape()` transformă gradele publice și aplică `Flip` după D18.
- **Testul exact:** `CollisionNarrowPhase2D.Support` primește cazul con:
  - dacă direcția cade în interiorul arcului, punctul de suport este pe arc;
  - altfel, este punctul cel mai bun dintre vârf și cele două capete ale arcului.

  Conul > 180° este împărțit în două subconuri ≤ 180°, iar perechea se suprapune dacă oricare subcon se suprapune. Căile rapide existente nu se ating. Etapa 0 verifică că niciun `switch` pe `ColliderShapeKind2D` nu rămâne fără caz:
  - narrow phase;
  - bounds;
  - debug;
  - `CollectPointHits`;
  - tile-urile.
- **Slotul:** `ColliderSlot2D` se generalizează cu un rol intern (corp sau vedere). `Sprite2D` primește al doilea slot, iar `Scene2D` primește primul ei slot.
  - Slotul de corp refuză `ConeCollider2D` (D17).
  - Slotul de vedere este tipat `ConeCollider2D`.
- **Ownership:**
  - `AllowsCollisionParent` acceptă un `Scene2D` numai pentru colliderul din slotul de vedere;
  - `ValidateParentChange` păstrează regula „numai prin proprietar”;
  - `Scene2D.Children` continuă să refuze collidere.
- **Rolul în lume:** intrarea din `CollisionWorld2D` primește un flag intern de senzor, calculat din slot.
  - Toate filtrele publice (`PassesQueryFilter`, `MoveAndCollide`, `Raycast`, `Overlap`, `CollectPointHits`) sar peste senzori.
  - Excepția: colliderul interogat este chiar senzorul (`Overlap(vision)`).
  - Contactele din planul 1 sar peste perechile senzor–senzor. O pereche corp–senzor se anunță într-un singur sens: numai pe senzor, cu `Other` = corpul. Corpul nu primește evenimentul (D4).
- **Geometria pe `Scene2D`:** se verifică în Etapa 0 că `SceneGeometry2D` calculează transformul unui collider al cărui părinte direct este un `Scene2D`. Azi părintele este mereu un `Sprite2D`.
- **Randarea și bounds-urile grupului:** `Scene2D` nu desenează și nu cullează după `Vision`. Bounds-urile vizibile și de hit-test ale grupului ignoră slotul de vedere.

## Fișiere estimate

- `UI/Controls/ConeCollider2D.cs`, nou.
- `UI/Controls/ColliderGeometry2D.cs`, `UI/Controls/CollisionNarrowPhase2D.cs` și orice alt fișier cu `switch` pe `ColliderShapeKind2D`, găsit în Etapa 0.
- `UI/Controls/ColliderSlot2D.cs`, `UI/Controls/Collider2D.cs`, `UI/Controls/Sprite2D.cs`, `UI/Controls/Scene2D.cs`.
- `UI/Controls/CollisionWorld2D.cs` și `CollisionWorld2D.Contacts.cs` (din planul 1), plus `UI/Controls/SceneGeometry2D.cs` dacă Etapa 0 cere.
- `UI/Controls/Scene2DDebugOverlay.cs`.
- SourceGen și Language, dacă `ConeCollider2D` sau proprietatea `Vision` nu sunt acceptate automat în markup. Se verifică în Etapa 0 cu testele din `tests/Cerneala.Tests.SourceGen` și `tests/Cerneala.Tests.Language`.
- Teste noi:
  - `tests/Cerneala.Tests/Controls/ConeCollider2DTests.cs` (forma și testul exact);
  - `tests/Cerneala.Tests/Controls/SceneVisionTests.cs`.

  Se extind `ColliderOwnershipTests` și `SceneDebugOverlayTests`.
- Docs:
  - pagina nouă `Cerneala.UI.Controls.ConeCollider2D.md` și intrarea ei în `docs-site/documentation/manifest.json`;
  - `Cerneala.UI.Controls.Sprite2D.md`, `Cerneala.UI.Controls.Scene2D.md`, `Cerneala.UI.Controls.Collider2D.md` (regula de ownership);
  - `docs/architecture/scene2d.md`.

## Etapa 0 — contract și teste RED

- [x] Se găsesc toate `switch`-urile și ramurile pe `ColliderShapeKind2D` (narrow phase, bounds, debug, picking, tile-uri, SceneItems2D). Se notează în notele de lucru ce primește fiecare pentru con. (Tabelul „Ramurile pe `ColliderShapeKind2D`”.)
- [x] Se caracterizează (GREEN) `ColliderOwnershipTests`: un collider pus în `Scene2D.Children` aruncă excepție, iar mutarea unui collider între două sprite-uri trece prin proprietar. (Testele existau deja: `EveryColliderKindRejectsSceneAndGenericTreeOwnership`, `ColliderOwnershipTests.cs:32-54`, și `ColliderReplacementIsAtomicAndGenericRemovalCannotCorruptOwnership`, `:122-145`. Pe codul neschimbat, împreună cu testul de explorare: 21/21.)
- [x] Se face tabelul apelanților pentru `AllowsCollisionParent`, `ValidateParentChange`, `EnumerateColliders` și `ColliderSlot2D`. Fiecare apelant primește o migrare sau un contract neschimbat. (Tabelul „Apelanții ownership-ului și ai slotului”.)
- [x] Se verifică geometria unui collider cu părinte `Scene2D` în `SceneGeometry2D`, cu un test de explorare. (Vezi „Geometria sub un `Scene2D`”.)
- [x] Teste RED în `ConeCollider2DTests`:
  - un punct, un cerc, un box și un poligon în fața conului se suprapun;
  - aceleași forme în spatele conului sau în afara razei nu se suprapun;
  - o formă care atinge doar o rază laterală se suprapune după regula D3;
  - conurile de 30°, 90°, 180°, 270°, 300° și 360° dau rezultatele așteptate pentru un set fix de puncte;
  - 360° dă același rezultat ca un `CircleCollider2D` cu aceeași rază;
  - `Direction`, `Rotation` și `Flip` (D18) rotesc sau oglindesc conul;
  - bounds-urile unui con îngust sunt mai mici decât bounds-urile cercului întreg;
  - valorile invalide (`Radius` ≤ 0, `Angle` ≤ 0 sau > 360, valori nefinite) aruncă excepție;
  - `ConeCollider2D` pus în `Sprite2D.Collider` aruncă excepție (D17).
- [x] Teste RED în `SceneVisionTests`: (plus testele pentru deciziile V1 și V2)
  - `Vision` pe `Sprite2D` și pe `Scene2D` primește `Entered`/`Exited` când un corp intră sau iese din con;
  - un corp în spatele albinei, dar în rază, nu dă `Entered`;
  - corpul care intră în con nu primește `Entered`, chiar dacă are handler: `fox.Collider.Entered` nu vine pentru `bee.Vision`;
  - `Vision` se mișcă și se rotește împreună cu proprietarul;
  - `MoveAndCollide` al altui sprite trece prin `Vision` fără coliziune și fără `TriggerHits`;
  - `Raycast` și `Overlap` ale altora nu întorc `Vision`;
  - un click în `Vision`, dar în afara corpului, nu lovește obiectul;
  - două `Vision` suprapuse nu dau evenimente;
  - `Overlap(vision)` întoarce numai corpurile din con;
  - un `Vision` imbricat pe copil anunță separat de `Vision`-ul grupului;
  - același con nu poate fi `Vision` pe două obiecte;
  - `Vision = null` dă `Exited` pentru contactele active.
- [x] Test RED de markup pentru `<Sprite2D.Vision><ConeCollider2D … /></Sprite2D.Vision>` și `<Scene2D.Vision>`, în proiectul SourceGen. (În `UiMarkupGeneratorVisionTests.cs`, plus decizia V4. Aceleași cazuri și completion-ul, în `ColliderOwnershipLanguageTests`.)

### Gate etapa 0

- [x] RED-urile pică pentru că lipsesc `ConeCollider2D` și `Vision`, nu din cauza fixture-ului. (Vezi „Dovada RED”.)
- [x] Testele de caracterizare sunt GREEN. (21/21 pe codul neschimbat, înainte să existe fișierele RED.)
- [x] Comanda: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~ConeCollider2D|FullyQualifiedName~SceneVision|FullyQualifiedName~ColliderOwnership"`. (Nu compilează: numai 3 erori `CS0246` pentru `ConeCollider2D`.)

## Etapa 1 — forma de con

- [x] Se adaugă `ConeCollider2D`, `ColliderShapeKind2D.Cone`, factory-ul de formă, bounds-urile și funcția de suport GJK. Conurile > 180° se împart în două subconuri. (Vezi „Forma de con (Etapa 1)”.)
- [x] Fiecare `switch` găsit în Etapa 0 primește cazul con. (`Support`, `TryFastContact`, `TryContact`, `GetCenter`, `Raycast`, `ShapeCast`, `ContainsPoint`, `TryGetColliderBounds`, `DrawCollider`.)

### Gate etapa 1

- [x] Testele din `ConeCollider2DTests` sunt GREEN, în afară de cel cu slotul de corp, care așteaptă Etapa 2. (259/261. Pică numai `ASpriteBodySlotRejectsAConeWithAMessageThatNamesVision` și `ARejectedConeLeavesTheExistingBodyInPlace`: nu se aruncă încă nicio excepție.)
- [x] Testele de coliziune existente rămân GREEN: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~Collision|FullyQualifiedName~Collider|FullyQualifiedName~ContinuousImpact|FullyQualifiedName~InitialContactMovement"`. (Cu `SceneDebugOverlay` adăugat: 517/519, iar cele 2 care pică sunt testele de mai sus. Language `ColliderOwnershipLanguage`: 21 de teste existente trec, cele 9 teste `Vision` pică până la Etapa 4. Suita completă nu a fost rulată.)

## Etapa 2 — slotul de vedere și ownership

- [x] Se generalizează `ColliderSlot2D` cu rolul de vedere. Se adaugă `Vision` (tipat `ConeCollider2D?`) pe `Sprite2D` și pe `Scene2D`. Slotul de corp refuză conul. (Vezi „Slotul de vedere (Etapa 2)”.)
- [x] Se extinde `AllowsCollisionParent` pentru slotul de vedere al unui `Scene2D`. (`ConeCollider2D.AllowsCollisionParent`: `Sprite2D` sau `Scene2D`.)
- [x] Bounds-urile și culling-ul `Scene2D` ignoră slotul de vedere. (`SceneChildBoundsIndex2D` sare peste collidere. Testele noi, RED întâi: `AVisionOnAGroupDoesNotChangeWhichChildren…` și `ALargeVisionOnAGroupAddsNoVisibleBoundsAndNoInputCandidate`.)

### Gate etapa 2

- [x] Testele de ownership, mișcare și geometrie din Etapa 0 sunt GREEN. (`ConeCollider2DTests` 261/261, `ColliderOwnershipTests` 18/18. Din `SceneVisionTests`, toate testele de ownership, slot, mișcare, rotație, transform de grup și `Exited` la scoatere trec. Cinci cazuri din `TheFlipOfTheSpriteMirrors…` pică numai pentru că albina își vede propriul corp, adică regula V1 din Etapa 3: la `SceneVisionTests.cs:187`, `Entered` are 2 box-uri, fox-ul și corpul albinei. Geometria `Flip` e arătată de `FlipMirrorsTheDirectionButNotTheOffsetOfTheVision`, care trece. Gate-ul 3 cere toate `SceneVisionTests` GREEN.)
- [x] `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~Collider|FullyQualifiedName~Collision|FullyQualifiedName~Scene2D|FullyQualifiedName~Sprite2D"` este GREEN. (501/503. Cele 2 care pică sunt teste ale Etapei 3 din `SceneVisionTests`, prinse de filtru după numele metodei, care conține „Collider”: `AGroupVisionIgnoresEveryColliderInsideTheGroupSubtree` (V1) și `OverlapOfAnotherColliderDoesNotReturnAVision` (senzorul). Toate testele care existau înainte trec. Suita completă nu a fost rulată.)

## Etapa 3 — senzorul în lume, picking și contacte

- [x] Intrarea din lume primește flag-ul de senzor. Filtrele de query, mișcare, raycast și picking sar peste senzori. Contactele sar peste perechile senzor–senzor și anunță perechile corp–senzor numai pe senzor. (Plus V1 și V2. Vezi „Senzorul (Etapa 3)”.)
- [x] Overlay-ul de debug desenează conul distinct de corp. (Culoarea `VisionColor` (190, 120, 255) și eticheta „vision”. Testele noi din `SceneDebugOverlayTests` au fost RED întâi: conul ieșea în culoarea layer-ului, (129, 221, 135).)

### Gate etapa 3

- [x] Toate testele din `SceneVisionTests` și `ConeCollider2DTests` sunt GREEN. (`SceneVisionTests` 55/55, cu 3 teste noi; `ConeCollider2DTests` 261/261.)
- [x] `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~SceneVision|FullyQualifiedName~ConeCollider2D|FullyQualifiedName~ColliderContact|FullyQualifiedName~CollisionStageFourInput|FullyQualifiedName~SceneDebugOverlay"` este GREEN. (392/392. Regresie pentru codul atins: `Collider|Collision|Scene2D|Sprite2D|SceneHit|SceneSimulation|UiHost|Servo|WindowRuntime|SceneChildBoundsIndex|RenderSurface2DSceneFoundation`, 857/857. Suita completă nu a fost rulată.)

## Etapa 4 — markup, documentație și verificare

- [x] `<Sprite2D.Vision>` și `<Scene2D.Vision>` cu `ConeCollider2D` funcționează în markup. Testul SourceGen este GREEN: `dotnet test tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj -c Release --filter "FullyQualifiedName~Vision"`. (23/23. În editor, Language `FullyQualifiedName~Vision`: 35/35, cu decizia V4. Vezi „Markup (Etapa 4)”.)
- [x] Pagina nouă `ConeCollider2D`, cu tabelul de forme din Rezumat, și intrarea ei în manifest. Paginile `Sprite2D`, `Scene2D` și `Collider2D` descriu `Vision`. `docs/architecture/scene2d.md` descrie forma de con și rolul de senzor. (Secțiunea „### Vision cones” din `scene2d.md`; citările ei au fost verificate de un Reviewer și de mine. `ApiDocumentationManifestIsValidAndReferencesExistingFiles`: 1/1.)
- [x] Raportul spune că suita completă nu a fost rulată. (Raportul final și mesajul de commit o spun.)

## Ordinea recomandată

Etapa 0 → 1 → 2 → 3 → 4. Etapa 1 (forma) nu depinde de planul 1 și poate începe înaintea lui. Etapele 2 și 3 au nevoie de contactele din planul 1.

## Definiția de gata

- [x] Toate casetele de mai sus sunt bifate.
- [x] Exemplul din Rezumat compilează într-un test și trece. (`SceneVisionTests.TheSummaryExampleCompilesAndWorks`.)
- [x] O albină cu `Vision` vede numai în fața ei, nu blochează și nu „fură” click-urile din jurul ei. (`ABodyBehindTheBeeButWithinTheRadiusRaisesNoEntered`, `MoveAndCollideThroughAVisionDoesNotBlockAndReportsNoTriggerHit`, `AClickInsideTheVisionButOutsideTheBodyDoesNotHitTheBee` și `ServoClickOnTheOwnerOfAVisionHitsTheOwnerAndNotTheEmptyPlaceInFrontOfIt`.)

## Note de lucru

### Decizii noi ale utilizatorului (Etapa 0, 2026-10-25)

Planul nu le decidea. Utilizatorul a ales, pentru fiecare, varianta recomandată:

- **V1, propriul corp:** un `Vision` nu își vede proprietarul. `bee.Vision` ignoră `bee.Collider`: nu vine `Entered` cu `Other = bee.Collider`, iar `Overlap(bee.Vision)` nu îl întoarce. `dragon.Vision` (pe un `Scene2D`) ignoră toate colliderele din grupul dragonului. `head.Vision` (un `Sprite2D` din dragon) ignoră numai `head.Collider` și vede aripile dragonului.
- **V2, `MoveAndCollide(vision, …)`:** aruncă `InvalidOperationException`, cu un mesaj care spune că un `Vision` este senzor și nu se mută cu `MoveAndCollide`.
- **V3, `Direction`:** proprietatea păstrează valoarea scrisă. După `cone.Direction = 450`, `cone.Direction` citește 450. Numai geometria normalizează unghiul, deci conul privește în jos, ca la 90.
- **V4, conul în slotul de corp în markup:** `<Sprite2D><ConeCollider2D /></Sprite2D>` dă eroare la compilare, CERNEALAUI005, cu un mesaj care trimite la `<Sprite2D.Vision>`. Editorul nu mai propune `ConeCollider2D` în conținutul lui `Sprite2D`.

### Alte alegeri din Etapa 0

- Proprietățile conului sunt `float`, ca `CircleCollider2D.Radius` (`CircleCollider2D.cs:7-17`).
- `Flip` oglindește numai `Direction` (D18). `OffsetX`/`OffsetY` nu se oglindesc. Exemplu: un con cu `OffsetX = 40` pe un sprite întors orizontal are vârful tot la +40 și privește spre stânga (testul `SceneVisionTests.FlipMirrorsTheDirectionButNotTheOffsetOfTheVision`).
- `Flip` vine din proprietatea `Sprite2D.Flip`, nu din flip-ul compus cu cel al cadrului de animație (`Sprite2D.cs:766-769`).

### Geometria sub un `Scene2D` (Etapa 0)

- `SceneGeometry2D.GetLocalToSceneTransform` (`SceneGeometry2D.cs:265-277`) urcă prin orice strămoș `SceneNode2D`. Nu presupune că părintele colliderului e un `Sprite2D`. Restricția de azi stă numai în `Collider2D.AllowsCollisionParent` (`Collider2D.cs:32`).
- Test de explorare, temporar, pe codul neschimbat. Un collider de test, care acceptă un `Scene2D` ca părinte, a fost pus sub un grup cu `InsertOwned`. El are aceeași origine în scenă ca un `BoxCollider2D` pe un `Sprite2D` aflat la (0, 0) în același grup. Asta a ținut cu rotația scenei exterioare, cu `X`/`Y`, `Rotation`, `TransformOrigin` și `Scale` 1 și 2 pe grup, și cu un offset.
- În aceeași rulare, `CollisionWorld.Overlap` al scenei rădăcină găsește colliderul de test, cu `Entity` = grupul. Cu un `SceneSimulationContext2D`, colliderul de test primește `Entered` și `Exited`.
- Deci `SceneGeometry2D.cs` nu trebuie schimbat. Fișierul de explorare a fost șters după rulare (21/21, împreună cu `ColliderOwnershipTests`).

### Dovada RED (Etapa 0)

- `dotnet build tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release` (și comanda din gate): numai 3 erori, toate `CS0246` pentru `ConeCollider2D` (`ConeCollider2DTests.cs:413`, `SceneVisionTests.cs:833`, `:843`). Compilatorul se oprește la erorile din declarații.
- Cu un `ConeCollider2D` gol, pus temporar în proiectul de teste și apoi șters: 81 de erori. Toate sunt simboluri lipsă: `Sprite2D.Vision`, `Scene2D.Vision`, `ColliderShapeKind2D.Cone`, `ColliderLocalShape2D.Cone`/`StartAngle`/`Sweep` și `GetLocalShape(flip)` (`CS1061`, `CS0117`, `CS1501`).
- SourceGen, `--filter "FullyQualifiedName~Vision"`: 7/7 pică. Generatorul dă `CERNEALAUI003` („Markup property 'Sprite2D.Vision' is not supported”) și `CERNEALAUI002` („Markup element 'ConeCollider2D' is not supported”).
- Language, același filtru: 9/9 pică, pe aceleași două diagnostice. Testele de completion pică la `Assert.NotNull`, pentru că tipul `ConeCollider2D` nu există. Ele cer întâi tipul, ca „conul nu e propus” să nu treacă din motivul greșit.
- Testul existent `CompletionOffersColliderShapesOnlyAtValidOwners` verifică numai cele patru forme din `Shapes` (`ColliderOwnershipLanguageTests.cs:16`, `:26-31`). Deci nu pică doar pentru că apare tipul `ConeCollider2D`.

### Forma de con (Etapa 1)

- `ConeCollider2D.GetLocalShape(flip)` (`ConeCollider2D.cs:66-99`): `Angle` 360 dă `ColliderLocalShape2D.Circle(Radius)`. Altfel, `Direction` se oglindește după `Flip`, se normalizează în `double`, iar startul ajunge în [0, 2π). `GetLocalShape()` ia `Flip` de la `Sprite2D`-ul proprietar.
- `ColliderLocalShape2D` are `StartAngle` și `Sweep`, în radiani. `Cone(radius, startAngle, sweep)` nu alocă.
- Funcția de suport (`GetConeSupport`) e valabilă pentru un con de cel mult 180°. `TryContact` împarte în două jumătăți un con mai larg de π + 1e-5. Marja de 1e-5 acoperă rotunjirea unui con de exact 180°. Fiecare jumătate are bounds-urile ei.
- `Raycast` și `ShapeCast` aruncă `InvalidOperationException` pentru un con. Din Etapa 3, filtrele lumii opresc conul înainte să ajungă acolo. Până atunci, un con pus în `Sprite2D.Collider` (permis până la Etapa 2) ar arunca din `Raycast` și din `MoveAndCollide`.
- Bounds-urile (`TryGetConeBounds`) iau vârful, cele două capete ale arcului și punctele de pe arc unde x sau y transformat e extrem.
- Overlay-ul de debug desenează conul: cele două raze și arcul, din segmente de cel mult 10°. Culoarea e încă cea a corpurilor; culoarea de senzor vine în Etapa 3.
- Verificare suplimentară a Implementerului, cu un test temporar, șters apoi: 3 914 conuri aleatoare (rotație, offset, scală neuniformă, 1 487 mai largi de 180°) față de un cerc mic, 0 diferențe față de `ContainsPoint`; 1 500 de perechi con–con, 0 suprapuneri ratate; bounds-urile a 3 000 de conuri, cea mai mare abatere 9,2e-5.
- Abatere de la ordinea planului: pagina `Cerneala.UI.Controls.ConeCollider2D.md` și intrarea ei în manifest au fost scrise în Etapa 1, nu în Etapa 4. Skill-ul `cerneala-implement-plan` cere documentația unui API public în aceeași etapă în care apare API-ul. Pagina nu descrie încă `Vision`, senzorul sau markup-ul; ele se adaugă în etapele lor. Intrarea din manifest stă în ordine alfabetică, după `ComboBoxItem`.
- `SceneVisionTests.cs` folosește `Vision`, care apare abia în Etapa 2. Ca proiectul de teste să compileze la gate-ul 1, fișierul a fost redenumit temporar `SceneVisionTests.cs.stage2`, fără schimbări de conținut. Etapa 2 îi dă înapoi numele.
- Până la Etapa 4, completion-ul din editor propune `ConeCollider2D` și în conținutul lui `Sprite2D`. Testul `VisionCompletionInSpriteContentOffersTheFourBodyShapesButNotTheCone` pică acum din motivul corect: tipul există și e propus.
- `Sprite2D.Flip` nu anunță încă lumea de coliziuni când se schimbă. Asta vine în Etapa 2, odată cu `Sprite2D.Vision`.

### Slotul de vedere (Etapa 2)

- `ColliderSlot2D` are acum un proprietar `SceneNode2D` și un rol intern, `ColliderSlotRole2D.Body` sau `Vision`. Slotul de corp refuză un `ConeCollider2D` înainte de orice schimbare, cu mesajul „ConeCollider2D can only be a Vision: assign it to Sprite2D.Vision or Scene2D.Vision, not to Sprite2D.Collider.”
- Algoritmul `Replace` nu s-a schimbat: o valoare nouă intră la sfârșitul lui `LogicalChildren`, iar o înlocuire ia indexul celei vechi. Așa conul unui `Scene2D` rămâne după toți `Children`. Pe asta se bazează `Scene2D.ChildCollection`, care inserează în `LogicalChildren` cu indexul din `Children` (`Scene2D.cs:476-548`). Testul `ChildrenOfASceneWithAVisionAddInsertReplaceAndRemoveWithoutTouchingTheCone` o arată.
- `ConeCollider2D.AllowsCollisionParent` acceptă `Sprite2D` sau `Scene2D`, după tip. Singura inserare făcută de proprietar în `LogicalChildren` al unui `Scene2D` e slotul de vedere. Regula `ownerManaged` din `Collider2D.ValidateParentChange` oprește orice altă cale: `scene.Children.Add(cone)` și `scene.LogicalChildren.Add(cone)` aruncă excepție.
- Mesajele din `Collider2D.ValidateParentChange` pomenesc acum și `Vision`.
- `Sprite2D.Flip` anunță lumea cu o mutație `Geometry` pentru `Vision`, când `Vision` există. Implementerul a verificat cu un test temporar, șters apoi: fără anunț, cele 7 cazuri cu `Flip` pică.
- Culling: `SceneChildBoundsIndex2D.Rebuild` sare acum peste collidere. Înainte, un grup cu `Vision` pierdea calea rapidă din `Scene2D.Record` (`indexed.Length 5 != Children.Count 4`) și ordona toți copiii. În plus, conul era candidat la hit-test. Desenul ieșea totuși corect, pentru că și calea lentă taie fiecare intrare. Indexul îl mai folosește numai `SceneItems2D`, care nu poate avea collidere.
- Limită: când se schimbă o proprietate a conului, grupul din scena exterioară e marcat murdar și se măsoară din nou. E muncă mică și nu a fost măsurată.
- Testele de markup din Language și SourceGen pică acum din alt motiv: `Vision` există, deci nu mai apare `CERNEALAUI003`. Apare `CERNEALAUI005` („Colliders can only be owned by Sprite2D or tiles”, `CernealaSemanticModel.cs:281`). Etapa 4 le rezolvă.
- Documentația scrisă în Etapa 2: secțiunea `Vision` din `Sprite2D.md` și `Scene2D.md`, tabelul de ownership din `Collider2D.md`, exemplul cu `bee.Vision` și refuzul slotului de corp în `ConeCollider2D.md`. Fără senzor și fără markup; ele vin în etapele 3 și 4. Grep după citări cu linie către fișierele schimbate (`Sprite2D.cs:`, `Scene2D.cs:`, `Collider2D.cs:`, `ColliderSlot2D.cs:`, `SceneChildBoundsIndex2D.cs:`), în afara planurilor: nicio citare mutată. Una era deja greșită și nu a fost atinsă: `benchmarks/Cerneala.Benchmarks/results/2026-09-04-collision-stage0/algorithm-market.md:331` citează `Scene2D.cs#L9`.

### Senzorul (Etapa 3)

- Flag-ul vine din slot: slotul de vedere pune `Collider2D.VisionOwner` (intern) înainte de `InsertOwned` și îl șterge după `RemoveOwned`. `IsVisionSensor` înseamnă `VisionOwner is not null`. Intrarea din lume (`CollisionEntry2D`) primește `SensorOwner` la creare, iar `AddOrUpdate` o refă când se schimbă.
- `PassesQueryFilter` respinge senzorii. Deci `Overlap`, `Raycast` și `MoveAndCollide` nu întorc niciodată un `Vision`. `CollectPointHits` (picking) sare peste senzori. `CollectDebugGeometry` îi întoarce în continuare, pentru overlay.
- `PassesPairRules` = regula senzorului plus filtrul layer/mask. O folosesc `Intersects`, `Overlap` și contactele. Regula senzorului: doi senzori nu se ating niciodată. Un senzor nu atinge un collider din subarborele proprietarului lui (V1): propriul corp pentru un `Sprite2D`, orice collider din grup pentru un `Scene2D`.
- Contactele (`UpdateContact`): un corp nu raportează niciodată un senzor. Într-o pereche corp–senzor, numai senzorul primește evenimentul, cu `Other` = corpul.
- `MoveAndCollide(vision, …)` aruncă excepție (V2) înainte de verificarea „nu e în lume”. Deci aruncă și cu `Enabled = false`, și când proprietarul nu e în scena lumii.
- Consecință a lui V1: un `Vision` pe `Scene2D`-ul rădăcină nu vede nimic, pentru că toate colliderele sunt în acea scenă (testul `AVisionOnTheRootScene…`). Scrisă în `ConeCollider2D.md`.
- Testele noi au fost verificate prin mutație: cu regula senzorului înlocuită temporar prin `return true`, 13 din 55 de teste `SceneVision` pică. Mutația a fost scoasă.
- Costul pe frame nu a fost măsurat. Munca adăugată: două verificări `null` pe pereche evaluată, o urcare prin `LogicalParent` pentru perechile corp–senzor și o verificare `bool` pe candidat în `CollectPointHits`. Regula senzorului nu alocă. O vulpe cu handlere face totuși testul exact față de un con fără handlere; rezultatul e aruncat.
- `CollisionWorld2DDiagnosticsSnapshot.EntryCount` numără și conurile `Vision`. Asta nu e documentat.
- Documentația scrisă în Etapa 3: secțiunile „A Vision is a sensor” și „A Vision never sees its owner” din `ConeCollider2D.md`, câte un punct în `Sprite2D.md` și `Scene2D.md`, contactele, `IsTrigger` și picking-ul în `Collider2D.md`, secțiunea „Vision sensors” din `CollisionWorld2D.md`, culoarea și arcul în `Scene2DDebugOverlay.md`. În `docs/architecture/scene2d.md` au fost corectate trei citări mutate de această etapă: `CollisionWorld2D.cs:347` devine `:354`, `:376-386` devine `:383-393`, iar `CollisionWorld2D.Contacts.cs:212-214` devine `:214-216`.

### Markup (Etapa 4)

- Editorul și build-ul au un singur set de reguli, în modelul de limbaj. Generatorul nu generează nimic când modelul raportează o eroare (`UiMarkupGenerator.cs:489`, `TryGetEmissionDocument`), deci `CERNEALAUI005` pentru un con pus greșit e diagnosticul modelului. `GetColliderPlacementError` (`CernealaSemanticModel.Tiles.cs`) acceptă exact un con într-un `Vision` și refuză conul în orice alt loc, cu un mesaj care trimite la `<Sprite2D.Vision>`. `ReportVisionPropertyElementShape` refuză un `Vision` gol, dublat, cu text sau cu atribute. Spațiile, comentariile și `xmlns` sunt permise.
- Generatorul emite `owner.Vision = cone;` (`EmitVisionPropertyElement`, `UiMarkupPropertyElementEmitter.cs`), după tiparul lui `<RenderSurface2D.Scene>`. Aceleași verificări de formă stau și acolo, ca plasă de siguranță, cu același mesaj.
- Completion-ul propune `ConeCollider2D` numai într-un `Vision` gol și nu propune acolo cele patru forme de corp. În conținutul lui `Sprite2D` nu mai propune conul.
- `GetColliderPlacementError` iese imediat pentru un element care nu e collider și nu stă într-un `Vision`. Așa elementele obișnuite nu plătesc o căutare nouă de tip. Costul nu a fost măsurat.
- Abatere de la brief: rândul SourceGen `<Sprite2D.Collider><ConeCollider2D /></Sprite2D.Collider>` a rămas. Generatorul nu cunoaște forma `<Sprite2D.Collider>`, dar modelul de limbaj o cunoaște și dă `CERNEALAUI005` înainte de emitere, deci rândul trece din motivul corect.
- Defect vechi, în afara planului, nereparat aici: `<Sprite2D><Sprite2D.Collider><BoxCollider2D /></Sprite2D.Collider></Sprite2D>` nu dă niciun diagnostic în editor, dar build-ul dă `CERNEALAUI002 Markup element 'Sprite2D.Collider' is not supported by the source generator`. Reprodus cu teste temporare pe master `fdb9a6a5` și pe codul final. Draft de issue: `.artifacts/issue-drafts/2026-10-25-sprite-collider-property-element.md` din checkout-ul principal, neposat.

### Auditul final

Un Reviewer a citit tot diff-ul și toate citările din `scene2d.md`. Ce a găsit și ce s-a făcut:

- **D1, defect, reparat.** Conul intra în ruta de input. Bounds-urile de input ale albinei îl includeau, iar `Servo ClickAsync` pe albină apăsa în fața ei și pica. Test RED întâi: bounds-urile ieșeau `X = 38, Y = -6.07, Width = 162, Height = 212.13` în loc de `38, 95, 10, 10`, iar click-ul dădea „center is not hit-testable”. Reparația: `Collider2D.ParticipatesInInputRoute` este `false` pentru un senzor (`Collider2D.cs:41-44`). Testele noi: `AVisionDoesNotChangeTheInputBoundsOfItsOwner`, `ServoClickOnTheOwnerOfAVisionHitsTheOwnerAndNotTheEmptyPlaceInFrontOfIt` și `AVisionIsNeitherInTheInputRouteNorInTheServoTreeWhileTheBodyIs`.
- **D2, nepotrivire editor/build, reparată.** Textul și atributele dintr-un `Vision` erau refuzate numai de generator. Acum le refuză și modelul de limbaj, cu același mesaj (test RED întâi: 5 cazuri).
- **Erori de documentație, reparate:** două citări din `scene2d.md` (`:358-363` devine `:359-365`, `:1021-1026` devine `:1021-1027`), fraza despre reguli din `ConeCollider2D.md` și fraza veche din `UIElementCollection.md` („the singular `Sprite2D.Collider` property”).
- **Mărunțișuri:** comentariile de început din testele RED descriu acum contractul. Numărul de segmente din `DrawCone` are o toleranță de 1e-4, ca un con de 90° să dea mereu 9 segmente.

Teste focusate pe codul final (suita completă nu a fost rulată):

- `Cerneala.Tests`, filtrul larg pentru scene, collidere, coliziuni, input, Servo, `UiHost`, `WindowRuntime`, `TileMap`, `SceneItems`, prezentare și animație: 1 461/1 461.
- `Cerneala.Tests.SourceGen` (`Vision|Collider|Sprite|Scene|Tile|PropertyElement|Ownership`): 181/181.
- `Cerneala.Tests.Language` (`ColliderOwnershipLanguage|Completion|Diagnostic|Semantic|Collider|Tile|SceneComponent`): 454 trecute, 1 sărit. Cel sărit are deja `Skip` (`CompletionTests.cs:757`).
- `Cerneala.Tests.LanguageServer` (`DiagnosticsTests`, cu catalogul golden): 13/13.
- `Cerneala.Tests.Scene2DPackages`: 104/104. `Cerneala.Tests.SceneVillage`: 29/29. `Cerneala.Tests.Scene2DImporters`: 173/173. `Cerneala.Tests.Timbre` (`OwnerAccessTests`): 10/10.
- `Cerneala.Tests.SdlGpu` (`Servo|SdlInput|NativeScene|NativeTetris`): 89 trecute, 17 sărite. Toate cele 17 sunt sărite cu motivul „Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured native matrix runner.” (`SdlNativeFactAttribute.cs:16`). Testele native SDL nu au fost rulate.
- `Cerneala.Tests.VisualStudio` (`ApiDocumentationManifestIsValidAndReferencesExistingFiles`): 1/1.

### Limite cunoscute

- `Flip` oglindește numai `Direction`. `OffsetX`/`OffsetY` nu se oglindesc (D18, scris în docs).
- `Flip` vine din `Sprite2D.Flip`, nu din flip-ul compus cu cel al cadrului de animație.
- Un `Vision` pe `Scene2D`-ul rădăcină nu vede nimic (consecință a lui V1, scrisă în docs).
- Servo nu găsește conul nici după id, pentru că nu e în arborele Servo. Un collider de corp poate fi găsit (`ServoSceneTests`).
- `CollisionWorld2DDiagnosticsSnapshot.EntryCount` numără și conurile.
- Proprietățile conului nu sunt în registrul Motion (`AnimatablePropertyRegistry.cs`, care are Box și Circle). Planul nu le promitea.
- Costul pe frame al regulii de senzor, al conului în GJK și al verificării din modelul de limbaj nu a fost măsurat.

### Ramurile pe `ColliderShapeKind2D` (Etapa 0)

Verificat pe master `fdb9a6a5` cu Roslyn `find_references` pe enum și pe membrii lui, plus Grep și citirea liniilor. Conul de 360° devine `Circle` în `ConeCollider2D.GetLocalShape()`, deci intră pe drumul cercului. Conul mai larg de 180° se împarte în două subconuri, fiecare de cel mult 180°.

| Locul | Ce face azi | Ce primește conul |
| --- | --- | --- |
| `CollisionNarrowPhase2D.Support` (`:902-934`) | Funcția de suport pentru GJK și EPA. `default` dă originea (`:928-930`). | Caz nou: punctul de pe arc dacă direcția locală cade în arc, altfel cel mai bun dintre vârf și cele două capete ale arcului. |
| `CollisionNarrowPhase2D.TryFastContact` (`:332-409`) | Căile rapide. Orice formă care nu e `Circle` e tratată ca poligon (`:372-373`), cu vârfurile din `GetLocalPolygonVertices`. | Conul iese din căile rapide la fel ca `Segment` (`:341-346`) și merge pe GJK. |
| `CollisionNarrowPhase2D.TryContact` (`:16-61`) | Intrarea comună pentru `Intersects`, `Overlap` și contacte. | Conul mai larg de 180°: perechea se testează cu fiecare subcon. |
| `CollisionNarrowPhase2D.GetCenter` (`:1230-1241`) | Un punct din interiorul formei. `default` dă originea. | Caz nou: un punct din interiorul conului, pe direcția lui. |
| `CollisionNarrowPhase2D.Raycast` (`:63-117`) și `RaycastDegenerate` (`:1111-1125`) | Raycast. O formă necunoscută merge pe drumul de poligon. | Nu se ajunge aici: `Raycast` sare peste senzori (Etapa 3). Conul primește o ramură explicită, ca să nu fie tratat ca poligon fără vârfuri. |
| `ShapeCast` (`:119`), `HasNonclosingContactPlane` (`:226-267`), `HasNonclosingEdge` (`:269-291`) | Mișcarea din `MoveAndCollide`. | Nu se ajunge aici: `MoveAndCollide` sare peste senzori. Ramură explicită, la fel ca la raycast. |
| `CollisionNarrowPhase2D.TryGetSimilarityCircle` (`:936-967`) | Recunoaște cercurile. | Neschimbat: un con nu e cerc. |
| `CollisionNarrowPhase2D.GetLocalPolygonVertices` (`:980-989`) | Vârfurile unui box sau poligon. | Neschimbat: nu se mai ajunge aici cu un con, după ramurile de mai sus. |
| `SceneGeometry2D.ContainsPoint` (`:298-330`) | Punct în formă, pentru picking. `_ => false`. | Caz nou, exact: distanța până la vârf și unghiul. Picking-ul sare oricum peste senzori. |
| `SceneGeometry2D.TryGetColliderBounds` (`:369-402`) | Bounds-urile din lume. `default` dă `false`, iar atunci colliderul nu intră deloc în lume (`TryCreateColliderGeometry`, `:288-291`). | Caz nou: bounds strânse în jurul vârfului și al arcului. |
| `Scene2DDebugOverlay.DrawCollider` (`:152-179`) | Desenul. O formă necunoscută e desenată ca poligon din vârfuri (`:168-176`). | Caz nou: arcul și cele două raze, cu o culoare proprie pentru senzor. |
| `SceneGeometry2D.TryGetEntityBounds` (`:39-57`) | Bounds de entitate, după un string, nu după enum. | Neatins. |
| Tile-urile: `TileColliderDescriptor2D.CreateLocalShape` (`:124-131`), `TileMap2D.Collision.cs:139` | Folosesc enumul separat `TileColliderShape2D`. | Neatinse: conul nu e formă de tile. |
| `SceneItems2D`, SourceGen, Language, `Scene2D.Importers` | Nicio referință la `ColliderShapeKind2D`. | Neatinse. |

Testele: singura referință este `CollisionStageOneContractTests.cs:63` (`Box`).

### Apelanții ownership-ului și ai slotului (Etapa 0)

Verificat cu Roslyn `find_references` și Grep pe `UI/`.

| Simbol | Apelanți | Migrare sau contract |
| --- | --- | --- |
| `Collider2D.AllowsCollisionParent` (`Collider2D.cs:32`, azi `parent is Sprite2D`) | `Collider2D.ValidateParentChange` (`Collider2D.cs:22`). Override: `TileStaticCollider2D.cs:36`. | Migrare: acceptă și un `Scene2D`, numai pentru conul din slotul lui de vedere. Tile-ul rămâne neschimbat. |
| `Collider2D.ValidateParentChange` (`Collider2D.cs:19-30`) | `UIElementCollection.ValidateInsertion` (`UIElementCollection.cs:70`), `Move` (`:131`, cu `ownerManaged: false`), `RemoveCore` (`:161`, cu `parent` = `null`, deci verifică `LogicalParent`). `TabItem.cs:247`. Apeluri `base` din `SceneNode2D.cs:37` și `TileMap2D.Streaming.cs:72`. | Contract neschimbat: rolul Logical și regula „numai prin proprietar”. Scoaterea conului din `Scene2D` trece prin `RemoveCore`, deci `AllowsCollisionParent` trebuie să accepte `Scene2D` și atunci. |
| `CollisionWorld2D.EnumerateColliders` (`CollisionWorld2D.cs:507-523`) | `Rebuild` (`:379`), `ReconcileAll` (`:398`), `ReconcileSubtree` (`:424`), el însuși (`:517`), `CollisionWorld2D.Streaming.cs:158`. | Contract neschimbat: parcurge `LogicalChildren` ale oricărui `SceneNode2D`, deci găsește conul și sub `Sprite2D`, și sub `Scene2D`. Flag-ul de senzor se pune la crearea intrării. |
| `ColliderSlot2D` (`ColliderSlot2D.cs:4`) | Numai `Sprite2D`: câmpul (`Sprite2D.cs:17`), constructorul (`:53`), `Collider` (`:56-60`), `AttachSurface` (`:62-66`). | Migrare: un rol (corp sau vedere) și un proprietar care poate fi `Sprite2D` sau `Scene2D`. |
