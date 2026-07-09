# Cerneala Roadmap

This file is the long-term memory for the project.

Goal: build a WPF-inspired UI framework on top of the existing drawing and input foundations. Names should stay familiar to WPF developers where that helps: `DependencyObject`, `UIElement`, `FrameworkElement`, `Control`, `Panel`, `Canvas`, `Button`, `TextBox`, `ItemsControl`, `Style`, `ControlTemplate`, etc.

This is not a promise to clone every WPF feature exactly. It is the planned project map. If scope changes, update this file first.

Before implementing or changing anything from this roadmap, read `architecture.md`. It explains the existing `Drawing` and `UI/Input` architecture, including which planned WPF-style classes would duplicate existing primitives if implemented blindly.

Legend:

- `[x]` Exists now.
- `[ ]` Planned.
- `[~]` Partially exists or likely needs reshaping later.

## 0. Existing Foundation

These files already exist and are the base that future WPF-style layers must build on.

### Drawing

- [x] `Drawing/DrawArgument.cs`
- [x] `Drawing/DrawColor.cs`
- [x] `Drawing/DrawCommand.cs`
- [x] `Drawing/DrawCommandKind.cs`
- [x] `Drawing/DrawCommandList.cs`
- [x] `Drawing/DrawingContext.cs`
- [x] `Drawing/DrawPoint.cs`
- [x] `Drawing/DrawRect.cs`
- [x] `Drawing/DrawTextRun.cs`
- [x] `Drawing/IDrawFont.cs`
- [x] `Drawing/IDrawImage.cs`
- [x] `Drawing/IDrawingBackend.cs`
- [x] `Drawing/IFontSource.cs`
- [x] `Drawing/MonoGame/MonoGameDrawingBackend.cs`
- [x] `Drawing/MonoGame/MonoGameImage.cs`
- [x] `Drawing/Text/RasterizedText.cs`
- [x] `Drawing/Text/SkiaFont.cs`
- [x] `Drawing/Text/SkiaTextRasterizer.cs`
- [x] `Drawing/Text/SkiaTextShaper.cs`
- [x] `Drawing/Text/SystemFontSource.cs`
- [x] `Drawing/Text/TextShapeResult.cs`

### Input

- [x] `UI/Input/CanExecuteRoutedEventArgs.cs`
- [x] `UI/Input/CommandBinding.cs`
- [x] `UI/Input/CommandEvents.cs`
- [x] `UI/Input/ExecutedRoutedEventArgs.cs`
- [x] `UI/Input/ICommand.cs`
- [x] `UI/Input/IInputSource.cs`
- [x] `UI/Input/InputButtonState.cs`
- [x] `UI/Input/InputEvents.cs`
- [x] `UI/Input/InputFrame.cs`
- [x] `UI/Input/InputKey.cs`
- [x] `UI/Input/InputMouseButton.cs`
- [x] `UI/Input/KeyboardFocusChangedEventArgs.cs`
- [x] `UI/Input/KeyboardSnapshot.cs`
- [x] `UI/Input/KeyEventArgs.cs`
- [x] `UI/Input/MonoGame/MonoGameInputMapper.cs`
- [x] `UI/Input/MonoGame/MonoGameInputSource.cs`
- [x] `UI/Input/MouseButtonEventArgs.cs`
- [x] `UI/Input/MouseEventArgs.cs`
- [x] `UI/Input/MouseWheelEventArgs.cs`
- [x] `UI/Input/PointerSnapshot.cs`
- [x] `UI/Input/RoutedCommand.cs`
- [x] `UI/Input/RoutedEvent.cs`
- [x] `UI/Input/RoutedEventArgs.cs`
- [x] `UI/Input/RoutedEventRegistry.cs`
- [x] `UI/Input/RoutedEventRouter.cs`
- [x] `UI/Input/RoutingStrategy.cs`
- [x] `UI/Input/TextCompositionEventArgs.cs`
- [x] `UI/Input/TextInputSnapshotEvent.cs`
- [x] `UI/Input/UiElementId.cs`
- [x] `UI/Input/UiInputElement.cs`
- [x] `UI/Input/UiInputTree.cs`

## 1. WPF-Style Object And Property System

This is the backbone needed before real controls can behave like WPF controls.

- [ ] `UI/Threading/DispatcherObject.cs`
- [ ] `UI/DependencyObject.cs`
- [ ] `UI/DependencyProperty.cs`
- [ ] `UI/DependencyPropertyKey.cs`
- [ ] `UI/PropertyMetadata.cs`
- [ ] `UI/FrameworkPropertyMetadata.cs`
- [ ] `UI/FrameworkPropertyMetadataOptions.cs`
- [ ] `UI/DependencyPropertyChangedEventArgs.cs`
- [ ] `UI/CoerceValueCallback.cs`
- [ ] `UI/PropertyChangedCallback.cs`
- [ ] `UI/ValidateValueCallback.cs`
- [ ] `UI/EffectiveValueEntry.cs`
- [ ] `UI/DependencyPropertyRegistry.cs`
- [ ] `UI/BaseValueSource.cs`
- [ ] `UI/ValueSource.cs`
- [ ] `UI/LocalValueEnumerator.cs`
- [ ] `UI/DependencyPropertyDescriptor.cs`
- [ ] `UI/UnsetValue.cs`
- [ ] `UI/AttachedPropertyBrowsableForTypeAttribute.cs`
- [ ] `UI/AttachedPropertyBrowsableWhenAttributePresentAttribute.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/DispatcherObjectTests.cs`
- [ ] `tests/Cerneala.Tests/UI/DependencyObjectTests.cs`
- [ ] `tests/Cerneala.Tests/UI/DependencyPropertyTests.cs`
- [ ] `tests/Cerneala.Tests/UI/PropertyMetadataTests.cs`
- [ ] `tests/Cerneala.Tests/UI/AttachedPropertyTests.cs`

## 2. Dispatcher And Application Host

Keep this small at first. Cerneala is game-loop based now, so this should integrate with update/draw cycles instead of copying WPF threading blindly.

- [ ] `UI/Application.cs`
- [ ] `UI/Threading/Dispatcher.cs`
- [ ] `UI/Threading/DispatcherPriority.cs`
- [ ] `UI/Threading/DispatcherOperation.cs`
- [ ] `UI/Threading/DispatcherTimer.cs`
- [ ] `UI/Window.cs`
- [ ] `UI/WindowCollection.cs`
- [ ] `UI/PresentationSource.cs`
- [ ] `UI/HwndSource.cs`
- [ ] `UI/MonoGame/MonoGamePresentationSource.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/ApplicationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Threading/DispatcherTests.cs`
- [ ] `tests/Cerneala.Tests/UI/WindowTests.cs`

## 3. Core Visual Tree

This is where WPF-style rendering and tree traversal starts.

- [ ] `UI/Media/Visual.cs`
- [ ] `UI/Media/ContainerVisual.cs`
- [ ] `UI/Media/DrawingVisual.cs`
- [ ] `UI/Media/VisualCollection.cs`
- [ ] `UI/Media/VisualTreeHelper.cs`
- [ ] `UI/LogicalTreeHelper.cs`
- [ ] `UI/ContentElement.cs`
- [ ] `UI/FrameworkContentElement.cs`
- [ ] `UI/Media/HitTestResult.cs`
- [ ] `UI/Media/HitTestParameters.cs`
- [ ] `UI/Media/PointHitTestParameters.cs`
- [ ] `UI/Media/GeometryHitTestParameters.cs`
- [ ] `UI/Media/HitTestFilterBehavior.cs`
- [ ] `UI/Media/HitTestResultBehavior.cs`
- [ ] `UI/Media/HitTestFilterCallback.cs`
- [ ] `UI/Media/HitTestResultCallback.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Media/VisualTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/VisualTreeHelperTests.cs`
- [ ] `tests/Cerneala.Tests/UI/LogicalTreeHelperTests.cs`
- [ ] `tests/Cerneala.Tests/UI/ContentElementTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/HitTestingTests.cs`

## 4. Layout And Elements

This is the first controls foundation.

- [ ] `UI/Controls/Size.cs`
- [ ] `UI/Controls/Point.cs`
- [ ] `UI/Controls/Rect.cs`
- [ ] `UI/Controls/Thickness.cs`
- [ ] `UI/Controls/CornerRadius.cs`
- [ ] `UI/Controls/HorizontalAlignment.cs`
- [ ] `UI/Controls/VerticalAlignment.cs`
- [ ] `UI/Controls/Visibility.cs`
- [ ] `UI/Controls/FlowDirection.cs`
- [ ] `UI/Controls/UIElement.cs`
- [ ] `UI/Controls/FrameworkElement.cs`
- [ ] `UI/Controls/NameScope.cs`
- [ ] `UI/Controls/ControlRoot.cs`
- [ ] `UI/Controls/LayoutManager.cs`
- [ ] `UI/Controls/MeasureRequest.cs`
- [ ] `UI/Controls/ArrangeRequest.cs`
- [ ] `UI/Controls/SizeChangedEventArgs.cs`
- [ ] `UI/Controls/HitTestResult.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/LayoutPrimitiveTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/UIElementTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/FrameworkElementTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ControlRootTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/LayoutManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/HitTestingTests.cs`

## 5. Routed Events, Focus, And Input Bridge

Some routed event primitives already exist. This phase makes them usable from `UIElement` and controls.

- [~] `UI/Input/RoutedEvent.cs`
- [~] `UI/Input/RoutedEventArgs.cs`
- [~] `UI/Input/RoutedEventRegistry.cs`
- [~] `UI/Input/RoutedEventRouter.cs`
- [ ] `UI/Input/EventManager.cs`
- [ ] `UI/Input/RoutedEventHandlerInfo.cs`
- [ ] `UI/Input/InputManager.cs`
- [ ] `UI/Input/Keyboard.cs`
- [ ] `UI/Input/Mouse.cs`
- [ ] `UI/Input/FocusManager.cs`
- [ ] `UI/Input/KeyboardNavigation.cs`
- [ ] `UI/Input/KeyboardNavigationMode.cs`
- [ ] `UI/Input/MouseDevice.cs`
- [ ] `UI/Input/KeyboardDevice.cs`
- [ ] `UI/Input/Cursors.cs`
- [ ] `UI/Input/Cursor.cs`
- [ ] `UI/Input/DragDrop.cs`
- [ ] `UI/Input/DragEventArgs.cs`
- [ ] `UI/DataObject.cs`
- [ ] `UI/Clipboard.cs`
- [ ] `UI/Controls/Input/ControlInputBridge.cs`

Tests:

- [x] `tests/Cerneala.Tests/Input/RoutedEventTests.cs`
- [x] `tests/Cerneala.Tests/Input/RoutedEventRouterTests.cs`
- [ ] `tests/Cerneala.Tests/Input/EventManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/InputManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/FocusManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/DragDropTests.cs`
- [ ] `tests/Cerneala.Tests/UI/ClipboardTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ControlInputBridgeTests.cs`

## 6. Commanding

Some commanding primitives already exist. This phase brings them closer to WPF shape.

- [~] `UI/Input/ICommand.cs`
- [~] `UI/Input/RoutedCommand.cs`
- [~] `UI/Input/CommandBinding.cs`
- [ ] `UI/Input/RoutedUICommand.cs`
- [ ] `UI/Input/CommandManager.cs`
- [ ] `UI/Input/InputBinding.cs`
- [ ] `UI/Input/KeyBinding.cs`
- [ ] `UI/Input/MouseBinding.cs`
- [ ] `UI/Input/InputGesture.cs`
- [ ] `UI/Input/KeyGesture.cs`
- [ ] `UI/Input/MouseGesture.cs`
- [ ] `UI/Input/ApplicationCommands.cs`
- [ ] `UI/Input/NavigationCommands.cs`
- [ ] `UI/Input/EditingCommands.cs`
- [ ] `UI/Input/ComponentCommands.cs`
- [ ] `UI/Input/MediaCommands.cs`

Tests:

- [x] `tests/Cerneala.Tests/Input/CommandingTests.cs`
- [ ] `tests/Cerneala.Tests/Input/CommandManagerTests.cs`
- [ ] `tests/Cerneala.Tests/Input/InputBindingTests.cs`
- [ ] `tests/Cerneala.Tests/Input/InputGestureTests.cs`

## 7. Panels

Panels should follow WPF names and basic layout behavior.

- [ ] `UI/Controls/Panel.cs`
- [ ] `UI/Controls/Canvas.cs`
- [ ] `UI/Controls/StackPanel.cs`
- [ ] `UI/Controls/Orientation.cs`
- [ ] `UI/Controls/Grid.cs`
- [ ] `UI/Controls/GridSplitter.cs`
- [ ] `UI/Controls/ColumnDefinition.cs`
- [ ] `UI/Controls/RowDefinition.cs`
- [ ] `UI/Controls/DefinitionBase.cs`
- [ ] `UI/Controls/GridLength.cs`
- [ ] `UI/Controls/GridUnitType.cs`
- [ ] `UI/Controls/DockPanel.cs`
- [ ] `UI/Controls/Dock.cs`
- [ ] `UI/Controls/WrapPanel.cs`
- [ ] `UI/Controls/UniformGrid.cs`
- [ ] `UI/Controls/VirtualizingPanel.cs`
- [ ] `UI/Controls/VirtualizingStackPanel.cs`
- [ ] `UI/Controls/Primitives/ResizeGrip.cs`
- [ ] `UI/Controls/Primitives/BulletDecorator.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/PanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/CanvasTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/StackPanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/GridTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/GridSplitterTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DockPanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/WrapPanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/UniformGridTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/VirtualizingStackPanelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ResizeGripTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/BulletDecoratorTests.cs`

## 8. Control Base Classes

This is the layer real controls inherit from.

- [ ] `UI/Controls/Control.cs`
- [ ] `UI/Controls/ContentControl.cs`
- [ ] `UI/Controls/HeaderedContentControl.cs`
- [ ] `UI/Controls/ItemsControl.cs`
- [ ] `UI/Controls/HeaderedItemsControl.cs`
- [ ] `UI/Controls/UserControl.cs`
- [ ] `UI/Controls/Templates/ControlTemplate.cs`
- [ ] `UI/Controls/Items/ItemsPanelTemplate.cs`
- [ ] `UI/Controls/Templates/DataTemplate.cs`
- [ ] `UI/Controls/HierarchicalDataTemplate.cs`
- [ ] `UI/Controls/TemplateBinding.cs`
- [ ] `UI/Controls/TemplateBindingExpression.cs`
- [ ] `UI/Controls/Templates/TemplatePartAttribute.cs`
- [ ] `UI/Controls/TemplateVisualStateAttribute.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ContentControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/UserControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ItemsControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ControlTemplateTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DataTemplateTests.cs`

## 9. Styling And Resources

This is where WPF-like syntax starts becoming pleasant instead of hardcoded.

- [ ] `UI/Style.cs`
- [ ] `UI/StyleSelector.cs`
- [ ] `UI/Setter.cs`
- [ ] `UI/SetterBase.cs`
- [ ] `UI/SetterBaseCollection.cs`
- [ ] `UI/TriggerBase.cs`
- [ ] `UI/Trigger.cs`
- [ ] `UI/MultiTrigger.cs`
- [ ] `UI/DataTrigger.cs`
- [ ] `UI/MultiDataTrigger.cs`
- [ ] `UI/EventTrigger.cs`
- [ ] `UI/ResourceDictionary.cs`
- [ ] `UI/ResourceKey.cs`
- [ ] `UI/ComponentResourceKey.cs`
- [ ] `UI/StaticResourceExtension.cs`
- [ ] `UI/DynamicResourceExtension.cs`
- [ ] `UI/FrameworkTemplate.cs`
- [ ] `UI/TemplateContent.cs`
- [ ] `UI/Themes/ThemeInfoAttribute.cs`
- [ ] `UI/SystemColors.cs`
- [ ] `UI/SystemFonts.cs`
- [ ] `UI/SystemParameters.cs`
- [ ] `Themes/Generic.xaml`

Tests:

- [ ] `tests/Cerneala.Tests/UI/StyleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/StyleSelectorTests.cs`
- [ ] `tests/Cerneala.Tests/UI/SetterTests.cs`
- [ ] `tests/Cerneala.Tests/UI/TriggerTests.cs`
- [ ] `tests/Cerneala.Tests/UI/ResourceDictionaryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/FrameworkTemplateTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Themes/ThemeInfoTests.cs`

## 10. Brushes, Pens, Shapes, And Media

Use WPF names, but bridge rendering to the existing `DrawingContext`.

- [ ] `UI/Freezable.cs`
- [ ] `UI/Media/Animation/Animatable.cs`
- [ ] `UI/Media/Brush.cs`
- [ ] `UI/Media/SolidColorBrush.cs`
- [ ] `UI/Media/LinearGradientBrush.cs`
- [ ] `UI/Media/RadialGradientBrush.cs`
- [ ] `UI/Media/ImageBrush.cs`
- [ ] `UI/Media/DrawingBrush.cs`
- [ ] `UI/Media/VisualBrush.cs`
- [ ] `UI/Media/GradientBrush.cs`
- [ ] `UI/Media/GradientStop.cs`
- [ ] `UI/Media/GradientStopCollection.cs`
- [ ] `UI/Media/Pen.cs`
- [ ] `UI/Media/DoubleCollection.cs`
- [ ] `UI/Media/Geometry.cs`
- [ ] `UI/Media/GeometryGroup.cs`
- [ ] `UI/Media/CombinedGeometry.cs`
- [ ] `UI/Media/RectangleGeometry.cs`
- [ ] `UI/Media/EllipseGeometry.cs`
- [ ] `UI/Media/LineGeometry.cs`
- [ ] `UI/Media/PathGeometry.cs`
- [ ] `UI/Media/PathFigure.cs`
- [ ] `UI/Media/PathFigureCollection.cs`
- [ ] `UI/Media/PathSegment.cs`
- [ ] `UI/Media/LineSegment.cs`
- [ ] `UI/Media/BezierSegment.cs`
- [ ] `UI/Media/QuadraticBezierSegment.cs`
- [ ] `UI/Media/ArcSegment.cs`
- [ ] `UI/Media/StreamGeometry.cs`
- [ ] `UI/Media/Drawing.cs`
- [ ] `UI/Media/GeometryDrawing.cs`
- [ ] `UI/Media/ImageDrawing.cs`
- [ ] `UI/Media/DrawingGroup.cs`
- [ ] `UI/Media/Transform.cs`
- [ ] `UI/Media/TranslateTransform.cs`
- [ ] `UI/Media/ScaleTransform.cs`
- [ ] `UI/Media/RotateTransform.cs`
- [ ] `UI/Media/SkewTransform.cs`
- [ ] `UI/Media/MatrixTransform.cs`
- [ ] `UI/Media/TransformGroup.cs`
- [ ] `UI/Media/Matrix.cs`
- [ ] `UI/Media/Color.cs`
- [ ] `UI/Media/Colors.cs`
- [ ] `UI/Shapes/Shape.cs`
- [ ] `UI/Shapes/Rectangle.cs`
- [ ] `UI/Shapes/Ellipse.cs`
- [ ] `UI/Shapes/Line.cs`
- [ ] `UI/Shapes/Polyline.cs`
- [ ] `UI/Shapes/Polygon.cs`
- [ ] `UI/Shapes/Path.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/FreezableTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/Animation/AnimatableTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/BrushTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/PenTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/GeometryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/PathGeometryTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/DrawingTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/TransformTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/ColorTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Shapes/ShapeTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Shapes/RectangleTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Shapes/EllipseTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Shapes/PathTests.cs`

## 11. Imaging And Multimedia

Image and media APIs should use WPF-style names while staying backend-aware.

- [ ] `UI/Media/ImageSource.cs`
- [ ] `UI/Media/BitmapSource.cs`
- [ ] `UI/Media/BitmapImage.cs`
- [ ] `UI/Media/CroppedBitmap.cs`
- [ ] `UI/Media/TransformedBitmap.cs`
- [ ] `UI/Media/RenderTargetBitmap.cs`
- [ ] `UI/Media/DrawingImage.cs`
- [ ] `UI/Media/BitmapCacheOption.cs`
- [ ] `UI/Media/BitmapCreateOptions.cs`
- [ ] `UI/Media/PixelFormat.cs`
- [ ] `UI/Media/PixelFormats.cs`
- [ ] `UI/Controls/MediaElement.cs`
- [ ] `UI/Media/MediaPlayer.cs`
- [ ] `UI/Media/MediaTimeline.cs`
- [ ] `UI/Media/SoundPlayerAction.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Media/ImageSourceTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/BitmapSourceTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/BitmapImageTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/DrawingImageTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/MediaElementTests.cs`

## 12. Basic Controls

These are the first actual user-facing controls.

- [ ] `UI/Controls/Primitives/ButtonBase.cs`
- [ ] `UI/Controls/Button.cs`
- [ ] `UI/Controls/Primitives/RepeatButton.cs`
- [ ] `UI/Controls/Primitives/ToggleButton.cs`
- [ ] `UI/Controls/CheckBox.cs`
- [ ] `UI/Controls/RadioButton.cs`
- [ ] `UI/Controls/AccessText.cs`
- [ ] `UI/Controls/Label.cs`
- [ ] `UI/Controls/Border.cs`
- [ ] `UI/Controls/Decorator.cs`
- [ ] `UI/Controls/Viewbox.cs`
- [ ] `UI/Controls/Image.cs`
- [ ] `UI/Controls/GroupBox.cs`
- [ ] `UI/Controls/Expander.cs`
- [ ] `UI/Controls/ProgressBar.cs`
- [ ] `UI/Controls/Slider.cs`
- [ ] `UI/Controls/Separator.cs`
- [ ] `UI/Controls/StatusBar.cs`
- [ ] `UI/Controls/StatusBarItem.cs`
- [ ] `UI/Controls/ToolBar.cs`
- [ ] `UI/Controls/ToolBarTray.cs`
- [ ] `UI/Controls/ToolTip.cs`
- [ ] `UI/Controls/ToolTipService.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ButtonTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ToggleButtonTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/CheckBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/RadioButtonTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/AccessTextTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/LabelTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/BorderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/GroupBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ExpanderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ImageTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ProgressBarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/SliderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/StatusBarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ToolBarTests.cs`

## 13. Text Controls

This depends on the existing Skia/HarfBuzz text pipeline.

- [ ] `UI/Controls/TextBlock.cs`
- [ ] `UI/Controls/TextBoxBase.cs`
- [ ] `UI/Controls/TextBox.cs`
- [ ] `UI/Controls/PasswordBox.cs`
- [ ] `UI/Controls/RichTextBox.cs`
- [ ] `UI/Media/FontFamily.cs`
- [ ] `UI/Media/Typeface.cs`
- [ ] `UI/FontWeight.cs`
- [ ] `UI/FontWeights.cs`
- [ ] `UI/FontStyle.cs`
- [ ] `UI/FontStyles.cs`
- [ ] `UI/FontStretch.cs`
- [ ] `UI/FontStretches.cs`
- [ ] `UI/Media/GlyphRun.cs`
- [ ] `UI/Media/GlyphTypeface.cs`
- [ ] `UI/FormattedText.cs`
- [ ] `UI/Documents/Inline.cs`
- [ ] `UI/Documents/Run.cs`
- [ ] `UI/Documents/Span.cs`
- [ ] `UI/Documents/Bold.cs`
- [ ] `UI/Documents/Italic.cs`
- [ ] `UI/Documents/Underline.cs`
- [ ] `UI/Documents/LineBreak.cs`
- [ ] `UI/Documents/Paragraph.cs`
- [ ] `UI/Documents/Block.cs`
- [ ] `UI/Documents/Section.cs`
- [ ] `UI/Documents/List.cs`
- [ ] `UI/Documents/ListItem.cs`
- [ ] `UI/Documents/Table.cs`
- [ ] `UI/Documents/TableRowGroup.cs`
- [ ] `UI/Documents/TableRow.cs`
- [ ] `UI/Documents/TableCell.cs`
- [ ] `UI/Documents/FlowDocument.cs`
- [ ] `UI/Documents/TextPointer.cs`
- [ ] `UI/Documents/TextRange.cs`
- [ ] `UI/Text/TextFormatter.cs`
- [ ] `UI/Text/TextLine.cs`
- [ ] `UI/Text/TextLayout.cs`
- [ ] `UI/Text/CaretElement.cs`
- [ ] `UI/Text/TextSelection.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/TextBlockTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/PasswordBoxTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/TypefaceTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/GlyphRunTests.cs`
- [ ] `tests/Cerneala.Tests/Documents/FlowDocumentTests.cs`
- [ ] `tests/Cerneala.Tests/Documents/TextElementTests.cs`
- [ ] `tests/Cerneala.Tests/Text/TextFormatterTests.cs`
- [ ] `tests/Cerneala.Tests/Text/TextSelectionTests.cs`

## 14. Items, Selection, And Data Display Controls

Items are a big chunk. Do not start this before layout, templates, and basic controls are stable.

- [ ] `UI/Controls/Items/ItemCollection.cs`
- [ ] `UI/Controls/Items/ItemContainerGenerator.cs`
- [ ] `UI/Controls/ItemsPresenter.cs`
- [ ] `UI/Controls/Primitives/Selector.cs`
- [ ] `UI/Controls/SelectionChangedEventArgs.cs`
- [ ] `UI/Controls/ListBox.cs`
- [ ] `UI/Controls/ListBoxItem.cs`
- [ ] `UI/Controls/ListView.cs`
- [ ] `UI/Controls/ListViewItem.cs`
- [ ] `UI/Controls/GridView.cs`
- [ ] `UI/Controls/GridViewColumn.cs`
- [ ] `UI/Controls/GridViewHeaderRowPresenter.cs`
- [ ] `UI/Controls/ComboBox.cs`
- [ ] `UI/Controls/ComboBoxItem.cs`
- [ ] `UI/Controls/TreeView.cs`
- [ ] `UI/Controls/TreeViewItem.cs`
- [ ] `UI/Controls/TabControl.cs`
- [ ] `UI/Controls/TabItem.cs`
- [ ] `UI/Controls/DataGrid.cs`
- [ ] `UI/Controls/DataGridColumn.cs`
- [ ] `UI/Controls/DataGridTextColumn.cs`
- [ ] `UI/Controls/DataGridCheckBoxColumn.cs`
- [ ] `UI/Controls/DataGridTemplateColumn.cs`
- [ ] `UI/Controls/DataGridRow.cs`
- [ ] `UI/Controls/DataGridCell.cs`
- [ ] `UI/Controls/DataGridColumnHeader.cs`
- [ ] `UI/Controls/Menu.cs`
- [ ] `UI/Controls/MenuItem.cs`
- [ ] `UI/Controls/ContextMenu.cs`
- [ ] `UI/Controls/Calendar.cs`
- [ ] `UI/Controls/DatePicker.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ItemCollectionTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ItemContainerGeneratorTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/SelectorTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ListBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ListViewTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/GridViewTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ComboBoxTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TreeViewTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/TabControlTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DataGridTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/MenuTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/CalendarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DatePickerTests.cs`

## 15. Navigation

Navigation can be minimal at first, but the WPF-style type names should be reserved.

- [ ] `UI/Controls/Page.cs`
- [ ] `UI/Controls/Frame.cs`
- [ ] `UI/Navigation/NavigationWindow.cs`
- [ ] `UI/Navigation/NavigationService.cs`
- [ ] `UI/Navigation/NavigationEventArgs.cs`
- [ ] `UI/Navigation/NavigatingCancelEventArgs.cs`
- [ ] `UI/Navigation/NavigationMode.cs`
- [ ] `UI/Documents/Hyperlink.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/PageTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/FrameTests.cs`
- [ ] `tests/Cerneala.Tests/Navigation/NavigationServiceTests.cs`
- [ ] `tests/Cerneala.Tests/Documents/HyperlinkTests.cs`

## 16. Scrolling

Scrolling needs layout, input, and clipping to be solid first.

- [ ] `UI/Controls/ScrollViewer.cs`
- [ ] `UI/Controls/ScrollBar.cs`
- [ ] `UI/Controls/Primitives/ScrollBarVisibility.cs`
- [ ] `UI/Controls/Primitives/IScrollInfo.cs`
- [ ] `UI/Controls/Primitives/ScrollContentPresenter.cs`
- [ ] `UI/Controls/Primitives/Thumb.cs`
- [ ] `UI/Controls/Primitives/Track.cs`
- [ ] `UI/Controls/Primitives/RangeBase.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/ThumbTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/RangeBaseTests.cs`

## 17. Documents, Viewers, Popups, And Adorners

These should wait until visual tree, hit testing, clipping, and focus are reliable.

- [ ] `UI/Documents/DocumentPaginator.cs`
- [ ] `UI/Documents/DocumentPage.cs`
- [ ] `UI/Documents/IDocumentPaginatorSource.cs`
- [ ] `UI/Documents/FixedDocument.cs`
- [ ] `UI/Documents/FixedPage.cs`
- [ ] `UI/Documents/PageContent.cs`
- [ ] `UI/Controls/DocumentViewer.cs`
- [ ] `UI/Controls/FlowDocumentReader.cs`
- [ ] `UI/Controls/FlowDocumentPageViewer.cs`
- [ ] `UI/Controls/FlowDocumentScrollViewer.cs`
- [ ] `UI/Controls/Primitives/Popup.cs`
- [ ] `UI/Documents/Adorner.cs`
- [ ] `UI/Documents/AdornerLayer.cs`
- [ ] `UI/Documents/AdornerDecorator.cs`
- [ ] `UI/Controls/ValidationError.cs`
- [ ] `UI/Controls/Validation.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Documents/DocumentPaginatorTests.cs`
- [ ] `tests/Cerneala.Tests/Documents/FixedDocumentTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/DocumentViewerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/FlowDocumentReaderTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/Primitives/PopupTests.cs`
- [ ] `tests/Cerneala.Tests/Documents/AdornerLayerTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/ValidationTests.cs`

## 18. Data Binding

Do this after dependency properties and resources. Otherwise it becomes painful.

- [ ] `UI/Data/Binding.cs`
- [ ] `UI/Data/BindingBase.cs`
- [ ] `UI/Data/BindingExpression.cs`
- [ ] `UI/Data/BindingMode.cs`
- [ ] `UI/Data/UpdateSourceTrigger.cs`
- [ ] `UI/Data/PropertyPath.cs`
- [ ] `UI/Data/PropertyPathParser.cs`
- [ ] `UI/Data/IValueConverter.cs`
- [ ] `UI/Data/IMultiValueConverter.cs`
- [ ] `UI/Data/MultiBinding.cs`
- [ ] `UI/Data/MultiBindingExpression.cs`
- [ ] `UI/Data/PriorityBinding.cs`
- [ ] `UI/Data/PriorityBindingExpression.cs`
- [ ] `UI/Data/RelativeSource.cs`
- [ ] `UI/Data/RelativeSourceMode.cs`
- [ ] `UI/Data/ObjectDataProvider.cs`
- [ ] `UI/Data/XmlDataProvider.cs`
- [ ] `UI/Data/CollectionView.cs`
- [ ] `UI/Data/ListCollectionView.cs`
- [ ] `UI/Data/CollectionViewSource.cs`
- [ ] `UI/Data/SortDescription.cs`
- [ ] `UI/Data/GroupDescription.cs`
- [ ] `UI/Data/PropertyGroupDescription.cs`
- [ ] `UI/Data/INotifyCollectionChangedAdapter.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Data/BindingTests.cs`
- [ ] `tests/Cerneala.Tests/Data/BindingExpressionTests.cs`
- [ ] `tests/Cerneala.Tests/Data/PropertyPathTests.cs`
- [ ] `tests/Cerneala.Tests/Data/ValueConverterTests.cs`
- [ ] `tests/Cerneala.Tests/Data/MultiBindingTests.cs`
- [ ] `tests/Cerneala.Tests/Data/CollectionViewTests.cs`

## 19. Markup / XAML-Like Layer

This is optional for the engine to work, but important if the final syntax should feel WPF-like.

- [ ] `UI/Markup/MarkupExtension.cs`
- [ ] `UI/Markup/ProvideValueServiceProvider.cs`
- [ ] `UI/Markup/IProvideValueTarget.cs`
- [ ] `UI/Markup/IXamlTypeResolver.cs`
- [ ] `UI/Markup/IAmbientProvider.cs`
- [ ] `UI/Markup/XamlReader.cs`
- [ ] `UI/Markup/XamlWriter.cs`
- [ ] `UI/Markup/XamlObjectWriter.cs`
- [ ] `UI/Markup/XamlObjectWriterSettings.cs`
- [ ] `UI/Markup/XamlSchemaContext.cs`
- [ ] `UI/Markup/XamlType.cs`
- [ ] `UI/Markup/XamlMember.cs`
- [ ] `UI/Markup/NameScope.cs`
- [ ] `UI/Markup/INameScope.cs`
- [ ] `UI/Markup/RuntimeNamePropertyAttribute.cs`
- [ ] `UI/Markup/ContentPropertyAttribute.cs`
- [ ] `UI/Markup/XmlnsDefinitionAttribute.cs`
- [ ] `UI/Markup/XmlnsPrefixAttribute.cs`
- [ ] `UI/Markup/DependsOnAttribute.cs`
- [ ] `UI/Markup/AmbientAttribute.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Markup/MarkupExtensionTests.cs`
- [ ] `tests/Cerneala.Tests/Markup/NameScopeTests.cs`
- [ ] `tests/Cerneala.Tests/Markup/XamlReaderTests.cs`
- [ ] `tests/Cerneala.Tests/Markup/XamlWriterTests.cs`
- [ ] `tests/Cerneala.Tests/Markup/XamlSchemaContextTests.cs`

## 20. Animation

Animation should be late because it touches dependency properties, timing, rendering, and layout invalidation.

- [ ] `UI/Media/Animation/Timeline.cs`
- [ ] `UI/Media/Animation/TimelineCollection.cs`
- [ ] `UI/Media/Animation/AnimationTimeline.cs`
- [ ] `UI/Media/Animation/DoubleAnimation.cs`
- [ ] `UI/Media/Animation/DoubleAnimationUsingKeyFrames.cs`
- [ ] `UI/Media/Animation/DoubleKeyFrame.cs`
- [ ] `UI/Media/Animation/LinearDoubleKeyFrame.cs`
- [ ] `UI/Media/Animation/DiscreteDoubleKeyFrame.cs`
- [ ] `UI/Media/Animation/SplineDoubleKeyFrame.cs`
- [ ] `UI/Media/Animation/ColorAnimation.cs`
- [ ] `UI/Media/Animation/PointAnimation.cs`
- [ ] `UI/Media/Animation/Storyboard.cs`
- [ ] `UI/Media/Animation/BeginStoryboard.cs`
- [ ] `UI/Media/Animation/Clock.cs`
- [ ] `UI/Media/Animation/AnimationClock.cs`
- [ ] `UI/Media/Animation/ClockController.cs`
- [ ] `UI/Media/Animation/RepeatBehavior.cs`
- [ ] `UI/Media/Animation/FillBehavior.cs`
- [ ] `UI/Media/Animation/Duration.cs`
- [ ] `UI/Media/Animation/KeyTime.cs`
- [ ] `UI/Media/Animation/KeySpline.cs`
- [ ] `UI/Media/Animation/EasingFunctionBase.cs`
- [ ] `UI/Media/Animation/IEasingFunction.cs`
- [ ] `UI/Media/Animation/QuadraticEase.cs`
- [ ] `UI/Media/Animation/CubicEase.cs`

Tests:

- [ ] `tests/Cerneala.Tests/UI/Media/Animation/TimelineTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/Animation/DoubleAnimationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/Animation/KeyFrameAnimationTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/Animation/StoryboardTests.cs`
- [ ] `tests/Cerneala.Tests/UI/Media/Animation/ClockTests.cs`

## 21. Automation And Accessibility

This is late-stage, but it should be tracked so it is not forgotten.

- [ ] `UI/Automation/AutomationPeer.cs`
- [ ] `UI/Automation/UIElementAutomationPeer.cs`
- [ ] `UI/Automation/FrameworkElementAutomationPeer.cs`
- [ ] `UI/Automation/ButtonAutomationPeer.cs`
- [ ] `UI/Automation/TextBoxAutomationPeer.cs`
- [ ] `UI/Automation/ItemsControlAutomationPeer.cs`
- [ ] `UI/Automation/SelectorAutomationPeer.cs`
- [ ] `UI/Automation/FrameworkElementAutomationPeerFactory.cs`
- [ ] `UI/Automation/AutomationProperties.cs`
- [ ] `UI/Automation/AutomationEvents.cs`
- [ ] `UI/Automation/AutomationControlType.cs`
- [ ] `UI/Automation/PatternInterface.cs`
- [ ] `UI/Automation/Provider/IInvokeProvider.cs`
- [ ] `UI/Automation/Provider/ISelectionProvider.cs`
- [ ] `UI/Automation/Provider/ISelectionItemProvider.cs`
- [ ] `UI/Automation/Provider/IValueProvider.cs`
- [ ] `UI/Automation/Provider/IRangeValueProvider.cs`
- [ ] `UI/Automation/Provider/IScrollProvider.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Automation/AutomationPeerTests.cs`
- [ ] `tests/Cerneala.Tests/Automation/AutomationPropertiesTests.cs`
- [ ] `tests/Cerneala.Tests/Automation/ButtonAutomationPeerTests.cs`
- [ ] `tests/Cerneala.Tests/Automation/TextBoxAutomationPeerTests.cs`

## 22. Digital Ink

Digital ink is optional, but WPF exposes it as a first-class area and it should be tracked.

- [ ] `UI/Controls/InkCanvas.cs`
- [ ] `UI/Controls/InkPresenter.cs`
- [ ] `UI/Ink/Stroke.cs`
- [ ] `UI/Ink/StrokeCollection.cs`
- [ ] `UI/Ink/StylusPoint.cs`
- [ ] `UI/Ink/StylusPointCollection.cs`
- [ ] `UI/Ink/DrawingAttributes.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Controls/InkCanvasTests.cs`
- [ ] `tests/Cerneala.Tests/Controls/InkPresenterTests.cs`
- [ ] `tests/Cerneala.Tests/Ink/StrokeTests.cs`

## 23. Dialogs And Shell Interop

Keep these isolated because they will be platform-specific.

- [ ] `UI/Dialogs/OpenFileDialog.cs`
- [ ] `UI/Dialogs/SaveFileDialog.cs`
- [ ] `UI/Dialogs/PrintDialog.cs`
- [ ] `UI/Shell/JumpList.cs`
- [ ] `UI/Shell/JumpTask.cs`
- [ ] `UI/Shell/TaskbarItemInfo.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Dialogs/OpenFileDialogTests.cs`
- [ ] `tests/Cerneala.Tests/Dialogs/SaveFileDialogTests.cs`
- [ ] `tests/Cerneala.Tests/Dialogs/PrintDialogTests.cs`

## 24. Diagnostics And Developer Tools

Needed so debugging the UI tree does not become hell.

- [ ] `UI/Diagnostics/VisualTreeDumper.cs`
- [ ] `UI/Diagnostics/LogicalTreeDumper.cs`
- [ ] `UI/Diagnostics/LayoutTrace.cs`
- [ ] `UI/Diagnostics/RoutedEventTrace.cs`
- [ ] `UI/Diagnostics/DependencyPropertyTrace.cs`
- [ ] `UI/Diagnostics/DebugAdorner.cs`

Tests:

- [ ] `tests/Cerneala.Tests/Diagnostics/VisualTreeDumperTests.cs`
- [ ] `tests/Cerneala.Tests/Diagnostics/LayoutTraceTests.cs`
- [ ] `tests/Cerneala.Tests/Diagnostics/RoutedEventTraceTests.cs`

## 25. Playground And Samples

The playground should move from manual drawing to real WPF-style UI scenarios.

- [x] `Playground/Cerneala.Playground/Program.cs`
- [x] `Playground/Cerneala.Playground/Game1.cs`
- [ ] `Playground/Cerneala.Playground/Samples/ControlsFoundationSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/LayoutSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/InputSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/ButtonsSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/TextSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/ItemsSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/StylesSample.cs`
- [ ] `Playground/Cerneala.Playground/Samples/AnimationSample.cs`

## Rules For Updating This File

- Add a planned file here before creating it.
- Mark `[x]` only after the file exists and has tests when tests make sense.
- Mark `[~]` when a file exists but is not yet in the final WPF-inspired shape.
- If a file is intentionally removed from scope, delete it from this roadmap in the same change that explains why.
- Keep WPF names when they make the API familiar, but do not copy WPF behavior blindly.
