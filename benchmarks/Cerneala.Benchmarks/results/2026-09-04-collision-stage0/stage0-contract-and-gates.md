# Collision stage 0: caller inventory, API contract și gate-uri

## Caller inventory pentru input unificat

| Caller / owner | Comportament observat acum | Contract/migrare aprobată |
|---|---|---|
| `ElementInputCache` | păstrează un singur `ElementInputRouteMap` și numără rebuild-urile | rămâne cache-ul unic; geometria animată invalidează hit-test, nu ruta structurală |
| `ElementInputRouteBuilder` | traversează numai `VisualChildren` | include numai subtree-uri logice oferite prin contractul intern specializat; pentru scenă ruta este `RenderSurface2D -> Scene2D -> SceneNode2D` |
| `ElementInputRouteMap` / `UiInputTree` | mapare bidirecțională `UIElement` / `UiElementId` real | neschimbat; niciun ID sintetic și niciun al doilea arbore |
| `HitTestService` | recursie visual-only și `ArrangedBounds` | rămâne intrarea unică; testează overlay-urile vizuale înainte de host-ul geometric, apoi suprafața |
| `RenderSurface2D` | aplică ViewBox numai la render | implementează contract intern geometric și aceeași inversă pentru root/control/scene/node; ViewBox neinversabil refuză inputul scenei |
| `ElementInputBridge` | hit-test, capture override și dispatch pentru mouse/wheel/key/text | neschimbat semantic; primește ținta reală a scenei și folosește aceeași rută |
| `TouchInputBridge` / `StylusInputBridge` | folosesc același `HitTestService` și route map | neschimbate; beneficiază automat de geometria specializată |
| `RoutedEventRouter` / `InputEvents` | tunnel/bubble, `Handled`, `handledEventsToo` | reutilizate; este interzis un router sau event args „de joc” |
| `PointerCaptureManager` | captura este `UIElement`; eliberează dacă ID-ul lipsește din ruta curentă | nod reparentat în același root păstrează captura pe același element; nod eliminat/ascuns/neparticipant o pierde la următorul dispatch |
| `FocusManager` | folosește ancestor walk vizual | ancestor path vine din route map pentru ținte logice; controalele vizuale păstrează aceeași rută |
| `CommandRouter`, `TextInputBridge`, `DragDropController` | folosesc route map | neschimbate; nu au nevoie de subsistem separat |
| `HoverTracker` | `VisualParent` | construiește path-ul din `UiInputTree` când ținta este logică |
| `PressedStateTracker` | caută contractul prin `VisualParent` | urcă prin ruta unică |
| `CursorService` | caută cursorul prin `VisualParent` | urcă prin ruta unică |
| `KeyboardActivationController` | caută activatorul prin `VisualParent` | urcă prin ruta unică |
| `ElementInputBridge.FindAncestor` / `ResolveFocusTarget` | `VisualParent` | urcă prin route map; nu se modifică layout-ul pentru a fabrica părinți vizuali |

Caracterizarea existentă pentru overlay este `CollisionStageZeroContractTests.ExistingVisualOverlayStillWinsBeforeSceneGeometry`; aceasta este GREEN înainte de implementare. RED-ul de rută reală cere aceleași preview/bubble events prin root, surface, scene și collider.

## API public înghețat

### Collidere

- `abstract Collider2D : SceneNode2D`
  - `Enabled: bool = true`
  - `IsTrigger: bool = false`
  - `OffsetX`, `OffsetY: float = 0`
  - `CollisionLayer: uint = 1`
  - `CollisionMask: uint = uint.MaxValue`
- `BoxCollider2D`: `Width`, `Height`, ambele finite și strict pozitive.
- `CircleCollider2D`: `Radius`, finită și strict pozitivă.
- `PolygonCollider2D`: `Points` este șir markup `x,y x,y ...`; `Vertices` expune copia read-only parsată. Sunt necesare minimum trei puncte finite, arie nenulă și convexitate. CW și CCW sunt acceptate; concav/coliniar se respinge explicit.

Toate proprietățile de mai sus sunt `UiProperty`. `OffsetX/Y`, `Width`, `Height`, `Radius` și transformurile moștenite pot fi interpolate de Motion. `Enabled`, `IsTrigger`, `CollisionLayer`, `CollisionMask` și `Points` sunt discrete; Aspect le poate stabili, Motion nu inventează mixere. Prism nu participă la colliderul nevizual.

### Lume și rezultate

- `Scene2D.CollisionWorld: CollisionWorld2D` este instanța unică a rădăcinii.
- `Intersects(Collider2D, Collider2D)` întoarce verdict exact.
- `Overlap(Collider2D, CollisionQuery2D = default)` întoarce `CollisionHit2D[]`.
- `Raycast(Vector2 origin, Vector2 direction, float maxDistance, CollisionQuery2D = default)` întoarce `CollisionHit2D[]`; direcția zero și distanțele nefinite/negative sunt respinse.
- `MoveAndCollide(Collider2D, Vector2 displacement, CollisionQuery2D = default)` întoarce `MoveCollisionResult2D` și nu mută colliderul.
- `CollisionHit2D` este immutable și conține `Collider`, `Entity`, `Point`, `Normal`, `Distance`, `Fraction`, `IsTrigger`.
- `MoveCollisionResult2D` este immutable și conține `RequestedDisplacement`, `Travel`, `Remainder`, primul `Collision` blocking și toate `TriggerHits` până la travel-ul cerut.
- `CollisionQuery2D` este immutable și poate restrânge query layer/mask, include/exclude triggers și exclude un collider sursă fără a schimba colliderele.

`Entity` este cel mai apropiat strămoș `SceneNode2D` non-collider; dacă nu există, este colliderul însuși.

## Semantici numerice și ordine

- Contactul la muchie este inclus. Epsilon-ul intern este `1e-5f` scene units și se aplică numai comparațiilor, nu coordonatelor returnate.
- Layer/mask sunt bit fields, nu indici: perechea trece numai dacă `(a.CollisionMask & b.CollisionLayer) != 0` și `(b.CollisionMask & a.CollisionLayer) != 0`.
- `CollisionLayer == 0` sau `CollisionMask == 0` înseamnă că acel collider nu interacționează cu nicio pereche bilaterală; `uint.MaxValue` înseamnă toate bit-urile.
- Triggerele apar în overlap/raycast și `TriggerHits`, dar nu pot fi `MoveCollisionResult2D.Collision` și nu limitează `Travel`.
- Ordinea overlap este ordinalul stabil de atașare în scenă. Raycast și move sortează prin `Fraction`, apoi distanță, apoi ordinal. Ordinea hash-ului nu este publică.
- Initial overlap are `Fraction = 0`, `Distance = 0`; normala este axa de penetrare minimă cu tie-break X înainte de Y și semn determinat de centre, apoi ordinal.
- `MoveAndCollide` folosește shape cast continuu pe întregul displacement; nu permite tunneling și nu mută modelul.

## Picking și coordonate

- `IsHitTestVisible`, `IsVisible`, `Visibility` și `IsEnabled` urmează contractul UI existent. Opacity zero rămâne hit-testabilă, ca în UI; Prism nu schimbă geometria.
- Un grup/nod cu collidere explicite este pick-uit prin reuniunea colliderelor enabled cu `CollisionLayer != 0`. Dacă nu are collider explicit, nodul vizual poate folosi bounds-ul exact disponibil din `SceneGeometry2D`; `Unknown` nu devine un dreptunghi inventat.
- Clip-ul vizual al arborelui UI și bounds-ul `RenderSurface2D` sunt verificate înainte de scenă. În scenă se testează reverse effective draw order (`Layer`, apoi Y când politica cere, apoi source ordinal).
- `MouseEventArgs.X/Y` rămân coordonate root brute compatibile. `GetPosition(UIElement relativeTo)` oferă `Vector2` și folosește aceeași cale de transform pentru elemente vizuale și noduri logice.
- `RenderSurface2D.TryRootToScene` refuză determinist un ViewBox neinversabil; `SceneToRoot` este inversa publică. Nu se rotunjește înainte de verdict.

## Invalidation și lifecycle

- Orice schimbare de formă, transform, offset, enabled, layer/mask, visibility, attach/detach/reparent actualizează versiunea lumii înainte de următorul query/hit-test.
- Add/remove/reparent afectează ruta și indexul; o schimbare numerică de geometrie afectează indexul/hit-test-ul, nu reconstruiește route map.
- Un sample Aspect/Motion trece prin aceeași mutație `UiProperty`; nu există index paralel.
- Tile-ul batch-uit rămâne date. `TileInstance2D.Colliders` există numai după promovare. `ReplacesImportedColliders` decide explicit dacă descriptorii importați sunt înlocuiți sau compuși, prevenind dublarea.

## Baseline și praguri

Runner: `dotnet run --project benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj -c Release -- --collision-stage0 <path>`.

Rezultat brut: [baseline.json](baseline.json). Punctele decisive măsurate pe host-ul arhivat în JSON:

| Scenariu | Algoritm | Build us | Update P95 us | Query P95 us | Retained bytes |
|---|---|---:|---:|---:|---:|
| large-sparse | exhaustive | 68.9 | 11.7 | 62,568.8 | 192,000 |
| large-sparse | sparse grid | 7,165.4 | 68.7 | 172.9 | 1,143,360 |
| large-sparse | dynamic AABB prototype | 10,606.8 | 2.4 | 179.4 | 1,620,864 |
| long-fence | sparse grid | 1,353.2 | 19.7 | 65.0 | 278,624 |
| high-churn | sparse grid | 1,782.5 | 614.1 | 78.3 | 368,192 |
| high-churn | dynamic AABB prototype | 5,751.9 | 18.1 | 104.3 | 409,600 |

Pragurile obligatorii pentru Etapa 2 sunt cele din `algorithm-market.md`: 500 us/150 us/1,5 MB large-sparse, 250 us/1.000 us high-churn, 150 us long-fence, plus zero false-negative față de oracle.

## Dovezi RED

- [stage0-core-red.trx](stage0-core-red.trx): 11 RED exclusiv din API/lume/rută absente, 1 GREEN pentru overlay-ul UI existent.
- [stage0-sourcegen-red.trx](stage0-sourcegen-red.trx): trebuie să conțină numai `CERNEALAUI002` pentru tipurile de collider absente; erorile de fixture/binding nu sunt acceptate.
