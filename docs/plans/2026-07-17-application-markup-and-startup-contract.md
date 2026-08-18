# Plan: `App.crn` and the declarative application contract

> Date: 2026-07-17
> Status: completed
> Purpose: introducing a declarative application definition that owns the startup, global resources and lifecycle, without the hardcoded convention `MainWindow`.

## 1. Summary

Cerneala will support a compiled pair:
```text
App.crn
App.crn.cs
```
with a root `Application`, similar in role to `App.xaml` from WPF and `App.axaml` from Avalonia:
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
            TargetType="TextBlock">
            @default
            {
                Foreground = $AccentBrush;
            }
        </Aspect>
    </Application.Resources>
</Application>
```
The C# companion becomes a real application object, not the special static hook that the generator currently uses:
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
The generator will resolve `StartupWindow` semantically, via Roslyn, to any concrete type derived from `Window`. `WelcomeWindow` is only the name chosen by the application in the example; there is no reserved class name. The generator will generate the entry point and the hosting descriptor from `Application`, not from a class magically named `MainWindow`.

The functional inspiration is deliberately restricted to the useful contracts Cerneala:

- WPF uses `Application` for startup, main window, shutdown and application-scope resources:
  <https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/application-management-overview>
- WPF allows the declarative choice of the initial window by `StartupUri`:
  <https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.startupuri>
- Avalonia uses `App.axaml` for global resources and `App.axaml.cs` for choosing the main window after initializing the framework:
  <ZZZ BLACK12ZZZ
- Avalonia explicitly separates desktop lifetime and shutdown policies:
  <https://docs.avaloniaui.net/docs/fundamentals/application-lifetimes>

We do not copy the WPF loading URI. Cerneala has compiled markup and can provide a more secure contract: `StartupWindow="WelcomeWindow"` is a type name resolved at build, not a path interpreted at runtime.

`<Application>`, `StartupWindow` and `ShutdownMode` are the new syntax introduced by this plan. The resource declaration does not receive a second invented language: it uses exactly the property-element and the resource elements already accepted by the generator (`<Owner.Resources>`, `SolidColorBrush`, `Aspect`, `Tween`, `Spring` and, where there is a visual namescope, `MotionClip`).

## 2. Baseline and the current problem

### 2.1 The startup is associated with the name `MainWindow`

`UiMarkupGenerator` enumerates the types named exactly `MainWindow` and sets:
```csharp
bool generateStartup =
    mainWindowCount == 1 &&
    windowPair.Pair.TypeSymbol.Name == "MainWindow";
```
`UiMarkupWindowGenerator` then emits either a `Main()` or a module initializer for hosting. The application does not explicitly state which window it starts; the class name decides this accidentally.

### 2.2 `App` is not an application object

The generator optionally looks for a static class `App` with a single:
```csharp
static void ConfigureServices(IServiceCollection services)
```
There is no:

- `Application.Current`;
- `Application.Resources`;
- `MainWindow`, `Windows` or `ActiveWindow` at application level;
- startup and exit events/overrides;
- declarative shutdown policy;
- a markup/code-behind pair for the application.

### 2.3 The runtime encodes a single shutdown policy

`WindowApplicationRuntime.Close` closes all other windows when `mainWindow` closes. The behavior is equivalent to `OnMainWindowClose`, but it is not configurable and does not belong to a `Application` object.

### 2.4 The resources have no scope of application

`WindowApplicationRuntime` can install the same `IResourceProvider` in each `UIRoot`, and `UIRoot` already knows how to react to resource changes. However, the generated startup does not build a resource provider from the application markup.

The result is that common resources must be kept in a visual owner, repeated or provided manually from the hosting. This becomes a real obstacle when windows and views are divided into components.

## 3. Fixed decisions

- The public type will be `Cerneala.UI.Application`.
- The companion `App.crn.cs` will declare a partial, concrete class derived from `Application`.
- `App.crn` will use root `<Application>`.
- An executable can have at most one `Application` paired definition.
- The generated entry point will belong to the definition `Application`, not to the window called `MainWindow`.
- The initial syntax will be `StartupWindow="<type-name>"`, not `StartupUri`.
- `StartupWindow` will be resolved semantically in the scope of the C# companion and must point to a concrete, accessible type derived from `Window`.
- `MainWindow` will not be a reserved type name; any window indicated by `StartupWindow` initially becomes the value of the runtime property `Application.MainWindow`.
- `ShutdownMode` will accept `OnLastWindowClose`, `OnMainWindowClose` and `OnExplicitShutdown`.
- The default value for applications with `App.crn` will be `OnLastWindowClose`, just like established desktop models.
- `Application.Resources` will be the same observable provider installed as application-scope in all application windows.
- The local element resources, `UserControl` and `Window` will continue to have priority over application resources.
- `<Application.Resources>` will accept the same existing declarations according to the application scope: brushes, `<Aspect>`, `<Tween>` and `<Spring>`.
- Motion clips that refer to elements by name will not be legal in `Application`, because the application does not have a visual tree or namescope.
- `@when`, `@if`, `@set`, `@animate`, visual copies, `Name`, `DataType` and element event handlers will not be legal directly on `Application`.
- `ConfigureServices`, startup and exit will be court overrides; the old static hook remains only in the legacy fallback without `App.crn`.
- In the absence of a `Application` definition, the existing `MainWindow` convention will remain temporarily functional for compatibility.
- If `App.crn` exists, the `MainWindow` fallback is completely disabled; two descriptors and two entry points are not issued.
- The existing hosted mode remains supported: the external host pumps the runtime, but `Application` and the service provider have the same lifecycle as in standalone.

## 4. Proposed public contract

### 4.1 `Application`

Minimum public contract:
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
Semantics:

- `Current` is set only once on the UI thread before `ConfigureServices` and is reset after the application is completely finished.
- `Resources` exists immediately after construction and is populated by the code generated before the creation of the first window.
- `Services` becomes available after building the provider and before `OnStartup`.
- `Windows` is a read-only view of the windows known to the runtime.
- `MainWindow` can be read and changed on the UI thread; changing it does not automatically close the previous window.
- `Shutdown(int)` forcefully closes all windows, raises `Exit` exactly once and sets the exit code of the process.
- cross-thread access to lifecycle operations throws the same descriptive exception used by Window APIs.

### 4.2 Lifecycle

Mandatory order:
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
`OnStartup` runs before the declarative instantiation of `StartupWindow`, similar to the WPF contract for `StartupUri`. If the startup explicitly requests shutdown or fails, the declarative window is not created.

### 4.3 Shutdown

- `OnLastWindowClose`: the application stops after successfully closing the last window.
- `OnMainWindowClose`: the application stops after successfully closing the window currently referenced by `MainWindow`.
- `OnExplicitShutdown`: closing the windows does not automatically stop the application; only `Shutdown` or stopping the host does it.
- cancellation of the `Closing` event does not trigger shutdown and does not raise `Exit`.
- closing an owned window continues to follow the current contract.
- `Exit` is raised only once, including when the startup fails after installing the application.

### 4.4 Application-scope resources

The lookup remains near-to-far:
```text
element
-> ancestors / control / window
-> Application.Resources
-> theme/default provider
```
Changing a resource observable from `Application.Resources` only invalidates dependent consumers from all attached `UIRoot`s. The windows opened afterwards see the last value.

Application-scope aspects apply to all windows, but closer aspects keep the current precedent. Global motion specs are reusable definitions; the Motion execution remains owned by the consuming window's `UIRoot`.

## 5. Non-objectives

- navigation framework, router or `Page`;
- support Linux/macOS or lifetimes Avalonia-style for mobile/browser;
- pack URI, runtime XML loading or choosing the startup by file path;
- merged resource dictionaries and included in this stage;
- hot reload for `App.crn`;
- splash screen;
- session activation, protocol activation or file activation;
- global handler for unprocessed exceptions;
- several `Application` instances in the same UI process/thread;
- dividing `PresentationWindow.crn` into chapters; this will be a separate plan;
- the transformation of all internal hosting APIs into public APIs.

## 6. Estimated files

### Production

- `UI/Application.cs` - the new public application object.
- `UI/ApplicationShutdownMode.cs` - public shutdown policy.
- `UI/ApplicationStartupEventArgs.cs` and `UI/ApplicationExitEventArgs.cs` - public lifecycle, if the arguments cannot be kept simple without dedicated types.
- `UI/Hosting/Windows/GeneratedWindowApplication.cs` - descriptor based on application factory and complete lifecycle.
- `UI/Hosting/Windows/WindowApplicationRuntime.cs` - main-window/shutdown delegation to `Application`.
- `UI/Elements/UIRoot.cs` and/or resource-provider composition - only if the previous application/theme cannot be expressed by the existing contract.
- `Cerneala.SourceGen/UiMarkupApplicationGenerator.cs` - pairing, validation and issuing for `<Application>`.
- `Cerneala.SourceGen/UiMarkupGenerator.cs` - catalog compilation-wide and the unique selection of the startup.
- `Cerneala.SourceGen/UiMarkupWindowGenerator.cs` - elimination of the primary entry point responsibility and keeping the legacy fallback.

### Tests and integration

- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorApplicationTests.cs`.
- `tests/Cerneala.Tests/UI/Hosting/ApplicationRuntimeTests.cs`.
- fake fixtures for the Window platform, extended only where the new lifecycle requires it.
- `CernealaPresentation/App.crn`.
- `CernealaPresentation/App.crn.cs`.

### Documentation

- new/updated pages in `docs-site/documentation/classes/`.
- `docs-site/documentation/manifest.json`.
- `docs/getting-started.md`.
- the markup documentation for the application definition, in a suitable existing page or in a new document oriented to authoring.

The list is an estimate. No decorative abstractions are created if the implementation can cleanly reuse `ResourceDictionary`, `IObservableResourceProvider`, `WindowApplicationRuntime` and the existing descriptor.

## 7. Implementation stages

### Stage 0 - Baseline RED and compatibility contracts

- [x] Add SourceGen RED tests that demonstrate that `<Application StartupWindow="ShellWindow">` paired with `App : Application` is not recognized in the baseline.
- [x] Add a RED test that requires the entry point to be issued from the definition `Application` even if the startup window is not called `MainWindow`.
- [x] Add RED tests for standalone and hosted mode, keeping the current difference between `Main()` and initializer modules.
- [x] Characterizes the legacy behavior through tests: an executable without `App.crn`, with exactly one `MainWindow`, continues to emit the current startup.
- [x] Add RED tests for duplicate application, missing companion, wrong root, wrong base type and user-declared constructor.
- [x] Add RED tests for `StartupWindow` missing, unknown, ambiguous, inaccessible, abstract, non-`Window` and reference to `Application` itself.
- [x] Add RED runtime tests for the three shutdown modes, `Closing` cancellation, exit code and `Exit` exactly once.
- [x] Add a RED test for the complete order of the lifecycle and the disposal of the provider.
- [x] Reindexes `Cerneala.slnx` after test changes.

**Gate Stage 0**

- [x] New tests fail exclusively because of missing `Application` contracts, and existing legacy tests remain green.
- [x] The syntax and order of the lifecycle in the tests coincide with the decisions fixed in this plan.

### Stage 1 - Application object and lifecycle ownership

- [x] Enter `Cerneala.UI.Application` with `Current`, `Resources`, `Services`, `MainWindow`, `Windows`, `ActiveWindow`, `ShutdownMode`, lifecycle and thread affinity.
- [x] Enter the enum `ApplicationShutdownMode` with exactly the three set values.
- [x] Enter startup/exit arguments only if they are required for command-line args and exit code; don't create an unnecessary hierarchy of events.
- [x] Moves the shutdown decision from `WindowApplicationRuntime.Close` to the current application policy.
- [x] Keeps the ownership of native windows and contexts in `WindowApplicationRuntime`; `Application` orchestrates, does not duplicate runtime dictionaries.
- [x] Allows changing `MainWindow` without closing the old window and applies `OnMainWindowClose` to the designated window at the time of closing.
- [x] Make `Shutdown` idempotent and safe if called from `OnStartup`, from an event handler or after closing all windows.
- [x] Ensure deterministic cleanup after partially failed startup: windows, runtime, services and `Application.Current`.
- [x] Extends runtime tests for standalone and hosted disposal, including repeated shutdown and reset between tests.
- [x] Reindexes `Cerneala.slnx`.

**Gate stage 1**

- [x] All `ApplicationRuntimeTests` and `WindowRuntimeTests` tests are green.
- [x] There are no two sources of truth for `MainWindow`, window list or shutdown.
- [x] Canceled closing does not produce `Exit`, disposal or closing of the other windows.

### Stage 2 - Generator for `App.crn`

- [x] Add pairing for a document with root `<Application>` and partial C# companion derived from `Application`.
- [x] Requests at most one `Application` definition in an executable output and issues accurate diagnosis for duplicates.
- [x] Solve `StartupWindow` with Roslyn in the scope of the companion, without reflection and without runtime type lookup.
- [x] Accepts the simple imported name and the fully qualified name according to the Roslyn rules already used by custom elements/DataType.
- [x] Connects the startup window to its markup pair and generates the necessary DI records for `Window<TViewModel>` and view model.
- [x] Issue the `App` constructor that initializes the markup, but preserves extensibility through the protected overrides of the base class.
- [x] Move the `Main()`/module initializer to the source generated for `Application`.
- [x] Replaces the descriptor centered on `createMainWindow` with a descriptor centered on `createApplication` plus the factory of the startup window.
- [x] Disable startup generation from `MainWindow` when there is a `Application` definition.
- [x] Keep the legacy fallback only when there is no `<Application>` paired.
- [x] Keeps the static hook `App.ConfigureServices` only in the legacy fallback and issues a clear diagnosis if it is mixed with the new `App : Application`.
- [x] Add diagnostics for illegal attributes/directives/children on `<Application>`.
- [x] Checks the deterministic output of the incremental generator regardless of the order `AdditionalFiles`.
- [x] Reindexes `Cerneala.slnx`.

**Gate stage 2**

- [x] All Stage 0 SourceGen RED instances are green.
- [x] An executable with `App.crn` and a window `ShellWindow` generates exactly one entry point and starts `ShellWindow`.
- [x] A legacy project without App continues to generate exactly the previous startup.
- [x] No new startup path uses reflection, `Activator.CreateInstance` or filename interpreted at runtime.

### Stage 3 - Global resources and cross-window precedent

- [x] Expose `Application.Resources` through `ResourceDictionary`/`IObservableResourceProvider`, reusing the existing observable infrastructure.
- [x] Reuses the existing `<Application.Resources>` syntax and the existing brush/`Aspect`/`Tween`/Z`Spring` elements; does not introduce parallel directives such as `@resources`, `@brush` or `@aspect`.
- [x] Extends the parser/emitter so that the valid declarations from `<Application.Resources>` populate the application's resources.
- [x] Add compilation-wide support for reference from Window/UserControl to resources declared in App, with compile-time diagnostics for incompatible name or type.
- [x] Allows `<Tween>` and `<Spring>` application-scope through the same grammar and the same validations already used in `Window.Resources`/Z`UserControl.Resources`.
- [x] Reject application-scope Motion clips that have nominal targets or assignments to elements.
- [x] Installs the application provider in each `UIRoot` created by `WindowApplicationRuntime`.
- [x] Keep the lookup order local -> application -> theme/default and explicitly document the shadowing.
- [x] Add tests with two windows consuming the same application-scope resource.
- [x] Add tests that modify a global resource and demonstrate the invalidation of both windows, without repeating work on unaffected consumers.
- [x] Add tests for local shadowing and for a window opened after changing the resource.
- [x] Add idle-frame regression: after stabilizing a global change, idle frames no longer contain layout/render work.
- [x] Reindexes `Cerneala.slnx`.

**Gate stage 3**

- [x] App resources are visible in all windows and are correctly overwritten by closer scopes.
- [x] A global update invalidates only real dependencies and converges back to idle.
- [x] The generator rejects all forms of Motion application-scope that would require a global visual tree.

### Stage 4 - Lifecycle standalone and hosted end-to-end

- [x] Adapts `GeneratedWindowApplication.Run` to create, install and close `Application` in the fixed order.
- [x] Adapts `RegisterStartup`, `PumpHosted` and `StopHosted` so that the external host remains the owner of the pump, and the App/services are created only once.
- [x] Propagate process arguments to startup in standalone; explicitly defines the arguments available in hosted mode.
- [x] Propagate the exit code from `Application.Shutdown(int)` to the standalone entry point.
- [x] Check startup failure at each point: construct App, init resources, ConfigureServices, build provider, OnStartup, resolve Window, create Window, first Show.
- [x] Secure `OnExit`/`Exit` exactly once for all failures that occurred after installing `Application.Current`.
- [x] Keep the original exception and add descriptive context for startup target and failed stage.
- [x] Add integration tests with fake platform for main window, secondary window, hosted pump and shutdown modes.
- [x] Reindexes `Cerneala.slnx`.

**Gate Stage 4**

- [x] Standalone returns the requested exit code and does not leave `Application.Current`, services or runtime installed.
- [x] Hosted mode creates App and startup window only once, even after repeated pumps.
- [x] A startup exception does not leave native windows, provider or static state hanging.

### Stage 5 - Migration `CernealaPresentation`

- [x] Add `CernealaPresentation/App.crn` with `StartupWindow="MainWindow"` and explicit shutdown mode.
- [x] Add `CernealaPresentation/App.crn.cs` derived from `Application`.
- [x] Move any DI application-scope configuration from the old static hook to the instance override, if it exists.
- [x] Move only the resources actually consumed by several windows to the App; do not mechanically empty local Window resources.
- [x] Demonstrates that `MainWindow`, `PresentationWindow` and `MotionLabWindow` see the same global resources where intended.
- [x] Keep the Continue behavior, the opening of secondary windows and automation/benchmark startup.
- [x] Checks that the Presentation has exactly one generated descriptor and no entry point derived from the `MainWindow` convention.
- [x] Run native Presentation and check startup, close, explicit secondary windows and exit process.
- [x] Reindexes `Cerneala.slnx`.

**Gate Stage 5**

- [x] Presentation starts from the declaration `App.crn`, not from the class name `MainWindow`.
- [x] All three windows work, and the closing follows `ShutdownMode` declared.
- [x] The existing Presentation Benchmark can start and stop the application without fragile timing or automation changes.

### Stage 6 - API docs, authoring docs and final compatibility

- [x] Use the skill `writing-api-documentation` for all types and new/changed public members.
- [x] Add/update the pages from `docs-site/documentation/classes/` for `Application`, shutdown mode, event args and the startup descriptor.
- [x] Updates `docs-site/documentation/manifest.json` for each new or renamed page.
- [x] Updates `docs/getting-started.md` with the standard App/Window pair and removes the `MainWindow` convention from the recommended flow.
- [x] Documents the syntax `<Application>`, `StartupWindow`, `ShutdownMode`, global resources, shadowing and forbidden directives.
- [x] Document the legacy fallback as a compatibility mechanism, not as a recommended style.
- [x] Add a complete minimal example that compiles without `Program.cs`.
- [x] Run a public API diff and confirm that all additions are intended, documented and nullable correctly.
- [x] Reindexes `Cerneala.slnx`.

**Gate stage 6**

- [x] The documentation describes exactly the tested behavior and does not promise URI loading, merged dictionaries or unimplemented lifetimes.
- [x] The docs manifest is synchronized and public, the diff API does not contain accidental changes.

### Stage 7 - Full check

- [x] Run targeted SourceGen tests.
- [x] Runs the targeted runtime tests for Application/Window/resources.
- [x] Run the entire solution.
- [x] Run build Release and formatter verification.
- [x] Run Presentation native smoke and the permanent frame budget benchmark to detect startup/resource wiring regressions.
- [x] Runs `git diff --check`.
- [x] Regenerates `FileTree.md`.
- [x] Finally reindex `Cerneala.slnx` and check `doctor/status`.

**Gate stage 7**

- [x] All final commands are green.
- [x] There are no Presentation processes, hosts, HWNDs or services left after the tests.
- [x] The App startup does not introduce frames above the budget in the Presentation benchmark.

## 8. Verification Orders
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
## 9. Recommended order

1. Freeze contracts in RED tests.
2. Enter the application object and remove the shutdown from the hardcoded logic of the main window.
3. Generate App pairing and type-safe startup.
4. Bind the application-scope resources to all `UIRoot`s.
5. Close the standalone/hosted lifecycle and all failure paths.
6. Migrate Presentation as the first real consumer.
7. Update the API docs, authoring docs and run the full check.

## 10. Stop conditions

- Do not add `StartupUri` or runtime markup loading just for familiarity with WPF.
- Do not extend the plan to navigation/pages.
- Do not implement merged dictionaries until simple application resources are complete and tested.
- Do not make `Application` the owner of the renderer, input or native contexts; they remain at `WindowApplicationRuntime`.
- Do not keep two active startup paths when there is App markup.
- Don't relax diagnostics to allow global Motion clips without namescope.
- Do not break `PresentationWindow.crn` in this plan; The App makes it possible to organize the resources, but the componentization is a separate delivery.
- Do not remove the legacy fallback until all samples and consumers of the repo have migrated and a breaking change is approved separately.

## 11. The definition of ready

- [x] An executable project can declare `App.crn` + `App.crn.cs` and does not need `Program.cs`.
- [x] `StartupWindow` can indicate any valid concrete type `Window`, regardless of its name.
- [x] The generator emits exactly one entry point/descriptor from the App and no longer depends on the name `MainWindow`.
- [x] `Application.Current`, `Resources`, `Services`, `MainWindow`, `Windows`, `ActiveWindow`, the lifecycle and the shutdown work according to the contract.
- [x] The resources declared in the App are visible and observable in all windows, with the correct local precedent.
- [x] The three shutdown modes are tested including for canceled close, replaced main window and explicit exit code.
- [x] Standalone and hosted modes have complete cleanup and events raised exactly once.
- [x] Legacy projects without App continue to work through the documented fallback.
- [x] `CernealaPresentation` uses the new App pair and starts `MainWindow` declaratively.
- [x] All public APIs are documented in the official source `docs-site/documentation/classes/`.
- [x] The targeted tests, the complete suite, the Release build, the formatter, the native smoke and the Presentation benchmark are green.