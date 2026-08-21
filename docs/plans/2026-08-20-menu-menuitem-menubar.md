# Plan de implementare: Menu, MenuItem si MenuBar

Data: 2026-08-20

## Baseline verificat

- Cerneala nu are in prezent controale de meniu; `docs-site/wpf-compendium.js` enumera explicit meniurile printre controalele absente.
- `ItemsControl` detine deja colectiile `Items`/`ItemsSource`, generarea si reciclarea containerelor, `ItemTemplate`, `DisplayMemberPath`, `ItemsPanel` si contractul `PART_ItemsPresenter` folosit de controalele compozite.
- `Overlay` si `OverlayManager` proiecteaza continut in acelasi `UIRoot`, suporta mai multe overlay-uri simultan si ofera light-dismiss, dar placement-ul este doar vertical (`Auto`, `Bottom`, `Top`), iar dismiss-ul considera fiecare overlay independent.
- `ButtonBase` este singura implementare curenta pentru `IInputCommandSource`, `ICommandStateSource` si `IInputActivatable`; logica pentru `Command`, `CommandParameter`, observarea `CanExecuteChanged` si refresh-ul pe UI relay trebuie reutilizata, nu copiata in `MenuItem`.
- `ComboBox` este precedentul pentru template parts, sincronizarea unei proprietati publice cu `Overlay.IsOpen`, navigare din tastatura, inchidere la disable/detach si dezabonarea partilor cand template-ul este schimbat.
- Semantica accesibila existenta are roluri pentru liste si butoane, dar nu are roluri de meniu si nici o proprietate care sa exprime starea expanded/collapsed.
- Catalogul de markup, source generator-ul si serviciul de completari descopera API-ul public din referintele proiectului; noile controale trebuie totusi acoperite explicit ca sa nu repetam faza cu IntelliSense-ul mut ca pestele.

## Obiective

- Introducerea controalelor publice `Menu`, `MenuItem` si `MenuBar` cu suport pentru declarare in C# si markup Cerneala.
- Suport pentru meniuri verticale, bare de meniu orizontale si submeniuri imbricate la adancime arbitrara.
- Interactiuni complete pentru mouse si tastatura, inclusiv deschiderea, schimbarea ramurii active, activarea unei frunze, inchiderea intregii sesiuni si restaurarea focusului.
- Executarea comenzilor prin infrastructura Cerneala existenta, cu starea enabled sincronizata cu `CanExecute`.
- Randarea submeniurilor prin `Overlay`, fara clipping de catre parintele vizual si cu placement lateral limitat la viewport.
- Un contract de sesiune comun care poate fi reutilizat ulterior de `ContextMenu`.
- Template-uri implicite, stari stilizabile, semantica accesibila, completari IntelliSense si documentatie API completa.

## Non-obiective

- `ContextMenu` nu se implementeaza in acest plan; va fi un plan dependent care reutilizeaza `Menu`, `MenuItem` si sesiunea comuna.
- Nu se implementeaza in prima livrare separatoare dedicate, icon-uri dedicate, text pentru shortcut-uri, mnemonic/access keys, roluri radio, elemente checkable sau `StaysOpenOnClick`.
- Nu se implementeaza acceleratoare globale de tip `Alt` care deschid bara de meniu cand focusul este in alta parte.
- Nu se adauga ferestre native, popup-uri cu alt `UIRoot` sau workaround-uri in aplicatii.
- Nu se schimba semantica verticala existenta pentru `OverlayPlacement.Auto`, `Bottom` sau `Top`.
- Nu se introduce virtualizare speciala pentru meniuri; meniurile folosesc infrastructura standard `ItemsControl` si scroll doar cand viewport-ul o cere.

## Contract public propus

### `Menu`

- `public class Menu : ItemsControl`
- Reprezinta un meniu vertical reutilizabil si genereaza implicit containere `MenuItem` pentru date care nu sunt deja `MenuItem`.
- Elementele declarate direct si elementele din `ItemsSource` folosesc aceeasi politica de containerizare si acelasi model de interactiune.
- Template part obligatoriu: `PART_ItemsPresenter` (`ItemsPresenter`).

### `MenuBar`

- `public class MenuBar : Menu`
- Foloseste implicit un `StackPanel` orizontal si comportamentul de radacina specific unei bare de meniu.
- Cu o sesiune inchisa, click/`Enter`/`Space` pe un element parinte deschide ramura; cu sesiunea deschisa, hover sau `Left`/`Right` muta ramura activa fara a inchide meniul.

### `MenuItem`

- `public class MenuItem : ItemsControl, IInputCommandSource, ICommandStateSource, IInputActivatable`
- Proprietati publice: `object? Header`, `ICommand? Command`, `object? CommandParameter`, `bool IsSubmenuOpen`.
- Evenimente rutate publice: `Click`, `SubmenuOpened`, `SubmenuClosed`.
- Template parts obligatorii: `PART_HeaderPresenter` (`ContentPresenter`), `PART_SubmenuOverlay` (`Overlay`) si `PART_ItemsPresenter` (`ItemsPresenter`).
- Un element cu copii este parinte: activarea lui deschide submeniul si nu executa `Command` in aceasta versiune.
- Un element fara copii este frunza: activarea ridica `Click`, executa `Command` prin routerul existent si inchide intreaga sesiune numai dupa activarea valida.
- `IsSubmenuOpen` poate fi setat programatic, dar este fortat la `false` la disable, detach, schimbarea radacinii sau inchiderea sesiunii.
- Datele simple devin `MenuItem`, iar valoarea rezolvata prin `DisplayMemberPath` este folosita ca `Header`; un `MenuItem` furnizat direct nu este impachetat din nou.

### Extensii publice auxiliare

- `OverlayPlacement.AutoHorizontal` selecteaza dreapta cand continutul incape, altfel stanga, apoi clamp la viewport daca nu incape complet pe niciuna dintre parti.
- `SemanticsRole.Menu`, `SemanticsRole.MenuBar` si `SemanticsRole.MenuItem` descriu structura accesibila.
- `SemanticsProperty.IsExpanded` descrie starea unui `MenuItem` cu submeniu.

## Arhitectura tinta

- `Menu` ramane proprietarul colectiei si al containerelor de nivel curent; nu se foloseste `Selector`, deoarece un meniu are o cale deschisa si focus activ, nu selectie persistenta.
- `MenuItem` foloseste `ItemsControl` pentru propriii copii. Template-ul muta `PART_ItemsPresenter` in continutul unui `Overlay`, exact cum `ComboBox` proiecteaza lista fara a muta responsabilitatea de items in overlay.
- Un coordonator intern `MenuSession` este creat/detinut de radacina `Menu` sau `MenuBar`. El pastreaza calea deschisa, elementul focusat, overlay-urile participante si elementul la care trebuie restaurat focusul.
- `MenuSession` este singurul loc care decide tranzitiile pointer/tastatura si inchiderea unei ramuri. `Menu`, `MenuBar` si `MenuItem` expun evenimente/stari, dar nu mentin trei masini de stare paralele.
- `OverlayManager` primeste un contract intern de grup/dismiss scope, astfel incat toate overlay-urile unei sesiuni de meniu si tintele lor sa fie considerate un singur compozit pentru click exterior si schimbarea focusului. Dismiss-ul inchide sesiunea intreaga, nu doar ultimul submenu.
- Motorul de placement din `OverlayManager` este extins cu axa orizontala fara regresii pentru placement-ul vertical folosit de `ComboBox`, `ColorSwatch` si `ToolTip`.
- Logica de comanda din `ButtonBase` se extrage intr-un helper intern reutilizabil pentru abonare, refresh si executie; API-ul si comportamentul public al butoanelor raman neschimbate.
- Navigarea cauta numai containere realizate, vizibile si enabled; elementele disabled/collapsed sunt sarite, iar lipsa unei destinatii lasa focusul stabil.
- Submeniurile sunt verticale. `MenuBar` este singurul nivel orizontal implicit.

## Contract de interactiune

### Pointer

- Click pe un parinte deschide/inchide ramura lui conform starii sesiunii.
- Cand o sesiune `MenuBar` este deja deschisa, hover peste alt element radacina muta ramura activa.
- Hover peste un parinte dintr-un meniu vertical deschide submeniul lui si inchide ramura sora.
- Click pe o frunza enabled o activeaza o singura data si inchide sesiunea.
- Click in orice submenu, in orice tinta a caii deschise sau in bara radacina nu este tratat ca light-dismiss.
- Click in afara intregii sesiuni inchide toate submeniurile si restaureaza focusul la elementul radacina relevant.

### Tastatura

- `Up`/`Down` navigheaza meniurile verticale; `Home`/`End` aleg primul/ultimul element eligibil.
- In `MenuBar`, `Left`/`Right` navigheaza intre elementele radacina, cu wrap; `Down`, `Enter` si `Space` deschid ramura si focuseaza primul copil eligibil.
- Intr-un submenu, `Right` deschide parintele focusat si intra in primul copil; `Left` inchide nivelul curent si revine la parinte.
- `Enter`/`Space` activeaza o frunza sau deschide un parinte.
- `Escape` inchide nivelul curent; la nivelul radacina inchide sesiunea completa si restaureaza focusul.
- `Tab` inchide sesiunea si lasa navigarea normala de focus sa continue.

## Fisiere estimate

### Productie

- `UI/Controls/Menu.cs` - container vertical si integrarea cu sesiunea.
- `UI/Controls/MenuBar.cs` - radacina orizontala si politica de navigare.
- `UI/Controls/MenuItem.cs` - header, copii, comanda, stari si template parts.
- `UI/Controls/MenuTemplates.cs` - template-urile implicite pentru cele trei controale.
- `UI/Controls/MenuSession.cs` - coordonatorul intern pentru calea deschisa, focus si dismiss.
- `UI/Controls/OverlayPlacement.cs` - `AutoHorizontal`.
- `UI/Controls/OverlayManager.cs` - masurare/aranjare laterala si dismiss scope compozit.
- `UI/Controls/Primitives/ButtonBase.cs` - delegare catre helper-ul comun de command source.
- `UI/Input/CommandSourceState.cs` - helper intern reutilizabil pentru command state.
- `UI/Accessibility/AutomationPeer.cs` - factory order pentru tipurile derivate din `ItemsControl`.
- `UI/Accessibility/MenuAutomationPeer.cs` - rolul pentru `Menu`/`MenuBar`.
- `UI/Accessibility/MenuItemAutomationPeer.cs` - rol, nume si expanded state.
- `UI/Accessibility/SemanticsRole.cs` - rolurile noi.
- `UI/Accessibility/SemanticsProperty.cs` - `IsExpanded`.

### Teste

- `tests/Cerneala.Tests/Controls/MenuTests.cs`
- `tests/Cerneala.Tests/Controls/MenuItemTests.cs`
- `tests/Cerneala.Tests/Controls/OverlayTests.cs`
- `tests/Cerneala.Tests/Controls/ButtonTests.cs` sau testele existente echivalente pentru regresia helper-ului de comanda.
- `tests/Cerneala.Tests/UI/Accessibility/MenuSemanticsTests.cs`
- `tests/Cerneala.Tests/UI/Invalidation/MenuInvalidationTests.cs`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorMenuTests.cs`
- `tests/Cerneala.Tests.Language/CompletionTests.cs`
- `tests/Cerneala.Tests.LanguageServer/CompletionProtocolTests.cs`

### Documentatie

- `docs-site/documentation/classes/Cerneala.UI.Controls.Menu.md`
- `docs-site/documentation/classes/Cerneala.UI.Controls.MenuBar.md`
- `docs-site/documentation/classes/Cerneala.UI.Controls.MenuItem.md`
- `docs-site/documentation/classes/Cerneala.UI.Controls.OverlayPlacement.md`
- `docs-site/documentation/classes/Cerneala.UI.Accessibility.SemanticsRole.md`
- `docs-site/documentation/classes/Cerneala.UI.Accessibility.SemanticsProperty.md`
- `docs-site/documentation/manifest.json`
- `docs-site/wpf-compendium.js`

## Etapa 1 - Contracte RED si placement orizontal

- [x] Adauga teste RED in `OverlayTests` pentru `AutoHorizontal`: dreapta cand incape, fallback la stanga, clamp cand nici o parte nu incape, viewport mic si tinta mutata dupa deschidere.
- [x] Adauga teste RED care demonstreaza ca un grup de overlay-uri imbricate nu se inchide cand pointerul/focusul trece intre membrii grupului si se inchide complet la interactiune externa.
- [x] Adauga `OverlayPlacement.AutoHorizontal` fara a modifica rezultatele pentru `Auto`, `Bottom` si `Top`.
- [x] Extinde masurarea si aranjarea `OverlayManager` pe axa orizontala, inclusiv remeasure cu spatiul lateral ales.
- [x] Introdu contractul intern de dismiss scope/grup si pastreaza comportamentul existent pentru overlay-urile care nu apartin unui grup.
- [x] Acopera deschiderea/inchiderea mai multor overlay-uri, z-order, hit testing si invalidarea placement-ului dupa mutarea tintei.
- [x] Reindexeaza solutia dupa fiecare batch de modificari C#.

- [x] Poarta etapei:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~OverlayTests"
```

## Etapa 2 - Fundatia MenuItem si command source comun

- [x] Adauga teste RED pentru API-ul `MenuItem`, valorile implicite, containerizarea copiilor si folosirea `Header` pentru continutul vizual.
- [x] Extrage din `ButtonBase` logica reutilizabila de command state intr-un helper intern, fara schimbarea API-ului public sau a ordinii `Click`/command existente.
- [x] Adauga teste de regresie pentru `ButtonBase`: `CanExecuteChanged`, schimbarea comenzii, schimbarea parametrului, detach/reattach si executie o singura data.
- [x] Implementeaza `MenuItem` ca `ItemsControl` si reutilizeaza helper-ul de comanda prin interfetele de input existente.
- [x] Implementeaza contractul parinte/frunza: parintele deschide copii, frunza ridica `Click` si executa comanda o singura data.
- [x] Construieste template-ul implicit cu header presenter, indicator vizual pentru submenu si `ItemsPresenter` proiectat intr-un `Overlay` cu `AutoHorizontal`.
- [x] Sincronizeaza bidirectional `IsSubmenuOpen` cu overlay-ul efectiv si ridica exact o data `SubmenuOpened`/`SubmenuClosed`.
- [x] Inchide si curata starea la disable, detach, eliminarea copiilor si schimbarea template-ului.
- [x] Adauga teste obligatorii de template swap/detach care verifica dezabonarea partilor vechi, inchiderea overlay-ului vechi si absenta handlerelor duplicate dupa reattach.
- [x] Adauga teste pentru schimbari `Items`, `ItemsSource`, `DisplayMemberPath` si containere `MenuItem` furnizate direct.
- [x] Reindexeaza solutia dupa fiecare batch de modificari C#.

- [x] Poarta etapei:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MenuItemTests|FullyQualifiedName~Button"
```

## Etapa 3 - MenuSession, Menu si MenuBar

- [x] Adauga teste RED pentru un `Menu` vertical cu frunze, parinti si minimum trei niveluri de submeniuri.
- [x] Adauga teste RED pentru `MenuBar`: layout orizontal, deschidere initiala, schimbarea ramurii active prin hover si o singura ramura radacina deschisa.
- [x] Implementeaza `MenuSession` ca unic proprietar al caii deschise, focusului activ, overlay group-ului si inchiderii complete.
- [x] Implementeaza `Menu` cu container implicit `MenuItem`, panel vertical si template part `PART_ItemsPresenter`.
- [x] Implementeaza `MenuBar` peste contractul `Menu`, cu panel orizontal si politica specifica radacinii.
- [x] Implementeaza navigarea pointer fara activari duble intre handlerul containerului, routed events si `ElementInputBridge`.
- [x] Implementeaza navigarea completa din tastatura: `Up`, `Down`, `Left`, `Right`, `Home`, `End`, `Enter`, `Space`, `Escape`, `Tab` si wrap doar la nivelul `MenuBar`.
- [x] Sare elementele disabled/collapsed si pastreaza focusul stabil cand nu exista o destinatie eligibila.
- [x] Inchide ramurile surori cand se schimba calea activa si inchide toate overlay-urile la click extern, disable, detach sau pierderea radacinii.
- [x] Restaureaza focusul la elementul radacina dupa `Escape`/light-dismiss, dar permite lui `Tab` sa continue navigarea normala.
- [x] Adauga teste pentru mutarea unui `MenuItem` intre radacini, eliminarea unui element deschis si schimbarea `ItemsSource` in timpul unei sesiuni.
- [x] Reindexeaza solutia dupa fiecare batch de modificari C#.

- [x] Poarta etapei:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MenuTests|FullyQualifiedName~MenuItemTests|FullyQualifiedName~OverlayTests"
```

## Etapa 4 - Semantica, markup si IntelliSense

- [x] Extinde `SemanticsRole` si `SemanticsProperty` cu contractele publice propuse.
- [x] Adauga peer-uri dedicate si ordoneaza factory-ul astfel incat `Menu`, `MenuBar` si `MenuItem` sa nu cada pe peer-ul generic de `ItemsControl`.
- [x] Acopera numele accesibil din `Header`, item count, enabled/focused si `IsExpanded`, inclusiv actualizarea arborelui semantic la open/close.
- [x] Adauga teste source-generator care compileaza markup cu `MenuBar`, `Menu`, `MenuItem`, submeniuri imbricate, `Header`, `Command`, `CommandParameter`, binding-uri si elemente provenite din `ItemsSource`.
- [x] Verifica explicit ca elementele copil declarate in `Menu`/`MenuBar` ajung in `Items`, iar copiii unui `MenuItem` ajung in colectia lui, nu in `Header` sau in visual tree-ul template-ului.
- [x] Adauga teste de completare pentru numele celor trei controale si pentru proprietatile specifice `MenuItem` in `CompletionTests`.
- [x] Adauga un test de protocol LSP care cere `textDocument/completion` intr-un `MenuItem` si verifica sugestiile `Header`, `Command`, `CommandParameter` si `IsSubmenuOpen`.
- [x] Verifica completarea valorii enum pentru `OverlayPlacement.AutoHorizontal`.
- [x] Reindexeaza solutia dupa fiecare batch de modificari C#.

- [x] Poarta etapei:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MenuSemanticsTests"
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter "FullyQualifiedName~UiMarkupGeneratorMenuTests"
dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj --filter "FullyQualifiedName~CompletionTests"
dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj --filter "FullyQualifiedName~CompletionProtocolTests"
```

## Etapa 5 - Invalidation, verificare vizuala si documentatie

- [x] Adauga un test de invalidation care deschide si inchide un lant de submeniuri, proceseaza cadrul rezultat, apoi verifica faptul ca urmatorul cadru este idle si nu repeta measure/arrange/render fara schimbari.
- [x] Acopera mutarea tintei, resize-ul viewport-ului si inchiderea unei ramuri, verificand ca se invalideaza numai overlay-urile afectate.
- [x] Ruleaza un harness nativ cu o bara de meniu si cel putin doua niveluri de submenu aproape de marginea dreapta si de marginea de jos.
- [x] Captureaza verificarile vizuale exclusiv prin `Window.SaveScreenshot`; confirma lipsa clipping-ului, fallback-ul lateral, z-order-ul si faptul ca elementele nu se suprapun incoerent.
- [x] Documenteaza API-urile publice noi si modificate in `docs-site/documentation/classes/`, folosind skill-ul `writing-api-documentation`.
- [x] Actualizeaza `docs-site/documentation/manifest.json` pentru paginile noi si pastreaza ordinea/conventiile existente.
- [x] Actualizeaza `docs-site/wpf-compendium.js` astfel incat meniurile sa nu mai fie raportate ca absente si noteaza `ContextMenu` separat ca follow-up.
- [x] Include in documentatia `MenuItem` contractul parinte/frunza, comenzile de tastatura, lifecycle-ul overlay-ului si limitarile intentionate ale primei versiuni.
- [x] Ruleaza verificarea finala de whitespace si confirma ca nu exista task-uri bifate prematur in plan.

- [x] Poarta etapei:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MenuInvalidationTests|FullyQualifiedName~MenuTests|FullyQualifiedName~OverlayTests"
dotnet build .\Cerneala.slnx --no-restore
dotnet test .\Cerneala.slnx --no-build
git diff --check
```

## Ordine de executie si dependente

1. Etapa 1 stabileste primitivele de overlay necesare submeniurilor.
2. Etapa 2 livreaza elementul ierarhic si reutilizarea corecta a comenzilor.
3. Etapa 3 construieste radacinile si masina de interactiune peste primele doua etape.
4. Etapa 4 face controalele consumabile real din Cerneala markup, Visual Studio si accessibility.
5. Etapa 5 inchide riscurile de invalidation, vizual, documentatie si regresie completa.
6. Planul viitor pentru `ContextMenu` depinde de finalizarea tuturor celor cinci etape si trebuie sa reutilizeze `MenuSession`, nu sa creeze o a doua masina de stare.

## Definitia de done

- [x] `Menu`, `MenuItem` si `MenuBar` sunt API-uri publice compilabile din C# si markup Cerneala.
- [x] Meniurile suporta date simple, containere explicite si submeniuri imbricate fara clipping.
- [x] Pointerul, tastatura, focusul, comenzile si light-dismiss-ul respecta contractul din plan.
- [x] Toate overlay-urile unei sesiuni sunt tratate ca un singur compozit si sunt curatate la inchidere/detach.
- [x] `ButtonBase` nu are regresii dupa extragerea helper-ului de command source.
- [x] Template swap/detach nu lasa overlay-uri, handler-e sau abonari `CanExecuteChanged` agatate.
- [x] Semantica accesibila expune rolurile corecte si starea expanded.
- [x] Source generator-ul si IntelliSense-ul sugereaza/compileaza toate API-urile noi.
- [x] Un cadru idle dupa stabilizare nu contine lucru repetitiv produs de meniuri.
- [x] Documentatia API si manifestul sunt sincronizate, iar compendiul nu mai declara meniurile absente.
- [x] Build-ul si toate testele solutiei sunt verzi.
- [x] `ContextMenu` poate fi implementat ulterior ca host de overlay peste aceeasi fundatie, fara schimbarea contractelor de baza.
