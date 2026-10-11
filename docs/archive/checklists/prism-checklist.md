# Prism — checklist fisiere C#

> Arhivat la 2026-10-10: inventar terminat, fără sarcini deschise. Sursa actuală pentru API: [manifest.json](../../../docs-site/documentation/manifest.json).

312 fisiere din inventarul implementarii dedicate Prism.

Bifele marcheaza fisiere inspectate si rezolvate (inclusiv pastrate fara modificari
justificate), dupa verificarea focalizata a lotului. Suita completa ramane pentru
finalul intregului target, conform acordului din 2026-09-30.

Checkpoint final 2026-10-01: 312/312 rezolvate. Verificarea automata Windows a
incheiat cu 6546 teste Passed, 0 Failed si 7 skip-uri existente, documentate mai jos.

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Backends.SdlGpu\Gpu
- [x] SdlGpuPrismFrameCounters.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Backends.SdlGpu\Prism
- [x] ISdlGpuBackdropFrameLease.cs
- [x] SdlGpuPrismDeviceResources.cs
- [x] SdlGpuPrismExecutionColdStartWarmup.cs
- [x] SdlGpuPrismExecutor.cs
- [x] SdlGpuPrismKernelSelector.cs
- [x] SdlGpuPrismUniforms.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Language\Prism\Catalog
- [x] PrismLanguageCatalog.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Language\Semantics
- [x] CernealaSemanticModel.MotionPrism.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.Language\Syntax\Embedded
- [x] PrismModelSyntax.cs
- [x] PrismSyntaxParser.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism
- [x] PrismCatalogCompiler.cs
- [x] PrismCatalogGenerator.cs
- [x] PrismOperationSourceEmitter.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism\Binding
- [x] BoundPrismModel.cs
- [x] PrismMarkupBinder.cs
- [x] PrismMotionResolver.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism\Emission
- [x] PrismMarkupEmitter.cs
- [x] PrismMotionEmitter.cs

## C:\Users\lauri\Desktop\Cerneala\Cerneala.SourceGen\Prism\Syntax
- [x] PrismDirectiveParser.cs
- [x] PrismMarkupLanguage.cs
- [x] PrismSyntax.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism
- [x] BackdropAlphaMode.cs
- [x] BackdropFrameMetadata.cs
- [x] BackdropFrameRequest.cs
- [x] BackdropPixelFormat.cs
- [x] IBackdropFrameLease.cs
- [x] IBackdropFrameSource.cs
- [x] Prism.cs
- [x] PrismBackdropSourceToken.cs
- [x] PrismCacheInvalidationQueue.cs
- [x] PrismCacheOwnerTokenAllocator.cs
- [x] PrismDrawResources.cs
- [x] PrismDrawScope.cs
- [x] PrismEnumValidation.cs
- [x] PrismImage.cs
- [x] PrismOperation.cs
- [x] PrismPipeline.cs
- [x] PrismRendererOptions.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Blend
- [x] PrismAdvancedBlendingStyle.cs
- [x] PrismBlendIfStyle.cs
- [x] PrismBlendMath.cs
- [x] PrismColorBlend.cs
- [x] PrismColorBurnBlend.cs
- [x] PrismColorDodgeBlend.cs
- [x] PrismDarkenBlend.cs
- [x] PrismDarkerColorBlend.cs
- [x] PrismDifferenceBlend.cs
- [x] PrismDissolveBlend.cs
- [x] PrismDivideBlend.cs
- [x] PrismExclusionBlend.cs
- [x] PrismHardLightBlend.cs
- [x] PrismHardMixBlend.cs
- [x] PrismHueBlend.cs
- [x] PrismLightenBlend.cs
- [x] PrismLighterColorBlend.cs
- [x] PrismLinearBurnBlend.cs
- [x] PrismLinearDodgeBlend.cs
- [x] PrismLinearLightBlend.cs
- [x] PrismLuminosityBlend.cs
- [x] PrismMultiplyBlend.cs
- [x] PrismNormalBlend.cs
- [x] PrismOverlayBlend.cs
- [x] PrismPassThroughBlend.cs
- [x] PrismPinLightBlend.cs
- [x] PrismSaturationBlend.cs
- [x] PrismScreenBlend.cs
- [x] PrismSoftLightBlend.cs
- [x] PrismSubtractBlend.cs
- [x] PrismVividLightBlend.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Catalog
- [x] PrismCatalog.cs
- [x] PrismFallbackPolicy.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Color
- [x] PrismColorPipeline.cs
- [x] PrismDisplayP3Style.cs
- [x] PrismLinearDisplayP3Style.cs
- [x] PrismLinearSrgbStyle.cs
- [x] PrismOklab.cs
- [x] PrismScRgbStyle.cs
- [x] PrismSrgbStyle.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Execution
- [x] PrismExecutionDiagnostics.cs
- [x] PrismGraphFallbackTracker.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Filters
- [x] PrismAccentedEdgesFilter.cs
- [x] PrismAdaptiveWideAngleFilter.cs
- [x] PrismAddNoiseFilter.cs
- [x] PrismAdjustmentMath.cs
- [x] PrismAdjustmentPlanner.cs
- [x] PrismAngledStrokesFilter.cs
- [x] PrismArbitraryFlatMorphology.cs
- [x] PrismAverageFilter.cs
- [x] PrismBasReliefFilter.cs
- [x] PrismBlackWhiteFilter.cs
- [x] PrismBlurFilter.cs
- [x] PrismBlurMoreFilter.cs
- [x] PrismBoxBlurFilter.cs
- [x] PrismBrightnessContrastFilter.cs
- [x] PrismCatalogArtisticMath.cs
- [x] PrismCatalogColorMath.cs
- [x] PrismCatalogFilterMath.cs
- [x] PrismCatalogFilterPlanner.cs
- [x] PrismCatalogGeometryMath.cs
- [x] PrismCatalogInkMath.cs
- [x] PrismCatalogPainterlyMath.cs
- [x] PrismCatalogProceduralMath.cs
- [x] PrismCatalogQuantizationMath.cs
- [x] PrismCatalogReliefMath.cs
- [x] PrismCatalogTextureMath.cs
- [x] PrismChalkCharcoalFilter.cs
- [x] PrismChannelMixerFilter.cs
- [x] PrismCharcoalFilter.cs
- [x] PrismChromaticAberrationFilter.cs
- [x] PrismChromeFilter.cs
- [x] PrismCloudsFilter.cs
- [x] PrismColorBalanceFilter.cs
- [x] PrismColoredPencilFilter.cs
- [x] PrismColorFilter.cs
- [x] PrismColorHalftoneFilter.cs
- [x] PrismColorLookupFilter.cs
- [x] PrismColorMatrixFilter.cs
- [x] PrismConteCrayonFilter.cs
- [x] PrismCraquelureFilter.cs
- [x] PrismCrosshatchFilter.cs
- [x] PrismCrystallizeFilter.cs
- [x] PrismCurveLut.cs
- [x] PrismCurvesFilter.cs
- [x] PrismCustomConvolutionFilter.cs
- [x] PrismCutoutFilter.cs
- [x] PrismDarkStrokesFilter.cs
- [x] PrismDeinterlaceFilter.cs
- [x] PrismDespeckleFilter.cs
- [x] PrismDifferenceCloudsFilter.cs
- [x] PrismDiffuseFilter.cs
- [x] PrismDiffuseGlowFilter.cs
- [x] PrismDisplaceFilter.cs
- [x] PrismDryBrushFilter.cs
- [x] PrismDustScratchesFilter.cs
- [x] PrismEmbossFilter.cs
- [x] PrismExposureFilter.cs
- [x] PrismExtrudeFilter.cs
- [x] PrismFacetFilter.cs
- [x] PrismFibersFilter.cs
- [x] PrismFibersNoise.cs
- [x] PrismFieldBlurFilter.cs
- [x] PrismFilmGrainFilter.cs
- [x] PrismFilterConformanceGallery.cs
- [x] PrismFilterParameterReader.cs
- [x] PrismFindEdgesFilter.cs
- [x] PrismFragmentFilter.cs
- [x] PrismFrescoFilter.cs
- [x] PrismGaussianBlurFilter.cs
- [x] PrismGlassFilter.cs
- [x] PrismGlowingEdgesFilter.cs
- [x] PrismGradientMapFilter.cs
- [x] PrismGradientMapLut.cs
- [x] PrismGrainFilter.cs
- [x] PrismGraphicPenFilter.cs
- [x] PrismHaldLut.cs
- [x] PrismHalftonePatternFilter.cs
- [x] PrismHighPassFilter.cs
- [x] PrismHueSaturationFilter.cs
- [x] PrismIncrementalVoronoiSet.cs
- [x] PrismInkOutlinesFilter.cs
- [x] PrismInvertFilter.cs
- [x] PrismIrisBlurFilter.cs
- [x] PrismLensBlurFilter.cs
- [x] PrismLensCorrectionFilter.cs
- [x] PrismLensFlareFilter.cs
- [x] PrismLensFlareRenderer.cs
- [x] PrismLensProfileFitter.cs
- [x] PrismLevelsAnalysis.cs
- [x] PrismLevelsFilter.cs
- [x] PrismLightingEffectsFilter.cs
- [x] PrismLiquifyFilter.cs
- [x] PrismMaximumFilter.cs
- [x] PrismMedianFilter.cs
- [x] PrismMezzotintFilter.cs
- [x] PrismMezzotintThreshold.cs
- [x] PrismMinimumFilter.cs
- [x] PrismMosaicFilter.cs
- [x] PrismMosaicTilesFilter.cs
- [x] PrismMotionBlurFilter.cs
- [x] PrismNeighborhoodMath.cs
- [x] PrismNeighborhoodPlanner.cs
- [x] PrismNeonGlowFilter.cs
- [x] PrismNotePaperFilter.cs
- [x] PrismNtscColorsFilter.cs
- [x] PrismOceanRippleFilter.cs
- [x] PrismOffsetFilter.cs
- [x] PrismOilPaintFilter.cs
- [x] PrismOkhsl.cs
- [x] PrismPaintDaubsFilter.cs
- [x] PrismPaletteKnifeFilter.cs
- [x] PrismPatchworkFilter.cs
- [x] PrismPathBlurFilter.cs
- [x] PrismPhotocopyFilter.cs
- [x] PrismPhotoFilterFilter.cs
- [x] PrismPinchFilter.cs
- [x] PrismPlasterFilter.cs
- [x] PrismPlasticWrapFilter.cs
- [x] PrismPointillizeFilter.cs
- [x] PrismPolarCoordinatesFilter.cs
- [x] PrismPosterEdgesFilter.cs
- [x] PrismPosterizeFilter.cs
- [x] PrismRadialBlurFilter.cs
- [x] PrismRecursiveWangBlueNoise.cs
- [x] PrismReduceNoiseFilter.cs
- [x] PrismResamplingMath.cs
- [x] PrismResamplingPlanner.cs
- [x] PrismReticulationFilter.cs
- [x] PrismRippleFilter.cs
- [x] PrismRoughPastelsFilter.cs
- [x] PrismScanlinesFilter.cs
- [x] PrismSelectiveColorFilter.cs
- [x] PrismShapeBlurFilter.cs
- [x] PrismSharpenEdgesFilter.cs
- [x] PrismSharpenFilter.cs
- [x] PrismSharpenMoreFilter.cs
- [x] PrismShearFilter.cs
- [x] PrismSmartBlurFilter.cs
- [x] PrismSmartSharpenFilter.cs
- [x] PrismSmudgeStickFilter.cs
- [x] PrismSolarizeFilter.cs
- [x] PrismSpatterFilter.cs
- [x] PrismSpherizeFilter.cs
- [x] PrismSpinBlurFilter.cs
- [x] PrismSpongeFilter.cs
- [x] PrismSprayedStrokesFilter.cs
- [x] PrismStainedGlassFilter.cs
- [x] PrismStampFilter.cs
- [x] PrismSumiEFilter.cs
- [x] PrismSurfaceBlurFilter.cs
- [x] PrismSurfaceTexture.cs
- [x] PrismTexturizerFilter.cs
- [x] PrismThresholdAnalysis.cs
- [x] PrismThresholdFilter.cs
- [x] PrismTilesFilter.cs
- [x] PrismTiltShiftFilter.cs
- [x] PrismTornEdgesFilter.cs
- [x] PrismTraceContourFilter.cs
- [x] PrismTransformFilter.cs
- [x] PrismTwirlFilter.cs
- [x] PrismUnderpaintingFilter.cs
- [x] PrismUnsharpMaskFilter.cs
- [x] PrismVibranceFilter.cs
- [x] PrismWatercolorFilter.cs
- [x] PrismWaterPaperFilter.cs
- [x] PrismWaveFilter.cs
- [x] PrismWaveNoise.cs
- [x] PrismWindFilter.cs
- [x] PrismXDogLuminance.cs
- [x] PrismZigZagFilter.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Graph
- [x] PrismAnalyzedScope.cs
- [x] PrismBackdropFramePolicy.cs
- [x] PrismBackdropRequirement.cs
- [x] PrismColdStartWarmup.cs
- [x] PrismDependencyStamp.cs
- [x] PrismFnv1aHash.cs
- [x] PrismFrameAnalysis.cs
- [x] PrismFrameAnalyzer.cs
- [x] PrismGraph.cs
- [x] PrismGraphBuilder.cs
- [x] PrismGraphCapabilities.cs
- [x] PrismGraphDiagnostic.cs
- [x] PrismGraphOptimizer.cs
- [x] PrismInputDependency.cs
- [x] PrismRasterPlanner.cs
- [x] PrismRetainedCacheKey.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Kernels
- [x] PrismKernelKind.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Masking
- [x] PrismClippingStyle.cs
- [x] PrismMaskMath.cs
- [x] PrismMaskStyle.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Styles
- [x] PrismBevelEmbossStyle.cs
- [x] PrismColorOverlayStyle.cs
- [x] PrismCssGradientLut.cs
- [x] PrismDropShadowStyle.cs
- [x] PrismGradientOverlayStyle.cs
- [x] PrismInnerGlowStyle.cs
- [x] PrismInnerShadowStyle.cs
- [x] PrismOuterGlowStyle.cs
- [x] PrismPatternOverlayStyle.cs
- [x] PrismSatinStyle.cs
- [x] PrismStrokeStyle.cs
- [x] PrismStylePlanner.cs

## C:\Users\lauri\Desktop\Cerneala\Drawing\Prism\Surfaces
- [x] PrismSurfaceAllocationException.cs
- [x] PrismSurfaceMemoryAccountant.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Detective
- [x] PrismRendererDiagnostics.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Hosting
- [x] PrismOperationalDiagnostics.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Markup
- [x] GeneratedMarkupPrism.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Prism\Definitions
- [x] PrismColorMatrixResource.cs
- [x] PrismClipDefinition.cs
- [x] PrismCurvePoint.cs
- [x] PrismDefinitionValidation.cs
- [x] PrismFilterDefinition.cs
- [x] PrismGradientMapResource.cs
- [x] PrismGroupDefinition.cs
- [x] PrismLayerDefinition.cs
- [x] PrismLensProfileJson.cs
- [x] PrismLensProfileResource.cs
- [x] PrismLightingResource.cs
- [x] PrismMaskDefinition.cs
- [x] PrismNodeDefinition.cs
- [x] PrismNodeId.cs
- [x] PrismParameterKey{T}.cs
- [x] PrismResourceId.cs
- [x] PrismSourceSpan.cs
- [x] PrismStyleDefinition.cs

## C:\Users\lauri\Desktop\Cerneala\UI\Prism\Runtime
- [x] PrismAdvancedBlend.cs
- [x] PrismAttachment.cs
- [x] PrismCatalogParameterValidation.cs
- [x] PrismInstance.cs
- [x] PrismParameterStore.cs
- [x] PrismStates.cs
- [x] PrismVersions.cs

## Jurnal repo-cleanup

### 2026-09-30 — agregarea contoarelor backendului

- Target: `SdlGpuPrismFrameCounters.cs`, citit integral; inspectate resetarea si
  agregarea din backend, producatorul diagnosticelor si testele consumatorilor.
- Decizie: implementarea ramane neschimbata. Agregarea explicita este coeziva;
  extragerea unui helper sau comprimarea constructorului nu elimina o problema
  concreta de mentenanta. Se pastreaza respingerea null, aritmetica checked,
  sursa numarului de fallback-uri, imutabilitatea si adunarea timpului CPU.
- Acoperire adaugata: `SdlGpuPrismFrameCountersTests.cs`, 10 cazuri pentru toate
  metricile, acumulari succesive, imutabilitate, diagnostice goale, null si overflow.
- Baseline: filtrul `FullyQualifiedName~PrismOperationalDiagnosticsTests`,
  proiectul SDL_GPU, Release: 6/6 trecute.
- Verificare: filtrele `SdlGpuPrismFrameCountersTests`,
  `PrismOperationalDiagnosticsTests`, `SdlGpuSurfaceRetainedTests` si
  `RenderSurface3DStageZeroNativeCharacterizationTests`, cu
  `CERNEALA_SDL_NATIVE_TESTS=1`, Release: 38/38 trecute, fara skip.
- Rezultate brute: `artifacts/prism-cleanup/backend-frame-counters/`
  (`frame-counters-baseline.trx`, `frame-counters-verified.trx`).
- API, algoritmi, executie GPU si documentatie canonica: nemodificate.
  Suita completa a repository-ului nu a fost inca rulata pentru acest cleanup.

### 2026-09-30 — contractul lease-ului SDL_GPU pentru backdrop

- Target: `ISdlGpuBackdropFrameLease.cs`, citit integral, impreuna cu interfata
  generica mostenita; inspectate implementarea reala, consumatorul din executor
  si cazurile de lifetime/retained dependency.
- Decizie: pastrat neschimbat. Interfata adauga doar handle-ul nativ `Texture`
  peste metadata/disposal generice. Unirea interfetelor ar muta detalii SDL in
  nucleul backend-neutral; extragerea unui wrapper nu rezolva o duplicare.
- Verificare: filtrele `PrismBackdropHostingMigrationTests` si
  `PrismRetainedDependencyMigrationTests`, proiectul SDL_GPU, Release,
  `CERNEALA_SDL_NATIVE_TESTS=1`: 16/16 trecute, fara skip. Acopera imprumutul
  targetului activ, expirarea la prezentare, accesul dupa disposal si invalidarea
  dependintelor backdrop; nu s-au schimbat asteptari sau referinte vizuale.
- Rezultat brut:
  `artifacts/prism-cleanup/backend-backdrop-lease/backdrop-lease-verified.trx`.
- Fara modificari de productie/API/documentatie. Verificarea intregului target
  ramane pendinte.

### 2026-09-30 — resurse GPU, promovare pending si contabilizare

- Target: `SdlGpuPrismDeviceResources.cs`, citit integral, inclusiv lease-ul din
  acelasi fisier. Inspectate consumatorii din executor, proprietarul din drawing
  resources, callback-urile command buffer, warmup-ul prin reflection si testele.
- Problema concreta: ramura de promovare pending compara identitatea targetului,
  dar ambele rezultate returneaza; contabilizarea calculeaza de doua ori acelasi
  produs checked. Simplificare: `ContainsKey` si reutilizarea lui `colorBytes`.
- Contract pastrat: prima promovare pentru perechea cheie/token ramane cea
  publicata; lease-ul concurent nu este promovat; pinning, ordinea validarilor,
  exceptiile, bugetele, mip accounting si lifecycle-ul nu au fost mutate.
- Caracterizare adaugata in `PrismSurfaceOwnershipTests`: aceeasi achizitie si
  achizitii diferite la promovarea repetata; confirma targetul pending/submitted,
  lipsa dublarii intrarii si eliberarea pin-urilor. Trecuta inainte de refactorizare.
- Baseline: 108/108. Caracterizare pre-refactorizare: 13/13. Verificare focalizata
  si suite conexe SDL_GPU: 187/187, fara skip; toate cele 108 cazuri baseline
  sunt prezente si trecute in rezultatul ulterior.
- Filtre baseline: `PrismSurfaceOwnershipTests`, `SdlGpuPrismExecutorTests`,
  `SdlGpuPrismBuiltinTextureAllocationTests`, `PrismCurvesTextureMigrationTests`,
  `PrismExecutionBudgetMigrationTests`, `PrismSpatterPointTextureCacheTests`.
  Verificarea adauga `SdlGpuFrameTransactionCharacterizationTests`,
  `PrismRetainedExecutionTests`, `PrismRetainedDependencyMigrationTests` si
  `SdlGpuPrismFrameCountersTests`. Configuratie Release, native tests activate.
- Gates existente de performanta: scenariile warmed simple/styles/nested/chained
  au masurat fiecare 0 octeti alocati in executorul delimitat de test; scenariul
  de 2048 de cadre a trecut gate-urile de reutilizare. Nu se revendica un castig
  de performanta si nici zero alocari pentru intregul frame/runtime.
- Conformance Windows SDL_GPU: 132/132 cazuri Prism resource-free plus 1/1 caz
  Drawing, fara skip. Capturi prin `Window.SaveScreenshot`; 132 rapoarte Prism
  cu referinta/captura/heatmap pastrate, fara modificari ale pragurilor/golden-urilor.
- Rezultate brute: `artifacts/prism-cleanup/backend-device-resources/`, fisierele
  `device-resources-baseline.trx`, `device-resources-characterization.trx`,
  `device-resources-verified.trx`, `device-resources-pixel-conformance.trx`,
  `device-resources-drawing-conformance.trx` si subdirectorul `pixel-diff`.
- Diff revizuit de parinte: numai cele doua simplificari interne si caracterizarea
  suplimentara. Fara API, generator, shader, algoritm sau documentatie canonica
  schimbate. Suita completa si matrix-ul cross-platform nu sunt validate aici.

### 2026-09-30 — warmup-ul metodelor de executie

- Target: `SdlGpuPrismExecutionColdStartWarmup.cs`, citit integral; inspectate
  caller-ul dupa primul submit, warmup-ul separat al grafului si testul ordinii.
- Decizie: pastrat neschimbat. Lazy Task, scheduler-ul explicit si filtrarea
  metodelor generice au o responsabilitate unica. Asemanarea cu scheduler-ul
  warmup-ului core nu justifica un helper nou intre owneri pentru aceasta rutina.
- Acoperire adaugata: `SdlGpuPrismExecutionColdStartWarmupTests.cs`; pornirea si
  asteptarea repetate propaga eventuale erori ale pregatirii reale a metodelor,
  fara reflection pe campuri private sau modificarea starii aplicatiei.
- Baseline: `PrismWarmupRunsOnlyAfterTheFirstFrameIsSubmitted`, 1/1 trecut.
  Verificare: `SdlGpuPrismExecutionColdStartWarmupTests` plus
  `SdlGpuWindowGraphicsSessionTests`, Release, 24/24 trecute, fara skip.
- Rezultate brute: `artifacts/prism-cleanup/backend-execution-warmup/`
  (`execution-warmup-baseline.trx`, `execution-warmup-verified.trx`).
- Nu se revendica un castig de startup sau modificarea scheduling-ului.
  API/implementare productie/documentatie nemodificate; suita completa pendinte.

### 2026-09-30 — executor, selectie kernel si uniforme SDL_GPU

- Target: cele trei fisiere citite integral, impreuna cu testele executorului,
  retained execution, bugetele si consumatorii relevanti. Explorer read-only a
  confirmat caller-ii/lifecycle-ul; parintele a verificat sursa si diff-ul actual.
- Executor: eliminat setul privat `promotedLeases`, doar scris/golit, fara cititori;
  eliminat parametrul callback al `RenderKernel`, null in toate cele sapte apeluri.
  Nu se modifica promovarea din device resources, frame lease disposal, ordinea
  randarii, datele uniformelor, diagnosticarea sau fallback-ul shaderului.
- Reflection: warmup-ul enumera metodelor declarate, fara rezolvare dupa numele
  membrilor eliminati. Lease equality/hash nu modifica starea resurselor. Nu s-au
  identificat consumatori dinamici sau serialized ai acestor membri privati.
- Selectorul si uniformele raman neschimbate: maparea explicita stable ID/kernel
  si buffer-ele reutilizate au owner clar; un helper nou nu reduce mentenanta.
  Layout-ul shaderului ramane 60 float4/960 bytes si 15 sloturi sampler.
- Baseline: 167/167. Dupa cleanup: 183/183, fara skip; toate cazurile baseline
  sunt prezente si trecute. Filtre: `SdlGpuPrismExecutorTests`,
  `PrismRetainedExecutionTests`, `PrismSurfaceOwnershipTests`,
  `PrismExecutionBudgetMigrationTests`, `SdlGpuPrismUniformAllocationTests`,
  `SdlGpuPrismExecutionColdStartWarmupTests`,
  `SdlGpuFrameTransactionCharacterizationTests`; verificarea adauga
  `PrismBackdropHostingMigrationTests` si `PrismRetainedDependencyMigrationTests`.
- Gate-uri warmed existente trecute: zero alocari in regiunea delimitata a
  executorului pentru simple/styles/chained/nested si reutilizare la 2048 cadre.
  Nu se revendica un castig CPU/GPU sau zero alocari pentru intregul frame.
- Conformance/contracte: 171/171, fara skip, inclusiv 132 cazuri Prism si 1 Drawing,
  enum admission si boundary de dependinte. Capturi prin `Window.SaveScreenshot`;
  132 rapoarte pixel-diff pastrate, fara schimbari de golden/praguri.
- Rezultate brute: `artifacts/prism-cleanup/backend-executor/`
  (`executor-baseline.trx`, `executor-verified.trx`, `executor-conformance.trx`,
  `pixel-diff/`). Release, `CERNEALA_SDL_NATIVE_TESTS=1`; `git diff --check` trecut.
- Fara schimbari publice/API, generator, shader sau docs canonice. Suita completa
  a repository-ului si verificarea cross-platform raman pentru final.

### 2026-09-30 — catalog, model syntax si semantica Language

- Target: toate cele patru fisiere Language citite integral (inclusiv partea
  Motion a fisierului comun), plus owner/calleri din semantic model, editor facts,
  diagnostics Language Server si testele de syntax/semantics relevante.
- Problema concreta: clasificarea lexicala a unei valori Prism era duplicata in
  parser si semantic binder pentru atribute XML, cu aceeasi ordine/politica.
  Refolosita implementarea existenta ca metoda interna `PrismSyntaxParser.ClassifyValue`;
  eliminata copia din binder. Validarea tipurilor/domeniilor, binding-urile,
  lookup-ul resurselor, diagnosticele si ordinea executiei raman nemodificate.
- Catalogul si modelul AST sunt pastrate: loader host-agnostic pentru acelasi JSON
  embedded din SourceGen, lookup ordinal si payload-uri syntax/spans explicite.
  Nu se introduce un catalog global nou, dependency sau abstractie de utilitati.
- Caracterizare permanenta: 15 cazuri de clasificare/text/absolute span prin
  parser si 8 cazuri atribut XML versus bloc pentru simbolurile/diagnosticele
  semantice. Toate cele 23 au trecut inainte de modificarile de productie.
- Baseline syntax/semantics: 48/48. Verificare dupa refactorizare: proiectul
  `Cerneala.Tests.Language`, Release, 259 trecute, 0 esuate, 1 skip explicit;
  toate cele 71 cazuri baseline/caracterizare sunt prezente si trecute.
- Skip existent: `WarmCompletionP95StaysBelowBudgetAndIndependentDocumentsDoNotBlock`
  este dezactivat permanent in sursa la cererea maintainer-ului din 2026-09-21;
  mesajul consemneaza un failure de performanta nerezolvat. Nu a fost activat,
  reparat sau ascuns de acest cleanup; bugetul CPU completion nu este verificat.
- Consumator LSP: filtrele `DiagnosticsTests` si `CompletionProtocolTests`,
  proiectul `Cerneala.Tests.LanguageServer`, Release: 11/11, fara skip.
- Rezultate brute: `artifacts/prism-cleanup/language/` (`language-baseline.trx`,
  `language-characterization.trx`, `language-verified.trx`,
  `language-server-verified.trx`). Fara schimbari de asteptari existente.
- Parintele a revizuit sursa actuala, diff-ul, rezultatele si checkpoint-ul;
  `git diff --check` trecut. Fara API public/protected, schema, generator sau docs
  canonice schimbate; suita completa a repository-ului ramane pentru final.

### 2026-09-30 — compilarea catalogului si API-ul generat

- Target: `PrismCatalogCompiler.cs`, `PrismCatalogGenerator.cs` si
  `PrismOperationSourceEmitter.cs`, citite integral; inspectate owner/callerii,
  AdditionalFile-ul din proiect, loader-ul embedded al binder-ului si testele.
- Decizie: pastrate neschimbate. Validarea catalogului, integrarea incrementala
  Roslyn si emiterea claselor de operatii au responsabilitati distincte. Nu exista
  dovada care sa justifice mutarea lor sau o abstractie noua.
- Maparile asemanatoare nu au acelasi contract: simbolurile sunt stocate ca int
  in catalog, dar expuse ca string in operatiile publice. Nu sunt deduplicate.
  Ordinea stabila a descriptorilor, ordinea operatiilor si dependency version
  sunt pastrate; nu se modifica JSON-ul, diagnostics sau codul generat.
- Baseline compiler: 25/25 trecute. Verificare: intregul proiect
  `Cerneala.Tests.SourceGen`, Release, 611/611, fara skip; toate cele 25 cazuri
  baseline sunt prezente si trecute. Consumator runtime: `PrismImageApiTests`
  plus `ObjectMotionTests`, 23/23, fara skip, inclusiv fiecare tip generat,
  defaults/domain validation si descriptorii generici de motion.
- Limita acoperirii: testele runtime exercita assembly-ul generat real, dar nu
  exista un test direct de generator-driver pentru zero/multiple AdditionalFiles.
  Aceste ramuri au fost inspectate, nu declarate validate runtime.
- Rezultate brute: `artifacts/prism-cleanup/sourcegen-catalog/`
  (`catalog-baseline.trx`, `catalog-verified.trx`, `catalog-runtime-verified.trx`).
  Explorer-ul read-only s-a incheiat; parintele a verificat sursa, lipsa diff-ului
  de productie, XML-urile TRX si `git diff --check`. Fara API/docs schimbate;
  suita completa a repository-ului ramane pentru final.

### 2026-09-30 — syntax Prism din SourceGen

- Target: cele trei fisiere Syntax citite integral, plus call path din
  `DirectiveCursor`, resource dispatch, cache-ul per element, semantic gate si
  consumatorii AST. Explorer-ul read-only s-a incheiat; sursa verificata de parinte.
- Decizie: pastrate neschimbate. Lista celor sapte directive, AST-ul cu locatii
  precise si parsarea stream-ului markup au owner clar. Clasificatorul SourceGen
  nu are contract identic cu Language: decimal versus double, null case-insensitive
  versus case-sensitive si numai double quotes versus ambele tipuri de quotes.
  Deduplicarea lor nu este un cleanup behavior-preserving.
- Verificare reutilizata din lotul imediat precedent: `catalog-verified.trx`,
  intregul proiect SourceGen, Release, 611/611, fara skip, inclusiv 57 teste al
  caror nume contine `.Prism`. Nicio sursa de productie sau test nu s-a schimbat
  intre acea executie si acest checkpoint. Testul de clasificare foloseste apel
  reflection direct; fixture-ul valid exercita syntax/binding/emission reale.
- Limita: snapshot-urile de erori prin generator nu dovedesc executarea ramurilor
  parserului SourceGen, deoarece `TryGetEmissionDocument` opreste emiterea dupa
  erori Language. Nu se revendica acoperire directa a tuturor ramurilor parserului.
- Debt separat, nemodificat: textul unknown-directive din
  `PrismDirectiveParser.cs` spune eight, lista efectiva si textul Language spun
  seven. Nu s-a stabilit aici un failure observabil pe ramura veche si nu s-a
  schimbat comportamentul diagnosticelor sub eticheta cleanup.
- Parintele a verificat XML-ul, sursa actuala, lipsa diff-ului de productie si
  `git diff --check`. API/docs nemodificate; suita completa ramane pentru final.

### 2026-09-30 — model bound si markup binder

- Target: `BoundPrismModel.cs` si `PrismMarkupBinder.cs` citite integral; inspectate
  call path din GenerationScope, catalogul resurselor de aplicatie, consumatorii
  emitter/motion si testele relevante. Explorer read-only incheiat.
- Modificare: eliminat getter-ul nefolosit `BoundPrismValue.IsConstant` din clasa
  privata nested. Nu au fost gasite referinte C#, reflection/dynamic sau
  serialization la acest getter; consumatorii folosesc explicit Parameter,
  IsBinding si IsDirectReference. Nu se schimba payload-ul modelului sau emiterea.
- Binder-ul ramane neschimbat: scope-uri lexicale Ordinal, identity/path indexes,
  resurse inaintea aplicatiilor, publicarea rezultatelor fara diagnostics noi,
  conversii/domain validation si politica ClipToBelow au responsabilitati clare.
  Nu se muta validarea in AST sau in utilitati generice.
- Baseline reutilizat: SourceGen `catalog-verified.trx`, 611/611. Dupa eliminarea
  getter-ului, intregul proiect SourceGen, Release: 611/611, fara skip; toate
  cazurile baseline prezente si trecute. Fixture-urile valide includ template
  resource scope, argumente independente, definitie partajata cu instante separate,
  binding/direct references si detach lifecycle.
- Limita: testele negative prin generator pot fi oprite de semantic gate-ul
  Language inaintea binder-ului local; nu sunt pretinse ca acoperire directa a
  tuturor ramurilor de validare din binder. Asteptarile existente nu sunt schimbate.
- Rezultat brut: `artifacts/prism-cleanup/sourcegen-binding/binding-model-verified.trx`.
  Parintele a revizuit sursa curenta, diff-ul de doua linii, TRX-ul si
  `git diff --check`. Fara schimbari public/protected, docs, schema sau algoritmi;
  suita completa ramane pentru final.

### 2026-09-30 — rezolvarea si emiterea Prism Motion

- Target: resolverul si emitterul Motion citite integral; inspectate calleri din
  resolverul Motion comun, tipul rezultat, generarea bound/unbound/set si testele.
  Explorer-ul read-only s-a incheiat; parintele a verificat referintele decisive.
- Modificare: eliminate proprietatile Application si Node, doar setate si fara
  cititori, din clasa privata ResolvedPrismMotionTarget, plus argumentele
  constructorului unic. Argumentele eliminate erau variabile locale, fara
  evaluari cu efecte. PrismMotionAccessor.Node este un membru distinct, folosit
  de emitter, si ramane intact. Nu au fost gasite folosiri reflection/serialization.
- Emitter-ul ramane neschimbat: getter din primul accessor, setter pe tot fan-out-ul
  in ordinea existenta, conversia integer/number, stable ID/slot, discrete writes,
  property ID si named-element code sunt pastrate. Fara abstractii sau output nou.
- Baseline: SourceGen `binding-model-verified.trx`, 611/611. Verificare dupa
  cleanup: intregul proiect SourceGen, Release, 611/611, fara skip; toate cazurile
  baseline sunt prezente si trecute. Include self/owner/named targets, scoped
  number/color, Boolean/enum/set si explicit binding compile/output fixtures.
- Consumator runtime: `PrismMotionIntegrationTests`, 11/11, fara skip, inclusiv
  cancel/detach/replacement, retained versions, 256 navigation cycles si gate-ul
  existent cu zero bytes masurati pe 32 ticks dupa 8 ticks warmup. Testele runtime
  construiesc direct contractul GeneratedMarkup, nu ruleaza generatorul.
- Limite: nu exista in testele inspectate un assertion dedicat integer/number
  conversion sau ordinii multi-accessor fan-out; aceste mecanisme nu sunt editate.
  Negative diagnostics pot veni din semantic gate-ul Language. Nu se revendica
  un castig de performanta sau zero alocari pentru toate frame-urile.
- Rezultate: `artifacts/prism-cleanup/sourcegen-motion/` (`motion-verified.trx`,
  `motion-runtime-verified.trx`). Parintele a revizuit sursa actuala, diff-ul,
  XML-urile si `git diff --check`. Fara API/docs schimbate; suita completa finala
  a repository-ului ramane pendinte.

### 2026-09-30 — emiterea markup Prism

- Target: `PrismMarkupEmitter.cs` citit integral; inspectate binder/model, calleri
  factory/window/user-control/scene-component, bridge-urile Motion partajate si
  testele relevante. Explorer read-only incheiat; parintele a verificat sursa.
- Simplificare: eliminat parametrul isCatalogParameter din wrapper-ul privat
  AddPrismPropertyBindingFactory: toate cele patru apeluri transmiteau false.
  Wrapper-ul transmite acum acelasi false direct metodei comune. Flag-ul din
  AddPrismBindingFactory ramane: parametrii catalogului au contract diferit.
  Ordinea, target expressions, converterele, diagnostics si output-ul se pastreaza.
- Caracterizare permanenta: trei cazuri Opacity float pe nod pentru referinta
  directa, OneWay si TwoWay; compilare reala a output-ului, converter tipizat,
  setter si alegerea reference/binding/mode. 3/3 trecute inainte de refactorizare.
  Baseline anterior SourceGen: 611/611. Dupa cleanup: intregul proiect SourceGen,
  Release, 614/614, fara skip; toate cele 614 cazuri baseline/caracterizare trecute.
- Experiment respins, pastrat brut: prima caracterizare presupunea un source
  PrismBlendMode pentru BlendMode; 0/3 trecute, PRISM2009 in validarea Language,
  inainte de orice editare a productiei. Nu a fost un RED valid pentru cleanup.
  ResolvePrismType("symbol") intoarce System.String in Language; emitterul are
  un mapping enum pentru proprietatile comune. Semantica acestei diferente nu
  este rezolvata aici si nu se modifica public API sau reguli de binding.
  Fixture-ul presupus acceptat a fost inlocuit cu scenariul numeric verificat;
  nicio asteptare existenta nu este slabita sau schimbata.
- Contracte pastrate: definitie partajata dupa identity, factory/instanta separata
  per aplicatie, stable ID si slot ordinal pe storage type, direct reference
  separat de binding si disposable factories pentru owner lifecycle. Fixture-ul
  existent executa output-ul pentru sharing/instante distincte/detach; noua
  caracterizare compileaza output-ul, nu pretinde exercitarea subscription lifetime.
- Rezultate brute: `artifacts/prism-cleanup/sourcegen-markup-emission/`
  (`markup-characterization.trx` pentru experimentul enum esuat,
  `markup-number-characterization.trx`, `markup-emission-verified.trx`).
  Parintele a revizuit source/diff, XML-urile, ordinea caracterizare/refactorizare
  si `git diff --check`. Fara schimbari API/docs/schema; suita completa finala
  ramane pendinte.

### 2026-09-30 — contractele backdrop Drawing

- Target: cele sase tipuri backdrop citite integral, plus politica grafului,
  consumatorii UiHost/SDL, testele relevante si cele sase pagini API canonice.
  Explorer-ul read-only s-a incheiat; parintele a verificat sursa decisiva.
- Rezolvare: pastrate neschimbate. Sunt contracte mici, backend-neutral, fara
  ownership GPU. Enum-urile, semnaturile publice, validarea si ordinea exceptiilor
  raman intacte. Constructorul metadata verifica finitudinea transformului;
  invertibilitatea apartine politicii grafului, inclusiv pentru default struct.
  Validarile request/metadata au mesaje distincte; nu se inventeaza un helper.
- Lifecycle: UiHost dobandeste cel mult un lease pentru frame-ul analizat si il
  elibereaza in finally, inclusiv la exceptii. SDL pastreaza ownership-ul texturii,
  valideaza frame-ul/versiunea si elibereaza borrow-ul o singura data. Aceste
  responsabilitati nu se muta in interfetele publice sau in value objects.
- Verificare Release: 83/83 teste core pentru hosting, graph, enum admission,
  retained cache keys/visual versions; apoi CERNEALA_SDL_NATIVE_TESTS=1, 16/16
  teste backdrop hosting si retained dependency migration, fara skip. Include
  expirarea lease-ului, resize, provider replacement, exception cleanup,
  lifecycle stress si scenariul existent cu goldens la fiecare scale.
- Rezultate brute: artifacts/prism-cleanup/drawing-backdrop-contracts/
  (backdrop-contracts-verified.trx, backdrop-native-verified.trx). Parintele a
  revizuit sursa actuala, absenta unui diff de productie, XML-urile si
  git diff --check. Nu se pretinde acoperire exhaustiva a tuturor argumentelor
  invalide ale constructorilor; nu exista modificari care sa necesite noi
  caracterizari. Fara API/docs/schema schimbate; suita completa ramane finala.

### 2026-09-30 — identity, invalidari si payload Drawing

- Target: PrismBackdropSourceToken, PrismCacheInvalidationQueue,
  PrismCacheOwnerTokenAllocator, PrismDrawResources, PrismDrawScope si
  PrismRendererOptions citite integral. Inspectati calleri host/attachment/image,
  recording/resource leases, graph/cache keys, SDL si testele; explorer incheiat.
- Rezolvare: cele sase fisiere raman neschimbate. Allocatorii numerici separati
  apartin unor spatii de identitate distincte; codul mic nu justifica un allocator
  generic. Coada pastreaza locking-ul, deduplicarea ultimului owner, All si FIFO;
  hub-ul pastreaza referinte slabe si publica in afara lock-ului propriu.
- Resurse: snapshot-uri pe sase categorii, ultima intrare pentru acelasi ID in
  categorie, precedence explicita intre categorii, identity/version si false cu
  iesiri zero pentru miss. Validarea initiala si mesajele exceptiilor difera intre
  categorii; lookup-ul generic exista deja. Nu se introduce un factory cu
  delegates/tuple projections sau noi alocari doar ca sa scurteze buclele.
- Scope: pastreaza coordonatele, instance identity, versiunile, dependenta image,
  strict allocation si input bounds. TranslateLocal nu compune transformul;
  ApplyLocalTransform are un contract distinct. Optiunile publice nu configureaza
  aplicatia SDL: consumer-ul intern valideaza si seteaza limita soft la 32 MiB.
- Limite: ApplyLocalTransform si TryGetVersion au doar apeluri directe in teste
  in cautarea repo-wide. Nu sunt eliminate impreuna cu assertions de contract
  pentru a produce artificial cleanup. Nu sunt gasite referinte dinamice/serializare
  la aceste nume in sursele cautate. Testele gasite nu probeaza exhaustiv FIFO/
  deduplicare concurenta sau precedence cu acelasi ID intre toate categoriile;
  aceste mecanisme nu sunt editate.
- Verificare Release: 78/78 core (cache keys/versions, image/attachment lifecycle,
  recording, options, host si resurse tipizate), inclusiv testul existent de
  10.000 cicluri attachment cu invalidari owner distincte. Apoi native SDL activ:
  45/45, fara skip, pentru retained surfaces/execution, adjustment resources si
  scene allocation/transform policy. Fara schimbari de expectations/goldens.
- Rezultate: artifacts/prism-cleanup/drawing-draw-state/ (draw-state-verified.trx,
  draw-state-native-verified.trx). Parintele a revizuit sursa, call paths decisive,
  absenta diff-ului de productie, XML-uri si git diff --check. Nicio revendicare
  noua de performanta, API/docs/schema neschimbate; suita completa ramane finala.

### 2026-09-30 — API-ul Drawing PrismImage/pipeline

- Target: Prism, PrismImage, PrismOperation, PrismPipeline si PrismEnumValidation
  citite integral. Inspectati recording-ul recursiv, observarile RenderSurface2D,
  state consumers, operation source emitter, reflection tests si paginile canonice.
  Explorer read-only incheiat; sursa decisiva si diff-ul revizuite de parinte.
- Cleanup: cele doua callback-uri private identice pentru schimbarea pipeline-ului
  si a sursei sunt unificate in OnInputContentChanged. Ambele perechi subscribe/
  unsubscribe folosesc acelasi callback. Ordinea, locking-ul, disposal-ul,
  invalidarea owner-ului si emiterea ContentChanged nu se modifica. Nu au fost
  gasite referinte externe/reflection/generated code la vechile nume private.
- Celelalte patru fisiere raman neschimbate: Apply pastreaza ordinea validarilor,
  pipeline-ul pastreaza subscription multiplicity si topology/content versions,
  operatiile pastreaza domeniile catalogului si transferul catre doua stari tipizate
  distincte, iar enum admission ramane explicit, fara runtime metadata discovery.
  Nu se introduce o interfata doar pentru deduplicarea switch-urilor filter/style.
- Caracterizare permanenta noua: doi observatori impart o singura abonare la
  source; schimbarea source si a unei operatii notifica observatorii curenti,
  remove non-final pastreaza observatia, ultimul remove o opreste, reattach
  o reactiveaza fara duplicate. 1/1 GREEN inaintea editarii productiei, apoi
  rerulat GREEN in lotul verificat; asteptarile existente raman intacte.
- Baseline Release: 55/55 core si 66/66 SDL, fara skip. Dupa cleanup: 161/161 core
  pentru API, enums, object motion, graph/cache si drawing/surface consumers;
  toate cele 55 cazuri baseline si noua caracterizare trecute. Apoi native SDL
  activ: aceleasi 66/66 cazuri, toate trecute, fara skip. Include OnDemand redraw,
  incetarea observarii unei imagini scoase din frame, nested images, retained
  reuse/invalidation/dispose si sprite batch observation.
- Rezultate brute: artifacts/prism-cleanup/drawing-image-api/ (image-api-baseline.trx,
  image-api-native-baseline.trx, image-observers-characterization.trx,
  image-api-verified.trx, image-api-native-verified.trx). Parintele a revizuit
  source/diff, XML-urile si prezenta cazurilor baseline, git diff --check.
  Fara schimbari public/protected/docs/schema sau revendicari de performanta;
  testele de observare sunt secventiale, nu o verificare exhaustiva a concurentei.
  Suita completa finala a repository-ului ramane pendinte.

### 2026-09-30 — formulele locale Blend

- Target: toate cele 31 fisiere Drawing/Prism/Blend citite integral. Inspectate
  call paths managed, shader dispatcher/Normal/PassThrough/BlendIf, optiunile,
  GraphBuilder.BuildGroup, contractul canonic al enum-ului si testele relevante.
  Explorer read-only incheiat; parintele a verificat direct sursa decisiva.
- Cleanup: eliminata normalizarea duplicata PassThrough -> Normal din cele doua
  apeluri locale Blend (compozitie asociata si knockout). Dispatcherul existent
  are deja ramura PassThrough, iar ambele formule locale intorc acelasi source
  dupa aceleasi clamp-uri. Ordinea aritmetica, validarile, formulele, enum stable
  IDs si mapping-ul shader 0..27 raman neschimbate. Semantica structurala distincta
  a grupurilor ramane la BuildGroup; nu este transformata in Normal.
- Celelalte 30 fisiere sunt pastrate. Formulele mici au deja primitive comune;
  cross-reuse Burn/Dodge/Overlay/VividLight exista. Nu se unifica arbitrar moduri,
  nu se schimba matematica floating-point sau algoritmul/rankmap-ul Dissolve si
  nu se introduce un framework de delegates pentru a scurta switch-ul explicit.
- Caracterizare permanenta: sase cazuri de alpha zero/partial/opac, RGB finit
  inclusiv in afara [0,1], optiuni default si Red|Alpha/BlendIf Blue pentru toate
  valorile Knockout, plus backdrop original distinct si shape diferit de alpha.
  Egalitate exacta Normal/PassThrough in calculul local: 6/6 GREEN inainte de
  editarea productiei; aceleasi sase cazuri GREEN dupa. Nu este un test care
  echivaleaza semantica grupurilor; testele de graph verifica distinctia.
- Baseline Release: core 111/111, native SDL 16/16. Dupa cleanup: core 128/128
  (inclusiv StylePipeline), native 22/22; toate cazurile baseline trecute, fara
  skip. Native all-modes compara GPU cu managed reference pentru 28 moduri si
  patru perechi source/backdrop; include optiuni avansate, dual-backdrop knockout
  si Dissolve. Aceasta este concordanta in scenariile testate, nu o dovada
  independenta/exhaustiva a corectitudinii fiecarei formule.
- Conformance: 47/47 baseline/style native migration cu frozen pixels, anchors,
  reuse si lifecycle; apoi 132/132 cazuri Prism resource-free pe Windows SDL_GPU,
  prin Window.SaveScreenshot. Cele 132 rapoarte pixel-diff sunt pastrate; praguri
  si goldens neschimbate. Toate gate-urile lotului fara skip.
- Rezultate brute: artifacts/prism-cleanup/drawing-blend/ (blend-core-baseline,
  blend-native-baseline, blend-passthrough-characterization, blend-core-verified,
  blend-native-verified, blend-conformance-verified si
  blend-window-conformance-verified.trx; pixel-diff/). Parintele a revizuit sursa
  actuala, diff-ul, XML-urile, prezenta cazurilor baseline si git diff --check.
  Fara API public/protected/docs/schema schimbate sau revendicari de performanta.
  Suita completa a repository-ului ramane pentru finalul checklist-ului.
### 2026-09-30 — catalogul public si politica fallback

- Target: PrismCatalog.cs si PrismFallbackPolicy.cs citite integral, plus
  proiectia generatorului, calleri operations/state/diagnostics/tracker,
  inventarul API, cinci pagini canonice ale catalogului si testele relevante.
  Explorer read-only incheiat; parintele a verificat sursa decisiva.
- Rezolvare: ambele fisiere sunt pastrate neschimbate. Catalogul proiecteaza
  descriptorii generati intr-un snapshot imutabil, pastreaza ordinea, stable ID,
  slot-uri si domenii numerice invariant-culture. Single pastreaza inclusiv
  exceptia actuala pentru ID invalid; nu se introduce un index/cache pentru
  lookup fara o nevoie masurata. Simbolurile sunt validate exact, ordinal,
  apoi rezolvate prin runtime-ul generat; nu se dubleaza enum admission.
- Fallback: actiunile raman centralizate (MissingBackdrop omite backdrop,
  InvalidColorProfile/SurfaceAllocationFailed ocolesc compozitia, restul
  ocolesc operatia). Codurile/stadiile diagnosticelor sunt metadata distincta,
  nu o a doua politica de actiune; tracker-ul aplica rezultatul primit.
- Verificare Release: core 36/36 pentru API/instance/definitions; separat 33/33
  pentru enum/symbol admission si metadata-to-planner coverage. Consumatori
  SDL: 7/7 pentru diagnostics si ID invalid, fara skip. Toate cele sapte motive
  fallback sunt exercitate in testul diagnosticelor; subsetul direct verifica
  explicit trei actiuni. Nu se pretinde o verificare exhaustiva a tuturor
  formelor domeniilor metadata sau a consumatorilor externi.
- Rezultate: artifacts/prism-cleanup/drawing-catalog/ (catalog-core-verified,
  catalog-metadata-verified, catalog-consumers-verified.trx). Parintele a revizuit
  sursa, absenta diff-ului de productie si XML-urile. Fara API/docs/schema sau
  comportament schimbate; suita completa ramane finala.

### 2026-09-30 — conversia culorilor si premultiplicarea

- Target: toate cele sapte fisiere Drawing/Prism/Color citite integral, plus
  calleri filter parameters/adjustments/style colors/gradient LUT, shader color
  contracts, documentatia profilelor si testele relevante. Explorer read-only
  incheiat; parintele a verificat direct sursa, preconditiile si diff-ul actual.
- Cleanup: Convert foloseste PrismPremultipliedColor.FromStraight pentru
  reasocierea canalelor; eliminat helper-ul privat Associate care duplica exact
  aceleasi trei inmultiri si returnarea alpha. Validarea ramane in pipeline,
  inaintea guard-ului alpha zero, decodarii si codarii. FromStraight nu adauga
  validare sau clamp. Ordinea aritmetica, transferurile semnate sRGB, coeficientii
  matricilor, profilele default si conventia alpha sunt pastrate.
- Celelalte sase fisiere raman neschimbate. Identitatea matematica LinearSrgb/
  ScRgb nu elimina distinctia semantica a profilelor. Oklab-ul gradientelor
  foloseste intermediari double/Math.Cbrt, iar Color filter foloseste float/
  MathF.Cbrt; asemanarea coeficientilor nu probeaza echivalenta numerica. Nu se
  deduplica peste aceasta diferenta, nu se adauga un fast path pentru profile
  identice si nu se muta clamp-ul local al gradientelor in conversia generica.
- Caracterizarea existenta acopera contractul reasocierii: alpha zero cu RGB
  nenul devine zero, pixeli de margine/partiali/opaci prin toate cinci profilele,
  nested crossings si double-gamma sentinel, plus RGB negativ/peste 1 in ScRgb.
  Baseline inaintea editarii: core 70/70 si native SDL 6/6, fara skip. Nu exista
  schimbari de asteptari sau un defect pentru care sa se inventeze RED.
- Dupa cleanup: core 179/179, incluzand aceleasi 70 cazuri si consumatorii
  neighborhood/distortion; native SDL activ 21/21, incluzand aceleasi sase cazuri
  si 15 style-conformance migration. Niciun skip. GPU/reference pentru cinci
  profile, ScRgb extended range, ChannelMixer/Posterize si Color grade; style
  conformance verifica pixelii si scenariile existente, fara goldens/praguri
  schimbate. Aceasta nu este o dovada exhaustiva a gamut-ului sau preciziei.
- Rezultate brute: artifacts/prism-cleanup/drawing-color/ (color-core-baseline,
  color-native-baseline, color-core-verified, color-native-verified.trx).
  Parintele a revizuit toate fisierele target, diff-ul, XML-urile, ordinea
  baseline/editare, prezenta fiecarui caz baseline si git diff --check. Fara
  schimbari API public/protected/docs/schema sau revendicari de performanta.
  Corpusul complet Window.SaveScreenshot si suita repository-ului raman gate-uri
  finale; lotul curent nu pretinde rerularea lor dupa aceasta editare.

### 2026-09-30 — diagnosticele execution/hosting/Detective

- Target: PrismExecutionDiagnostics, PrismGraphFallbackTracker,
  PrismRendererDiagnostics si PrismOperationalDiagnostics citite integral.
  Inspectate calleri backend/host/presentation/Servo, snapshot/API/audit,
  warmup, proiecte/friend assemblies, contracte si teste. Explorer read-only
  incheiat; referintele cu prefixe de directoare eronate au fost corectate dupa
  reverificarea filesystem-ului. Parintele a verificat direct sursa decisiva.
- Cleanup: eliminat fisierul intern PrismGraphFallbackTracker.cs (47 linii,
  anterior nemodificat si urmarit de Git), fara calleri C# sau teste gasite in
  sursele actuale, fara referinte generated/reflection/serialization gasite.
  Hit-urile ramase sunt istoricul auditului si snapshot-uri de build inputs,
  nu consumatori runtime. Stergerea este recuperabila din Git; inventarul
  initial de 312 target-uri ramane intact in checklist, cu item-ul rezolvat.
- Nu se muta politica sau ownership-ul. Trackerul nefolosit avea stare
  per-scope BypassComposition; executorul SDL activ are taint per-node si per-
  consumer pentru cache promotion. Sunt mecanisme diferite, nu implementari
  echivalente. Executorul si politica fallback raman neschimbate.
- Eliminata si fabrica interna PrismRendererDiagnostics.Empty, fara calleri
  gasiti; tipul public readonly, constructorul intern, campurile, getters si
  metodele de reason counts raman intacte. Snapshot-ul public documentat nu
  este un live accessor SDL si nu este eliminat pentru lipsa unui consumer.
- Primele rebuild-uri core au expus CS0234: importul global Execution din
  tests/Cerneala.Tests/Cerneala.Tests.csproj depindea de namespace-ul al carui
  singur tip era trackerul. Eroare reprodusa si pastrata in
  namespace-import-build-failure.log, inainte de reparare; nu este un RED de
  comportament. Eliminat doar acel import nefolosit din proiect, dupa cautarea
  tuturor referintelor namespace-ului. Generated GlobalUsings este regenerat
  de build; nu este editat manual si nu se adauga un shim de namespace.
- Celelalte doua fisiere sunt pastrate. BeginFrame reseteaza contoarele/captura,
  nu LastFallback; aceasta observatie nu este reinterpretata ca defect sau
  schimbata in cleanup. Bufferele detalierii se reutilizeaza si sunt curatate
  pentru a nu retine sursele; dump-ul pastreaza corelarea si redactionarea.
  Capacitatile entry/pass au praguri distincte, iar lookup-urile node au
  contracte throw/redact distincte; nu se comprima intr-un helper arbitrar.
- Baseline core/public: 18/18. Prima selectie SDL fara env native: 114/115,
  un skip explicit al scenariului native; rerulare cu native activ, inainte
  de editare: 115/115, fara skip. Dupa cleanup/repararea importului: core 94/94
  incluzand application/window runtime; SDL 140/140 incluzand native execution,
  budgets si retained execution. Toate cazurile baseline trecute, fara skip.
  Testul existent disabled-details verifica 0 bytes alocati pe firul curent
  dupa 2.048 warmups, pentru 4.096 frame-uri; aceasta nu este o noua promisiune
  de performanta pentru toate modurile diagnostice sau pentru intreaga UI.
- PrismAudit --check Release: 178 intrari, 31 common properties, 216 tipuri
  publice Prism si 8 extended, zero gaps; API/docs raport sincronizat fara
  regenerare sau schimbari de contract public. git diff --check trecut.
- FileTree regenerat: fata de copia pre-refresh, doar eliminarea trackerului
  si marker-ul ultimului sibling Execution. Restul diff-ului preexistent este
  pastrat. Auditul istoric si snapshot-urile istorice nu sunt rescrise.
- Evidenta: artifacts/prism-cleanup/execution-diagnostics/ (*.trx,
  namespace-import-build-failure.log, prism-audit-check.log,
  FileTree-before-refresh.md). Parintele a revizuit sursa actuala, diff-urile,
  raw rezultate, ordinea baseline/editare/reparare si prezenta cazurilor
  baseline. Cautarile nu pretind excluderea oricarui consumer binar extern.
  Fara goldens sau asteptari schimbate; suita completa si corpusul final de
  conformance raman pendinte.

### 2026-09-30 — contabilizarea si esecurile alocarii suprafetelor

- Target: cele doua fisiere din `Drawing/Prism/Surfaces`, citite integral;
  explorare Luna read-only si inspectie parent a callerilor, testelor, contractelor,
  warmup-ului reflectiv, API auditului si inputurilor proiectului.
- Eliminat exact `Drawing/Prism/Surfaces/PrismSurfaceMemoryAccountant.cs` (159
  linii), recuperabil din Git. Ambele tipuri interne din fisier,
  `PrismSurfaceMemoryAccountant` si `PrismSurfaceBudget`, nu au consumatori gasiti
  in sursa curenta. Cautarea hidden in repo a exclus `.git`, `bin`, `obj` si
  `artifacts`; alte potriviri sunt inventarul/checklistul, auditul istoric si
  snapshoturi istorice de compile items. Nu exista referinte curente C#/markup/
  build inputs, apeluri directe, typeof sau nume reflectate gasite pentru ele.
  Rezultatul negativ nu este o afirmatie despre orice consumator extern posibil.
- Ownerul activ ramane `SdlGpuPrismDeviceResources`: propriile `totalBytes`,
  `freeBytes`, `peakBytes`, admitere `EnsureBudget`, estimarea mip-chain-ului,
  pin-uri pe command buffer, pooling, retained eviction si retirement. Eliminarea
  clasei nefolosite nu muta aceste responsabilitati si nu inlocuieste algoritmul
  SDL cu vechea contabilitate transient/retained. Contractul curent din
  `architecture.md` si `docs/prism-technical-design.md` este pastrat.
- `PrismSurfaceAllocationException.cs` pastrat fara modificari: trei throw sites
  in SDL resources si catch in executor numai pentru `!strictSurfaceAllocation`.
  Mesajul, inner exception si cele patru proprietati sunt folosite in teste;
  nu am adaugat validari sau schimbat fallback-ul scenelor stricte/obisnuite.
- Baseline si post-eliminare, cu `CERNEALA_SDL_NATIVE_TESTS=1`:
  `dotnet test tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj -c Release
  --no-restore --filter 'FullyQualifiedName~PrismSurfaceOwnershipTests|
  FullyQualifiedName~ScenePrismAllocationTests|
  FullyQualifiedName~PrismOperationalDiagnosticsTests|
  FullyQualifiedName~SdlGpuPrismExecutorTests|FullyQualifiedName~SdlArchitectureTests'`
  -> **115/115**, zero skip, ambele rulari; baseline a avut si `--no-build`,
  post-eliminare a recompilat. Acopera admitere, mip bytes, reutilizare,
  2.048 cadre, ownership/pinning, thread affinity, esec hard cap, esec strict,
  fallback obisnuit si recuperare. Nu este un test al clasei eliminate.
- Core, baseline si recompilat post-eliminare:
  `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-restore
  --filter 'FullyQualifiedName~PrismRendererOptionsTests|
  FullyQualifiedName~PrismDefinitionContractTests'` -> **14/14**, zero skip;
  baseline a avut si `--no-build`. Toate cazurile baseline din ambele proiecte
  sunt prezente si Passed in TRX post-eliminare, verificat prin XML.
- `dotnet run --project Tools/PrismAudit/PrismAudit.csproj -c Release --no-restore
  -- --check` -> exit 0: **178 intrari, 31 proprietati comune, 216 tipuri Prism
  publice + 8 tipuri extinse, zero gaps**. API public/canonical docs neschimbate.
- Raw evidence: `artifacts/prism-cleanup/drawing-surfaces/`: cele patru TRX
  `surfaces-baseline`, `surfaces-verified`, `surfaces-core-baseline`,
  `surfaces-core-verified`, `prism-audit-check.log` si
  `accountant-reference-search-before.log`.
- `New-FileTree.ps1` rerulat; comparatia cu copia imediat anterioara confirma
  numai eliminarea accountantului si schimbarea markerului ultimului sibling.
  Modificarile anterioare ale FileTree au fost pastrate. `git diff --check`
  trecut; review parent al stergerii, exceptiei curente, rezultatelor brute si
  checkpointului. Nu au fost slabite teste/goldens si nu raman joburi temporare.
- Bifate **2/2**: total **85/312**, raman **227**. Nu sunt revendicate imbunatatiri
  de performanta sau validare manuala/cross-platform. Suita completa si corpusul
  vizual complet raman pentru gate-ul final al targetului.

### 2026-09-30 — masking si clipping, referinte CPU si traseul GPU

- Utilizatorul a reconfirmat explicit: pastram acordul anterior privind gate-urile
  focalizate inainte de bife; suita completa numai la final. In intervalul de
  clarificare au fost doar citiri, fara teste, editari de productie sau bife noi.
- Target: `PrismClippingStyle.cs`, `PrismMaskMath.cs`, `PrismMaskStyle.cs`, citite
  integral; explorer Luna read-only, inspectie parent a callerilor efectivi,
  graph builder, executor, dispatcher si shadere, contracte/docs si teste.
- **3/3 pastrate fara modificari justificate**. `PrismMaskMath` este fatada
  referintelor CPU folosite de `PrismMaskPipelineTests`; `PrismMaskStyle` detine
  extragerea scalarului/validarea canalului si densitatii/feather-nine; clippingul
  foloseste alpha bazei. `Scale` multiplica toate cele patru canale premultiplied,
  deja reutilizat de masca si clipping. Nu adaug un utilitar generic, nu unific
  canalele Alpha/Luminance si nu inlocuiesc referintele CPU cu apeluri GPU.
- Cautarea hidden pentru cele trei nume gaseste apeluri C# numai in targeturi si
  testul referintei CPU; in `PrismEnumValidationTests` este un file-path string,
  nu apel. Faptul ca rendererul nu apeleaza aceste tipuri nu le face cod mort:
  testele le consuma efectiv. Nu au fost schimbate dependente sau API-uri publice.
- Traseul activ verificat prin sursa: graph `ApplyMask` emite extract/optional
  feather-H/feather-V si muchia `MaskAlpha`; chain-ul clipping pastreaza muchii
  distincte `ClipBaseAlpha` catre baza unclipped. Executorul foloseste kernel 41
  extract, 42 feather, 40 composite mask, 43 clip. Dispatcherul SDL leaga aceste
  ID-uri de shaderele comune `Composition/Mask.hlsl` si `Clipping.hlsl`.
  Densitatea este aplicata la extract fara feather sau la ultimul feather pass;
  missing image foloseste masca opaca. Nu schimb aceste contracte sau ordinea.
- `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-build
  --no-restore --filter 'FullyQualifiedName~PrismMaskPipelineTests|
  FullyQualifiedName~PrismEnumValidationTests|
  FullyQualifiedName~PrismDefinitionContractTests'` -> **47/47**, zero skip.
  Verifica inclusiv scalar/partial alpha, density-zero identity, feather sampling
  bounds fara layout/output expansion si baza comuna a siblingurilor clipped.
- Acelasi proiect/configuratie, filter `FullyQualifiedName~PrismGraphContractTests|
  FullyQualifiedName~PrismRetainedCacheKeyTests` -> **31/31**, zero skip;
  include muchii mask/clip distincte, invisible clip base, diagnostic context,
  boundary de grup si dependentele retained.
- Cu `CERNEALA_SDL_NATIVE_TESTS=1`, `dotnet test
  tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj -c Release --no-build
  --no-restore --filter 'FullyQualifiedName~PrismFundamentalGpuTests|
  FullyQualifiedName~PrismBaselineConformanceMigrationTests|
  FullyQualifiedName~PrismRetainedExecutionTests'` -> **52/52**, zero skip.
  TRX confirma Passed pentru mask, clip, mask-transform, clipping-chain, masca
  alpha/luminance/invert/density/UV/feather si scena masked group cached/fresh.
  `FundamentalKernelsPreservePremultipliedAlpha` exercita inclusiv kernel 43
  prin tabelul de cazuri, desi nu exista literalul `RunKernel(43)`.
- Raw TRX in `artifacts/prism-cleanup/drawing-masking/`: `masking-core-verified`,
  `masking-graph-cache-verified`, `masking-native-verified`. Parintele a inspectat
  rezultatele XML, case presence si sursa actuala; `git diff --quiet --
  Drawing/Prism/Masking` confirma zero schimbari; `git diff --check` trecut.
- Observatie separata, nu verdict de bug si nu patch: CPU luminance clampeaza
  suma ponderata, iar shaderul clampeaza canalele straight inainte de suma.
  Cazurile GPU inspectate nu stabilesc echivalenta pentru RGB straight in afara
  [0,1]. Nu canonizez sau repar aceasta diferenta in cadrul cleanup-ului.
- Gate-urile au verificat starea curenta fara schimbari de productie in lot;
  nu era necesar un RED sau un al doilea test run identic dupa editare inexistenta.
  Fara teste/goldens/docs/API rescrise, fara performanta revendicata, fara joburi
  ramase. Testele native folosesc fixture-ul de randare/pixeli, nu sunt capturi
  Window.SaveScreenshot. Corpusul complet vizual si suita repo raman la final.
- Bifate **3/3**: total **88/312**, raman **224**. Goalul ramane activ.

### 2026-09-30 — enumul intern de categorii kernel nefolosit

- Target: `Drawing/Prism/Kernels/PrismKernelKind.cs` (35 linii), citit integral;
  Luna read-only si parent au cautat referinte de tip, namespace, generare,
  reflection/serialization si build inputs. Tipul intern nu are consumer C#
  curent gasit; celelalte potriviri sunt inventarul/auditul/compile-item evidence
  istoric. Nu pretind absenta oricarui consumer binar extern.
- Eliminat numai acest fisier, recuperabil din Git, si importul global inutil
  `<Using Include="Cerneala.Drawing.Prism.Kernels" />` din proiectul core de teste,
  citit integral inainte de editare. Acesta era singurul namespace dependency
  gasit, iar enumul era singurul tip al namespace-ului. Nu am lasat un namespace
  shim si nu am editat GlobalUsings generat; outputul regenerat nu mai importa
  `Kernels`. Eliminarea anterioara a importului `Execution` a fost pastrata.
- Ownerul runtime este neschimbat: `SdlGpuPrismKernelSelector` si dispatcherul
  HLSL folosesc ID-uri numerice, `PrismGraphNodeKind`, catalog filter IDs si
  culoare/blend modes; niciunul nu foloseste acest enum de categorii. Warmup-ul
  reflectiv pregateste trei clase explicite, nu descopera enumul. API auditul
  reflecta tipurile exportate; tipul eliminat este intern. Nu remapez ID-uri,
  nu inlocuiesc selectorul cu enumul vechi si nu modific shader assets.
- Baseline reutilizat din starea imediat pre-editare, fara alte editari de
  productie intre rulari si eliminare: cele doua TRX core masking **47+31=78**,
  native masking **52** si cele **43** cazuri `SdlGpuPrismExecutorTests` din TRX
  surfaces-verified. Toate Passed, fara skip. Evit rerularea redundanta a aceluiasi
  baseline. Source tests inspectate acopera maparea tuturor modurilor blend,
  profilele culoare, toate catalog entries si aliasurile/pass-urile specializate.
- Post-eliminare recompilat Release, `--no-restore`, core filter
  `PrismMaskPipelineTests|PrismEnumValidationTests|PrismDefinitionContractTests|
  PrismGraphContractTests|PrismRetainedCacheKeyTests` -> **78/78**, zero skip.
- Post-eliminare recompilat Release, `--no-restore`, SDL filter
  `PrismFundamentalGpuTests|PrismBaselineConformanceMigrationTests|
  PrismRetainedExecutionTests|SdlGpuPrismExecutorTests`, cu
  `CERNEALA_SDL_NATIVE_TESTS=1` -> **95/95**, zero skip. Ambele comenzi folosesc
  filtre FullyQualifiedName~ pentru fiecare clasa. XML confirma toate cele 78
  core si 95 native cazuri baseline prezente si Passed in outputul nou.
- `dotnet run --project Tools/PrismAudit/PrismAudit.csproj -c Release --no-restore
  -- --check` -> exit 0, **178 catalog entries, 31 common properties, 216 tipuri
  Prism publice + 8 extended, zero gaps**. API/docs/manifest neschimbate.
- Evidenta noua: `artifacts/prism-cleanup/drawing-kernels/`:
  `kernels-core-verified.trx`, `kernels-native-verified.trx`,
  `prism-audit-check.log`, `kernel-kind-reference-search-before.log` si
  `FileTree-before-refresh.md`. Baseline-urile sunt in folderele drawing-masking
  si drawing-surfaces deja documentate. Fara RED comportamental inventat pentru
  eliminarea unui enum nefolosit; nu au fost schimbate asteptari/goldens.
- FileTree regenerat: comparatia cu copia imediat precedenta arata doar
  eliminarea liniei fisierului; directorul gol nu a fost sters printr-o operatie
  filesystem separata. Modificarile anterioare ale tree-ului pastrate. Parent
  review al diff-ului actual, callerilor/namespace input, GlobalUsings, TRX,
  auditului si checkpointului; `git diff --check` trecut, fara joburi ramase.
- Bifat **1/1**: total **89/312**, raman **223**. Nu revendic performanta,
  validare umana sau cross-platform. Gate-urile repo/corpus vizual complet raman
  pentru final; goalul nu este terminat.

### 2026-09-30 — cele zece familii style, planner si CSS gradient LUT

- Target: toate cele 12 fisiere Styles citite integral; Luna read-only incheiat,
  parent a inspectat callerii, catalogul, parameter store, optimizer/raster planner,
  executor/resources, teste si contracte. Nu se schimba ownership-ul geometriei,
  preFillStyleSource, resursele, blend/profile sau algoritmii de randare.
- Cleanup numai in PrismStylePlanner.PaintKindCode: eliminat conditionalul cu
  ambele ramuri Color si campul ColorSymbol folosit numai de el. Gradient/Pattern
  raman distincte; orice alt simbol ramane Color. ResolveSymbol pentru FillType
  este hash pur, nu validare/admitere sau initializare mutabila de catalog.
- Celelalte 11 fisiere pastrate. Mapperele au familii/flaguri/resurse distincte;
  glow-urile difera Choke/Spread/Origin. CSS LUT are 1.024 Vector4 asociate alpha,
  Oklab/classic/profile; GradientMap LUT are 256 Vector3 si contract diferit.
  Nu unific bucle asemanatoare sau creez helper-e cu switch boolean.
- Characterization permanent StrokePaintKindCodesPreserveDeclaredSymbolsAndColorFallback:
  trei simboluri declarate si cinci int-uri necunoscute; GREEN 1/1 inainte de
  productie (TRX finish 16:06:52 UTC, productie editata 16:09:14 UTC). Nu inventez
  RED pentru refactor echivalent si nu schimb asteptari. Acesta verifica helperul;
  testele existente de catalog/graph/native exercita traseul plan/executor.
- Baseline core 54/54 (StylePipeline, GradientOverlayPipeline, EnumValidation),
  native activ 58/58 (StyleConformanceMigration, SdlGpuPrismExecutor), zero skip.
  Filtrul baseline native includea si un nume gresit PrismGradientOverlayGpuTests:
  clasa nu exista; cele 58 cazuri efective sunt 15+43, nu pretind acel coverage.
- Recompilat Release --no-restore dupa productie: core 122/122 cu aceleasi clase
  plus GraphOptimizer, RasterPlanner, RetainedCacheKey si RenderSurface2D foundation;
  SDL native 92/92 cu cele 58 baseline plus BaselineConformanceMigration si
  BuiltinTextureAllocation. XML confirma toate cazurile baseline Passed, zero skip.
  Cele zece scene style congelate au trecut; fixture numeric, nu screenshot Window.
  Testul cache gradient existent are 32 warmups + 256 lookup-uri, acelasi handle/
  count si zero bytes pe fir; nu extrapolez performanta intregului renderer.
- Corpusul complet PrismSdlGpuPixelConformanceTests: 132/132, zero skip, exit 0,
  prin Window.SaveScreenshot, native activ, fara goldens/praguri schimbate.
  In pixel-diff: 132 rapoarte TXT si 396 PNG reference/current/diff. Rapoartele
  Stroke si GradientOverlay au MAE=0, P99=0, max=0 (praguri 1/10/49).
  Referintele istorice nu sunt executie curenta MonoGame sau oracle matematic.
- PrismAudit --check recompilat: 178 intrari, 31 common properties, 216 tipuri
  publice Prism + 8 extended, zero gaps. API/docs/manifest neschimbate.
- Evidenta: artifacts/prism-cleanup/drawing-styles/ (cele sase TRX, audit log,
  pixel-diff). Parent review al sursei actuale, diff-ului, rezultatelor brute,
  ordinii characterization/editare, case presence si checkpointului;
  git diff --check trecut. Fara joburi sau experimente temporare ramase.
- Bifate 12/12: total 101/312, raman 211. Suita completa repository-ului ramane
  la final; corpusul relevant trebuie rerulat dupa modificari ulterioare aplicabile.
  Nu revendic validare umana/cross-platform. Goalul ramane activ.

### 2026-09-30 — valori Graph, dependente input si cheia retained

- Target: PrismDependencyStamp, PrismFnv1aHash, PrismInputDependency,
  PrismRetainedCacheKey si PrismBackdropRequirement, citite integral. Luna/max
  read-only incheiat; parent a inspectat direct analyzer/builder/optimizer,
  scene input/domain, host, executor/resources, teste si contractele relevante.
- 5/5 pastrate fara schimbari justificate. Stamp-ul este record value-only cu
  sase dimensiuni; cheia adauga fingerprints, raster/context/backdrop. Descrierea
  ampla din design doc nu inseamna ca fiecare dimensiune este membru al stamp-ului.
  FNV MixInt64 agregheaza un cuvant, iar fingerprint-ul mixeaza doua jumatati
  UInt32 si lungimea: nu le unific sub presupunerea unui contract identic.
- Fingerprint equality verifica hash + lungime + componente exacte; hash-ul
  singur nu valideaza hit-ul. Builder-ul ordoneaza nodes/edges/scopes/parameters,
  dar pastreaza ordinea dependencies primita. Reuse structural ramane conditionat
  de controalele optimizerului; nu adaug invalidari/cache flush sau optimizari.
- Input dependency separa whole-input de supported local footprint. Traversarile
  similare au rezultate/ordine diferite, iar grupurile PassThrough tin cont de
  background. LocalInputCache este per-scene-node, tine WeakReference si compara
  instance/structure/values/scale/transform. Mask feather citeste resursa mastii,
  nu vecini din scene input; ownerul achizitiei resursei nu se muta aici.
- BackdropRequirement pastreaza scope indexes in ordinea producerului si object
  identity prin analysis/request. Host-ul achizitioneaza o singura lease pentru
  toate scope-urile; nu introduc sortare, equality valorica sau validari noi.
- Core Release --no-build --no-restore: 182/182, zero skip. Filtru FullyQualifiedName~
  pentru RetainedCacheKeyTests, GraphPlanningAllocationTests,
  RetainedCommandContractTests, BackdropHostingContractTests,
  ScenePrismStreamingTests si GraphContractTests (prefix Prism pentru toate in
  afara ScenePrismStreamingTests). Filtrul continea si PrismFrameAnalyzerTests,
  nume inexistent; nu pretind coverage pentru el. TRX confirma clasele efective:
  11 + 15 + 16 + 14 + 106 + 20 = 182.
- Core suplimentar, aceeasi configuratie, filtru
  FullyQualifiedName~PrismStyleBackdropAnalysisTests: 6/6, zero skip.
  Include reevaluarea backdrop dupa schimbarea contributiilor bevel, cu analyzer
  reutilizat. Total core verificat in lot: 188 cazuri distincte.
- SDL Release --no-build --no-restore, CERNEALA_SDL_NATIVE_TESTS=1, filtre
  FullyQualifiedName~PrismRetainedExecutionTests|PrismRetainedDependencyMigrationTests|
  PrismBackdropHostingMigrationTests|PrismSurfaceOwnershipTests: 85/85, zero skip
  (13 + 8 + 8 + 56). Verifica cache-on/fresh pixels, owner isolation, invalidare,
  backdrop/lower-UI, budgets/pinning/lifetime. Fixture native numeric, nu capturi
  Window.SaveScreenshot; corpusul Window a trecut la checkpointul precedent,
  fara schimbari de productie de atunci.
- Testele de allocation existente au warmup si allowance-uri explicite: local
  cache 256 warmups, 10.000 queries, <=32 bytes/query; nu pretind masurare noua
  zero-allocation/CPU/GPU. Testul de equality enumera multe dimensiuni, dar nu
  modifica direct DrawContentVersion; scenariul separat draw verifica invalidarea
  graph-ului, nu acel assert direct. Nu ascund aceasta limita a coverage-ului.
- Evidenta: artifacts/prism-cleanup/graph-dependencies/ (trei TRX). Parent a
  verificat XML counters/case presence, sursa actuala si callerii decisivi;
  git diff --quiet -- Drawing/Prism/Graph confirma zero schimbari. Nu necesita
  RED sau rerun identic post-editare inexistenta. Audit API zero gaps din Styles
  este aceeasi stare de productie; API/docs/schema/goldens neschimbate.
- git diff --check trecut; tree generation Unchanged, modificari preexistente
  pastrate. Explorer si joburi incheiate, fara experimente temporare.
  Bifate 5/5: total 106/312, raman 206. Suita completa si gate-urile finale raman
  pendinte; fara validare umana/cross-platform revendicata. Goalul ramane activ.

### 2026-09-30 — snapshots Graph, backdrop policy, diagnostics si warmup

- Target: PrismAnalyzedScope, PrismBackdropFramePolicy, PrismColdStartWarmup,
  PrismFrameAnalysis, PrismGraphCapabilities si PrismGraphDiagnostic, citite
  integral. Luna/max read-only incheiat; parent a inspectat direct producerii,
  callerii, state analysis/frame context, backend first-present, teste si contracte.
- Toate sase pastrate fara schimbari de productie justificate. Snapshot record
  equality este consumat de graph reuse. FrameAnalysis pastreaza distincte
  fast predicate si validarea throwing cu mesaje/context; nu unific traseele.
  Capabilities sunt flags interne backend-neutral; diagnosticul pastreaza
  PRISM7201, composition/node/span si inner exception, fara schimbari API.
- Backdrop policy pastreaza validarea metadata, transformarea celor patru colturi,
  floor/ceil/clamp si dependency source/content/lower-UI. Nu schimb toleranta de
  determinant, ordinea validarilor, algoritmul sau hash-ul pentru concizie.
- Graph warmup ruleaza retained UI analysis + OuterGlow/Invert prin aceeasi Lazy
  task; backend execution warmup pregateste metode prin reflection si este owner
  diferit. Nu introduc un serviciu generic intre proiecte. Begin este chemat dupa
  present/submission; workload-urile folosesc deja CreateWorkload comun.
- Gap de coverage rezolvat: test permanent PrismColdStartWarmupTests pentru doua
  perechi Begin/Complete, asteptand task-ul real; exceptiile ar esua testul. Fara
  mock, sleep sau promisiune de timp startup; nu dovedeste numarul de task-uri
  create sau latenta la primul frame. Testul SDL existent verifica alt warmup.
- Baseline core Release --no-build --no-restore: 119/119. Recompilat Release
  --no-restore cu testul nou: 120/120, zero skip; XML confirma toate 119 baseline
  prezente Passed si caracterizarea noua Passed. Filtre FullyQualifiedName~ pentru
  PrismGraphContractTests, PrismRetainedCommandContractTests,
  PrismGraphPlanningAllocationTests, PrismNestedScopeCullingTests,
  PrismEnumValidationTests, PrismStyleBackdropAnalysisTests,
  PrismBackdropHostingContractTests, PrismRetainedCacheKeyTests; dupa adaugare
  si PrismColdStartWarmupTests. Acopera stale same-sized list/value state, scope
  hierarchy/culling/capabilities, backdrop crop si diagnostic attribution.
- SDL Release --no-build --no-restore cu CERNEALA_SDL_NATIVE_TESTS=1: 61/61, zero
  skip. Filtre FullyQualifiedName~ pentru PrismBackdropHostingMigrationTests,
  PrismRetainedDependencyMigrationTests, SdlGpuPrismExecutionColdStartWarmupTests,
  PrismWarmupRunsOnlyAfterTheFirstFrameIsSubmitted si SdlGpuPrismExecutorTests
  (8+8+1+1+43). First-present este FakeSdlApi; cele native folosesc fixture-ul
  numeric, nu capturi Window.SaveScreenshot. Nu pretind coverage nativ din fake.
- Evidenta: artifacts/prism-cleanup/graph-snapshots/ (trei TRX si copia tree).
  git diff --quiet -- Drawing/Prism/Graph confirma zero productie schimbata;
  nu necesita RED sau rerun nativ dupa adaugarea unui test exclusiv core.
  Nu au fost modificate goldens/asteptari/docs/schema/API. Auditul Styles zero
  gaps si corpusul Window trecut sunt aceeasi stare de productie, nu noi rulari.
- Tree regenerat: doar linia noului test fata de copia imediat anterioara;
  modificarile precedente pastrate. Parent review al sursei actuale, testului,
  diff-ului, XML case presence, tree si checkpointului; git diff --check trecut.
  Explorer/task warmup/joburi incheiate; fara experimente temporare ramase.
- Bifate 6/6: total 112/312, raman 200. Gate-urile complete raman la final;
  fara performanta, validare umana sau cross-platform revendicate. Goal activ.

### 2026-09-30 — frame analyzer si modelul semantic Graph

- Target: PrismFrameAnalyzer.cs (468 linii initial) si PrismGraph.cs (710), citite
  integral; Luna/max read-only incheiat. Parent a inspectat callerii host/surface,
  builder, snapshot/live-state si tipurile/constructorii/testele relevante.
- Cleanup: eliminat numai parametrul privat includeBackdrop din EstimateNode si
  cele doua argumente retransmise. Era numai declarat/transmis recursiv, fara
  citire; argumentele erau locale bool, fara side effects. EstimateCapabilities
  pastreaza includeBackdrop, BackdropInput si +4 surfaces. Refactor: trei linii
  eliminate, fara schimbarea algoritmului, ownership-ului sau API-ului public.
- Characterization permanent nou PrismFrameAnalyzerTests: PassThrough group ->
  Normal/Multiply group -> Invert layer; cerinte backdrop false/true, flags exacte
  si estimari 7/11 pe scope/frame. GREEN 2/2 inainte de productie: TRX finish
  16:59:04 UTC, editare productie 16:59:39 UTC; ordinea verificata prin timestamps.
  Sunt estimari existente, nu numarul masurat de GPU passes sau bytes alocati.
  Nu inventez RED comportamental pentru eliminarea unui argument nefolosit.
- Backdrop ramane derivat din starea live si catalog blend-opacity contributions;
  estimarile capabilitatilor/suprafetelor raman bazate pe definitii. Nu redenumesc
  aceasta observatie ca bug si nu schimb politica, culling-ul sau retained reuse.
- Graph model pastrat: record identity include analysis-scope occurrence; clasele
  graph/node nu au equality structurala. Constructorii pastreaza validation order,
  familia unica prepared-plan, metadata placement, uniqueness/edge existence si
  cycle rejection. Arrays si diagnostic enumeration isi pastreaza ordinea. Nu
  combin validarea aciclicitatii cu sortarea execution sau helper-e generice.
- Baseline reutilizat din checkpointul imediat anterior: core 120/120 si SDL
  61/61, plus characterization 2/2; zero skip. Post-editare recompilat Release
  --no-restore: core 260/260, SDL native 121/121, zero skip. XML confirma toate
  cele 120+2 cazuri core si 61 native baseline prezente Passed in outputul nou.
- Core filtre FullyQualifiedName~ pentru PrismFrameAnalyzerTests,
  PrismColdStartWarmupTests, PrismGraphContractTests, PrismGraphOptimizerTests,
  PrismGraphPlanningAllocationTests, PrismRasterPlannerTests,
  PrismRetainedCommandContractTests, PrismNestedScopeCullingTests,
  PrismEnumValidationTests, PrismStyleBackdropAnalysisTests,
  PrismBackdropHostingContractTests, PrismRetainedCacheKeyTests si
  ScenePrismStreamingTests. Scope/golden graph snapshots, staleness/descendants,
  cache/key/planning/streamed input si original characterization trecute.
- SDL CERNEALA_SDL_NATIVE_TESTS=1, filtre FullyQualifiedName~ pentru cele cinci
  selectii din graph-snapshots plus PrismRetainedExecutionTests,
  PrismBaselineConformanceMigrationTests si PrismStyleConformanceMigrationTests.
  61+13+32+15=121; scene frozen/nested/cache-on-fresh si graph selectors trecute.
  First-present foloseste fake API; nu il confund cu scenariile native.
- Corpus complet PrismSdlGpuPixelConformanceTests Release --no-build --no-restore:
  132/132, zero skip, Window.SaveScreenshot, native activ. In pixel-diff sunt 132
  TXT si 396 PNG reference/current/diff; trei rapoarte inspectate au 0/0/0,
  praguri existente MAE<=1/P99<=10/max<=49. Goldens/asteptari neschimbate.
  Referintele istorice nu sunt un backend MonoGame activ sau oracle matematic.
- PrismAudit --check recompilat: 178 entries, 31 common properties, 216 tipuri
  publice Prism + 8 extended, zero gaps. Docs/schema/manifest/API neschimbate.
- Raw evidence: artifacts/prism-cleanup/graph-analysis-model/ (patru TRX, audit
  log, pixel-diff si tree copy). Tree regenerat: numai linia noului analyzer test
  fata de copia precedenta, restul modificarilor pastrate. Parent review al sursei,
  diff-ului, original scenario, case presence, raw metrics si checkpointului;
  git diff --check trecut. Explorer/joburi incheiate, fara temporare ramase.
- Acoperirea nu este un test separat pentru fiecare ramura/argument invalid din
  model si nu demonstreaza orice input posibil. Fara promisiuni noi CPU/GPU/
  allocation, validare umana sau cross-platform. Suita repo ramane la final.
- Bifate 2/2: total 114/312, raman 198. Goalul ramane activ.

### 2026-09-30 — semantic Graph builder

- Target unic PrismGraphBuilder.cs, toate 1578 linii citite; Luna/max read-only
  incheiat. Parent a inspectat contractul Stage 4, callerii SDL/warmup/outset,
  state access pentru filtre/stiluri si testele decisive pentru ordine, reuse,
  mask/clipping, knockout si packing-ul catalogului.
- Pastrat fara modificari justificate. Layer si group au surse de stil, shape,
  fill si consum de background diferite; nu le unific printr-un helper cu flags.
  Snapshot-urile filter/style folosesc tipuri distincte de state, fara baza comuna;
  reunirea switch-urilor ar necesita adaptori/delegates sau schimbarea API-ului,
  nu o simplificare proportionala. Nicio schimbare de ownership/algoritm/API.
- Reuse compara snapshot-uri complete de scope si backdrop descriptor; cache-ul
  se publica numai dupa graph construit si a doua verificare EnsureCurrent.
  Aceasta este constatare din sursa; nu exista un test dedicat reutilizarii dupa
  build esuat. Nu revendic coverage pentru acea ramura sau toate inputurile.
- Ordinea bottom-up, capture unic, izolarea non-PassThrough, background-ul direct
  pentru PassThrough, baza clipping distincta de masca si shape distinct de opacity
  sunt acoperite de contracte/teste existente. FilterOriginal ramane selectat de
  tipul pass-ului; ordinals intermediare/finale si packing typed raman intacte.
- Verificare noua Release --no-build --no-restore: 631/631, zero skip. Filtru
  FullyQualifiedName~Cerneala.Tests.Drawing.Prism&FullyQualifiedName!~PrismSdlGpuPixelConformanceTests.
  XML confirma GraphContract 20, GraphPlanningAllocation 15, MaskPipeline 4,
  AdvancedBlendingKnockout 3, StylePipeline 18, CatalogFilter 68,
  NeighborhoodFilter 59 si celelalte familii core Prism prezente Passed.
  Raw TRX: artifacts/prism-cleanup/graph-builder/graph-builder-core-verified.trx.
- SDL 121/121, Window.SaveScreenshot 132/132 si PrismAudit zero gaps reutilizate
  din graph-analysis-model: aceeasi stare de productie, nu noi rulari. Fara
  schimbari de goldens, asteptari, docs/schema/manifest sau fisiere noi; tree nu
  necesita refresh. git diff --quiet pentru builder si git diff --check trecute.
- Parent review al sursei actuale, interactiunilor, diff-ului, raw XML si
  checkpointului; explorer/job incheiate, fara temporare. Nicio promisiune noua
  de performanta sau validare umana/cross-platform; suita repo ramane la final.
- Bifat 1/1: total 115/312, raman 197. Goalul ramane activ.

### 2026-09-30 — semantic Graph optimizer

- Target unic PrismGraphOptimizer.cs, toate 1944 linii initiale citite; Luna/max
  read-only incheiat. Parent a inspectat Stage 5, callerii SDL/warmup si contractul
  lifetime folosit de raster planner, testele pentru alias/fusion, bounds,
  dependency propagation, retained reuse, fan-out si nested capture consumers.
- Cleanup numai doua helper-e private fara apeluri: Inflate(DrawRect, float) si
  Translate(DrawRect, float, float), 14 linii eliminate. Toate apelurile active
  Inflate au trei argumente si overload-ul lor este pastrat integral. Cautarea
  referintelor/dynamic uses si lectura ownerului nu au gasit consumatori ai celor
  doua helper-e; warmup-ul reflectiv SDL enumera numai cele trei tipuri backend,
  nu optimizerul. Nu schimb API, algoritm, ownership sau comportament.
- Restul pastrat: alias numai cu dovada no-op/catalog fusion, dependențe ale
  nodurilor eliminate colectate in tinta, reachability din outputs, ordine
  topologica scope-aware, cache eligibility/fingerprints, bounds conservatoare si
  lifetime inclusive cu parent capture implicit. Nu inlocuiesc algoritmi pe baza
  intuitiei si nu unific validarea modelului cu ordonarea executorului.
- Baseline core builder 631/631 plus checkpointul Graph anterior 260/260 trecute
  inainte de productie. Fara test nou inutil pentru cod neapelat, fara RED fictiv
  sau asteptari modificate. Timestamp baseline finish 17:19:53 UTC, editare
  17:29:11 UTC. Post-edit recompilat Release --no-restore: core 751/751, zero skip.
  Filtru (Drawing.Prism except PrismSdlGpuPixelConformanceTests) plus
  ScenePrismStreamingTests si PrismBackdropHostingContractTests. XML confirma
  toate cazurile baseline 631 si 260 prezente Passed; diferential/order/lifetime/
  bounds si allocation budgets existente trecute, fara metrici noi revendicate.
- SDL recompilat Release --no-restore, CERNEALA_SDL_NATIVE_TESTS=1: 121/121,
  zero skip; aceleasi opt selectii ca graph-analysis-model. XML confirma toate
  cele 121 precedente prezente Passed. Executie frozen/nested/retained, baseline
  si style numeric conformance trecute; first-present fake ramane distinct.
- Corpus Window.SaveScreenshot PrismSdlGpuPixelConformanceTests: 132/132,
  zero skip, Release --no-build --no-restore cu native activ. Raw 132 TXT + 396
  PNG; trei rapoarte inspectate 0/0/0, pragurile existente 1/10/49 neschimbate.
  Goldens pastrate; referinte istorice, nu oracle matematic/backend activ secundar.
- PrismAudit --check recompilat exit 0: 178 entries, 31 common properties,
  216 public Prism + 8 extended types, zero gaps. Docs/schema/manifest neschimbate.
  Raw evidence artifacts/prism-cleanup/graph-optimizer/: trei TRX, audit log,
  pixel-diff. Fara fisiere adaugate/eliminate, tree nu necesita refresh.
- Parent review sursa actuala/diff exact 0+/14-, interactiuni, raw XML case
  presence si checkpoint; git diff --check trecut. Explorer/joburi incheiate,
  fara temporare. Suita repo, human/cross-platform raman neverificate la acest
  checkpoint; nu sunt declarate complete.
- Bifat 1/1: total 116/312, raman 196. Goalul ramane activ.

### 2026-09-30 — raster lowering planner

- Target unic PrismRasterPlanner.cs, toate 273 linii citite; Luna/max read-only
  incheiat. Parent a citit integral PrismRasterPlannerTests, contractul tehnic
  extents-then-lower si callerii SDL pentru extent resolution, surface formats,
  auxiliary execution si inclusive release; native conformance relevante.
- Pastrat neschimbat: ownerul produce un plan typed fara alocare GPU, dupa
  dimensiuni cunoscute. Reuse cere aceeasi referinta semantic plan, dimensiuni si
  snapshot egal de scope extents; dictionarul callerului este copiat dupa Build.
  Null si empty map sunt echivalente cand snapshotul este gol. Extent record
  equality include X/Y desi emiterea pass-urilor foloseste numai Width/Height;
  nu schimb politica de reuse pe baza acestei observatii.
- Threshold are CDF R32Float BinCount x 1 si selection RGBA8 1 x 1, cu muchii
  explicite. Shadow spread/blur si distance/bevel sunt auxiliare typed in aceeasi
  ordine execution. Sharing este local unui Build, keyed StyleSource + coverage
  directionala; Glow/Bevel pot partaja, Stroke ramane separat. JFA+1 si ultimul
  pass jump=1 pastrate, fara inlocuire de algoritm sau helper generic.
- Semantic node plans/fingerprints sunt pastrate; auxiliarele au candidate None.
  Lifetime/peak sunt recalculate prin ownerul semantic Graph, inclusiv nested
  parent capture. SDL aloca/elibereaza lease-urile, nu plannerul. Nu mut
  responsabilitati intre backend si plan si nu duplic lifetime calculations.
- Gate-uri reutilizate din graph-optimizer, aceeasi stare de productie: core
  751/751, SDL 121/121, Window.SaveScreenshot 132/132, zero skip; PrismAudit zero
  gaps. XML inspectat confirma toate 10 cazuri PrismRasterPlannerTests Passed:
  1x1/2x1/32x16/48x32 JFA sequences, field sharing, typed threshold dependencies,
  semantic cache keys, mutable extent copy/reuse si empty dimensions rejection.
  Native XML confirma Glow holes/48px, cinci bevel profiles si shadow isotropy.
  Aceste verificari native sunt fixture renders, distincte de Window corpus.
- Raw evidence ramane artifacts/prism-cleanup/graph-optimizer/, nu pretind o
  rerulare noua identica. Nu exista productie/test nou, deci nu inventez RED sau
  characterization. Coverage nu include fiecare dimensiune/int boundary posibila.
  Fara metrici CPU/GPU noi, validare umana sau cross-platform revendicate.
- Parent review sursa actuala, interactiuni, diff clean pentru planner, raw XML
  si checkpoint; git diff --check trecut. Explorer/joburi incheiate, fara
  temporare; tree/docs/schema/manifest/goldens neschimbate.
- Bifat 1/1: total 117/312, raman 195. Sectiunea Graph complet rezolvata;
  suita repo ramane la final, goalul ramane activ.

### 2026-09-30 — immutable definition core

- Target 12 fisiere: CompositionDefinition, CurvePoint (si CurvesResource in
  acelasi fisier), DefinitionValidation, FilterDefinition, GroupDefinition,
  LayerDefinition, MaskDefinition, NodeDefinition, NodeId, ParameterKey{T},
  SourceSpan si StyleDefinition; parent citite integral. Luna/max read-only
  incheiat, cu cautari bounded de coverage pentru snapshot/equality/addressing.
  Callerii instance/store/generated emitter/graph si contractele canonice
  relevante inspectate; nu bifez resource definitions sau runtime din partial reads.
- Productie pastrata fara refactor justificat. Validation helper deja comun,
  copii ImmutableArray, equality/hash structurale si ordine declaration versus
  evaluation distincte. Layer/group au cerinte diferite, nu adaug baza/helper cu
  flags. Pastrez validation order, mesajele, catalog defaults, enum handling si
  valoarea default a record structs; constructor validation nu este un invariant
  universal al tuturor valorilor default. Nu mut validarea catalogului in key.
- Names sunt ordinal scoped paths, IDs globale pe copac; setul sibling names si
  indexul complet produc contexte de diagnostic distincte. SourceSpan ramane
  metadata exclusa din equality/hash. Masca pastreaza verificarea feather/density,
  curbele identity defaults/endpoints/strict-increasing si tipurile typed slots.
- Trei characterization tests permanente in PrismDefinitionContractTests:
  golirea celor sase liste caller-owned nu schimba snapshots/indexul; composition,
  group si layer din locatii sursa diferite pastreaza equality/hash; doua grupuri
  pot avea acelasi nume local, dar resping duplicate IDs globale/local names si
  lookup-ul ramane case-sensitive. Sunt contracte existente, nu bug fix/RED fictiv.
  Nu pretind coverage pentru orice invalid argument, anonymous address case sau
  enumeration adversarial; nici input processing/manual UI din constructii directe.
- Baseline Release --no-build --no-restore 791/791; recompilat Release
  --no-restore cu cele trei teste: 794/794, zero skip. Filtru UI.Prism plus
  Drawing.Prism except PrismSdlGpuPixelConformanceTests, ScenePrismStreamingTests
  si PrismBackdropHostingContractTests. XML confirma toate 791 baseline Passed
  si cele trei noi Passed; DefinitionContract are 14 cazuri, celelalte UI Prism
  7 Attachment + 11 Instance + 11 MotionIntegration, plus consumatori core.
- Raw evidence artifacts/prism-cleanup/definitions-core/: baseline si verified
  TRX. SDL 121/121, Window.SaveScreenshot 132/132 si PrismAudit zero gaps de la
  graph-optimizer reutilizate: productie identica, nu rulari noi. Teste noi doar
  core; fara schimbari API/docs/schema/manifest/goldens si fara fisiere noi.
- Parent review sursa actuala, diff numai 84 linii de characterization, callerii,
  contractele, XML case presence si checkpoint; productie Definitions diff clean,
  git diff --check trecut. FileTree regenerat la inceput Unchanged; explorer/joburi
  incheiate, fara temporare. Full repo/human/cross-platform raman neverificate.
- Bifate 12/12: total 129/312, raman 183. Suita repo ramane la final; goal activ.

### 2026-09-30 — typed immutable definition resources

- Target sase fisiere: ColorMatrixResource, GradientMapResource, LensProfileJson,
  LensProfileResource, LightingResource si ResourceId. Parent citite integral,
  callerii typed draw-resource resolution, LUT/lens consumers, teste existente si
  contractele canonice inspectate. Luna/max read-only incheiat: coverage JSON.
- Productie pastrata neschimbata. Matrix/offset au validari finite distincte;
  gradient stops sunt nondecreasing (hard stops), diferite de curbele strict
  increasing. Copiile immutable si identitatea/versionarea typed raman la owner.
  ResourceId pastreaza key si FNV UTF16 32-bit pozitiv; nu il confund cu hash-ul
  de fingerprint 64-bit si nu schimb equality intre named si numeric IDs.
- Lens regions sortate, nonoverlapping cu endpoints comune permise; sparse
  polynomial pastreaza ordinea termenilor, acumularea double si exponentii 0..2.
  Lighting pastreaza HDR nonnegative, directional normalization, wrong-kind
  access errors si respingerea default light. Nu adaug clamps sau validari noi.
- Trei cazuri permanente de characterization JSON: unknown property, wrong-case
  property si invalid pupil grid. Fixture valida verificata inainte de mutatie;
  Parse si Load resping cu JsonException, streamul ramane deschis, grid invalid
  pastreaza ArgumentOutOfRangeException ca inner exception. Nu inventez RED sau
  coverage pentru malformed/null/culture, toate valorile limite ori input UI.
- Release recompilat --no-restore, acelasi filtru core/UI de la definitions-core:
  797/797, zero skip. Raw XML confirma toate 794 baseline Passed prezente si
  toate trei cazuri noi Passed. TRX:
  artifacts/prism-cleanup/definition-resources/definition-resources-core-verified.trx.
  SDL 121/121, Window.SaveScreenshot 132/132 si audit zero gaps reutilizate din
  graph-optimizer: aceeasi productie, nu rulari noi revendicate.
- Parent review actual source, test diff 30 linii, contract/error envelope,
  raw XML/case presence si checkpoint; Definitions production diff clean,
  git diff --check trecut. Explorer si joburi incheiate, fara temporare/fisiere
  noi; tree/docs/API/schema/manifest/goldens neschimbate. Full repo, human,
  cross-platform si metrici CPU/GPU noi nu sunt declarate verificate.
- Bifate 6/6: total 135/312, raman 177. Definitions complet; goal activ.

### 2026-09-30 — generated markup runtime bridges

- Target GeneratedMarkupPrism.cs, toate 440 linii citite; Luna/max read-only
  incheiat. Parent a inspectat callerii SourceGen markup/Motion, typed state
  access, attachment, MarkupObservation, Relay dispatcher, contractul canonic,
  testele de binding/lifecycle/Motion si generated-assembly execution.
- Eliminat exclusiv campul private readonly owner si atribuirea this.owner:
  0+/2-. Nicio citire a campului in clasa non-partial; lambda dispatcherului
  captureaza parametrul constructorului (shadowing), pastrata identic. Nu mut
  ownerul lifecycle sau Relay si nu revendic masuratori de performance/memorie.
- API-urile publice typed sunt punti folosite de generator, nu dead wrappers;
  nu le inlocuiesc cu lookup generic, delegates suplimentare ori reflection.
  Get inainte de Motion Set, order Start/set/finally Stop pentru direct reference,
  equality/no-op, reentrancy, TwoWay event subscription si idempotent disposal
  neschimbate. Generic UiProperty binding are value-source/base-layer semantics
  diferite de Prism; nu extrag un controller comun dupa asemanare sintactica.
- Baseline inainte de edit: SourceGen recompilat 614/614; UI.Prism, UI.Markup,
  UI.Relay si DrawCommandListBuilder tests 192/192. Dupa edit: recompilat core
  union cu Prism/scene/backdrop gates 946/946, apoi SourceGen 614/614. Zero skip;
  XML confirma toate 797 core + 192 runtime baseline cazuri (43 comune) si toate
  614 SourceGen baseline prezente Passed, inclusiv TwoWay reset/replacement si
  generated factories shared definition/independent instances/lifecycle cleanup.
- PrismAudit recompilat exit 0: 178 entries, 31 common properties, 216 public
  Prism + 8 extended types, zero gaps. Raw evidence artifacts/prism-cleanup/markup:
  patru TRX baseline/post si audit log. Nu exista test nou sau RED fictiv pentru
  eliminarea unui camp necitit. Direct-reference exception/disposal si OneWay
  controller runtime gaps raman explicit neacoperite; codegen text nu le probeaza.
- Parent review actual source, diff exact, capture binding, interactiuni, raw
  XML si checkpoint; git diff --check trecut. Jobs/explorer incheiate, fara
  temporare/fisiere noi, tree/docs/schema/manifest/goldens neschimbate. SDL/Window
  conformance istoric nu este o rulare pe noul assembly; nu s-au schimbat render,
  backend, routing sau invalidation paths, deci nu cer o noua matrice vizuala
  pentru campul necitit. Corpusul complet ramane gate la final. Full repo, human
  si cross-platform neverificate; fara input UI/manual revendicat.
- Bifat 1/1: total 136/312, raman 176. Goal activ; suita repo ramane la final.

### 2026-09-30 — runtime blend and version value contracts

- Target PrismAdvancedBlend.cs (76 linii) si PrismVersions.cs (10 linii), citite
  integral; Luna/max bounded read-only incheiat. Parent a inspectat canonicele,
  setters/conversiile typed, instance version increments/no-op, Graph snapshots,
  blend/channel consumers, retained fingerprint components si teste relevante.
- Pastrate neschimbate: flags/enums publice cu numeric identities catalog,
  BlendRange finite/unit/ordered, conversion order si mesaje/param names. Nu
  reunesc ValidateThreshold cu UnitInterval doar fiindca au acelasi predicate:
  diagnostic contract diferit. Egalitatea pragurilor este permisa si consumerul
  trateaza rampele degenerate ca hard cutoffs; nu fac pragurile strict increasing.
- StructuralVersion si ValueVersion raman tipuri nominale separate. Doua Next
  de o linie cu checked(Value + 1) nu justifica infrastructura generica/un tip
  comun. Constructorii publici nu adauga validation, default ramane zero;
  instance owner decide ce schimbare incrementeaza fiecare versiune. Nu bifez
  Instance/States/Store din aceste reads partiale.
- Gate core 946/946, SourceGen 614/614 si PrismAudit zero gaps reutilizate din
  markup: aceeasi productie actuala, fara rerulari identice revendicate. XML
  confirma AdvancedKnockout trei cazuri, advanced graph snapshot, catalog
  defaults, no-op/reset/replacement, Motion version separation Passed.
- Rulare noua SDL test project recompilat Release --no-restore, filter
  FullyQualifiedName~PrismBlendMathMigrationTests: 14/14, zero skip. Raw TRX
  artifacts/prism-cleanup/runtime-values/runtime-values-blend-verified.trx;
  cinci BlendIf linear split feather cazuri, sase PassThrough-vs-Normal cu
  advanced options si trei opaque sentinels Passed. Sunt CPU math tests in
  proiectul SDL, nu GPU renders sau Window screenshots.
- Coverage limits: fara teste targeted invalid BlendRange constructor inputs
  sau version long.MaxValue overflow; inspectia checked nu este runtime overflow
  validation. Fara bug/refactor pentru care sa inventez RED/new characterization.
- Parent review actual source, caller contracts, target diff clean, raw XML,
  checkpoint; git diff --check trecut. Jobs/explorer incheiate, fara temporare
  sau fisiere noi. Tree/docs/API/schema/manifest/goldens neschimbate. Full repo,
  human/cross-platform si performance metrics noi raman neverificate la checkpoint.
- Bifate 2/2: total 138/312, raman 174 (169 Filters + 5 Runtime). Goal activ.

### 2026-09-30 — per-element attachment lifecycle owner

- Target PrismAttachment.cs integral (316 linii); Luna/max read-only incheiat.
  Parent a citit integral AttachmentTests si a inspectat ElementLifecycle,
  UIElement attach/detach/renderability/invalidation, render command builder,
  Motion current-instance/invalidation consumers si contractele canonice.
- Productie pastrata: CWT per-element, replacement dispose-before-register,
  token invalidation inainte de Root=null, cleanup finally si reverse binding
  disposal. Initial hidden -> first visible pastreaza instanta; visible -> hidden
  -> visible creeaza instanta/token noi. Nu extrag un helper cu flags care ar
  unifica gresit aceste ramuri ori rollback si disposal ownership distincte.
- Doua characterization Facts permanente, 68 linii in AttachmentTests: un
  binding factory arunca dupa doua lifetimes create, acestea sunt eliberate
  reverse si CWT/render state sunt eliminate; un disposer arunca, celelalte doua
  sunt tot eliberate, AggregateException pastreaza failure, dispose repetat este
  no-op. Ambele verifica posibilitatea unui nou attachment pe owner deja attached.
  Reverse order este caracterizarea mecanismului existent, nu o noua promisiune
  API documentata. Nu schimb exception expectations ori productie pentru GREEN.
- Baseline markup core 946/946; recompilat Release --no-restore cu acelasi filtru
  core/UI/scene/backdrop: 948/948, zero skip. XML confirma toate 946 baseline
  Passed si cele doua noi Passed, inclusiv testul existent 10,000 lifecycle cycles.
  Raw TRX artifacts/prism-cleanup/attachment/attachment-core-verified.trx.
  SourceGen 614/614 si audit zero gaps markup, plus math SDL14, sunt aceeasi
  productie reutilizata, nu rulari noi. Fara backend/render code changes.
- Parent review sursa/diff actual, public entry fixture attached assertion,
  cleanup ownership, raw case presence si checkpoint; git diff --check trecut.
  Explorer/joburi incheiate, fara temporare/fisiere noi; FileTree refresh Unchanged
  la inceput. API/docs/schema/manifest/goldens neschimbate. Null factory,
  simultaneously failing factory+disposer si every hidden failure branch raman
  neacoperite; nu pretind UI input/manual/GPU/cross-platform/full repo validation.
- Bifat 1/1: total 139/312, raman 173 (169 Filters + 4 Runtime). Goal activ.

### 2026-09-30 — runtime graph construction and typed parameter storage

- Target cele patru fisiere runtime ramase, citite integral; Luna/max bounded
  read-only incheiat. Parent a inspectat generatorul typed slots/descriptors,
  catalog defaults/domains, definition/topology, graph snapshot consumers,
  canonical state/instance contracts, generated setter si metadata-access tests.
- PrismInstance: eliminat exclusiv array-ul local roots neconsumat, 1+/2-.
  Create nu citea/returna/captura array-ul; fiecare BuildNode insereaza state-ul
  in dictionary, iar group states pastreaza separat children. Ordinea construirii,
  slot allocation, generatia, dictionary identity si definition traversal raman
  identice. Discard-ul pastreaza apelul BuildNode; return value ramane necesar
  pentru copii. Fara API/architecture changes sau performance claims masurate.
- Validation, ParameterStore si States pastrate neschimbate. Cele sase storage
  lanes si nominal keys sunt existente; OperationParameterAccess deja unifica
  filter/style access. Float.Equals vs celelalte equality operators, range parsing
  float vs double, symbol FNV signed vs resource positive IDs, validation/error
  order si no-op/version notification semantics nu sunt duplicare echivalenta.
  Nu adaug generic store/controller, reflection ori validation caching speculativ.
- Cinci characterization cazuri permanente, 46 linii in PrismInstanceTests:
  public metadata setter respinge NaN, infinity si doua valori out-of-range fara
  schimbarea valorii sau versiunilor; required resource respinge default dupa
  setarea unui ID valid, pastrand valoarea si ValueVersion. Existing generated-key
  domain tests nu probau aceasta cale publica. GREEN inainte de productie,
  fara RED fictiv pentru refactor sau expectations schimbate.
- Baseline core attachment 948/948; cu noile cazuri 953/953 inainte de edit si
  953/953 dupa edit, Release rebuild --no-restore, acelasi union filter core/UI/
  scene/backdrop. Dupa edit SourceGen recompilat 614/614, SDL affected native
  121/121 cu CERNEALA_SDL_NATIVE_TESTS=1. XML confirma toate baseline cases
  prezente Passed; zero skip la toate aceste rulari.
- Dupa edit Window conformance --no-build --no-restore: 132/132, zero skip,
  toate historical baseline cases prezente. Screenshot exclusiv Window.SaveScreenshot;
  praguri neschimbate MAE<=1, P99<=10, max<=49. Persistate 132 pixel-diff.txt si
  396 PNG prin artifact helper; comparatie cu referinte istorice, nu oracle
  matematic ori validare manuala. PrismAudit recompilat --check exit 0:
  178 entries, 31 common properties, 216 public Prism + 8 extended, zero gaps.
  Raw TRX, audit log si imagini: artifacts/prism-cleanup/runtime-store/.
- Parent review actual source/diff, graph ownership, raw counters/case presence,
  Window artifact helper/reports si gates; git diff --check trecut. Explorer si
  toate joburile incheiate, fara temporare/fisiere noi. Tree/docs/API/schema/
  manifest/goldens neschimbate. Equal-definition live handles, version overflow,
  negative integer domains si toate vector branches nu au targeted tests noi;
  nu revendic acoperire exhaustiva, CPU/GPU metrics sau input UI/manual validation.
- Bifate 4/4: total 143/312, raman 169, exclusiv Filters. Runtime complet;
  full repo si cross-platform gates raman la final. Goal activ.

### 2026-09-30 — color adjustment math, lowering and lookup utilities

- Target 25 fisiere: AdjustmentMath/Planner/FilterParameterReader, cele 16
  operation helpers si CurveLut/GradientMapLut/HaldLut/LevelsAnalysis/
  ThresholdAnalysis/Okhsl. Parent citit integral targetele, direct graph snapshot,
  optimizer/raster/executor callers, HLSL dispatcher, resource contracts si teste.
  Luna/max bounded read-only incheiat; fara delegare de edits/review/checks.
- Eliminat numai PrismColorLookupFilter.ApplyLookup, 0+/8-: internal helper in
  clasa internal static non-partial, fara caller/sourcegen/reflection/dynamic/
  serializer/docs match in repo. Callerul efectiv foloseste Apply, pastrat identic:
  Hald precedence, clamping si callback failure. Nu transform lookup intensity
  intr-o promisiune API si nu schimb operation enum/uniforms/shader/catalog.
- Celelalte 24 targets pastrate. Plannerul detine catalog-to-uniform lowering;
  readerul comun existent valideaza count/index/kind si symbol order. Managed
  math este folosit de teste, nu o cale C# de pixel execution gasita in productie;
  SDL consuma planul in HLSL. Nu confund CPU unit GREEN cu GPU execution.
  PCHIP channel-before-composite, Hald trilinear, gradient hard stops/dither,
  clipped-level histogram si Otsu au contracte distincte; nu extrag un generic
  LUT/histogram helper si nu rescriu algoritmi sau float evaluation order.
- Baseline core runtime-store 953/953, inclusiv 34 adjustment cases; baseline
  nou SDL --no-build 9/9 (8 native + 1 FakeSdl texture/resource-lifetime Fact).
  Dupa edit Release rebuild --no-restore: core union 953/953, SourceGen614/614,
  SDL affected 41/41 (baseline9 + BaselineConformance32), native env=1. XML
  confirma toate baseline cases Passed. Zero skip. Fara teste noi/RED fictiv
  pentru helperul neapelat; existing CPU callbacks/Hald/profile/alpha tests raman.
- Window post-edit --no-build --no-restore 132/132, zero skip, toate baseline
  cases Passed; Window.SaveScreenshot exclusiv. Aceleasi praguri MAE<=1,
  P99<=10, max<=49, 132 reports + 396 PNG persistate. Historical references,
  nu oracle matematic; resource-free/default corpus nu probeaza fiecare shader
  branch sau fiecare resource path. Direct GPU-vs-CPU comparisons din lot sunt
  ChannelMixer si Posterize; typed curves/Hald failures sunt native resource tests.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw evidence in
  artifacts/prism-cleanup/adjustments/: baseline/post TRX, audit log, pixel-diff/.
  Parent review actual source/diff, caller identity, raw XML/reports si gates;
  git diff --check trecut. Explorer si toate joburile terminale, fara temporare
  ori fisiere noi; API/docs/schema/manifest/goldens/tree neschimbate. Reader
  malformed graph si every LUT/histogram invalid argument branch nu au targeted
  coverage noua. Fara performance, input UI/manual ori cross-platform claims.
- Bifate 25/25: total 168/312, raman 144 Filters. Goal activ; full repo la final.

### 2026-09-30 — catalog color, shared pixel helpers and stochastic operations

- Target 13: CatalogFilterMath/CatalogColorMath, Color/ColorMatrix/
  CustomConvolution/Solarize/NtscColors/ChromaticAberration/Pointillize/
  IncrementalVoronoiSet/Scanlines/AddNoise/FilmGrain. Parent citit integral
  targetele, callers, contracte si teste relevante. Luna/max bounded read-only
  incheiat; parent detine edit, verificare si review.
- Eliminat numai private RotateHue din CatalogFilterMath, 0+/13-. Clasa
  internal static non-partial, fara exact-token referinte in source/docs/
  generator/shaders; audit reflection inspectat enumera public members,
  retained-cache reflection inspectat enumera instance fields, nu acest helper.
  Rotația executata pentru Color este OKLab in PrismColorFilter, neschimbata.
  Nu revendic excluderea oricarei reflectii externe arbitrare asupra internals.
- Celelalte 12 fisiere pastrate. ParameterMagnitude are doi callers prin
  using-static in GeometryMath si ArtisticMath; nu este dead code. Voronoi Rank
  serveste si SDL gradient dither, Center serveste Reticulation. Pastrate seed
  split high bits, sampling/edge modes, associated alpha, optional typed matrix,
  signed cube roots, extended half-float bounds si evaluation order. Color OKLab
  vs Okhsl, Gaussian noise vs FilmGrain spatial normalization, straight vs
  associated luminance si convolution scalar kernel nu sunt duplicare echivalenta.
- Managed catalog math este CPU oracle/test path; repo search nu a gasit
  productie care apeleaza Apply. SDL executa implementari HLSL separate.
  Existing CPU tests probeaza hue/chroma/lightness, affine RGBA, threshold
  equality, convolution edges, chromatic alpha, scanline coverage, blue-noise
  ranks, film-grain signal/spatial controls si Gaussian noise distribution.
  Fara teste noi sau RED fictiv pentru private helper neapelat.
- Baseline core 953/953 din adjustments (aceeasi productie pre-edit), baseline
  nativ nou 10/10. Post-edit Release rebuild --no-restore: core 953/953,
  SourceGen 614/614, SDL 42/42 (baseline10 + BaselineConformance32), native env=1.
  XML confirma baseline case names prezente Passed; zero skip.
- Prima core recompilare a esuat inainte de teste: MSB3073, Access denied la
  Scene2D LDtk staging in Playground obj. Read-only inspection a gasit staging
  absent dupa exit; cauza ramane neatribuita. Aceeasi comanda repetata o singura
  data a recompilat pachetele si a trecut 953/953. Fara Scene2D edits, permisiuni
  schimbate, deletion, build-gate bypass ori suppressions. Mesajul initial si
  output-ul complet al repetarii pastrate in artifacts; nu ascund esecul.
- Window post-edit --no-build --no-restore: 132/132, zero skip, toate historical
  cases Passed. Exclusiv Window.SaveScreenshot, praguri MAE<=1, P99<=10, max<=49;
  persistate 132 reports + 396 PNG. Maxime observate in reports: MAE=0.3704,
  P99=10, max=41. Historical reference disagreement gate, nu oracle matematic.
  Dedicated native CPU-vs-GPU cazuri pentru Color/Matrix/Chromatic/Solarize/
  Scanlines/Convolution; AddNoise deterministic/full-seed native case.
  Fara dedicated native mathematical oracle nou pentru Pointillize/NTSC/FilmGrain;
  resource-free/default Window corpus nu probeaza toti parametrii/resources.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX, audit, build failure/repeat
  si pixel-diff: artifacts/prism-cleanup/catalog-color/. Parent review actual
  source/diff, callers, raw counters/case presence si reports; git diff --check
  trecut. Explorer si toate joburile proprii incheiate; fara temporare/API/docs/
  manifest/schema/shader/golden/tree changes. Fara performance, input UI/manual
  sau cross-platform validation claims.
- Bifate 13/13: total 181/312, raman 131 Filters. Goal activ; full repo la final.

### 2026-09-30 — procedural/video math and seeded screens

- Target 10: CatalogProceduralMath, Clouds/DifferenceClouds, Fibers/FibersNoise,
  Deinterlace, Mezzotint/MezzotintThreshold, ColorHalftone/HalftonePattern.
  Parent citit integral toate targetele, lowering/selectors/shader paths,
  canonical operation pages si existing CPU/native tests relevante. Luna/max
  bounded read-only incheiat; niciun edit/check/review delegat.
- Eliminat primaryResource nefolosit din Procedural si din singurul caller
  in CatalogFilterMath: 0+/1- in fiecare fisier. Complete body nu citea acel
  parametru; call-site transmitea doar variabila, nu un callback invocation.
  Apply pastreaza semnatura si required-resource validation/error order;
  Texture/Convolution pastreaza acel callback. Auxiliary height si typed
  lighting raman transmise identic. CatalogFilterMath deja bifat a fost
  re-review-uit si reverificat, nu o a doua bifa.
- Celelalte 9 targete pastrate. Clouds/DifferenceClouds deja folosesc o cale
  comuna; bool difference este contract intern existent, nu propun un nou
  bool-switch helper. Fibers are Perlin anisotropic five-octave/full-seed math,
  Mezzotint are doua rank screens si footprint/phase proprii; hash/fade/rank
  syntax similar nu justifica generic noise ownership/coupling.
  HalftonePattern dot area foloseste asin; ColorHalftone circular threshold
  foloseste o alta functie coverage, patru screen angles si CMYK. Nu le unific.
  GGX helper are caller unqualified cu using-static in CatalogTextureMath
  PlasticWrap; pastrat. Deinterlace foloseste associated luminance + alpha
  cost, distinct de straight luminance; pastrate tie/evaluation order si edges.
- Baseline core 953/953 din catalog-color, cu 21 relevant named cases Passed;
  nativ baseline nou 5/5 (clouds CPU spectral comparison, halftone tone/alpha,
  color-halftone angles, packed lighting/height/exposure, Fibers render coherence).
  Fara characterization noua/RED fictiv pentru parameter nefolosit; zero
  behavioral expectations changes.
- Dupa edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  SDL native 37/37 (baseline5 + BaselineConformance32), native env=1.
  Raw XML confirma toate baseline cases prezente Passed; zero skip. Core rebuild
  a pregatit si ambele Scene2D Playground packages fara Access denied;
  cauza esecului anterior ramane neatribuita, nu declar defectul extern reparat.
- Window post-edit --no-build --no-restore 132/132, zero skip, toate baseline
  cases Passed. Window.SaveScreenshot exclusiv; thresholds neschimbate
  MAE<=1, P99<=10, max<=49. Persistate 132 reports + 396 PNG; maxime reports
  MAE=0.3704, P99=10, max=41. Historical default/resource-free corpus, nu
  validare exhaustiva parameter/resources ori oracle CPU pentru toate filtrele.
  Nu exista un dedicated DifferenceClouds mathematical oracle nou, nici
  assertions directe noi pe FibersNoise/MezzotintThreshold private helpers.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX/logs/pixel-diff:
  artifacts/prism-cleanup/procedural/. Parent review actual source/diff,
  callback/resource flow, CPU/HLSL ownership, raw counters/case presence si
  reports; git diff --check trecut. Explorer si toate joburile proprii terminale.
  Fara temporare, API/docs/manifest/schema/shader/golden/tree modifications;
  fara CPU/GPU performance claims, user-input/manual sau cross-platform validation.
- Bifate 10/10: total 191/312, raman 121 Filters. Goal activ; full repo la final.

### 2026-09-30 — quantization filters and Recursive Wang field

- Target 7: CatalogQuantizationMath, Crystallize/Cutout/Facet/Fragment/Mosaic
  si RecursiveWangBlueNoise. Parent citit integral toate targetele, direct CPU/
  SDL callers, asset ownership, shader/selector context, canonical operation
  docs si teste relevante. Luna/max bounded read-only incheiat; parent detine
  toate decisions/edits/checks/review.
- Eliminat numai Generate(ReadOnlySpan<byte>, int), overload intern neapelat,
  0+/20- in RecursiveWangBlueNoise. Repo current source/tests/generator/audit
  cautate fara caller exact; clasa internal static non-partial, fara contract
  public. Overload-ul dubla Parse + root selection. Nu adaug helper/extraction
  pentru o cale inutilizata. CreateField(span), embedded resource loader,
  private Generate(TileSet,Tile,int), Lazy ExecutionAndPublication, point ranks,
  packing, seed offsets si exception paths folosite raman identice.
  Text searches nu exclud reflectie externa arbitrara asupra internals.
- Celelalte 6 targets pastrate. FacetStructureTensor/FacetSectorWeights au
  callers unqualified prin using-static in PainterlyMath si TextureMath;
  nu sunt dead helpers. Facet fixed anisotropic Kuwahara, painterly polynomial
  variants, bilateral Mosaic, mean-shift Cutout si 3x3 nearest-generator
  Crystallize au contracte diferite, nu un generic blur/cell helper. Fragment
  pastreaza bilinear fractional offsets; Crystallize/Mosaic center sampling
  nu este echivalent. Fara schimbari de algoritm, evaluation order ori clamps.
- Baseline core procedural 953/953 (aceeasi productie pre-edit), inclusiv
  10 named Cutout/Crystallize/Facet/Fragment/Mosaic/RecursiveWang cases Passed.
  Baseline SDL nou 4/4: Spatter CPU-vs-GPU mean error, Cutout multipass,
  Mosaic bilateral rendering, builtin texture warm lookup allocation test.
  Fara teste noi sau RED fictiv pentru overload neapelat.
- Dupa edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  SDL native 36/36 (baseline4 + BaselineConformance32), native env=1. XML
  confirma toate baseline cases prezente Passed, zero skip. Built-in Wang
  field test verifica 65,536 occupied points/progressive ranks; GPU parity test
  pastreaza mean-difference tolerance <0.06, nu o promisiune pixel-perfect.
- Window post-edit --no-build --no-restore: 132/132, zero skip, toate baseline
  cases Passed. Window.SaveScreenshot exclusiv; historical resource-free/
  default corpus, thresholds MAE<=1, P99<=10, max<=49. Persistate 132 reports
  si 396 PNG; maxime reports MAE=0.3704, P99=10, max=41.
  Fara dedicated native mathematical oracle nou pentru Crystallize/Facet/
  Fragment; malformed tileset/root selection/lazy concurrency nu au tests noi.
  Nu revendic coverage exhaustiva, manual/input UI ori cross-platform validation.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX/logs/pixel-diff:
  artifacts/prism-cleanup/quantization/. Parent review actual source/diff,
  overload identity/callers, resource flow, raw XML baseline presence si reports;
  git diff --check trecut. Toate joburile proprii si explorer terminale.
  Tree generator Unchanged; fara temporare/API/docs/manifest/catalog/shader/
  golden changes. Fara CPU/GPU performance improvement claims.
- Bifate 7/7: total 198/312, raman 114 Filters. Goal activ; full repo la final.

### 2026-09-30 — artistic filters and surface textures

- Target 15: CatalogArtisticMath, CatalogTextureMath, SurfaceTexture si 12
  wrappers DryBrush/AngledStrokes/PaintDaubs/PaletteKnife/PlasticWrap/
  RoughPastels/SmudgeStick/Sponge/Underpainting/Crosshatch/Spatter/SprayedStrokes.
  Parent citit integral targetele, direct dispatch/planner/callers, relevante
  CPU/native tests, shader si canonical docs context. Luna/max bounded
  read-only incheiat; decisions/edits/checks/review apartin parentului.
- Eliminat numai private Fraction(float) din CatalogTextureMath, 0+/3-.
  Cele doua calls din ProceduralTextureHeight folosesc helperul identic din
  CatalogFilterMath, deja importat using-static. Aceeasi expresie float
  value - MathF.Floor(value), inclusiv negative coordinates; fara helper nou,
  schimbare de dependencies, evaluation order, algoritm sau contract public.
  Exact-token source/generator/reflection searches fara alte referinte la
  metoda privata; nu exclud reflectie externa arbitrara asupra internals.
- Celelalte 14 targets pastrate. PolynomialAnisotropicKuwahara are caller
  extern real OilPaint prin using-static GeometryMath, nu este dead code.
  SurfaceTexture continuous ValueNoise si Catalog procedural texture height
  au seed/sampling/formule diferite, nu le unific. Hash-ul local SurfaceTexture
  ramane: evit dependenta suplimentara de catalog math pentru acest primitive.
  Artistic fallback, GGX, sector kernels, seed packing, Wang point field,
  bilinear sampling, thresholds si associated-alpha flow raman neschimbate.
- Baseline core quantization 953/953 reutilizat: aceeasi productie pre-edit;
  inclusiv 42 named artistic/texture-consumer cases Passed. Baseline SDL nou
  21/21, zero skip. Nu adaug teste sau RED fictiv pentru deduplicare echivalenta;
  RoughPastels/Underpainting existing tests exercita inclusiv Brick texture.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  SDL native 53/53 (baseline21 + BaselineConformance32), zero skip, native=1.
  Raw XML confirma toate baseline core/native cases prezente Passed.
- Window conformance 132/132, zero skip, toate baseline cases Passed;
  inclusiv toate cele 12 operation entries si Texturizer/ConteCrayon consumers.
  Window.SaveScreenshot exclusiv, 132 reports/396 PNG, maxime MAE=0.3704,
  P99=10, max=41 sub pragurile 1/10/49. Historical resource-free/default corpus,
  nu oracle exhaustiv CPU-vs-GPU pentru toate filtrele/parameters/resources.
  Fara isolated ValueNoise tests noi sau perf/input/manual/cross-platform claims.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX/logs/pixel-diff:
  artifacts/prism-cleanup/artistic-texture/. Parent review actual source/diff,
  binding/callers/ownership si raw counters/case presence/reports trecut;
  git diff --check trecut. Explorer si toate joburile proprii terminale.
  Tree generator Unchanged; fara temporare/API/docs/manifest/catalog/shader/
  golden modifications. Full repo suite ramane la final.
- Bifate 15/15: total 213/312, raman 99 Filters. Goal activ.

### 2026-10-01 — painterly pipelines and paper consumers

- Target 9: CatalogPainterlyMath, Fresco/ColoredPencil, ConteCrayon,
  ChalkCharcoal/SumiE/Watercolor wrappers, WaterPaper si Texturizer. Parent
  citit integral targetele, direct CPU dispatch/callees/planner, test scenarios,
  native/HLSL route si canonical docs. Luna/max bounded read-only incheiat;
  parent detine decisions/edits/checks/review, fara delegated verification.
- Eliminat numai private StraightLuminance(Vector3) din PainterlyMath, 0+/4-.
  Cele trei calls ComposeColoredPencil folosesc implementarea identica din
  CatalogFilterMath deja importata using-static: acelasi Vector3.Dot si aceiasi
  coeficienti float. Fara helper/dependency/API/algorithm/alpha changes.
  Source/SourceGen/reflection name searches fara alte referinte la helperul
  privat; nu exclud reflectie externa arbitrara asupra internals.
- Celelalte 8 targets pastrate. Fresco float bounded blur si ColoredPencil
  integer-ceiling blur au radius/normalization contracts diferite, nu le
  unific. WaterPaper hash multiplica seed-ul; nu este SurfaceTexture.Hash.
  WaterPaper Unpremultiply are MinimumAlpha diferit de common math <=0.
  ConteCrayon foloseste Charcoal.Analyze si SurfaceTexture existent; Texturizer
  pastreaza custom resource branch, wrap si Scharr height gradient. Ink wrappers
  pastreaza ownerul stabilit; CatalogInkMath ramane target separat nebifat.
- Baseline core artistic-texture 953/953 reutilizat, aceeasi productie pre-edit,
  inclusiv 18 named painterly/consumer cases Passed. Baseline SDL nou 13/13,
  zero skip. Existing ColoredPencil test exercita output/control/alpha path;
  fara teste noi sau RED fictiv pentru deduplicare echivalenta.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  SDL native 45/45 (baseline13 + BaselineConformance32), native=1, zero skip.
  Raw XML confirma toate baseline core/native cases prezente Passed.
- Window conformance 132/132, zero skip, toate baseline cases Passed si toate
  cele 8 operation entries prezente. Window.SaveScreenshot exclusiv; persistate
  132 reports/396 PNG. Maxime MAE=0.3704, P99=10, max=41, praguri 1/10/49.
  Historical resource-free/default corpus, nu exhaustive parameter/resource
  coverage sau CPU oracle pentru toate pipelines. Dedicated GPU tests verificate
  pentru Fresco/ColoredPencil/ConteCrayon/WaterPaper/Texturizer; nu exista teste
  dedicate gasite pentru ChalkCharcoal/SumiE/Watercolor, acoperite CPU si Window.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX/logs/pixel-diff:
  artifacts/prism-cleanup/painterly/. Parent review actual source/diff,
  helper binding/ownership si raw counters/baseline presence/reports trecut;
  git diff --check trecut. Explorer si toate joburile proprii terminale.
  Tree generator Unchanged; fara temporare/API/docs/manifest/catalog/shader/
  golden changes, performance improvement/manual/input/cross-platform claims.
- Bifate 9/9: total 222/312, raman 90 Filters. Goal activ; full repo la final.

### 2026-10-01 — ink thresholds and guided relief

- Target 7: CatalogInkMath, CatalogReliefMath, AccentedEdges/DarkStrokes/
  InkOutlines/PosterEdges/BasRelief wrappers. Parent citit integral targetele,
  CPU dispatch/callees/planner, affected test scenarios, native/HLSL routes si
  canonical docs. Luna/max bounded read-only incheiat; parent detine decisions,
  edits, verificarea si review-ul, fara delegated verification.
- Centralizat cele cinci expresii identice response/epsilon/Tanh/clamp intr-un
  private XDogThreshold din ownerul existent CatalogInkMath, 16+/30-.
  ChalkCharcoal/AccentedEdges/DarkStrokes/InkOutlines/SumiE pastreaza fiecare
  propriile controale, normalizarea alpha-weighted, constantele (inclusiv phi=24
  pentru DarkStrokes), response calculation, branch si compozitia ulterioara.
  Fara helper public, mutare de ownership, dependinte sau schimbare de algoritm.
- Celelalte 6 targets pastrate. GuidedFilter si GuidedScharrGradient au callers
  reali in BasRelief si PosterEdges; PosterEdgesSample are opt calls live.
  Guided box blur scalar si XDoG Gaussian blur weighted nu au acelasi contract.
  PrismXDogLuminance.Build foloseste alta clamp/unpremultiply policy decat Ink;
  nu le unific. Watercolor sampling/morphology/paper/density raman neschimbate.
- Baseline reutilizat din painterly post-cleanup, aceeasi productie pre-edit:
  core 953/953, inclusiv 19 named target/consumer cases, si native 45/45,
  inclusiv cele 5 XDoG/PosterEdges/BasRelief cases. Fara teste noi sau RED fictiv
  pentru extractie echivalenta; nu exista boundary tests noi pentru threshold.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614.
  Prima comanda core a esuat la Tee-Object deoarece directorul nu exista;
  TRX-ul initial confirma totusi 953/953, pastrat separat. Director creat si
  rerulare cu log valid 953/953. Prima comanda SDL a folosit variabila gresita:
  2 Passed/13 Skipped, pastrata separat si neacceptata ca gate. Rerulare cu
  CERNEALA_SDL_NATIVE_TESTS=1 pe build-ul nou: 40/40 (Multipass8 + Baseline32),
  zero skip. Raw XML confirma core953/SourceGen614 baseline cases prezente
  Passed si toate cele 40 native cases prezente Passed in baseline-ul de 45.
- Window conformance 132/132, zero skip, toate baseline cases Passed, inclusiv
  cele 8 ink/relief/Watercolor operation entries. Window.SaveScreenshot exclusiv;
  132 reports/396 PNG, 132 unique symbols. Maxime MAE=0.3704, P99=10, max=41,
  praguri 1/10/49. Historical resource-free/default corpus, nu exhaustive
  parameter/resource coverage sau CPU oracle pentru toate pipelines. Dedicated
  native CPU parity pentru AccentedEdges/DarkStrokes/InkOutlines/BasRelief;
  PosterEdges native verifica guided/quantization/edge composite. ChalkCharcoal/
  SumiE/Watercolor au CPU si Window coverage, nu dedicated native parity gasit.
- PrismAudit rebuild --check exit0: 178 entries, 31 common properties,
  216 public Prism + 8 extended, zero gaps. Raw TRX/logs/pixel-diff:
  artifacts/prism-cleanup/ink-relief/. Parent review actual source/diff,
  helper binding/callers/ownership, raw counters/baseline presence/reports si
  git diff --check trecut. Explorer si toate joburile proprii terminale.
  Tree generator Unchanged; fara temporare/API/docs/manifest/catalog/shader/
  golden changes sau performance improvement/manual/input/cross-platform claims.
- Bifate 7/7: total 229/312, raman 83 Filters. Goal activ; full repo la final.

### 2026-10-01 — geometry, relief edges and inverse tile remap

- Target 6: CatalogGeometryMath, Extrude/OilPaint wrappers, Emboss, FindEdges
  si Tiles. Parent citit integral toate targetele si inspectat direct dispatch,
  planner controls/radii, callees, CPU/native assertions, HLSL routes si cele
  5 canonical operation pages. Luna/max bounded read-only incheiat; parent
  detine decisions/verificare/review, fara delegated verification.
- Toate 6 pastrate fara production edits; nu fabric cleanup pentru bife.
  Geometry importa ColorMath.ParameterMagnitude si TextureMath.Kuwahara cu
  calls reale. Extrude block si pyramid au hit/priority/shading contracts
  diferite; nu le ascund intr-un helper configurat prin boolean switches.
  Extrude sin hash nu este hash-ul uint comun. Tiles pastreaza samplerul local:
  integer-coordinate fast path intoarce sursa direct, common bilinear face
  lerp-uri si pe acest path. Fara identitate/performance assumption care sa
  justifice eliminarea. Emboss directional relief si FindEdges Scharr/threshold
  au formule distincte, nu sunt duplicate. Thin wrappers pastreaza ownerul.
- Verificare noua Release --no-build --no-restore pe build-ul ink-relief curent,
  fara modificari C# intre loturi: core 13/13 (inclusiv catalog determinism,
  associated alpha si group/mask/clip integration), SDL native 3/3 cu
  CERNEALA_SDL_NATIVE_TESTS=1: Emboss analytic center parity, Tiles CPU parity,
  Extrude projected side/front cap assertions. Zero skip; raw XML confirma
  toate core13 prezente Passed si in baseline-ul ink-relief core953.
- Reutilizate explicit, nu rerulate: ink-relief core953/SourceGen614, Window132
  si PrismAudit --check exit0 (178 entries/31 properties/216+8 types/zero gaps),
  aceeasi productie. Parent verificat raw counters si toate cele 5 Window entries
  Passed. Reports: Emboss MAE=0.0007/P99=0/max=1, Extrude 0/0/0, FindEdges 0/0/0,
  OilPaint 0.0075/0/2, Tiles 0/0/0, sub pragurile 1/10/49. Historical resource-free
  defaults, nu exhaustive parameter/resource coverage sau full CPU/GPU parity.
  Fara dedicated native parity gasit pentru OilPaint/FindEdges in scope.
- Raw results noi: artifacts/prism-cleanup/geometry/; gates reutilizate:
  artifacts/prism-cleanup/ink-relief/. Parent review actual unchanged source,
  empty target diff, raw assertions/results si git diff --check trecut. Explorer
  si joburile proprii terminale. Tree Unchanged, fara C# temporare/API/docs/
  manifest/catalog/shader/golden changes sau perf/manual/cross-platform claims.
- Bifate 6/6: total 235/312, raman 77 Filters. Goal activ; full repo la final.

### 2026-10-01 — flow sketch, guided plaster and contour/glow paths

- Target 7: Charcoal, GraphicPen, Plaster, XDogLuminance, NeonGlow,
  GlowingEdges si TraceContour. Parent citit integral toate targetele, direct
  dispatch/consumers/callees, affected CPU/native assertions, planner/kernel/
  HLSL routes si cele 6 canonical operation pages. Luna/max bounded read-only
  incheiat; parent detine decisions/edits/verificare/review.
- Eliminat trei private SmoothStep identice si GraphicPen private Fraction;
  7 SmoothStep + 2 Fraction calls folosesc explicit PrismCatalogFilterMath,
  utilitatile comune existente cu aceeasi formula/evaluation order/clamp.
  Charcoal 2+/8-, GraphicPen 9+/15-, Plaster 1+/7-; total 12+/30-.
  Fara using-static/helper nou, module/project dependency/API changes sau
  presupunere de JIT/inlining/performance. Cele 9 calls locale inspectate;
  bounded searches fara named reflection/generated uses gasite. Nu exclud
  arbitrary external reflection asupra helperelor private eliminate.
- Celelalte 4 targets pastrate. Charcoal.Analyze este ownerul FlowXDoG folosit
  si de GraphicPen/ConteCrayon, nu cod mort. Charcoal grain hash difera de
  common seeded hash. Alpha thresholds/divisors si luminance clamp policies
  difera intre Charcoal/Plaster/XDog/GlowingEdges/TraceContour; nu le unific.
  Plaster mediaza coeficientii guided neponderat, ReliefMath ponderat alpha.
  Gaussian division/guards si samplerele de margine au contracte diferite;
  GlowingEdges floor/clamp nu este common clamp-before-fraction sampling.
  NeonGlow ramane dispatcher spre ownerul resampling/mip; TraceContour
  pastreaza eight-neighborhood si equality-as-upper. GuidedLuminanceForTesting
  are caller de productie si test, deci nu este doar test scaffolding.
- Baseline core ink-relief 953/953 reutilizat, aceeasi productie pre-edit,
  inclusiv 33 named target/consumer/Neon cases Passed; fresh native 4/4,
  zero skip. Fara teste noi/RED fictiv pentru deduplicare echivalenta. Existing
  GraphicPen finite-hatch/direction/control cases exercita Fraction/SmoothStep;
  Charcoal/Plaster CPU control/duotone/alpha cases exercita celelalte calls.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  native 36/36 (baseline4 + BaselineConformance32), native=1, zero skip.
  Raw XML confirma toate core953/SourceGen614 si native4+32 baseline names
  prezente Passed. GraphicPen native compara mean-ink CPU/GPU (tolerance0.12),
  nu fiecare pixel; Plaster verifica relief/associated alpha, nu CPU parity;
  TraceContour verifica lower/upper CPU parity (0.002). Nu exista dedicated
  native parity gasit pentru Charcoal/GlowingEdges/NeonGlow in bounded scope.
- Window conformance 132/132, zero skip, toate baseline names Passed si toate
  9 target/consumer operation entries prezente. Window.SaveScreenshot exclusiv,
  132 reports/396 PNG/132 unique symbols; maxime MAE=0.3704, P99=10, max=41,
  praguri 1/10/49. Historical resource-free/default corpus, nu exhaustive
  parameters/resources sau full CPU/GPU oracle. Fara manual/input/perf/
  cross-platform validation claims ori golden updates.
- PrismAudit rebuild --check exit0: 178 entries/31 properties/216+8 public
  types/zero gaps. Raw evidence: artifacts/prism-cleanup/edge-sketch/.
  Parent review actual source/diff, explicit helper bindings/ownership,
  raw counters/baseline case presence/pixel reports si git diff --check trecut.
  Explorer si toate joburile proprii terminale. Tree Unchanged; fara temporare,
  public API/docs/manifest/catalog/shader changes. Full repo suite la final.
- Bifate 7/7: total 242/312, raman 70 Filters. Goal activ.

### 2026-10-01 — paper, cellular surfaces and tile relief

- Scope: Chrome, NotePaper, Photocopy, Stamp, TornEdges, Craquelure,
  Reticulation, StainedGlass, Grain, MosaicTiles, Patchwork. Parent full-read
  toate cele 11 implementari (1935 lines), dispatch/planner/helper context,
  CPU/native test sources si cele 11 pagini canonice. Luna/max read-only
  explorer a documentat helper contracts, callers si coverage limits.
- Deduplicare in 6 fisiere, 22 insertions/94 deletions: opt calls SmoothStep
  din cinci filtre merg explicit la existing PrismCatalogFilterMath helper;
  Patchwork foloseste existing float Hash; StainedGlass foloseste existing
  Option/OptionVector/Unpremultiply. Noua helpers private duplicate eliminate,
  fara helper/import static/dependenta de proiect/API nou. Operand order,
  clamp/cubic, uint mixing/24-bit normalization, fallback si alpha division
  sunt aceleasi expresii inspectate; common helpers nu au fost modificate.
- Chrome/Photocopy/Stamp/TornEdges/MosaicTiles pastrate fara editari justificate.
  Chrome red-channel luminance nu este NotePaper RGB luminance; XDog reuse
  exista deja, Stamp este delegare utila cu packing propriu. Raw uint hashes,
  quintic/cubic noise, seed decoding si StainedGlass zero-fallback raman
  locale; common Seed are alt fallback. NotePaper SmoothCurve are inca
  doi value-noise callers. Tile-center sampling este deja comun, shading
  si grout au politici distincte. Fara normalizare fortata sau algoritmi noi.
- Baseline core edge-sketch 953/953 reutilizat pentru aceeasi productie
  pre-edit, inclusiv 37 named cases din cele 11 clase CPU; fresh native
  baseline 22/22, native=1, zero skip. Existing control/seed/alpha/rendering
  cases acopera calls schimbate; fara teste noi sau RED fictiv de refactor.
- Primul core rebuild s-a oprit inainte de teste: Scene2D LDtk package
  staging AccessDenied/MSB3073. Exact failed temp path nu mai exista;
  parent ACL permite scriere. Source writer creeaza staging, il muta si
  curata fisierele proprii la exceptii; mesajul nu identifica operatia exacta.
  Repetarea unica a aceleiasi comenzi a pregatit ambele pachete si a trecut.
  Cauza intermitenta necunoscuta; fara permission bypass, stergere sau patch
  Scene2D. Log initial pastrat in core-rebuild-initial-failure.log.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  native 54/54 (22 target + BaselineConformance32), native=1, zero skip.
  Raw XML confirma toate baseline names core953/SourceGen614/native22+32
  prezente Passed. CPU/GPU comparisons: Craquelure RGB0.035, Grain0.045,
  Reticulation0.025, MosaicTiles/Patchwork0.006; StainedGlass permite 2%
  boundary shifts pana la0.065, border/light=0. NotePaper si XDog native
  verifica rendering/alpha, nu CPU parity; Chrome native doar selector.
- Window conformance 132/132, zero skip, toate baseline names Passed si
  toate cele 11 target entries prezente; Window.SaveScreenshot exclusiv.
  132 reports/396 PNG/132 unique symbols, maxime MAE0.3704/P99=10/max41
  sub pragurile1/10/49. Historical resource-free/default corpus, nu toate
  parameters/resources ori universal CPU/GPU oracle. Fara golden updates,
  performance/manual/input/cross-platform validation claims.
- PrismAudit rebuild --check exit0: 178 entries/31 properties/216+8 public
  types/zero gaps. Raw evidence artifacts/prism-cleanup/surface-paper/.
  Parent review actual source/diff/helper bindings, raw counters/baseline
  case presence/pixel reports si git diff --check trecut. Explorer si toate
  joburile proprii terminale; fara temporare/API/docs/manifest/catalog/shader
  changes. Suita completa a repo-ului ramane gate final.
- Bifate 11/11: total 253/312, raman 59 Filters. Goal activ.

### 2026-10-01 — neighborhood kernels, plans and morphology

- Scope 31 files: NeighborhoodMath, NeighborhoodPlanner, ArbitraryFlatMorphology
  plus Average/Blur/BlurMore/BoxBlur/Despeckle/DustScratches/FieldBlur/GaussianBlur/
  HighPass/IrisBlur/LensBlur/Maximum/Median/Minimum/MotionBlur/PathBlur/RadialBlur/
  ReduceNoise/ShapeBlur/SharpenEdges/Sharpen/SharpenMore/SmartBlur/SmartSharpen/
  SpinBlur/SurfaceBlur/TiltShift/UnsharpMask. Parent full-read all 31 files,
  inspected dispatch/callers, relevant test bodies and six canonical pages.
  Two bounded Luna/max read-only explorers finished with source/ownership/
  generator/catalog/docs/test evidence. Parent owns decisions and verification.
- Three changed files, 20 insertions/127 deletions. Removed private Sharpen
  and RadiusPlan: declaration-only in inspected source, no named reflective
  or generated references found. This does not exclude arbitrary external
  reflection into private implementation. Live SeparableRadiusPlan preserved.
- Eleven adaptive-median/despeckle calls now use existing local Luminance,
  with exactly the same associated-color unpremultiplication/dot expression.
  JPEG deblocking uses existing local guarded SmoothStep (one call); removed
  the duplicate helper, preserving denominator floor, clamp and cubic order.
  Catalog SmoothStep has a different denominator contract and was not used.
- MotionBlur Apply now directly owns its existing Gaussian/bilinear line
  loop. Its sole private SampleLine caller always passed true/true; removed
  unused options, wrapper and unread plan parameter plus sole dispatch argument.
  Sampling coordinates, tap order, weights, accumulation and normalization
  unchanged. Internal owner only; no public/protected API or algorithm changes.
- Other 28 files retained without justified edits. Separate blur kernels,
  alpha/edge policies, RK4 resource advection, spin/radial sampling, guided
  denoise, Richardson-Lucy passes and chord morphology remain distinct.
  Maximum/Minimum retain catalog/morphology ownership, not neighborhood plans.
  No new helpers, dependencies, optimization or performance claims.
- Baseline core surface-paper 953/953 reused for unchanged pre-edit source;
  fresh SDL test-project baseline 40/40, zero skip. Existing CPU tests cover
  changed median/despeckle/JPEG/motion behavior; no new expectations or fake RED.
- Post-edit Release rebuild --no-restore: core 953/953, SourceGen 614/614,
  SDL test-project 40/40 (SpinBlur2, planner ownership3, fake-API Motion3,
  BaselineConformance32), native=1, zero skip. Raw TRX confirms every baseline
  case name present Passed. The 40 cases are not all native pixel tests.
- Window conformance 132/132, zero skip, all baseline names Passed;
  Window.SaveScreenshot exclusively. 132 reports/396 PNG/132 unique symbols,
  maxima MAE=0.3704/P99=10/max=41, thresholds1/10/49. Includes 25/28 target
  operations; FieldBlur/PathBlur/ShapeBlur excluded by RequiresResource policy
  (catalog required resource properties). Their existing CPU cases passed.
  Historical/default resource-free corpus, not universal CPU/GPU parity or
  exhaustive parameters/resources. No golden updates or manual/input/perf/
  cross-platform validation claims. Initial all-28-presence assumption rejected
  after raw report inspection; no harness/coverage policy changes.
- PrismAudit rebuild --check exit0: 178 entries/31 properties/216+8 public
  types/zero gaps. Raw evidence artifacts/prism-cleanup/neighborhood/.
  Parent reviewed actual current source/diff/helper bindings, baseline case
  preservation, counters, pixel reports and git diff --check. Explorers and
  all own jobs terminal; no temporary files, API/docs/manifest/catalog/shader
  edits. FileTree regenerated; unrelated InvestorReel changes preserved.
  Full repository suite remains the final gate under the existing agreement.
- Bifate 31/31: total 284/312, raman 28 Filters. Goal activ.

### 2026-10-01 — resampling plans and distortion kernels

- Scope 19 files: ResamplingMath/Planner plus AdaptiveWideAngle/DiffuseGlow/
  Displace/Glass/LensCorrection/Liquify/OceanRipple/Offset/Pinch/PolarCoordinates/
  Ripple/Shear/Spherize/Transform/Twirl/Wave/ZigZag. Parent full-read all 4230
  pre-edit source lines, direct dispatch/graph/kernel/HLSL routes, relevant CPU/
  native assertions and all 17 canonical operation pages. Two bounded Luna/max
  read-only explorers finished. WaveNoise was read but not changed/bifated:
  it belongs to Clouds/CatalogPlanner, not Wave deformation; final lot retains it.
- One production file changed, 5 insertions/23 deletions: five Glass lattice/
  DiffuseGlow Grain calls use existing PrismCatalogFilterMath.Hash. Removed its
  private exact duplicate from ResamplingMath. Same uint multiplication/XOR/
  shifts/24-bit mask/divisor16777215f, same operands and evaluation order.
  Common owner has no static initialization fields/constructor. No new helper,
  module/project dependency, import-static, public API or algorithm changes.
  Bounded source searches found no named reflection/generator uses of the
  private helper; arbitrary external reflection is not excluded.
- WaveHash, OceanHash and WaveNoise.Hash01 remain distinct (mixing/seed/divisor
  policies differ). Guarded SmoothStep uses denominator floor0.0001, not common
  unguarded or neighborhood floor0.000001. Bilinear continuous UV wrap/mirror,
  bicubic per-tap wrapping, polar angular wrap/radial transparency, mip averaging,
  Feline/EWA covariance precision/tap budgets and resource coordinates differ;
  no forced unification, intuitive optimization or cross-owner algorithm split.
  Remaining 18 targets retained; thin wrappers keep operation ownership and
  planner option/bounds/resource contracts. NeonGlow consumer already checked.
- Existing baseline core neighborhood953/953 included all 50 Distortion cases.
  Fresh SDL baseline34/34 (Transform/Spherize2 + BaselineConformance32), zero skip.
  Existing DiffuseGlow default and explicit test Grain=0 did not exercise the
  changed nonzero-grain path. Added 48-line PrismDiffuseGlowCharacterizationTests
  with three grain values, 3x2 pixels, alpha0..1, RGB saturation and pixel/seed
  expectations against existing common Hash through complete Math.Apply.
- Initial characterization compile failed CS0246/CS0103 because my new test
  lacked ColorManagement import; corrected test import, log preserved as
  characterization-initial-compile-error.log. Not a behavioral RED. Before
  production edits, characterization plus Distortion suite passed53/53, zero skip.
  No expectations weakened and no production patch before that baseline GREEN.
- Post-edit Release rebuild --no-restore: core956/956 (953+three new cases),
  SourceGen614/614, SDL34/34, native=1, zero skip. Raw XML confirms every core953,
  pre-edit53, SourceGen614 and SDL34 baseline name present Passed. Actual GPU
  Transform translation/transparent edges tolerance0.003; Spherize six amount/
  mode cases compare CPU per-pixel at0.004. Not dedicated parity for every filter.
- Window conformance132/132, all baseline names Passed, zero skip; screenshot
  capture exclusively Window.SaveScreenshot. 132 reports/396 PNG/132 unique
  symbols, maxima MAE0.3704/P99=10/max41 under1/10/49. Includes15/17 target entries;
  Displace/Liquify excluded because catalog requires resources. CPU resource/
  tiling/safe-folded sampling cases passed. Default resource-free historical
  corpus, not exhaustive parameters/resources or universal CPU/GPU parity;
  DiffuseGlow default Grain=0 does not prove GPU parity for nonzero grain.
  No golden updates, performance/manual/input/cross-platform validation claims.
- PrismAudit rebuild --check exit0: 178 entries/31 properties/216+8 public types/
  zero gaps. Raw evidence artifacts/prism-cleanup/resampling/. Parent reviewed
  actual current source/diff/new test, helper bindings, raw baseline names/
  counters/pixel reports and git diff --check. All explorers/own jobs terminal.
  No temporary files/API/docs/manifest/catalog/shader edits; dirty unrelated
  work preserved. FileTree regenerated. Full repo suite remains final gate.
- Bifate19/19: total303/312, raman9 Filters. Goal activ.

### 2026-10-01 — final catalog, diffusion, wind and lens lot

- Scope9: CatalogFilterPlanner, Diffuse, FilterConformanceGallery, LensFlareFilter,
  LensFlareRenderer, LensProfileFitter, LightingEffects, WaveNoise and Wind.
  Parent full-read all4979 pre-edit lines, direct callers/common sampler/graph
  bounds consumers, CPU/native assertions and canonical public filter/fitter
  pages. Two bounded Luna/max explorers finished; no delegated implementation.
- Three production files changed12+/43-: ten Diffuse sampling calls now use the
  existing CatalogFilterMath.SamplePixelBilinear; removed its exact25-line local
  duplicate plus separator. Clamp-before-fraction, floor/neighbor selection and
  all three lerps/operand order remain identical. Wind's existing method-scale
  helper became internal; planner consumes it instead of a duplicate switch.
  Wind/Blast/Stagger scales4/5.5/4.5 and radius cap64 remain unchanged. No new
  helper/module dependency, public API, shader, catalog or algorithm change.
- Six other files retained. Fitter Gram loops have different ridge/roles; no
  speculative solver rewrite or extraction. Lens radiance accumulation is not
  associated-image clamping. Wave spectral tables belong to Clouds, not Wave
  deformation. Local luminance cutoffs, seed mixing and hash divisors differ;
  no forced common implementation. Gallery remains lazy/catalog-driven.
- Added six Wind planner characterization cases before production edits: all
  methods, deviceScale3, unclamped and capped integration radii, unchanged zero
  bounds expansion. My first test incorrectly equated integration radius and
  bounds outset: six failures were wrong expectations, not behavioral RED.
  Source sets both bounds radii0; GraphOptimizer preserves bounds for0/0.
  Corrected only the test, preserved invalid-expectation log/TRX, then12/12
  characterization+Diffuse/Wind cases passed before production edits.
- Existing core956 baseline includes fitting/lens, lighting, gallery and spectral
  cases. Fresh native baseline37/37: Wind3, WaveNoise1, LightingEffects1 and
  BaselineConformanceMigration32. Initial native filter misspelled the baseline
  class and selected5 cases; corrected command reran37 before production edits.
- Post-edit Release rebuilds --no-restore: core962/962, SourceGen614/614,
  SDL37/37 with native=1, zero skip. Raw XML baseline names all preserved Passed:
  core956, characterization12, SourceGen614 and native37. WaveNoise compares
  actual GPU pixels to CPU at0.006; Wind/Lighting native assertions are response/
  alpha/associated checks, not universal CPU/GPU equality. CPU Diffuse tests
  exercise all four modes, seeded repeat/change, constant input and coverage.
- Window132/132, zero skip; all previous132 names Passed. Application-owned
  Window.SaveScreenshot captures:132 reports/396PNG/132symbols; maxima
  MAE0.3704/P99=10/max41 under1/10/49. Includes Diffuse/Wind/Clouds/DifferenceClouds;
  required-resource LensFlare/LightingEffects are outside this default corpus.
  CPU lens tests/native lighting passed; no exhaustive resource/parameter claim.
- PrismAudit rebuilt --check exit0:178 entries/31 properties/216+8 public types/
  zero gaps. Evidence artifacts/prism-cleanup/final-filters/. Parent reviewed
  actual source/diff/new tests, baseline names/counters and pixel artifacts;
  git diff --check passed, all explorers/own jobs terminal. FileTree regenerated
  by parent; catalog explorer also regenerated it despite read-only assignment.
  No temporary experiments, public API/docs/manifest/shader/golden edits.
  Unrelated Cerneala.slnx InvestorReel inclusion and untracked project preserved.
- Bifate9/9: total312/312, raman0. File inspection/lot gates complete; full
  repository Release build, complete tests and final runtime gates PENDING.
  Goal remains active until those final gates are accounted for. No manual,
  cross-platform or new performance measurement claims.

### 2026-10-01 — full repository acceptance checkpoint

- All312 inventory items resolved and checked; no unchecked items remain.
  No further production edits after the final-filter lot. Final gates ran only
  after the inventory reached312/312, under the settled focused-lot/full-final
  agreement. No failing behavior required a subsequent production repair.
- Windows, pinned SDK10.0.400, Release. dotnet tool restore and
  dotnet restore Cerneala.slnx exit0. Tools/scripts/Cerneala.BuildInputs.Tests.ps1
  passed: legitimate Compile/EmbeddedResource/None/AdditionalFiles retained,
  zero archived inputs. Its uniquely named temporary fixture was cleaned by
  the script's path-checked finally block.
- dotnet build Cerneala.slnx -c Release --no-restore exit0: zero errors,
  one CS0108 warning in unrelated InvestorReel generated InkScene.Drop,
  hiding inherited UIElement.Drop. No suppression, generated-source edit,
  project exclusion or unrelated repair. InvestorReel inclusion/work preserved.
- PrismAudit --check after full build exit0:178 entries/31 common properties/
  216+8 public types/zero gaps. Offline SdlShaderCompiler --verify exit0:
  all10 current SDL shader artifacts verified. Neither gate was bypassed.
- Full solution command, exit0:
  `dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1 --logger trx --results-directory artifacts/prism-cleanup/final-repository/test-results`.
  CERNEALA_SDL_NATIVE_TESTS=1; CERNEALA_SDL_CONFORMANCE_ARTIFACTS points to the
  absolute artifacts/prism-cleanup/final-repository/full-suite-captures directory.
  Serial project execution preserves the shared interactive-desktop boundary.
  Automatic TRX names avoid overwriting reports between projects.

  | Project | Passed | Failed | Skipped |
  | --- | ---: | ---: | ---: |
  | Cerneala.Tests.Language | 259 | 0 | 1 |
  | Cerneala.Tests.LanguageServer | 40 | 0 | 0 |
  | Cerneala.Tests.PreviewHost | 17 | 0 | 0 |
  | Cerneala.Tests.Scene2DImporters | 173 | 0 | 0 |
  | Cerneala.Tests.Scene2DPackages | 104 | 0 | 0 |
  | Cerneala.Tests.SceneVillage | 43 | 0 | 1 |
  | Cerneala.Tests.SdlGpu | 961 | 0 | 5 |
  | Cerneala.Tests.SourceGen | 614 | 0 | 0 |
  | Cerneala.Tests.VisualStudio | 47 | 0 | 0 |
  | Cerneala.Tests | 4257 | 0 | 0 |
  | Cerneala.Tetris.Tests | 31 | 0 | 0 |
  | Total, 11 projects / 6553 discovered cases | 6546 | 0 | 7 |

- Counts validated from actual UnitTestResult outcomes and project codeBase,
  not exit code alone. Adapter Counters.notExecuted reports0 even for skipped
  cases; total minus executed and per-case NotExecuted correctly expose7.
  All focused baseline962 core/614 generator/37 native names remain Passed in
  the full run. Mandatory native pipeline1/1, NativeRenderSurface3D27/27 and
  Drawing/Prism pixel cases133/133 Passed, with zero skip in these subsets.
- Seven unchanged, explicit exclusions are NOT passing tests: four alpha-
  occlusion cases(content1/3/6/7) disabled by prior user request (reported NVIDIA
  issue; driver root cause not independently established); native ownership/
  input/lifetime test disabled by maintainer request2026-09-21 due foreground-
  focus failure; completion CPU P95 disabled by that dated maintainer request,
  underlying performance failure unresolved; Village cadence gate is opt-in
  CERNEALA_VILLAGE_PERF_GATE=1 on a reference Windows machine, not enabled here.
  No skips/assertions/goldens modified to obtain this result.
- Final full-suite pixel artifacts:133 reports and133 each actual/reference/
  heatmap PNG(399 total), all under canonical1/10/49 thresholds; maxima
  MAE0.3704/P99=10/max41. Initial postprocessing assumed every report had the
  Prism symbol-prefixed second-line format; Drawing puts unprefixed metrics on
  line1. Inspected actual reports/writer and parsed both existing forms; no
  conformance-test failure, artifact rewrite or threshold change.
- Runtime smoke commands used Release --no-build --no-restore, all exit0:
  Cerneala.SdlGpuSmoke --mode multi-window (mainFrames2/secondaryFrames1,
  two PNG); --mode prism(mainFrames2, one PNG); --mode rendersurface3d
  (captures8/orbit12/pan12/zoom1/selected0/pose1/draws54, eight PNG).
  All11 screenshots nonempty and captured only via Window.SaveScreenshot.
  Parent inspected Prism/3D captures; this is not authorized human validation.
  3D input used Servo Click/Drag/Scroll with routing/controller assertions;
  preset-direction captures alone are not counted as user-input evidence.
  Benchmarks --rendersurface3d-probe-args-test exit0:valid2/invalid7, not a
  CPU/GPU performance benchmark or timing acceptance claim.
- Logs/TRX/captures retained in artifacts/prism-cleanup/final-repository/.
  Parent reviewed actual current complete change-set/source interactions,
  ledger conformity, every project result, baseline preservation, skip reasons,
  native subset counts, pixel reports and smoke captures/logs. git diff --check
  passed. All explorers and own sessions/cells terminal; no temporary experiments
  or unaccounted own jobs. FileTree regenerated after the final source state;
  newly observed unrelated Markup_Authoring_Findings_01102026.md also preserved.
- Public API/docs/manifest/catalog/shader/goldens unchanged by this cleanup;
  unrelated work preserved. No commit/push/publication. Automated cleanup and
  standard Windows regression acceptance complete. Linux/macOS/other-RID native
  execution, human validation, disabled/opt-in tests and new performance metrics
  remain unverified; no cross-platform or exhaustive parameter/resource claim.
  Final312/312; no remaining cleanup stage. Goal may now be closed.
