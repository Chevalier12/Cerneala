# Plan: fundatia declarativa a scenei `RenderSurface2D`

Status: finalizat
Acopera: #2, #10, #11, #12
Depinde de: nimic
Deblocheaza: tilemap, coliziuni/picking, animatie, import/debug

## Rezultat urmarit

Markup-ul poate rezolva atlasuri imagine, poate grupa si transforma noduri in coordonate de scena, poate controla explicit ordinea si poate actualiza incremental entitati dinamice. Comportamentul existent al `Sprite2D`, ordinea implicita si combinatia `OnDraw` + `Scene` raman compatibile.

Fundatia garanteaza si integrarea comuna Aspect/Motion/Prism pentru nodurile vizuale high-level, grupuri si straturi. `SceneItems2D` ramane materializator: capabilitatile se declara pe nodurile produse in `@templates`, nu pe container.

## Ownership si fisiere estimate

- Contract scena: `UI/Controls/SceneNode2D.cs`, `Scene2D.cs`, `Sprite2D.cs`, tipuri noi scene-space sub `UI/Controls/`.
- Gazda si resurse: `UI/Controls/RenderSurface2D.cs`, resolver intern comun cu `UI/Controls/Image.cs`, `UI/Resources/ImageResource*.cs` numai daca ownership-ul existent o cere.
- Colectii: `UI/Controls/SceneItems2D.cs`, `UI/Collections/IObservableList*.cs` doar pentru caracterizare, nu pentru redesign.
- Markup: `Cerneala.SourceGen/UiMarkupGenerator.cs` si testele lui.
- Consumatori: testele RenderSurface, `Tetrisish/MainWindow.crn`, `Tetrisish/TetrisGameSurface.cs` pentru compatibilitate.
- Documentatie: paginile canonice ale claselor afectate, pagini pentru tipurile noi, ghidul markup si manifestul.

## Contracte care trebuie inghetate inainte de productie

- Ordinea implicita ramane ordinea colectiei. Sortarea explicita este stabila; egalitatile revin la ordinea sursei.
- `Layer` separa planuri majore; modul Y sorteaza dupa o ancora scene-space definita, nu dupa bounds de layout UI.
- Transformul de grup are origine si unitati scene-space explicite. Nu se reutilizeaza orb `ElementVisualTransform`, deoarece nodurile scenei nu au `ArrangedBounds`.
- Acelasi transform compus si aceleasi bounds alimenteaza ulterior redarea, culling-ul, picking-ul si coliziunea.
- `SourceResourceId` are aceeasi precedenta, dependency tracking, invalidare si durata de viata ca la `Image`; resursa este eliberata de cache/sesiune, nu de sprite.
- Orice proprietate scene-space ce trebuie controlata de Aspect/Motion este `UiProperty` cu invalidarea corecta. Valorile structurale/resursele nu sunt declarate animabile fara mixer si contract explicit.
- Prism pe un grup/strat incadreaza comenzile descendentilor si foloseste bounds agregate transformate; deschiderea/inchiderea scope-ului este exception-safe.
- Aspect/Motion/Prism imbricate urmeaza contractul existent al sistemelor. Combinatiile nesuportate primesc diagnostic explicit, nu fallback tacit.
- Pentru `SceneItems2D`, `Add`, `Remove`, `Move` si `Replace` ating doar intervalul necesar. `Reset`, schimbarea template-ului sau o sursa fara delta precisa pot reconstrui complet.
- Indexul dat template-ului ramane corect dupa insert/move/remove. Aceasta cerinta poate obliga re-realizarea intervalului cu index schimbat, dar nu justifica reconstruirea elementelor anterioare neafectate.

## Etapa 0 - baseline, API si teste RED/characterization

- [x] Se inventariaza prin RoslynIndexer toate referintele la `SceneNode2D`, `Scene2D`, `SceneItems2D`, `Sprite2D`, `Image.SourceResourceId` si proprietatile de transform ale `UIElement`; rezultatul se noteaza in plan daca ownership-ul estimat se schimba.
- [x] Se adauga teste de caracterizare pentru: ordinea curenta a copiilor, `OnDraw` urmat de `Scene`, invalidarea la schimbarea unui nod, attach/detach, cache-ul imaginii si faptul ca transformurile UI nedocumentate nu modifica azi sprite-ul.
- [x] Se pastreaza ca teste de caracterizare GREEN contractele existente `SpriteAspectAppliesSpritePropertiesThroughTheSceneLogicalTree`, `SpriteMotionAnimatesSpritePropertiesThroughTheSceneLogicalTree`, `SpritePrismCapturesOnlyTheSpriteDrawUsingDestinationBounds` si transformarea Prism prin ViewBox.
- [x] Se adauga teste RED pentru o resursa `ImageResource` declarata in `.crn` si consumata de `Sprite2D` prin ID tipat; esecul trebuie sa fie lipsa suportului, nu fixture invalid.
- [x] Se adauga teste RED pentru transform compus in doua grupuri imbricate, clip/view-box si bounds transformate, inclusiv transform neinversabil.
- [x] Se adauga teste RED pentru `Layer`, sortare Y stabila, vizibilitate si egalitati.
- [x] Se adauga teste RED markup/runtime pentru Aspect+Motion+Prism pe grup, strat si nodul creat in `SceneItems2D.@templates`, inclusiv scope-uri imbricate, bounds agregate si lifecycle attach/detach.
- [x] Testele SourceGen folosesc sintaxa reala existenta: `Aspect`/property-element, `@animate with`, `@prism { ... }` si `@templates`; nicio sintaxa demonstrativa inventata nu devine contract.
- [x] Se adauga teste RED `SceneItems2D` pentru add/remove/move/replace/reset, duplicate ca identitate/valoare, schimbarea template-ului, DataContext/index si attach/detach/reattach fara abonamente duble.
- [x] Se decide si se arhiveaza forma minima a API-ului public: tipul transformului scene-space, proprietatile de ordine si sintaxa resursei. Decizia include exemple de markup compilabile, compatibilitate si alternative respinse.

### Inventar si decizie API inghetata la etapa 0

- Inventarul RoslynIndexer confirma ownership-ul estimat. `RenderSurface2D` este singurul proprietar al radacinii `Scene2D` si al transformului ViewBox; `Scene2D` si `SceneItems2D` detin copiii logici; `Sprite2D` este singurul emitator de sprite din arborele curent. Consumatorul extern relevant este `Tetrisish`, iar testele de resurse sunt singurii consumatori ai `Image.SourceResourceId`. Registrul Motion detine mixerele proprietatilor de transform `UIElement`; niciuna dintre aceste proprietati, in afara rotatiei consumate direct de `Sprite2D`, nu este aplicata azi de arborele scenei. Nu se schimba ownership-ul estimat.
- Transformul de grup nu introduce un al doilea tip matriceal public. `Scene2D` reutilizeaza canalele `UIElement` deja animabile (`TranslateX/Y`, `Scale`, `ScaleX/Y`, `Rotation`, `SkewX/Y`, `RenderTransform`) si adauga `TransformOrigin: DrawPoint`, in unitati locale scene-space, implicit `(0, 0)`. Matricea locala este `T(-origin) * Scale * Skew * Rotation * Translation * RenderTransform * T(origin)`; unghiurile sunt in radiani, conform `Matrix3x2`. `RenderTransformOrigin` ramane contract de layout normalizat si nu este reinterpretat. Transformurile imbricate se compun local-la-parinte, iar acelasi rezultat intern alimenteaza desenul, bounds, apoi planurile de input/culling/coliziune. Pentru o matrice neinversabila, conversia world-to-local esueaza explicit, dar transformarea forward, bounds conservatoare si desenul continua.
- API-ul minim de ordine este `Scene2D.OrderMode: SceneOrderMode`, cu valorile `Source` (implicit), `Layer` si `LayerThenY`, plus `SceneNode2D.Layer: int` implicit `0`. `Source` emite strict ordinea colectiei. `Layer` sorteaza crescator dupa layer si stabil dupa indexul sursei. `LayerThenY` sorteaza crescator dupa layer, apoi dupa marginea `Bottom` a bounds-ului scene-space transformat al nodului si stabil dupa indexul sursei; bounds necunoscute folosesc ancora `0`. `Sprite2D.LayerDepth` ramane payload pentru backend si nu participa la ordinea arborelui.
- Sintaxa resursei este declaratia specializata `<resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />` intr-un property-element `.Resources`, apoi `SourceResourceId="$WorldAtlas"` pe `Sprite2D`. Tipul proprietatii este `ResourceId<ImageResource>?`; un ID nenul are precedenta fata de `Source`, exact ca la `Image`. Doua sprite-uri cu acelasi ID folosesc acelasi cache al radacinii, iar sprite-urile nu detin si nu elibereaza imaginea.
- Grupul si stratul folosesc ambele `Scene2D`; nu se introduce `SceneLayer2D` si nu se dubleaza o colectie de copii. Un `RenderSurface2D` pastreaza o singura scena-radacina, dar aceasta poate contine oricate `Scene2D` imbricate.
- Alternative respinse: reutilizarea directa a `ElementVisualTransform` (depinde de `ArrangedBounds` inexistente pentru nodurile scenei), un nou `SceneTransform2D` paralel cu `Transform`, folosirea `LayerDepth` pentru ordinea arborelui, mutarea sortarii in colectia utilizatorului si un container separat pentru efectele nodurilor produse de `SceneItems2D`.

Markup-ul de contract este:

```xml
<RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
  <RenderSurface2D.Resources>
    <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D OrderMode="LayerThenY" TranslateX="32" TransformOrigin="128,96">
      <Scene2D Layer="10">
        <Sprite2D SourceResourceId="$WorldAtlas" />
      </Scene2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

Evidenta RED/GREEN din 2026-09-04:

- 22 teste de caracterizare trec, inclusiv ordinea sursei, `OnDraw` inainte de scena, invalidarea, lifecycle-ul, cache-ul imaginii, transformurile UI ignorate azi si contractele existente Aspect/Motion/Prism/ViewBox.
- Testele de transform esueaza prin absenta comenzilor de transform/originii si a scope-ului Prism de grup; testele de ordine esueaza prin absenta `Layer`/`OrderMode`.
- Cele doua teste SourceGen esueaza numai cu `CERNEALAUI002`/`CERNEALAUI003` pentru `ImageResource`, `SourceResourceId`, `OrderMode` si `Layer`; parserul accepta sintaxa reala Aspect, `@animate with`, `@prism` si `@templates` din acelasi fixture.
- Testele incrementale compileaza si separa contractul curent valid de delta absenta: reset/template/subscriptii trec, iar add/insert/remove/move/replace esueaza prin recrearea nodurilor neafectate, nu prin fixture sau mediu.

### Gate etapa 0

- [x] Testele de caracterizare sunt verzi pe baseline.
- [x] Fiecare test RED esueaza pentru contractul absent si nu din cauza generatorului, fixture-ului sau mediului.
- [x] Nu incepe productia pana cand semantica originii transformului si a egalitatilor de sortare este explicita.

## Etapa 1 - rezolvare imagine din markup

- [x] Se extrage/reutilizeaza o cale interna unica pentru rezolvarea `ImageResource`, dependency tracking si cache; comportamentul existent al `Image` ramane caracterizat.
- [x] Se adauga suport generatorului pentru declararea `ImageResource` in dictionarele `.crn` si pentru referinta tipata `$WorldAtlas` catre proprietatea scenei.
- [x] `Sprite2D` primeste contractul de resursa planificat fara sa elimine `Source`; precedenta celor doua cai este validata.
- [x] Schimbarea resursei, atasarea la alta radacina, descarcarea si eliminarea resursei invalideaza cadrul corect si nu dubleaza/elimina prematur imaginea partajata.
- [x] Se ruleaza testele focusate core si generator, apoi se reindexeaza solutia.

### Gate etapa 1

- [x] Un `.crn` minim declara atlasul o singura data si doua sprite-uri il folosesc fara cod de incarcare si fara ownership duplicat.
- [x] Testele de lifecycle demonstreaza o singura rezolvare/cache per owner si disposal numai de catre owner.

Evidenta GREEN din 2026-09-04:

- 19/19 teste core focusate trec pentru `Sprite2DImageResourceTests`, `ImageResourceCacheTests` si `ImageResourceInvalidationTests`; doua sprite-uri impart acelasi cache, schimbarea/eliminarea resursei si reatasarea la alta radacina invalideaza cadrul, iar imaginile sunt eliberate numai de cache-ul proprietar.
- 2/2 teste SourceGen focusate trec; fixture-ul minim declara un singur `ImageResource`, creeaza doua `Sprite2D` cu acelasi `ResourceId<ImageResource>` tipat si invoca fabrica generata fara cod manual de incarcare.
- 8/8 teste `SemanticScopesTests` trec dupa extinderea modelului semantic comun pentru declaratia `ImageResource` si compatibilitatea `ResourceId<T>`.
- Indexul Roslyn a fost actualizat dupa ultima modificare C# (3.631 documente, 94.648 simboluri si 385.767 referinte; cele 7 avertismente sunt baseline-ul cunoscut al solutiei).

## Etapa 2 - context comun de scena, bounds si transformari de grup

- [x] Se introduce contextul intern de inregistrare a scenei care poarta transformul compus, clip-ul/vizibilul scene-space, ordinea si accesul la resurse, fara a expune backend-uri.
- [x] Contextul ofera o singura cale exception-safe de Prism scope pentru un nod vizual emitator sau un grup/strat vizual, astfel incat fiecare astfel de nod sa nu reinventeze calculul bounds/inchiderea scope-ului; `SceneItems2D` ramane doar materializator.
- [x] `SceneNode2D.Record` si nodurile existente sunt migrate atomic la context; `RenderSurface2DFrame` ramane API-ul de desen, nu devine arbore de scena.
- [x] Se implementeaza transformul explicit de grup pe `Scene2D`/tipul stabilit la etapa 0, cu compunere determinista si conversie world/local testata.
- [x] Se defineste contractul intern de bounds pentru sprite si grup; bounds necunoscute sunt conservatoare si nu sunt eliminate eronat.
- [x] Transformul si proprietatile de prezentare ale grupului sunt `UiProperty` compatibile Aspect/Motion; fiecare sample invalideaza o data si actualizeaza bounds-urile folosite de redare/input.
- [x] Prism pe grup incadreaza exact comenzile descendentilor, foloseste bounds scene-space agregate si compune corect cu Prism pe un copil conform comportamentului caracterizat.
- [x] Se verifica ViewBox `Uniform`, `UniformToFill`, `Fill`, clip-ul, transformurile negative si transformul neinversabil.
- [x] Se reindexeaza si se ruleaza testele RenderSurface/Scene.

### Gate etapa 2

- [x] Un grup mutat/rotit/scalat deseneaza si raporteaza aceeasi geometrie calculata de helper-ul comun.
- [x] Scenariile vechi fara transform produc aceleasi comenzi si imagini ca baseline-ul.

Evidenta GREEN din 2026-09-04:

- 12/12 teste de contract ale etapei trec pentru compunerea transformurilor, originea scene-space, conversia world/local, bounds agregate, transform negativ/neinversabil, opacitate si inchiderea exception-safe a scope-urilor Transform/Prism.
- 9/9 teste existente `RenderSurface2DSceneTests` si 20/20 teste `RenderSurface2DTests`/`RenderSurface2DRenderingTests` trec; scenele fara transform pastreaza comenzile, ordinea `OnDraw` + `Scene`, ViewBox `Uniform` si bounds Prism existente.
- 3/3 teste SourceGen focusate trec, inclusiv `TransformOrigin="2,3"` in sintaxa markup reala si contractul atlasului din etapa 1.
- Indexul Roslyn a fost actualizat dupa ultima modificare C# (3.633 documente, 94.826 simboluri si 386.471 referinte; 7 avertismente baseline).

## Etapa 3 - straturi si sortare Y

- [x] Se implementeaza API-ul minim pentru straturi si mod de ordine, fara a duplica o a doua colectie de copii; daca este necesar `SceneLayer2D`, reutilizeaza implementarea `Scene2D` in loc sa o copieze. (`SceneLayer2D` nu a fost necesar; stratul este un `Scene2D` cu `Layer`.)
- [x] Sortarea construieste o vedere stabila pentru inregistrare; nu muta colectia utilizatorului si nu schimba arborele logic.
- [x] Cheia Y foloseste ancora/bounds scene-space stabilite si aplica transformurile parintelui.
- [x] Ordinea efectiva este expusa intern pentru picking si overlay-ul de debug ulterior.
- [x] Se testeaza straturi imbricate, egalitati, valori negative, schimbari runtime, visibilitate si combinatii cu `LayerDepth` existent; conflictul semantic cu `LayerDepth` este documentat si rezolvat explicit.
- [x] Proprietatile de layer/order/transform/opacity aprobate sunt controlabile prin Aspect si Motion; Motion pe Y produce resortarea in acelasi cadru logic. (Conform contractului etapei 0, `Layer`/`OrderMode` structurale sunt controlabile prin Aspect, nu sunt declarate animabile fara mixer; transformul si opacitatea sunt controlabile prin Aspect/Motion.)
- [x] Prism pe strat incadreaza numai comenzile stratului si nu include fratii anteriori/urmatori.

### Gate etapa 3

- [x] Ordinea implicita este byte-for-byte/comanda-pentru-comanda compatibila cu baseline-ul.
- [x] Sortarea Y este stabila si produce aceeasi ordine pe ambele backend-uri.

Evidenta GREEN din 2026-09-04:

- 20/20 teste de contract ale fundatiei, exceptand testul incremental rezervat etapei 4, trec: modul `Source` ramane identic, `Layer` si `LayerThenY` sunt stabile, folosesc transformurile parintelui, suporta straturi imbricate/valori negative/schimbari runtime/vizibilitate si ignora explicit `LayerDepth` la ordonarea arborelui.
- Testele demonstreaza Aspect pe `Layer`/`OrderMode`, Motion pe Y cu resortare in acelasi cadru, Motion pe opacitatea stratului si Prism limitat exact la descendentii stratului.
- 4/4 teste SourceGen RenderSurface2D trec cu sintaxa reala Aspect, `@animate with`, `@prism` cu filtru valid si `@templates`.
- Scena produce o singura lista de comenzi comuna ambelor backend-uri; testul MonoGame de ordine si cele doua teste SDL GPU de painter-order/batch-break trec (1/1, respectiv 2/2), fara cale de sortare specifica backend-ului.
- Indexul Roslyn a fost actualizat dupa ultima modificare C# (3.635 documente, 94.905 simboluri si 386.982 referinte; 7 avertismente baseline).

## Etapa 4 - actualizari incrementale `SceneItems2D`

- [x] Se mapeaza deltele `IObservableList` si `INotifyCollectionChanged` la operatii incrementale asupra nodurilor realizate si `LogicalChildren`.
- [x] `Add`, `Remove`, `Move` si `Replace` pastreaza nodurile neafectate si ciclul lor de attach; `Reset` ramane rebuild explicit.
- [x] Se actualizeaza corect indexurile/DataContext-urile afectate fara a lasa handlers, resurse sau surface references vechi.
- [x] Schimbarea `ItemsSource` si `Templates` dezaboneaza vechea sursa exact o data.
- [x] Se adauga contoare/test hooks pentru numarul de noduri create, atasate, mutate si eliminate; nu se proclama „incremental” doar din inspectia codului.
- [x] Nodurile create de `SceneItems2D.@templates` isi pastreaza Aspect/Motion/Prism, lifecycle-ul si scope-urile proprii dupa add/remove/move/replace; containerul nu introduce o a doua semantica de efect.

### Gate etapa 4

- [x] O inserare la sfarsitul unei liste de 10.000 de entitati creeaza/ataseaza un singur nod; contoarele dovedesc rezultatul.
- [x] Mutarile si inlocuirile respecta identitatea si ordinea, iar resetul reconstruieste intentionat.

Evidenta GREEN din 2026-09-04:

- 11/11 teste incrementale `SceneItems2DIncrementalContractTests` trec pentru deltele Cerneala si `INotifyCollectionChanged`: append-ul la 10.000 produce exact `+1` nod creat, `+1` atasat, `0` eliminat si `0` mutat, iar insert/remove/move/replace pastreaza identitatea nodurilor din afara intervalului afectat si actualizeaza indexul/DataContext-ul nodurilor re-realizate.
- Pentru ca indexul template-ului este capturat imuabil, mutarea re-realizeaza numai intervalul dintre pozitia veche si cea noua; contorul de mutari ramane corect `0`, in timp ce contoarele de creare/eliminare dovedesc intervalul atins. `Reset`, schimbarea template-ului si schimbarea sursei reconstruiesc intentionat setul necesar.
- Testele de lifecycle dovedesc un singur abonament activ dupa attach/reattach, o singura dezabonare a sursei inlocuite si nicio resubscriere la schimbarea template-ului. Nodurile eliminate sunt detasate inainte de scoaterea din `LogicalChildren`.
- Testul de integrare template trece impreuna cu cele 11 teste incrementale (12/12 total) si exercita Aspect, Motion si Prism dupa add/move/replace/remove si detach/reattach, cu scope-urile Prism limitate la nodurile materializate.
- Documentatia canonica `SceneItems2D` descrie actualizarile pe interval, rebuild-urile intentionate, lifecycle-ul sursei si ownership-ul Aspect/Motion/Prism. Manifestul contine exact o intrare pentru pagina canonica.
- Indexul Roslyn a fost actualizat dupa ultima modificare C# (3.635 documente, 95.003 simboluri si 387.452 referinte; 7 avertismente baseline).

## Etapa 5 - documentatie, compatibilitate si verificare

- [x] Se ruleaza skill-ul `writing-api-documentation` pentru toate API-urile publice/protected introduse sau modificate.
- [x] Se actualizeaza paginile canonice pentru `RenderSurface2D`, `SceneNode2D`, `Scene2D`, `SceneItems2D`, `Sprite2D`, tipurile noi, ghidul markup si manifestul.
- [x] Exemplele documentate folosesc exclusiv API-uri publice reale si includ resursa atlas, grup transformat, strat si sortare Y.
- [x] Exemplele si testele SourceGen includ Aspect+Motion+Prism pe grup, strat si noduri declarate in `SceneItems2D.@templates`; diagnosticele pentru proprietati neanimabile sau compozitii Prism invalide sunt verificate.
- [x] Se ruleaza testele focusate:
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RenderSurface2D|FullyQualifiedName~SceneItems2D|FullyQualifiedName~ImageResource"`.
- [x] Se ruleaza testele generatorului:
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~RenderSurface2D|FullyQualifiedName~ImageResource"`.
- [x] Se ruleaza scenariul `Tetrisish` si se verifica automat ca `OnDraw` + `Scene` continua sa functioneze; orice validare umana ramasa este declarata, nu inventata.
- [x] Se reindexeaza, se ruleaza API Compat strict si testul manifestului.
- [x] Se ruleaza `dotnet test .\Cerneala.slnx`.

Evidenta finala din 2026-09-04:

- Documentatia canonica a fost auditata cu workflow-ul `writing-api-documentation`; cele sase pagini de scena, pagina noua `SceneOrderMode`, ghidul markup si manifestul descriu API-ul implementat. Exemplul complet din `RenderSurface2D` foloseste numai tipuri publice reale si aceeasi sintaxa compilata de testul SourceGen: atlas tipat, grup transformat, `LayerThenY`, strat si Aspect/Motion/Prism pe grup, strat si radacina template-ului.
- 90/90 teste core focusate si 6/6 teste SourceGen focusate trec. Generatorul accepta exemplul pozitiv si raporteaza explicit `CERNEALAUI006` pentru Motion pe `Layer`, respectiv `PRISM1003` pentru un `@filter` invalid direct sub `@prism`.
- Auditul suplimentar al `TransformOrigin` a demonstrat prin test runtime ca Motion interpoleaza `DrawPoint` si actualizeaza proprietatea scenei; ipoteza ca ar lipsi o inregistrare dedicata in `AnimatablePropertyRegistry` a fost respinsa de executie, deci nu s-a introdus cod de productie inutil.
- 29/29 teste `Tetrisish` trec. Scenariul nou creeaza fereastra reala din markup, inregistreaza cadrul si demonstreaza automat ca ultima comanda imperativa `DrawRectangle` precede prima comanda `DrawImage` a scenei. Nu se pretinde validare vizuala/manuala umana.
- Build-ul Release al `Cerneala.csproj` trece cu zero avertismente si zero erori. Gate-ul API Compat dedicat trece in mod strict cu exact 12 suprimari pentru adaugarile publice aprobate de plan si `PermitUnnecessarySuppressions=false`; nu suprima eliminari, schimbari de semnatura sau API fara legatura. Testul manifestului trece 1/1.
- `dotnet test .\Cerneala.slnx` trece: 4.213 teste verzi si 7 teste native/pixel-conformance sarite de propriile conditii de mediu, fara esec. Rezultatele pe proiect sunt 3.291 core, 490 SourceGen, 185 Language, 118 SDL GPU, 47 Visual Studio, 40 Language Server, 13 Preview Host si 29 Tetrisish.
- Indexul final contine 3.638 documente, 95.037 simboluri si 387.594 referinte. Cele 7 avertismente sunt skip-urile/baseline-ul cunoscut al indexerului.

## Definitia de done

- [x] #2, #10, #11 si #12 au teste RED devenite verzi si documentatie canonica.
- [x] Nu exista rezolvare/cache imagine duplicata si nici ownership mutat in noduri.
- [x] Transformul si ordinea au un singur adevar reutilizabil de planurile dependente.
- [x] Grupurile, straturile si nodurile materializate din `@templates` satisfac matricea Aspect/Motion/Prism din index pe ambele backend-uri.
- [x] `SceneItems2D` este masurat incremental pentru deltele precise.
- [x] Compatibilitatea API si `Tetrisish` este demonstrata, iar suita completa trece.
