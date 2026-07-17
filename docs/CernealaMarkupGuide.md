# Cerneala Markup Guide for Visual UI Work

This guide is written for an implementation agent editing Cerneala UI files,
especially files under `CernealaPresentation/`.

The most important rule is simple:

> Cerneala markup resembles XAML, but it is not WPF, Avalonia, WinUI, HTML, or
> CSS. Use only syntax and types that exist in this repository.

Do not invent familiar XAML features and hope the generator understands them.
The build-time source generator validates the document and will reject unknown
elements, properties, resources, bindings, directives, or motion targets.

## 1. Agent contract

When doing visual UI work:

1. Read the entire target `.cui.xml` file before editing it.
2. Read `App.cui.xml` and one or two visually related sibling views.
3. Preserve existing `Name`, event handler, `Aspect`, `MotionClip`, and binding
   identifiers unless the task explicitly requires changing behavior.
4. Prefer existing global brushes and aspects over local duplicates.
5. Use `Grid`, `StackPanel`, `Border`, text, shapes, and existing controls before
   inventing a custom control.
6. Keep behavior in markup only when Cerneala already supports it. Put arbitrary
   application logic in the companion C# partial class.
7. Build after every meaningful markup change.
8. Inspect a native Cerneala screenshot. A successful build is not visual QA.
9. Never edit generated files under `obj/`, `bin/`, or
   `tests/CodexPresentationHarness/generated/`.

## 2. File model

Cerneala build-time markup files use the `.cui.xml` suffix.

Typical file pairs:

```text
DashboardView.cui.xml
DashboardView.cui.xml.cs
```

The companion class name must match the base file name:

```csharp
using Cerneala.UI.Controls;

namespace MyApp;

public partial class DashboardView : UserControl
{
}
```

Important companion-class constraints:

- It must be a non-nested, non-generic `partial` class.
- It must derive from `UserControl`, `UserControl<TViewModel>`, or `Window` as
  appropriate.
- Do not add a user-declared constructor. The markup generator owns
  construction.
- The XML root and companion base class must agree.

The project must include markup as Roslyn additional files. The presentation
project uses:

```xml
<AdditionalFiles Include="**\*.cui.xml" Exclude="bin\**;obj\**" />
```

## 3. Root documents

### UserControl

Use `UserControl` for reusable views and pages:

```xml
<UserControl>
    <Border
        Background="$PanelBrush"
        Padding="24">
        <TextBlock
            Text="Dashboard"
            FontSize="28"
            Foreground="$PaperBrush" />
    </Border>
</UserControl>
```

### Window

Use `Window` for native top-level windows:

```xml
<Window
    Title="Cerneala"
    Width="1280"
    Height="800"
    MinWidth="960"
    MinHeight="640"
    WindowStartupLocation="CenterScreen"
    Background="$InkBrush">
    <Grid />
</Window>
```

### Application

`App.cui.xml` owns application-wide resources and startup:

```xml
<Application
    StartupWindow="MainWindow"
    ShutdownMode="OnMainWindowClose">
    <Application.Resources>
        <!-- Global resources and default aspects. -->
    </Application.Resources>
</Application>
```

## 4. Basic XML rules

### Names

Use `Name`, not `x:Name`:

```xml
<TextBlock
    Name="StatusText"
    Text="READY" />
```

A named element is available to:

- the generated partial class;
- direct bindings such as `$StatusText.Text`;
- Motion targets such as `$StatusText.Opacity`.

Names are contracts. Do not casually rename elements in an existing view.

### Property attributes

Most properties are assigned as XML attributes:

```xml
<Border
    Width="320"
    Height="160"
    Margin="16,12,16,12"
    Padding="20"
    HorizontalAlignment="Left"
    VerticalAlignment="Top"
    Background="$PanelBrush"
    BorderBrush="$LineBrush"
    BorderThickness="1"
    Opacity="0.9"
    ClipToBounds="True" />
```

Safe value forms commonly used in the repository:

| Type | Examples |
| --- | --- |
| Boolean | `True`, `False` |
| Number | `12`, `0.75`, `-18` |
| Thickness | `12` or `12,8,12,8` |
| Color | `Red`, `#RRGGBB`, `#AARRGGBB` |
| Alignment | `Left`, `Center`, `Right`, `Top`, `Bottom`, `Stretch` |
| Visibility | `Visible`, `Hidden`, `Collapsed` |
| Orientation | `Horizontal`, `Vertical` |
| Grid length | `Auto`, `*`, `1.5*`, `240` |
| Duration | `180ms`, `1.2s`, `4s` |

Prefer one-value or four-value thicknesses. Do not assume CSS-style two-value
or three-value shorthand is supported.

### Property elements

Use property elements for collections or structured values:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="64" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="240" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
</Grid>
```

### Attached properties

Attached layout properties use dotted names:

```xml
<TextBlock
    Grid.Row="1"
    Grid.Column="0"
    Grid.ColumnSpan="2"
    Text="Placed by the parent Grid" />
```

For `Canvas`, verify the exact attached property in `UI/Controls/Canvas.cs`
before using it.

### XML escaping

Normal XML escaping still applies:

```xml
<TextBlock Text="PREVIOUS  &lt;-" />
<TextBlock Text="Line one&#xA;Line two" />
```

## 5. Layout primitives

### Grid

Use `Grid` for page structure, stable tool layouts, and overlays.

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="280" />
    </Grid.ColumnDefinitions>

    <Border
        Grid.Column="0"
        Background="$InkBrush" />

    <Border
        Grid.Column="1"
        Background="$PanelBrush"
        BorderBrush="$LineBrush"
        BorderThickness="1,0,0,0" />
</Grid>
```

Children in the same cell overlap in declaration order. This is useful for
backgrounds, halos, decoration, and HUD overlays.

### StackPanel

Use `StackPanel` for simple linear groups:

```xml
<StackPanel
    Orientation="Vertical">
    <TextBlock Text="TITLE" />
    <TextBlock
        Text="Supporting copy"
        Margin="0,8,0,0" />
</StackPanel>
```

Do not use deeply nested `StackPanel` trees when a `Grid` would provide stable
alignment.

### Border

`Border` is the standard single-child surface and divider primitive:

```xml
<Border
    Background="$PanelBrush"
    BorderBrush="$LineStrongBrush"
    BorderThickness="1"
    Padding="18">
    <TextBlock Text="One child only" />
</Border>
```

Use border sides for dividers:

```xml
<Border
    BorderBrush="$LineBrush"
    BorderThickness="0,0,0,1" />
```

### Canvas

Use `Canvas` only for genuinely absolute compositions. Prefer `Grid` for
responsive application UI.

### Responsive layout

- Use star-sized grid rows and columns for flexible regions.
- Use `Auto` for intrinsic content.
- Use fixed sizes only for stable tools, rails, artwork, or deliberate formats.
- Always respect the containing window's minimum size.
- Use `TextWrapping="Wrap"` for copy that can become narrow.
- Use `ClipToBounds="True"` for animated or decorative scenes.
- Do not rely on viewport-scaled font sizes. Cerneala markup uses explicit
  numeric sizes.

## 6. Common controls

The repository currently contains these useful visual controls:

### Structure and content

- `Border`
- `Canvas`
- `ContentControl`
- `ContentPresenter`
- `Grid`
- `ItemsControl`
- `ItemsPresenter`
- `Panel`
- `ScrollViewer`
- `StackPanel`
- `TabControl`
- `TabItem`
- `UserControl`
- `Window`

### Text and input

- `Label`
- `PasswordBox`
- `TextBlock`
- `TextBox`

### Commands and selection

- `Button`
- `CheckBox`
- `ComboBox`
- `ListBox`
- `ListBoxItem`
- `RadioButton`
- `RepeatButton`
- `ToggleButton`

### Value and progress

- `ProgressBar`
- `ScrollBar`
- `Slider`
- `Thumb`

### Visual media

- `Ellipse`
- `Image`
- `Path`
- `Rectangle`

### Specialized

- `InkCanvas`
- `ToolTip`

This list describes available runtime classes, not a promise that every WPF
property exists. Before using an unfamiliar control or property:

1. Find an existing `.cui.xml` usage.
2. Read its class under `UI/Controls/`.
3. Check its page under `docs-site/documentation/classes/`.
4. Build immediately after the first small usage.

Custom project controls can be used by their CLR class name. For example,
`CernealaPresentation` uses controls such as `BrandMark` and `SvgImage`.

## 7. Text

Common `TextBlock` properties:

```xml
<TextBlock
    Text="A retained interface."
    FontFamily="Bahnschrift SemiBold"
    FontSize="32"
    Foreground="$PaperBrush"
    TextWrapping="Wrap"
    HorizontalAlignment="Left"
    Margin="0,8,0,0" />
```

Use an installed font family name. Existing presentation-safe families include:

- `Segoe UI Variable Text`
- `Bahnschrift`
- `Bahnschrift SemiBold`
- `Cascadia Mono`
- `Cascadia Mono SemiBold`

Use display-sized text only for true page-level titles. Compact controls,
sidebars, telemetry, and cards need smaller type.

## 8. Shapes and visual decoration

Use shapes for simple artwork and indicators:

```xml
<Grid
    Width="80"
    Height="80">
    <Ellipse
        Fill="$OrangeBrush"
        Stroke="$PaperBrush"
        StrokeThickness="1" />
    <Ellipse
        Width="24"
        Height="24"
        Fill="$PaperBrush"
        Opacity="0.3"
        HorizontalAlignment="Left"
        VerticalAlignment="Top"
        Margin="12,12,0,0" />
</Grid>
```

Useful shape properties include `Fill`, `Stroke`, and `StrokeThickness`.

Do not hand-write SVG markup inside a `.cui.xml` file. Use Cerneala's `Path`,
`Image`, an existing custom SVG control, or a raster asset according to the
project's established pattern.

## 9. Resources

### Resource scope

Resources can live at application, window, or user-control scope:

```xml
<UserControl>
    <UserControl.Resources>
        <SolidColorBrush
            Name="LocalAccentBrush"
            Color="#FFFFC44D" />
    </UserControl.Resources>

    <TextBlock
        Text="LOCAL ACCENT"
        Foreground="$LocalAccentBrush" />
</UserControl>
```

Use `$ResourceName` to reference a resource:

```xml
Background="$PanelBrush"
Foreground="$PaperBrush"
Aspect="$GhostButton"
```

Do not use `{StaticResource ...}`, `{DynamicResource ...}`, `x:Key`, or merged
WPF resource dictionaries.

### Supported build-time resource kinds

- `SolidColorBrush`
- `LinearGradientBrush`
- `RadialGradientBrush`
- `ImageBrush`
- `DrawingBrush`
- `Aspect`
- `Tween`
- `Spring`
- `MotionClip`

`VisualBrush` is runtime-only because its source is a live element.

### Solid brushes

```xml
<SolidColorBrush
    Name="AccentBrush"
    Color="#FF4DF0FF"
    Opacity="0.9" />
```

### Gradient brushes

```xml
<LinearGradientBrush
    Name="HeaderBrush"
    StartPoint="0,0"
    EndPoint="1,1"
    Opacity="1">
    <GradientStop
        Offset="0"
        Color="#FF10282D" />
    <GradientStop
        Offset="1"
        Color="#FF2C1323" />
</LinearGradientBrush>
```

```xml
<RadialGradientBrush
    Name="HaloBrush"
    Center="0.5,0.5"
    RadiusX="0.5"
    RadiusY="0.5">
    <GradientStop
        Offset="0"
        Color="#804DF0FF" />
    <GradientStop
        Offset="1"
        Color="#004DF0FF" />
</RadialGradientBrush>
```

Gradient stop offsets must be between `0` and `1`.

### Current CernealaPresentation palette

`CernealaPresentation/App.cui.xml` currently defines:

- Neutral: `InkBrush`, `PanelBrush`, `PanelAltBrush`, `RaisedBrush`
- Lines: `LineBrush`, `LineStrongBrush`
- Text: `PaperBrush`, `SlateBrush`, `SlateDimBrush`
- Cyan: `CyanBrush`, `CyanWashBrush`
- Pink: `PinkBrush`, `PinkWashBrush`
- Lime: `LimeBrush`, `LimeWashBrush`
- Orange: `OrangeBrush`, `OrangeWashBrush`
- Utility: `TransparentBrush`

It also defines reusable aspects such as `PageReveal`, `GhostButton`, and
`LimeButton`.

Reuse these before adding more colors. Add a local semantic brush only when the
view genuinely needs one.

## 10. Events and code-behind

Markup events reference methods by name:

```xml
<Button
    Name="SaveButton"
    Content="SAVE"
    Click="OnSave" />
```

A common routed event handler shape is:

```csharp
using Cerneala.UI.Input;

private void OnSave(UiElementId sender, RoutedEventArgs args)
{
    // Application behavior belongs here.
}
```

Different events can require different event argument types. Copy the signature
from a working handler or inspect the control's event declaration. Do not guess.

Use code-behind for:

- navigation;
- mutations involving multiple application objects;
- service calls;
- asynchronous work;
- complex state transitions;
- screenshot or automation helpers.

Use markup for:

- structure;
- resources;
- static property values;
- bindings;
- visual states;
- supported Motion transitions.

## 11. Typed bindings

Cerneala bindings use `$` paths. They do not use `{Binding ...}`.

### Data context

Declare the root data type:

```xml
<UserControl
    DataType="MyApp.DashboardViewModel">
    <TextBlock
        Text="$DataContext.Title" />
</UserControl>
```

`DataType` is allowed only on the root UI element.

### One-way binding

One-way is the default:

```xml
<TextBlock
    Text="$DataContext.Status" />
```

The explicit form is:

```xml
<TextBlock
    Text="$DataContext.Status:OneWay" />
```

### Two-way binding

Use the final `:TwoWay` suffix:

```xml
<TextBox
    Text="$DataContext.Query:TwoWay" />
```

The source must be writable and observable in a way supported by the generated
binding runtime.

### Element and self bindings

```xml
<TextBlock
    Name="SourceText"
    Text="LIVE" />
<TextBlock
    Text="$SourceText.Text" />
```

Inside aspects and templates, these source forms are common:

- `$self.Property`
- `$owner.Property`
- `$DataContext.Property`
- `$NamedElement.Property`
- `$self.parts.$PART_Name.Property`

### String interpolation

String properties can mix literals and binding paths:

```xml
<TextBlock
    Text="STATUS / $DataContext.Status" />
```

Binding modes are not allowed inside interpolated strings.

To render a literal dollar sign where interpolation is possible, escape it as
`\$`.

### Binding rules

- A direct binding is unquoted in Aspect assignment syntax.
- A binding path cannot end in a dot.
- Binding paths are statically validated against the declared data type and
  known elements.
- Do not write a quoted string containing only a binding path in directive
  assignment syntax. That is intentionally treated as ambiguous.

## 12. Aspects

`Aspect` is Cerneala's styling, state, template, and motion composition system.
Do not create WPF `Style`, trigger, or storyboard markup.

### Named aspect

```xml
<UserControl.Resources>
    <Aspect
        Name="QuietButton"
        Target="Button">
        @default
        {
            Background = $TransparentBrush;
            Foreground = $PaperBrush;
            BorderBrush = $LineStrongBrush;
            BorderThickness = "1";
            Padding = "16,10,16,10";
        }
        @when IsMouseOver
        {
            @if IsMouseOver == true
            {
                Background = $CyanWashBrush;
                Foreground = $CyanBrush;
                BorderBrush = $CyanBrush;
            }
            @if IsMouseOver == false
            {
                Background = $TransparentBrush;
                Foreground = $PaperBrush;
                BorderBrush = $LineStrongBrush;
            }
        }
    </Aspect>
</UserControl.Resources>
```

Apply it with:

```xml
<Button
    Aspect="$QuietButton"
    Content="OPEN" />
```

### Default aspect

An unnamed aspect targets all matching controls in its resource scope:

```xml
<Aspect Target="TextBlock">
    @default
    {
        FontFamily = "Segoe UI Variable Text";
        FontSize = 14;
        Foreground = $PaperBrush;
    }
</Aspect>
```

### Target and TargetType

Existing markup uses:

- `Target="Button"` for a normal control target;
- `TargetType="Fully.Qualified.CustomType"` for a specific CLR type.

Follow the pattern already used in the surrounding file.

### Reactive conditions

Supported condition shapes include boolean sources, comparisons, groups, and
logical expressions:

```text
@when IsMouseOver
@when $self.Visibility
@if value == Visible
@if IsChecked == true
@if ($DataContext.IsReady == true && IsEnabled == true)
```

Use `value` for the current value of the source watched by `@when`.

### Templates

An aspect can own one control template:

```xml
<Aspect
    Name="CompactButton"
    Target="Button">
    @template
    {
    <Border
        Background="$owner.Background"
        BorderBrush="$owner.BorderBrush"
        BorderThickness="$owner.BorderThickness"
        Padding="$owner.Padding">
        <ContentPresenter
            Content="$owner.Content"
            FontFamily="$owner.FontFamily"
            FontSize="$owner.FontSize"
            Foreground="$owner.Foreground"
            HorizontalAlignment="Center"
            VerticalAlignment="Center" />
    </Border>
    }
</Aspect>
```

Inside a template:

- `$owner` is the templated control;
- `PART_` names are template-part contracts;
- `$self.parts.$PART_Name.Property` can target a generated template part from
  the applied aspect.

Do not nest arbitrary extra template roots. A template has one visual root.

## 13. Motion

Motion is typed and generated. It is not a WPF storyboard and not CSS
animation.

For purely visual work, start with `Tween`, `Spring`, `@animate`, `@from`,
`@to`, `@parallel`, and `@sequence`. Use advanced directives only after reading
an existing working example.

### Motion spec resources

```xml
<Tween
    Name="HoverIn"
    Duration="180ms"
    Easing="EaseOut" />

<Tween
    Name="HoverOut"
    Duration="140ms"
    Easing="EaseIn"
    Delay="0ms"
    FillMode="Both" />

<Spring
    Name="Settle"
    Stiffness="520"
    Damping="38"
    Mass="1"
    VelocityMode="Preserve" />
```

Common inline specs:

```text
Tween(180ms, EaseOut)
Spring(520, 38)
Repeat(Tween(4s, Linear), forever)
```

### State animation

```xml
<Aspect
    Name="AnimatedTile"
    Target="Border">
    @when IsMouseOver
    {
        @if IsMouseOver == true
        {
            @animate with $HoverIn
            {
                @from
                {
                    Opacity = current;
                    Scale = current;
                }
                @to
                {
                    Opacity = 1;
                    Scale = 1.03;
                }
            }
        }
        @if IsMouseOver == false
        {
            @animate with $HoverOut
            {
                @from
                {
                    Opacity = current;
                    Scale = current;
                }
                @to
                {
                    Opacity = 0.88;
                    Scale = 1;
                }
            }
        }
    }
</Aspect>
```

Use `current` when an animation should redirect smoothly from the element's
current visual value.

### Composition

```text
@parallel
{
    @animate with Tween(180ms, EaseOut) { ... }
    @animate with Spring(420, 32) { ... }
}
```

```text
@sequence
{
    @animate with Tween(120ms, EaseOut) { ... }
    @animate with Tween(220ms, EaseInOut) { ... }
}
```

### MotionClip

Use a named `MotionClip` for reusable or long-running sequences:

```xml
<MotionClip
    Name="OrbitCycle"
    TargetType="MyApp.OrbitView">
    @animate with Repeat(Tween(8s, Linear), forever)
    {
        @from
        {
            $OrbitalLayer.Rotation = 0;
        }
        @to
        {
            $OrbitalLayer.Rotation = 6.283185;
        }
    }
</MotionClip>
```

Run and cancel clips through an aspect handle:

```text
@handle Playback;

@when $self.Visibility
{
    @if value == Visible
    {
        @run $OrbitCycle as Playback;
    }
    @if value == Collapsed
    {
        @cancel Playback;
    }
}
```

### Common visual motion properties

- `Opacity`
- `TranslateX`
- `TranslateY`
- `Scale`
- `ScaleX`
- `ScaleY`
- `Rotation`

Use `RenderTransformOrigin` when rotation or scaling needs a specific pivot.

### Advanced directives

The parser also supports directives including:

- `@set`
- `@keyframes`
- `@stagger`
- `@run`
- `@cancel`
- `@handle`
- `@parameter`
- `@on`
- `@presence`
- `@layout`
- `@scroll`
- `@drag`
- `@gesture`

These have strict context and grammar rules. Do not improvise their syntax.
Copy a current repository example and preserve its structure.

## 14. What is not WPF-compatible

Do not use these unless the repository later adds explicit support:

- `xmlns` and `x:` conventions copied from WPF;
- `x:Name`, `x:Key`, or `StaticResource`;
- `{Binding ...}` markup extensions;
- WPF `Style`, `Setter`, `Trigger`, or `Storyboard`;
- merged resource dictionaries;
- arbitrary nested property-element syntax;
- converters declared with WPF markup extensions;
- controls or properties remembered from WPF but absent from Cerneala;
- CSS class names, selectors, flexbox, or grid syntax;
- HTML/SVG elements embedded directly into the UI tree.

The safe Cerneala equivalents are:

| Familiar concept | Cerneala mechanism |
| --- | --- |
| `x:Name` | `Name` |
| `StaticResource` | `$ResourceName` |
| `Binding` | `$DataContext.Path` |
| `Style` | `Aspect` |
| Trigger | `@when` and `@if` |
| ControlTemplate | `Aspect` plus `@template` |
| Storyboard | Motion directives |
| Visual state animation | `@animate` |

## 15. Visual composition guidance

For application and developer-tool UI:

- Prefer quiet, structured surfaces over decorative card walls.
- Do not put cards inside cards.
- Use full-width regions and restrained borders for page sections.
- Keep cards for repeated items, dialogs, or genuinely framed tools.
- Use a clear page title, but keep headings compact inside sidebars and panels.
- Use the existing cyan, pink, lime, and orange accents intentionally. Do not
  tint the whole page with one hue.
- Use icons from an existing icon solution when available. Do not draw random
  mini-SVG icons in markup.
- Keep button dimensions stable between states.
- Use fixed dimensions for boards, orbital scenes, meters, and toolbars, but
  wrap them in flexible parent layout.
- Ensure the longest text fits at the minimum window size.
- Avoid decorative gradients, halos, and glowing shapes unless they reinforce
  the domain or current presentation language.
- Preserve a visible next-section cue only on actual landing or hero pages.

## 16. Repository workflow

This repository requires RoslynIndexer for navigation.

From the repository root:

```powershell
.\Tools\scripts\New-FileTree.ps1
```

Read `FileTree.md`, then use:

```powershell
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- read .\path\to\File.cs
```

```powershell
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- search "SymbolOrText" --json
```

After code or project-file changes, refresh the index:

```powershell
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
```

## 17. Build and native visual validation

Build the relevant project:

```powershell
dotnet build .\CernealaPresentation\CernealaPresentation.csproj --no-restore
```

For `CernealaPresentation`, the built-in automation supports:

| Environment variable | Purpose |
| --- | --- |
| `CERNEALA_PRESENTATION_AUTO_CONTINUE=1` | Skip the orientation window |
| `CERNEALA_PRESENTATION_START_CHAPTER=N` | Open one-based chapter `N` |
| `CERNEALA_PRESENTATION_TOUR_CAPTURE=path.png` | Save a native rendered frame |
| `CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION=1` | Capture while motion is active |

Example setup:

```powershell
$env:CERNEALA_PRESENTATION_AUTO_CONTINUE = "1"
$env:CERNEALA_PRESENTATION_START_CHAPTER = "6"
$env:CERNEALA_PRESENTATION_TOUR_CAPTURE = ".\solar-system.png"
$env:CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION = "1"
```

Launch the built presentation executable, wait for the capture, inspect the PNG,
then close the process and remove temporary artifacts.

The capture also writes a `.metrics.txt` file with the selected chapter and
render-cache metrics.

## 18. Validation checklist

Before declaring visual work complete:

- [ ] The target `.cui.xml` is well-formed XML.
- [ ] The relevant project builds with zero errors.
- [ ] Existing element names and handlers still resolve.
- [ ] Every `$Resource` exists in local, ancestor, or application scope.
- [ ] Every custom control exists in the project.
- [ ] No WPF-only syntax was introduced.
- [ ] A native screenshot was inspected.
- [ ] Text does not clip or overlap.
- [ ] The minimum supported viewport remains usable.
- [ ] Hover, checked, disabled, focus, and selected states remain readable.
- [ ] Motion does not resize layout unexpectedly.
- [ ] Infinite motion is canceled when its view leaves the visual stage.
- [ ] Temporary screenshots and processes were cleaned up.

## 19. Minimal complete example

The following combines resources, layout, a named aspect, and Motion without
using WPF-only syntax:

```xml
<UserControl>
    <UserControl.Resources>
        <SolidColorBrush
            Name="SurfaceBrush"
            Color="#FF14161B" />
        <SolidColorBrush
            Name="AccentBrush"
            Color="#FF4DF0FF" />
        <SolidColorBrush
            Name="TextBrush"
            Color="#FFEDEFF3" />
        <SolidColorBrush
            Name="MutedBrush"
            Color="#FF8A93A6" />
        <SolidColorBrush
            Name="LineBrush"
            Color="#FF2A2E38" />
        <SolidColorBrush
            Name="TransparentBrush"
            Color="#00000000" />
        <Tween
            Name="HoverTween"
            Duration="160ms"
            Easing="EaseOut" />
        <Aspect
            Name="PanelButton"
            Target="Button">
            @default
            {
                Background = $SurfaceBrush;
                Foreground = $TextBrush;
                BorderBrush = $LineBrush;
                BorderThickness = "1";
                Padding = "16,10,16,10";
            }
            @when IsMouseOver
            {
                @if IsMouseOver == true
                {
                    @animate with $HoverTween
                    {
                        @from
                        {
                            BorderBrush = current;
                            Scale = current;
                        }
                        @to
                        {
                            BorderBrush = $AccentBrush;
                            Scale = 1.02;
                        }
                    }
                }
                @if IsMouseOver == false
                {
                    @animate with $HoverTween
                    {
                        @from
                        {
                            BorderBrush = current;
                            Scale = current;
                        }
                        @to
                        {
                            BorderBrush = $LineBrush;
                            Scale = 1;
                        }
                    }
                }
            }
        </Aspect>
    </UserControl.Resources>

    <Border
        Background="$SurfaceBrush"
        BorderBrush="$LineBrush"
        BorderThickness="1"
        Padding="24">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <StackPanel
                Grid.Row="0">
                <TextBlock
                    Text="SYSTEM OVERVIEW"
                    FontFamily="Cascadia Mono SemiBold"
                    FontSize="10"
                    Foreground="$AccentBrush" />
                <TextBlock
                    Text="A stable retained surface."
                    FontFamily="Bahnschrift SemiBold"
                    FontSize="32"
                    Foreground="$TextBrush"
                    Margin="0,8,0,0" />
                <TextBlock
                    Text="Layout remains flexible while the visual state animates independently."
                    FontSize="13"
                    Foreground="$MutedBrush"
                    TextWrapping="Wrap"
                    Margin="0,8,0,0" />
            </StackPanel>

            <Border
                Grid.Row="1"
                Background="$TransparentBrush"
                BorderBrush="$LineBrush"
                BorderThickness="1"
                Margin="0,20,0,20" />

            <Button
                Grid.Row="2"
                Aspect="$PanelButton"
                Content="OPEN VIEW"
                HorizontalAlignment="Left" />
        </Grid>
    </Border>
</UserControl>
```

If this guide conflicts with current compiler diagnostics or a working
repository example, the current compiler and repository win. Update the guide
instead of forcing stale syntax.
