# Plan: extensia Cerneala pentru Visual Studio Community

> Data: 2026-08-13
> Status: in progres
> Dependente: `docs/plans/2026-08-13-cerneala-language-core.md`, `docs/plans/2026-08-13-cerneala-language-server.md`, `docs/plans/2026-08-14-crn-markup-extension-migration.md`
> Target: Visual Studio Community 2026 18.9 pe Windows
> Scop: impachetam language serverul intr-un VSIX simplu de instalat si integram complet `.crn` in editor fara sa afectam XML-ul obisnuit.

## 1. Baseline si strategie

Repo-ul nu are proiect VSIX, document type Cerneala, grammar, language configuration ori host pentru server. Prima alegere este modelul out-of-process `Microsoft.VisualStudio.Extensibility`, deoarece expune oficial `LanguageServerProvider`, lifecycle si document types. Spike-ul din 2026-08-14 a demonstrat ca providerul cere un document type, iar extensia simpla `.crn` evita conflictul produs de extensia compusa veche cu editorul XML. Activarea foloseste `DocumentFilter.FromDocumentType(...)` peste un `DocumentTypeConfiguration` asociat exclusiv cu `.crn`.

TextMate grammar asigura colorizare imediata inainte ca serverul sa fie ready, iar Language Configuration asigura brackets, comments, auto-closing si indentation locale. Semantic tokens de la server rafineaza rezultatul; grammar-ul nu incearca sa recreeze semantica Cerneala prin regex-uri kilometrice si blestemate.

## 2. Non-obiective

- Fara designer, preview, toolbox de controale ori property grid.
- Fara comenzi de build/deploy care dubleaza functiile Visual Studio.
- Fara dependenta de SKU Enterprise/Professional si fara target Visual Studio 2022 in prima versiune.
- Fara instalator separat pentru language server sau runtime.
- Fara capturi de ecran prin OS/Computer Use in verificare.

## 3. Fisiere estimate

- `Cerneala.VisualStudio/Cerneala.VisualStudio.csproj`
- `Cerneala.VisualStudio/CernealaExtension.cs`
- `Cerneala.VisualStudio/CernealaLanguageServerProvider.cs`
- `Cerneala.VisualStudio/Grammars/cerneala.tmLanguage.json`
- `Cerneala.VisualStudio/language-configuration.json`
- `Cerneala.VisualStudio/Cerneala.pkgdef`
- `Cerneala.VisualStudio/extension.vsixmanifest` sau manifestul cerut de SDK-ul validat
- `Cerneala.VisualStudio/Assets/`
- `tests/Cerneala.Tests.VisualStudio/`
- `tests/Fixtures/VisualStudioConsumer/`
- `docs/visual-studio-community.md`
- `Cerneala.slnx`

## 4. Etape de implementare

### Etapa 0 - Spike pe Visual Studio Community 2026

- [x] Creeaza un prototip minim `Microsoft.VisualStudio.Extensibility` care se incarca in Experimental Instance a instalarii Community 2026 18.9.
- [x] Demonstreaza ca `LanguageServerProvider` poate porni un proces/server bundled si poate schimba mesaje initialize/shutdown. (Exceptie aprobata: providerul out-of-process nu se activeaza in 18.9; hostul VSSDK clasic a demonstrat lifecycle-ul complet.)
- [x] Demonstreaza ca document type-ul `.crn` si `DocumentFilter.FromDocumentType(...)` activeaza providerul pentru `View.crn`, dar nu pentru `app.config`, `foo.xml`, `View.crn.cs` sau vechiul `View.cui.xml`. (Exceptie aprobata: activarea finala foloseste content type MEF exact `.crn`, dupa gap-ul reproductibil al providerului out-of-process.)
- [x] Demonstreaza coexistenta cu editorul XML, TextMate grammar, semantic tokens si Error List fara diagnostics duplicate.
- [x] Verifica suportul pentru completion, completion resolve, diagnostics, hover, definition, references, rename, formatting, semantic tokens si code actions oferit de clientul Visual Studio 18.9.
- [x] Documenteaza orice feature gap cu proiect minim si rezultat reproductibil; foloseste VSSDK clasic numai daca gap-ul blocheaza un contract obligatoriu si nu are workaround in modelul nou. (Raport: `docs/visual-studio-community-spike.md`; fallback VSSDK aprobat explicit.)
- [x] Sterge codul de spike care nu devine baza proiectului final si reindexeaza solutia.

**Gate etapa 0**

- [x] Exista o cale host confirmata pentru toate capabilitatile obligatorii sau o exceptie arhitecturala aprobata explicit.
- [x] Activarea extensiei `.crn` este precisa si nu preia fisiere XML sau alte documente straine.

### Etapa 1 - Proiect VSIX si document type Cerneala

- [x] Adauga proiectul `Cerneala.VisualStudio` si manifestul cu target exclusiv Visual Studio Community-compatible 18.x stabilit de spike.
- [x] Defineste identitate stabila, publisher, version, install target, prerequisites si icon/assets fara dependinte de un path local.
- [x] Inregistreaza document type/content type Cerneala si extensia `.crn` fara conflict de language service ori preluarea fisierelor XML.
- [x] Configureaza activation rules astfel incat extensia sa nu porneasca la solution load daca nu se deschide un document Cerneala.
- [x] Adauga o comanda discreta `Cerneala: Restart Language Server` si output channel pentru troubleshooting; nu adauga UI promotional.
- [x] Adauga teste de manifest si package contents care esueaza daca serverul, grammar-ul ori configuration lipsesc. (GREEN: 4 teste.)
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] VSIX-ul se instaleaza in Experimental Instance, extensia se incarca lazy si numai documentele `.crn` primesc content type Cerneala. (Runtime 18.9: `.crn` -> `cerneala-crn`; pachetul ramane neincarcat pana la comanda si se incarca fara erori la `Tools.CernealaRestartLanguageServer`.)
- [x] Deschiderea unui XML normal ramane identica instalarii fara extensie. (`app.config`, `foo.xml` si `View.cui.xml` au ramas content type `XML`; `.crn.cs` a ramas `CSharp`.)

### Etapa 2 - Grammar si Language Configuration

- [x] Defineste TextMate scopes pentru tags, property elements, attributes, namespaces, strings, bindings, resource references, directives, Motion si Prism.
- [x] Pastreaza XML comments, entities si malformed/incomplete tokens vizibile fara ca grammar-ul sa consume restul documentului.
- [x] Defineste brackets, auto-closing pairs, surrounding pairs, comments, indentation si word pattern in `language-configuration.json`.
- [x] Verifica precedence: semantic tokens castiga pentru simboluri tipizate, iar TextMate ramane fallback pentru textul neanalizat. (Dovada runtime Stage 0 ramane valida; grammar-ul final foloseste numai scopes standard si nu fixeaza culori.)
- [x] Adauga golden tokenization tests pe corpus si teste pentru o editare la mijlocul unei directive incomplete. (GREEN: 11 teste VisualStudio, inclusiv corpusul golden si recovery dupa `@lay` incomplet.)
- [x] Verifica teme light, dark si high contrast prin classification API, fara culori hardcodate care devin invizibile. (`TextMateSharp.Registry` a rezolvat scopes prin `VisualStudioLight`, `VisualStudioDark` si `HighContrastDark`.)
- [x] Reindexeaza solutia. (3.078 documente, 75.147 simboluri, zero erori.)

**Gate etapa 2**

- [x] Colorizarea de baza apare imediat si nu clipeste catre XML generic cand serverul porneste. (Classifier API Stage 0 a raportat TextMate inainte de server ready; VSIX-ul curent instalat pastreaza content type-ul exact `cerneala-crn` si include grammar-ul verificat byte-for-byte.)
- [x] Brackets/comments/indentation functioneaza local chiar daca serverul este oprit. (Configuratia este mapata direct pe content type in `Cerneala.pkgdef`, iar cele 11 teste valideaza toate contractele locale fara proces LSP.)

### Etapa 3 - Host, lifecycle si distributia serverului

- [x] Implementeaza `CernealaLanguageServerProvider` ca adaptor subtire care porneste serverul bundled si ii transmite solution/workspace initialization data. (Trimite `solutionPath`, hostul Visual Studio, diagnostics push-only si telemetry dezactivata.)
- [x] Alege packaging self-contained sau runtime-bundled pe baza spike-ului, cu regula ferma ca end-userul nu instaleaza separat .NET pentru extensie. (Server `win-x64` self-contained; VSIX-ul si instalarea contin `coreclr.dll` byte-for-byte.)
- [x] Izoleaza fisierele serverului pe versiune si rezolva paths relativ la install root, nu la repo sau desktopul dezvoltatorului. (Runtime curat: `Extensions/.../Server/0.1.0/Cerneala.LanguageServer.exe`.)
- [x] Propaga cancellation la solution close, extension disable, update si Visual Studio shutdown; termina procesul fortat numai dupa timeout si log explicit. (Teste GREEN si shutdown runtime fara force-kill.)
- [x] Reporneste serverul dupa crash cu backoff limitat si dezactiveaza restart loop-ul dupa prag, afisand cauza in output channel. (Runtime: PID nou dupa crash, backoff 250 ms; missing binary s-a oprit dupa exact 3 incercari.)
- [x] Nu trimite telemetry ori continut de document; orice telemetry viitoare ramane opt-in si in afara acestui plan. (Initialization option `telemetryEnabled=false`; cele doua teste push/privacy sunt GREEN.)
- [x] Adauga tests pentru missing binary, startup failure, protocol failure, crash, restart, disable si uninstall. (21/21 teste VisualStudio GREEN.)
- [x] Reindexeaza solutia. (3.085 documente, 75.628 simboluri, zero erori noi.)

**Gate etapa 3**

- [x] Instalarea curata porneste serverul fara SDK/runtime separat si nu lasa procese dupa inchiderea Visual Studio. (`Stage3Final`: VSIX exact instalat, provider/server/coreclr cu hash identic artifactului, initialize/ready, exit 0 si zero procese ramase.)
- [x] Failure-ul serverului nu blocheaza editorul si poate fi diagnosticat din output/log API. (Proba missing-binary a pastrat documentul activ, a inchis Visual Studio normal si a scris path-ul/cauza/pragul in ActivityLog si output.)

### Etapa 4 - Integrare end-to-end in editor

- [x] Creeaza fixture consumer extern cu Cerneala package/source generator, custom controls, `DataContext`, resources, ItemsControl templates, Aspect, Motion si Prism. (Fixture-ul extern compileaza GREEN si acopera toate constructiile cerute.)
- [x] Automatizeaza Experimental Instance prin API-urile Visual Studio: deschide solutia, deschide `.crn`, tasteaza text, invoca completion, accepta itemul, navigheaza si salveaza. (Visual Studio Community ascuns, controlat prin DTE si API-urile editorului; 49/49 verificari GREEN.)
- [x] Verifica diagnostics si Error List pentru document valid, document invalid, editare incompleta si reparatie fara restart de IDE. (Valid zero erori, `CERNEALAUI002`, `CERNEALAUI001` si reparatie live demonstrate.)
- [x] Verifica completion/hover/signature help/go-to-definition/references/rename/formatting/code actions pe matricea obligatorie. (Toate capabilitatile au raspuns in hostul Community real.)
- [x] Verifica buffer nesalvat, undo/redo, paste mare, multi-caret daca hostul il aplica si editari simultane in doua documente. (Paste de 6.400/7.600 caractere printr-o operatie de editor, multi-caret pe doua selectii si doua documente simultane GREEN.)
- [x] Verifica project reload dupa adaugarea unui tip/proprietati C#, schimbarea `DataType`, package reference si target framework. (Reload CPS prin `IVsSolution4`, apoi IntelliSense si build `net9.0-windows` GREEN.)
- [x] Ruleaza aceeasi matrice pe `CernealaPresentation` si cere zero erori false pentru toate documentele `.crn` valide. (12/12 documente fara error tags; fisierele repository au ramas byte-for-byte neschimbate.)
- [x] Nu modifica direct buffer properties sau semantic state in scenariile user-like; foloseste comenzile/editor input APIs. (Testele structurale interzic mutatia directa si API-urile globale de input/clipboard.)
- [x] Reindexeaza solutia. (3.100 documente, 75.667 simboluri, 308.906 referinte, zero erori si doua avertismente cunoscute.)

**Gate etapa 4**

- [x] Un end-user poate scrie un view Cerneala complet numai cu IntelliSense, iar build-ul rezultat este GREEN. (`StackPanel` a fost selectat si acceptat din completion, iar fixture-ul final a compilat cu zero warnings/errors.)
- [x] Toate features functioneaza in Community SKU, nu doar in testele protocol-level. (Host `Community` 18.0: 49/49 verificari runtime si 24/24 teste VisualStudio GREEN.)

### Etapa 5 - Performanta si rezilienta Visual Studio

- [x] Masoara extension load, server cold start, first diagnostics, first completion, warm completion si solution reload pe fixture si `Cerneala.slnx`. (Raport raw si Markdown: `benchmarks/Cerneala.Benchmarks/results/2026-08-15-visual-studio-community-extension.*`.)
- [x] Impune lazy load si zero munca Cerneala la startup pentru solutii fara document deschis `.crn`. (Assembly, pachet si server absente inainte de deschiderea documentului.)
- [x] Confirma zero synchronous waits pe UI thread in provider si comenzi prin test/instrumentare Visual Studio. (29/29 teste VisualStudio GREEN; harness ascuns, API-only.)
- [x] Stabileste gate-uri pe hardware documentat: provider activation sub 100 ms CPU in devenv, server ready sub 2 s cold si first useful completion sub 2.5 s cold; bugetele warm raman cele din planul LSP. (Fixture: 15,625/833,196/1.357,454 ms; `Cerneala.slnx`: 0/964,291/1.922,658 ms; proba JSON-RPC full-solution a impus completion p95 sub 100 ms si diagnostics p95 sub 200 ms.)
- [x] Ruleaza soak cu 100 open/close cycles, 1.000 edits, restart server si solution close/reopen; verifica memory/process plateau. (Crestere second-half: devenv 4,68 MiB/0 MiB, server 0 MiB/0 MiB; restart si ambele reload-uri GREEN.)
- [x] Verifica comportamentul cu extensia disabled, server indisponibil si proiect cu build errors fara crash ori modal dialogs repetitive. (Toate probele ascunse GREEN, cu zero server in scenariile disabled/unavailable.)
- [x] Reindexeaza solutia. (3.104 documente, 75.773 simboluri, 309.188 referinte, zero erori si doua avertismente cunoscute.)

**Gate etapa 5**

- [x] Extensia nu produce freeze perceptibil in Visual Studio si nu mentine devenv/server alive dupa shutdown. (30 verificari in-process si cleanup pentru toate cele cinci PID-uri server observate.)
- [x] Masuratorile si hardware-ul sunt publicate impreuna cu rezultatele, nu doar declarate "pare rapid". (Community 18.9.12105.275, AMD EPYC 9354, 8 procesoare logice, 15,98 GiB RAM, Windows 10.0.26200.0.)

### Etapa 6 - Packaging, documentatie si release candidate

- [ ] Produce VSIX Release determinist care contine providerul, serverul, dependentele, grammar, language configuration, assets si license notices.
- [ ] Adauga smoke test care instaleaza VSIX-ul intr-un Experimental Instance curat, ruleaza scenariul minimal si il dezinstaleaza fara reziduuri.
- [ ] Verifica upgrade de la versiunea N la N+1, downgrade refuzat/gestionat si compatibilitatea setarilor.
- [ ] Semneaza artifactul conform politicii de release aleasa si genereaza checksum; nu publica in Marketplace in acest plan.
- [ ] Scrie `docs/visual-studio-community.md` cu instalare, update, uninstall, features, troubleshooting, logs, privacy si target versions.
- [ ] Actualizeaza documentatia markup astfel incat sa nu mai recomande XML-only tooling dupa lansarea extensiei.
- [ ] Actualizeaza API docs pentru orice membru public introdus si verifica `docs-site/documentation/manifest.json`.
- [ ] Ruleaza testele proiectelor Language, LanguageServer si VisualStudio, apoi `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.

**Gate etapa 6**

- [ ] Artifactul se instaleaza prin dublu-click/Extension Manager pe Visual Studio Community 2026 18.9 si nu cere pasi manuali suplimentari.
- [ ] Documentatia descrie exact capabilitatile testate si orice limitare ramasa.

## 5. Definitia de gata

- [ ] VSIX-ul ofera automat IntelliSense complet pentru `**/*.crn` si nu afecteaza fisiere XML ori alte tipuri de documente.
- [ ] Toate capabilitatile LSP obligatorii sunt validate end-to-end in Visual Studio Community.
- [ ] `CernealaPresentation` si fixture-ul extern au zero diagnostics false.
- [ ] Instalarea, update-ul, disable, restart si uninstall sunt curate si documentate.
- [ ] Extensia respecta bugetele de startup, typing si memory si nu blocheaza UI thread-ul.
