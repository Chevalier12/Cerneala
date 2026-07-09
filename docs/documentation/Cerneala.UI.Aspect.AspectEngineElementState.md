# AspectEngineElementState Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectEngineElementState.cs`

Stores the last applied aspect result and diagnostics snapshot for one element tracked by `AspectEngine`.

```csharp
internal sealed class AspectEngineElementState
```

Inheritance:
`object` -> `AspectEngineElementState`

## Examples

The type is an internal implementation detail. `AspectEngine` creates and updates it through a per-element `ConditionalWeakTable`:

```csharp
AspectEngineElementState state = states.GetOrCreateValue(element);
bool changed = ApplyResolved(element, state.LastResolved, resolved);

state.LastResolved = resolved;
state.Diagnostics = BuildDiagnostics(resolved, environment, counters.Snapshot());
```

## Remarks

`AspectEngineElementState` is the mutable state record used by `AspectEngine` for a single `UIElement`. The engine keeps instances in a `ConditionalWeakTable<UIElement, AspectEngineElementState>`, so state is associated with an element without adding public API surface to the element itself.

`LastResolved` stores the previous `ResolvedAspect` produced by `Apply`. `AspectEngine.Apply` compares this value with the next resolved aspect to update changed aspect-base values and clear aspect-base values that no longer resolve. `AspectEngine.Clear` uses the same stored result to clear previously applied `UiPropertyValueSource.AspectBase` values, then sets `LastResolved` to `null`.

`Diagnostics` stores the last diagnostics snapshot built during `Apply`. It is initialized to an empty `AspectDiagnostics.Snapshot`, replaced after successful application, returned by `AspectEngine.GetDiagnostics`, and reset to an empty snapshot by `Clear`.

This class is internal and is not intended for application code. Public consumers should use `AspectEngine.Apply`, `AspectEngine.GetDiagnostics`, `AspectEngine.GetDependencies`, and `AspectEngine.Clear` instead.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LastResolved` | `ResolvedAspect?` | Gets or sets the last resolved aspect result applied for the element, or `null` before application or after `AspectEngine.Clear`. |
| `Diagnostics` | `AspectDiagnostics.Snapshot` | Gets or sets the diagnostics snapshot associated with the latest application; defaults to an empty snapshot. |

## Applies to

Cerneala UI aspect engine per-element state tracking.

## See also

- `AspectEngine`
- `AspectDiagnostics.Snapshot`
- `ResolvedAspect`
- `AspectDependencySet`
