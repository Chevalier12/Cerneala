# Plan: Motion Studio

> Data: 2026-07-28
> Status: propus
> Scop: Adauga o aplicatie desktop Cerneala separata, numita Motion Studio, care permite unui utilizator sa construiasca si sa ruleze vizual componente Motion asupra unui singur shape, fara cod Motion scris de utilizator, Prism sau authoring Aspect.

## Viziune

Motion Studio este un demonstrativ si un instrument de explorare pentru API-ul imperativ Cerneala Motion. Canvas-ul contine exact un singur shape global. Utilizatorul creeaza componente numite, iar fiecare componenta descrie un arbore Motion editabil care tinteste acelasi shape. Componentele nu sunt layere vizuale si nu creeaza alte elemente in canvas.

Fluxul principal este:

1. utilizatorul configureaza shape-ul global;
2. creeaza sau selecteaza o componenta Motion;
3. construieste vizual animatii si compozitii pentru componenta;
4. apasa Play sau Replay;
5. Motion Studio traduce definitia editabila in apeluri reale Cerneala Motion;
6. acelasi shape global afiseaza rezultatul.

## Decizii de produs si arhitectura

- Canvas-ul contine exact un shape: `Rectangle`, `Ellipse` sau `Path` SVG.
- Panoul inspirat de Photoshop se numeste `COMPONENTS`, nu `LAYERS`.
- O componenta este un program Motion numit, nu un element vizual, control sau container.
- Toate componentele tintesc aceeasi instanta de shape din canvas.
- O singura componenta ruleaza la un moment dat. Play pe alta componenta anuleaza executia activa cu `KeepCurrent`, apoi porneste noua componenta. Aceasta regula evita conflicte invizibile intre doua programe editate separat.
- `Play` porneste din starea curenta a shape-ului. `Replay` anuleaza executia, restaureaza baseline-ul documentului si ruleaza componenta selectata. `Reset Target` restaureaza doar baseline-ul. `Stop` pastreaza valoarea vizuala curenta.
- In interiorul unei componente, operatiile pot rula secvential sau in paralel. Arborele Motion, nu o lista plata, este sursa de adevar.
- Modelul editabil al documentului ramane separat de `MotionHandle`, `MotionGroupHandle` si celelalte obiecte runtime.
- Executorul foloseste API-ul Cerneala Motion; aplicatia nu implementeaza un al doilea motor de interpolare sau un frame loop paralel.
- Front-end-ul expune toate capabilitatile Motion aplicabile unui singur element fix: proprietati animabile compatibile, From/To, Tween, Spring, Keyframes, Decay, Sequence, Parallel, start options, retarget mode, priority, hold-on-complete, cancel/complete si implicit transactions unde acestea au semantica utila.
- Nu se expun functionalitati care necesita alte tinte sau alt context: Stagger multi-target, layout transitions intre elemente, Presence, ScrollTimeline, Drag, Gestures sau animatii de colectii.
- `MotionStateBuilder` nu intra in UI pana cand API-ul sau public ofera comportament executabil, nu doar un facade gol.
- Nu se folosesc Prism, `PrismInstance`, filtre, styles, backdrops sau diagnostics Prism.
- Nu se definesc resurse, reguli, stari ori motion prin Aspect. Aplicatia nu inregistreaza un pachet Aspect propriu si nu foloseste directive `@when`, `@animate`, `@parallel` sau `@sequence`.
- Stilul Cerneala Presentation este reprodus local printr-o paleta C# si factory/helper-e mici pentru controale, fara dependenta de proiectul `CernealaPresentation`.
- Fereastra si arborele UI sunt construite in C#. Singurul markup permis este `App.crn` minimal necesar contractului actual de startup desktop; acesta nu contine UI, Aspect sau Motion.
- Prima versiune nu salveaza proiecte pe disc, nu exporta video/cod, nu implementeaza undo/redo si nu permite plugin-uri.

## Directia vizuala si layout

- Fereastra tinta: 1360x880, utilizabila pana la minimum 1100x720.
- Paleta locala urmeaza Presentation: ink `#0A0B0E`, panel `#14161B`, panel-alt `#101217`, raised `#191C22`, linii `#2A2E38`/`#424754`, paper `#EDEFF3`, slate `#8A93A6`, cyan `#4DF0FF`, pink `#FF3EA5`, lime `#C6FF3D`, orange `#FF8A3D`.
- `Cascadia Mono` este folosit pentru etichete, valori, status si diagnostics; textul general foloseste `Segoe UI Variable`.
- Structura principala:
  - bara superioara cu titlu, starea playback-ului, Reset Target, Stop, Play si Replay;
  - panou stanga `COMPONENTS`, cu creare, selectare, redenumire, duplicare, stergere si Play;
  - canvas central inchis, cu grid discret, bounds vizibile si shape-ul unic;
  - inspector dreapta contextual pentru shape sau nodul Motion selectat;
  - zona inferioara `MOTION TREE`, cu arborele componentei si actiuni de add/reorder/delete.
- Layout-ul preia densitatea si contrastul Presentation, dar nu copiaza turul, navigatia pe capitole sau continutul Prism Studio.

## Modelul documentului

- `MotionStudioDocument`
  - `TargetShapeDefinition Target`
  - `IList<MotionComponentDefinition> Components`
  - `Guid? SelectedComponentId`
- `TargetShapeDefinition`
  - tipul shape-ului;
  - geometria Path optionala;
  - width, height, fill, stroke si stroke thickness;
  - baseline pentru pozitie, translate, scale si opacity;
  - render transform origin daca API-ul shape-ului il permite fara schimbari speculative de framework.
- `MotionComponentDefinition`
  - id stabil;
  - nume valid si unic pentru afisare;
  - un singur `MotionNodeDefinition Root`.
- Noduri initiale:
  - `SequenceNodeDefinition`;
  - `ParallelNodeDefinition`;
  - `AnimationNodeDefinition`;
  - `SetValueNodeDefinition` doar pentru schimbari instantanee explicite;
  - `TransactionNodeDefinition` numai daca etapa de fezabilitate confirma o corespondenta clara cu `BeginTransaction`.
- `AnimationNodeDefinition`
  - proprietate selectata dintr-un catalog tipizat;
  - From optional si To obligatoriu;
  - spec: Tween, Spring, Keyframes sau Decay;
  - `MotionPropertyStartOptions`: retarget mode, priority, debug name si hold-on-complete.
- Keyframes au offset in `[0, 1]`, valoare tipizata, easing optional si hold; primul offset este 0, ultimul este 1, iar lista ramane sortata.

## Etape de implementare

### Etapa 0 - Contractul de capabilitati si probele de fezabilitate

- [ ] Inventariaza API-ul public Motion aplicabil unui singur `UIElement` si scrie o matrice interna `capabilitate -> control front-end -> API Cerneala -> test`.
- [ ] Confirma prin teste focalizate ca un shape atasat poate anima corect proprietatile de baza `TranslateX`, `TranslateY`, `Scale` si `Opacity` prin `MotionElementFacade`.
- [ ] Verifica proprietatile Shape care pot fi animate prin `Animate<T>` si mixerele inregistrate; nu afisa in catalog proprietati fara mixer sau fara semantica de randare corecta.
- [ ] Verifica secventierea, paralelismul, anularea, complete, retarget, priority si hold-on-complete pe o singura tinta.
- [ ] Verifica daca implicit transactions pot fi reprezentate predictibil de un nod vizual; elimina `TransactionNodeDefinition` din scope daca nu aduce o capabilitate distincta fata de Parallel.
- [ ] Adauga numai testele de regresie Cerneala necesare pentru defecte sau goluri reale descoperite de probe.
- [ ] Daca este necesara schimbarea unui API public Cerneala, actualizeaza in aceeasi etapa paginile corespunzatoare din `docs-site/documentation/classes/`; nu extinde framework-ul doar pentru comoditatea aplicatiei.

**Gate etapa 0**

- [ ] Matricea de capabilitati este completa, fiecare control planificat are o cale runtime verificata, iar scope-ul final nu promite functionalitati Motion incompatibile cu o singura tinta.

### Etapa 1 - Proiectul standalone si shell-ul programatic

- [ ] Adauga `MotionStudio/MotionStudio.csproj` ca aplicatie `net8.0-windows`/`WinExe`, cu referinte la `Cerneala.csproj` si generatorul Cerneala.
- [ ] Adauga proiectul in `Cerneala.slnx` si configureaza icon-ul si dependentele minime necesare.
- [ ] Adauga `MotionStudio/App.crn` exclusiv pentru `StartupWindow` si shutdown mode, fara resources, Aspect sau Motion markup.
- [ ] Adauga `MotionStudio/App.crn.cs` si `MotionStudio/MotionStudioWindow.cs`; construieste integral continutul ferestrei in C#.
- [ ] Adauga `MotionStudio/Visual/MotionStudioPalette.cs` si helper-e DRY pentru etichete, butoane, panouri si separatoare, fara un mini-framework de styling.
- [ ] Construieste layout-ul responsive cu header, Components, canvas, inspector si Motion Tree.
- [ ] Adauga un proiect de teste `tests/MotionStudio.Tests/MotionStudio.Tests.csproj` si include-l in solutie.

**Gate etapa 1**

- [ ] Motion Studio porneste ca aplicatie separata la dimensiunea implicita si minima, fara referinta la `CernealaPresentation`, Prism sau un pachet Aspect definit de aplicatie.

### Etapa 2 - Modelul editabil si comenzile front-end

- [ ] Implementeaza modelele documentului in `MotionStudio/Model/` ca obiecte independente de UI si runtime handles.
- [ ] Implementeaza catalogul tipizat de proprietati si specs in `MotionStudio/Motion/MotionCapabilityCatalog.cs`, alimentat explicit din matricea etapei 0.
- [ ] Implementeaza validarea pentru nume, valori finite, durate pozitive, parametri Spring/Decay, keyframes si structura arborelui.
- [ ] Implementeaza operatiile create/select/rename/duplicate/delete component si add/move/delete node ca servicii de model testabile.
- [ ] Defineste un document initial determinist cu un ellipse si trei componente demonstrative: `Entrance`, `Bounce` si `Exit`.
- [ ] Foloseste `ActionCommand` pentru toate actiunile UI si actualizeaza `CanExecute` pentru selectii, radacina invalida si playback activ.
- [ ] Adauga teste pentru mutatii, selectie, duplicare deep-copy, stergerea ultimei componente, validare si presetul initial.

**Gate etapa 2**

- [ ] Documentul si arborele Motion pot fi editate complet in teste fara a construi o fereastra sau a porni frame loop-ul.

### Etapa 3 - Compilatorul si sesiunea de playback Motion

- [ ] Implementeaza `MotionComponentCompiler`, care transforma recursiv definitiile in apeluri reale Motion si returneaza un handle agregat detinut de sesiune.
- [ ] Compileaza `Sequence` cu pornire lazy a fiecarui pas si `Parallel` cu toate animatiile copil pornite in acelasi frame logic.
- [ ] Compileaza Tween, Spring, Keyframes si Decay folosind specs publice Cerneala si valori tipizate din catalog.
- [ ] Aplica From numai cand este prezent, iar To, retarget mode, priority, debug name si hold-on-complete exact conform definitiei.
- [ ] Implementeaza `MotionPlaybackSession` cu starile Idle, Playing, Completed, Canceled si Faulted, fara a retine handle-uri terminale.
- [ ] Implementeaza politica exclusiva: Play pe alta componenta anuleaza sesiunea activa cu KeepCurrent.
- [ ] Implementeaza Play, Replay, Stop si Reset Target conform deciziilor planului.
- [ ] Propaga erorile de compilare/runtime in diagnostics UI fara a lasa noduri active sau shape-ul intr-o stare invalida.
- [ ] Adauga teste cu clock controlat pentru ordine, paralelism, valori finale, replay determinist, anulare, schimbarea componentei si disposal.

**Gate etapa 3**

- [ ] Preseturile si arborii sintetici ruleaza exclusiv prin Cerneala Motion, au rezultate deterministe si nu lasa handles/noduri active dupa finalizare sau anulare.

### Etapa 4 - Canvas-ul si inspectorul shape-ului global

- [ ] Construieste canvas-ul cu viewport centrat, clipping, grid discret si bounds pentru shape, fara Prism sau efecte de backdrop.
- [ ] Permite schimbarea intre Rectangle, Ellipse si Path pastrand o singura tinta atasata.
- [ ] Conecteaza inspectorul shape-ului la dimensiuni, fill, stroke, stroke thickness si baseline transform.
- [ ] La schimbarea tipului de shape, anuleaza playback-ul, inlocuieste tinta o singura data si reaplica baseline-ul.
- [ ] Valideaza Path SVG si pastreaza ultima geometrie valida cand textul introdus este invalid.
- [ ] Afiseaza coordonatele si valorile runtime curente separat de baseline, astfel incat Play sa nu rescrie documentul.
- [ ] Adauga teste pentru inlocuirea tintei, baseline/reset, input invalid, clipping si absenta copiilor vizuali suplimentari in canvas.

**Gate etapa 4**

- [ ] Canvas-ul are permanent exact un shape tinta, modificarile inspectorului sunt imediate, iar Reset Target restaureaza complet starea documentata.

### Etapa 5 - Components si editorul Motion Tree

- [ ] Construieste panoul Components cu selectie, create, rename, duplicate, delete, Play si indicator de stare per componenta.
- [ ] Construieste Motion Tree recursiv pentru Sequence, Parallel, Animation, Set Value si Transaction numai daca a ramas in scope.
- [ ] Permite adaugarea unui copil valid contextual, mutarea sus/jos, mutarea intre containere compatibile si stergerea fara a produce un arbore invalid.
- [ ] Construieste inspectorul contextual pentru nodurile de compozitie si animatie.
- [ ] Genereaza editori tipizati pentru float, color/brush si orice alt tip confirmat de catalog, fara string casting in executor.
- [ ] Construieste editorii Tween, Spring, Keyframes si Decay; keyframes suporta add, delete, reorder prin offset, easing si hold.
- [ ] Expune start options intr-o sectiune Advanced si afiseaza explicatii scurte pentru retarget, priority si hold-on-complete.
- [ ] Marcheaza vizual nodul activ si progresul componentei fara a face modelul documentului dependent de starea runtime.
- [ ] Adauga teste de comanda si view-model pentru toate editele si starile `CanExecute`.

**Gate etapa 5**

- [ ] Un utilizator poate construi din front-end o componenta cu animatii secventiale si paralele, o poate modifica si o poate rula fara a scrie cod sau markup.

### Etapa 6 - Diagnostics, lifecycle si accesibilitate

- [ ] Afiseaza componenta activa, starea sesiunii, nodul activ, timpul scurs, nodurile Motion active si ultimul motiv de anulare/eroare.
- [ ] Adauga Stop global, reset sigur si cleanup la inchiderea ferestrei.
- [ ] Asigura focus vizibil, ordine Tab coerenta, activare din tastatura si accessible names pentru comenzile icon-only.
- [ ] Adauga shortcut-uri documentate pentru Play/Replay, Stop, Reset Target, New Component si Delete Node folosind input commands Cerneala.
- [ ] Verifica schimbari repetate de componenta, shape si nod in timpul playback-ului pentru subscription leaks si handles abandonate.
- [ ] Adauga un mod de automatizare/captura determinist prin variabile de mediu, similar ca intentie cu Presentation, dar local proiectului Motion Studio.

**Gate etapa 6**

- [ ] Playback-ul repetat si inchiderea ferestrei nu lasa handles, callbacks sau subscriptions active, iar fluxul principal este utilizabil integral din tastatura.

### Etapa 7 - Verificare vizuala, performanta si documentatie

- [ ] Adauga smoke tests pentru startup, presetul initial, Play, Replay, Stop, schimbarea componentei si schimbarea shape-ului.
- [ ] Captureaza si inspecteaza vizual fereastra la 1360x880 si 1100x720 pentru clipping, overlap, contrast, focus si lizibilitate.
- [ ] Verifica frame budget-ul in timpul unei componente cu Sequence + Parallel + Keyframes si confirma lipsa layout invalidation per frame pentru transform/opacity.
- [ ] Ruleaza testele Motion tintite, `MotionStudio.Tests` si apoi `dotnet test .\Cerneala.slnx`.
- [ ] Ruleaza reindexarea si `doctor` prin RoslynRepoIndexer dupa modificarile finale.
- [ ] Actualizeaza documentatia publica Cerneala numai pentru API-uri publice schimbate in implementare; documentatia aplicatiei ramane un README scurt in `MotionStudio/README.md`.
- [ ] Ruleaza `git diff --check` si auditeaza dependentele proiectului pentru a confirma absenta Prism, `CernealaPresentation` si Aspect authoring.

**Gate etapa 7**

- [ ] Toate testele si verificarile sunt verzi, capturile sunt corecte la ambele dimensiuni, frame budget-ul este acceptabil si nu exista cod temporar sau dependente in afara scope-ului.

## Dependente intre etape

- Etapa 0 blocheaza catalogul, modelul animatiilor si executorul.
- Etapa 1 poate incepe dupa stabilirea contractului de startup si ramane separata de executor.
- Etapa 2 depinde de matricea etapei 0.
- Etapa 3 depinde de modelul etapei 2.
- Etapa 4 depinde de shell-ul etapei 1 si de contractul de baseline din etapa 2.
- Etapa 5 depinde de etapele 2-4.
- Etapa 6 depinde de playback-ul si UI-ul complete.
- Etapa 7 incepe numai dupa inchiderea tuturor gate-urilor anterioare.

## Non-obiective

- mai multe shape-uri simultan in canvas;
- layere vizuale sau z-order;
- import de imagini ori documente Photoshop;
- Prism si orice filtru/compositor Prism;
- Aspect authoring, Aspect motion sau copierea resurselor Presentation;
- markup Motion;
- scroll-linked, gesture, drag, presence, layout motion sau stagger multi-target;
- editor de cod, consola de scripting sau generare C# in prima versiune;
- persistenta, undo/redo, export video/GIF sau project files;
- modificarea semanticii Cerneala Motion pentru a acomoda un workaround de aplicatie.

## Definitia de gata

- [ ] Motion Studio este un proiect desktop standalone inclus in solutie si porneste independent de Presentation.
- [ ] Canvas-ul contine exact un shape global configurabil.
- [ ] Utilizatorul poate crea mai multe componente Motion, toate tintind acelasi shape.
- [ ] Fiecare componenta poate combina vizual animatii in Sequence si Parallel si poate folosi toate specs/options Motion confirmate ca aplicabile unei singure tinte.
- [ ] Play, Replay, Stop, Reset Target si schimbarea componentei au comportament determinist si testat.
- [ ] Utilizatorul nu scrie cod sau markup pentru a construi animatiile.
- [ ] Aplicatia nu depinde de Prism, CernealaPresentation sau Aspect authoring.
- [ ] Modelul editabil este separat de runtime handles si executorul foloseste motorul Cerneala Motion existent.
- [ ] Testele unitare, integration, lifecycle, smoke, suita completa si inspectia vizuala sunt verzi.
- [ ] Orice API public Cerneala schimbat are documentatia din `docs-site/documentation/classes/` sincronizata.
