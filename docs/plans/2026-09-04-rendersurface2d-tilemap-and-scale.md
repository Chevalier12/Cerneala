# Plan: `TileMap2D`, culling, chunking si batching

Status: finalizat
Acopera: #1, #7, #8, #9
Depinde de: `2026-09-04-rendersurface2d-scene-foundation.md`

## Rezultat urmarit

O harta mare, stratificata, poate fi declarata/legata in scena fara un `UIElement` permanent per tile. Numai chunk-urile vizibile sunt inregistrate, batch-urile neschimbate sunt reutilizate, iar orice celula poate fi promovata explicit/lazy intr-un nod sparse cu Aspect/Motion/Prism si lifecycle propriu. Nodul promovat este punctul de integrare pentru input geometric/picking, animatie de cadre si collider, implementate de planurile dependente dedicate.

## Ownership si limite

- Modelul tilemap si nodul de scena apartin core-ului platform-neutral, probabil sub `UI/Controls/` sau un namespace Scene2D stabilit la etapa 0.
- Inregistrarea reutilizeaza `DrawSpriteBatch`, `DrawSprite2D`, `DrawCommandListBuilder` si dependency tracking-ul existent.
- Backend-urile MonoGame si SDL_GPU consuma aceleasi comenzi. Nu se adauga o comanda nativa tilemap si nu se expun handle-uri pana cand un benchmark instrumentat demonstreaza ca batch-ul actual incalca gate-ul.
- Importul Tiled/LDtk nu apartine acestui plan; el va produce modelul core definit aici.
- Entitatile independente nu sunt tile-uri. Playerii, NPC-urile si proiectilele raman noduri produse prin `SceneItems2D.@templates`/sprite-uri; un tile promovat ramane o celula adresata din tilemap, chiar daca devine animat sau interactiv.
- Un tile special nu obliga intreaga harta sa devina arbore UI: numai celulele promovate au nod scene-space si lifecycle propriu.

## Contract propus pentru auditul API

- Un model de date independent de backend descrie dimensiunea tile-ului, tileset-urile/atlasurile, straturile si chunk-urile.
- ID-ul `0`/valoarea goala, limitele ID-urilor, sursa atlasului si flip-urile au semantica explicita si validabila.
- Datele publicate sunt imutabile sau versionate. Mutatia fara schimbare de versiune, invizibila cache-ului, este interzisa.
- Chunk-urile pot avea dimensiuni configurabile in model, dar runtime-ul nu inventeaza chunk-uri suprapuse.
- Un strat are vizibilitate, offset/transform, opacity/tint, ordine si optional metadate; aceste valori se compun cu contractul comun de scena.
- Tilemap-ul raporteaza bounds finite sau bounds pe chunk-uri pentru harti infinite/sparse.
- Harta si fiecare strat adresabil sunt noduri high-level compatibile Aspect/Motion/Prism. Modelul de strat ramane separat de nodul lui de prezentare.
- Orice celula poate avea o singura instanta promovata, identificata stabil prin harta/strat/coordonata. Cat timp este promovata, celula este exclusa din batch-ul static si instanta ei ocupa exact slotul semantic de ordine al tile-ului.
- Instanta promovata reutilizeaza tile ID/source rect/atlas implicit, dar poate suprascrie proprietatile vizuale si adauga Aspect/Motion/Prism. Demotion/replacement are lifecycle explicit; inputul geometric/picking, animatia de cadre si colliderul se ataseaza prin API-urile planurilor dependente.
- Sintaxa noua pentru colectia de instante se ingheata numai prin teste SourceGen pe gramatica `.crn` reala; Aspect foloseste formele existente, Motion `@animate with`, Prism `@prism { ... }`, iar `@templates` ramane calea pentru colectii de entitati.

## Observabilitate obligatorie

Inainte de optimizare se expun intern, prin test hooks/Detective snapshot, cel putin:

- chunk-uri totale, candidate si vizibile;
- tile-uri candidate si desenate;
- batch-uri construite, reconstruite si reutilizate;
- comenzi de desen emise per atlas/strat;
- bytes/obiecte retinute de cache-ul tilemap;
- invalidari cauzate de o mutatie de tile/chunk.
- instante promovate vizibile/culled, promovari/demotari si batch splits produse de ordering sau scope-uri Prism individuale.

Aceste contoare sunt contracte de test, nu promisiuni publice de telemetry. Timpii CPU si alocarile se masoara dupa warmup; planul nu inventeaza un prag numeric pe care utilizatorul nu l-a cerut.

## Etapa 0 - model, fixture si baseline de cost

- [x] Se inventariaza semantic tot lantul `DrawSpriteBatch` pana la ambele backend-uri si testele de lifecycle; se noteaza orice caller care ar fi afectat de modificarea comenzii.
- [x] Se defineste prin teste/API review modelul minim pentru tileset, tile, layer si chunk, inclusiv harta finita si sparse/infinita, atlasuri multiple, flip, tile gol si proprietati opace pentru importeri.
- [x] Se creeaza un fixture determinist de „sat” suficient de mare ca sa aiba chunk-uri vizibile si invizibile, fara asset-uri cu licenta incerta.
- [x] Se adauga teste RED pentru markup `TileMap2D`, rezolvarea atlasului, ordinea straturilor si desenarea source rect-urilor corecte.
- [x] Se adauga teste RED SourceGen/runtime pentru Aspect+Motion+Prism pe harta, strat si un tile promovat; sintaxa testata este derivata din `Tetrisish/MainWindow.crn` (`Aspect`, `@animate with`, `@prism`) si nu din pseudo-XML inventat.
- [x] Se adauga teste RED pentru promovare/demotare: zero desen dublu, aceeasi ordine, coordonate stabile, tile gol/inexistent, duplicate si revenirea corecta in batch.
- [x] Se caracterizeaza scope-urile Prism imbricate existente si se adauga RED pentru map -> layer -> tile promovat pe MonoGame si SDL_GPU; combinatiile interzise trebuie sa aiba diagnostic generator, nu efect pierdut tacit.
- [x] Se adauga teste RED pentru culling si invalidare locala, bazate pe contoare; esecul trebuie sa arate munca in exces, nu doar o imagine diferita.
- [x] Se adauga benchmark-uri baseline pentru o harta calda/statica, pan al camerei si mutatie de chunk; se masoara CPU, alocari, comenzi si rebuild-uri separat.
- [x] Se stabileste un buget de memorie si un gate numeric de performanta din baseline-ul masurat inainte de a declara optimizarea terminata; valoarea si hardware-ul sunt arhivate in `benchmarks/Cerneala.Benchmarks/results/`.

### Gate etapa 0

- [x] Modelul poate reprezenta fixture-ul finit si unul sparse fara dependenta de Tiled/LDtk.
- [x] Testele RED disting corect lipsa tilemap-ului, culling-ului si invalidarii locale.
- [x] Contractul stabileste forma nodului de strat si a instantei promovate, identitatea celulei, ordering-ul si lifecycle-ul fara a materializa toate tile-urile.
- [x] Baseline-ul, warmup-ul, configuratia si contoarele sunt reproductibile.

Dovezi etapa 0: `benchmarks/Cerneala.Benchmarks/results/2026-09-04-tilemap-baseline/` contine baseline-ul masurat, configuratia/hardware-ul, inventarul semantic, API review-ul, gate-urile numerice si rularile RED arhivate.

## Etapa 1 - modelul core si markup-ul `TileMap2D`

- [x] Se implementeaza tipurile imutabile/versionate stabilite si validarea structurala minima necesara constructiei sigure.
- [x] Se implementeaza `TileMap2D` ca `SceneNode2D` care consuma modelul, nu ca o colectie de controale/noduri per tile.
- [x] Se implementeaza prezentarea high-level a stratului ca nod logic/adresabil si colectia sparse de instante promovate; numele finale sunt aprobate prin API review, nu copiate din exemple speculative.
- [x] Se integreaza `SourceResourceId`/resursele atlas in calea comuna a fundatiei; atlasurile multiple sunt grupate determinist.
- [x] Generatorul `.crn` primeste sintaxa minima pentru instantierea/legarea tilemap-ului si proprietatilor sale; datele voluminoase nu sunt expandate in mii de expresii C# daca pot fi o resursa/model.
- [x] Generatorul foloseste sintaxa Cerneala existenta pentru Aspect/Motion/Prism pe harta, strat si instanta promovata si produce diagnostice cu locatie pentru coordonate/straturi duplicate sau invalide.
- [x] Bounds-urile tilemap-ului si straturilor folosesc contractul scene-space comun.
- [x] Proprietatile aprobate de transform/prezentare sunt `UiProperty`: Aspect le poate furniza, Motion le poate esantiona, iar fiecare sample invalideaza render/bounds/order exact unde trebuie.
- [x] Prism pe harta/strat/instanta promovata foloseste bounds scene-space corecte si incadreaza numai comenzile tintei.
- [x] Se reindexeaza si se ruleaza testele core si SourceGen focusate.

### Gate etapa 1

- [x] Fixture-ul de sat este randat corect din markup, cu atlasuri, flip-uri, straturi si tile-uri goale.
- [x] Numarul nodurilor scenei nu creste proportional cu numarul tile-urilor.
- [x] Un tile promovat arata identic cu varianta batch-uită fara efecte, apoi raspunde individual la Aspect/Motion/Prism.

Dovezi etapa 1: testele focusate sunt verzi (15 core pentru `TileMapStage=0|1`, 6 SourceGen pentru `TileMapStage=0|1`, plus 38 regresii Scene2D/resurse). Factory-ul generat construieste si inregistreaza modelul legat din markup cu doua atlasuri, flip H/V, doua straturi si tile gol; fixture-ul de 36.864 celule pastreaza trei noduri de strat plus numai promotiile sparse; testul de promovare verifica atlas/source rect/flip identic, iar testul Aspect/Motion/Prism verifica scope-ul individual de 16x16 cu o singura comanda tinta. Cele 13 pagini API canonice si cele 13 intrari noi de manifest au trecut auditul si testul oficial de manifest.

## Etapa 2 - chunking si invalidare locala

- [x] Se construieste cache-ul per chunk, strat, atlas si segment de ordine, cu chei ce includ toate proprietatile care schimba geometria/comanda.
- [x] Datele imutabile/versionate invalideaza exact chunk-urile afectate; schimbarea tileset-ului invalideaza numai dependentii lui.
- [x] Promovarea/demotarea suprima/reintroduce exact celula din geometria chunk-ului si reconstruieste numai segmentul dependent.
- [x] Schimbarea transformului/camerei nu reconstruieste geometria statica a chunk-urilor.
- [x] Attach/detach, schimbarea radacinii grafice, device loss si disposal elibereaza cache-ul detinut fara a elibera atlasuri partajate.
- [x] Se testeaza mutatii la marginea chunk-ului, dimensiuni partiale, coordonate negative, straturi goale si harti sparse.

### Gate etapa 2

- [x] O schimbare intr-un tile reconstruieste exact chunk-ul/segmentele dependente, fapt dovedit de contoare.
- [x] Un cadru neschimbat dupa warmup reutilizeaza toate batch-urile si nu face alocari per tile; orice alocare ramasa este masurata si explicata.
- [x] N instante promovate creeaza O(N) noduri, nu O(numarul total de tile-uri), iar demotion elibereaza handlers, Motion/Prism state si referintele de integrare detinute de nod; lifecycle-ul concret pentru picking/collider este verificat de planul dependent.

Dovezi etapa 2: `TileMap2DCacheContractTests` acopera 7 scenarii verzi: identitatea batch-urilor este reutilizata integral dupa warmup si dupa pan/transform; o schimbare reala de flip intr-un tile si o schimbare reala de source rect/version in tileset reconstruiesc numai segmentul dependent; promovarea/demotarea modifica numai chunk-ul proprietar; cache-ul este eliberat la backend-state loss, detach/disposal si schimbarea radacinii fara a dispune atlasurile partajate; coordonatele negative, chunk-urile partiale/de granita, straturile goale si harta sparse raman stabile. Masurarea cu `GC.GetAllocatedBytesForCurrentThread` compara 4 cu 4.096 tile-uri intr-un singur chunk cached si impune o diferenta de maximum 8.192 B, deci alocarea calda ramasa este cost fix de frame/lista de comenzi, nu per tile. Testul cu 4.096 tile-uri si 32 promovari demonstreaza 32 noduri sparse; demotion scoate nodul din owner/root, ruleaza detach-ul handlerelor generate, anuleaza si dreneaza Motion, elimina instanta Prism si lasa nodul in afara arborelui logic/rutelor de integrare curente. Verificarea concreta a picking-ului si colliderului ramane in planul dependent. Verificare curenta: build core cu 0 warnings/0 errors, 22/22 teste `TileMapStage=0|1|2`, 6/6 teste SourceGen `TileMapStage=0|1` si 68/68 regresii RenderSurface2D/Scene2D/Prism/Motion.

## Etapa 3 - culling inainte de emiterea comenzilor

- [x] ViewBox-ul si transformul comun produc regiunea vizibila conservatoare in coordonate de tilemap.
- [x] Intersectia chunk/bounds elimina chunk-urile invizibile inainte de enumerarea tile-urilor si construirea comenzilor.
- [x] Rotatia/scalarea, clip-ul, viewport-ul gol si transformul neinversabil au comportament sigur: niciun continut vizibil nu este eliminat fals.
- [x] Harta sparse foloseste indexul chunk-urilor, nu o iteratie prin dreptunghiul infinit.
- [x] Instantele promovate sunt culled individual; bounds-ul efectiv Prism este caracterizat astfel incat efectele vizibile la marginea viewport-ului sa nu fie eliminate fals.
- [x] Se testeaza pan/zoom la granite de pixel si chunk pentru a evita popping sau gauri.

### Gate etapa 3

- [x] Pentru fixture-ul mare, contoarele arata ca numarul de chunk-uri/tile-uri procesate urmareste viewport-ul, nu dimensiunea hartii.
- [x] Capturile deterministe inainte/dupa pan nu contin tile-uri lipsa la margini pe niciun backend.
- [x] Un tile promovat cu Motion/Prism intra si iese din viewport fara popping, desen dublu sau stare pierduta.

Dovezi etapa 3: contextul scenei inverseaza conservator lantul ViewBox/map/layer/tile, intoarce `Unknown` pentru transformuri neinversabile si `Empty` pentru viewport gol. Indexul spatial retained interogheaza bucket-uri construite o singura data per model si face intersectia exacta inainte de a atinge tile-urile; fixture-ul sparse de 258 chunk-uri produce maximum 4 candidate, exact 1 vizibil si 64 tile-uri candidate/desenate. Cele 4 teste `TileMapStage=3` acopera rotatie/scalare, viewport gol, transform neinversabil, coordonate negative/sparse, pan/zoom la 15,5/16 pixeli si culling individual al unui tile promovat cu Blur Prism si Motion activ; scope-ul Prism ramane 16x16, iar starea supravietuieste iesirii/reintrarii fara desen dublu. Verificarea curenta este verde: 26/26 teste core `TileMapStage=0|1|2|3`, 6/6 teste SourceGen `TileMapStage=0|1` si 68/68 regresii RenderSurface2D/Scene2D/Prism/Motion. Harness-urile native WindowsDX/MonoGame si SDL_GPU au produs prin `Window.SaveScreenshot` cate doua capturi in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-tilemap-stage3/`; fiecare rulare valideaza toti pixelii, exact trei granite vizibile si schimbarea primei benzi dupa pan, iar ambele build-uri au 0 warnings/0 errors.

## Etapa 4 - batching si masurare backend

- [x] Tile-urile vizibile consecutive cu acelasi atlas, sampling/address mode si ordine sunt emise prin `DrawSpriteBatch` existent.
- [x] Separarea batch-urilor pastreaza ordinea semantica; batching-ul nu muta tile-uri prin straturi doar pentru a reduce draw calls.
- [x] Un tile promovat este extras din batch; comenzile lui individuale si scope-ul Prism sunt inserate in pozitia semantica, iar batch-ul se sparge numai cat cere ordinea.
- [x] Se ruleaza benchmark-urile dupa warmup si se compara CPU, alocari, rebuild-uri si comenzi cu baseline-ul etapei 0.
- [x] Se profileaza separat MonoGame/WindowsDX si SDL_GPU. O diferenta de damage tracking este raportata, nu mascata.
- [x] Numai daca gate-ul numeric ramane ratat si profilarea atribuie costul comenzii existente se propune, intr-un amendament separat cu caller inventory, o schimbare low-level. (Nu a fost necesar: gate-ul core a trecut, fara schimbarea comenzii sau a ownership-ului backend.)

### Gate etapa 4

- [x] Benchmark-urile ating gate-ul numeric stabilit si rezultatele sunt arhivate cu hardware, runtime, warmup si commit.
- [x] Numarul de comenzi este proportional cu segmentele atlas/ordine vizibile, nu cu tile-urile individuale.
- [x] Contoarele raporteaza separat batch splits pentru instante promovate/Prism; zero instante promovate pastreaza calea compacta initiala.
- [x] Niciun backend nu schimba ownership-ul resurselor sau contractul public de desen.

Dovezi etapa 4: cele 29 de teste core `TileMapStage=0|1|2|3|4` sunt verzi si includ batching determinist pe atlas, ordering intre straturi si slotul exact al unui tile promovat cu Prism. `benchmarks/Cerneala.Benchmarks/results/2026-09-04-tilemap-stage4/optimized.json` arhiveaza toate cele 19 gate-uri verzi dupa 64 warmup/512 iteratii: warm static P95 458,8 us si 15.819 B/op cu 36 comenzi/36 reutilizari; pan P95 189,3 us si 18.834 B/op cu 48 comenzi/0 rebuild; mutatie locala P95 803,2 us si 198.923 B/op cu exact 1 rebuild, 35 reutilizari si 1 invalidare. `backend-profile.json` arhiveaza 12 warmup/96 cadre separat pe WindowsDX si SDL_GPU: cadrul static reutilizeaza 36 segmente pe ambele; WindowsDX nu rerasterizeaza cadrul static, dar ViewBox-ul context-sensitive forteaza damage complet de 393.216 pixeli la pan/mutatie, in timp ce SDL_GPU rerandeaza explicit intreaga suprafata offscreen la schimbarea versiunii si nu expune damage rectangle. Nu s-a adaugat comanda tilemap nativa si nu s-a schimbat ownership-ul resurselor sau contractul public de desen.

## Etapa 5 - conformance, documentatie si integrare

- [x] Se adauga scene conformance pentru atlasuri multiple, opacity/tint, flip-uri, transformuri, straturi, chunk edges, pan/zoom si tile promovat cu Aspect+Motion+Prism.
- [x] Mismatch-urile intre backend-uri sunt rezolvate din contract; golden-urile se actualizeaza numai daca schimbarea este justificata.
- [x] Capturile aplicatiei folosesc exclusiv `Window.SaveScreenshot`.
- [x] Se foloseste `writing-api-documentation`; se documenteaza modelul, `TileMap2D`, stratul adresabil, promovarea/demotarea, sintaxa `.crn` reala, costul batch splits, mutatia/versionarea si limitele.
- [x] Se actualizeaza manifestul si se ruleaza testul lui.
- [x] Se reindexeaza, se ruleaza API Compat strict, benchmark-urile, suitele backend si `dotnet test .\Cerneala.slnx`.

Stare curenta etapa 5: scenele/capturile de conformance, comparatia backend, documentatia canonica, manifestul, API Compat strict, benchmark-urile, suitele backend si suita completa serializata sunt verzi. Dupa inchiderea workload-ului extern, rularea standard normal-priority a trecut toate cele 19 gate-uri: warm static P95 458,8 us, pan P95 189,3 us si mutatie de chunk P95 803,2 us fata de pragul 1.135 us, cu alocarile si contoarele structurale asteptate. Profilul WindowsDX/SDL_GPU a fost regenerat in aceeasi stare idle. Istoricul masuratorilor contaminate si rezultatul final sunt arhivate in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-tilemap-stage5/benchmark-final-verification.md`.

Dovezi etapa 5: fixture-ul nativ cu doua atlasuri, doua straturi, opacity/tint, flip-uri, transformuri, chunk edge, pan/zoom si tile promovat cu Aspect+Motion+Prism a produs exclusiv prin `Window.SaveScreenshot` trei capturi 800x525 pe fiecare backend. Comparatia fizica din `comparison.json` are zero pixeli nerezolvati; cele 8.525/8.525/8.235 diferente ramase sunt limitate la acoperirea raster de un pixel pe muchii in ambele imagini, iar `GoldenUpdated` este `false`. Mismatch-urile reale au fost reparate in ownerii de contract: transformul analizat Prism, restaurarea target-ului MonoGame si compozitia de grup/state persistence peste host ranges in SDL_GPU. Documentatia canonica si cele 13 intrari de manifest sunt sincronizate, iar testul oficial de manifest trece 1/1. Build-ul Release are zero warnings/errors; API Compat strict trece cu exact 13 tipuri tilemap aprobate si `PermitUnnecessarySuppressions=false`; benchmark-ul core are 19/19 gate-uri verzi; profilul nativ WindowsDX/SDL_GPU a iesit 0; SDL_GPU are 119 teste verzi si 5 skip-uri conditionate. `dotnet test .\Cerneala.slnx --no-build --no-restore -m:1` trece cu 4.255 teste verzi si 7 skip-uri conditionate, fara esec.

## Definitia de done

- [x] #1, #7, #8 si #9 sunt implementate si masurate.
- [x] Harta mare nu creeaza un nod per tile, nu enumera harta intreaga per cadru si nu reconstruieste batch-uri statice la pan.
- [x] Orice tile poate fi promovat sparse si foloseste individual Aspect/Motion/Prism fara desen dublu sau pierderea ordering-ului; nodul promovat furnizeaza lifecycle-ul si punctul de integrare pentru input geometric/picking, animatie de cadre si collider, ale caror implementari apartin explicit planurilor dependente `rendersurface2d-collision-and-picking` si `rendersurface2d-sprite-animation`.
- [x] Mutatiile locale, lifetime-ul si device loss sunt acoperite.
- [x] Paritatea vizuala este demonstrata pe MonoGame/WindowsDX si SDL_GPU.
- [x] API-ul, markup-ul, documentatia, manifestul, API diff-ul si suita completa sunt verzi.
