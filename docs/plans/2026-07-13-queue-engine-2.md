# Plan: Queue Engine 2.0

> Data: 2026-07-13
> Status: finalizat
> Scop: modernizarea cozilor de invalidare si a ordonarii elementelor fara schimbarea contractelor publice sau a semanticii frame-ului

## 1. Rezumat executiv

Queue Engine 2.0 va inlocui implementarea repetata `HashSet<UIElement> + List<UIElement>` din cozile de invalidare cu un nucleu intern comun, cu operatii O(1) pentru interogarile uzuale si cu o singura indexare a ordinii vizuale pentru fiecare versiune a arborelui.

Schimbarea urmareste patru probleme concrete:

- `HasWork` construieste snapshot-uri si poate traversa arborele doar ca sa afle daca o coada este goala;
- fiecare coada isi recalculeaza separat ordinea elementelor;
- eliminarea elementelor foloseste cautari si stergeri liniare din lista;
- aceeasi logica de coada este duplicata in mai multe clase si risca sa evolueze diferit.

Implementarea va pastra clasele publice existente si comportamentul observabil. Nu facem o revolutie cu furca in scheduler; schimbam motorul de sub capota si verificam fiecare surub.

## 2. Baseline si problema actuala

### 2.1 Implementarea actuala

Urmatoarele cozi folosesc aceeasi structura de baza:

- `LayoutQueue`;
- `RenderQueue`;
- `AspectQueue`;
- `HitTestQueue`;
- `InheritedPropertyQueue`;
- `CommandStateQueue`.

Modelul curent este, in esenta:

```text
HashSet<UIElement> membership
List<UIElement> order
Snapshot() -> ElementQueueOrder.Sort(root, order)
Remove(element) -> order.RemoveAll(...)
HasWork -> Snapshot().Count > 0
```

### 2.2 Costuri observate

- [x] Capturam un baseline reproductibil pentru `HasWork`, snapshot si drain.
- [x] Confirmam numarul de traversari complete ale arborelui pe un frame idle.
- [x] Confirmam alocarile produse de interogarile repetate `HasWork`.
- [x] Confirmam curba de scalare pentru golirea unei cozi cu 100, 1.000 si 10.000 de elemente.
- [x] Salvam rezultatele baseline in artefactul de benchmark al proiectului.

Observatia curenta, care trebuie transformata in test si masuratoare:

- `ElementQueueOrder.Sort` construieste un dictionar al intregului arbore pentru fiecare snapshot;
- `LayoutQueue.HasWork` cere doua snapshot-uri, unul pentru measure si unul pentru arrange;
- `Scheduler.HasWork` poate fi consultat din mai multe puncte ale aceluiasi update;
- un frame idle poate ajunge sa traverseze acelasi arbore de mai multe ori fara sa existe lucru real;
- `List.RemoveAll` executat pentru fiecare element poate transforma golirea unei cozi mari intr-un cost apropiat de O(Q²).

## 3. Obiective masurabile

- [x] `HasWork` este O(1) pentru fiecare coada.
- [x] `HasWork` nu construieste snapshot-uri.
- [x] `HasWork` nu traverseaza arborele vizual.
- [x] `Contains`, `Enqueue` si `Remove` sunt O(1) amortizat.
- [x] Snapshot-ul sorteaza numai elementele aflate efectiv in coada.
- [x] Ordinea vizuala completa este indexata cel mult o data pentru aceeasi valoare `UIRoot.TreeVersion`.
- [x] Indexul ordinii este reutilizat de toate cozile aceluiasi root.
- [x] Golirea unei cozi nu mai face stergeri liniare repetate dintr-o lista auxiliara.
- [x] Ordinea publica a snapshot-urilor ramane identica cu ordinea vizuala actuala.
- [x] Semantica snapshot-ului schedulerului ramane neschimbata.
- [x] Nicio semnatura publica nu este modificata.
- [x] Toate testele existente raman verzi.
- [x] Diagnosticul Playground pentru scenariul reparat ramane la o singura masurare utila.

## 4. Non-obiective

Queue Engine 2.0 nu va include urmatoarele schimbari:

- [x] Nu rescriem algoritmii `MeasureCore` sau `ArrangeCore` ai controalelor.
- [x] Nu introducem un nou state machine pentru layout.
- [x] Nu adaugam layout multi-pass limitat printr-un frame budget.
- [x] Nu optimizam separat `Grid`, `StackPanel` sau alte panouri.
- [x] Nu schimbam politica de invalidare a proprietatilor.
- [x] Nu schimbam ordinea fazelor din `UiFrameScheduler`.
- [x] Nu introducem concurenta sau procesare paralela a cozilor.
- [x] Nu expunem un API public nou doar pentru diagnostic.
- [x] Nu combinam aici cache-ul de render, hit-testing sau alte optimizari fara legatura directa cu motorul cozilor.

Acestea pot deveni proiecte separate dupa ce motorul cozilor are costuri predictibile. Altfel am pune turbo pe masina in timp ce schimbam rotile, ceea ce e spectaculos doar pana intra gardul in discutie.

## 5. Contracte care trebuie pastrate

### 5.1 Contractul cozilor

- [x] Un element poate exista cel mult o data intr-o coada.
- [x] Identitatea elementului este referentiala, nu bazata pe egalitate de valoare.
- [x] Re-enqueue dupa `Remove` functioneaza normal.
- [x] `Snapshot` nu modifica implicit elementele valide din coada.
- [x] Elementele detasate nu sunt returnate consumatorului.
- [x] O mutatie de arbore poate lasa in coada invalidarea root-ului care reprezinta acea mutatie.
- [x] Snapshot-urile publice pastreaza ordinea vizuala determinista.

### 5.2 Contractul special al `LayoutQueue`

- [x] Metadata `LayoutQueueEntryKind` este pastrata.
- [x] Prioritatea ramane `Direct > Required > Propagated`.
- [x] O invalidare cu prioritate mai mare promoveaza intrarea existenta.
- [x] O invalidare cu prioritate mai mica nu retrogradeaza intrarea existenta.
- [x] `SnapshotMeasure` public ramane parent-first.
- [x] Snapshot-ul intern folosit de measure incremental poate ramane bottom-up acolo unde contractul actual o cere.
- [x] Measure si arrange raman cozi logic distincte.

### 5.3 Contractul schedulerului

- [x] Fiecare faza proceseaza un snapshot stabil.
- [x] Re-enqueue in aceeasi faza este amanat pentru frame-ul urmator.
- [x] Lucrul produs pentru o faza ulterioara poate fi consumat in acelasi frame.
- [x] Daca procesarea arunca o exceptie, elementul curent si restul snapshot-ului nu se pierd.
- [x] Ordinea fazelor ramane neschimbata.
- [x] Un frame fara invalidari nu porneste lucru artificial.

## 6. Arhitectura propusa

### 6.1 Nucleu intern reutilizabil

Introducem un tip intern generic, cu un nume final stabilit in implementare, de forma:

```csharp
internal sealed class ElementWorkQueue<TMetadata>
{
    private readonly Dictionary<UIElement, TMetadata> entries;

    public int Count { get; }
    public bool HasWork { get; }
    public bool Contains(UIElement element);
    public void Enqueue(UIElement element, TMetadata metadata);
    public bool Remove(UIElement element);
    public IReadOnlyList<ElementWorkItem<TMetadata>> Snapshot(UIRoot root);
}
```

Responsabilitati:

- stocheaza o singura intrare pentru fiecare instanta `UIElement`;
- foloseste comparare prin referinta;
- aplica o functie explicita de merge/promotion pentru metadata;
- expune `Count` si `HasWork` fara snapshot;
- elimina intrarile direct din dictionar;
- sorteaza doar cheile aflate in coada cand se cere snapshot;
- curata defensiv intrarile care nu mai apartin root-ului.

Pentru cozile fara metadata folosim un tip intern minimal, nu sase implementari copiate cu acelasi rahat in alta palarie.

### 6.2 Index comun al ordinii vizuale

Introducem un serviciu intern asociat root-ului, de forma:

```text
ElementQueueOrderIndex
  root
  indexedTreeVersion
  Dictionary<UIElement, int> preorderOrdinal
```

Reguli:

- indexul este construit lazy, doar la primul snapshot care are efectiv elemente;
- cheia de validare este `UIRoot.TreeVersion`;
- toate cozile aceluiasi root folosesc acelasi index;
- o mutatie de arbore invalideaza logic indexul prin schimbarea versiunii;
- reconstruirea inlocuieste complet dictionarul vechi pentru a nu pastra referinte stale;
- ordinea este exact preorder-ul vizual folosit acum;
- elementele absente din index sunt considerate detasate si sunt eliminate defensiv din snapshot.

### 6.3 Pastrarea wrapperelor existente

Clasele publice actuale raman fatada compatibila:

```text
RenderQueue -------------------+
AspectQueue -------------------|
HitTestQueue ------------------|--> ElementWorkQueue<Unit>
InheritedPropertyQueue --------|
CommandStateQueue -------------+

LayoutQueue ----------------------> ElementWorkQueue<LayoutQueueEntryKind>
                                    + doua instante: measure si arrange
```

Avantaje:

- consumatorii nu trebuie modificati;
- API-ul public si documentatia raman stabile;
- migrarea se poate face coada cu coada;
- nucleul comun poate fi testat separat de scheduler.

### 6.4 Curatarea elementelor detasate

Folosim doua niveluri de protectie:

1. Curatare activa la detach, printr-un punct intern unic al root-ului care elimina elementul sau subarborele din toate cozile relevante.
2. Curatare defensiva la snapshot, pentru cazurile in care o cale rara de detach ocoleste mecanismul principal.

Conditii:

- [x] Curatarea activa nu elimina invalidarea root-ului produsa de mutatia arborelui.
- [x] Curatarea unui subarbore nu traverseaza intregul arbore ramas.
- [x] Snapshot-ul verifica numai elementele aflate in coada, nu toate elementele vizuale.
- [x] `HasWork` ramane O(1); nu ascundem o traversare sub un getter cu fata nevinovata.

## 7. Structura de fisiere estimata

Fisiere noi posibile:

- `Cerneala/UI/Invalidation/ElementWorkQueue.cs`;
- `Cerneala/UI/Invalidation/ElementQueueOrderIndex.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementWorkQueueTests.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementQueueOrderIndexTests.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementQueueContractTests.cs`;
- `benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj`;
- `benchmarks/Cerneala.Benchmarks/QueueEngineBenchmarks.cs`;
- `benchmarks/Cerneala.Benchmarks/README.md`.

Fisiere existente probabil modificate:

- `Cerneala/UI/Invalidation/ElementQueueOrder.cs`;
- `Cerneala/UI/Invalidation/LayoutQueue.cs`;
- `Cerneala/UI/Invalidation/RenderQueue.cs`;
- `Cerneala/UI/Invalidation/AspectQueue.cs`;
- `Cerneala/UI/Invalidation/HitTestQueue.cs`;
- `Cerneala/UI/Invalidation/InheritedPropertyQueue.cs`;
- `Cerneala/UI/Invalidation/CommandStateQueue.cs`;
- `Cerneala/UI/Invalidation/UiFrameScheduler.cs`;
- clasa interna care detaseaza elemente din `UIRoot`;
- testele existente ale cozilor si schedulerului.

Lista este orientativa. Nu cream fisiere sau abstractii doar pentru ca arata frumos intr-o diagrama.

## 8. Plan de implementare

### Etapa 0 - Baseline si plasa de siguranta

- [x] Ruleaza generatorul `FileTree.md` si actualizeaza indexul Roslyn.
- [x] Ruleaza intreaga suita de teste si noteaza numarul de teste si durata.
- [x] Adauga teste de caracterizare pentru ordinea tuturor snapshot-urilor.
- [x] Adauga teste de caracterizare pentru deduplicare si re-enqueue.
- [x] Adauga teste de caracterizare pentru detach.
- [x] Adauga teste de caracterizare pentru exceptii in scheduler.
- [x] Adauga teste de caracterizare pentru lucrul produs intre faze.
- [x] Instrumenteaza intern numarul de build-uri ale ordinii vizuale pentru teste.
- [x] Masoara baseline-ul pentru scenariile din sectiunea de benchmark.
- [x] Salveaza rezultatele cu informatii despre build, runtime si hardware.

**Gate etapa 0**

- [x] Toate testele de caracterizare trec pe implementarea curenta.
- [x] Baseline-ul este reproductibil.
- [x] Avem o dovada masurabila pentru traversarile si alocarile actuale.
- [x] Nu exista schimbari de comportament in aceasta etapa.

### Etapa 1 - Indexul comun de ordine vizuala

- [x] Introdu `ElementQueueOrderIndex` ca tip intern.
- [x] Leaga indexul de un singur `UIRoot`.
- [x] Construieste preorder ordinal identic cu algoritmul actual.
- [x] Cache-uieste rezultatul dupa `TreeVersion`.
- [x] Reconstruieste indexul doar cand versiunea arborelui s-a schimbat.
- [x] Inlocuieste dictionarul vechi la rebuild pentru a elibera referintele stale.
- [x] Expune un hook intern de diagnostic pentru numarul de build-uri, disponibil testelor.
- [x] Adauga sortarea unui set mic de elemente folosind ordinalele cache-uite.
- [x] Trateaza determinist elementele care nu apartin root-ului.
- [x] Pastreaza `ElementQueueOrder` temporar ca adaptor, daca asta reduce riscul migrarii. (Nu a fost necesar; adaptorul vechi a fost eliminat.)

Teste etapa 1:

- [x] Primul snapshot construieste indexul o singura data.
- [x] Snapshot-uri repetate pe acelasi `TreeVersion` reutilizeaza indexul.
- [x] Snapshot-uri din cozi diferite reutilizeaza acelasi index.
- [x] O mutatie de arbore produce exact un rebuild la urmatoarea utilizare.
- [x] Ordinea dupa rebuild reflecta noua structura vizuala.
- [x] Un element detasat nu primeste ordinal valid.
- [x] Un arbore gol si un root singur sunt tratate corect.
- [x] Arborii adanci nu folosesc o recursie noua mai riscanta decat implementarea existenta.

**Gate etapa 1**

- [x] Ordinea rezultata este byte-for-byte echivalenta in testele de caracterizare.
- [x] Cel mult un build complet are loc pentru fiecare `TreeVersion` folosit.
- [x] Nicio interogare `HasWork` nu cere indexul.
- [x] Toate testele raman verzi.

### Etapa 2 - Nucleul `ElementWorkQueue<TMetadata>`

- [x] Introdu tipul intern generic.
- [x] Foloseste un comparer de identitate referentiala explicit.
- [x] Implementeaza `Count` si `HasWork` direct peste numarul de intrari.
- [x] Implementeaza `Contains` fara snapshot.
- [x] Implementeaza enqueue cu deduplicare O(1) amortizat.
- [x] Implementeaza merge-ul de metadata printr-o strategie simpla injectata in constructor.
- [x] Implementeaza `Remove` prin stergere directa din dictionar.
- [x] Implementeaza snapshot stabil peste o copie a intrarilor curente.
- [x] Sorteaza copia folosind indexul comun al root-ului.
- [x] Elimina defensiv intrarile detasate descoperite la snapshot.
- [x] Evita LINQ pe caile fierbinti daca produce alocari evitabile.
- [x] Nu adauga pooling pana cand profilerul nu demonstreaza ca este necesar.

Teste etapa 2:

- [x] Enqueue repetat nu dubleaza elementul.
- [x] Doua instante distincte raman distincte chiar daca ar avea egalitate de valoare.
- [x] Remove inexistent este sigur.
- [x] Remove urmat de enqueue readauga elementul o singura data.
- [x] Snapshot-ul este stabil daca se fac enqueue-uri dupa capturarea lui.
- [x] Merge-ul de metadata promoveaza, dar nu retrogradeaza.
- [x] Elementele detasate sunt curatate fara a traversa tot arborele.
- [x] `HasWork` si `Count` nu construiesc indexul.
- [x] Coada goala nu aloca la `HasWork` dupa warmup.

**Gate etapa 2**

- [x] Nucleul comun trece toate testele izolate.
- [x] Operatiile simple nu mai depind de o lista auxiliara.
- [x] Nu exista `RemoveAll` pe calea de drain.
- [x] Nu s-a modificat inca niciun contract public.

### Etapa 3 - Migrarea cozilor fara metadata

Ordine recomandata de migrare:

- [x] `RenderQueue`.
- [x] `AspectQueue`.
- [x] `HitTestQueue`.
- [x] `InheritedPropertyQueue`.
- [x] `CommandStateQueue`.

Pentru fiecare coada:

- [x] Pastreaza numele clasei si semnaturile publice.
- [x] Inlocuieste colectiile duplicate cu nucleul comun.
- [x] Pastreaza validarea argumentelor si exceptiile existente.
- [x] Pastreaza ordinea snapshot-ului.
- [x] Pastreaza semantica detach.
- [x] Ruleaza testele specifice cozii dupa migrare.
- [x] Ruleaza testele schedulerului dupa migrare.
- [x] Reindexeaza repository-ul dupa fiecare modificare de cod sau proiect.

**Gate etapa 3**

- [x] Toate cele cinci cozi folosesc acelasi nucleu.
- [x] Testele contractuale comune trec pentru fiecare wrapper.
- [x] Nu exista copii ramase ale modelului `HashSet + List`.
- [x] API diff-ul public este gol.
- [x] Toate testele raman verzi.

### Etapa 4 - Migrarea `LayoutQueue`

- [x] Modeleaza measure si arrange ca doua instante ale nucleului comun.
- [x] Pastreaza separat numarul de intrari pentru fiecare faza.
- [x] Implementeaza merge-ul `LayoutQueueEntryKind` cu prioritatea actuala.
- [x] Pastreaza snapshot-ul public measure in ordine parent-first.
- [x] Pastreaza inversarea interna pentru measure incremental unde este necesara.
- [x] Pastreaza snapshot-ul arrange in ordinea ceruta de scheduler.
- [x] Pastreaza metodele de remove individual si remove complet.
- [x] Verifica situatiile in care acelasi element se afla simultan in measure si arrange.
- [x] Verifica enqueue in timpul procesarii measure si arrange.
- [x] Elimina implementarea veche numai dupa echivalenta completa.

Teste etapa 4:

- [x] `Direct` promoveaza `Required` si `Propagated`.
- [x] `Required` promoveaza `Propagated`, dar nu `Direct`.
- [x] `Propagated` nu retrogradeaza nimic.
- [x] Parent-first public ramane neschimbat.
- [x] Bottom-up intern ramane neschimbat.
- [x] Measure si arrange nu isi corup reciproc intrarile.
- [x] `HasWork` nu construieste niciunul dintre snapshot-uri.
- [x] O singura intrare measure din Playground produce o singura masurare utila.

**Gate etapa 4**

- [x] `LayoutQueue` nu mai contine colectii sau sortare duplicata.
- [x] Toate testele layout si scheduler trec.
- [x] Scenariul Playground ramane reparat.
- [x] API diff-ul public este gol.

### Etapa 5 - Curatarea activa la detach

- [x] Identifica punctul unic prin care un element sau subarbore paraseste un `UIRoot`.
- [x] Adauga o metoda interna a schedulerului/root-ului pentru eliminarea elementului din toate cozile.
- [x] Curata toate elementele subarborelui detasat.
- [x] Pastreaza invalidarile valide ale root-ului generate de mutatie.
- [x] Pastreaza fallback-ul defensiv din snapshot.
- [x] Evita expunerea publica a mecanismului.
- [x] Verifica reatasarea aceluiasi element si re-enqueue ulterior.

Teste etapa 5:

- [x] Detach elimina elementul din fiecare tip de coada.
- [x] Detach de subarbore elimina toti descendentii programati.
- [x] Fratii ramasi in arbore nu sunt eliminati.
- [x] Root-ul ramane programat daca mutatia l-a invalidat.
- [x] Reattach permite invalidari noi.
- [x] Snapshot-ul defensiv curata o intrare stale simulata.
- [x] `HasWork` devine false imediat cand ultima intrare reala este detasata.

**Gate etapa 5**

- [x] Cozile nu pastreaza intentionat referinte la subarbori detasati.
- [x] Semantica testelor existente pentru detach este pastrata.
- [x] Nu exista traversari complete ale arborelui ramas pentru cleanup.

### Etapa 6 - Integrarea si stabilitatea schedulerului

- [x] Pastreaza modelul snapshot plus remove per element.
- [x] Profita de noul `Remove` O(1) fara a introduce un API destructiv prematur.
- [x] Verifica toate punctele care consulta `Scheduler.HasWork`.
- [x] Confirma ca interogarile repetate sunt ieftine si fara efecte secundare.
- [x] Verifica restaurarea intrarilor la exceptie.
- [x] Verifica amanarea re-enqueue-ului in aceeasi faza.
- [x] Verifica procesarea downstream in acelasi frame.
- [x] Verifica limita de lucru si conditiile de continuare ale frame-ului.
- [x] Elimina adaptoarele temporare ramase din `ElementQueueOrder`, daca nu mai sunt folosite.

Teste etapa 6:

- [x] Re-enqueue measure in measure este amanat.
- [x] Invalidarea arrange produsa de measure este procesata in acelasi frame cand contractul o permite.
- [x] Invalidarea render produsa de layout este procesata in acelasi frame.
- [x] Exceptia nu pierde elementul curent.
- [x] Exceptia nu pierde elementele neprocesate ale snapshot-ului.
- [x] Ordinea elementelor ramane determinista dupa recuperare.
- [x] Frame idle nu construieste index si nu aloca prin `HasWork`.
- [x] Frame fara schimbari raporteaza zero lucru de layout.

**Gate etapa 6**

- [x] `UiFrameSchedulerTests` trec integral.
- [x] `FrameSchedulerStabilityTests` trec integral.
- [x] Toate testele repository-ului trec.
- [x] Nu exista diferente observabile in afara performantei.

### Etapa 7 - Benchmark-uri si praguri de performanta

Adauga un proiect BenchmarkDotNet separat numai pentru scenariile stabile ale motorului de cozi. Nu pune benchmark-uri cu praguri de timp fragile in suita unitara.

Scenarii:

- [x] `HasWork` idle pe arbori de 100, 1.000 si 10.000 de elemente.
- [x] `HasWork` repetat de mai multe ori in acelasi frame.
- [x] Snapshot cu 1, 10, 100 si 1.000 de elemente programate intr-un arbore mare.
- [x] Drain pentru 100, 1.000 si 10.000 de intrari.
- [x] Snapshot-uri succesive din cozi diferite pe acelasi `TreeVersion`.
- [x] Rebuild dupa o mutatie de arbore.
- [x] Promotion de metadata in `LayoutQueue`.
- [x] Detach de subarbore cu elemente programate.

Metrici:

- [x] Timp mediu si distributie.
- [x] Bytes alocati per operatie.
- [x] Gen0/Gen1 collections unde sunt relevante.
- [x] Numar de build-uri ale indexului.
- [x] Numar de noduri vizitate pentru indexare.
- [x] Numar de elemente sortate per snapshot.

Praguri functionale obligatorii:

- [x] `HasWork` face zero traversari vizuale.
- [x] `HasWork` aloca zero bytes dupa warmup.
- [x] Un `TreeVersion` produce cel mult un build al indexului comun.
- [x] Drain nu mai contine stergeri O(Q) pentru fiecare element.
- [x] Costul drain-ului creste aproximativ liniar cu numarul de intrari.
- [x] Snapshot-ul depinde de Q elemente programate plus cel mult un rebuild per versiune, nu de N pentru fiecare coada.

Pragurile absolute in milisecunde se stabilesc dupa baseline pe acelasi hardware si se documenteaza in artefactul benchmark-ului. Nu bagam in CI un cronometru isteric care pica pentru ca Windows a decis sa-si scarpine antivirusul.

**Gate etapa 7**

- [x] Rezultatele before/after sunt salvate si comparabile.
- [x] Toate pragurile functionale sunt indeplinite.
- [x] Nu exista regresie semnificativa in snapshot-urile mici.
- [x] Orice regresie acceptata este explicata explicit. (Nu a fost necesara acceptarea vreunei regresii.)

### Etapa 8 - Curatare, documentatie si verificare finala

- [x] Elimina codul vechi si adaptoarele nefolosite.
- [x] Elimina hook-urile de diagnostic care nu sunt necesare testelor sau benchmark-urilor.
- [x] Pastreaza hook-urile ramase `internal`, nu publice.
- [x] Ruleaza formatterul proiectului.
- [x] Ruleaza `dotnet build` pentru solutie.
- [x] Ruleaza toate testele.
- [x] Ruleaza benchmark-urile finale in configuratie Release.
- [x] Ruleaza scenariul Playground si salveaza diagnosticul relevant.
- [x] Genereaza din nou `FileTree.md`.
- [x] Reindexeaza solutia cu RoslynIndexer.
- [x] Ruleaza `git diff --check`.
- [x] Verifica manual diff-ul public de API.
- [x] Actualizeaza acest plan bifand taskurile executate.

Documentatie:

- [x] Confirma ca nu exista schimbari de API public.
- [x] Daca apare inevitabil o schimbare publica, opreste implementarea si discuta separat contractul. (Nu s-a aplicat: API-ul public a ramas neschimbat.)
- [x] Pentru orice schimbare publica aprobata, actualizeaza documentatia din `docs-site/documentation/classes/` folosind skill-ul obligatoriu. (Nu s-a aplicat: nu exista schimbari publice.)
- [x] Documenteaza benchmark-urile si modul lor de rulare in README-ul proiectului de benchmark.

**Gate etapa 8**

- [x] Build verde.
- [x] Toate testele verzi.
- [x] Benchmark-uri finale arhivate.
- [x] API public neschimbat.
- [x] Repository curat, fara fisiere temporare sau procese ramase.

## 9. Strategie de testare

### 9.1 Teste unitare

Testele unitare valideaza invarianti, nu timpi de executie:

- [x] identitate referentiala;
- [x] deduplicare;
- [x] promotion de metadata;
- [x] remove si re-enqueue;
- [x] snapshot stabil;
- [x] ordine vizuala;
- [x] cache dupa `TreeVersion`;
- [x] detach si reattach;
- [x] coada goala;
- [x] exceptii.

### 9.2 Teste contractuale pentru wrappere

Aceeasi suita de baza trebuie aplicata tuturor cozilor simple:

- [x] enqueue o data;
- [x] enqueue duplicat;
- [x] contains;
- [x] remove;
- [x] snapshot order;
- [x] detached pruning;
- [x] repeated `HasWork`.

Astfel evitam situatia in care cinci cozi folosesc acelasi motor, dar una isi pune mustata falsa si decide ca regulile nu i se aplica.

### 9.3 Teste de integrare

- [x] frame idle;
- [x] frame cu o singura invalidare measure;
- [x] frame cu invalidari in toate fazele;
- [x] invalidare produsa de o faza upstream;
- [x] re-enqueue in aceeasi faza;
- [x] exceptie la mijlocul snapshot-ului;
- [x] mutatie de arbore intre doua frame-uri;
- [x] detach in timpul procesarii;
- [x] Playground fara interactiune.

### 9.4 Benchmark-uri

Benchmark-urile masoara performanta, dar nu inlocuiesc testele functionale. Rezultatele trebuie comparate in Release, pe acelasi runtime si acelasi hardware, cu suficiente iteratii pentru stabilizare.

## 10. Riscuri si mitigari

### Risc: ordinea vizuala se schimba subtil

Mitigare:

- [x] teste de caracterizare inainte de refactor;
- [x] acelasi preorder ca implementarea actuala;
- [x] comparatii explicite pe arbori cu mai multe niveluri si frati.

### Risc: indexul comun pastreaza referinte stale

Mitigare:

- [x] dictionarul este inlocuit complet la schimbarea `TreeVersion`;
- [x] cleanup activ la detach;
- [x] fallback defensiv la snapshot;
- [x] teste cu detach/reattach si colectare unde este practic.

### Risc: `HasWork` raporteaza intrari detasate

Mitigare:

- [x] cleanup sincron la detach;
- [x] teste care cer `HasWork == false` imediat dupa eliminarea ultimei intrari;
- [x] snapshot pruning ramane protectie secundara, nu mecanism principal.

### Risc: metadata de layout este pierduta sau retrogradata

Mitigare:

- [x] merge function izolata si testata exhaustiv;
- [x] migrarea `LayoutQueue` se face ultima;
- [x] testele curente raman sursa de adevar pentru comportament.

### Risc: exceptiile pierd lucru din snapshot

Mitigare:

- [x] nu schimbam contractul snapshot al schedulerului;
- [x] pastram restaurarea explicita;
- [x] teste pentru elementul curent si restul neprocesat.

### Risc: abstractia generica devine prea desteapta

Mitigare:

- [x] nucleul cunoaste numai membership, metadata, snapshot si order index;
- [x] politica fiecarei faze ramane in wrapper/scheduler;
- [x] fara pooling, batching distructiv sau concurenta pana nu exista dovada ca sunt necesare.

### Risc: benchmark-ul masoara zgomotul sistemului

Mitigare:

- [x] BenchmarkDotNet in Release;
- [x] aceeasi masina si acelasi runtime pentru comparatii;
- [x] praguri structurale in teste, praguri temporale in rapoarte;
- [x] mai multe dimensiuni de intrare pentru a vedea curba, nu doar un numar sexy.

## 11. Conditii de oprire

Implementarea se opreste pentru reevaluare daca apare oricare dintre situatiile urmatoare:

- [ ] este necesara o schimbare de API public;
- [ ] ordinea actuala nu poate fi reprodusa fara schimbarea contractului;
- [ ] `TreeVersion` nu acopera toate mutatiile relevante ale arborelui;
- [ ] cleanup-ul activ la detach necesita modificari largi in ownership-ul elementelor;
- [ ] recuperarea la exceptie nu poate fi pastrata cu nucleul propus;
- [ ] benchmark-urile arata regresii persistente pentru cozile mici;
- [ ] solutia incepe sa ceara pooling, lock-uri sau concurenta fara date care sa le justifice.

In aceste cazuri se documenteaza problema si se decide separat. Nu acoperim crapatura cu silicon si optimism.

## 12. Secventa recomandata de commit-uri

Implementarea a fost livrata ca un singur working-tree batch; nu s-au creat commit-uri deoarece utilizatorul nu a cerut commit sau publicare. Lista ramane nebifata intentionat si nu reprezinta lucru tehnic restant.

- [ ] `test: characterize queue engine behavior`
- [ ] `perf: cache visual queue order per tree version`
- [ ] `refactor: add shared element work queue core`
- [ ] `refactor: migrate invalidation queues to shared core`
- [ ] `refactor: migrate layout queue metadata handling`
- [ ] `fix: remove detached subtrees from pending queues`
- [ ] `test: expand scheduler queue stability coverage`
- [ ] `bench: add queue engine performance scenarios`
- [ ] `docs: record queue engine 2.0 results`

Commit-urile pot fi comasate daca diff-urile sunt mici, dar ordinea conceptuala trebuie pastrata. Fiecare commit trebuie sa construiasca si sa aiba testele relevante verzi.

## 13. Checklist final de acceptanta

### Corectitudine

- [x] Toate testele existente si noi trec.
- [x] Ordinea snapshot-urilor este identica cu baseline-ul.
- [x] Deduplicarea si promotion-ul functioneaza.
- [x] Detach nu lasa lucru fals in cozi.
- [x] Exceptiile nu pierd lucru.
- [x] Semantica fazelor schedulerului este neschimbata.

### Performanta

- [x] `HasWork` este O(1), fara traversari si fara alocari dupa warmup.
- [x] Ordinea vizuala este construita cel mult o data per `TreeVersion`.
- [x] Toate cozile reutilizeaza acelasi index al root-ului.
- [x] `Remove` nu mai executa scanari liniare.
- [x] Drain-ul nu mai are comportament patratic.
- [x] Snapshot-ul sorteaza numai intrarile programate.
- [x] Playground ramane la o singura masurare utila in scenariul validat.

### Arhitectura

- [x] Exista un singur nucleu pentru membership si snapshot.
- [x] Wrapperele publice raman subtiri si compatibile.
- [x] Politicile specifice fazelor nu sunt impinse in nucleul generic.
- [x] Nu exista cod duplicat ramas pentru aceeasi operatie de coada.
- [x] Nu au fost introduse abstractii nefolosite.

### Livrare

- [x] Build Release verde.
- [x] Suita completa verde.
- [x] Benchmark before/after disponibil.
- [x] API public neschimbat.
- [x] Documentatia relevanta este sincronizata.
- [x] `FileTree.md` si indexul Roslyn sunt actualizate.
- [x] Planul este bifat conform stadiului real, fara checkbox-uri de decor.

## 14. Definitia de "gata"

Queue Engine 2.0 este gata cand un frame idle poate intreba de lucru de cate ori are nevoie fara sa traverseze arborele, cozile pot drena loturi mari fara cost patratic, toate fazele impart aceeasi ordine vizuala cache-uita, iar utilizatorul nu observa nicio schimbare in afara faptului ca framework-ul nu mai gafaie ca dupa urcat zece etaje cu frigiderul in brate.
