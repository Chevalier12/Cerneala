# MainWindow generator gaps

`MainWindow.cui.xml` is intentionally a compiling showcase of the complete supported surface. The snippets below are intentionally not placed in a `.cui.xml` file because each one currently produces a generator diagnostic.

## Unsupported controls and properties

Only `Panel`, `StackPanel`, `Border`, `Button`, `TextBlock`, and accessible custom `UIElement` types with a parameterless constructor can be created. Property metadata is still hardcoded, so common properties are missing:

```xml
<Grid Width="600" Height="400" />
<StackPanel Orientation="Horizontal" />
<Button Command="{Binding SaveCommand}" />
<ShowcaseBadge Text="Custom CLR property assignment is not resolved" />
```

Custom tags can be leaves, but the generator does not inspect their content model. A custom panel or content control cannot receive XML children yet.

## Missing bindings

There is no general property binding syntax. `$DataContext` is currently usable only by `@when` conditions:

```xml
<TextBlock Text="$DataContext.UserName" />
<ProfileCard DataContext="$DataContext.Profile" />
```

Consequently, a nested `UserControl<TChild>` cannot receive a different child ViewModel declaratively.

## UserControl construction limits

- Code-behind constructors are forbidden because the generator owns `MainWindow()` and `MainWindow(TViewModel)`.
- The wrapper has one permanent root; a direct conditional root is rejected.
- The initial `DataContextChanged` raised by the typed constructor happens before markup event handlers are wired. Later changes are observed.
- A conditional `Name` is nullable and cached correctly, but using that changing member itself as an `@when $Name` source is not observable yet.

## Syntax not implemented

```xml
@else { }
@animate $base { }
<Button Click="OnClick(42)" />
<Border CornerRadius="8" />
```

There are no styles, setters, templates, attached properties, XML namespace aliases, command bindings, converters, multi-bindings, or arbitrary markup extensions yet. Raw text is supported only by `TextBlock` and `Button`.
