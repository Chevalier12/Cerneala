# Plan: nucleu comun de limbaj Cerneala

> Data: 2026-08-13
> Status: finalizat
> Dependenta: niciuna
> Scop: extragem parsing-ul si semantica `.crn` intr-un nucleu tolerant si editor-agnostic, apoi migram source generatorul pe el fara schimbari de comportament la build.

## 1. Baseline si problema actuala

`Cerneala.SourceGen/UiMarkupGenerator.cs` detecteaza `*.crn`, protejeaza comparatori din directive, inveleste documentul intr-un fragment artificial si il parseaza prin `XDocument.Parse`. Un tag sau quote neterminat invalideaza intregul document, comportament acceptabil la build, dar inutil pentru IntelliSense in timp ce utilizatorul tasteaza.

Diagnostics `CERNEALAUI*` sunt declarate in source generator, iar rezolvarea de tipuri, bindings, resources, templates, Aspect, Motion si Prism este impartita intre `UiMarkupGenerator.GenerationScope`, partiale si subfolderele Prism. Aceste tipuri sunt strans legate de `SourceProductionContext`, `XElement` si emiterea C#. Un language server nu le poate reutiliza fara sa ruleze generatorul sau sa copieze semantica.

## 2. Arhitectura tinta

- Proiect nou `Cerneala.Language/Cerneala.Language.csproj`, compatibil cu `netstandard2.0` pentru consum din source generator si fara dependinte Visual Studio/LSP.
- Text model imuabil cu offset-uri, line map, versiuni si source spans stabile.
- Lexer/parser tolerant pentru XML-ul Cerneala si limbajele embedded, cu noduri lipsa si tokeni omisi/skipped reprezentati explicit.
- Syntax tree lossless: trivia, comments, ordine de atribute si textul original pot fi reconstruite fara pierdere.
- Semantic model separat de syntax tree, construit peste un adaptor de simboluri Roslyn si capabil sa raspunda incremental la queries de editor.
- Catalog unic pentru diagnostics Cerneala; source generatorul si language serverul fac doar conversia catre tipul de diagnostic al hostului.
- Emiterea C# ramane in `Cerneala.SourceGen`, dar primeste noduri si simboluri deja rezolvate din nucleul comun.

## 3. Non-obiective

- Fara protocol LSP, VSIX, editor UI sau dependinte `Microsoft.VisualStudio.*` in acest proiect.
- Fara reflection, runtime XML parser ori schimbarea formatului `.crn`.
- Fara rescriere estetica a codului generat daca output-ul actual este semantic echivalent.
- Fara tolerarea la build a documentelor incomplete; recovery este pentru analiza editorului, iar compilarea documentului salvat ramane stricta.

## 4. Fisiere estimate

- `Cerneala.Language/Cerneala.Language.csproj`
- `Cerneala.Language/Text/` pentru source text, spans, line map si incremental changes
- `Cerneala.Language/Syntax/` pentru tokens, nodes, lexer, parser si recovery
- `Cerneala.Language/Semantics/` pentru compilation context, scopes, symbols si semantic model
- `Cerneala.Language/Diagnostics/` pentru catalogul comun `CERNEALAUI*`
- `Cerneala.Language/Features/` pentru completion facts, symbol locations si document outline independente de LSP
- `Cerneala.SourceGen/Cerneala.SourceGen.csproj`
- `Cerneala.SourceGen/UiMarkupGenerator.cs` si partialele sale
- `Cerneala.SourceGen/Prism/**`
- `tests/Cerneala.Tests.Language/`
- `tests/Cerneala.Tests.SourceGen/`
- `Cerneala.slnx`
- API docs din `docs-site/documentation/classes/` pentru orice tip public necesar intre assemblies

## 5. Etape de implementare

### Etapa 0 - Inventar semantic si corpus RED

- [x] Inventariaza toate constructiile acceptate de source generator din `UiMarkupGenerator`, `UiMarkupBindingResolver`, `UiMarkupDirectiveParser`, Motion si Prism si mapeaza fiecare constructie la testele existente.
- [x] Construieste un corpus versionat din toate fisierele `.crn` din repo, exemplele documentate si markup-urile valide/invalide din `Cerneala.Tests.SourceGen`.
- [x] Adauga `tests/Cerneala.Tests.Language/Cerneala.Tests.Language.csproj` si un harness care ruleaza acelasi document prin parserul nou, semantic model si source generator.
- [x] Adauga teste RED pentru documente incomplete dupa fiecare categorie de token: `<`, nume de element, atribut, quote, property element, binding, directive body, Motion si Prism.
- [x] Adauga teste RED care cer maximum un diagnostic primar per zona sintactica rupta si absenta diagnostics semantice sub nodul nerecuperabil.
- [x] Captureaza baseline-ul actual al diagnostics sourcegen dupa id, severitate, mesaj si span pentru corpusul invalid.
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] Fiecare constructie Cerneala curenta apare in matrice si are cel putin un exemplu valid si unul invalid relevant.
- [x] Testele recovery sunt RED din cauza dependentei actuale de `XDocument`, nu din cauza harness-ului.

### Etapa 1 - Text model si parser tolerant lossless

- [x] Implementeaza source text, line map si aplicarea editurilor incrementale cu offset-uri UTF-16 compatibile LSP/Roslyn.
- [x] Implementeaza lexerul pentru delimitatori XML, names, namespaces, strings, comments, CDATA/trivia si text embedded fara sa modifice caracterele directivei.
- [x] Implementeaza syntax nodes pentru document, element, attribute, property element, text, comment si error/missing nodes.
- [x] Implementeaza recovery local pentru closing tags lipsa, quotes neterminate, elemente suprapuse, text top-level si EOF in interiorul unui nod.
- [x] Pastreaza exact source spans pentru tokenii reali si defineste zero-width spans deterministe pentru tokenii lipsa.
- [x] Adauga round-trip tests care reconstruiesc byte-for-byte documentele valide, inclusiv whitespace si comments.
- [x] Adauga mutation tests care aplica editari caracter cu caracter si confirma ca parserul nu arunca exceptii.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Parserul proceseaza intregul corpus si 10.000 de editari incrementale randomizate fara crash, hang sau span in afara documentului.
- [x] Documentele valide au un arbore complet, iar cele incomplete pastreaza siblings de dupa eroare cand delimitarea permite recovery.

### Etapa 2 - Limbajele embedded si diagnostics comune

- [x] Muta gramatica pentru bindings, interpolari si modes din `UiMarkupBindingResolver` in syntax nodes independente de emitere.
- [x] Muta `@template`, `@when`, `@if`, assignments si celelalte directive din `UiMarkupDirectiveParser` intr-un parser embedded cu source spans absolute.
- [x] Muta sintaxa Motion din `UiMarkupMotionSyntax`, `MotionMarkupLanguage` si resolverele aferente in nucleul comun.
- [x] Muta sintaxa si catalogul Prism din `Cerneala.SourceGen/Prism/Syntax` si `Prism/Catalog`, fara dependinte de emitter.
- [x] Centralizeaza descriptorii `CERNEALAUI*` intr-un catalog host-agnostic cu id, severity, message format, category si exact span.
- [x] Defineste modurile `Editor` si `Build`: acelasi parser si aceeasi semantica, dar diagnostics de incompletitudine tranzitorie sunt reduse in editor si stricte la build.
- [x] Adauga teste de recovery pentru fiecare limbaj embedded, inclusiv braces, commas, quotes, comparatori si nesting neterminat.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Niciun parser embedded nu primeste `XText`, `XElement` sau `SourceProductionContext`.
- [x] Diagnostics valide existente isi pastreaza id-ul si mesajul; orice schimbare de span este explicata de o localizare mai precisa si aprobata in golden files.

### Etapa 3 - Workspace semantic si adaptor Roslyn

- [x] Defineste `CernealaCompilation`, `CernealaDocument` si `CernealaSemanticModel` cu lifecycle explicit si cancellation.
- [x] Defineste adaptorul minim peste Roslyn `Compilation`, `ITypeSymbol`, membri, accessibility, inheritance, XML docs si source locations.
- [x] Rezolva `clr-namespace`, aliases, root type, paired `.crn.cs`, `Application`, `Window`, `UserControl` si custom controls prin simbolurile proiectului.
- [x] Modeleaza content properties, normal properties, property elements, attached properties, events si conversiile de literal existente.
- [x] Separa bind-ul semantic de emitere: rezultatul contine simboluri si valori validate, nu fragmente C#.
- [x] Adauga cache-uri versionate pe compilation/document si invalideaza numai proiectele/documentele afectate de schimbari.
- [x] Adauga teste cu project references, tipuri partiale, namespace aliases, tipuri duplicate si compilatii cu erori C# independente.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Acelasi markup si aceeasi `Compilation` produc acelasi set ordonat de simboluri si diagnostics indiferent de host.
- [x] Nucleul semantic nu incarca assemblies si nu foloseste reflection.

### Etapa 4 - Scopes, bindings, resources, templates si Aspect

- [x] Muta in semantic model namescopes, resource scopes, application resources si regulile de shadowing/duplicate names.
- [x] Rezolva `$DataContext`, `$root`, `$self`, elemente numite, resources, template owner/parts si binding modes prin simboluri tipizate.
- [x] Modeleaza schimbarile locale de `DataContext` si valideaza segmentele ulterioare fata de tipul rezultat, inclusiv in `ContentTemplate DataType`.
- [x] Rezolva `ItemsControl.Templates`, selectia template-ului dupa `DataType`, `ItemsPanel`, `ItemsSource` si content ownership.
- [x] Muta `Aspect` resources, `TargetType`, assignments, templates, conditions si application-site validation in nucleul comun.
- [x] Adauga diagnostics anti-cascada: o sursa de binding nerezolvata nu produce cate o eroare pentru fiecare segment dependent.
- [x] Adauga parity tests pentru toate testele binding/template/Aspect existente si pentru markup-ul real din `CernealaPresentation`.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Semantic model poate raspunde tipului si simbolului la orice segment valid de binding si la orice resource reference.
- [x] Corpusul valid pentru bindings, templates si Aspect are zero divergente fata de source generator.

### Etapa 5 - Semantica Motion si Prism

- [x] Muta rezolvarea targeturilor, events, properties, specs, compositions si lifecycle Motion in semantic model, lasand emitterul doar sa traduca rezultatul.
- [x] Muta binding-ul Prism pentru directives, catalog symbols, parameters, values, nesting si Motion interop in semantic model.
- [x] Expune facts editor-agnostic pentru directive keywords, argument lists, parameter types, enum-like values si symbol locations.
- [x] Adauga parity tests pentru toate suitele `UiMarkupGeneratorMotion*` si `PrismMarkupContractTests`.
- [x] Adauga recovery tests pentru un document Motion/Prism incomplet care pastreaza semantic understanding pentru elementele XML neafectate.
- [x] Reindexeaza solutia.

**Gate etapa 5**

- [x] Motion si Prism nu mai au binder semantic privat care poate diverge de nucleul comun.
- [x] Toate diagnostics Motion/Prism existente au paritate exacta host-independent.

### Etapa 6 - Migrarea source generatorului

- [x] Referentiaza `Cerneala.Language` din `Cerneala.SourceGen` fara sa schimbi compatibilitatea `netstandard2.0` a analyzerului.
- [x] Inlocuieste `ParseDocument`, `MarkupDocument`, `XElement`-based binding si diagnostics private cu syntax tree si semantic model comune.
- [x] Adapteaza emitters pentru elemente, bindings, resources, Aspect, Motion si Prism la rezultatele semantice comune.
- [x] Elimina parserii, descriptorii si resolverele duplicate numai dupa ce toate parity tests sunt GREEN.
- [x] Pastreaza incremental generator caching: schimbarea unui document nu trebuie sa regenereze semantic toate documentele independente.
- [x] Compara output-ul generat pentru corpus; accepta diferente textuale numai daca assembly behavior si diagnostics sunt identice sau imbunatatirea este aprobata explicit.
- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj` si `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Reindexeaza solutia.

**Gate etapa 6**

- [x] `Cerneala.SourceGen` nu mai foloseste `XDocument`/`XElement` pentru analiza markup-ului.
- [x] Toate testele sourcegen existente sunt GREEN si corpusul valid genereaza assemblies functionale.

### Etapa 7 - Performanta, API si documentatie

- [x] Adauga benchmarkuri pentru parse cold/warm, edit incremental, semantic bind si query at-position pe documente mici, medii si `AspectChapterView.crn`.
- [x] Stabileste baseline hardware si gate-uri: parse/edit p95 sub 50 ms pentru documentul mare, query semantic warm p95 sub 25 ms si zero operatie sincrona neanulabila peste 100 ms.
- [x] Profileaza allocatiile si elimina reconstruirile complete produse de o editare locala acolo unde benchmarkul demonstreaza impact. (Nu a fost necesara optimizarea: editarea mare are p95 1,534 ms, aproximativ 32x sub buget, desi aloca 369.984 B/op.)
- [x] Marcheaza suprafata cross-assembly minima; evita API-uri publice de consum general si documenteaza obligatoriu orice tip public ramas.
- [x] Actualizeaza `docs/CernealaMarkupGuide.md`, documentatia bindings/Motion/Prism si pagina `UiMarkupGenerator` cu noul model comun fara a promite LSP inainte de planul 2.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`, benchmarkurile aprobate, `git diff --check` si reindexarea finala.

**Gate etapa 7**

- [x] Nucleul comun respecta bugetele, nu are dependinte de host si are documentatie/API docs sincronizate.
- [x] Nu exista diferente semantice cunoscute intre build si serviciile editor-agnostic.

## 6. Definitia de gata

- [x] Exista un singur parser tolerant si un singur semantic model pentru Cerneala.
- [x] Source generatorul foloseste nucleul comun pentru toate dialectele `.crn`.
- [x] Documentele incomplete pot fi analizate incremental fara crash si fara cascada inutila de diagnostics.
- [x] Diagnostics sunt host-agnostic si au paritate exacta la build.
- [x] Toate testele si benchmarkurile planului sunt GREEN.
