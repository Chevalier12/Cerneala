# Plan: `App.cui.xml` si contractul declarativ de aplicatie

> Data: 2026-07-17
> Status: finalizat
> Scop: introducerea unei definitii declarative de aplicatie care detine startup-ul, resursele globale si lifecycle-ul, fara conventia hardcodata `MainWindow`.

## 1. Rezumat

Cerneala va suporta o pereche compilata:

```text
App.cui.xml
App.cui.xml.cs
```

cu o radacina `Application`, similara ca rol cu `App.xaml` din WPF si `App.axaml` din Avalonia:

```xml
<Application StartupWindow="WelcomeWindow"
             ShutdownMode="OnMainWindowClose">
    <Application.Resources>
        <SolidColorBrush
            Name="AccentBrush"
            Color="#FFB8FF2C" />
        <Tween
            Name="QuickTransition"
            Duration="150ms"
            Easing="EaseOut" />
        <Aspect
            Name="AppCaption"
            Target="TextBlock">
            @default
            {
                Foreground = $AccentBrush;
            }
        </Aspect>
    </Application.Resources>
</Application>
```

Companionul C# devine un application object real, nu hook-ul static special pe care il foloseste generatorul in prezent:

```csharp
namespace Cerneala.Presentation;

public partial class App : Application
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Serviciile aplicatiei.
    }
}
```

Generatorul va rezolva `StartupWindow` semantic, prin Roslyn, la orice tip concret derivat din `Window`. `WelcomeWindow` este numai numele ales de aplicatie in exemplu; nu exista niciun nume de clasa rezervat. Generatorul va genera entry point-ul si descriptorul de hosting din `Application`, nu dintr-o clasa numita magic `MainWindow`.

Inspiratia functionala este deliberat restransa la contractele utile Cerneala:

- WPF foloseste `Application` pentru startup, main window, shutdown si resurse application-scope:
  <https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/application-management-overview>
- WPF permite alegerea declarativa a ferestrei initiale prin `StartupUri`:
  <https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.startupuri>
- Avalonia foloseste `App.axaml` pentru resurse globale si `App.axaml.cs` pentru alegerea main window-ului dupa initializarea framework-ului:
  <https://docs.avaloniaui.net/docs/fundamentals/main-window>
- Avalonia separa explicit lifetime-ul desktop si politicile de shutdown:
  <https://docs.avaloniaui.net/docs/fundamentals/application-lifetimes>

Nu copiem URI loading-ul WPF. Cerneala are markup compilat si poate oferi un contract mai sigur: `StartupWindow="WelcomeWindow"` este un nume de tip rezolvat la build, nu o cale interpretata la runtime.

`<Application>`, `StartupWindow` si `ShutdownMode` sunt sintaxa noua introdusa de acest plan. Declararea resurselor nu primeste o a doua limba inventata: foloseste exact property-element-ul si elementele de resursa acceptate deja de generator (`<Owner.Resources>`, `SolidColorBrush`, `Aspect`, `Tween`, `Spring` si, unde exista namescope vizual, `MotionClip`).

## 2. Baseline si problema actuala

### 2.1 Startup-ul este legat de numele `MainWindow`

`UiMarkupGenerator` numara tipurile numite exact `MainWindow` si seteaza:

```csharp
bool generateStartup =
    mainWindowCount == 1 &&
    windowPair.Pair.TypeSymbol.Name == "MainWindow";
```

`UiMarkupWindowGenerator` emite apoi fie un `Main()`, fie un module initializer pentru hosting. Aplicatia nu declara explicit ce fereastra porneste; numele clasei decide asta accidental.

### 2.2 `App` nu este application object

Generatorul cauta optional o clasa statica `App` cu un singur:

```csharp
static void ConfigureServices(IServiceCollection services)
```

Nu exista:

- `Application.Current`;
- `Application.Resources`;
- `MainWindow`, `Windows` sau `ActiveWindow` la nivel de aplicatie;
- evenimente/override-uri de startup si exit;
- politica declarativa de shutdown;
- o pereche markup/code-behind pentru aplicatie.

### 2.3 Runtime-ul codifica o singura politica de shutdown

`WindowApplicationRuntime.Close` inchide toate celelalte ferestre cand se inchide `mainWindow`. Comportamentul echivaleaza cu `OnMainWindowClose`, dar nu este configurabil si nu apartine unui obiect `Application`.

### 2.4 Resursele nu au scope de aplicatie

`WindowApplicationRuntime` poate instala acelasi `IResourceProvider` in fiecare `UIRoot`, iar `UIRoot` stie deja sa reactioneze la schimbari de resurse. Totusi, startup-ul generat nu construieste un resource provider din markup-ul aplicatiei.

Rezultatul este ca resursele comune trebuie tinute intr-un owner vizual, repetate sau furnizate manual din hosting. Asta devine o piedica reala cand ferestrele si view-urile sunt impartite in componente.

## 3. Decizii fixate

- Tipul public va fi `Cerneala.UI.Application`.
- Companionul `App.cui.xml.cs` va declara o clasa partiala, concreta, derivata din `Application`.
- `App.cui.xml` va folosi radacina `<Application>`.
- Un executabil poate avea cel mult o definitie `Application` paired.
- Entry point-ul generat va apartine definitiei `Application`, nu ferestrei numite `MainWindow`.
- Sintaxa initiala va fi `StartupWindow="<type-name>"`, nu `StartupUri`.
- `StartupWindow` va fi rezolvat semantic in scope-ul companionului C# si trebuie sa indice un tip concret, accesibil, derivat din `Window`.
- `MainWindow` nu va fi un nume de tip rezervat; orice fereastra indicata de `StartupWindow` devine initial valoarea proprietatii runtime `Application.MainWindow`.
- `ShutdownMode` va accepta `OnLastWindowClose`, `OnMainWindowClose` si `OnExplicitShutdown`.
- Valoarea implicita pentru aplicatiile cu `App.cui.xml` va fi `OnLastWindowClose`, la fel ca modelele desktop consacrate.
- `Application.Resources` va fi acelasi provider observabil instalat drept application-scope in toate ferestrele aplicatiei.
- Resursele locale de element, `UserControl` si `Window` vor continua sa aiba prioritate fata de resursele aplicatiei.
- `<Application.Resources>` va accepta aceleasi declaratii existente potrivite scope-ului de aplicatie: brush-uri, `<Aspect>`, `<Tween>` si `<Spring>`.
- Motion clips care refera elemente prin nume nu vor fi legale in `Application`, deoarece aplicatia nu are visual tree sau namescope.
- `@when`, `@if`, `@set`, `@animate`, copii vizuali, `Name`, `DataType` si event handlers de element nu vor fi legale direct pe `Application`.
- `ConfigureServices`, startup-ul si exit-ul vor fi override-uri de instanta; vechiul hook static ramane numai in fallback-ul legacy fara `App.cui.xml`.
- In absenta unei definitii `Application`, conventia existenta `MainWindow` va ramane temporar functionala pentru compatibilitate.
- Daca exista `App.cui.xml`, fallback-ul `MainWindow` este dezactivat complet; nu se emit doi descriptori si doua entry point-uri.
- Hosted mode-ul existent ramane suportat: host-ul extern pompeaza runtime-ul, dar `Application` si service provider-ul au acelasi lifecycle ca in standalone.

## 4. Contract public propus

### 4.1 `Application`

Contractul public minim:

```text
Application
|- static Current
|- Resources
|- Services
|- MainWindow
|- Windows
|- ActiveWindow
|- ShutdownMode
|- Shutdown()
|- Shutdown(exitCode)
|- Startup
|- Exit
|- protected ConfigureServices(...)
|- protected OnStartup(...)
|- protected OnExit(...)
```

Semantica:

- `Current` este setat o singura data pe thread-ul UI inainte de `ConfigureServices` si este resetat dupa terminarea completa a aplicatiei.
- `Resources` exista imediat dupa constructie si este populat de codul generat inainte de crearea primei ferestre.
- `Services` devine disponibil dupa construirea provider-ului si inainte de `OnStartup`.
- `Windows` este o vedere read-only asupra ferestrelor cunoscute runtime-ului.
- `MainWindow` poate fi citit si schimbat pe thread-ul UI; schimbarea lui nu inchide automat fereastra anterioara.
- `Shutdown(int)` inchide fortat toate ferestrele, ridica `Exit` exact o data si stabileste exit code-ul procesului.
- accesul cross-thread la operatiile de lifecycle arunca aceeasi exceptie descriptiva folosita de Window APIs.

### 4.2 Lifecycle

Ordinea obligatorie:

```text
construct App
-> initialize App markup and Resources
-> install Application.Current
-> ConfigureServices
-> build and publish IServiceProvider
-> OnStartup / Startup
-> resolve StartupWindow from DI
-> assign MainWindow
-> show MainWindow
-> run/pump
-> shutdown condition or explicit Shutdown
-> close remaining windows
-> OnExit / Exit
-> dispose IServiceProvider
-> clear Application.Current
```

`OnStartup` ruleaza inainte de instantierea declarativa a `StartupWindow`, similar contractului WPF pentru `StartupUri`. Daca startup-ul cere explicit shutdown sau esueaza, fereastra declarativa nu este creata.

### 4.3 Shutdown

- `OnLastWindowClose`: aplicatia se opreste dupa inchiderea cu succes a ultimei ferestre.
- `OnMainWindowClose`: aplicatia se opreste dupa inchiderea cu succes a ferestrei referite curent de `MainWindow`.
- `OnExplicitShutdown`: inchiderea ferestrelor nu opreste automat aplicatia; numai `Shutdown` sau oprirea host-ului o face.
- anularea evenimentului `Closing` nu declanseaza shutdown si nu ridica `Exit`.
- inchiderea unei ferestre owned continua sa urmeze contractul actual.
- `Exit` se ridica o singura data inclusiv cand startup-ul esueaza dupa instalarea aplicatiei.

### 4.4 Resurse application-scope

Lookup-ul ramane apropiat-catre-departat:

```text
element
-> ancestors / control / window
-> Application.Resources
-> theme/default provider
```

Schimbarea unei resurse observabile din `Application.Resources` invalideaza numai consumatorii dependenti din toate `UIRoot`-urile atasate. Ferestrele deschise ulterior vad ultima valoare.

Aspectele application-scope se aplica tuturor ferestrelor, dar aspectele mai apropiate pastreaza precedenta actuala. Motion specs globale sunt definitii reutilizabile; executia Motion ramane detinuta de `UIRoot`-ul ferestrei consumatoare.

## 5. Non-obiective

- navigation framework, router sau `Page`;
- suport Linux/macOS ori lifetimes Avalonia-style pentru mobile/browser;
- pack URI, runtime XML loading sau alegerea startup-ului prin cale de fisier;
- merged resource dictionaries si includes in aceasta etapa;
- hot reload pentru `App.cui.xml`;
- splash screen;
- session activation, protocol activation sau file activation;
- handler global pentru exceptii neprocesate;
- mai multe instante `Application` in acelasi proces/thread UI;
- impartirea `PresentationWindow.cui.xml` in capitole; aceasta va fi un plan separat;
- transformarea tuturor API-urilor interne de hosting in API public.

## 6. Fisiere estimate

### Productie

- `UI/Application.cs` - noul application object public.
- `UI/ApplicationShutdownMode.cs` - politica publica de shutdown.
- `UI/ApplicationStartupEventArgs.cs` si `UI/ApplicationExitEventArgs.cs` - lifecycle public, daca argumentele nu pot fi tinute simplu fara tipuri dedicate.
- `UI/Hosting/Windows/GeneratedWindowApplication.cs` - descriptor bazat pe application factory si lifecycle complet.
- `UI/Hosting/Windows/WindowApplicationRuntime.cs` - delegarea main-window/shutdown catre `Application`.
- `UI/Elements/UIRoot.cs` si/sau resource-provider composition - numai daca precedenta application/theme nu poate fi exprimata prin contractul existent.
- `Cerneala.SourceGen/UiMarkupApplicationGenerator.cs` - pairing, validare si emitere pentru `<Application>`.
- `Cerneala.SourceGen/UiMarkupGenerator.cs` - catalog compilation-wide si selectia unica a startup-ului.
- `Cerneala.SourceGen/UiMarkupWindowGenerator.cs` - eliminarea responsabilitatii primare de entry point si pastrarea fallback-ului legacy.

### Teste si integrare

- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorApplicationTests.cs`.
- `tests/Cerneala.Tests/UI/Hosting/ApplicationRuntimeTests.cs`.
- fixture-uri fake pentru platforma Window, extinse numai unde lifecycle-ul nou o cere.
- `CernealaPresentation/App.cui.xml`.
- `CernealaPresentation/App.cui.xml.cs`.

### Documentatie

- pagini noi/actualizate in `docs-site/documentation/classes/`.
- `docs-site/documentation/manifest.json`.
- `docs/getting-started.md`.
- documentatia markup pentru application definition, intr-o pagina existenta potrivita sau intr-un document nou orientat pe authoring.

Lista este estimativa. Nu se creeaza abstractions decorative daca implementarea poate reutiliza curat `ResourceDictionary`, `IObservableResourceProvider`, `WindowApplicationRuntime` si descriptorul existent.

## 7. Etape de implementare

### Etapa 0 - Baseline RED si contracte de compatibilitate

- [x] Adauga teste SourceGen RED care demonstreaza ca `<Application StartupWindow="ShellWindow">` paired cu `App : Application` nu este recunoscut in baseline.
- [x] Adauga un test RED care cere ca entry point-ul sa fie emis din definitia `Application` chiar daca startup window nu se numeste `MainWindow`.
- [x] Adauga teste RED pentru standalone si hosted mode, pastrand diferenta actuala dintre `Main()` si module initializer.
- [x] Caracterizeaza prin teste comportamentul legacy: un executabil fara `App.cui.xml`, cu exact un `MainWindow`, continua sa emita startup-ul actual.
- [x] Adauga teste RED pentru aplicatie duplicata, companion lipsa, radacina gresita, tip de baza gresit si constructor declarat de utilizator.
- [x] Adauga teste RED pentru `StartupWindow` lipsa, necunoscut, ambiguu, inaccesibil, abstract, non-`Window` si referinta la `Application` insasi.
- [x] Adauga teste runtime RED pentru cele trei shutdown modes, anularea `Closing`, exit code si `Exit` exact o data.
- [x] Adauga un test RED pentru ordinea completa a lifecycle-ului si disposal-ul provider-ului.
- [x] Reindexeaza `Cerneala.slnx` dupa modificarile de teste.

**Gate etapa 0**

- [x] Testele noi esueaza exclusiv din cauza contractelor `Application` lipsa, iar testele legacy existente raman verzi.
- [x] Sintaxa si ordinea lifecycle-ului din teste coincid cu deciziile fixate in acest plan.

### Etapa 1 - Application object si ownership-ul lifecycle-ului

- [x] Introdu `Cerneala.UI.Application` cu `Current`, `Resources`, `Services`, `MainWindow`, `Windows`, `ActiveWindow`, `ShutdownMode`, lifecycle si thread affinity.
- [x] Introdu enum-ul `ApplicationShutdownMode` cu exact cele trei valori stabilite.
- [x] Introdu argumentele de startup/exit numai daca sunt necesare pentru command-line args si exit code; nu crea o ierarhie de evenimente inutila.
- [x] Muta decizia de shutdown din `WindowApplicationRuntime.Close` in politica aplicatiei curente.
- [x] Pastreaza ownership-ul ferestrelor si al contextelor native in `WindowApplicationRuntime`; `Application` orchestreaza, nu dubleaza dictionarele runtime-ului.
- [x] Permite schimbarea `MainWindow` fara inchiderea ferestrei vechi si aplica `OnMainWindowClose` ferestrei desemnate la momentul inchiderii.
- [x] Fa `Shutdown` idempotent si sigur daca este apelat din `OnStartup`, dintr-un event handler sau dupa inchiderea tuturor ferestrelor.
- [x] Asigura cleanup determinist dupa startup partial esuat: ferestre, runtime, services si `Application.Current`.
- [x] Extinde testele runtime pentru standalone si hosted disposal, inclusiv oprire repetata si reset intre teste.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 1**

- [x] Toate testele `ApplicationRuntimeTests` si `WindowRuntimeTests` sunt verzi.
- [x] Nu exista doua surse de adevar pentru `MainWindow`, lista de ferestre sau shutdown.
- [x] Inchiderea anulata nu produce `Exit`, disposal sau inchiderea celorlalte ferestre.

### Etapa 2 - Generatorul pentru `App.cui.xml`

- [x] Adauga pairing pentru un document cu radacina `<Application>` si companion C# partial derivat din `Application`.
- [x] Cere cel mult o definitie `Application` intr-un output executabil si emite diagnostic precis pentru duplicate.
- [x] Rezolva `StartupWindow` cu Roslyn in scope-ul companionului, fara reflection si fara runtime type lookup.
- [x] Accepta numele simplu importat si numele complet calificat conform regulilor Roslyn deja folosite de custom elements/DataType.
- [x] Leaga startup window-ul de perechea sa markup si genereaza inregistrarile DI necesare pentru `Window<TViewModel>` si view model.
- [x] Emite constructorul `App` care initializeaza markup-ul, dar pastreaza extensibilitatea prin override-urile protejate ale clasei de baza.
- [x] Muta `Main()`/module initializer-ul in source-ul generat pentru `Application`.
- [x] Inlocuieste descriptorul centrat pe `createMainWindow` cu un descriptor centrat pe `createApplication` plus factory-ul startup window-ului.
- [x] Dezactiveaza startup generation din `MainWindow` cand exista o definitie `Application`.
- [x] Pastreaza fallback-ul legacy numai cand nu exista niciun `<Application>` paired.
- [x] Pastreaza hook-ul static `App.ConfigureServices` numai in fallback-ul legacy si emite diagnostic clar daca este amestecat cu noul `App : Application`.
- [x] Adauga diagnostice pentru atribute/directive/copii ilegali pe `<Application>`.
- [x] Verifica output-ul determinist al incremental generatorului indiferent de ordinea `AdditionalFiles`.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 2**

- [x] Toate cazurile SourceGen RED din etapa 0 sunt verzi.
- [x] Un executable cu `App.cui.xml` si o fereastra `ShellWindow` genereaza exact un entry point si porneste `ShellWindow`.
- [x] Un proiect legacy fara App continua sa genereze exact startup-ul anterior.
- [x] Nicio cale de startup noua nu foloseste reflection, `Activator.CreateInstance` sau nume de fisier interpretat la runtime.

### Etapa 3 - Resurse globale si precedenta cross-window

- [x] Expune `Application.Resources` prin `ResourceDictionary`/`IObservableResourceProvider`, reutilizand infrastructura observabila existenta.
- [x] Reutilizeaza sintaxa existenta `<Application.Resources>` si elementele existente de brush/`Aspect`/`Tween`/`Spring`; nu introduce directive paralele precum `@resources`, `@brush` sau `@aspect`.
- [x] Extinde parserul/emitterul pentru ca declaratiile valide din `<Application.Resources>` sa populeze resursele aplicatiei.
- [x] Adauga suport compilation-wide pentru referirea din Window/UserControl la resursele declarate in App, cu diagnostice compile-time pentru nume sau tip incompatibil.
- [x] Permite `<Tween>` si `<Spring>` application-scope prin aceeasi gramatica si aceleasi validari folosite deja in `Window.Resources`/`UserControl.Resources`.
- [x] Respinge Motion clips application-scope care au tinte nominale sau assignments catre elemente.
- [x] Instaleaza providerul aplicatiei in fiecare `UIRoot` creat de `WindowApplicationRuntime`.
- [x] Pastreaza ordinea de lookup local -> application -> theme/default si documenteaza explicit shadowing-ul.
- [x] Adauga teste cu doua ferestre care consuma aceeasi resursa application-scope.
- [x] Adauga teste care modifica o resursa globala si demonstreaza invalidarea ambelor ferestre, fara lucru repetat pe consumatori neafectati.
- [x] Adauga teste pentru shadowing local si pentru o fereastra deschisa dupa schimbarea resursei.
- [x] Adauga idle-frame regression: dupa stabilizarea unei schimbari globale, frame-urile idle nu mai contin layout/render work.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 3**

- [x] Resursele App sunt vizibile in toate ferestrele si sunt suprascrise corect de scope-uri mai apropiate.
- [x] O actualizare globala invalideaza numai dependentele reale si converge inapoi la idle.
- [x] Generatorul respinge toate formele de Motion application-scope care ar necesita un visual tree global.

### Etapa 4 - Lifecycle standalone si hosted end-to-end

- [x] Adapteaza `GeneratedWindowApplication.Run` sa creeze, instaleze si inchida `Application` in ordinea fixata.
- [x] Adapteaza `RegisterStartup`, `PumpHosted` si `StopHosted` astfel incat host-ul extern sa ramana proprietarul pump-ului, iar App/services sa fie create o singura data.
- [x] Propaga argumentele procesului catre startup in standalone; defineste explicit argumentele disponibile in hosted mode.
- [x] Propaga exit code-ul din `Application.Shutdown(int)` catre entry point-ul standalone.
- [x] Verifica startup failure in fiecare punct: construct App, init resources, ConfigureServices, build provider, OnStartup, resolve Window, create Window, first Show.
- [x] Asigura `OnExit`/`Exit` exact o data pentru toate esecurile aparute dupa instalarea `Application.Current`.
- [x] Pastreaza exceptia originala si adauga context descriptiv pentru startup target si etapa esuata.
- [x] Adauga teste de integrare cu fake platform pentru main window, secondary window, hosted pump si shutdown modes.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 4**

- [x] Standalone returneaza exit code-ul cerut si nu lasa `Application.Current`, services sau runtime instalate.
- [x] Hosted mode creeaza App si startup window o singura data, chiar dupa pump-uri repetate.
- [x] O exceptie de startup nu lasa ferestre native, provider sau stare statica agatata.

### Etapa 5 - Migrarea `CernealaPresentation`

- [x] Adauga `CernealaPresentation/App.cui.xml` cu `StartupWindow="MainWindow"` si shutdown mode explicit.
- [x] Adauga `CernealaPresentation/App.cui.xml.cs` derivat din `Application`.
- [x] Muta orice configurare DI application-scope din vechiul hook static in override-ul de instanta, daca exista.
- [x] Muta in App numai resursele consumate real de mai multe ferestre; nu goli mecanic Window resources locale.
- [x] Demonstreaza ca `MainWindow`, `PresentationWindow` si `MotionLabWindow` vad aceleasi resurse globale unde este intentionat.
- [x] Pastreaza comportamentul Continue, deschiderea ferestrelor secundare si automation/benchmark startup.
- [x] Verifica faptul ca Presentation are exact un descriptor generat si niciun entry point derivat din conventia `MainWindow`.
- [x] Ruleaza Presentation nativ si verifica startup, close, explicit secondary windows si proces exit.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 5**

- [x] Presentation porneste din declaratia `App.cui.xml`, nu din numele clasei `MainWindow`.
- [x] Toate cele trei ferestre functioneaza, iar inchiderea urmeaza `ShutdownMode` declarat.
- [x] Benchmarkul Presentation existent poate porni si opri aplicatia fara schimbari fragile de timing sau automation.

### Etapa 6 - API docs, authoring docs si compatibilitate finala

- [x] Foloseste skillul `writing-api-documentation` pentru toate tipurile si membrii publici noi/schimbati.
- [x] Adauga/actualizeaza paginile din `docs-site/documentation/classes/` pentru `Application`, shutdown mode, event args si descriptorul de startup.
- [x] Actualizeaza `docs-site/documentation/manifest.json` pentru fiecare pagina noua sau redenumita.
- [x] Actualizeaza `docs/getting-started.md` cu perechea standard App/Window si elimina conventia `MainWindow` din fluxul recomandat.
- [x] Documenteaza sintaxa `<Application>`, `StartupWindow`, `ShutdownMode`, resursele globale, shadowing-ul si directivele interzise.
- [x] Documenteaza fallback-ul legacy ca mecanism de compatibilitate, nu ca stil recomandat.
- [x] Adauga un exemplu minimal complet care compileaza fara `Program.cs`.
- [x] Ruleaza un public API diff si confirma ca toate adaugarile sunt intentionate, documentate si nullable corect.
- [x] Reindexeaza `Cerneala.slnx`.

**Gate etapa 6**

- [x] Documentatia descrie exact comportamentul testat si nu promite URI loading, merged dictionaries sau lifetimes neimplementate.
- [x] Manifestul docs este sincronizat si public API diff-ul nu contine schimbari accidentale.

### Etapa 7 - Verificare completa

- [x] Ruleaza testele SourceGen tintite.
- [x] Ruleaza testele runtime tintite pentru Application/Window/resources.
- [x] Ruleaza intreaga solutie.
- [x] Ruleaza build Release si formatter verification.
- [x] Ruleaza Presentation native smoke si benchmarkul permanent de frame budget pentru a detecta regresii de startup/resource wiring.
- [x] Ruleaza `git diff --check`.
- [x] Regenereaza `FileTree.md`.
- [x] Reindexeaza final `Cerneala.slnx` si verifica `doctor/status`.

**Gate etapa 7**

- [x] Toate comenzile finale sunt verzi.
- [x] Nu exista procese Presentation, host-uri, HWND-uri sau servicii ramase dupa teste.
- [x] Startup-ul App nu introduce frame-uri peste buget in benchmarkul Presentation.

## 8. Comenzi de verificare

```powershell
dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ApplicationRuntimeTests|FullyQualifiedName~WindowRuntimeTests|FullyQualifiedName~Resource"
dotnet test .\Cerneala.slnx
dotnet build .\Cerneala.slnx -c Release
dotnet format .\Cerneala.slnx --verify-no-changes
dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667
git diff --check
.\Tools\scripts\New-FileTree.ps1
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- doctor --json
```

## 9. Ordinea recomandata

1. Ingheata contractele in teste RED.
2. Introdu application object-ul si scoate shutdown-ul din logica hardcodata a main window-ului.
3. Genereaza App pairing si startup type-safe.
4. Leaga resursele application-scope de toate `UIRoot`-urile.
5. Inchide lifecycle-ul standalone/hosted si toate failure paths.
6. Migreaza Presentation ca primul consumator real.
7. Actualizeaza API docs, authoring docs si ruleaza verificarea completa.

## 10. Stop conditions

- Nu adauga `StartupUri` sau runtime markup loading doar pentru familiaritate cu WPF.
- Nu extinde planul la navigation/pages.
- Nu implementa merged dictionaries pana cand application resources simple nu sunt complete si testate.
- Nu face `Application` proprietarul rendererului, input-ului sau contextelor native; acestea raman la `WindowApplicationRuntime`.
- Nu pastra doua cai active de startup cand exista App markup.
- Nu relaxa diagnosticele pentru a permite Motion clips globale fara namescope.
- Nu sparge `PresentationWindow.cui.xml` in acest plan; App face posibila organizarea resurselor, dar componentizarea este o livrare separata.
- Nu elimina fallback-ul legacy pana cand toate sample-urile si consumatorii repo-ului au migrat si o schimbare breaking este aprobata separat.

## 11. Definitia de gata

- [x] Un proiect executabil poate declara `App.cui.xml` + `App.cui.xml.cs` si nu are nevoie de `Program.cs`.
- [x] `StartupWindow` poate indica orice tip concret `Window` valid, indiferent de numele lui.
- [x] Generatorul emite exact un entry point/descriptor din App si nu mai depinde de numele `MainWindow`.
- [x] `Application.Current`, `Resources`, `Services`, `MainWindow`, `Windows`, `ActiveWindow`, lifecycle-ul si shutdown-ul functioneaza conform contractului.
- [x] Resursele declarate in App sunt vizibile si observabile in toate ferestrele, cu precedenta locala corecta.
- [x] Cele trei shutdown modes sunt testate inclusiv pentru close anulat, main window inlocuit si explicit exit code.
- [x] Standalone si hosted mode au cleanup complet si evenimente ridicate exact o data.
- [x] Proiectele legacy fara App continua sa functioneze prin fallback-ul documentat.
- [x] `CernealaPresentation` foloseste noul App pair si porneste `MainWindow` declarativ.
- [x] Toate API-urile publice sunt documentate in sursa oficiala `docs-site/documentation/classes/`.
- [x] Testele tintite, suita completa, build-ul Release, formatterul, smoke-ul nativ si benchmarkul Presentation sunt verzi.
