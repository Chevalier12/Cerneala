# File Tree

Generated from `.`.

```text
./
|-- Cerneala.SourceGen/
|   |-- Cerneala.SourceGen.csproj
|   +-- UiMarkupGenerator.cs
|-- docs/
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
|   |       |-- 2026-07-05-clarify-text-services-mvp.md
|   |       +-- 2026-07-05-freeze-later-experimental-scope.md
|   +-- architecture-v2.md
|-- Playground/
|   +-- Cerneala.Playground/
|       |-- .config/
|       |   +-- dotnet-tools.json
|       |-- Content/
|       |   +-- Content.mgcb
|       |-- Samples/
|       |   |-- DiagnosticsSample.cs
|       |   |-- InvalidationStatsOverlay.cs
|       |   |-- LayoutSample.cs
|       |   |-- PlaygroundText.cs
|       |   |-- RetainedButtonSample.cs
|       |   |-- SampleSelector.cs
|       |   +-- TextSample.cs
|       |-- app.manifest
|       |-- Cerneala.Playground.csproj
|       |-- Game1.cs
|       |-- Icon.bmp
|       |-- Icon.ico
|       +-- Program.cs
|-- tests/
|   |-- Cerneala.Tests/
|   |   |-- Architecture/
|   |   |   |-- MonoGameDependencyBoundaryTests.cs
|   |   |   |-- NamespaceBoundaryTests.cs
|   |   |   +-- RepositoryShapeTests.cs
|   |   |-- Controls/
|   |   |   |-- Primitives/
|   |   |   |   |-- ButtonBaseCommandTests.cs
|   |   |   |   |-- ButtonBaseTests.cs
|   |   |   |   |-- RangeBaseTests.cs
|   |   |   |   |-- SelectorTests.cs
|   |   |   |   |-- ThumbTests.cs
|   |   |   |   +-- TrackTests.cs
|   |   |   |-- BorderTests.cs
|   |   |   |-- ButtonContentArchitectureTests.cs
|   |   |   |-- ButtonTests.cs
|   |   |   |-- CanvasTests.cs
|   |   |   |-- CheckBoxTests.cs
|   |   |   |-- ComboBoxTests.cs
|   |   |   |-- ContentControlTests.cs
|   |   |   |-- ContentPresenterTests.cs
|   |   |   |-- ControlTemplateTests.cs
|   |   |   |-- ControlTests.cs
|   |   |   |-- DataTemplateTests.cs
|   |   |   |-- DecoratorTests.cs
|   |   |   |-- ImageTests.cs
|   |   |   |-- InkCanvasTests.cs
|   |   |   |-- ItemContainerGeneratorTests.cs
|   |   |   |-- ItemContainerRecyclePoolTests.cs
|   |   |   |-- ItemsControlTests.cs
|   |   |   |-- ItemsPanelTemplateTests.cs
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
|   |   |   |-- TextBlockInvalidationTests.cs
|   |   |   |-- TextBlockTests.cs
|   |   |   |-- TextBoxTests.cs
|   |   |   |-- ToggleButtonTests.cs
|   |   |   +-- ToolTipTests.cs
|   |   |-- Drawing/
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
|   |   |   |-- DragDropControllerTests.cs
|   |   |   |-- ElementInputBridgeTests.cs
|   |   |   |-- ElementInputRouteBuilderTests.cs
|   |   |   |-- FocusManagerTests.cs
|   |   |   |-- GestureRecognizerTests.cs
|   |   |   |-- HitTestServiceTests.cs
|   |   |   |-- HoverTrackerTests.cs
|   |   |   |-- InputEventsTests.cs
|   |   |   |-- InputFrameTests.cs
|   |   |   |-- InputGestureTests.cs
|   |   |   |-- ManipulationProcessorTests.cs
|   |   |   |-- MonoGameInputMapperTests.cs
|   |   |   |-- PointerCaptureManagerTests.cs
|   |   |   |-- PressedStateTrackerTests.cs
|   |   |   |-- RetainedRoutedEventIntegrationTests.cs
|   |   |   |-- RoutedCommandExecutionTests.cs
|   |   |   |-- RoutedEventRouterTests.cs
|   |   |   |-- RoutedEventTests.cs
|   |   |   |-- StylusInputBridgeTests.cs
|   |   |   |-- TextInputBridgeTests.cs
|   |   |   +-- TouchInputBridgeTests.cs
|   |   |-- Playground/
|   |   |   |-- Samples/
|   |   |   |   +-- PlaygroundSampleTests.cs
|   |   |   +-- Game1SourceTests.cs
|   |   |-- UI/
|   |   |   |-- Accessibility/
|   |   |   |   |-- AccessibilityPlatformTests.cs
|   |   |   |   |-- ButtonSemanticsTests.cs
|   |   |   |   |-- SemanticsProviderTests.cs
|   |   |   |   |-- SemanticsTreeTests.cs
|   |   |   |   +-- TextBoxSemanticsTests.cs
|   |   |   |-- Animation/
|   |   |   |   |-- AnimationClockTests.cs
|   |   |   |   |-- AnimationInvalidationTests.cs
|   |   |   |   |-- AnimationSchedulerTests.cs
|   |   |   |   |-- TransitionTests.cs
|   |   |   |   +-- TypedAnimationTests.cs
|   |   |   |-- Controls/
|   |   |   |   +-- Shapes/
|   |   |   |       +-- ShapeTests.cs
|   |   |   |-- Core/
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
|   |   |   |   +-- TypedBindingTests.cs
|   |   |   |-- Diagnostics/
|   |   |   |   |-- DirtyTreeDumperTests.cs
|   |   |   |   |-- ElementTreeDumperTests.cs
|   |   |   |   |-- FrameDiagnosticsTests.cs
|   |   |   |   |-- InvalidationTraceTests.cs
|   |   |   |   |-- RenderCacheDumperTests.cs
|   |   |   |   |-- RoutedEventTraceTests.cs
|   |   |   |   +-- StyleTraceTests.cs
|   |   |   |-- Elements/
|   |   |   |   |-- ElementHandlerStoreTests.cs
|   |   |   |   |-- ElementLifecycleTests.cs
|   |   |   |   |-- ElementTreeWalkerTests.cs
|   |   |   |   |-- UIElementCollectionInvalidationTests.cs
|   |   |   |   |-- UIElementCollectionTests.cs
|   |   |   |   |-- UIElementInvalidationTests.cs
|   |   |   |   |-- UIElementTreeTests.cs
|   |   |   |   +-- UIRootTests.cs
|   |   |   |-- Hosting/
|   |   |   |   |-- FakeDrawingBackend.cs
|   |   |   |   |-- FakeInputSource.cs
|   |   |   |   |-- FakeUiClock.cs
|   |   |   |   |-- MonoGameUiHostBoundaryTests.cs
|   |   |   |   |-- UiHostFrameContractTests.cs
|   |   |   |   |-- UiHostFrameStatsIntegrityTests.cs
|   |   |   |   |-- UiHostLateTreeMutationTests.cs
|   |   |   |   |-- UiHostTests.cs
|   |   |   |   +-- UiViewportTests.cs
|   |   |   |-- Input/
|   |   |   |   |-- ElementInputCacheInvalidationTests.cs
|   |   |   |   |-- HitTestCacheInvalidationTests.cs
|   |   |   |   +-- InputControlBoundaryTests.cs
|   |   |   |-- Invalidation/
|   |   |   |   |-- DirtyPropagationTests.cs
|   |   |   |   |-- DirtyStateTests.cs
|   |   |   |   |-- FrameStatsTests.cs
|   |   |   |   |-- HitTestQueueTests.cs
|   |   |   |   |-- InvalidationFlagsTests.cs
|   |   |   |   |-- LayoutQueueTests.cs
|   |   |   |   |-- RenderQueueTests.cs
|   |   |   |   |-- RetainedNoWorkFrameTests.cs
|   |   |   |   +-- UiFrameSchedulerTests.cs
|   |   |   |-- Layout/
|   |   |   |   |-- CanvasTests.cs
|   |   |   |   |-- GridTests.cs
|   |   |   |   |-- LayoutInvalidationTests.cs
|   |   |   |   |-- LayoutManagerTests.cs
|   |   |   |   |-- LayoutPrimitiveTests.cs
|   |   |   |   |-- StackPanelTests.cs
|   |   |   |   |-- UIElementMeasureArrangeTests.cs
|   |   |   |   |-- VirtualizationTests.cs
|   |   |   |   |-- VirtualizingStackPanelTests.cs
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
|   |   |   |-- Platform/
|   |   |   |   |-- PlatformBoundaryTests.cs
|   |   |   |   +-- ServiceRegistrationTests.cs
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
|   |   |   |   |-- ResourceRenderDependencyTests.cs
|   |   |   |   |-- RetainedRenderCacheTests.cs
|   |   |   |   |-- RetainedRendererDrawPurityTests.cs
|   |   |   |   |-- RetainedRendererTests.cs
|   |   |   |   +-- TextRenderDependencyTests.cs
|   |   |   |-- Resources/
|   |   |   |   |-- FontResourceInvalidationTests.cs
|   |   |   |   |-- ImageResourceInvalidationTests.cs
|   |   |   |   |-- ResourceDependencyTrackerTests.cs
|   |   |   |   |-- ResourceIdTests.cs
|   |   |   |   +-- ResourceStoreTests.cs
|   |   |   |-- Styling/
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
|   |   |       |-- TextCompositionManagerTests.cs
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
|   |   |-- CollectionView{T}.cs
|   |   |-- FilterPredicate{T}.cs
|   |   |-- IObservableList{T}.cs
|   |   |-- IValueConverter{TIn,TOut}.cs
|   |   |-- ObservableList{T}.cs
|   |   |-- ObservableValue{T}.cs
|   |   |-- PropertyAdapter{TOwner,TValue}.cs
|   |   |-- SortDescription{T}.cs
|   |   +-- StringPropertyPath.cs
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
|   |   +-- StyleTrace.cs
|   |-- Drawing/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameDrawingBackend.cs
|   |   |   +-- MonoGameImage.cs
|   |   |-- Text/
|   |   |   |-- RasterizedText.cs
|   |   |   |-- SkiaFont.cs
|   |   |   |-- SkiaTextRasterizer.cs
|   |   |   |-- SkiaTextShaper.cs
|   |   |   |-- SystemFontSource.cs
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
|   |   |-- UIElement.cs
|   |   |-- UIElementCollection.cs
|   |   +-- UIRoot.cs
|   |-- Hosting/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameContentServices.cs
|   |   |   |-- MonoGameUiHost.cs
|   |   |   +-- MonoGameUiHostOptions.cs
|   |   |-- IUiBackend.cs
|   |   |-- IUiClock.cs
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
|   |   |-- FocusScope.cs
|   |   |-- GestureRecognizer.cs
|   |   |-- HitTestFilter.cs
|   |   |-- HitTestResult.cs
|   |   |-- HitTestService.cs
|   |   |-- HoverTracker.cs
|   |   |-- ICommand.cs
|   |   |-- IInputCommandSource.cs
|   |   |-- IInputPressable.cs
|   |   |-- IInputSource.cs
|   |   |-- InputBinding.cs
|   |   |-- InputButtonState.cs
|   |   |-- InputEvents.cs
|   |   |-- InputFrame.cs
|   |   |-- InputGesture.cs
|   |   |-- InputKey.cs
|   |   |-- InputMouseButton.cs
|   |   |-- IPointerDragSource.cs
|   |   |-- KeyBinding.cs
|   |   |-- KeyboardFocusChangedEventArgs.cs
|   |   |-- KeyboardNavigation.cs
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
|   |   |-- DirtyPropagation.cs
|   |   |-- DirtyState.cs
|   |   |-- ElementQueueOrder.cs
|   |   |-- FrameBudget.cs
|   |   |-- FramePhase.cs
|   |   |-- FramePhaseProcessors.cs
|   |   |-- FrameStats.cs
|   |   |-- HitTestQueue.cs
|   |   |-- IInvalidationSink.cs
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
|   |   |-- RenderContext.cs
|   |   |-- RenderCounters.cs
|   |   |-- RenderDependency.cs
|   |   |-- RenderLayer.cs
|   |   |-- RenderQueueProcessor.cs
|   |   |-- RetainedRenderCache.cs
|   |   +-- RetainedRenderer.cs
|   |-- Resources/
|   |   |-- MonoGame/
|   |   |   +-- MonoGameImageLoader.cs
|   |   |-- FontResource.cs
|   |   |-- IImageLoader.cs
|   |   |-- ImageResource.cs
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
|       |-- TextCompositionManager.cs
|       |-- TextCompositionState.cs
|       |-- TextDocument.cs
|       |-- TextEditingController.cs
|       |-- TextEditor.cs
|       |-- TextLayoutCache.cs
|       |-- TextLayoutKey.cs
|       |-- TextLine.cs
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

