# File Tree

Generated from `.` (repository files only: tracked files and new files not ignored by `.gitignore`).

```text
./
|-- .claude/
|   +-- skills/
|       |-- algorithm-market/
|       |   |-- references/
|       |   |   +-- license-policy.md
|       |   +-- SKILL.md
|       |-- cerneala-breaker/
|       |   |-- references/
|       |   |   |-- attack-techniques.md
|       |   |   +-- test-evidence.md
|       |   +-- SKILL.md
|       |-- cerneala-checklist-plan/
|       |   |-- references/
|       |   |   +-- semantic-audit.md
|       |   +-- SKILL.md
|       |-- cerneala-fix-bug/
|       |   +-- SKILL.md
|       |-- cerneala-implement-plan/
|       |   +-- SKILL.md
|       |-- cerneala-performance-gate/
|       |   +-- SKILL.md
|       |-- cerneala-pr-review-merge/
|       |   +-- SKILL.md
|       |-- git-session-commit-push/
|       |   +-- SKILL.md
|       |-- github-issue-raise/
|       |   +-- SKILL.md
|       |-- repo-cleanup/
|       |   +-- SKILL.md
|       +-- writing-api-documentation/
|           |-- references/
|           |   +-- wpf-api-doc-formula.md
|           +-- SKILL.md
|-- .config/
|   +-- dotnet-tools.json
|-- .github/
|   +-- workflows/
|       |-- desktop-backends.yml
|       |-- pages.yml
|       |-- prism-shaders.yml
|       +-- scene-village-windows.yml
|-- benchmarks/
|   |-- Cerneala.Benchmarks/
|   |   |-- AspectResolutionBenchmarks.cs
|   |   |-- AspectVisualConformanceRunner.cs
|   |   |-- CerberusPlanningBenchmarks.cs
|   |   |-- Cerneala.Benchmarks.csproj
|   |   |-- CernealaLanguageBenchmarks.cs
|   |   |-- CollisionStageTwoBenchmarks.cs
|   |   |-- CollisionStageZeroBenchmarks.cs
|   |   |-- DrawingBatchBenchmarks.cs
|   |   |-- DrawingShapeBenchmarks.cs
|   |   |-- DrawingStateBenchmarks.cs
|   |   |-- DrawingStrokeBenchmarks.cs
|   |   |-- DrawingTextLayoutBenchmarks.cs
|   |   |-- Measure-TextCharacterization.ps1
|   |   |-- PrismInstanceBenchmarks.cs
|   |   |-- PrismRetainedCacheBenchmarkRunner.cs
|   |   |-- PrismSdlGpuBenchmarkRunner.cs
|   |   |-- Program.cs
|   |   |-- QueueEngineBenchmarks.cs
|   |   |-- README.md
|   |   |-- RenderSurface3DProbeRunner.cs
|   |   |-- SceneDebugOverlayBenchmarks.cs
|   |   |-- SpriteAnimationBenchmarks.cs
|   |   |-- TextCharacterizationRunner.cs
|   |   |-- TileMapBaselineBenchmarks.cs
|   |   |-- TileMapStage4BackendProfileRunner.cs
|   |   |-- TileMapStage4Benchmarks.cs
|   |   |-- TimbreCoreBenchmarkRunner.cs
|   |   |-- TimbreDecodingBenchmarkRunner.cs
|   |   |-- TimbreMotionBenchmarkRunner.cs
|   |   +-- UiRelayBenchmarks.cs
|   |-- Cerneala.PresentationFrameBudget/
|   |   |-- Cerneala.PresentationFrameBudget.csproj
|   |   |-- Program.cs
|   |   +-- README.md
|   +-- Cerneala.WpfDispatcherBenchmarks/
|       |-- Cerneala.WpfDispatcherBenchmarks.csproj
|       |-- DispatcherComparisonBenchmarks.cs
|       |-- DispatcherComparisonSmoke.cs
|       |-- DispatcherHarnesses.cs
|       |-- Program.cs
|       +-- README.md
|-- Cerneala.Backends.SdlGpu/
|   |-- Gpu/
|   |   |-- Shaders/
|   |   |   |-- Drawing.frag.dxil
|   |   |   |-- Drawing.frag.hlsl
|   |   |   |-- Drawing.frag.msl
|   |   |   |-- Drawing.frag.spv
|   |   |   |-- Drawing.vert.dxil
|   |   |   |-- Drawing.vert.hlsl
|   |   |   |-- Drawing.vert.msl
|   |   |   |-- Drawing.vert.spv
|   |   |   |-- DrawingImageDomain.frag.dxil
|   |   |   |-- DrawingImageDomain.frag.hlsl
|   |   |   |-- DrawingImageDomain.frag.msl
|   |   |   |-- DrawingImageDomain.frag.spv
|   |   |   |-- DrawingImageDomain.vert.dxil
|   |   |   |-- DrawingImageDomain.vert.hlsl
|   |   |   |-- DrawingImageDomain.vert.msl
|   |   |   |-- DrawingImageDomain.vert.spv
|   |   |   |-- Surface3D.frag.dxil
|   |   |   |-- Surface3D.frag.hlsl
|   |   |   |-- Surface3D.frag.msl
|   |   |   |-- Surface3D.frag.spv
|   |   |   |-- Surface3D.vert.dxil
|   |   |   |-- Surface3D.vert.hlsl
|   |   |   |-- Surface3D.vert.msl
|   |   |   +-- Surface3D.vert.spv
|   |   |-- Cerberus.cs
|   |   |-- SdlGpuDebugLabels.cs
|   |   |-- SdlGpuDeviceOwner.cs
|   |   |-- SdlGpuDrawingBackend.BrushCaptures.cs
|   |   |-- SdlGpuDrawingBackend.cs
|   |   |-- SdlGpuDrawingBackend.Paint.cs
|   |   |-- SdlGpuDrawingBackend.Surface2D.cs
|   |   |-- SdlGpuDrawingBackend.Surface3D.cs
|   |   |-- SdlGpuDrawingBackend.SurfaceDamage.cs
|   |   |-- SdlGpuDrawingBackend.Text.cs
|   |   |-- SdlGpuDrawingFrameCounters.cs
|   |   |-- SdlGpuDrawingResources.cs
|   |   |-- SdlGpuGeometryCache.cs
|   |   |-- SdlGpuGeometryUploadArena.cs
|   |   |-- SdlGpuImageDomainGeometry.cs
|   |   |-- SdlGpuImageLoader.cs
|   |   |-- SdlGpuPresentationOptions.cs
|   |   |-- SdlGpuPrismFrameCounters.cs
|   |   |-- SdlGpuRenderSurface3DDeviceResources.cs
|   |   |-- SdlGpuRenderSurface3DDiagnostics.cs
|   |   |-- SdlGpuShaderArtifacts.cs
|   |   |-- SdlGpuTextAtlasAllocator.cs
|   |   +-- SdlGpuWindowGraphicsSession.cs
|   |-- Hosting/
|   |   +-- SdlGpuApplicationBackend.cs
|   |-- Prism/
|   |   |-- Shaders/
|   |   |   |-- Prism.vert.dxil
|   |   |   |-- Prism.vert.hlsl
|   |   |   |-- Prism.vert.msl
|   |   |   |-- Prism.vert.spv
|   |   |   |-- PrismCatalogArtistic.frag.dxil
|   |   |   |-- PrismCatalogArtistic.frag.hlsl
|   |   |   |-- PrismCatalogArtistic.frag.msl
|   |   |   |-- PrismCatalogArtistic.frag.spv
|   |   |   |-- PrismCatalogFilters10To14.frag.dxil
|   |   |   |-- PrismCatalogFilters10To14.frag.hlsl
|   |   |   |-- PrismCatalogFilters10To14.frag.msl
|   |   |   |-- PrismCatalogFilters10To14.frag.spv
|   |   |   |-- PrismCatalogFilters15To20.frag.dxil
|   |   |   |-- PrismCatalogFilters15To20.frag.hlsl
|   |   |   |-- PrismCatalogFilters15To20.frag.msl
|   |   |   |-- PrismCatalogFilters15To20.frag.spv
|   |   |   |-- PrismCatalogFilters21To26.frag.dxil
|   |   |   |-- PrismCatalogFilters21To26.frag.hlsl
|   |   |   |-- PrismCatalogFilters21To26.frag.msl
|   |   |   |-- PrismCatalogFilters21To26.frag.spv
|   |   |   |-- PrismCatalogFilters27To32.frag.dxil
|   |   |   |-- PrismCatalogFilters27To32.frag.hlsl
|   |   |   |-- PrismCatalogFilters27To32.frag.msl
|   |   |   |-- PrismCatalogFilters27To32.frag.spv
|   |   |   |-- PrismCatalogFilters33To39.frag.dxil
|   |   |   |-- PrismCatalogFilters33To39.frag.hlsl
|   |   |   |-- PrismCatalogFilters33To39.frag.msl
|   |   |   |-- PrismCatalogFilters33To39.frag.spv
|   |   |   |-- PrismCatalogFilters89To97.frag.dxil
|   |   |   |-- PrismCatalogFilters89To97.frag.hlsl
|   |   |   |-- PrismCatalogFilters89To97.frag.msl
|   |   |   |-- PrismCatalogFilters89To97.frag.spv
|   |   |   |-- PrismCatalogProcedural.frag.dxil
|   |   |   |-- PrismCatalogProcedural.frag.hlsl
|   |   |   |-- PrismCatalogProcedural.frag.msl
|   |   |   |-- PrismCatalogProcedural.frag.spv
|   |   |   |-- PrismCatalogSpatial.frag.dxil
|   |   |   |-- PrismCatalogSpatial.frag.hlsl
|   |   |   |-- PrismCatalogSpatial.frag.msl
|   |   |   |-- PrismCatalogSpatial.frag.spv
|   |   |   |-- PrismComposite.frag.dxil
|   |   |   |-- PrismComposite.frag.hlsl
|   |   |   |-- PrismComposite.frag.msl
|   |   |   |-- PrismComposite.frag.spv
|   |   |   |-- PrismCopy.frag.dxil
|   |   |   |-- PrismCopy.frag.hlsl
|   |   |   |-- PrismCopy.frag.msl
|   |   |   |-- PrismCopy.frag.spv
|   |   |   |-- PrismKernels.hlsl
|   |   |   |-- PrismNeighborhood.frag.dxil
|   |   |   |-- PrismNeighborhood.frag.hlsl
|   |   |   |-- PrismNeighborhood.frag.msl
|   |   |   |-- PrismNeighborhood.frag.spv
|   |   |   |-- PrismPresentation.frag.dxil
|   |   |   |-- PrismPresentation.frag.hlsl
|   |   |   |-- PrismPresentation.frag.msl
|   |   |   |-- PrismPresentation.frag.spv
|   |   |   |-- PrismResampling.frag.dxil
|   |   |   |-- PrismResampling.frag.hlsl
|   |   |   |-- PrismResampling.frag.msl
|   |   |   |-- PrismResampling.frag.spv
|   |   |   |-- PrismStyle.frag.dxil
|   |   |   |-- PrismStyle.frag.hlsl
|   |   |   |-- PrismStyle.frag.msl
|   |   |   +-- PrismStyle.frag.spv
|   |   |-- ISdlGpuBackdropFrameLease.cs
|   |   |-- SdlGpuPrismDeviceResources.cs
|   |   |-- SdlGpuPrismExecutionColdStartWarmup.cs
|   |   |-- SdlGpuPrismExecutor.cs
|   |   |-- SdlGpuPrismExecutor.Filters.cs
|   |   |-- SdlGpuPrismExecutor.RasterExtents.cs
|   |   |-- SdlGpuPrismExecutor.Rendering.cs
|   |   |-- SdlGpuPrismExecutor.Styles.cs
|   |   |-- SdlGpuPrismFragmentShader.cs
|   |   |-- SdlGpuPrismKernelSelector.cs
|   |   +-- SdlGpuPrismUniforms.cs
|   |-- Shaders/
|   |   |-- artifacts.json
|   |   +-- manifest.json
|   +-- Cerneala.Backends.SdlGpu.csproj
|-- Cerneala.Language/
|   |-- Compatibility/
|   |   +-- IsExternalInit.cs
|   |-- Diagnostics/
|   |   |-- AnalysisMode.cs
|   |   |-- CernealaDiagnosticCatalog.cs
|   |   |-- LanguageDiagnostic.cs
|   |   |-- LanguageDiagnosticDescriptor.cs
|   |   +-- LanguageDiagnosticSeverity.cs
|   |-- Features/
|   |   |-- CernealaCodeActionService.cs
|   |   |-- CernealaCompletionModels.cs
|   |   |-- CernealaCompletionService.Context.cs
|   |   |-- CernealaCompletionService.cs
|   |   |-- CernealaCompletionService.Directives.cs
|   |   |-- CernealaCompletionService.Timbre.cs
|   |   |-- CernealaDocumentation.cs
|   |   |-- CernealaFormattingModels.cs
|   |   |-- CernealaFormattingService.cs
|   |   |-- CernealaLanguageFacts.cs
|   |   |-- CernealaNavigationModels.cs
|   |   |-- CernealaNavigationService.cs
|   |   |-- CernealaStructureModels.cs
|   |   +-- CernealaStructureService.cs
|   |-- Prism/
|   |   +-- Catalog/
|   |       |-- PrismBlendRangeRule.cs
|   |       +-- PrismLanguageCatalog.cs
|   |-- Properties/
|   |   +-- AssemblyInfo.cs
|   |-- Semantics/
|   |   |-- Symbols/
|   |   |   |-- LanguageSymbols.cs
|   |   |   +-- RoslynCompilationSymbols.cs
|   |   |-- CernealaCompilation.cs
|   |   |-- CernealaDocument.cs
|   |   |-- CernealaSemanticModel.AspectAttachments.cs
|   |   |-- CernealaSemanticModel.Bindings.cs
|   |   |-- CernealaSemanticModel.Completion.cs
|   |   |-- CernealaSemanticModel.cs
|   |   |-- CernealaSemanticModel.MotionPrism.cs
|   |   |-- CernealaSemanticModel.MotionPrism.Motion.cs
|   |   |-- CernealaSemanticModel.MotionPrism.Prism.cs
|   |   |-- CernealaSemanticModel.Navigation.cs
|   |   |-- CernealaSemanticModel.Scopes.cs
|   |   |-- CernealaSemanticModel.Tiles.cs
|   |   |-- CernealaSemanticModel.Timbre.cs
|   |   |-- CernealaSemanticModel.TimbreMotion.cs
|   |   +-- SemanticSymbols.cs
|   |-- Syntax/
|   |   |-- Embedded/
|   |   |   |-- AspectAttachmentScanner.cs
|   |   |   |-- BindingSyntax.cs
|   |   |   |-- BindingSyntaxParser.cs
|   |   |   |-- DirectiveSyntaxParser.cs
|   |   |   |-- EmbeddedSyntax.cs
|   |   |   |-- MotionSyntaxParser.cs
|   |   |   |-- PrismModelSyntax.cs
|   |   |   +-- PrismSyntaxParser.cs
|   |   |-- MarkupLexer.cs
|   |   |-- MarkupParser.cs
|   |   |-- SyntaxDiagnostic.cs
|   |   |-- SyntaxKind.cs
|   |   |-- SyntaxNode.cs
|   |   +-- SyntaxToken.cs
|   |-- Text/
|   |   |-- LinePosition.cs
|   |   |-- SourceText.cs
|   |   |-- TextChange.cs
|   |   +-- TextSpan.cs
|   |-- Timbre/
|   |   |-- TimbreMarkupBinder.cs
|   |   +-- TimbreMarkupSyntax.cs
|   |-- Cerneala.Language.csproj
|   +-- CernealaDocumentPath.cs
|-- Cerneala.LanguageServer/
|   |-- Features/
|   |   |-- BuildDiagnosticStore.cs
|   |   |-- CompletionService.cs
|   |   |-- DiagnosticPublisher.cs
|   |   |-- DiagnosticService.cs
|   |   |-- FormattingService.cs
|   |   |-- LspTextCoordinates.cs
|   |   |-- NavigationService.cs
|   |   +-- StructureService.cs
|   |-- Logging/
|   |   |-- CrashReporter.cs
|   |   |-- IServerLogger.cs
|   |   |-- ServerTelemetry.cs
|   |   |-- ServerTraceLevel.cs
|   |   +-- StructuredServerLogger.cs
|   |-- Properties/
|   |   +-- AssemblyInfo.cs
|   |-- Protocol/
|   |   |-- LanguageServerEndpoint.cs
|   |   |-- LanguageServerHost.cs
|   |   +-- LspContracts.cs
|   |-- Workspace/
|   |   |-- CernealaWorkspace.cs
|   |   |-- DocumentOverlayStore.cs
|   |   |-- PathComparer.cs
|   |   |-- ProjectContext.cs
|   |   |-- WorkspaceConfiguration.cs
|   |   |-- WorkspaceDocumentSnapshot.cs
|   |   |-- WorkspaceInfoDiagnostic.cs
|   |   |-- WorkspaceProjectSummary.cs
|   |   +-- WorkspaceState.cs
|   |-- Cerneala.LanguageServer.csproj
|   +-- Program.cs
|-- Cerneala.Platforms.Sdl3/
|   |-- Audio/
|   |   |-- ISdlAudioApi.cs
|   |   |-- NativeSdlAudioApi.cs
|   |   +-- SdlTimbreOutput.cs
|   |-- Hosting/
|   |   |-- SdlCursorService.cs
|   |   |-- SdlPlatformLifetime.cs
|   |   |-- SdlWindowPlatform.cs
|   |   +-- SdlWindowSurface.cs
|   |-- Input/
|   |   +-- SdlInputSource.cs
|   |-- Interop/
|   |   |-- ISdlApi.cs
|   |   |-- NativeSdlApi.cs
|   |   +-- SdlWindowsWindowFeatures.cs
|   +-- Cerneala.Platforms.Sdl3.csproj
|-- Cerneala.PreviewHost/
|   |-- Properties/
|   |   +-- AssemblyInfo.cs
|   |-- Cerneala.PreviewHost.csproj
|   |-- DisabledPreviewTimbreOutput.cs
|   |-- PreviewCompiler.cs
|   |-- PreviewHostServer.cs
|   |-- PreviewMarkupHotReload.cs
|   |-- PreviewRenderSession.cs
|   +-- Program.cs
|-- Cerneala.Scene2D.Importers/
|   |-- Cerneala.Scene2D.Importers.csproj
|   |-- ImportContext.cs
|   |-- ImportConventions.cs
|   |-- LdtkScene2DImporter.cs
|   |-- Scene2DImportOptions.cs
|   |-- Scene2DImportResult.cs
|   |-- TileDataDecoder.cs
|   +-- TiledScene2DImporter.cs
|-- Cerneala.Scene2D.Packages/
|   |-- Cerneala.Scene2D.Packages.csproj
|   |-- IScene2DPackageRangeReader.cs
|   |-- LocalPackageRangeReader.cs
|   |-- PackageFiles.cs
|   |-- PackageIndex.cs
|   |-- PackageValueCodec.cs
|   |-- Scene2DPackage.cs
|   |-- Scene2DPackageEntityInfo.cs
|   |-- Scene2DPackageGridChunkMetadata.cs
|   |-- Scene2DPackageLevel.cs
|   |-- Scene2DPackageLevel.Model.cs
|   |-- Scene2DPackageMetadata.cs
|   +-- Scene2DPackageWriter.cs
|-- Cerneala.SourceGen/
|   |-- Prism/
|   |   |-- Binding/
|   |   |   |-- BoundPrismModel.cs
|   |   |   |-- PrismMarkupBinder.cs
|   |   |   +-- PrismMotionResolver.cs
|   |   |-- Catalog/
|   |   |   |-- prism-catalog.json
|   |   |   +-- prism-catalog.schema.json
|   |   |-- Emission/
|   |   |   |-- PrismMarkupEmitter.cs
|   |   |   +-- PrismMotionEmitter.cs
|   |   |-- Syntax/
|   |   |   |-- PrismDirectiveParser.cs
|   |   |   |-- PrismMarkupLanguage.cs
|   |   |   +-- PrismSyntax.cs
|   |   |-- PrismCatalogCompiler.cs
|   |   |-- PrismCatalogGenerator.cs
|   |   +-- PrismOperationSourceEmitter.cs
|   |-- Properties/
|   |   +-- AssemblyInfo.cs
|   |-- Cerneala.SourceGen.csproj
|   |-- MarkupDom.cs
|   |-- MotionMarkupLanguage.cs
|   |-- SourceGeneratorDiagnosticAdapter.cs
|   |-- SourceGeneratorSemanticModel.cs
|   |-- UiMarkupApplicationGenerator.cs
|   |-- UiMarkupAspectEmitter.cs
|   |-- UiMarkupAspectReader.cs
|   |-- UiMarkupAspectSites.cs
|   |-- UiMarkupBackendSelection.cs
|   |-- UiMarkupBindingResolver.cs
|   |-- UiMarkupBrushEmitter.cs
|   |-- UiMarkupCompilationFacts.cs
|   |-- UiMarkupDirectiveParser.cs
|   |-- UiMarkupDirectiveParser.Expressions.cs
|   |-- UiMarkupDirectiveParser.MotionExecutions.cs
|   |-- UiMarkupDirectiveParser.MotionTriggers.cs
|   |-- UiMarkupDirectiveParser.MotionValues.cs
|   |-- UiMarkupDirectiveParser.Syntax.cs
|   |-- UiMarkupDirectiveParser.Timbre.cs
|   |-- UiMarkupElementEmitter.cs
|   |-- UiMarkupGenerator.cs
|   |-- UiMarkupMotionActivationEmitter.cs
|   |-- UiMarkupMotionResolver.cs
|   |-- UiMarkupMotionSpecResolver.cs
|   |-- UiMarkupMotionSyntax.cs
|   |-- UiMarkupPropertyElementEmitter.cs
|   |-- UiMarkupPropertyEmitter.cs
|   |-- UiMarkupPropertyResolver.cs
|   |-- UiMarkupReactiveEmitter.cs
|   |-- UiMarkupResourceEmitter.cs
|   |-- UiMarkupSceneComponentGenerator.cs
|   |-- UiMarkupTemplateEmitter.cs
|   |-- UiMarkupTimbreEmitter.cs
|   |-- UiMarkupTimbreMotionEmitter.cs
|   |-- UiMarkupUserControlGenerator.cs
|   |-- UiMarkupValueEmitter.cs
|   +-- UiMarkupWindowGenerator.cs
|-- Cerneala.VisualStudio/
|   |-- Assets/
|   |   +-- cerneala.png
|   |-- Grammars/
|   |   +-- cerneala.tmLanguage.json
|   |-- Preview/
|   |   |-- CernealaPreviewChrome.cs
|   |   |-- CernealaPreviewHostClient.cs
|   |   |-- CernealaPreviewInputQueue.cs
|   |   |-- CernealaPreviewMargin.cs
|   |   |-- CernealaPreviewMarginProvider.cs
|   |   |-- CernealaPreviewModeBar.cs
|   |   |-- CernealaPreviewPixels.cs
|   |   |-- CernealaPreviewSession.cs
|   |   +-- CernealaPreviewSurface.cs
|   |-- Server/
|   |   |-- CernealaServerProcessManager.cs
|   |   +-- SystemCernealaServerProcess.cs
|   |-- Cerneala.pkgdef
|   |-- Cerneala.VisualStudio.csproj
|   |-- CernealaCommands.vsct
|   |-- CernealaContentType.cs
|   |-- CernealaLanguageServerProvider.cs
|   |-- CernealaOutputChannel.cs
|   |-- CernealaPackage.cs
|   |-- language-configuration.json
|   |-- RestartLanguageServerCommand.cs
|   |-- source.extension.vsixmanifest
|   |-- THIRD-PARTY-NOTICES.txt
|   +-- VisualStudioServerLog.cs
|-- CernealaPresentation/
|   |-- Properties/
|   |   +-- AssemblyInfo.cs
|   |-- App.crn
|   |-- App.crn.cs
|   |-- AspectChapterView.crn
|   |-- AspectChapterView.crn.cs
|   |-- AspectStudioPropertyRows.cs
|   |-- BackendRegistration.cs
|   |-- BrandMark.crn
|   |-- BrandMark.crn.cs
|   |-- CernealaPresentation.csproj
|   |-- FramePipelineChapterView.crn
|   |-- FramePipelineChapterView.crn.cs
|   |-- MarkupChapterView.crn
|   |-- MarkupChapterView.crn.cs
|   |-- MotionChapterView.crn
|   |-- MotionChapterView.crn.cs
|   |-- OpeningView.crn
|   |-- OpeningView.crn.cs
|   |-- PresentationWindow.crn
|   |-- PresentationWindow.crn.cs
|   |-- PresentationWindow.OuterGlowLab.cs
|   |-- PresentationWindow.Servo.cs
|   |-- PrismChapterView.crn
|   |-- PrismChapterView.crn.cs
|   |-- PrismOuterGlowLabView.crn
|   |-- PrismOuterGlowLabView.crn.cs
|   |-- PrismStudioModel.cs
|   |-- PrismStudioRows.cs
|   |-- RetainedModelChapterView.crn
|   |-- RetainedModelChapterView.crn.cs
|   |-- WelcomeChapterView.crn
|   +-- WelcomeChapterView.crn.cs
|-- docs/
|   |-- architecture/
|   |   |-- diagrams/
|   |   |   |-- cerneala-drawing-flowchart.svg
|   |   |   |-- retained-frame-loop.md
|   |   |   +-- ui-layer-boundaries.md
|   |   |-- accessibility.md
|   |   |-- aspect.md
|   |   |-- build-tools.md
|   |   |-- detective.md
|   |   |-- drawing.md
|   |   |-- hosting-and-platform.md
|   |   |-- ink.md
|   |   |-- input.md
|   |   |-- invalidation-and-frame.md
|   |   |-- language-tooling.md
|   |   |-- layout.md
|   |   |-- markup-and-sourcegen.md
|   |   |-- motion.md
|   |   |-- overview.md
|   |   |-- prism-technical-design.md
|   |   |-- property-system.md
|   |   |-- relay.md
|   |   |-- rendering.md
|   |   |-- scene2d.md
|   |   |-- sdl-desktop-backend.md
|   |   |-- servo.md
|   |   |-- text.md
|   |   |-- theming-and-resources.md
|   |   +-- timbre.md
|   |-- archive/
|   |   |-- checklists/
|   |   |   |-- 2026-08-18-class-checklist.md
|   |   |   |-- aspect-checklist.md
|   |   |   |-- documentation-checklist.md
|   |   |   |-- motion-checklist.md
|   |   |   +-- prism-checklist.md
|   |   |-- roadmap-v2/
|   |   |   |-- audit-fix-plan.md
|   |   |   |-- roadmap-v2-audit.md
|   |   |   +-- roadmap-v2.md
|   |   |-- superpowers/
|   |   |   |-- plans/
|   |   |   |   |-- 2026-07-03-fix-retained-render-frame-contract.md
|   |   |   |   |-- 2026-07-03-fix-tree-mutation-invalidation.md
|   |   |   |   |-- 2026-07-03-integrate-style-phase.md
|   |   |   |   |-- 2026-07-04-cache-input-route-hit-test.md
|   |   |   |   |-- 2026-07-05-authoring-preview-completion-gate.md
|   |   |   |   |-- 2026-07-05-clarify-layout-scheduler-contract-and-diagnostics.md
|   |   |   |   |-- 2026-07-05-clarify-package-boundary-dependencies.md
|   |   |   |   |-- 2026-07-05-clarify-text-services-mvp.md
|   |   |   |   |-- 2026-07-05-complete-textblock-layout-contract.md
|   |   |   |   |-- 2026-07-05-consolidate-button-content-composition.md
|   |   |   |   |-- 2026-07-05-core-preview-completion-gate.md
|   |   |   |   |-- 2026-07-05-create-retained-ui-mvp-vertical-slice.md
|   |   |   |   |-- 2026-07-05-default-theme-and-style-vertical-slice.md
|   |   |   |   |-- 2026-07-05-fix-focus-visibility-semantics.md
|   |   |   |   |-- 2026-07-05-fix-viewport-and-pre-input-frame-contract.md
|   |   |   |   |-- 2026-07-05-freeze-later-experimental-scope.md
|   |   |   |   |-- 2026-07-05-implement-inherited-property-tree-propagation.md
|   |   |   |   |-- 2026-07-05-items-scroll-data-vertical-slice.md
|   |   |   |   |-- 2026-07-05-next-authoring-preview-plan-index.md
|   |   |   |   |-- 2026-07-05-next-core-completion-plan-index.md
|   |   |   |   |-- 2026-07-05-next-core-preview-plan-index.md
|   |   |   |   |-- 2026-07-05-observable-items-source-and-recycling-stability.md
|   |   |   |   |-- 2026-07-05-retained-command-state-refresh.md
|   |   |   |   |-- 2026-07-05-retained-semantics-tree-core-contract.md
|   |   |   |   |-- 2026-07-05-root-owned-resource-invalidation.md
|   |   |   |   |-- 2026-07-05-template-content-presenter-state-contract.md
|   |   |   |   |-- 2026-07-05-textbox-editing-viewport-and-caret-contract.md
|   |   |   |   |-- 2026-07-05-typed-binding-lifetime-and-two-way-text.md
|   |   |   |   |-- 2026-07-05-wire-keyboard-control-activation.md
|   |   |   |   |-- 2026-07-05-wire-minimal-retained-input-bindings.md
|   |   |   |   |-- 2026-07-06-add-preview-api-scope-guardrails.md
|   |   |   |   |-- 2026-07-06-add-retained-stress-budget-tests.md
|   |   |   |   |-- 2026-07-06-cache-content-resources-and-textures-lifetime.md
|   |   |   |   |-- 2026-07-06-close-retained-lifecycle-subscription-leaks.md
|   |   |   |   |-- 2026-07-06-create-developer-preview-docs-and-sample-gate.md
|   |   |   |   |-- 2026-07-06-developer-preview-completion-gate.md
|   |   |   |   |-- 2026-07-06-harden-layout-authoring-mutation-contracts.md
|   |   |   |   |-- 2026-07-06-harden-monogame-render-backend-state.md
|   |   |   |   |-- 2026-07-06-next-developer-preview-hardening-plan-index.md
|   |   |   |   |-- 2026-07-06-next-runtime-preview-plan-index.md
|   |   |   |   |-- 2026-07-06-normalize-viewport-scale-pointer-and-render-coordinates.md
|   |   |   |   |-- 2026-07-06-runtime-diagnostics-and-playground-polish.md
|   |   |   |   |-- 2026-07-06-runtime-preview-completion-gate.md
|   |   |   |   |-- 2026-07-06-textbox-caret-blink-hit-testing.md
|   |   |   |   |-- 2026-07-06-wire-platform-services-cursor-and-clipboard.md
|   |   |   |   |-- 2026-07-06-wire-tab-focus-navigation-contract.md
|   |   |   |   |-- 2026-07-07-modern-motion-system.md
|   |   |   |   |-- 2026-07-08-modern-aspect-template-system.md
|   |   |   |   |-- 2026-07-09-markup-aspect-resources.md
|   |   |   |   +-- developer-preview-smoke-failure-fix-plan.md
|   |   |   |-- specs/
|   |   |   |   +-- 2026-07-09-markup-aspect-resources-design.md
|   |   |   +-- README.md
|   |   |-- architecture-v2.md
|   |   |-- conceptual-ideas.md
|   |   |-- developer-preview-checklist.md
|   |   +-- visual-studio-community-spike.md
|   |-- assets/
|   |   +-- cerneala-architecture.png
|   |-- audits/
|   |   |-- 2026-07-25-prism-visual-algorithm-checklist.md
|   |   |-- 2026-08-02-prism-visual-style-algorithm-checklist.md
|   |   |-- 2026-08-25-monogame-coupling-inventory.md
|   |   |-- 2026-08-25-sdl3-sdlgpu-stage-0-baseline.md
|   |   |-- 2026-08-26-sdlgpu-stage-5-drawing-conformance.md
|   |   |-- 2026-08-26-sdlgpu-stage-6-shader-toolchain.md
|   |   |-- 2026-09-02-aspect-audit.md
|   |   |-- 2026-09-02-motion-audit.md
|   |   |-- 2026-09-02-prism-audit.md
|   |   |-- 2026-09-02-relay-audit.md
|   |   |-- 2026-09-05-monogame-removal.md
|   |   +-- 2026-09-08-prism-architecture-remediation.md
|   |-- guides/
|   |   |-- application-markup.md
|   |   |-- getting-started.md
|   |   |-- language-server.md
|   |   |-- markup-guide.md
|   |   |-- motion-api.md
|   |   |-- motion-diagnostics.md
|   |   |-- prism-guide.md
|   |   |-- servo.md
|   |   |-- timbre-guide.md
|   |   +-- visual-studio-community.md
|   |-- plans/
|   |   |-- 2026-07-10-inline-component-template-markup.md
|   |   |-- 2026-07-10-window-windowsdx-migration.md
|   |   |-- 2026-07-11-background-and-borderbrush-brush-migration.md
|   |   |-- 2026-07-11-brush-types-and-rendering.md
|   |   |-- 2026-07-11-foreground-brush-migration.md
|   |   |-- 2026-07-13-markup-logical-expressions.md
|   |   |-- 2026-07-13-queue-engine-2.md
|   |   |-- 2026-07-13-repeat-button.md
|   |   |-- 2026-07-13-scrollviewer-template-parts.md
|   |   |-- 2026-07-14-markup-data-bindings.md
|   |   |-- 2026-07-14-relay-auto-marshaling-implementation-notes.md
|   |   |-- 2026-07-14-relay-auto-marshaling.md
|   |   |-- 2026-07-15-motion-markup-composition-and-clips.md
|   |   |-- 2026-07-15-motion-markup-foundation.md
|   |   |-- 2026-07-15-motion-markup-integration-and-hardening.md
|   |   |-- 2026-07-15-motion-markup-plan-index.md
|   |   |-- 2026-07-15-motion-markup-presence-and-layout.md
|   |   |-- 2026-07-15-motion-markup-scroll-and-input.md
|   |   |-- 2026-07-15-motion-markup-timelines-and-specs.md
|   |   |-- 2026-07-17-application-markup-and-startup-contract.md
|   |   |-- 2026-07-17-text-texture-cache-frame-budget-hardening.md
|   |   |-- 2026-07-18-prism-backdrop-hosting.md
|   |   |-- 2026-07-18-prism-color-blend-and-styles.md
|   |   |-- 2026-07-18-prism-filter-catalog.md
|   |   |-- 2026-07-18-prism-foundation-and-catalog.md
|   |   |-- 2026-07-18-prism-integration-and-hardening.md
|   |   |-- 2026-07-18-prism-markup-motion-and-lifecycle.md
|   |   |-- 2026-07-18-prism-monogame-compositor.md
|   |   |-- 2026-07-18-prism-plan-index.md
|   |   |-- 2026-07-18-prism-retained-composition-graph.md
|   |   |-- 2026-07-18-prism-retained-pixel-cache.md
|   |   |-- 2026-07-22-prism-studio-presentation.md
|   |   |-- 2026-07-28-motion-studio.md
|   |   |-- 2026-08-13-cerneala-language-core.md
|   |   |-- 2026-08-13-cerneala-language-server.md
|   |   |-- 2026-08-13-visual-studio-community-extension.md
|   |   |-- 2026-08-13-visual-studio-language-integration-plan-index.md
|   |   |-- 2026-08-14-crn-markup-extension-migration.md
|   |   |-- 2026-08-20-menu-menuitem-menubar.md
|   |   |-- 2026-08-24-rendersurface2d-complete-drawing-api.md
|   |   |-- 2026-08-24-rendersurface2d-stage-0-baseline.md
|   |   |-- 2026-08-25-sdl3-sdlgpu-backend-and-explicit-generator-selection.md
|   |   |-- 2026-08-27-unify-aspect-runtime.md
|   |   |-- 2026-09-02-private-sdlgpu-cerberus.md
|   |   |-- 2026-09-03-servo-code-first-ui-automation.md
|   |   |-- 2026-09-04-rendersurface2d-collision-and-picking.md
|   |   |-- 2026-09-04-rendersurface2d-import-debug-validation.md
|   |   |-- 2026-09-04-rendersurface2d-scene-foundation.md
|   |   |-- 2026-09-04-rendersurface2d-sprite-animation.md
|   |   |-- 2026-09-04-rendersurface2d-tilemap-and-scale.md
|   |   |-- 2026-09-04-rendersurface2d-world-authoring-plan-index.md
|   |   |-- 2026-09-21-rendersurface3d-control.md
|   |   |-- 2026-09-21-rendersurface3d-gpu-foundation.md
|   |   |-- 2026-09-21-rendersurface3d-plan-index.md
|   |   |-- 2026-09-22-sdlgpu-retained-submit-prerequisite.md
|   |   |-- 2026-09-23-scene2d-simple-collections-and-internal-spatial.md
|   |   |-- 2026-09-24-scene2d-stage0-contract-proposal.md
|   |   |-- 2026-10-03-timbre-core-runtime.md
|   |   |-- 2026-10-03-timbre-decoding-streaming.md
|   |   |-- 2026-10-03-timbre-markup-aspect.md
|   |   |-- 2026-10-03-timbre-motion-parameters.md
|   |   |-- 2026-10-03-timbre-sdl3-backend.md
|   |   |-- 2026-10-08-aspect-runtime-program.md
|   |   |-- 2026-10-08-decizii-pentru-review.md
|   |   |-- 2026-10-08-timbre-prism-in-aspect.md
|   |   |-- 2026-10-10-architecture-docs.md
|   |   |-- 2026-10-10-repo-docs-reorganization.md
|   |   |-- 2026-10-11-docs-content-audit.md
|   |   +-- README.md
|   |-- reference/
|   |   |-- developer-preview-scope.md
|   |   |-- markup-data-bindings.md
|   |   |-- motion-markup-syntax.md
|   |   |-- prism-adjustment-filters.md
|   |   |-- prism-catalog-filters.md
|   |   |-- prism-completeness-report.generated.md
|   |   |-- prism-distortion-filters.md
|   |   |-- prism-filter-reference.generated.md
|   |   |-- prism-markup-syntax.md
|   |   |-- prism-neighborhood-filters.md
|   |   |-- prism-public-api-baseline.md
|   |   +-- wpf-event-coverage.md
|   +-- README.md
|-- docs-site/
|   |-- assets/
|   |   |-- cerneala-mascot-void-suction-well.png
|   |   |-- cerneala-mascot-void-suction-well.svg
|   |   |-- cerneala-mascot-void-suction-well.svg.cerneala.png
|   |   +-- cerneala-mascot-void-suction-well.svg.cerneala.png.sha256
|   |-- documentation/
|   |   |-- classes/
|   |   |   |-- Cerneala.Drawing.Color.md
|   |   |   |-- Cerneala.Drawing.DrawAddressMode.md
|   |   |   |-- Cerneala.Drawing.DrawArcDirection.md
|   |   |   |-- Cerneala.Drawing.DrawBlendMode.md
|   |   |   |-- Cerneala.Drawing.DrawBlendScope.md
|   |   |   |-- Cerneala.Drawing.DrawBrushAlignmentX.md
|   |   |   |-- Cerneala.Drawing.DrawBrushAlignmentY.md
|   |   |   |-- Cerneala.Drawing.DrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.DrawBrushKind.md
|   |   |   |-- Cerneala.Drawing.DrawBrushStretch.md
|   |   |   |-- Cerneala.Drawing.DrawClipScope.md
|   |   |   |-- Cerneala.Drawing.DrawCommand.md
|   |   |   |-- Cerneala.Drawing.DrawCommandKind.md
|   |   |   |-- Cerneala.Drawing.DrawCommandList.md
|   |   |   |-- Cerneala.Drawing.DrawCommandStateAnalysis.md
|   |   |   |-- Cerneala.Drawing.DrawCommandStateAnalyzer.md
|   |   |   |-- Cerneala.Drawing.DrawCommandStateEntry.md
|   |   |   |-- Cerneala.Drawing.DrawCornerRadius.md
|   |   |   |-- Cerneala.Drawing.DrawFillRule.md
|   |   |   |-- Cerneala.Drawing.DrawGradientStop.md
|   |   |   |-- Cerneala.Drawing.DrawImageFlip.md
|   |   |   |-- Cerneala.Drawing.DrawImageOptions.md
|   |   |   |-- Cerneala.Drawing.DrawingContext.md
|   |   |   |-- Cerneala.Drawing.DrawingDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.DrawingFrameContext.md
|   |   |   |-- Cerneala.Drawing.DrawInsets.md
|   |   |   |-- Cerneala.Drawing.DrawLayerOptions.md
|   |   |   |-- Cerneala.Drawing.DrawLayerScope.md
|   |   |   |-- Cerneala.Drawing.DrawLineBatch.md
|   |   |   |-- Cerneala.Drawing.DrawLineCap.md
|   |   |   |-- Cerneala.Drawing.DrawLineJoin.md
|   |   |   |-- Cerneala.Drawing.DrawLineSegment2D.md
|   |   |   |-- Cerneala.Drawing.DrawLineSegment3D.md
|   |   |   |-- Cerneala.Drawing.DrawMarker3D.md
|   |   |   |-- Cerneala.Drawing.DrawMesh2D.md
|   |   |   |-- Cerneala.Drawing.DrawOpacityScope.md
|   |   |   |-- Cerneala.Drawing.DrawPath.md
|   |   |   |-- Cerneala.Drawing.DrawPathBuilder.md
|   |   |   |-- Cerneala.Drawing.DrawPathContour.md
|   |   |   |-- Cerneala.Drawing.DrawPathFactory.md
|   |   |   |-- Cerneala.Drawing.DrawPathParser.md
|   |   |   |-- Cerneala.Drawing.DrawPathSegment.md
|   |   |   |-- Cerneala.Drawing.DrawPathSegmentKind.md
|   |   |   |-- Cerneala.Drawing.DrawPen.md
|   |   |   |-- Cerneala.Drawing.DrawPoint.md
|   |   |   |-- Cerneala.Drawing.DrawPointBatch.md
|   |   |   |-- Cerneala.Drawing.DrawPrimitiveTopology.md
|   |   |   |-- Cerneala.Drawing.DrawRay3D.md
|   |   |   |-- Cerneala.Drawing.DrawRect.md
|   |   |   |-- Cerneala.Drawing.DrawSamplingMode.md
|   |   |   |-- Cerneala.Drawing.DrawSize.md
|   |   |   |-- Cerneala.Drawing.DrawSprite2D.md
|   |   |   |-- Cerneala.Drawing.DrawSpriteBatch.md
|   |   |   |-- Cerneala.Drawing.DrawStrokeAlignment.md
|   |   |   |-- Cerneala.Drawing.DrawStrokeStyle.md
|   |   |   |-- Cerneala.Drawing.DrawTextAlignment.md
|   |   |   |-- Cerneala.Drawing.DrawTextDirection.md
|   |   |   |-- Cerneala.Drawing.DrawTextLayout.md
|   |   |   |-- Cerneala.Drawing.DrawTextLayoutBuilder.md
|   |   |   |-- Cerneala.Drawing.DrawTextLayoutLine.md
|   |   |   |-- Cerneala.Drawing.DrawTextLayoutOptions.md
|   |   |   |-- Cerneala.Drawing.DrawTextLayoutRun.md
|   |   |   |-- Cerneala.Drawing.DrawTextRun.md
|   |   |   |-- Cerneala.Drawing.DrawTextSpan.md
|   |   |   |-- Cerneala.Drawing.DrawTextTrimming.md
|   |   |   |-- Cerneala.Drawing.DrawTextWrapping.md
|   |   |   |-- Cerneala.Drawing.DrawTileMode.md
|   |   |   |-- Cerneala.Drawing.DrawTransformScope.md
|   |   |   |-- Cerneala.Drawing.DrawVertex2D.md
|   |   |   |-- Cerneala.Drawing.IDrawBrush.md
|   |   |   |-- Cerneala.Drawing.IDrawFont.md
|   |   |   |-- Cerneala.Drawing.IDrawImage.md
|   |   |   |-- Cerneala.Drawing.IDrawingBackend.md
|   |   |   |-- Cerneala.Drawing.IFontSource.md
|   |   |   |-- Cerneala.Drawing.ImageDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.LinearGradientDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.Prism.AccentedEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.AdaptiveWideAngleFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.AddNoiseFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.AngledStrokesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.AverageFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BackdropAlphaMode.md
|   |   |   |-- Cerneala.Drawing.Prism.BackdropFrameMetadata.md
|   |   |   |-- Cerneala.Drawing.Prism.BackdropFrameRequest.md
|   |   |   |-- Cerneala.Drawing.Prism.BackdropPixelFormat.md
|   |   |   |-- Cerneala.Drawing.Prism.BasReliefFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BevelEmbossStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.BlackWhiteFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BlurMoreFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BoxBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.BrightnessContrastFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismBlendMode.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismCatalog.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismCatalogOperationInfo.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismCatalogOperationKind.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismCatalogParameterInfo.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismCatalogValueKind.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismColorProfile.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismFilterId.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismSampling.md
|   |   |   |-- Cerneala.Drawing.Prism.Catalog.PrismStyleId.md
|   |   |   |-- Cerneala.Drawing.Prism.ChalkCharcoalFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ChannelMixerFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CharcoalFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ChromaticAberrationFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ChromeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CloudsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorBalanceFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColoredPencilFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorHalftoneFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorLookupFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorMatrixFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ColorOverlayStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.ConteCrayonFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CraquelureFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CrosshatchFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CrystallizeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CurvesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CustomConvolutionFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.CutoutFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DarkStrokesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DeinterlaceFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DespeckleFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DifferenceCloudsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DiffuseFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DiffuseGlowFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DisplaceFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DropShadowStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.DryBrushFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.DustScratchesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.EmbossFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ExposureFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ExtrudeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FacetFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FibersFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FieldBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FilmGrainFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.Filters.PrismLensFlareRaySample.md
|   |   |   |-- Cerneala.Drawing.Prism.Filters.PrismLensProfileFitOptions.md
|   |   |   |-- Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md
|   |   |   |-- Cerneala.Drawing.Prism.FindEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FragmentFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.FrescoFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GaussianBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GlassFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GlowingEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GradientMapFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GradientOverlayStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.GrainFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.GraphicPenFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.HalftonePatternFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.HighPassFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.HueSaturationFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.IBackdropFrameLease.md
|   |   |   |-- Cerneala.Drawing.Prism.IBackdropFrameSource.md
|   |   |   |-- Cerneala.Drawing.Prism.InkOutlinesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.InnerGlowStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.InnerShadowStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.InvertFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.IrisBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LensBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LensCorrectionFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LensFlareFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LevelsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LightingEffectsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.LiquifyFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MaximumFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MedianFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MezzotintFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MinimumFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MosaicFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MosaicTilesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.MotionBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.NeonGlowFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.NotePaperFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.NtscColorsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.OceanRippleFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.OffsetFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.OilPaintFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.OuterGlowStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.PaintDaubsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PaletteKnifeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PatchworkFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PathBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PatternOverlayStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.PhotocopyFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PhotoFilterFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PinchFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PlasterFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PlasticWrapFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PointillizeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PolarCoordinatesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PosterEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PosterizeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.Prism.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismCacheEvictionReason.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismCacheMissReason.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismCacheOwnerToken.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismDependencyChange.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismDrawScope.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismImage.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismOperation.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismPipeline.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismRendererOptions.md
|   |   |   |-- Cerneala.Drawing.Prism.PrismStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.RadialBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ReduceNoiseFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ReticulationFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.RippleFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.RoughPastelsFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SatinStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.ScanlinesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SelectiveColorFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ShapeBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SharpenEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SharpenFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SharpenMoreFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ShearFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SmartBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SmartSharpenFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SmudgeStickFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SolarizeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SpatterFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SpherizeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SpinBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SpongeFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SprayedStrokesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.StainedGlassFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.StampFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.StrokeStyle.md
|   |   |   |-- Cerneala.Drawing.Prism.SumiEFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.SurfaceBlurFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TexturizerFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ThresholdFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TilesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TiltShiftFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TornEdgesFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TraceContourFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TransformFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.TwirlFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.UnderpaintingFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.UnsharpMaskFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.VibranceFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.WatercolorFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.WaterPaperFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.WaveFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.WindFilter.md
|   |   |   |-- Cerneala.Drawing.Prism.ZigZagFilter.md
|   |   |   |-- Cerneala.Drawing.RadialGradientDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.RenderProjection3D.md
|   |   |   |-- Cerneala.Drawing.RenderProjection3DKind.md
|   |   |   |-- Cerneala.Drawing.SolidDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.Text.RasterizedText.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaFont.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaTextRasterizer.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaTextShaper.md
|   |   |   |-- Cerneala.Drawing.Text.SystemFontSource.md
|   |   |   |-- Cerneala.Drawing.Text.TextCaretVerticalMetrics.md
|   |   |   |-- Cerneala.Drawing.Text.TextShaper.md
|   |   |   |-- Cerneala.Drawing.Text.TextShapeResult.md
|   |   |   |-- Cerneala.Drawing.TileDrawBrushDescriptor.md
|   |   |   |-- Cerneala.Drawing.VisualDrawBrushDescriptor.md
|   |   |   |-- Cerneala.GameBootstrap.md
|   |   |   |-- Cerneala.Scene2D.Importers.LdtkScene2DImporter.md
|   |   |   |-- Cerneala.Scene2D.Importers.Scene2DImportOptions.md
|   |   |   |-- Cerneala.Scene2D.Importers.Scene2DImportResult.md
|   |   |   |-- Cerneala.Scene2D.Importers.TiledScene2DImporter.md
|   |   |   |-- Cerneala.Scene2D.Packages.IScene2DPackageRangeReader.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackage.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageEntityInfo.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageGridChunkMetadata.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageLevel.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageMetadata.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackagePart.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageReadOptions.md
|   |   |   |-- Cerneala.Scene2D.Packages.Scene2DPackageWriter.md
|   |   |   |-- Cerneala.SourceGen.UiMarkupGenerator.md
|   |   |   |-- Cerneala.Timbre.Delay.md
|   |   |   |-- Cerneala.Timbre.ITimbreOutput.md
|   |   |   |-- Cerneala.Timbre.ITimbreOutputClient.md
|   |   |   |-- Cerneala.Timbre.LowPass.md
|   |   |   |-- Cerneala.Timbre.TimbreClipDefinition.md
|   |   |   |-- Cerneala.Timbre.TimbreClipSound.md
|   |   |   |-- Cerneala.Timbre.TimbreErrorKind.md
|   |   |   |-- Cerneala.Timbre.TimbreException.md
|   |   |   |-- Cerneala.Timbre.TimbreHandle.md
|   |   |   |-- Cerneala.Timbre.TimbreInput_T_.md
|   |   |   |-- Cerneala.Timbre.TimbreLoading.md
|   |   |   |-- Cerneala.Timbre.TimbreMemoryBudget.md
|   |   |   |-- Cerneala.Timbre.TimbreMemoryReservation.md
|   |   |   |-- Cerneala.Timbre.TimbreModifier.md
|   |   |   |-- Cerneala.Timbre.TimbreParameter_T_.md
|   |   |   |-- Cerneala.Timbre.TimbreParameter.md
|   |   |   |-- Cerneala.Timbre.TimbrePlayback.md
|   |   |   |-- Cerneala.Timbre.TimbrePlaybackResult.md
|   |   |   |-- Cerneala.Timbre.TimbrePlaybackState.md
|   |   |   |-- Cerneala.Timbre.TimbreReader.md
|   |   |   |-- Cerneala.Timbre.TimbreReadResult.md
|   |   |   |-- Cerneala.Timbre.TimbreRuntime.md
|   |   |   |-- Cerneala.Timbre.TimbreRuntimeOptions.md
|   |   |   |-- Cerneala.Timbre.TimbreScope.md
|   |   |   |-- Cerneala.Timbre.TimbreSound.md
|   |   |   |-- Cerneala.Timbre.TimbreSource.md
|   |   |   |-- Cerneala.Timbre.TimbreStartOptions.md
|   |   |   |-- Cerneala.UI.Accessibility.AccessibleName.md
|   |   |   |-- Cerneala.UI.Accessibility.AutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.ButtonAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.ItemsControlAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.MenuAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.MenuItemAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.PasswordBoxAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsNode.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsProperty.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsProvider.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsRole.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsTree.md
|   |   |   |-- Cerneala.UI.Accessibility.TextBoxAutomationPeer.md
|   |   |   |-- Cerneala.UI.Application.md
|   |   |   |-- Cerneala.UI.ApplicationExitEventArgs.md
|   |   |   |-- Cerneala.UI.ApplicationShutdownMode.md
|   |   |   |-- Cerneala.UI.ApplicationStartupEventArgs.md
|   |   |   |-- Cerneala.UI.Aspect.AspectApplicationResult.md
|   |   |   |-- Cerneala.UI.Aspect.AspectAuthoringKind.md
|   |   |   |-- Cerneala.UI.Aspect.AspectBehavior.md
|   |   |   |-- Cerneala.UI.Aspect.AspectCatalog.md
|   |   |   |-- Cerneala.UI.Aspect.AspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionDependency.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionDependencyKind.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionKey.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionResult.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDataContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDataDependency.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDeclaration.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDependencySet.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEngine.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEnvironment.md
|   |   |   |-- Cerneala.UI.Aspect.AspectInvalidation.md
|   |   |   |-- Cerneala.UI.Aspect.AspectInvalidationGraph.md
|   |   |   |-- Cerneala.UI.Aspect.AspectLayer.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMatchContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMotion.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMotionSource.md
|   |   |   |-- Cerneala.UI.Aspect.AspectOrigin.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackage.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackageBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackageDiagnostic.md
|   |   |   |-- Cerneala.UI.Aspect.AspectProcessor.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPropertyConditionBuilder_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRef.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRegistry.md
|   |   |   |-- Cerneala.UI.Aspect.AspectResolutionContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRuleSet.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRuleSetBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.AspectSlot_TOwner_TTarget_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectSlot.md
|   |   |   |-- Cerneala.UI.Aspect.AspectSlotPath.md
|   |   |   |-- Cerneala.UI.Aspect.AspectSpecificity.md
|   |   |   |-- Cerneala.UI.Aspect.AspectState.md
|   |   |   |-- Cerneala.UI.Aspect.AspectStateSet.md
|   |   |   |-- Cerneala.UI.Aspect.AspectTarget.md
|   |   |   |-- Cerneala.UI.Aspect.AspectToken_T_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectToken.md
|   |   |   |-- Cerneala.UI.Aspect.AspectTokenBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.AspectTokenDefinition.md
|   |   |   |-- Cerneala.UI.Aspect.AspectValue_T_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectValue.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantKey_TOwner_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantKey.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantSet.md
|   |   |   |-- Cerneala.UI.Aspect.ComponentAspectBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.ContentTemplateBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectPackage.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Brush.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Color.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Motion.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Spacing.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Stroke.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Typography.md
|   |   |   |-- Cerneala.UI.Aspect.ElementAspect.md
|   |   |   |-- Cerneala.UI.Aspect.ElementAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.ElementAspectValue.md
|   |   |   |-- Cerneala.UI.Aspect.RejectedAspectDeclaration.md
|   |   |   |-- Cerneala.UI.Aspect.ResolvedAspect.md
|   |   |   |-- Cerneala.UI.Aspect.ResolvedAspectValue.md
|   |   |   |-- Cerneala.UI.Aspect.ThemeTokenBridge.md
|   |   |   |-- Cerneala.UI.Controls.Border.md
|   |   |   |-- Cerneala.UI.Controls.BoxCollider2D.md
|   |   |   |-- Cerneala.UI.Controls.Button.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonKind.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonSize.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonSlots.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonTemplates.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonTokens.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonVariants.md
|   |   |   |-- Cerneala.UI.Controls.Canvas.md
|   |   |   |-- Cerneala.UI.Controls.CheckBox.md
|   |   |   |-- Cerneala.UI.Controls.CircleCollider2D.md
|   |   |   |-- Cerneala.UI.Controls.Collider2D.md
|   |   |   |-- Cerneala.UI.Controls.CollisionHit2D.md
|   |   |   |-- Cerneala.UI.Controls.CollisionQuery2D.md
|   |   |   |-- Cerneala.UI.Controls.CollisionWorld2D.md
|   |   |   |-- Cerneala.UI.Controls.CollisionWorld2DDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Controls.ColorPicker.md
|   |   |   |-- Cerneala.UI.Controls.ColorSpectrum.md
|   |   |   |-- Cerneala.UI.Controls.ColorSwatch.md
|   |   |   |-- Cerneala.UI.Controls.ComboBox.md
|   |   |   |-- Cerneala.UI.Controls.ComboBoxItem.md
|   |   |   |-- Cerneala.UI.Controls.ContentControl.md
|   |   |   |-- Cerneala.UI.Controls.ContentPresenter.md
|   |   |   |-- Cerneala.UI.Controls.Control.Background.md
|   |   |   |-- Cerneala.UI.Controls.Control.BorderBrush.md
|   |   |   |-- Cerneala.UI.Controls.Control.Foreground.md
|   |   |   |-- Cerneala.UI.Controls.Control.md
|   |   |   |-- Cerneala.UI.Controls.ControlTextFont.md
|   |   |   |-- Cerneala.UI.Controls.Decorator.md
|   |   |   |-- Cerneala.UI.Controls.Image.md
|   |   |   |-- Cerneala.UI.Controls.ImageLoadingState.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvas.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvasStrokeCollectedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.IScene2DDebugNavigationGrid.md
|   |   |   |-- Cerneala.UI.Controls.IScrollInfo.md
|   |   |   |-- Cerneala.UI.Controls.ISelectableItemContainer.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemCollection.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemContainerGenerator.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemContainerRecyclePool.md
|   |   |   |-- Cerneala.UI.Controls.ItemsControl.md
|   |   |   |-- Cerneala.UI.Controls.ItemsPresenter.md
|   |   |   |-- Cerneala.UI.Controls.Label.md
|   |   |   |-- Cerneala.UI.Controls.ListBox.md
|   |   |   |-- Cerneala.UI.Controls.ListBoxItem.md
|   |   |   |-- Cerneala.UI.Controls.Menu.md
|   |   |   |-- Cerneala.UI.Controls.MenuBar.md
|   |   |   |-- Cerneala.UI.Controls.MenuItem.md
|   |   |   |-- Cerneala.UI.Controls.MoveCollisionResult2D.md
|   |   |   |-- Cerneala.UI.Controls.Overlay.md
|   |   |   |-- Cerneala.UI.Controls.OverlayPlacement.md
|   |   |   |-- Cerneala.UI.Controls.Panel.md
|   |   |   |-- Cerneala.UI.Controls.PasswordBox.md
|   |   |   |-- Cerneala.UI.Controls.PolygonCollider2D.md
|   |   |   |-- Cerneala.UI.Controls.PopupRoot.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ButtonBase.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragCompletedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragDeltaEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragStartedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.RangeBase.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.RepeatButton.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ScrollBar.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ScrollEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ScrollEventType.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Selector.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Thumb.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ToggleButton.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Track.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.TrackLayoutPanel.md
|   |   |   |-- Cerneala.UI.Controls.ProgressBar.md
|   |   |   |-- Cerneala.UI.Controls.RadioButton.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2D.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2DDrawEventHandler.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2DFrame.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2DPresentationState.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2DRedrawMode.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface2DSpriteFlip.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface3D.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface3DDrawEventHandler.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface3DFrame.md
|   |   |   |-- Cerneala.UI.Controls.RenderSurface3DRedrawMode.md
|   |   |   |-- Cerneala.UI.Controls.ResizeMode.md
|   |   |   |-- Cerneala.UI.Controls.Scene2D.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DAsset.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDebugFlags.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDebugOverlay.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDebugOverlayDiagnostics.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDiagnostic.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDiagnosticCollector.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDiagnosticSeverity.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DDocument.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DEntity.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DLevel.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DModelValidator.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DValidationOptions.md
|   |   |   |-- Cerneala.UI.Controls.Scene2DValidationResult.md
|   |   |   |-- Cerneala.UI.Controls.SceneCollisionRegion2D.md
|   |   |   |-- Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md
|   |   |   |-- Cerneala.UI.Controls.SceneItems2D.md
|   |   |   |-- Cerneala.UI.Controls.SceneJsonValue2D.md
|   |   |   |-- Cerneala.UI.Controls.SceneNode2D.md
|   |   |   |-- Cerneala.UI.Controls.SceneOrderMode.md
|   |   |   |-- Cerneala.UI.Controls.SceneSimulationContext2D.md
|   |   |   |-- Cerneala.UI.Controls.ScrollBarVisibility.md
|   |   |   |-- Cerneala.UI.Controls.ScrollChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.ScrollContentPresenter.md
|   |   |   |-- Cerneala.UI.Controls.ScrollViewer.md
|   |   |   |-- Cerneala.UI.Controls.SegmentCollider2D.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionChangeResult.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionModel_T_.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionModel.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Ellipse.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Line.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Path.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Polygon.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Polyline.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Rectangle.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Shape.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.SvgPath.md
|   |   |   |-- Cerneala.UI.Controls.Slider.md
|   |   |   |-- Cerneala.UI.Controls.Sprite2D.md
|   |   |   |-- Cerneala.UI.Controls.SpriteAnimationClip.md
|   |   |   |-- Cerneala.UI.Controls.SpriteAnimationFrame.md
|   |   |   |-- Cerneala.UI.Controls.SpriteAnimationSet.md
|   |   |   |-- Cerneala.UI.Controls.SpriteAnimationStateChangeMode.md
|   |   |   |-- Cerneala.UI.Controls.StackPanel.md
|   |   |   |-- Cerneala.UI.Controls.SvgImage.md
|   |   |   |-- Cerneala.UI.Controls.TabControl.md
|   |   |   |-- Cerneala.UI.Controls.TabItem.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplate_TControl_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplate.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplateContext_TControl_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplateContext.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplateDefinition.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ComponentTemplateInstance.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplate_TData_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplate.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateContext_TData_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateContext.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateDefinition.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateMatchContext.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateRegistry.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateBinding_T_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateBinding.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplatePartAttribute.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplatePartMap.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateRecycleKey.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateRecyclePool.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateSlotMap.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateTokenBinding_T_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateTokenBinding.md
|   |   |   |-- Cerneala.UI.Controls.TextBlock.md
|   |   |   |-- Cerneala.UI.Controls.TextBox.md
|   |   |   |-- Cerneala.UI.Controls.TextChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.TextSearch.md
|   |   |   |-- Cerneala.UI.Controls.Tile.md
|   |   |   |-- Cerneala.UI.Controls.TileCell2D.md
|   |   |   |-- Cerneala.UI.Controls.TileCellKey2D.md
|   |   |   |-- Cerneala.UI.Controls.TileChunk2D.md
|   |   |   |-- Cerneala.UI.Controls.TileColliderDescriptor2D.md
|   |   |   |-- Cerneala.UI.Controls.TileColliderShape2D.md
|   |   |   |-- Cerneala.UI.Controls.TileCoordinate2D.md
|   |   |   |-- Cerneala.UI.Controls.TileDefinition2D.md
|   |   |   |-- Cerneala.UI.Controls.TileFlip2D.md
|   |   |   |-- Cerneala.UI.Controls.TileMap2D.md
|   |   |   |-- Cerneala.UI.Controls.TileMap2DModel.md
|   |   |   |-- Cerneala.UI.Controls.TileMapBounds2D.md
|   |   |   |-- Cerneala.UI.Controls.TileMapChunkData2D.md
|   |   |   |-- Cerneala.UI.Controls.TilePromotion2D.md
|   |   |   |-- Cerneala.UI.Controls.TileSet2D.md
|   |   |   |-- Cerneala.UI.Controls.ToolTip.md
|   |   |   |-- Cerneala.UI.Controls.UserControl_TViewModel_.md
|   |   |   |-- Cerneala.UI.Controls.UserControl.md
|   |   |   |-- Cerneala.UI.Controls.Window_TViewModel_.md
|   |   |   |-- Cerneala.UI.Controls.Window.md
|   |   |   |-- Cerneala.UI.Controls.WindowClosingEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.WindowStartupLocation.md
|   |   |   |-- Cerneala.UI.Controls.WindowState.md
|   |   |   |-- Cerneala.UI.Core.CoerceValue_T_.md
|   |   |   |-- Cerneala.UI.Core.IUiPropertyOwner.md
|   |   |   |-- Cerneala.UI.Core.UiObject.md
|   |   |   |-- Cerneala.UI.Core.UiProperty_T_.md
|   |   |   |-- Cerneala.UI.Core.UiProperty.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyChangedEventArgs_T_.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyKey_T_.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyMetadata_T_.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyMutation.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyMutationObserver.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyOptions.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyRegistry.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyStore.md
|   |   |   |-- Cerneala.UI.Core.UiPropertyValueSource.md
|   |   |   |-- Cerneala.UI.Core.ValidateValue_T_.md
|   |   |   |-- Cerneala.UI.Data.Binding_T_.md
|   |   |   |-- Cerneala.UI.Data.Binding.md
|   |   |   |-- Cerneala.UI.Data.BindingMode.md
|   |   |   |-- Cerneala.UI.Data.BindingOperations.md
|   |   |   |-- Cerneala.UI.Data.BindingSubscriptionCollection.md
|   |   |   |-- Cerneala.UI.Data.CollectionView_T_.md
|   |   |   |-- Cerneala.UI.Data.FilterPredicate_T_.md
|   |   |   |-- Cerneala.UI.Data.IObservableList_T_.md
|   |   |   |-- Cerneala.UI.Data.IObservableList.md
|   |   |   |-- Cerneala.UI.Data.IValueConverter_TIn_TOut_.md
|   |   |   |-- Cerneala.UI.Data.ObservableList_T_.md
|   |   |   |-- Cerneala.UI.Data.ObservableListChangedEventArgs_T_.md
|   |   |   |-- Cerneala.UI.Data.ObservableListChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Data.ObservableListChangeKind.md
|   |   |   |-- Cerneala.UI.Data.ObservableValue_T_.md
|   |   |   |-- Cerneala.UI.Data.ObservableValueChangedEventArgs_T_.md
|   |   |   |-- Cerneala.UI.Data.PropertyAdapter_TOwner_TValue_.md
|   |   |   |-- Cerneala.UI.Data.SortDescription_T_.md
|   |   |   |-- Cerneala.UI.Data.StringPropertyPath.md
|   |   |   |-- Cerneala.UI.Data.UiPropertyBinding_T_.md
|   |   |   |-- Cerneala.UI.Detective.AspectConditionTrace.md
|   |   |   |-- Cerneala.UI.Detective.AspectDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.AspectDiagnostics.Snapshot.md
|   |   |   |-- Cerneala.UI.Detective.AspectEngineCounters.md
|   |   |   |-- Cerneala.UI.Detective.AspectResolutionStep.md
|   |   |   |-- Cerneala.UI.Detective.AspectTokenTrace.md
|   |   |   |-- Cerneala.UI.Detective.AspectTrace.md
|   |   |   |-- Cerneala.UI.Detective.AspectTraceSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.DebugAdorner.md
|   |   |   |-- Cerneala.UI.Detective.DebugOverlay.md
|   |   |   |-- Cerneala.UI.Detective.Detective.md
|   |   |   |-- Cerneala.UI.Detective.DetectiveSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.DirtyTreeDumper.md
|   |   |   |-- Cerneala.UI.Detective.ElementRenderDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.ElementTreeDumper.md
|   |   |   |-- Cerneala.UI.Detective.FrameDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.FrameDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.InputDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.InputDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.InvalidationTrace.md
|   |   |   |-- Cerneala.UI.Detective.InvalidationTraceEntry.md
|   |   |   |-- Cerneala.UI.Detective.InvalidationTraceEventKind.md
|   |   |   |-- Cerneala.UI.Detective.LayoutDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.LayoutDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.MotionDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.MotionGraphSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.MotionTrace.md
|   |   |   |-- Cerneala.UI.Detective.MotionTraceEvent.md
|   |   |   |-- Cerneala.UI.Detective.MotionTraceEventKind.md
|   |   |   |-- Cerneala.UI.Detective.PlatformDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.PrismRendererDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.RenderCacheDumper.md
|   |   |   |-- Cerneala.UI.Detective.RenderDiagnostics.md
|   |   |   |-- Cerneala.UI.Detective.ResourceDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.RootInputDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.RootRenderDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.RoutedEventTrace.md
|   |   |   |-- Cerneala.UI.Detective.RoutedEventTraceSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.RoutedEventTraceStep.md
|   |   |   |-- Cerneala.UI.Detective.TileMapDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.TimbreDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Detective.ViewportDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Elements.ElementChildRole.md
|   |   |   |-- Cerneala.UI.Elements.ElementHandlerStore.md
|   |   |   |-- Cerneala.UI.Elements.ElementIdProvider.md
|   |   |   |-- Cerneala.UI.Elements.ElementLifecycle.md
|   |   |   |-- Cerneala.UI.Elements.ElementTreeChange.md
|   |   |   |-- Cerneala.UI.Elements.ElementTreeChangeKind.md
|   |   |   |-- Cerneala.UI.Elements.ElementTreeWalker.md
|   |   |   |-- Cerneala.UI.Elements.IElementChildHost.md
|   |   |   |-- Cerneala.UI.Elements.IElementHost.md
|   |   |   |-- Cerneala.UI.Elements.InheritedPropertyPropagator.md
|   |   |   |-- Cerneala.UI.Elements.SizeChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Elements.UIElement.md
|   |   |   |-- Cerneala.UI.Elements.UIElementCollection.md
|   |   |   |-- Cerneala.UI.Elements.UIElementVisibility.md
|   |   |   |-- Cerneala.UI.Elements.UIRoot.md
|   |   |   |-- Cerneala.UI.Hosting.DrawingContentServices.md
|   |   |   |-- Cerneala.UI.Hosting.IUiBackend.md
|   |   |   |-- Cerneala.UI.Hosting.IUiClock.md
|   |   |   |-- Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.md
|   |   |   |-- Cerneala.UI.Hosting.UiCoordinateMapper.md
|   |   |   |-- Cerneala.UI.Hosting.UiFrame.md
|   |   |   |-- Cerneala.UI.Hosting.UiHost.md
|   |   |   |-- Cerneala.UI.Hosting.UiHostOptions.md
|   |   |   |-- Cerneala.UI.Hosting.UiViewport.md
|   |   |   |-- Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute.md
|   |   |   |-- Cerneala.UI.Hosting.Windowing.GeneratedWindowApplication.md
|   |   |   |-- Cerneala.UI.Hosting.Windowing.GeneratedWindowStartupDescriptor.md
|   |   |   |-- Cerneala.UI.Ink.Stroke.md
|   |   |   |-- Cerneala.UI.Ink.StrokeCollection.md
|   |   |   |-- Cerneala.UI.Ink.StrokeCollectionChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Ink.StrokeCollectionChangeKind.md
|   |   |   |-- Cerneala.UI.Input.ActionCommand.md
|   |   |   |-- Cerneala.UI.Input.CanExecuteRoutedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.ClickTracker.md
|   |   |   |-- Cerneala.UI.Input.CommandBinding.md
|   |   |   |-- Cerneala.UI.Input.CommandBindingCollection.md
|   |   |   |-- Cerneala.UI.Input.CommandEvents.md
|   |   |   |-- Cerneala.UI.Input.CommandRouter.md
|   |   |   |-- Cerneala.UI.Input.Cursor.md
|   |   |   |-- Cerneala.UI.Input.CursorService.md
|   |   |   |-- Cerneala.UI.Input.DataTransfer.md
|   |   |   |-- Cerneala.UI.Input.DragDropController.md
|   |   |   |-- Cerneala.UI.Input.DragEventArgs.md
|   |   |   |-- Cerneala.UI.Input.ElementInputBridge.md
|   |   |   |-- Cerneala.UI.Input.ElementInputCache.md
|   |   |   |-- Cerneala.UI.Input.ElementInputRouteBuilder.md
|   |   |   |-- Cerneala.UI.Input.ElementInputRouteMap.md
|   |   |   |-- Cerneala.UI.Input.ElementRoutedEventStore.md
|   |   |   |-- Cerneala.UI.Input.ExecutedRoutedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.FocusManager.md
|   |   |   |-- Cerneala.UI.Input.FocusPolicy.md
|   |   |   |-- Cerneala.UI.Input.FocusScope.md
|   |   |   |-- Cerneala.UI.Input.GestureEvent.md
|   |   |   |-- Cerneala.UI.Input.GestureKind.md
|   |   |   |-- Cerneala.UI.Input.GestureRecognizer.md
|   |   |   |-- Cerneala.UI.Input.GestureSample.md
|   |   |   |-- Cerneala.UI.Input.HitTestFilter.md
|   |   |   |-- Cerneala.UI.Input.HitTestFilterBehavior.md
|   |   |   |-- Cerneala.UI.Input.HitTestResult.md
|   |   |   |-- Cerneala.UI.Input.HitTestService.md
|   |   |   |-- Cerneala.UI.Input.HoverTracker.md
|   |   |   |-- Cerneala.UI.Input.ICommand.md
|   |   |   |-- Cerneala.UI.Input.ICommandStateSource.md
|   |   |   |-- Cerneala.UI.Input.IInputActivatable.md
|   |   |   |-- Cerneala.UI.Input.IInputCommandSource.md
|   |   |   |-- Cerneala.UI.Input.IInputPressable.md
|   |   |   |-- Cerneala.UI.Input.IInputSource.md
|   |   |   |-- Cerneala.UI.Input.InputBinding.md
|   |   |   |-- Cerneala.UI.Input.InputBindingCollection.md
|   |   |   |-- Cerneala.UI.Input.InputButtonState.md
|   |   |   |-- Cerneala.UI.Input.InputEvents.md
|   |   |   |-- Cerneala.UI.Input.InputFrame.KeyboardFrame.md
|   |   |   |-- Cerneala.UI.Input.InputFrame.md
|   |   |   |-- Cerneala.UI.Input.InputFrame.PointerFrame.md
|   |   |   |-- Cerneala.UI.Input.InputGesture.md
|   |   |   |-- Cerneala.UI.Input.InputKey.md
|   |   |   |-- Cerneala.UI.Input.InputMouseButton.md
|   |   |   |-- Cerneala.UI.Input.IObservableCommand.md
|   |   |   |-- Cerneala.UI.Input.IPointerDragSource.md
|   |   |   |-- Cerneala.UI.Input.KeyBinding.md
|   |   |   |-- Cerneala.UI.Input.KeyboardFocusChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.KeyboardNavigation.md
|   |   |   |-- Cerneala.UI.Input.KeyboardSnapshot.md
|   |   |   |-- Cerneala.UI.Input.KeyEventArgs.md
|   |   |   |-- Cerneala.UI.Input.KeyGesture.md
|   |   |   |-- Cerneala.UI.Input.KeyModifiers.md
|   |   |   |-- Cerneala.UI.Input.ManipulationDelta.md
|   |   |   |-- Cerneala.UI.Input.ManipulationPoint.md
|   |   |   |-- Cerneala.UI.Input.ManipulationProcessor.md
|   |   |   |-- Cerneala.UI.Input.MouseButtonEventArgs.md
|   |   |   |-- Cerneala.UI.Input.MouseEventArgs.md
|   |   |   |-- Cerneala.UI.Input.MouseWheelEventArgs.md
|   |   |   |-- Cerneala.UI.Input.PointerCaptureManager.md
|   |   |   |-- Cerneala.UI.Input.PointerSnapshot.md
|   |   |   |-- Cerneala.UI.Input.PressedStateTracker.md
|   |   |   |-- Cerneala.UI.Input.RoutedCommand.md
|   |   |   |-- Cerneala.UI.Input.RoutedCommandContext.md
|   |   |   |-- Cerneala.UI.Input.RoutedEvent.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventHandler.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventRegistry.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventRouter.md
|   |   |   |-- Cerneala.UI.Input.RoutedPropertyChangedEventArgs_T_.md
|   |   |   |-- Cerneala.UI.Input.RoutingStrategy.md
|   |   |   |-- Cerneala.UI.Input.StylusEventArgs.md
|   |   |   |-- Cerneala.UI.Input.StylusInputAction.md
|   |   |   |-- Cerneala.UI.Input.StylusInputBridge.md
|   |   |   |-- Cerneala.UI.Input.StylusInputFrame.md
|   |   |   |-- Cerneala.UI.Input.StylusInputPoint.md
|   |   |   |-- Cerneala.UI.Input.TextCompositionEventArgs.md
|   |   |   |-- Cerneala.UI.Input.TextInputBridge.md
|   |   |   |-- Cerneala.UI.Input.TextInputSnapshotEvent.md
|   |   |   |-- Cerneala.UI.Input.TouchEventArgs.md
|   |   |   |-- Cerneala.UI.Input.TouchInputAction.md
|   |   |   |-- Cerneala.UI.Input.TouchInputBridge.md
|   |   |   |-- Cerneala.UI.Input.TouchInputFrame.md
|   |   |   |-- Cerneala.UI.Input.TouchInputPoint.md
|   |   |   |-- Cerneala.UI.Input.UiElementId.md
|   |   |   |-- Cerneala.UI.Input.UiInputElement.md
|   |   |   |-- Cerneala.UI.Input.UiInputTree.md
|   |   |   |-- Cerneala.UI.Invalidation.AspectQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.CommandStateQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.DirtyPropagation.md
|   |   |   |-- Cerneala.UI.Invalidation.DirtyState.md
|   |   |   |-- Cerneala.UI.Invalidation.FrameBudget.md
|   |   |   |-- Cerneala.UI.Invalidation.FramePhase.md
|   |   |   |-- Cerneala.UI.Invalidation.FramePhaseProcessors.md
|   |   |   |-- Cerneala.UI.Invalidation.FrameStats.md
|   |   |   |-- Cerneala.UI.Invalidation.HitTestQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.IInvalidationSink.md
|   |   |   |-- Cerneala.UI.Invalidation.InheritedPropertyQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.InvalidationFlags.md
|   |   |   |-- Cerneala.UI.Invalidation.InvalidationRequest.md
|   |   |   |-- Cerneala.UI.Invalidation.LayoutQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.RenderQueue.md
|   |   |   |-- Cerneala.UI.Invalidation.UiFrameScheduler.md
|   |   |   |-- Cerneala.UI.Layout.ArrangeContext.md
|   |   |   |-- Cerneala.UI.Layout.HorizontalAlignment.md
|   |   |   |-- Cerneala.UI.Layout.ILayoutElement.md
|   |   |   |-- Cerneala.UI.Layout.LayoutBoundary.md
|   |   |   |-- Cerneala.UI.Layout.LayoutManager.md
|   |   |   |-- Cerneala.UI.Layout.LayoutPoint.md
|   |   |   |-- Cerneala.UI.Layout.LayoutRect.md
|   |   |   |-- Cerneala.UI.Layout.LayoutResult.md
|   |   |   |-- Cerneala.UI.Layout.LayoutRounding.md
|   |   |   |-- Cerneala.UI.Layout.LayoutSize.md
|   |   |   |-- Cerneala.UI.Layout.MeasureContext.md
|   |   |   |-- Cerneala.UI.Layout.Orientation.md
|   |   |   |-- Cerneala.UI.Layout.Panels.Canvas.md
|   |   |   |-- Cerneala.UI.Layout.Panels.ColumnDefinition.md
|   |   |   |-- Cerneala.UI.Layout.Panels.Grid.md
|   |   |   |-- Cerneala.UI.Layout.Panels.GridDefinitionCollection_TDefinition_.md
|   |   |   |-- Cerneala.UI.Layout.Panels.GridLength.md
|   |   |   |-- Cerneala.UI.Layout.Panels.GridUnitType.md
|   |   |   |-- Cerneala.UI.Layout.Panels.Panel.md
|   |   |   |-- Cerneala.UI.Layout.Panels.RowDefinition.md
|   |   |   |-- Cerneala.UI.Layout.Panels.StackPanel.md
|   |   |   |-- Cerneala.UI.Layout.Panels.VirtualizingStackPanel.md
|   |   |   |-- Cerneala.UI.Layout.Thickness.md
|   |   |   |-- Cerneala.UI.Layout.VerticalAlignment.md
|   |   |   |-- Cerneala.UI.Layout.Virtualization.IItemsVirtualizingPanel.md
|   |   |   |-- Cerneala.UI.Layout.Virtualization.ItemsVirtualizationViewport.md
|   |   |   |-- Cerneala.UI.Layout.Virtualization.RealizationWindow.md
|   |   |   |-- Cerneala.UI.Layout.Virtualization.VirtualizationContext.md
|   |   |   |-- Cerneala.UI.Layout.Visibility.md
|   |   |   |-- Cerneala.UI.Markup.BrushMarkupReader.md
|   |   |   |-- Cerneala.UI.Markup.ContentPropertyAttribute.md
|   |   |   |-- Cerneala.UI.Markup.DesignTimeOnlyAttribute.md
|   |   |   |-- Cerneala.UI.Markup.GeneratedMarkup.md
|   |   |   |-- Cerneala.UI.Markup.GeneratedUiFactory.md
|   |   |   |-- Cerneala.UI.Markup.MarkupConditionalContent.md
|   |   |   |-- Cerneala.UI.Markup.MarkupConditionalValue.md
|   |   |   |-- Cerneala.UI.Markup.MarkupConditionRule.md
|   |   |   |-- Cerneala.UI.Markup.MarkupDataPathSegment.md
|   |   |   |-- Cerneala.UI.Markup.MarkupDiagnostic.md
|   |   |   |-- Cerneala.UI.Markup.MarkupDiagnosticSeverity.md
|   |   |   |-- Cerneala.UI.Markup.MarkupLoadOptions.md
|   |   |   |-- Cerneala.UI.Markup.MarkupMotionExecution.md
|   |   |   |-- Cerneala.UI.Markup.MarkupObservation.md
|   |   |   |-- Cerneala.UI.Markup.MarkupResult_T_.md
|   |   |   |-- Cerneala.UI.Markup.MarkupValueConstraint.md
|   |   |   |-- Cerneala.UI.Markup.MarkupValueConstraintAttribute.md
|   |   |   |-- Cerneala.UI.Markup.UiFactory.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupAttribute.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupChildContent.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupContent.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupDocument.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupElementRegistration.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupNode.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupPropertyRegistration.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupReader.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupSchema.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupTextContent.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupTypeRegistry.md
|   |   |   |-- Cerneala.UI.Markup.UiMarkupWriter.md
|   |   |   |-- Cerneala.UI.Media.BitmapImage.md
|   |   |   |-- Cerneala.UI.Media.Brush.md
|   |   |   |-- Cerneala.UI.Media.DrawingBrush.md
|   |   |   |-- Cerneala.UI.Media.EllipseGeometry.md
|   |   |   |-- Cerneala.UI.Media.Geometry.md
|   |   |   |-- Cerneala.UI.Media.GradientStop.md
|   |   |   |-- Cerneala.UI.Media.ImageBrush.md
|   |   |   |-- Cerneala.UI.Media.ImageSource.md
|   |   |   |-- Cerneala.UI.Media.LinearGradientBrush.md
|   |   |   |-- Cerneala.UI.Media.Matrix3x2.md
|   |   |   |-- Cerneala.UI.Media.PathGeometry.md
|   |   |   |-- Cerneala.UI.Media.Pen.md
|   |   |   |-- Cerneala.UI.Media.RadialGradientBrush.md
|   |   |   |-- Cerneala.UI.Media.RectangleGeometry.md
|   |   |   |-- Cerneala.UI.Media.RenderTargetImage.md
|   |   |   |-- Cerneala.UI.Media.SolidColorBrush.md
|   |   |   |-- Cerneala.UI.Media.SvgGeometry.md
|   |   |   |-- Cerneala.UI.Media.TileBrush.md
|   |   |   |-- Cerneala.UI.Media.Transform.md
|   |   |   |-- Cerneala.UI.Media.VisualBrush.md
|   |   |   |-- Cerneala.UI.Motion.Core.DerivedMotionValue_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.IMotionClock.md
|   |   |   |-- Cerneala.UI.Motion.Core.IReducedMotionSource.md
|   |   |   |-- Cerneala.UI.Motion.Core.ManualMotionTimeline.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionCancelBehavior.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionChannel.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionCompletedEventArgs.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionCompletionSource.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionCompletionState.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionComposition.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionConflictResolver.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionFrame.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionFrameCoordinator.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionFramePhase.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionFrameReason.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionFrameResult.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionGraph.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionGroup.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionGroupHandle.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionHandle.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionNode.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionNodeTickResult.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionPriority.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionSequence.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionStagger.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionStartOptions.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionSystem.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionTimeline.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionTimelineRegistry.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValueChanged_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.ReducedMotionMode.md
|   |   |   |-- Cerneala.UI.Motion.Core.ReducedMotionPolicy.md
|   |   |   |-- Cerneala.UI.Motion.Core.SystemMotionClock.md
|   |   |   |-- Cerneala.UI.Motion.Input.DragMotionController.md
|   |   |   |-- Cerneala.UI.Motion.Input.GestureMotionController.md
|   |   |   |-- Cerneala.UI.Motion.Input.MotionRange.md
|   |   |   |-- Cerneala.UI.Motion.Input.PointerMotionState.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollMotionBinding_T_.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollTimeline.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollTimelineProgress.md
|   |   |   |-- Cerneala.UI.Motion.Input.VelocityTracker.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.BrushMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.ColorMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.DoubleMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.DrawPointMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.DrawRectMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.DrawSizeMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.FloatMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.IValueMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.ThicknessMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.TransformComponents.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.TransformInterpolationMode.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.TransformMixer.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.ValueMixer_T_.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.ValueMixerRegistry.md
|   |   |   |-- Cerneala.UI.Motion.Interpolation.Vector4Mixer.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionBinding.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionCoordinator.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionId.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionOptions.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutSnapshot.md
|   |   |   |-- Cerneala.UI.Motion.MotionAnimationBuilder_T_.md
|   |   |   |-- Cerneala.UI.Motion.MotionDefaults.md
|   |   |   |-- Cerneala.UI.Motion.MotionElementFacade.md
|   |   |   |-- Cerneala.UI.Motion.MotionExtensions.md
|   |   |   |-- Cerneala.UI.Motion.MotionProperty_TTarget_TValue_.md
|   |   |   |-- Cerneala.UI.Motion.MotionProperty.md
|   |   |   |-- Cerneala.UI.Motion.MotionPropertyShortcut_T_.md
|   |   |   |-- Cerneala.UI.Motion.MotionStateBuilder.md
|   |   |   |-- Cerneala.UI.Motion.MotionStateTargetBuilder.md
|   |   |   |-- Cerneala.UI.Motion.ObjectMotionAnimationBuilder_TTarget_TValue_.md
|   |   |   |-- Cerneala.UI.Motion.ObjectMotionFacade_TTarget_.md
|   |   |   |-- Cerneala.UI.Motion.ObjectMotionFacade.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceCoordinator.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceOptions.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceState.md
|   |   |   |-- Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionClearBehavior.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyBinding_T_.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyBinding.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyFlushResult.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyInvalidationClassifier.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyKey.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyOptions.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStartOptions.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStore.md
|   |   |   |-- Cerneala.UI.Motion.Specs.CubicBezierEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.DecaySpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Easings.md
|   |   |   |-- Cerneala.UI.Motion.Specs.FillMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.IEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.KeyframesSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Motion.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionCompletion.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionKeyframe_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSampler_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpec.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpecContext.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionVelocity_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.PingPongSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RepeatSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RetargetMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.SpringSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.SpringVelocityMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.StepEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.StepPosition.md
|   |   |   |-- Cerneala.UI.Motion.Specs.TweenSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionStateRule.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionTokens.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionVisualStateController.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionVisualStateSnapshot.md
|   |   |   |-- Cerneala.UI.Motion.States.ThemeMotionTokens.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransaction.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransactionContext.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransactionOptions.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransactionScope.md
|   |   |   |-- Cerneala.UI.Platform.CursorShape.md
|   |   |   |-- Cerneala.UI.Platform.FileDialogFilter.md
|   |   |   |-- Cerneala.UI.Platform.FileDialogOptions.md
|   |   |   |-- Cerneala.UI.Platform.IAccessibilityPlatform.md
|   |   |   |-- Cerneala.UI.Platform.IClipboard.md
|   |   |   |-- Cerneala.UI.Platform.ICursorService.md
|   |   |   |-- Cerneala.UI.Platform.IDpiProvider.md
|   |   |   |-- Cerneala.UI.Platform.IFileDialogService.md
|   |   |   |-- Cerneala.UI.Platform.IPlatformServices.md
|   |   |   |-- Cerneala.UI.Platform.ITextInputPlatform.md
|   |   |   |-- Cerneala.UI.Platform.PlatformServices.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismClipDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismColorMatrixResource.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismCurvePoint.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismCurvesResource.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismFilterDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismGradientMapPoint.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismGradientMapResource.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismGroupDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLayerDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLensFlareGhost.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLensFlarePolynomialInput.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLensFlarePolynomialRegion.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLensProfileJson.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLensProfileResource.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLight.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLightingResource.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismLightKind.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismMaskChannel.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismMaskDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismNodeDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismNodeId.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismResourceId.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismSourceSpan.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismSparsePolynomial.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismSparsePolynomialTerm.md
|   |   |   |-- Cerneala.UI.Prism.Definitions.PrismStyleDefinition.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismBlendChannels.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismBlendIfChannel.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismBlendRange.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismClipState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismFilterState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismGroupState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismInstance.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismKnockout.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismLayerState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismMaskState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismNodeState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismStructuralVersion.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismStyleState.md
|   |   |   |-- Cerneala.UI.Prism.Runtime.PrismValueVersion.md
|   |   |   |-- Cerneala.UI.Relay.UiRelay.md
|   |   |   |-- Cerneala.UI.Relay.UiRelayOptions.md
|   |   |   |-- Cerneala.UI.Rendering.ClipNode.md
|   |   |   |-- Cerneala.UI.Rendering.DrawCommandListBuilder.md
|   |   |   |-- Cerneala.UI.Rendering.DrawCommandListPool.md
|   |   |   |-- Cerneala.UI.Rendering.ElementRenderCache.md
|   |   |   |-- Cerneala.UI.Rendering.IRenderableElement.md
|   |   |   |-- Cerneala.UI.Rendering.ITimeSensitiveRenderElement.md
|   |   |   |-- Cerneala.UI.Rendering.RenderContext.md
|   |   |   |-- Cerneala.UI.Rendering.RenderCounters.md
|   |   |   |-- Cerneala.UI.Rendering.RenderDependency.md
|   |   |   |-- Cerneala.UI.Rendering.RenderLayer.md
|   |   |   |-- Cerneala.UI.Rendering.RenderQueueProcessor.md
|   |   |   |-- Cerneala.UI.Rendering.RetainedRenderCache.md
|   |   |   |-- Cerneala.UI.Rendering.RetainedRenderer.md
|   |   |   |-- Cerneala.UI.Rendering.TimeSensitiveRenderInvalidator.md
|   |   |   |-- Cerneala.UI.Resources.FontResource.md
|   |   |   |-- Cerneala.UI.Resources.IAsyncImageLoader.md
|   |   |   |-- Cerneala.UI.Resources.IImageLoader.md
|   |   |   |-- Cerneala.UI.Resources.ImageReference.md
|   |   |   |-- Cerneala.UI.Resources.ImageResource.md
|   |   |   |-- Cerneala.UI.Resources.ImageResourceCache.md
|   |   |   |-- Cerneala.UI.Resources.ImageResourceLease.md
|   |   |   |-- Cerneala.UI.Resources.IObservableResourceProvider.md
|   |   |   |-- Cerneala.UI.Resources.IResourceProvider.md
|   |   |   |-- Cerneala.UI.Resources.ResourceChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyChange.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDictionary.md
|   |   |   |-- Cerneala.UI.Resources.ResourceId_T_.md
|   |   |   |-- Cerneala.UI.Resources.ResourceStore.md
|   |   |   |-- Cerneala.UI.Servo.Servo.md
|   |   |   |-- Cerneala.UI.Servo.ServoCondition.md
|   |   |   |-- Cerneala.UI.Servo.ServoElement.md
|   |   |   |-- Cerneala.UI.Servo.ServoException.md
|   |   |   |-- Cerneala.UI.Servo.ServoModifiers.md
|   |   |   |-- Cerneala.UI.Servo.ServoOptions.md
|   |   |   |-- Cerneala.UI.Servo.ServoPoint.md
|   |   |   |-- Cerneala.UI.Servo.ServoTarget.md
|   |   |   |-- Cerneala.UI.Servo.ServoTargetAmbiguousException.md
|   |   |   |-- Cerneala.UI.Servo.ServoTargetNotActionableException.md
|   |   |   |-- Cerneala.UI.Servo.ServoTargetNotFoundException.md
|   |   |   |-- Cerneala.UI.Servo.ServoTimeoutException.md
|   |   |   |-- Cerneala.UI.Text.BidiTextRun.md
|   |   |   |-- Cerneala.UI.Text.BidiTextService.md
|   |   |   |-- Cerneala.UI.Text.FontResolver.md
|   |   |   |-- Cerneala.UI.Text.LineBreakService.md
|   |   |   |-- Cerneala.UI.Text.ResolvedTextFont.md
|   |   |   |-- Cerneala.UI.Text.TextAspect.md
|   |   |   |-- Cerneala.UI.Text.TextCaret.md
|   |   |   |-- Cerneala.UI.Text.TextCaretLayout.md
|   |   |   |-- Cerneala.UI.Text.TextDirection.md
|   |   |   |-- Cerneala.UI.Text.TextLayoutCache.md
|   |   |   |-- Cerneala.UI.Text.TextLayoutKey.md
|   |   |   |-- Cerneala.UI.Text.TextLine.md
|   |   |   |-- Cerneala.UI.Text.TextMeasurer.md
|   |   |   |-- Cerneala.UI.Text.TextMeasureResult.md
|   |   |   |-- Cerneala.UI.Text.TextRenderer.md
|   |   |   |-- Cerneala.UI.Text.TextSelection.md
|   |   |   |-- Cerneala.UI.Text.TextTrimming.md
|   |   |   |-- Cerneala.UI.Text.TextWrapping.md
|   |   |   |-- Cerneala.UI.Theming.DefaultTheme.md
|   |   |   |-- Cerneala.UI.Theming.Theme.md
|   |   |   |-- Cerneala.UI.Theming.ThemeChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Theming.ThemeKey_T_.md
|   |   |   |-- Cerneala.UI.Theming.ThemePalette.md
|   |   |   |-- Cerneala.UI.Theming.ThemeProvider.md
|   |   |   |-- Cerneala.UI.Theming.ThemeResource_T_.md
|   |   |   |-- Cerneala.UI.Timbre.TimbreMotionAnimationBuilder.md
|   |   |   |-- Cerneala.UI.Timbre.TimbreMotionFacade.md
|   |   |   +-- Cerneala.VisualStudio.CernealaPackage.md
|   |   +-- manifest.json
|   |-- .nojekyll
|   |-- about.css
|   |-- about.html
|   |-- benchmarks.css
|   |-- benchmarks.html
|   |-- benchmarks.js
|   |-- contributors.css
|   |-- contributors.html
|   |-- contributors.js
|   |-- documentation.css
|   |-- documentation.html
|   |-- index.html
|   |-- readability.css
|   |-- roadmap.css
|   |-- roadmap.html
|   |-- roadmap.js
|   |-- wpf-compendium.css
|   |-- wpf-compendium.html
|   +-- wpf-compendium.js
|-- Drawing/
|   |-- Paths/
|   |   |-- DrawEllipseCoverage.cs
|   |   |-- DrawEllipseRowTessellator.cs
|   |   |-- DrawPathFlattener.cs
|   |   |-- DrawPathMeshBuilder.cs
|   |   |-- DrawStrokeMeshBuilder.cs
|   |   |-- DrawStrokeTessellator.cs
|   |   |-- DrawTriangleMesh.cs
|   |   +-- SvgPathFlattener.cs
|   |-- Prism/
|   |   |-- Blend/
|   |   |   |-- Assets/
|   |   |   |   |-- dissolve-fastnoise-ranks.bin
|   |   |   |   +-- README.md
|   |   |   |-- PrismAdvancedBlendingStyle.cs
|   |   |   |-- PrismBlendIfStyle.cs
|   |   |   |-- PrismBlendMath.cs
|   |   |   |-- PrismColorBlend.cs
|   |   |   |-- PrismColorBurnBlend.cs
|   |   |   |-- PrismColorDodgeBlend.cs
|   |   |   |-- PrismDarkenBlend.cs
|   |   |   |-- PrismDarkerColorBlend.cs
|   |   |   |-- PrismDifferenceBlend.cs
|   |   |   |-- PrismDissolveBlend.cs
|   |   |   |-- PrismDivideBlend.cs
|   |   |   |-- PrismExclusionBlend.cs
|   |   |   |-- PrismHardLightBlend.cs
|   |   |   |-- PrismHardMixBlend.cs
|   |   |   |-- PrismHueBlend.cs
|   |   |   |-- PrismLightenBlend.cs
|   |   |   |-- PrismLighterColorBlend.cs
|   |   |   |-- PrismLinearBurnBlend.cs
|   |   |   |-- PrismLinearDodgeBlend.cs
|   |   |   |-- PrismLinearLightBlend.cs
|   |   |   |-- PrismLuminosityBlend.cs
|   |   |   |-- PrismMultiplyBlend.cs
|   |   |   |-- PrismNormalBlend.cs
|   |   |   |-- PrismOverlayBlend.cs
|   |   |   |-- PrismPassThroughBlend.cs
|   |   |   |-- PrismPinLightBlend.cs
|   |   |   |-- PrismSaturationBlend.cs
|   |   |   |-- PrismScreenBlend.cs
|   |   |   |-- PrismSoftLightBlend.cs
|   |   |   |-- PrismSubtractBlend.cs
|   |   |   +-- PrismVividLightBlend.cs
|   |   |-- Catalog/
|   |   |   |-- PrismCatalog.cs
|   |   |   +-- PrismFallbackPolicy.cs
|   |   |-- Color/
|   |   |   |-- PrismColorPipeline.cs
|   |   |   |-- PrismDisplayP3Style.cs
|   |   |   |-- PrismLinearDisplayP3Style.cs
|   |   |   |-- PrismLinearSrgbStyle.cs
|   |   |   |-- PrismOklab.cs
|   |   |   |-- PrismScRgbStyle.cs
|   |   |   +-- PrismSrgbStyle.cs
|   |   |-- Execution/
|   |   |   +-- PrismExecutionDiagnostics.cs
|   |   |-- Filters/
|   |   |   |-- Assets/
|   |   |   |   +-- bluenoise.bin
|   |   |   |-- PrismAccentedEdgesFilter.cs
|   |   |   |-- PrismAdaptiveWideAngleFilter.cs
|   |   |   |-- PrismAddNoiseFilter.cs
|   |   |   |-- PrismAdjustmentMath.cs
|   |   |   |-- PrismAdjustmentPlanner.cs
|   |   |   |-- PrismAngledStrokesFilter.cs
|   |   |   |-- PrismArbitraryFlatMorphology.cs
|   |   |   |-- PrismAverageFilter.cs
|   |   |   |-- PrismBasReliefFilter.cs
|   |   |   |-- PrismBlackWhiteFilter.cs
|   |   |   |-- PrismBlurFilter.cs
|   |   |   |-- PrismBlurMoreFilter.cs
|   |   |   |-- PrismBoxBlurFilter.cs
|   |   |   |-- PrismBrightnessContrastFilter.cs
|   |   |   |-- PrismCatalogArtisticMath.cs
|   |   |   |-- PrismCatalogColorMath.cs
|   |   |   |-- PrismCatalogFilterMath.cs
|   |   |   |-- PrismCatalogFilterPlanner.cs
|   |   |   |-- PrismCatalogFilterPlanner.Options.cs
|   |   |   |-- PrismCatalogFilterPlanner.Passes.cs
|   |   |   |-- PrismCatalogFilterPlanner.Settings.cs
|   |   |   |-- PrismCatalogGeometryMath.cs
|   |   |   |-- PrismCatalogInkMath.cs
|   |   |   |-- PrismCatalogPainterlyMath.cs
|   |   |   |-- PrismCatalogProceduralMath.cs
|   |   |   |-- PrismCatalogQuantizationMath.cs
|   |   |   |-- PrismCatalogReliefMath.cs
|   |   |   |-- PrismCatalogTextureMath.cs
|   |   |   |-- PrismChalkCharcoalFilter.cs
|   |   |   |-- PrismChannelMixerFilter.cs
|   |   |   |-- PrismCharcoalFilter.cs
|   |   |   |-- PrismChromaticAberrationFilter.cs
|   |   |   |-- PrismChromeFilter.cs
|   |   |   |-- PrismCloudsFilter.cs
|   |   |   |-- PrismColorBalanceFilter.cs
|   |   |   |-- PrismColoredPencilFilter.cs
|   |   |   |-- PrismColorFilter.cs
|   |   |   |-- PrismColorHalftoneFilter.cs
|   |   |   |-- PrismColorLookupFilter.cs
|   |   |   |-- PrismColorMatrixFilter.cs
|   |   |   |-- PrismConteCrayonFilter.cs
|   |   |   |-- PrismCraquelureFilter.cs
|   |   |   |-- PrismCrosshatchFilter.cs
|   |   |   |-- PrismCrystallizeFilter.cs
|   |   |   |-- PrismCurveLut.cs
|   |   |   |-- PrismCurvesFilter.cs
|   |   |   |-- PrismCustomConvolutionFilter.cs
|   |   |   |-- PrismCutoutFilter.cs
|   |   |   |-- PrismDarkStrokesFilter.cs
|   |   |   |-- PrismDeinterlaceFilter.cs
|   |   |   |-- PrismDespeckleFilter.cs
|   |   |   |-- PrismDifferenceCloudsFilter.cs
|   |   |   |-- PrismDiffuseFilter.cs
|   |   |   |-- PrismDiffuseGlowFilter.cs
|   |   |   |-- PrismDisplaceFilter.cs
|   |   |   |-- PrismDryBrushFilter.cs
|   |   |   |-- PrismDustScratchesFilter.cs
|   |   |   |-- PrismEmbossFilter.cs
|   |   |   |-- PrismExposureFilter.cs
|   |   |   |-- PrismExtrudeFilter.cs
|   |   |   |-- PrismFacetFilter.cs
|   |   |   |-- PrismFibersFilter.cs
|   |   |   |-- PrismFibersNoise.cs
|   |   |   |-- PrismFieldBlurFilter.cs
|   |   |   |-- PrismFilmGrainFilter.cs
|   |   |   |-- PrismFilterConformanceGallery.cs
|   |   |   |-- PrismFilterParameterReader.cs
|   |   |   |-- PrismFindEdgesFilter.cs
|   |   |   |-- PrismFragmentFilter.cs
|   |   |   |-- PrismFrescoFilter.cs
|   |   |   |-- PrismGaussianBlurFilter.cs
|   |   |   |-- PrismGlassFilter.cs
|   |   |   |-- PrismGlowingEdgesFilter.cs
|   |   |   |-- PrismGradientMapFilter.cs
|   |   |   |-- PrismGradientMapLut.cs
|   |   |   |-- PrismGrainFilter.cs
|   |   |   |-- PrismGraphicPenFilter.cs
|   |   |   |-- PrismHaldLut.cs
|   |   |   |-- PrismHalftonePatternFilter.cs
|   |   |   |-- PrismHighPassFilter.cs
|   |   |   |-- PrismHueSaturationFilter.cs
|   |   |   |-- PrismIncrementalVoronoiSet.cs
|   |   |   |-- PrismInkOutlinesFilter.cs
|   |   |   |-- PrismInvertFilter.cs
|   |   |   |-- PrismIrisBlurFilter.cs
|   |   |   |-- PrismLensBlurFilter.cs
|   |   |   |-- PrismLensCorrectionFilter.cs
|   |   |   |-- PrismLensFlareFilter.cs
|   |   |   |-- PrismLensFlareRenderer.cs
|   |   |   |-- PrismLensProfileFitter.cs
|   |   |   |-- PrismLevelsAnalysis.cs
|   |   |   |-- PrismLevelsFilter.cs
|   |   |   |-- PrismLightingEffectsFilter.cs
|   |   |   |-- PrismLiquifyFilter.cs
|   |   |   |-- PrismMaximumFilter.cs
|   |   |   |-- PrismMedianFilter.cs
|   |   |   |-- PrismMezzotintFilter.cs
|   |   |   |-- PrismMezzotintThreshold.cs
|   |   |   |-- PrismMinimumFilter.cs
|   |   |   |-- PrismMosaicFilter.cs
|   |   |   |-- PrismMosaicTilesFilter.cs
|   |   |   |-- PrismMotionBlurFilter.cs
|   |   |   |-- PrismNeighborhoodMath.Blur.cs
|   |   |   |-- PrismNeighborhoodMath.cs
|   |   |   |-- PrismNeighborhoodMath.Noise.cs
|   |   |   |-- PrismNeighborhoodMath.Sharpen.cs
|   |   |   |-- PrismNeighborhoodPlanner.cs
|   |   |   |-- PrismNeonGlowFilter.cs
|   |   |   |-- PrismNotePaperFilter.cs
|   |   |   |-- PrismNtscColorsFilter.cs
|   |   |   |-- PrismOceanRippleFilter.cs
|   |   |   |-- PrismOffsetFilter.cs
|   |   |   |-- PrismOilPaintFilter.cs
|   |   |   |-- PrismOkhsl.cs
|   |   |   |-- PrismPaintDaubsFilter.cs
|   |   |   |-- PrismPaletteKnifeFilter.cs
|   |   |   |-- PrismPatchworkFilter.cs
|   |   |   |-- PrismPathBlurFilter.cs
|   |   |   |-- PrismPhotocopyFilter.cs
|   |   |   |-- PrismPhotoFilterFilter.cs
|   |   |   |-- PrismPinchFilter.cs
|   |   |   |-- PrismPlasterFilter.cs
|   |   |   |-- PrismPlasticWrapFilter.cs
|   |   |   |-- PrismPointillizeFilter.cs
|   |   |   |-- PrismPolarCoordinatesFilter.cs
|   |   |   |-- PrismPosterEdgesFilter.cs
|   |   |   |-- PrismPosterizeFilter.cs
|   |   |   |-- PrismRadialBlurFilter.cs
|   |   |   |-- PrismRecursiveWangBlueNoise.cs
|   |   |   |-- PrismReduceNoiseFilter.cs
|   |   |   |-- PrismResamplingMath.CoordinateMapping.cs
|   |   |   |-- PrismResamplingMath.cs
|   |   |   |-- PrismResamplingMath.FelineSampling.cs
|   |   |   |-- PrismResamplingMath.Glow.cs
|   |   |   |-- PrismResamplingMath.LiquifySampling.cs
|   |   |   |-- PrismResamplingMath.PolarSampling.cs
|   |   |   |-- PrismResamplingMath.Sampling.cs
|   |   |   |-- PrismResamplingMath.Wave.cs
|   |   |   |-- PrismResamplingPlanner.cs
|   |   |   |-- PrismReticulationFilter.cs
|   |   |   |-- PrismRippleFilter.cs
|   |   |   |-- PrismRoughPastelsFilter.cs
|   |   |   |-- PrismScanlinesFilter.cs
|   |   |   |-- PrismSelectiveColorFilter.cs
|   |   |   |-- PrismShapeBlurFilter.cs
|   |   |   |-- PrismSharpenEdgesFilter.cs
|   |   |   |-- PrismSharpenFilter.cs
|   |   |   |-- PrismSharpenMoreFilter.cs
|   |   |   |-- PrismShearFilter.cs
|   |   |   |-- PrismSmartBlurFilter.cs
|   |   |   |-- PrismSmartSharpenFilter.cs
|   |   |   |-- PrismSmudgeStickFilter.cs
|   |   |   |-- PrismSolarizeFilter.cs
|   |   |   |-- PrismSpatterFilter.cs
|   |   |   |-- PrismSpherizeFilter.cs
|   |   |   |-- PrismSpinBlurFilter.cs
|   |   |   |-- PrismSpongeFilter.cs
|   |   |   |-- PrismSprayedStrokesFilter.cs
|   |   |   |-- PrismStainedGlassFilter.cs
|   |   |   |-- PrismStampFilter.cs
|   |   |   |-- PrismSumiEFilter.cs
|   |   |   |-- PrismSurfaceBlurFilter.cs
|   |   |   |-- PrismSurfaceTexture.cs
|   |   |   |-- PrismTexturizerFilter.cs
|   |   |   |-- PrismThresholdAnalysis.cs
|   |   |   |-- PrismThresholdFilter.cs
|   |   |   |-- PrismTilesFilter.cs
|   |   |   |-- PrismTiltShiftFilter.cs
|   |   |   |-- PrismTornEdgesFilter.cs
|   |   |   |-- PrismTraceContourFilter.cs
|   |   |   |-- PrismTransformFilter.cs
|   |   |   |-- PrismTwirlFilter.cs
|   |   |   |-- PrismUnderpaintingFilter.cs
|   |   |   |-- PrismUnsharpMaskFilter.cs
|   |   |   |-- PrismVibranceFilter.cs
|   |   |   |-- PrismWatercolorFilter.cs
|   |   |   |-- PrismWaterPaperFilter.cs
|   |   |   |-- PrismWaveFilter.cs
|   |   |   |-- PrismWaveNoise.cs
|   |   |   |-- PrismWindFilter.cs
|   |   |   |-- PrismXDogLuminance.cs
|   |   |   +-- PrismZigZagFilter.cs
|   |   |-- Graph/
|   |   |   |-- PrismAnalyzedScope.cs
|   |   |   |-- PrismBackdropFramePolicy.cs
|   |   |   |-- PrismBackdropRequirement.cs
|   |   |   |-- PrismColdStartWarmup.cs
|   |   |   |-- PrismDependencyStamp.cs
|   |   |   |-- PrismFnv1aHash.cs
|   |   |   |-- PrismFrameAnalysis.cs
|   |   |   |-- PrismFrameAnalyzer.cs
|   |   |   |-- PrismGraph.cs
|   |   |   |-- PrismGraphBuilder.cs
|   |   |   |-- PrismGraphCapabilities.cs
|   |   |   |-- PrismGraphDiagnostic.cs
|   |   |   |-- PrismGraphOptimizer.Bounds.cs
|   |   |   |-- PrismGraphOptimizer.CachePolicy.cs
|   |   |   |-- PrismGraphOptimizer.cs
|   |   |   |-- PrismGraphOptimizer.ExecutionPlan.cs
|   |   |   |-- PrismGraphOptimizer.Topology.cs
|   |   |   |-- PrismInputDependency.cs
|   |   |   |-- PrismRasterPlanner.cs
|   |   |   +-- PrismRetainedCacheKey.cs
|   |   |-- Masking/
|   |   |   |-- PrismClippingStyle.cs
|   |   |   |-- PrismMaskMath.cs
|   |   |   +-- PrismMaskStyle.cs
|   |   |-- Shaders/
|   |   |   +-- Hlsl/
|   |   |       |-- Blends/
|   |   |       |   |-- AdvancedBlending.hlsl
|   |   |       |   |-- All.hlsl
|   |   |       |   |-- BlendIf.hlsl
|   |   |       |   |-- Color.hlsl
|   |   |       |   |-- ColorBurn.hlsl
|   |   |       |   |-- ColorDodge.hlsl
|   |   |       |   |-- Common.hlsl
|   |   |       |   |-- Darken.hlsl
|   |   |       |   |-- DarkerColor.hlsl
|   |   |       |   |-- Difference.hlsl
|   |   |       |   |-- Dispatcher.hlsl
|   |   |       |   |-- Dissolve.hlsl
|   |   |       |   |-- Divide.hlsl
|   |   |       |   |-- Exclusion.hlsl
|   |   |       |   |-- HardLight.hlsl
|   |   |       |   |-- HardMix.hlsl
|   |   |       |   |-- Hue.hlsl
|   |   |       |   |-- Lighten.hlsl
|   |   |       |   |-- LighterColor.hlsl
|   |   |       |   |-- LinearBurn.hlsl
|   |   |       |   |-- LinearDodge.hlsl
|   |   |       |   |-- LinearLight.hlsl
|   |   |       |   |-- Luminosity.hlsl
|   |   |       |   |-- Multiply.hlsl
|   |   |       |   |-- Normal.hlsl
|   |   |       |   |-- Overlay.hlsl
|   |   |       |   |-- PassThrough.hlsl
|   |   |       |   |-- PinLight.hlsl
|   |   |       |   |-- Saturation.hlsl
|   |   |       |   |-- Screen.hlsl
|   |   |       |   |-- SoftLight.hlsl
|   |   |       |   |-- Subtract.hlsl
|   |   |       |   +-- VividLight.hlsl
|   |   |       |-- Color/
|   |   |       |   |-- All.hlsl
|   |   |       |   |-- Common.hlsl
|   |   |       |   |-- DisplayP3.hlsl
|   |   |       |   |-- LinearDisplayP3.hlsl
|   |   |       |   |-- LinearSrgb.hlsl
|   |   |       |   |-- ScRgb.hlsl
|   |   |       |   |-- Srgb.hlsl
|   |   |       |   +-- WorkingColorConversion.hlsl
|   |   |       |-- Composition/
|   |   |       |   |-- Clipping.hlsl
|   |   |       |   |-- Copy.hlsl
|   |   |       |   |-- Mask.hlsl
|   |   |       |   +-- RasterCoordinates.hlsl
|   |   |       |-- Filters/
|   |   |       |   |-- Catalog/
|   |   |       |   |   |-- AccentedEdges.hlsl
|   |   |       |   |   |-- AngledStrokes.hlsl
|   |   |       |   |   |-- Artistic.hlsl
|   |   |       |   |   |-- BasRelief.hlsl
|   |   |       |   |   |-- CategoryDispatchers.hlsl
|   |   |       |   |   |-- ChalkCharcoal.hlsl
|   |   |       |   |   |-- Charcoal.hlsl
|   |   |       |   |   |-- ChromaticAberration.hlsl
|   |   |       |   |   |-- Chrome.hlsl
|   |   |       |   |   |-- Clouds.hlsl
|   |   |       |   |   |-- Color.hlsl
|   |   |       |   |   |-- ColoredPencil.hlsl
|   |   |       |   |   |-- ColorHalftone.hlsl
|   |   |       |   |   |-- ColorMatrix.hlsl
|   |   |       |   |   |-- Common.hlsl
|   |   |       |   |   |-- ConteCrayon.hlsl
|   |   |       |   |   |-- Craquelure.hlsl
|   |   |       |   |   |-- Crosshatch.hlsl
|   |   |       |   |   |-- Crystallize.hlsl
|   |   |       |   |   |-- CustomConvolution.hlsl
|   |   |       |   |   |-- Cutout.hlsl
|   |   |       |   |   |-- DarkStrokes.hlsl
|   |   |       |   |   |-- DifferenceClouds.hlsl
|   |   |       |   |   |-- Diffuse.hlsl
|   |   |       |   |   |-- Dispatcher.hlsl
|   |   |       |   |   |-- DryBrush.hlsl
|   |   |       |   |   |-- Emboss.hlsl
|   |   |       |   |   |-- Extrude.hlsl
|   |   |       |   |   |-- Facet.hlsl
|   |   |       |   |   |-- Fibers.hlsl
|   |   |       |   |   |-- FilmGrain.hlsl
|   |   |       |   |   |-- Fragment.hlsl
|   |   |       |   |   |-- Fresco.hlsl
|   |   |       |   |   |-- GlowingEdges.hlsl
|   |   |       |   |   |-- Grain.hlsl
|   |   |       |   |   |-- GraphicPen.hlsl
|   |   |       |   |   |-- HalftonePattern.hlsl
|   |   |       |   |   |-- InkOutlines.hlsl
|   |   |       |   |   |-- LensFlare.hlsl
|   |   |       |   |   |-- LightingEffects.hlsl
|   |   |       |   |   |-- Maximum.hlsl
|   |   |       |   |   |-- Mezzotint.hlsl
|   |   |       |   |   |-- Minimum.hlsl
|   |   |       |   |   |-- Mosaic.hlsl
|   |   |       |   |   |-- MosaicTiles.hlsl
|   |   |       |   |   |-- NotePaper.hlsl
|   |   |       |   |   |-- NtscColors.hlsl
|   |   |       |   |   |-- OilPaint.hlsl
|   |   |       |   |   |-- PaintDaubs.hlsl
|   |   |       |   |   |-- PaletteKnife.hlsl
|   |   |       |   |   |-- Patchwork.hlsl
|   |   |       |   |   |-- Photocopy.hlsl
|   |   |       |   |   |-- Plaster.hlsl
|   |   |       |   |   |-- PlasticWrap.hlsl
|   |   |       |   |   |-- Pointillize.hlsl
|   |   |       |   |   |-- PosterEdges.hlsl
|   |   |       |   |   |-- Procedural.hlsl
|   |   |       |   |   |-- Quantization.hlsl
|   |   |       |   |   |-- Reticulation.hlsl
|   |   |       |   |   |-- RoughPastels.hlsl
|   |   |       |   |   |-- Scanlines.hlsl
|   |   |       |   |   |-- SmudgeStick.hlsl
|   |   |       |   |   |-- Solarize.hlsl
|   |   |       |   |   |-- Spatter.hlsl
|   |   |       |   |   |-- Sponge.hlsl
|   |   |       |   |   |-- SprayedStrokes.hlsl
|   |   |       |   |   |-- StainedGlass.hlsl
|   |   |       |   |   |-- Stamp.hlsl
|   |   |       |   |   |-- SumiE.hlsl
|   |   |       |   |   |-- Texture.hlsl
|   |   |       |   |   |-- Texturizer.hlsl
|   |   |       |   |   |-- Tiles.hlsl
|   |   |       |   |   |-- TornEdges.hlsl
|   |   |       |   |   |-- TraceContour.hlsl
|   |   |       |   |   |-- Underpainting.hlsl
|   |   |       |   |   |-- Video.hlsl
|   |   |       |   |   |-- Watercolor.hlsl
|   |   |       |   |   |-- WaterPaper.hlsl
|   |   |       |   |   +-- Wind.hlsl
|   |   |       |   |-- AdaptiveWideAngle.hlsl
|   |   |       |   |-- AddNoise.hlsl
|   |   |       |   |-- Adjustment.hlsl
|   |   |       |   |-- AdjustmentDispatcher.hlsl
|   |   |       |   |-- Average.hlsl
|   |   |       |   |-- BlackWhite.hlsl
|   |   |       |   |-- Blur.hlsl
|   |   |       |   |-- BlurMore.hlsl
|   |   |       |   |-- BoxBlur.hlsl
|   |   |       |   |-- BrightnessContrast.hlsl
|   |   |       |   |-- ChannelMixer.hlsl
|   |   |       |   |-- ColorBalance.hlsl
|   |   |       |   |-- ColorLookup.hlsl
|   |   |       |   |-- Curves.hlsl
|   |   |       |   |-- Despeckle.hlsl
|   |   |       |   |-- DiffuseGlow.hlsl
|   |   |       |   |-- Displace.hlsl
|   |   |       |   |-- DustScratches.hlsl
|   |   |       |   |-- Exposure.hlsl
|   |   |       |   |-- FieldBlur.hlsl
|   |   |       |   |-- GaussianBlur.hlsl
|   |   |       |   |-- Glass.hlsl
|   |   |       |   |-- GradientMap.hlsl
|   |   |       |   |-- HighPass.hlsl
|   |   |       |   |-- HueSaturation.hlsl
|   |   |       |   |-- Invert.hlsl
|   |   |       |   |-- IrisBlur.hlsl
|   |   |       |   |-- LensBlur.hlsl
|   |   |       |   |-- LensCorrection.hlsl
|   |   |       |   |-- Levels.hlsl
|   |   |       |   |-- Liquify.hlsl
|   |   |       |   |-- Median.hlsl
|   |   |       |   |-- MotionBlur.hlsl
|   |   |       |   |-- Neighborhood.hlsl
|   |   |       |   |-- NeighborhoodDispatcher.hlsl
|   |   |       |   |-- NeonGlow.hlsl
|   |   |       |   |-- OceanRipple.hlsl
|   |   |       |   |-- Offset.hlsl
|   |   |       |   |-- OptimizedBilinearGaussian.hlsl
|   |   |       |   |-- PathBlur.hlsl
|   |   |       |   |-- PhotoFilter.hlsl
|   |   |       |   |-- Pinch.hlsl
|   |   |       |   |-- PolarCoordinates.hlsl
|   |   |       |   |-- Posterize.hlsl
|   |   |       |   |-- RadialBlur.hlsl
|   |   |       |   |-- ReduceNoise.hlsl
|   |   |       |   |-- Resampling.hlsl
|   |   |       |   |-- ResamplingDispatcher.hlsl
|   |   |       |   |-- Ripple.hlsl
|   |   |       |   |-- SelectiveColor.hlsl
|   |   |       |   |-- ShapeBlur.hlsl
|   |   |       |   |-- Sharpen.hlsl
|   |   |       |   |-- SharpenEdges.hlsl
|   |   |       |   |-- SharpenMore.hlsl
|   |   |       |   |-- Shear.hlsl
|   |   |       |   |-- SmartBlur.hlsl
|   |   |       |   |-- SmartSharpen.hlsl
|   |   |       |   |-- Spherize.hlsl
|   |   |       |   |-- SpinBlur.hlsl
|   |   |       |   |-- SurfaceBlur.hlsl
|   |   |       |   |-- Threshold.hlsl
|   |   |       |   |-- TiltShift.hlsl
|   |   |       |   |-- Transform.hlsl
|   |   |       |   |-- Twirl.hlsl
|   |   |       |   |-- UnsharpMask.hlsl
|   |   |       |   |-- Vibrance.hlsl
|   |   |       |   |-- Wave.hlsl
|   |   |       |   +-- ZigZag.hlsl
|   |   |       |-- Styles/
|   |   |       |   |-- BevelEmboss.hlsl
|   |   |       |   |-- ColorOverlay.hlsl
|   |   |       |   |-- Common.hlsl
|   |   |       |   |-- DistanceField.hlsl
|   |   |       |   |-- DropShadow.hlsl
|   |   |       |   |-- GradientOverlay.hlsl
|   |   |       |   |-- InnerGlow.hlsl
|   |   |       |   |-- InnerShadow.hlsl
|   |   |       |   |-- LayerStyle.hlsl
|   |   |       |   |-- OuterGlow.hlsl
|   |   |       |   |-- PatternOverlay.hlsl
|   |   |       |   |-- Satin.hlsl
|   |   |       |   +-- Stroke.hlsl
|   |   |       +-- AllKernels.hlsl
|   |   |-- Styles/
|   |   |   |-- PrismBevelEmbossStyle.cs
|   |   |   |-- PrismColorOverlayStyle.cs
|   |   |   |-- PrismCssGradientLut.cs
|   |   |   |-- PrismDropShadowStyle.cs
|   |   |   |-- PrismGradientOverlayStyle.cs
|   |   |   |-- PrismInnerGlowStyle.cs
|   |   |   |-- PrismInnerShadowStyle.cs
|   |   |   |-- PrismOuterGlowStyle.cs
|   |   |   |-- PrismPatternOverlayStyle.cs
|   |   |   |-- PrismSatinStyle.cs
|   |   |   |-- PrismStrokeStyle.cs
|   |   |   +-- PrismStylePlanner.cs
|   |   |-- Surfaces/
|   |   |   +-- PrismSurfaceAllocationException.cs
|   |   |-- BackdropAlphaMode.cs
|   |   |-- BackdropFrameMetadata.cs
|   |   |-- BackdropFrameRequest.cs
|   |   |-- BackdropPixelFormat.cs
|   |   |-- IBackdropFrameLease.cs
|   |   |-- IBackdropFrameSource.cs
|   |   |-- Prism.cs
|   |   |-- PrismBackdropSourceToken.cs
|   |   |-- PrismCacheInvalidationQueue.cs
|   |   |-- PrismCacheOwnerTokenAllocator.cs
|   |   |-- PrismDrawResources.cs
|   |   |-- PrismDrawScope.cs
|   |   |-- PrismEnumValidation.cs
|   |   |-- PrismImage.cs
|   |   |-- PrismOperation.cs
|   |   |-- PrismPipeline.cs
|   |   +-- PrismRendererOptions.cs
|   |-- Text/
|   |   |-- OpenTypeFontData.cs
|   |   |-- RasterizedText.cs
|   |   |-- SkiaFont.cs
|   |   |-- SkiaTextBlobCache.cs
|   |   |-- SkiaTextRasterizer.cs
|   |   |-- SkiaTextRendering.cs
|   |   |-- SkiaTextShaper.cs
|   |   |-- SystemFontSource.cs
|   |   |-- TextCaretVerticalMetrics.cs
|   |   |-- TextShaper.cs
|   |   |-- TextShapeResult.cs
|   |   |-- UnicodeBidiEngine.cs
|   |   +-- UnicodeLineBreakEngine.cs
|   |-- Color.cs
|   |-- DrawArgument.cs
|   |-- DrawBatches.cs
|   |-- DrawCommand.cs
|   |-- DrawCommand.Images.cs
|   |-- DrawCommand.Shapes.cs
|   |-- DrawCommand.Text.cs
|   |-- DrawCommandKind.cs
|   |-- DrawCommandList.cs
|   |-- DrawCommandMetadata.cs
|   |-- DrawCornerRadius.cs
|   |-- DrawFillRule.cs
|   |-- DrawImageFlip.cs
|   |-- DrawImageOptions.cs
|   |-- DrawingBackendFrameTiming.cs
|   |-- DrawingContext.cs
|   |-- DrawingContext.Images.cs
|   |-- DrawingContext.Shapes.cs
|   |-- DrawingContext.Text.cs
|   |-- DrawingFrameContext.cs
|   |-- DrawMesh2D.cs
|   |-- DrawPath.cs
|   |-- DrawPathBuilder.cs
|   |-- DrawPathFactory.cs
|   |-- DrawPathParser.cs
|   |-- DrawPoint.cs
|   |-- DrawRect.cs
|   |-- DrawSize.cs
|   |-- DrawState.cs
|   |-- DrawStrokeStyle.cs
|   |-- DrawTextLayout.cs
|   |-- DrawTextRun.cs
|   |-- IDrawBrush.cs
|   |-- IDrawFont.cs
|   |-- IDrawImage.cs
|   |-- IDrawingBackend.cs
|   |-- IFontSource.cs
|   |-- IRenderSurface2DSource.cs
|   |-- IRenderSurface3DSource.cs
|   |-- RenderProjection3D.cs
|   |-- RenderSurface2DGeometry.cs
|   +-- SvgRasterizer.cs
|-- Playground/
|   |-- Assets/
|   |   |-- cerneala-playground.ico
|   |   +-- fxemoji.svg
|   |-- AvaloniaOracle/
|   |   |-- App.cs
|   |   |-- AvaloniaOracle.csproj
|   |   |-- MainWindow.cs
|   |   |-- MotionLabHeaderWindow.cs
|   |   |-- Program.cs
|   |   +-- SvgWindow.cs
|   |-- Cerneala.ComboBoxLab/
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.ComboBoxLab.csproj
|   |   |-- MainWindow.crn
|   |   +-- MainWindow.crn.cs
|   |-- Cerneala.InvestorReel/
|   |   |-- Themes/
|   |   |   |-- Brutalist/
|   |   |   |   |-- BrutalBindScene.crn
|   |   |   |   |-- BrutalBindScene.crn.cs
|   |   |   |   |-- BrutalCloseScene.crn
|   |   |   |   |-- BrutalCloseScene.crn.cs
|   |   |   |   |-- BrutalEditorScene.crn
|   |   |   |   |-- BrutalEditorScene.crn.cs
|   |   |   |   |-- BrutalMotionScene.crn
|   |   |   |   |-- BrutalMotionScene.crn.cs
|   |   |   |   |-- BrutalOpenScene.crn
|   |   |   |   |-- BrutalOpenScene.crn.cs
|   |   |   |   |-- BrutalPrismScene.crn
|   |   |   |   |-- BrutalPrismScene.crn.cs
|   |   |   |   |-- BrutalProofScene.crn
|   |   |   |   |-- BrutalProofScene.crn.cs
|   |   |   |   |-- BrutalRedrawScene.crn
|   |   |   |   |-- BrutalRedrawScene.crn.cs
|   |   |   |   |-- BrutalReel.crn
|   |   |   |   |-- BrutalReel.crn.cs
|   |   |   |   |-- BrutalRuleScene.crn
|   |   |   |   |-- BrutalRuleScene.crn.cs
|   |   |   |   |-- BrutalSketchScene.crn
|   |   |   |   |-- BrutalSketchScene.crn.cs
|   |   |   |   |-- BrutalStackScene.crn
|   |   |   |   |-- BrutalStackScene.crn.cs
|   |   |   |   |-- BrutalWorldScene.crn
|   |   |   |   +-- BrutalWorldScene.crn.cs
|   |   |   |-- Cyberpunk/
|   |   |   |   |-- CyberBindScene.crn
|   |   |   |   |-- CyberBindScene.crn.cs
|   |   |   |   |-- CyberCloseScene.crn
|   |   |   |   |-- CyberCloseScene.crn.cs
|   |   |   |   |-- CyberEditorScene.crn
|   |   |   |   |-- CyberEditorScene.crn.cs
|   |   |   |   |-- CyberMotionScene.crn
|   |   |   |   |-- CyberMotionScene.crn.cs
|   |   |   |   |-- CyberOpenScene.crn
|   |   |   |   |-- CyberOpenScene.crn.cs
|   |   |   |   |-- CyberPrismScene.crn
|   |   |   |   |-- CyberPrismScene.crn.cs
|   |   |   |   |-- CyberProofScene.crn
|   |   |   |   |-- CyberProofScene.crn.cs
|   |   |   |   |-- CyberRedrawScene.crn
|   |   |   |   |-- CyberRedrawScene.crn.cs
|   |   |   |   |-- CyberReel.crn
|   |   |   |   |-- CyberReel.crn.cs
|   |   |   |   |-- CyberRuleScene.crn
|   |   |   |   |-- CyberRuleScene.crn.cs
|   |   |   |   |-- CyberSketchScene.crn
|   |   |   |   |-- CyberSketchScene.crn.cs
|   |   |   |   |-- CyberStackScene.crn
|   |   |   |   |-- CyberStackScene.crn.cs
|   |   |   |   |-- CyberWorldScene.crn
|   |   |   |   +-- CyberWorldScene.crn.cs
|   |   |   |-- Ink/
|   |   |   |   |-- BindScene.crn
|   |   |   |   |-- BindScene.crn.cs
|   |   |   |   |-- EditorScene.crn
|   |   |   |   |-- EditorScene.crn.cs
|   |   |   |   |-- InkReel.crn
|   |   |   |   |-- InkReel.crn.cs
|   |   |   |   |-- InkScene.crn
|   |   |   |   |-- InkScene.crn.cs
|   |   |   |   |-- PageScene.crn
|   |   |   |   |-- PageScene.crn.cs
|   |   |   |   |-- PaperScene.crn
|   |   |   |   |-- PaperScene.crn.cs
|   |   |   |   |-- ProofScene.crn
|   |   |   |   |-- ProofScene.crn.cs
|   |   |   |   |-- RuleScene.crn
|   |   |   |   |-- RuleScene.crn.cs
|   |   |   |   |-- ScoreScene.crn
|   |   |   |   |-- ScoreScene.crn.cs
|   |   |   |   |-- SealScene.crn
|   |   |   |   |-- SealScene.crn.cs
|   |   |   |   |-- SketchScene.crn
|   |   |   |   |-- SketchScene.crn.cs
|   |   |   |   |-- StackScene.crn
|   |   |   |   |-- StackScene.crn.cs
|   |   |   |   |-- WorldScene.crn
|   |   |   |   +-- WorldScene.crn.cs
|   |   |   +-- Sakura/
|   |   |       |-- Blossom.crn
|   |   |       |-- Blossom.crn.cs
|   |   |       |-- SakuraBindScene.crn
|   |   |       |-- SakuraBindScene.crn.cs
|   |   |       |-- SakuraCloseScene.crn
|   |   |       |-- SakuraCloseScene.crn.cs
|   |   |       |-- SakuraEditorScene.crn
|   |   |       |-- SakuraEditorScene.crn.cs
|   |   |       |-- SakuraMotionScene.crn
|   |   |       |-- SakuraMotionScene.crn.cs
|   |   |       |-- SakuraOpenScene.crn
|   |   |       |-- SakuraOpenScene.crn.cs
|   |   |       |-- SakuraPrismScene.crn
|   |   |       |-- SakuraPrismScene.crn.cs
|   |   |       |-- SakuraProofScene.crn
|   |   |       |-- SakuraProofScene.crn.cs
|   |   |       |-- SakuraRedrawScene.crn
|   |   |       |-- SakuraRedrawScene.crn.cs
|   |   |       |-- SakuraReel.crn
|   |   |       |-- SakuraReel.crn.cs
|   |   |       |-- SakuraRuleScene.crn
|   |   |       |-- SakuraRuleScene.crn.cs
|   |   |       |-- SakuraSketchScene.crn
|   |   |       |-- SakuraSketchScene.crn.cs
|   |   |       |-- SakuraStackScene.crn
|   |   |       |-- SakuraStackScene.crn.cs
|   |   |       |-- SakuraWorldScene.crn
|   |   |       +-- SakuraWorldScene.crn.cs
|   |   |-- App.crn
|   |   |-- App.crn.cs
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.InvestorReel.csproj
|   |   |-- README.md
|   |   |-- ReelWindow.Capture.cs
|   |   |-- ReelWindow.crn
|   |   |-- ReelWindow.crn.cs
|   |   |-- ShellViewModel.cs
|   |   |-- ThemePicker.crn
|   |   +-- ThemePicker.crn.cs
|   |-- Cerneala.MenuLab/
|   |   |-- .gitignore
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.MenuLab.csproj
|   |   |-- MainWindow.crn
|   |   |-- MainWindow.crn.cs
|   |   |-- MenuLabViewModel.cs
|   |   +-- README.md
|   |-- Cerneala.Playground/
|   |   |-- Properties/
|   |   |   +-- AssemblyInfo.cs
|   |   |-- SceneWorldAssets/
|   |   |   |-- New-Assets.ps1
|   |   |   |-- README.md
|   |   |   |-- village.ldtk
|   |   |   |-- village.tmj
|   |   |   +-- world-atlas.png
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.Playground.csproj
|   |   |-- DrawingApiShowcase.cs
|   |   |-- DrawingApiShowcaseView.crn
|   |   |-- DrawingApiShowcaseView.crn.cs
|   |   |-- MainWindow.crn
|   |   |-- MainWindow.crn.cs
|   |   |-- SceneWorldConformance.cs
|   |   |-- SceneWorldPackage.cs
|   |   |-- SceneWorldPackages.targets
|   |   |-- SceneWorldShowcase.crn
|   |   |-- SceneWorldShowcase.crn.cs
|   |   +-- SvgWindow.cs
|   |-- Cerneala.SceneVillage/
|   |   |-- Assets/
|   |   |   |-- ch003.png
|   |   |   |-- CREDITS.md
|   |   |   |-- Kenney-TinyTown-License.txt
|   |   |   +-- tiny-town.png
|   |   |-- App.crn
|   |   |-- App.crn.cs
|   |   |-- Cerneala.SceneVillage.csproj
|   |   |-- MainWindow.crn
|   |   |-- MainWindow.crn.cs
|   |   |-- README.md
|   |   |-- VillageArt.cs
|   |   |-- VillageCamera.cs
|   |   |-- VillageGameSurface.cs
|   |   |-- VillageInput.cs
|   |   |-- VillageLayout.cs
|   |   +-- VillageStress.cs
|   |-- Cerneala.SolarSystem/
|   |   |-- App.crn
|   |   |-- App.crn.cs
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.SolarSystem.csproj
|   |   |-- MainWindow.Automation.cs
|   |   |-- MainWindow.crn
|   |   |-- MainWindow.crn.cs
|   |   +-- SolarSystemViewModel.cs
|   +-- CernealaOracle/
|       |-- BackendRegistration.cs
|       |-- CernealaOracle.csproj
|       |-- MainWindow.crn
|       +-- MainWindow.crn.cs
|-- Properties/
|   +-- AssemblyInfo.cs
|-- Shared/
|   +-- PreviewProtocol.cs
|-- tests/
|   |-- Baselines/
|   |   +-- Conformance/
|   |       |-- RetiredWindowsDx/
|   |       |   |-- AccentedEdges.png
|   |       |   |-- AdaptiveWideAngle.png
|   |       |   |-- AddNoise.png
|   |       |   |-- AngledStrokes.png
|   |       |   |-- Average.png
|   |       |   |-- BasRelief.png
|   |       |   |-- BevelEmboss.png
|   |       |   |-- BlackWhite.png
|   |       |   |-- Blur.png
|   |       |   |-- BlurMore.png
|   |       |   |-- BoxBlur.png
|   |       |   |-- BrightnessContrast.png
|   |       |   |-- ChalkCharcoal.png
|   |       |   |-- ChannelMixer.png
|   |       |   |-- Charcoal.png
|   |       |   |-- ChromaticAberration.png
|   |       |   |-- Chrome.png
|   |       |   |-- Clouds.png
|   |       |   |-- Color.png
|   |       |   |-- ColorBalance.png
|   |       |   |-- ColoredPencil.png
|   |       |   |-- ColorHalftone.png
|   |       |   |-- ColorMatrix.png
|   |       |   |-- ColorOverlay.png
|   |       |   |-- comparison.json
|   |       |   |-- ConteCrayon.png
|   |       |   |-- Craquelure.png
|   |       |   |-- Crosshatch.png
|   |       |   |-- Crystallize.png
|   |       |   |-- Cutout.png
|   |       |   |-- DarkStrokes.png
|   |       |   |-- Deinterlace.png
|   |       |   |-- Despeckle.png
|   |       |   |-- DifferenceClouds.png
|   |       |   |-- Diffuse.png
|   |       |   |-- DiffuseGlow.png
|   |       |   |-- drawing-api.png
|   |       |   |-- DropShadow.png
|   |       |   |-- DryBrush.png
|   |       |   |-- DustScratches.png
|   |       |   |-- Emboss.png
|   |       |   |-- Exposure.png
|   |       |   |-- Extrude.png
|   |       |   |-- Facet.png
|   |       |   |-- Fibers.png
|   |       |   |-- FilmGrain.png
|   |       |   |-- FindEdges.png
|   |       |   |-- Fragment.png
|   |       |   |-- Fresco.png
|   |       |   |-- GaussianBlur.png
|   |       |   |-- Glass.png
|   |       |   |-- GlowingEdges.png
|   |       |   |-- GradientOverlay.png
|   |       |   |-- Grain.png
|   |       |   |-- GraphicPen.png
|   |       |   |-- HalftonePattern.png
|   |       |   |-- HighPass.png
|   |       |   |-- HueSaturation.png
|   |       |   |-- InkOutlines.png
|   |       |   |-- InnerGlow.png
|   |       |   |-- InnerShadow.png
|   |       |   |-- Invert.png
|   |       |   |-- IrisBlur.png
|   |       |   |-- LensBlur.png
|   |       |   |-- LensCorrection.png
|   |       |   |-- Levels.png
|   |       |   |-- Maximum.png
|   |       |   |-- Median.png
|   |       |   |-- Mezzotint.png
|   |       |   |-- Minimum.png
|   |       |   |-- Mosaic.png
|   |       |   |-- MosaicTiles.png
|   |       |   |-- MotionBlur.png
|   |       |   |-- NeonGlow.png
|   |       |   |-- NotePaper.png
|   |       |   |-- NtscColors.png
|   |       |   |-- OceanRipple.png
|   |       |   |-- Offset.png
|   |       |   |-- OilPaint.png
|   |       |   |-- OuterGlow.png
|   |       |   |-- PaintDaubs.png
|   |       |   |-- PaletteKnife.png
|   |       |   |-- Patchwork.png
|   |       |   |-- Photocopy.png
|   |       |   |-- PhotoFilter.png
|   |       |   |-- Pinch.png
|   |       |   |-- Plaster.png
|   |       |   |-- PlasticWrap.png
|   |       |   |-- Pointillize.png
|   |       |   |-- PolarCoordinates.png
|   |       |   |-- PosterEdges.png
|   |       |   |-- Posterize.png
|   |       |   |-- RadialBlur.png
|   |       |   |-- README.md
|   |       |   |-- ReduceNoise.png
|   |       |   |-- Reticulation.png
|   |       |   |-- Ripple.png
|   |       |   |-- RoughPastels.png
|   |       |   |-- Satin.png
|   |       |   |-- Scanlines.png
|   |       |   |-- SelectiveColor.png
|   |       |   |-- sha256.json
|   |       |   |-- Sharpen.png
|   |       |   |-- SharpenEdges.png
|   |       |   |-- SharpenMore.png
|   |       |   |-- Shear.png
|   |       |   |-- SmartBlur.png
|   |       |   |-- SmartSharpen.png
|   |       |   |-- SmudgeStick.png
|   |       |   |-- Solarize.png
|   |       |   |-- Spatter.png
|   |       |   |-- Spherize.png
|   |       |   |-- SpinBlur.png
|   |       |   |-- Sponge.png
|   |       |   |-- SprayedStrokes.png
|   |       |   |-- StainedGlass.png
|   |       |   |-- Stamp.png
|   |       |   |-- Stroke.png
|   |       |   |-- SumiE.png
|   |       |   |-- SurfaceBlur.png
|   |       |   |-- Texturizer.png
|   |       |   |-- Threshold.png
|   |       |   |-- Tiles.png
|   |       |   |-- TiltShift.png
|   |       |   |-- TornEdges.png
|   |       |   |-- TraceContour.png
|   |       |   |-- Transform.png
|   |       |   |-- Twirl.png
|   |       |   |-- Underpainting.png
|   |       |   |-- UnsharpMask.png
|   |       |   |-- Vibrance.png
|   |       |   |-- Watercolor.png
|   |       |   |-- WaterPaper.png
|   |       |   |-- Wave.png
|   |       |   |-- Wind.png
|   |       |   +-- ZigZag.png
|   |       |-- SdlGpu/
|   |       |   |-- AccentedEdges.png
|   |       |   |-- AdaptiveWideAngle.png
|   |       |   |-- AddNoise.png
|   |       |   |-- AngledStrokes.png
|   |       |   |-- Average.png
|   |       |   |-- BasRelief.png
|   |       |   |-- BevelEmboss.png
|   |       |   |-- BlackWhite.png
|   |       |   |-- Blur.png
|   |       |   |-- BlurMore.png
|   |       |   |-- BoxBlur.png
|   |       |   |-- BrightnessContrast.png
|   |       |   |-- ChalkCharcoal.png
|   |       |   |-- ChannelMixer.png
|   |       |   |-- Charcoal.png
|   |       |   |-- ChromaticAberration.png
|   |       |   |-- Chrome.png
|   |       |   |-- Clouds.png
|   |       |   |-- Color.png
|   |       |   |-- ColorBalance.png
|   |       |   |-- ColoredPencil.png
|   |       |   |-- ColorHalftone.png
|   |       |   |-- ColorMatrix.png
|   |       |   |-- ColorOverlay.png
|   |       |   |-- comparison.json
|   |       |   |-- ConteCrayon.png
|   |       |   |-- Craquelure.png
|   |       |   |-- Crosshatch.png
|   |       |   |-- Crystallize.png
|   |       |   |-- Cutout.png
|   |       |   |-- DarkStrokes.png
|   |       |   |-- Deinterlace.png
|   |       |   |-- Despeckle.png
|   |       |   |-- DifferenceClouds.png
|   |       |   |-- Diffuse.png
|   |       |   |-- DiffuseGlow.png
|   |       |   |-- drawing-api.png
|   |       |   |-- DropShadow.png
|   |       |   |-- DryBrush.png
|   |       |   |-- DustScratches.png
|   |       |   |-- Emboss.png
|   |       |   |-- Exposure.png
|   |       |   |-- Extrude.png
|   |       |   |-- Facet.png
|   |       |   |-- Fibers.png
|   |       |   |-- FilmGrain.png
|   |       |   |-- FindEdges.png
|   |       |   |-- Fragment.png
|   |       |   |-- Fresco.png
|   |       |   |-- GaussianBlur.png
|   |       |   |-- Glass.png
|   |       |   |-- GlowingEdges.png
|   |       |   |-- GradientOverlay.png
|   |       |   |-- Grain.png
|   |       |   |-- GraphicPen.png
|   |       |   |-- HalftonePattern.png
|   |       |   |-- HighPass.png
|   |       |   |-- HueSaturation.png
|   |       |   |-- InkOutlines.png
|   |       |   |-- InnerGlow.png
|   |       |   |-- InnerShadow.png
|   |       |   |-- Invert.png
|   |       |   |-- IrisBlur.png
|   |       |   |-- LensBlur.png
|   |       |   |-- LensCorrection.png
|   |       |   |-- Levels.png
|   |       |   |-- Maximum.png
|   |       |   |-- Median.png
|   |       |   |-- Mezzotint.png
|   |       |   |-- Minimum.png
|   |       |   |-- Mosaic.png
|   |       |   |-- MosaicTiles.png
|   |       |   |-- MotionBlur.png
|   |       |   |-- NeonGlow.png
|   |       |   |-- NotePaper.png
|   |       |   |-- NtscColors.png
|   |       |   |-- OceanRipple.png
|   |       |   |-- Offset.png
|   |       |   |-- OilPaint.png
|   |       |   |-- OuterGlow.png
|   |       |   |-- PaintDaubs.png
|   |       |   |-- PaletteKnife.png
|   |       |   |-- Patchwork.png
|   |       |   |-- Photocopy.png
|   |       |   |-- PhotoFilter.png
|   |       |   |-- Pinch.png
|   |       |   |-- Plaster.png
|   |       |   |-- PlasticWrap.png
|   |       |   |-- Pointillize.png
|   |       |   |-- PolarCoordinates.png
|   |       |   |-- PosterEdges.png
|   |       |   |-- Posterize.png
|   |       |   |-- RadialBlur.png
|   |       |   |-- README.md
|   |       |   |-- ReduceNoise.png
|   |       |   |-- Reticulation.png
|   |       |   |-- Ripple.png
|   |       |   |-- RoughPastels.png
|   |       |   |-- Satin.png
|   |       |   |-- Scanlines.png
|   |       |   |-- SelectiveColor.png
|   |       |   |-- sha256.json
|   |       |   |-- Sharpen.png
|   |       |   |-- SharpenEdges.png
|   |       |   |-- SharpenMore.png
|   |       |   |-- Shear.png
|   |       |   |-- SmartBlur.png
|   |       |   |-- SmartSharpen.png
|   |       |   |-- SmudgeStick.png
|   |       |   |-- Solarize.png
|   |       |   |-- Spatter.png
|   |       |   |-- Spherize.png
|   |       |   |-- SpinBlur.png
|   |       |   |-- Sponge.png
|   |       |   |-- SprayedStrokes.png
|   |       |   |-- StainedGlass.png
|   |       |   |-- Stamp.png
|   |       |   |-- Stroke.png
|   |       |   |-- SumiE.png
|   |       |   |-- SurfaceBlur.png
|   |       |   |-- Texturizer.png
|   |       |   |-- Threshold.png
|   |       |   |-- Tiles.png
|   |       |   |-- TiltShift.png
|   |       |   |-- TornEdges.png
|   |       |   |-- TraceContour.png
|   |       |   |-- Transform.png
|   |       |   |-- Twirl.png
|   |       |   |-- Underpainting.png
|   |       |   |-- UnsharpMask.png
|   |       |   |-- Vibrance.png
|   |       |   |-- Watercolor.png
|   |       |   |-- WaterPaper.png
|   |       |   |-- Wave.png
|   |       |   |-- Wind.png
|   |       |   +-- ZigZag.png
|   |       |-- WindowsDx/
|   |       |   |-- drawing-api.png
|   |       |   |-- multi-window-a.png
|   |       |   |-- multi-window-b.png
|   |       |   |-- prism.metrics.txt
|   |       |   +-- prism.png
|   |       |-- capture-windowsdx-drawing.json
|   |       +-- scenes.json
|   |-- Cerneala.SdlGpuSmoke/
|   |   |-- App.crn
|   |   |-- App.crn.cs
|   |   |-- BackendRegistration.cs
|   |   |-- Cerneala.SdlGpuSmoke.csproj
|   |   |-- MainWindow.crn
|   |   |-- MainWindow.crn.cs
|   |   |-- RenderSurface3DPreview.crn
|   |   |-- RenderSurface3DPreview.crn.cs
|   |   |-- SmokeDrawingSurface.cs
|   |   |-- SmokeOptions.cs
|   |   |-- TimbreMarkupPanel.crn
|   |   |-- TimbreMarkupPanel.crn.cs
|   |   |-- TimbreMarkupSmoke.cs
|   |   |-- TimbreMotionPanel.crn
|   |   |-- TimbreMotionPanel.crn.cs
|   |   |-- TimbreMotionSmoke.cs
|   |   +-- TimbreSmoke.cs
|   |-- Cerneala.Tests/
|   |   |-- Architecture/
|   |   |   |-- DesktopBackendDependencyBoundaryTests.cs
|   |   |   |-- DeveloperPreviewScopeTests.cs
|   |   |   |-- ModernAspectArchitectureTests.cs
|   |   |   |-- NamespaceBoundaryTests.cs
|   |   |   |-- RepositoryShapeTests.cs
|   |   |   +-- SdlDependencyBoundaryTests.cs
|   |   |-- Controls/
|   |   |   |-- Primitives/
|   |   |   |   |-- ButtonBaseCommandStateIntegrationTests.cs
|   |   |   |   |-- ButtonBaseCommandTests.cs
|   |   |   |   |-- ButtonBaseTests.cs
|   |   |   |   |-- RangeBaseTests.cs
|   |   |   |   |-- RepeatButtonTests.cs
|   |   |   |   |-- SelectorTests.cs
|   |   |   |   |-- ThumbTests.cs
|   |   |   |   +-- TrackTests.cs
|   |   |   |-- Templates/
|   |   |   |   |-- ComponentTemplateTests.cs
|   |   |   |   |-- ContentTemplateRegistryTests.cs
|   |   |   |   +-- ItemsContentTemplateIntegrationTests.cs
|   |   |   |-- BorderTests.cs
|   |   |   |-- ButtonContentArchitectureTests.cs
|   |   |   |-- ButtonKeyboardActivationTests.cs
|   |   |   |-- ButtonTests.cs
|   |   |   |-- CanvasTests.cs
|   |   |   |-- CheckBoxTests.cs
|   |   |   |-- Collider2DSimulationInterestContractTests.cs
|   |   |   |-- ColliderOwnershipTests.cs
|   |   |   |-- CollisionStageFourInputTests.cs
|   |   |   |-- CollisionStageOneContractTests.cs
|   |   |   |-- CollisionStageThreeTileMapTests.cs
|   |   |   |-- CollisionStageTwoContractTests.cs
|   |   |   |-- CollisionStageZeroContractTests.cs
|   |   |   |-- ColorPickerTests.cs
|   |   |   |-- ColorSwatchTests.cs
|   |   |   |-- ComboBoxTests.cs
|   |   |   |-- ComponentTemplateLifecycleTests.cs
|   |   |   |-- ContentControlTests.cs
|   |   |   |-- ContentPresenterDefaultTextTests.cs
|   |   |   |-- ContentPresenterTests.cs
|   |   |   |-- ContentTemplateTests.cs
|   |   |   |-- ContinuousImpactNormalTests.cs
|   |   |   |-- ControlTests.cs
|   |   |   |-- DecoratorTests.cs
|   |   |   |-- ElementAspectTests.cs
|   |   |   |-- ImageTests.cs
|   |   |   |-- InitialContactMovementTests.cs
|   |   |   |-- InkCanvasTests.cs
|   |   |   |-- ItemContainerGeneratorTests.cs
|   |   |   |-- ItemContainerRecyclePoolTests.cs
|   |   |   |-- ItemsControlRecyclingStabilityTests.cs
|   |   |   |-- ItemsControlRetainedInvalidationTests.cs
|   |   |   |-- ItemsControlTests.cs
|   |   |   |-- ItemsPresenterTests.cs
|   |   |   |-- ItemsSourceObservableTests.cs
|   |   |   |-- ListBoxTests.cs
|   |   |   |-- MenuItemTests.cs
|   |   |   |-- MenuTests.cs
|   |   |   |-- OverlayTests.cs
|   |   |   |-- PanelTests.cs
|   |   |   |-- PasswordBoxTests.cs
|   |   |   |-- ProgressBarTests.cs
|   |   |   |-- RenderSurface2DSceneFoundationContractTests.cs
|   |   |   |-- RenderSurface2DSceneTests.cs
|   |   |   |-- RenderSurface2DTests.cs
|   |   |   |-- RenderSurface3DConformanceFixtureTests.cs
|   |   |   |-- RenderSurface3DStageOnePresenceTests.cs
|   |   |   |-- RenderSurface3DStageZeroCharacterizationTests.cs
|   |   |   |-- RenderSurface3DTests.cs
|   |   |   |-- SceneBoundedCopyTests.cs
|   |   |   |-- SceneChildBoundsIndexTests.cs
|   |   |   |-- SceneCollisionStreamingTests.cs
|   |   |   |-- SceneComponentRuntimeTests.cs
|   |   |   |-- SceneDebugOverlayTests.cs
|   |   |   |-- SceneDocumentContractTests.cs
|   |   |   |-- SceneEntityBoundsTests.cs
|   |   |   |-- SceneImagePresentationTests.cs
|   |   |   |-- SceneImportGeometryTests.cs
|   |   |   |-- SceneImportStageZeroContractTests.cs
|   |   |   |-- SceneItems2DIncrementalContractTests.cs
|   |   |   |-- SceneItems2DSimpleCollectionContractTests.cs
|   |   |   |-- SceneJsonValue2DTests.cs
|   |   |   |-- SceneModelValidationTests.cs
|   |   |   |-- ScenePresentationTests.cs
|   |   |   |-- ScenePrismStreamingTests.cs
|   |   |   |-- SceneSimulationContext2DTests.cs
|   |   |   |-- SceneSpatialResidency2DTests.cs
|   |   |   |-- SceneSpatialSelectionScalingTests.cs
|   |   |   |-- SceneSpatialSource2DTests.cs
|   |   |   |-- SceneStreamingReflectionContractTests.cs
|   |   |   |-- ScrollBarTests.cs
|   |   |   |-- ScrollViewerTests.cs
|   |   |   |-- SelectionModelTests.cs
|   |   |   |-- SliderTests.cs
|   |   |   |-- Sprite2DAuthoringTests.cs
|   |   |   |-- Sprite2DImageResourceTests.cs
|   |   |   |-- Sprite2DSamplingContractTests.cs
|   |   |   |-- SpriteAnimationIntegrationTests.cs
|   |   |   |-- SpriteAnimationSamplerTests.cs
|   |   |   |-- SpriteAnimationSchedulingTests.cs
|   |   |   |-- SpriteAnimationStageZeroContractTests.cs
|   |   |   |-- SpriteResourceProviderCompatibilityTests.cs
|   |   |   |-- StackPanelTests.cs
|   |   |   |-- SvgImageTests.cs
|   |   |   |-- TabControlTests.cs
|   |   |   |-- TabItemTests.cs
|   |   |   |-- TemplateBindingTests.cs
|   |   |   |-- TemplatedButtonStateContractTests.cs
|   |   |   |-- TemplateOwnershipTests.cs
|   |   |   |-- TemplateReflectionTests.cs
|   |   |   |-- TextBlockInvalidationTests.cs
|   |   |   |-- TextBlockLayoutContractTests.cs
|   |   |   |-- TextBlockTests.cs
|   |   |   |-- TextBoxArchitectureTests.cs
|   |   |   |-- TextBoxCaretBlinkTests.cs
|   |   |   |-- TextBoxClipboardShortcutTests.cs
|   |   |   |-- TextBoxEditingVisualContractTests.cs
|   |   |   |-- TextBoxTests.cs
|   |   |   |-- TextBoxTwoWayBindingTests.cs
|   |   |   |-- TileMap2DCacheContractTests.cs
|   |   |   |-- TileMap2DContractTests.cs
|   |   |   |-- TileMap2DDrainContractTests.cs
|   |   |   |-- TileMap2DFactoryTests.cs
|   |   |   |-- TileMap2DStageZeroContractTests.cs
|   |   |   |-- TileMapConstructionContractTests.cs
|   |   |   |-- TileMapIdentityValidationTests.cs
|   |   |   |-- TileMapPaletteResidencyTests.cs
|   |   |   |-- TileMapPresentationStreamingTests.cs
|   |   |   |-- TileMapSingleLayerContractTests.cs
|   |   |   |-- TileMapSource2DTests.cs
|   |   |   |-- TileMapSpatialResidencyTests.cs
|   |   |   |-- TileMapStreamingTests.cs
|   |   |   |-- TileMapTestSource.cs
|   |   |   |-- TilePlacementContractTests.cs
|   |   |   |-- ToggleButtonTests.cs
|   |   |   |-- ToolTipTests.cs
|   |   |   |-- UserControlTests.cs
|   |   |   |-- WindowRenderingTests.cs
|   |   |   +-- WpfEventSurfaceTests.cs
|   |   |-- Drawing/
|   |   |   |-- Paths/
|   |   |   |   |-- DrawPathTests.cs
|   |   |   |   |-- DrawStrokeTests.cs
|   |   |   |   +-- SvgPathFlattenerTests.cs
|   |   |   |-- Prism/
|   |   |   |   |-- Cache/
|   |   |   |   |   |-- PrismRetainedCacheKeyTests.cs
|   |   |   |   |   +-- PrismRetainedVisualVersionTests.cs
|   |   |   |   |-- PrismAccentedEdgesFilterTests.cs
|   |   |   |   |-- PrismAdjustmentFilterTests.cs
|   |   |   |   |-- PrismAdvancedBlendingKnockoutTests.cs
|   |   |   |   |-- PrismBasReliefFilterTests.cs
|   |   |   |   |-- PrismCatalogFilterTests.cs
|   |   |   |   |-- PrismChalkCharcoalFilterTests.cs
|   |   |   |   |-- PrismCharcoalFilterTests.cs
|   |   |   |   |-- PrismChromaticAberrationFilterTests.cs
|   |   |   |   |-- PrismChromeFilterTests.cs
|   |   |   |   |-- PrismCloudsCatalogContractTests.cs
|   |   |   |   |-- PrismColdStartWarmupTests.cs
|   |   |   |   |-- PrismColorBlendStyleCoverageTests.cs
|   |   |   |   |-- PrismColorFilterTests.cs
|   |   |   |   |-- PrismColorMatrixFilterTests.cs
|   |   |   |   |-- PrismColorPipelineTests.cs
|   |   |   |   |-- PrismCommandListContractTests.cs
|   |   |   |   |-- PrismConteCrayonFilterTests.cs
|   |   |   |   |-- PrismCraquelureFilterTests.cs
|   |   |   |   |-- PrismCrosshatchFilterTests.cs
|   |   |   |   |-- PrismCustomConvolutionFilterTests.cs
|   |   |   |   |-- PrismDarkStrokesFilterTests.cs
|   |   |   |   |-- PrismDiffuseGlowCharacterizationTests.cs
|   |   |   |   |-- PrismDissolveBlendTests.cs
|   |   |   |   |-- PrismDistortionFilterTests.cs
|   |   |   |   |-- PrismEmbossFilterTests.cs
|   |   |   |   |-- PrismEnumValidationTests.cs
|   |   |   |   |-- PrismFrameAnalyzerTests.cs
|   |   |   |   |-- PrismFrescoFilterTests.cs
|   |   |   |   |-- PrismGlowingEdgesFilterTests.cs
|   |   |   |   |-- PrismGradientOverlayPipelineTests.cs
|   |   |   |   |-- PrismGrainFilterTests.cs
|   |   |   |   |-- PrismGraphContractTests.cs
|   |   |   |   |-- PrismGraphicPenFilterTests.cs
|   |   |   |   |-- PrismGraphOptimizerTests.cs
|   |   |   |   |-- PrismGraphPlanningAllocationTests.cs
|   |   |   |   |-- PrismImageApiTests.cs
|   |   |   |   |-- PrismInkOutlinesFilterTests.cs
|   |   |   |   |-- PrismLensProfileTests.cs
|   |   |   |   |-- PrismLightingResourceTests.cs
|   |   |   |   |-- PrismMaskPipelineTests.cs
|   |   |   |   |-- PrismMosaicTilesFilterTests.cs
|   |   |   |   |-- PrismNeighborhoodFilterTests.cs
|   |   |   |   |-- PrismNestedScopeCullingTests.cs
|   |   |   |   |-- PrismNotePaperFilterTests.cs
|   |   |   |   |-- PrismOilPaintFilterTests.cs
|   |   |   |   |-- PrismPatchworkFilterTests.cs
|   |   |   |   |-- PrismPhotocopyFilterTests.cs
|   |   |   |   |-- PrismPlasterFilterTests.cs
|   |   |   |   |-- PrismPosterEdgesFilterTests.cs
|   |   |   |   |-- PrismRasterPlannerTests.cs
|   |   |   |   |-- PrismRendererOptionsTests.cs
|   |   |   |   |-- PrismRetainedCommandContractTests.cs
|   |   |   |   |-- PrismReticulationFilterTests.cs
|   |   |   |   |-- PrismRoughPastelsFilterTests.cs
|   |   |   |   |-- PrismScanlinesFilterTests.cs
|   |   |   |   |-- PrismSdlGpuPixelConformanceTests.cs
|   |   |   |   |-- PrismShaderBuildIncrementalityTests.cs
|   |   |   |   |-- PrismSmudgeStickFilterTests.cs
|   |   |   |   |-- PrismSolarizeFilterTests.cs
|   |   |   |   |-- PrismSpatterFilterTests.cs
|   |   |   |   |-- PrismSpongeFilterTests.cs
|   |   |   |   |-- PrismSprayedStrokesFilterTests.cs
|   |   |   |   |-- PrismStainedGlassFilterTests.cs
|   |   |   |   |-- PrismStampFilterTests.cs
|   |   |   |   |-- PrismStyleBackdropAnalysisTests.cs
|   |   |   |   |-- PrismStylePipelineTests.cs
|   |   |   |   |-- PrismSumiEFilterTests.cs
|   |   |   |   |-- PrismTexturizerFilterTests.cs
|   |   |   |   |-- PrismTilesFilterTests.cs
|   |   |   |   |-- PrismTornEdgesFilterTests.cs
|   |   |   |   |-- PrismTraceContourFilterTests.cs
|   |   |   |   |-- PrismUnderpaintingFilterTests.cs
|   |   |   |   |-- PrismVisualStyleSourcePairTests.cs
|   |   |   |   |-- PrismWatercolorFilterTests.cs
|   |   |   |   |-- PrismWaterPaperFilterTests.cs
|   |   |   |   +-- PrismWindFilterTests.cs
|   |   |   |-- AdvancedDrawCommandTests.cs
|   |   |   |-- ColorTests.cs
|   |   |   |-- CompleteDrawingApiBaselineTests.cs
|   |   |   |-- ConformanceBaseline.cs
|   |   |   |-- DrawCommandListTests.cs
|   |   |   |-- DrawingContextTests.cs
|   |   |   |-- DrawingImageMeshBatchTests.cs
|   |   |   |-- DrawingIntegrationLifecycleTests.cs
|   |   |   |-- DrawingResourceTests.cs
|   |   |   |-- DrawingShapeTests.cs
|   |   |   |-- DrawingStateTests.cs
|   |   |   |-- DrawingTextLayoutTests.cs
|   |   |   |-- ImageMetadataSnapshotTests.cs
|   |   |   |-- RetainedCommandVersionAlignmentTests.cs
|   |   |   |-- SdlGpuConformanceArtifacts.cs
|   |   |   |-- SdlGpuConformanceArtifactsTests.cs
|   |   |   |-- SdlGpuDrawingConformanceTests.cs
|   |   |   |-- SpriteBatchGeometryParityTests.cs
|   |   |   +-- TextPipelineTests.cs
|   |   |-- Golden/
|   |   |   +-- Prism/
|   |   |       |-- Analytic/
|   |   |       |   |-- alpha-extremes.png
|   |   |       |   |-- coverage-inputs.png
|   |   |       |   |-- multiply-opaque.png
|   |   |       |   +-- premultiplied-edge.png
|   |   |       |-- backdrop-hosting-150.png
|   |   |       |-- backdrop-hosting-native.png
|   |   |       |-- blend-combination.png
|   |   |       |-- clip.png
|   |   |       |-- clipping-chain.png
|   |   |       |-- conformance.json
|   |   |       |-- fill.png
|   |   |       |-- mask-transform.png
|   |   |       |-- mask.png
|   |   |       |-- nested-groups.png
|   |   |       |-- nested-prism.png
|   |   |       |-- normal-blend.png
|   |   |       |-- opacity.png
|   |   |       |-- style-bevel-emboss.png
|   |   |       |-- style-color-overlay.png
|   |   |       |-- style-drop-shadow.png
|   |   |       |-- style-gradient-overlay.png
|   |   |       |-- style-inner-glow.png
|   |   |       |-- style-inner-shadow.png
|   |   |       |-- style-outer-glow.png
|   |   |       |-- style-pattern-overlay.png
|   |   |       |-- style-satin.png
|   |   |       |-- style-stroke.png
|   |   |       +-- transform.png
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
|   |   |   |-- DrawingApiShowcaseVisualTests.cs
|   |   |   |-- MainWindowContractTests.cs
|   |   |   +-- SceneWorldShowcaseTests.cs
|   |   |-- Presentation/
|   |   |   |-- AspectChapterViewTests.cs
|   |   |   |-- PresentationChapterLayoutTests.cs
|   |   |   |-- PresentationOverlayAspectTests.cs
|   |   |   |-- PrismChapterViewTests.cs
|   |   |   |-- PrismOuterGlowLabViewTests.cs
|   |   |   +-- PrismStudioModelTests.cs
|   |   |-- UI/
|   |   |   |-- Accessibility/
|   |   |   |   |-- AccessibilityPlatformTests.cs
|   |   |   |   |-- AuthoringSemanticsContractTests.cs
|   |   |   |   |-- ButtonSemanticsTests.cs
|   |   |   |   |-- MenuSemanticsTests.cs
|   |   |   |   |-- RetainedSemanticsCacheTests.cs
|   |   |   |   |-- SemanticsProviderTests.cs
|   |   |   |   |-- SemanticsStressBudgetTests.cs
|   |   |   |   |-- SemanticsTreeTests.cs
|   |   |   |   +-- TextBoxSemanticsTests.cs
|   |   |   |-- Aspect/
|   |   |   |   |-- AspectAuditRegressionTests.cs
|   |   |   |   |-- AspectBreakerRegressionTests.cs
|   |   |   |   |-- AspectCheckedExpandedTests.cs
|   |   |   |   |-- AspectEngineStressBudgetTests.cs
|   |   |   |   |-- AspectEngineTests.cs
|   |   |   |   |-- AspectPackageTests.cs
|   |   |   |   |-- AspectRootRegistryTests.cs
|   |   |   |   |-- AspectRuleSetTests.cs
|   |   |   |   |-- AspectRuntimeMigrationTests.cs
|   |   |   |   |-- AspectSlotTests.cs
|   |   |   |   |-- AspectStateSetTests.cs
|   |   |   |   |-- AspectTemplateCatalogIntegrationTests.cs
|   |   |   |   |-- AspectTokenTests.cs
|   |   |   |   |-- AspectUnificationContractTests.cs
|   |   |   |   |-- AspectVariantTests.cs
|   |   |   |   |-- DefaultAspectPackageTests.cs
|   |   |   |   |-- ThemeTokenBridgeTests.cs
|   |   |   |   +-- ThemeTokenPrecedenceTests.cs
|   |   |   |-- Controls/
|   |   |   |   |-- Shapes/
|   |   |   |   |   |-- ShapePrimitiveTests.cs
|   |   |   |   |   |-- ShapeRenderingContractTests.cs
|   |   |   |   |   |-- ShapeRetainedReuseTests.cs
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
|   |   |   |   |-- BindingRelayTests.cs
|   |   |   |   |-- CollectionViewTests.cs
|   |   |   |   |-- ObservableListTests.cs
|   |   |   |   |-- ObservableValueTests.cs
|   |   |   |   |-- StringPropertyPathTests.cs
|   |   |   |   |-- TypedBindingTests.cs
|   |   |   |   +-- UiPropertyBindingTests.cs
|   |   |   |-- Detective/
|   |   |   |   |-- DetectiveSnapshotTests.cs
|   |   |   |   |-- DetectiveTests.cs
|   |   |   |   |-- DirtyTreeDumperTests.cs
|   |   |   |   |-- ElementTreeDumperTests.cs
|   |   |   |   |-- FrameDiagnosticsTests.cs
|   |   |   |   |-- InvalidationTraceTests.cs
|   |   |   |   |-- ModernAspectTraceTests.cs
|   |   |   |   |-- MotionDiagnosticsTests.cs
|   |   |   |   |-- RenderCacheDumperTests.cs
|   |   |   |   |-- RoutedEventTraceTests.cs
|   |   |   |   +-- TileMapDiagnosticsTests.cs
|   |   |   |-- Elements/
|   |   |   |   |-- CollectionMembershipRoleTests.cs
|   |   |   |   |-- ElementHandlerStoreTests.cs
|   |   |   |   |-- ElementLifecycleTests.cs
|   |   |   |   |-- ElementTreeWalkerTests.cs
|   |   |   |   |-- RetainedLifecycleCleanupTests.cs
|   |   |   |   |-- UIElementCollectionInvalidationTests.cs
|   |   |   |   |-- UIElementCollectionTests.cs
|   |   |   |   |-- UIElementInvalidationTests.cs
|   |   |   |   |-- UIElementMotionPropertyTests.cs
|   |   |   |   |-- UIElementTreeTests.cs
|   |   |   |   +-- UIRootTests.cs
|   |   |   |-- Hosting/
|   |   |   |   |-- ApplicationBackendAttributeTests.cs
|   |   |   |   |-- ApplicationBackendRegistrationTests.cs
|   |   |   |   |-- ApplicationRuntimeTests.cs
|   |   |   |   |-- ApplicationTimbreIntegrationTests.cs
|   |   |   |   |-- CoreHostingBoundaryTests.cs
|   |   |   |   |-- DrawingContentServicesLifetimeTests.cs
|   |   |   |   |-- FakeDrawingBackend.cs
|   |   |   |   |-- FakeInputSource.cs
|   |   |   |   |-- FakeUiClock.cs
|   |   |   |   |-- GridAuthoringFrameContractTests.cs
|   |   |   |   |-- ObservableListAuthoringSliceTests.cs
|   |   |   |   |-- PrismBackdropHostingContractTests.cs
|   |   |   |   |-- RetainedStressBudgetTests.cs
|   |   |   |   |-- SdlProcessSmokeTests.cs
|   |   |   |   |-- TabNavigationFrameContractTests.cs
|   |   |   |   |-- UiHostFrameContractTests.cs
|   |   |   |   |-- UiHostFrameStatsIntegrityTests.cs
|   |   |   |   |-- UiHostLateTreeMutationTests.cs
|   |   |   |   |-- UiHostScaleHitTestContractTests.cs
|   |   |   |   |-- UiHostTests.cs
|   |   |   |   |-- UiHostViewportFrameContractTests.cs
|   |   |   |   |-- UiViewportScaleContractTests.cs
|   |   |   |   |-- UiViewportTests.cs
|   |   |   |   |-- WindowRuntimeTestCollection.cs
|   |   |   |   +-- WindowRuntimeTests.cs
|   |   |   |-- Input/
|   |   |   |   |-- ElementInputCacheInvalidationTests.cs
|   |   |   |   |-- HitTestCacheInvalidationTests.cs
|   |   |   |   +-- InputControlBoundaryTests.cs
|   |   |   |-- Invalidation/
|   |   |   |   |-- DetachedQueuedElementTests.cs
|   |   |   |   |-- DirtyPropagationTests.cs
|   |   |   |   |-- DirtyStateTests.cs
|   |   |   |   |-- ElementQueueContractTests.cs
|   |   |   |   |-- ElementQueueOrderIndexTests.cs
|   |   |   |   |-- ElementWorkQueueTests.cs
|   |   |   |   |-- FrameSchedulerStabilityTests.cs
|   |   |   |   |-- FrameStatsTests.cs
|   |   |   |   |-- HitTestQueueTests.cs
|   |   |   |   |-- InvalidationFlagsTests.cs
|   |   |   |   |-- LayoutQueueTests.cs
|   |   |   |   |-- MenuInvalidationTests.cs
|   |   |   |   |-- QueueSchedulerContractTests.cs
|   |   |   |   |-- RenderQueueTests.cs
|   |   |   |   |-- RetainedNoWorkFrameTests.cs
|   |   |   |   +-- UiFrameSchedulerTests.cs
|   |   |   |-- Layout/
|   |   |   |   |-- ArrangeInvalidationAfterArrangeSameFrameRegressionTests.cs
|   |   |   |   |-- CanvasTests.cs
|   |   |   |   |-- CollapsedLayoutBreakerRegressionTests.cs
|   |   |   |   |-- DirectArrangeInvalidationSameFrameRegressionTests.cs
|   |   |   |   |-- GridDefinitionMutationTests.cs
|   |   |   |   |-- GridTests.cs
|   |   |   |   |-- LayoutDiagnosticsAccuracyTests.cs
|   |   |   |   |-- LayoutInvalidationTests.cs
|   |   |   |   |-- LayoutManagerTests.cs
|   |   |   |   |-- LayoutPrimitiveTests.cs
|   |   |   |   |-- MeasureInvalidationAfterMeasureSameFrameRegressionTests.cs
|   |   |   |   |-- ParentRequeuedAfterAncestorMeasuredItTests.cs
|   |   |   |   |-- SelfArrangeInvalidationDuringArrangeRegressionTests.cs
|   |   |   |   |-- SelfMeasureInvalidationDuringMeasureRegressionTests.cs
|   |   |   |   |-- SiblingLayoutSameFrameRegressionTests.cs
|   |   |   |   |-- StackPanelTests.cs
|   |   |   |   |-- SubtreeArrangeInvalidationSameFrameRegressionTests.cs
|   |   |   |   |-- SubtreeMeasureInvalidationSameFrameRegressionTests.cs
|   |   |   |   |-- UIElementMeasureArrangeTests.cs
|   |   |   |   |-- VirtualizationBreakerRegressionTests.cs
|   |   |   |   |-- VirtualizationTests.cs
|   |   |   |   |-- VirtualizingStackPanelTests.cs
|   |   |   |   |-- VisibilityCombinationTests.cs
|   |   |   |   +-- VisibilityTests.cs
|   |   |   |-- Markup/
|   |   |   |   |-- BrushMarkupReaderTests.cs
|   |   |   |   |-- GeneratedMarkupBindingTests.cs
|   |   |   |   |-- GeneratedMarkupMotionTests.cs
|   |   |   |   |-- GeneratedMarkupObservationTests.cs
|   |   |   |   |-- MarkupDiagnosticTests.cs
|   |   |   |   |-- MarkupMotionExecutionTests.cs
|   |   |   |   |-- MotionMarkupLifecycleStressTests.cs
|   |   |   |   |-- UiFactoryTests.cs
|   |   |   |   |-- UiMarkupReaderTests.cs
|   |   |   |   +-- UiMarkupWriterTests.cs
|   |   |   |-- Media/
|   |   |   |   |-- BrushTests.cs
|   |   |   |   |-- GeometryTests.cs
|   |   |   |   |-- ImageSourceTests.cs
|   |   |   |   |-- RichPathGeometryTests.cs
|   |   |   |   +-- TransformTests.cs
|   |   |   |-- Motion/
|   |   |   |   |-- Core/
|   |   |   |   |   |-- ManualMotionClock.cs
|   |   |   |   |   |-- MotionCompositionReducedMotionTests.cs
|   |   |   |   |   |-- MotionGroupTests.cs
|   |   |   |   |   |-- MotionHandleThreadAffinityTests.cs
|   |   |   |   |   |-- MotionRepeatTimelineTests.cs
|   |   |   |   |   |-- MotionSystemTests.cs
|   |   |   |   |   |-- MotionValueTests.cs
|   |   |   |   |   +-- SpringRetargetTests.cs
|   |   |   |   |-- Input/
|   |   |   |   |   +-- MotionInputTimelineTests.cs
|   |   |   |   |-- Interpolation/
|   |   |   |   |   |-- TransformMixerDegenerateTests.cs
|   |   |   |   |   +-- ValueMixerBuiltInTests.cs
|   |   |   |   |-- Layout/
|   |   |   |   |   +-- LayoutMotionCoordinatorTests.cs
|   |   |   |   |-- Presence/
|   |   |   |   |   +-- PresenceCoordinatorTests.cs
|   |   |   |   |-- Properties/
|   |   |   |   |   |-- MotionOvershootPropertyTests.cs
|   |   |   |   |   +-- MotionPropertyBindingTests.cs
|   |   |   |   |-- Specs/
|   |   |   |   |   |-- EasingTests.cs
|   |   |   |   |   |-- MotionSpecTests.cs
|   |   |   |   |   +-- TransformSpringTests.cs
|   |   |   |   |-- Transactions/
|   |   |   |   |   +-- MotionTransactionTests.cs
|   |   |   |   |-- MotionAllocationTests.cs
|   |   |   |   |-- MotionFacadeTests.cs
|   |   |   |   |-- MotionReflectionTests.cs
|   |   |   |   |-- MotionStressTests.cs
|   |   |   |   +-- ObjectMotionTests.cs
|   |   |   |-- Platform/
|   |   |   |   |-- PlatformBoundaryTests.cs
|   |   |   |   |-- ServiceRegistrationTests.cs
|   |   |   |   +-- UiHostPlatformServicesIntegrationTests.cs
|   |   |   |-- Prism/
|   |   |   |   |-- PrismAttachmentTests.cs
|   |   |   |   |-- PrismDefinitionContractTests.cs
|   |   |   |   |-- PrismInstanceTests.cs
|   |   |   |   +-- PrismMotionIntegrationTests.cs
|   |   |   |-- Relay/
|   |   |   |   |-- FirstPartyRelayIntegrationTests.cs
|   |   |   |   |-- RelayStageZeroTests.cs
|   |   |   |   |-- UiRelayCoreTests.cs
|   |   |   |   |-- UiRelayHostingIntegrationTests.cs
|   |   |   |   |-- UiRelayInvocationCompletionTests.cs
|   |   |   |   |-- UiRelayStressAndAllocationTests.cs
|   |   |   |   |-- UiRelaySynchronizationContextTests.cs
|   |   |   |   +-- UiThreadAffinityTests.cs
|   |   |   |-- Rendering/
|   |   |   |   |-- ArchitectureBoundaryTests.cs
|   |   |   |   |-- DrawCommandListBuilderTests.cs
|   |   |   |   |-- DrawCommandListPoolTests.cs
|   |   |   |   |-- ElementRenderCacheTests.cs
|   |   |   |   |-- RenderBackdoorContractTests.cs
|   |   |   |   |-- RenderCacheImageLifetimeTests.cs
|   |   |   |   |-- RenderCountersTests.cs
|   |   |   |   |-- RenderDependencyTests.cs
|   |   |   |   |-- RenderingTestElement.cs
|   |   |   |   |-- RenderLayerMotionTests.cs
|   |   |   |   |-- RenderLayerTests.cs
|   |   |   |   |-- RenderQueueProcessorTests.cs
|   |   |   |   |-- RenderStressBudgetTests.cs
|   |   |   |   |-- ResourceRenderDependencyTests.cs
|   |   |   |   |-- RetainedRenderCacheTests.cs
|   |   |   |   |-- RetainedRendererDrawPurityTests.cs
|   |   |   |   |-- RetainedRendererTests.cs
|   |   |   |   |-- RetainedTransformContractTests.cs
|   |   |   |   +-- TextRenderDependencyTests.cs
|   |   |   |-- Resources/
|   |   |   |   |-- ApplicationResourceIntegrationTests.cs
|   |   |   |   |-- DetachedResourceDependencyCleanupTests.cs
|   |   |   |   |-- ElementResourceDictionaryTests.cs
|   |   |   |   |-- FontResourceInvalidationTests.cs
|   |   |   |   |-- HostResourceInvalidationIntegrationTests.cs
|   |   |   |   |-- ImageAsyncPresentationTests.cs
|   |   |   |   |-- ImageResourceAsyncLeaseTests.cs
|   |   |   |   |-- ImageResourceCacheTests.cs
|   |   |   |   |-- ImageResourceInvalidationTests.cs
|   |   |   |   |-- ImageResourceLeaseTests.cs
|   |   |   |   |-- ImageResourceResidentProbeTests.cs
|   |   |   |   |-- PathBackedImageResourceIntegrationTests.cs
|   |   |   |   |-- ResourceDependencyTrackerTests.cs
|   |   |   |   |-- ResourceIdTests.cs
|   |   |   |   +-- ResourceStoreTests.cs
|   |   |   |-- Servo/
|   |   |   |   |-- ServoIdContractTests.cs
|   |   |   |   |-- ServoInputTests.cs
|   |   |   |   |-- ServoPublicApiContractTests.cs
|   |   |   |   |-- ServoQueryTests.cs
|   |   |   |   |-- ServoSceneTests.cs
|   |   |   |   |-- ServoSynchronizationTests.cs
|   |   |   |   +-- WindowScreenshotRegionTests.cs
|   |   |   |-- Text/
|   |   |   |   |-- BidiTextServiceTests.cs
|   |   |   |   |-- FontResolverTests.cs
|   |   |   |   |-- TextBlockTextServiceIntegrationTests.cs
|   |   |   |   |-- TextBoxEditorIntegrationTests.cs
|   |   |   |   |-- TextCaretLayoutTests.cs
|   |   |   |   |-- TextCompositionManagerTests.cs
|   |   |   |   |-- TextDocumentTests.cs
|   |   |   |   |-- TextEditingControllerTests.cs
|   |   |   |   |-- TextEditorTests.cs
|   |   |   |   |-- TextLayoutCacheTests.cs
|   |   |   |   |-- TextMeasurerTests.cs
|   |   |   |   |-- TextRendererTests.cs
|   |   |   |   |-- TextRendererWrapContractTests.cs
|   |   |   |   +-- UndoRedoStackTests.cs
|   |   |   +-- Theming/
|   |   |       +-- ThemeTests.cs
|   |   |-- Cerneala.Tests.csproj
|   |   |-- GameBootstrapTests.cs
|   |   |-- GlobalUsings.cs
|   |   |-- SdlBackendTestBootstrap.cs
|   |   +-- xunit.runner.json
|   |-- Cerneala.Tests.Language/
|   |   |-- Corpus/
|   |   |   |-- constructs.json
|   |   |   |-- crn-migration-stage0-inventory.txt
|   |   |   |-- repository-documents.txt
|   |   |   |-- sourcegen-diagnostics.json
|   |   |   |-- timbre-aspect-corpus.json
|   |   |   |-- timbre-corpus.json
|   |   |   +-- timbre-motion-corpus.json
|   |   |-- Cerneala.Tests.Language.csproj
|   |   |-- CernealaDocumentPathTests.cs
|   |   |-- ColliderOwnershipLanguageTests.cs
|   |   |-- CompletionTests.cs
|   |   |-- CorpusCase.cs
|   |   |-- CorpusCoverageTests.cs
|   |   |-- DiagnosticCatalogTests.cs
|   |   |-- EmbeddedSyntaxTests.cs
|   |   |-- FormattingTests.cs
|   |   |-- LanguagePipelineHarness.cs
|   |   |-- MarkupParserTests.cs
|   |   |-- MotionPrismSemanticTests.cs
|   |   |-- NavigationTests.cs
|   |   |-- RecoveryBaselineTests.cs
|   |   |-- RenderSurface3DLanguageTests.cs
|   |   |-- RoslynCompilationSymbolsTests.cs
|   |   |-- SceneComponentLanguageTests.cs
|   |   |-- SEMANTIC-INVENTORY.md
|   |   |-- SemanticScopesTests.cs
|   |   |-- SemanticWorkspaceTests.cs
|   |   |-- SourceGeneratorDiagnosticBaselineTests.cs
|   |   |-- SourceTextTests.cs
|   |   |-- StructureTests.cs
|   |   |-- SyntaxTraversalTests.cs
|   |   |-- TileMapSourceSemanticTests.cs
|   |   |-- TimbreAspectSemanticTests.cs
|   |   |-- TimbreMotionSemanticTests.cs
|   |   |-- TimbreSemanticTests.cs
|   |   +-- TimbreToolingTests.cs
|   |-- Cerneala.Tests.LanguageServer/
|   |   |-- Diagnostics/
|   |   |   +-- diagnostic-catalog-golden.json
|   |   |-- Cerneala.Tests.LanguageServer.csproj
|   |   |-- CompletionProtocolTests.cs
|   |   |-- CrashReporterTests.cs
|   |   |-- DiagnosticsTests.cs
|   |   |-- FormattingProtocolTests.cs
|   |   |-- HardeningProtocolTests.cs
|   |   |-- NavigationProtocolTests.cs
|   |   |-- ProcessLifecycleTests.cs
|   |   |-- ProtocolContractTests.cs
|   |   |-- ProtocolTestClient.cs
|   |   |-- StructureProtocolTests.cs
|   |   +-- WorkspaceTests.cs
|   |-- Cerneala.Tests.PreviewHost/
|   |   |-- Cerneala.Tests.PreviewHost.csproj
|   |   |-- PreviewCompilerFastPathTargetTypeTests.cs
|   |   |-- PreviewHostTests.cs
|   |   +-- RenderSurface3DPreviewTests.cs
|   |-- Cerneala.Tests.Scene2DImporters/
|   |   |-- Cerneala.Tests.Scene2DImporters.csproj
|   |   |-- ImportStageZeroContractTests.cs
|   |   |-- JsonMetadataContractTests.cs
|   |   |-- LdtkImporterTests.cs
|   |   |-- ReferencedFileContractTests.cs
|   |   |-- SceneWorldAssetTests.cs
|   |   |-- TiledContractCoverageTests.cs
|   |   +-- TiledHostileInputTests.cs
|   |-- Cerneala.Tests.Scene2DPackages/
|   |   |-- Cerneala.Tests.Scene2DPackages.csproj
|   |   |-- ImportedPackageTests.cs
|   |   |-- PackageCompilerTests.cs
|   |   |-- PackageValueCodecTests.cs
|   |   |-- Scene2DPackageEntityTests.cs
|   |   |-- Scene2DPackageGridGeometryTests.cs
|   |   |-- Scene2DPackageGridTests.cs
|   |   |-- Scene2DPackageTests.cs
|   |   |-- Scene2DPublicBoundaryContractTests.cs
|   |   +-- Stage0PackageContractTests.cs
|   |-- Cerneala.Tests.SceneVillage/
|   |   |-- Cerneala.Tests.SceneVillage.csproj
|   |   |-- NativeCharacterAtlasTests.cs
|   |   |-- NativeVillageFrameRateGateTests.cs
|   |   |-- NativeVillageScaleTests.cs
|   |   |-- NativeVillageTileBoundaryTests.cs
|   |   |-- NativeVillageWindowTests.cs
|   |   |-- VillageAnimationCaptureSamplingTests.cs
|   |   |-- VillageModelTests.cs
|   |   |-- VillageNativeFactAttribute.cs
|   |   |-- VillageScaleContractTests.cs
|   |   +-- xunit.runner.json
|   |-- Cerneala.Tests.SdlGpu/
|   |   |-- Prism/
|   |   |   |-- PrismAdditionalKernelGpuTests.cs
|   |   |   |-- PrismAdjustmentResourceMigrationTests.cs
|   |   |   |-- PrismBackdropHostingMigrationTests.cs
|   |   |   |-- PrismBaselineConformanceMigrationTests.cs
|   |   |   |-- PrismBlendMathMigrationTests.cs
|   |   |   |-- PrismChromaticAberrationGpuTests.cs
|   |   |   |-- PrismChromeGpuTests.cs
|   |   |   |-- PrismColorGpuTests.cs
|   |   |   |-- PrismColorMatrixGpuTests.cs
|   |   |   |-- PrismConteCrayonGpuTests.cs
|   |   |   |-- PrismCraquelureGpuTests.cs
|   |   |   |-- PrismCurvesTextureMigrationTests.cs
|   |   |   |-- PrismCustomConvolutionGpuTests.cs
|   |   |   |-- PrismDissolveGpuTests.cs
|   |   |   |-- PrismEmbossGpuTests.cs
|   |   |   |-- PrismExecutionBudgetMigrationTests.cs
|   |   |   |-- PrismExecutionMigrationTests.cs
|   |   |   |-- PrismFundamentalGpuTests.cs
|   |   |   |-- PrismGrainGpuTests.cs
|   |   |   |-- PrismGraphicPenGpuTests.cs
|   |   |   |-- PrismLargeShadowTests.cs
|   |   |   |-- PrismMosaicTilesGpuTests.cs
|   |   |   |-- PrismMultipassKernelGpuTests.cs
|   |   |   |-- PrismNestedScopeCullingRenderingTests.cs
|   |   |   |-- PrismNotePaperGpuTests.cs
|   |   |   |-- PrismOperationalDiagnosticsTests.cs
|   |   |   |-- PrismPatchworkGpuTests.cs
|   |   |   |-- PrismPhotocopyGpuTests.cs
|   |   |   |-- PrismPlannerOwnershipTests.cs
|   |   |   |-- PrismPlasterGpuTests.cs
|   |   |   |-- PrismRetainedDependencyMigrationTests.cs
|   |   |   |-- PrismRetainedExecutionTests.cs
|   |   |   |-- PrismReticulationGpuTests.cs
|   |   |   |-- PrismScanlinesGpuTests.cs
|   |   |   |-- PrismSolarizeGpuTests.cs
|   |   |   |-- PrismSpatterPointTextureCacheTests.cs
|   |   |   |-- PrismSpinBlurGpuTests.cs
|   |   |   |-- PrismStainedGlassGpuTests.cs
|   |   |   |-- PrismStrokeDistanceFieldGpuTests.cs
|   |   |   |-- PrismStyleConformanceMigrationTests.cs
|   |   |   |-- PrismSurfaceOwnershipTests.cs
|   |   |   |-- PrismTexturizerGpuTests.cs
|   |   |   |-- PrismTilesGpuTests.cs
|   |   |   |-- PrismTraceContourGpuTests.cs
|   |   |   |-- PrismWaterPaperGpuTests.cs
|   |   |   |-- PrismWindGpuTests.cs
|   |   |   |-- ScenePrismAllocationTests.cs
|   |   |   +-- SdlPrismKernelFixture.cs
|   |   |-- AlphaBlendRenderingTests.cs
|   |   |-- BrushRenderingTests.cs
|   |   |-- CerberusTests.cs
|   |   |-- Cerneala.Tests.SdlGpu.csproj
|   |   |-- ColorPickerSdlGpuTests.cs
|   |   |-- DelayedSdlImageLoader.cs
|   |   |-- DrawingBackendLifecycleContractTests.cs
|   |   |-- DrawingRetainedPayloadTests.cs
|   |   |-- EdgeAntialiasingRenderingTests.cs
|   |   |-- FakeSdlApi.cs
|   |   |-- FakeSdlAudioApi.cs
|   |   |-- GraphixManagedDependencyTests.cs
|   |   |-- GraphixNativeDependencyTests.cs
|   |   |-- NativeDrawingInterpolationBoundaryTests.cs
|   |   |-- NativeDrawingSmokeTests.cs
|   |   |-- NativeImageDomainWarmMeasurements.cs
|   |   |-- NativeImageLeaseTests.cs
|   |   |-- NativePackageGridSubdivisionTests.cs
|   |   |-- NativePackageLifecycleTests.cs
|   |   |-- NativePackageWarmStreamingTests.cs
|   |   |-- NativeRenderSurface3DTests.cs
|   |   |-- NativeSceneImagePresentationTests.cs
|   |   |-- NativeScenePresentationTests.cs
|   |   |-- NativeScenePrismDomainTests.cs
|   |   |-- NativeScenePrismStreamingTests.cs
|   |   |-- NativeSdlLifetimeTests.cs
|   |   |-- NativeServoHeldKeyTests.cs
|   |   |-- NativeTetrisWindowTests.cs
|   |   |-- NativeTimbreOutputTests.cs
|   |   |-- NativeUiImagePresentationTests.cs
|   |   |-- RectanglePixelBoundaryMigrationTests.cs
|   |   |-- RenderSurface2DPresentationTests.cs
|   |   |-- RenderSurface3DBackendRouteTests.cs
|   |   |-- RenderSurface3DMathOracleTests.cs
|   |   |-- RenderSurface3DStage3LifecycleTests.cs
|   |   |-- RenderSurface3DStageZeroNativeCharacterizationTests.cs
|   |   |-- RenderSurface3DStageZeroOracle.cs
|   |   |-- SdlArchitectureTests.cs
|   |   |-- SdlGpuAsyncImageLoadingTests.cs
|   |   |-- SdlGpuBrushTextureLifetimeTests.cs
|   |   |-- SdlGpuCommandRangeStateTests.cs
|   |   |-- SdlGpuDeviceOwnerTests.cs
|   |   |-- SdlGpuDrawingBackendTests.cs
|   |   |-- SdlGpuDrawingFrameCountersTests.cs
|   |   |-- SdlGpuFrameTransactionCharacterizationTests.cs
|   |   |-- SdlGpuGeometryAllocationTests.cs
|   |   |-- SdlGpuGeometryCacheTests.cs
|   |   |-- SdlGpuGeometryUploadArenaTests.cs
|   |   |-- SdlGpuGradientCacheTests.cs
|   |   |-- SdlGpuImageDomainGeometryTests.cs
|   |   |-- SdlGpuImageDomainPayloadTests.cs
|   |   |-- SdlGpuImageDomainWarmBaselineTests.cs
|   |   |-- SdlGpuImageLifetimeTests.cs
|   |   |-- SdlGpuNativePipelineDescriptorTests.cs
|   |   |-- SdlGpuPrismBuiltinTextureAllocationTests.cs
|   |   |-- SdlGpuPrismExecutionColdStartWarmupTests.cs
|   |   |-- SdlGpuPrismExecutorTests.cs
|   |   |-- SdlGpuPrismFrameCountersTests.cs
|   |   |-- SdlGpuPrismUniformAllocationTests.cs
|   |   |-- SdlGpuRenderSurface3DDiagnosticsTests.cs
|   |   |-- SdlGpuSampledTextureReuseTests.cs
|   |   |-- SdlGpuScreenshotRegionTests.cs
|   |   |-- SdlGpuShaderArtifactTests.cs
|   |   |-- SdlGpuSurfaceBufferOwnershipTests.cs
|   |   |-- SdlGpuSurfaceDamageAllocationTests.cs
|   |   |-- SdlGpuSurfaceRetainedTests.cs
|   |   |-- SdlGpuTextAtlasAllocatorTests.cs
|   |   |-- SdlGpuTextCacheContractTests.cs
|   |   |-- SdlGpuTextCacheTests.cs
|   |   |-- SdlGpuWindowGraphicsSessionTests.cs
|   |   |-- SdlInputSourceTests.cs
|   |   |-- SdlNativeFactAttribute.cs
|   |   |-- SdlNativeTestCollection.cs
|   |   |-- SdlPlatformLifetimeTests.cs
|   |   |-- SdlShaderCompilerVerificationTests.cs
|   |   |-- SdlTimbreOutputRuntimeTests.cs
|   |   |-- SdlTimbreOutputTests.cs
|   |   |-- SdlWindowMigrationContractTests.cs
|   |   |-- SdlWindowPlatformAudioTests.cs
|   |   |-- SdlWindowPlatformTests.cs
|   |   |-- SdlWindowsNativeContractTests.cs
|   |   |-- SdlWindowSurfaceTests.cs
|   |   |-- ShapePrimitiveRenderingTests.cs
|   |   |-- SkiaNativeDependencyTests.cs
|   |   |-- SmokeOptionsTests.cs
|   |   +-- WindowTestDoubles.cs
|   |-- Cerneala.Tests.SourceGen/
|   |   |-- Prism/
|   |   |   |-- PrismCatalogCompilerTests.cs
|   |   |   +-- PrismMarkupContractTests.cs
|   |   |-- Cerneala.Tests.SourceGen.csproj
|   |   |-- PresentationMarkupRegressionTests.cs
|   |   |-- UiMarkupCompilationRetentionTests.cs
|   |   |-- UiMarkupGeneratorApplicationTests.cs
|   |   |-- UiMarkupGeneratorAspectUnificationTests.cs
|   |   |-- UiMarkupGeneratorBackendSelectionTests.cs
|   |   |-- UiMarkupGeneratorBindingStageFiveTests.cs
|   |   |-- UiMarkupGeneratorBindingStageFourTests.cs
|   |   |-- UiMarkupGeneratorBindingStageThreeTests.cs
|   |   |-- UiMarkupGeneratorBindingStageZeroTests.cs
|   |   |-- UiMarkupGeneratorBrushConstructionTests.cs
|   |   |-- UiMarkupGeneratorColliderOwnershipTests.cs
|   |   |-- UiMarkupGeneratorCollisionStageZeroTests.cs
|   |   |-- UiMarkupGeneratorFileExtensionTests.cs
|   |   |-- UiMarkupGeneratorFloatLiteralTests.cs
|   |   |-- UiMarkupGeneratorMenuTests.cs
|   |   |-- UiMarkupGeneratorMotionClipTests.cs
|   |   |-- UiMarkupGeneratorMotionCompositionTests.cs
|   |   |-- UiMarkupGeneratorMotionDiagnosticsTests.cs
|   |   |-- UiMarkupGeneratorMotionHandleTests.cs
|   |   |-- UiMarkupGeneratorMotionLayoutTests.cs
|   |   |-- UiMarkupGeneratorMotionParameterTests.cs
|   |   |-- UiMarkupGeneratorMotionPresenceTests.cs
|   |   |-- UiMarkupGeneratorMotionTests.cs
|   |   |-- UiMarkupGeneratorMotionTimelineTests.cs
|   |   |-- UiMarkupGeneratorRenderSurface2DTests.cs
|   |   |-- UiMarkupGeneratorRenderSurface3DTests.cs
|   |   |-- UiMarkupGeneratorResourceBindingModeTests.cs
|   |   |-- UiMarkupGeneratorSceneComponentTests.cs
|   |   |-- UiMarkupGeneratorSceneImportStageZeroTests.cs
|   |   |-- UiMarkupGeneratorSceneWorldBindingTests.cs
|   |   |-- UiMarkupGeneratorShapeTests.cs
|   |   |-- UiMarkupGeneratorSingleLayerTileMapTests.cs
|   |   |-- UiMarkupGeneratorSpriteAnimationStageZeroTests.cs
|   |   |-- UiMarkupGeneratorSpriteAuthoringTests.cs
|   |   |-- UiMarkupGeneratorSpriteSamplingTests.cs
|   |   |-- UiMarkupGeneratorTemplateOwnerPrecedenceTests.cs
|   |   |-- UiMarkupGeneratorTests.cs
|   |   |-- UiMarkupGeneratorTileAuthoringTests.cs
|   |   |-- UiMarkupGeneratorTileMap2DStageZeroTests.cs
|   |   |-- UiMarkupGeneratorTimbreAttachmentTests.cs
|   |   |-- UiMarkupGeneratorTimbreMotionTests.cs
|   |   |-- UiMarkupGeneratorTimbreTests.cs
|   |   +-- UiMarkupIncrementalTests.cs
|   |-- Cerneala.Tests.Timbre/
|   |   |-- Contracts/
|   |   |   |-- ExternalConsumerTests.cs
|   |   |   +-- TimbreArchitectureTests.cs
|   |   |-- Corpus/
|   |   |   |-- corpus-manifest.json
|   |   |   |-- mp3-corrupt-sync.mp3
|   |   |   |-- mp3-id3v2-id3v1-44100-stereo.mp3
|   |   |   |-- mp3-long-22050-mono-cbr64.mp3
|   |   |   |-- mp3-mpeg1-32000-stereo-vbr.mp3
|   |   |   |-- mp3-mpeg1-44100-stereo-cbr128.mp3
|   |   |   |-- mp3-mpeg1-48000-mono-cbr64.mp3
|   |   |   |-- mp3-mpeg2-16000-stereo-cbr48.mp3
|   |   |   |-- mp3-mpeg2-22050-mono-vbr.mp3
|   |   |   |-- mp3-mpeg25-8000-mono-cbr16.mp3
|   |   |   |-- mp3-no-info-tag-44100-stereo.mp3
|   |   |   |-- mp3-reservoir-wrap-48000-stereo-cbr64.mp3
|   |   |   |-- mp3-truncated.mp3
|   |   |   |-- mpeg-layer2-44100-stereo.mp2
|   |   |   |-- ogg-chained-vorbis.ogg
|   |   |   |-- ogg-flac.ogg
|   |   |   |-- ogg-multiplexed-vorbis-opus.ogg
|   |   |   |-- opus-long-mono-32k.opus
|   |   |   |-- opus-mono-16000-24k.opus
|   |   |   |-- opus-stereo-48000-96k.opus
|   |   |   |-- opus-stereo-preskip-trim.opus
|   |   |   |-- opus-surround-6ch.opus
|   |   |   |-- opus-truncated.opus
|   |   |   |-- vorbis-16000-mono-q2.ogg
|   |   |   |-- vorbis-192000-stereo-q2.ogg
|   |   |   |-- vorbis-22050-mono-q2.ogg
|   |   |   |-- vorbis-44100-stereo-q4.ogg
|   |   |   |-- vorbis-48000-stereo-q2.ogg
|   |   |   |-- vorbis-8000-mono-q2.ogg
|   |   |   |-- vorbis-96000-stereo-q4.ogg
|   |   |   |-- vorbis-corrupt-crc.ogg
|   |   |   |-- vorbis-long-22050-mono-q2.ogg
|   |   |   +-- vorbis-truncated.ogg
|   |   |-- Decoding/
|   |   |   |-- DecodingCorpus.cs
|   |   |   |-- ObservedFileStream.cs
|   |   |   |-- PcmOracle.cs
|   |   |   |-- StreamingDrive.cs
|   |   |   |-- TimbreDecoderConformanceTests.cs
|   |   |   |-- TimbreDecoderErrorTests.cs
|   |   |   |-- TimbreDecoderIntegrationTests.cs
|   |   |   |-- TimbreDecoderPartitionTests.cs
|   |   |   |-- TimbreDecoderRuntimeTests.cs
|   |   |   |-- TimbreStreamingCancelTests.cs
|   |   |   |-- TimbreStreamingFailureTests.cs
|   |   |   |-- TimbreStreamingIncrementalTests.cs
|   |   |   |-- TimbreStreamingLoopTests.cs
|   |   |   |-- TimbreStreamingSeekTests.cs
|   |   |   |-- TimbreStreamingStressTests.cs
|   |   |   |-- TimbreStreamingTransportTests.cs
|   |   |   +-- WavFixture.cs
|   |   |-- Definitions/
|   |   |   |-- TimbreClipDefinitionTests.cs
|   |   |   +-- TimbreDefinitionTests.cs
|   |   |-- Dsp/
|   |   |   |-- DelayKernelTests.cs
|   |   |   |-- DspChainEngineTests.cs
|   |   |   |-- DspOracle.cs
|   |   |   +-- LowPassKernelTests.cs
|   |   |-- Engine/
|   |   |   |-- ConcurrencyStressTests.cs
|   |   |   |-- DeClickTests.cs
|   |   |   |-- HandleAndScopeTests.cs
|   |   |   |-- LifecycleTests.cs
|   |   |   |-- MemoryBudgetTests.cs
|   |   |   |-- PlainMixTests.cs
|   |   |   |-- PlaybackIdentityTests.cs
|   |   |   |-- RealtimeAllocationTests.cs
|   |   |   |-- StreamingPrimingTests.cs
|   |   |   +-- TransportTests.cs
|   |   |-- Harness/
|   |   |   |-- DeterministicTimbreOutput.cs
|   |   |   |-- DeterministicTimbreReader.cs
|   |   |   |-- HarnessObserverTests.cs
|   |   |   |-- HarnessWait.cs
|   |   |   |-- TimbreRig.cs
|   |   |   +-- TimbreRigTests.cs
|   |   |-- Hosting/
|   |   |   +-- OwnerAccessTests.cs
|   |   |-- Markup/
|   |   |   |-- AspectRuntimeProgramTests.cs
|   |   |   |-- GeneratedTimbreConsumer.cs
|   |   |   |-- MarkupTimbreFixture.cs
|   |   |   |-- TimbreAspectIntegrationTests.cs
|   |   |   |-- TimbreAspectLifecycleTests.cs
|   |   |   |-- TimbreAttachmentMarkupTests.cs
|   |   |   |-- TimbreMarkupParityTests.cs
|   |   |   +-- TimbreMarkupTransportTests.cs
|   |   |-- Motion/
|   |   |   |-- TimbreMotionApiBindingTests.cs
|   |   |   |-- TimbreMotionContractTests.cs
|   |   |   |-- TimbreMotionIntegrationTests.cs
|   |   |   |-- TimbreMotionLifecycleTests.cs
|   |   |   |-- TimbreMotionMarkupTests.cs
|   |   |   |-- TimbreMotionPcmTests.cs
|   |   |   |-- TimbreMotionTestKit.cs
|   |   |   |-- TimbreMotionTimelineTests.cs
|   |   |   +-- TimbreMotionTransportTests.cs
|   |   |-- Cerneala.Tests.Timbre.csproj
|   |   +-- PlaybackConstructionTests.cs
|   |-- Cerneala.Tests.VisualStudio/
|   |   |-- Golden/
|   |   |   |-- cerneala-tokenization.crn
|   |   |   +-- cerneala-tokenization.golden.json
|   |   |-- Cerneala.Tests.VisualStudio.csproj
|   |   |-- CernealaGrammarTests.cs
|   |   |-- CernealaServerProcessManagerTests.cs
|   |   |-- PreviewChromeTests.cs
|   |   |-- PreviewInputQueueTests.cs
|   |   |-- PreviewPixelsTests.cs
|   |   |-- Stage4IntegrationHarnessTests.cs
|   |   |-- Stage5PerformanceHarnessTests.cs
|   |   |-- Stage6ReleaseHarnessTests.cs
|   |   |-- VisualStudioBuildGraphTests.cs
|   |   +-- VisualStudioPackageTests.cs
|   |-- Fixtures/
|   |   |-- LanguageServerWorkspace/
|   |   |   |-- LanguageServerWorkspace.csproj
|   |   |   |-- View.crn
|   |   |   +-- View.crn.cs
|   |   |-- Scene2DImport/
|   |   |   |-- invalid/
|   |   |   |   |-- absent-promotion.tmj
|   |   |   |   |-- atlas-bounds.tmj
|   |   |   |   |-- chunk-overlap.tmj
|   |   |   |   |-- degenerate-collider.tmj
|   |   |   |   |-- duplicate-promotion.tmj
|   |   |   |   |-- editor-warning.tmj
|   |   |   |   |-- empty-promotion.tmj
|   |   |   |   |-- layer-mask.tmj
|   |   |   |   |-- ldtk-field.ldtk
|   |   |   |   |-- missing-file.tmj
|   |   |   |   |-- path-escape.tmj
|   |   |   |   |-- tile-id.tmj
|   |   |   |   |-- unknown-field.tmj
|   |   |   |   |-- unsupported.tmj
|   |   |   |   +-- version.tmj
|   |   |   |-- levels/
|   |   |   |   +-- Fixture.ldtkl
|   |   |   |-- tilesets/
|   |   |   |   +-- atlas.tsj
|   |   |   |-- atlas.svg
|   |   |   |-- common.golden.json
|   |   |   |-- compatibility-matrix.json
|   |   |   |-- diagnostic-cases.json
|   |   |   |-- ldtk-inline.ldtk
|   |   |   |-- ldtk-separate.ldtk
|   |   |   |-- New-CompatibilityMatrix.ps1
|   |   |   |-- New-Fixtures.ps1
|   |   |   |-- Test-Fixtures.ps1
|   |   |   |-- tiled-empty-promotion.tmj
|   |   |   |-- tiled-external.tmj
|   |   |   |-- tiled-finite.tmj
|   |   |   |-- tiled-flips.tmj
|   |   |   |-- tiled-group.tmj
|   |   |   |-- tiled-gzip.tmj
|   |   |   |-- tiled-infinite.tmj
|   |   |   |-- tiled-objects.tmj
|   |   |   |-- tiled-raw.tmj
|   |   |   +-- tiled-zlib.tmj
|   |   |-- TimbreConsumer/
|   |   |   |-- ConsumerScenarios.cs
|   |   |   |-- DocumentationExamples.cs
|   |   |   |-- StandaloneUsage.cs
|   |   |   +-- TimbreConsumer.csproj
|   |   |-- TimbreCorpusGenerator/
|   |   |   |-- CorpusSignal.cs
|   |   |   |-- OggWriter.cs
|   |   |   |-- Program.cs
|   |   |   +-- TimbreCorpusGenerator.csproj
|   |   |-- VisualStudioConsumer/
|   |   |   |-- AuthoringView.crn
|   |   |   |-- AuthoringView.crn.cs
|   |   |   |-- DashboardModels.cs
|   |   |   |-- Invoke-CommunityIntegration.ps1
|   |   |   |-- Invoke-CommunityPerformance.ps1
|   |   |   |-- Invoke-CommunityRelease.ps1
|   |   |   |-- MainView.crn
|   |   |   |-- MainView.crn.cs
|   |   |   |-- SecondaryView.crn
|   |   |   |-- SecondaryView.crn.cs
|   |   |   |-- StatusCard.cs
|   |   |   |-- VisualStudioConsumer.csproj
|   |   |   +-- VisualStudioConsumer.slnx
|   |   +-- VisualStudioIntegrationHost/
|   |       |-- source.extension.vsixmanifest
|   |       |-- Stage4Commands.vsct
|   |       |-- Stage4IntegrationPackage.cs
|   |       |-- VisualStudioIntegrationHost.csproj
|   |       |-- VisualStudioStage4Runner.cs
|   |       |-- VisualStudioStage4Runner.Presentation.cs
|   |       +-- VisualStudioStage5Runner.cs
|   +-- Shared/
|       |-- CollisionStageFiveFixture.cs
|       |-- PrismStainedGlassTestData.cs
|       |-- PrismTestData.cs
|       |-- RenderSurface3DConformanceFixture.cs
|       |-- SceneDebugOverlayConformanceFixture.cs
|       |-- SdlDrawingFixture.cs
|       |-- SpriteAnimationConformanceFixture.cs
|       |-- TileMapStage3CaptureFixture.cs
|       +-- TileMapStage5ConformanceFixture.cs
|-- Tetrisish/
|   |-- Assets/
|   |   |-- tetromino-atlas.svg
|   |   |-- tetromino-atlas.svg.cerneala.png
|   |   +-- tetromino-atlas.svg.cerneala.png.sha256
|   |-- Core/
|   |   |-- IPiece.cs
|   |   |-- JPiece.cs
|   |   |-- LPiece.cs
|   |   |-- OPiece.cs
|   |   |-- SPiece.cs
|   |   |-- TetrisBoard.cs
|   |   |-- TetrisGame.cs
|   |   |-- TetrominoPiece.cs
|   |   |-- TPiece.cs
|   |   +-- ZPiece.cs
|   |-- Tests/
|   |   |-- Tetris.Tests.csproj
|   |   +-- TetrisGameTests.cs
|   |-- .gitignore
|   |-- App.crn
|   |-- App.crn.cs
|   |-- MainWindow.crn
|   |-- MainWindow.crn.cs
|   |-- README.md
|   |-- Tetris.csproj
|   |-- Tetris.slnx
|   |-- TetrisGameSurface.cs
|   |-- TetrisSceneModel.cs
|   +-- TetrominoAtlas.cs
|-- Timbre/
|   |-- Catalog/
|   |   +-- TimbreCatalog.cs
|   |-- Decoding/
|   |   |-- NLayer/
|   |   |   |-- Decoder/
|   |   |   |   |-- BitReservoir.cs
|   |   |   |   |-- Huffman.cs
|   |   |   |   |-- LayerDecoderBase.cs
|   |   |   |   |-- LayerIDecoder.cs
|   |   |   |   |-- LayerIIDecoder.cs
|   |   |   |   |-- LayerIIDecoderBase.cs
|   |   |   |   +-- LayerIIIDecoder.cs
|   |   |   |-- Enums.cs
|   |   |   |-- IMpegFrame.cs
|   |   |   +-- MpegFrameDecoder.cs
|   |   |-- CanonicalConverter.cs
|   |   |-- DecodedSource.cs
|   |   |-- DecodedTimbreReader.cs
|   |   |-- Mp3Source.cs
|   |   |-- OggStreamReader.cs
|   |   |-- OpusSource.cs
|   |   |-- THIRD-PARTY-NOTICES.txt
|   |   |-- TimbreFormats.cs
|   |   |-- VorbisSource.cs
|   |   +-- WavSource.cs
|   |-- Dsp/
|   |   |-- DelayKernel.cs
|   |   |-- LowPassKernel.cs
|   |   +-- TimbreDspChain.cs
|   |-- Engine/
|   |   |-- AsyncAutoResetSignal.cs
|   |   |-- ITimbreBlockObserver.cs
|   |   |-- PumpDispatcher.cs
|   |   |-- StreamingFeed.cs
|   |   |-- TimbreDecoders.cs
|   |   |-- TimbreFeed.cs
|   |   |-- TimbreMemoryPool.cs
|   |   |-- TimbrePayloadCache.cs
|   |   |-- TimbreTime.cs
|   |   +-- TimbreVoice.cs
|   |-- ITimbreOutput.cs
|   |-- TimbreClipDefinition.cs
|   |-- TimbreClipSound.cs
|   |-- TimbreException.cs
|   |-- TimbreHandle.cs
|   |-- TimbreInput.cs
|   |-- TimbreLoading.cs
|   |-- TimbreMemoryBudget.cs
|   |-- TimbreModifier.cs
|   |-- TimbreParameter.cs
|   |-- TimbrePlayback.cs
|   |-- TimbrePlaybackResult.cs
|   |-- TimbrePlaybackState.cs
|   |-- TimbreReader.cs
|   |-- TimbreRuntime.Counters.cs
|   |-- TimbreRuntime.cs
|   |-- TimbreRuntime.Loading.cs
|   |-- TimbreRuntime.Mixer.cs
|   |-- TimbreRuntimeDiagnostics.cs
|   |-- TimbreRuntimeOptions.cs
|   |-- TimbreScope.cs
|   |-- TimbreSound.cs
|   |-- TimbreSource.cs
|   +-- TimbreStartOptions.cs
|-- Tools/
|   |-- Cerneala.Scene2D.PackageCompiler/
|   |   |-- Cerneala.Scene2D.PackageCompiler.csproj
|   |   |-- Program.cs
|   |   +-- README.md
|   |-- Cerneala.SdlShaderCompiler/
|   |   |-- Cerneala.SdlShaderCompiler.csproj
|   |   +-- Program.cs
|   |-- Cerneala.SvgAssetCompiler/
|   |   |-- Cerneala.SvgAssetCompiler.csproj
|   |   +-- Program.cs
|   |-- PrismAudit/
|   |   |-- PrismAudit.csproj
|   |   +-- Program.cs
|   |-- RoslynMcp/
|   |   |-- manifest.json
|   |   |-- README.md
|   |   |-- roslyn-5.9.patch
|   |   |-- test_install_retry.ps1
|   |   +-- test_roslyn_mcp.py
|   |-- scripts/
|   |   |-- Archive-Repo.ps1
|   |   |-- Archive-Repo.Tests.ps1
|   |   |-- Build-CernealaVisualStudioRelease.ps1
|   |   |-- Cerneala.BuildInputs.Tests.ps1
|   |   |-- Install-RoslynMcp.ps1
|   |   |-- Invoke-SdlGpuSmoke.ps1
|   |   |-- Measure-PrismOuterGlowColdStart.ps1
|   |   |-- New-FileTree.ps1
|   |   |-- New-PrismFilterReference.ps1
|   |   |-- Publish-SdlGpuSmoke.ps1
|   |   |-- SdlGpuSmoke.Common.ps1
|   |   |-- Start-RoslynMcp.ps1
|   |   |-- Test-MarkdownLinks.ps1
|   |   |-- Test-MarkdownLinks.Tests.ps1
|   |   |-- Test-PrismMotionBlur144Hz.ps1
|   |   +-- Test-TimbreDecoderAssets.ps1
|   |-- TimbreDecoderProbe/
|   |   |-- Prototype/
|   |   |   |-- CanonicalConverter.cs
|   |   |   |-- DecodedSource.cs
|   |   |   |-- Mp3Source.cs
|   |   |   |-- OggStreamReader.cs
|   |   |   |-- OpusSource.cs
|   |   |   |-- ProbeStandIns.cs
|   |   |   |-- VorbisSource.cs
|   |   |   +-- WavSource.cs
|   |   |-- CountingStream.cs
|   |   |-- Footprint.cs
|   |   |-- Format-ProbeSummary.ps1
|   |   |-- Invoke-DecoderProbe.ps1
|   |   |-- LibraryCandidates.cs
|   |   |-- Program.cs
|   |   +-- TimbreDecoderProbe.csproj
|   +-- TimbreDecoderSelection/
|       |-- Program.cs
|       +-- TimbreDecoderSelection.csproj
|-- UI/
|   |-- Accessibility/
|   |   |-- AccessibleName.cs
|   |   |-- AutomationPeer.cs
|   |   |-- ButtonAutomationPeer.cs
|   |   |-- ItemsControlAutomationPeer.cs
|   |   |-- MenuAutomationPeer.cs
|   |   |-- MenuItemAutomationPeer.cs
|   |   |-- PasswordBoxAutomationPeer.cs
|   |   |-- SemanticsNode.cs
|   |   |-- SemanticsProperty.cs
|   |   |-- SemanticsProvider.cs
|   |   |-- SemanticsRole.cs
|   |   |-- SemanticsTree.cs
|   |   +-- TextBoxAutomationPeer.cs
|   |-- Aspect/
|   |   |-- AspectBehavior.cs
|   |   |-- AspectCatalog.cs
|   |   |-- AspectCondition.cs
|   |   |-- AspectConditionDependency.cs
|   |   |-- AspectConditionKey.cs
|   |   |-- AspectConditionNode.cs
|   |   |-- AspectConditionResult.cs
|   |   |-- AspectDataContext.cs
|   |   |-- AspectDataDependency.cs
|   |   |-- AspectDeclaration.cs
|   |   |-- AspectDependencySet.cs
|   |   |-- AspectEngine.cs
|   |   |-- AspectEngineElementState.cs
|   |   |-- AspectEnvironment.cs
|   |   |-- AspectInvalidation.cs
|   |   |-- AspectInvalidationGraph.cs
|   |   |-- AspectLayer.cs
|   |   |-- AspectMatchContext.cs
|   |   |-- AspectMotion.cs
|   |   |-- AspectOrigin.cs
|   |   |-- AspectPackage.cs
|   |   |-- AspectPackageBuilder.cs
|   |   |-- AspectProcessor.cs
|   |   |-- AspectRef.cs
|   |   |-- AspectRegistry.cs
|   |   |-- AspectResolutionContext.cs
|   |   |-- AspectRuleSet.cs
|   |   |-- AspectRuleSetBuilder.cs
|   |   |-- AspectSlot.cs
|   |   |-- AspectSlot{TOwner,TTarget}.cs
|   |   |-- AspectSlotPath.cs
|   |   |-- AspectSpecificity.cs
|   |   |-- AspectState.cs
|   |   |-- AspectStateSet.cs
|   |   |-- AspectTarget.cs
|   |   |-- AspectToken.cs
|   |   |-- AspectToken{T}.cs
|   |   |-- AspectTokenBuilder.cs
|   |   |-- AspectTokenDefinition.cs
|   |   |-- AspectValue.cs
|   |   |-- AspectValue{T}.cs
|   |   |-- AspectVariantKey.cs
|   |   |-- AspectVariantKey{TOwner,TValue}.cs
|   |   |-- AspectVariantSet.cs
|   |   |-- ComponentAspectBuilder.cs
|   |   |-- ContentTemplateBuilder.cs
|   |   |-- DefaultAspectPackage.cs
|   |   |-- DefaultAspectTokens.cs
|   |   |-- ElementAspect.cs
|   |   |-- RejectedAspectDeclaration.cs
|   |   |-- ResolvedAspect.cs
|   |   |-- ResolvedAspectValue.cs
|   |   +-- ThemeTokenBridge.cs
|   |-- Controls/
|   |   |-- Buttons/
|   |   |   |-- ButtonKind.cs
|   |   |   |-- ButtonSize.cs
|   |   |   |-- ButtonSlots.cs
|   |   |   |-- ButtonTemplates.cs
|   |   |   |-- ButtonTokens.cs
|   |   |   +-- ButtonVariants.cs
|   |   |-- Items/
|   |   |   |-- DisplayMemberPathAccessor.cs
|   |   |   |-- ItemCollection.cs
|   |   |   |-- ItemContainerGenerator.cs
|   |   |   +-- ItemContainerRecyclePool.cs
|   |   |-- Primitives/
|   |   |   |-- ButtonBase.cs
|   |   |   |-- DirectionGlyphs.cs
|   |   |   |-- DragCompletedEventArgs.cs
|   |   |   |-- DragDeltaEventArgs.cs
|   |   |   |-- DragStartedEventArgs.cs
|   |   |   |-- RangeBase.cs
|   |   |   |-- RepeatButton.cs
|   |   |   |-- ScrollBar.cs
|   |   |   |-- ScrollBarLayoutPanel.cs
|   |   |   |-- ScrollBarTemplates.cs
|   |   |   |-- ScrollEventArgs.cs
|   |   |   |-- Selector.cs
|   |   |   |-- Thumb.cs
|   |   |   |-- ToggleButton.cs
|   |   |   |-- Track.cs
|   |   |   |-- TrackLayoutPanel.cs
|   |   |   +-- TrackTemplates.cs
|   |   |-- Selection/
|   |   |   |-- SelectionChangedEventArgs.cs
|   |   |   |-- SelectionChangeResult.cs
|   |   |   |-- SelectionModel.cs
|   |   |   +-- SelectionModel{T}.cs
|   |   |-- Shapes/
|   |   |   |-- Ellipse.cs
|   |   |   |-- Line.cs
|   |   |   |-- Path.cs
|   |   |   |-- PointShapeGeometry.cs
|   |   |   |-- Polygon.cs
|   |   |   |-- Polyline.cs
|   |   |   |-- Rectangle.cs
|   |   |   |-- Shape.cs
|   |   |   +-- SvgPath.cs
|   |   |-- Templates/
|   |   |   |-- ComponentTemplate.cs
|   |   |   |-- ComponentTemplateContext.cs
|   |   |   |-- ComponentTemplateDefinition.cs
|   |   |   |-- ComponentTemplateInstance.cs
|   |   |   |-- ContentTemplate.cs
|   |   |   |-- ContentTemplateContext.cs
|   |   |   |-- ContentTemplateDefinition.cs
|   |   |   |-- ContentTemplateMatchContext.cs
|   |   |   |-- ContentTemplateRegistry.cs
|   |   |   |-- TemplateAspectContext.cs
|   |   |   |-- TemplateBinding{T}.cs
|   |   |   |-- TemplateOwnership.cs
|   |   |   |-- TemplatePartAttribute.cs
|   |   |   |-- TemplatePartMap.cs
|   |   |   |-- TemplateRecycleKey.cs
|   |   |   |-- TemplateRecyclePool.cs
|   |   |   |-- TemplateSlotMap.cs
|   |   |   +-- TemplateTokenBinding.cs
|   |   |-- Border.cs
|   |   |-- BoxCollider2D.cs
|   |   |-- Button.cs
|   |   |-- Canvas.cs
|   |   |-- CheckBox.cs
|   |   |-- CheckBoxTemplates.cs
|   |   |-- CircleCollider2D.cs
|   |   |-- Collider2D.cs
|   |   |-- ColliderGeometry2D.cs
|   |   |-- ColliderSlot2D.cs
|   |   |-- CollisionHit2D.cs
|   |   |-- CollisionNarrowPhase2D.cs
|   |   |-- CollisionQuery2D.cs
|   |   |-- CollisionWorld2D.cs
|   |   |-- CollisionWorld2D.Streaming.cs
|   |   |-- CollisionWorld2DDiagnosticsSnapshot.cs
|   |   |-- ColorPicker.cs
|   |   |-- ColorPickerColorMath.cs
|   |   |-- ColorPickerTemplates.cs
|   |   |-- ColorSpectrum.cs
|   |   |-- ColorSwatch.cs
|   |   |-- ColorSwatchTemplates.cs
|   |   |-- ComboBox.cs
|   |   |-- ComboBoxItem.cs
|   |   |-- ComboBoxTemplates.cs
|   |   |-- ComboBoxTextMatcher.cs
|   |   |-- ContentControl.cs
|   |   |-- ContentPresenter.cs
|   |   |-- Control.cs
|   |   |-- ControlTextFont.cs
|   |   |-- Decorator.cs
|   |   |-- Image.cs
|   |   |-- InkCanvas.cs
|   |   |-- InkCanvasEventArgs.cs
|   |   |-- IRenderSurfaceResourceOwner.cs
|   |   |-- ISceneSpatialParticipant2D.cs
|   |   |-- IScrollInfo.cs
|   |   |-- ItemsControl.cs
|   |   |-- ItemsPresenter.cs
|   |   |-- ITextInputHost.cs
|   |   |-- Label.cs
|   |   |-- ListBox.cs
|   |   |-- ListBoxItem.cs
|   |   |-- Menu.cs
|   |   |-- MenuBar.cs
|   |   |-- MenuItem.cs
|   |   |-- MenuItemContainerPolicy.cs
|   |   |-- MenuSession.cs
|   |   |-- MenuTemplates.cs
|   |   |-- MoveCollisionResult2D.cs
|   |   |-- Overlay.cs
|   |   |-- OverlayDismissScope.cs
|   |   |-- OverlayManager.cs
|   |   |-- OverlayPlacement.cs
|   |   |-- Panel.cs
|   |   |-- PasswordBox.cs
|   |   |-- PolygonCollider2D.cs
|   |   |-- PopupRoot.cs
|   |   |-- ProgressBar.cs
|   |   |-- RadioButton.cs
|   |   |-- RenderSurface2D.cs
|   |   |-- RenderSurface2D.Presentation.cs
|   |   |-- RenderSurface2DFrame.cs
|   |   |-- RenderSurface2DFrame.Images.cs
|   |   |-- RenderSurface2DFrame.Shapes.cs
|   |   |-- RenderSurface2DFrame.Text.cs
|   |   |-- RenderSurface2DRedrawMode.cs
|   |   |-- RenderSurface3D.cs
|   |   |-- RenderSurface3DFrame.cs
|   |   |-- RenderSurface3DRedrawMode.cs
|   |   |-- ResizeMode.cs
|   |   |-- Scene2D.cs
|   |   |-- Scene2DDebugOverlay.cs
|   |   |-- Scene2DDocument.cs
|   |   |-- Scene2DModelValidator.cs
|   |   |-- Scene2DRecordContext.cs
|   |   |-- SceneChildBoundsIndex2D.cs
|   |   |-- SceneGeometry2D.cs
|   |   |-- SceneHitTest2D.cs
|   |   |-- SceneItems2D.cs
|   |   |-- SceneJsonValue2D.cs
|   |   |-- SceneNode2D.cs
|   |   |-- SceneOrderMode.cs
|   |   |-- ScenePreparationRequest2D.cs
|   |   |-- SceneSimulationContext2D.cs
|   |   |-- SceneSpatialResidency2D.cs
|   |   |-- SceneSpatialSource2D.cs
|   |   |-- ScrollBarVisibility.cs
|   |   |-- ScrollChangedEventArgs.cs
|   |   |-- ScrollContentPresenter.cs
|   |   |-- ScrollViewer.cs
|   |   |-- ScrollViewerTemplates.cs
|   |   |-- SegmentCollider2D.cs
|   |   |-- Slider.cs
|   |   |-- SliderTemplates.cs
|   |   |-- SparseCollisionGrid2D.cs
|   |   |-- Sprite2D.cs
|   |   |-- SpriteAnimation.cs
|   |   |-- StackPanel.cs
|   |   |-- SvgImage.cs
|   |   |-- TabControl.cs
|   |   |-- TabItem.cs
|   |   |-- TextBlock.cs
|   |   |-- TextBox.cs
|   |   |-- TextChangedEventArgs.cs
|   |   |-- TextInputCore.cs
|   |   |-- TextInputPolicy.cs
|   |   |-- TextInputViewport.cs
|   |   |-- TextSearch.cs
|   |   |-- Tile.cs
|   |   |-- TileColliderDescriptor2D.cs
|   |   |-- TileFlipGeometry2D.cs
|   |   |-- TileMap2D.Cache.cs
|   |   |-- TileMap2D.Collision.cs
|   |   |-- TileMap2D.cs
|   |   |-- TileMap2D.Streaming.cs
|   |   |-- TileMap2D.WarmCache.cs
|   |   |-- TileMap2DDiagnosticsSnapshot.cs
|   |   |-- TileMap2DModel.cs
|   |   |-- TileMapCatalog2D.cs
|   |   |-- TileMapSource2D.cs
|   |   |-- TileStaticCollider2D.cs
|   |   |-- ToolTip.cs
|   |   |-- UserControl.cs
|   |   |-- Window.cs
|   |   |-- WindowClosingEventArgs.cs
|   |   |-- WindowStartupLocation.cs
|   |   +-- WindowState.cs
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
|   |   |-- UiPropertyMutation.cs
|   |   |-- UiPropertyMutationObserver.cs
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
|   |   |-- ObservableListChangedEventArgs.cs
|   |   |-- ObservableListChangedEventArgs{T}.cs
|   |   |-- ObservableListChangeKind.cs
|   |   |-- ObservableValue{T}.cs
|   |   |-- PropertyAdapter{TOwner,TValue}.cs
|   |   |-- SortDescription{T}.cs
|   |   |-- StringPropertyPath.cs
|   |   +-- UiPropertyBinding{T}.cs
|   |-- Detective/
|   |   |-- AspectConditionTrace.cs
|   |   |-- AspectDiagnostics.cs
|   |   |-- AspectEngineCounters.cs
|   |   |-- AspectResolutionStep.cs
|   |   |-- AspectTokenTrace.cs
|   |   |-- AspectTrace.cs
|   |   |-- DebugAdorner.cs
|   |   |-- DebugOverlay.cs
|   |   |-- Detective.cs
|   |   |-- DirtyTreeDumper.cs
|   |   |-- ElementTreeDumper.cs
|   |   |-- FrameDiagnostics.cs
|   |   |-- InputDiagnostics.cs
|   |   |-- InvalidationTrace.cs
|   |   |-- LayoutDiagnostics.cs
|   |   |-- MotionDiagnostics.cs
|   |   |-- MotionGraphSnapshot.cs
|   |   |-- MotionTrace.cs
|   |   |-- MotionTraceEvent.cs
|   |   |-- PrismRendererDiagnostics.cs
|   |   |-- RenderCacheDumper.cs
|   |   |-- RenderDiagnostics.cs
|   |   |-- RoutedEventTrace.cs
|   |   |-- TileMapDiagnosticsSnapshot.cs
|   |   +-- TimbreDiagnosticsSnapshot.cs
|   |-- Drawing/
|   |   +-- DrawingContentServices.cs
|   |-- Elements/
|   |   |-- ElementChildRole.cs
|   |   |-- ElementHandlerStore.cs
|   |   |-- ElementIdProvider.cs
|   |   |-- ElementLifecycle.cs
|   |   |-- ElementTreeChange.cs
|   |   |-- ElementTreeChangeKind.cs
|   |   |-- ElementTreeWalker.cs
|   |   |-- ElementVisualTransform.cs
|   |   |-- IElementChildHost.cs
|   |   |-- IElementHost.cs
|   |   |-- IElementLifecycleBehavior.cs
|   |   |-- InheritedPropertyPropagator.cs
|   |   |-- RootPropertyMutationObserver.cs
|   |   |-- UIElement.cs
|   |   |-- UIElement.Events.cs
|   |   |-- UIElement.InputEvents.cs
|   |   |-- UIElement.Timbre.cs
|   |   |-- UIElementCollection.cs
|   |   |-- UIElementVisibility.cs
|   |   |-- UIRoot.cs
|   |   |-- UIRoot.Subscriptions.cs
|   |   +-- UIRoot.Timbre.cs
|   |-- Hosting/
|   |   |-- Windowing/
|   |   |   |-- ApplicationBackendAttribute.cs
|   |   |   |-- DesignPreviewSession.cs
|   |   |   |-- GeneratedWindowApplication.cs
|   |   |   |-- IWindowPlatform.cs
|   |   |   |-- WindowApplicationRuntime.cs
|   |   |   |-- WindowingBackendRegistry.cs
|   |   |   +-- WindowScreenshotRegion.cs
|   |   |-- BackdropFrameCounters.cs
|   |   |-- IUiBackend.cs
|   |   |-- IUiClock.cs
|   |   |-- PrismOperationalDiagnostics.cs
|   |   |-- UiCoordinateMapper.cs
|   |   |-- UiFrame.cs
|   |   |-- UiHost.cs
|   |   |-- UiHostOptions.cs
|   |   +-- UiViewport.cs
|   |-- Ink/
|   |   |-- Stroke.cs
|   |   |-- StrokeCollection.cs
|   |   |-- StrokeCollectionChangedEventArgs.cs
|   |   +-- StrokeCollectionChangeKind.cs
|   |-- Input/
|   |   |-- ActionCommand.cs
|   |   |-- CanExecuteRoutedEventArgs.cs
|   |   |-- ClickTracker.cs
|   |   |-- CommandBinding.cs
|   |   |-- CommandBindingCollection.cs
|   |   |-- CommandEvents.cs
|   |   |-- CommandRouter.cs
|   |   |-- CommandSourceState.cs
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
|   |   |-- IGeometricHitTestHost.cs
|   |   |-- IInputActivatable.cs
|   |   |-- IInputCommandSource.cs
|   |   |-- IInputCoordinateSpace.cs
|   |   |-- IInputPressable.cs
|   |   |-- IInputRouteGuard.cs
|   |   |-- IInputSource.cs
|   |   |-- IInputSubtreeHost.cs
|   |   |-- InputBinding.cs
|   |   |-- InputBindingCollection.cs
|   |   |-- InputButtonState.cs
|   |   |-- InputCoordinateConverter.cs
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
|   |   |-- RepeatButtonController.cs
|   |   |-- RetainedInputBindingProcessor.cs
|   |   |-- RoutedCommand.cs
|   |   |-- RoutedCommandContext.cs
|   |   |-- RoutedEvent.cs
|   |   |-- RoutedEventArgs.cs
|   |   |-- RoutedEventRegistry.cs
|   |   |-- RoutedEventRouter.cs
|   |   |-- RoutedPropertyChangedEventArgs.cs
|   |   |-- RoutingStrategy.cs
|   |   |-- StylusEventArgs.cs
|   |   |-- StylusInputAction.cs
|   |   |-- StylusInputBridge.cs
|   |   |-- StylusInputFrame.cs
|   |   |-- StylusInputPoint.cs
|   |   |-- TextCompositionEventArgs.cs
|   |   |-- TextInputBridge.cs
|   |   |-- TextInputSnapshotEvent.cs
|   |   |-- TouchInputBridge.cs
|   |   |-- UiElementId.cs
|   |   |-- UiInputElement.cs
|   |   +-- UiInputTree.cs
|   |-- Invalidation/
|   |   |-- AspectQueue.cs
|   |   |-- CommandStateQueue.cs
|   |   |-- DirtyPropagation.cs
|   |   |-- DirtyState.cs
|   |   |-- ElementQueueOrderIndex.cs
|   |   |-- ElementWorkQueue.cs
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
|   |   +-- UiFrameScheduler.cs
|   |-- Layout/
|   |   |-- Panels/
|   |   |   |-- Canvas.cs
|   |   |   |-- ColumnDefinition.cs
|   |   |   |-- Grid.cs
|   |   |   |-- GridDefinitionCollection{TDefinition}.cs
|   |   |   |-- GridLength.cs
|   |   |   |-- Panel.cs
|   |   |   |-- RowDefinition.cs
|   |   |   |-- StackPanel.cs
|   |   |   +-- VirtualizingStackPanel.cs
|   |   |-- Virtualization/
|   |   |   |-- IItemsVirtualizingPanel.cs
|   |   |   |-- ItemsVirtualizationViewport.cs
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
|   |   |-- Orientation.cs
|   |   |-- Thickness.cs
|   |   +-- Visibility.cs
|   |-- Markup/
|   |   |-- BrushMarkupReader.cs
|   |   |-- ContentPropertyAttribute.cs
|   |   |-- DesignTimeOnlyAttribute.cs
|   |   |-- GeneratedMarkupBindings.cs
|   |   |-- GeneratedMarkupConditions.cs
|   |   |-- GeneratedMarkupMotion.cs
|   |   |-- GeneratedMarkupPrism.cs
|   |   |-- GeneratedMarkupResources.cs
|   |   |-- GeneratedMarkupTimbre.cs
|   |   |-- GeneratedUiFactory.cs
|   |   |-- MarkupDiagnostic.cs
|   |   |-- MarkupLoadOptions.cs
|   |   |-- MarkupMotionExecution.cs
|   |   |-- MarkupResult{T}.cs
|   |   |-- MarkupValueConstraintAttribute.cs
|   |   |-- UiFactory.cs
|   |   |-- UiMarkupAttribute.cs
|   |   |-- UiMarkupChildContent.cs
|   |   |-- UiMarkupContent.cs
|   |   |-- UiMarkupDocument.cs
|   |   |-- UiMarkupElementRegistration.cs
|   |   |-- UiMarkupNode.cs
|   |   |-- UiMarkupPropertyRegistration.cs
|   |   |-- UiMarkupReader.cs
|   |   |-- UiMarkupSchema.cs
|   |   |-- UiMarkupTextContent.cs
|   |   |-- UiMarkupTypeRegistry.cs
|   |   +-- UiMarkupWriter.cs
|   |-- Media/
|   |   |-- BitmapImage.cs
|   |   |-- Brush.cs
|   |   |-- DrawingBrush.cs
|   |   |-- EllipseGeometry.cs
|   |   |-- Geometry.cs
|   |   |-- GradientStop.cs
|   |   |-- ImageBrush.cs
|   |   |-- ImageSource.cs
|   |   |-- LinearGradientBrush.cs
|   |   |-- Matrix3x2.cs
|   |   |-- PathGeometry.cs
|   |   |-- Pen.cs
|   |   |-- RadialGradientBrush.cs
|   |   |-- RectangleGeometry.cs
|   |   |-- RenderTargetImage.cs
|   |   |-- SolidColorBrush.cs
|   |   |-- SvgGeometry.cs
|   |   |-- TileBrush.cs
|   |   |-- Transform.cs
|   |   +-- VisualBrush.cs
|   |-- Motion/
|   |   |-- Core/
|   |   |   |-- DerivedMotionValue{T}.cs
|   |   |   |-- IMotionClock.cs
|   |   |   |-- IReducedMotionSource.cs
|   |   |   |-- ManualMotionTimeline.cs
|   |   |   |-- MotionCancellation.cs
|   |   |   |-- MotionChannel.cs
|   |   |   |-- MotionCompletionSource.cs
|   |   |   |-- MotionComposition.cs
|   |   |   |-- MotionConflictResolver.cs
|   |   |   |-- MotionFrame.cs
|   |   |   |-- MotionFrameCoordinator.cs
|   |   |   |-- MotionFramePhase.cs
|   |   |   |-- MotionFrameResult.cs
|   |   |   |-- MotionGraph.cs
|   |   |   |-- MotionGroup.cs
|   |   |   |-- MotionGroupHandle.cs
|   |   |   |-- MotionHandle.cs
|   |   |   |-- MotionNode.cs
|   |   |   |-- MotionPriority.cs
|   |   |   |-- MotionSequence.cs
|   |   |   |-- MotionStagger.cs
|   |   |   |-- MotionStartOptions.cs
|   |   |   |-- MotionSystem.cs
|   |   |   |-- MotionTimeline.cs
|   |   |   |-- MotionTimelineRegistry.cs
|   |   |   |-- MotionValue.cs
|   |   |   |-- MotionValue{T}.cs
|   |   |   |-- ReducedMotionMode.cs
|   |   |   |-- ReducedMotionPolicy.cs
|   |   |   +-- SystemMotionClock.cs
|   |   |-- Input/
|   |   |   |-- DragMotionController.cs
|   |   |   |-- GestureMotionController.cs
|   |   |   |-- MotionRange.cs
|   |   |   |-- PointerMotionState.cs
|   |   |   |-- ScrollMotionBinding.cs
|   |   |   |-- ScrollTimeline.cs
|   |   |   +-- VelocityTracker.cs
|   |   |-- Interpolation/
|   |   |   |-- BrushMixer.cs
|   |   |   |-- ColorMixer.cs
|   |   |   |-- DoubleMixer.cs
|   |   |   |-- DrawPointMixer.cs
|   |   |   |-- DrawRectMixer.cs
|   |   |   |-- DrawSizeMixer.cs
|   |   |   |-- FloatMixer.cs
|   |   |   |-- IValueMixer.cs
|   |   |   |-- IValueMixerDispatcher.cs
|   |   |   |-- ThicknessMixer.cs
|   |   |   |-- TransformComponents.cs
|   |   |   |-- TransformInterpolationMode.cs
|   |   |   |-- TransformMixer.cs
|   |   |   |-- ValueMixer.cs
|   |   |   |-- ValueMixerRegistry.cs
|   |   |   +-- Vector4Mixer.cs
|   |   |-- Layout/
|   |   |   |-- LayoutMotionBinding.cs
|   |   |   |-- LayoutMotionCoordinator.cs
|   |   |   |-- LayoutMotionId.cs
|   |   |   |-- LayoutMotionOptions.cs
|   |   |   +-- LayoutSnapshot.cs
|   |   |-- Presence/
|   |   |   |-- PresenceCoordinator.cs
|   |   |   |-- PresenceOptions.cs
|   |   |   +-- PresenceState.cs
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
|   |   |   |-- PingPongSpec.cs
|   |   |   |-- RepeatSpec.cs
|   |   |   |-- RetargetMode.cs
|   |   |   |-- SpringSpec.cs
|   |   |   |-- SpringVelocityMode.cs
|   |   |   |-- StepEasing.cs
|   |   |   |-- TransformSpringSampler.cs
|   |   |   +-- TweenSpec.cs
|   |   |-- States/
|   |   |   |-- MotionStateRule.cs
|   |   |   |-- MotionTokens.cs
|   |   |   |-- MotionVisualStateController.cs
|   |   |   |-- MotionVisualStateSnapshot.cs
|   |   |   +-- ThemeMotionTokens.cs
|   |   |-- Transactions/
|   |   |   |-- MotionTransaction.cs
|   |   |   |-- MotionTransactionContext.cs
|   |   |   |-- MotionTransactionOptions.cs
|   |   |   +-- MotionTransactionScope.cs
|   |   |-- MotionAnimationBuilder.cs
|   |   |-- MotionDefaults.cs
|   |   |-- MotionElementFacade.cs
|   |   |-- MotionExtensions.cs
|   |   |-- MotionProperty.cs
|   |   |-- MotionPropertyShortcut.cs
|   |   |-- MotionStateBuilder.cs
|   |   |-- MotionStateTargetBuilder.cs
|   |   |-- ObjectMotionAnimationBuilder.cs
|   |   |-- ObjectMotionExpressionPropertyCache.cs
|   |   |-- ObjectMotionFacade.cs
|   |   +-- ObjectMotionRuntime.cs
|   |-- Platform/
|   |   |-- CursorShape.cs
|   |   |-- FileDialogFilter.cs
|   |   |-- FileDialogOptions.cs
|   |   |-- IAccessibilityPlatform.cs
|   |   |-- IClipboard.cs
|   |   |-- ICursorService.cs
|   |   |-- IDpiProvider.cs
|   |   |-- IFileDialogService.cs
|   |   |-- IPlatformServices.cs
|   |   |-- ITextInputPlatform.cs
|   |   +-- PlatformServices.cs
|   |-- Prism/
|   |   |-- Definitions/
|   |   |   |-- PrismClipDefinition.cs
|   |   |   |-- PrismColorMatrixResource.cs
|   |   |   |-- PrismCurvePoint.cs
|   |   |   |-- PrismDefinitionValidation.cs
|   |   |   |-- PrismFilterDefinition.cs
|   |   |   |-- PrismGradientMapResource.cs
|   |   |   |-- PrismGroupDefinition.cs
|   |   |   |-- PrismLayerDefinition.cs
|   |   |   |-- PrismLensProfileJson.cs
|   |   |   |-- PrismLensProfileResource.cs
|   |   |   |-- PrismLightingResource.cs
|   |   |   |-- PrismMaskDefinition.cs
|   |   |   |-- PrismNodeDefinition.cs
|   |   |   |-- PrismNodeId.cs
|   |   |   |-- PrismParameterKey{T}.cs
|   |   |   |-- PrismResourceId.cs
|   |   |   |-- PrismSourceSpan.cs
|   |   |   +-- PrismStyleDefinition.cs
|   |   +-- Runtime/
|   |       |-- PrismAdvancedBlend.cs
|   |       |-- PrismAttachment.cs
|   |       |-- PrismCatalogParameterValidation.cs
|   |       |-- PrismInstance.cs
|   |       |-- PrismParameterStore.cs
|   |       |-- PrismStates.cs
|   |       +-- PrismVersions.cs
|   |-- Relay/
|   |   |-- CapturedUiThreadAccess.cs
|   |   |-- IUiThreadAccess.cs
|   |   |-- UiRelay.cs
|   |   |-- UiRelayDrainResult.cs
|   |   |-- UiRelayOptions.cs
|   |   |-- UiRelayRefreshDispatcher.cs
|   |   +-- UiRelaySynchronizationContext.cs
|   |-- Rendering/
|   |   |-- ClipNode.cs
|   |   |-- DrawCommandListBuilder.cs
|   |   |-- DrawCommandListPool.cs
|   |   |-- DrawCommandTransform.cs
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
|   |   |-- FontResource.cs
|   |   |-- IAsyncImageLoader.cs
|   |   |-- IImageLoader.cs
|   |   |-- ImageReference.cs
|   |   |-- ImageResource.cs
|   |   |-- ImageResourceCache.cs
|   |   |-- ImageResourceLease.cs
|   |   |-- ImageResourceLeaseSet.cs
|   |   |-- ImageResourceResolutionEpoch.cs
|   |   |-- ImageResourceResolver.cs
|   |   |-- IObservableResourceProvider.cs
|   |   |-- IResourceProvider.cs
|   |   |-- ResourceChangedEventArgs.cs
|   |   |-- ResourceDependencyTracker.cs
|   |   |-- ResourceDictionary.cs
|   |   |-- ResourceId{T}.cs
|   |   +-- ResourceStore.cs
|   |-- Servo/
|   |   |-- IServoInputDriver.cs
|   |   |-- RetainedServoInputDriver.cs
|   |   |-- Servo.cs
|   |   |-- ServoActionEngine.cs
|   |   |-- ServoCaptureEngine.cs
|   |   |-- ServoCondition.cs
|   |   |-- ServoContext.cs
|   |   |-- ServoElement.cs
|   |   |-- ServoException.cs
|   |   |-- ServoInputSequence.cs
|   |   |-- ServoInputState.cs
|   |   |-- ServoModifiers.cs
|   |   |-- ServoOperation.cs
|   |   |-- ServoOptions.cs
|   |   |-- ServoPoint.cs
|   |   |-- ServoQueryEngine.cs
|   |   |-- ServoSynchronization.cs
|   |   +-- ServoTarget.cs
|   |-- Text/
|   |   |-- BidiTextRun.cs
|   |   |-- BidiTextService.cs
|   |   |-- FontResolver.cs
|   |   |-- LineBreakService.cs
|   |   |-- ResolvedTextFont.cs
|   |   |-- TextAspect.cs
|   |   |-- TextCaret.cs
|   |   |-- TextCaretLayout.cs
|   |   |-- TextCompositionManager.cs
|   |   |-- TextCompositionState.cs
|   |   |-- TextDirection.cs
|   |   |-- TextDocument.cs
|   |   |-- TextEditingController.cs
|   |   |-- TextEditor.cs
|   |   |-- TextEditorSnapshot.cs
|   |   |-- TextLayoutCache.cs
|   |   |-- TextLayoutKey.cs
|   |   |-- TextLine.cs
|   |   |-- TextLineMetrics.cs
|   |   |-- TextMeasurer.cs
|   |   |-- TextMeasureResult.cs
|   |   |-- TextRenderer.cs
|   |   |-- TextSelection.cs
|   |   |-- TextTrimming.cs
|   |   |-- TextWrapping.cs
|   |   +-- UndoRedoStack.cs
|   |-- Theming/
|   |   |-- DefaultTheme.cs
|   |   |-- Theme.cs
|   |   |-- ThemeKey{T}.cs
|   |   |-- ThemePalette.cs
|   |   |-- ThemeProvider.cs
|   |   +-- ThemeResource.cs
|   |-- Timbre/
|   |   |-- ElementTimbreOwner.cs
|   |   |-- PlatformTimbreOutput.cs
|   |   |-- TimbreAttachment.cs
|   |   |-- TimbreMotionFacade.cs
|   |   +-- TimbrePlaybackMotion.cs
|   |-- Application.cs
|   |-- ApplicationExitEventArgs.cs
|   |-- ApplicationShutdownMode.cs
|   +-- ApplicationStartupEventArgs.cs
|-- .gitattributes
|-- .gitignore
|-- .mcp.json
|-- Cerneala.csproj
|-- Cerneala.slnx
|-- CLAUDE.md
|-- GameBootstrap.cs
|-- global.json
|-- LICENSE
|-- NuGet.Config
|-- README.md
+-- ROADMAP.md
```

