# Markup Data Bindings

Cerneala `.cui.xml` files support source-generated property bindings. The
generator resolves every source and target with Roslyn and emits typed getters,
setters, and observation factories. Binding paths are not interpreted through
reflection or `StringPropertyPath` at runtime.

## View Model Contract

A `$DataContext` binding requires `DataType` on the root element. Paired
`Window<TViewModel>` and `UserControl<TViewModel>` documents infer that type
from `TViewModel`, so they do not need to repeat `DataType`.

Every CLR object that owns a property along a reactive path must implement
`INotifyPropertyChanged`. This includes intermediate objects, not only the root
view model.

```csharp
using System.ComponentModel;

public sealed class EditorViewModel : INotifyPropertyChanged
{
    private string name = "Initial";
    private string shortName = "Init";
    private string longName = "Initial name";
    private int count;
    private bool useShortName;
    private bool useLongName;
    private bool isDebug;
    private ProfileViewModel profile = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => name;
        set => Set(ref name, value, nameof(Name));
    }

    public string ShortName
    {
        get => shortName;
        set => Set(ref shortName, value, nameof(ShortName));
    }

    public string LongName
    {
        get => longName;
        set => Set(ref longName, value, nameof(LongName));
    }

    public int Count
    {
        get => count;
        set => Set(ref count, value, nameof(Count));
    }

    public bool UseShortName { get => useShortName; set => Set(ref useShortName, value, nameof(UseShortName)); }
    public bool UseLongName { get => useLongName; set => Set(ref useLongName, value, nameof(UseLongName)); }
    public bool IsDebug { get => isDebug; set => Set(ref isDebug, value, nameof(IsDebug)); }
    public ProfileViewModel Profile { get => profile; set => Set(ref profile, value, nameof(Profile)); }

    private void Set<T>(ref T field, T value, string propertyName)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ProfileViewModel : INotifyPropertyChanged
{
    private string displayName = "Initial profile";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName
    {
        get => displayName;
        set
        {
            displayName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }
}
```

An auto-property without a notification event is not treated as magically
observable. The generator reports an actionable diagnostic instead of emitting
a binding that would update only once.

Named elements, `$self`, and template parts are `UiObject` sources. Their
changes are observed through Cerneala's UI property system rather than
`INotifyPropertyChanged`.

## Binding Grammar

An entire binding value has this form:

```text
source-path[:mode]
```

The supported modes are `OneWay` and `TwoWay`. Omitting the suffix selects
`OneWay`.

| Source | Meaning |
| --- | --- |
| `$DataContext.Property` | Reads a typed CLR property path from the inherited data context. |
| `$element.Property` | Reads a UI property from a named element in the same name scope. |
| `$self.Property` | Reads another UI property on the target element. |
| `$control.parts.$part.Property` | Reads a UI property from a named component-template part. `parts` is lowercase. |
| `$owner.Property` | Reads a component-template owner property through the existing one-way template binding. |

XML attribute values still require XML quotes. The quotes delimit the XML
attribute; they are not part of the binding expression.

```xml
<StackPanel DataType="EditorViewModel">
  <TextBlock Text="$DataContext.Name" />
  <TextBlock Text="$DataContext.Name:OneWay" />
  <TextBlock Text="$DataContext.Count" />
  <TextBlock Text="$DataContext.Profile.DisplayName" />
  <TextBox Text="$DataContext.Name:TwoWay" />
</StackPanel>
```

The implicit and explicit `OneWay` forms are equivalent. `TwoWay` requires an
accessible source setter and a writable target UI property.

## String Projection

A `OneWay` binding to a `string` target accepts any source type. Each value is
converted with `Convert.ToString(value, CultureInfo.CurrentCulture)`, and a
terminal `null` becomes `string.Empty`.

```xml
<TextBlock Text="$DataContext.Count" />
```

This conversion is source-to-target only. A `TwoWay` binding from a non-string
source to a string target is rejected because Cerneala does not invent inverse
parsing. Markup bindings do not currently support custom converters.

## Reactive String Interpolation

A string target may combine literal text with one or more paths:

```xml
<TextBlock Text="User: $DataContext.Name, commands: $DataContext.Count" />
<TextBlock Text="Repeated: $DataContext.Name / $DataContext.Name" />
```

Interpolation is always `OneWay`. Every distinct path is observed, repeated
paths share one observation, and a change to any source recomposes the complete
string. Each fragment uses the same current-culture string conversion described
above. Modes are illegal inside interpolation:

```xml
<!-- Invalid: a fragment cannot carry :OneWay or :TwoWay. -->
<TextBlock Text="User: $DataContext.Name:OneWay" />
```

Use `\$` when a dollar sign must remain literal. The backslash is consumed and
the following text is not parsed as a binding, resource, or interpolation.

```xml
<TextBlock Text="Literal: \$DataContext.Name:TwoWay" />
```

A standalone `$` that does not begin a valid path also remains literal.

## Named Elements And Self

Named element bindings may point backward or forward within the same name
scope. Attachment is emitted after the named elements have been constructed.

```xml
<StackPanel>
  <ProgressBar Maximum="100" Value="$Volume.Value" />
  <Slider Name="Volume" Maximum="100" Value="40" />
</StackPanel>
```

`$self` is useful when the target property reads a different property on the
same element:

```xml
<TextBlock IsVisible="True" IsEnabled="$self.IsVisible" />
```

Binding a property directly to itself is rejected, including the equivalent
named form:

```xml
<!-- Both forms are invalid. -->
<TextBlock IsEnabled="$self.IsEnabled" />
<TextBlock Name="Label" Text="$Label.Text" />
```

A name inside a component template belongs to that template name scope. A
binding cannot reach an ordinary named element outside the current scope.

## Template Owners And Parts

Inside `@template`, `$owner.Property` and `$owner.Property:OneWay` use the
existing one-way `TemplateBinding` path. `$owner.Property:TwoWay` is not
supported.

```xml
<Button Content="Save">
  @template
  {
    <ContentPresenter Content="$owner.Content" />
  }
</Button>
```

A binding outside a template can observe a property on one named part of a
named control:

```xml
<StackPanel>
  <Button Name="Host">
    @template { <Border Name="Chrome" IsEnabled="True" /> }
  </Button>
  <TextBlock IsEnabled="$Host.parts.$Chrome.IsEnabled" />
</StackPanel>
```

The path must have exactly the form
`$control.parts.$part.Property`. The terminal property is mandatory, names are
case-sensitive, and only one template level is traversed. The observation
reconnects when the named control receives a new `ComponentTemplate`.

## Conditional Binding Values

The right-hand side of a directive assignment may be a binding. Unlike XML
attributes, an entire binding assignment is written without language-level
quotes and must end with `;`.

```xml
<TextBox DataType="EditorViewModel" Text="Base">
  @when $DataContext.UseShortName
  {
    Text = $DataContext.ShortName;
  }
  @when $DataContext.UseLongName
  {
    Text = $DataContext.LongName:TwoWay;
  }
</TextBox>
```

A quoted ordinary string remains a literal, and a quoted string with literal
text plus a path is interpolation:

```text
Text = "MyText";
Text = "Hello, $DataContext.Name";
```

A quoted value containing only one path is invalid because it looks like a
binding written in the wrong form:

```text
Text = "$DataContext.Name"; // Invalid; remove the quotes.
```

For each target property, only the binding supplied by the winning conditional
rule is active. An inactive provider does not observe its value source or write
back to it. Activation reads the current source immediately; deactivation
removes only the `MarkupConditional` contribution, revealing the current
`MarkupBase` binding or literal beneath it.

## Read-Only Reactive Conditions

Sources in `@when` and `@if` are read-only observations. They do not accept a
binding mode suffix.

```xml
<TextBlock DataType="EditorViewModel" Text="Idle">
  @when ($DataContext.UseShortName and $DataContext.UseLongName)
      or $DataContext.IsDebug
  {
    Text = "Active";
  }
</TextBlock>
```

Generated Boolean evaluation uses normal C# short-circuit behavior. All source
leaves written in the expression remain observed even when evaluation skips a
branch, so a later change can still trigger reevaluation.

## Nulls, Cascade, And Lifecycle

A terminal `null` is a resolved value. It is written to a nullable target, or
projected to an empty string for a string target. If an intermediate path owner
is `null`, the path is temporarily unresolved: the binding clears only its own
markup value and reconnects when the missing segment becomes available.

`OneWay` requires a readable source and a writable target UI property.
`TwoWay` additionally requires an accessible source setter. A read-only target,
or a read-only source endpoint requested with `TwoWay`, produces a generator
diagnostic rather than a partially working runtime binding.

Attribute bindings own the `MarkupBase` slot. Conditional bindings own the
`MarkupConditional` slot only while their rule wins. Local values, conditional
values, and restoration of a lower cascade slot do not cause accidental
TwoWay write-back; only a relevant target change from the `Local` source is
written to the endpoint.

Bindings stop their subscriptions when the target detaches and rebuild them on
reattach. Bindings created inside a component template are registered with the
template instance lifetime and are disposed when that template is replaced.
Disposal and repeated activation/deactivation are idempotent.

## Threading

A markup binding uses the Relay owned by the target's attached `UIRoot`.
Notifications already raised on that UI thread keep the synchronous fast path.
For a CLR `INotifyPropertyChanged` notification raised on a worker thread, the
handler filters the property name but does not evaluate the path or touch the
target. It requests one coalesced Relay refresh for the active binding.

The refresh runs on the UI thread, reads the current state of the complete path,
and reconnects changed intermediate objects. A burst can therefore collapse to
one pending refresh while still displaying the latest coherently published
state. Detach, reattach, conditional-provider replacement, template replacement,
and disposal invalidate callbacks from older activation generations.

This auto-marshaling applies to CLR notifications consumed by an attached
binding; it does not make the source object thread-safe. The view model must
publish state so it can be read coherently later on the UI thread. A direct
mutation of an attached `UiObject` or `UIElement` has already changed UI state
before its notification exists and is rejected off-thread instead of being
marshaled after the fact.

Mutable collections are not made concurrent. An `ObservableList<T>` mutation
observed by an attached control must run on the UI thread, for example with
`await root.Relay.InvokeAsync(() => items.Add(item))`. Programmatic bindings on
generic or unattached targets can use the explicit-Relay overloads on
`BindingOperations`; without an attached or explicit Relay, an off-thread
notification fails with an actionable diagnostic.

## Limits

Markup bindings intentionally do not provide WPF `{Binding ...}` syntax,
runtime reflection paths, custom converters, `FallbackValue`,
`TargetNullValue`, `UpdateSourceTrigger`, `OneTime`, `OneWayToSource`,
multi-binding, or arbitrary C# expressions. Invalid syntax, inaccessible
members, incompatible types, read-only endpoints, and unobservable CLR owners
produce source-generator diagnostics.

## See Also

- [Getting Started](getting-started.md)
- [Aspect System](aspect-system.md)
- [GeneratedMarkup API](../docs-site/documentation/classes/Cerneala.UI.Markup.GeneratedMarkup.md)
- [UiMarkupGenerator API](../docs-site/documentation/classes/Cerneala.SourceGen.UiMarkupGenerator.md)
- [UiRelay API](../docs-site/documentation/classes/Cerneala.UI.Relay.UiRelay.md)
