# File Tree

Generated from `.`.

```text
./
|-- Cerneala.SourceGen/
|   |-- Cerneala.SourceGen.csproj
|   +-- UiMarkupGenerator.cs
|-- docs/
|   |-- bug-reports/
|   |-- diagrams/
|   |   |-- cerneala-drawing-flowchart.svg
|   |   |-- retained-frame-loop.md
|   |   +-- ui-layer-boundaries.md
|   |-- superpowers/
|   |   +-- plans/
|   |       |-- 2026-07-03-fix-retained-render-frame-contract.md
|   |       |-- 2026-07-03-fix-tree-mutation-invalidation.md
|   |       |-- 2026-07-03-integrate-style-phase.md
|   |       |-- 2026-07-04-cache-input-route-hit-test.md
|   |       |-- 2026-07-05-authoring-preview-completion-gate.md
|   |       |-- 2026-07-05-clarify-layout-scheduler-contract-and-diagnostics.md
|   |       |-- 2026-07-05-clarify-package-boundary-dependencies.md
|   |       |-- 2026-07-05-clarify-text-services-mvp.md
|   |       |-- 2026-07-05-complete-textblock-layout-contract.md
|   |       |-- 2026-07-05-consolidate-button-content-composition.md
|   |       |-- 2026-07-05-core-preview-completion-gate.md
|   |       |-- 2026-07-05-create-retained-ui-mvp-vertical-slice.md
|   |       |-- 2026-07-05-default-theme-and-style-vertical-slice.md
|   |       |-- 2026-07-05-fix-focus-visibility-semantics.md
|   |       |-- 2026-07-05-fix-viewport-and-pre-input-frame-contract.md
|   |       |-- 2026-07-05-freeze-later-experimental-scope.md
|   |       |-- 2026-07-05-implement-inherited-property-tree-propagation.md
|   |       |-- 2026-07-05-items-scroll-data-vertical-slice.md
|   |       |-- 2026-07-05-next-authoring-preview-plan-index.md
|   |       |-- 2026-07-05-next-core-completion-plan-index.md
|   |       |-- 2026-07-05-next-core-preview-plan-index.md
|   |       |-- 2026-07-05-observable-items-source-and-recycling-stability.md
|   |       |-- 2026-07-05-retained-command-state-refresh.md
|   |       |-- 2026-07-05-retained-semantics-tree-core-contract.md
|   |       |-- 2026-07-05-root-owned-resource-invalidation.md
|   |       |-- 2026-07-05-template-content-presenter-state-contract.md
|   |       |-- 2026-07-05-textbox-editing-viewport-and-caret-contract.md
|   |       |-- 2026-07-05-typed-binding-lifetime-and-two-way-text.md
|   |       |-- 2026-07-05-wire-keyboard-control-activation.md
|   |       |-- 2026-07-05-wire-minimal-retained-input-bindings.md
|   |       |-- 2026-07-06-add-preview-api-scope-guardrails.md
|   |       |-- 2026-07-06-add-retained-stress-budget-tests.md
|   |       |-- 2026-07-06-cache-content-resources-and-textures-lifetime.md
|   |       |-- 2026-07-06-close-retained-lifecycle-subscription-leaks.md
|   |       |-- 2026-07-06-create-developer-preview-docs-and-sample-gate.md
|   |       |-- 2026-07-06-developer-preview-completion-gate.md
|   |       |-- 2026-07-06-harden-layout-authoring-mutation-contracts.md
|   |       |-- 2026-07-06-harden-monogame-render-backend-state.md
|   |       |-- 2026-07-06-next-developer-preview-hardening-plan-index.md
|   |       |-- 2026-07-06-next-runtime-preview-plan-index.md
|   |       |-- 2026-07-06-normalize-viewport-scale-pointer-and-render-coordinates.md
|   |       |-- 2026-07-06-runtime-diagnostics-and-playground-polish.md
|   |       |-- 2026-07-06-runtime-preview-completion-gate.md
|   |       |-- 2026-07-06-textbox-caret-blink-hit-testing.md
|   |       |-- 2026-07-06-wire-platform-services-cursor-and-clipboard.md
|   |       |-- 2026-07-06-wire-tab-focus-navigation-contract.md
|   |       |-- 2026-07-07-modern-motion-system.md
|   |       +-- developer-preview-smoke-failure-fix-plan.md
|   |-- architecture-v2.md
|   |-- developer-preview-checklist.md
|   |-- developer-preview-scope.md
|   +-- getting-started.md
|-- Playground/
|   +-- Cerneala.Playground/
|       |-- .config/
|       |   +-- dotnet-tools.json
|       |-- Content/
|       |   |-- Content.mgcb
|       |   +-- PreviewImage.png
|       |-- Samples/
|       |   |-- AuthoringAppSample.cs
|       |   |-- DiagnosticsSample.cs
|       |   |-- GettingStartedSample.cs
|       |   |-- InvalidationStatsOverlay.cs
|       |   |-- LayoutSample.cs
|       |   |-- PlaygroundText.cs
|       |   |-- RetainedAppSample.cs
|       |   |-- RetainedButtonSample.cs
|       |   |-- RuntimePreviewSample.cs
|       |   |-- SampleSelector.cs
|       |   +-- TextSample.cs
|       |-- app.manifest
|       |-- Cerneala.Playground.csproj
|       |-- Game1.cs
|       |-- Icon.bmp
|       |-- Icon.ico
|       +-- Program.cs
|-- Properties/
|   +-- AssemblyInfo.cs
|-- tests/
|   |-- Cerneala.Tests/
|   |   |-- Architecture/
|   |   |   |-- DeveloperPreviewCompletionTests.cs
|   |   |   |-- DeveloperPreviewScopeTests.cs
|   |   |   |-- MonoGameDependencyBoundaryTests.cs
|   |   |   |-- NamespaceBoundaryTests.cs
|   |   |   +-- RepositoryShapeTests.cs
|   |   |-- Controls/
|   |   |   |-- Primitives/
|   |   |   |   |-- ButtonBaseCommandStateIntegrationTests.cs
|   |   |   |   |-- ButtonBaseCommandTests.cs
|   |   |   |   |-- ButtonBaseTests.cs
|   |   |   |   |-- RangeBaseTests.cs
|   |   |   |   |-- SelectorTests.cs
|   |   |   |   |-- ThumbTests.cs
|   |   |   |   +-- TrackTests.cs
|   |   |   |-- BorderTests.cs
|   |   |   |-- ButtonContentArchitectureTests.cs
|   |   |   |-- ButtonKeyboardActivationTests.cs
|   |   |   |-- ButtonTests.cs
|   |   |   |-- CanvasTests.cs
|   |   |   |-- CheckBoxTests.cs
|   |   |   |-- ComboBoxTests.cs
|   |   |   |-- ContentControlTests.cs
|   |   |   |-- ContentPresenterDefaultTextTests.cs
|   |   |   |-- ContentPresenterTests.cs
|   |   |   |-- ControlTemplateTests.cs
|   |   |   |-- ControlTests.cs
|   |   |   |-- DataTemplateTests.cs
|   |   |   |-- DecoratorTests.cs
|   |   |   |-- ImageTests.cs
|   |   |   |-- InkCanvasTests.cs
|   |   |   |-- ItemContainerGeneratorTests.cs
|   |   |   |-- ItemContainerRecyclePoolTests.cs
|   |   |   |-- ItemsControlRecyclingStabilityTests.cs
|   |   |   |-- ItemsControlRetainedInvalidationTests.cs
|   |   |   |-- ItemsControlTests.cs
|   |   |   |-- ItemsPanelTemplateTests.cs
|   |   |   |-- ItemsSourceObservableTests.cs
|   |   |   |-- ListBoxTests.cs
|   |   |   |-- PanelTests.cs
|   |   |   |-- PasswordBoxTests.cs
|   |   |   |-- ProgressBarTests.cs
|   |   |   |-- ScrollBarTests.cs
|   |   |   |-- ScrollViewerTests.cs
|   |   |   |-- SelectionModelTests.cs
|   |   |   |-- SliderTests.cs
|   |   |   |-- StackPanelTests.cs
|   |   |   |-- TabControlTests.cs
|   |   |   |-- TabItemTests.cs
|   |   |   |-- TemplateBindingTests.cs
|   |   |   |-- TemplatedButtonStateContractTests.cs
|   |   |   |-- TextBlockInvalidationTests.cs
|   |   |   |-- TextBlockLayoutContractTests.cs
|   |   |   |-- TextBlockTests.cs
|   |   |   |-- TextBoxCaretBlinkTests.cs
|   |   |   |-- TextBoxClipboardShortcutTests.cs
|   |   |   |-- TextBoxEditingVisualContractTests.cs
|   |   |   |-- TextBoxTests.cs
|   |   |   |-- TextBoxTwoWayBindingTests.cs
|   |   |   |-- ToggleButtonTests.cs
|   |   |   +-- ToolTipTests.cs
|   |   |-- Docs/
|   |   |   +-- GettingStartedDocsTests.cs
|   |   |-- Drawing/
|   |   |   |-- MonoGame/
|   |   |   |   |-- MonoGameClipStackTests.cs
|   |   |   |   |-- MonoGameDrawingBackendStateTests.cs
|   |   |   |   +-- MonoGameDrawMapperTests.cs
|   |   |   |-- AdvancedDrawCommandTests.cs
|   |   |   |-- DrawCommandListTests.cs
|   |   |   |-- DrawingContextTests.cs
|   |   |   |-- DrawingResourceTests.cs
|   |   |   +-- TextPipelineTests.cs
|   |   |-- Input/
|   |   |   |-- ActionCommandTests.cs
|   |   |   |-- ClickTrackerTests.cs
|   |   |   |-- CommandBindingCollectionTests.cs
|   |   |   |-- CommandingTests.cs
|   |   |   |-- CommandRouterTests.cs
|   |   |   |-- CommandStateSchedulerTests.cs
|   |   |   |-- CursorPlatformIntegrationTests.cs
|   |   |   |-- DragDropControllerTests.cs
|   |   |   |-- ElementInputBridgeTests.cs
|   |   |   |-- ElementInputRouteBuilderTests.cs
|   |   |   |-- FocusManagerTests.cs
|   |   |   |-- FocusPolicyTests.cs
|   |   |   |-- GestureRecognizerTests.cs
|   |   |   |-- HitTestServiceTests.cs
|   |   |   |-- HoverTrackerTests.cs
|   |   |   |-- InputEventsTests.cs
|   |   |   |-- InputFrameTests.cs
|   |   |   |-- InputGestureTests.cs
|   |   |   |-- KeyboardNavigationContractTests.cs
|   |   |   |-- ManipulationProcessorTests.cs
|   |   |   |-- MonoGameInputCoordinateScaleTests.cs
|   |   |   |-- MonoGameInputMapperTests.cs
|   |   |   |-- PointerCaptureManagerTests.cs
|   |   |   |-- PressedStateTrackerTests.cs
|   |   |   |-- RetainedInputBindingTests.cs
|   |   |   |-- RetainedRoutedEventIntegrationTests.cs
|   |   |   |-- RoutedCommandExecutionTests.cs
|   |   |   |-- RoutedEventRouterTests.cs
|   |   |   |-- RoutedEventTests.cs
|   |   |   |-- StylusInputBridgeTests.cs
|   |   |   |-- TextInputBridgeTests.cs
|   |   |   +-- TouchInputBridgeTests.cs
|   |   |-- Playground/
|   |   |   |-- Samples/
|   |   |   |   |-- AuthoringAppSampleContractTests.cs
|   |   |   |   |-- GettingStartedSampleContractTests.cs
|   |   |   |   |-- PlaygroundSampleTests.cs
|   |   |   |   |-- RuntimePreviewIntegrationTests.cs
|   |   |   |   +-- RuntimePreviewSampleContractTests.cs
|   |   |   |-- Game1SourceTests.cs
|   |   |   |-- PlaygroundGameLoopSmokeTests.cs
|   |   |   |-- RetainedAppSampleContractTests.cs
|   |   |   +-- RetainedAppStyleContractTests.cs
|   |   |-- UI/
|   |   |   |-- Accessibility/
|   |   |   |   |-- AccessibilityPlatformTests.cs
|   |   |   |   |-- AuthoringSemanticsContractTests.cs
|   |   |   |   |-- ButtonSemanticsTests.cs
|   |   |   |   |-- RetainedSemanticsCacheTests.cs
|   |   |   |   |-- SemanticsProviderTests.cs
|   |   |   |   |-- SemanticsStressBudgetTests.cs
|   |   |   |   |-- SemanticsTreeTests.cs
|   |   |   |   +-- TextBoxSemanticsTests.cs
|   |   |   |-- Animation/
|   |   |   |   |-- AnimationClockTests.cs
|   |   |   |   |-- AnimationInvalidationTests.cs
|   |   |   |   |-- AnimationSchedulerTests.cs
|   |   |   |   |-- LegacyAnimationCompatibilityTests.cs
|   |   |   |   |-- TransitionTests.cs
|   |   |   |   +-- TypedAnimationTests.cs
|   |   |   |-- Controls/
|   |   |   |   |-- Shapes/
|   |   |   |   |   +-- ShapeTests.cs
|   |   |   |   +-- ListStressBudgetTests.cs
|   |   |   |-- Core/
|   |   |   |   |-- InheritedPropertyTreePropagationTests.cs
|   |   |   |   |-- InheritedUiPropertyTests.cs
|   |   |   |   |-- ReadOnlyUiPropertyTests.cs
|   |   |   |   |-- UiPropertyInvalidationTests.cs
|   |   |   |   |-- UiPropertyRegistryTests.cs
|   |   |   |   |-- UiPropertyStoreTests.cs
|   |   |   |   +-- UiPropertyTests.cs
|   |   |   |-- Data/
|   |   |   |   |-- CollectionViewTests.cs
|   |   |   |   |-- ObservableListTests.cs
|   |   |   |   |-- ObservableValueTests.cs
|   |   |   |   |-- StringPropertyPathTests.cs
|   |   |   |   |-- TypedBindingTests.cs
|   |   |   |   +-- UiPropertyBindingTests.cs
|   |   |   |-- Diagnostics/
|   |   |   |   |-- DirtyTreeDumperTests.cs
|   |   |   |   |-- ElementTreeDumperTests.cs
|   |   |   |   |-- FrameDiagnosticsTests.cs
|   |   |   |   |-- InvalidationTraceTests.cs
|   |   |   |   |-- RenderCacheDumperTests.cs
|   |   |   |   |-- RoutedEventTraceTests.cs
|   |   |   |   |-- RuntimeDiagnosticsTests.cs
|   |   |   |   +-- StyleTraceTests.cs
|   |   |   |-- Elements/
|   |   |   |   |-- ElementHandlerStoreTests.cs
|   |   |   |   |-- ElementLifecycleTests.cs
|   |   |   |   |-- ElementTreeWalkerTests.cs
|   |   |   |   |-- RetainedLifecycleCleanupTests.cs
|   |   |   |   |-- UIElementCollectionInvalidationTests.cs
|   |   |   |   |-- UIElementCollectionTests.cs
|   |   |   |   |-- UIElementInvalidationTests.cs
|   |   |   |   |-- UIElementTreeTests.cs
|   |   |   |   +-- UIRootTests.cs
|   |   |   |-- Hosting/
|   |   |   |   |-- AuthoringPreviewContractTests.cs
|   |   |   |   |-- CorePreviewContractTests.cs
|   |   |   |   |-- DeveloperPreviewContractTests.cs
|   |   |   |   |-- FakeDrawingBackend.cs
|   |   |   |   |-- FakeInputSource.cs
|   |   |   |   |-- FakeUiClock.cs
|   |   |   |   |-- GridAuthoringFrameContractTests.cs
|   |   |   |   |-- MonoGameContentServicesLifetimeTests.cs
|   |   |   |   |-- MonoGameUiHostBoundaryTests.cs
|   |   |   |   |-- ObservableListAuthoringSliceTests.cs
|   |   |   |   |-- RetainedListScrollVerticalSliceTests.cs
|   |   |   |   |-- RetainedStressBudgetTests.cs
|   |   |   |   |-- RetainedVerticalSliceTests.cs
|   |   |   |   |-- RuntimePreviewContractTests.cs
|   |   |   |   |-- TabNavigationFrameContractTests.cs
|   |   |   |   |-- UiHostFrameContractTests.cs
|   |   |   |   |-- UiHostFrameStatsIntegrityTests.cs
|   |   |   |   |-- UiHostLateTreeMutationTests.cs
|   |   |   |   |-- UiHostScaleHitTestContractTests.cs
|   |   |   |   |-- UiHostTests.cs
|   |   |   |   |-- UiHostViewportFrameContractTests.cs
|   |   |   |   |-- UiViewportScaleContractTests.cs
|   |   |   |   +-- UiViewportTests.cs
|   |   |   |-- Input/
|   |   |   |   |-- ElementInputCacheInvalidationTests.cs
|   |   |   |   |-- HitTestCacheInvalidationTests.cs
|   |   |   |   +-- InputControlBoundaryTests.cs
|   |   |   |-- Invalidation/
|   |   |   |   |-- DetachedQueuedElementTests.cs
|   |   |   |   |-- DirtyPropagationTests.cs
|   |   |   |   |-- DirtyStateTests.cs
|   |   |   |   |-- FrameSchedulerStabilityTests.cs
|   |   |   |   |-- FrameStatsTests.cs
|   |   |   |   |-- HitTestQueueTests.cs
|   |   |   |   |-- InvalidationFlagsTests.cs
|   |   |   |   |-- LayoutQueueTests.cs
|   |   |   |   |-- RenderQueueTests.cs
|   |   |   |   |-- RetainedNoWorkFrameTests.cs
|   |   |   |   +-- UiFrameSchedulerTests.cs
|   |   |   |-- Layout/
|   |   |   |   |-- CanvasTests.cs
|   |   |   |   |-- GridDefinitionMutationTests.cs
|   |   |   |   |-- GridTests.cs
|   |   |   |   |-- LayoutDiagnosticsAccuracyTests.cs
|   |   |   |   |-- LayoutInvalidationTests.cs
|   |   |   |   |-- LayoutManagerTests.cs
|   |   |   |   |-- LayoutPrimitiveTests.cs
|   |   |   |   |-- StackPanelTests.cs
|   |   |   |   |-- UIElementMeasureArrangeTests.cs
|   |   |   |   |-- VirtualizationTests.cs
|   |   |   |   |-- VirtualizingStackPanelTests.cs
|   |   |   |   |-- VisibilityCombinationTests.cs
|   |   |   |   +-- VisibilityTests.cs
|   |   |   |-- Markup/
|   |   |   |   |-- MarkupDiagnosticTests.cs
|   |   |   |   |-- UiFactoryTests.cs
|   |   |   |   |-- UiMarkupReaderTests.cs
|   |   |   |   +-- UiMarkupWriterTests.cs
|   |   |   |-- Media/
|   |   |   |   |-- BrushTests.cs
|   |   |   |   |-- GeometryTests.cs
|   |   |   |   |-- ImageSourceTests.cs
|   |   |   |   +-- TransformTests.cs
|   |   |   |-- Motion/
|   |   |   |   |-- Core/
|   |   |   |   |   |-- ManualMotionClock.cs
|   |   |   |   |   |-- MotionSystemTests.cs
|   |   |   |   |   +-- MotionValueTests.cs
|   |   |   |   |-- Interpolation/
|   |   |   |   |   +-- ValueMixerBuiltInTests.cs
|   |   |   |   |-- Properties/
|   |   |   |   |   +-- MotionPropertyBindingTests.cs
|   |   |   |   +-- Specs/
|   |   |   |       |-- EasingTests.cs
|   |   |   |       +-- MotionSpecTests.cs
|   |   |   |-- Platform/
|   |   |   |   |-- PlatformBoundaryTests.cs
|   |   |   |   |-- ServiceRegistrationTests.cs
|   |   |   |   +-- UiHostPlatformServicesIntegrationTests.cs
|   |   |   |-- Rendering/
|   |   |   |   |-- ArchitectureBoundaryTests.cs
|   |   |   |   |-- DrawCommandListBuilderTests.cs
|   |   |   |   |-- DrawCommandListPoolTests.cs
|   |   |   |   |-- ElementRenderCacheTests.cs
|   |   |   |   |-- RenderBackdoorContractTests.cs
|   |   |   |   |-- RenderCountersTests.cs
|   |   |   |   |-- RenderDependencyTests.cs
|   |   |   |   |-- RenderingTestElement.cs
|   |   |   |   |-- RenderQueueProcessorTests.cs
|   |   |   |   |-- RenderStressBudgetTests.cs
|   |   |   |   |-- ResourceRenderDependencyTests.cs
|   |   |   |   |-- RetainedRenderCacheTests.cs
|   |   |   |   |-- RetainedRendererDrawPurityTests.cs
|   |   |   |   |-- RetainedRendererTests.cs
|   |   |   |   +-- TextRenderDependencyTests.cs
|   |   |   |-- Resources/
|   |   |   |   |-- DetachedResourceDependencyCleanupTests.cs
|   |   |   |   |-- FontResourceInvalidationTests.cs
|   |   |   |   |-- HostResourceInvalidationIntegrationTests.cs
|   |   |   |   |-- ImageResourceCacheTests.cs
|   |   |   |   |-- ImageResourceInvalidationTests.cs
|   |   |   |   |-- PathBackedImageResourceIntegrationTests.cs
|   |   |   |   |-- ResourceDependencyTrackerTests.cs
|   |   |   |   |-- ResourceIdTests.cs
|   |   |   |   +-- ResourceStoreTests.cs
|   |   |   |-- Styling/
|   |   |   |   |-- DefaultThemeTemplateTests.cs
|   |   |   |   |-- DefaultThemeVerticalSliceTests.cs
|   |   |   |   |-- PseudoClassTests.cs
|   |   |   |   |-- SetterTests.cs
|   |   |   |   |-- StyleApplicatorTests.cs
|   |   |   |   |-- StyleInvalidationTests.cs
|   |   |   |   |-- StyleRuleTests.cs
|   |   |   |   |-- StyleSchedulerIntegrationTests.cs
|   |   |   |   |-- StyleTests.cs
|   |   |   |   +-- ThemeTests.cs
|   |   |   +-- Text/
|   |   |       |-- BidiTextServiceTests.cs
|   |   |       |-- FontResolverTests.cs
|   |   |       |-- TextBlockTextServiceIntegrationTests.cs
|   |   |       |-- TextBoxEditorIntegrationTests.cs
|   |   |       |-- TextCaretLayoutTests.cs
|   |   |       |-- TextCompositionManagerTests.cs
|   |   |       |-- TextDocumentTests.cs
|   |   |       |-- TextEditingControllerTests.cs
|   |   |       |-- TextEditorTests.cs
|   |   |       |-- TextLayoutCacheTests.cs
|   |   |       |-- TextMeasurerTests.cs
|   |   |       |-- TextRendererTests.cs
|   |   |       |-- TextRendererWrapContractTests.cs
|   |   |       +-- UndoRedoStackTests.cs
|   |   |-- Cerneala.Tests.csproj
|   |   +-- GameBootstrapTests.cs
|   +-- Cerneala.Tests.SourceGen/
|       |-- Cerneala.Tests.SourceGen.csproj
|       +-- UiMarkupGeneratorTests.cs
|-- Tools/
|   |-- RoslynRepoIndexer/
|   |   |-- src/
|   |   |   |-- Ri.Mcp/
|   |   |   |   |-- Program.cs
|   |   |   |   |-- Ri.Mcp.csproj
|   |   |   |   |-- RoslynMcpContracts.cs
|   |   |   |   |-- RoslynMcpToolCatalog.cs
|   |   |   |   +-- RoslynMcpTools.cs
|   |   |   |-- RoslynRepoIndexer.Cli/
|   |   |   |   |-- Program.cs
|   |   |   |   +-- RoslynRepoIndexer.Cli.csproj
|   |   |   +-- RoslynRepoIndexer.Core/
|   |   |       |-- Configuration.cs
|   |   |       |-- Discovery.cs
|   |   |       |-- IndexStore.cs
|   |   |       |-- Models.cs
|   |   |       |-- RepositoryFileReader.cs
|   |   |       |-- RoslynIndexerApplicationService.cs
|   |   |       |-- RoslynIndexing.cs
|   |   |       |-- RoslynRepoIndexer.Core.csproj
|   |   |       |-- Search.cs
|   |   |       +-- TextUtilities.cs
|   |   |-- tests/
|   |   |   +-- RoslynRepoIndexer.Tests/
|   |   |       |-- ApplicationServiceTests.cs
|   |   |       |-- ArchitectureWrapperTests.cs
|   |   |       |-- AssemblyInfo.cs
|   |   |       |-- CliBehaviorTests.cs
|   |   |       |-- CoreBehaviorTests.cs
|   |   |       |-- CrossPlatformCompatibilityTests.cs
|   |   |       |-- FileReadCliTests.cs
|   |   |       |-- IntegrationCliTests.cs
|   |   |       |-- JsonContractSnapshotTests.cs
|   |   |       |-- LinkedGeneratedIndexingTests.cs
|   |   |       |-- McpBehaviorTests.cs
|   |   |       |-- PerformanceSmokeTests.cs
|   |   |       |-- RoslynMcpTests.cs
|   |   |       |-- RoslynRepoIndexer.Tests.csproj
|   |   |       |-- RoslynSymbolIntegrationTests.cs
|   |   |       |-- SearchScorerTests.cs
|   |   |       +-- TextIndexingTests.cs
|   |   |-- README.md
|   |   |-- RoslynRepoIndexer.sln
|   |   +-- RoslynRepoIndexer.slnx
|   +-- scripts/
|       |-- Archive-Repo.ps1
|       |-- Archive-Repo.Tests.ps1
|       +-- New-FileTree.ps1
|-- UI/
|   |-- Accessibility/
|   |   |-- AccessibleName.cs
|   |   |-- AutomationPeer.cs
|   |   |-- ButtonAutomationPeer.cs
|   |   |-- ItemsControlAutomationPeer.cs
|   |   |-- SemanticsNode.cs
|   |   |-- SemanticsProperty.cs
|   |   |-- SemanticsProvider.cs
|   |   |-- SemanticsRole.cs
|   |   |-- SemanticsTree.cs
|   |   +-- TextBoxAutomationPeer.cs
|   |-- Animation/
|   |   |-- AnimatedValueSource.cs
|   |   |-- Animation.cs
|   |   |-- Animation{T}.cs
|   |   |-- AnimationClock.cs
|   |   |-- AnimationScheduler.cs
|   |   |-- Easing.cs
|   |   |-- Storyboard.cs
|   |   |-- Transition.cs
|   |   +-- Transition{T}.cs
|   |-- Controls/
|   |   |-- Primitives/
|   |   |   |-- ButtonBase.cs
|   |   |   |-- RangeBase.cs
|   |   |   |-- ScrollBar.cs
|   |   |   |-- Selector.cs
|   |   |   |-- Thumb.cs
|   |   |   |-- ToggleButton.cs
|   |   |   +-- Track.cs
|   |   |-- Shapes/
|   |   |   |-- Ellipse.cs
|   |   |   |-- Path.cs
|   |   |   |-- Rectangle.cs
|   |   |   +-- Shape.cs
|   |   |-- Border.cs
|   |   |-- Button.cs
|   |   |-- Canvas.cs
|   |   |-- CheckBox.cs
|   |   |-- ComboBox.cs
|   |   |-- ContentControl.cs
|   |   |-- ContentPresenter.cs
|   |   |-- Control.cs
|   |   |-- ControlTemplate.cs
|   |   |-- ControlTemplate{TControl}.cs
|   |   |-- ControlTextFont.cs
|   |   |-- DataTemplate.cs
|   |   |-- DataTemplate{T}.cs
|   |   |-- Decorator.cs
|   |   |-- Image.cs
|   |   |-- InkCanvas.cs
|   |   |-- IScrollInfo.cs
|   |   |-- ItemCollection.cs
|   |   |-- ItemContainerGenerator.cs
|   |   |-- ItemContainerRecyclePool.cs
|   |   |-- ItemsControl.cs
|   |   |-- ItemsPanelTemplate.cs
|   |   |-- ItemsPresenter.cs
|   |   |-- Label.cs
|   |   |-- ListBox.cs
|   |   |-- ListBoxItem.cs
|   |   |-- Panel.cs
|   |   |-- PasswordBox.cs
|   |   |-- PopupRoot.cs
|   |   |-- ProgressBar.cs
|   |   |-- RadioButton.cs
|   |   |-- ScrollBarVisibility.cs
|   |   |-- ScrollContentPresenter.cs
|   |   |-- ScrollViewer.cs
|   |   |-- SelectionModel.cs
|   |   |-- SelectionModel{T}.cs
|   |   |-- Slider.cs
|   |   |-- StackPanel.cs
|   |   |-- TabControl.cs
|   |   |-- TabItem.cs
|   |   |-- TemplateBinding{T}.cs
|   |   |-- TemplateContext.cs
|   |   |-- TemplateInstance.cs
|   |   |-- TemplatePartAttribute.cs
|   |   |-- TextBlock.cs
|   |   |-- TextBox.cs
|   |   |-- TextBoxBase.cs
|   |   |-- ToolTip.cs
|   |   +-- VisualState.cs
|   |-- Core/
|   |   |-- CoerceValue.cs
|   |   |-- IUiPropertyOwner.cs
|   |   |-- UiObject.cs
|   |   |-- UiProperty.cs
|   |   |-- UiProperty{T}.cs
|   |   |-- UiPropertyChangedEventArgs.cs
|   |   |-- UiPropertyChangedEventArgs{T}.cs
|   |   |-- UiPropertyKey{T}.cs
|   |   |-- UiPropertyMetadata{T}.cs
|   |   |-- UiPropertyOptions.cs
|   |   |-- UiPropertyRegistry.cs
|   |   |-- UiPropertyStore.cs
|   |   |-- UiPropertyValueSource.cs
|   |   |-- Unset.cs
|   |   +-- ValidateValue.cs
|   |-- Data/
|   |   |-- Binding.cs
|   |   |-- Binding{T}.cs
|   |   |-- BindingMode.cs
|   |   |-- BindingOperations.cs
|   |   |-- BindingSubscriptionCollection.cs
|   |   |-- CollectionView{T}.cs
|   |   |-- FilterPredicate{T}.cs
|   |   |-- IObservableList{T}.cs
|   |   |-- IValueConverter{TIn,TOut}.cs
|   |   |-- ObservableList{T}.cs
|   |   |-- ObservableValue{T}.cs
|   |   |-- PropertyAdapter{TOwner,TValue}.cs
|   |   |-- SortDescription{T}.cs
|   |   |-- StringPropertyPath.cs
|   |   +-- UiPropertyBinding{T}.cs
|   |-- Diagnostics/
|   |   |-- DebugAdorner.cs
|   |   |-- DebugOverlay.cs
|   |   |-- DirtyTreeDumper.cs
|   |   |-- ElementTreeDumper.cs
|   |   |-- FrameDiagnostics.cs
|   |   |-- InputDiagnostics.cs
|   |   |-- InvalidationTrace.cs
|   |   |-- LayoutDiagnostics.cs
|   |   |-- RenderCacheDumper.cs
|   |   |-- RenderDiagnostics.cs
|   |   |-- RoutedEventTrace.cs
|   |   |-- RuntimeDiagnostics.cs
|   |   +-- StyleTrace.cs
|   |-- Drawing/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameClipStack.cs
|   |   |   |-- MonoGameDrawingBackend.cs
|   |   |   |-- MonoGameDrawMapper.cs
|   |   |   +-- MonoGameImage.cs
|   |   |-- Text/
|   |   |   |-- RasterizedText.cs
|   |   |   |-- SkiaFont.cs
|   |   |   |-- SkiaTextRasterizer.cs
|   |   |   |-- SkiaTextShaper.cs
|   |   |   |-- SystemFontSource.cs
|   |   |   |-- TextCaretVerticalMetrics.cs
|   |   |   |-- TextShaper.cs
|   |   |   +-- TextShapeResult.cs
|   |   |-- DrawArgument.cs
|   |   |-- DrawColor.cs
|   |   |-- DrawCommand.cs
|   |   |-- DrawCommandKind.cs
|   |   |-- DrawCommandList.cs
|   |   |-- DrawingContext.cs
|   |   |-- DrawPoint.cs
|   |   |-- DrawRect.cs
|   |   |-- DrawTextRun.cs
|   |   |-- IDrawFont.cs
|   |   |-- IDrawImage.cs
|   |   |-- IDrawingBackend.cs
|   |   +-- IFontSource.cs
|   |-- Elements/
|   |   |-- ElementChildRole.cs
|   |   |-- ElementHandlerStore.cs
|   |   |-- ElementIdProvider.cs
|   |   |-- ElementLifecycle.cs
|   |   |-- ElementTreeChange.cs
|   |   |-- ElementTreeChangeKind.cs
|   |   |-- ElementTreeWalker.cs
|   |   |-- IElementChildHost.cs
|   |   |-- IElementHost.cs
|   |   |-- InheritedPropertyPropagator.cs
|   |   |-- UIElement.cs
|   |   |-- UIElementCollection.cs
|   |   |-- UIElementVisibility.cs
|   |   +-- UIRoot.cs
|   |-- Hosting/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameContentServices.cs
|   |   |   |-- MonoGameUiHost.cs
|   |   |   +-- MonoGameUiHostOptions.cs
|   |   |-- IUiBackend.cs
|   |   |-- IUiClock.cs
|   |   |-- UiCoordinateMapper.cs
|   |   |-- UiFrame.cs
|   |   |-- UiHost.cs
|   |   |-- UiHostOptions.cs
|   |   +-- UiViewport.cs
|   |-- Ink/
|   |   |-- Stroke.cs
|   |   +-- StrokeCollection.cs
|   |-- Input/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameInputMapper.cs
|   |   |   +-- MonoGameInputSource.cs
|   |   |-- ActionCommand.cs
|   |   |-- CanExecuteRoutedEventArgs.cs
|   |   |-- ClickTracker.cs
|   |   |-- CommandBinding.cs
|   |   |-- CommandBindingCollection.cs
|   |   |-- CommandEvents.cs
|   |   |-- CommandRouter.cs
|   |   |-- Cursor.cs
|   |   |-- CursorService.cs
|   |   |-- DataTransfer.cs
|   |   |-- DragDropController.cs
|   |   |-- ElementInputBridge.cs
|   |   |-- ElementInputCache.cs
|   |   |-- ElementInputRouteBuilder.cs
|   |   |-- ElementInputRouteMap.cs
|   |   |-- ElementRoutedEventStore.cs
|   |   |-- ExecutedRoutedEventArgs.cs
|   |   |-- FocusManager.cs
|   |   |-- FocusPolicy.cs
|   |   |-- FocusScope.cs
|   |   |-- GestureRecognizer.cs
|   |   |-- HitTestFilter.cs
|   |   |-- HitTestResult.cs
|   |   |-- HitTestService.cs
|   |   |-- HoverTracker.cs
|   |   |-- ICommand.cs
|   |   |-- ICommandStateSource.cs
|   |   |-- IInputCommandSource.cs
|   |   |-- IInputPressable.cs
|   |   |-- IInputSource.cs
|   |   |-- InputBinding.cs
|   |   |-- InputBindingCollection.cs
|   |   |-- InputButtonState.cs
|   |   |-- InputEvents.cs
|   |   |-- InputFrame.cs
|   |   |-- InputGesture.cs
|   |   |-- InputKey.cs
|   |   |-- InputMouseButton.cs
|   |   |-- IObservableCommand.cs
|   |   |-- IPointerDragSource.cs
|   |   |-- KeyBinding.cs
|   |   |-- KeyboardActivationController.cs
|   |   |-- KeyboardDispatchResult.cs
|   |   |-- KeyboardFocusChangedEventArgs.cs
|   |   |-- KeyboardNavigation.cs
|   |   |-- KeyboardNavigationController.cs
|   |   |-- KeyboardSnapshot.cs
|   |   |-- KeyEventArgs.cs
|   |   |-- KeyGesture.cs
|   |   |-- ManipulationProcessor.cs
|   |   |-- MouseButtonEventArgs.cs
|   |   |-- MouseEventArgs.cs
|   |   |-- MouseWheelEventArgs.cs
|   |   |-- PointerCaptureManager.cs
|   |   |-- PointerSnapshot.cs
|   |   |-- PressedStateTracker.cs
|   |   |-- RetainedInputBindingProcessor.cs
|   |   |-- RoutedCommand.cs
|   |   |-- RoutedCommandContext.cs
|   |   |-- RoutedEvent.cs
|   |   |-- RoutedEventArgs.cs
|   |   |-- RoutedEventRegistry.cs
|   |   |-- RoutedEventRouter.cs
|   |   |-- RoutingStrategy.cs
|   |   |-- StylusInputBridge.cs
|   |   |-- TextCompositionEventArgs.cs
|   |   |-- TextInputBridge.cs
|   |   |-- TextInputSnapshotEvent.cs
|   |   |-- TouchInputBridge.cs
|   |   |-- UiElementId.cs
|   |   |-- UiInputElement.cs
|   |   +-- UiInputTree.cs
|   |-- Invalidation/
|   |   |-- CommandStateQueue.cs
|   |   |-- DirtyPropagation.cs
|   |   |-- DirtyState.cs
|   |   |-- ElementQueueOrder.cs
|   |   |-- FrameBudget.cs
|   |   |-- FramePhase.cs
|   |   |-- FramePhaseProcessors.cs
|   |   |-- FrameStats.cs
|   |   |-- HitTestQueue.cs
|   |   |-- IInvalidationSink.cs
|   |   |-- InheritedPropertyQueue.cs
|   |   |-- InvalidationFlags.cs
|   |   |-- InvalidationRequest.cs
|   |   |-- LayoutQueue.cs
|   |   |-- RenderQueue.cs
|   |   |-- StyleQueue.cs
|   |   +-- UiFrameScheduler.cs
|   |-- Layout/
|   |   |-- Panels/
|   |   |   |-- Canvas.cs
|   |   |   |-- ColumnDefinition.cs
|   |   |   |-- Grid.cs
|   |   |   |-- GridDefinitionCollection{TDefinition}.cs
|   |   |   |-- GridLength.cs
|   |   |   |-- Orientation.cs
|   |   |   |-- Panel.cs
|   |   |   |-- RowDefinition.cs
|   |   |   |-- StackPanel.cs
|   |   |   +-- VirtualizingStackPanel.cs
|   |   |-- Virtualization/
|   |   |   |-- RealizationWindow.cs
|   |   |   +-- VirtualizationContext.cs
|   |   |-- Alignment.cs
|   |   |-- ArrangeContext.cs
|   |   |-- ILayoutElement.cs
|   |   |-- LayoutBoundary.cs
|   |   |-- LayoutManager.cs
|   |   |-- LayoutPoint.cs
|   |   |-- LayoutRect.cs
|   |   |-- LayoutResult.cs
|   |   |-- LayoutRounding.cs
|   |   |-- LayoutSize.cs
|   |   |-- MeasureContext.cs
|   |   |-- Thickness.cs
|   |   +-- Visibility.cs
|   |-- Markup/
|   |   |-- ContentPropertyAttribute.cs
|   |   |-- DesignTimeOnlyAttribute.cs
|   |   |-- GeneratedUiFactory.cs
|   |   |-- MarkupDiagnostic.cs
|   |   |-- MarkupLoadOptions.cs
|   |   |-- UiFactory.cs
|   |   |-- UiMarkupDocument.cs
|   |   |-- UiMarkupReader.cs
|   |   |-- UiMarkupSchema.cs
|   |   |-- UiMarkupTypeRegistry.cs
|   |   +-- UiMarkupWriter.cs
|   |-- Media/
|   |   |-- BitmapImage.cs
|   |   |-- Brush.cs
|   |   |-- EllipseGeometry.cs
|   |   |-- Geometry.cs
|   |   |-- GradientStop.cs
|   |   |-- ImageSource.cs
|   |   |-- LinearGradientBrush.cs
|   |   |-- Matrix3x2.cs
|   |   |-- OpacityLayer.cs
|   |   |-- PathGeometry.cs
|   |   |-- Pen.cs
|   |   |-- RadialGradientBrush.cs
|   |   |-- RectangleGeometry.cs
|   |   |-- RenderTargetImage.cs
|   |   |-- ShadowEffect.cs
|   |   |-- SolidColorBrush.cs
|   |   +-- Transform.cs
|   |-- Motion/
|   |   |-- Core/
|   |   |   |-- DerivedMotionValue{T}.cs
|   |   |   |-- IMotionClock.cs
|   |   |   |-- MotionCancellation.cs
|   |   |   |-- MotionCompletionSource.cs
|   |   |   |-- MotionFrame.cs
|   |   |   |-- MotionFrameCoordinator.cs
|   |   |   |-- MotionFramePhase.cs
|   |   |   |-- MotionFrameResult.cs
|   |   |   |-- MotionGraph.cs
|   |   |   |-- MotionHandle.cs
|   |   |   |-- MotionNode.cs
|   |   |   |-- MotionPriority.cs
|   |   |   |-- MotionStartOptions.cs
|   |   |   |-- MotionSystem.cs
|   |   |   |-- MotionThreadGuard.cs
|   |   |   |-- MotionTimelineRegistry.cs
|   |   |   |-- MotionValue.cs
|   |   |   |-- MotionValue{T}.cs
|   |   |   |-- ReducedMotionMode.cs
|   |   |   |-- ReducedMotionPolicy.cs
|   |   |   +-- SystemMotionClock.cs
|   |   |-- Diagnostics/
|   |   |   +-- MotionDiagnostics.cs
|   |   |-- Interpolation/
|   |   |   |-- ColorMixer.cs
|   |   |   |-- DoubleMixer.cs
|   |   |   |-- DrawPointMixer.cs
|   |   |   |-- DrawRectMixer.cs
|   |   |   |-- FloatMixer.cs
|   |   |   |-- IValueMixer.cs
|   |   |   |-- ThicknessMixer.cs
|   |   |   |-- TransformMixer.cs
|   |   |   |-- ValueMixer.cs
|   |   |   +-- ValueMixerRegistry.cs
|   |   |-- Properties/
|   |   |   |-- AnimatablePropertyRegistry.cs
|   |   |   |-- MotionClearBehavior.cs
|   |   |   |-- MotionPropertyBinding.cs
|   |   |   |-- MotionPropertyBinding{T}.cs
|   |   |   |-- MotionPropertyInvalidationCategory.cs
|   |   |   |-- MotionPropertyInvalidationClassifier.cs
|   |   |   |-- MotionPropertyKey.cs
|   |   |   |-- MotionPropertyOptions.cs
|   |   |   |-- MotionPropertyStartOptions.cs
|   |   |   +-- MotionPropertyStore.cs
|   |   |-- Specs/
|   |   |   |-- CubicBezierEasing.cs
|   |   |   |-- DecaySpec.cs
|   |   |   |-- Easings.cs
|   |   |   |-- FillMode.cs
|   |   |   |-- IEasing.cs
|   |   |   |-- KeyframesSpec.cs
|   |   |   |-- Motion.cs
|   |   |   |-- MotionCompletion.cs
|   |   |   |-- MotionSampler.cs
|   |   |   |-- MotionSpec.cs
|   |   |   |-- MotionSpec{T}.cs
|   |   |   |-- MotionSpecContext.cs
|   |   |   |-- MotionVelocity.cs
|   |   |   |-- RetargetMode.cs
|   |   |   |-- SpringSpec.cs
|   |   |   |-- SpringVelocityMode.cs
|   |   |   |-- StepEasing.cs
|   |   |   +-- TweenSpec.cs
|   |   +-- Styling/
|   |       +-- MotionTokens.cs
|   |-- Platform/
|   |   |-- IAccessibilityPlatform.cs
|   |   |-- IClipboard.cs
|   |   |-- ICursorService.cs
|   |   |-- IDpiProvider.cs
|   |   |-- IFileDialogService.cs
|   |   |-- IPlatformServices.cs
|   |   |-- ITextInputPlatform.cs
|   |   +-- PlatformServices.cs
|   |-- Rendering/
|   |   |-- ClipNode.cs
|   |   |-- DrawCommandListBuilder.cs
|   |   |-- DrawCommandListPool.cs
|   |   |-- ElementRenderCache.cs
|   |   |-- IRenderableElement.cs
|   |   |-- ITimeSensitiveRenderElement.cs
|   |   |-- RenderContext.cs
|   |   |-- RenderCounters.cs
|   |   |-- RenderDependency.cs
|   |   |-- RenderLayer.cs
|   |   |-- RenderQueueProcessor.cs
|   |   |-- RetainedRenderCache.cs
|   |   |-- RetainedRenderer.cs
|   |   +-- TimeSensitiveRenderInvalidator.cs
|   |-- Resources/
|   |   |-- MonoGame/
|   |   |   +-- MonoGameImageLoader.cs
|   |   |-- FontResource.cs
|   |   |-- IImageLoader.cs
|   |   |-- ImageResource.cs
|   |   |-- ImageResourceCache.cs
|   |   |-- IObservableResourceProvider.cs
|   |   |-- IResourceProvider.cs
|   |   |-- ResourceChangedEventArgs.cs
|   |   |-- ResourceDependencyTracker.cs
|   |   |-- ResourceId{T}.cs
|   |   +-- ResourceStore.cs
|   |-- Styling/
|   |   |-- DefaultTheme.cs
|   |   |-- PseudoClass.cs
|   |   |-- PseudoClassRegistry.cs
|   |   |-- Setter.cs
|   |   |-- Setter{T}.cs
|   |   |-- Style.cs
|   |   |-- StyleApplicator.cs
|   |   |-- StyleDiagnostics.cs
|   |   |-- StyleInvalidation.cs
|   |   |-- StyleProcessor.cs
|   |   |-- StyleRule.cs
|   |   |-- StyleSelector.cs
|   |   |-- StyleSheet.cs
|   |   |-- StyleTransition.cs
|   |   |-- Theme.cs
|   |   |-- ThemeKey{T}.cs
|   |   |-- ThemePalette.cs
|   |   |-- ThemeProvider.cs
|   |   |-- ThemeResource.cs
|   |   +-- VisualStateRule.cs
|   +-- Text/
|       |-- BidiTextService.cs
|       |-- ClipboardAdapter.cs
|       |-- FontResolver.cs
|       |-- LineBreakService.cs
|       |-- ResolvedTextFont.cs
|       |-- TextCaret.cs
|       |-- TextCaretLayout.cs
|       |-- TextCompositionManager.cs
|       |-- TextCompositionState.cs
|       |-- TextDocument.cs
|       |-- TextEditingController.cs
|       |-- TextEditor.cs
|       |-- TextLayoutCache.cs
|       |-- TextLayoutKey.cs
|       |-- TextLine.cs
|       |-- TextLineMetrics.cs
|       |-- TextMeasurer.cs
|       |-- TextMeasureResult.cs
|       |-- TextRenderer.cs
|       |-- TextRunStyle.cs
|       |-- TextSelection.cs
|       |-- TextTrimming.cs
|       |-- TextWrapping.cs
|       +-- UndoRedoStack.cs
|-- .gitignore
|-- AGENTS.md
|-- architecture.md
|-- AUDIT_FIX_PLAN.md
|-- Cerneala.csproj
|-- Cerneala.slnx
|-- ConceptualIdeas.md
|-- GameBootstrap.cs
|-- mcp.md
|-- ROADMAP.md
|-- ROADMAPv2_AUDIT.md
|-- ROADMAPv2.md
+-- roslyn_indexer_codex_plan_final.md
```

