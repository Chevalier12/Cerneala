# Modern Aspect And Template System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current MVP aspect/template surface with a modern 2026-grade system: typed design tokens, cascade layers, variants, slots, scoped aspect environments, component templates, content templates, data templates, diagnostics, and predictable invalidation. Do not copy WPF/Avalonia resource dictionaries, implicit target-type aspects, trigger bags, or string selector soup.

**Architecture:** Keep Cerneala's retained UI, `UiProperty`, `UIRoot`, invalidation queues, the current MVP aspect work queue, inherited property propagation, and motion system. Add a new aspect engine inside `Cerneala.UI.Aspect` and a modern template layer inside `Cerneala.UI.Controls.Templates`. Bridge the existing API only while migrating controls. When migration is complete, delete compatibility shims instead of leaving `[Obsolete]` garbage.

**Tech Stack:** C#/.NET, xUnit, existing `UiProperty` value precedence, the root aspect processor introduced by this plan, `ThemeProvider`, `Motion`, retained renderer, RoslynIndexer for repo navigation.

**Naming Decision:** The public system name is **Aspect**, matching **Motion**. Existing MVP classes must be treated as pre-rebrand compatibility, not as the model to copy. New public API should use `AspectPackage`, `AspectRegistry`, `AspectCatalog`, `AspectEngine`, `AspectToken<T>`, `AspectCondition`, and `AspectSlot`. Internal files should move to `UI/Aspect` as they are migrated.

---

## Current Audit

- Current MVP aspect applicator is a linear rule loop: every rule is evaluated, matching setters overwrite previous setters, and values are applied through base / visual-state value sources.
- Current MVP selector is predicate based. It is flexible, but opaque: no structured selector model, no specificity, no dependency graph, no indexed matching, and weak diagnostics.
- Current theme object is a typed key/value bag. Useful, but not enough for semantic tokens, component tokens, modes, density, aliases, or scoped overrides.
- Current default theme hardcodes a tiny button/text/border rule sheet and a button `ControlTemplate`.
- Current `UI/Controls/ControlTemplate{TControl}.cs` is a simple factory returning one root plus `TemplateBinding`s.
- Current `UI/Controls/TemplateContext.cs` only exposes owner and `Bind`.
- Current `UI/Controls/ContentPresenter.cs` supports explicit `DataTemplate`, direct `UIElement`, and string fallback, but no implicit content template lookup.
- Current `UI/Controls/DataTemplate{T}.cs` is a simple typed factory. No keyed templates, predicate templates, template registry, data context, or recycling hooks.
- Current `UI/Controls/ItemsPresenter.cs` can virtualize and recycle through `ItemContainerGenerator`, but template selection is not a first-class system.

## Pre-Rebrand Migration Map

The names in this table are exact current-code search targets only. They are not the new public API vocabulary and must not leak into docs, samples, or final public types.

| Current search target | New destination/concept |
| --- | --- |
| `UI/Styling/StyleProcessor.cs` | `UI/Aspect/AspectProcessor.cs` |
| `UI/Styling/StyleApplicator.cs` | `UI/Aspect/AspectApplicator.cs` or delete after `AspectEngine` owns the runtime path |
| `UI/Styling/StyleInvalidation.cs` | `UI/Aspect/AspectInvalidation.cs` |
| `UI/Styling/StyleDiagnostics.cs` | `UI/Aspect/AspectDiagnostics.cs` |
| `UI/Styling/Style.cs` | delete or compatibility-only old rule collection |
| `UI/Styling/StyleSheet.cs` | delete or compatibility-only old rule sheet |
| `UI/Styling/StyleRule.cs` | replace with `AspectRuleSet` |
| `UI/Styling/StyleSelector.cs` | replace with `AspectTarget` + `AspectCondition` |
| `UI/Styling/Setter.cs` and `Setter{T}.cs` | keep as internal declaration apply helpers or replace with `AspectDeclaration` |
| `UI/Styling/VisualStateRule.cs` | replace with `AspectState` / `AspectCondition.State` |
| `UI/Styling/PseudoClass*.cs` | bridge into `AspectStateSet.FromElement(...)` |
| `UI/Styling/Theme*.cs` and `DefaultTheme.cs` | bridge into `DefaultAspectPackage` and token setup |
| `UI/Elements/UIRoot.StyleProcessor` | add `UIRoot.AspectProcessor`; remove old root property after migration if no compatibility path remains |
| `UI/Core/UiPropertyValueSource.StyleBase` / `StyleVisualState` | keep internal until precedence migration is explicit, or rename through a dedicated value-source migration task |

## Design North Star

- Aspect should feel like a typed, composable runtime design system, not like WPF/Avalonia with new class names.
- A component should expose named aspect slots, variants, states, and tokens.
- A theme should define semantic tokens and component token defaults, not random string resources.
- A template should be a typed component recipe with named slots and parts, not an unstructured factory that happens to return a root.
- Data templates should be resolved by a registry using type/key/predicate priority, not only explicit property assignment.
- Diagnostics should answer "why is this value here?" without spelunking through random runtime state.

## Explicit Non-Goals

- Do not introduce XAML.
- Do not introduce CSS strings as the core API.
- Do not introduce WPF-like `ResourceDictionary`, `BasedOn`, trigger collections, `DataTrigger`, `MultiDataTrigger`, `FindName`, or global target-type implicit rules as the primary model.
- Do not redesign layout/rendering/input architecture.
- Do not add unrelated controls.
- Do not keep `[Obsolete]` wrappers at the end. Either the old API is still genuinely supported, or it is deleted.

## New Public Model

The implementation should converge on this conceptual authoring shape:

```csharp
public static class AppAspect
{
    public static readonly AspectToken<Color> Surface = AspectToken.Color("app.surface");
    public static readonly AspectToken<Color> Text = AspectToken.Color("app.text");
    public static readonly AspectToken<Color> Accent = AspectToken.Color("app.accent");

    public static AspectPackage Create()
    {
        return AspectPackage.Create("App")
            .Tokens(tokens => tokens
                .Set(Surface, new Color(255, 255, 255))
                .Set(Text, new Color(28, 35, 48))
                .Set(Accent, new Color(37, 99, 235)))
            .Components(components => components
                .For<Button>(button => button
                    .Slot(ButtonSlots.Root)
                    .Base(s => s
                        .Set(Control.BackgroundProperty, AspectRef.To(Surface))
                        .Set(Control.ForegroundProperty, AspectRef.To(Text)))
                    .Variant(ButtonVariants.Primary, s => s
                        .Set(Control.BackgroundProperty, AspectRef.To(Accent)))
                    .State(AspectState.Hover, s => s
                        .Set(Control.BorderColorProperty, AspectRef.To(Accent)))
                    .When(AspectCondition.Property(ToggleButton.IsCheckedProperty).Is(true), s => s
                        .Set(Control.BorderThicknessProperty, new Thickness(2)))
                    .When(AspectCondition.All(
                        AspectCondition.State(AspectState.Hover),
                        AspectCondition.Data<UserCardModel>(
                            "important user card",
                            model => model.IsImportant,
                            AspectDataDependency.Property<UserCardModel, bool>(nameof(UserCardModel.IsImportant)))), s => s
                        .Set(Control.BorderColorProperty, AspectRef.To(Accent)))
                    .Template(ButtonTemplates.Modern)));
    }
}
```

This is only the shape to target. Keep the final API small and consistent with existing Cerneala types.

---

## Phase 1 - Foundation: Typed Aspect Values And Tokens

- [x] Add tests first in `tests/Cerneala.Tests/UI/Aspect/AspectTokenTests.cs`.
  - [x] `TypedTokenCarriesNameAndValueType`.
  - [x] `TokenRejectsEmptyName`.
  - [x] `TokenNamesAreComparedByOrdinalNameAndType`.
  - [x] `TokenReferenceResolvesThroughAspectEnvironment`.
  - [x] `MissingTokenProducesDiagnosticFailureInsteadOfInvalidCast`.
- [x] Add `UI/Aspect/AspectToken.cs`.
  - [x] Define abstract `AspectToken` with:
    - [x] `string Name`.
    - [x] `Type ValueType`.
    - [x] `static AspectToken<T> Create<T>(string name)`.
    - [x] convenience factories: `Color`, `Thickness`, `Float`, `String`, `Motion`.
  - [x] Validate `name` is non-empty and contains no whitespace-only value.
- [x] Add `UI/Aspect/AspectToken{T}.cs`.
  - [x] Define sealed `AspectToken<T> : AspectToken`.
  - [x] Expose `AspectValue<T> Ref()` returning a token reference value.
- [x] Add `UI/Aspect/AspectValue.cs`.
  - [x] Define abstract `AspectValue` with `Type ValueType`, `IReadOnlyList<AspectToken> Dependencies`, and `object? Resolve(AspectResolutionContext context)`.
- [x] Add `UI/Aspect/AspectValue{T}.cs`.
  - [x] Define sealed wrapper for literal values and token references.
  - [x] Static constructors:
    - [x] `AspectValue<T>.Literal(T value)`.
    - [x] `AspectValue<T>.Token(AspectToken<T> token)`.
    - [x] `AspectValue<T>.Computed(Func<AspectResolutionContext, T> compute, IReadOnlyList<AspectToken> dependencies)`.
- [x] Add `UI/Aspect/AspectRef.cs`.
  - [x] `AspectRef.To<T>(AspectToken<T> token)` returns `AspectValue<T>`.
- [x] Add `UI/Aspect/AspectResolutionContext.cs`.
  - [x] Contains:
    - [x] `UIElement Element`.
    - [x] `AspectEnvironment Environment`.
    - [x] `AspectStateSet States`.
    - [x] `AspectVariantSet Variants`.
    - [x] `ThemeProvider? ThemeProvider`.
- [x] Add `UI/Aspect/AspectEnvironment.cs`.
  - [x] Stores token values and scoped overrides.
  - [x] Methods:
    - [x] `Set<T>(AspectToken<T> token, T value)`.
    - [x] `TryGet<T>(AspectToken<T> token, out T value)`.
    - [x] `CreateChildScope(string name)`.
    - [x] `Version` incremented on changes.
  - [x] Parent lookup must be explicit and deterministic.
- [x] Add bridge tests in `tests/Cerneala.Tests/UI/Aspect/ThemeTokenBridgeTests.cs`.
  - [x] `ThemeKeyCanBeProjectedIntoAspectToken`.
  - [x] `DefaultThemeTokensMatchExistingDefaultThemeValues`.
- [x] Add `UI/Aspect/ThemeTokenBridge.cs`.
  - [x] Maps existing `ThemeKey<T>` to `AspectToken<T>` while migration is active.
  - [x] This is a temporary bridge, but do not mark it `[Obsolete]`.
  - [x] Add a final cleanup task to delete it once no production code needs it.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectTokenTests|FullyQualifiedName~ThemeTokenBridgeTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

Expected skeleton:

```csharp
namespace Cerneala.UI.Aspect;

public abstract class AspectToken
{
    private protected AspectToken(string name, Type valueType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Aspect token name cannot be empty.", nameof(name));
        }

        Name = name;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }

    public string Name { get; }

    public Type ValueType { get; }

    public static AspectToken<T> Create<T>(string name) => new(name);
}
```

---

## Phase 2 - Structured Aspect Targets, Slots, States, And Variants

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectStateSetTests.cs`.
  - [x] `StateSetTracksHoverPressedFocusDisabledSelected`.
  - [x] `StateSetCanContainCustomNamedState`.
  - [x] `StateSetEqualityIsOrderIndependent`.
- [x] Add `UI/Aspect/AspectState.cs`.
  - [x] Define sealed value object with `Name`.
  - [x] Built-ins:
    - [x] `Hover`.
    - [x] `Pressed`.
    - [x] `Focus`.
    - [x] `FocusWithin`.
    - [x] `Disabled`.
    - [x] `Selected`.
    - [x] `Checked`.
    - [x] `Expanded`.
- [x] Add `UI/Aspect/AspectStateSet.cs`.
  - [x] Immutable-ish set API:
    - [x] `Contains(AspectState state)`.
    - [x] `Add(AspectState state)`.
    - [x] `Remove(AspectState state)`.
    - [x] `FromElement(UIElement element, PseudoClassRegistry registry)`.
  - [x] Internally keep stable ordering for diagnostics.
- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectVariantTests.cs`.
  - [x] `VariantKeyIsTypedByOwnerControl`.
  - [x] `VariantSetStoresTypedVariantValues`.
  - [x] `VariantSetRejectsValueFromDifferentKeyType`.
- [x] Add `UI/Aspect/AspectVariantKey.cs`.
  - [x] Abstract base with `Name`, `OwnerType`, `ValueType`.
- [x] Add `UI/Aspect/AspectVariantKey{TOwner,TValue}.cs`.
  - [x] Typed variant key for component variants.
  - [x] Example: `AspectVariantKey<Button, ButtonKind>`.
- [x] Add `UI/Aspect/AspectVariantSet.cs`.
  - [x] Stores current component variant values.
  - [x] Supports `Set<TControl,TValue>(AspectVariantKey<TControl,TValue> key, TValue value)`.
  - [x] Supports `TryGet`.
- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectSlotTests.cs`.
  - [x] `SlotKeyIsTypedByOwnerAndPart`.
  - [x] `RootSlotIsAlwaysAvailable`.
  - [x] `SlotPathFormatsForDiagnostics`.
- [x] Add `UI/Aspect/AspectSlot.cs`.
  - [x] Abstract base with `Name`, `OwnerType`, `TargetType`.
- [x] Add `UI/Aspect/AspectSlot{TOwner,TTarget}.cs`.
  - [x] Typed slot key.
  - [x] Static factory `AspectSlot.For<TOwner,TTarget>(string name)`.
- [x] Add `UI/Aspect/AspectSlotPath.cs`.
  - [x] Represents target slot path from owner to template part.
  - [x] Contains `AspectSlot Slot` and optional `string DiagnosticPath`.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectStateSetTests|FullyQualifiedName~AspectVariantTests|FullyQualifiedName~AspectSlotTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 3 - Modern Rule Model And Cascade Layers

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectRuleSetTests.cs`.
  - [x] `BaseRuleMatchesControlTypeAndSlot`.
  - [x] `StateRuleMatchesOnlyWhenStateIsPresent`.
  - [x] `VariantRuleMatchesOnlyWhenVariantValueMatches`.
  - [x] `PropertyConditionMatchesCurrentUiPropertyValue`.
  - [x] `DataConditionMatchesTypedTemplateData`.
  - [x] `MultiConditionAllRequiresEveryCondition`.
  - [x] `MultiConditionAnyRequiresAtLeastOneCondition`.
  - [x] `MultiConditionNotInvertsCondition`.
  - [x] `ConditionDependenciesAreReportedForInvalidation`.
  - [x] `AspectDoesNotExposeTriggerCollections`.
  - [x] `LayerOrderWinsBeforeDeclarationOrder`.
  - [x] `HigherSpecificityWinsWithinSameLayer`.
  - [x] `LaterDeclarationWinsForEqualLayerAndSpecificity`.
- [x] Add `UI/Aspect/AspectLayer.cs`.
  - [x] Define sealed value object with `Name` and `Order`.
  - [x] Built-ins:
    - [x] `Reset` order 0.
    - [x] `Theme` order 100.
    - [x] `Component` order 200.
    - [x] `App` order 300.
    - [x] `User` order 400.
    - [x] `Runtime` order 500.
- [x] Add `UI/Aspect/AspectSpecificity.cs`.
  - [x] Fields:
    - [x] `int Component`.
    - [x] `int Slot`.
    - [x] `int Variant`.
    - [x] `int State`.
    - [x] `int Property`.
    - [x] `int Data`.
    - [x] `int Compound`.
    - [x] `int Predicate`.
  - [x] Implement `IComparable<AspectSpecificity>`.
  - [x] Specificity order must be deterministic for all condition kinds: component, slot, variant, state, property, data, compound, predicate.
- [x] Add `UI/Aspect/AspectConditionNode.cs`.
  - [x] Internal base class for structured matching.
  - [x] Subtypes:
    - [x] `TypeAspectCondition`.
    - [x] `SlotAspectCondition`.
    - [x] `StateAspectCondition`.
    - [x] `VariantAspectCondition`.
    - [x] `PropertyAspectCondition<TValue>`.
    - [x] `DataAspectCondition<TData>`.
    - [x] `DataAspectCondition<TData,TValue>`.
    - [x] `AllAspectCondition`.
    - [x] `AnyAspectCondition`.
    - [x] `NotAspectCondition`.
    - [x] `PredicateAspectCondition`.
  - [x] Predicate condition must carry a diagnostic name.
  - [x] Property/data/predicate conditions must declare dependency metadata; hidden dependencies are not allowed.
  - [x] This is the modern replacement for WPF `Trigger`, `DataTrigger`, and `MultiDataTrigger`; do not add `Trigger` classes.
- [x] Add `UI/Aspect/AspectCondition.cs`.
  - [x] Public sealed facade/value type wrapping an internal `AspectConditionNode`.
  - [x] Static factory methods:
    - [x] `AspectCondition.State(AspectState state)`.
    - [x] `AspectCondition.Variant<TControl,TValue>(AspectVariantKey<TControl,TValue> key, TValue value)`.
    - [x] `AspectCondition.Property<TValue>(UiProperty<TValue> property)`.
    - [x] `AspectCondition.Data<TData>(string diagnosticName, Func<TData, bool> predicate, params AspectDataDependency[] dependencies)`.
    - [x] `AspectCondition.Data<TData,TValue>(string diagnosticName, Func<TData, TValue> selector, Func<TValue, bool> predicate, params AspectDataDependency[] dependencies)`.
    - [x] `AspectCondition.All(params AspectCondition[] conditions)`.
    - [x] `AspectCondition.Any(params AspectCondition[] conditions)`.
    - [x] `AspectCondition.Not(AspectCondition condition)`.
  - [x] `Property(...).Is(value)` must compare with the property's metadata comparer where available.
  - [x] `Property(...).Matches(predicate, diagnosticName)` must require a diagnostic name.
  - [x] `Data(...)` must require a non-empty diagnostic name and explicit dependencies, be typed, and fail closed when data is null or incompatible.
- [x] Add `UI/Aspect/AspectDataDependency.cs`.
  - [x] Represents a named dependency used by data conditions.
  - [x] Contains:
    - [x] `string Name`.
    - [x] `Type? OwnerType`.
    - [x] `string? PropertyName`.
  - [x] Provide helpers:
    - [x] `AspectDataDependency.Property<TData,TValue>(string propertyName)`.
    - [x] `AspectDataDependency.Named(string name)`.
- [x] Add `UI/Aspect/AspectConditionDependency.cs`.
  - [x] Tracks dependency kind:
    - [x] `State`.
    - [x] `Variant`.
    - [x] `UiProperty`.
    - [x] `DataContext`.
    - [x] `Token`.
    - [x] `Predicate`.
  - [x] Used by the invalidation graph so data/property conditions do not force full-tree aspect recalculation.
- [x] Add `UI/Aspect/AspectConditionResult.cs`.
  - [x] Contains:
    - [x] `bool Matches`.
    - [x] `IReadOnlyList<AspectConditionDependency> Dependencies`.
    - [x] `string DiagnosticText`.
    - [x] child results for `All` / `Any` / `Not`.
- [x] Add `UI/Aspect/AspectDataContext.cs`.
  - [x] Represents current data item for content/data template aspect.
  - [x] Contains:
    - [x] `object? Data`.
    - [x] `Type? DataType`.
    - [x] `int? Index`.
    - [x] `object? Owner`.
  - [x] Exposed in `AspectMatchContext`.
- [x] Add `UI/Aspect/AspectTarget.cs`.
  - [x] Contains:
    - [x] `Type ElementType`.
    - [x] `AspectSlot? Slot`.
    - [x] `IReadOnlyList<AspectCondition> Conditions`.
    - [x] calculated `AspectSpecificity`.
  - [x] Method `bool Matches(AspectMatchContext context)`.
- [x] Add `UI/Aspect/AspectMatchContext.cs`.
  - [x] Contains element, owner component, slot path, states, variants, environment version.
  - [x] Add data context fields for Aspect data conditions:
    - [x] `AspectDataContext DataContext`.
    - [x] `object? Data`.
    - [x] `Type? DataType`.
    - [x] `int? ItemIndex`.
- [x] Add `UI/Aspect/AspectDeclaration.cs`.
  - [x] Contains:
    - [x] `UiProperty Property`.
    - [x] `AspectValue Value`.
    - [x] `AspectMotion? Motion`.
    - [x] `string? DiagnosticName`.
- [x] Add `UI/Aspect/AspectMotion.cs`.
  - [x] Represents an optional Motion transition attached to an aspect declaration.
  - [x] Contains:
    - [x] `UiProperty Property`.
    - [x] `string TokenName`.
    - [x] source mask for base/state/variant/data-driven changes.
  - [x] Bridges to existing Motion tokens until default aspect tokens own motion presets.
- [x] Add `UI/Aspect/AspectRuleSet.cs`.
  - [x] Replacement for the current pre-rebrand rule type in the new engine.
  - [x] Contains:
    - [x] `string Name`.
    - [x] `AspectLayer Layer`.
    - [x] `AspectTarget Target`.
    - [x] `IReadOnlyList<AspectDeclaration> Declarations`.
    - [x] `int DeclarationOrder`.
- [x] Add `UI/Aspect/AspectRuleSetBuilder.cs`.
  - [x] Fluent API for packages.
  - [x] Must not use strings for core selector matching except diagnostic names and token names.
- [x] Keep existing `AspectRule` untouched in this phase.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectRuleSetTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

Expected cascade compare:

```csharp
internal readonly record struct AspectCascadeKey(
    int LayerOrder,
    AspectSpecificity Specificity,
    int DeclarationOrder)
    : IComparable<AspectCascadeKey>;
```

---

## Phase 4 - Aspect Packages And Registries

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectPackageTests.cs`.
  - [x] `PackageContainsNamedTokensComponentsAndTemplates`.
  - [x] `RegistryCombinesPackagesInRegistrationOrder`.
  - [x] `DuplicatePackageNameThrows`.
  - [x] `DuplicateTokenWithDifferentValueTypeThrows`.
  - [x] `PackageCanContributeContentTemplates`.
- [x] Add `UI/Aspect/AspectPackage.cs`.
  - [x] Contains:
    - [x] `string Name`.
    - [x] `IReadOnlyList<AspectTokenDefinition> Tokens`.
    - [x] `IReadOnlyList<AspectRuleSet> Rules`.
    - [x] `IReadOnlyList<ComponentTemplateDefinition> ComponentTemplates`.
    - [x] `IReadOnlyList<ContentTemplateDefinition> ContentTemplates`.
  - [x] Static `Create(string name)`.
- [x] Add `UI/Aspect/AspectTokenDefinition.cs`.
  - [x] Holds `AspectToken Token` and `AspectValue DefaultValue`.
- [x] Add `UI/Aspect/AspectPackageBuilder.cs`.
  - [x] Sections:
    - [x] `Tokens(Action<AspectTokenBuilder> build)`.
    - [x] `Components(Action<ComponentAspectBuilder> build)`.
    - [x] `Content(Action<ContentTemplateBuilder> build)`.
- [x] Add `UI/Aspect/AspectRegistry.cs`.
  - [x] Holds packages and builds immutable `AspectCatalog`.
  - [x] Methods:
    - [x] `Register(AspectPackage package)`.
    - [x] `Unregister(string packageName)`.
    - [x] `BuildCatalog()`.
  - [x] Maintains `Version`.
- [x] Add `UI/Aspect/AspectCatalog.cs`.
  - [x] Immutable resolved catalog:
    - [x] indexed rules by element type.
    - [x] token defaults.
    - [x] component templates.
    - [x] content templates.
    - [x] package diagnostics.
- [x] Wire `UIRoot` with a `AspectRegistry` property.
  - [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectRootRegistryTests.cs`.
  - [x] `RootCreatesDefaultAspectRegistry`.
  - [x] `RegisteringPackageInvalidatesAspectForSubtree`.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectPackageTests|FullyQualifiedName~AspectRootRegistryTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 5 - Aspect Engine, Resolved Aspects, And Invalidation Graph

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectEngineTests.cs`.
  - [x] `EngineAppliesResolvedAspectValuesToElement`.
  - [x] `EngineClearsValuesNoLongerProvided`.
  - [x] `EngineDoesNotReapplyWhenCatalogEnvironmentAndStatesAreUnchanged`.
  - [x] `EngineReappliesWhenTokenDependencyChanges`.
  - [x] `EngineReappliesWhenStateDependencyChanges`.
  - [x] `EngineReappliesWhenVariantDependencyChanges`.
  - [x] `EngineReappliesWhenPropertyConditionDependencyChanges`.
  - [x] `EngineReappliesWhenDataContextDependencyChanges`.
  - [x] `EngineDoesNotReapplyUnrelatedDataConditions`.
  - [x] `EngineReportsWinnerAndRejectedDeclarations`.
- [x] Add `UI/Aspect/ResolvedAspect.cs`.
  - [x] Contains:
    - [x] `IReadOnlyDictionary<UiProperty, ResolvedAspectValue> Values`.
    - [x] `IReadOnlyList<AspectRuleSet> MatchedRules`.
    - [x] `IReadOnlyList<RejectedAspectDeclaration> RejectedDeclarations`.
    - [x] `AspectDependencySet Dependencies`.
- [x] Add `UI/Aspect/ResolvedAspectValue.cs`.
  - [x] Contains property, resolved value, source declaration, cascade key, motion.
- [x] Add `UI/Aspect/RejectedAspectDeclaration.cs`.
  - [x] Contains rejected declaration, winning declaration, reason.
- [x] Add `UI/Aspect/AspectDependencySet.cs`.
  - [x] Tracks:
    - [x] tokens.
    - [x] states.
    - [x] variants.
    - [x] `UiProperty` dependencies used by property conditions.
    - [x] data context dependencies used by data conditions.
    - [x] composed condition dependencies from `All` / `Any` / `Not`.
    - [x] slot.
    - [x] catalog version.
    - [x] environment version.
- [x] Add `UI/Aspect/AspectEngine.cs`.
  - [x] Public methods:
    - [x] `AspectApplicationResult Apply(UIElement element, AspectCatalog catalog, AspectEnvironment environment, ThemeProvider? themeProvider = null)`.
    - [x] `ResolvedAspect Resolve(UIElement element, AspectCatalog catalog, AspectEnvironment environment, ThemeProvider? themeProvider = null)`.
    - [x] `AspectDiagnostics.Snapshot GetDiagnostics(UIElement element)`.
    - [x] `Clear(UIElement element)`.
  - [x] Internally cache per element in `ConditionalWeakTable<UIElement, AspectEngineElementState>`.
  - [x] Use structured match cache keyed by element type + slot + state set + variant set + catalog version.
  - [x] Apply values through the current pre-rebrand `UiPropertyValueSource` entries until Phase 11 decides whether to rename value sources or keep them as internal compatibility.
- [x] Add `UI/Aspect/AspectEngineElementState.cs`.
  - [x] Stores previous resolved values, dependency set, diagnostics.
- [x] Add `UI/Aspect/AspectInvalidationGraph.cs`.
  - [x] Maps token/state/variant/catalog dependency changes to affected elements.
  - [x] Keep it small: register dependencies from last successful apply only.
- [x] Modify `UI/Aspect/AspectProcessor.cs`.
  - [x] Use `AspectEngine` when `UIRoot.AspectRegistry` has modern packages.
  - [x] Bridge from the current root processor property until `UIRoot.AspectProcessor` exists.
  - [x] Fall back to the current MVP applicator only until default theme migration is complete.
- [x] Modify `UI/Elements/UIRoot.cs`.
  - [x] Add `AspectProcessor` as the canonical root aspect processing property.
  - [x] Keep the current pre-rebrand processor property only as a temporary forwarding compatibility member if existing tests require it.
  - [x] Add a Phase 11 cleanup task to delete the forwarding property once call sites are migrated.
- [x] Modify `UI/Aspect/AspectInvalidation.cs`.
  - [x] Support modern aspect engine invalidation.
  - [x] Keep old invalidation tests green.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectEngineTests|FullyQualifiedName~AspectInvalidationTests|FullyQualifiedName~AspectApplicatorTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 6 - Modern Diagnostics And Aspect Trace

- [x] Add tests in `tests/Cerneala.Tests/UI/Diagnostics/ModernAspectTraceTests.cs`.
  - [x] `TraceShowsWinningDeclarationLayerSpecificityAndPackage`.
  - [x] `TraceShowsRejectedDeclarationsWithReasons`.
  - [x] `TraceShowsTokenResolutionChain`.
  - [x] `TraceShowsSlotAndVariantContext`.
- [x] Extend `UI/Aspect/AspectDiagnostics.cs`.
  - [x] Add modern fields without breaking current tests:
    - [x] `ResolvedAspect? ResolvedAspect`.
    - [x] `IReadOnlyList<AspectResolutionStep> ResolutionSteps`.
    - [x] `IReadOnlyList<AspectTokenTrace> TokenTraces`.
- [x] Add `UI/Aspect/AspectResolutionStep.cs`.
  - [x] Fields:
    - [x] package name.
    - [x] rule name.
    - [x] target.
    - [x] layer.
    - [x] specificity.
    - [x] declaration order.
    - [x] outcome.
- [x] Add `UI/Aspect/AspectTokenTrace.cs`.
  - [x] Fields:
    - [x] token.
    - [x] provider/scope name.
    - [x] raw value.
    - [x] resolved value.
- [x] Modify `UI/Diagnostics/AspectTrace.cs`.
  - [x] Teach it to consume modern diagnostics.
  - [x] Keep existing trace tests green.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ModernAspectTraceTests|FullyQualifiedName~AspectTraceTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 7 - Component Templates With Slots And Parts

- [x] Add tests in `tests/Cerneala.Tests/Controls/Templates/ComponentTemplateTests.cs`.
  - [x] `TypedComponentTemplateReceivesOwnerStateVariantsAndTokens`.
  - [x] `TemplateRegistersNamedSlots`.
  - [x] `TemplateRegistersRequiredParts`.
  - [x] `MissingRequiredPartFailsWithClearDiagnostic`.
  - [x] `ChangingTemplateDetachesOldInstanceAndBindings`.
  - [x] `SameTemplateKeepsStableGeneratedRoot`.
- [x] Add folder `UI/Controls/Templates/`.
- [x] Add `UI/Controls/Templates/ComponentTemplate.cs`.
  - [x] Abstract base with:
    - [x] `Type OwnerType`.
    - [x] `string Name`.
    - [x] `ComponentTemplateInstance CreateInstance(Control owner, ComponentTemplateContext context)`.
- [x] Add `UI/Controls/Templates/ComponentTemplate{TControl}.cs`.
  - [x] Takes `Func<ComponentTemplateContext<TControl>, UIElement?>`.
  - [x] Validates owner type.
- [x] Add `UI/Controls/Templates/ComponentTemplateContext.cs`.
  - [x] Contains:
    - [x] `Control Owner`.
    - [x] `AspectEnvironment Environment`.
    - [x] `AspectStateSet States`.
    - [x] `AspectVariantSet Variants`.
    - [x] `RegisterSlot(AspectSlot slot, UIElement element)`.
    - [x] `RequirePart<TElement>(string name, TElement element)`.
    - [x] `Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty)`.
    - [x] `BindToken<T>(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty)`.
- [x] Add `UI/Controls/Templates/ComponentTemplateContext{TControl}.cs`.
  - [x] Typed owner convenience.
- [x] Add `UI/Controls/Templates/ComponentTemplateInstance.cs`.
  - [x] Contains:
    - [x] `UIElement? Root`.
    - [x] template bindings.
    - [x] token bindings.
    - [x] `TemplateSlotMap`.
    - [x] `TemplatePartMap`.
  - [x] Methods:
    - [x] `Attach(Control owner)`.
    - [x] `Detach()`.
    - [x] `Dispose()`.
- [x] Add `UI/Controls/Templates/TemplateSlotMap.cs`.
  - [x] Maps `AspectSlot` to generated element.
  - [x] Used by aspect engine to target slots.
- [x] Add `UI/Controls/Templates/TemplatePartMap.cs`.
  - [x] Maps part names to elements and validates required parts.
- [x] Add `UI/Controls/Templates/TemplateTokenBinding.cs`.
  - [x] Resolves token from aspect environment and updates target property when environment version changes.
- [x] Modify `UI/Controls/Control.cs`.
  - [x] Add new `ComponentTemplate? ComponentTemplate` property or rename existing `Template` only if all tests and existing API migration are handled in the same phase.
  - [x] Safer path:
    - [x] Add `ComponentTemplateProperty`.
    - [x] Keep `TemplateProperty` temporarily for current tests.
    - [x] `ApplyTemplate()` chooses `ComponentTemplate` first, then old `Template`.
  - [x] Do not mark old `TemplateProperty` `[Obsolete]` yet.
- [x] Add `UI/Controls/Templates/ControlTemplateAdapter.cs`.
  - [x] Converts existing `ControlTemplate` to `ComponentTemplate` only during migration.
  - [x] Final cleanup phase must delete this if no production code uses old `ControlTemplate`.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ComponentTemplateTests|FullyQualifiedName~ControlTemplateTests|FullyQualifiedName~TemplateBindingTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 8 - Content Templates And Data Template Registry

- [x] Add tests in `tests/Cerneala.Tests/Controls/Templates/ContentTemplateRegistryTests.cs`.
  - [x] `RegistryResolvesTemplateByExactDataType`.
  - [x] `RegistryResolvesTemplateByAssignableDataTypeWithNearestTypeWinning`.
  - [x] `KeyedTemplateWinsWhenKeyRequested`.
  - [x] `PredicateTemplateCanOverrideTypeTemplateByPriority`.
  - [x] `MissingTemplateFallsBackToStringTextBlock`.
  - [x] `NullContentProducesNoChildUnlessNullTemplateRegistered`.
- [x] Add `UI/Controls/Templates/ContentTemplate.cs`.
  - [x] Abstract base:
    - [x] `Type? DataType`.
    - [x] `string? Key`.
    - [x] `int Priority`.
    - [x] `bool CanApply(ContentTemplateMatchContext context)`.
    - [x] `UIElement? Create(ContentTemplateContext context)`.
- [x] Add `UI/Controls/Templates/ContentTemplate{TData}.cs`.
  - [x] Typed factory with `ContentTemplateContext<TData>`.
- [x] Add `UI/Controls/Templates/ContentTemplateContext.cs`.
  - [x] Contains:
    - [x] `object? Data`.
    - [x] `ContentPresenter? Presenter`.
    - [x] `AspectEnvironment Environment`.
    - [x] `AspectVariantSet Variants`.
    - [x] `int Index`.
    - [x] `object? Owner`.
- [x] Add `UI/Controls/Templates/ContentTemplateContext{TData}.cs`.
  - [x] Typed `Data`.
- [x] Add `UI/Controls/Templates/ContentTemplateMatchContext.cs`.
  - [x] Contains data, requested key, presenter, owner, index.
- [x] Add `UI/Controls/Templates/ContentTemplateRegistry.cs`.
  - [x] Supports:
    - [x] `Register(ContentTemplate template)`.
    - [x] `Unregister(ContentTemplate template)`.
    - [x] `TryResolve(ContentTemplateMatchContext context, out ContentTemplate template)`.
  - [x] Match order:
    - [x] explicit key.
    - [x] predicate priority.
    - [x] exact type.
    - [x] nearest assignable type.
    - [x] registration order as last tie-breaker.
- [x] Add `UI/Controls/Templates/ContentTemplateDefinition.cs`.
  - [x] Allows aspect packages to contribute content templates.
- [x] Modify `UI/Controls/ContentPresenter.cs`.
  - [x] Add `ContentTemplateKeyProperty`.
  - [x] Add `ContentTemplateRegistry? LocalTemplateRegistry` or resolve via root aspect catalog.
  - [x] Resolution order:
    - [x] explicit existing `DataTemplate` property.
    - [x] explicit modern `ContentTemplate` property if added.
    - [x] registry by key/type/predicate.
    - [x] direct `UIElement`.
    - [x] string fallback to `TextBlock`.
  - [x] Preserve existing `ContentPresenterTests`.
- [x] Add bridge `UI/Controls/Templates/DataTemplateAdapter.cs`.
  - [x] Wrap current `DataTemplate` as modern `ContentTemplate`.
  - [x] Delete in final cleanup if no production code requires old `DataTemplate`.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ContentTemplateRegistryTests|FullyQualifiedName~ContentPresenterTests|FullyQualifiedName~DataTemplateTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 9 - ItemsControl Integration And Template Recycling

- [x] Add tests in `tests/Cerneala.Tests/Controls/Templates/ItemsContentTemplateIntegrationTests.cs`.
  - [x] `ItemsControlUsesRegistryTemplateWhenItemTemplateIsNull`.
  - [x] `ExplicitItemTemplateOverridesRegistryTemplate`.
  - [x] `VirtualizedItemsReuseContainersWithoutLeakingOldDataContext`.
  - [x] `TemplateContextReceivesItemIndex`.
  - [x] `ChangingTemplateRegistryInvalidatesRealizedItems`.
- [x] Add `UI/Controls/Templates/TemplateRecycleKey.cs`.
  - [x] Contains content template identity, data type, container type, and slot info.
- [x] Add `UI/Controls/Templates/TemplateRecyclePool.cs`.
  - [x] Small pool for template-generated item visuals.
  - [x] Must detach and reset bindings before reuse.
- [x] Modify `UI/Controls/ItemContainerGenerator.cs`.
  - [x] Pass item index and template registry context into container preparation.
  - [x] Ensure recycled `ContentPresenter` refreshes when template selection changes.
- [x] Modify `UI/Controls/ItemsPresenter.cs`.
  - [x] When `ItemsOwner` is null, use content template registry before `ItemTemplate?.CreateElement`.
  - [x] When `ItemsOwner` exists, preserve existing generator/recycling flow.
- [x] Modify `UI/Controls/ItemsControl.cs`.
  - [x] Add optional `ItemTemplateKeyProperty`.
  - [x] Add invalidation when root aspect catalog content template version changes.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ItemsContentTemplateIntegrationTests|FullyQualifiedName~ItemsControlTests|FullyQualifiedName~ItemsControlRecyclingStabilityTests|FullyQualifiedName~ItemContainerRecyclePoolTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 10 - Default Design System Package

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/DefaultAspectPackageTests.cs`.
  - [x] `DefaultPackageDefinesCoreSemanticTokens`.
  - [x] `DefaultPackageDefinesButtonComponentTokens`.
  - [x] `DefaultPackageRegistersModernButtonTemplate`.
  - [x] `DefaultPackageAspectsTextBlockBorderAndButton`.
  - [x] `DefaultPackageDoesNotRequireLegacyRuleSheet`.
- [x] Add `UI/Aspect/DefaultAspectTokens.cs`.
  - [x] Tokens:
    - [x] `Color.Background`.
    - [x] `Color.Foreground`.
    - [x] `Color.Surface`.
    - [x] `Color.Border`.
    - [x] `Color.Accent`.
    - [x] `Typography.FontFamily`.
    - [x] `Typography.FontSize`.
    - [x] `Spacing.ControlPadding`.
    - [x] `Stroke.ControlBorderThickness`.
    - [x] `Motion.Fast`.
    - [x] `Motion.Normal`.
- [x] Add `UI/Controls/Buttons/ButtonSlots.cs`, `UI/Controls/Buttons/ButtonVariants.cs`, and `UI/Controls/Buttons/ButtonTokens.cs`.
  - [x] Slots:
    - [x] `ButtonSlots.Root`.
    - [x] `ButtonSlots.Content`.
  - [x] Variants:
    - [x] `ButtonVariants.Kind` with enum `ButtonKind { Neutral, Primary, Danger }`.
    - [x] `ButtonVariants.Size` with enum `ButtonSize { Small, Medium, Large }`.
  - [x] Component tokens:
    - [x] `ButtonTokens.Background`.
    - [x] `ButtonTokens.Foreground`.
    - [x] `ButtonTokens.BorderColor`.
    - [x] `ButtonTokens.HoverBackground`.
    - [x] `ButtonTokens.PressedBackground`.
    - [x] `ButtonTokens.DisabledOpacity`.
- [x] Add `UI/Controls/ButtonTemplates.cs`.
  - [x] `public static readonly ComponentTemplate<Button> Modern`.
  - [x] Template structure:
    - [x] root `Border` registered as `ButtonSlots.Root`.
    - [x] inner `ContentPresenter` registered as `ButtonSlots.Content`.
    - [x] token bindings for border/background/foreground/padding/font.
    - [x] template bindings for `ContentControl.ContentProperty`.
- [x] Add `UI/Aspect/DefaultAspectPackage.cs`.
  - [x] Creates `AspectPackage`.
  - [x] Defines semantic tokens and button component rules.
  - [x] Registers default content template for strings only if needed; keep existing string fallback if simpler.
- [x] Modify `UI/Aspect/DefaultTheme.cs`.
  - [x] Make it delegate to `DefaultAspectPackage` for modern path.
  - [x] Keep the old rule-sheet factory only until all call sites use `DefaultAspectPackage`.
- [x] Modify startup/root initialization.
  - [x] Register `DefaultAspectPackage.Create()` by default.
  - [x] Ensure playground samples still get default aspects.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~DefaultAspectPackageTests|FullyQualifiedName~ThemeTests|FullyQualifiedName~RetainedAppAspectContractTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 11 - Migration Away From Old Rule And Template APIs

- [x] Add architecture tests in `tests/Cerneala.Tests/Architecture/ModernAspectArchitectureTests.cs`.
  - [x] `DefaultThemeDoesNotCreateLegacyRuleSheetForRuntimePath`.
  - [x] `RootExposesAspectProcessorAsCanonicalProperty`.
  - [x] `AspectValueSourcesHaveDocumentedPrecedence`.
  - [x] `ProductionCodeDoesNotReferenceControlTemplateAdapterAfterMigration`.
  - [x] `ProductionCodeDoesNotReferenceDataTemplateAdapterAfterMigration`.
  - [x] `NoObsoleteAspectOrTemplateTypesRemain`.
- [x] Search references with RoslynIndexer:
  - [x] legacy rule-sheet type.
  - [x] `AspectRule`.
  - [x] `AspectSelector`.
  - [x] `ControlTemplate`.
  - [x] `TemplateContext`.
  - [x] `DataTemplate`.
- [x] Migrate production call sites:
  - [x] old default theme rule-sheet factory.
  - [x] `UIRoot` aspect initialization.
  - [x] root processor property references.
  - [x] value-source names or compatibility comments.
  - [x] playground samples.
  - [x] docs examples.
  - [x] tests that assert runtime behavior rather than old API shape.
- [x] Decide `UiPropertyValueSource` naming by actual blast radius:
  - [ ] Preferred: rename the current aspect-related value sources to canonical Aspect names in one focused phase with tests proving precedence is unchanged.
  - [x] Acceptable: keep the current enum member names as internal compatibility only, and document that public Aspect APIs must never expose those names.
  - [x] Do not invent enum values in implementation without a migration test.
- [x] Decide old API fate by actual references:
  - [x] If public API must remain for compatibility, move old classes under `UI/Aspect/Compatibility/` and document them as supported compatibility, not `[Obsolete]`.
  - [ ] If no production/tests/docs require them, delete:
    - [ ] `UI/Aspect/Aspect.cs`.
    - [ ] legacy rule-sheet file.
    - [ ] `UI/Aspect/AspectRule.cs`.
    - [ ] `UI/Aspect/AspectSelector.cs`.
    - [ ] old-only branches in `AspectApplicator`.
    - [ ] `UI/Controls/ControlTemplate.cs`.
    - [ ] `UI/Controls/ControlTemplate{TControl}.cs`.
    - [ ] `UI/Controls/TemplateContext.cs`.
    - [ ] `UI/Controls/TemplateBinding.cs` and `{T}` only if component templates replaced their use.
    - [ ] `UI/Controls/DataTemplate.cs`.
    - [ ] `UI/Controls/DataTemplate{T}.cs`.
    - [ ] all adapters added for migration.
  - [x] Do not leave `[Obsolete]` tags. The user explicitly does not want leftover trash.
- [x] Update tests to modern names only after equivalent behavior is covered.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ModernAspectArchitectureTests|FullyQualifiedName~DefaultAspectPackageTests|FullyQualifiedName~ComponentTemplateTests|FullyQualifiedName~ContentTemplateRegistryTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 12 - Playground And Developer-Facing Samples

- [x] Add tests in `tests/Cerneala.Tests/Playground/Samples/ModernAspectSampleTests.cs`.
  - [x] `ModernAspectSampleRegistersPackage`.
  - [x] `ModernAspectSampleShowsVariantsStatesTokensAndSlots`.
  - [x] `ModernAspectSampleDoesNotUseLegacyRuleSheet`.
- [x] Add `Playground/Cerneala.Playground/Samples/ModernAspectSample.cs`.
  - [x] Demo should show:
    - [x] semantic token live change.
    - [x] button variants.
    - [x] hover/pressed/focus aspect states.
    - [x] content template selected by data type.
    - [x] slot-targeted aspect change.
  - [x] Keep it practical, not a landing page.
- [x] Register sample in `SampleSelector.cs`.
- [x] Update docs:
  - [x] `docs/aspect-system.md`.
  - [x] `docs/getting-started.md` minimal modern aspect snippet.
  - [x] `docs/developer-preview-checklist.md` if it references old aspect.
- [x] Add docs tests in `tests/Cerneala.Tests/Docs/AspectDocsTests.cs`.
  - [x] Validate docs do not mention WPF/Avalonia aspect concepts as the model.
  - [x] Validate docs mention tokens, variants, slots, component templates, content templates.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~ModernAspectSampleTests|FullyQualifiedName~AspectDocsTests|FullyQualifiedName~PlaygroundSampleTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 13 - Performance And Stress Budget

- [x] Add tests in `tests/Cerneala.Tests/UI/Aspect/AspectEngineStressBudgetTests.cs`.
  - [x] `ApplyingDefaultPackageToThousandButtonsStaysWithinBudget`.
  - [x] `TokenChangeInvalidatesOnlyDependentElements`.
  - [x] `StateChangeDoesNotRecomputeUnrelatedRules`.
  - [x] `ContentTemplateLookupUsesCacheForRepeatedTypes`.
- [x] Add counters to aspect diagnostics if missing:
  - [x] rules considered.
  - [x] rules matched.
  - [x] declarations resolved.
  - [x] token lookups.
  - [x] cache hits.
  - [x] cache misses.
- [x] Add `UI/Aspect/AspectEngineCounters.cs`.
- [x] Wire counters into existing frame diagnostics if appropriate.
- [x] Run targeted tests:
  - [x] `dotnet test --filter "FullyQualifiedName~AspectEngineStressBudgetTests"`
- [x] Re-index `Cerneala.slnx` with RoslynIndexer after changes.

---

## Phase 14 - Final Verification And Cleanup

- [x] Run RoslynIndexer reference sweeps:
  - [x] No unwanted production references to the old rule-sheet runtime path.
  - [x] No unwanted production references to old `ControlTemplate` runtime path.
  - [x] No unwanted production references to old `DataTemplate` runtime path.
  - [x] No `[Obsolete]` aspect/template leftovers.
- [x] Run targeted suites:
  - [x] `dotnet test --filter "FullyQualifiedName~Aspect"`
  - [x] `dotnet test --filter "FullyQualifiedName~Template"`
  - [x] `dotnet test --filter "FullyQualifiedName~ContentPresenter"`
  - [x] `dotnet test --filter "FullyQualifiedName~ItemsControl"`
  - [x] `dotnet test --filter "FullyQualifiedName~Playground"`
- [x] Run full suite:
  - [x] `dotnet test`
- [x] Update this plan checklist as tasks complete.
- [x] Final changed-files summary must list:
  - [x] new aspect foundation files.
  - [x] new template files.
  - [x] migrated controls.
  - [x] deleted compatibility files.
  - [x] tests added/updated.
  - [x] docs/playground changes.

---

## Acceptance Criteria

- [x] The default runtime path uses `AspectPackage`, `AspectRegistry`, `AspectCatalog`, and `AspectEngine`.
- [x] Aspects can target component type, slot, state, and variant without string selector parsing.
- [x] Tokens are typed, scoped, diagnosable, and dependency tracked.
- [x] Component templates expose typed slots and required parts.
- [x] Content/data templates resolve from an explicit registry with deterministic priority.
- [x] ItemsControl can use registry content templates while preserving virtualization/recycling stability.
- [x] Diagnostics explain winning and rejected declarations, token resolution, slots, states, variants, and package/layer origin.
- [x] Default button/text/border aspect is expressed through the modern package system.
- [x] No `[Obsolete]` aspect/template/control-template/data-template leftovers remain.
- [x] All targeted tests and `dotnet test` pass.
