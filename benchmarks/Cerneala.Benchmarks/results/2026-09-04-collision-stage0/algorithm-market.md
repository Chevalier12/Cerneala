# Piața algoritmilor: broadphase și teste exacte pentru scena 2D

Acoperirea cercetării:

- Track research: au fost căutate lucrările originale și benchmark-urile pentru exhaustive scan, grid/hash ierarhic, quadtree/loose quadtree, k-d/BSP, SAP, R/R*-tree, BVH/DBVT, hibrizi CPU și LBVH GPU. După apariția hibridului k-d + SAP, două runde consecutive suplimentare nu au produs o familie credibilă nouă pentru ținta normalizată; au produs doar variante paralele, hardware și structuri pentru alte domenii.
- Track production: au fost inspectate implementări din Box2D, Chipmunk2D, Bullet, Jolt, Godot și Parry/Rapier. Două runde consecutive după setul inițial nu au produs o familie nouă; au reconfirmat grid/hash, SAP și BVH dinamic.
- Au fost evaluate complet 16 opțiuni din 6 familii: exhaustiv, grid/hash, subdivizare spațială, sweep/range, arbori de indexare și ierarhii de volume.
- Gate-ul exhaustiv a fost satisfăcut: minimum 10 candidați, minimum 3 familii și două runde saturate pe fiecare track.

## 1. Contractul țintei

Intrarea este un set incremental de AABB-uri provenite din collidere box, circle și polygon convex, cu majoritatea geometriei statice (tile-uri, garduri, clădiri) și un procent mic sau moderat de actori mutați. Ieșirea broadphase trebuie să fie un superset fără false-negative al candidaților; verdictul public este întotdeauna dat de narrow phase exact. Interogările sunt overlap, raycast și shape cast pentru `MoveAndCollide`.

Gate-uri obligatorii:

- rezultate exacte după narrow phase, inclusiv contact la margine și mișcare continuă;
- determinism prin sortarea finală după fracție/distanță și ordinal de atașare stabil;
- update incremental, fără rebuild al geometriei statice la mișcarea unui actor;
- CPU managed, fără dependență de backend grafic, GPU sau simulare fizică;
- coordonate negative și lume sparse fără domeniu finit obligatoriu;
- implementare clean-room; sursele externe sunt dovezi, nu cod copiat.

Preferințe: alocări mici după warmup, structură simplă și observabilă, integrare directă cu `Scene2D`. Necunoscute: distribuția exactă a jocurilor viitoare și raportul real între collidere foarte mari și foarte mici. De aceea strategia rămâne internă.

Corpusul local este determinist (`seed = 0x0C0111D3`) și include: 128 obiecte mici, 12.000 obiecte sparse, gard de 4.096 segmente, 2.304 tile-uri care se ating la colțuri/muchii, 2.048 obiecte cu overlap inițial, mișcare rapidă și 4.096 obiecte cu 25% churn. Fișierul brut este [baseline.json](baseline.json).

## 2. Matricea de evaluare

| Dimensiune | Puncte |
|---|---:|
| Corectitudine, robustețe numerică și determinism posibil | 22 |
| Potrivire cu măsurătorile corpusului Cerneala | 22 |
| Costul update-urilor dinamice | 14 |
| Scalare și cost query | 12 |
| Memorie și alocări | 10 |
| Integrare și mentenanță în Cerneala | 12 |
| Maturitate și licențiere | 8 |
| **Total** | **100** |

Hard gate-urile de mai sus nu pot fi compensate prin scor.

## 3. Comparație

Scorurile sunt judecăți inginerești susținute de surse și de corpusul local; numai numerele marcate ca măsurători provin din benchmark.

| Candidat | Familie | Scor | Hard gate | Compromisul decisiv |
|---|---|---:|---|---|
| Scanare exhaustivă AABB | Exhaustiv | 54 | Respins | Exact și trivial, dar query-ul mare este O(n). |
| Grid uniform dens | Grid | 61 | Respins | Memoria cere un domeniu finit și explodează în lumea sparse. |
| Grid uniform sparse / spatial hash | Hash | 89 | Trecut | Cel mai bun query măsurat pe lumile sparse; sensibil la mărimi foarte variate. |
| Hash/grid ierarhic | Hash ierarhic | 84 | Trecut | Gestionează scări variate, dar are lookup și deduplicare cross-level mai complexe. |
| Quadtree regional | Subdivizare | 70 | Trecut | Bun pe distribuții locale; rebuild-ul naiv este inacceptabil la churn. |
| Loose quadtree | Subdivizare | 75 | Trecut | Update mai ieftin, dar mai mulți candidați și tuning al looseness-ului. |
| k-d tree | Subdivizare | 66 | Respins pentru implementarea inițială | Query bun, update/rebalansare slabă pentru actorii mutați. |
| BSP semi-adjusting | Subdivizare | 73 | Trecut | Adaptiv, dar mult mai greu de implementat și verificat decât cere ținta 2D. |
| Incremental sweep-and-prune | Sweep | 79 | Trecut | Excelent cu coerență temporală; degradare la teleport/churn și clustere. |
| R-tree | Indexare | 67 | Trecut | General și matur, dar split/reinsert nu aduce un avantaj local demonstrat. |
| R*-tree | Indexare | 69 | Trecut | Calitate mai bună a arborelui cu cost de update mai mare. |
| Dynamic AABB tree / DBVT | BVH | 86 | Trecut | Update excelent; implementarea trebuie balansată ca să evite build-ul patologic pe gard. |
| BVH static/refit | BVH | 76 | Trecut condiționat | Foarte bun pentru static, dar singur nu rezolvă churn-ul fără rebuild/refit. |
| Wide BVH cu separare static/dynamic | BVH hibrid | 88 | Trecut | Cel mai puternic model production, dar prea multă complexitate pentru prima versiune. |
| k-d + SAP / range+interval+SAP | Hibrid sweep/tree | 90 | Trecut | Cel mai puternic absolut pe workload-uri masive, dar integrarea și SIMD depășesc scopul. |
| GPU grid/LBVH/SAP | GPU | 58 | Respins | Leagă coliziunea de hardware/backend și plătește rebuild/transfer. |

## 4. Dovezi pe candidat

[ ] **Scanare exhaustivă AABB**

Familie: testează fiecare AABB sau fiecare pereche, fără index.

Surse: [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf) include brute force ca baseline comun.

Licență: descriere matematică; nu se adoptă sursă.

Garanții și calitate: nu poate rata candidatul; ordinea este trivial deterministă.

Cost: O(n) per query și O(n²) pentru toate perechile. Măsurat local: 62.569 us P95 pentru 512 query-uri în lumea sparse de 12.000 obiecte.

Potrivire: oracle excelent pentru teste mici, nu broadphase production.

Verdict: respins ca index; păstrat ca oracle.

[ ] **Grid uniform dens**

Familie: discretizează un domeniu finit în celule indexate direct.

Surse: clasificarea și limitele grid-urilor apar în [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf).

Licență: nu se adoptă sursă.

Garanții și calitate: fără false-negative dacă un obiect intră în fiecare celulă suprapusă.

Cost: lookup O(celule traversate), dar memoria este proporțională cu domeniul, inclusiv spațiul gol.

Potrivire: contrazice lumea sparse, coordonatele negative nelimitate și streaming-ul viitor.

Verdict: respins.

[ ] **Grid uniform sparse / spatial hash**

Familie: aceleași celule uniforme, materializate numai când sunt ocupate într-un hash map.

Surse: [Teschner et al.](https://cgl.ethz.ch/Downloads/Publications/Papers/2003/Tes03/Tes03.pdf) descriu spatial hashing pentru detecție realtime; [Chipmunk2D 7.0.3](https://github.com/slembcke/Chipmunk2D/blob/Chipmunk-7.0.3/src/cpSpaceHash.c) oferă dovadă production.

Licență: Chipmunk2D este [MIT la tag-ul 7.0.3](https://github.com/slembcke/Chipmunk2D/blob/Chipmunk-7.0.3/LICENSE.txt), compatibil pentru studiu; implementarea Cerneala rămâne clean-room.

Garanții și calitate: superset exact la nivel broadphase; are nevoie de deduplicare când forma acoperă mai multe celule.

Cost: update proporțional cu celulele vechi+noi. Măsurat local: query P95 173 us pentru 512 query-uri/12.000 obiecte, update P95 614 us pentru 1.024 mutări și aproximativ 1,14 MB retained la 12.000 obiecte.

Potrivire: câștigător măsurat pentru tile-uri, garduri și lume sparse; dimensiunea celulei rămâne internă.

Verdict: recomandat pentru implementarea inițială.

[ ] **Hash/grid ierarhic**

Familie: alege nivelul grid-ului după dimensiunea obiectului și verifică relațiile cross-level.

Surse: [Mirtich, hierarchical spatial hash](https://www.merl.com/publications/TR97-23); o variantă realtime ulterioară este descrisă de [Eitz și Lixu](https://ieeexplore.ieee.org/document/4273369/).

Licență: lucrări academice; nu se adoptă sursă.

Garanții și calitate: poate evita parametrul unic de cell size, dar corectitudinea cere verificări între niveluri.

Cost: mai bun pentru obiecte polidisperse, mai multă logică și deduplicare decât grid-ul simplu.

Potrivire: alternativă dacă benchmark-urile reale arată collidere cu scări foarte diferite.

Verdict: alternativă viabilă, YAGNI acum.

[ ] **Quadtree regional**

Familie: împarte recursiv planul în patru regiuni.

Surse: [Finkel și Bentley, Quad Trees](https://doi.org/10.1007/BF00288933).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: exact ca broadphase dacă obiectele care traversează split-ul rămân la strămoș.

Cost: query dependent de distribuție; prototipul local cu rebuild la fiecare mutare a ajuns la 906.939 us P95 în high-churn.

Potrivire: dovedește că varianta rebuild nu este acceptabilă; o variantă incrementală ar cere complexitate nouă.

Verdict: respins pentru prima implementare.

[ ] **Loose quadtree**

Familie: noduri suprapuse/extinse pentru a reduce reinsertările.

Surse: evaluat alături de hash, SAP și BSP în [Luque, Comba și Freitas](https://www.inf.ufrgs.br/~comba/papers/2005/sabsp-i3d05.pdf).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: superset corect dacă looseness-ul este inclus în query.

Cost: mai puține mutări structurale, mai multe false-positive-uri.

Potrivire: viabil, dar nu bate simplitatea/măsurarea hash-ului sparse.

Verdict: alternativă.

[ ] **k-d tree**

Familie: subdiviziuni binare aliniate pe axe.

Surse: [Bentley, multidimensional binary search trees](https://cs.wmich.edu/gupta/teaching/cs6310/lectureNotes_cs6310/kdtree-bentley.pdf).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: query spațial bun, dar calitatea depinde de split și balansare.

Cost: build tipic O(n log n); update-urile dinamice cer reinsert/rebuild.

Potrivire: nepotrivit ca unic index pentru actori teleportați/mutați frecvent.

Verdict: respins pentru implementarea inițială.

[ ] **BSP semi-adjusting**

Familie: arbore BSP care amână și adaptează planele de separare la distribuția dinamică.

Surse: [Broad-phase collision detection using semi-adjusting BSP-trees](https://www.inf.ufrgs.br/~comba/papers/2005/sabsp-i3d05.pdf).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: broadphase corect; adaptarea reduce candidații în unele simulări.

Cost: algoritm și mentenanță mai complexe decât formele/scara Cerneala.

Potrivire: nu există dovadă locală că această complexitate este necesară.

Verdict: respins ca over-engineering.

[ ] **Incremental sweep-and-prune**

Familie: menține endpoint-urile sortate și exploatează coerența temporală.

Surse: [I-COLLIDE](https://graphics.stanford.edu/~jgao/collision-detection.html); [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf); [Chipmunk Sweep1D 7.0.3](https://github.com/slembcke/Chipmunk2D/blob/Chipmunk-7.0.3/src/cpSweep1D.c).

Licență: Chipmunk2D MIT la tag-ul fixat, compatibil pentru studiu.

Garanții și calitate: broadphase exact; determinism posibil prin endpoint și ordinal stabil.

Cost: foarte bun la mișcări mici, dar costul swap-urilor crește la teleport, churn și clustere.

Potrivire: alternativă pentru workload-uri coerente; corpusul Cerneala include explicit mișcare rapidă.

Verdict: alternativă, nu default.

[ ] **R-tree**

Familie: ierarhie balansată de dreptunghiuri cu split la overflow.

Surse: [Guttman, R-trees](https://www.cs.princeton.edu/courses/archive/fall08/cos597B/papers/rtrees.pdf).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: query exact ca index, dar dreptunghiurile interne se pot suprapune.

Cost: O(log n) așteptat pentru insert/query, cu degradare dependentă de overlap și split.

Potrivire: matur pentru baze de date, fără avantaj demonstrat față de grid/DBVT în scena realtime.

Verdict: respins.

[ ] **R*-tree**

Familie: R-tree cu split/reinsert optimizat pentru mai puțin overlap.

Surse: [Beckmann et al., R*-tree](https://www.csd.uoc.gr/~hy460/pdf/Rstar3.pdf).

Licență: descriere academică; nu se adoptă sursă.

Garanții și calitate: aceeași corectitudine, calitate mai bună a indexului în schimbul update-ului mai scump.

Cost: reinsertări forțate și algoritm de split mai complex.

Potrivire: optimizarea este orientată spre index persistent, nu frame-time incremental simplu.

Verdict: respins.

[ ] **Dynamic AABB tree / DBVT**

Familie: BVH binar incremental cu AABB-uri „fat” și reinsert numai când forma iese din proxy.

Surse: [Box2D 3.1.1 `dynamic_tree.c`](https://github.com/erincatto/box2d/blob/v3.1.1/src/dynamic_tree.c), [Chipmunk BBTree 7.0.3](https://github.com/slembcke/Chipmunk2D/blob/Chipmunk-7.0.3/src/cpBBTree.c), [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf).

Licență: Box2D este [MIT la v3.1.1](https://github.com/erincatto/box2d/blob/v3.1.1/LICENSE), Chipmunk MIT. Nu se copiază sursă.

Garanții și calitate: fat bounds produc false-positive-uri controlate, eliminate de narrow phase.

Cost: prototip local: update P95 18 us pentru 1.024 mutări, dar build 217.597 us pe gardul ordonat deoarece prototipul intenționat minimal nu are rotațiile/rebuild-ul production.

Potrivire: foarte puternic pentru dinamic, dar implementarea complet balansată mărește riscul față de grid-ul câștigător.

Verdict: alternativa principală și posibil tier dinamic viitor.

[ ] **BVH static/refit**

Familie: construcție de calitate pentru geometria statică, urmată de refit/rebuild parțial.

Surse: [OBBTree](https://techreports.cs.unc.edu/papers/96-013.pdf) demonstrează ierarhii de volume și teste de separare; [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf) compară BVH-uri broadphase.

Licență: lucrări academice; nu se adoptă sursă.

Garanții și calitate: exact ca broadphase; calitatea depinde de build și refit.

Cost: excelent pentru sat static, dar un singur arbore refit nu satisface singur churn-ul fără separare.

Potrivire: posibil tier static după măsurători ulterioare.

Verdict: viabil condiționat.

[ ] **Wide BVH cu separare static/dynamic**

Familie: BVH lat/quad cu arbori/layers separate pentru corpuri statice și dinamice.

Surse: [arhitectura Jolt 5.6.0](https://github.com/jrouwe/JoltPhysics/blob/v5.6.0/Docs/Architecture.md) și [BroadPhaseQuadTree.cpp 5.6.0](https://github.com/jrouwe/JoltPhysics/blob/v5.6.0/Jolt/Physics/Collision/BroadPhase/BroadPhaseQuadTree.cpp).

Licență: Jolt este [MIT la v5.6.0](https://github.com/jrouwe/JoltPhysics/blob/v5.6.0/LICENSE), compatibil pentru studiu.

Garanții și calitate: query-urile broadphase trebuie reverificate exact; separarea evită rebuild-ul static.

Cost: throughput production ridicat, dar reclamă rebuild în background, sincronizare și structură mult mai mare.

Potrivire: cel mai bun model production general, disproporționat pentru prima versiune single-thread Cerneala.

Verdict: alternativă de scalare, nu implementarea inițială.

[ ] **k-d + SAP / range+interval+SAP**

Familie: subdivizează spațiul, apoi rulează sweep/range queries în frunze.

Surse: [Flexible Use of Temporal and Spatial Reasoning](https://diglib.eg.org/collections/020d52cd-6f15-46bb-a4d2-57c71cc41a4b) și hibridul CGAL descris în [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf).

Licență: lucrarea este referință; adoptarea CGAL ar necesita revizuirea GPL/LGPL/commercial și este exclusă pentru această schimbare.

Garanții și calitate: generalitate și performanță puternice pe distribuții/scări variate.

Cost: SIMD, două structuri și politici adaptive cresc mult complexitatea.

Potrivire: strongest absolute, nu best fit pentru un framework managed fără motor fizic.

Verdict: cel mai bun absolut; respins pentru integrarea curentă.

[ ] **GPU grid/LBVH/SAP**

Familie: sortare Morton, grid sau SAP paralel și rebuild pe GPU.

Surse: [GPU Gems 3, broadphase CUDA](https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-32-broad-phase-collision-detection-cuda) și [Broadmark](https://diglib.eg.org/bitstream/handle/10.1111/cgf13884/v39i1pp436-449.pdf).

Licență: referințe educaționale/academice; nicio adopție de sursă.

Garanții și calitate: poate produce superset exact, dar ordinea brută paralelă nu este contract determinist.

Cost: transfer, dispatch și rebuild; eficient abia la volume foarte mari.

Potrivire: încalcă hard gate-ul care ține coliziunea în scenă și backend-urile grafice complet neștiutoare.

Verdict: respins.

### Narrow phase și mișcare continuă

Pentru setul fix box/circle/polygon convex, alegerea este dispatch pair-specific: teste analitice box-box, circle-circle și circle-polygon, iar pentru polygon-polygon se folosește SAT pe normalele muchiilor. SAT este o consecință directă a teoremei axei separatoare folosite și în [OBBTree](https://techreports.cs.unc.edu/papers/96-013.pdf). GJK rămâne alternativa generală documentată de [Gilbert, Johnson și Keerthi](https://graphics.stanford.edu/courses/cs448b-00-winter/papers/gilbert.pdf), dar simplex/EPA este complexitate inutilă pentru numai trei forme 2D.

`MoveAndCollide` folosește swept AABB doar pentru broadphase, apoi shape cast conservativ pe perechile aprobate, cu interval de timp normalizat `[0,1]`, initial-overlap explicit și bisection determinist când o soluție analitică nu există. Conservative advancement este documentat în [C2A](https://graphics.ewha.ac.kr/C2A/C2A.pdf). Nu există integrare de viteză sau solver fizic ascuns.

## 5. Metoda curentă

Nu există broadphase, collider sau lume de coliziune în implementarea curentă. [`Scene2D`](../../../../UI/Controls/Scene2D.cs#L9) păstrează doar ordinea și înregistrarea nodurilor, [`SceneGeometry2D`](../../../../UI/Controls/SceneGeometry2D.cs#L1) calculează transformuri/bounds de randare, iar [`HitTestService`](../../../../UI/Input/HitTestService.cs#L10) traversează numai `VisualChildren` și bounds de layout. Prin urmare, un `SceneNode2D` logic nu poate fi astăzi ținta reală a inputului.

## 6. Cel mai bun absolut

Hibridul k-d + SAP/adaptive range structures este cel mai puternic când costul de integrare nu contează: combină reasoning spațial și temporal și a fost evaluat la scară de până la un milion de obiecte în lucrarea citată. Nu este selectat deoarece cere SIMD, două structuri și politici adaptive fără dovadă că Cerneala are acel workload.

## 7. Cea mai bună potrivire

Grid-ul uniform sparse este best fit. A câștigat query-urile locale pe toate scenariile sparse/tile/gard, a rămas sub 0,7 ms P95 pentru 1.024 mutări high-churn, acceptă coordonate negative și nu reconstruiește staticul când se mută un actor. Dynamic AABB tree rămâne o strategie internă permisă dacă distribuțiile reale cu dimensiuni variate infirmă alegerea.

Gate numeric înghețat pentru Etapa 2, pe același host și corpus după warmup:

- `large-sparse`: query P95 <= 500 us pentru 512 query-uri, update P95 <= 150 us, retained <= 1,5 MB la 12.000 intrări;
- `high-churn`: query P95 <= 250 us pentru 256 query-uri și update P95 <= 1.000 us pentru 1.024 mutări;
- `long-fence`: query P95 <= 150 us pentru 256 query-uri;
- zero false-negative față de oracle-ul exhaustiv; false-positive-urile broadphase sunt permise și contorizate;
- o mutare actualizează numai celulele vechi/noi ale colliderului și nu reconstruiește intrările statice.

## 8. Schița implementării

1. `Scene2D` deține un index spatial hash sparse intern, cu cell size intern și liste ordonate prin ordinal stabil.
2. Colliderele furnizează un singur AABB scene-space prin helper-ul comun de geometrie; add/remove/move actualizează celulele afectate.
3. Query-ul broadphase deduplică ID-urile cu stamp array; layer/mask sigur se aplică înainte de narrow phase.
4. Narrow phase dispatch-uiește analitic/SAT, iar shape cast verifică intervalul continuu.
5. Rezultatele se sortează după fracție/distanță și ordinal, niciodată după ordinea hash-ului.
6. Testele randomizate cu seed fix compară toate rezultatele cu scanarea exhaustivă.

Au fost modificate numai runner-ul de benchmark, testele RED și artefactele Etapei 0; nu a fost introdus cod de coliziune production.
