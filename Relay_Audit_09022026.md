# Audit Relay — 2 septembrie 2026

## Verdict

Relay are un nucleu MPSC simplu, ownership per `UIRoot`, integrare coerentă cu frame loop-ul și teste bune pentru FIFO, buget, anulare, `ExecutionContext`, lifecycle și surse reactive. Auditul nu a găsit pierderi sau dublări de callback în scenariile existente, iar întreaga soluție este verde.

Totuși, verdictul nu este „fără defecte”. Au fost confirmate trei lacune de comportament/contract public și o derivă de documentație:

- **MEDIUM:** un lambda `async () => ...` este legat de overload-ul generic `InvokeAsync<T>(Func<T>)`, rezultând `Task<Task>`; primul task se termină înaintea operației async, iar un `await` obișnuit poate abandona completarea și excepția reală;
- **MEDIUM:** `UiHost.Update` drenează Relay înainte să aplice viewport-ul transmis acelui update; callback-ul vede viewport-ul vechi, contrar ordinii declarate în planul Relay;
- **LOW:** anularea cooperativă după pornirea callback-ului async păstrează starea `Canceled`, dar pierde identitatea tokenului de anulare;
- **LOW:** documentația canonică, planul marcat complet și benchmarkul committed conțin mai multe afirmații care nu mai corespund implementării sau testelor.

Cele 81 de teste selectate prin filtrul Relay au trecut. Verificarea completă a soluției a trecut cu **4.062 passed, 0 failed, 7 skipped**. Green-ul nu infirmă constatările: niciun test permanent nu acoperă lambda-ul async fără parametru, ordinea viewport-versus-drain sau identitatea tokenului după anulare cooperativă.

## Stare după remediere — 2 septembrie 2026

Toate cele patru constatări Relay au fost remediate:

- `UiRelay` expune acum un overload explicit `InvokeAsync(Func<Task>, CancellationToken)`, unwrap-uiește operația și propagă completarea sau fault-ul real fără `Task<Task>`;
- `UiHost.UpdateCore` aplică viewport-ul și invalidările de pregătire înainte de drain, păstrând contextul Relay activ pe întreg update-ul;
- completarea cooperativ anulată păstrează tokenul transportat de `OperationCanceledException`;
- documentația canonică, planul completed și README-ul benchmarkului descriu acum comportamentul testat și numele real `MaxCallbacksPerUpdate`.

Cele trei reproduceri permanente au fost confirmate RED înaintea modificării de producție și GREEN după fix. Filtrul Relay extins este verde cu **84 passed, 0 failed**, iar întregul proiect `Cerneala.Tests` este verde cu **3.159 passed, 0 failed, 2 skipped**.

Poarta întregii soluții nu este verde în sesiunea de remediere: a produs **4.062 passed, 3 failed, 7 skipped**. Cele trei eșecuri sunt în `Cerneala.Tests.LanguageServer`, proiect care referențiază numai `Cerneala.LanguageServer` și `Cerneala.SourceGen`, nu Cerneala UI/Relay. Un timeout s-a vindecat la rerulare izolată; două teste de buget P95 au rămas peste prag atât sub concurență externă, cât și după terminarea ei. Cauza lor nu a fost diagnosticată în acest task și nu este atribuită Relay fără dovezi. Acesta este un blocker de verificare globală, nu dovadă că Relay este complet verde la nivel de repository.

## Natura și scopul auditului

Auditul inițial a fost read-only asupra contractelor, arhitecturii, implementării, integrărilor, testelor și artefactelor de performanță Relay. Documentul a fost extins ulterior cu starea remedierii, fixurile de producție și testele permanente.

Au fost inspectate:

- implementarea din `UI/Relay/**`;
- ownership-ul și pomparea din `UIRoot`, `UiHost`, `MonoGameUiHost` și `WindowApplicationRuntime`;
- coalescing-ul reactiv prin `UiRelayRefreshDispatcher` și consumatorii din binding, markup, command state, theme și resources;
- delegarea autorității de thread către Aspect și Motion prin `IUiThreadAccess`;
- documentația canonică pentru `UiRelay`, `UiRelayOptions`, `UIRoot` și `UiHost`;
- planul completat `docs/plans/2026-07-14-relay-auto-marshaling.md` și notele lui de implementare;
- testele Relay, binding, hosting, window runtime și affinity;
- benchmarkurile și rezultatele committed din `benchmarks/results/2026-07-14-relay/`.

Nu a fost făcută validare manuală de UI și nu au fost rerulate benchmarkurile BenchmarkDotNet. Probele runtime izolate au folosit numai API-uri publice și au fost executate într-un proiect temporar din `%TEMP%`, în afara globurilor SDK ale repository-ului.

## Contracte observate

Evaluarea s-a bazat pe următoarele contracte declarate și testate:

1. fiecare `UIRoot` deține un singur `UiRelay`, iar thread-ul constructorului root-ului este owner-ul;
2. Relay mută execuția, nu face arborele UI sau sursele mutable thread-safe;
3. `Post` și `InvokeAsync` sunt async-first și nu există un `Invoke` blocant public;
4. coada este multi-producer/single-consumer, FIFO după linearizarea enqueue-ului și exact-once pentru work acceptat;
5. drain-ul procesează un snapshot stabil, limitat numeric de `MaxCallbacksPerUpdate`, iar repostările așteaptă update-ul următor;
6. `ExecutionContext` curge de la producer, iar contextul de sincronizare Relay este instalat temporar și restaurat;
7. excepțiile `InvokeAsync` apar pe task, în timp ce excepțiile `Post` sunt agregate după continuarea snapshot-ului;
8. anularea câștigată înainte de execuție împiedică apelarea callback-ului; după pornire, overload-ul async cooperează prin token;
9. Relay este drenat o singură dată per update, înainte de scheduler și input, iar standalone windows se trezesc când există backlog;
10. notificările reactive off-thread sunt fie coalesced ca refresh de stare curentă, fie postate FIFO când delta nu poate fi colapsată;
11. mutațiile directe ale UI-ului atașat rămân UI-affine și eșuează înainte de schimbarea stării.

Sursele principale sunt planul Relay marcat `completed`, documentația canonică din `docs-site/documentation/classes/`, implementarea și testele permanente.

## Arhitectură observată

Separarea de ownership este în mare parte sănătoasă:

- `UIRoot` construiește și expune Relay-ul; host-urile doar îl proiectează și nu creează o autoritate paralelă;
- `UiRelay` deține transportul MPSC, work item-urile și stările atomice `Pending/Running/Completed/Canceled`;
- `UIRoot.BeginUpdate` instalează contextul Relay, drenează snapshot-ul și publică rezultatul în `FrameStats`;
- `UiHost.UpdateCore` păstrează un singur scope peste pregătire, scheduler, input și commit, evitând două drain-uri în același update;
- `WindowApplicationRuntime.PumpOnce` include `Root.Relay.HasPendingWork` în wake predicate;
- `UiRelayRefreshDispatcher` deține coalescing-ul și generation guards; consumatorii de binding/markup/input nu implementează fiecare o coadă proprie;
- Aspect și Motion root-owned primesc `IUiThreadAccess` din Relay, iar obiectele standalone folosesc `CapturedUiThreadAccess` fără să publice un al doilea dispatcher;
- resursele păstrează deltele FIFO, în timp ce theme, bindings și command re-query folosesc refresh coalesced al stării curente.

Această arhitectură este proporțională cu problema. Nu există `Task.Run` consumer, thread UI secundar, priority scheduler sau nested message pump ascuns.

## Findings

### RELAY-AUD-001 — MEDIUM — Lambda-ul async fără parametru produce `Task<Task>` și semnalizează completarea prea devreme

**Stare: REMEDIAT.** Overload-ul explicit `InvokeAsync(Func<Task>, CancellationToken)` elimină selecția generică accidentală. Testul `ParameterlessAsyncInvokeTracksCompletionAndFaultWithoutNestedTask` verifică faptul că task-ul rămâne incomplet după pornirea delegate-ului și propagă fault-ul produs după `await`.

**Comportament observat**

Proba publică a apelat:

```csharp
var operation = root.Relay.InvokeAsync(async () =>
{
    await Task.Yield();
    flag = 1;
});
```

Rezultatul observat a fost:

```text
type=System.Threading.Tasks.Task`1[System.Threading.Tasks.Task]
after1 completed=True; flag=0; pending=1
after2 completed=True; flag=1; pending=0
```

Compilatorul selectează `InvokeAsync<T>(Func<T>)` cu `T == Task`, nu overload-ul async cu token. După primul frame, task-ul exterior este complet, dar continuarea reală este încă în Relay. În forma naturală:

```csharp
await root.Relay.InvokeAsync(async () =>
{
    await SaveAsync();
});
```

primul `await` produce task-ul interior ca rezultat, iar expression statement-ul îl abandonează. Caller-ul poate continua prea devreme, iar o excepție după primul `await` nu mai este observată de task-ul pe care crede că l-a așteptat.

**Contract afectat**

Documentația canonică spune că `InvokeAsync` este calea pentru completion și exception propagation (`Cerneala.UI.Relay.UiRelay.md:32,92`). Suprafața publică are un overload async numai pentru `Func<CancellationToken, Task>` (`UI/Relay/UiRelay.cs:72`), iar overload-ul generic sincron este la liniile `63-69`. Combinația permite un apel C# perfect natural care arată corect, compilează, dar nu respectă intenția async-first.

**Impact**

- operații dependente pot porni înainte ca lucrarea UI să se termine;
- excepțiile din continuarea async pot rămâne neobservate;
- anularea și lifetime-ul perceput de caller nu mai corespund operației reale;
- tipul `Task<Task>` este ușor de ascuns prin `var` și printr-un singur `await`.

**Owner probabil al invariantului**

Owner-ul este setul de overload-uri publice `UiRelay.InvokeAsync`, nu `SynchronizationContext` și nu frame loop-ul. O rezolvare trebuie să decidă explicit contractul pentru `Func<Task>` și să adauge teste de overload resolution/completion/fault; nu trebuie mascată în consumeri.

### RELAY-AUD-002 — MEDIUM — Callback-ul Relay vede viewport-ul vechi în update-ul care primește un viewport nou

**Stare: REMEDIAT.** `UiHost.UpdateCore` aplică viewport-ul înainte de `BeginUpdate`/drain, sub același `UiRelaySynchronizationContext`. Testul `HostAppliesTheCurrentViewportBeforeDrainingRelay` observă `640x480` în callback-ul din update-ul care primește acel viewport.

**Comportament observat**

Proba publică a creat un root `100x100`, a postat un callback care citește `root.ViewportWidth`, apoi a executat `host.Update(..., new UiViewport(640, 480), ...)`.

Rezultat:

```text
callback=100; after=640
```

Sursa confirmă ordinea:

- `UiHost.UpdateCore` intră în `currentRoot.BeginUpdate(stats)` la `UI/Hosting/UiHost.cs:129`;
- `BeginUpdate` drenează Relay;
- abia apoi `ApplyViewportIfChanged` rulează la `UI/Hosting/UiHost.cs:132`.

**Contract afectat**

Planul Relay marcat complet declară ordinea opusă: „Apply viewport, initial-frame and time-sensitive invalidations”, apoi „Drain Relay once” (`docs/plans/2026-07-14-relay-auto-marshaling.md:323-324`).

Documentația canonică `UiHost` spune doar că update-ul aplică viewport-ul și drenează înainte de scheduler/input, deci nu elimină contradicția. Trebuie decis dacă planul este încă autoritativ sau dacă stale viewport este comportamentul intenționat; starea actuală nu documentează această diferență.

**Impact**

Un callback Relay care calculează stare, layout auxiliar sau resurse în funcție de viewport folosește dimensiunile frame-ului anterior. Schimbarea viewport-ului se aplică în același update după callback, deci rezultatul callback-ului poate fi inconsistent cu scheduler-ul și input-ul care urmează.

**Owner probabil al invariantului**

Owner-ul este ordinea de pregătire din `UiHost.UpdateCore`. `UiRelay.Drain` execută corect atunci când este chemat; mutarea logicii în work item-uri sau adăugarea de repostări ar fi workaround-uri.

### RELAY-AUD-003 — LOW — Anularea async după pornire pierde tokenul original

**Stare: REMEDIAT.** `AsyncWorkItem.CompleteFromTask` extrage tokenul din `OperationCanceledException` și îl folosește la completarea task-ului wrapper. Testul `CancellationAfterAsyncStartPreservesTheOperationToken` verifică identitatea tokenului.

**Comportament observat**

Proba a pornit `InvokeAsync(async token => { await Task.Yield(); token.ThrowIfCancellationRequested(); }, cancellation.Token)`, a drenat startul, a anulat tokenul și a drenat continuarea.

Rezultat:

```text
canceled=True; tokenMatches=False; canCancel=False
```

Task-ul wrapper este corect în starea `Canceled`, dar `OperationCanceledException.CancellationToken` este tokenul default, nu tokenul furnizat.

**Dovezi în sursă**

- anularea înainte de start folosește `TrySetCanceled(token)` (`UI/Relay/UiRelay.cs:347,390,448`);
- completarea unui task async deja anulat folosește `completion.TrySetCanceled()` fără token (`UI/Relay/UiRelay.cs:463-468`).

**Impact**

Codul care diferențiază sursele de anulare prin identitatea tokenului, filtre `catch` sau diagnostic pierde informația numai pe calea cooperativă post-start. Starea task-ului și exact-once rămân corecte, de aceea severitatea este LOW.

**Owner probabil al invariantului**

Owner-ul este `AsyncWorkItem.CompleteFromTask`. Orice fix trebuie să păstreze atât starea, cât și tokenul operației fără a bloca drain-ul.

### RELAY-AUD-004 — LOW — Documentația și ledger-ul „completed” nu sunt sincronizate cu implementarea

**Stare: REMEDIAT.** Contractul păstrează skip-ul pre-canceled, `InvalidOperationException` pentru `SynchronizationContext.Send` off-thread și statisticile exclusiv per-frame. Documentația canonică, planul și README-ul benchmarkului au fost sincronizate cu aceste decizii; manifestul nu necesită modificare deoarece nu a fost adăugată sau redenumită nicio pagină.

Au fost confirmate patru divergențe:

1. Documentația canonică afirmă că `Post` și fiecare overload `InvokeAsync` „always enqueue work” (`Cerneala.UI.Relay.UiRelay.md:84`). Implementarea sare enqueue-ul când tokenul era deja anulat (`UI/Relay/UiRelay.cs:147-153`), iar testul permanent cere explicit zero backlog (`UiRelayCoreTests.cs:141-152`). Comportamentul testat este rezonabil; afirmația absolută din docs este falsă.
2. Planul spune că `SynchronizationContext.Send` off-thread aruncă `NotSupportedException` (`relay-auto-marshaling.md:90`). Implementarea și testele folosesc `InvalidOperationException` (`UiRelaySynchronizationContext.cs:18-25`). Tipul este intern, dar ledger-ul nu mai reproduce implementarea.
3. Planul spune că `UiRelay` păstrează „internal cumulative counters” (`relay-auto-marshaling.md:475`). Implementarea păstrează numai `pendingCount`; contabilizarea execute/cancel/fault/deferred este per-frame în `FrameStats`, fără contoare cumulative Relay.
4. README-ul benchmarkului numește opțiunea `MaxCallbacksPerFrame` (`benchmarks/results/2026-07-14-relay/README.md:33`), în timp ce API-ul real și restul contractului folosesc `MaxCallbacksPerUpdate`.

Acestea nu demonstrează defecte suplimentare ale cozii, dar reduc valoarea documentației ca sursă reproductibilă de contract. Pentru primul punct trebuie corectată documentația sau schimbat comportamentul numai după o decizie explicită; testul existent arată că skip-ul pre-canceled este intenționat în implementarea curentă.

## Verificări executate

| Verificare | Rezultat |
| --- | --- |
| `New-FileTree.ps1` + inspecție `FileTree.md` | structură Relay, teste, benchmarkuri și docs identificate; fișierul generat a fost restaurat ulterior |
| RoslynIndexer `index .\Cerneala.slnx --json` | 3.577 documente, 92.458 simboluri, 376.600 referințe; index incremental reîmprospătat |
| RoslynIndexer după modificările C# | 3.577 documente, 92.487 simboluri, 376.691 referințe; index incremental reîmprospătat cu succes |
| `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-restore --filter FullyQualifiedName~Relay` | 81 passed, 0 failed |
| `dotnet test .\Cerneala.slnx --no-restore` | 4.062 passed, 0 failed, 7 skipped |
| probă publică async lambda fără parametru | `Task<Task>`; outer completed după frame 1, continuarea după frame 2 |
| probă publică viewport-versus-drain | callback `100`, root după update `640` |
| probă publică token cooperativ | task canceled; tokenul excepției default, nu tokenul sursă |
| test structural de alocare Relay existent | trecut în suita focalizată; zero bytes după warmup conform aserțiunii testului |
| cele trei teste permanente, înainte de fix | 3 failed pentru motivele intenționate: nested-task/completion prematură, viewport vechi și token pierdut |
| cele trei teste permanente, după fix | 3 passed, 0 failed |
| filtrul Relay după fix | 84 passed, 0 failed |
| `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-restore` după fix | 3.159 passed, 0 failed, 2 skipped |
| `dotnet format .\Cerneala.slnx --no-restore --verify-no-changes --include ...` pentru cele patru fișiere C# modificate | passed; fără schimbări de formatare necesare |
| `dotnet test .\Cerneala.slnx --no-restore` după fix | 4.062 passed, 3 failed, 7 skipped; eșecurile sunt exclusiv LanguageServer |
| rerulare izolată a celor trei eșecuri LanguageServer | timeout-ul a trecut; două gate-uri P95 au eșuat cu 129,85 ms și 103,33 ms sub concurență externă |
| rerulare a celor două gate-uri P95 după terminarea concurenței externe | ambele au rămas roșii: 127,31 ms și 100,31 ms; ipoteza „numai contention” a fost respinsă |

Cele șapte teste skipped din verificarea completă sunt gate-uri SDL_GPU/native sau de conformance care cer capabilități native indisponibile în sesiune. Nu sunt teste Relay focalizate, dar rămân neverificate în full-suite.

## Rezultate negative

Auditul a căutat și nu a confirmat următoarele clase de probleme în aria verificată:

- nu au fost observate callback-uri pierdute sau duplicate în testele FIFO, snapshot, budget și multi-producer;
- stress-ul existent cu 100.000 de `InvokeAsync` a contabilizat exact toate operațiile;
- anularea înainte de drain și cursa deterministă dequeue-versus-cancel au trecut;
- excepțiile `Post` nu abandonează restul snapshot-ului, iar excepțiile action/result `InvokeAsync` ajung pe task;
- `ExecutionContext`, culture, `AsyncLocal` și `Activity` curg în testele existente;
- două roots pe același thread nu își amestecă continuările;
- contextul Relay este restaurat după drain și după fault în scenariile testate;
- host-ul drenează o singură dată, procesează invalidările Relay în același update și amână munca postată de input;
- standalone window se trezește pentru backlog și evită graphics session după închiderea ferestrei din callback;
- binding bursts sunt coalesced, source path nu este citit pe worker, iar detach/rebind invalidează callback-urile stale în testele existente;
- theme și command state coalesce corect, iar resource deltas păstrează FIFO;
- mutațiile directe ale UI-ului atașat, Motion și Aspect eșuează off-thread înainte de schimbarea stării;
- API-ul public nu expune `Invoke`/`Send` blocant și host-urile nu dublează ownership-ul Relay.

Aceste rezultate înseamnă numai că verificările enumerate au trecut. Nu demonstrează absența altor curse sau probleme în toate sursele reactive terțe.

## Performanță și observabilitate

Artefactul istoric din 14 iulie raportează, pe configurația lui:

- `Post`: 1,920 µs și 64 B/op în baseline-ul inițial, 2,420 µs în run-ul final;
- `InvokeAsync`: 2,640 µs și 168 B/op inițial, 2,940 µs final;
- drain 1.024 callback-uri: 56,575 µs inițial, 71,500 µs final, 120 B/frame;
- zero alocări pentru citirile idle și `Drain` după warmup în testul structural.

Aceste numere sunt istorice, rulează un job foarte scurt și chiar README-ul avertizează asupra intervalelor largi/outlierilor. `UiRelayRefreshDispatcher` a fost modificat ulterior, în august. Auditul nu revendică throughput sau alocări curente pe baza lor. Singura dovadă curentă de alocare este testul structural focalizat care a trecut.

Observabilitatea per-frame este utilă: snapshot, dequeued, executed, canceled, faulted, deferred și backlog ajung în `FrameStats`. Nu există contoare cumulative Relay, iar planul a fost corectat ca să nu le mai pretindă.

## Recomandări ordonate

1. **COMPLET:** contractul async parameterless are overload explicit, test RED/GREEN pentru completion și fault și documentație canonică.
2. **COMPLET:** planul rămâne autoritativ; viewport-ul curent este aplicat înainte de drain și este verificat prin test public.
3. **COMPLET:** anularea cooperativă păstrează identitatea tokenului și are aserțiune permanentă.
4. **COMPLET:** documentația canonică, planul și README-ul benchmarkului sunt sincronizate fără schimbarea contractului pre-canceled.
5. **RĂMAS:** benchmarkurile nu au fost rerulate. Mașina nu a demonstrat o stare potrivită pentru măsurători reproductibile: chiar și după terminarea suitelelor concurente, două gate-uri P95 fără ownership Relay au rămas peste prag.

## Limite și incertitudini rămase

- Nu a fost executat un stress nou care să intercaleze continuu producers și drain-ul pe milioane de operații; au fost folosite testele concurente existente și stress-ul de 100.000 de itemi.
- Nu au fost probate toate combinațiile de dispose/rebind concurent din surse reactive terțe.
- Nu au fost rerulate benchmarkurile BenchmarkDotNet și nu se revendică performanță curentă; gate-urile P95 LanguageServer indică faptul că mediul sau acel subsistem necesită diagnostic separat înaintea unei comparații de performanță.
- Nu a fost făcută validare manuală de UI sau captură de screenshot; auditul Relay nu a necesitat o afirmație de conformanță vizuală.
- Șapte teste native/conformance au fost skipped în full-suite.
- RoslynIndexer a raportat avertismente pentru două project references fără metadata matching și pentru fișiere prea mari omise din index; fișierele C# Relay și integrările analizate au fost indexate și citite semantic.
- Poarta full-solution rămâne roșie din cauza celor două gate-uri P95 LanguageServer reproductibile și a unui timeout văzut numai în run-ul complet; nu au fost slăbite pragurile și nu a fost modificat cod LanguageServer.

## Artefacte și cleanup

- Auditul a creat `Relay_Audit_09022026.md`; remedierea a modificat producția Relay/hosting, testele permanente și documentația enumerată în secțiunea de stare.
- Proiectul temporar de reproducere din `%TEMP%\CernealaRelayAudit` a fost șters.
- `FileTree.md`, regenerat pentru orientare, a fost restaurat la starea Git.
- Niciun proiect temporar și niciun artefact benchmark nou nu au rămas în repository.
