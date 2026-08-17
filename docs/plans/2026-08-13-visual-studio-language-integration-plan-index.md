# Plan index: integrare completa Cerneala in Visual Studio Community

> Data: 2026-08-13
> Status: propus
> Target validat: Visual Studio Community 2026 18.9 pe Windows
> Sursa de decizie: discutia despre IntelliSense pentru `.crn` si cerinta ca markup-ul valid Cerneala sa nu produca erori false in editor
> Scop: livram suport complet de limbaj pentru Cerneala in Visual Studio Community, fara sa duplicam parserul si semantica intre source generator, language server si extensia VSIX.

## 1. Baseline si problema actuala

`Cerneala.SourceGen/UiMarkupGenerator.cs` consuma astazi fisierele `*.crn` din `AdditionalFiles`, iar nucleul si serverul finalizate folosesc aceeasi semantica editor-agnostic. Inaintea VSIX-ului, contractul de fisier migreaza prin planul dedicat la extensia simpla `.crn`, deoarece spike-ul Visual Studio 18.9 a demonstrat ca extensia compusa ramane clasificata drept XML generic. Build-ul intelege Cerneala, dar editorul Visual Studio nu poate oferi completari tipizate, navigare, diagnostics Cerneala ori recovery fara hostul VSIX.

Un XSD poate descrie doar o felie statica din structura XML. Nu poate reprezenta corect bindings tipizate, resources cu scope, `DataContext`, `Aspect`, templates, Motion, Prism ori simbolurile proiectului C#. Solutia trebuie sa foloseasca acelasi nucleu de limbaj pentru build si editor; altfel ajungem cu doua adevaruri care se injura peste gard.

Visual Studio ofera oficial un `LanguageServerProvider` out-of-process, document filters si integrare LSP. Colorizarea si comportamentul local al editorului pot fi completate prin Language Configuration si TextMate grammar. Referinte de implementare: [Language Server Provider](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/language-server-provider/language-server-provider?view=visualstudio), [LSP extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=visualstudio), [Language Configuration](https://learn.microsoft.com/en-us/visualstudio/extensibility/language-configuration?view=visualstudio).

## 2. Contractul pentru "full syntax support"

- Orice document `.crn` care compileaza fara diagnostics Cerneala trebuie sa aiba zero diagnostics Cerneala false in editor.
- Source generatorul si editorul folosesc aceleasi reguli, aceleasi coduri de diagnostic, aceleasi severitati si aceleasi source spans pentru erorile semantice.
- Un document temporar incomplet in timpul tastarii produce diagnostics locale si recuperabile, nu o avalansa de erori secundare pe restul fisierului.
- IntelliSense acopera elemente, proprietati, property elements, attached properties, events, valori, enums, resources, namespaces, tipuri CLR, `DataType`, `TargetType`, bindings, templates, `Aspect`, Motion si Prism.
- Visual Studio ofera completion, signature help unde sintaxa are argumente, hover, go-to-definition, references, rename sigur, document symbols, semantic colorization, folding, formatting si code actions pentru cazurile in care exista o reparatie determinista.
- Extensia se activeaza exclusiv pentru `**/*.crn`; nu confisca fisiere `.xml` ori alte documente.
- Instalarea se face printr-un singur VSIX care include serverul si dependentele lui; utilizatorul nu instaleaza manual un runtime ori un proces separat.

## 3. Decizii arhitecturale

- `Cerneala.Language` devine nucleul comun: text source, parser tolerant, syntax tree, diagnostics, semantic model si servicii editor-agnostic.
- `Cerneala.SourceGen` consuma `Cerneala.Language` si ramane proprietarul emiterii C#; nu mai detine o a doua implementare de parsing sau binding.
- `Cerneala.LanguageServer` este un proces LSP out-of-process care gestioneaza document snapshots, workspace Roslyn si operatiile IntelliSense.
- `Cerneala.VisualStudio` este un host VSIX subtire bazat initial pe `Microsoft.VisualStudio.Extensibility`; fallback-ul la VSSDK clasic este permis numai daca un spike reproductibil demonstreaza un feature gap blocant.
- Nucleul comun nu depinde de Visual Studio, JSON-RPC sau UI. VSIX-ul nu contine reguli Cerneala si language serverul nu emite cod de view.
- Parserul tolerant inlocuieste `XDocument` ca adevar sintactic comun. `XDocument` nu ramane o cale paralela ascunsa in source generator.
- Roslyn `Compilation` ramane sursa pentru tipuri, membri, XML documentation si simboluri C#. Nu introducem reflection ori scanare runtime.

## 4. Planuri si dependente

1. `docs/plans/2026-08-13-cerneala-language-core.md` - parser tolerant, model semantic comun si migrarea source generatorului.
2. `docs/plans/2026-08-13-cerneala-language-server.md` - server LSP si toate capabilitatile IntelliSense; dependent de planul 1.
3. `docs/plans/2026-08-14-crn-markup-extension-migration.md` - breaking change-ul de la `.cui.xml` la `.crn`; dependent de planurile 1-2.
4. `docs/plans/2026-08-13-visual-studio-community-extension.md` - host VSIX, editor integration, packaging si verificare in Visual Studio Community; dependent de planurile 1-3.

Planul 4 incepe numai dupa migrarea completa la `.crn`; spike-ul `.cui.xml` ramane dovada arhitecturala, nu baza proiectului final.

## 5. Non-obiective

- Nu implementam un designer vizual, live preview ori drag-and-drop XAML designer.
- Nu promitem compatibilitate generala XAML/WPF/Avalonia; extensia intelege dialectul Cerneala existent.
- Nu targetam Visual Studio 2022 in prima livrare. Compatibilitatea retroactiva primeste un plan separat dupa ce targetul Community 2026 este GREEN.
- Nu publicam automat extensia in Visual Studio Marketplace. Planul produce un VSIX release-ready, instalabil si actualizabil; procesul editorial Marketplace ramane separat.
- Nu invocam build-ul complet sau source generatorul la fiecare tasta si nu folosim XSD drept semantic model.
- Nu adaugam suport VS Code in aceste planuri, desi serverul LSP trebuie sa ramana host-agnostic.

## 6. Gate-uri globale

- [ ] Dupa fiecare modificare C# sau de proiect, ruleaza `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.
- [ ] Orice API public nou sau modificat are pagina sincronizata in `docs-site/documentation/classes/`, creata cu skill-ul `writing-api-documentation`; `docs-site/documentation/manifest.json` este actualizat cand o pagina este adaugata sau redenumita.
- [ ] Fiecare feature IntelliSense are teste la nivel de nucleu semantic, protocol LSP si, unde integrarea Visual Studio poate schimba comportamentul, test in Experimental Instance.
- [ ] Corpusul tuturor fisierelor `.crn` din repo si al testelor sourcegen valide ramane fara diagnostics false in editor.
- [ ] Diagnostics sourcegen si LSP sunt comparate automat dupa id, severitate, mesaj si span; divergenta blocheaza gate-ul.
- [ ] Testele de tastare folosesc editari incrementale si comenzi de editor, nu asignari directe ale starii semantic modelului.
- [ ] Nicio verificare UI nu foloseste Computer Use sau screen capture de sistem. Corectitudinea IntelliSense se valideaza prin API-urile editorului si protocolului; daca va fi ceruta dovada vizuala, se foloseste numai un API de captura oferit de aplicatie/extensie.
- [ ] Full suite ramane GREEN cu `dotnet test .\Cerneala.slnx`.

## 7. Ordinea de livrare

- [ ] Finalizeaza planul 1 si demonstreaza ca source generatorul foloseste exclusiv nucleul comun.
- [ ] Finalizeaza planul 2 si demonstreaza protocol-level toate capabilitatile declarate.
- [ ] Finalizeaza planul 3 si demonstreaza breaking change-ul complet la `.crn` fara suport dual ascuns.
- [ ] Finalizeaza planul 4, instaleaza VSIX-ul intr-o instanta curata Visual Studio Community 2026 si ruleaza matricea end-to-end.
- [ ] Ruleaza gate-ul global pe `CernealaPresentation`, Playground si un proiect consumer minim din afara solutiei.
- [ ] Publica raportul final de compatibilitate, performanta si limitari cunoscute in documentatia extensiei.

## 8. Definitia de gata

- [ ] Un utilizator instaleaza un singur VSIX, redeschide Visual Studio Community si primeste automat suport Cerneala pentru `*.crn`.
- [ ] Toate constructiile Cerneala acceptate de build au colorizare si semantic understanding corect in editor.
- [ ] Completion, hover, navigation, rename, diagnostics, formatting si code actions functioneaza conform planurilor dependente.
- [ ] Documentele valide din repo au zero erori false, iar documentele invalide indica tokenul relevant fara cascada inutila.
- [ ] Source generatorul si language serverul nu contin parseri sau bindere concurente.
- [ ] Deschiderea, tastarea si completion-ul nu blocheaza UI thread-ul Visual Studio si respecta bugetele masurate din planurile dependente.
- [ ] VSIX-ul se instaleaza, se actualizeaza si se dezinstaleaza curat pe Visual Studio Community 2026 18.9.
