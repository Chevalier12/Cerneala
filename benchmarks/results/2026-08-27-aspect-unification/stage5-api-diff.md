# Stage 5 public API diff

Compared `HEAD` and the current `Cerneala.dll` with the .NET SDK `ValidateAssembliesTask` in strict mode. The raw output is stored in `stage5-api-diff.txt`. ApiCompat exits with code 1 because this approved breaking migration intentionally adds and removes public surface; every reported difference is classified below.

## Approved additions

| Surface | Justification |
| --- | --- |
| `AspectBehavior`, `AspectPackage.Behaviors`, `AspectCatalog.Behaviors`, `ComponentAspectBuilder.AddBehavior` | Minimal generated ABI for target-typed, disposable Motion/event/observation sidecars owned by visible packages. It does not resolve declarations or create a second cascade. |
| `AspectConditionKey`, `AspectCondition.Signal` | Per-element reactive signal used by generated subscriptions to invalidate the canonical engine instead of writing conditional Aspect values directly. |
| `ElementAspectCondition`, the target/name/condition-aware `ElementAspect` constructors and properties | Makes named/inline `ElementAspect` a rule/declaration adapter consumed by the canonical engine. |
| `ElementAspectValue(UiProperty, AspectValue)` and `DynamicValue` | Allows resource/computed values to remain `AspectValue` declarations until engine resolution. |
| `GeneratedMarkup.CombineLifetimes` | Composes generated non-style sidecar lifetimes for deterministic cleanup. |
| Seven-argument `MarkupConditionRule` constructor | Adds the condition-state callback that updates `AspectConditionKey`; styling remains owned by `AspectEngine`. |
| `UiPropertyValueSource.TemplateOwnerBinding` | Template-subsystem source required by existing palette/chrome projection tests: explicit owner-to-part bindings must beat the part's own Aspect value without reusing an Aspect authoring band or hard `Local` value. |

## Approved removals

| Surface | Justification |
| --- | --- |
| `MarkupAspectResource` | The parallel `Action<UIElement>` executor is the runtime being deleted by the plan; retaining it would be a forbidden shim. |
| `UiPropertyValueSource.ApplicationAspectBase`, `ApplicationAspectVisualState`, `LocalAspectBase`, `LocalAspectConditional` | These bands encoded authoring origin rather than subsystem semantics. All reusable/local Aspect declarations now publish through canonical Aspect sources. |
| Six-argument `MarkupConditionRule` constructor | Replaced atomically by the explicit condition-state contract. There are no external users and retaining the overload would be compatibility-only surface. |

No other type or member removal was reported. Strict attribute comparison was evaluated separately and produced order-sensitive `TemplatePartAttribute` diagnostics on unchanged source declarations, so the stored contract run keeps strict API/member and parameter-name comparison enabled while leaving attribute-instance ordering out of the diff.
