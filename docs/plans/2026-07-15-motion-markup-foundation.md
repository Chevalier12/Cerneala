# Plan: foundation pentru Motion markup

> Data: 2026-07-15
> Status: finalizat
> Dependenta: niciuna
> Scop: livram prima verticala utilizabila cu specs Tween/Spring, Aspect inline/named, `@when`, `@if`, `@on`, `@animate`, `@from` si `@to`, complet source-generated si cu lifecycle corect.

## 1. Baseline si problema actuala

`UiMarkupDirectiveParser.cs` parseaza astazi `@when`, `@if`, `@default` si `@template` intr-un AST intern. `UiMarkupReactiveEmitter` rezolva surse tipizate si `GeneratedMarkup.AttachConditions` leaga un controller de `IElementLifecycleBehavior`. `UiMarkupGenerator` cunoaste deja resources `Aspect`, dar acestea contin assignments/template, nu comportament Motion.

Motion expune `MotionAnimationBuilder<T>`, `MotionPropertyBinding<T>` si specs tipizate. Builder-ul public seteaza implicit `HoldOnComplete=true`, dar nu expune toate `MotionPropertyStartOptions`; markup-ul nu trebuie sa ocoleasca aceasta lipsa prin acces la membri internal.

## 2. Arhitectura propusa

- Extindem parserul existent printr-un AST Motion separat, nu prin concatenare de stringuri C#.
- Separarea recomandata este `UiMarkupMotionParser`, `UiMarkupMotionResolver` si `UiMarkupMotionEmitter` ca partiale ale generatorului; numele sunt estimative, responsabilitatile nu.
- Adaugam un bridge runtime generat in `UI/Markup/GeneratedMarkupMotion.cs`: un behavior per element/Aspect creeaza o sesiune la attach, detaseaza events/observations si anuleaza executions la detach.
- Resursele `Tween` si `Spring` sunt declaratii generator-known. La fiecare utilizare, resolverul construieste `MotionSpec<T>` pentru tipul proprietatii; nu apare un spec netipizat runtime.
- Named Aspects sunt expandate si validate la fiecare application site. Compatibilitatea foloseste assignability (`elementType` deriva din `TargetType`), nu egalitate de nume.
- `$Name.Property` este rezolvat static fata de elementele numite vizibile la application site; o aplicare care nu poate satisface toate targets primeste diagnostic.

## 3. Non-obiective

- Fara `MotionClip`, composition, keyframes, handles, Presence, Layout, Scroll, Drag sau Gesture in aceasta verticala.
- Fara `$event`, reflection, method interception, arbitrary C# expressions sau runtime parsing.
- Fara schimbarea semanticii existente pentru assignments si templates din `Aspect`.

## 4. Fisiere estimate

- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`
- partiale noi sub `Cerneala.SourceGen/` pentru AST, rezolvare si emitere Motion
- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `UI/Markup/GeneratedMarkupMotion.cs`
- `UI/Motion/MotionAnimationBuilder.cs` numai daca este necesar un overload public pentru `MotionPropertyStartOptions`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorMotionTests.cs`
- `tests/Cerneala.Tests/UI/Markup/GeneratedMarkupMotionTests.cs`
- paginile API afectate din `docs-site/documentation/classes/`

## 5. Etape de implementare

### Etapa 0 - RED si contract public minim

- [x] Adauga teste sourcegen RED pentru resources `Tween`/`Spring`, Aspect named si inline, un `@animate` cu `@from`/`@to`, `current`, spec implicit si spec per proprietate.
- [x] Adauga diagnostics RED pentru `@to` lipsa, proprietate inexistenta, tip incompatibil, mixer absent, `@from` fara pereche in `@to`, resource necunoscut si directive Motion in context ilegal.
- [x] Adauga teste RED pentru `retarget`, `holdOnComplete` si `debugName`; confirma printr-un test de API ca generatorul nu are nevoie de membri internal.
- [x] Stabileste overload-ul public minim al `MotionAnimationBuilder<T>` sau bridge-ul public `GeneratedMarkup` care transmite `MotionPropertyStartOptions`, fara a duplica logica din `MotionPropertyBinding<T>`.
- [x] Actualizeaza API docs pentru orice membru public introdus si ruleaza public API diff review.
- [x] Reindexeaza solutia dupa modificarile C#.

**Gate etapa 0**

- [x] Testele noi esueaza din motivele comportamentale asteptate, iar contractul ales poate reprezenta toate cele trei options reale.
- [x] Nu a fost introdus un model runtime generic de AST sau un spec netipizat.

### Etapa 1 - Gramatica si AST Motion

- [x] Extinde `DirectiveCursor` astfel incat sa recunoasca directivele Motion numai in corpurile Aspect permise, pastrand `@when`/`@if` existente compatibile.
- [x] Modeleaza explicit `@animate`, `@from`, `@to`, assignments targetate, `with`, options si source locations.
- [x] Parseaza valorile Motion printr-o gramatica limitata: literals tipizabile, `current`, sursele reactive deja suportate, resource references si conditional expression folosita de proposal; nu emite direct text arbitrar ca C#.
- [x] Parseaza duratele `ms`/`s`, easing names si constructorii inline `Tween(...)`/`Spring(...)` cu diagnostics la tokenul gresit.
- [x] Pastreaza XML controls interzise in execution bodies si pastreaza directivele non-Motion neschimbate.
- [x] Adauga teste de recovery pentru acolade, semicolon, quote si directive necunoscute.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] AST-ul separa syntax, semantic type si emitted code; niciun test nu depinde de matching fragil pe corpuri brute.
- [x] Toate testele parserului si testele vechi `@when`/`@if` sunt GREEN.

### Etapa 2 - Rezolvare semantica statica

- [x] Rezolva `TargetType` inclusiv custom controls si valideaza aplicarea prin assignability.
- [x] Rezolva unqualified properties pe target si `$Name.Property` per application site, inclusiv forward references in namescope-ul aplicatiei.
- [x] Infer type-ul `T`, verifica `UiProperty<T>`, accesibilitatea, read-only si interpolatorul/mixerul compatibil.
- [x] Specializeaza fiecare resource Tween/Spring la `MotionSpec<T>` la locul utilizarii si deduplica doar constructiile identice din acelasi generated scope.
- [x] Rezolva `current` la valoarea vizuala Motion curenta, nu la o citire stale a base value.
- [x] Valideaza ca fiecare proprietate din `@from` exista in `@to`; `@from` omis porneste din valoarea vizuala curenta.
- [x] Valideaza options exact la `Restart|PreserveProgress`, Boolean si string; respinge `conflict`, channel si reduced-motion options inventate.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Diagnostics diferentiaza type missing, property missing, wrong target, wrong value type, missing mixer si invalid spec type.
- [x] Nicio rezolvare runtime dupa nume nu apare in codul generat.

### Etapa 3 - Activare prin stare si events

- [x] Refoloseste resolverul de observations pentru `@when` si `@if`, astfel incat toate dependentele sa ramana observate si reevaluarea sa respecte short-circuit-ul existent.
- [x] Defineste activarea: fiecare trecere relevanta a ramurii porneste execution-ul declarat; reevaluarea fara schimbare nu reporneste animatia.
- [x] Rezolva `@on EventName` la `IEventSymbol` pe `TargetType`/base types si genereaza abonare/dezabonare directa `+=`/`-=`, inclusiv pentru wrappers de routed events.
- [x] Emite diagnostic daca event-ul lipseste, este inaccesibil sau membrul omonim nu este event; nu cauta si nu modifica metode.
- [x] Ignora event args in limbaj, conform deciziei fara `$event`.
- [x] Adauga teste cu event built-in, custom CLR event, custom routed event, event mostenit si `TargetType` prea general.
- [x] Adauga teste attach/detach/reattach care numara invocarile si demonstreaza o singura subscription activa.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Custom events functioneaza fara reflection si fara method injection.
- [x] Dupa detach, event-ul si observations nu mai pot porni Motion.

### Etapa 4 - Emitere si lifecycle

- [x] Implementeaza behavior-ul runtime per Aspect cu sesiune noua la attach si cleanup idempotent la detach/dispose.
- [x] Emite pornirea proprietatilor dintr-un `@animate` in paralel si grupeaza handles pentru cancellation la detach, fara sa schimbe semantica publicului `MotionGroupHandle`.
- [x] Aplica `@from` prin calea Motion corecta, apoi porneste `@to` cu spec si options; evita flash-ul intermediar intre writes.
- [x] Asigura ordinea: resources si Presence/Layout-independent properties sunt configurate inainte de attach, iar animations cer root Motion numai dupa attach.
- [x] Adauga teste cu doua instante ale aceluiasi Aspect pentru a demonstra sessions si handles independente.
- [x] Adauga test de replacement/detach in timpul animatiei si confirma zero active handles dupa cleanup.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Verticala hover/event din proposal ruleaza complet din markup.
- [x] Nu exista subscriptions sau graph nodes ramase dupa 100 cicluri attach/detach.

## 6. Verificare

- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Ruleaza testele targetate `GeneratedMarkupMotionTests` din `Cerneala.Tests`.
- [x] Inspecteaza codul generat pentru event wiring, spec specialization si absenta reflection.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`.
- [x] Ruleaza `git diff --check` si reindexarea finala.

## 7. Definitia de gata

- [x] Named si inline Aspects pot porni tween/spring animations prin stare sau events.
- [x] `@from`, `@to`, `current`, target properties si options sunt tipizate si diagnosticate la build.
- [x] Custom events sunt abonate direct si curatate determinist.
- [x] API docs si documentatia conceptuala descriu exact verticala livrata.
