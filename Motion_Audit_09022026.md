# Audit Motion — 2 septembrie 2026

## Verdict

**NECONFORM.** Implementarea Motion are o bază arhitecturală coerentă și acoperire automată largă, dar nu respectă toate contractele publicate. Auditul a confirmat două defecte cu severitate ridicată care pot lăsa handle-uri permanent active și `Completion` neterminat, un defect de recuperare a tranzacțiilor, diagnostice care raportează contoare false și diferențe materiale între planul declarat complet, documentația de ansamblu și API-ul livrat.

Nu am modificat implementarea Motion. Singurul artefact produs de audit este acest raport.

## Stare după remediere — 2 septembrie 2026

**REMEDIAT pentru toate cele șase constatări Motion.** Textul de mai sus descrie baseline-ul auditat; remedierea ulterioară a fost făcută într-un al doilea worktree temporar, pornit de la commitul `590a5ca4c492f098dc78dc2956b8ab7a6272d4a4`.

| Constatare | Remediere verificată |
|---|---|
| `MOTION-AUD-001` | Traseele terminale folosesc terminalizare garantată după detașarea din graf; excepția subscriberului continuă să fie propagată, dar handle-ul devine terminal și nodul rămâne eliminat. |
| `MOTION-AUD-002` | Secvența observă handle-uri deja terminale, anulează grupul când copilul este anulat și respinge explicit fabricile care întorc `null`. |
| `MOTION-AUD-003` | Scope-ul este marcat disposed numai după un `Pop` reușit, astfel încât ordinea poate fi corectată și stiva recuperată. |
| `MOTION-AUD-004` | Snapshot-ul folosește contoarele ultimului `MotionFrameResult` și numără numai binding-urile/property/layout motions efectiv active. |
| `MOTION-AUD-005` | Prioritățile sunt aplicate în traseul de producție (`Interactive < Normal < ReducedMotion`); state builder-ul are `When(...).Set(...)`; registry-ul de timeline-uri are operații publice thread-affine; documentația și planul au fost sincronizate. |
| `MOTION-AUD-006` | Setterul `MaxDelta` și sampler-ele Repeat/PingPong resping deltele negative la intrare. |

Regresiile permanente au fost confirmate RED înaintea modificărilor de producție: 11 teste pentru defectele 001–004 și 006 au eșuat din motivele intenționate, iar cele șase teste inițiale pentru capabilitățile din 005 nu compilau deoarece API-urile lipseau. După implementare, toate testele respective și testele suplimentare de integrare Aspect/prioritate și registry thread-affinity sunt verzi.

Reproducătorul public-API refăcut după remediere a produs:

```text
terminal-listener: exception=InvalidOperationException; active=False; completed=True; graphActive=False
sequence-precompleted: started=2; completed=True; canceled=False
sequence-child-cancel: completed=False; canceled=True
transaction-recovery: first=InvalidOperationException; remainingDepth=0
diagnostics: frameSampled=1; snapshotSampled=1; snapshotActiveBindings=0
negative-time: maxDelta=ArgumentOutOfRangeException; repeatDelta=ArgumentOutOfRangeException
```

Verificare după remediere:

| Verificare | Rezultat |
|---|---:|
| `Cerneala.Tests`, filtru `FullyQualifiedName~Motion` | 292 PASS, 0 FAIL, 0 SKIP |
| `Cerneala.Tests`, filtru `FullyQualifiedName~AspectEngineTests` | 15 PASS, 0 FAIL, 0 SKIP |
| `Cerneala.Tests.SourceGen`, filtru Motion | 182 PASS, 0 FAIL, 0 SKIP |
| `Cerneala.Tests.Language`, filtru Motion | 52 PASS, 0 FAIL, 0 SKIP |
| Manifest/documentație canonică | 1.080 intrări, 0 fișiere lipsă, 0 nume duplicate, 0 placeholders în cele 17 pagini verificate |
| `dotnet test .\Cerneala.slnx --no-restore --nologo` | FAIL din blocajele non-Motion deja reproduse de audit |

În poarta completă, `Cerneala.Tests` a avut 3.187 pass, 0 fail și 2 skip. Au rămas exact clasele de eșec non-Motion documentate mai jos: un test Language pentru CRLF/LF, două teste LanguageServer (timeout și executabil Release absent) și fixture-ul `VisualStudioIntegrationHost` fără `project.assets.json`; testele native/conformance marcate skip nu au fost forțate. Nu există validare manuală UI, capturi vizuale sau benchmark-uri noi.

## Baseline și metodă

- Commit auditat: `5d0123ead1fa6875cb4c6feca80ec568fd6eb6ca` (`fix(relay): repair async dispatch contracts and host ordering`, 2026-09-02 17:38:18 +03:00).
- Auditul a rulat într-un worktree temporar separat: `C:\Users\lauri\Desktop\Cerneala-motion-audit-09022026`.
- Nu au fost folosite skill-uri, conform cererii.
- Înainte de inspecția largă am regenerat și citit `FileTree.md`.
- Pentru navigarea și citirea semantică a surselor C# am indexat `Cerneala.slnx` cu RoslynRepoIndexer: 3.579 documente, 92.757 simboluri, 358.944 referințe și 3.190.449 tokenuri. Indexerul a raportat 10 avertismente interne; indexarea a reușit.
- Au fost inspectate cele 116 fișiere C# urmărite de Git din `UI/Motion`, integrarea cu `UIRoot`/`UiFrameScheduler`, testele Motion din proiectele core, SourceGen și Language, documentația publică și planul `docs/superpowers/plans/2026-07-07-modern-motion-system.md`.
- Defectele critice de stare au fost reproduse printr-un executabil public-API separat, în afara repository-ului, referit la assembly-ul construit din worktree. Nu au fost introduse fișiere-probă în proiectele Cerneala.

## Contracte folosite ca reper

1. Un `MotionHandle` sau `MotionGroupHandle` devine terminal după completare, anulare sau eroare (`docs/motion-system.md:14`).
2. O secvență pornește pasul următor după terminarea pasului curent și handle-ul de grup se completează după toate etapele reușite (`docs-site/documentation/classes/Cerneala.UI.Motion.Core.MotionSequence.md:57-61`).
3. `MotionTransactionScope.Dispose` este idempotent după primul `Pop` reușit (`docs-site/documentation/classes/Cerneala.UI.Motion.Transactions.MotionTransactionScope.md:48`).
4. Snapshot-urile de diagnostic raportează starea și activitatea Motion, inclusiv valori eșantionate și proprietăți scrise (`docs/motion-diagnostics.md:9`).
5. Implementările concrete `MotionSampler<T>` resping delte negative (`docs-site/documentation/classes/Cerneala.UI.Motion.Specs.MotionSampler_T_.md:59`).

## Constatări

### MOTION-AUD-001 — RIDICAT — o excepție dintr-un subscriber poate abandona un handle neterminat

**Comportament observat**

Un listener al unui `MotionValue<T>` care aruncă în momentul eșantionului terminal face ca `MotionSystem.Tick` să propage excepția, dar handle-ul rămâne `IsActive == true`, `Completion` rămâne incomplet, iar graful nu mai conține nodul.

Reproducere:

```text
PROBE completion-listener-fault
tick=InvalidOperationException; handleActive=True; handleCompleted=False;
handleCanceled=False; completionDone=False; graphActive=False
```

**Cauză confirmată**

În `UI/Motion/Core/MotionValue{T}.cs`, traseul de completare naturală detașează mai întâi mișcarea din graf (`:277`), apoi notifică listenerii prin `ApplySample(completionValue)` (`:283`) și abia după aceea apelează `FinishCompleted` (`:285`). Dacă listenerul aruncă, ultima operație nu mai rulează. Aceeași ordine vulnerabilă există în traseele de `Complete` și anulare cu aplicarea unei valori terminale (`:201-208`, `:251` și următoarele).

**Impact**

- consumatorii care așteaptă `Completion` pot aștepta permanent;
- handle-ul declară că este activ, deși graful nu îl mai poate avansa;
- compoziții precum grupurile și secvențele pot rămâne blocate;
- contractul de terminalizare publicat este încălcat.

**Remediere recomandată**

Definiți explicit politica pentru excepțiile callback-urilor și garantați terminalizarea handle-ului într-un traseu care nu poate fi sărit. Adăugați teste RED pentru completare naturală, `Complete` și anulările `Revert`/`Complete` cu listener care aruncă. Nu ascundeți excepția fără a stabili contractul public de eroare.

### MOTION-AUD-002 — RIDICAT — `MotionSequence` poate rămâne permanent neterminat

**Comportament observat**

Au fost confirmate două trasee independente:

```text
PROBE sequence-precompleted
started=1; completed=False; canceled=False; completionDone=False

PROBE sequence-child-cancel
childCanceled=True; completed=False; canceled=False; completionDone=False
```

Primul apare când fabrica întoarce un handle deja terminal. Este un caz accesibil în utilizare normală, inclusiv când politica reduced-motion permite completare sincronă. Al doilea apare când pasul activ este anulat direct.

**Cauză confirmată**

`MotionSequence.Start` creează pasul (`UI/Motion/Core/MotionSequence.cs:25`) și se abonează ulterior la `Completed` (`:26`). Evenimentul unui `MotionHandle` deja terminal nu este reluat pentru abonați tardivi, iar secvența nu verifică starea după abonare. În handler, cazul `args.IsCanceled` doar revine (`:28`) fără să terminalizeze grupul.

**Impact**

Handle-ul returnat nu ajunge nici completat, nici anulat. Orice await, grup exterior sau logică de orchestrat bazată pe el poate rămâne blocată.

**Remediere recomandată**

Secvența trebuie să observe atomic și stările deja terminale. Politica pentru anularea/eroarea copilului trebuie decisă și exprimată în rezultat — de exemplu, terminalizarea grupului ca anulat — nu prin abandonarea sa. Sunt necesare teste pentru pas completat sincron, reduced motion, pas pre-anulat și anularea pasului activ.

### MOTION-AUD-003 — MEDIU — un `Dispose` invalid poate corupe persistent stiva de tranzacții

**Comportament observat**

După eliminarea intenționat greșită a scope-ului exterior înaintea celui interior, ordinea este corectată, dar scope-ul exterior nu mai poate fi eliminat:

```text
PROBE transaction-recovery
firstDispose=InvalidOperationException; remainingDepth=1
```

**Cauză confirmată**

`MotionTransactionScope.Dispose` setează `disposed = true` înainte de `context.Pop(Transaction)` (`UI/Motion/Transactions/MotionTransactionScope.cs:23-24`). Dacă `Pop` respinge ordinea sau thread-ul, scope-ul este marcat consumat deși tranzacția se află încă în stivă. Un al doilea `Dispose` nu mai încearcă recuperarea.

**Impact**

Contextul poate rămâne permanent cu o tranzacție activă. Mutații ulterioare pot moșteni implicit opțiuni de animație care nu le aparțin, fără o eroare locală care să explice legătura.

**Remediere recomandată**

Marcați scope-ul ca eliminat numai după un `Pop` reușit sau restaurați starea la eșec. Testați recuperarea după ordine greșită și după acces de pe thread greșit.

### MOTION-AUD-004 — MEDIU — snapshot-ul de diagnostic raportează contoare de cadru false

**Comportament observat**

```text
PROBE diagnostics-sampled
frameSampled=1; snapshotSampled=0; frameWrites=0; snapshotWrites=0

PROBE diagnostics-property-write
frameWrites=1; snapshotWrites=0; bindings=1
```

**Cauză confirmată**

`MotionDiagnostics.CreateSnapshot` setează necondiționat `ValuesSampledThisFrame: 0` și `PropertiesWrittenThisFrame: 0` (`UI/Motion/Diagnostics/MotionDiagnostics.cs:77-78`). În plus, denumirile `ActivePropertyBindings` și `ActiveLayoutMotions` sunt alimentate din numărul de binding-uri înregistrate/cached, care pot rămâne atașate după terminarea mișcării; ele nu reprezintă neapărat activitate în cadru.

Documentația canonică recunoaște explicit că implementarea curentă întoarce zero (`docs-site/documentation/classes/Cerneala.UI.Motion.Diagnostics.MotionDiagnostics.md:62`), dar ghidul de diagnostic spune că snapshot-urile raportează activitatea și arată valori nenule (`docs/motion-diagnostics.md:9,14`).

**Impact**

Instrumentarea nu poate fi folosită cu încredere pentru depanare, alerte sau măsurarea costului unui cadru. Zero poate însemna fie lipsă de activitate, fie pur și simplu lipsă de implementare.

**Remediere recomandată**

Păstrați rezultatul ultimului cadru în owner-ul potrivit sau furnizați-l explicit snapshot-ului. Separați în API „înregistrat/cached” de „activ”. Adăugați teste care compară snapshot-ul cu rezultatul cadrului după eșantionare, scriere de proprietate și terminarea unui binding.

### MOTION-AUD-005 — MEDIU — planul complet bifat și documentația de ansamblu supraestimează suprafața livrată

**Dovezi**

- `MotionTimelineRegistry` este încă o clasă goală (`UI/Motion/Core/MotionTimelineRegistry.cs:3-5`), deși planul cere explicit extinderea shell-ului în Phase 19 (`docs/superpowers/plans/2026-07-07-modern-motion-system.md:1296`). Documentația canonică recunoaște că nu există operații publice de registru.
- `element.Motion().States()` întoarce un `MotionStateBuilder` fără membri publici de înregistrare (`UI/Motion/MotionStateBuilder.cs:3-10`). Documentația canonică recunoaște acest lucru, în timp ce planul bifează scenarii de state/hover și prioritate (`:1009`, `:1202`). Există o cale separată de state motion în `AspectEngine`, dar builder-ul public expus rămâne inutilizabil pentru configurare.
- `MotionPriority` conține numai `Normal` (`UI/Motion/Core/MotionPriority.cs:5`). Opțiunea este transportată prin API, însă `MotionValue<T>` nu aplică o comparație de prioritate; `MotionConflictResolver` nu este conectat la traseul de producție inspectat. Totuși `docs/motion-system.md:16` afirmă că pornirile pe același canal folosesc prioritatea configurată.
- Planul bifează conversia cross-parent (`:1055`), iar testele conțin un scenariu cross-parent, însă `docs/motion-system.md:7` declară încă limitarea same-parent-only.
- Planul bifează o politică explicită pentru anularea/eșecul copiilor (`:1345`), contrazisă direct de defectul `MOTION-AUD-002`.

**Impact**

Planul nu mai poate fi tratat ca ledger verificat, iar cititorii primesc contracte diferite în funcție de documentul consultat. Shell-urile publice cresc suprafața API fără comportament utilizabil.

**Remediere recomandată**

Decideți intenția de produs înainte de cod: implementați capabilitățile declarate sau restrângeți contractul și eliminați/etichetați suprafețele premature. După decizie, sincronizați planul, `docs/motion-system.md` și documentația canonică. Nu este justificat să ghicim în audit ce comportament public este dorit.

### MOTION-AUD-006 — SCĂZUT — validarea timpului este inconsistentă

**Comportament observat**

```text
PROBE negative-max-delta
result=ArgumentOutOfRangeException; message=Delta cannot be negative. (Parameter 'delta')

PROBE repeat-negative-delta
result=no-exception; current=0
```

Setarea unui `MotionSystem.MaxDelta` negativ este acceptată (`UI/Motion/Core/MotionSystem.cs:76-82`), apoi un `Tick` valid e transformat într-o deltă negativă la `:125` și eșuează mai jos în sampler. În schimb, sampler-ele `RepeatSpec` și `PingPongSpec` adună direct delta (`UI/Motion/Specs/RepeatSpec.cs:74-76`, `PingPongSpec.cs:74-76`) și nu resping o valoare negativă, deși contractul generic spune că toate implementările concrete o resping.

**Impact**

Erorile apar departe de intrarea invalidă sau nu apar deloc, iar sampler-ele nu sunt substituibile după contractul lor comun.

**Remediere recomandată**

Validați `MaxDelta` în setter conform contractului decis (`>= 0` sau `> 0`) și aplicați aceeași regulă de deltă tuturor sampler-elor concrete. Acoperiți toate implementările printr-un test de contract parametrizat.

## Ce este solid

- Ownership-ul principal este root-based: `MotionSystem`, graful, proprietățile, layout-ul, presence și diagnostica sunt compuse la nivelul rădăcinii.
- Integrarea în cadru este reală: Motion rulează prin fazele `BeforeLayout`, `AfterLayout` și `BeforeRender`, nu printr-un scheduler paralel ascuns.
- Testele confirmă că proprietățile exclusiv vizuale nu cer layout, iar proprietățile de layout cer măsurare/aranjare unde este necesar.
- Există teste pentru layout cross-parent, cleanup la detach, lifecycle presence repetat, reduced motion, tranzacții, drag/gesture/scroll și integrarea markup/source generation.
- Nu am găsit `TODO`, `FIXME` sau `NotImplementedException` în sursele `UI/Motion`.

Aceste puncte sunt dovezi despre structură și cazurile testate; nu anulează defectele de terminalizare de mai sus.

## Verificare executată

| Verificare | Rezultat |
|---|---:|
| `dotnet restore .\Cerneala.slnx --nologo` | PASS |
| `Cerneala.Tests`, filtru `FullyQualifiedName~Motion` | 272 PASS, 0 FAIL, 0 SKIP |
| `Cerneala.Tests.SourceGen`, filtru Motion | 182 PASS, 0 FAIL, 0 SKIP |
| `Cerneala.Tests.Language`, filtru Motion | 52 PASS, 0 FAIL, 0 SKIP |
| Total teste Motion focalizate | **506 PASS** |
| Probă public-API pentru cele patru clase de defecte | defectele reproduse |
| `dotnet test .\Cerneala.slnx --no-restore --nologo` | **FAIL** |

Rezultatele vizibile ale suitei complete:

- `Cerneala.Tests`: 3.159 pass, 0 fail, 2 skip;
- `Cerneala.Tests.SourceGen`: 485 pass, 0 fail;
- `Cerneala.Tests.Language`: 181 pass, 1 fail — `FormattingTests.RangeAndOnTypeFormattingTouchOnlyTheSelectedLinesAndToleratePartialMarkup`, diferență CRLF/LF;
- `Cerneala.Tests.LanguageServer`: 38 pass, 2 fail — timeout la inițializarea protocolului și fixture care caută un executabil Release inexistent în rularea Debug;
- `Cerneala.Tests.SdlGpu`: 115 pass, 0 fail, 5 skip;
- `Cerneala.Tests.PreviewHost`: 12 pass;
- `Cerneala.Tetris.Tests`: 25 pass;
- comanda de soluție s-a oprit și pe `tests/Fixtures/VisualStudioIntegrationHost`, al cărui `obj/project.assets.json` lipsea după restore-ul soluției.

Aceste eșecuri sunt în trasee din afara Motion. Auditul nu are dovezi că sunt cauzate de Motion, dar poarta de repository completă **nu este verde** și nu trebuie prezentată ca atare. Cele două teste native de conformance omise din proiectul core și cele cinci omise din SdlGpu nu au fost executate.

## Limitări și incertitudini rămase

- Nu am făcut validare manuală de interacțiune umană și nu pretind că am făcut-o.
- Nu am capturat screenshot-uri și nu am rulat o matrice nouă de conformance vizuală. Transformările complexe, rotațiile și tranzițiile cross-parent nu sunt validate vizual de acest audit.
- Nu am rulat benchmark-uri noi; nu există afirmații de performanță sau zero-alocări în acest raport.
- Nu am forțat testele native omise și nu am reparat infrastructura suitei complete.
- Siguranța concurentă a abonării/dezabonării listenerilor `MotionValue<T>` rămâne neverificată; traseul folosește o listă mutabilă, iar contractul exact de thread ownership trebuie stabilit înainte de a-l numi defect.

## Ordine recomandată

1. Reparați și testați terminalizarea atomică a `MotionValue<T>` (`MOTION-AUD-001`).
2. Decideți politica de anulare/eșec și reparați `MotionSequence` (`MOTION-AUD-002`).
3. Reparați recuperarea `MotionTransactionScope` (`MOTION-AUD-003`).
4. Faceți snapshot-urile diagnostice adevărate sau restrângeți explicit contractul (`MOTION-AUD-004`).
5. Luați o decizie de produs pentru priority, state builder și timeline registry, apoi sincronizați planul și documentația (`MOTION-AUD-005`).
6. Uniformizați validarea deltelor și adăugați testul de contract (`MOTION-AUD-006`).
7. Rulați din nou testele focalizate, reproducătorii originali, proiectele afectate și poarta completă a repository-ului.
