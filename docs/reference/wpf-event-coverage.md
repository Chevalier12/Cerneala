# WPF event coverage audit

This audit maps the current retained control surface to applicable WPF event concepts. It intentionally does not add decorative events for capabilities the repository does not implement.

## Implemented infrastructure

- `RoutedEvent`: routing strategy, args type, canonical identity, `AddOwner`, owner queries.
- `UIElement`: `AddHandler`, `RemoveHandler`, `RaiseEvent`, `handledEventsToo`, CLR routed-event wrappers.
- Lifecycle/property/layout: `Initialized`, `Loaded`, `Unloaded`, `DataContextChanged`, `IsEnabledChanged`, `IsVisibleChanged`, `FocusableChanged`, focus state changes, `IsMouseDirectlyOverChanged`, and `SizeChanged`.
- Input: mouse, keyboard, focus, text composition, stylus, touch, manipulation, and drag/drop events exposed through WPF-like CLR names over the existing retained input catalog.
- `Control`: `PreviewMouseDoubleClick` and `MouseDoubleClick`.

## Implemented control events

- `ButtonBase`: `Click`.
- `ToggleButton`, `CheckBox`, `RadioButton`: `Checked` and `Unchecked` inherited from the shared primitive, raised on every `IsChecked` change.
- `RangeBase`, `Slider`, `ProgressBar`, `ScrollBar`: `ValueChanged`; `ScrollBar` also exposes `Scroll`.
- `Selector`, `ListBox`, `ComboBox`, `TabControl`: `SelectionChanged`; selectable item containers also raise `Selected` and `Unselected`.
- `ComboBox`: routed `DropDownOpened` and `DropDownClosed`, emitted when its in-root overlay is actually projected or withdrawn.
- `Thumb`: routed `DragStarted`, `DragDelta`, and `DragCompleted` while retaining typed CLR handlers.
- `TextBox`: `TextChanged` and text `SelectionChanged`. `PasswordBox`: `PasswordChanged`. There is no shared `TextBoxBase`; both derive from `Control`.
- `ScrollViewer`: `ScrollChanged`.
- `ToolTip`: `Opened` and `Closed`.
- `Window`: `SourceInitialized`, `Activated`, `Deactivated`, `Closing`, `Closed`, `StateChanged`, `LocationChanged`, `ContentRendered`, and `FrameRendered`.
- `MenuItem`: routed `Click`, `SubmenuOpened`, and `SubmenuClosed`.
- `InkCanvas`: `StrokeCollected` for completed stylus/touch strokes.

## Not applicable to the current surface

- `Image.ImageFailed`: `Image` has no failure event. It resolves `Source` or `SourceResourceId` itself and reports the outcome through the read-only `LoadingState` (`Loading`, `Ready`, `Error`) and `LoadingError` properties.
- `InkCanvas` erasing, gesture, and selection events: there is no erase mode, gesture recognition contract, or stroke selection model in `InkCanvas` yet.
- WPF `TextBoxBase` rich-text/clipboard command events: there is no `RichTextBox`, document formatting surface, or WPF command manager equivalent.
- Navigation, media, calendar, date picker, document, and data-grid events: those control families do not exist in this repository.
- WPF dispatcher/layout-pass `LayoutUpdated`: Cerneala exposes deterministic retained frame/layout diagnostics instead; a per-element WPF-style global layout callback would fight the retained scheduler and create misleading semantics.

## Primary references

- [Routed events overview](https://learn.microsoft.com/dotnet/desktop/wpf/events/routed-events-overview)
- [UIElement events and routed-event APIs](https://learn.microsoft.com/dotnet/api/system.windows.uielement)
- [ButtonBase.Click](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.buttonbase.click)
- [Selector and SelectionChanged](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector)
- [TextBoxBase.TextChanged](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.textboxbase.textchanged)
- [Thumb drag event args](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.dragstartedeventargs)
- [RangeBase.ValueChanged](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.rangebase.valuechanged)
- [ScrollViewer events](https://learn.microsoft.com/dotnet/api/system.windows.controls.scrollviewer)
- [ToolTip routed events](https://learn.microsoft.com/dotnet/api/system.windows.controls.tooltip)
