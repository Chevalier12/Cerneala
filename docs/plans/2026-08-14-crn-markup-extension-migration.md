# Plan: migrarea markup-ului Cerneala la extensia `.crn`

> Data: 2026-08-14
> Status: finalizat
> Dependente: `docs/plans/2026-08-13-cerneala-language-core.md`, `docs/plans/2026-08-13-cerneala-language-server.md`
> Blocheaza: `docs/plans/2026-08-13-visual-studio-community-extension.md`
> Scop: inlocuim direct si complet extensia compusa `.cui.xml` cu `.crn`, pastrand neschimbat dialectul XML Cerneala si fara compatibilitate temporara pentru extensia veche.

## 1. Rezumat si decizii

Markup-ul Cerneala ramane acelasi dialect XML tolerant si source-generated. Se schimba numai contractul de nume al documentului: `View.cui.xml` devine `View.crn`, iar companionul `View.cui.xml.cs` devine `View.crn.cs` deoarece pairing-ul curent este definit ca `document.Path + ".cs"`.

Migrarea este un breaking change imediat. Source generatorul, language serverul si proiectele nu mai accepta `.cui.xml` dupa inchiderea acestui plan. Nu adaugam warning de tranzitie, alias ori suport permanent dublu. Decizia evita conflictul Visual Studio dintre extensia compusa si content type-ul XML generic; nu schimbam sintaxa doar ca sa-i facem editorului pe plac, ca aia ar fi alta belea si alt plan.

## 2. Baseline si problema actuala

- `Cerneala.SourceGen/UiMarkupGenerator.cs` filtreaza `AdditionalText` prin `EndsWith(".cui.xml")`.
- `Cerneala.LanguageServer/Workspace/ProjectContext.cs` incarca numai additional documents cu acelasi sufix.
- `Cerneala.Language/Semantics/CernealaSemanticModel.cs` elimina sufixul compus pentru a rezolva tipul companion.
- Cinci proiecte includ markup-ul prin globul `AdditionalFiles Include="**\*.cui.xml"`; fixture-ul Language Server este al saselea owner si include explicit `View.cui.xml`.
- Repo-ul contine 16 documente markup versionate si 16 companions cu conventia veche.
- Testele Language, SourceGen si LanguageServer, corpusul versionat, benchmarkurile, documentatia publica si planurile contin cai sau exemple `.cui.xml`.
- Spike-ul Visual Studio 18.9 a demonstrat ca `DocumentFilter.FromGlobPattern("**/*.cui.xml")` este respins de evaluatorul compile-time al `LanguageServerProvider`, iar un `DocumentTypeConfiguration` cu `.cui.xml` compileaza dar documentul este deschis efectiv cu content type `XML`. Extensia simpla `.crn` elimina ambiguitatea fara bridge MEF in-proc.

## 3. Obiective

- Un singur contract intern pentru extensie, detectie, nume logic si cale companion.
- Source generatorul si language serverul accepta exclusiv `.crn`, case-insensitive.
- Toate documentele versionate si companions lor folosesc `.crn`/`.crn.cs` fara schimbarea continutului XML sau C#.
- Build-ul, diagnostics, output-ul generat, semantic pairing-ul si toate capabilitatile LSP raman echivalente dupa redenumire.
- Toate proiectele, testele, corpusurile, benchmarkurile, documentatia si planurile active descriu `.crn` ca extensie canonica.

## 4. Non-obiective si stop conditions

- Fara format declarativ nou, parser nou, schema noua ori modificari de sintaxa.
- Fara suport dual, redirect, warning de depreciere sau auto-conversie pentru `.cui.xml`.
- Fara modificari ale API-ului public runtime Cerneala.
- Fara bridge Visual Studio MEF/content type; planul VSIX se reia numai dupa ce `.crn` este contractul GREEN al repo-ului.
- Daca redenumirea schimba output-ul C# generat, diagnostics ori rezolvarea companionului in alt mod decat calea fisierului, batch-ul se opreste si divergenta se investigheaza in layer-ul care detine conventia.

## 5. Arhitectura propusa

`Cerneala.Language` devine proprietarul conventiei printr-un helper intern unic, de exemplu `CernealaDocumentPath`, accesibil assembly-urilor prietene deja declarate in `Properties/AssemblyInfo.cs`. Helperul expune sufixul canonic `.crn`, verificarea case-insensitive, derivarea numelui logic si calea companion `.crn.cs`. `Cerneala.SourceGen`, `Cerneala.LanguageServer` si semantic modelul il folosesc in locul literalelor duplicate.

MSBuild ramane proprietarul includerii fisierelor ca `AdditionalFiles`, dar fiecare proiect foloseste exclusiv globul `**\*.crn`. Continutul XML nu este interpretat diferit. Renumele fisierelor se face cu operatii care pastreaza istoricul si continutul, inclusiv modificarile locale deja existente.

## 6. Fisiere estimate

- `Cerneala.Language/` pentru helperul intern al conventiei de cale.
- `Cerneala.Language/Semantics/CernealaSemanticModel.cs`.
- `Cerneala.SourceGen/UiMarkupGenerator.cs`.
- `Cerneala.LanguageServer/Workspace/ProjectContext.cs`.
- `Cerneala.csproj`, `CernealaPresentation/CernealaPresentation.csproj`, proiectele Playground si `tests/Fixtures/LanguageServerWorkspace/LanguageServerWorkspace.csproj`.
- Cele 16 fisiere markup si cele 16 companions din `CernealaPresentation/`, `Playground/` si fixture-ul Language Server.
- Suitele `tests/Cerneala.Tests.Language/`, `tests/Cerneala.Tests.SourceGen/` si `tests/Cerneala.Tests.LanguageServer/`.
- Corpusul si benchmarkurile de limbaj.
- Documentatia din `docs/`, `docs-site/` si planurile de integrare Visual Studio.

## 7. Etape de implementare

### Etapa 0 - Baseline si teste RED pentru noul contract

- [x] Inventariaza si ingheata lista celor 16 documente markup si 16 companions care trebuie redenumite, plus toate proiectele care le includ ca `AdditionalFiles`. (Inventar: `tests/Cerneala.Tests.Language/Corpus/crn-migration-stage0-inventory.txt`.)
- [x] Adauga teste RED in `Cerneala.Tests.Language` pentru detectia `.crn`, derivarea numelui logic `View` si pairing-ul `View.crn` -> `View.crn.cs`.
- [x] Adauga teste RED in `Cerneala.Tests.SourceGen` care cer generare pentru `View.crn` si zero output pentru acelasi continut furnizat ca `View.cui.xml`.
- [x] Adauga teste RED in `Cerneala.Tests.LanguageServer` care cer project ownership si semantic context pentru `View.crn`, dar nu pentru `View.cui.xml`.
- [x] Captureaza baseline-ul output-ului generat si al diagnostics pentru un document reprezentativ inainte de redenumire, astfel incat schimbarea de cale sa nu mascheze o divergenta semantica. (Hint `ViewFactory.abdf9b8e.g.cs`, SHA-256 `DCD437128E0720708F736B79865B5D8C9F3A1D38C2BA580473DD655E62F4CF9F`, zero diagnostics.)
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] Testele `.crn` sunt RED exclusiv din cauza literalelor si globurilor vechi, nu din cauza harness-ului.
- [x] Testele negative pentru `.cui.xml` descriu breaking change-ul aprobat si nu accepta fallback implicit.

### Etapa 1 - Contractul intern unic si hosturile de limbaj

- [x] Adauga helperul intern de cale in `Cerneala.Language` cu extensia `.crn`, comparatie case-insensitive, nume logic si companion path; nu introduce API public.
- [x] Inlocuieste filtrul privat din `UiMarkupGenerator.IsMarkupFile` cu helperul comun.
- [x] Inlocuieste filtrarea additional documents din `ProjectContext.CreateAsync` cu helperul comun.
- [x] Inlocuieste strip-ul `.cui.xml` si construirea companionului din `CernealaSemanticModel` cu helperul comun.
- [x] Elimina literalele de conventie duplicate din codul de productie si pastreaza ownership-ul in `Cerneala.Language`.
- [x] Ruleaza testele RED din etapa 0 si suitele tintite Language, SourceGen si LanguageServer afectate. (GREEN: 8 Language, 3 SourceGen si 2 LanguageServer.)
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] `.crn` este singura extensie acceptata de toate cele trei hosturi, iar `.cui.xml` este ignorata determinist.
- [x] Pairing-ul root type gaseste `View.crn.cs`, class-name generation elimina exact `.crn`, iar output-ul semantic ramane identic baseline-ului.

### Etapa 2 - Redenumirea fisierelor si integrarea MSBuild

- [x] Redenumeste toate cele 16 documente `.cui.xml` la `.crn` fara modificarea continutului.
- [x] Redenumeste toate cele 16 companions `.cui.xml.cs` la `.crn.cs`, pastrand integral modificarile locale existente.
- [x] Schimba toate cele sase globuri `AdditionalFiles` la `**\*.crn` si confirma ca niciun proiect nu include extensia veche.
- [x] Actualizeaza fixture-ul Language Server, corpusul `repository-documents.txt`, golden paths si orice resursa de test care encodeaza numele vechi.
- [x] Actualizeaza benchmarkurile de limbaj sa incarce documentele `.crn` redenumite fara schimbarea corpusului masurat.
- [x] Construieste `CernealaPresentation` si cele trei proiecte Playground si confirma ca source generatorul produce aceleasi tipuri/hint names aprobate. (GREEN: 15 output-uri cu stem-urile aprobate; numai hash-ul derivat din cale s-a schimbat.)
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Nu mai exista fisiere versionate `*.cui.xml` sau `*.cui.xml.cs` in afara dovezilor temporare ale spike-ului, care sunt sterse inainte de checkpoint.
- [x] Toate proiectele reale si fixture-ul extern compileaza folosind exclusiv `AdditionalFiles` `.crn`.

### Etapa 3 - Migrarea completa a testelor si corpusurilor

- [x] Actualizeaza toate path literals din testele Language pentru `.crn`, inclusiv recovery, semantic scopes, navigation, formatting, completion si sourcegen parity.
- [x] Actualizeaza toate path literals din testele SourceGen, inclusiv Application, bindings, Motion, Prism si presentation regression.
- [x] Actualizeaza toate path literals din protocol tests LanguageServer, inclusiv workspace reload, diagnostics, completion, navigation, formatting, structure si hardening.
- [x] Pastreaza exemple `.cui.xml` numai in testele negative explicite care verifica respingerea breaking change-ului.
- [x] Ruleaza `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj`, `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj` si `dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`. (GREEN: 125 + 431 + 30 teste.)
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Toate parity, corpus si protocol tests sunt GREEN pe `.crn`.
- [x] Nicio capacitate de limbaj nu depinde accidental de faptul ca documentul avea extensia finala `.xml`.

### Etapa 4 - Documentatie si planuri dependente

- [x] Actualizeaza ghidurile active din `docs/` si paginile produsului din `docs-site/` astfel incat exemplele, setup-ul MSBuild si descrierile sa foloseasca `.crn`.
- [x] Actualizeaza pagina API `docs-site/documentation/classes/Cerneala.SourceGen.UiMarkupGenerator.md` cu skill-ul `writing-api-documentation`; verifica manifestul, fara pagina noua daca numele API-ului nu se schimba. (Pagina existenta ramane in manifest; nu a fost adaugata sau redenumita.)
- [x] Actualizeaza celelalte pagini API care descriu explicit `.cui.xml`, fara a schimba documentatie fara legatura.
- [x] Actualizeaza planurile Language/Core, LanguageServer si indexul Visual Studio la contractul `.crn`, pastrand checkmark-urile istorice valide.
- [x] Adauga acest plan ca dependenta explicita a `2026-08-13-visual-studio-community-extension.md` si schimba activation/document filters/fixtures la `*.crn`.
- [x] Documenteaza breaking change-ul in ghidul de pornire: rename `View.cui.xml` -> `View.crn`, `View.cui.xml.cs` -> `View.crn.cs` si `AdditionalFiles` -> `**\*.crn`; fara promisiune de compatibilitate.

**Gate etapa 4**

- [x] Documentatia publica descrie un singur contract `.crn`, iar planul VSIX nu mai cere workaround pentru XML generic.
- [x] Orice aparitie ramasa a textului `.cui.xml` este fie in acest istoric de migrare, fie intr-un test negativ explicit si are motiv verificabil. (Exceptii intentionate: ghidul de migrare, acest plan si indexul/spike-ul istoric, inventarul Stage 0 si cele trei teste negative.)

### Etapa 5 - Verificare finala si inchiderea breaking change-ului

- [x] Ruleaza build-urile proiectelor `CernealaPresentation`, Playground si fixture-ul LanguageServer dupa ultima modificare relevanta. (GREEN; dovezile Stage 2 au ramas valide pentru proiectele neatinse, iar `CernealaPresentation` a fost reconstruit dupa actualizarea markup-ului.)
- [x] Ruleaza suitele tintite Language, SourceGen si LanguageServer in starea finala. (GREEN: 125 + 431 + 30 teste.)
- [x] Ruleaza `dotnet test .\Cerneala.slnx` o singura data dupa ultima modificare de cod/proiect/fisier redenumit. (GREEN final: 3.507 teste; un esec tranzitoriu de allocation-gate a trecut izolat si la rerularea completa fara schimbare de cod.)
- [x] Verifica prin inventar ca exista 16 documente `*.crn`, 16 companions `*.crn.cs` si zero fisiere reale `*.cui.xml`/`*.cui.xml.cs`.
- [x] Ruleaza `git diff --check`, inspecteaza rename detection si confirma ca redenumirea nu a pierdut modificarile locale preexistente. (32 mapari: 27 blob-uri identice cu HEAD si 5 redenumiri cu continut modificat asteptat; toate cele 32 au pastrat SHA-256 la mutare.)
- [x] Reindexeaza solutia finala si cere indexare fara erori noi.

**Gate etapa 5**

- [x] Build-ul, source generatorul, semantic modelul si LSP folosesc exclusiv `.crn` si toate suitele sunt GREEN.
- [x] Migrarea este documentata ca breaking change complet; nu exista alias, warning temporar ori suport dual ascuns.

## 8. Ordinea recomandata

1. Inchide etapele 0-5 ale acestui plan in ordine, cate un batch atomic per etapa.
2. Abia dupa gate-ul final reia `docs/plans/2026-08-13-visual-studio-community-extension.md` de la Etapa 0.
3. Refaca spike-ul VSIX direct pe document type-ul simplu `.crn`; nu reutiliza bridge-ul `.cui.xml` abandonat.

## 9. Definitia de gata

- [x] `.crn` este singura extensie de markup Cerneala acceptata de build, semantic model, language server si proiecte.
- [x] Toate documentele si companions versionate sunt redenumite fara schimbare de continut sau comportament generat.
- [x] `.cui.xml` este respinsa explicit si apare numai in dovezi istorice ori teste negative aprobate.
- [x] Toate testele tintite, proiectele consumatoare si `dotnet test .\Cerneala.slnx` sunt GREEN.
- [x] Documentatia, API docs si planurile dependente sunt sincronizate cu breaking change-ul.
