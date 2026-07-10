# Getting Started

## What Cerneala Is

Cerneala is a retained, code-first UI layer. The Developer Preview path is C# objects, retained invalidation, typed bindings, commands, aspect packages, and a host that you update and draw every frame.

## Retained Update/Draw Contract

Create the UI once, mutate state, then call `UiHost.Update(...)` every frame and `UiHost.Draw(...)` every frame. The first update should do retained work. An unchanged second update should be a no-work frame, and repeated draws should preserve draw-purity by not advancing scheduler work or retained render cache state.

## Create A Root

```csharp
UIRoot root = new(800, 600);
UiHost host = new(new UiHostOptions { Root = root });
```

## Apply Default Aspects

```csharp
// UIRoot registers DefaultAspectPackage.Create() automatically.
root.AspectRegistry.Register(AppAspectPackage.Create());
```

`UIRoot` registers the default aspect package automatically. Register an app package when you want app-level tokens, variants, slots, component templates, or content templates.

## Build UI In Code

```csharp
ObservableValue<string> entryText = new(string.Empty);
ObservableValue<string> statusText = new("Type a value to enable add.");
ObservableList<string> items = new(["First item"]);

TextBox entry = new();
entry.Bindings.Add(BindingOperations.BindTwoWay(entry, TextBoxBase.TextProperty, entryText));

TextBlock status = new();
status.Bindings.Add(BindingOperations.BindOneWay(status, TextBlock.TextProperty, statusText));

ListBox list = new()
{
    ItemsSource = items
};
```

## Build A Generated Window In Markup

Paired `.cui.xml` Window and UserControl files are a supported generated authoring path. The generator resolves controls and UI properties semantically through Roslyn, validates event handlers against the companion code-behind, and emits typed C#.

```xml
<Window Title="Sample" Width="800" Height="600">
    <Button Content="Run">
        @template
        {
            <Border Background="$owner.Background"
                    Padding="$owner.Padding">
                @when $owner.IsMouseOver
                {
                    Background = "#252B36";
                }

                <ContentPresenter Content="$owner.Content" />
            </Border>
        }
    </Button>
</Window>
```

`@template` maps to `ComponentTemplate<TControl>` and therefore works only on types derived from `Control`. A `StackPanel` can be the visual root inside a template, but cannot declare its own template.

## Use ActionCommand And Command State

```csharp
ActionCommand addCommand = new(
    _ =>
    {
        string value = entryText.Value.Trim();
        items.Add(value);
        entryText.Value = string.Empty;
        statusText.Value = $"Added {value}.";
    },
    _ => !string.IsNullOrWhiteSpace(entryText.Value));

entryText.ValueChanged += (_, args) =>
{
    statusText.Value = string.IsNullOrWhiteSpace(args.NewValue)
        ? "Type a value to enable add."
        : $"Ready to add {args.NewValue}.";
    addCommand.RaiseCanExecuteChanged();
};

Button addButton = new()
{
    Content = "Add",
    Command = addCommand
};
```

## Run Update And Draw

```csharp
UiFrame frame = host.Update(inputFrame, new UiViewport(800, 600), elapsed);
host.Draw(drawingBackend);
```

Use targeted tests to protect no-work frames, Tab focus navigation, command state, typed binding, list mutation, and draw-purity.

## What Not To Use Yet

These surfaces remain deferred for Developer Preview: arbitrary XAML compatibility, runtime markup parsing, string-path binding as the core hot path, package split, native accessibility completion, full IME, and advanced rendering claims. Use only the documented `.cui.xml` grammar; unsupported XAML syntax is not interpreted dynamically.
