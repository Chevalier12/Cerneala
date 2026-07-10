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
- `ToggleButton`, `CheckBox`, `RadioButton`: `Checked` and `Unchecked` inherited from the shared primitive.
- `RangeBase`, `Slider`, `ProgressBar`, `ScrollBar`: `ValueChanged`; `ScrollBar` also exposes `Scroll`.
- `Selector`, `ListBox`, `ComboBox`, `TabControl`: `SelectionChanged`; selectable item containers also raise `Selected` and `Unselected`.
- `Thumb`: routed `DragStarted`, `DragDelta`, and `DragCompleted` while retaining typed CLR handlers.
- `TextBoxBase`, `TextBox`, `PasswordBox`: `TextChanged`, text `SelectionChanged`, and `PasswordChanged`.
- `ScrollViewer`: `ScrollChanged`.
- `ToolTip`: `Opened` and `Closed`.
- `InkCanvas`: `StrokeCollected` for completed stylus/touch strokes.

## Not applicable to the current surface

- `ComboBox.DropDownOpened/DropDownClosed`: this `ComboBox` currently has no popup/drop-down state or open/close capability.
- `Image.ImageFailed`: the current `Image` consumes an already resolved `ImageSource`; loading and failure live in resource services, not the control.
- `InkCanvas` erasing, gesture, and selection events: there is no erase mode, gesture recognition contract, or stroke selection model in `InkCanvas` yet.
- `TextBoxBase` rich-text/clipboard command events: there is no `RichTextBox`, document formatting surface, or WPF command manager equivalent.
- Window, navigation, menu, media, calendar, date picker, document, and data-grid events: those control families do not exist in this repository.
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
