# Final public API diff

The .NET SDK `ValidateAssembliesTask` compared the `HEAD` and final Release `Cerneala.dll` assemblies in strict mode with parameter-name checks. Raw output is `final-api-diff.txt`. ApiCompat exits 1 because this explicitly approved pre-user breaking migration adds and removes public surface; every reported difference is classified below.

## Unified runtime and sidecar additions

- `AspectBehavior`, `AspectPackage.Behaviors`, `AspectCatalog.Behaviors`, and `ComponentAspectBuilder.AddBehavior` provide the minimal target-typed disposable ABI for non-style package sidecars.
- `AspectConditionKey`, `AspectCondition.Signal`, `ElementAspectCondition`, expanded `ElementAspect`, and dynamic `ElementAspectValue` keep generated named/inline/conditional declarations in the canonical engine.
- `GeneratedMarkup.CombineLifetimes` and the condition-state `MarkupConditionRule` constructor own generated cleanup/signals without a second declaration resolver.
- `TemplateOwnerBinding` replaces the former misuse of `LocalAspectBase` for explicit template-owner chrome projection.

## Diagnostics additions and intentional shape changes

- `AspectAuthoringKind`, `AspectOrigin`, and origin properties on packages, rules, and `ElementAspect` provide immutable document/default/named/inline metadata that does not participate in cascade.
- `AspectConditionTrace`, expanded `AspectResolutionStep`, `AspectRuleSet.SourceOrder`/`Scope`, `ResolvedAspectValue.SourceRule`, and rejected/winning rules expose the exact captured resolution path.
- `AspectEngineCounters.ConditionEvaluations` measures actual condition nodes after structural filtering.
- `AspectResolutionStep` remains a record with value equality/init properties. Its old seven-field constructor and seven-field `Deconstruct` are removed because the old shape could not represent source order, origin, scope, conditions, or dependencies.
- The old three-argument `RejectedAspectDeclaration` constructor is replaced by the rule-aware constructor required to identify both origins.

## Intentional removals

- `Cerneala.UI.Markup.MarkupAspectResource` is deleted; retaining it would preserve the forbidden executor.
- `UiPropertyValueSource.ApplicationAspectBase`, `ApplicationAspectVisualState`, `LocalAspectBase`, and `LocalAspectConditional` are deleted because authoring path is not a subsystem precedence coordinate.
- The old six-argument `MarkupConditionRule` constructor is replaced atomically by the explicit condition-state callback contract.

No other public type/member removal or signature change appears in the final strict diff. Every added public type/member has a canonical page in `docs-site/documentation/classes/`, and the manifest validation gate is GREEN.
