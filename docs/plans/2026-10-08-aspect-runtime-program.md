# Plan: programul unui Aspect devine comportament per aplicare

> Data: 2026-10-08
> Status: finalizat
> Baseline: commit `59c0e257` (redenumirea Sound → Timbre).
> Dependent: [Timbre și Prism în Aspect](2026-10-08-timbre-prism-in-aspect.md) pornește numai după gate-ul final al acestui plan.
> Scop: când un element primește un Aspect, inclusiv prin înlocuire la runtime, Aspect-ul aduce tot programul lui (`@on`, Motion, Presence/Layout/Scroll/Drag/GesturePress, acțiunile Timbre existente); când Aspect-ul pleacă, tot ce a adus dispare.

## 1. Rezumat

Regula aprobată pe 2026-10-08: **„Dacă Aspect-ul este înlocuit, tot ce a adus cu el trebuie să dispară, iar noul Aspect aplică din nou tot ce are.”** Prima jumătate funcționează azi. A doua nu funcționează pentru niciun sistem, pentru că SourceGen cablează programul fiecărui Aspect la compilare, per pereche (element, Aspect static). Planul mută acest program în comportamentul `ElementAspect`, care există deja și are exact durata de viață dorită.

## 2. Baseline (fapte observate)

### 2.1 Experiment (2026-10-08, test temporar șters după rulare)

Fixture `MarkupTimbreFixture` (`tests/Cerneala.Tests.Timbre/Markup`), resurse `Aspect A` (`@on Click { @timbre $Tone; }`) și `Aspect B` (`@on Click { @timbre $Other; @animate with Tween(10ms, Linear) { @to { Opacity = 0.25; } } }`), `TargetType="Button"`:

| Scenariu | Sunet după click | `Opacity` după click + 100 ms |
|---|---|---|
| Control: buton cu `Aspect="$B"` static | pornește `audio/other.wav` | 0.25 |
| Buton cu `Aspect="$A"`; `button.Aspect = B` (găsit cu `TryFindResource(new ResourceId<ElementAspect>("B"))`); click | nimic | 1 |

Concluzie, în condițiile testate: un Aspect atribuit la runtime nu aduce nici `@on`, nici Motion, nici Timbre; programul lui A este retras corect. Nu s-a testat separat dacă valorile/condițiile lui B se aplică (calea lor trece prin `behaviorFactory`, vezi 2.2).

### 2.2 Cod

- `UI/Aspect/ElementAspect.cs`: constructorul primește `Func<UIElement, IDisposable?>? behaviorFactory`; `AttachBehavior(element)` îl invocă.
- `UI/Elements/UIElement.cs` (`AttachElementAspectBehavior`/`DetachElementAspectBehavior`, apelate la attach/detach ~685/~712) și `UIElement.Events.cs` (`OnPropertyChanged` pe `AspectProperty`, ~179–197): comportamentul trăiește cât timp elementul este atașat **și** are acel Aspect; la înlocuire se dispune vechiul și se atașează noul.
- `Cerneala.SourceGen/UiMarkupAspectEmitter.cs`:
  - `PrepareAspectBehavior(targetType, aspect, includeMotion)` emite deja în `behaviorFactory` planul reactiv (valori/condiții) față de un `target` generic; cu `includeMotion: true` emite și `EmitMotionPresence/Layout/Activations` pe `target`.
  - `includeMotion: true` se folosește numai pentru `AspectPackage` când `SupportsPackageBehavior` (fără `@on`, Presence, Layout, Scroll, Drag, GesturePress, Motion/Timbre în condiții).
  - Aspect-urile resursă (`EmitNamedElementAspectResource`) și inline/locale (`EmitLocalAspect`) folosesc `includeMotion: false`; Motion, Presence, Layout, `@on` și Timbre se emit **per element** în bucla de aplicare (`EmitMotionActivations(..., bindToElementAspect: true)`, `EmitTimbreActivations(...)`, ~482–525), legate de Aspect-ul capturat la construcție.
- Sesiunile aspect-scoped (`GeneratedMarkupTimbre.MarkupTimbreSession`, sesiunea Motion echivalentă) observă `AspectProperty` și se retrag la înlocuire — de aceea „dispare” funcționează și „reaplică” nu.

## 3. Decizii aprobate

| Decizie | Valoare |
|---|---|
| Ordinea | Acest plan este fundația; planul Timbre/Prism vine după. |
| Referințe `$Name` | Se rezolvă la compilare în namescope-ul unde este **scris** Aspect-ul: pentru inline, namescope-ul elementului declarant; pentru resursă, namescope-ul unde e declarată resursa. Fără lookup la runtime. |
| Resurse fără namescope cu elemente (de ex. `App.crn`) | Doar `$self` și `$owner`; `$Name` este eroare de compilare (diagnosticul existent „not available in this namescope” sau unul echivalent). |
| `$owner` | Ownerul de template al elementului **țintă**, rezolvat la runtime printr-o legătură internă element → owner înregistrată când un template își creează elementele (decizia 2A, 2026-10-08). Nu e căutare după nume. Azi un Aspect resursă îl folosește deja când e aplicat în template (`TemplateChildMotion` în `UiMarkupGeneratorMotionTests`), rezolvat la compilare per aplicare. |
| `@presence` și `@layout` | Se aplică și pe element deja atașat (decizia 1A, 2026-10-08): protecția „must be applied before the element is attached” din codul generat dispare; la înlocuire animația de intrare nu se reia, ieșirea și layout-ul viitor folosesc setările noi. |
| Aspect aplicat pe alt element | Permis pentru orice Aspect, inclusiv inline (`extra.Aspect = PauseButton.Aspect;`): fiecare element primește propria aplicare, iar `$Name` rămâne legat de namescope-ul de declarare. |
| Înlocuire | Behavior-ul vechi este dispus complet (execuții Motion, handler-e, sesiuni, sunete); noul Aspect își atașează programul ca la o aplicare nouă. |
| `$owner.parts`, `$owner.prism`, `$self.prism` într-un Aspect resursă | Tipul se deduce din locurile din markup unde e aplicat Aspect-ul; un loc fără parte/compoziție, tipuri diferite sau un Aspect nefolosit în markup dau eroare de compilare; aplicat la runtime pe alt element, aruncă excepție dacă partea lipsește, are alt tip sau Prism-ul e altă compoziție (decizia din 2026-10-08). |

## 4. Contract țintă

- Pentru **orice** Aspect (inline, resursă locală, resursă de aplicație, package), tot programul se emite o singură dată în `behaviorFactory(target)`: plan reactiv cu valori **și** activări Motion/Timbre, `@on`, Presence, Layout, Scroll, Drag, GesturePress, `@handle`/`@run`/`@cancel` Motion, acțiunile Timbre actuale.
- Emisia per element a acestor părți (bucla din `UiMarkupAspectEmitter` ~470–525, `bindToElementAspect: true`) dispare; elementul doar atribuie Aspect-ul.
- `$Name` este capturat de `behaviorFactory` din variabila elementului generat în namescope-ul de declarare (resursele sunt create per instanță a documentului, deci fiecare instanță își capturează propriile elemente; Etapa 0 verifică resursele declarate în template-uri).
- Un Aspect atribuit din C# altui element (inclusiv inline) funcționează: comportamentul se aplică pe `target`, referințele `$Name` rămân cele de la declarare.
- `@presence`/`@layout` sunt setate de behavior; pe un element neatașat încă (aplicare statică) efectul e identic cu azi, pentru că behavior-ul rulează la primul attach înainte de intrarea Presence — de verificat în Etapa 0; dacă intrarea nu se mai joacă la aplicarea statică, oprește-te.
- `$owner` dintr-un Aspect resursă folosește legătura element → owner de template; referințele `$Name` care apar în resursă după declararea ei (forward reference, de ex. `ForwardNamedMotion` → `$Child`) se capturează fără a folosi variabile încă nedeclarate.
- Semantica observabilă a scenariilor statice actuale (Aspect static, fără înlocuire) nu se schimbă: aceleași valori, ordine sursă, Motion, Timbre, hidden/collapsed, template retirement, Live Preview.

## 5. Non-obiective

- Sintaxa nouă Timbre/Prism (planul dependent).
- Lookup de nume la runtime pentru `$Name` (respins prin decizia din §3).
- Schimbări de precedence/layer în `AspectEngine` sau în `AspectPackage` dincolo de ce cere mutarea programului.
- Optimizări speculative ale costului de aplicare; doar gate-urile de regresie din Etapa 4.

## 6. Fișiere estimate

- **Modificate:** `Cerneala.SourceGen/UiMarkupAspectEmitter.cs`, `UiMarkupAspectReader.cs`, `UiMarkupMotionActivationEmitter.cs`, `UiMarkupMotionResolver.cs` (rezolvarea `$self`/`$owner`/`$Name` pentru `target`), `UiMarkupReactiveEmitter.cs`, `UiMarkupTimbreEmitter.cs` (sesiune legată de behavior, nu de Aspect capturat), `UiMarkupTimbreMotionEmitter.cs`; `UI/Markup/GeneratedMarkupTimbre.cs` și helper-ele Motion aspect-scoped din `UI/Markup/GeneratedMarkupMotion*.cs` (inventar complet în Etapa 0); `Cerneala.Language/Semantics/*Motion*.cs`/`*Timbre*.cs` pentru rezolvarea `$Name` în namescope-ul de declarare și diagnosticul din `App.crn`.
- **Teste:** `tests/Cerneala.Tests.SourceGen` (Aspect/Motion/Timbre generator), `tests/Cerneala.Tests` (Aspect/Motion runtime), `tests/Cerneala.Tests.Timbre/Markup/TimbreAspectLifecycleTests.cs`, corpus Language.
- **Docs:** `docs/CernealaMarkupGuide.md` (secțiunile Aspect/Motion: aplicare, înlocuire, unde se rezolvă `$Name`), `docs/timbre-guide.md` (înlocuire), paginile docs-site ale helper-elor `GeneratedMarkup` schimbate.

## 7. Etape de implementare

### Etapa 0 — Inventar, RED și decizii rămase

- [x] Citește complet `UiMarkupAspectEmitter.cs`, `UiMarkupAspectReader.cs`, `UiMarkupMotionActivationEmitter.cs` și helper-ele `GeneratedMarkup` aspect-scoped; scrie tabelul callerilor pentru fiecare emisie per element (Motion, Presence, Layout, Scroll, Drag, GesturePress, `@on`, Timbre) și migrarea ei în behavior.
- [x] Inventariază Aspect-urile resursă care folosesc `$Name` (teste, corpus, fixture-uri, Playground, smoke, docs) și verifică pentru fiecare că numele există în namescope-ul de declarare; resursele din `App.crn` cu `$Name` devin eroare.
- [x] Verifică unde se creează resursele declarate în template-uri (per instanță de template sau nu) și ce element capturează `$Name` acolo; dacă ar cere lookup la runtime, oprește-te și cere aprobare.
- [x] Verifică ordinea la primul attach: `UIElement` atașează behavior-ul Aspect-ului (`AttachElementAspectBehavior`, ~685) înainte sau după ce `PresenceCoordinator` decide intrarea; caracterizează cu un test runtime că un `@presence` aplicat static își joacă azi intrarea și că, setat din behavior la attach, ar juca-o la fel. Dacă nu, oprește-te.
- [x] Inventariază toate locurile unde un template își creează elementele (`ComponentTemplateContext`, codul generat pentru `@template`, template-uri C#) — punctele unde se înregistrează legătura element → owner.
- [x] Test: Aspect inline cu `$Speaker` atribuit din C# altui buton; ambele butoane comandă același Speaker.
- [x] Transformă experimentul din §2.1 în test permanent RED în `tests/Cerneala.Tests.Timbre/Markup/TimbreAspectLifecycleTests.cs` (sunet după swap A→B) și în același harness (`MarkupTimbreFixture`, singurul care rulează markup generat; `Cerneala.Tests` nu are unul) pentru Motion (Opacity după swap); confirmă că eșuează exact pe lipsa programului lui B, cu controlul static GREEN.
- [x] Adaugă RED pentru: swap B→A→B (fiecare aplicare are programul ei, fără dubluri de handler-e), același Aspect resursă pe două elemente, detach/reattach cu Aspect neschimbat (programul se reatașează o singură dată), Aspect package cu `@on`.
- [x] Rulează și salvează baseline-ul GREEN: `Cerneala.Tests`, `Cerneala.Tests.SourceGen`, `Cerneala.Tests.Language`, `Cerneala.Tests.Timbre`, `Cerneala.Tests.PreviewHost` în `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage0/`.

**Gate etapa 0**

- [x] Tabelul callerilor acoperă fiecare emisie per element; RED-urile eșuează pe comportament; captura `$Name` în resursele din template-uri este stabilită. Evidence: `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage0/README.md` (RED-ul cu Aspect inline copiat eșuează azi la generator pe regula schimbată în Etapa 1).

### Etapa 1 — Language: `$Name` rezolvat în namescope-ul de declarare

- [x] Azi `FindMotionNamedElement` acceptă `$Name` doar printre descendenții elementului de aplicare; regula nouă (namescope-ul de declarare, inclusiv frați ca `$Speaker`) o extinde, iar cazurile existente rezolvă același element. Fă GREEN testul `InlineAspectAssignedToAnotherElementKeepsItsNamedReferences` pe partea de generator.
- [x] Language și SourceGen rezolvă `$Name` dintr-un Aspect resursă în namescope-ul unde e declarată resursa (nu în cel al elementului de aplicare); resursele fără namescope cu elemente (`App.crn`) primesc diagnostic pentru `$Name`, cu mesaj care indică `$self`/`$owner`.
- [x] Migrează sau corectează cazurile găsite în inventarul din Etapa 0. (Nu a fost necesar: toate cazurile rezolvă același element.)

**Gate etapa 1**

- [x] `Cerneala.Tests.Language` GREEN cu corpusul nou; aceleași diagnostice în SourceGen pentru cazurile din corpus. Evidence: `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage1/README.md`.

### Etapa 2 — SourceGen: tot programul în `behaviorFactory`

- [x] Extinde `PrepareAspectBehavior` la program complet pentru toate felurile de Aspect (elimină dependența de `SupportsPackageBehavior` pentru Motion/`@on`); `target` este singurul element implicit, `$Name` din Aspect inline este capturat din variabila elementului generat.
- [x] Elimină emisia per element din bucla de aplicare; elementul primește doar `Aspect = ...`/`AttachResource(...)`.
- [x] Sesiunile Motion și Timbre devin lifetime-uri returnate de behavior (în `CombineLifetimes`), nu observatori de `AspectProperty`; păstrează ordinea sursă Timbre→Motion și politica hidden/collapsed.
- [x] Template-uri și paired roots: behavior-ul se aplică la fiecare element din template; retirement-ul template-ului dispune behavior-ul prin detach.
- [x] Legătura internă element → owner de template (`ConditionalWeakTable` sau câmp intern pe `UIElement`), scrisă la crearea elementelor de template și ștearsă la retirement; teste: owner corect în template-uri imbricate, după retirement nu mai există legătura.
- [x] `$owner` dintr-un Aspect resursă se emite prin legătura element → owner (nu prin contextul template de la compilare); `@presence`/`@layout` se emit în behavior fără protecția „before the element is attached”; actualizează `UiMarkupGeneratorMotionPresenceTests`/`UiMarkupGeneratorMotionLayoutTests` care așteaptă protecția.

**Gate etapa 2**

- [x] RED-urile din Etapa 0 sunt GREEN; `Cerneala.Tests.SourceGen` și `Cerneala.Tests.Timbre` GREEN; nicio emisie per element pentru Motion/`@on`/Timbre nu mai există (verificat pe textul generat al fixture-urilor reprezentative și prin simbolurile apelurilor). Evidence: `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage2/README.md` (suita completă GREEN; include reparațiile de runtime găsite: activare dublă a condițiilor, behavior-urile package atașate la attach).

### Etapa 3 — Runtime și API public

- [x] Teste runtime pentru `@presence`/`@layout` aduse de un Aspect aplicat la runtime: setările noi se aplică la ieșire/layout, fără excepție.
- [x] Simplifică helper-ele `GeneratedMarkup` aspect-scoped (de ex. `AttachTimbreSession(owner, aspect)`) la forma cerută de behavior; fixează diff-ul ApiCompat așteptat față de `59c0e257`.
- [x] Teste runtime: înlocuire în timpul unei animații/redări (vechiul program anulat sincron), reentrancy în handler-ul care schimbă Aspect-ul, idle frame fără muncă după swap.

**Gate etapa 3**

- [x] `Cerneala.Tests` și `Cerneala.Tests.Timbre` GREEN; testul idle-frame arată zero invalidări/execuții Motion după stabilizarea swap-ului. Evidence: `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage3/README.md`.

### Etapa 4 — Documentație și verificare completă

- [x] Actualizează ghidul markup (aplicare și înlocuire Aspect, unde se rezolvă `$Name`) și ghidul Timbre (secțiunea despre înlocuire); paginile docs-site ale helper-elor schimbate, cu `writing-api-documentation`.
- [x] ApiCompat strict față de `59c0e257`, după modelul `docs/plans/evidence/2026-10-03-timbre-motion-stage3/api-compat.proj`; clasifică fiecare diferență.
- [x] Regresie cost: aplicarea Aspect-urilor la construcția unui panou mare existent (contoarele `AspectEngineCounters`/Detective) — numărul de behavior-uri create și execuții Motion la idle nu crește față de baseline pentru scenariile statice.
- [x] `dotnet build .\Cerneala.slnx -c Release -m:1`; `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1`, `CERNEALA_TIMBRE_AUDIO_DEVICE=1`; smoke Windows `tests/Cerneala.SdlGpuSmoke --mode timbre-markup`/`timbre-motion`.

**Gate etapa 4**

- [x] Suita completă GREEN (skip-uri doar preexistente), smoke-uri exit 0, ApiCompat clasificat, ghidurile descriu doar ce e implementat. Evidence: `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage4/README.md` (include regresia de cost găsită și reparată în `UiMarkupReactiveEmitter`).

## 8. Condiții de oprire

- Dacă `$owner` într-un Aspect resursă cere lookup nou la runtime.
- Dacă mutarea în behavior schimbă semantica observabilă a unui scenariu static existent fără ca un test să o justifice.
- Dacă emisia per element nu poate fi eliminată complet pentru un fel de Aspect: nu se lasă două căi paralele.

## 9. Definiția de gata

- Înlocuirea Aspect-ului la runtime retrage tot programul vechi și aplică tot programul nou (sunet, Motion, `@on`, Presence/Layout etc.), dovedit de testele din Etapa 0.
- `$Name` se rezolvă în namescope-ul de declarare pentru orice Aspect; resursele fără namescope cu elemente (`App.crn`) au doar `$self`/`$owner`; orice Aspect se poate aplica pe alt element.
- Nu mai există emisie per element pentru programul Aspect; o singură cale prin `behaviorFactory`.
- Suita completă, smoke-urile Windows și ApiCompat sunt verificate cu evidence în `docs/plans/evidence/2026-10-08-aspect-runtime-program-stage*/`.
