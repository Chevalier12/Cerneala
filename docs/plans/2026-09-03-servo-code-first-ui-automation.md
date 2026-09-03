# Plan: Servo, automatizare UI code-first pentru Cerneala

> Data: 2026-09-03
>
> Status: finalizat
> Scop: înlocuirea automatizării UI actuale cu subsistemul public, independent și modular `Servo`, limitat la aplicații construite cu Cerneala și integrat exclusiv prin arborele semantic, inputul și frame lifecycle-ul Cerneala.

## 1. Decizii aprobate

- Numele public al subsistemului este `Servo`.
- `Servo` este o clasă publică instanțiabilă, nu un serviciu global și nu un assembly separat.
- Servo automatizează numai aplicații Cerneala, în același proces. Nu implementează Win32 UI Automation, accessibility bridges pentru aplicații terțe, remote control sau automatizare cross-process.
- API-ul este code-first și asincron. Runner-ul JSON actual și variabilele generale `CERNEALA_AUTOMATION_SCRIPT` / `CERNEALA_AUTOMATION_WINDOW_TITLE` se elimină.
- `ServoTarget` este un descriptor reutilizabil. Fiecare query, acțiune și condiție îl rezolvă din nou; `ServoElement` este un snapshot read-only și nu păstrează sau expune `UIElement`.
- Acțiunile Servo intră prin pipeline-ul real Cerneala de `InputFrame`, hit testing, focus și routed events. Nu setează direct proprietăți de control pentru a simula utilizatorul.
- O acțiune se termină după ce inputul ei a fost procesat și efectele au fost comise într-un frame. Nu așteaptă implicit idle global; `WaitForIdleAsync` este explicit deoarece Motion poate rula continuu.
- Servo poate salva atât screenshot-ul complet al ferestrei, cât și pixelii vizibili din dreptunghiul unui `ServoTarget`; ambele căi rămân în pipeline-ul application-owned `Window.SaveScreenshot`, fără captură OS-level.
- Identificatorul attached devine `Servo.Id`: `Servo.IdProperty`, `Servo.GetId` și `Servo.SetId`; în markup se scrie `Servo.Id="login"`.
- Accessibility rămâne owner separat. `AutomationPeer`, peers specializați, `SemanticsTree`, `SemanticsNode`, `SemanticsRole` și `SemanticsProvider` nu se redenumesc în Servo.
- Migrarea este breaking și curată: nu se păstrează aliasuri, wrappers sau un ciclu `[Obsolete]` pentru API-ul `Cerneala.UI.Automation` aprobat pentru eliminare.

## 2. Baseline și problema actuală

Implementarea actuală este concentrată în `UI/Automation/`, dar suprafața publică și responsabilitățile sunt amestecate:

- `AutomationSession` interoghează direct `VisualChildren`, construiește o proiecție XML separată și oferă XPath și căutare după `AutomationId`.
- `AutomationElement` ține și expune un `UIElement` live; astfel un rezultat poate deveni stale după înlocuirea, reparentarea sau virtualizarea elementului.
- `IAutomationInputDriver` și `RetainedAutomationInputDriver` sunt publice, deși sunt mecanisme de infrastructură.
- `Window.CreateAutomationSession()` și `WindowApplicationRuntime.CreateAutomationSession()` compun sesiunea, iar runtime-ul general pornește `AutomationScriptRunner` din variabile de mediu după un frame prezentat.
- `AutomationScriptRunner` menține o a doua suprafață declarativă, JSON, pentru numai patru acțiuni și duplică selecția prin ID/XPath.
- `AutomationProperties.AutomationId` este hardcodat în `Cerneala.SourceGen/UiMarkupGenerator.cs`; același generator și `Cerneala.Language/Semantics/CernealaSemanticModel.cs` includ `Cerneala.UI.Automation` în rezoluția built-in, iar corpusul limbajului conține sintaxa veche. Redenumirea attached property nu este doar o mutare de namespace.

Arborele semantic existent oferă fundația corectă, dar nu poate fi consumat naiv:

- `SemanticsProvider` construiește nodurile prin `AutomationPeer` și `UIRoot.GetSemanticsTree()` le cache-uiește pe baza invalidării semantics/tree version.
- Proiecția accessibility curentă omite subarborii care nu participă la randare. Servo trebuie însă să poată diferenția `Hidden` de `Missing` fără să schimbe contractul accessibility existent.
- `SemanticsNode.ElementId` poate fi rezolvat intern prin `UIRoot.ElementIds`; Servo poate astfel calcula bounds și actionability din elementul live fără să îl publice.
- `UIElement.SetArrangedBounds` nu invalidează semantics. Din acest motiv, bounds nu se copiază într-un cache semantic Servo; snapshot-ul citește bounds live la momentul rezolvării.
- `UIElement.VisibilityProperty` are `AffectsSemantics`, deci schimbările de vizibilitate pot invalida corect o proiecție semantică Servo care include nodurile nerandate.

Consumatorii existenți care trebuie migrați sunt confirmați semantic: runtime-ul de window, design preview/PreviewHost, `CernealaPresentation`, playground-urile ComboBox/Menu/general, source generatorul, semantic modelul limbajului, corpusul `.crn`, testele `AutomationSessionTests`, testele `AspectChapterViewTests`, testul source generator pentru attached property, documentația și manifestul. Cele două fișiere JSON din playground nu au call-site-uri în repo.

Baseline-ul public pentru API diff este commitul `fed724b954bc2823c4799db69c94b92e2790b2b5`. Worktree-ul curent este dirty în fișiere din afara Servo; implementarea trebuie să le păstreze și să nu le atribuie acestei migrări.

## 3. Obiective observabile

- Un consumator poate crea `new Servo(window)` pentru o fereastră afișată sau `new Servo(host)` pentru un `UiHost` reținut și poate automatiza aceeași aplicație din C#.
- Un target poate fi compus după ID, nume, rol semantic și ancestor scope, fără XPath și fără expunerea arborelui vizual brut.
- Zero, unul și mai multe rezultate au contracte distincte și deterministe; lipsa, ambiguitatea, non-actionability și timeout-ul au excepții Servo separate.
- Click, hover, drag, scroll, key chord și text folosesc cadre de input reale. `TypeIntoAsync` și `ReplaceTextAsync` compun aceleași primitive, nu introduc o cale de mutație directă.
- Așteptările observă schimbările frame/semantic state, respectă timeout și cancellation și nu blochează relay-ul UI.
- Două instanțe Servo pe același host/window nu pot interfera prin cadre parțiale, taste/modifiers rămase apăsate sau drag state partajat incorect.
- Captura pentru `Window` trece exclusiv prin `Window.SaveScreenshot`; Servo nu introduce screen capture la nivel OS.
- Captura unui element decupează framebuffer-ul complet redat la bounds-ul live al target-ului; păstrează overlay-urile care îl acoperă și taie efectele care ies în afara acelui dreptunghi.
- Namespace-ul public `Cerneala.UI.Automation` dispare numai pentru tipurile de automatizare migrate; tipurile accessibility cu „Automation” în nume rămân neschimbate.
- Markup-ul generat, sample-urile code-first, testele managed și un smoke native folosesc noul API.

## 4. Non-obiective

- Nu se automatizează aplicații non-Cerneala și nu se adaugă discovery de procese, ferestre după titlu sau transport remote.
- Nu se implementează UI Automation/Accessibility adapters pentru Windows, macOS sau Linux.
- Nu se adaugă recorder, designer vizual, DSL, JSON/YAML runner, XPath, CSS selectors, reflection selectors sau playback de scripturi.
- Nu se mută ori redenumește subsistemul accessibility pentru a se potrivi brandului Servo.
- Nu se expune `UIElement`, `AutomationPeer`, `ElementIdProvider`, input driver-ul sau runtime context-ul prin API-ul Servo.
- Nu se schimbă contractul public existent `Window.SaveScreenshot(string)`, `InputKey` sau regulile de routed input; extensia regională a capturii rămâne internă, iar API-ul public nou este numai overload-ul Servo.
- Nu se adaugă polling interval ori cache public configurabil fără o problemă măsurată.
- Nu se afirmă paritate vizuală sau performanță fără un gate separat măsurabil; această livrare este despre contract și corectitudine.

## 5. Contractul public țintă

Toate tipurile noi locuiesc în namespace-ul `Cerneala.UI.Servo`, în assembly-ul `Cerneala`.

### 5.1 `Servo`

- Constructori publici: `Servo(Window, ServoOptions?)` și `Servo(UiHost, ServoOptions?)`.
- Metadata attached statică pe aceeași clasă: `IdProperty`, `GetId(UIElement)` și `SetId(UIElement, string?)`. Se păstrează contractul actual de normalizare: trim exterior, iar whitespace-only devine `null`.
- Queries async: `FindAsync`, `FindAllAsync`, `ExistsAsync`.
- Acțiuni async: `ClickAsync`, `HoverAsync`, `DragAsync`, `ScrollAsync`, `PressKeyAsync`, `SendTextAsync`, `TypeIntoAsync`, `ReplaceTextAsync`.
- Sincronizare async: `WaitForAsync`, `WaitUntilAsync`, `WaitForIdleAsync`.
- Captură async: `SaveScreenshotAsync(string path, CancellationToken)` pentru fereastra completă și `SaveScreenshotAsync(ServoTarget target, string path, CancellationToken)` pentru un element.
- Fiecare operație async acceptă `CancellationToken`; timeout-ul implicit vine din `ServoOptions`. Timeout-ul produce `ServoTimeoutException`, iar cancellation extern păstrează `OperationCanceledException`.
- Instanța nu implementează `IDisposable`: subscriptions pentru așteptări sunt tranzitorii și trebuie detașate pe succes, timeout, cancellation și excepție. Contextul nativ rămâne deținut de window runtime.

### 5.2 Selectori și snapshot-uri

- `ServoTarget` este imuabil și oferă `ById(string)`, `ByName(string)`, `ByRole(SemanticsRole)`, `WithName(string)` și `Within(ServoTarget)`.
- Compararea ID-ului și numelui este exactă, ordinală. `Within` filtrează după ancestry semantică; nu rezolvă și nu cache-uiește separat un container live.
- `FindAsync` cere exact un rezultat: zero aruncă `ServoTargetNotFoundException`, iar mai multe aruncă `ServoTargetAmbiguousException`.
- `FindAllAsync` întoarce snapshot-uri în ordinea stabilă a arborelui semantic și poate întoarce lista goală. `ExistsAsync` răspunde dacă există cel puțin un match și nu transformă multiplicitatea în eroare.
- `ServoElement` este un snapshot read-only cu `TypeName`, `Id`, `Name`, `Role`, `Bounds`, `IsVisible`, `IsEnabled`, `IsFocused`, `Value` și `Properties`; `Bounds` folosește `LayoutRect`, iar `Properties` folosește cheile `SemanticsProperty` existente.
- `ServoPoint` este value type-ul public pentru coordonate Servo. Coordonatele sunt în spațiul client DIP al hostului/window-ului.

### 5.3 Condiții, opțiuni și erori

- `ServoCondition` conține `Exists`, `Missing`, `Visible`, `Hidden`, `Enabled`, `Disabled` și `Focused`.
- `Exists`/`Missing` operează pe cardinalitate; condițiile de stare cer un target unic și raportează ambiguitatea imediat.
- `ServoOptions` expune timeout-ul implicit, validat ca durată pozitivă și finită. Nu expune scheduling sau polling intern.
- `ServoModifiers` păstrează valorile actuale `None`, `Shift`, `Control`, `Alt`.
- Familia publică de excepții este `ServoException`, `ServoTargetNotFoundException`, `ServoTargetAmbiguousException`, `ServoTargetNotActionableException` și `ServoTimeoutException`.

### 5.4 Semantica acțiunilor și așteptărilor

- Acțiunile pe target îl rezolvă fresh, verifică apartenența la host, vizibilitatea efectivă, enabled state, bounds utilizabile și hit-test-ul înainte de input. Eșecul este `ServoTargetNotActionableException`, nu un click mut în centru.
- `HoverAsync`, `ClickAsync`, `DragAsync` și `ScrollAsync` folosesc poziții absolute calculate din bounds live; `DragAsync` păstrează secvența down/move/up chiar la cancellation prin reset intern sigur.
- `PressKeyAsync` și `SendTextAsync` lucrează pe focusul Cerneala curent. `TypeIntoAsync` focalizează target-ul prin click apoi trimite text. `ReplaceTextAsync` compune focus, `Control+A` și text prin pipeline-ul de input.
- Finalizarea unei acțiuni înseamnă commit după input. Pentru `Window`, confirmarea vine după frame-ul prezentat de runtime; pentru `UiHost`, după revenirea din `UiHost.Update` și retained commit. Niciun adaptor nu pornește un al doilea loop nativ.
- `WaitForAsync` și `WaitUntilAsync` reevaluează la frame boundaries și pot folosi wake-up de semantics invalidation; nu țin blocat lock-ul care serializează acțiunile cât rulează predicate user code.
- `WaitForIdleAsync` cere simultan lipsa lucrului programat și lipsa Motion activ. O animație continuă ajunge la timeout, nu este declarată idle.
- `SaveScreenshotAsync` pe `Window` apelează provider-ul intern care ajunge la `Window.SaveScreenshot`. Overload-ul cu target îl rezolvă fresh în aceeași operație serializată, cere un element efectiv vizibil cu bounds finite și nenule și folosește `ServoTargetNotActionableException` pentru un target care nu poate produce un crop. Enabled și hit-testability nu sunt condiții pentru captură.
- Crop-ul este definit în pixeli reali: latura stângă/sus folosește floor, latura dreaptă/jos folosește ceil după aplicarea scale-ului viewport-ului, apoi dreptunghiul este intersectat cu framebuffer-ul client. Un rezultat gol este non-capturable.
- Captura unui target este crop din randarea completă a ferestrei, nu rerandarea izolată a subtree-ului. Astfel surprinde pixelii efectiv vizibili, inclusiv elementele/overlay-urile desenate peste target; umbrele, transformările sau alte efecte care depășesc bounds-ul target-ului sunt tăiate deliberat.
- Pentru un `Servo` construit numai cu `UiHost`, unde repo-ul nu oferă un encoder/capture owner general, ambele overload-uri aruncă determinist `NotSupportedException`.

## 6. Arhitectura țintă

### 6.1 Fațadă și module interne

`Servo` este singura fațadă de orchestrare publică. Implementarea este împărțită intern după owner, nu după convenience public:

1. query engine: compilează `ServoTarget`, consumă proiecția semantică și produce IDs/snapshot-uri;
2. input driver: transformă acțiunile în `InputFrame` și folosește `UiHost.InputBridge` prin `UiHost.Update`/runtime;
3. synchronization: serializare per context, frame completion, timeout, cancellation și idle;
4. capture adapter: numai pentru contextul `Window`, cu full-frame și crop după bounds;
5. context adapter: leagă fațada de un `WindowApplicationRuntime.WindowContext` sau de un `UiHost` direct.

Tipurile orientative sunt `ServoQueryEngine`, `IServoInputDriver`, `RetainedServoInputDriver`, `ServoSynchronization` și un contract intern de context. Inventarul este estimativ; nu se creează interfețe fără două implementări reale sau o limită de testare/ownership demonstrată.

### 6.2 Proiecție semantică unică

Servo nu reconstruiește XML și nu traversează o a doua reprezentare ad-hoc a arborelui. `SemanticsProvider` primește o opțiune internă de proiecție care reutilizează `AutomationPeer` și aceeași ordine a copiilor, dar include nodurile care nu participă la randare pentru query-urile Servo. Proiecția accessibility implicită rămâne neschimbată și continuă să excludă acele noduri.

Vizibilitatea efectivă, bounds și actionability se calculează la rezolvare din `SemanticsNode.ElementId` -> `UIRoot.ElementIds.TryGetElement`. Servo nu pune bounds într-un cache semantic care nu este invalidat la arrange. Dacă se introduce ulterior cache pentru proiecția Servo, el trebuie să folosească token-urile reale semantics/tree version și să aibă teste de invalidare; fără măsurători, baseline-ul este rebuild corect, nu cache speculativ.

### 6.3 Ownership și concurență

- Pentru `Window`, coada de input, starea pointer/key și sincronizarea sunt per window context și deținute de `WindowApplicationRuntime`; toate instanțele Servo ale aceleiași ferestre folosesc același context serializat.
- Pentru `UiHost`, contextul este per host și serializat pe `UIRoot.Relay`; nu există stare process-global.
- O operație Servo este atomică la nivelul secvenței sale de cadre. Predicatele și waits nu monopolizează serializarea între reevaluări.
- Close/root replacement/failure invalidează operațiile în așteptare cu o eroare explicită și detașează callbacks. Nu se reține fereastra/root-ul după ce runtime-ul și-a închis contextul.

### 6.4 Captura regională

`IWindowScreenshotSource.RenderPng` redă deja un framebuffer complet offscreen în ambele implementări native: SDL_GPU citește `WindowPreviewFrame` și encodează cu Skia, iar WindowsDX salvează un `RenderTarget2D`. Servo nu decodează un PNG temporar și nu citește direct presented-frame internals.

Calea application-owned de screenshot primește un contract regional intern, folosit de un overload intern `Window.SaveScreenshot`/runtime. Fiecare backend decupează framebuffer-ul complet după randare și înainte de encodarea PNG. `Window.SaveScreenshot(string)` public rămâne neschimbat și continuă să ceară cadrul complet; noua suprafață publică regională există numai pe Servo.

Testele backend trebuie să demonstreze aceeași convenție floor/ceil/clamp și aceleași dimensiuni/pixeli pentru scale 1, 1.5 și 2. Nu se adaugă o dependență de imagine nouă: SDL_GPU are deja encoderul Skia, iar calea WindowsDX are deja render target-ul și encoderul MonoGame; dacă unul dintre backend-uri nu poate aplica regiunea fără decode din fișier sau screen capture OS-level, etapa se oprește.

### 6.5 Migrare și dependențe

| Suprafață actuală | Migrare | Verificare |
| --- | --- | --- |
| `AutomationSession` | `Servo`; constructorii publici înlocuiesc `Window.CreateAutomationSession()` | API tests, runtime tests, ApiCompat clasificat |
| `AutomationElement` | `ServoElement` snapshot; acțiunile rămân pe `Servo` | test că public API nu expune `UIElement`; stale-target regression |
| `AutomationProperties.AutomationId` | `Servo.Id` | runtime property tests și generated-markup test |
| `AutomationModifiers` | `ServoModifiers` | key-chord tests și PreviewHost tests |
| `IAutomationInputDriver` | `IServoInputDriver` internal | public API reflection test |
| `RetainedAutomationInputDriver` | `RetainedServoInputDriver` internal | retained input și preview regression tests |
| `AutomationScriptRunner` | eliminat, fără înlocuitor declarativ | absență API, eliminarea testului JSON și a hook-ului runtime |
| `Window.CreateAutomationSession()` | eliminat; call-site-urile folosesc `new Servo(window)` | `OpeningView` și runtime tests |
| XPath / `FindByAutomationId` | `ServoTarget` + `FindAsync`/`FindAllAsync` | query/cardinality tests |
| `DesignPreviewSession` / `PreviewRenderSession` | păstrează API-ul lor de preview, dar folosesc driverul intern redenumit și `ServoModifiers` | `Cerneala.Tests.PreviewHost` |
| Presentation și playgrounds | `Servo.SetId` / `Servo.Id`; flows code-first mutate prin Servo | build + teste dedicate |
| Language semantic model și corpus | built-in namespace și exemplul attached devin `Cerneala.UI.Servo` / `Servo.Id` | `Cerneala.Tests.Language` |
| `IWindowScreenshotSource.RenderPng` | contract intern full-frame/regional, folosit prin owner-ul `WindowApplicationRuntime` | teste pixel/dimensiune WindowsDX și SDL_GPU |
| Call-site-urile full-frame `PrismBackdropMonoGameAdapterTests`, `PrismWindowsDxConformanceTests` și fake-ul din `WindowRuntimeTests` | rămân pe calea full-frame existentă; overload-ul/regiunea opțională nu le schimbă pixelii sau diagnostics | suitele existente, plus compile full |
| Cele două scripturi JSON playground | eliminate; scenariile utile sunt acoperite de smoke-ul code-first, nu mutate într-un DSL nou | search fără referințe JSON/runner |

## 7. Fișiere estimate

Producție/runtime:

- `UI/Automation/*.cs` (eliminate după migrare)
- `UI/Servo/Servo.cs`
- `UI/Servo/ServoTarget.cs`
- `UI/Servo/ServoElement.cs`
- `UI/Servo/ServoPoint.cs`
- `UI/Servo/ServoCondition.cs`
- `UI/Servo/ServoOptions.cs`
- `UI/Servo/ServoModifiers.cs`
- `UI/Servo/ServoException.cs` și excepțiile specializate
- modulele interne query/input/synchronization/context/capture din `UI/Servo/`
- `UI/Accessibility/SemanticsProvider.cs`
- `UI/Controls/Window.cs`
- `UI/Hosting/Windowing/WindowApplicationRuntime.cs`
- `UI/Hosting/Windowing/IWindowPlatform.cs`
- `UI/Hosting/Windowing/DesignPreviewSession.cs`
- `UI/Hosting/Windows/WindowsDxWindowGraphicsSession.cs`
- `Cerneala.Backends.SdlGpu/Gpu/SdlGpuWindowGraphicsSession.cs`
- `Cerneala.PreviewHost/PreviewRenderSession.cs`
- `Cerneala.PreviewHost/PreviewHostServer.cs`
- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `Cerneala.Language/Semantics/CernealaSemanticModel.cs`
- call-site-urile `CernealaPresentation` și playground identificate în tabelul de migrare

Teste și smoke:

- `tests/Cerneala.Tests/UI/Servo/*.cs`
- `tests/Cerneala.Tests/UI/Accessibility/SemanticsProviderTests.cs` sau testul semantic existent echivalent
- `tests/Cerneala.Tests/UI/Hosting/WindowRuntimeTests.cs`
- `tests/Cerneala.Tests/Drawing/MonoGame/PrismBackdropMonoGameAdapterTests.cs`
- `tests/Cerneala.Tests/Drawing/MonoGame/PrismWindowsDxConformanceTests.cs`
- `tests/Cerneala.Tests.SdlGpu/SdlGpuWindowGraphicsSessionTests.cs`
- `tests/Cerneala.Tests/Presentation/AspectChapterViewTests.cs`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`
- `tests/Cerneala.Tests.Language/Corpus/constructs.json`
- testele semantic model/markup corpus din `tests/Cerneala.Tests.Language/`
- `tests/Cerneala.Tests.PreviewHost/*`
- `tests/Cerneala.SdlGpuSmoke/SmokeOptions.cs`
- scenariul Servo code-first din `tests/Cerneala.SdlGpuSmoke/`

Documentație și artefacte:

- pagini noi/renumite `docs-site/documentation/classes/Cerneala.UI.Servo.*.md`
- eliminarea paginilor `docs-site/documentation/classes/Cerneala.UI.Automation.*.md`
- `docs-site/documentation/classes/Cerneala.UI.Controls.Window.md`
- `docs-site/documentation/manifest.json`
- `DOCUMENTATION_CHECKLIST.md`
- `docs/servo.md` pentru ghidul code-first, dacă nu există la implementare un ghid conceptual canonic echivalent
- `benchmarks/results/2026-09-03-servo/api-compat.proj`
- `benchmarks/results/2026-09-03-servo/api-compat.md`
- eliminarea `Playground/Cerneala.MenuLab/automation/capture-open-menu.json`
- eliminarea `Playground/Cerneala.Playground/automation/capture-drawing-api.json`

## 8. Etape de implementare

### Etapa 0 — Baseline, contract API și plasa RED

- [x] Construiește commitul baseline `fed724b954bc2823c4799db69c94b92e2790b2b5` într-un worktree detached separat și arhivează în `benchmarks/results/2026-09-03-servo/api-compat.md` commitul, comenzile, SDK-ul și assembly-ul baseline; nu face checkout/reset/clean în worktree-ul dirty curent.
- [x] Rulează testele existente `AutomationSessionTests`, testul runtime pentru scriptul post-frame, `AspectChapterViewTests`, testele PreviewHost și testul source generator pentru `AutomationProperties.AutomationId`; clasifică rezultatele drept caracterizare GREEN, nu drept dovadă pentru Servo.
- [x] Adaugă un test RED de suprafață publică bazat pe reflection care cere `Cerneala.UI.Servo.Servo`, constructorii `Window`/`UiHost`, membrii enumerați în secțiunea 5 și absența expunerii `UIElement`/input driver; confirmă că eșuează fiindcă API-ul Servo lipsește, nu din cauza fixture-ului.
- [x] Adaugă caracterizări pentru proiecția accessibility curentă: hidden/collapsed subtree este omis, ordinea nodurilor este stabilă, iar `ElementId` se rezolvă numai cât elementul aparține root-ului.
- [x] Fixează prin teste contractele actuale care trebuie păstrate: normalizarea ID-ului, input prin hit-test/routed events, frame commit după input, screenshot prin `Window.SaveScreenshot`, izolarea a două window contexts și resetarea key/pointer state după secvențe complete.
- [x] După fiecare modificare C# sau `.csproj`, reindexează cu `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.

**Gate etapa 0**

- [x] Caracterizările sunt GREEN, testul de API Servo este RED pentru lipsa suprafeței aprobate, baseline-ul public este reproductibil, iar nicio presupunere despre XPath/JSON nu a fost transformată în compatibilitate obligatorie.

### Etapa 1 — Fațada publică minimă și proiecția semantică Servo

- [x] Creează tipurile publice din secțiunea 5 cu nullability, validare și exception hierarchy fixate prin teste; nu implementa încă acțiunile printr-un driver public.
- [x] Mută attached property pe `Servo.IdProperty`/`GetId`/`SetId` și păstrează normalizarea existentă; adaugă RED înainte pentru whitespace, clear și invalid argument behavior.
- [x] Extinde intern `SemanticsProvider` cu proiecția Servo care include subarborii nerandați fără să schimbe rezultatul implicit accessibility; adaugă RED pentru `Hidden` versus `Missing`, hidden ancestor, ordering și duplicate IDs.
- [x] Implementează query engine-ul intern peste `SemanticsNode`/`ElementIdProvider`; citește bounds și starea efectivă live și nu păstrează `UIElement` în `ServoElement` ori `ServoTarget`.
- [x] Adaugă RED apoi GREEN pentru `ById`, `ByName`, `ByRole`, `WithName`, `Within`, zero/unul/mai multe rezultate, `FindAllAsync` empty și re-rezolvarea aceluiași target după replace/reparent.
- [x] Verifică prin reflection că toate modulele de infrastructură rămân internal și că `ServoElement` nu expune referințe către arborele live.
- [x] Reindexează soluția după modificările C#.

**Gate etapa 1**

- [x] Query-urile și snapshot-urile sunt GREEN, accessibility default are același rezultat caracterizat, `Hidden` este distinct de `Missing`, iar target-ul reutilizat găsește instanța curentă după mutația arborelui.

### Etapa 2 — Input real și finalizare la frame boundary

- [x] Redenumește driverul retained ca `RetainedServoInputDriver`, mută interfața la `IServoInputDriver` internal și extrage numai primitivele necesare acțiunilor aprobate; nu publica hooks de preview sau cadre brute.
- [x] Mută ownership-ul stării sintetice și al serializării în contextul per-window/per-host; adaugă RED pentru două instanțe Servo pe același context și două windows intercalate.
- [x] Adaugă RED apoi implementează `HoverAsync`, `ClickAsync`, `DragAsync` și `ScrollAsync` prin cadre reale, cu target fresh, bounds live, hit testing și `ServoTargetNotActionableException` pentru hidden/disabled/detached/zero-bounds/non-hit-testable.
- [x] Adaugă RED apoi implementează `PressKeyAsync`, `SendTextAsync`, `TypeIntoAsync` și `ReplaceTextAsync`; dovedește focus routing, modifier chord, select-all și text input fără assign direct la `TextBox.Text` sau stare de control.
- [x] Pentru cancellation/excepții în mijlocul drag/chord, garantează frame de release/reset și dovedește că următoarea acțiune nu moștenește pointer button ori modifier apăsat.
- [x] Pentru `Window`, enqueue-uiește secvențele prin frame loop-ul existent și finalizează după present; pentru `UiHost`, finalizează după `Update`/retained commit. Adaugă un test care separă „input trimis” de „frame comis”.
- [x] Migrează `DesignPreviewSession`, `WindowApplicationRuntime` preview helpers și `Cerneala.PreviewHost` la driverul intern/`ServoModifiers`, păstrând API-ul public de preview și testele lui.
- [x] Reindexează soluția după modificările C#.

**Gate etapa 2**

- [x] Toate acțiunile trec prin `InputFrame` și routed input, nu există stare blocată după failure/cancellation, finalizarea respectă contractul de commit, iar testele PreviewHost rămân GREEN.

### Etapa 3 — Waits, timeout, idle, lifecycle și captură

- [x] Adaugă RED apoi implementează `WaitForAsync` pentru toate valorile `ServoCondition`, cu cardinalitatea din secțiunea 5 și reevaluare frame-driven.
- [x] Adaugă RED apoi implementează `WaitUntilAsync` astfel încât predicatele async să poată apela queries Servo fără deadlock și excepțiile predicate-ului să fie propagate, nu mascate ca timeout.
- [x] Adaugă RED apoi implementează `WaitForIdleAsync` pe starea reală scheduler + Motion; include scenarii no-work, work programat, Motion finit și Motion continuu care expiră.
- [x] Dovedește timeout/cancellation pentru query, acțiune și wait; toate subscriptions/callbacks se detașează și următoarea operație rămâne utilizabilă.
- [x] Dovedește close de window, root replacement la `UiHost` și element detached în timp ce o operație așteaptă; nu lăsa task-uri nefinalizate ori referințe reținute de callbacks.
- [x] Implementează `SaveScreenshotAsync` numai prin provider-ul window existent și testează calea `Window.SaveScreenshot`; pentru constructorul `UiHost`, fixează testul `NotSupportedException` fără fallback OS-level.
- [x] Adaugă RED pentru `SaveScreenshotAsync(target, path)`: target fresh, target disabled sau non-hit-testable încă este capturabil, iar missing/ambiguous/hidden/zero-bounds/outside-client au rezultatele fixate în secțiunea 5.4.
- [x] Extinde contractul intern application-owned de screenshot cu regiune pixel și implementează crop după full render, înainte de PNG encode, în WindowsDX și SDL_GPU; nu salva/decodează un PNG temporar și nu expune region overload pe `Window` public.
- [x] Adaugă o matrice deterministă de pixeli pentru full-window versus target crop la scale 1, 1.5 și 2, bounds fracționare, clamp la fiecare margine și overlay peste target; verifică dimensiunile PNG și culorile exacte/toleranțele backend existente.
- [x] Reindexează soluția după modificările C#.

**Gate etapa 3**

- [x] Wait-urile sunt deterministe și anulabile, Motion continuu nu devine idle fals, failure paths curăță subscriptions/input state, iar screenshot-urile full-window și target folosesc exclusiv API-ul de captură al aplicației și au crop identic pe WindowsDX/SDL_GPU.

### Etapa 4 — Markup, consumatori și eliminarea căii vechi

- [x] Adaugă un test source-generator RED pentru `Servo.Id="..."`, apoi schimbă recunoașterea attached property și fallback-ul built-in hardcodate din `UiMarkupGenerator` la `Cerneala.UI.Servo.Servo` / `IdProperty`; verifică output-ul compilat și valoarea runtime.
- [x] Adaugă un test language RED pentru rezoluția semantică a `Servo.Id`, mută built-in namespace-ul din `CernealaSemanticModel` la `Cerneala.UI.Servo` și actualizează cazul `xml-attached-property` din `tests/Cerneala.Tests.Language/Corpus/constructs.json`.
- [x] Migrează toate call-site-urile `AutomationProperties` din Presentation, ComboBoxLab, MenuLab și Playground la `Servo.SetId` sau `Servo.Id` și toate call-site-urile `AutomationSession` la noua fațadă async.
- [x] Migrează `OpeningView` la `new Servo(window)` și elimină lanțul sincron `CreateAutomationSession().FindByAutomationId(...).Click()` fără a schimba condițiile application-specific de auto-continue/capture.
- [x] Migrează `AspectChapterViewTests` la Servo public; testele nu construiesc input driver-ul internal și continuă să exercite click/key/text prin host.
- [x] Elimină `Window.CreateAutomationSession`, `WindowApplicationRuntime.CreateAutomationSession`, `RunAutomationScriptIfRequested`, `AutomationScriptRunner`, XPath/XML projection și cele două variabile generale de mediu.
- [x] Elimină cele două fișiere JSON fără referințe. Adaugă în SDL_GPU smoke un mod `servo` code-first care deschide o aplicație Cerneala, găsește prin `Servo.Id`, execută input real, așteaptă starea observabilă, salvează screenshot complet și target crop prin `Window.SaveScreenshot` și închide controlat.
- [x] Șterge directorul `UI/Automation/` numai după ce Roslyn search și textual search pentru markup/docs/config confirmă că fiecare call-site din tabel a fost migrat și că nu se ating `UI/Accessibility/AutomationPeer*`.
- [x] Reindexează soluția după modificările C# și rulează din nou references pentru toate tipurile eliminate.

**Gate etapa 4**

- [x] Nu mai există cod, generated source, test, doc sau hook runtime care depinde de `Cerneala.UI.Automation`, XPath, runner-ul JSON ori variabilele lui; accessibility peers sunt nemodificați semantic, iar toate aplicațiile consumatoare compilează.

### Etapa 5 — Documentație, API review și verificare completă

- [x] Folosește skill-ul `writing-api-documentation` pentru fiecare tip public Servo și pentru membrii lui; exemplele folosesc numai API-uri publice reale și arată target reutilizabil, input async, wait explicit și screenshot pe `Window`.
- [x] Elimină paginile canonice `Cerneala.UI.Automation.*`, actualizează pagina `Window`, sincronizează `docs-site/documentation/manifest.json` și `DOCUMENTATION_CHECKLIST.md` și adaugă/actualizează ghidul conceptual code-first fără a duplica referința API.
- [x] Construiește Release pentru baseline și worktree, rulează taskul SDK `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` în strict mode cu parameter-name checks și arhivează proiectul/comanda/output-ul în `benchmarks/results/2026-09-03-servo/`.
- [x] Clasifică fiecare diferență publică/protected: numai adăugările Servo și eliminările/renumirile aprobate din secțiunea 6.5 sunt acceptate; orice alt diff oprește etapa.
- [x] Rulează testele focalizate Servo: `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~Cerneala.Tests.UI.Servo`.
- [x] Rulează testele runtime afectate: `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~Cerneala.Tests.UI.Hosting.WindowRuntimeTests`.
- [x] Rulează testele Presentation afectate: `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~Cerneala.Tests.Presentation.AspectChapterViewTests`.
- [x] Rulează source generator: `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests`.
- [x] Rulează limbajul și corpusul markup: `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj`.
- [x] Rulează PreviewHost: `dotnet test .\tests\Cerneala.Tests.PreviewHost\Cerneala.Tests.PreviewHost.csproj`.
- [x] Rulează testele backend SDL_GPU pentru screenshot regional: `dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj --filter FullyQualifiedName~SdlGpuWindowGraphicsSessionTests`.
- [x] Rulează documentația/manifest: `dotnet test .\tests\Cerneala.Tests.VisualStudio\Cerneala.Tests.VisualStudio.csproj --filter FullyQualifiedName~ApiDocumentationManifestIsValidAndReferencesExistingFiles`.
- [x] Rulează smoke-ul nativ SDL_GPU Servo pe Windows: `dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release -- --mode servo --artifacts .\artifacts\sdlgpu-servo`; verifică exit code, screenshot-ul complet, target crop-ul cu dimensiunile așteptate și rezultatul stării automatizate. Ambele PNG-uri trebuie produse prin `Window.SaveScreenshot`. Acesta este gate obligatoriu; un backend/driver indisponibil este blocker raportat, nu GREEN.
- [x] Rulează build-ul complet: `dotnet build .\Cerneala.slnx -c Release`.
- [x] Rulează suita completă: `dotnet test .\Cerneala.slnx -c Release --no-build`.
- [x] Rulează formatter verification: `dotnet format .\Cerneala.slnx --verify-no-changes --no-restore`; orice failure preexistent se separă prin lista exactă de fișiere și se rulează suplimentar verificarea goal-scoped, fără a edita fișierele dirty ale utilizatorului.
- [x] Rulează comparația API arhivată: `dotnet msbuild .\benchmarks\results\2026-09-03-servo\api-compat.proj -t:Compare -v:minimal`.
- [x] Reindexează final și cere status curat: `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`, apoi `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- status .\Cerneala.slnx --json`.
- [x] Rulează `git diff --check`, inventariază exact fișierele schimbate și confirmă că modificările dirty preexistente au fost păstrate.

**Gate etapa 5**

- [x] API docs și manifestul sunt sincronizate, diff-ul public conține numai breaking changes aprobate, toate testele focalizate și full suite sunt GREEN, smoke-ul nativ este GREEN, indexul este fresh și nu rămân referințe sau artefacte ale căii vechi.

**Dovezi etapa 5 (2026-09-03)**

- Documentație: 12 pagini canonice Servo, zero pagini `Cerneala.UI.Automation.*`, toate cele 12 intrări prezente în manifest; testul manifestului a trecut 1/1.
- Teste focalizate: Servo 38/38, WindowRuntime 34/34, Presentation 24/24, source generator 461/461, Language 184/184, PreviewHost 12/12 și SDL_GPU regional 21/21.
- Build Release: `dotnet build .\Cerneala.slnx -c Release` a trecut cu zero warnings și zero erori.
- Suită completă: rularea serializată `dotnet test .\Cerneala.slnx -c Release --no-build -m:1` a trecut 4165 teste, cu 9 skip-uri existente. Rularea implicit paralelă a expus o singură expirare de 2 minute în `CompletionProtocolTests.MenuItemPropertyCompletionRunsThroughTheRealProtocol`; același test a trecut izolat, proiectul LanguageServer a trecut 40/40, iar suita completă serializată a trecut fără modificarea surselor.
- Smoke nativ: mod `servo` GREEN; `servo-main.png` este 800×525, iar `servo-target.png` este 200×60, ambele produse prin calea application-owned.
- Formatter: verificarea completă a separat exact 10 fișiere cu probleme preexistente/concurente; verificarea goal-scoped a trecut pentru toate cele 111 fișiere C# schimbate.
- ApiCompat strict: 119 diferențe aprobate (20 Servo, 98 Detective și overload-ul `GeneratedMarkup.AttachMotionSession(UIElement, ElementAspect?)` aprobat explicit); comanda arhivată a ieșit cu cod 0.
- RoslynIndexer: status `valid`, 3621 documente și `dirtyFiles: 0`; `git diff --check` a trecut, inventarul `git status --short` are 239 intrări, iar sentinel-urile preexistente `D AGENTS.md`, `?? AGENTS_DEPRECATED.md` și `?? .codex/config.toml` au rămas intacte.

## 9. Ordinea recomandată

1. Etapa 0 fixează baseline-ul și RED-ul public înainte de surse.
2. Etapa 1 stabilește modelul semantic și snapshot-urile; inputul nu poate presupune o rezoluție neverificată.
3. Etapa 2 implementează inputul și frame completion peste target-uri deja stabile.
4. Etapa 3 adaugă waits/lifecycle/capture peste contextul deja funcțional.
5. Etapa 4 migrează consumatorii și elimină complet calea veche numai după ce Servo este verificabil.
6. Etapa 5 închide contractul public, documentația, API diff-ul și porțile complete.

Nu se începe etapa următoare până când gate-ul etapei curente nu este satisfăcut și bifat în acest fișier.

## 10. Condiții de stop

- Dacă proiecția semantică Servo nu poate diferenția hidden de missing fără să modifice contractul accessibility implicit, implementarea se oprește; nu se reintroduce traversarea paralelă XML/visual tree.
- Dacă runtime-ul nu poate confirma un frame prezentat fără un al doilea loop sau fără schimbarea contractului public `Window`, se revine la decizia de API înainte de workaround.
- Dacă un API accessibility este folosit în afara cone-ului inventariat și ar trebui rupt pentru Servo, schimbarea nu intră automat în scope.
- Dacă smoke-ul necesită screen capture OS-level, captura este blocată; nu se ocolește regula `Window.SaveScreenshot`.
- Dacă un backend nu poate decupa framebuffer-ul complet înainte de PNG encode fără fișier temporar/decoder nou, implementarea se oprește pentru decizie; nu se livrează comportament diferit între WindowsDX și SDL_GPU.
- Dacă apar cerințe pentru remote, recorder, alte aplicații sau DSL declarativ, ele devin inițiative separate.

## 11. Definiția de gata

- [x] `Servo` este clasa publică independentă și modulară descrisă în secțiunea 5, exclusiv pentru aplicații Cerneala.
- [x] Query-urile folosesc proiecția semantică comună, target-urile se rezolvă fresh și niciun API public Servo nu expune `UIElement` sau infrastructura internă.
- [x] Toate acțiunile folosesc inputul real Cerneala și finalizează după commit/present conform contextului.
- [x] Waits, timeout, cancellation, close, detach, root replacement, două sesiuni și două ferestre au teste deterministe GREEN.
- [x] `Servo.Id` funcționează din C# și markup generated.
- [x] `Cerneala.UI.Automation`, XPath, JSON runner-ul și variabilele runtime generale au fost eliminate fără shim; accessibility a rămas owner separat.
- [x] Screenshot-urile full-window și target crop trec numai prin `Window.SaveScreenshot`; crop-ul respectă floor/ceil/clamp și pixelii reali afișați pe WindowsDX/SDL_GPU, iar calea `UiHost` unsupported este documentată și testată.
- [x] Documentația publică, manifestul, checklist-ul și API diff-ul sunt sincronizate și clasificate.
- [x] Testele focalizate, build-ul Release, full suite, formatter gate și smoke-ul nativ SDL_GPU sunt GREEN.
- [x] Indexul Roslyn este fresh, `git diff --check` este curat, iar modificările preexistente ale utilizatorului sunt intacte.
