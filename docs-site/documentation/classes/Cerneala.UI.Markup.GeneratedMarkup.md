# GeneratedMarkup Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`,
`UI/Markup/GeneratedMarkupBindings.cs`, `UI/Markup/GeneratedMarkupMotion.cs`,
`UI/Markup/GeneratedMarkupPrism.cs`, `UI/Markup/GeneratedMarkupResources.cs`,
`UI/Markup/GeneratedMarkupTimbre.cs`

Factory methods used by source-generated markup to observe reactive sources and
attach generated property bindings, Prism instances, Motion executions, and
Timbre attachments and commands.

```csharp
public static class GeneratedMarkup
```

## Examples
```csharp
MarkupObservation observation = GeneratedMarkup.ObserveProperty(element, UIElement.IsVisibleProperty);
using Binding binding = GeneratedMarkup.AttachPropertyBinding(
    element,
    element,
    UIElement.IsEnabledProperty,
    observation,
    BindingMode.OneWay,
    value => (bool)value!,
    "$self.IsVisible");
```

Generated XML property attributes default to a live `OneWay` binding:

```crn
$DataContext.Name             // keep the target synchronized with the source
$DataContext.Name:OneWay      // keep the target synchronized with the source
$DataContext.Name:TwoWay      // synchronize and write target changes back
```

This default also applies to property attributes inside templates. Directive
assignments retain their separate rules: a bare property path is read once;
live binding modes must be explicit in contexts that support them. Motion and
Prism direct values retain their snapshot behavior. `ReadReference<T>` remains
the read-once helper; attribute paths use binding attachment helpers instead.

## Methods
| Signature | Return Type | Description |
| --- | --- | --- |
| `CombineLifetimes(params IDisposable?[] lifetimes)` | `IDisposable?` | Returns `null`, the single lifetime, or one idempotent reverse-order composite lifetime. |
| `ApplyAspectValue<T>(UiObject target, UiProperty<T> property, T value)` | `IDisposable` | Sets a value an Aspect program brings (`@presence`, `@layout`) with the `AspectBase` source; disposing the returned lifetime clears it, so a replacing Aspect does not inherit it. |
| `GetTemplateOwner(UIElement element)` | `Control` | Returns the component template owner of an element created by a template while that template instance is attached; used for `$owner` in an Aspect applied at runtime. |
| `GetTemplatePart<T>(UIElement element, string name)` | `T` | Returns the named part of the element's template owner, checked to be a `T`; used for `$owner.parts.$Name` in a resource Aspect. |
| `RequirePrismClip(UIElement element, string definitionName)` | `UIElement` | Returns the element after checking that its attached Prism instance uses the composition named `definitionName`; used for `$self.prism` and `$owner.prism` in a resource Aspect. |
| `ObserveProperty(UiObject source, UiProperty property)` | `MarkupObservation` | Observes a Cerneala UI property and provides a writable endpoint when the property is writable. |
| `ObserveTemplatePartProperty(Control owner, string partName, UiProperty property)` | `MarkupObservation` | Observes a property on a named component-template part and reconnects after template replacement. |
| `ObserveObject(Func<object?> getter)` | `MarkupObservation` | Observes a getter-backed object value. |
| `ObserveDataPath(UIElement owner, params MarkupDataPathSegment[] segments)` | `MarkupObservation` | Observes a typed `DataContext` property path and its intermediate owners. |
| `ObserveInheritedDataPath(UIElement owner, params MarkupDataPathSegment[] segments)` | `MarkupObservation` | Observes a typed path from the owner's logical parent (or visual parent when no logical parent exists), excluding the owner's own `DataContext`. Generated interior `DataContext` attributes use this source when establishing a new scope. |
| `ObserveDataPath(object? source, params MarkupDataPathSegment[] segments)` | `MarkupObservation` | Observes a typed property path from a fixed source object without resolving `DataContext` through an element tree. Source-generated content templates use this overload for their item. |
| `ReadReference<T>(MarkupObservation observation, Func<object?, T> projection)` | `T` | Starts an observation long enough to read and project its current value, then stops it without attaching a binding. |
| `AttachConditions(UIElement owner, IReadOnlyList<MarkupObservation> observations, IReadOnlyList<MarkupConditionRule> rules)` | `IDisposable` | Attaches observations and rules to an element lifecycle and gates rule activation callbacks on effective renderability; sound activations of a rule are not gated by renderability. |
| `AttachMotionSession(UIElement owner)` | `IDisposable` | Creates the Motion session of one Aspect application for generated triggers and executions; generated code returns it as a lifetime of the Aspect behavior, so replacing the Aspect or detaching the owner disposes it. |
| `AttachMotionTriggers(UIElement owner, Action attach, Action detach)` | `IDisposable` | Runs direct event-subscription callbacks on attach and their matching unsubscription callbacks on detach. |
| `AddMotionTrigger(IDisposable session, Action attach, Action detach)` | `void` | Adds direct subscribe/unsubscribe callbacks to a generated motion session. |
| `StartMotion(IDisposable session, Func<IReadOnlyList<MotionHandle>> start)` | `MotionGroupHandle` | Starts one parallel generated execution and returns a group handle canceled with its session. |
| `StartMotionExecution(IDisposable session, Func<MarkupMotionExecution> start)` | `MarkupMotionExecution` | Starts and tracks a leaf or composed generated execution in the supplied lifecycle session. |
| `StartMotionExecution(IDisposable session, string handleName, Func<MarkupMotionExecution> start)` | `MarkupMotionExecution` | Starts an execution in a named session slot, canceling and replacing the previous active execution in that slot. |
| `CancelMotionExecution(IDisposable session, string handleName)` | `void` | Cancels and clears the active execution in a named session slot; does nothing when the slot is empty. |
| `CanStartMotionExecution(IDisposable session)` | `bool` | Returns whether the session would accept a new execution now: not disposed, owner attached and renderable. |
| `AttachTimbre(UIElement target, ResourceId<TimbreClipDefinition> clip, IReadOnlyDictionary<string, float>? arguments)` | `IDisposable` | Attaches the `@timbre $Clip(…)` of one Aspect application: resolves the clip resource now, gives every sound one playback slot in a scope of its own, starts the `AutoPlay` sounds in declaration order and registers the attachment as the target's current one. Disposing it stops every sound it started. |
| `AttachTimbre(UIElement target, TimbreClipDefinition clip, IReadOnlyDictionary<string, float>? arguments)` | `IDisposable` | The same for an inline `@timbre { … }` clip. |
| `PlayTimbre(UIElement target, string sound)` | `TimbrePlayback` | Starts the named sound of the target's current attachment; a running playback of the same sound is replaced (restart). Attachment arguments are applied to the parameters the sound uses. |
| `StopTimbre(UIElement target, string sound)` | `void` | Cancels the sound's running playback; a sound that is not playing is a no-op. |
| `PauseTimbre(UIElement target, string sound)` | `void` | Pauses the sound's running playback; a sound that is not playing is a no-op. |
| `ResumeTimbre(UIElement target, string sound)` | `void` | Resumes the sound's paused playback; a sound that is not playing is a no-op. |
| `SeekTimbre(UIElement target, string sound, TimeSpan position)` | `Task` | Requests an absolute seek on the playback running now and returns the seek task without waiting; a sound that is not playing returns a completed task. |
| `AttachTimbreSession(UIElement owner)` | `IDisposable` | Creates the Timbre session of one generated Aspect application: it owns the event handlers of `@on` bodies with Timbre commands or audio Motion and the audio Motion executions the Aspect starts. |
| `AddTimbreTrigger(IDisposable session, Action attach, Action detach)` | `void` | Adds event subscribe/unsubscribe callbacks that are active while the owner is attached, regardless of renderability. |
| `GetTimbreParameter(TimbreSound sound, string name)` | `TimbreParameter<float>` | Returns the sound's float parameter descriptor with the given name. |
| `StartTimbreMotion(IDisposable session, Func<MarkupMotionExecution> start)` | `MarkupMotionExecution` | Starts an audio Motion execution owned by the Timbre session: hiding the owner does not cancel it; detach, Aspect replacement and session disposal do. |
| `StartTimbreMotionProperty(UIElement target, string sound, string parameterName, bool hasFrom, float from, bool toCurrent, float to, MotionSpec<float>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts one `$X.timbre.Sound.Property` leaf: captures the sound's running playback and its `Volume` or named parameter once and animates it like `TimbreMotionAnimationBuilder.With`; a sound that is not playing completes immediately without starting audio. |
| `StartMotionProperty<T>(IDisposable session, UIElement target, UiProperty<T> property, bool hasFrom, T from, bool toCurrent, T to, MotionSpec<T>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts one typed property animation through the target root's motion system. |
| `StartBoundMotionProperty<T>(IDisposable session, UIElement target, UiProperty<T> property, bool hasFrom, T from, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, MotionSpec<T>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts one typed property animation whose destination follows an explicit one-way or two-way markup binding for the lifetime of the execution. |
| `StartPrismMotionProperty<T>(IDisposable session, UIElement target, int propertyId, Func<PrismInstance, T> getValue, Action<PrismInstance, T> setValue, bool discrete, bool hasFrom, T from, bool toCurrent, T to, MotionSpec<T>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts a statically resolved Prism property animation through the existing motion session and scheduler. |
| `StartBoundPrismMotionProperty<T>(IDisposable session, UIElement target, int propertyId, Func<PrismInstance, T> getValue, Action<PrismInstance, T> setValue, bool discrete, bool hasFrom, T from, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, MotionSpec<T>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts a Prism property animation whose destination follows an explicit one-way or two-way markup binding for the lifetime of the execution. |
| `AttachPrism(UIElement owner, Func<PrismInstance> instanceFactory)` | `IDisposable` | Attaches one generated Prism instance factory and replaces any previous Prism attachment on the element. |
| `AttachPrism(UIElement owner, Func<PrismInstance> instanceFactory, IReadOnlyList<Func<PrismInstance, IDisposable>> bindingFactories)` | `IDisposable` | Attaches a Prism factory plus generated dynamic-binding factories managed by element renderability. |
| `TryGetPrismInstance(UIElement owner, out PrismInstance? instance)` | `bool` | Gets the current attached instance when one has been created. |
| `GetPrismInstance(UIElement owner)` | `PrismInstance` | Gets the current attached instance or throws when the element has none. |
| `SetPrismMotionProperty<T>(UIElement target, Func<PrismInstance, T> getValue, Action<PrismInstance, T> setValue, T value)` | `void` | Applies a generated direct Motion `@set` through statically emitted typed accessors. |
| `AttachPrismValueBinding<T>(UIElement owner, PrismInstance instance, MarkupObservation observation, Func<PrismInstance, T> getValue, Action<PrismInstance, T> setValue, BindingMode mode, Func<object?, T> projection, string description)` | `IDisposable` | Attaches an explicit one-way or two-way binding to a generated Prism filter or style value. |
| `ApplyPrismValueReference<T>(PrismInstance instance, MarkupObservation observation, Action<PrismInstance, T> setValue, Func<object?, T> projection)` | `IDisposable` | Reads and applies a direct Prism reference once without retaining a source subscription. |
| `GetPrismFilterBoolean(PrismFilterState state, int entryStableId, int slot)` | `bool` | Reads a Boolean catalog slot from generated filter state. |
| `GetPrismFilterInteger(PrismFilterState state, int entryStableId, int slot)` | `int` | Reads an integer or enum catalog slot from generated filter state. |
| `GetPrismFilterNumber(PrismFilterState state, int entryStableId, int slot)` | `float` | Reads a numeric catalog slot from generated filter state. |
| `GetPrismFilterColor(PrismFilterState state, int entryStableId, int slot)` | `Color` | Reads a color catalog slot from generated filter state. |
| `GetPrismFilterVector(PrismFilterState state, int entryStableId, int slot)` | `Vector4` | Reads a vector catalog slot from generated filter state. |
| `GetPrismFilterResource(PrismFilterState state, int entryStableId, int slot)` | `PrismResourceId` | Reads a resource catalog slot from generated filter state. |
| `SetPrismFilterBoolean(PrismFilterState state, int entryStableId, int slot, bool value)` | `void` | Writes a Boolean catalog slot in generated filter state. |
| `SetPrismFilterInteger(PrismFilterState state, int entryStableId, int slot, int value)` | `void` | Writes an integer or enum catalog slot in generated filter state. |
| `SetPrismFilterNumber(PrismFilterState state, int entryStableId, int slot, float value)` | `void` | Writes a numeric catalog slot in generated filter state. |
| `SetPrismFilterColor(PrismFilterState state, int entryStableId, int slot, Color value)` | `void` | Writes a color catalog slot in generated filter state. |
| `SetPrismFilterVector(PrismFilterState state, int entryStableId, int slot, Vector4 value)` | `void` | Writes a vector catalog slot in generated filter state. |
| `SetPrismFilterResource(PrismFilterState state, int entryStableId, int slot, PrismResourceId value)` | `void` | Writes a resource catalog slot in generated filter state. |
| `GetPrismStyleBoolean(PrismStyleState state, int entryStableId, int slot)` | `bool` | Reads a Boolean catalog slot from generated style state. |
| `GetPrismStyleInteger(PrismStyleState state, int entryStableId, int slot)` | `int` | Reads an integer or enum catalog slot from generated style state. |
| `GetPrismStyleNumber(PrismStyleState state, int entryStableId, int slot)` | `float` | Reads a numeric catalog slot from generated style state. |
| `GetPrismStyleColor(PrismStyleState state, int entryStableId, int slot)` | `Color` | Reads a color catalog slot from generated style state. |
| `GetPrismStyleVector(PrismStyleState state, int entryStableId, int slot)` | `Vector4` | Reads a vector catalog slot from generated style state. |
| `GetPrismStyleResource(PrismStyleState state, int entryStableId, int slot)` | `PrismResourceId` | Reads a resource catalog slot from generated style state. |
| `SetPrismStyleBoolean(PrismStyleState state, int entryStableId, int slot, bool value)` | `void` | Writes a Boolean catalog slot in generated style state. |
| `SetPrismStyleInteger(PrismStyleState state, int entryStableId, int slot, int value)` | `void` | Writes an integer or enum catalog slot in generated style state. |
| `SetPrismStyleNumber(PrismStyleState state, int entryStableId, int slot, float value)` | `void` | Writes a numeric catalog slot in generated style state. |
| `SetPrismStyleColor(PrismStyleState state, int entryStableId, int slot, Color value)` | `void` | Writes a color catalog slot in generated style state. |
| `SetPrismStyleVector(PrismStyleState state, int entryStableId, int slot, Vector4 value)` | `void` | Writes a vector catalog slot in generated style state. |
| `SetPrismStyleResource(PrismStyleState state, int entryStableId, int slot, PrismResourceId value)` | `void` | Writes a resource catalog slot in generated style state. |
| `AttachPropertyBinding<T>(UIElement owner, UiObject target, UiProperty<T> targetProperty, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, string description)` | `Binding` | Attaches a typed one-way or two-way binding in the `MarkupBase` value slot. |
| `AttachInterpolatedStringBinding(UIElement owner, UiObject target, UiProperty<string> targetProperty, IReadOnlyList<MarkupObservation> observations, Func<string> compose, string description)` | `Binding` | Attaches a one-way string composer backed by one or more observations. |
| `AttachResource<T>(UIElement owner, UiObject target, UiProperty<T> targetProperty, string key, UiPropertyValueSource valueSource)` | `IDisposable` | Resolves the nearest resource, tracks application-provider changes, and updates the generated target value slot. |
| `CreateConditionalPropertyBinding<T>(UiObject target, UiProperty<T> targetProperty, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, string description)` | `MarkupConditionalValue` | Creates a reactive conditional value provider activated only while its rule wins. |
| `CreateConditionalInterpolatedStringBinding(UiObject target, UiProperty<string> targetProperty, IReadOnlyList<MarkupObservation> observations, Func<string> compose, string description)` | `MarkupConditionalValue` | Creates a conditional one-way string composer. |
| `FormatStringValue(object? value)` | `string` | Converts a binding value with `CurrentCulture`; `null` becomes `string.Empty`. |

## Remarks
The whole program of a markup Aspect (reactive values, `@on`, Motion,
`@presence`, `@layout`, input Motion and Timbre actions) is the behavior of the
Aspect object. It runs every time the Aspect is applied to an element, also when
an Aspect is assigned at runtime, and everything it attached is disposed when the
Aspect is replaced or the element detaches. A resource Aspect is compiled once:
`$owner.parts.$Name`, `$owner.prism` and `$self.prism` take their type from the
markup places where the Aspect is applied, and `GetTemplatePart<T>` /
`RequirePrismClip` throw `InvalidOperationException` when an element reached at
runtime does not match.

Returned observations are lifecycle-managed by the attached controller. The
generated path and template observers reconnect when their source changes.
Content-template paths start from the item carried by `ContentTemplateContext`,
while ordinary `$DataContext` paths continue to follow the element's effective
data context.

An ordinary generated factory's root `DataContext` binding is a distinct case:
its path starts from the factory argument, while descendant paths follow the
root's resulting effective data context. The generator uses the object-source
observation overload for that root expression to keep its input independent of
its output.

An interior element's `DataContext` attribute observes the context inherited
from its logical parent, falling back to its visual parent. It must not read
back its own resulting `DataContext`. `ObserveInheritedDataPath` provides this
separate source: it tracks parent context and path-owner notifications while
active, and resolves the current parent again when restarted. With no parent,
a nonempty path is unresolved even if the owner has its own data context.
Content-template paths that already use the template item as a fixed source
continue to use the object-source overload instead.

A property attribute such as `Text="$DataContext.Name"` attaches a live
`OneWay` binding, equivalent to `Text="$DataContext.Name:OneWay"`. This includes
attributes in component templates, where `$owner.Property` uses the live
template-binding path. Such bindings require observable CLR path owners and a
writable UI-property target. Suffix-less values in directive assignments, Prism
definitions and Motion executions still read once when applied; live bindings
there require an explicit mode in a supported context. Quotes around XML
attribute values are only XML delimiters, not a request for snapshot behavior.

Property bindings write to `MarkupBase`; conditional providers write to
`MarkupConditional` only while active. Two-way bindings accept write-back only
from an effective `Local` target change and remove that transient local value
after the source is updated. Binding controllers stop observations on detach,
refresh on reattach, and clear only their owned value slot when disposed.

Conditional observations continue to reconcile rule values and content while
their owner is attached but effectively hidden. Rule activation callbacks are
deactivated while the owner or an ancestor is hidden, collapsed, or invisible.
When the owner becomes renderable, the controller reevaluates the latest rule
state and activates matching callbacks. This allows generated Motion conditions
to be prepared in a hidden subtree without starting an execution prematurely.

Generated Aspect conditions use rule condition-state callbacks instead of `MarkupConditionalValue`. Observations update `AspectConditionKey` state, which queues the canonical Aspect engine. Ordinary non-Aspect conditional markup continues to use `MarkupConditional` values and content reconciliation.

`AttachResource<T>` follows element and ancestor resources before the attached
root provider. A local resource therefore shadows an application resource with
the same key. The controller subscribes only while its owner is attached,
re-resolves the nearest value after matching application-provider changes, and
marshals a cross-thread provider notification through the root Relay.

Disposing the lifetime returned by `AttachResource<T>` unsubscribes the
controller and stops further target updates from that attachment. Its callbacks
already queued through Relay become no-ops when the lifetime is disposed before
they execute. Disposal is idempotent and does not clear the previously written
target value.

Property-binding and condition observation use the current owner's relay when
available. Owner-thread notifications remain synchronous; worker notifications
are coalesced before refreshing the source and writing the target. Without a
relay, activation captures a thread and off-thread notifications are rejected.
Independent [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md)
adoption rebinds these data observers and subtree retirement disconnects them,
without setting UI attachment state. Conditional data values can therefore
update colliders without UI. Rule activation callbacks, Aspect processing,
Motion, Prism and root resource-provider services retain their existing UI
requirements; the independent context does not synthesize those services.

These methods are public so emitted source in consuming assemblies can call
them. `MarkupPropertyBindingController<T>`, conditional provider activation,
resolved/unresolved path state, and write-endpoint details remain internal.

Motion markup lowering calls these helpers with statically resolved
`UiProperty<T>`, event, target, resource, and `MotionSpec<T>` references. The
generated path does not use reflection, `dynamic`, element lookup by string, or
per-frame discovery.

Motion sessions do not subscribe a detached or effectively non-renderable owner.
`Hidden`, `Collapsed`, or `IsVisible=false` on the owner or an ancestor removes
the active subscription set and synchronously cancels every owned execution.
Returning to a renderable state recreates trigger subscriptions but does not
revive canceled executions. Detach and disposal perform the same cleanup
idempotently. Sessions attached to other elements are independent. Generated
Aspect Motion creates its session inside the Aspect behavior and returns it as
one of the behavior's lifetimes, so replacing or removing the Aspect disposes
the session and cancels its executions, and the new Aspect creates its own.

`StartMotionExecution` accepts the unified generated execution adapter, so
nested parallel and sequential groups are tracked without treating runtime
group handles as leaf handles. Cancellation is parameterless and idempotent for
the composed execution.

Named execution slots are scoped to the motion session created for one Aspect
application. Replacing a slot cancels its previous execution first. A terminal
execution removes itself from both session tracking and its slot, while detach
cancels all remaining slots and releases their references.

`StartMotionProperty<T>` resolves omitted specs from the registered animatable
property metadata. An explicit `from` value is staged before the animation is
started, while `toCurrent` captures the binding's current sampled value.

An explicit Motion binding is valid only for an `@animate` destination. Its
source is observed for the entire execution and every source change retargets
the active animation without replacing the returned handle. `TwoWay` also
writes animated samples back through the observation endpoint. Direct
references used by Motion are start-time snapshots; `@from`, `@set`, and
keyframe values reject explicit binding modes because those values define
snapshots rather than live destinations.

`AttachPrism` permits one attachment per element. Replacement disposes the old
attachment before registering the new one. Dynamic Prism bindings exist only
while the owner is effectively renderable. Hiding the owner disconnects them and
leaves the previous instance inert; showing it creates a fresh instance and
reapplies current base and bound values through the binding factories. Detach and
disposal release the instance, factories, subscriptions, and owner references.

The Prism filter and style accessors are public compiler-runtime bridges. Generated
code supplies stable catalog entry IDs and dense typed slots, so these methods do
not perform reflection or string lookup. An identical write is a no-op for
`PrismInstance.ValueVersion`.

The `@timbre` of an Aspect is lowered to `AttachTimbre`, a lifetime of the Aspect
behavior: every application of the Aspect, including every template or item
occurrence, attaches its own sounds and starts the `AutoPlay` ones; replacing the
Aspect, detaching the element or retiring the template disposes the attachment
and cancels every playback it started. Reattaching applies the Aspect again and
never revives earlier playbacks. Hiding or collapsing the element or an ancestor
does not stop its sounds. Each element has at most one current attachment, and
`@play`, `@stop`, `@pause`, `@resume` and `@seek` address one of its sounds by
name, so a command written in another Aspect (`@pause $Speaker.timbre.Music;`)
reaches the sounds of the Aspect the target has when the command runs.
Addressing a detached element, an element whose current Aspect brings no
`@timbre`, or a sound its clip does not declare throws, which stops the rest of
the generated action body; asynchronous I/O, decoder or device failures complete
the playback as `Failed` without rolling back earlier actions. Generated event
handlers for bodies that mix Timbre and Motion actions run them in source order
and start the Motion part only when `CanStartMotionExecution` is true.

An `@animate` or `@keyframes` execution whose targets are `$X.timbre.Sound.Property`
paths is an audio execution: it is lowered to `StartTimbreMotion` on the Timbre
session, runs in source order with the Timbre commands of its body (no
`CanStartMotionExecution` guard), and each leaf calls `StartTimbreMotionProperty`,
which reads the sound's running playback once at activation. The leaf animates exactly as
[TimbreMotionAnimationBuilder](Cerneala.UI.Timbre.TimbreMotionAnimationBuilder.md)
does; when `spec` is `null` it uses a 180 ms `Easings.Standard` tween, the
default of other non-property Motion targets.

```csharp
// What `@timbre $UiSounds(Brightness = 800);` and
// `@on Click { @play $self.timbre.Click; }` lower to in the Aspect behavior.
IDisposable sounds = GeneratedMarkup.AttachTimbre(
    button,
    new ResourceId<TimbreClipDefinition>("UiSounds"),
    new Dictionary<string, float> { ["Brightness"] = 800f });
GeneratedMarkup.PlayTimbre(button, "Click");
GeneratedMarkup.PauseTimbre(button, "Click");
_ = GeneratedMarkup.SeekTimbre(button, "Click", TimeSpan.FromSeconds(30));
sounds.Dispose(); // the Aspect is replaced: every sound it started stops
```

`StartPrismMotionProperty<T>` shares the regular Motion graph, scheduler, specs,
and cancellation. Numbers and colors interpolate continuously; generated Boolean,
integer, and enum targets use the discrete flag. A hidden, collapsed, invisible,
detached, or replaced target is canceled without restoring a value into an inert
Prism instance.

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Observation factories | `ArgumentNullException` or `ArgumentException` | A required source, getter, path segment collection, property, owner, or part name is invalid. |
| Property binding factories | `ArgumentNullException` | A required owner, target, target property, observation, observation collection, or projection delegate is `null`. |
| Interpolated binding factories | `ArgumentNullException` | A required owner, target, target property, observation collection, or compose delegate is `null`. |
| `AttachResource<T>` | `ArgumentNullException` or `ArgumentException` | A required owner, target, target property, or resource key is invalid. |
| `GetTemplateOwner` | `InvalidOperationException` | The element was not created by a component template that is currently attached to its owner. |
| `GetTemplatePart<T>` | `InvalidOperationException` | The template owner has no part with that name, or the part is not a `T`. |
| `RequirePrismClip` | `InvalidOperationException` | The element has no Prism instance, or its composition has another name. |
| Motion session factories | `ArgumentNullException` | A required owner, callback, start delegate, target, property, accessor delegate, or options value is `null`. |
| `AddMotionTrigger`, `StartMotion`, `StartMotionExecution`, `CancelMotionExecution`, `StartMotionProperty<T>`, `StartPrismMotionProperty<T>` | `ArgumentException` | The supplied lifetime was not created by `AttachMotionSession`, or a named execution slot is empty or whitespace. |
| `AddMotionTrigger`, `StartMotion`, `StartMotionExecution`, `CancelMotionExecution`, `StartMotionProperty<T>`, `StartPrismMotionProperty<T>` | `ObjectDisposedException` | The motion session has already been disposed. |
| `StartMotion`, `StartMotionExecution`, `StartMotionProperty<T>`, `StartPrismMotionProperty<T>` | `InvalidOperationException` | The session owner is detached or non-renderable, the target is not attached to the same root, an execution returns `null`, or a Prism property ID is reused with an incompatible type. |
| `AttachPrism(...)`, `TryGetPrismInstance(...)`, `GetPrismInstance(...)`, `SetPrismMotionProperty<T>(...)` | `ArgumentNullException` | A required owner, target, instance factory, binding-factory collection, getter, or setter is `null`. |
| `AttachPrism(...)` | `ArgumentException` | The binding-factory collection contains a `null` entry. |
| `AttachPrism(...)` | `InvalidOperationException` | The instance factory or a binding factory returns `null`. |
| `AttachPrism(...)` | `AggregateException` | One or more active Prism bindings fail while being disconnected. |
| `GetPrismInstance(...)`, `SetPrismMotionProperty<T>(...)`, `StartPrismMotionProperty<T>` | `InvalidOperationException` | The target has no current attached Prism instance. |
| Prism filter and style accessors | `ArgumentException` | The stable catalog entry ID does not match the supplied state. |
| Prism filter and style accessors | `InvalidOperationException` | The supplied state belongs to an obsolete Prism definition generation. |
| Binding factories | `ArgumentException` | The observation collection is empty. |
| Property binding factories | `InvalidOperationException` | The target property is read-only, or `TwoWay` is requested without a writable observation endpoint. |
| Property binding factories | `ArgumentOutOfRangeException` | The binding mode is not `OneWay` or `TwoWay`. |
| Active binding callbacks | `InvalidOperationException` | A consumed source notification or activation occurs on a thread other than the captured UI/update thread. |
| `AttachTimbre` | `InvalidOperationException` | The target is detached, or an argument names no parameter of the clip. |
| `AttachTimbre(UIElement, ResourceId<TimbreClipDefinition>, …)` | `KeyNotFoundException` | No `TimbreClipDefinition` resource with the key is reachable from the target. |
| `AttachTimbre`, `PlayTimbre` | `InvalidOperationException` | The target's root has no `TimbreRuntime` (raised when a sound starts, including `AutoPlay`). |
| `PlayTimbre`, `StopTimbre`, `PauseTimbre`, `ResumeTimbre`, `SeekTimbre`, `StartTimbreMotionProperty` | `InvalidOperationException` | The target is detached, its current Aspect brings no `@timbre`, or its clip declares no sound with that name. |
| `AttachTimbre`, `PlayTimbre` | `TimbreException` | The runtime rejects a start, for example `VoiceLimitExceeded`. |
| `AttachTimbre`, `PlayTimbre`, `SeekTimbre` | `ArgumentOutOfRangeException` | An argument value is outside the range of the modifier inputs it feeds, or a seek target is negative or beyond a known duration. |
| `AddTimbreTrigger`, `StartTimbreMotion` | `ArgumentException` | The lifetime was not created by `AttachTimbreSession`. |
| `AddTimbreTrigger`, `StartTimbreMotion` | `ObjectDisposedException` | The Timbre session has been disposed. |
| `GetTimbreParameter` | `ArgumentException` | The sound declares no float parameter with that name. |

## Applies to
Source-generated reactive, Prism, Motion, and Timbre markup.

## See Also
- `Cerneala.UI.Markup.MarkupObservation`
- `Cerneala.UI.Markup.MarkupDataPathSegment`
- `Cerneala.UI.Markup.MarkupConditionalValue`
- `Cerneala.UI.Markup.MarkupMotionExecution`
- [TimbreClipDefinition](Cerneala.Timbre.TimbreClipDefinition.md)
- [TimbreScope](Cerneala.Timbre.TimbreScope.md)
- [TimbreHandle](Cerneala.Timbre.TimbreHandle.md)
- [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md)
- [TimbreMotionFacade](Cerneala.UI.Timbre.TimbreMotionFacade.md)
- `Cerneala.UI.Prism.Runtime.PrismInstance`
- `Cerneala.UI.Prism.Runtime.PrismFilterState`
- `Cerneala.UI.Prism.Runtime.PrismStyleState`
- `docs/reference/markup-data-bindings.md`
- `docs/reference/motion-markup-syntax.md`
- `docs/reference/prism-markup-syntax.md`
