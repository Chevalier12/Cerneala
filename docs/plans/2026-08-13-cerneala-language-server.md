# Plan: Cerneala Language Server si IntelliSense complet

> Data: 2026-08-13
> Status: finalizat
> Dependenta: `docs/plans/2026-08-13-cerneala-language-core.md`
> Scop: construim un language server LSP host-agnostic care transforma syntax tree-ul si semantic modelul comun in IntelliSense complet, rapid si determinist.

## 1. Baseline si contract

Repo-ul nu contine astazi un language server. Source generatorul primeste `Compilation` si `AdditionalFiles` numai in timpul build-ului, iar Visual Studio nu are un proces care sa mentina document snapshots, sa mapeze `.crn` la proiect ori sa raspunda la cereri editoriale.

Serverul trebuie sa lucreze pe bufferul nesalvat, sa incarce contextul C# al proiectului, sa anuleze cererile stale si sa nu ruleze generatorul complet la fiecare tasta. Orice rezultat semantic vine din `Cerneala.Language`; serverul traduce doar catre LSP.

## 2. Capabilitati obligatorii

- Diagnostics push/pull cu paritate sourcegen si recovery in timpul tastarii.
- Completion si completion resolve pentru structura XML, simboluri C#, bindings, resources, templates, Aspect, Motion si Prism.
- Hover, signature help, go-to-definition, references, document highlight si rename sigur.
- Document symbols, workspace symbols pentru simbolurile Cerneala, folding ranges si selection ranges.
- Semantic tokens pentru XML Cerneala si limbajele embedded; TextMate ramane fallback instant, nu sursa semantica.
- Full/range formatting, on-type formatting si code actions deterministe.

## 3. Arhitectura tinta

- Proiect executabil `Cerneala.LanguageServer` cu transport stream-based si lifecycle controlat de host.
- Proiect `Cerneala.LanguageServer.ProtocolTests` care porneste serverul real prin JSON-RPC/LSP, nu cheama direct handlers in testele end-to-end.
- Workspace service care mapeaza document -> project -> Roslyn `Compilation`, urmareste reload-uri si pastreaza snapshots versionate.
- Feature handlers subtiri peste servicii editor-agnostic din `Cerneala.Language`.
- Scheduler cu cancellation, coalescing si latest-document-version wins.

## 4. Fisiere estimate

- `Cerneala.LanguageServer/Cerneala.LanguageServer.csproj`
- `Cerneala.LanguageServer/Protocol/`
- `Cerneala.LanguageServer/Workspace/`
- `Cerneala.LanguageServer/Features/`
- `Cerneala.LanguageServer/Program.cs`
- `tests/Cerneala.Tests.LanguageServer/`
- `tests/Fixtures/LanguageServerWorkspace/`
- `Cerneala.slnx`
- API docs din `docs-site/documentation/classes/` numai pentru suprafata publica inevitabila

## 5. Etape de implementare

### Etapa 0 - Contract LSP si harness RED

- [x] Selecteaza o implementare LSP mentinuta si compatibila cu runtime-ul livrat; documenteaza protocol version, framing, serialization si politica de upgrade.
- [x] Adauga serverul si proiectul de protocol tests in solutie, cu transport in-memory pentru teste si stdio/duplex stream pentru host.
- [x] Adauga un test RED care initializeaza serverul, deschide un `.crn`, aplica `didChange` incremental si cere diagnostics/completion.
- [x] Defineste capabilities declarate exact; serverul nu anunta un feature pana cand testul protocol-level al feature-ului este GREEN.
- [x] Defineste logging structurat, trace levels si crash reports fara continut de document implicit.
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] Procesul server porneste, negociaza initialize/shutdown/exit si poate fi terminat fara process leak.
- [x] Testul functional este RED pentru lipsa feature handlers, nu pentru transport ori fixture.

### Etapa 1 - Workspace, proiecte si sincronizare documente

- [x] Mapeaza URI-urile `.crn` la proiectele care le includ ca `AdditionalFiles`, inclusiv `.slnx`, `.sln`, project references si linked files.
- [x] Construieste Roslyn compilations pentru proiectul owner si actualizeaza contextul la schimbari `.cs`, `.csproj`, references, configuration sau target framework.
- [x] Defineste politica multi-target: selecteaza contextul activ oferit de host si deduplica rezultatele identice; nu amesteca simboluri incompatibile intre TFMs.
- [x] Mentine overlay pentru bufferul nesalvat fara sa scrie pe disk si aplica numai schimbari cu versiune mai noua.
- [x] Anuleaza parse/bind/feature requests pentru versiuni stale si publica rezultate numai daca document version mai este curenta.
- [x] Gestioneaza fisiere standalone cu syntax-only support si un diagnostic informational unic privind lipsa proiectului semantic.
- [x] Adauga teste pentru project reload, rename/delete, document in doua proiecte, broken C# compilation si server restart.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Bufferul nesalvat si source generatorul salvat folosesc acelasi semantic context dupa save/build.
- [x] Nicio cerere stale nu poate suprascrie diagnostics ori completion pentru o versiune noua.

### Etapa 2 - Diagnostics fara erori false

- [x] Implementeaza diagnostics pe syntax si semantics folosind catalogul comun, cu mapping exact UTF-16 line/column.
- [x] Publica diagnostics strict pentru documentul/version analizat si retrage diagnostics disparute dupa reparatie.
- [x] Suprima diagnostics semantice dependente sub syntax nodes incomplete si limiteaza duplicatele la aceeasi cauza/span.
- [x] Dedupeaza diagnostics LSP fata de diagnostics build/sourcegen din Error List dupa id, document si span, fara sa ascunda erori distincte.
- [x] Adauga golden tests pentru toate `CERNEALAUI*`, Motion si Prism si compara rezultatul LSP cu source generatorul.
- [x] Adauga scenarii de tastare caracter-cu-caracter pentru deschidere/inchidere tag, attribute, binding, directive si template.
- [x] Ruleaza corpusul repo si cere zero diagnostics editor pentru fiecare document care compileaza valid.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Paritatea id/severity/message/span este exacta pentru erorile semantice stabile.
- [x] `CernealaPresentation` si Playground au zero diagnostics false in editor.

### Etapa 3 - Completion, resolve si signature help

- [x] Completeaza root elements si child elements permise de parent/content property, cu tipurile custom accesibile prin namespace aliases.
- [x] Completeaza attributes, property elements, attached properties si events, eliminand membrii deja folositi unde contractul nu permite duplicate.
- [x] Completeaza valori booleene, numerice, enum, colors/brushes, thickness, cursor, alignment si celelalte conversii recunoscute de build.
- [x] Completeaza `xmlns` aliases, CLR namespaces, `DataType`, `TargetType`, template `DataType` si tipuri assignable valide.
- [x] Completeaza resources vizibile dupa scope, element names, Aspect names, Motion specs/clips, Prism symbols si parameters.
- [x] Completeaza binding sources si fiecare segment dupa tipul rezultat, inclusiv dupa schimbari locale de `DataContext`, cu modes numai unde sunt legale.
- [x] Completeaza directive keywords, blocks, argument names si valori Motion/Prism pe baza contextului sintactic exact.
- [x] Implementeaza completion resolve cu signature, declaring type, XML documentation, deprecation si source assembly fara a incarca totul upfront.
- [x] Implementeaza signature help pentru directives/functions/specs/filter parameters cu active parameter corect dupa edits incomplete.
- [x] Adauga tests negative care demonstreaza ca sugestiile imposibile nu apar.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Matricea de completion acopera toate categoriile din corpus si fiecare item inserat produce markup valid in contextul testat.
- [x] Completion warm p95 este sub 100 ms pe documentul mare si nu blocheaza alte documente.

### Etapa 4 - Hover, navigare, references si rename

- [x] Afiseaza hover pentru elemente, proprietati, events si tipuri cu signature, inherited/declaring type, default value si XML docs disponibile.
- [x] Afiseaza hover tipizat pentru binding segments, resources, Aspect/Motion/Prism symbols si diagnostics explanation fara duplicarea mesajului brut.
- [x] Implementeaza go-to-definition catre tip/membru C#, paired `.crn.cs`, named element, resource, template, Aspect, Motion clip/spec si Prism symbol definit local.
- [x] Implementeaza references pentru names/resources/simboluri declarative cu scopes corecte si pentru simboluri C# prin Roslyn.
- [x] Implementeaza document highlights pentru declaratie si utilizari in fisierul curent.
- [x] Permite rename numai cand toate referintele sunt rezolvate exact si editurile nu ating text arbitrar; refuza explicit cazurile ambigue.
- [x] Adauga teste cross-file, cross-project, shadowing, duplicate names, generated companion si documente partial invalide.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Navigarea nu conduce la generated `.g.cs` cand exista sursa user-authored mai buna.
- [x] Rename produce workspace edits compilabile si nu modifica simboluri cu acelasi text din alt scope.

### Etapa 5 - Semantic tokens, symbols, folding si selection

- [x] Defineste semantic token legend pentru element type, property, attached property, event, namespace, resource, binding source/member, directive, Motion si Prism.
- [x] Emite semantic tokens full si delta, versionate si anulabile, fara overlap invalid.
- [x] Emite document symbols ierarhice pentru root, named elements, resources, templates, Aspects, Motion si Prism declarations.
- [x] Emite workspace symbols pentru declaratiile Cerneala navigabile fara a indexa literals sau generated noise.
- [x] Emite folding ranges pentru elements, resources, templates si directive blocks, pastrand comments/regions XML.
- [x] Emite selection ranges de la token la expression, attribute, element si document.
- [x] Adauga tests pe documente mixte si incomplete.
- [x] Reindexeaza solutia.

**Gate etapa 5**

- [x] Tokenii semantici acopera sintaxa Cerneala pe care TextMate nu o poate distinge si raman stabili dupa editari locale.
- [x] Symbols/folding nu dispar integral din cauza unei erori locale recuperabile.

### Etapa 6 - Formatting si code actions

- [x] Defineste un formatter canonical care pastreaza comments, text literal, directive semantics si ordinea atributelor user-authored daca nu exista motiv semantic de reordonare.
- [x] Implementeaza document/range formatting, indentarea property elements si directive blocks si on-type formatting pentru `>`, newline si closing delimiters.
- [x] Asigura idempotenta: doua formatari consecutive produc zero edits.
- [x] Adauga code actions numai pentru reparatii deterministe: namespace alias lipsa, closing tag lipsa, typo cu candidat unic, event handler companion si conversie attribute/property-element unde este valida.
- [x] Adauga organize/fix-all numai pentru diagnostics independente; refuza fix-all cand edits se suprapun ori schimba semantica.
- [x] Adauga snapshot tests pentru markup real, comments, Motion, Prism si documente partiale.
- [x] Reindexeaza solutia.

**Gate etapa 6**

- [x] Formatterul este lossless semantic, idempotent si nu produce diff pe corpusul deja formatat dupa aprobarea baseline-ului.
- [x] Fiecare code action aplicata elimina diagnosticul tinta si lasa documentul parseabil.

### Etapa 7 - Concurenta, performanta si hardening

- [x] Instrumenteaza parse, bind, completion, diagnostics, navigation, queue time, cancellation si allocation fara a colecta text user-authored.
- [x] Adauga stress tests cu typing rapid, doua documente active, project reload si 100 de cereri completion anulate.
- [x] Impune latest-version wins, limite de cache si cleanup la close/solution unload/shutdown.
- [x] Stabileste gate-uri pe hardware documentat: diagnostics warm p95 sub 200 ms, completion p95 sub 100 ms, hover/navigation p95 sub 100 ms si zero request neanulabila peste 500 ms.
- [x] Verifica memory plateau dupa 1.000 open/change/close cycles si absenta child processes dupa shutdown/crash host.
- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`, `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.
- [x] Documenteaza protocol capabilities, logging, troubleshooting si limitarea syntax-only pentru fisiere standalone.

**Gate etapa 7**

- [x] Toate capabilitatile anuntate de server sunt testate protocol-level si respecta bugetele.
- [x] Serverul este host-agnostic, nu referentiaza Visual Studio SDK si se inchide curat.

## 6. Definitia de gata

- [x] Language serverul ofera toate capabilitatile obligatorii pentru intreg dialectul Cerneala.
- [x] Diagnostics sunt identice cu build-ul si tolerante in timpul tastarii.
- [x] Workspace-ul urmareste corect bufferul nesalvat, proiectele si compilatiile Roslyn.
- [x] Completion si navigarea sunt tipizate, scoped si rapide.
- [x] Toate protocol tests, stress tests si full suite sunt GREEN.
