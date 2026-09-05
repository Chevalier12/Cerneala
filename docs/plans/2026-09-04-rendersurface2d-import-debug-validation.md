# Plan: import Tiled/LDtk, validare si overlay de debug

Status: finalizat (2026-09-05; tinta SDL GPU, exceptia MonoGame aprobata si validarea vizuala umana in asteptare sunt explicite mai jos)
Acopera: #14, #15, #16
Depinde de: `2026-09-04-rendersurface2d-scene-foundation.md`, `2026-09-04-rendersurface2d-tilemap-and-scale.md` si `2026-09-04-rendersurface2d-collision-and-picking.md`; demonstratia animata depinde si de `2026-09-04-rendersurface2d-sprite-animation.md`

## Rezultat urmarit

Un proiect poate importa un subset declarat si versionat din Tiled sau LDtk in acelasi model core de tilemap/scena, poate afisa diagnostice actionabile si poate porni un overlay care arata collidere, chunk-uri, coordonate tile, ordine si o grila externa de navigatie. Un exemplu integrat demonstreaza padurea, satul, casa, gardurile si playerul.

### Schimbare de tinta aprobata — 2026-09-05

Utilizatorul a cerut explicit continuarea planului fara MonoGame ca tinta de livrare: defectul MonoGame ramane documentat, dar nu mai blocheaza planul. Pentru etapele ramase, backend-ul obligatoriu este SDL GPU. Se pastreaza testele si dovezile defectului WindowsDX/MSAA; nu se marcheaza rezultatele rosii drept verzi si nu se implementeaza un workaround. Eliminarea backend-ului MonoGame din repository nu face parte din acest plan.

Capturile si conformance-ul obligatoriu verifica SDL fata de contractul semantic, nu fata de o referinta MonoGame defecta. Suita solutiei ramane inventariata la etapa 6: esecurile exclusiv MonoGame sunt raportate separat ca neblocante prin decizia utilizatorului; testele core, importer, SourceGen, Servo, SDL si celelalte verificari aplicabile trebuie sa fie verzi. Nu se exclud defecte comune doar pentru ca un test le expune prin MonoGame.

## Contracte externe si politica de versiune

- Tiled JSON este documentat oficial la <https://doc.mapeditor.org/en/stable/reference/json-map-format/>. Contractul include harti finite/infinite, chunks, straturi, tilesets, obiecte, flip flags si mai multe encodari/compresii; acest plan nu pretinde automat suport complet.
- LDtk descrie proiectul, levels, layer instances si schema JSON oficiala la <https://ldtk.io/docs/game-dev/json-overview/>, <https://ldtk.io/docs/game-dev/json-overview/json-schema/>, <https://ldtk.io/docs/game-dev/json-overview/levels-section/> si <https://ldtk.io/docs/game-dev/json-overview/layer-instances/>. Formatul evolueaza, deci versiunea/schema acceptata trebuie verificata, nu presupusa.
- Nivelurile LDtk separate sunt documentate la <https://ldtk.io/docs/game-dev/json-overview/optional-separate-levels/>; fisierele lipsa sau versiunile incompatibile produc diagnostice, nu harti partiale tacute.

## Subset v1 propus, de aprobat la etapa 0

### Tiled

- JSON orthogonal, harti finite si infinite chunked;
- tilesets JSON embedded si external, un atlas/imagine per tileset in v1;
- tile, group si object layers; offset, visibility, opacity/tint si proprietati primitive;
- date numerice JSON si base64 necomprimat/zlib/gzip; flip flags;
- obiecte rectangle, ellipse, convex polygon, polyline si point pentru collidere/spawn/metadate;
- respingere explicita pentru isometric/staggered/hexagonal, zstd, template objects, image layers si extensii necunoscute pana la o etapa ulterioara justificata.

### LDtk

- proiect `.ldtk` cu levels inline si optiunea oficiala `.ldtkl` pentru levels separate;
- Tile, AutoLayer, IntGrid si Entity layers, world/layer offsets, tilesets, tile source/position/flip;
- entitati si field-uri primitive necesare pentru spawn, collider, layer/mask si stare initiala;
- diagnostice pentru field kinds, layouts sau versiuni nesuportate; niciun field relevant nu este ignorat silentios.

Subsetul este o propunere de implementare proportionala, nu un fapt stabilit de codul actual. Extinderea lui cere fixture, contract, teste si documentatie, nu doar un `switch` tolerant.

### Decizii si audit etapa 0 (2026-09-05)

- Utilizatorul a aprobat explicit subsetul v1 propus si importul la runtime, la incarcarea hartii. Integrarea de build/preconversie nu face parte din v1; parserul si validatorul raman independente de backend si de generatorul `.crn`.
- Utilizatorul a delegat explicit alegerea deciziilor pentru acest plan si a reconfirmat delegarea dupa cererile de clarificare. Se aplica deciziile consemnate mai jos; delegarea nu relaxeaza gate-urile, nu permite aproximari tacute si nu autorizeaza extinderea in afara planului.
- Auditul sursei a identificat un decalaj fata de subset: `TileFlip2D` din `UI/Controls/TileMap2DModel.cs` accepta numai Horizontal/Vertical, iar Tiled orthogonal include si flip diagonal. Pastrarea tuturor flip-urilor necesita extinderea contractului core si verificarea randarii/colliderelor/promovarii, nu mascarea bitului diagonal.
- Se extinde contractul core pentru flip diagonal si transformul local afin al descriptorilor. Narrow phase-ul existent suporta cercuri transformate afin (inclusiv elipse); importerul conserva geometria, fara aproximare prin cerc/polygon si fara noduri publice per celula statica.
- Decizie polyline: se pastreaza punctele si metadatele originale. Cand rolul explicit este collider, fiecare pereche consecutiva de puncte defineste un segment cu grosime zero si contact pe ambele laturi. Traseul nu se inchide implicit, nu se extrudeaza, iar segmentele degenerative sunt erori de date. Punctele izolate raman spawn/metadate, nu collidere cu raza inventata. Testele acopera capete, colturi, ambele sensuri, transformuri si traversare rapida.
- Gate-ul etapei ramane deschis pana la arhivarea matricei versionate si verificarea fixture-urilor/testelor RED.
- Cercetarea oficiala si inventarul partial sunt arhivate in [compatibility-research.md](evidence/2026-09-04-scene-import-stage0/compatibility-research.md). Tiled editor 1.12.2 scrie format JSON 1.11; schema LDtk consultata este 1.5.3. Aceste constatari nu reprezinta suport implementat sau gate de compatibilitate inchis.

## Ownership si packaging

- Modelul rezultat, validarea lui structurala si diagnosticele independente de format apartin core-ului Scene2D.
- Parsarea formatelor externe apartine unui proiect optional separat, estimat `Cerneala.Scene2D.Importers`, care referentiaza core-ul si foloseste `System.Text.Json`; dependente suplimentare cer justificare.
- Importerii produc date core, resource IDs/cai si descriptori de collider/entitate. Nu incarca GPU resources si nu cunosc backend-ul.
- Overlay-ul este un nod/serviciu de scena care emite comenzi de desen existente. Nu modifica indexul spatial, rezultatele picking sau datele hartii.
- Generatorul `.crn` leaga o resursa/model importat la `TileMap2D`; nu parseaza formatul complet in source generator daca asta dubleaza parserul runtime/build-time.
- Importerul poate marca o celula drept candidata la promovare prin metadate aprobate, dar nodul scene-space si Aspect/Motion/Prism sunt create de compozitia scenei/markup, nu de parserul JSON.
- Debug overlay-ul este compatibil Aspect/Motion/Prism. Modelele importate, validatorul si tile-urile nepromovate raman date si nu primesc aceste sisteme.
- Toate exemplele `.crn` folosesc sintaxa Cerneala reala (`Aspect`, `@animate with`, `@prism`, `@templates`); documentatia nu canonizeaza pseudo-XML din discutii.

## Model de diagnostic

Fiecare problema are cel putin cod stabil, severitate, mesaj, fisier si locatie JSON/entitate cand este disponibila. Se separa:

- eroare fatala: fisier/schema/version/tileset obligatoriu imposibil de rezolvat;
- eroare de date: tile ID/source rect/collider/layer invalid;
- warning: informatie optionala cunoscuta dar nefolosita conform subsetului;
- unsupported: constructie recunoscuta in afara matricei v1, niciodata ignorata tacit.

Validarea ruleaza si pentru modele create programatic, nu numai pentru importeri.

## Etapa 0 - fixture-uri, matrice de compatibilitate si teste RED

- [x] Se confirma versiunile de format/schema suportate din documentatia oficiala si se arhiveaza o matrice camp-cu-camp pentru subsetul v1 propus.
- [x] Se obtine aprobarea explicita a subsetului v1 sau se ajusteaza lista inainte de cod de productie; orientările/feature-urile respinse raman non-obiective documentate.
- [x] Se creeaza fixture-uri mici, copyright-safe: Tiled finit, Tiled infinite chunks, external tileset, LDtk inline, LDtk separate level si cate un caz invalid pentru fiecare categorie de diagnostic.
- [x] Se adauga golden models independente de parser; testele compara semantica rezultata, nu ordinea accidentala a proprietatilor JSON.
- [x] Se adauga teste RED pentru versiune, cai relative, fisier lipsa, ID tile, atlas bounds, flip, layer order, chunk overlap, collider degenerativ, layer/mask si field unsupported.
- [x] Se adauga teste RED pentru referinta/metadatele unei celule promovate: coordonata/stratul stabil, duplicate, tile absent, promotie peste tile gol si proprietati importate folosite de template/Aspect.
- [x] Se adauga teste RED pentru overlay: aceeasi scena produce aceleasi rezultate de collision/picking cu overlay oprit sau pornit.
- [x] Se adauga teste RED Aspect+Motion+Prism pe debug overlay si se verifica faptul ca sistemele nu sunt atasate modelelor/importerilor.
- [x] Se stabileste daca importul are loc la build, runtime sau ambele. Daca ambele sunt necesare, acelasi parser/validator este reutilizat si diagnostics mapping-ul este caracterizat. (Decizie: runtime; integrarea build-time nu este necesara in v1.)

### Gate etapa 0

- [x] Matricea de compatibilitate si versiunile sunt explicite si aprobate.
- [x] Fixture-urile sunt valide conform instrumentelor/schema oficiala unde verificarea este disponibila.
- [x] Fiecare RED esueaza pentru lipsa parserului/validatorului/overlay-ului, nu din cauza unui fixture corupt accidental.

Dovezi etapa 0: [checkpoint si comenzi](evidence/2026-09-04-scene-import-stage0/README.md), [contractul complet](evidence/2026-09-04-scene-import-stage0/compatibility-matrix.md) si inventarul JSON cu 399 campuri/26 scope-uri. Cele 16 verificari de fixture sunt verzi; regenerarea pastreaza 33 fisiere identice. Core: 33 RED pentru API/parser absent si 1 GREEN de fixture; SourceGen: 1 RED numai `CERNEALAUI002` pentru overlay absent si 1 GREEN pentru binding-ul modelului. Nu s-a modificat cod de productie. Gate-ul RED nu pretinde ca implementarea sau suita completa sunt deja verzi.

## Etapa 1 - validatorul core (#16)

- [x] Se implementeaza validatorul modelului core pentru dimensiuni, ID-uri, source rect-uri, referinte atlas, bounds/chunk-uri, ordine/layers, valori finite si versiuni.
- [x] Se implementeaza validarea colliderelor: shape, puncte, raza, convexitate conform contractului, layer/mask si asociere cu chunk/entitate.
- [x] Se implementeaza agregarea diagnosticelor determinista, cu limite pentru a nu produce memorie nelimitata pe fisiere ostile.
- [x] Constructia programatica si importerii folosesc acelasi validator; nu apar reguli paralele cu mesaje contradictorii. (Contractul reutilizabil core este verificat; parserele din etapele 2/3 il vor consuma, fara reguli geometrice duplicate.)
- [x] Datele invalide nu ajung partial in cache-ul grafic sau indexul spatial.
- [x] Se fuzz-uiesc/parser-test inputuri trunchiate, numere extreme, duplicate, path traversal si dimensiuni intentionat uriase; limitele acceptate sunt documentate. (Matricea ostila core si caile declarative sunt verificate; JSON/compresia si accesul fizic sub root apartin etapelor 2/3 conform ordinii inghetate la etapa 0.)

### Gate etapa 1

- [x] Toate categoriile #16 au diagnostic stabil si teste: tile ID, source rect, collider, layer/mask si asset/referinta.
- [x] Inputul ostil esueaza controlat fara OOM, loop infinit sau acces in afara radacinii permise. (Core nu acceseaza filesystem-ul; gate-ul fizic de root ramane obligatoriu la parser.)

Dovezi etapa 1: [checkpoint, RED/GREEN, comenzi si limite](evidence/2026-09-04-scene-import-stage1/README.md). 172 teste afectate + 3 RED core originale sunt verzi; manifestul API trece. Sweep-ul de chunk-uri si validarea sparse elimina scanarile patratice masurate. Testul de atlas invalid dovedeste zero chunk-uri publicate partial. Conformanta nativa si suita finala nu sunt declarate efectuate anticipat.

## Etapa 2 - importerul Tiled

- [x] Se creeaza proiectul optional si test project-ul aferent, fara a adauga parserul in assembly-ul core.
- [x] Se implementeaza rezolvarea caii map -> external tileset -> atlas sub o politica de baza explicita si sigura.
- [x] Se parseaza numai matricea v1: layers/groups/objects, finite/infinite chunks, encodari aprobate, flip flags si proprietati primitive.
- [x] Obiectele de coliziune si spawn sunt mapate la descriptori core cu transform/offset corect; ordinea si layer metadata se pastreaza.
- [x] Metadatele aprobate pentru tile interactiv/promovabil sunt pastrate cu identitate layer/coordonata stabila; importerul nu materializeaza toate celulele ca noduri.
- [x] Orice orientation, compression, object/template sau field cunoscut dar nesuportat produce `unsupported`/eroare conform matricei.
- [x] Se ruleaza fixture-urile, golden model comparison, validarea core si teste de cai/fisiere lipsa.
- [x] Se reindexeaza solutia dupa modificarile de proiect/C#.

### Gate etapa 2

- [x] Fixture-urile Tiled finite si infinite produc modelul golden exact.
- [x] Fisierele nesuportate nu produc o harta „aproape buna”; diagnosticul identifica motivul si locatia.

Dovezi etapa 2: [checkpoint si RED/GREEN](evidence/2026-09-04-scene-import-stage2/README.md). 76 teste Tiled, 175 regresii core si manifestul API sunt verzi. Compresia stricta foloseste o dependenta izolata in proiectul optional, [justificata prin reproducerea inputului trunchiat](evidence/2026-09-04-scene-import-stage2/compression-decision.md). Gate-ul fizic de root include o junction reala; politica documentata presupune un arbore stabil pe durata importului, nu un sandbox impotriva mutatiilor concurente ale filesystem-ului.

## Etapa 3 - importerul LDtk

- [x] Se implementeaza verificarea versiunii/schema si rezolvarea levels `.ldtkl` separate.
- [x] Tile/AutoLayer/IntGrid/Entity layers sunt mapate la acelasi model core, cu world/layer offsets, source rect si flip corecte.
- [x] Field-urile aprobate mapeaza spawn/collider/layer/mask/stare; tipurile necunoscute produc diagnostic conform matricei.
- [x] Field-urile aprobate pot identifica usa/tile-ul interactiv ce va fi promovat de compozitia scenei, fara a cupla parserul LDtk la Aspect/Motion/Prism.
- [x] Level-urile lipsa, UID-urile duplicate, tileset-urile absente si referintele circulare/eronate esueaza controlat.
- [x] Se ruleaza fixture-urile, golden model comparison si validatorul core.

### Gate etapa 3

- [x] Fixture-urile LDtk inline si separate produc modelul golden semantic echivalent.
- [x] Diferentele Tiled/LDtk care nu pot fi normalizate sunt pastrate ca metadate sau diagnosticate, nu pierdute tacit.

Dovezi etapa 3: [checkpoint, RED/GREEN si decizii de reprezentare](evidence/2026-09-04-scene-import-stage3/README.md). 150 teste de import (76 Tiled + 74 LDtk), 175 regresii core si manifestul API sunt verzi. Inventarul inchis este testat la toate cele 16 scope-uri LDtk, inclusiv metadatele nested si referintele externe. Verificarea este semantica pentru subsetul inghetat, nu o pretentie de validare generala a fiecarui camp editor-only prin JSON Schema.

## Etapa 4 - overlay de debug (#15)

Checkpoint: [implementare, corpus nativ SDL, masuratori si defectul MonoGame documentat](evidence/2026-09-04-scene-import-stage4/README.md). Etapa este verificata pe SDL: 12 capturi native, picking/collision identice, zero modificari ale indexului si revenire pixel-exacta la off. Defectul MonoGame hardware ramane documentat si neblocant prin decizia explicita de mai sus; nu s-a introdus un workaround si nu s-a relaxat toleranta vizuala SDL.

- [x] Se implementeaza flags independente pentru collidere, chunk bounds, coordonate/ID tile, ordine/layer si grila de navigatie furnizata extern.
- [x] Se adauga un flag/indicator pentru tile-uri promovate si pozitia lor fata de slotul batch-uit, util pentru detectarea desenului dublu sau ordering-ului gresit.
- [x] Overlay-ul foloseste transformul, clip-ul si bounds-urile comune; liniile/textul au politica de grosime/scalare documentata la zoom.
- [x] Identitatile/culorile trigger/layer/collision state sunt deterministe si configurabile numai unde exista cerinta reala. (Stare de participare/filtrare, nu istoric de contacte inventat.)
- [x] Informatiile voluminoase sunt culled; activarea overlay-ului nu enumera toata harta in afara viewport-ului.
- [x] Grila de navigatie este doar un contract de vizualizare a datelor furnizate. Nu se implementeaza pathfinding.
- [x] Overlay-ul nu intra in picking, coliziune, sortarea gameplay sau serializare.
- [x] Proprietatile vizuale/configurabile ale overlay-ului sunt UiProperties compatibile Aspect/Motion, iar comenzile lui sunt incadrate corect de Prism.
- [x] Se masoara costul lui pornit/oprit si se testeaza ca oprit nu adauga comenzi/alocari dupa warmup.

### Gate etapa 4

- [x] Capturile deterministe arata fiecare flag pe SDL GPU si sunt obtinute numai prin `Window.SaveScreenshot`. (Cerinta WindowsDX retrasa explicit de utilizator.)
- [x] Collision/picking queries sunt identice cu overlay oprit si pornit.
- [x] Overlay oprit are zero comenzi proprii; costul pornit este masurat si arhivat.
- [x] Aspect/Motion/Prism pe overlay schimba numai prezentarea debug si nu datele/indexurile observate.

## Etapa 5 - demonstratia integrata si markup

- [x] Se adauga in `Playground` sau sample-ul aprobat o lume copyright-safe cu padure, sat, casa, garduri, player si atlas declarat ca resursa.
- [x] Markup-ul compune `TileMap2D`, straturi/grupuri, entitati dinamice, collidere si player animat; code-behind ramane pentru logica gameplay/input, nu pentru reconstructia declaratiilor.
- [x] O casa are o usa interactiva reprezentata ca tile promovat sau entitate, cu stare inchis/deschis, animatie, input unificat si collider activ numai cand este inchisa.
- [x] Cel putin un tile individual promovat foloseste Aspect, Motion si Prism cu sintaxa `.crn` reala; tile-urile obisnuite raman in batch.
- [x] Camera/pan demonstreaza culling si chunk reuse prin contoare, iar o mutatie locala demonstreaza rebuild limitat.
- [x] Inputul user-like selecteaza/muta playerul prin API-ul aprobat, iar gardurile produc contactul asteptat.
- [x] Un toggle user-like porneste overlay-ul si `Window.SaveScreenshot` captureaza scenarii fixe pe SDL GPU. (Cerinta WindowsDX retrasa explicit de utilizator.)
- [x] Se pastreaza separat un fixture echivalent Tiled si LDtk care produce aceeasi lume core sau diferente explicit documentate.

### Gate etapa 5

- [x] Scenariul complet demonstreaza toate #1-#16, nu doar compilarea markup-ului.
- [x] Scenariul demonstreaza matricea Aspect/Motion/Prism, inclusiv tile individual si debug overlay, fara aplicarea sistemelor pe `SceneItems2D` in locul nodurilor din `@templates`.
- [x] Capturile, contoarele si query results sunt arhivate/reproductibile.
- [x] Orice validare vizuala umana necesara este ceruta utilizatorului si nu este declarata efectuata de agent. (Ceruta la 2026-09-05; confirmarea umana ramane in asteptare.)

Dovezi etapa 5: [checkpoint, defecte RED/GREEN, matrice #1-#16 si reproducere](evidence/2026-09-04-scene-import-stage5/README.md). Scenariul SDL final trece cu 361 cadre, 9 capturi complete si 9 crop-uri Servo; contactele sunt -32/-44/+36, mutatia reconstruieste un singur batch, iar debug pastreaza coliziunea/indexul/picking-ul. 277 teste afectate core, 186 Language, 515 SourceGen, 151 importer, 121 SDL (+5 opt-in neexecutate) si manifestul API sunt verzi. Validarea vizuala umana nu este pretinsa.

## Etapa 6 - documentatie, API si suita completa

- [x] Se foloseste `writing-api-documentation` pentru importeri, optiuni, rezultate, diagnostice, overlay si orice API core nou.
- [x] Se documenteaza matricea exacta Tiled/LDtk, politica versiunii/cailor, exemplele markup, diagnosticele si non-obiectivele.
- [x] Se documenteaza promovarea sparse, usa interactiva si Aspect/Motion/Prism cu sintaxa reala; exemplele `SceneItems2D` plaseaza directivele pe nodul din `@templates`.
- [x] Se actualizeaza `docs-site/documentation/manifest.json` si se ruleaza testul manifestului.
- [x] Se ruleaza testele focusate importer/validator, core Scene2D, SourceGen, Servo si conformance pe SDL GPU. (Cerinta WindowsDX retrasa explicit de utilizator.)
- [x] Se reindexeaza, se ruleaza API Compat strict cu raport arhivat si se revizuieste orice diferenta publica.
- [x] Se ruleaza `dotnet test .\Cerneala.slnx` si orice proiect optional care nu este inclus automat in solutie; rezultatele exclusiv MonoGame sunt inventariate separat, neblocante conform deciziei aprobate, fara a pretinde ca au trecut.

Dovezi etapa 6: [checkpoint final, comenzi si inventarul complet](evidence/2026-09-04-scene-import-stage6/README.md), [API Compat strict si revizuirea diferentelor](evidence/2026-09-04-scene-import-stage6/api-compat.md). Suita finala: 4584 PASS, 2 FAIL exclusiv MonoGame (aceleasi cazuri MSAA documentate, 47/255 si 25/255), 7 SKIP opt-in. Comanda completa iese cu cod 1; nu este declarata integral verde. Toate gate-urile aplicabile tintei aprobate trec, inclusiv manifestul, cele 277 regresii afectate si corpusul nativ SDL. Coliziunea de nume `Scene2D` din build-ul restaurat al testelor a fost reprodusa si rezolvata cu aliasuri locale, fara schimbarea asertiunilor sau a API-ului public.

## Definitia de done

- [x] #14-#16 sunt livrate pe subsetul versionat si documentat.
- [x] Tiled si LDtk produc model core verificabil; constructiile nesuportate au diagnostice explicite.
- [x] Validatorul este unic si protejeaza atat importul, cat si constructia programatica.
- [x] Overlay-ul este fidel, culled si fara efect semantic asupra scenei.
- [x] Tile-urile interactive importate pot fi promovate individual si pot folosi Aspect/Motion/Prism fara ca importerul ori toate celulele sa devina `UIElement`.
- [x] Demonstratia integrata dovedeste toate #1-#16 cu input, capturi si contoare reale.
- [x] Documentatia, manifestul, API diff-ul, SDL si suita aplicabila sunt verzi; exceptiile exclusiv MonoGame sunt raportate explicit conform deciziei aprobate.
