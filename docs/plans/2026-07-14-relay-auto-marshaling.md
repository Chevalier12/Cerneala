# Plan: Relay - auto-marshaling UI modern

> Data: 2026-07-14
> Status: finalizat
> Dependenta: nucleul Relay-ului este independent; integrarea generatorului
> de binding depinde de `docs/plans/2026-07-14-markup-data-bindings.md`
> Scop: introducerea unui Relay UI async-first, determinist si observabil,
> integrat cu frame loop-ul Cerneala si cu sursele reactive externe, fara thread
> UI secundar, blocking invoke sau mutatii concurente ale arborelui retained

## 1. Rezumat

Cerneala nu are astazi un mecanism general prin care un semnal venit de pe un
worker thread sa fie executat sigur pe thread-ul `Update`/UI. Evenimentele C#
sunt sincrone, astfel ca `INotifyPropertyChanged`, `ObservableValue<T>`,
`CanExecuteChanged`, schimbarile de tema si schimbarile de resurse ruleaza
handlerul pe thread-ul emitent. Daca acel handler scrie o proprietate UI,
invalideaza un element sau modifica o coada retained, UI-ul este atins de pe
thread-ul gresit.

Solutia tinta este un `UiRelay` detinut de fiecare `UIRoot`, cu:

- coada multi-producer/single-consumer thread-safe;
- API public async-first: `Post`, `InvokeAsync`, `CheckAccess` si `VerifyAccess`;
- drain o singura data la inceputul fiecarui update, inainte de scheduler si
  input;
- snapshot si buget determinist, astfel incat un callback care se reposteaza sa
  nu manance frame-ul cu tot cu farfurie;
- integrare cu `SynchronizationContext` numai pe durata executiei UI;
- anulare, propagarea exceptiilor, statistici si teste de concurenta;
- coalescing specializat pentru binding-uri si alte semnale de tip "re-query
  current state";
- fail-fast pentru mutatiile UI directe off-thread, fiindca auto-marshaling-ul
  nu transforma arborele retained intr-o colectie concurenta.

### 1.1 Branding si vocabular

Subsistemul se numeste **Relay**: preia lucru de pe orice thread si il preda
thread-ului proprietar al root-ului, fara sa ascunda mutatii concurente si fara
sa inventeze un al doilea UI thread. Formula scurta este: **Relay muta executia,
nu datele**.

| Subsistem | Rol | API reprezentativ |
| --- | --- | --- |
| Aspect | style, tema si cascada | `AspectEngine`, `AspectRegistry` |
| Motion | storyboard si animatii | `MotionSystem`, `MotionGraph` |
| Relay | handoff intre thread-uri, continuari async si auto-marshaling | `UiRelay`, `UiRelayOptions` |

Branding-ul public foloseste namespace-ul `Cerneala.UI.Relay`, proprietatea
`UIRoot.Relay` si tipurile `UiRelay`/`UiRelayOptions`. Verbele tehnice familiare
raman `Post`, `InvokeAsync`, `CheckAccess` si `VerifyAccess`; nu expunem in
paralel tipuri sau proprietati publice numite `Dispatcher`, fiindca doua nume
pentru aceeasi dracovenie ar produce numai confuzie.

## 2. Decizii stabilite si presupuneri

- Thread-ul proprietar al unui `UIRoot` este thread-ul pe care root-ul este
  construit. Relay devine unica sursa de adevar pentru thread affinity in
  serviciile root-owned, inclusiv Aspect si Motion.
- `UiRelay` nu creeaza si nu detine un thread. `UiHost.Update`,
  `MonoGameUiHost.Update`, `WindowApplicationRuntime.PumpOnce` si apelurile
  directe `UIRoot.ProcessFrame` pompeaza lucrul pe thread-ul existent al UI-ului.
- Fiecare root are propriul Relay si propria coada. Nu exista un
  `UiRelay.Current` static global, deoarece Cerneala suporta mai multe
  root-uri si ferestre.
- `Post` si `InvokeAsync` pun intotdeauna lucrul in coada, inclusiv cand sunt
  apelate de pe thread-ul UI, pentru ordine FIFO predictibila. Codul deja aflat
  pe UI thread poate apela direct operatia daca doreste executie imediata.
- Nu exista `Invoke`/`Send` blocant public, nested message pump sau asteptare
  sincrona. `.Wait()`, `.Result` si `GetAwaiter().GetResult()` pe thread-ul UI
  raman utilizari gresite si trebuie documentate ca potential deadlock.
- Callback-urile existente la inceputul drain-ului formeaza un snapshot stabil.
  Lucrul postat in timpul drain-ului este amanat pentru urmatorul update.
- Ordinea este FIFO dupa linearizarea enqueue-ului; fiecare producer isi
  pastreaza ordinea. Nu promitem o ordine arbitrara intre doua thread-uri care
  posteaza simultan.
- Bugetul implicit este de 1.024 callback-uri per update si este configurabil
  prin `UiRelayOptions.MaxCallbacksPerUpdate`. Bugetul este numeric, nu
  bazat pe cronometru, pentru teste si frame-uri deterministe.
- API-urile publice de scheduling captureaza `ExecutionContext`, astfel incat
  cultura, `AsyncLocal` si contextul de tracing sa urmeze callback-ul. Codul de
  drain restaureaza intotdeauna contextul anterior.
- `InvokeAsync` captureaza exceptia sau anularea in `Task`; o exceptie din
  `Post` nu este inghitita. Relay-ul proceseaza restul snapshot-ului, apoi
  arunca un `AggregateException` din update pentru callback-urile fire-and-forget
  esuate.
- Anularea inainte de executie impiedica apelarea callback-ului. Dupa ce un
  callback sincron a inceput, token-ul nu il poate intrerupe. Overload-ul async
  primeste token-ul si isi controleaza cooperativ anularea.
- `SynchronizationContext.Send` executa inline numai pe thread-ul proprietar;
  off-thread arunca `NotSupportedException` si indica `InvokeAsync`. `Post`
  delega la Relay.
- Un `INotifyPropertyChanged` off-thread nu este evaluat pe worker. Handlerul
  filtreaza numai numele proprietatii, marcheaza o versiune atomica si programeaza
  o reevaluare a starii curente pe UI thread.
- Binding-urile cu tinta atasata fac fast-path sincron pentru notificari deja
  ridicate pe UI thread. Numai notificarile off-thread sunt coalesced si amanate.
- `UiObject.PropertyChanged` ridicat off-thread nu poate fi "reparat" dupa fapt:
  proprietatea UI a fost deja mutata. Mutatia directa a unui `UIElement` atasat
  ramane interzisa si trebuie oprita inaintea scrierii.
- Auto-marshaling-ul protejeaza UI-ul, nu face automat thread-safe ViewModel-ul,
  colectiile sau obiectele sursa. Sursa trebuie sa permita o citire coerenta pe
  UI thread dupa notificare.
- `ObservableList<T>` si mutatiile incrementale ale colectiilor raman UI-affine
  in acest plan. Pentru ele se foloseste explicit
  `await root.Relay.InvokeAsync(() => items.Add(item))`; nu simulam
  thread-safety peste un `List<T>` care nu o are.
- Pana la implementarea etapei de integrare cu binding-urile, planul
  `2026-07-14-markup-data-bindings.md` pastreaza fail-fast strict pentru orice
  `PropertyChanged` off-thread.

## 3. Baseline si problema actuala

### 3.1 Frame loop si ownership

- `UIRoot` detine `UiFrameScheduler`, cozile retained, `MotionSystem`, cache-ul de
  render si serviciile de root.
- `UiHost.UpdateCore` poate apela `UIRoot.ProcessFrame` o data inainte de input si
  inca o data dupa input. Un Relay integrat naiv in `ProcessFrame` ar putea
  drena de doua ori in acelasi update si ar strica semantica snapshot-ului.
- `MonoGameUiHost.Update` pompeaza `GeneratedWindowApplication`, apoi deleaga la
  `UiHost.Update`.
- `WindowApplicationRuntime.PumpOnce` randeaza o fereastra numai daca exista
  `RenderRequested`, lucru in `UiFrameScheduler`, motion activ sau pointer repeat.
  Un Relay cu backlog trebuie adaugat explicit acestui wake predicate.
- `WindowApplicationRuntime` are deja un `ownerThreadId` si `VerifyAccess`, iar
  Motion are propriul `MotionThreadGuard`; cele doua mecanisme paralele sunt
  baseline-ul care trebuie eliminat, nu arhitectura pe care o conservam.

### 3.2 Surse reactive

- `UiObject.OnPropertyChanged` invoca handler-ele sincron.
- `GeneratedMarkupConditions.Subscribe` asculta direct
  `UiObject.PropertyChanged` sau `INotifyPropertyChanged` pe thread-ul emitent.
- `UiPropertyBinding<T>` asculta direct `ObservableValue<T>.ValueChanged` si
  scrie imediat tinta.
- `ObservableValue<T>` si `ObservableList<T>` nu sunt colectii concurente.
- `UIRoot.OnResourceChanged`, `ThemeChangedSubscription` si
  `ButtonBase.OnCanExecuteChanged` ajung in invalidari sau cozi retained fara un
  marshal general.

### 3.3 Infrastructura reutilizabila existenta

- Queue Engine 2.0 ofera cozi rapide pentru elemente retained, dar acestea sunt
  UI-thread-only si ordonate vizual; nu trebuie refolosite fortat ca o coada MPSC
  de delegate.
- `FrameStats` si `InvalidationTrace` sunt punctele existente pentru
  observabilitatea unui frame.
- `IElementLifecycleBehavior`, attach/detach si generation guards din markup sunt
  punctele potrivite pentru anularea callback-urilor reactive stale.

## 4. Obiective

- [x] O operatie postata de pe orice worker thread ruleaza exclusiv pe thread-ul
  proprietar al root-ului, la inceputul urmatorului update eligibil.
- [x] `UiRelay.CheckAccess` si `VerifyAccess` ofera acelasi adevar pentru
  hosting, binding-uri, motion si mutatii UI atasate.
- [x] Enqueue este thread-safe si O(1) amortizat, iar verificarea backlog-ului nu
  traverseaza arborele si nu aloca dupa warmup.
- [x] Drain-ul este FIFO, bugetat si bazat pe snapshot; nu exista starvation
  provocat de auto-repost in acelasi update.
- [x] Lucrul Relay-ului ruleaza inainte de schedulerul retained si input,
  iar invalidarile produse sunt procesate in acelasi update cand bugetele
  schedulerului permit.
- [x] `UiHost.Update` dreneaza o singura data, chiar daca proceseaza schedulerul
  pre-input si post-input.
- [x] Standalone Windows se trezeste pentru backlog-ul Relay-ului chiar daca
  nu exista invalidari sau motion.
- [x] Continuarile `await` pornite din callback-uri UI revin prin Relay fara
  instalarea permanenta a unui context global.
- [x] Rafalele off-thread de `PropertyChanged` produc cel mult o reevaluare
  pending per binding activ, fara pierderea ultimei schimbari.
- [x] Detach, template swap, root replacement si disposal nu permit unui callback
  vechi sa scrie intr-o tinta inactiva.
- [x] Mutatiile UI directe off-thread sunt respinse inaintea schimbarii starii.
- [x] API-ul public are documentatie completa si exemple async fara blocking.
- [x] Exista statistici pentru enqueue, execute, cancel, fault, deferred si
  backlog, plus benchmark-uri multi-producer.

## 5. Non-obiective

- Nu cream un thread UI nou si nu preluam bucla principala MonoGame.
- Nu facem layout, render, hit testing sau motion in paralel.
- Nu adaugam prioritati, delayed dispatch, timers, cron jobs ori un task scheduler
  general. Pot veni ulterior numai pe baza unui caz real.
- Nu implementam un `Relay.Invoke` blocant si nu emulam nested message loops
  din framework-uri desktop vechi.
- Nu facem `ObservableList<T>`, ViewModel-urile sau colectiile utilizatorului
  thread-safe prin magie. Mutatiile incrementale de colectie se posteaza explicit
  pe Relay.
- Nu interceptam automat orice event arbitrar din aplicatie. Migrarile automate
  se limiteaza la punctele first-party care ating UI-ul si au semantica definita
  in acest plan.
- Nu schimbam ordinea fazelor retained din `UiFrameScheduler`; Relay-ul este
  o poarta pre-frame, nu o noua coada de elemente vizuale.
- Nu mutam un `UIRoot` deja construit pe alt thread. O asemenea migrare ar cere
  un contract separat pentru motion, backend grafic si resurse native.
- Nu garantam snapshot atomic pentru o cale CLR ale carei obiecte sunt modificate
  simultan fara sincronizarea autorului ViewModel-ului.

## 6. Contract public propus

### 6.1 `UiRelay`

Suprafata tinta din `Cerneala.UI.Relay`:

```csharp
public sealed class UiRelay
{
    public bool CheckAccess();
    public void VerifyAccess();

    public bool HasPendingWork { get; }
    public int PendingCount { get; }

    public void Post(Action callback);

    public Task InvokeAsync(
        Action callback,
        CancellationToken cancellationToken = default);

    public Task<T> InvokeAsync<T>(
        Func<T> callback,
        CancellationToken cancellationToken = default);

    public Task InvokeAsync(
        Func<CancellationToken, Task> callback,
        CancellationToken cancellationToken = default);
}
```

Reguli:

- toate overload-urile valideaza `null` sincron;
- toate enqueue-urile sunt FIFO si asincrone fata de apelant;
- `TaskCompletionSource` foloseste `RunContinuationsAsynchronously`;
- `InvokeAsync(Func<CancellationToken, Task>)` porneste delegatul pe UI thread,
  nu blocheaza drain-ul pana la completare si propaga rezultatul in task-ul
  returnat;
- `Post` este pentru fire-and-forget controlat; codul care are nevoie de rezultat,
  anulare sau tratarea exceptiei foloseste `InvokeAsync`;
- `PendingCount` include work items neincepute, nu operatii async deja pornite;
- callback-urile publice ruleaza cu `ExecutionContext` capturat la enqueue.

### 6.2 `UiRelayOptions`

```csharp
public sealed class UiRelayOptions
{
    public int MaxCallbacksPerUpdate { get; init; } = 1024;
}
```

- valorile mai mici sau egale cu zero sunt respinse;
- optiunile sunt copiate/validate la constructia root-ului, nu citite mutabil in
  mijlocul frame-ului;
- configurarea intra ca ultim parametru optional in constructorul `UIRoot`, fara
  sa rupa apelurile existente.

### 6.3 Expunere prin hosting

- `UIRoot.Relay` este sursa de adevar non-null.
- `UiHost.Relay` si `MonoGameUiHost.Relay` sunt proprietati nullable de
  convenienta, deoarece host-ul poate exista temporar fara root.
- `SetRoot` verifica faptul ca este apelat pe thread-ul Relay-ului noului
  root; nu inchide Relay-ul root-ului vechi, deoarece acel root poate fi
  refolosit sau procesat direct.

### 6.4 Context async scoped

Un `UiRelaySynchronizationContext` intern este instalat numai in jurul:

- drain-ului Relay-ului;
- procesarii retained;
- rutarii input-ului si handler-elor apelate de ea;
- callback-urilor de hosting care ruleaza ca parte din update.

Contextul anterior este restaurat in `finally`. Doua root-uri pompate succesiv pe
acelasi thread nu isi amesteca continuarile. `SynchronizationContext.Post`
apeleaza `UiRelay.Post`; `Send` este permis inline numai cand
`CheckAccess == true`, altfel arunca.

## 7. Arhitectura propusa

### 7.1 Nucleu MPSC si work items

`UiRelay` foloseste un `ConcurrentQueue<UiRelayWorkItem>` si contoare
atomice pentru backlog. Un singur consumer, thread-ul UI, executa drain-ul.

Fiecare work item contine numai ce cere contractul sau:

- callback si state;
- `ExecutionContext` capturat;
- tipul fire-and-forget sau request/response;
- completion source optional;
- token si registration optional;
- stare atomica `Pending`, `Running`, `Completed` sau `Canceled`.

Nu folosim `Channel<T>`, thread pool consumer sau `Task.Run` pentru drain. Coada
este doar transport intre produceri si frame loop; altfel am construi un autobuz
ca sa traversam bucataria.

### 7.2 Drain determinist

La inceputul update-ului:

1. se verifica thread-ul proprietar;
2. se captureaza numarul pending existent;
3. limita este `min(snapshotCount, MaxCallbacksPerUpdate)`;
4. se proceseaza cel mult acea limita FIFO;
5. work items anulate sunt finalizate ca anulate fara callback;
6. exceptiile `InvokeAsync` finalizeaza task-ul faulted;
7. exceptiile `Post` sunt colectate, fara sa abandoneze restul snapshot-ului;
8. backlog-ul ramas este raportat si amanat pentru update-ul urmator;
9. dupa snapshot se arunca un singur `AggregateException` pentru `Post`-urile
   esuate.

Un enqueue concurent dupa capturarea snapshot-ului este vizibil in
`HasPendingWork`, dar nu intra in drain-ul curent. Contoarele trebuie sa evite
lost wakeups cand enqueue si dequeue se intercaleaza.

### 7.3 Integrare cu `UiHost` si `UIRoot`

`UiHost.UpdateCore` deschide o singura sesiune de update pentru root:

```text
VerifyAccess
Install scoped SynchronizationContext
Apply viewport, initial-frame si time-sensitive invalidations
Drain Relay once
Run pre-input scheduler gate
Dispatch input
Run post-input scheduler gate
Commit retained render data
Restore previous SynchronizationContext
```

`UIRoot.ProcessFrame` ramane util in teste si hosting custom: apelul public
deschide aceeasi poarta, dreneaza o data si apoi proceseaza schedulerul. `UiHost`
foloseste un core intern care nu redreneaza intre poarta pre-input si cea
post-input.

Callback-urile Relay-ului pot invalida UI-ul, iar schedulerul vede acele
invalidari in acelasi update. Un `Post` facut din input sau din timpul drain-ului
asteapta update-ul urmator.

### 7.4 Wake-up pentru Windows si MonoGame

- `MonoGameUiHost` este pompat de joc la fiecare update si nu necesita semnal OS
  separat.
- `WindowApplicationRuntime.PumpOnce` include
  `context.Root.Relay.HasPendingWork` in predicatul care cere `Render`.
- `RenderRequested` si backlog-ul Relay-ului raman concepte separate; un
  callback care nu schimba vizual nimic poate produce un update fara draw nou,
  conform contractului retained existent.
- `UiHost.SetRoot` si `WindowApplicationRuntime.GetOrCreateContext` verifica
  compatibilitatea thread-ului root-ului cu runtime-ul ferestrei.

### 7.5 Thread affinity pentru UI

Se introduce un hook intern de mutatie in `UiObject`, no-op pentru obiectele
generice si verificat de `UIElement` cand este atasat:

```text
UiObject.SetValue/ClearValue/SetValueUntyped/ClearValueUntyped
    -> VerifyMutationAccess()
UIElement.VerifyMutationAccess()
    -> Root?.Relay.VerifyAccess()
```

Aceeasi verificare se aplica punctelor canonice care ocolesc proprietatile:

- mutatii in `UIElementCollection`;
- attach/detach de subtree;
- metode mutabile ale `UIRoot`;
- `UiHost.Update` si `Draw`;
- integrarea root-owned a motion.

Obiectele detasate pot fi construite si configurate inainte de attach, dar
attach-ul si orice mutatie ulterioara a unui element atasat trebuie sa fie pe
thread-ul root-ului. Citirile nu primesc lock-uri; UI-ul ramane thread-affine.

### 7.5.1 Motion foloseste Relay, fara guard propriu

`MotionThreadGuard` se sterge complet. Nu ramane adaptor `[Obsolete]`, alias,
compatibility shim sau copie interna cu alta palarie. Pentru Motion root-owned,
`MotionSystem` deleaga verificarile catre `UIRoot.Relay`; punctele interne pot
folosi un `MotionSystem.VerifyAccess()` subtire, dar acesta nu detine thread ID si
nu expune o a doua sursa de adevar.

Schimbarea publica este intentionat breaking:

- se sterge tipul si fisierul `MotionThreadGuard`;
- se sterge `MotionSystem.ThreadGuard`;
- se sterg constructorii `MotionGraph` care primesc `MotionThreadGuard`;
- constructorii publici standalone ai `MotionGraph` si `ManualMotionTimeline`
  captureaza intern thread-ul curent prin contractul comun intern Relay;
- constructorul intern root-owned al `MotionGraph` primeste accesul Relay al
  root-ului, fara sa publice o abstractie noua numai pentru compatibilitate;
- toate apelurile `motion.ThreadGuard.VerifyAccess()` devin verificari delegate
  catre Relay, iar referintele din teste si exemple sunt rescrise.

### 7.5.2 Aspect foloseste aceeasi autoritate

Nu se creeaza `AspectThreadGuard`. Operatiile Aspect care modifica registre,
environment, subscriptions, invalidation sau aplica stiluri pe un root atasat
verifica accesul prin acelasi Relay. Auditul include cel putin
`AspectRegistry.Register/Unregister`, `AspectEnvironment.Set`,
`AspectInvalidation.Track/Recompute/Untrack`, `AspectEngine.Apply` si
`AspectProcessor.Process/Clear`. Obiectele standalone fara root isi pastreaza
contractul local actual; din clipa in care ating un root, Relay decide thread-ul.

### 7.6 Auto-marshaling pentru binding-uri

Controllerul reactiv comun are doua cai:

- pe UI thread: reevalueaza imediat, pastrand latenta si semantica existenta;
- off-thread: nu citeste calea si nu atinge tinta; incrementeaza o versiune
  atomica si cere o singura reevaluare pending pe Relay.

Coalescing-ul foloseste `requestedVersion`, `processedVersion`, un flag atomic
de enqueue si generation-ul activarii. Daca o notificare soseste in cursul
reevaluarii, callback-ul nu pierde wakeup-ul: programeaza exact o continuare
pentru update-ul urmator. La executie se citeste starea curenta, nu valoarea
capturata de primul event.

Reguli lifecycle:

- controllerul captureaza Relay-ul root-ului la attach/activare;
- detach dezaboneaza sursele si invalideaza generation-ul;
- un callback deja in coada verifica generation-ul si devine no-op;
- reattach porneste cu generation nou si refresh complet;
- template swap/disposal nu lasa coada sa retina permanent controllerul;
- binding-ul conditional inactiv si fragmentele short-circuited nu programeaza
  refresh inutil;
- interpolarile si expresiile `@when` fac coalescing la nivelul controllerului
  compus, nu cate o scriere UI pentru fiecare frunza.

Pentru `TwoWay`, scrierea target-to-source porneste de pe UI thread. Daca sursa
raspunde ulterior cu `PropertyChanged` off-thread, acel echo urmeaza aceeasi cale
coalesced si reentrancy guard-ul ramane valid peste granita de frame.

### 7.7 Binding programatic

`UiPropertyBinding<T>` adopta acelasi controller de dispatch:

- un target `UIElement` atasat foloseste automat `Root.Relay`;
- `BindingOperations` primeste overload-uri cu `UiRelay` explicit pentru un
  `UiObject` generic sau un target inca neatasat;
- un event off-thread fara Relay rezolvabil produce o eroare actionabila,
  nu o scriere directa;
- Relay-ul explicit trebuie sa coincida cu Relay-ul root-ului dupa
  attach, altfel binding-ul este respins;
- API-urile existente raman compatibile pentru utilizarile strict UI-thread.

### 7.8 Alte notificari first-party

Se face un audit al handler-elor externe care ating root-ul. Politica initiala:

| Semnal | Politica |
| --- | --- |
| CLR `INotifyPropertyChanged` | auto-marshal, coalesced per controller |
| `ObservableValue<T>.ValueChanged` | auto-marshal cand binding-ul are Relay |
| `ICommand.CanExecuteChanged` | auto-marshal si coalesce per command source/control |
| `ThemeProvider.ThemeChanged` | auto-marshal si coalesce per root |
| `IObservableResourceProvider.ResourceChanged` | marshal FIFO; fara coalescing pana cand semantica delta permite |
| `ObservableList<T>.Changed` | UI-thread-only; mutatia se posteaza explicit |
| `UiObject.PropertyChanged` | UI-thread-only; mutatia este verificata inainte de event |
| input, layout, render, motion graph | UI-thread-only; folosesc explicit Relay-ul la intrare |

Nu introducem un adaptor generic care captureaza orice event prin reflection.
Fiecare integrare isi declara politica de coalescing, lifecycle si consistenta.

### 7.9 Statistici si diagnostic

`FrameStats` primeste contoare pentru:

- `RelayedCallbacks`;
- `CanceledRelayCallbacks`;
- `FaultedRelayCallbacks`;
- `DeferredRelayCallbacks`;
- `RelayBacklogAfterUpdate`.

`HasWork` include callback-urile executate in update. Un frame care doar dreneaza
Relay-ul nu este raportat ca idle. `UiRelay` pastreaza contoare cumulative interne
pentru benchmark si teste, fara un sistem paralel de logging. Finalizarea
ulterioara a unui delegate async nu modifica retroactiv un `FrameStats` deja
publicat; rezultatul sau exceptia raman pe `Task`.

Mesajele fail-fast includ:

- numele operatiei;
- thread-ul proprietar si thread-ul curent;
- root-ul sau proprietatea diagnosticabila, cand exista;
- recomandarea concreta `Relay.Post` sau `await Relay.InvokeAsync`.

## 8. Fisiere estimate

Fisiere noi probabile:

- `UI/Relay/UiRelay.cs`;
- `UI/Relay/UiRelayOptions.cs`;
- `UI/Relay/UiRelaySynchronizationContext.cs`;
- `UI/Relay/UiRelayWorkItem.cs`;
- un contract intern minimal de thread access sub `UI/Relay/`, numai daca este
  necesar pentru obiectele Motion standalone;
- `tests/Cerneala.Tests/UI/Relay/UiRelayTests.cs`;
- `tests/Cerneala.Tests/UI/Relay/UiRelayConcurrencyTests.cs`;
- `tests/Cerneala.Tests/UI/Relay/UiRelaySynchronizationContextTests.cs`;
- `tests/Cerneala.Tests/UI/Hosting/UiHostRelayIntegrationTests.cs`;
- `tests/Cerneala.Tests/UI/Data/UiPropertyBindingThreadingTests.cs`;
- `benchmarks/Cerneala.Benchmarks/UiRelayBenchmarks.cs`;
- pagini API noi sub `docs-site/documentation/classes/` pentru tipurile publice.

Fisiere existente probabil modificate:

- `UI/Elements/UIRoot.cs`;
- `UI/Hosting/UiHost.cs`;
- `UI/Hosting/MonoGame/MonoGameUiHost.cs`;
- `UI/Hosting/Windows/WindowApplicationRuntime.cs`;
- `UI/Invalidation/FrameStats.cs`;
- `UI/Core/UiObject.cs`;
- `UI/Elements/UIElement.cs`;
- `UI/Elements/UIElementCollection.cs`;
- `UI/Elements/ElementLifecycle.cs`;
- `UI/Motion/Core/MotionSystem.cs`;
- `UI/Motion/Core/MotionGraph.cs`;
- `UI/Motion/Core/ManualMotionTimeline.cs`;
- toate fisierele Motion care apeleaza astazi `ThreadGuard.VerifyAccess()`;
- implementarea Aspect pentru registry, environment, invalidation, engine si
  processor;
- `UI/Data/BindingOperations.cs` si controllerul comun de binding;
- integrarile command, theme si resources;
- testele si documentatia API ale tuturor tipurilor publice afectate.

Fisiere sterse intentionat:

- `UI/Motion/Core/MotionThreadGuard.cs`;
- testele dedicate exclusiv vechiului guard;
- `docs-site/documentation/classes/Cerneala.UI.Motion.Core.MotionThreadGuard.md`;
- intrarea sa din `docs-site/documentation/manifest.json`.

Dependente intre planuri:

- nucleul Relay, hosting-ul, thread affinity, Motion si Aspect pot fi
  implementate independent;
- integrarea completa a binding-urilor se face dupa contractul sintactic din
  `docs/plans/2026-07-14-markup-data-bindings.md`;
- pana atunci, binding-urile pastreaza contractul strict/fail-fast descris acolo.

## 9. Checklist de implementare

### Etapa 0 - Inventar, baseline si teste RED

- [x] Regenereaza `FileTree.md`, indexeaza `Cerneala.slnx --json` si confirma
  `doctor` verde inainte de schimbari.
- [x] Foloseste RoslynIndexer pentru definitii si referinte ale `MotionThreadGuard`,
  `MotionSystem.ThreadGuard`, constructorilor `MotionGraph` si apelurilor
  `ThreadGuard.VerifyAccess`; salveaza inventarul in notele implementarii.
- [x] Inventariaza exemplele si paginile API care construiesc
  `MotionThreadGuard`, plus intrarea exacta din manifest.
- [x] Caracterizeaza thread ownership pentru `UIRoot`, `UiHost`,
  `MonoGameUiHost`, `WindowApplicationRuntime`, Motion standalone si Aspect.
- [x] Caracterizeaza ordinea actuala din `UiHost.Update`: pre-input, input,
  post-input, scheduler si commit, astfel incat Relay sa fie drenat exact o data.
- [x] Adauga teste RED pentru `Post` worker-to-UI, FIFO, exact-once, snapshot,
  buget, anulare, exceptii si backlog pe un root idle.
- [x] Adauga teste RED pentru continuari `await`, doua root-uri pe acelasi thread
  si restaurarea `SynchronizationContext` anterior.
- [x] Adauga teste RED care dovedesc ca mutatiile directe off-thread nu schimba
  property store-ul, arborele sau cozile retained inainte sa arunce.
- [x] Adauga teste RED pentru Motion root-owned si Aspect care trebuie sa accepte
  numai thread-ul declarat de Relay.
- [x] Adauga teste de caracterizare pentru constructorii Motion standalone care
  trebuie sa ramana utilizabili dupa stergerea guard-ului public.
- [x] Foloseste bariere si primitive deterministe in testele concurente; nu folosi
  `Thread.Sleep` ca dovada de sincronizare.

**Gate etapa 0**

- [x] Testele RED esueaza din motivele de threading asteptate, nu din setup sau
  timing fragil.
- [x] Ordinea frame-ului si comportamentul UI-thread existent sunt caracterizate.
- [x] Toate referintele publice si documentare ale `MotionThreadGuard` sunt
  inventariate inainte de stergere.
- [x] Baseline-ul complet este verde in afara testelor RED noi.
- [x] Nicio semnatura publica nu este schimbata in etapa 0.

### Etapa 1 - Thread access si nucleul `UiRelay`

- [x] Introdu `UiRelayOptions` cu default 1.024 si validare stricta pentru
  `MaxCallbacksPerUpdate > 0`.
- [x] Introdu `UiRelay` root-owned, capturand owner thread ID in
  constructorul `UIRoot`.
- [x] Implementeaza `CheckAccess`, `VerifyAccess`, `HasPendingWork` si
  `PendingCount` fara traversari sau lock global pe calea de citire.
- [x] Implementeaza coada MPSC cu `ConcurrentQueue`, pending counter atomic si
  work item state machine care nu poate fi executat de doua ori.
- [x] Implementeaza `Post(Action)` cu capturarea `ExecutionContext`.
- [x] Implementeaza overload-urile `InvokeAsync` cu
  `TaskCompletionSource` configurat `RunContinuationsAsynchronously`.
- [x] Implementeaza anularea race-safe inainte de dequeue, intre dequeue si run
  si dupa pornirea callback-ului async.
- [x] Implementeaza drain-ul intern snapshot-based cu buget numeric si FIFO.
- [x] Proceseaza restul snapshot-ului dupa exceptii `Post`, apoi arunca un
  `AggregateException`; nu transforma exceptiile `InvokeAsync` in erori de
  frame.
- [x] Elibereaza `CancellationTokenRegistration`, `ExecutionContext`, delegatele
  si completion sources dupa finalizare pentru a nu retine grafuri de obiecte.
- [x] Adauga teste pentru argumente null, optiuni invalide, access checks,
  enqueue/dequeue, FIFO, anulare si exceptii.
- [x] Adauga teste multi-producer care verifica exact-once si FIFO per producer,
  fara sa impuna o ordine falsa intre thread-uri concurente.
- [x] Adauga teste pentru enqueue concurent cu finalul drain-ului, verificand ca
  `HasPendingWork` nu pierde wakeup-ul.
- [x] Adauga teste pentru callback care se reposteaza: noul work item ramane
  pentru update-ul urmator.
- [x] Adauga teste pentru bugetul 1.024, backlog si continuarea in update-uri
  succesive.
- [x] Reindexeaza dupa fiecare modificare C# sau project-file.

**Gate etapa 1**

- [x] Nucleul trece toate testele unitare si de concurenta repetate.
- [x] Nicio operatie UI nu este inca migrata partial la Relay.
- [x] Enqueue si `HasPendingWork` nu depind de arborele retained.
- [x] Nu exista blocking invoke, nested pump sau thread creat de Relay.

### Etapa 2 - `SynchronizationContext` async-first

- [x] Introdu adaptorul intern `UiRelaySynchronizationContext`.
- [x] Leaga `Post` de Relay si implementeaza `Send` numai ca fast-path pe
  owner thread; off-thread arunca un mesaj care recomanda `InvokeAsync`.
- [x] Captureaza si restaureaza contextul anterior cu `try/finally`.
- [x] Adauga un scope intern idempotent pentru update-uri nested controlate, fara
  sa lase contextul instalat dupa iesire sau exceptie.
- [x] Verifica faptul ca un `await Task.Yield()` pornit intr-un callback UI isi
  continua executia pe Relay intr-un update ulterior.
- [x] Verifica `AsyncLocal`, cultura si contextul de tracing peste `Post`,
  `InvokeAsync` si continuari async.
- [x] Verifica doua root-uri pe acelasi thread: fiecare continuare revine in
  coada root-ului care a capturat-o.
- [x] Verifica faptul ca un context preexistent este restaurat exact dupa update.
- [x] Verifica exceptiile si anularea callback-urilor async fara blocarea drain-ului.
- [x] Reindexeaza dupa modificarile C#.

**Gate etapa 2**

- [x] Continuarile async revin pe root-ul corect.
- [x] Niciun test nu depinde de `.Wait()` sau `.Result` pe owner thread.
- [x] Contextul nu ramane global instalat intre doua update-uri.
- [x] API-ul public ramane async-first si minimal.

### Etapa 3 - Integrarea cu frame loop-ul si hosting-ul

- [x] Adauga `UIRoot.Relay` si construieste-l inainte de serviciile
  root-owned care au nevoie de thread access.
- [x] Introdu poarta interna de update care verifica accesul, instaleaza contextul
  si dreneaza Relay-ul exact o data.
- [x] Refactorizeaza `UIRoot.ProcessFrame` intr-o intrare publica cu drain si un
  core intern reutilizat de `UiHost` fara al doilea drain.
- [x] Integreaza poarta in `UiHost.UpdateCore` inainte de scheduler si input.
- [x] Adauga `UiHost.Relay` si `MonoGameUiHost.Relay` ca proprietati de
  convenienta fara duplicarea ownership-ului.
- [x] Verifica `UiHost.Update`, `Draw`, `SetRoot` si root-ul nou prin
  `VerifyAccess` inainte de mutatii sau backend calls.
- [x] Adauga `Relay.HasPendingWork` in wake predicate-ul
  `WindowApplicationRuntime.PumpOnce`.
- [x] Verifica la crearea contextului de fereastra faptul ca runtime-ul si root-ul
  au acelasi owner thread.
- [x] Pastreaza o singura pompare in `MonoGameUiHost.Update`, inclusiv cand
  `GeneratedWindowApplication.PumpHosted` ruleaza inaintea host-ului principal.
- [x] Asigura ca invalidarile produse de Relay sunt procesate in poarta
  pre-input a aceluiasi update.
- [x] Asigura ca un `Post` facut in input sau drain este amanat pana la urmatorul
  update, chiar daca host-ul ruleaza schedulerul post-input.
- [x] Extinde `FrameStats` cu contoarele de dispatch si include executia in
  `HasWork`.
- [x] Pastreaza testele idle: zero callback-uri inseamna zero alocari noi pe
  calea de verificare si nu porneste schedulerul artificial.
- [x] Adauga teste de root replacement: vechiul Relay nu este inchis,
  noul root este verificat, iar fiecare coada este pompata numai cu root-ul sau.
- [x] Reindexeaza dupa fiecare modificare C#.

**Gate etapa 3**

- [x] `UiHost.Update` dreneaza exact o data pe toate caile.
- [x] Standalone Windows proceseaza un callback pe un root anterior idle.
- [x] Invalidarile Relay-ului ajung in retained scheduler in acelasi update.
- [x] Testele existente de ordine input/scheduler/render si no-work raman verzi.

### Etapa 4 - Thread affinity pentru UI, Motion si Aspect

- [x] Adauga hook-ul intern `VerifyMutationAccess` in `UiObject` si apeleaza-l
  in toate caile typed/untyped de `SetValue` si `ClearValue` inaintea accesului la
  `UiPropertyStore`.
- [x] Suprascrie hook-ul in `UIElement` folosind `Root?.Relay.VerifyAccess`,
  pastrand configurarea libera pentru elemente detasate.
- [x] Protejeaza mutatiile canonice din `UIElementCollection` si
  `ElementLifecycle.AttachSubtree/DetachSubtree` inainte de schimbarea arborelui.
- [x] Protejeaza metodele mutabile root-owned care pot invalida, schimba tema,
  resursele, viewport-ul sau serviciile platformei.
- [x] Introdu numai daca este necesar contractul intern minimal de thread access
  din subsistemul Relay; `UiRelay` il implementeaza, iar varianta standalone
  captureaza thread-ul curent fara API public nou.
- [x] Migreaza `MotionSystem` root-owned la acelasi thread access detinut de
  `UIRoot.Relay` si adauga un `MotionSystem.VerifyAccess()` intern doar ca punct
  de delegare, fara owner thread propriu.
- [x] Sterge proprietatea publica `MotionSystem.ThreadGuard` si inlocuieste toate
  apelurile `motion.ThreadGuard.VerifyAccess()` din coordinatoare, bindings,
  transactions si frame processing.
- [x] Rescrie constructorii `MotionGraph`: overload-urile standalone captureaza
  intern thread-ul apelant, iar constructorul root-owned intern primeste thread
  access-ul Relay; niciun overload nu mai primeste `MotionThreadGuard`.
- [x] Migreaza `ManualMotionTimeline` si testele standalone la noul contract
  intern, pastrand verificarea thread affinity fara guard public.
- [x] Sterge complet `UI/Motion/Core/MotionThreadGuard.cs`; nu adauga `[Obsolete]`,
  adaptor, alias, forwarding type sau shim de compatibilitate.
- [x] Verifica prin RoslynIndexer ca nu mai exista referinte C# la tip,
  `MotionSystem.ThreadGuard` sau constructorii eliminati.
- [x] Leaga punctele mutable Aspect la Relay fara `AspectThreadGuard`: registry,
  environment, invalidation, engine si processor verifica acelasi owner thread
  inainte de a modifica stare root-owned.
- [x] Adauga teste Aspect pe owner thread si off-thread pentru
  `Register/Unregister`, `Set`, `Track/Recompute/Untrack`, `Apply`, `Process` si
  `Clear`, ajustate la suprafata publica/interna reala gasita la implementare.
- [x] Adauga teste care confirma ca o mutatie off-thread nu schimba valoarea,
  sursa valorii, dirty flags, coada de invalidare sau versiunea arborelui.
- [x] Adauga teste pentru element detasat configurat pe worker si atasat ulterior
  pe owner thread.
- [x] Adauga teste pentru attach/detach, Motion, Aspect si root methods off-thread.
- [x] Ruleaza un audit Roslyn al punctelor publice care modifica un root atasat si
  noteaza explicit exceptiile ramase UI-thread-only prin contract superior.
- [x] Reindexeaza dupa fiecare modificare C#.

**Gate etapa 4**

- [x] Proprietatile si arborele UI nu pot fi mutate off-thread prin caile
  canonice.
- [x] Motion si restul root-ului folosesc acelasi owner thread.
- [x] Aspect si restul root-ului folosesc acelasi owner thread, fara guard separat.
- [x] `MotionThreadGuard`, `MotionSystem.ThreadGuard` si constructorii care il
  primeau nu mai exista in API-ul compilat.
- [x] Elementele detasate raman usor de construit fara un Relay artificial.
- [x] Nu au fost introduse lock-uri in layout, render sau property store.

### Etapa 5 - Auto-marshaling pentru binding-uri

- [x] Implementeaza in controllerul reactiv comun fast-path UI si calea
  off-thread coalesced, fara evaluarea sursei pe worker.
- [x] Foloseste versiuni atomice si generation guards astfel incat o notificare
  sosita in timpul refresh-ului sa nu fie pierduta.
- [x] Re-evalueaza pe UI thread intreaga cale tipizata si reconecteaza segmentele
  nested la starea curenta.
- [x] Coalesce-uieste per controller binding-urile simple, interpolarile si
  expresiile `@when`, nu per event brut.
- [x] Pastreaza short-circuit-ul logic la evaluare, dar permite oricarei frunze
  observate sa ceara reevaluarea compozitiei.
- [x] Anuleaza logic callback-urile stale la detach, reattach, template swap,
  pierderea unei ramuri conditionale si disposal.
- [x] Pastreaza fast-path-ul sincron pentru `PropertyChanged` deja ridicat pe
  thread-ul UI.
- [x] Verifica `TwoWay`: write-back-ul local ramane imediat, echo-ul off-thread
  nu creeaza bucla si ultima valoare castiga determinist.
- [x] Migreaza `UiPropertyBinding<T>` si `ObservableValue<T>.ValueChanged` la
  acelasi mecanism de dispatch.
- [x] Adauga overload-uri `BindingOperations` cu Relay explicit pentru
  tinte generice/neatasate si valideaza mismatch-ul dupa attach.
- [x] Pastreaza fail-fast actionabil cand un binding programatic off-thread nu
  poate rezolva niciun Relay.
- [x] Adauga teste cu 1, 2 si 10.000 de notificari, mai multi produceri, cai
  nested, schimbarea `DataContext`, interpolare si `(A and B) or C`.
- [x] Adauga teste pentru event in-flight simultan cu detach/dispose si confirma
  zero scrieri stale si zero retineri dupa urmatorul drain.
- [x] Adauga teste care confirma ca sursa nu este citita deloc pe worker thread.
- [x] Actualizeaza contractul strict din planul si documentatia data binding:
  CLR `INotifyPropertyChanged` atasat devine auto-marshal, in timp ce
  `UiObject.PropertyChanged` si mutatiile UI directe raman strict UI-thread.
- [x] Reindexeaza dupa fiecare modificare C# sau source-generator.

**Gate etapa 5**

- [x] Toate formele de binding din planul dependent reactioneaza off-thread fara
  a atinge UI-ul pe worker.
- [x] Burst-urile sunt coalesced si ultima stare nu se pierde.
- [x] Lifecycle-ul nu permite callback-uri stale sau subscription leaks.
- [x] Comportamentul UI-thread existent ramane imediat si compatibil.

### Etapa 6 - Alte semnale first-party

- [x] Inventariaza prin Roslyn toate subscription-urile in care un event extern
  ajunge in `Invalidate`, intr-o coada retained sau intr-o mutatie UI.
- [x] Migreaza `ICommand.CanExecuteChanged` la un refresh coalesced pe Relay
  per control/sursa activa.
- [x] Migreaza `ThemeProvider.ThemeChanged` la o invalidare coalesced per root.
- [x] Migreaza `IObservableResourceProvider.ResourceChanged` la callback-uri FIFO
  pe Relay, pastrand ordinea deltelor si fara coalescing nejustificat.
- [x] Verifica unsubscribe si callback stale la schimbarea providerului, detach,
  root replacement si disposal.
- [x] Pastreaza `ObservableList<T>.Changed` UI-thread-only si adauga diagnostic
  inainte de procesarea unei mutatii off-thread observate de un control atasat.
- [x] Adauga exemple si teste pentru mutatia corecta a colectiei prin
  `InvokeAsync`, inclusiv anulare.
- [x] Nu migra un event doar fiindca exista; documenteaza pentru fiecare daca
  este UI-owned, auto-marshaled sau cere dispatch explicit.
- [x] Reindexeaza dupa fiecare modificare C#.

**Gate etapa 6**

- [x] Niciun handler first-party inventariat nu atinge accidental cozi retained
  de pe worker.
- [x] Semnalele coalesced nu pierd starea finala, iar deltele FIFO nu isi schimba
  ordinea.
- [x] Colectiile mutable nu sunt prezentate fals ca thread-safe.
- [x] Toate subscription-urile noi au cleanup verificat.

### Etapa 7 - Performanta, stres si diagnostic

- [x] Adauga benchmark-uri Release pentru `Post`, `InvokeAsync`, drain gol,
  drain 1/100/1.024, backlog peste buget si enqueue cu 1/2/4/8 produceri.
- [x] Adauga benchmark pentru coalescing-ul a 10.000 de notificari intr-un singur
  binding si intr-o interpolare cu mai multe surse.
- [x] Masoara timp, bytes alocati, Gen0 si throughput; arhiveaza hardware-ul,
  runtime-ul si configuratia alaturi de rezultate.
- [x] Verifica structural ca `HasPendingWork` si `PendingCount` nu aloca dupa
  warmup.
- [x] Verifica faptul ca un frame idle fara backlog nu aloca din cauza
  Relay-ului si nu instaleaza inutil obiecte noi de context.
- [x] Ruleaza stress test cu produceri concurenti, anulare, exceptii, detach si
  root replacement pentru cel putin 100.000 de work items, fara sleeps fragile.
- [x] Verifica exact-once: executate + anulate = acceptate, fara pierderi sau
  duplicate.
- [x] Verifica backlog bounded per binding datorita coalescing-ului, chiar daca
  root-ul ramane nepompat temporar.
- [x] Nu adauga pooling pana cand benchmark-ul demonstreaza o problema reala si
  testele pot garanta resetarea completa a work item-ului.
- [x] Reindexeaza dupa modificarile de benchmark/project.

**Gate etapa 7**

- [x] Rezultatele sunt reproductibile si comparate cu baseline-ul.
- [x] Drain-ul respecta bugetul si scaleaza aproximativ liniar.
- [x] Frame-ul idle nu primeste regresii de alocare sau lucru fals.
- [x] Nu exista lost wakeups, duplicate sau callback-uri dupa anulare confirmata.

### Etapa 8 - Documentatie API si verificare finala

- [x] Foloseste skill-ul `writing-api-documentation` pentru toate schimbarile
  publice din `docs-site/documentation/classes/`.
- [x] Adauga pagini pentru `UiRelay` si `UiRelayOptions` si actualizeaza
  `docs-site/documentation/manifest.json`.
- [x] Actualizeaza paginile `UIRoot`, `UiHost`, `MonoGameUiHost`, `FrameStats`,
  `BindingOperations`, `UiPropertyBinding<T>`, `MotionSystem`, `MotionGraph`,
  `ManualMotionTimeline` si orice alt tip public modificat.
- [x] Sterge pagina API `Cerneala.UI.Motion.Core.MotionThreadGuard.md`, elimina
  intrarea din manifest si rescrie toate exemplele care construiau guard-ul.
- [x] Confirma prin cautare text ca `MotionThreadGuard` nu mai apare in C#,
  teste, API docs sau manifest; mentiunile istorice din acest plan sunt singura
  exceptie acceptata.
- [x] Documenteaza branding-ul Relay alaturi de Aspect si Motion, fara alias
  public `Dispatcher` si fara nume amestecate intre namespace, tipuri si host-uri.
- [x] Documenteaza exemple pentru `Post`, `InvokeAsync`, rezultat generic,
  anulare, exceptii si mutatia unei `ObservableList<T>` pe UI thread.
- [x] Documenteaza ordinea exacta fata de input/scheduler, snapshot-ul,
  bugetul, latenta de un update si faptul ca root-ul trebuie pompat.
- [x] Documenteaza diferenta dintre auto-marshaling-ul unei notificari CLR si
  interdictia mutarii directe a unui `UIElement` off-thread.
- [x] Documenteaza obligatia sursei de a permite citire coerenta si faptul ca
  auto-marshaling-ul nu face ViewModel-ul sau colectia thread-safe.
- [x] Documenteaza lipsa `Invoke` blocant, lipsa prioritatilor si riscul de
  deadlock al asteptarii sincrone pe owner thread.
- [x] Actualizeaza documentatia conceptuala de hosting si data binding, fara a
  pune API docs sub `docs/documentation/`.
- [x] Ruleaza un diff public API si confirma ca toate adaugarile sunt intentionate,
  nullable corecte si documentate.
- [x] Ruleaza formatterul, `dotnet build .\Cerneala.slnx` si
  `dotnet test .\Cerneala.slnx`.
- [x] Ruleaza testele tinta cu
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiRelay|FullyQualifiedName~Relay"`.
- [x] Ruleaza testele de hosting, data binding, Motion, Aspect, invalidation,
  lifecycle si Windows runtime.
- [x] Ruleaza benchmark-urile finale in Release si salveaza raportul.
- [x] Regenereaza `FileTree.md`, reindexeaza `Cerneala.slnx --json` si confirma
  zero warnings ale indexerului.
- [x] Ruleaza `git diff --check` si confirma ca nu au ramas fisiere temporare,
  procese sau teste flaky.

**Gate etapa 8**

- [x] Build-ul si suita completa sunt verzi.
- [x] API diff-ul contine numai suprafata aprobata.
- [x] Documentatia API si manifestul sunt sincronizate.
- [x] Benchmark-urile si testele de stres sunt arhivate si reproductibile.

## 10. Riscuri si mitigari

### Lost wakeup intre coalescing si drain

- [x] Foloseste versiuni atomice si teste cu bariere in toate ferestrele de race,
  nu un simplu flag resetat naiv.

### Callback async care blocheaza frame-ul

- [x] Drain-ul porneste delegatul async si urmareste task-ul fara sa astepte
  sincron completarea lui.

### Backlog nelimitat pe un root nepompat

- [x] Coalesce-uieste automat semnalele de stare, expune `PendingCount`, aplica
  buget per update si documenteaza anularea pentru operatiile explicite.

### Exceptie fire-and-forget pierduta

- [x] Proceseaza snapshot-ul complet, numara fault-ul si arunca agregat din
  update; nu lasa erorile sa dispara in neant ca salariul dupa facturi.

### Mutatie UI deja facuta inainte de marshal

- [x] Verifica accesul la intrarile canonice de mutatie; nu incerca sa marshal-uiesti
  `UiObject.PropertyChanged` dupa ce property store-ul a fost schimbat.

### Colectie citita in timp ce worker-ul o modifica

- [x] Pastreaza `ObservableList<T>` UI-thread-only si cere postarea mutatiei
  complete pe Relay.

### Dublu drain in `UiHost.Update`

- [x] Separa poarta publica `UIRoot.ProcessFrame` de core-ul intern folosit de
  host si testeaza explicit update-ul pre-input plus post-input.

### Standalone Windows nu se trezeste

- [x] Include backlog-ul Relay-ului in wake predicate si testeaza un root
  complet idle cu un singur callback worker.

### Migrarea breaking a Motion lasa o cale veche in urma

- [x] Sterge guard-ul si overload-urile vechi intr-un singur batch coerent,
  migreaza toate call site-urile din repo si valideaza explicit API diff-ul;
  compatibilitatea urmarita este comportamentul standalone, nu semnaturile vechi.

### ExecutionContext retine obiecte

- [x] Curata referintele dupa executie/anulare si adauga teste cu weak references
  pentru callback-uri si controllere detasate.

## 11. Conditii de oprire

Implementarea se opreste pentru reevaluare daca:

Audit final: niciuna dintre conditiile de oprire nu s-a activat. Bifele de mai
jos confirma explicit verificarea negativa a fiecarei conditii.

- [x] Confirmat ca frame loop-ul poate garanta un singur drain fara schimbarea publica a
  semanticii `UiHost.Update`;
- [x] Confirmat ca `WindowApplicationRuntime` si root-urile sale nu au owner threads
  legitim diferite in configuratia actuala;
- [x] Confirmat ca integrarea cu binding-urile nu evalueaza cai CLR pe worker;
- [x] Confirmat ca versiunile atomice evita lost wakeups fara o coada nebounded per event;
- [x] Confirmat ca nu este necesar un blocking invoke sau nested pump pentru compatibilitate;
- [x] Confirmat ca mutatiile first-party de colectie nu sunt acceptate off-thread fara
  o sursa thread-safe;
- [x] Confirmat ca API-ul public nu acumuleaza prioritati, timers sau scheduler generic
  fara cerinta separata;
- [x] Confirmat ca benchmark-urile nu arata o regresie idle persistenta care nu poate fi izolata.

Problema se documenteaza si se discuta separat; nu o acoperim cu un lock global
si o rugaciune.

## 12. Ordinea recomandata

1. Caracterizeaza thread-ul, frame order si defectele actuale.
2. Construieste si verifica izolat nucleul MPSC.
3. Adauga contextul async scoped.
4. Integreaza o singura poarta de drain in root si host-uri.
5. Unifica thread affinity pentru property store, arbore, Motion si Aspect si
   sterge complet `MotionThreadGuard`.
6. Dupa finalizarea planului de data binding, adopta Relay-ul si coalescing-ul
   in controllerul reactiv comun.
7. Migreaza celelalte semnale first-party conform tabelului, fara adaptor magic.
8. Inchide performanta, documentatia si suita completa.

## 13. Definitia de gata

- [x] Orice `Post`/`InvokeAsync` acceptat de Relay ruleaza exact o data sau
  este anulat explicit, niciodata pe worker si niciodata de doua ori.
- [x] Relay-ul este drenat o singura data la inceputul update-ului, inainte de
  scheduler si input, cu snapshot si buget determinist.
- [x] Un root Windows idle este trezit de backlog, iar un host MonoGame il
  proceseaza la urmatorul `Update`.
- [x] `await` din cod UI revine prin contextul root-ului corect, iar contextul
  anterior este restaurat dupa update.
- [x] Mutatiile directe ale unui element atasat sunt respinse off-thread inainte
  sa schimbe property store-ul, arborele sau cozile retained.
- [x] Motion si Aspect folosesc autoritatea Relay a root-ului; nu exista
  `MotionThreadGuard`, `AspectThreadGuard` sau alta sursa paralela de owner thread.
- [x] `MotionSystem.ThreadGuard`, constructorii care primeau guard-ul, pagina API
  si intrarea din manifest au disparut, iar Motion standalone ramane functional.
- [x] Binding-urile simple, nested, `TwoWay`, interpolate, conditionale si
  expresiile `@when` auto-marshal-uiesc `INotifyPropertyChanged` off-thread,
  coalesce-uiesc burst-urile si afiseaza ultima stare.
- [x] Detach, reattach, template swap, root replacement si disposal nu lasa
  callback-uri stale, task-uri uitate sau referinte retinute.
- [x] `CanExecuteChanged`, tema si resursele urmeaza politica declarata, iar
  colectiile mutable cer folosirea explicita a Relay-ului.
- [x] Frame-ul idle ramane ieftin, backlog-ul este observabil, iar benchmark-urile
  confirma scalarea multi-producer si drain-ul bugetat.
- [x] Toate API-urile publice noi sunt documentate in sursa unica de adevar,
  manifestul este sincronizat, API diff-ul este aprobat, build-ul si toate
  testele sunt verzi.
- [x] Suprafata publica poarta consecvent branding-ul `Relay`: namespace
  `Cerneala.UI.Relay`, `UiRelay`, `UiRelayOptions` si proprietatile `.Relay`, fara
  dubluri publice numite `Dispatcher`.

Sistemul este gata cand un worker poate anunta UI-ul fara sa-l atinga direct,
frame loop-ul decide exact cand ruleaza lucrul, iar o rafala de notificari nu
transforma update-ul intr-un tomberon de callback-uri nedeterministe.
