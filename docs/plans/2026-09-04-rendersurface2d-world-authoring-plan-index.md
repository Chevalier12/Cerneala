# Plan index: authoring de lumi 2D in `RenderSurface2D`

Status: propus, neinceput
Data: 2026-09-04

## Scop

Acest index transforma cerinta „padure, sat, casa, garduri cu hitbox-uri si playeri, declarate din markup” intr-o initiativa verificabila. Acopera explicit toate lipsurile #1-#16 discutate, fara sa transforme `RenderSurface2D` intr-un motor fizic, ECS sau editor de nivel.

## Baseline observat

- `RenderSurface2D.Scene` accepta un singur `Scene2D`; nodurile sunt copii logici, nu copii vizuali aranjati de layout.
- `Sprite2D` poate inregistra un sprite cu `Source`, `Destination`, `SourceRect`, `Tint`, `Origin`, `Flip`, `LayerDepth` si rotatie, dar nu rezolva imagini printr-un ID de resursa din markup.
- `SceneItems2D` realizeaza toate elementele si reconstruieste intreaga lista la orice schimbare a colectiei.
- `Scene2D` reda copiii in ordinea colectiei; nu exista straturi explicite, sortare Y sau transform comun de grup cu semantica de scena.
- `HitTestService` traverseaza astazi numai arborele vizual. Nodurile scenei sunt deja `UIElement`, dar sunt copii logici si nu au bounds de layout; inputul unificat cere o extensie geometrica a aceluiasi hit-test si includerea lor in aceeasi harta de rutare.
- `DrawSpriteBatch` exista deja si este implementat de backend-uri. Un tilemap trebuie sa-l reutilizeze inainte de a justifica modificari low-level.
- MonoGame/WindowsDX pastreaza damage/replay partial, iar SDL_GPU reinregistreaza tinta suprafetei cand versiunea cadrului se schimba. Paritatea functionala si vizuala este obligatorie; identitatea contoarelor interne de damage nu este.
- `Image` are deja `SourceResourceId`, rezolvare prin resursele radacinii, dependency tracking si cache cu ownership de backend. Aceasta cale trebuie reutilizata, nu duplicata.
- `SceneNode2D.Invalidate` proceseaza deja Aspect, iar testele existente demonstreaza Aspect si Motion pe proprietatile `Sprite2D`; `Sprite2D.Record` deschide explicit scope-ul Prism. Aceasta integrare nu exista automat pentru viitoarele noduri care inregistreaza copii sau batch-uri.
- `Tetrisish` este un consumator real al combinatiei `OnDraw` + `Scene`; compatibilitatea lui este un gate.

## Harta cerintelor #1-#16

| # | Capacitate | Plan proprietar |
|---|---|---|
| 1 | `TileMap2D` | [tilemap si scalare](./2026-09-04-rendersurface2d-tilemap-and-scale.md) |
| 2 | imagini incarcate direct din markup | [fundatia scenei](./2026-09-04-rendersurface2d-scene-foundation.md) |
| 3 | collidere declarative | [coliziuni si picking](./2026-09-04-rendersurface2d-collision-and-picking.md) |
| 4 | asocierea colliderului cu entitatea vizuala | [coliziuni si picking](./2026-09-04-rendersurface2d-collision-and-picking.md) |
| 5 | lume de coliziune si interogari spatiale | [coliziuni si picking](./2026-09-04-rendersurface2d-collision-and-picking.md) |
| 6 | conversie coordonate, picking si input pe nod | [coliziuni si picking](./2026-09-04-rendersurface2d-collision-and-picking.md) |
| 7 | culling | [tilemap si scalare](./2026-09-04-rendersurface2d-tilemap-and-scale.md) |
| 8 | chunking | [tilemap si scalare](./2026-09-04-rendersurface2d-tilemap-and-scale.md) |
| 9 | batching specializat pentru tile-uri | [tilemap si scalare](./2026-09-04-rendersurface2d-tilemap-and-scale.md) |
| 10 | actualizari incrementale `SceneItems2D` | [fundatia scenei](./2026-09-04-rendersurface2d-scene-foundation.md) |
| 11 | ordine, straturi si sortare Y | [fundatia scenei](./2026-09-04-rendersurface2d-scene-foundation.md) |
| 12 | transformari de grup | [fundatia scenei](./2026-09-04-rendersurface2d-scene-foundation.md) |
| 13 | animatie din sprite sheet | [animatie sprite](./2026-09-04-rendersurface2d-sprite-animation.md) |
| 14 | import Tiled/LDtk | [import, diagnostic si debug](./2026-09-04-rendersurface2d-import-debug-validation.md) |
| 15 | vizualizare debug | [import, diagnostic si debug](./2026-09-04-rendersurface2d-import-debug-validation.md) |
| 16 | validare | [import, diagnostic si debug](./2026-09-04-rendersurface2d-import-debug-validation.md) |

## Ordine si dependente

```text
fundatia scenei
   |-- tilemap si scalare
   |      |-- import, diagnostic si debug
   |      `-- coliziuni si picking
   |              `-- import, diagnostic si debug
   `-- animatie sprite
          `-- demonstratia finala din import/debug
```

- [ ] Se implementeaza mai intai [fundatia scenei](./2026-09-04-rendersurface2d-scene-foundation.md); ea stabileste resursele, transformul comun, bounds-urile si ordinea.
- [ ] [Tilemap si scalare](./2026-09-04-rendersurface2d-tilemap-and-scale.md) incepe numai dupa gate-ul fundatiei.
- [ ] [Coliziuni si picking](./2026-09-04-rendersurface2d-collision-and-picking.md) poate rula dupa fundatie, dar integrarea colliderelor din tilemap asteapta modelul de date al tilemap-ului.
- [ ] [Animatie sprite](./2026-09-04-rendersurface2d-sprite-animation.md) poate rula dupa fundatie si ramane independenta de importeri.
- [ ] [Import, diagnostic si debug](./2026-09-04-rendersurface2d-import-debug-validation.md) este ultimul plan: consuma contractele tilemap-ului si coliziunilor si livreaza demonstratia integrata.

## Contract arhitectural comun

- `RenderSurface2D` ramane controlul-gazda si proprietarul maparii view-box, invalidarii si inregistrarii cadrului.
- `Scene2D` ramane arbore logic de scena si nu este fortat prin layout/`ArrangedBounds` false.
- Conform deciziei utilizatorului din 2026-09-04, exista un singur sistem de input: nodurile scenei intra in acelasi `ElementInputRouteMap`/`UiInputTree`, folosesc aceleasi `InputEvents`, tunnel/bubble, `Handled`, hover, capture, focus, cursor si commands. `HitTestService` ramane intrarea unica si deleaga intern geometria scene-space catre o extensie specializata a `RenderSurface2D`.
- Un singur calculator intern de transform/bounds este folosit de redare, culling, picking si coliziuni. Patru implementari „aproape egale” ar fi un bug latent.
- Continutul static (tilemap) si entitatile dinamice (`SceneItems2D`, sprite-uri animate) raman cai distincte, compuse in aceeasi scena.
- `DrawSpriteBatch` este substratul initial pentru batch-urile de tile-uri. Backend-urile se schimba numai daca instrumentarea dovedeste ca acest contract este insuficient.
- Resursele imagine raman detinute de cache-ul/sesiunea grafica. Nodurile scenei nu elibereaza imagini partajate.
- Coliziunea este un subsistem de scena, nu o consecinta a pixelilor desenati si nu o functie a backend-ului grafic.
- Toate API-urile publice noi sunt platform-neutral si documentate canonic in `docs-site/documentation/classes/`.

## Matrice obligatorie Aspect/Motion/Prism

| Tinta planificata | Aspect | Motion | Prism | Limita |
|---|---:|---:|---:|---|
| `Sprite2D` si sprite animat | da | da | da | Prism proceseaza cadrul vizual curent |
| grup si strat `Scene2D` | da | da | da | Prism incadreaza subtree-ul, cu bounds agregate |
| `TileMap2D` si stratul sau adresabil | da | da | da | la nivel de harta/strat |
| tile individual promovat intr-un nod sparse | da | da | da | disponibil pentru orice celula, cost numai pentru tile-urile promovate |
| `SceneItems2D` | nu pe container | nu pe container | nu pe container | Aspect/Motion/Prism se declara pe nodurile din `@templates` |
| entitate/player/NPC compus din noduri | da | da | da | transformul comun actualizeaza desen, input si coliziune |
| collider | da | da | nu direct | nu emite pixeli; debug overlay-ul lui poate folosi Prism |
| debug overlay | da | da | da | nu schimba semantica gameplay |
| model tile/chunk nepromovat, collision world, importer, validator | nu | nu | nu | sunt date/servicii, nu `UIElement` |

- Aspect si Motion opereaza numai prin `UiProperty` publice cu invalidarea proprietarului corect; nu se marcheaza arbitrar toate datele ca animabile.
- O valoare Motion care schimba transformul, Y/order sau geometria colliderului actualizeaza in acelasi sample redarea, hit-test-ul, sortarea si indexul spatial relevante.
- Orice nod vizual adresabil care emite pixeli deschide/inchide Prism exception-safe cu bounds scene-space corecte. Grupurile/straturile pot incadra intervalul de comenzi al descendentilor; `SceneItems2D` nu primeste aceasta responsabilitate.
- Aplicarea Prism nu schimba coliziunea sau picking-ul geometric. Efectele vizuale si gameplay-ul raman contracte distincte.
- Prism map-level, layer-level si tile-level necesita caracterizarea scope-urilor imbricate pe ambele backend-uri; daca o combinatie nu este suportata de contractul Prism existent, generatorul emite diagnostic explicit in loc sa ignore unul dintre scope-uri.
- Orice celula poate fi promovata explicit/lazy intr-un nod scene-space sparse (numele public se ingheata prin API review). Celula promovata este scoasa din batch-ul static, pastreaza pozitia de ordine si poate primi Aspect/Motion/Prism, input, animatie si collider; celelalte tile-uri raman date compacte.
- Sintaxa `.crn` urmeaza gramatica reala Cerneala: `Aspect="$Resource"` sau property-element Aspect, Motion prin directivele existente `@animate with`, Prism prin `@prism { ... }`, iar materializarea prin `@templates`. Planul nu introduce sintaxa XML fictiva pentru Motion/Prism.

## Non-obiective comune

- motor rigid-body, gravitatie, forte, joints sau integrare fizica in timp;
- ECS, networking, replicare sau serializarea starii jocului;
- pathfinding/navigation; overlay-ul poate afisa o grila furnizata, dar nu inventeaza un sistem de navigatie;
- editor vizual de harti ori inlocuitor pentru Tiled/LDtk;
- suport implicit pentru toate orientările si extensiile formatelor externe;
- expunerea handle-urilor native, `SpriteBatch`, `GraphicsDevice` sau comenzilor SDL_GPU;
- rescrierea API-ului imperativ `OnDraw` ori migrarea fortata a consumatorilor existenti.
- un al doilea router sau o a doua familie publica de evenimente pentru „game input”; geometria difera, sistemul UI/input nu.
- materializarea automata a fiecarui tile ca `UIElement`; numai tile-urile promovate explicit/lazy primesc nod.

## Politica de platforma si verificare

- [ ] Fiecare etapa care modifica C# sau un proiect ruleaza imediat indexarea:
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.
- [ ] Testele unitare/core si testele generatorului trec pe fiecare etapa relevanta.
- [ ] Scenariile reale de randare trec atat pe MonoGame/WindowsDX, cat si pe SDL_GPU; diferentele sunt judecate dupa contract si dif-uri vizuale, nu dupa presupunerea ca backend-ul vechi are automat dreptate.
- [ ] Capturile Cerneala sunt produse exclusiv prin `Window.SaveScreenshot`.
- [ ] Gate-ul public API foloseste `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask`, cu verificarea numelor parametrilor, si arhiveaza raportul; o simpla compilare nu tine loc de API diff.
- [ ] Pentru orice API public/protected se foloseste skill-ul `writing-api-documentation`, se actualizeaza sursa canonica din `docs-site/documentation/classes/` si `docs-site/documentation/manifest.json`.
- [ ] Se ruleaza validarea manifestului:
  `dotnet test .\tests\Cerneala.Tests.VisualStudio\Cerneala.Tests.VisualStudio.csproj --filter "FullyQualifiedName~Stage6ReleaseHarnessTests.ApiDocumentationManifestIsValidAndReferencesExistingFiles"`.
- [ ] Se ruleaza gate-ul complet:
  `dotnet test .\Cerneala.slnx`.

## Gate final al initiativei

- [ ] Un exemplu determinist incarcat din markup afiseaza o harta cu padure, sat, casa, garduri si player, fara cate un `UIElement` per tile.
- [ ] Camera elimina chunk-urile invizibile din munca de inregistrare; contoarele dovedesc culling, reutilizarea batch-urilor si actualizarile limitate la chunk-urile schimbate.
- [ ] Gardurile au collidere declarative/importate, playerul este interogat prin lumea de coliziune, iar picking-ul geometric livreaza nodul scenei prin acelasi pipeline UI de routed events, focus, capture si commands.
- [ ] Playerul isi schimba starile de animatie si flip-ul folosind ceasul cadrului, fara timer sau bucla de randare paralela.
- [ ] Matricea Aspect/Motion/Prism este demonstrata prin markup si teste combinate pe fiecare tinta marcata „da”; schimbarile Motion raman coerente cu culling, ordering, hit-test si coliziune.
- [ ] Cel putin un tile individual este promovat dintr-un chunk batch-uit si foloseste Aspect, Motion, Prism, input si animatie fara desen dublu; contoarele arata ca restul hartii ramane batch-uit.
- [ ] Casa din exemplu are o usa desenata declarativ: varianta decorativa poate ramane tile obisnuit, iar varianta interactiva este un tile promovat sau o entitate din `SceneItems2D.@templates`, cu stare inchis/deschis, input UI unificat si collider activ numai cand este inchisa.
- [ ] Fisierele Tiled si LDtk din subsetul declarat produc acelasi model core sau diagnostice explicite; campurile nesuportate nu sunt ignorate tacit.
- [ ] Overlay-ul de debug poate arata collidere, chunk-uri, coordonate tile, ordine si o grila externa de navigatie fara sa schimbe rezultatele picking/coliziune.
- [ ] Compatibilitatea `Tetrisish` pentru `OnDraw` + `Scene` este demonstrata prin teste si scenariul runtime existent.
- [ ] Toate planurile dependente sunt complet bifate, API diff-ul este revizuit, documentatia este sincronizata, testele complete si conformance-ul ambelor backend-uri sunt verzi.
