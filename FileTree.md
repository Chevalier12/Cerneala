# File Tree

Generated from `.`.

```text
./
|-- .agents/
|-- .github/
|   +-- workflows/
|       +-- pages.yml
|-- .superpowers/
|   +-- brainstorm/
|       +-- manual-1783588990/
|           |-- content/
|           |   +-- visual-direction.html
|           +-- state/
|               +-- server-stopped
|-- Cerneala.SourceGen/
|   |-- Generated/
|   |-- Cerneala.SourceGen.csproj
|   |-- UiMarkupDirectiveParser.cs
|   |-- UiMarkupGenerator.cs
|   |-- UiMarkupReactiveEmitter.cs
|   |-- UiMarkupUserControlGenerator.cs
|   +-- UiMarkupWindowGenerator.cs
|-- docs/
|   |-- bug-reports/
|   |-- diagrams/
|   |   |-- cerneala-drawing-flowchart.svg
|   |   |-- retained-frame-loop.md
|   |   +-- ui-layer-boundaries.md
|   |-- plans/
|   |   |-- 2026-07-10-inline-component-template-markup.md
|   |   +-- 2026-07-10-window-windowsdx-migration.md
|   |-- superpowers/
|   |   |-- plans/
|   |   |   |-- 2026-07-03-fix-retained-render-frame-contract.md
|   |   |   |-- 2026-07-03-fix-tree-mutation-invalidation.md
|   |   |   |-- 2026-07-03-integrate-style-phase.md
|   |   |   |-- 2026-07-04-cache-input-route-hit-test.md
|   |   |   |-- 2026-07-05-authoring-preview-completion-gate.md
|   |   |   |-- 2026-07-05-clarify-layout-scheduler-contract-and-diagnostics.md
|   |   |   |-- 2026-07-05-clarify-package-boundary-dependencies.md
|   |   |   |-- 2026-07-05-clarify-text-services-mvp.md
|   |   |   |-- 2026-07-05-complete-textblock-layout-contract.md
|   |   |   |-- 2026-07-05-consolidate-button-content-composition.md
|   |   |   |-- 2026-07-05-core-preview-completion-gate.md
|   |   |   |-- 2026-07-05-create-retained-ui-mvp-vertical-slice.md
|   |   |   |-- 2026-07-05-default-theme-and-style-vertical-slice.md
|   |   |   |-- 2026-07-05-fix-focus-visibility-semantics.md
|   |   |   |-- 2026-07-05-fix-viewport-and-pre-input-frame-contract.md
|   |   |   |-- 2026-07-05-freeze-later-experimental-scope.md
|   |   |   |-- 2026-07-05-implement-inherited-property-tree-propagation.md
|   |   |   |-- 2026-07-05-items-scroll-data-vertical-slice.md
|   |   |   |-- 2026-07-05-next-authoring-preview-plan-index.md
|   |   |   |-- 2026-07-05-next-core-completion-plan-index.md
|   |   |   |-- 2026-07-05-next-core-preview-plan-index.md
|   |   |   |-- 2026-07-05-observable-items-source-and-recycling-stability.md
|   |   |   |-- 2026-07-05-retained-command-state-refresh.md
|   |   |   |-- 2026-07-05-retained-semantics-tree-core-contract.md
|   |   |   |-- 2026-07-05-root-owned-resource-invalidation.md
|   |   |   |-- 2026-07-05-template-content-presenter-state-contract.md
|   |   |   |-- 2026-07-05-textbox-editing-viewport-and-caret-contract.md
|   |   |   |-- 2026-07-05-typed-binding-lifetime-and-two-way-text.md
|   |   |   |-- 2026-07-05-wire-keyboard-control-activation.md
|   |   |   |-- 2026-07-05-wire-minimal-retained-input-bindings.md
|   |   |   |-- 2026-07-06-add-preview-api-scope-guardrails.md
|   |   |   |-- 2026-07-06-add-retained-stress-budget-tests.md
|   |   |   |-- 2026-07-06-cache-content-resources-and-textures-lifetime.md
|   |   |   |-- 2026-07-06-close-retained-lifecycle-subscription-leaks.md
|   |   |   |-- 2026-07-06-create-developer-preview-docs-and-sample-gate.md
|   |   |   |-- 2026-07-06-developer-preview-completion-gate.md
|   |   |   |-- 2026-07-06-harden-layout-authoring-mutation-contracts.md
|   |   |   |-- 2026-07-06-harden-monogame-render-backend-state.md
|   |   |   |-- 2026-07-06-next-developer-preview-hardening-plan-index.md
|   |   |   |-- 2026-07-06-next-runtime-preview-plan-index.md
|   |   |   |-- 2026-07-06-normalize-viewport-scale-pointer-and-render-coordinates.md
|   |   |   |-- 2026-07-06-runtime-diagnostics-and-playground-polish.md
|   |   |   |-- 2026-07-06-runtime-preview-completion-gate.md
|   |   |   |-- 2026-07-06-textbox-caret-blink-hit-testing.md
|   |   |   |-- 2026-07-06-wire-platform-services-cursor-and-clipboard.md
|   |   |   |-- 2026-07-06-wire-tab-focus-navigation-contract.md
|   |   |   |-- 2026-07-07-modern-motion-system.md
|   |   |   |-- 2026-07-08-modern-aspect-template-system.md
|   |   |   |-- 2026-07-09-markup-aspect-resources.md
|   |   |   +-- developer-preview-smoke-failure-fix-plan.md
|   |   +-- specs/
|   |       +-- 2026-07-09-markup-aspect-resources-design.md
|   |-- architecture-v2.md
|   |-- aspect-system.md
|   |-- developer-preview-checklist.md
|   |-- developer-preview-scope.md
|   |-- getting-started.md
|   |-- motion-api.md
|   |-- motion-diagnostics.md
|   |-- motion-system.md
|   +-- wpf-event-coverage.md
|-- docs-site/
|   |-- documentation/
|   |   |-- classes/
|   |   |   |-- Cerneala.Drawing.DrawArgument.md
|   |   |   |-- Cerneala.Drawing.DrawColor.md
|   |   |   |-- Cerneala.Drawing.DrawCommand.md
|   |   |   |-- Cerneala.Drawing.DrawCommandKind.md
|   |   |   |-- Cerneala.Drawing.DrawCommandList.md
|   |   |   |-- Cerneala.Drawing.DrawingContext.md
|   |   |   |-- Cerneala.Drawing.DrawPoint.md
|   |   |   |-- Cerneala.Drawing.DrawRect.md
|   |   |   |-- Cerneala.Drawing.DrawSize.md
|   |   |   |-- Cerneala.Drawing.DrawTextRun.md
|   |   |   |-- Cerneala.Drawing.IDrawFont.md
|   |   |   |-- Cerneala.Drawing.IDrawImage.md
|   |   |   |-- Cerneala.Drawing.IDrawingBackend.md
|   |   |   |-- Cerneala.Drawing.IFontSource.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameClipStack.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameDrawingBackend.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameDrawingBackend.TextTexture.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameDrawingBackend.TextTextureKey.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameDrawMapper.md
|   |   |   |-- Cerneala.Drawing.MonoGame.MonoGameImage.md
|   |   |   |-- Cerneala.Drawing.Text.RasterizedText.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaFont.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaTextRasterizer.md
|   |   |   |-- Cerneala.Drawing.Text.SkiaTextShaper.md
|   |   |   |-- Cerneala.Drawing.Text.SystemFontSource.md
|   |   |   |-- Cerneala.Drawing.Text.TextCaretVerticalMetrics.md
|   |   |   |-- Cerneala.Drawing.Text.TextShaper.md
|   |   |   |-- Cerneala.Drawing.Text.TextShapeResult.md
|   |   |   |-- Cerneala.GameBootstrap.md
|   |   |   |-- Cerneala.SourceGen.UiMarkupGenerator.GenerationScope.md
|   |   |   |-- Cerneala.SourceGen.UiMarkupGenerator.MarkupSource.md
|   |   |   |-- Cerneala.SourceGen.UiMarkupGenerator.md
|   |   |   |-- Cerneala.UI.Accessibility.AccessibleName.md
|   |   |   |-- Cerneala.UI.Accessibility.AutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.ButtonAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.ItemsControlAutomationPeer.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsNode.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsProperty.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsProvider.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsRole.md
|   |   |   |-- Cerneala.UI.Accessibility.SemanticsTree.md
|   |   |   |-- Cerneala.UI.Accessibility.TextBoxAutomationPeer.md
|   |   |   |-- Cerneala.UI.Aspect.AllAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.AnyAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.AspectApplicationResult.md
|   |   |   |-- Cerneala.UI.Aspect.AspectCascadeKey.md
|   |   |   |-- Cerneala.UI.Aspect.AspectCatalog.md
|   |   |   |-- Cerneala.UI.Aspect.AspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionDependency.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionDependencyKind.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionNode.md
|   |   |   |-- Cerneala.UI.Aspect.AspectConditionResult.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDataContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDataDependency.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDeclaration.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDependencySet.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDiagnostics.md
|   |   |   |-- Cerneala.UI.Aspect.AspectDiagnostics.Snapshot.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEngine.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEngineCounters.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEngineElementState.md
|   |   |   |-- Cerneala.UI.Aspect.AspectEnvironment.md
|   |   |   |-- Cerneala.UI.Aspect.AspectInvalidation.md
|   |   |   |-- Cerneala.UI.Aspect.AspectInvalidationGraph.DependencyHolder.md
|   |   |   |-- Cerneala.UI.Aspect.AspectInvalidationGraph.md
|   |   |   |-- Cerneala.UI.Aspect.AspectLayer.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMatchContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMotion.md
|   |   |   |-- Cerneala.UI.Aspect.AspectMotionSource.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackage.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackageBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPackageDiagnostic.md
|   |   |   |-- Cerneala.UI.Aspect.AspectProcessor.md
|   |   |   |-- Cerneala.UI.Aspect.AspectPropertyConditionBuilder_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRef.md
|   |   |   |-- Cerneala.UI.Aspect.AspectRegistry.md
|   |   |   |-- Cerneala.UI.Aspect.AspectResolutionContext.md
|   |   |   |-- Cerneala.UI.Aspect.AspectResolutionStep.md
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
|   |   |   |-- Cerneala.UI.Aspect.AspectTokenTrace.md
|   |   |   |-- Cerneala.UI.Aspect.AspectValue_T_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectValue.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantKey_TOwner_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantKey.md
|   |   |   |-- Cerneala.UI.Aspect.AspectVariantSet.md
|   |   |   |-- Cerneala.UI.Aspect.ComponentAspectBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.ContentTemplateBuilder.md
|   |   |   |-- Cerneala.UI.Aspect.DataAspectCondition_TData_.md
|   |   |   |-- Cerneala.UI.Aspect.DataAspectCondition_TData_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectPackage.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Color.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Motion.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Spacing.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Stroke.md
|   |   |   |-- Cerneala.UI.Aspect.DefaultAspectTokens.Typography.md
|   |   |   |-- Cerneala.UI.Aspect.NotAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.PredicateAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.PropertyAspectCondition_TValue_.md
|   |   |   |-- Cerneala.UI.Aspect.RejectedAspectDeclaration.md
|   |   |   |-- Cerneala.UI.Aspect.ResolvedAspect.md
|   |   |   |-- Cerneala.UI.Aspect.ResolvedAspectValue.md
|   |   |   |-- Cerneala.UI.Aspect.StateAspectCondition.md
|   |   |   |-- Cerneala.UI.Aspect.ThemeTokenBridge.md
|   |   |   |-- Cerneala.UI.Aspect.VariantAspectCondition.md
|   |   |   |-- Cerneala.UI.Controls.Border.md
|   |   |   |-- Cerneala.UI.Controls.Button.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonKind.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonSize.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonSlots.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonTemplates.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonTokens.md
|   |   |   |-- Cerneala.UI.Controls.Buttons.ButtonVariants.md
|   |   |   |-- Cerneala.UI.Controls.Canvas.md
|   |   |   |-- Cerneala.UI.Controls.CheckBox.md
|   |   |   |-- Cerneala.UI.Controls.ComboBox.md
|   |   |   |-- Cerneala.UI.Controls.ContentControl.ContentValueEqualityComparer.md
|   |   |   |-- Cerneala.UI.Controls.ContentControl.md
|   |   |   |-- Cerneala.UI.Controls.ContentPresenter.md
|   |   |   |-- Cerneala.UI.Controls.ContentPresenter.ReferenceContentEqualityComparer.md
|   |   |   |-- Cerneala.UI.Controls.Control.md
|   |   |   |-- Cerneala.UI.Controls.ControlTextFont.md
|   |   |   |-- Cerneala.UI.Controls.Decorator.md
|   |   |   |-- Cerneala.UI.Controls.Image.md
|   |   |   |-- Cerneala.UI.Controls.Image.ReferenceImageComparer.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvas.InkInputAction.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvas.InkInputKey.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvas.InkInputKind.md
|   |   |   |-- Cerneala.UI.Controls.InkCanvas.md
|   |   |   |-- Cerneala.UI.Controls.IScrollInfo.md
|   |   |   |-- Cerneala.UI.Controls.ISelectableItemContainer.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemCollection.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemContainerGenerator.ItemContainerInfo.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemContainerGenerator.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemContainerRecyclePool.md
|   |   |   |-- Cerneala.UI.Controls.Items.ItemsPanelTemplate.md
|   |   |   |-- Cerneala.UI.Controls.ItemsControl.md
|   |   |   |-- Cerneala.UI.Controls.ItemsPresenter.md
|   |   |   |-- Cerneala.UI.Controls.Label.md
|   |   |   |-- Cerneala.UI.Controls.ListBox.md
|   |   |   |-- Cerneala.UI.Controls.ListBoxItem.md
|   |   |   |-- Cerneala.UI.Controls.Panel.md
|   |   |   |-- Cerneala.UI.Controls.PasswordBox.md
|   |   |   |-- Cerneala.UI.Controls.PopupRoot.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ButtonBase.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragCompletedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragDeltaEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.DragStartedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.RangeBase.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ScrollBar.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Selector.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Selector.SelectorClickHandlerRegistration.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Thumb.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.ToggleButton.md
|   |   |   |-- Cerneala.UI.Controls.Primitives.Track.md
|   |   |   |-- Cerneala.UI.Controls.ProgressBar.md
|   |   |   |-- Cerneala.UI.Controls.RadioButton.md
|   |   |   |-- Cerneala.UI.Controls.ScrollBarVisibility.md
|   |   |   |-- Cerneala.UI.Controls.ScrollContentPresenter.md
|   |   |   |-- Cerneala.UI.Controls.ScrollViewer.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionChangeResult.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionModel_T_.md
|   |   |   |-- Cerneala.UI.Controls.Selection.SelectionModel.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Ellipse.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Path.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Rectangle.md
|   |   |   |-- Cerneala.UI.Controls.Shapes.Shape.md
|   |   |   |-- Cerneala.UI.Controls.Slider.md
|   |   |   |-- Cerneala.UI.Controls.StackPanel.md
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
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateRegistry.CacheKey.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateRegistry.md
|   |   |   |-- Cerneala.UI.Controls.Templates.ContentTemplateRegistry.RegisteredTemplate.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateBinding_T_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateBinding.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateChildOwner.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplatePartAttribute.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplatePartMap.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateRecycleKey.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateRecyclePool.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateSlotMap.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateTokenBinding_T_.md
|   |   |   |-- Cerneala.UI.Controls.Templates.TemplateTokenBinding.md
|   |   |   |-- Cerneala.UI.Controls.TextBlock.md
|   |   |   |-- Cerneala.UI.Controls.TextBox.md
|   |   |   |-- Cerneala.UI.Controls.TextBoxBase.md
|   |   |   |-- Cerneala.UI.Controls.ToolTip.md
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
|   |   |   |-- Cerneala.UI.Core.Unset.md
|   |   |   |-- Cerneala.UI.Core.Unset.UnsetValue.md
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
|   |   |   |-- Cerneala.UI.Diagnostics.AspectTrace.md
|   |   |   |-- Cerneala.UI.Diagnostics.AspectTraceSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.DebugAdorner.md
|   |   |   |-- Cerneala.UI.Diagnostics.DebugOverlay.md
|   |   |   |-- Cerneala.UI.Diagnostics.DirtyTreeDumper.DirtyTraceInfo.md
|   |   |   |-- Cerneala.UI.Diagnostics.DirtyTreeDumper.md
|   |   |   |-- Cerneala.UI.Diagnostics.ElementRenderDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.ElementTreeDumper.md
|   |   |   |-- Cerneala.UI.Diagnostics.FrameDiagnostics.md
|   |   |   |-- Cerneala.UI.Diagnostics.FrameDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.InputDiagnostics.md
|   |   |   |-- Cerneala.UI.Diagnostics.InputDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.InvalidationTrace.md
|   |   |   |-- Cerneala.UI.Diagnostics.InvalidationTraceEntry.md
|   |   |   |-- Cerneala.UI.Diagnostics.InvalidationTraceEventKind.md
|   |   |   |-- Cerneala.UI.Diagnostics.LayoutDiagnostics.md
|   |   |   |-- Cerneala.UI.Diagnostics.LayoutDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RenderCacheDumper.md
|   |   |   |-- Cerneala.UI.Diagnostics.RenderDiagnostics.md
|   |   |   |-- Cerneala.UI.Diagnostics.RootRenderDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RoutedEventTrace.md
|   |   |   |-- Cerneala.UI.Diagnostics.RoutedEventTraceSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RoutedEventTraceStep.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeDiagnostics.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeInputDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimePlatformDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeRenderDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeResourceDiagnosticsSnapshot.md
|   |   |   |-- Cerneala.UI.Diagnostics.RuntimeViewportDiagnosticsSnapshot.md
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
|   |   |   |-- Cerneala.UI.Elements.UIElement.md
|   |   |   |-- Cerneala.UI.Elements.UIElementCollection.md
|   |   |   |-- Cerneala.UI.Elements.UIElementVisibility.md
|   |   |   |-- Cerneala.UI.Elements.UIRoot.md
|   |   |   |-- Cerneala.UI.Elements.UIRoot.ThemeChangedSubscription.md
|   |   |   |-- Cerneala.UI.Hosting.IUiBackend.md
|   |   |   |-- Cerneala.UI.Hosting.IUiClock.md
|   |   |   |-- Cerneala.UI.Hosting.MonoGame.MonoGameContentServices.md
|   |   |   |-- Cerneala.UI.Hosting.MonoGame.MonoGameUiHost.md
|   |   |   |-- Cerneala.UI.Hosting.MonoGame.MonoGameUiHost.MonoGameUiBackend.md
|   |   |   |-- Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions.md
|   |   |   |-- Cerneala.UI.Hosting.UiCoordinateMapper.md
|   |   |   |-- Cerneala.UI.Hosting.UiFrame.md
|   |   |   |-- Cerneala.UI.Hosting.UiHost.md
|   |   |   |-- Cerneala.UI.Hosting.UiHostOptions.md
|   |   |   |-- Cerneala.UI.Hosting.UiViewport.md
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
|   |   |   |-- Cerneala.UI.Input.CursorService.CursorBox.md
|   |   |   |-- Cerneala.UI.Input.CursorService.md
|   |   |   |-- Cerneala.UI.Input.DataTransfer.md
|   |   |   |-- Cerneala.UI.Input.DragDropController.DragSession.md
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
|   |   |   |-- Cerneala.UI.Input.KeyboardActivationController.md
|   |   |   |-- Cerneala.UI.Input.KeyboardDispatchKind.md
|   |   |   |-- Cerneala.UI.Input.KeyboardDispatchResult.md
|   |   |   |-- Cerneala.UI.Input.KeyboardFocusChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.KeyboardNavigation.md
|   |   |   |-- Cerneala.UI.Input.KeyboardNavigation.NavigationCandidate.md
|   |   |   |-- Cerneala.UI.Input.KeyboardNavigationController.md
|   |   |   |-- Cerneala.UI.Input.KeyboardSnapshot.md
|   |   |   |-- Cerneala.UI.Input.KeyEventArgs.md
|   |   |   |-- Cerneala.UI.Input.KeyGesture.md
|   |   |   |-- Cerneala.UI.Input.KeyModifiers.md
|   |   |   |-- Cerneala.UI.Input.ManipulationDelta.md
|   |   |   |-- Cerneala.UI.Input.ManipulationPoint.md
|   |   |   |-- Cerneala.UI.Input.ManipulationProcessor.md
|   |   |   |-- Cerneala.UI.Input.ManipulationSnapshot.md
|   |   |   |-- Cerneala.UI.Input.MonoGame.MonoGameInputMapper.md
|   |   |   |-- Cerneala.UI.Input.MonoGame.MonoGameInputSource.md
|   |   |   |-- Cerneala.UI.Input.MouseButtonEventArgs.md
|   |   |   |-- Cerneala.UI.Input.MouseEventArgs.md
|   |   |   |-- Cerneala.UI.Input.MouseWheelEventArgs.md
|   |   |   |-- Cerneala.UI.Input.PointerCaptureManager.md
|   |   |   |-- Cerneala.UI.Input.PointerSnapshot.md
|   |   |   |-- Cerneala.UI.Input.PressedStateTracker.md
|   |   |   |-- Cerneala.UI.Input.RetainedInputBindingProcessor.md
|   |   |   |-- Cerneala.UI.Input.RoutedCommand.md
|   |   |   |-- Cerneala.UI.Input.RoutedCommandContext.md
|   |   |   |-- Cerneala.UI.Input.RoutedEvent.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventArgs.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventHandler.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventRegistry.md
|   |   |   |-- Cerneala.UI.Input.RoutedEventRouter.md
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
|   |   |   |-- Cerneala.UI.Invalidation.ElementQueueOrder.ElementOrder.md
|   |   |   |-- Cerneala.UI.Invalidation.ElementQueueOrder.md
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
|   |   |   |-- Cerneala.UI.Layout.Panels.Canvas.CanvasPosition.md
|   |   |   |-- Cerneala.UI.Layout.Panels.Canvas.md
|   |   |   |-- Cerneala.UI.Layout.Panels.ColumnDefinition.md
|   |   |   |-- Cerneala.UI.Layout.Panels.Grid.GridPlacement.md
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
|   |   |   |-- Cerneala.UI.Layout.Virtualization.RealizationWindow.md
|   |   |   |-- Cerneala.UI.Layout.Virtualization.VirtualizationContext.md
|   |   |   |-- Cerneala.UI.Layout.Visibility.md
|   |   |   |-- Cerneala.UI.Markup.ContentPropertyAttribute.md
|   |   |   |-- Cerneala.UI.Markup.DesignTimeOnlyAttribute.md
|   |   |   |-- Cerneala.UI.Markup.GeneratedUiFactory.md
|   |   |   |-- Cerneala.UI.Markup.MarkupDiagnostic.md
|   |   |   |-- Cerneala.UI.Markup.MarkupDiagnosticSeverity.md
|   |   |   |-- Cerneala.UI.Markup.MarkupLoadOptions.md
|   |   |   |-- Cerneala.UI.Markup.MarkupResult_T_.md
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
|   |   |   |-- Cerneala.UI.Media.EllipseGeometry.md
|   |   |   |-- Cerneala.UI.Media.Geometry.md
|   |   |   |-- Cerneala.UI.Media.GradientStop.md
|   |   |   |-- Cerneala.UI.Media.ImageSource.md
|   |   |   |-- Cerneala.UI.Media.LinearGradientBrush.md
|   |   |   |-- Cerneala.UI.Media.Matrix3x2.md
|   |   |   |-- Cerneala.UI.Media.OpacityLayer.md
|   |   |   |-- Cerneala.UI.Media.PathGeometry.md
|   |   |   |-- Cerneala.UI.Media.Pen.md
|   |   |   |-- Cerneala.UI.Media.RadialGradientBrush.md
|   |   |   |-- Cerneala.UI.Media.RectangleGeometry.md
|   |   |   |-- Cerneala.UI.Media.RenderTargetImage.md
|   |   |   |-- Cerneala.UI.Media.ShadowEffect.md
|   |   |   |-- Cerneala.UI.Media.SolidColorBrush.md
|   |   |   |-- Cerneala.UI.Media.Transform.md
|   |   |   |-- Cerneala.UI.Motion.Core.DerivedMotionValue_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.DerivedMotionValue_T_.Subscription.md
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
|   |   |   |-- Cerneala.UI.Motion.Core.MotionThreadGuard.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionTimeline.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionTimelineRegistry.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue_T_.Subscription.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue_T_.ValueNode.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValue.md
|   |   |   |-- Cerneala.UI.Motion.Core.MotionValueChanged_T_.md
|   |   |   |-- Cerneala.UI.Motion.Core.ReducedMotionMode.md
|   |   |   |-- Cerneala.UI.Motion.Core.ReducedMotionPolicy.md
|   |   |   |-- Cerneala.UI.Motion.Core.SystemMotionClock.md
|   |   |   |-- Cerneala.UI.Motion.Diagnostics.MotionDiagnostics.md
|   |   |   |-- Cerneala.UI.Motion.Diagnostics.MotionGraphSnapshot.md
|   |   |   |-- Cerneala.UI.Motion.Diagnostics.MotionTrace.md
|   |   |   |-- Cerneala.UI.Motion.Diagnostics.MotionTraceEvent.md
|   |   |   |-- Cerneala.UI.Motion.Diagnostics.MotionTraceEventKind.md
|   |   |   |-- Cerneala.UI.Motion.Input.DragMotionController.md
|   |   |   |-- Cerneala.UI.Motion.Input.GestureMotionController.md
|   |   |   |-- Cerneala.UI.Motion.Input.MotionRange.md
|   |   |   |-- Cerneala.UI.Motion.Input.PointerMotionState.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollMotionBinding_T_.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollTimeline.md
|   |   |   |-- Cerneala.UI.Motion.Input.ScrollTimelineProgress.md
|   |   |   |-- Cerneala.UI.Motion.Input.VelocityTracker.md
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
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionBinding.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionCoordinator.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionId.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutMotionOptions.md
|   |   |   |-- Cerneala.UI.Motion.Layout.LayoutSnapshot.md
|   |   |   |-- Cerneala.UI.Motion.MotionAnimationBuilder_T_.md
|   |   |   |-- Cerneala.UI.Motion.MotionDefaults.md
|   |   |   |-- Cerneala.UI.Motion.MotionElementFacade.md
|   |   |   |-- Cerneala.UI.Motion.MotionExtensions.md
|   |   |   |-- Cerneala.UI.Motion.MotionPropertyShortcut_T_.md
|   |   |   |-- Cerneala.UI.Motion.MotionStateBuilder.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceCoordinator.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceHandle.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceOptions.md
|   |   |   |-- Cerneala.UI.Motion.Presence.PresenceState.md
|   |   |   |-- Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionClearBehavior.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyBinding_T_.BindingNode.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyBinding_T_.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyBinding.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyFlushResult.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyInvalidationClassifier.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyKey.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyOptions.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStartOptions.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStore.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStore.PendingWrite.md
|   |   |   |-- Cerneala.UI.Motion.Properties.MotionPropertyStore.PendingWriteKind.md
|   |   |   |-- Cerneala.UI.Motion.Specs.CubicBezierEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.DecaySpec_T_.DecaySampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.DecaySpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Easings.LinearEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Easings.md
|   |   |   |-- Cerneala.UI.Motion.Specs.FillMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.IEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.KeyframesSpec_T_.KeyframesSampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.KeyframesSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Motion.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Motion.UntypedSpringSpec.md
|   |   |   |-- Cerneala.UI.Motion.Specs.Motion.UntypedTweenSpec.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionCompletion.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionKeyframe_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSampler_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpec.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionSpecContext.md
|   |   |   |-- Cerneala.UI.Motion.Specs.MotionVelocity_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.PingPongSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.PingPongSpec_T_.Sampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RepeatSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RepeatSpec_T_.Sampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RepeatSpec_T_.StaticSampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.RetargetMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.SpringSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.SpringSpec_T_.VectorSpringSampler.md
|   |   |   |-- Cerneala.UI.Motion.Specs.SpringVelocityMode.md
|   |   |   |-- Cerneala.UI.Motion.Specs.StepEasing.md
|   |   |   |-- Cerneala.UI.Motion.Specs.StepPosition.md
|   |   |   |-- Cerneala.UI.Motion.Specs.TweenSpec_T_.md
|   |   |   |-- Cerneala.UI.Motion.Specs.TweenSpec_T_.TweenSampler.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionStateRule.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionTokens.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionVisualStateController.md
|   |   |   |-- Cerneala.UI.Motion.States.MotionVisualStateSnapshot.md
|   |   |   |-- Cerneala.UI.Motion.States.ThemeMotionTokens.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransaction.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransactionContext.md
|   |   |   |-- Cerneala.UI.Motion.Transactions.MotionTransactionContext.UntypedMotionSpecAdapter_T_.md
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
|   |   |   |-- Cerneala.UI.Rendering.ClipNode.ClipBox.md
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
|   |   |   |-- Cerneala.UI.Resources.IImageLoader.md
|   |   |   |-- Cerneala.UI.Resources.ImageResource.md
|   |   |   |-- Cerneala.UI.Resources.ImageResourceCache.md
|   |   |   |-- Cerneala.UI.Resources.IObservableResourceProvider.md
|   |   |   |-- Cerneala.UI.Resources.IResourceProvider.md
|   |   |   |-- Cerneala.UI.Resources.MonoGame.MonoGameImageLoader.md
|   |   |   |-- Cerneala.UI.Resources.ResourceChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyChange.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.ReferenceEqualityComparer.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.ResourceDependency.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.ResourceKey.md
|   |   |   |-- Cerneala.UI.Resources.ResourceDependencyTracker.ResourceProviderReferenceEqualityComparer.md
|   |   |   |-- Cerneala.UI.Resources.ResourceId_T_.md
|   |   |   |-- Cerneala.UI.Resources.ResourceStore.Entry.md
|   |   |   |-- Cerneala.UI.Resources.ResourceStore.md
|   |   |   |-- Cerneala.UI.Resources.ResourceStore.ResourceKey.md
|   |   |   |-- Cerneala.UI.Text.BidiTextRun.md
|   |   |   |-- Cerneala.UI.Text.BidiTextService.md
|   |   |   |-- Cerneala.UI.Text.FontResolver.FallbackDrawFont.md
|   |   |   |-- Cerneala.UI.Text.FontResolver.md
|   |   |   |-- Cerneala.UI.Text.LineBreakService.md
|   |   |   |-- Cerneala.UI.Text.LineBreakService.TextElement.md
|   |   |   |-- Cerneala.UI.Text.ResolvedTextFont.md
|   |   |   |-- Cerneala.UI.Text.TextAspect.md
|   |   |   |-- Cerneala.UI.Text.TextCaret.md
|   |   |   |-- Cerneala.UI.Text.TextCaretLayout.md
|   |   |   |-- Cerneala.UI.Text.TextCompositionManager.md
|   |   |   |-- Cerneala.UI.Text.TextCompositionState.md
|   |   |   |-- Cerneala.UI.Text.TextDirection.md
|   |   |   |-- Cerneala.UI.Text.TextDocument.md
|   |   |   |-- Cerneala.UI.Text.TextEditingController.md
|   |   |   |-- Cerneala.UI.Text.TextEditor.md
|   |   |   |-- Cerneala.UI.Text.TextEditorSnapshot.md
|   |   |   |-- Cerneala.UI.Text.TextLayoutCache.md
|   |   |   |-- Cerneala.UI.Text.TextLayoutKey.md
|   |   |   |-- Cerneala.UI.Text.TextLine.md
|   |   |   |-- Cerneala.UI.Text.TextLineMetrics.md
|   |   |   |-- Cerneala.UI.Text.TextMeasurer.md
|   |   |   |-- Cerneala.UI.Text.TextMeasureResult.md
|   |   |   |-- Cerneala.UI.Text.TextRenderer.md
|   |   |   |-- Cerneala.UI.Text.TextSelection.md
|   |   |   |-- Cerneala.UI.Text.TextTrimming.md
|   |   |   |-- Cerneala.UI.Text.TextWrapping.md
|   |   |   |-- Cerneala.UI.Text.UndoRedoStack.md
|   |   |   |-- Cerneala.UI.Theming.DefaultTheme.md
|   |   |   |-- Cerneala.UI.Theming.Theme.EntryKey.md
|   |   |   |-- Cerneala.UI.Theming.Theme.md
|   |   |   |-- Cerneala.UI.Theming.ThemeChangedEventArgs.md
|   |   |   |-- Cerneala.UI.Theming.ThemeKey_T_.md
|   |   |   |-- Cerneala.UI.Theming.ThemePalette.md
|   |   |   |-- Cerneala.UI.Theming.ThemeProvider.md
|   |   |   +-- Cerneala.UI.Theming.ThemeResource_T_.md
|   |   +-- manifest.json
|   |-- .nojekyll
|   |-- documentation.html
|   +-- index.html
|-- Drawing/
|   |-- MonoGame/
|   |   |-- MonoGameClipStack.cs
|   |   |-- MonoGameDrawingBackend.cs
|   |   |-- MonoGameDrawMapper.cs
|   |   +-- MonoGameImage.cs
|   |-- Skia/
|   |-- Text/
|   |   |-- RasterizedText.cs
|   |   |-- SkiaFont.cs
|   |   |-- SkiaTextRasterizer.cs
|   |   |-- SkiaTextShaper.cs
|   |   |-- SystemFontSource.cs
|   |   |-- TextCaretVerticalMetrics.cs
|   |   |-- TextShaper.cs
|   |   +-- TextShapeResult.cs
|   |-- DrawArgument.cs
|   |-- DrawColor.cs
|   |-- DrawCommand.cs
|   |-- DrawCommandKind.cs
|   |-- DrawCommandList.cs
|   |-- DrawingContext.cs
|   |-- DrawPoint.cs
|   |-- DrawRect.cs
|   |-- DrawSize.cs
|   |-- DrawTextRun.cs
|   |-- IDrawFont.cs
|   |-- IDrawImage.cs
|   |-- IDrawingBackend.cs
|   +-- IFontSource.cs
|-- Generated/
|-- Playground/
|   |-- AvaloniaOracle/
|   |   |-- App.cs
|   |   |-- AvaloniaOracle.csproj
|   |   |-- MainWindow.cs
|   |   +-- Program.cs
|   +-- Cerneala.Playground/
|       |-- Cerneala.Playground.csproj
|       |-- MainWindow.cui.xml
|       +-- MainWindow.cui.xml.cs
|-- Properties/
|   +-- AssemblyInfo.cs
|-- tests/
|   |-- Cerneala.Tests/
|   |   |-- Architecture/
|   |   |   |-- DeveloperPreviewScopeTests.cs
|   |   |   |-- ModernAspectArchitectureTests.cs
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
|   |   |   |-- ComboBoxTests.cs
|   |   |   |-- ComponentTemplateLifecycleTests.cs
|   |   |   |-- ContentControlTests.cs
|   |   |   |-- ContentPresenterDefaultTextTests.cs
|   |   |   |-- ContentPresenterTests.cs
|   |   |   |-- ContentTemplateTests.cs
|   |   |   |-- ControlTests.cs
|   |   |   |-- DecoratorTests.cs
|   |   |   |-- ElementAspectTests.cs
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
|   |   |   |-- TemplateReflectionTests.cs
|   |   |   |-- TextBlockInvalidationTests.cs
|   |   |   |-- TextBlockLayoutContractTests.cs
|   |   |   |-- TextBlockTests.cs
|   |   |   |-- TextBoxCaretBlinkTests.cs
|   |   |   |-- TextBoxClipboardShortcutTests.cs
|   |   |   |-- TextBoxEditingVisualContractTests.cs
|   |   |   |-- TextBoxTests.cs
|   |   |   |-- TextBoxTwoWayBindingTests.cs
|   |   |   |-- ToggleButtonTests.cs
|   |   |   |-- ToolTipTests.cs
|   |   |   |-- UserControlTests.cs
|   |   |   +-- WpfEventSurfaceTests.cs
|   |   |-- Docs/
|   |   |   |-- AspectDocsTests.cs
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
|   |   |   +-- MainWindowContractTests.cs
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
|   |   |   |-- Aspect/
|   |   |   |   |-- AspectEngineStressBudgetTests.cs
|   |   |   |   |-- AspectEngineTests.cs
|   |   |   |   |-- AspectPackageTests.cs
|   |   |   |   |-- AspectRootRegistryTests.cs
|   |   |   |   |-- AspectRuleSetTests.cs
|   |   |   |   |-- AspectSlotTests.cs
|   |   |   |   |-- AspectStateSetTests.cs
|   |   |   |   |-- AspectTokenTests.cs
|   |   |   |   |-- AspectVariantTests.cs
|   |   |   |   |-- DefaultAspectPackageTests.cs
|   |   |   |   +-- ThemeTokenBridgeTests.cs
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
|   |   |   |   |-- ModernAspectTraceTests.cs
|   |   |   |   |-- RenderCacheDumperTests.cs
|   |   |   |   |-- RoutedEventTraceTests.cs
|   |   |   |   +-- RuntimeDiagnosticsTests.cs
|   |   |   |-- Elements/
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
|   |   |   |   |-- FakeDrawingBackend.cs
|   |   |   |   |-- FakeInputSource.cs
|   |   |   |   |-- FakeUiClock.cs
|   |   |   |   |-- GridAuthoringFrameContractTests.cs
|   |   |   |   |-- MonoGameContentServicesLifetimeTests.cs
|   |   |   |   |-- MonoGameUiHostBoundaryTests.cs
|   |   |   |   |-- ObservableListAuthoringSliceTests.cs
|   |   |   |   |-- RetainedStressBudgetTests.cs
|   |   |   |   |-- TabNavigationFrameContractTests.cs
|   |   |   |   |-- UiHostFrameContractTests.cs
|   |   |   |   |-- UiHostFrameStatsIntegrityTests.cs
|   |   |   |   |-- UiHostLateTreeMutationTests.cs
|   |   |   |   |-- UiHostScaleHitTestContractTests.cs
|   |   |   |   |-- UiHostTests.cs
|   |   |   |   |-- UiHostViewportFrameContractTests.cs
|   |   |   |   |-- UiViewportScaleContractTests.cs
|   |   |   |   |-- UiViewportTests.cs
|   |   |   |   |-- Win32WindowPlatformTests.cs
|   |   |   |   |-- WindowRuntimeTests.cs
|   |   |   |   +-- WindowsDxProcessSmokeTests.cs
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
|   |   |   |   |   |-- MotionCompositionReducedMotionTests.cs
|   |   |   |   |   |-- MotionGroupTests.cs
|   |   |   |   |   |-- MotionRepeatTimelineTests.cs
|   |   |   |   |   |-- MotionSystemTests.cs
|   |   |   |   |   +-- MotionValueTests.cs
|   |   |   |   |-- Diagnostics/
|   |   |   |   |   +-- MotionDiagnosticsTests.cs
|   |   |   |   |-- Input/
|   |   |   |   |   +-- MotionInputTimelineTests.cs
|   |   |   |   |-- Interpolation/
|   |   |   |   |   +-- ValueMixerBuiltInTests.cs
|   |   |   |   |-- Layout/
|   |   |   |   |   +-- LayoutMotionCoordinatorTests.cs
|   |   |   |   |-- Presence/
|   |   |   |   |   +-- PresenceCoordinatorTests.cs
|   |   |   |   |-- Properties/
|   |   |   |   |   +-- MotionPropertyBindingTests.cs
|   |   |   |   |-- Specs/
|   |   |   |   |   |-- EasingTests.cs
|   |   |   |   |   +-- MotionSpecTests.cs
|   |   |   |   |-- States/
|   |   |   |   |-- Transactions/
|   |   |   |   |   +-- MotionTransactionTests.cs
|   |   |   |   |-- MotionAllocationTests.cs
|   |   |   |   |-- MotionFacadeTests.cs
|   |   |   |   |-- MotionReflectionTests.cs
|   |   |   |   +-- MotionStressTests.cs
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
|   |   |   |   |-- RenderLayerMotionTests.cs
|   |   |   |   |-- RenderQueueProcessorTests.cs
|   |   |   |   |-- RenderStressBudgetTests.cs
|   |   |   |   |-- ResourceRenderDependencyTests.cs
|   |   |   |   |-- RetainedRenderCacheTests.cs
|   |   |   |   |-- RetainedRendererDrawPurityTests.cs
|   |   |   |   |-- RetainedRendererTests.cs
|   |   |   |   +-- TextRenderDependencyTests.cs
|   |   |   |-- Resources/
|   |   |   |   |-- DetachedResourceDependencyCleanupTests.cs
|   |   |   |   |-- ElementResourceDictionaryTests.cs
|   |   |   |   |-- FontResourceInvalidationTests.cs
|   |   |   |   |-- HostResourceInvalidationIntegrationTests.cs
|   |   |   |   |-- ImageResourceCacheTests.cs
|   |   |   |   |-- ImageResourceInvalidationTests.cs
|   |   |   |   |-- PathBackedImageResourceIntegrationTests.cs
|   |   |   |   |-- ResourceDependencyTrackerTests.cs
|   |   |   |   |-- ResourceIdTests.cs
|   |   |   |   +-- ResourceStoreTests.cs
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
|   |   +-- GlobalUsings.cs
|   |-- Cerneala.Tests.SourceGen/
|   |   |-- Cerneala.Tests.SourceGen.csproj
|   |   +-- UiMarkupGeneratorTests.cs
|   +-- Cerneala.WindowsDxSmoke/
|       |-- Cerneala.WindowsDxSmoke.csproj
|       +-- WindowsDxSmokeApplication.cs
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
|   |-- Aspect/
|   |   |-- AspectCatalog.cs
|   |   |-- AspectCondition.cs
|   |   |-- AspectConditionDependency.cs
|   |   |-- AspectConditionNode.cs
|   |   |-- AspectConditionResult.cs
|   |   |-- AspectDataContext.cs
|   |   |-- AspectDataDependency.cs
|   |   |-- AspectDeclaration.cs
|   |   |-- AspectDependencySet.cs
|   |   |-- AspectDiagnostics.cs
|   |   |-- AspectEngine.cs
|   |   |-- AspectEngineCounters.cs
|   |   |-- AspectEngineElementState.cs
|   |   |-- AspectEnvironment.cs
|   |   |-- AspectInvalidation.cs
|   |   |-- AspectInvalidationGraph.cs
|   |   |-- AspectLayer.cs
|   |   |-- AspectMatchContext.cs
|   |   |-- AspectMotion.cs
|   |   |-- AspectPackage.cs
|   |   |-- AspectPackageBuilder.cs
|   |   |-- AspectProcessor.cs
|   |   |-- AspectRef.cs
|   |   |-- AspectRegistry.cs
|   |   |-- AspectResolutionContext.cs
|   |   |-- AspectResolutionStep.cs
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
|   |   |-- AspectTokenTrace.cs
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
|   |   |   |-- ItemCollection.cs
|   |   |   |-- ItemContainerGenerator.cs
|   |   |   |-- ItemContainerRecyclePool.cs
|   |   |   +-- ItemsPanelTemplate.cs
|   |   |-- Primitives/
|   |   |   |-- ButtonBase.cs
|   |   |   |-- DragCompletedEventArgs.cs
|   |   |   |-- DragDeltaEventArgs.cs
|   |   |   |-- DragStartedEventArgs.cs
|   |   |   |-- RangeBase.cs
|   |   |   |-- ScrollBar.cs
|   |   |   |-- ScrollEventArgs.cs
|   |   |   |-- Selector.cs
|   |   |   |-- Thumb.cs
|   |   |   |-- ToggleButton.cs
|   |   |   +-- Track.cs
|   |   |-- Selection/
|   |   |   |-- SelectionChangedEventArgs.cs
|   |   |   |-- SelectionChangeResult.cs
|   |   |   |-- SelectionModel.cs
|   |   |   +-- SelectionModel{T}.cs
|   |   |-- Shapes/
|   |   |   |-- Ellipse.cs
|   |   |   |-- Path.cs
|   |   |   |-- Rectangle.cs
|   |   |   +-- Shape.cs
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
|   |   |   |-- TemplateBinding{T}.cs
|   |   |   |-- TemplatePartAttribute.cs
|   |   |   |-- TemplatePartMap.cs
|   |   |   |-- TemplateRecycleKey.cs
|   |   |   |-- TemplateRecyclePool.cs
|   |   |   |-- TemplateSlotMap.cs
|   |   |   +-- TemplateTokenBinding.cs
|   |   |-- Border.cs
|   |   |-- Button.cs
|   |   |-- Canvas.cs
|   |   |-- CheckBox.cs
|   |   |-- ComboBox.cs
|   |   |-- ContentControl.cs
|   |   |-- ContentPresenter.cs
|   |   |-- Control.cs
|   |   |-- ControlTextFont.cs
|   |   |-- Decorator.cs
|   |   |-- Image.cs
|   |   |-- InkCanvas.cs
|   |   |-- InkCanvasEventArgs.cs
|   |   |-- IScrollInfo.cs
|   |   |-- ItemsControl.cs
|   |   |-- ItemsPresenter.cs
|   |   |-- Label.cs
|   |   |-- ListBox.cs
|   |   |-- ListBoxItem.cs
|   |   |-- Panel.cs
|   |   |-- PasswordBox.cs
|   |   |-- PopupRoot.cs
|   |   |-- ProgressBar.cs
|   |   |-- RadioButton.cs
|   |   |-- ResizeMode.cs
|   |   |-- ScrollBarVisibility.cs
|   |   |-- ScrollChangedEventArgs.cs
|   |   |-- ScrollContentPresenter.cs
|   |   |-- ScrollViewer.cs
|   |   |-- Slider.cs
|   |   |-- StackPanel.cs
|   |   |-- TabControl.cs
|   |   |-- TabItem.cs
|   |   |-- TextBlock.cs
|   |   |-- TextBox.cs
|   |   |-- TextBoxBase.cs
|   |   |-- TextChangedEventArgs.cs
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
|   |-- Diagnostics/
|   |   |-- AspectTrace.cs
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
|   |   +-- RuntimeDiagnostics.cs
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
|   |   |-- IElementLifecycleBehavior.cs
|   |   |-- InheritedPropertyPropagator.cs
|   |   |-- UIElement.cs
|   |   |-- UIElement.Events.cs
|   |   |-- UIElement.InputEvents.cs
|   |   |-- UIElementCollection.cs
|   |   |-- UIElementVisibility.cs
|   |   +-- UIRoot.cs
|   |-- Hosting/
|   |   |-- MonoGame/
|   |   |   |-- MonoGameContentServices.cs
|   |   |   |-- MonoGameUiHost.cs
|   |   |   +-- MonoGameUiHostOptions.cs
|   |   |-- Windows/
|   |   |   |-- GeneratedWindowApplication.cs
|   |   |   |-- IWindowPlatform.cs
|   |   |   |-- Win32.cs
|   |   |   |-- Win32InputSource.cs
|   |   |   |-- Win32WindowPlatform.cs
|   |   |   |-- WindowApplicationRuntime.cs
|   |   |   +-- WindowsDxWindowGraphicsSession.cs
|   |   |-- IUiBackend.cs
|   |   |-- IUiClock.cs
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
|   |   |-- IInputActivatable.cs
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
|   |   |-- ContentPropertyAttribute.cs
|   |   |-- DesignTimeOnlyAttribute.cs
|   |   |-- GeneratedMarkupConditions.cs
|   |   |-- GeneratedUiFactory.cs
|   |   |-- MarkupAspectResource.cs
|   |   |-- MarkupDiagnostic.cs
|   |   |-- MarkupLoadOptions.cs
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
|   |   |   |-- MotionThreadGuard.cs
|   |   |   |-- MotionTimeline.cs
|   |   |   |-- MotionTimelineRegistry.cs
|   |   |   |-- MotionValue.cs
|   |   |   |-- MotionValue{T}.cs
|   |   |   |-- ReducedMotionMode.cs
|   |   |   |-- ReducedMotionPolicy.cs
|   |   |   +-- SystemMotionClock.cs
|   |   |-- Diagnostics/
|   |   |   |-- MotionDiagnostics.cs
|   |   |   |-- MotionGraphSnapshot.cs
|   |   |   |-- MotionTrace.cs
|   |   |   +-- MotionTraceEvent.cs
|   |   |-- Input/
|   |   |   |-- DragMotionController.cs
|   |   |   |-- GestureMotionController.cs
|   |   |   |-- MotionRange.cs
|   |   |   |-- PointerMotionState.cs
|   |   |   |-- ScrollMotionBinding.cs
|   |   |   |-- ScrollTimeline.cs
|   |   |   +-- VelocityTracker.cs
|   |   |-- Interpolation/
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
|   |   |   +-- ValueMixerRegistry.cs
|   |   |-- Layout/
|   |   |   |-- LayoutMotionBinding.cs
|   |   |   |-- LayoutMotionCoordinator.cs
|   |   |   |-- LayoutMotionId.cs
|   |   |   |-- LayoutMotionOptions.cs
|   |   |   +-- LayoutSnapshot.cs
|   |   |-- Presence/
|   |   |   |-- PresenceCoordinator.cs
|   |   |   |-- PresenceHandle.cs
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
|   |   |-- MotionPropertyShortcut.cs
|   |   +-- MotionStateBuilder.cs
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
|   |   |-- ResourceDictionary.cs
|   |   |-- ResourceId{T}.cs
|   |   +-- ResourceStore.cs
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
|   +-- Theming/
|       |-- DefaultTheme.cs
|       |-- Theme.cs
|       |-- ThemeKey{T}.cs
|       |-- ThemePalette.cs
|       |-- ThemeProvider.cs
|       +-- ThemeResource.cs
|-- .gitignore
|-- AGENTS.md
|-- architecture.md
|-- AUDIT_FIX_PLAN.md
|-- Cerneala.csproj
|-- Cerneala.slnx
|-- ClassChecklist.md
|-- ConceptualIdeas.md
|-- GameBootstrap.cs
|-- global.json
|-- mcp.md
|-- ROADMAP.md
|-- ROADMAPv2_AUDIT.md
|-- ROADMAPv2.md
+-- roslyn_indexer_codex_plan_final.md
```

