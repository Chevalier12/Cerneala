# Plan: declarative bindings in markup and common reactive sources

> Date: 2026-07-14
> Status: completed
> Dependency: `docs/plans/2026-07-13-markup-logical-expressions.md` (implemented)
> Purpose: we add source-generated bindings `OneWay` and `TwoWay` in attributes and conditional assignments `.crn` and formalize sources from `@when` as read-only compound bindings, without reflection paths or a second reactive engine.

## 1. Summary

The markup must be able to bind a UI property to a typed path:
```xml
<TextBlock Text="$DataContext.Name" />
<TextBlock Text="$DataContext.Name:OneWay" />
<TextBlock Text="$DataContext.Type.Name:OneWay" />
<TextBlock Text="$DataContext.Count" />
<TextBox Text="$DataContext.Name:TwoWay" />
```
When the target is `string`, a `OneWay` binding automatically accepts any source and
projects the value by standard to text conversion. For example, a `int`
from `Count` becomes text using the current culture; a `null` becomes the empty string.
A string literal can incorporate one or more reactive paths:
```xml
<TextBlock Text="Salut, $DataContext.Name" />
 <TextBlock Text="Commands: $DataContext.Count, user: $DataContext.Name" />
<TextBlock Text="Literal: \$DataContext.Name" />
```
The interpolation is always `OneWay`: all paths are observed, the values
they are automatically converted to text, and changing any source recomposes
the complete string. `:OneWay` and `:TwoWay` are prohibited inside a
interpolations. The sequence `\$` displays a literal `$` and does not start any binding,
no interpolation.

Paths to template parts preserve the existing grammar and must
necessarily ends in a property:
```xml
<TextBlock
    Text="$MyScrollViewer.parts.$PART_VerticalScrollBar.Name:OneWay" />
```
Named element properties can be direct sources, without traversing a
template part. `OneWay` is the default mode when the suffix is ​​missing:
```xml
<Slider Name="VolumeSlider" Value="40" />
<ProgressBar Value="$VolumeSlider.Value" />
<ProgressBar Value="$VolumeSlider.Value:OneWay" />
<ProgressBar Value="$VolumeSlider.Value:TwoWay" />
```
`OneWay|TwoWay` or `OneWay/TwoWay` are just short notations in the documentation
for the two alternatives. In the markup, exactly one way is written;
`:OneWay/TwoWay` is not a legal value.

Sources in `@when` do not support mods. They are read-only bindings, too
operators and parentheses construct a derived Boolean binding:
```xml
@when ($DataContext.IsEnabled and $DataContext.User.IsAdmin)
    or $DataContext.IsDebug
{
    Background = "Green";
}
```
All leaves of the expression remain observed. The evaluation respects the short-circuit,
but subscriptions are not created and destroyed according to the active branch.

Assignments from a conditional branch can also select a binding, no
just a static value. Integral bindings are unquoted expressions;
quoted strings without paths remain literal, and those with literal text plus paths
become interpolations:
```xml
@when $DataContext.UseShortName
{
    Text = $DataContext.ShortName;
}

@when $DataContext.UseLongName
{
    Text = $DataContext.LongName:TwoWay;
}
```
`Text = "MyText";` is a legal literal. An expression that looks like binding, but
is put in quotation marks, for example
`Text = "$DataContext.ShortName:OneWay";`, is illegal and receives a diagnosis
instead of being treated silently as a text.

A quoted string that also has literal content is legal interpolation:
```text
Text = "Salut, $DataContext.ShortName";
```
XML quotes remain mandatory for attribute values and are only
XML delimiters. The unquoted rule applies to expressions on the right side of a
the assignments in the directives.

## 2. Established decisions

- All binding paths use `OneWay` by default when the suffix is missing.
`:OneWay` remains the equivalent explicit form, and `:TwoWay` must be requested
  explicitly.
- Condition expressions from `@when` and `@if` do not accept `:OneWay`, `:TwoWay` or
  another way; their sources are always read-only/one-way. This rule does not
  prohibits the mode from the value of an assignment found in the body of the branch.
- `$owner.Property` remains the implicit one-way binding existing inside
  to a `@template`; `$owner.Property:OneWay` is the legal explicit alias again
  `:TwoWay` remains outside this plan and receives a diagnosis.
- `$control.parts.$part.Property` remains the canonical form for template parts:
  `parts` is lowercase, names are case-sensitive, there is only one level
  of templates and the terminal property is mandatory.
- `$element.Property[:Mode]` directly binds a property of a named element
  visible in scope; the element can be declared before or after the target, and
  the generator defers attachment until after the named elements are built.
- `$self.Property[:Mode]` is allowed only when the source property is
  different from the target property. A direct self-binding, such as
  `IsEnabled="$self.IsEnabled"`, is rejected with dedicated diagnosis.
- The assignments in `@when` / `@if` accept bindings only as expressions
  unquoted. An ordinary quoted string remains literal, and a quoted string that
  has the form of a binding path is rejected with diagnostic. The mode belongs
  the value of the assignment, not the Boolean expression that controls the branch.
- A conditional binding is active only as long as its assignment is
  winner for the target property. When deactivated, it stops, and at o
  subsequent activation immediately rereads the current source.
- `and`, `or` and brackets do not produce dynamic subscriptions. All sources
  syntax are subscribed, and the generated predicate keeps the C# short-circuit.
- The source and target types must be compatible at compile time, with o
  the only built-in conversion: for a binding `OneWay` to `string`,
  any source value is transformed with semantics
  `Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty`.
- The automatic conversion to `string` is only source-to-target. A binding
  `TwoWay` to `string` requests source `string`; we are not inventing reverse parsing for
  numbers, enums, data or arbitrary objects.
- A string value with literal text and one or more built-in paths is a
  derivative interpolated binding, always `OneWay`. All paths are observed,
  each value uses the same conversion to text, and a segment
  unresolved or terminal `null` contributes `string.Empty` until refresh.
- `:OneWay` and `:TwoWay` modes are illegal inside the interpolation.
  The mode can only be set on a value that is integrally one way of
binding.
- `\$` is the canonical escape for a literal `$` in attributes and in
  quoted strings from directives. The scanner consumes the backslash, emits `$`
  and does not try to resolve the following sequence as the resource, binding or
  interpolation. The other backslash sequences keep their contract
  existing.
- A `$DataContext` path requires `DataType` on the root, according to the contract
  existing reagent.
- An incomplete path due to an intermediate segment `null` is
  temporarily unavailable; the binding removes its markup value and se
  reconnect when the segment becomes available. A `null` returned by
  the terminal property remains a valid value for a nullable target.
- The attribute binding occupies `UiPropertyValueSource.MarkupBase`, so that
  `MarkupConditional` to be able to overwrite it and then restore it. The binding from a
  assignment winner occupies `MarkupConditional` only as long as its branch is
  active.
- For `TwoWay`, only a relevant target change from source `Local` is
  pushed back. Conditional values, animations and waterfall restores
  it must not dirty the ViewModel.
- Bindings and interpolations must react to source changes;
  we do not accept the silent degradation at a simple initial reading. UI sources se
  observe through `UiObject.PropertyChanged`, and each CLR owner from a path
  `$DataContext` must implement `INotifyPropertyChanged`; otherwise
  the generator issues an actionable diagnosis.
- The initial strict threading contract was extended by
  `docs/plans/2026-07-14-relay-auto-marshaling.md`: a `INotifyPropertyChanged`
  The attached CLR can notify off-thread, and the controller also coalesce-uies
  reevaluates the full path on `UIRoot.Relay`, without readings on the worker.
  `UiObject.PropertyChanged` and direct UI mutations remain strictly UI-thread.

## 3. Baseline and the current problem

- `BindingOperations` and `UiPropertyBinding<T>` connect only one today
  `ObservableValue<T>` by a `UiProperty<T>`; the markup generator does not
  uses.
- `StringPropertyPath` is intentionally disabled. New paths must be validated
  by Roslyn and issued as typed accessories, not evaluated by reflection.
- `UiMarkupGenerator.EmitProperty` treats `$owner.Property` as straight
  `TemplateBinding` only in `@template`; the other values `$Name` from the attributes
  are references to resources.
- `UiMarkupReactiveEmitter` can already observe UI properties,
  `$DataContext.Path`, `$owner.Property`, `$self.Property` and
  `$control.parts.$part.Property`.
- `GeneratedMarkup`, `MarkupObservation` and `MarkupDataPathSegment` already contain
  reconnection for `DataContext`, `INotifyPropertyChanged`, `UiObject` and
  replacing the template.
- `ReactivePlan` deduplicates observations and keeps all dependencies of one
  expressions with `and`/`or`, including the short-circuited branches.
- `UIElement.Bindings` is emptied at detachment, while the generated conditions
  I use `IElementLifecycleBehavior` for stop/restart. The new binding of
  markup must have the same attach/detach contract as the markup
  reactive, not to definitively lose the subscriptions at the first detachment.

The architectural problem is that reactive sources are only solved for
directives, and the runtime bindings only support `ObservableValue<T>`. If
we add another parser and another subscription network directly in
`EmitProperty`, we get two engines fighting like hell on the same ones
properties.

## 4. Objectives

- Fully source-generated attribute bindings for simple and nested paths
  from `$DataContext`.
- Attribute bindings to UI properties of elements named and to
  another property of `$self`.
- Attribute bindings to part template properties using grammar
  existence.
- `OneWay` and `TwoWay` bindings as assignment values
  conditional, expressed unquoted and activated per winning branch.
- Automatic source-to-string conversion for `OneWay` bindings that have a
  target property `string`, including conditional assignments.
- Reactive string interpolations with one or more paths, automatic conversion a
  of each value and recomposition when changing any source.
- `OneWay` and `TwoWay` modes, with immediate initialization, reentrancy guard,
  reconnection and deterministic cleanup.
- A single semantic solution for the sources used by attributes and by
  `@when`.
- Keeping the waterfall `MarkupBase` / `MarkupConditional` and the contract of
  template lifecycle.
- Accurate source-generator diagnostics for syntax, types, accessibility
  and invalid modes.

## 5. Non-objectives

- WPF syntax `{Binding ...}`.
- `StringPropertyPath` activation or evaluation by reflection.
- Conversion, `FallbackValue`, `TargetNullValue`, declarative validation or
  `UpdateSourceTrigger`.
- `OneTime`, `OneWayToSource`, multi-binding or arbitrary C# expressions.
- Auto-binding of a property to itself through `$self`; the generator
  reject instead of building a meaningless loop.
- Binding to the raw object `$control.parts.$part`; the path must end
  in a property.
- Binding to the raw object `$element`; the named direct source must be
  end in a UI property.
- Recursive navigation through several templates, for example
  `ScrollViewer -> ScrollBar -> Track -> Thumb` in one way.
- Changing the public behavior of `BindingOperations` for
  ZZZ BLACK30ZZZ.
- Extension of `$owner.Property` to `TwoWay` in this plan; form without mode and
  `:OneWay` remain `TemplateBinding` one-way.
- Binding modes inside interpolations, for example
  `Text="Salut, $DataContext.Name:OneWay"`; interpolation is already a binding
  derivative `OneWay`, so fragment modes are rejected.

## 6. The proposed architecture

### 6.1 Unique semantic solution in source generator

A minimal internal descriptor of one is extracted from `UiMarkupReactiveEmitter`
typed sources, reused by directives and attribute bindings. The descriptor
must contain:

- type of terminal value;
- the code for building a `MarkupObservation`;
- the availability of a terminal setter for `TwoWay`;
- the necessary source-to-target projection, limited in this plan to its identity
  built-in conversion to `string`;
- the location and canonical form of the expression for diagnostics;
- scope information for `$DataContext`, named elements, `$owner`,
  `$self` and template parts;
- the identity of the source and target UI property, for self-binding detection
  directly before the release of C#.

Named elements use the same name scope as reactive directives. oh
the forward reference is valid in the same scope, but a reference that tries to
exits from a template name scope receives diagnosis. The generator builds
first the named elements and attach the dependent bindings after all
scope references are available.

The AST of the generator is not exposed as a public API and the parsing is not moved
`.crn` in runtime.

### 6.2 Endpoint reusable runtime

`MarkupObservation` remains the common reading and notification endpoint. He will
internally distinguish between:

- resolved value, including terminal value `null`;
- temporarily unresolved path due to a missing owner or intermediate segment;
- writable or read-only endpoint.

`MarkupDataPathSegment` receives a typed setter issued by the generator only
for the writable terminal segment. Observations by `UiProperty` and templates
part can be written via `SetValue` when the property is not read-only. The same
observation of `UiProperty` covers `$element.Property` and
`$self.OtherProperty`; a separate runtime endpoint is not introduced just for
as the source has a different spelling in the markup.

`DataPathObservation` continues to subscribe to `UiObject.PropertyChanged` and
`INotifyPropertyChanged` for every owner in the way and rebuilds all
descending segments when one changes. The semantic resolver rejects a
owner CLR without notification contract, so a binding declared reactive
it can't work only at initialization without saying anything.

### 6.3 Controller for the binding of a property

`GeneratedMarkup` receives a generic public factory for attaching a
binding between a `MarkupObservation` and a `UiProperty<T>`. Implementation
internal, estimated as `MarkupPropertyBindingController<T>`, will:

- implement the contract `Binding`/`IDisposable` and
  `IElementLifecycleBehavior`;
- start the observation and write the initial value in the configured slot;
- update the target at each change `OneWay`;
- for `TwoWay`, observe the target, write through the terminal setter and block
  recursive loops;
- ignore target-to-source propagation for changes from other value sources;
- stop subscriptions at detach and rebuild them at reattach;
- removes only the contribution of the slot it owns for final disposal;
- it could be registered through `TemplateEmissionContext.RegisterLifetime` when
  the binding is created in a template.

The controller internally accepts the target slot (`MarkupBase` or `MarkupConditional`)
and an activation contract. For attribute binding it is active throughout
the lifetime of the element. For a binding found in the assignment of a
branches, `MarkupConditionController` activates it only as long as the assignment
win the target property.

The controller applies the source-to-target projection before writing the slot.
For the `string` target, the generator outputs the standard conversion with
`CultureInfo.CurrentCulture`; `null` produces `string.Empty`. Projection is not
a configurable `IValueConverter` and has no reverse path. That's why one
`TwoWay` with non-string source and target `string` is rejected at compilation.

`UiPropertyBinding<T>` is not forced to accept reflection paths. This one
the direct binding remains for `ObservableValue<T>`, and the controller de
markup reuses `BindingMode`, the writable target rules and the convention of
disposal.

### 6.4 Derived Boolean Binding for `@when`

`@when` continues to use `ReactivePlan`, `MarkupObservation` and
`MarkupConditionRule`; we don't create a useless `UiPropertyBinding<T>` for everyone
the leaf The documented contract becomes:
```text
source bindings -> expresie tipizata derivata -> bool -> regula conditionala
```
Identical leaves are deduplicated. The parentheses define the AST, `and` and `or`
are emitted as `&&` and `||`, and all observations remain active regardless of
short circuit.

### 6.5 Binding values in conditional assignments

`MarkupConditionController` remains the sole arbiter of the slot
`MarkupConditional`; the binding controllers do not write concurrently in the same
slot. The conditional assignment model will distinguish between:

- static value already supported;
- reactive value factory that can be activated and deactivated.

When a rule becomes a winner for a property, the controller:

1. disable the previous provider for that property;
2. activate the binding of the new branch and immediately read the current source;
3. publish updates only as long as the activation token is current;
4. for `TwoWay`, write back only the changes `Local` produced while
   the assignment is active;
5. when the branch is lost, stop the observation and eliminate the contribution
   `MarkupConditional`, allowing the binding or value `MarkupBase` to reappear.

If the source of the conditional binding becomes temporarily unresolved, the contribution
`MarkupConditional` is removed until reconnection, and `MarkupBase` becomes
again visible without changing the winning Boolean branch.

Observations of the Boolean expression in `@when` remain permanently active according to
the existing contract. Only the observation of the binding value from the assignment can
be stopped as long as the branch does not win; when reactivated, it refreshes completely, so
cannot display an old value.

Legal forms are unquoted expressions; lack of mode means `OneWay`:
```text
Text = $DataContext.ShortName;
Text = $DataContext.ShortName:OneWay;
Text = $DataContext.ShortName:TwoWay;
```
The unquoted form is tokenized only in the grammar of assignments and must
to consume the entire value until `;`. We are not relaxing the XML rules for
attributes. `Text = "MyText";` remains literally legal again
`Text = "Salut, $DataContext.ShortName";` is interpolation. A quoted string
consisting exclusively of a path, such as `Text = "$DataContext.ShortName";`, or
which puts a mode in a fragment, such as
`Text = "$DataContext.ShortName:OneWay";`, get diagnosis.

### 6.6 Grammar of attribute and assignment bindings

General form, with optional mode:
```text
<source-path>[:<mode>]
```
Sources accepted in this plan:
```text
$DataContext.Property
$DataContext.Property:OneWay
$DataContext.Parent.Child.Property:TwoWay
$owner.Property
$owner.Property:OneWay
$element.Property
$element.Property:OneWay
$element.Property:TwoWay
$self.OtherProperty
$self.OtherProperty:OneWay
$self.OtherProperty:TwoWay
$control.parts.$part.Property
$control.parts.$part.Property:OneWay
$control.parts.$part.Property:TwoWay
```
`$element.Property` requires an accessible terminal UI property. `$self` se
resolve to the element on which the target property is located and it is legal only
if the terminal property differs from the target. The comparison is done semantically after
the symbol/identity `UiProperty`, not by text, so that a property
the inheritance cannot bypass the diagnosis through another qualification.

The parser separates the suffix only if the last segment after `:` is exact
`OneWay` or `TwoWay`; if the suffix is missing, the descriptor uses
`BindingMode.OneWay`. `$Accent` continues to be the resource, and a value that
starts with `$` but does not follow a binding form, remains on the existing route of
resolution of resources or receives the existing diagnosis. In assignments,
the parser reads the complete unquoted expression and explicitly rejects a path of
binding written as quoted string. For the special root `$owner`, module
implicitly and `:OneWay` are legal, and `:TwoWay` is explicitly diagnosed.

After solving the path, the type check accepts the direct assignment or, only
for `OneWay` with target `string`, the projection incorporated in the text. Others
incompatibilities remain diagnostic; in particular, `string` is not used as
magic bridge for `TwoWay`.

### 6.7 Reactive string interpolations

For a target property `string`, a literal text value and at least one
built-in path produces an interpolation descriptor. The scanner recognizes
the same roots and the same name scopes as the integral bindings, consume
the longest typed path is valid and stops the path before the characters
literals such as space, comma or punctuation mark.
```text
"Salut, $DataContext.Name"
"Commands: $DataContext.Count, user: $DataContext.Name"
"Valoare: $VolumeSlider.Value"
"Literal: \$DataContext.Name"
```
Rules:

- an interpolated path must end in a property;
- `$Accent` without the terminal property does not become interpolation and keeps it
  existing resource/literal contract;
- a `$` that does not start a valid path remains a literal character;
- the exact pair `\$` is consumed before recognizing horses, produces
  a single `$` literal and does not create any observations; the rule applies identically
  attributes and quoted strings from directives, without changing the others
  existing escapes;
- `:OneWay`, `:TwoWay` and any other mod suffix in a fragment are
  diagnostics;
- identical fragments are semantically deduplicated, but appear in all positions
  from the final result;
- all active interpolation observations remain subscribed and any changes
  recompose the entire string;
- a `null` or temporarily unresolved fragment produces `string.Empty` until
  the source becomes available again;
- an attribute interpolation writes in `MarkupBase`; one of an assignment
  conditional uses the same activate/deactivate contract as the binding
  conditional and write in `MarkupConditional` only as long as the branch wins.

In assignments, a quoted string consisting exclusively of a single path remains
author error: should be written as binding unquoted. The presence of a fragment
literal real transforms the quoted string into a legal interpolation. In
XML attributes, quotes are mandatory delimiters, so the exact value
`$DataContext.Name` remains integral binding, not interpolation.

## 7. Estimated files

Modified files:

- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`
- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`, for binding expressions
  unquoted and the diagnosis of bindings written as string quoted
- `UI/Markup/GeneratedMarkupConditions.cs`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`
- `docs/getting-started.md` or the current conceptual page for `.crn`
- `docs-site/documentation/classes/Cerneala.UI.Markup.GeneratedMarkup.md`
- `docs-site/documentation/classes/Cerneala.UI.Markup.MarkupObservation.md`
- `docs-site/documentation/classes/Cerneala.UI.Markup.MarkupDataPathSegment.md`

New files possible, if the separation clearly reduces the complexity of the files
existing:

- `Cerneala.SourceGen/UiMarkupBindingEmitter.cs`
- `UI/Markup/GeneratedMarkupBindings.cs`
- `tests/Cerneala.Tests/UI/Markup/GeneratedMarkupBindingTests.cs`
Don't add files just to walk three methods out of a corner
another; separation is justified only if it delimits the parser/emitter from
binding and runtime controller.

## 8. Implementation stages

### Stage 0 - Baseline and RED tests

- [x] Add RED source-generator tests for
  `Text="$DataContext.Name:OneWay"` and check the initial plus value
  update after `INotifyPropertyChanged`.
- [x] Add a RED test for a nested path
  `$DataContext.Type.Name:OneWay`, including the replacement of the object `Type` and
  unsubscribing from the old terminal segment.
- [x] Add RED tests for `Text="$DataContext.Count"`: numeric value
  initial and updates become text with `CurrentCulture`, and a source
  nullable `null` produces `string.Empty`.
- [x] Add RED tests for interpolation
  `Text="Salut, $DataContext.Name"`: the initial value is composed, the change
  of `Name` updates the text and an explicit way is not necessary.
- [x] Add RED tests for `Text="Literal: \$DataContext.Name"` and for
  the quoted equivalent from a conditional assignment: the result contains
  `$DataContext.Name` literally, the backslash is consumed and there is no subscription
  reactive for the escaped sequence.
- [x] Add a RED test with more fragments, including a non-string `Count`,
  a repeated fragment, a terminal `null` and a nested path that replaces itself
  an intermediate segment.
- [x] Add RED tests for interpolations with `$element.Property`,
  `$self.OtherProperty`, `$owner.Property` in templates and
  `$control.parts.$part.Property`, respecting the same name scopes.
- [x] Add a RED test that confirms that an owner `$DataContext` without
  `INotifyPropertyChanged` receives diagnosis instead of a binding that would
  update only the initial value.
- [x] Add a RED test that picks up `INotifyPropertyChanged` from a worker
  thread and confirm that the binding gives fail-fast before reading its source
  write the target property; the message identifies the binding, the captured UI thread
  and the issuing thread.
- [x] Add a RED test for inheriting and replacing `DataContext` on a
  descendant element.
- [x] Add a test RED `TextBox.Text` with
  `$DataContext.Name:TwoWay`: the source initializes the target, the input modifies
  the source, and a subsequent change of the source returns to the target without a loop.
- [x] Add a RED test for
  `$Host.parts.$Chrome.IsEnabled:OneWay` and reconnection after replacement
  `ComponentTemplate`.
- [x] Add RED tests for binding directly to named element,
  `Value="$VolumeSlider.Value"`, `:OneWay` and `:TwoWay`, with the declared source
  both before and after the target in the same name scope.
- [x] Add a RED test for `$self.IsVisible:OneWay` used on another
compatible target property and a diagnostic test for
  `IsEnabled="$self.IsEnabled"`.
- [x] Add RED tests for a conditional binding
  `Text = $DataContext.ShortName;` and the explicit form `:OneWay`; confirm that
  are equivalent, then check activation, updates and restore
  base values.
- [x] Add a RED test that confirms that `Text = "MyText";` remains literal
  legally and that `Text = "$DataContext.ShortName:OneWay";` receives a diagnosis.
- [x] Add a RED test for conditional binding `TwoWay`: the write-back
  it only works as long as the assignment wins the property and stops
  after changing the branch.
- [x] Add GREEN characterization tests for `$owner.Content` one-way and
  its equivalent to `$owner.Content:OneWay`, plus the resources `$Accent` and
  the existing `@when` expressions, so that the new parser does not confuse them with bindings
  of attribute.
- [x] Add a characterization test for `(A and B) or C` that confirms that
  all three sources remain observed, including the short-circuited branch.
- [x] Run the RED tests and record the exact diagnoses or behavior
  missing before implementation.

  RED evidence from 2026-07-14: filter `MarkupBindingStageZero` has 14 cases
  failed exclusively on the planned capabilities and a case of compatibility
  GREEN. Attribute bindings /named/`$self`/template-part/conditional emit
  currently `CERNEALAUI004`, alias `$owner.Content:OneWay` emits
  `CERNEALAUI007`, the interpolations remain literal text, and the paths quoted from
  assignment do not issue the required diagnosis yet. The separate baseline is
  `123/123` GREEN, without skipping.

**Gate Stage 0**

- [x] The new tests fail exclusively because the attribute bindings,
  named/`$self` and conditional assignment are not implemented; the baseline
  existing remains GREEN.
- [x] The existing semantics for `@when`, template binding and resources is
  covered before refactoring.

### Stage 1 - Contract runtime for observable endpoints

- [x] Extends `MarkupObservation` with internal solution state and with a
  internal terminal write contract, without exposing arbitrary setters as
  General API for users.
- [x] Extends `MarkupDataPathSegment` with a public overload for the setter
  terminal issued by the generator; keep the existing constructor compatible.
- [x] Modify `DataPathObservation` to keep the current terminal owner, to
  differentiate terminal `null` from incomplete path and reconnect the getter and
  the setter after changing any segment.
- [x] Allows `UiPropertyObservation` and `TemplatePartPropertyObservation` to
  report writable only for properties that are not read-only.
- [x] Add focused runtime tests for simple path, nested path,
intermediate `null`, nullable terminal, replacement by `DataContext` and templates
  swap.
- [x] Add runtime tests for the endpoint of a direct UI property,
  including writable setter, read-only property and changing the value on one
  element named or on `$self`.
- [x] Add runtime tests that confirm that the change of any owner
  `INotifyPropertyChanged` from a path reconstructs the descending segments and
  that the old owners are unsubscribed.
- [x] After each modification C# reindexes with
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.

  Proof stage 1 of 2026-07-14: `GeneratedMarkupObservationTests` is
  `4/4` GREEN, including explicit subscription counters for owners
  old, and the existing source-generator regression is `123/123` GREEN. The index
  Roslyn reports zero warnings. The new public overload and internal semantics
  solutions are synchronized in the existing API pages.

**Gate stage 1**

- [x] Endpoints read and reconnect identically to the current observations,
  and the setter exists only when the terminal source is writable.
- [x] No existing tests for `GeneratedMarkup` or reactive directives
  regresses.

### Stage 2 - The generated binding controller

- [x] Add the generic factory in `GeneratedMarkup` and the internal controller that
  link a `MarkupObservation` to a `UiProperty<T>` to a `BindingMode`.
- [x] Write source-to-target values using `MarkupBase` for the binding
  of attribute and `MarkupConditional` for the active assignment, never
  `Local`, and remove only the contribution held upon disposal/deactivation.
- [x] Implements `OneWay` with immediate initialization, subsequent changes,
  reentrancy guard and temporarily unavailable path.
- [x] Allows the controller a typed source-to-target projection and applies
  the projection incorporated in `string` before writing in `MarkupBase` or
  `MarkupConditional`, without introducing a configurable public converter.
- [x] Add an interpolation controller/composer that owns the list
  deduplicated by `MarkupObservation`, recomposes the complete string upon change
  any source and respects the lifecycle of the simple binding.
- [x] For conditional interpolation, enable and disable all
  the observations together with the winning assignment and refresh completely at
  reactivation.
- [x] Add runtime tests for conversion to `string` of numbers, enums,
  of an object with `ToString()` overwritten and of `null`, including a test with
  culture set and restored deterministically.
- [x] Implements `TwoWay` only for endpoint writable; propagate back
  target changes from `UiPropertyValueSource.Local` and ignore
`MarkupConditional`, animation, appearance and own updates.
- [x] After a successful write `TwoWay`, normalizes the effective value back to
  the binding slot so that a transient `Local` does not block
  future updates of the source.
- [x] If a path `TwoWay` is temporarily unresolved, ignore the write-back without
  exception and restores the target when the source becomes available again.
- [x] If the source of an active conditional binding becomes temporarily unresolved,
  remove the contribution `MarkupConditional`, expose `MarkupBase` and reapply
  conditional value only after reconnection.
- [x] Register the controller as `IElementLifecycleBehavior`: stop la
  detach, refresh to reattach and final disposal for the lifetime of one
  template replaced.
- [x] Add idempotent activation/deactivation of the controller, with refresh
  immediately at each reactivation and ignoring delayed callbacks from
  an old activation.
- [x] Captures the `Update`/UI thread when the controller is activated. Implementation
  the initial fail-fast was later replaced with coalescing on `UIRoot.Relay`
  for `INotifyPropertyChanged`; the worker callback does not evaluate the path and no
  move the target.
- [x] Extends the conditional assignment with a reactive supplier, keeping
  `MarkupConditionController` as the sole owner of the slot
  `MarkupConditional` and activating at most one provider per target property.
- [x] Add runtime tests for attach/detach/reattach, idempotent disposal,
  template swap, reentrancy and lack of updates after disposal.
- [x] Add a cascade test where `@when` overrides the binding
  `MarkupBase`, and disabling the rule restores the current value of the source.
- [x] Add cascade tests with two branches that offer different bindings
  the same properties; only the binding of the winning branch can update
  target or source `TwoWay`.
- [x] Re-index the solution after each C# or project-file modification.

  Stage 2 proof from 2026-07-14: `GeneratedMarkupBindingTests` is `11/11`
  GREEN; the matrix combined with the existing observations and bindings is
  `34/34` GREEN; the existing source-generator regression is `123/123` GREEN.
  Slots, crop conversion, deduplicated interpolation,
  TwoWay and `Local` normalization, the conditional fallback, the lifecycle,
  template the swap and fail-fast off-thread before the getter. The Roslyn Index
  it has zero warnings, and the new public factories are documented in the API pages
  existing.

**Gate stage 2**

- [x] The controller passes the one-way, two-way, conditional activation tests,
  interpolation, cascade and lifecycle without subscription leaks or loops
  recursive.
- [x] `BindingOperations` and `UiPropertyBinding<T>` keep their behavior
  existing public.
### Stage 3 - Parser and common semantic resolution

- [x] Add a minimal parser for the optional trailing suffix `:OneWay` /
  `:TwoWay`; use `OneWay` when missing and keep the exact location for
  a present but invalid mode.
- [x] Extends the assignment parser to recognize the ca binding expression
  token unquoted finished by `;`, without literal content or remnants after the mode.
- [x] Parse quoted strings as literal fragments plus interpolations
  optional, but diagnoses a string consisting exclusively of a single path
  which should be written as binding unquoted.
- [x] Add interpolation scanner for targets `string`: separate fragments
  longest valid path literals, reuses the common semantic resolver
  and deduplicate identical observations. Process `\$` before recognition
  horses, removing the backslash and keeping `$` as text.
- [x] Reject any `:Mode` in an interpolated fragment and keep as literal
  a `$` that does not start a valid path.
- [x] Enter the common internal source descriptor and move the solution to it
  used today by `EmitObservation`, without changing the logical AST already
  implemented.
- [x] Reuses the descriptor in `UiMarkupReactiveEmitter` for the leaves from
  `@when` and `@if`; keep the existing deduplication after the canonical expression.
- [x] Resolves `$DataContext.Path` at compile time, emits getter for each
  segment and setter only for the writable terminal property.
- [x] Resolve `$element.Property` to a UI property of an element named
  from the same name scope, including forward references, and emits the setter only
  for `TwoWay` on a non-read-only property.
- [x] Resolve `$self.Property` to the target element and compare the identity
  the source property with the target property; issues dedicated diagnosis for
  direct self-binding and allows another compatible property.
- [x] Solve `$control.parts.$part.Property` with exactly four segments
  existing before the mod suffix and emits the template part endpoint.
- [x] Keep `$owner.Property` and `$owner.Property:OneWay` on the route
  `TemplateBinding` exists and diagnoses `$owner.Property:TwoWay`.
- [x] Validates the source type against `PropertySpec.ValueType`, the target
  read-only and writable source for `TwoWay` before the C# release.
- [x] Accept source/target incompatibility only when the target is `string`
  and the mode is `OneWay`; attach to the descriptor the standard projection with
  `CurrentCulture` and rejects the same type pair for `TwoWay`.
- [x] Validates that each CLR owner in a path `$DataContext` implements
  `INotifyPropertyChanged`; sources `UiObject` continue to use
  `PropertyChanged`, and types without notification receive diagnosis.
- [x] Confirm that the documents `Window<TViewModel>` and
  `UserControl<TViewModel>` use the existing generic type when `DataType` does not
  it is repeated according to the current solution.
- [x] Re-index the solution after each C# or project-file change.

**Gate stage 3**

- [x] The same semantic resolver feeds attribute bindings and
  assignment, interpolations and reactive leaves, without two implementations of
  path walking.
- [x] The generated code does not contain reflection, string property paths or lookup de
  members at runtime.

Stage 3 proof from 2026-07-14: the parser and common descriptor are covered by
`BindingStageThree` (`6/6`), including the exact offset of the invalid mode,
assignment unquoted, quoted/interpolation, `\$`, deduplication, all endpoints,
type validation/writability/observability and generic inference for
`Window<TViewModel>` / `UserControl<TViewModel>`. The source-gen suite without samples
RED from stage 0 is green (`129/129`), indexing has zero warnings again
RoslynIndexer no longer finds the old symbols `EmitDataObservation` or
`EmitTemplatePartObservation`; inspection of the generated code confirms typed access,
without reflection or property paths evaluated at runtime.

### Stage 4 - Issuing the markup bindings

- [x] Extends `EmitProperty` to detect binding before references to
  resources, but only for forms with valid source and mode.
- [x] Issue the observation, controller and lifetime recording for
  `$DataContext.Path:OneWay` and `:TwoWay`.
- [x] Issue source-to-string projection for attributes and assignments
  conditionals `OneWay`, without reflection or string property paths.
- [x] Issue the interpolations as literal fragments plus typed observations and o
  composition function, without runtime evaluation of the path written as a string.
- [x] Issue direct bindings for `$element.Property[:Mode]` after
  building all named elements from the scope, including when the source is
  declared after the target and when the default mode is used.
- [x] Issue `$self.OtherProperty[:Mode]` using the same observation of
  UI property as named sources, without a duplicate runtime controller and
  with default `OneWay`.
- [x] Issue bindings to template parts and confirm reconnection to
  `ComponentTemplate` new.
- [x] Issue for unquoted conditional assignments binding factory
  activateable, apply default `OneWay` and register it in
  the corresponding rule/property from `MarkupConditionController`.
- [x] Confirm that an inactive conditional binding does not observe its source, does not
  write-back and rereads the current value as soon as the branch returns
  winning.
- [x] For elements from `@template`, register the controller via
  `TemplateEmissionContext.RegisterLifetime`; for common items,
use the lifecycle owner of the target.
- [x] Keeps the order of emission so that all elements named and
  the templates necessary for the source to exist before attaching the binding.
- [x] Inspect the generated code for simple examples with default/explicit mode,
  nested, directly named, `$self`, conditional unquoted, two-way and part: all
  accesses must be typed and fully qualified.
- [x] Re-index the solution after each C# or project-file change.

**Gate Stage 4**

- [x] The target examples, including direct binding between elements and binding
  conditional unquoted with default `OneWay` plus multi-source interpolations,
  compiles and works end-to-end in factory, `Window<TViewModel>` and
  `UserControl<TViewModel>`.
- [x] No binding generated remains active after template disposal or detach.

Stage 4 proof from 2026-07-14: Stage 0 plus Stage 4 scenarios are GREEN
(`19/19`), the entire source-generator suite is GREEN (`148/148`), and the matrix
runtime for bindings and observations is GREEN (`15/15`), without tests
skipped. Inspection of the generated source confirms typical and complete factories
qualified for direct binding, nested, named forward, `$self`, conditional,
interpolation and template lifetime, without reflection. End-to-end testing covers
factory, `Window<TViewModel>`, `UserControl<TViewModel>`, template swap and
detach/reattach; RoslynIndexer reports zero warnings, and `git diff --check`
does not report errors.

### Stage 5 - Diagnostics and compatibility

- [x] Add tested diagnostics for unknown mode, empty path, property
  missing terminal and `parts` written with a different capitalization; the lack of mode is
  legal and select `OneWay`.
- [x] Add diagnostics for `DataType` missing, non-existent member,
  inaccessible getter, type incompatibility and read-only target.
- [x] Add diagnostic for `TwoWay` on a source property without setter
  accessible or on a read-only part template property.
- [x] Add diagnostic for `TwoWay` between a non-string source and a target
  `string`, explaining that the automatic text projection is only `OneWay` and that
  reverse parsing requires a future converter.
- [x] Add diagnostics for non-existent named element, outside reference
  scope name, non-existent/read-only terminal UI property for
  `TwoWay` and direct self-binding of the target property to itself.
- [x] Add diagnostics for `:OneWay` and `:TwoWay` used in `@when` or
  `@if`, explaining that directives are always read-only.
- [x] Differentiates in tests the prohibited mode in the expression of the condition from the legal mode
  from the right side of an assignment located in the body of the same directive.
- [x] Add diagnostics for binding unquoted without `;`, with text left after
mode, with unknown mode or used as a fragment in a longer value.
- [x] Add diagnostic for a binding path quoted in assignment, inclusive
  with default mode, `:OneWay` or `:TwoWay`, without rejecting literal strings
  common ones like `"MyText"` or interpolations with real literal text.
- [x] Add diagnostics for `:OneWay`, `:TwoWay` or unknown mode in any
  interpolated fragment and for the self-reference of the same target property,
  including when the element is referred to by name instead of `$self`.
- [x] Add diagnostic for any CLR owner from a reactive path that does not
  implements `INotifyPropertyChanged`, explaining that the binding cannot
  observe a CLR property without a change signal.
- [x] Tests that `$Accent`, `$NamedAspect`, brush resources and values
  literals with `:` are not accidentally interpreted as bindings.
- [x] Tests as a string with a path inside, e.g
  `"Salut, $DataContext.Name"`, is interpolated and reactive, while
  `"Salut, lume"` remains literal, and the entire value `$DataContext.Count`
  use the simple binding with projection to `string`.
- [x] Tests the punctuation and delimitation of several interpolated paths, the fragments
  repeated, `$` literal that does not form a path and the entire resource `$Accent`.
- [x] Test the contrast between reactive `$DataContext.Name` and
  `\$DataContext.Name` literal in attributes and directives, including a sequence
  escaped that resembles a fragment with `:OneWay` / `:TwoWay` and must not
  interpreted or diagnosed as a mode of binding.
- [x] Test that `$owner.Content` continues to emit `context.Bind(...)` and that
  the conditional rules in the templates restore its value.
- [x] Tests that a path to the template part without terminal property is
  rejected both in the attribute and in `@when`.
- [x] Tests as `$VolumeSlider.Value` and
  `$VolumeSlider.Value:OneWay` emit the same binding, and `$VolumeSlider` without
  terminal property continues to follow the resource/diagnosis contract
  existing.
- [x] Tests that the unquoted assignment without mode and the one with `:OneWay` emit
  the same semantic descriptor, while the quoted form receives diagnosis.
- [x] Re-index the solution after each C# or project-file change.

**Gate Stage 5**

- [x] All invalid forms fail in source generator with diagnosis
  actionable, not except runtime or obscure C# error in the generated code.
- [x] The existing syntax for resources, aspects, templates and directives remains
  compatible, and `:Mode` from the condition `@when` remains illegal even if it is
  legally in the assignment of the branch.

Proof stage 5 of 2026-07-14: the matrix dedicated to diagnostics and
compatibility is GREEN (`10/10`), the entire source-generator suite is
GREEN (`158/158`), and the runtime tests targeted for bindings, observations
and template binding are GREEN (`23/23`), without skipped tests. They are covered
syntax and invalid modes, accessibility, types, writability, name
scope, direct and named self-binding, resources/aspects, interpolation,
the escape `\$`, `$owner` with conditional restoration and template parts in
attributes and conditions. RoslynIndexer reports zero warnings, again
`git diff --check` reports no errors.

### Stage 6 - Documentation and public API

- [x] Updates the conceptual documentation `.crn` with grammar
  `source-path[:mode]`, default `OneWay`, OneWay/TwoWay examples, nested paths,
  named elements, `$self`, template parts and request `DataType`.
- [x] Documents unquoted bindings from conditional assignments,
  the diagnosis for a quoted path, the fact that the quotation marks remain mandatory
  in attributes and the activate/deactivate/refresh cycle of the winning branch.
- [x] Documents the automatic conversion of `OneWay` to `string`, the current culture,
  empty result for `null` and no reverse conversion `TwoWay`.
- [x] Documents reactive interpolation, multiple paths, delimitation,
  the conversion of each fragment, the lack of modes in fragments and the difference between
  integral binding, interpolation and simple literal, including `\$` for a `$`
  literally.
- [x] Documents the `INotifyPropertyChanged` obligation for CLR owners from
  `$DataContext` and the fact that `UiObject`/template parts use their system of
  properties; it does not promise the magical observation of an auto-property without event.
- [x] Documents the fact that `PropertyChanged` for a linked source must
  raised on the `Update`/UI thread, the fail-fast off-thread behavior and the fact
  as this plan does not offer auto-marshaling.
- [x] Document the diagnosis for direct self-binding and show a separate one
  legal example where `$self` reads another property of the same element.
- [x] Explicitly documents that the sources in `@when` are read-only bindings
  composed, that all the leaves are observed and that the short-circuit affects
  only the assessment.
- [x] Document the semantics for intermediate `null`, nullable terminal,
  source/target read-only, cascade and lifecycle.
- [x] Use the `writing-api-documentation` skill for any public member
  new or modified from `GeneratedMarkup`, `MarkupObservation` and
  `MarkupDataPathSegment`.
- [x] Updates the corresponding pages exclusively under
  `docs-site/documentation/classes/` and synchronize
  `docs-site/documentation/manifest.json` only if added or renamed
  pages.
- [x] Run a public API diff and confirm that the change is limited to
  the helpers necessary for the source-generated code; does not expose the internal controller.

**Gate stage 6**
- [x] The documentation describes exactly the supported syntax, limits and behavior
  tested, no WPF examples that don't compile in Cerneala.
- [x] All public changes have synchronized API pages and valid manifest.

Evidence stage 6 of 2026-07-14: conceptual guide `docs/markup-data-bindings.md`
and the getting started page describe grammar, modes, sources, conversion,
interpolation, conditions, nulls, cascade, lifecycle and the contract of
threading. API pages for `GeneratedMarkup`, `MarkupObservation`,
`MarkupDataPathSegment`, `MarkupConditionalValue` and `UiMarkupGenerator` are
synchronized under `docs-site/documentation/classes/`. Roslyn audit/public diff
limits the new surface to the writable constructor of the segment and the five
factory/helper methods required for the generated code; the controller and the endpoints
remain internal. The JSON manifest has `858` entries, one valid for each
affected page, and remained unchanged because it was not added or renamed
no page. Automatic verification of content, links and placeholders
is GREEN, and `git diff --check` reports no errors.

### Stage 7 - Final check

- [x] Run the targeted source-generator tests:
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter FullyQualifiedName~UiMarkupGeneratorTests`.
- [x] Run the targeted runtime tests:
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter FullyQualifiedName~GeneratedMarkupBindingTests`.
- [x] Run the existing binding and template tests:
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~TextBoxTwoWayBindingTests|FullyQualifiedName~TemplateBindingTests"`.
- [x] Runs the entire suite with `dotnet test .\Cerneala.slnx` and does not accept
  new failed or skipped tests.
- [x] Manually inspect the generated code for the lack of reflection,
  duplicate subscriptions and setters issued on intermediate segments.
- [x] Executes a smoke test with repeated detach/reattach, change of
  `DataContext`, template swap, forward reference to named element, change
  between two conditional bindings, interpolation with several sources and o
  expression `(A and B) or C`.
- [x] Final reindex with
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json` and confirm zero warnings.
- [x] Review `git diff` to confirm that no conversion has entered,
  reflection paths or other extensions from non-objectives.

**Gate stage 7**

- [x] The targeted tests and the complete suite are GREEN.
- [x] The generated code, public API diff, documentation and RoslynIndexer are clean.
Stage 7 proof from 2026-07-14: the targeted source-generator tests are GREEN
(`158/158`), the targeted runtime tests are GREEN (`11/11`), and the regressions
existing binding/template are GREEN (`20/20`). The complete suite is GREEN:
`1769/1769` runtime plus `158/158` source-generator, in total `1927` tests, with
zero failed and zero skipped. The composite smoke passed separately (`1` runtime and
`6` source-generator) for detach/reattach, change of `DataContext`,
template swap, forward named source, two conditional suppliers, interpolation
multi-source and `(A and B) or C`. Emitter inspection and code test
generated confirm typed accessors without reflection, deduplication after expression
canonical and setter only on the terminal segment; the production audit reports
zero prohibited terms. The final reindex covers documents `1936`, `27843`
symbols and `114062` references with zero warnings, and `git diff --check` is
clean

## 9. Recommended order

1. Freeze the existing behavior and add RED tests.
2. Extend the runtime endpoints without touching the markup syntax yet.
3. Implement and verify the one-way/two-way plus lifecycle controller.
4. Extract the common semantic resolver and connect the existing directives.
5. Issue the attribute bindings, named/`$self` sources and providers
   conditional unquoted plus interpolations with default `OneWay`, then close
   diagnostics of type, scope and observability.
6. Update the documentation and run the full checks.

Do not continue to the attribute emitter if the runtime endpoint does not pass the tests
reconnection and disposal. Do not add conversion or multi-binding like that
"we prepare the future"; the future can handle itself, so be it.

## 10. The definition of ready

- [x] `Text="$DataContext.Name:OneWay"` initializes and updates the target.
- [x] `Text="$DataContext.Name"` is equivalent to the explicit form
  `:OneWay`, and `:TwoWay` remains opt-in.
- [x] The nested paths reconnect correctly when any segment is changed.
- [x] `TwoWay` propagates target input back without loops and continues to accept
  subsequent changes from the source.
- [x] `$control.parts.$part.Property[:Mode]` works with default `OneWay`
  and reconnects after template swap.
- [x] `$element.Property[:Mode]` works for backward references as well
  forward from the same name scope, with default `OneWay` and diagnosis for
  inaccessible sources.
- [x] `$self.OtherProperty[:Mode]` works, and the binding
  the target property itself is rejected with precise diagnosis.
- [x] `@when` uses the same typed sources, observes all the leaves and
  respect `and`, `or`, the brackets and the short-circuit.
- [x] Conditional assignments accept unquoted bindings with `OneWay`
implicitly, I reject a quoted path as a diagnosis, it only activates the binding
  winner, I stop the write-back when it loses and refresh to
  reactivation.
- [x] `Text="Salut, $DataContext.Name"` and multi-path interpolations
  recompose on any change, convert non-string values, treat `null`
  as empty and reject fragmentary modes.
- [x] `\$` produces a literal `$` both in attributes and in quoted strings
  from directives, without observations or false diagnoses of binding.
- [x] No path `$DataContext` with unobservable CLR owner compiles as a
  apparently reactive binding; the diagnosis requires `INotifyPropertyChanged`.
- [x] Any off-thread CLR `PropertyChanged` notification is coalesced and
  reevaluated on Relay before accessing the UI; without solvable Relay,
  the programmatic binding remains fail-fast with actionable diagnostics.
- [x] Incomplete paths, nullable terminals, detach/reattach and disposal au
  deterministic and tested behavior.
- [x] The markup binding is correctly overwritten/restored by
  `MarkupConditional`.
- [x] `$owner.Property` and `$owner.Property:OneWay` are equivalent,
  `$owner.Property:TwoWay` is diagnosed, and resource references remain
  compatible.
- [x] There are no reflection paths, conversion or additional modes.
- [x] The diagnoses are accurate, the public documentation is synchronized,
  the tests are GREEN and the Roslyn index is clean.
