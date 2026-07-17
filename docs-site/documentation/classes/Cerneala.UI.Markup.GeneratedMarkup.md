# GeneratedMarkup Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/GeneratedMarkupConditions.cs`, `UI/Markup/GeneratedMarkupBindings.cs`, `UI/Markup/GeneratedMarkupMotion.cs`, `UI/Markup/GeneratedMarkupResources.cs`

Factory methods used by source-generated markup to observe reactive sources and
attach generated property bindings.

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

## Methods
| Signature | Return Type | Description |
| --- | --- | --- |
| `ObserveProperty(UiObject source, UiProperty property)` | `MarkupObservation` | Observes a Cerneala UI property and provides a writable endpoint when the property is writable. |
| `ObserveTemplatePartProperty(Control owner, string partName, UiProperty property)` | `MarkupObservation` | Observes a property on a named component-template part and reconnects after template replacement. |
| `ObserveObject(Func<object?> getter)` | `MarkupObservation` | Observes a getter-backed object value. |
| `ObserveDataPath(UIElement owner, params MarkupDataPathSegment[] segments)` | `MarkupObservation` | Observes a typed `DataContext` property path and its intermediate owners. |
| `AttachConditions(UIElement owner, IReadOnlyList<MarkupObservation> observations, IReadOnlyList<MarkupConditionRule> rules)` | `IDisposable` | Attaches observations and rules to an element lifecycle. |
| `AttachMotionSession(UIElement owner)` | `IDisposable` | Creates a lifecycle-scoped session for generated motion triggers and executions. |
| `AttachMotionTriggers(UIElement owner, Action attach, Action detach)` | `IDisposable` | Runs direct event-subscription callbacks on attach and their matching unsubscription callbacks on detach. |
| `AddMotionTrigger(IDisposable session, Action attach, Action detach)` | `void` | Adds direct subscribe/unsubscribe callbacks to a generated motion session. |
| `StartMotion(IDisposable session, Func<IReadOnlyList<MotionHandle>> start)` | `MotionGroupHandle` | Starts one parallel generated execution and returns a group handle canceled with its session. |
| `StartMotionExecution(IDisposable session, Func<MarkupMotionExecution> start)` | `MarkupMotionExecution` | Starts and tracks a leaf or composed generated execution in the supplied lifecycle session. |
| `StartMotionExecution(IDisposable session, string handleName, Func<MarkupMotionExecution> start)` | `MarkupMotionExecution` | Starts an execution in a named session slot, canceling and replacing the previous active execution in that slot. |
| `CancelMotionExecution(IDisposable session, string handleName)` | `void` | Cancels and clears the active execution in a named session slot; does nothing when the slot is empty. |
| `StartMotionProperty<T>(IDisposable session, UIElement target, UiProperty<T> property, bool hasFrom, T from, bool toCurrent, T to, MotionSpec<T>? spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts one typed property animation through the target root's motion system. |
| `AttachPropertyBinding<T>(UIElement owner, UiObject target, UiProperty<T> targetProperty, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, string description)` | `Binding` | Attaches a typed one-way or two-way binding in the `MarkupBase` value slot. |
| `AttachInterpolatedStringBinding(UIElement owner, UiObject target, UiProperty<string> targetProperty, IReadOnlyList<MarkupObservation> observations, Func<string> compose, string description)` | `Binding` | Attaches a one-way string composer backed by one or more observations. |
| `AttachResource<T>(UIElement owner, UiObject target, UiProperty<T> targetProperty, string key, UiPropertyValueSource valueSource)` | `IDisposable` | Resolves the nearest resource, tracks application-provider changes, and updates the generated target value slot. |
| `CreateConditionalPropertyBinding<T>(UiObject target, UiProperty<T> targetProperty, MarkupObservation observation, BindingMode mode, Func<object?, T> projection, string description)` | `MarkupConditionalValue` | Creates a reactive conditional value provider activated only while its rule wins. |
| `CreateConditionalInterpolatedStringBinding(UiObject target, UiProperty<string> targetProperty, IReadOnlyList<MarkupObservation> observations, Func<string> compose, string description)` | `MarkupConditionalValue` | Creates a conditional one-way string composer. |
| `FormatStringValue(object? value)` | `string` | Converts a binding value with `CurrentCulture`; `null` becomes `string.Empty`. |

## Remarks
Returned observations are lifecycle-managed by the attached controller. The
generated path and template observers reconnect when their source changes.

Property bindings write to `MarkupBase`; conditional providers write to
`MarkupConditional` only while active. Two-way bindings accept write-back only
from an effective `Local` target change and remove that transient local value
after the source is updated. Binding controllers stop observations on detach,
refresh on reattach, and clear only their owned value slot when disposed.

`AttachResource<T>` follows element and ancestor resources before the attached
root provider. A local resource therefore shadows an application resource with
the same key. The controller subscribes only while its owner is attached,
re-resolves the nearest value after matching application-provider changes, and
marshals a cross-thread provider notification through the root Relay.

The binding thread is captured when the controller first activates. A source
notification received on another thread fails before the observation reads its
source or writes the target. These helpers do not marshal notifications.

These methods are public so emitted source in consuming assemblies can call
them. `MarkupPropertyBindingController<T>`, conditional provider activation,
resolved/unresolved path state, and write-endpoint details remain internal.

Motion markup lowering calls these helpers with statically resolved
`UiProperty<T>`, event, target, resource, and `MotionSpec<T>` references. The
generated path does not use reflection, `dynamic`, element lookup by string, or
per-frame discovery.

Motion sessions do not subscribe a detached owner. They keep at most one active
subscription set, remove it on detach, recreate it on reattach, and perform the
same cleanup idempotently when disposed. Detach also cancels every execution
owned by that session and clears its property bindings. Sessions attached to
other elements are independent.

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

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Observation factories | `ArgumentNullException` or `ArgumentException` | A required source, getter, path segment collection, property, owner, or part name is invalid. |
| Property binding factories | `ArgumentNullException` | A required owner, target, target property, observation, observation collection, or projection delegate is `null`. |
| Interpolated binding factories | `ArgumentNullException` | A required owner, target, target property, observation collection, or compose delegate is `null`. |
| `AttachResource<T>` | `ArgumentNullException` or `ArgumentException` | A required owner, target, target property, or resource key is invalid. |
| Motion session factories | `ArgumentNullException` | A required owner, callback, start delegate, target, property, or options value is `null`. |
| `AddMotionTrigger`, `StartMotion`, `StartMotionExecution`, `CancelMotionExecution`, `StartMotionProperty<T>` | `ArgumentException` | The supplied lifetime was not created by `AttachMotionSession`, or a named execution slot is empty or whitespace. |
| `AddMotionTrigger`, `StartMotion`, `StartMotionExecution`, `CancelMotionExecution`, `StartMotionProperty<T>` | `ObjectDisposedException` | The motion session has already been disposed. |
| `StartMotion`, `StartMotionExecution`, `StartMotionProperty<T>` | `InvalidOperationException` | The session owner is detached, the target is not attached to the same root, or the execution returns `null`. |
| Binding factories | `ArgumentException` | The observation collection is empty. |
| Property binding factories | `InvalidOperationException` | The target property is read-only, or `TwoWay` is requested without a writable observation endpoint. |
| Property binding factories | `ArgumentOutOfRangeException` | The binding mode is not `OneWay` or `TwoWay`. |
| Active binding callbacks | `InvalidOperationException` | A consumed source notification or activation occurs on a thread other than the captured UI/update thread. |

## Applies to
Source-generated reactive markup.

## See Also
- `Cerneala.UI.Markup.MarkupObservation`
- `Cerneala.UI.Markup.MarkupDataPathSegment`
- `Cerneala.UI.Markup.MarkupConditionalValue`
- `Cerneala.UI.Markup.MarkupMotionExecution`
- `docs/markup-data-bindings.md`
- `docs/motion-markup-syntax-proposal.md`
