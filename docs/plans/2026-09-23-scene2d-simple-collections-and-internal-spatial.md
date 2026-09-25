# Plan: colecții simple în scena 2D, infrastructură spațială internă

> Data: 2026-09-23
> Status: **finalizat; etapele 0–3 verificate, checkpoint-ul final în audit**
> Scop: `SceneItems2D` consumă colecții obișnuite, iar contractele publice de sursă/intrare/lease/rezidență spațială dispar fără un înlocuitor public cu alt nume.
> Blocaje de la redactarea inițială: contractul public pentru încărcarea progresivă a hărților/pachetelor și semantica auxiliară `SceneItems2D` nu erau decise atunci; rezolvarea delegată și auditul etapei 0 sunt consemnate în §7. Starea gate-urilor ulterioare este consemnată în checkpoint-urile etapelor 1–3.

## 1. Decizii și limite

Decizii explicite ale utilizatorului:

- `Scene2D.Children` și utilizarea directă a `Sprite2D` rămân calea simplă pentru o scenă mică; nu sunt eliminate sau forțate prin `SceneItems2D`.
- `SceneItems2D.ItemsSource` trebuie să accepte un `IEnumerable` obișnuit și colecții observabile, inclusiv `ObservableCollection<TetrisSpriteModel>`, păstrând legarea modelelor în template-urile existente. Modelul Tetris nu mai construiește manual catalog spațial, dicționar de identități sau loader.
- Toată infrastructura publică de sursă/intrare/lease/rezidență spațială trebuie să iasă din API-ul public; mutarea internă nu înseamnă wrapper public redenumit, cale publică dublă sau API „avansat” opțional echivalent.
- `TileMap2D.FromModel` pentru hărți statice rămâne. Nu se promite obținerea automată a bounds-urilor înainte de realizarea unui model arbitrar și nici streaming lazy arbitrar prin colecția simplă.
- Funcționalitatea existentă de desen, input, coliziune și încărcare progresivă nu se șterge prin presupunere. Ruptura aprobată privește contractele publice spațiale numite, nu constituie singură o decizie de eliminare a capabilităților de hartă/pachet.
- Utilizatorul a confirmat păstrarea posibilității ca aplicația să furnizeze cod propriu pentru încărcarea hărților și obiectelor din fișierele sale sau de pe server, printr-un API public simplu, fără concepte spațiale publice. Confirmarea decide păstrarea capabilității, nu semnăturile sau semantica noului contract.
- La 2026-09-24, utilizatorul a eliminat cerința veche a unui instrument obligatoriu de interogare a referințelor semantice. Rămân obligatorii inventarul apelanților din sursa curentă, inspectarea utilizărilor în context, migrarea lor și declararea limitelor dovezilor; o potrivire textuală singură nu dovedește identitatea simbolului sau absența altor apelanți.

**Blocaj material la redactarea inițială; rezolvat ulterior în §7.** `TileMap2D.Source`, `TileMapSource2D`, catalogul/chunk-urile publice și `Scene2DPackageLevel.CreateEntitySource` expun sau produc contracte spațiale. Pachetele și loader-ele personalizate le folosesc pentru încărcare, retenție, anulare, retry și eliberare progresivă. După decizia de păstrare de mai sus, trebuia aprobat explicit contractul public *ne-spațial* pentru încărcarea proprie a hărților/obiectelor, precum și soarta fiecărei semantici de warm cache, retenție, anulare, retry și eliberare a payload-urilor; nimic nu se elimină tacit. Planul inițial nu alesese singur noul API, nu păstra semnăturile incompatibile și nu ascundea alegerea într-un adaptor.

Tot înainte de producție trebuie clasificate membrii publici `SceneItems2D.TryGetRealizedNode(string, ...)`, `Refresh()`, `Preparation`, `PreparationError` și `RealizedItemCount`: primii patru au astăzi semantică de ID/catalog, pregătire sau retry spațial; nu se presupune automat nici păstrarea, nici eliminarea lor. Se fixează și semantica publică a unui `IEnumerable` simplu: momentul enumerării/re-enumerării, politica modificărilor pe alt fir, indexul template-ului și sensul lui `Refresh` dacă supraviețuiește.

Acesta este **un singur cutover public**. O etapă „colecții simple gata de livrat” separată ar rupe apelanții existenți ai `CreateEntitySource` sau ar cere calea publică dublă interzisă.

## 2. Baseline observat și limita dovezilor

- `UI/Controls/SceneItems2D.cs`: `ItemsSourceProperty`/`ItemsSource` sunt `ISceneSpatialSource2D<object>?`. Controlul deține un `SceneSpatialResidency2D<object>` per instanță/sursă activă, selectează catalogul după interesul de cameră și coliziune, realizează nodurile după `Entry.Id`/`Version`, le atașează în `LogicalChildren` și le retrage la înlocuire/detach. Template-ul primește payload-ul; constructorul `ContentTemplateContext` folosește implicit `Index = -1` în această cale. Documentația canonică `docs-site/documentation/classes/Cerneala.UI.Controls.SceneItems2D.md` declară explicit că `IEnumerable` a fost eliminat anterior printr-o migrare incompatibilă.
- `UI/Controls/SceneSpatialSource2D.cs` și `SceneSpatialResidency2D.cs` declară familia publică `SceneSpatialEntry2D`, `SceneSpatialLease2D<T>`, `ISceneSpatialSource2D<T>`, `SceneSpatialSource2D<T>`, `SceneSpatialResidency2D<T>` și `SceneSpatialRegion2D<T>`. Rezidența partajează achiziții pe `(Id, Version)`, limitează încărcările simultane și ține payload-urile până la retragerea ultimului interes; `DisposeAsync` urmărește și eșecurile de eliberare târzie. Acestea sunt comportamente actuale de caracterizat, nu invitație la ștergerea testelor.
- `UI/Controls/TileMapSource2D.cs`, `TileMapCatalog2D.cs`, `TileMap2D.cs` și `TileMap2D.Streaming.cs` folosesc aceeași familie în semnături publice și în rezidența internă. Worktree-ul avea deja modificări ale utilizatorului pentru `TileMap2D.FromModel`, documentația lui și un test nou; implementarea planului trebuie să pornească de la starea curentă fără a le suprascrie.
- `Cerneala.Scene2D.Packages/Scene2DPackageLevel.cs` expune lease-uri pentru metadate/entități/promovări, `CreateEntitySource` și surse de tile-uri; implementarea pachetului și compilerul folosesc aceeași familie. `Tetrisish/TetrisSceneModel.cs` și `Tetrisish/Tests/TetrisGameTests.cs` sunt consumatori direcți. Un test Tetris folosește separat `SceneSpatialResidency2D<object>`; migrarea modelului singură nu acoperă API-ul.
- `UI/Controls/ISceneSpatialParticipant2D.cs` definește catalogul de simulare și pregătirea coliziunii; `UI/Controls/CollisionWorld2D.Streaming.cs` citește/cache-uiește acel catalog, `IsSimulated`/`CollisionBounds` și așteaptă `GetCollisionPreparation` înainte de a considera o regiune pregătită. Aceasta este o dependență internă cunoscută a cutover-ului, nu un posibil bug viitor. Excepția publică `SceneCollisionRegionNotReadyException.EntryId` exprimă astăzi identitatea unei intrări nepregătite.
- Testele existente `SceneSpatialSource2DTests`, `SceneSpatialResidency2DTests`, `SceneSpatialSelectionScalingTests`, `SceneItems2DIncrementalContractTests` și testele package/tile/native acoperă în sursă identitate, ordonare, culling, pregătire, anulare, refcount, eșec, retry, release, input și coliziune. Sunt **candidați de caracterizare GREEN așteptată**, de executat și confirmat la etapa 1; nu au fost rulate în această sesiune de planificare. Un test nou pentru `IEnumerable`/`ObservableCollection<T>` trebuie să fie RED pe implementarea curentă pentru motivul corect.
- Inventarul de mai jos provine din citiri directe și căutări textuale `rg`; este provizoriu, iar fiecare potrivire trebuie verificată în context înainte de a fi tratată ca utilizare a simbolului vizat. Nu s-a demonstrat absența altor apelanți și nu s-au rulat încă probele de compilare/API ale cutover-ului.

### Tabel provizoriu de migrare a apelanților inspectați

| Apelant/owner actual | Dependență observată | Destinație sau decizie necesară | Verificare după decizie |
| --- | --- | --- | --- |
| `SceneItems2D`, `SceneSimulationContext2D`, `RenderSurface2D` | sursă spațială, rezidență, readiness, noduri logice și interese de cameră/coliziune | materializare din colecție simplă; eventuale mecanisme interne distincte doar dacă un contract aprobat le cere | teste de template, attach/detach, input, coliziune, frame idle |
| `ISceneSpatialParticipant2D`, `CollisionWorld2D.Streaming.cs` | `SimulationCatalog`, `IsSimulated`/`CollisionBounds`, snapshot de interes și `GetCollisionPreparation` | integrare internă obligatorie: stabilește la etapa 0 cum nodurile realizate din colecție furnizează interesul/readiness-ul existent fără catalog public; păstrează semantica publică a coliziunii și clasifică `SceneCollisionRegionNotReadyException.EntryId` | teste de regiune nepregătită/pregătită, coliziune offscreen și mutații |
| `Tetrisish/TetrisSceneModel.cs`, `TetrisGameSurface.cs`, `MainWindow.crn`, `TetrisGameTests.cs` | catalog/loader/identități spațiale și test direct de rezidență | `ObservableCollection<TetrisSpriteModel>` și template-urile curente; migrarea testului de rezidență după decizia API, nu ștergere mecanică | build `.crn`, testele Tetris, input user-like, redare |
| `TileMap2D`, `TileMapSource2D`, `TileMapCatalog2D`, `TileMapChunkInfo2D`, `TileMap2D.Streaming.cs` | `Source`, `Spatial`, `Entries`, loader și rezidență de chunk | `FromModel` static păstrat; restul contractului progresiv **nedecis** | teste tile/cache/collision și scenariile native |
| `Scene2DPackageLevel`, `Scene2DPackage`, `PackageValueCodec`, `Scene2DPackageWriter`, package compiler | lease-uri, surse de entități/tile, loading/release progresiv | contract package/streaming **nedecis**; nicio semnătură publică spațială rămasă | suitele package/compiler, retry/release/cancel și API diff |
| `Playground/Cerneala.Playground/SceneWorldPackage.cs`, `SceneWorldShowcase.crn.cs` | consumă sursele returnate de package | migrare numai după aprobarea contractului package | build/showcase, Servo și capturi prin `Window.SaveScreenshot` |
| Testele core, SourceGen, package, SDL native menționate mai sus | construiesc explicit tipurile spațiale și verifică efecte transversale | păstrează caracterizarea funcțională, mută testarea internelor unde este potrivit și schimbă așteptările doar conform contractului aprobat | RED→GREEN, suitele afectate, conformance |
| `docs-site/documentation/classes/` și `manifest.json` | pagini publice pentru familia spațială, scene items, tile și package | sincronizare canonică cu suprafața publică finală folosind `writing-api-documentation` | manifest test și auditul linkurilor/API |
| `Scene2D.Children`, `Sprite2D`, API-urile de coliziune nelegate direct de semnăturile eliminate | compunere directă, imagine și input/collision existente | intenționat neschimbate; nu se refactorizează preventiv | testele existente de scenă, input, coliziune, backend |

Lista este **provizorie până la inventarul apelanților din etapa 0**. În mod special, importerele, markup-ul generat, benchmark-urile, testele, exemplele și documentația trebuie inspectate înainte de a fixa inventarul de fișiere sau de a elimina un membru public. Consumatorii externi soluției nu pot fi inventariați din acest repository; compatibilitatea lor se evaluează prin contractul public aprobat, ApiCompat și documentație, fără a pretinde acoperire directă.

## 3. Arhitectură țintă condiționată

`SceneItems2D` rămâne materializatorul de noduri logice sub un `Scene2D`; fiecare instanță de control deține realizările, abonarea la colecția observabilă și ciclul lor de attach/detach. Datele aplicației nu devin proprietatea controlului. `IEnumerable` simplu nu oferă metadata spațială înainte de realizare: bounds/picking/collision trebuie să se bazeze pe nodurile realizate și pe contractele de scenă existente, nu pe bounds fictive sau pe un catalog public ascuns. Nu se promite că o colecție de 10.000 de obiecte va realiza doar viewport-ul. Ordinea sursei, duplicatele, `null`, rebind-ul, deltele `Add`/`Remove`/`Move`/`Replace`/`Reset` și DataContext-ul/indexul template-ului se fixează prin teste de contract înainte de implementare.

Notificările și schimbările de template/sursă trebuie să păstreze regula owner-thread/`UiRelay`, un singur abonament activ, lipsa publicărilor târzii în controlul înlocuit, invalidarea necesară fără frame-uri idle perpetue și lifecycle-ul normal Aspect/Motion/Prism, resurselor imagine, inputului și colliderelor. Nu se creează un al doilea arbore de input sau un owner de resurse imagine în sprite. `Scene2D.Children` și `Sprite2D` rămân compoziție directă.

Mecanismele spațiale necesare contractelor păstrate **rămân interne și gestionate de framework** în ownerii reali (`SceneItems2D`/simulare/coliziune, tilemap, package); asta nu cere păstrarea oarbă a fiecărui detaliu al implementării vechi. Integrarea exactă de ownership, proprietatea datelor încărcate, regulile de anulare/retire și granița publică package/map nu pot fi specificate definitiv înainte de decizia de la etapa 0. A ascunde vechiul `ISceneSpatialSource2D<T>` sub alt nume public nu satisface ținta.

**Non-obiective:** un ECS, o nouă familie de evenimente de input, schimbarea semanticii `Scene2D.Children`, streaming lazy pentru orice `IEnumerable`, pre-bounds automate, optimizări speculative, rescrierea coliziunii sau a backend-urilor și eliminarea funcțiilor package/map fără aprobare explicită.

## 4. Fișiere estimate, nu inventar final

- Core: `UI/Controls/SceneItems2D.cs`, `SceneSpatialSource2D.cs`, `SceneSpatialResidency2D.cs`, `SceneSimulationContext2D.cs`, `RenderSurface2D.cs`, `ISceneSpatialParticipant2D.cs`, `CollisionWorld2D.Streaming.cs` (ultimele două sunt dependențe certe); alte adaptări de scenă/input/coliziune numai dacă RED-urile le atribuie încălcarea.
- Tile/package: `UI/Controls/TileMap2D*.cs`, `TileMapSource2D.cs`, `TileMapCatalog2D.cs`, `Cerneala.Scene2D.Packages/`, `Tools/Cerneala.Scene2D.PackageCompiler/` și importerele doar după inventarul apelanților/decizia de contract.
- Consumatori: `Tetrisish/`, `Playground/Cerneala.Playground/` și toate celelalte utilizări identificate la etapa 0.
- Teste: `tests/Cerneala.Tests/Controls/`, `tests/Cerneala.Tests.SourceGen/`, `tests/Cerneala.Tests.Scene2DPackages/`, `tests/Cerneala.Tests.Scene2DImporters/`, `tests/Cerneala.Tests.SdlGpu/`, `Tetrisish/Tests/`; fixture-ul nativ se schimbă numai dacă o migrare aprobată o cere.
- Documentație: paginile canonice afectate sub `docs-site/documentation/classes/` și `docs-site/documentation/manifest.json`. Artefactul ApiCompat al acestei schimbări trebuie creat separat de proiectele istorice cu căi absolute/suppressions vechi.

## 5. Etape de implementare — etapele 0–3 verificate

### Etapa 0 — deblocarea contractului și inventarul apelanților

- [x] Obține acordul explicit al utilizatorului pentru soarta încărcării progresive package/map, a loader-elor personalizate, warm cache-ului, anulării/retry-ului și eliberării payload-urilor, inclusiv semnătura publică rămasă sau eliminarea deliberată a fiecărei capabilități. Nu inventa un provider nou și nu păstra vechea cale ca API „avansat”. (Rezolvat prin delegarea explicită a alegerilor către agent; semnăturile nu au fost aprobate individual.)
- [x] Decide explicit soarta `SceneItems2D.TryGetRealizedNode`, `Refresh`, `Preparation`, `PreparationError`, `RealizedItemCount` și contractul exact al enumerării, indexului de template, deltelor și notificărilor off-thread pentru colecția simplă.
- [x] Fixează contractul intern `ISceneSpatialParticipant2D`/`CollisionWorld2D.Streaming` după eliminarea catalogului public: cum nodurile realizate din colecție contribuie la interesul de coliziune, când `PrepareRegionAsync`/`GetCollisionPreparation` raportează readiness și ce înseamnă `SceneCollisionRegionNotReadyException.EntryId`. Păstrează semantica publică a coliziunii; nu o redesena în această schimbare.
- [x] Pentru fiecare tip și membru public/protected mutat/eliminat, inventariază definițiile și utilizările din proiectele soluției prin citiri directe și căutări `rg` cu scope explicit; inspectează fiecare candidat în context și corelează importerele, markup-ul generat, benchmark-urile, testele, exemplele și documentația. Arhivează comenzile, scope-ul, versiunea sursei, rezultatele și limitele de acoperire; completează tabelul cu fiecare apelant identificat, migrarea sau motivul pentru care rămâne neschimbat. Nu transforma potrivirile textuale în dovadă de identitate sau de absență a altor apelanți; investighează orice lacună materială.
- [x] Înregistrează starea inițială a worktree-ului și distinge modificările utilizatorului, mai ales `TileMap2D.FromModel`, documentația și testul lui; nu le reseta și nu le revendica drept efect al acestui plan.
- [x] Îngheață o matrice de contract pentru lifecycle, proprietar de resurse, ordonare, template, input/coliziune, rendering/Prism, source generator și package/map după decizia utilizatorului. Stabilește pentru WindowsDX, SDL_GPU, Linux și macOS care probe sunt obligatorii și executabile; orice platformă neexecutată rămâne **blocată** până la decizie explicită de waiver, fără a moșteni waiverele planurilor vechi.

**Gate etapa 0**

- [x] Contractul public package/map și membrii auxiliari `SceneItems2D` sunt aprobați; inventarul din sursa curentă acoperă proiectele relevante și fiecare apelant identificat are o migrare sau un contract intenționat neschimbat, fără lacune materiale neinvestigate. Până atunci **nu se modifică producția și planul rămâne neimplementabil**. (Autoritate de decizie delegată de utilizator; audit independent al artefactelor etapei 0 acceptat. Nu reprezintă GREEN pentru etapele 1–3.)

### Etapa 1 — caracterizare GREEN și regresii RED înainte de cutover

- [x] Rulează și arhivează testele de caracterizare existente ale `SceneItems2D`, surselor/rezidenței, tilemap-ului, pachetelor, coliziunii, Prism, SourceGen și Tetris; verifică statutul GREEN așteptat și separă skip-urile native de execuția reală. Nu transforma testele actuale de streaming în „RED” și nu le șterge pentru a obține GREEN.
- [x] Adaugă cel mai mic consumer/probe de compilare RED pentru `SceneItems2D.ItemsSource` cu `IEnumerable` și `ObservableCollection<TetrisSpriteModel>`; eșecul trebuie să fie incompatibilitatea reală a contractului curent, nu fixture-ul, build-ul sau mediul.
- [x] Adaugă RED deterministe pentru colecție simplă: ordine și duplicate, `null`/înlocuire, deltele observabile, DataContext și indexul definit la etapa 0, template swap, attach/detach/reattach, notificări off-thread conform politicii aprobate, și păstrarea identității nodurilor neafectate unde contractul o cere. Testele de input folosesc click/key user-like, nu doar setări de proprietăți.
- [x] Adaugă RED de API public care arată familia și semnăturile spațiale încă expuse; pentru package/map adaugă numai RED-uri pentru contractul de înlocuire **aprobat**, nu pentru un API imaginat.
- [x] Fixează baseline-ul binar/API al worktree-ului curent într-un artefact propriu și pregătește `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` strict, cu numele parametrilor verificate, fără a copia căile absolute sau suppressions din `docs/plans/evidence/2026-09-04-scene-import-stage6/api-compat.proj`.

**Gate etapa 1**

- [x] GREEN-urile de caracterizare sunt verzi pe baseline; fiecare RED nou eșuează exclusiv pentru comportamentul/API-ul lipsă și este reprodus cu comanda și output-ul arhivate. Nicio așteptare nouă nu este stabilită prin simpla ștergere a unui test vechi. (Auditul independent a acceptat dovezile integrate după corecțiile fixture/oracle; comportamentele din corpul RED-urilor încă nu au rulat după guard-urile API și rămân de verificat în etapa 2.)

### Etapa 2 — cutover atomic al graniței publice și al apelanților

- [x] Migrează `SceneItems2D.ItemsSource` și realizarea template-urilor la colecția simplă aprobată, fără cale publică duală; păstrează ordinea, DataContext/indexul decis, identitatea acolo unde se cere, lifecycle-ul și invalidările corecte.
- [x] Migrează modelul Tetris la `ObservableCollection<TetrisSpriteModel>` și elimină catalogul/dicționarul/loader-ul manual; păstrează compunerea `OnDraw` + scenă, animația, inputul și identitatea pieselor conform testelor existente.
- [x] Migrează **în același cutover** API-urile `TileMap2D`/catalog/chunk și package/compiler/Playground conform contractului semnat la etapa 0; păstrează `TileMap2D.FromModel` static și funcțiile de streaming aprobate. Nu lăsa o semnătură publică ce cere tipurile eliminate și nu introduce aliasuri/wrappere publice spațiale.
- [x] Mută mecanica spațială rămasă în ownerii interni necesari; păstrează lifetime-ul per instanță, achizițiile partajate unde contractul le cere, anularea/retry-ul și eliberarea inclusiv la eșec, detach, înlocuire și disposal. Nu muta ownership-ul imaginilor din cache/sesiune în sprite sau în model.
- [x] Adaptează obligatoriu `ISceneSpatialParticipant2D` și `CollisionWorld2D.Streaming` la contractul intern decis la etapa 0; menține pregătirea regiunii, interesul colliderelor simulate și readiness-ul public fără metadate spațiale fabricate înainte de realizare.
- [x] Migrează toate utilizările inventariate, inclusiv markup/source generator, teste, fixture-uri și exemple; actualizează testele vechi numai după legarea așteptării de contractul aprobat. Nu atinge cod fără legătură.
- [x] Folosește `writing-api-documentation` pentru toate modificările public/protected: actualizează/elimină paginile canonice relevante numai conform suprafeței finale și sincronizează `docs-site/documentation/manifest.json`; exemplele compilează cu API-uri reale.

**Gate etapa 2**

- [x] Noile RED-uri sunt GREEN; suitele core, SourceGen, package, importeri și Tetris afectate trec. ApiCompat clasifică **fiecare** eliminare/schimbare drept aprobată și nu conține rupturi suplimentare; nicio referință publică la familia eliminată sau cale duală nu rămâne. Documentația canonică și manifestul descriu aceeași suprafață.

### Etapa 3 — conformance, cost și verificare completă

- [x] Reexecută reproducerea consumatorului Tetris și scenariile package/map native după cutover: încărcare/anulare/retry/release, pan/zoom, template swap, input/collision, Prism și cache de imagini. Capturile aplicației Cerneala se fac exclusiv cu `Window.SaveScreenshot`; conformance-ul Windows SDL_GPU se judecă după contract. WindowsDX a fost exclus explicit din gate-ul curent prin recordul de supersesiune din secțiunea 10, nu testat.
- [x] Verifică în teste deterministe că un cadru idle stabil nu reface materializarea, layout-ul sau invalidarea fără cauză, iar deltele observabile afectează numai intervalele impuse de contract. Nu cere vechiul gate „10.000 de intrări realizează doar viewport-ul” pentru `IEnumerable` fără metadata pre-realizare.
- [x] Rulează focused core `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~SceneItems2D|FullyQualifiedName~SceneSpatial|FullyQualifiedName~TileMap|FullyQualifiedName~Collision"`, apoi proiectele `tests/Cerneala.Tests.SourceGen`, `tests/Cerneala.Tests.Scene2DPackages`, `tests/Cerneala.Tests.Scene2DImporters`, `tests/Cerneala.Tests.SdlGpu` și `Tetrisish/Tests/Tetris.Tests.csproj` cu filtrele contractelor migrate; arhivează comenzile exacte, numărul de pass/fail/skip și motivele skip-urilor.
- [x] Rulează probele native relevante cu opt-in-ul real `CERNEALA_SDL_NATIVE_TESTS=1` pe runner Windows configurat, inclusiv `NativePackageWarmStreamingTests`, `NativePackageGridSubdivisionTests` și `NativeScenePrismStreamingTests`; suita SDL fără opt-in nu îndeplinește acest gate. Linux, macOS și WindowsDX au fost excluse explicit din gate-ul **acestei schimbări** prin secțiunea 10; nu sunt declarate testate.
- [x] Rulează testul de manifest `dotnet test .\tests\Cerneala.Tests.VisualStudio\Cerneala.Tests.VisualStudio.csproj --filter "FullyQualifiedName~Stage6ReleaseHarnessTests.ApiDocumentationManifestIsValidAndReferencesExistingFiles"`, ApiCompat strict parametrizat pentru acest baseline și `dotnet test .\Cerneala.slnx`; investighează fiecare eșec și fiecare skip relevant. Rulează `git diff --check` și verifică absența artefactelor temporare.

**Gate etapa 3**

- [x] Reproducerea inițială, testele focusate, suitele afectate, suita completă, API diff-ul, documentația și conformance-ul cerut sunt verificate. Orice gate obligatoriu neexecutat rămâne blocant; validarea umană se declară separat dacă este cerută, nu se pretinde.

## 6. Ordine, condiții de oprire și definiția de gata

Ordine: **decizia și inventarul apelanților (etapa 0) → baseline GREEN/RED (etapa 1) → cutover public atomic (etapa 2) → conformance și full suite (etapa 3)**. Nu se începe etapa următoare fără gate-ul etapei curente și auditul checkpoint-ului; checklist-ul se bifează numai după dovada verificată.

Oprește implementarea dacă: contractul package/map rămâne ambiguu; inventarul apelanților are lacune materiale neinvestigate; o modificare din worktree-ul utilizatorului se suprapune nesigur; un test RED eșuează din fixture/mediu; un gate de platformă sau API relevă o ruptură neaprobată. Nu transforma aceste opriri în shim-uri, suppressions globale sau presupuneri despre produs.

**Definiția de gata**

- [x] `SceneItems2D` acceptă colecții simple/observabile și Tetris folosește `ObservableCollection<TetrisSpriteModel>` fără catalog/dicționar/loader spațial manual, cu template binding, input și lifecycle demonstrate.
- [x] Niciun API public/protected din soluție nu expune familia de sursă/intrare/lease/rezidență spațială; migrarea fiecărui apelant identificat este verificată, iar `Scene2D.Children`, `Sprite2D` și `TileMap2D.FromModel` rămân disponibile.
- [x] Funcțiile package/map/streaming aprobate au comportamentul, lifetime-ul și verificarea native convenite; nicio funcție nedeclarată ca eliminată nu dispare tacit.
- [x] RED→GREEN, suitele afectate și complete, API Compat strict, documentația canonică/manifestul și gate-urile de platformă cerute sunt închise cu dovezi reproductibile. Nicio validare manuală neexecutată nu este pretinsă.

**Starea istorică la prima decizie din 2026-09-24, înainte de checkpoint:** plan de decizie și roadmap verificabil, **nu** autorizare de a porni producția. Blocajele de contract din antet erau încă deschise; cerința de instrument obligatoriu de interogare semantică fusese eliminată. Recordul ulterior de mai jos și checkpoint-ul etapei 0 supersedează numai acele blocaje de contract.

## 7. Record ulterior de supersesiune — 2026-09-24, etapa 0 verificată

Paragraful de stare anterior și blocajele din antet consemnează **starea istorică în momentul redactării planului**. Ulterior, utilizatorul a delegat explicit agentului alegerea blocajelor de design, API, arhitectură și compatibilitate și a cerut continuarea fără validare individuală a fiecărei semnături. Această delegare nu este un waiver pentru teste, platforme, ApiCompat sau audit. Nu rescriem deciziile istorice ca și cum ar fi fost aprobate individual.

Contractul selectat sub acea autoritate este documentat în [recordul de decizie pentru etapa 0](2026-09-24-scene2d-stage0-contract-proposal.md): `SceneItems2D` folosește ocurențe `IEnumerable`/colecții observabile cu fault terminal numai după eșecul mutației structurale; pachetele CPV2 primesc un reader asincron de range-uri ne-spațial și închidere async drenată; hărțile package păstrează streaming-ul privat, iar `Collider2D.IsSimulated` marchează numai geometria propriului collider deja materializat. Reducerile de capabilitate pentru formatul custom arbitrar, `CreateEntitySource` pre-load și editarea incrementală `CellPublication` sunt explicite în acel record, nu presupuse echivalente. Formatul CPV2 scris anterior rămâne obligatoriu lizibil.

Auditul strict ApiCompat din etapa 2 a găsit o ruptură **neaprobată** în candidatul intermediar: `TileMapChunkData2D` fusese internalizat, deși inventarul etapei 0 îl identifica drept valoare de payload fără dependență publică de familia de sursă/lease și interzicea eliminarea mecanică. Sub autoritatea de design delegată, coordonatorul root a ales păstrarea sa **publică**, cu semnătura existentă; sursa/catalogul/header-ele rămân interne. CP0001 pentru acest tip este un RED de reparat, nu o eliminare pusă pe lista de aprobări. Pagina canonică și manifestul trebuie restaurate/sincronizate, iar strict ApiCompat și testul de suprafață publică trebuie rerulate după reparație. Această decizie nu marchează gate-ul etapei 2 ca îndeplinit.

[Baseline-ul worktree-ului](evidence/2026-09-23-scene2d-stage0/worktree-baseline.md), [inventarul contextual al apelanților](evidence/2026-09-23-scene2d-stage0/caller-inventory.md) și [matricea contractului/platformelor](evidence/2026-09-23-scene2d-stage0/contract-and-platform-matrix.md) sunt artefactele verificabile ale etapei 0. Auditorul independent a acceptat contractul/inventarul/matricea integrate după corecția precisă `Collider2D.Enabled` versus `UIElement.IsEnabled`; coordonatorul root a autorizat numai atunci bifarea celor șase sarcini și a gate-ului etapei 0. Verificările mecanice ale checkpoint-ului: `git diff --check` și scanarea whitespace/conflict-marker pentru Markdown neindexat; niciun build/test/harness/API diff nu a fost rulat în etapa 0. La acel checkpoint, etapele 1–3 și definiția de gata erau nebifate. WindowsDX nu are acum harness executabil identificat; Linux/macOS nu au fost rulate pe hostul Windows. Aceste gate-uri ulterioare rămân obligatorii sau necesită un waiver nou explicit.

## 8. Checkpoint etapa 1 — 2026-09-24

Auditorul independent a acceptat candidatul integrat al etapei 1 după repararea
testelor RED și a fixture-urilor; coordonatorul root a autorizat apoi bifarea
exclusiv a celor cinci sarcini și a gate-ului acestei etape. Comenzile exacte,
logurile/TRX brute, clasificările per test, hash-urile surselor și intrărilor
înghețate sunt în [ledgerul etapei 1](evidence/2026-09-23-scene2d-stage1/stage1-evidence-ledger.md)
și [recordul de integritate](evidence/2026-09-23-scene2d-stage1/stage1-integrity-current.txt).

Pe baseline-ul worktree-ului au trecut **1.304** teste de caracterizare
(Core filtrat 410, SourceGen 610, package 81, importeri 173, Tetris 30).
Cele șase teste SDL native au fost **skip**, nu executate ca conformance.
Consumer-ul de compilare izolat a eșuat numai cu **două CS0266** intenționate.
RED-urile curente au **50 eșecuri API intenționate** (Core 28, package 22),
clasificate individual; nu au fost șterse testele vechi de streaming.
Corpurile de comportament din spatele guard-urilor API lipsă **nu au fost încă
executate**: RED→GREEN și regresiile pe codul nou aparțin etapei 2.

Baseline-ul binar actual și proiectul strict `ValidateAssembliesTask` sunt
pregătite; numai self-compare-ul identic este GREEN. **ApiCompat după cutover**
și clasificarea fiecărei rupturi aprobate rămân gate-ul etapei 2. Producția,
documentația API canonică și manifestul nu au fost schimbate în etapa 1.
Gate-urile native/platformă și full suite rămân deschise; niciun skip sau
self-compare nu este declarat waiver ori dovadă de conformance.

Verificările mecanice ale acestui checkpoint: `git diff --check` exit 0 și
scanarea planului neindexat pentru whitespace la sfârșit de linie și markeri de
conflict, fără probleme. Aceste verificări nu substituie auditul checklist-ului
după bifare sau vreun gate al etapei 2.

## 9. Checkpoint etapa 2 — 2026-09-24

Auditorul independent a acceptat candidatul integrat **după** remedierea a
două findings: `SceneItems2D` păstrează snapshot-ul B deja enumerat la
rebind/template recovery și nu re-enumeră o sursă one-shot la editarea
reentrantă a template-ului; fixture-ul `NativePackageWarmStreamingTests`
detașează hărțile înainte de disposal terminal și continuă drenarea. RED-urile
focusate, corecțiile, suitele și hash-urile curente sunt în
[ledgerul integrat al etapei 2](evidence/2026-09-23-scene2d-stage2/integration/stage2-integrated-verification.md),
[dovezile SceneItems](evidence/2026-09-23-scene2d-stage2/sceneitems/verification.md)
și [review-ul ApiCompat](evidence/2026-09-23-scene2d-stage2/integration/api-compat-review.md).
Coordonatorul root a autorizat bifarea exclusiv a celor șapte sarcini și a
gate-ului etapei 2 după această acceptare; nu este un waiver al etapei 3.

Pe sursa curentă, filtrul Core afectat a trecut **1.446** teste, cu un skip
istoric native/pixel conformance; SourceGen **610**, package **104** și Tetris
**31** au trecut după ultima corecție SceneItems. Language **234** (plus un test
P95 preexistent explicit skip) și importerii **173** provin din rulările
anterioare ale etapei 2: regulile Language și sursa importerilor nu au fost
schimbate de cele două reparații SceneItems/test native; aceste suite nu au
fost pretinse ca rerulate pe ultimul DLL Core.
Proiectul SDL_GPU afectat a compilat cu 0 erori/avertismente, fără rulare
native. Testul oficial al manifestului a trecut **1/1** după ultima corecție
de documentație. Strict ApiCompat a raportat **23 Core + 20 package**
diagnostice CP, fiecare clasificat exact ca ruptura/adăugarea selectată;
comenzile au exit **1**, nu succes fals ori suppression global. Ruptura
neaprobată a payload-ului `TileMapChunkData2D` a fost reparată și nu mai apare.

Etapa 3 și definiția de gata rămân **nebifate**: probele native cu opt-in,
WindowsDX/platformele cerute, conformance-ul, suita completă a soluției și
eventuala validare umană nu sunt demonstrate de acest checkpoint.

## 10. Supersesiune limitată a gate-urilor de platformă — 2026-09-24

După inventarul etapei 3, utilizatorul a decis explicit: „Nu avem cum sa
testam Linux si MacOs pe statia asta. Toate verificarile/gateurile raman doar
la nivel de Windows”, apoi „Scoate WindowsDX; gate-urile sunt Windows SDL”.
Pentru **această schimbare**, gate-urile de platformă rămase aplicabile sunt
numai cele Windows SDL_GPU cu opt-in-ul nativ real. Linux, macOS și WindowsDX
nu au fost testate și sunt scoase din scope-ul de acceptare prin această
decizie, nu bifate ca rezultate GREEN. Matricea etapei 0 păstrează decizia
istorică de dinaintea acestei supersesiuni; nu trebuie citită ca o dovadă de
execuție. Skips-urile existente în suita Windows se raportează separat și nu
devin pass prin această revizuire. Nicio verificare manuală nu este implicată.

## 11. Checkpoint etapa 3 și definiția de gata — 2026-09-24

Auditorul independent a acceptat sursa, testele și gate-urile integrate ale
etapei 3 după o corecție strict de evidență: noile teste deterministe
idle/delta au trecut fără fază RED sau fix de producție în etapa 3. Coordonatorul
root a autorizat apoi bifarea celor cinci sarcini, a gate-ului etapei 3 și a
celor patru puncte de definiție de gata. Auditul separat al **acestui
checkpoint editat** rămâne de confirmat.

[Ledgerul etapei 3](evidence/2026-09-23-scene2d-stage3/stage3-verification.md),
[integritatea finală](evidence/2026-09-23-scene2d-stage3/integration/final-integrity.md)
și logurile/TRX brute din `evidence/2026-09-23-scene2d-stage3/integration/`
documentează reproducerea Windows SDL cu opt-in, testele deterministe de
16 cadre idle fără noi measure/arrange/invalidation, filtrul Core **328/328**,
importerii **173/173**, manifestul și suita completă build-enabled
**6.303 pass, 0 fail, 6 skip** în 10 proiecte. Nu se pretinde CPU sau alocări
zero. Cele șase cazuri disabled sunt investigate și raportate individual,
**nu** considerate pass. Strict ApiCompat a ieșit **1** pentru cele **23 Core +
20 package** diagnostice CP intenționate, fiecare identic cu clasificarea
aprobată; nu a fost adăugat suppression global.

Conform §10, numai gate-urile Windows SDL sunt aplicabile acestei schimbări;
Linux, macOS și WindowsDX rămân **netestate**, nu GREEN. Documentația canonică
și manifestul au fost verificate în suita VisualStudio **47/47**. `git diff
--check` final a ieșit **0**, iar scanările scoped ale artefactelor temporare
nu au găsit candidați; nu s-au șters artefactele preexistente ale utilizatorului.
Nu a avut loc validare manuală umană și nu s-a făcut Git publish.
