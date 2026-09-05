# Plan: collidere, lume de coliziune si picking pentru scena 2D

Status: finalizat
Acopera: #3, #4, #5, #6
Depinde de: `2026-09-04-rendersurface2d-scene-foundation.md`; integrarea colliderelor de tile asteapta `2026-09-04-rendersurface2d-tilemap-and-scale.md`

## Rezultat urmarit

Un autor poate grupa in markup un sprite si unul sau mai multe collidere, poate interoga lumea scenei (`overlap`, `raycast`, miscare cu contact) si poate primi pe nodul ales aceleasi routed events ca orice alt `UIElement`. Geometria, transformurile si ordinea folosite de coliziune/picking raman coerente cu redarea.

## Limita arhitecturala

- Colliderele si indexul spatial apartin scenei. Backend-urile grafice nu cunosc coliziunea.
- Exista un singur sistem UI/input. `HitTestService` ramane intrarea unica, dar poate cere unui host intern specializat sa testeze geometrie care nu vine din layout. `RenderSurface2D` converteste pozitia si returneaza nodul `SceneNode2D` real ca `HitTestResult`.
- Nodurile scenei sunt inregistrate in acelasi `ElementInputRouteMap`/`UiInputTree` sub `RenderSurface2D` si parintii lor logici. Ele folosesc aceleasi `InputEvents`, tunnel/bubble, `Handled`, hover, pointer capture, focus, cursor si command routing; nu se creeaza un router ori evenimente „de joc” paralele.
- Copiii vizuali/overlay-urile suprafetei sunt testati inaintea continutului scenei, astfel incat un control UI desenat deasupra sa ramana tinta.
- Asocierea vizual-collider este structurala: sprite-ul si colliderul impart un grup/parinte si DataContext. Nu se introduce un ECS sau un `RigidBody` obligatoriu.
- `MoveAndCollide` calculeaza deplasarea/impactul si returneaza un rezultat; nu muta arbitrar modelul jocului si nu ruleaza o simulare fizica.
- Triggerele participa la interogari/evenimente conform contractului, dar nu blocheaza miscarea.
- Formele initiale sunt box, circle si polygon convex. Polygon concav trebuie respins sau descompus explicit; nu este aproximat tacit.
- Colliderele sunt `SceneNode2D` fara continut vizual propriu: primesc Aspect si Motion pe proprietatile aprobate, dar Prism nu are ce procesa decat prin overlay-ul de debug.

## API/semantica ce trebuie decisa prin teste

- `Enabled`, `IsTrigger`, offset, layer si mask, plus forma si transformul scene-space.
- Convenția layer/mask: o pereche interactioneaza numai daca filtrarea bilaterala stabilita este satisfacuta; zero/all au semantica documentata.
- Reguli pentru contacte la muchie, epsilon, coordonate negative, raza zero si poligoane degenerative.
- Ordinea determinista a rezultatelor multiple si tie-break-ul raycast.
- Picking-ul: `IsHitTestVisible`, geometria folosita (collider explicit, bounds vizual sau ambele), clip/visibility/opacity si reverse effective draw order. Se prefera contractul UI existent in locul unui duplicat `IsPickable`, daca testele nu dovedesc o nevoie distincta.
- `MouseEventArgs`/API-ul comun de coordonate trebuie sa permita obtinerea pozitiei fata de suprafata, scena sau nodul rutat, fara o familie separata de event args si fara `UiElementId` fals.
- Aspect poate stabili forma/offset/enabled/trigger/layer/mask. Motion poate anima valorile geometrice/transformul cu mixere valide; valori discrete precum layer/mask/trigger se schimba numai prin politica discreta aprobata.
- Fiecare sample Aspect/Motion care schimba geometria ori filtrarea actualizeaza indexul spatial inaintea urmatoarei interogari si invalideaza hit-test-ul comun; Prism ramane exclus din semantica de coliziune.
- Cazul ViewBox neinversabil: inputul de scena este refuzat determinist, fara coordonate fabricate.

## Ownership si caller inventory pentru inputul unificat

- Constructia rutei: `ElementInputRouteBuilder`, `ElementInputRouteMap`, `UiInputTree` si invalidarea din `ElementInputCache`.
- Geometria: `HitTestService`, `HitTestResult`, `RenderSurface2D` si helper-ul comun de transform/bounds al scenei.
- Dispecerizarea: `ElementInputBridge`, `RoutedEventRouter`, `FocusManager`, `CommandRouter`, `TextInputBridge` si `DragDropController`.
- Starea comuna ce urca azi prin `VisualParent`: `HoverTracker`, `PressedStateTracker`, `CursorService`, `KeyboardActivationController` si cautarile de focus/command/drag din `ElementInputBridge`.
- Captura: `PointerCaptureManager` lucreaza deja cu `UIElement` si route-map ID; trebuie caracterizat pentru un nod logic eliminat/reparentat.
- Testele existente pentru input, focus, commands, cursor, drag si overlay sunt caller contracts si trebuie sa ramana verzi; planul nu schimba layout-ul doar ca sa satisfaca inputul.

## Etapa 0 - contract, algoritmi si reproduceri RED

- [x] Se inventariaza semantic `HitTestService`, `ElementInputRouteBuilder`, `ElementInputRouteMap`, `UiInputTree`, `ElementInputBridge`, `InputEvents`, `PointerCaptureManager`, `FocusManager`, `CommandRouter`, `TextInputBridge`, `DragDropController`, `RenderSurface2D` si toate caile care ridica mouse/touch/keyboard/text; se arhiveaza caller table cu migrarea sau contractul neschimbat.
- [x] Se inventariaza toate ancestor walks din input bazate pe `VisualParent`; `HoverTracker`, `PressedStateTracker`, `CursorService`, `KeyboardActivationController` si cautarile din `ElementInputBridge` sunt migrate la ruta unica numai unde este necesar, cu caracterizare pentru controalele UI existente.
- [x] Se foloseste workflow-ul `algorithm-market` pentru broadphase dinamic si testele exacte de forme; se compara cel putin uniform grid/spatial hash, quadtree si dynamic AABB tree pe scenariile reale: garduri statice, tile colliders si actori dinamici.
- [x] Se construieste un corpus determinist cu lume mica, lume mare sparse, gard lung, colțuri, overlap initial, miscare rapida si obiecte mutate frecvent; se masoara build/update/query, candidati, alocari si memorie.
- [x] Se alege algoritmul numai dupa rezultate si se arhiveaza decizia, alternativele si pragurile. Daca niciun algoritm nu domina, contractul permite strategii interne fara a le expune public.
- [x] Se adauga teste RED pentru box/circle/polygon, transformuri imbricate, offset, layer/mask, trigger, add/remove/move si ordine determinista.
- [x] Se adauga teste RED pentru Aspect pe enabled/trigger/layer/mask/geometrie si Motion pe offset/transform/dimensiuni; fiecare sample trebuie observat simultan de query si hit-test, fara rebuild al geometriei statice neafectate.
- [x] Se adauga teste RED pentru collider pe un tile promovat sparse, inclusiv promovare/demotare, schimbarea chunk-ului si o usa care dezactiveaza colliderul cand starea devine `Open`.
- [x] Se adauga teste RED pentru `Intersects`/overlap, `Raycast` si `MoveAndCollide`, inclusiv tunneling-ul acoperit de contractul ales.
- [x] Se adauga teste RED de input user-like care cer aceleasi `InputEvents`: ViewBox screen-to-world, noduri suprapuse, strat/Y order, tunnel/bubble prin scena si `RenderSurface2D`, enter/leave, press/release, wheel, cursor, focus, command, captura, nod eliminat in timpul capturii si `Handled`/`handledEventsToo`.
- [x] Se caracterizeaza inputul UI existent pe controale suprapuse cu un `RenderSurface2D`, pentru a preveni furtul inputului de la overlay-uri UI.

### Gate etapa 0

- [x] RED-urile esueaza din cauza subsistemelor absente, nu din cauza coordonatelor gresite din fixture.
- [x] Broadphase-ul si algoritmii exacti sunt alesi din masuratori/referinte, nu din intuitie.
- [x] API review-ul stabileste semantica de contact, filtrare, ordine, coordonate relative si captura inainte de productie; un al doilea event/router public este interzis de decizia utilizatorului.

Evidenta RED si decizia din 2026-09-04:

- Inventarul callerilor, ancestor-walk-urilor, contractul API/numeric/input/capture si pragurile sunt arhivate in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-collision-stage0/stage0-contract-and-gates.md`.
- `algorithm-market.md` evalueaza 16 candidati din 6 familii; ambele trasee de cercetare au ajuns la saturatie dupa doua runde consecutive fara o familie credibila noua. Grid-ul uniform sparse este alegerea initiala best-fit, hibridul k-d + SAP este best absolute, iar dynamic AABB tree ramane alternativa interna.
- Corpusul determinist arhivat in `baseline.json` masoara build/update/query, candidati, alocari si memorie pentru lumea mica, lumea mare sparse, gard lung, colturi, overlap initial, miscare rapida si high-churn. Pragurile inghetate pentru Etapa 2 sunt 500 us query/150 us update/1,5 MB pe `large-sparse`, 250 us query/1.000 us update pe `high-churn`, 150 us query pe `long-fence` si zero false-negative fata de oracle.
- Proiectele core si SourceGen compileaza fara warnings/errors. `stage0-core-red.trx` contine 11 RED numai pentru API/lume/ruta absente si 1 GREEN pentru overlay-ul UI existent; `stage0-sourcegen-red.trx` contine 2 RED numai `CERNEALAUI002` pentru tipurile collider absente.

## Etapa 1 - forme declarative si legarea de entitate

- [x] Se implementeaza baza collider si formele aprobate cu UiProperties bindable pentru markup.
- [x] UiProperties colliderului sunt compatibile Aspect/Motion conform matricei; proprietatile fara mixer valid sunt discrete sau primesc diagnostic, nu interpolare inventata.
- [x] Generatorul accepta collidere in arborele `Scene2D`, inclusiv intr-un grup comun cu sprite-ul si binding-uri catre acelasi DataContext.
- [x] Un singur helper transforma forma locala in scene/world bounds si geometrie; acelasi helper este consumabil de debug overlay.
- [x] Mutatia formei, transformului, filtrului, visibility/enabled si reparenting-ul invalideaza indexul exact o data.
- [x] Schimbarile produse de Aspect/Motion urmeaza aceeasi cale de mutatie/indexare ca setarea directa si nu creeaza un index paralel.
- [x] Se valideaza formele degenerative si valorile nefinite la granita publica, cu exceptii/diagnostice documentate.

### Gate etapa 1

- [x] Markup-ul poate descrie un gard vizual cu mai multe box colliders si un player cu collider, fara cod de wiring intre ele.
- [x] Transformarea grupului muta coerent sprite-ul si toate colliderele sale.
- [x] O usa declarata ca entitate sau tile promovat poate schimba vizualul si colliderul prin acelasi state binding; inchis blocheaza, deschis nu.

Evidenta etapa 1 din 2026-09-04:

- `Collider2D`, `BoxCollider2D`, `CircleCollider2D` si `PolygonCollider2D` expun UiProperties validate; `SceneGeometry2D.TryCreateColliderGeometry` este singura conversie forma-locala -> geometrie/bounds scene-space.
- Mutatiile structurale, geometrice, de filtru si participare publica o singura notificare pe radacina scenei. Testele acopera setare directa, Aspect, sample Motion, transform de grup, remove/add si acelasi `ObservableValue<bool>` legat de vizualul si colliderul usii.
- Generatorul compileaza gardul/player-ul declarativ, gruparea cu sprite, DataContext-ul comun, `@set` discret pentru layer/mask `uint` si Motion geometric. Suita SourceGen este verde 498/498.
- Proiectul core este verde 3332/3332 cu RED-urile intentionate `CollisionStage=0` excluse; rularea nefiltrata ramane deliberat la 11 RED pentru lumea/query/input din etapele 2-4 si 3333 teste verzi. Testele focusate scena + etapa 1 sunt verzi 37/37.
- Paginile canonice pentru cele patru tipuri sunt in `docs-site/documentation/classes/`; manifestul JSON si prezenta sectiunilor obligatorii au fost validate.

## Etapa 2 - lumea de coliziune si indexul spatial

Decizie de arhitectura confirmata de utilizator la 2026-09-04: toate transformurile afine ale colliderelor se pastreaza exact. Narrow phase-ul foloseste fast-path-uri analitice/SAT pentru formele native si cercurile transformate prin similitudine, iar cercurile transformate in elipse prin scale neuniform/skew folosesc support mapping cu GJK/EPA; miscarea continua foloseste conservative advancement. Nu se resping si nu se aproximeaza tacit aceste transformuri, iar API-ul public ramane independent de algoritmii interni.

- [x] Radacina scenei detine/invalideaza indexul descendentilor; attach/detach/reparent nu lasa intrari zombie sau duplicate.
- [x] Se separa broadphase-ul de testele exacte de forma, astfel incat false-positive-urile broadphase sa nu devina rezultate publice.
- [x] Se implementeaza filtrarea layer/mask/trigger inainte de narrow phase unde este sigur.
- [x] Se implementeaza interogarile aprobate cu rezultate imutabile ce contin colliderul, entitatea/parintele relevant, punctul, normala, distanta/fraction si starea trigger unde se aplica.
- [x] `MoveAndCollide` trateaza miscarea continua/discreta exact cum a stabilit contractul si nu produce o bucla de simulare ascunsa.
- [x] Se instrumenteaza numarul de intrari, candidati, teste exacte, rebuild/update si timpul de query.
- [x] Se ruleaza corpusul determinist dupa warmup si se compara cu gate-ul numeric stabilit la etapa 0.

### Gate etapa 2

- [x] Rezultatele brute sunt comparate cu un oracle exhaustiv pe lumi mici randomizate cu seed fix.
- [x] Lumea mare atinge gate-ul masurat, iar mutarea unui actor nu reconstruieste geometria statica a satului.
- [x] Detach/dispose lasa indexul gol si nu pastreaza DataContext-uri sau noduri.

Evidenta etapa 2 din 2026-09-04:

- `CollisionWorld2D` este detinut de radacina `Scene2D`, reconciliaza incremental mutatiile si elimina referintele la subtree-uri detasate. `SparseCollisionGrid2D` ramane broadphase intern, iar `CollisionNarrowPhase2D` confirma fiecare rezultat prin analitic/SAT sau support mapping GJK/EPA; shape cast-ul foloseste conservative advancement si nu muta nodul.
- Testele `CollisionStageTwoContractTests` sunt verzi 8/8: oracle exhaustiv cu seed fix, false-positive AABB respins, elipsa afina si transform singular pastrate, tunneling blocat, filtrare inainte de narrow phase, lume unica, update incremental, imutabilitate si colectarea nodului/DataContext-ului dupa detach. Contractele de lume din etapa 0 sunt verzi; cele cinci RED-uri ramase apartin exclusiv tilemap/input din etapele 3-4.
- Proiectul core, cu RED-urile intentionate din etapa 0 excluse, este verde 3340/3340 cu 2 teste de conformance sarite. Build-ul core si benchmark-ul Release nu au warnings sau errors; verificarea `dotnet format --verify-no-changes` pentru fisierele etapei este verde.
- Runner-ul de productie si rezultatul sunt arhivate in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-collision-stage2/`. Dupa 8 warmup-uri si 48 de masuratori: `large-sparse` 107,6 us update P95 / 268,6 us query P95 / 1.425.544 bytes, `high-churn` 293,1 us / 100,2 us, `long-fence` 16,2 us / 51,4 us; toate au zero false-negative si trec pragurile inghetate.
- Cele cinci pagini API noi si pagina `Scene2D` sunt sincronizate in sursa canonica `docs-site/documentation/classes/`; manifestul JSON are toate intrarile, fara duplicate sau fisiere lipsa.

## Etapa 3 - collidere de tilemap

- [x] Se defineste adaptorul dintre metadatele/collider-descriptorii tilemap si lumea de coliziune; importerii nu introduc forme direct in backend.
- [x] Instanta promovata a unui tile poate detine collidere declarative si participa la inputul UI unificat; celula batch-uită ramane date pana cand este promovata.
- [x] Colliderele statice adiacente pot fi coalesced numai daca rezultatul este semantic identic pentru layer, mask, trigger, proprietati si debug identity.
- [x] Incarcarea/eliminarea unui chunk adauga/elimina numai colliderele lui; schimbarea unui tile actualizeaza regiunea dependenta.
- [x] Culling-ul vizual nu elimina colliderele active din lume. Streaming-ul de gameplay, daca va exista, ramane o politica explicita separata.
- [x] Se testeaza garduri pe granite de chunk, coordonate negative si modificari runtime.

### Gate etapa 3

- [x] Fixture-ul satului blocheaza playerul la garduri indiferent daca chunk-ul este vizibil sau nu.
- [x] Contoarele dovedesc update local al indexului la o mutatie de tile/chunk.
- [x] Promovarea unui tile interactiv nu dubleaza colliderul importat/static; politica de inlocuire/compozitie este explicita si testata.

Evidenta etapa 3 din 2026-09-04:

- `TileColliderDescriptor2D` si `TileColliderShape2D` formeaza contractul immutable de import pentru box/circle/polygon, filtre, trigger, proprietati si debug identity. `TileMap2D.Collision` il adapteaza in collidere scene-owned; backend-urile grafice nu primesc forme, iar celulele batch-uite nu devin `UIElement`.
- Adaptorul este cache-uit per chunk. Testul de mutatie pastreaza acelasi collider pentru chunk-ul nemodificat si masoara exact `IncrementalUpdateCount + 1`, `UpdatedEntryCount + 1`, fara cresterea `RebuildCount`. Coalescing-ul este testat separat pentru diferente de layer, mask, trigger, proprietati si debug identity.
- `TileInstance2D.Colliders` este content property declarativ, iar generatorul emite colectia reala. Instanta si collider-ele raman `SceneNode2D`/`UIElement` reale in arborele logic al scenei; conectarea geometriei lor la route-map si hit-test-ul user-like ramane gate-ul explicit al etapei 4.
- Fixture-ul cu garduri trece granite de chunk si coordonate negative, executa un frame cu toate chunk-urile culled vizual, apoi confirma atat blocarea prin `MoveAndCollide`, cat si raycast-ul pe chunk-ul indepartat. Promovarea testeaza compozitia implicita, inlocuirea explicita fara dublare si restaurarea descriptorului static la demotion.
- Verificarea curenta este verde: `CollisionStage=3` 5/5, suita SourceGen 499/499, proiectul core fara RED-urile intentionate `CollisionStage=0` 3345/3345 cu 2 teste de conformance sarite. `dotnet format --verify-no-changes`, validarea documentatiei/manifestului si `git diff --check` sunt verzi. Paginile canonice pentru descriptor, enum, definitie, instanta si harta sunt sincronizate in `docs-site/documentation/classes/`.

## Etapa 4 - hit-test geometric in pipeline-ul UI unificat

- [x] Se introduce un contract intern de input-subtree/geometric-hit-test: route builder-ul poate include descendentii scenei fara sa includa arbitrar toti copiii logici ai UI-ului, iar `HitTestService` poate delega geometria catre `RenderSurface2D`.
- [x] `RenderSurface2D` consolideaza conversiile root/control-local, scene/world si node-local folosind exact inversa transformului de randare/ViewBox; API review-ul stabileste calea publica prin care un handler comun obtine pozitia relativa.
- [x] `ElementInputRouteBuilder` adauga `RenderSurface2D -> Scene2D -> SceneNode2D` in acelasi `ElementInputRouteMap`, cu aceleasi handlers si enabled/visibility rules, fara a le adauga in `VisualChildren` sau layout.
- [x] `HitTestService` testeaza mai intai copiii vizuali de deasupra, apoi geometria specializata a scenei, apoi suprafata; rezultatul scenei contine elementul si `UiElementId` reale din route map.
- [x] Picking-ul foloseste bounds pentru broadphase si geometria efectiva pentru verdict, respectand visibility, clip, transform, layer si reverse draw order stabil.
- [x] Ancestor lookups pentru hover, pressed, cursor, focus, command si keyboard activation folosesc ruta unica atunci cand tinta este un nod logic; comportamentul controalelor vizuale existente ramane caracterizat.
- [x] `RoutedEventRouter`, `InputEvents`, `Handled`, `handledEventsToo`, enter/leave si `PointerCaptureManager` sunt reutilizate nemodificat semantic; eliminarea/ascunderea/reparentarea tintei invalideaza ruta si elibereaza captura sigur.
- [x] Se pastreaza coordonatele brute ale inputului si conversia scene/local evita rotunjirea prematura, chiar daca proprietatile legacy `MouseEventArgs.X/Y` raman compatibile.
- [x] Testele folosesc `Click`, `PressKey`/operatiile Servo relevante si nu seteaza direct proprietati ca substitut pentru input.

### Gate etapa 4

- [x] Un click user-like selecteaza nodul vizibil deasupra si ridica aceleasi evenimente tunnel/bubble prin nod, parintii scenei, `RenderSurface2D` si arborele UI.
- [x] Un overlay UI deasupra suprafetei continua sa primeasca input si scena nu il fura.
- [x] Focusul, command routing-ul, cursorul, `Handled`, captura, hover si eliminarea nodului folosesc aceleasi servicii UI si nu lasa stare blocata.
- [x] Contorul route-map demonstreaza ca schimbarile scenei invalideaza/reconstruiesc ruta numai cand structura/participarea la input se schimba, nu la fiecare cadru animat.

Evidenta etapa 4 din 2026-09-04:

- `IInputSubtreeHost` si `IGeometricHitTestHost` sunt contracte interne explicite. `ElementInputRouteBuilder` include numai subarborele scenei declarat de host, iar `HitTestService` pastreaza ordinea overlay vizual -> geometrie scena -> suprafata si returneaza elementul/`UiElementId` reale.
- `SceneHitTest2D` foloseste candidatii indexului spatial, verdictul geometric exact si reverse effective draw order. Colliderele statice batch-uite raman date; o entitate logica cu collidere directe este tinta reala, iar visibility, enabled, `IsHitTestVisible`, layer si filtrul comun sunt respectate.
- `RenderSurface2D.TryRootToScene`, `RenderSurface2D.SceneToRoot` si `MouseEventArgs.GetPosition` folosesc transformul de randare/ViewBox si ruta scenei fara rotunjire prematura. `MouseEventArgs.X/Y` raman compatibile prin rotunjirea coordonatelor root brute; transformurile neinversabile refuza conversia determinist.
- Hover, pressed, cursor, focus, input bindings, command routing, keyboard activation, drag si repeat-button cauta stramosii prin acelasi route-map. Regresia a expus faptul ca filtrarea initiala din `UIRoot` ignora modificarile handler-map; ruta este acum invalidata la adaugarea/eliminarea handlerelor, dar nu la esantioane geometrice/transform animate.
- Reproducerile originale si corpusul etapei sunt verzi 18/18 (`CollisionStage=0|CollisionStage=4`). Regresia pentru input, Servo si primitive este verde 306/306. Verificarea formatter-ului pe toate fisierele C# ale etapei, sectiunile documentatiei canonice, parsarea manifestului si `git diff --check` sunt verzi; indexul final are zero erori RoslynIndexer.

## Etapa 5 - documentatie si verificare

- [x] Se foloseste `writing-api-documentation` pentru toate tipurile, proprietatile, rezultatele si evenimentele publice.
- [x] Se documenteaza explicit sistemele de coordonate, contactul la margine, layer/mask, triggerele, limitele `MoveAndCollide`, ordinea picking si faptul ca scena foloseste acelasi input/routed-event system ca UI-ul.
- [x] Se documenteaza Aspect/Motion pe collidere, absenta Prism pe nodul nevizual si folosirea debug overlay-ului pentru efecte vizuale.
- [x] Se actualizeaza exemplele markup pentru gard/player si manifestul.
- [x] Se ruleaza testele focusate core pentru scena, input si coliziune, apoi corpusul/benchmark-ul spatial.
- [x] Se ruleaza scenariile reale MonoGame/WindowsDX si SDL_GPU; capturile sunt facute numai cu `Window.SaveScreenshot`.
- [x] Se reindexeaza, se ruleaza API Compat strict, validarea manifestului si `dotnet test .\Cerneala.slnx`.

Evidenta etapa 5 din 2026-09-04:

- Documentatia canonica din `docs-site/documentation/classes/` acopera cele 15 pagini publice relevante, inclusiv coordonate, contact la margine, filtrare bilaterala layer/mask, triggere, ordine determinista, limitele `MoveAndCollide`, inputul UI unificat si matricea Aspect/Motion/Prism. `docs/CernealaMarkupGuide.md` contine exemple declarative pentru casa din pereti separati, usa interactiva si player, folosind binding-uri tipate conforme sintaxei Cerneala.
- Manifestul contine 1.109 intrari, fara duplicate ori fisiere lipsa; toate cele 15 pagini obligatorii exista si au sectiunile `Definition`, `Examples` si `Remarks`. Cele doua exemple SourceGen ale documentatiei sunt verzi 2/2.
- Testele focusate curente pentru contractele de coliziune si input sunt verzi 259/259. Verificarea finala a rezolvat si trei regresii expuse de suita completa: dependenta interzisa `UI/Input -> UI/Controls`, asteptarile vechi de rebuild al rutei la mutatii pur geometrice si al doilea render produs de reconcilierea hover neconditionata.
- Corpusul spatial arhivat in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-collision-stage5/benchmark-results.json` trece toate pragurile cu zero false-negative: `large-sparse` 51,4 us update / 241,3 us query / 1.425.544 bytes, `high-churn` 472,8 us / 138,0 us / 403.040 bytes si `long-fence` 18,9 us / 91,1 us / 205.184 bytes.
- Scenariile reale WindowsDX si SDL_GPU verifica usa inchisa/deschisa, coliziunea, raycast-ul si round-trip-ul de coordonate. Capturile `collision-closed.png` si `collision-open.png` sunt produse exclusiv prin `Window.SaveScreenshot`; rapoartele masoara 5.600, respectiv 8.775 pixeli schimbati si marcheaza `CollisionContract`/`CoordinateRoundTrip` cu `PASS`.
- Build-urile Release ale ambelor smoke projects sunt verzi fara warnings/errors. API Compat strict trece cu `PermitUnnecessarySuppressions=false`; assembly-ul curent are 5.242.368 bytes si SHA-256 `1D7E8C683E6929BC994774230D77F9B34BDB8D5A73A9838282B0750DA7B0A1E9`. Formatter-ul scopat si `git diff --check` sunt verzi, iar ultimul index are zero erori si cele sapte warnings baseline.
- Prima rulare paralela a solutiei a expirat intr-un singur test LanguageServer dupa doua minute; testul a trecut izolat in 27 s. Rularea completa finala, serializata pentru a elimina contention-ul dintre host-urile de protocol, este verde: Language 185/185, LanguageServer 40/40, PreviewHost 13/13, SDL_GPU 119/119 cu 5 skip-uri native declarate, SourceGen 501/501, VisualStudio 47/47, core 3.363/3.363 cu 2 skip-uri de conformance declarate si Tetris 29/29.

## Definitia de done

- [x] #3-#6 sunt acoperite de contracte si teste RED devenite verzi.
- [x] Coliziunea si picking-ul impart transformul/bounds-urile scenei, dar nu sunt cuplate de backend sau layout UI.
- [x] Nodurile scenei participa complet la ruta UI comuna; nu exista event args, router, focus, capture sau command subsystem paralel.
- [x] Colliderele si colliderele tile-urilor promovate satisfac Aspect+Motion, iar Prism nu influenteaza niciun rezultat de coliziune/picking.
- [x] Broadphase-ul este justificat si masurat; rezultatele sunt verificate cu oracle exhaustiv.
- [x] Inputul user-like, captura si overlay-urile UI sunt verificate.
- [x] Documentatia, manifestul, API diff-ul, ambele backend-uri si suita completa sunt verzi.
