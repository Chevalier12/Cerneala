# Markup And Source Generation

> Code: `Cerneala.SourceGen`, `UI/Markup`, `UI/Hosting/Windowing/GeneratedWindowApplication.cs`, `Cerneala.Language/Diagnostics` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The source generator lowers `.crn` markup into typed C# at build time: one
generated file per markup file, plus the process entry point for an executable.
It does not parse or validate markup itself (it reuses `Cerneala.Language`, see
[language-tooling.md](language-tooling.md)), and nothing is loaded from markup at
runtime. Generated code calls the public `GeneratedMarkup` helpers, which own
the runtime behavior of resources, bindings, Motion, Prism and Timbre.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `UiMarkupGenerator` | `Cerneala.SourceGen/UiMarkupGenerator.cs` and about 40 `partial` files | The only markup `IIncrementalGenerator`. `UiMarkupApplicationGenerator.cs`, `UiMarkupWindowGenerator.cs`, `UiMarkupUserControlGenerator.cs` and `UiMarkupSceneComponentGenerator.cs` are partial files of this one class, not separate generators. | Roslyn, per compilation |
| `PrismCatalogGenerator` | `Cerneala.SourceGen/Prism/PrismCatalogGenerator.cs` | Second generator: turns `prism-catalog.json` into `PrismCatalog.Generated.g.cs` and `PrismOperations.Generated.g.cs`. See [prism-technical-design.md](prism-technical-design.md). | Roslyn |
| `MarkupSource`, `MarkupElement`, `EmissionMarkupDocument` | `UiMarkupGenerator.cs:726-732`, `Cerneala.SourceGen/MarkupDom.cs` | One `.crn` file: the `Cerneala.Language` parse plus an emission tree built with `MarkupElement.FromSyntax`. | Generator pipeline |
| `SourceGeneratorSemanticModel` | `Cerneala.SourceGen/SourceGeneratorSemanticModel.cs` | Wraps the `Cerneala.Language` semantic model of one file. | Generator pipeline |
| `UiMarkupDirectiveParser` | `Cerneala.SourceGen/UiMarkupDirectiveParser.cs` | `ParseDirectiveContent(MarkupElement, DirectiveContentKind)` reads `@` directive content of an element. | Static |
| `GenerationScope` | private partial class in about 26 emitter files (`UiMarkupElementEmitter.cs`, `UiMarkupPropertyEmitter.cs`, `UiMarkupBindingResolver.cs`, `UiMarkupReactiveEmitter.cs`, `UiMarkupMotionActivationEmitter.cs`, `UiMarkupTimbreEmitter.cs`, `UiMarkupAspectEmitter.cs`, `Prism/Emission/*`) | Emits C# lines for one file and collects its diagnostics. | One file, one generator run |
| `SourceGeneratorDiagnosticAdapter` | `Cerneala.SourceGen/SourceGeneratorDiagnosticAdapter.cs` | Builds Roslyn descriptors from `CernealaDiagnosticCatalog.All` and converts language diagnostics to Roslyn diagnostics. | Static |
| `GeneratedMarkup` | `UI/Markup/GeneratedMarkup*.cs` (public static partial class) | Runtime surface called by generated code: `AttachResource`, `AttachPropertyBinding`, `StartMotionProperty`, `AttachPrism`, `AttachTimbre`, `GetTemplatePart`, and others. | Static; the controllers it creates belong to the owner element |
| `GeneratedUiFactory` | `UI/Markup/GeneratedUiFactory.cs` | Wraps a generated `Create` delegate; `Create()` returns `MarkupResult<UIElement>` and turns a throw or a null root into a `MARKUP030` diagnostic. | Caller |
| `GeneratedWindowApplication`, `GeneratedWindowStartupDescriptor` | `UI/Hosting/Windowing/GeneratedWindowApplication.cs` | Runtime target of the generated entry point: `Run(descriptor[, args])` and `RegisterStartup(descriptor)`. | Process (static state) |
| `ApplicationBackendAttribute` | `UI/Hosting/Windowing/ApplicationBackendAttribute.cs` | Assembly attribute that names the backend class the generated startup registers. | Assembly |

## Data Flow

1. **Input.** A project passes its markup to the compiler as
   `<AdditionalFiles Include="**\*.crn" />` and references `Cerneala.SourceGen`
   as an analyzer (`Cerneala.csproj:50-51`,
   `CernealaPresentation/CernealaPresentation.csproj:13-16`).
   `Cerneala.SourceGen.csproj:28-34` ships `Cerneala.Language.dll` next to the
   analyzer.
2. **Filter and parse.** `UiMarkupGenerator.Initialize`
   (`UiMarkupGenerator.cs:204-236`) keeps files for which
   `CernealaDocumentPath.IsMarkupFile` is true (extension `.crn`, case
   ignored) and builds a `MarkupSource` for each.
3. **Semantic analysis.** For each file, `AnalyzeMarkupFile` (`:238-258`)
   builds a `CernealaCompilation` in `AnalysisMode.Build` over
   `RoslynCompilationSymbols` of the C# compilation, and takes the file's
   semantic model. The incremental step is named
   `CernealaLanguageSemanticModel`.
4. **Pairing.** One `RegisterSourceOutput` calls `GenerateFiles` (`:260-421`)
   with every file. A file whose root is `Application`, `Window`, or another
   component (a `UserControl`-style root or `Scene2D`) and that has a C#
   partial class companion is *paired*: the generator completes that class. A
   file with no companion gets a static factory class
   `Cerneala.GeneratedUi.<Name>Factory` with `Create()` and
   `AsGeneratedFactory()` (`:455-576`). The name comes from the file path;
   duplicates are disambiguated, then suffixed with a hash (`:423-453`).
5. **Startup.** In an executable project (any output kind other than a
   library), startup goes to the single paired `Application`, or, when the
   project has no `Application` document, to the single paired `Window` named
   `MainWindow` (`:313-321`). The generator then resolves the backend
   (`UiMarkupBackendSelection.cs:27-97`). If that fails, it reports
   `CERNEALAUI015` and generates no file at all for the project (`:328-335`).
6. **Validation gate per file.** `TryGetEmissionDocument` (`:610-630`): if the
   file's semantic model has any Error diagnostic, every diagnostic of that
   model is reported and the file is skipped.
7. **Emission.** `GenerationScope` walks the emission tree and writes C#
   lines. Generated code constructs controls and sets typed properties
   directly. For behavior that must react at runtime it calls `GeneratedMarkup`
   helpers, for example `AttachResource` for a resource reference
   (`UiMarkupPropertyEmitter.cs:218`). Data bindings are lowered to
   `GeneratedMarkup.AttachPropertyBinding` (`UiMarkupBindingResolver.cs:1112`);
   their syntax and rules are in
   [markup data bindings](../reference/markup-data-bindings.md).

Example: a project holds `View.crn` with `<Button Content="Migration baseline" />`.
The generator produces exactly one source file and no Error diagnostic. The
same text in a file named `View.cui.xml` produces no source and no diagnostic
(`tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorFileExtensionTests.cs`:
`CrnMarkupGeneratesSource`, `LegacyCuiXmlMarkupIsIgnored`).

## Backend Selection And Startup

An executable needs exactly one assembly attribute:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

(`SdlGpuApplicationBackend` is declared in `Cerneala.Backends.SdlGpu/Hosting/SdlGpuApplicationBackend.cs`, namespace `Cerneala.UI.Hosting.Sdl`.)

The selected type must be a public, non-generic static or concrete class with
exactly one `public static void EnsureRegistered()` (`UiMarkupBackendSelection.cs:53-93`).
The generated startup calls `<Backend>.EnsureRegistered();` before it starts
the application (`AppendApplicationBackendRegistration`, `:121-127`).
`SdlGpuApplicationBackend.EnsureRegistered` registers the SDL windowing
backend in `WindowingBackendRegistry`.

What the generated startup looks like depends on whether the project already
has an entry point:

| Case | Generated code |
|---|---|
| Paired `Application`, no user `Main` | `private static int Main(string[] args)` that returns `GeneratedWindowApplication.Run(CreateDescriptor(), args)` (`UiMarkupApplicationGenerator.cs:285-288`). |
| Paired `MainWindow`, no `Application`, no user `Main` | `private static void Main()` that calls `GeneratedWindowApplication.Run(CreateDescriptor())` (`UiMarkupWindowGenerator.cs:338-341`). |
| The project already declares `Main` | A `[ModuleInitializer]` method that calls `GeneratedWindowApplication.RegisterStartup(CreateDescriptor())` (`UiMarkupApplicationGenerator.cs:293-297`, `UiMarkupWindowGenerator.cs:346-350`). |

## Build Diagnostics

All markup diagnostics are defined once in `CernealaDiagnosticCatalog`
(`Cerneala.Language/Diagnostics/CernealaDiagnosticCatalog.cs:12-55`), which
has 44 descriptors:

| IDs | Category | Meaning |
|---|---|---|
| `CERNEALAUI001`–`017` | `Cerneala.UiMarkup` | Malformed markup, unsupported elements and properties, invalid values, document shape, directives, binding sources, `UserControl`/`Window`/`Application` declarations and startup, backend selection (`015`), sprite animation, `Scene2D` components (`017`). |
| `CERNEALAUI020`–`026` | `Cerneala.UiMarkup.Motion` | Motion syntax, targets, events, types, composition, lifecycle, capability. |
| `CERNEALAUI030`–`033` | `Cerneala.UiMarkup.Timbre` | Timbre syntax, references, values, directive context. |
| `PRISM1001`–`1003`, `PRISM2001`–`2013` | `Cerneala.Prism.Markup`, `Cerneala.Prism.Binding` | Prism markup syntax and binding. |

Each descriptor has two severities. `BuildSeverity` is `Error` for all 44.
`EditorSeverity` is `Information` only for `CERNEALAUI001` and `PRISM1002`
(input that is merely incomplete while typing), and `Error` for the rest.
`SourceGeneratorDiagnosticAdapter` always uses `BuildSeverity`
(`SourceGeneratorDiagnosticAdapter.cs:16`).

Diagnostics reach Roslyn by three routes:

- language diagnostics of a file, through `TryGetEmissionDocument` (`UiMarkupGenerator.cs:624-627`);
- direct reports in `GenerateFiles`, for example a second `Application` in an executable (`CERNEALAUI014`, `:273-280`), and backend selection (`CERNEALAUI015`);
- `GenerationScope.Report` during emission. It drops a diagnostic with the same ID, span and message as one already reported, and marks the file as failed (`:888-902`).

Outside the catalog: `PrismCatalogGenerator` reports `PRISM3000`–`PRISM3007`
against `prism-catalog.json`, and the runtime uses a separate `MARKUP###`
family (for example `MARKUP030` in `GeneratedUiFactory`).

## Lifecycle And Ownership

- The generator is stateless between runs. Roslyn caches the incremental steps.
- `GeneratedMarkup` helpers create controllers and attach them to the owner
  element. `AttachResource` registers its controller with
  `owner.AddLifecycleBehavior(controller)` and returns it as `IDisposable`
  (`GeneratedMarkupResources.cs:9-30`).
- `GeneratedWindowApplication` keeps the registered startup descriptor in
  static state. Registering a second, different descriptor throws
  `"Only one generated Application startup descriptor may be registered."`
  (`GeneratedWindowApplication.cs:45-54`).

## Invariants

- Only `.crn` files are markup; `.cui.xml` is ignored without diagnostics.
  `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorFileExtensionTests.cs:CrnMarkupGeneratesSource`, `:LegacyCuiXmlMarkupIsIgnored`.
- An executable with zero or two `ApplicationBackend` attributes gets one
  `CERNEALAUI015` Error ("Exactly one ..." or "found 2").
  `UiMarkupGeneratorBackendSelectionTests.cs:ExecutableStartupRequiresAnExplicitBackendSelection`, `:ExecutableStartupRejectsDuplicateBackendSelections`.
- Changing the attribute's backend type regenerates `BackendB.EnsureRegistered()`
  and equals a fresh run.
  `UiMarkupIncrementalTests.cs:BackendAttributeChangeUpdatesGeneratedRegistration`.
- A paired `Application` emits `private static int Main(string[] args)` that
  returns `GeneratedWindowApplication.Run(CreateDescriptor(), args)`.
  `UiMarkupGeneratorApplicationTests.cs:PairedApplicationOwnsStandaloneEntryPointForArbitraryStartupWindow`.
- With a user `Main`, the generated `MainWindow` startup becomes a module
  initializer that calls `RegisterStartup`.
  `UiMarkupGeneratorTests.cs:ExecutableMainWindowEmitsAutomaticEntryPointOrHostedDescriptor`,
  `UiMarkupIncrementalTests.cs:AddingEntryPointChangesGeneratedStartupToModuleInitializer`.
- A paired `Scene2D` component creates an independent instance at each use.
  `UiMarkupGeneratorSceneComponentTests.cs:SceneComponentPairsMarkupWithSceneClassAndCreatesIndependentLogicalInstances`.
- The catalog holds exactly the 44 IDs listed above, each once, each with
  build severity `Error`; editor mode lowers `CERNEALAUI001` to `Information`
  and keeps `CERNEALAUI002` as `Error`.
  `tests/Cerneala.Tests.Language/DiagnosticCatalogTests.cs:CommonCatalogContainsEveryCurrentDescriptorExactlyOnce`, `:EditorModeReducesOnlyTransientIncompleteDiagnostics`.
- A file with a language Error diagnostic produces no generated source: netestat.
- `GenerationScope.Report` de-duplication: netestat.
- Backend selection failure suppresses every generated file of the project: netestat.

## Known Limitations

- `ApplicationBackendAttribute` is read only by the generator. Runtime startup
  does not read it; it relies on the generated `EnsureRegistered()` call.
- The catalog has no descriptors `CERNEALAUI018`, `019`, `027`, `028`, `029`.
- No literal producer of `PRISM2006` or `PRISM2007` was found outside the
  catalog and its test. A computed ID was not ruled out.
