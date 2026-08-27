# Plan: component templates declared directly in the markup

> Historical note (2026-08-27): the direct `MarkupAspectResource`/`LocalAspectBase` runtime described here is superseded by `docs/plans/2026-08-27-unify-aspect-runtime.md`. Template syntax remains supported, but active implementation guidance is the unified `AspectPackage`/`ElementAspect` -> `AspectProcessor` -> `AspectEngine` path.

**Date:** 2026-07-10
**Status:** Implemented and verified
**Purpose:** Extend the Cerneala markup so that any element derived from `Control` can declare a local `@template`, and a `Aspect` can provide the same modern type of `ComponentTemplate`.

## Summary

We want to allow the local form:
```xml
<Button Content="Close">
    @template
    {
        <Border Name="Bd"
                Background="$owner.Background"
                CornerRadius="6">
            @when $owner.IsMouseOver
            {
                Background = "#252B36";
            }

            <ContentPresenter Content="$owner.Content"
                              HorizontalAlignment="Center"
                              VerticalAlignment="Center"/>
        </Border>
    }
</Button>
```
and the same capacity in an Aspect:
```xml
<Aspect Name="TitleBarButton" TargetType="Button">
    @default
    {
        Width = 28;
        Height = 28;
        Background = "Transparent";
        Foreground = $InkDim;
    }

    @template
    {
        <Border Name="Bd"
                Background="$owner.Background"
                CornerRadius="6">
            @when $owner.IsMouseOver
            {
                Background = "#252B36";
            }

            <ContentPresenter Content="$owner.Content"
                              HorizontalAlignment="Center"
                              VerticalAlignment="Center"/>
        </Border>
    }
</Aspect>
```
The generator will transform the statement into a modern `ComponentTemplate<TControl>`. We do not introduce a second infrastructure of templates and we do not revive legacy APIs.

## Decisions that must be confirmed at the review

1. **The Aspect API remains unchanged: `Name` + `Target`.** The implementation adds `@template` on top of the existing contract and does not rename attributes or runtime members.
2. **`@template` is only available on `Control`.** A `StackPanel`, for example, derives from `UIElement`, not `Control`, so it gets diagnostic.
3. **Inside the template, `$owner` is the templated control, and `$self` is the current visual element.** An unqualified property from a `@when`, such as `IsMouseOver`, is shorthand for `$owner.IsMouseOver` in this context only.
4. **The content is not designed by default.** If the template must display `Button.Content`, the author explicitly writes a `ContentPresenter Content="$owner.Content"`.
5. **Templates cannot be combined.** The source with the highest precedent wins; `@default` and the conditions of the Aspect continue to apply.
6. **A `@template` directly at the root of a paired `UserControl` defines the body of the already generated template.** We do not assign a second `ComponentTemplate` above the one used by the generator for that `UserControl`.

## Semantic behavior

### Eligibility

- `@template` can appear directly in the content of an element whose resolved type derives from `Cerneala.UI.Controls.Control`.
- The rule is determined semantically by Roslyn, not by a hardcoded catalog of controls.
- Custom controls are automatically accepted if they derive from `Control`.
- An element can have at most one direct `@template`.
- A template contains exactly one root element that derives from `UIElement`.
- The raw text, assignments and several root elements directly below `@template` are invalid.
- `@template` cannot appear directly in a `@when` or `@if` in the first version. The dynamic change of the template remains outside the scope.
- A Control inside another template can declare its own `@template`; this case must work recursively.

### The content of the control

- The `@template` directive is consumed by the compiler and does not become `Content` or a visual child.
- A `Button` can simultaneously have a `@template` and normal content.
- Normal content continues to be assigned to the `Content` property according to existing rules.
- The template decides if and where it projects that content through an explicit `ContentPresenter`.
- We do not introduce default design, default slot or a secretly generated `ContentPresenter`.

### The field of expressions

- `$owner.Property` refers to the Control instance for which the template is applied.
- `$self.Property` refers to the current visual element in which the reactive directive appears.
- Inside a template, `@when IsMouseOver` is equivalent to `@when $owner.IsMouseOver`.
- Outside of a template, unqualified expressions keep their current semantics: the current element.
- `$ResourceName` keeps its lexical resource semantics.
- The names `$owner` and `$self` become reserved and cannot be used as resource or element identities.
- Arbitrary access `$ElementName.Property` inside the template does not enter the first stage; the parts are accessed through `ComponentTemplateInstance.Parts` at runtime.

### Template binding

An attribute such as:
```xml
<Border Background="$owner.Background"/>
```
will be issued as a binding made by `ComponentTemplateContext.Bind`, not as a value copied only once:
```csharp
templateContext.Bind(
    Control.BackgroundProperty,
    border,
    Control.BackgroundProperty);
```
The generator must validate semantically:

- the existence of the property on the owner;
- the existence of the target property on the element;
- type compatibility;
- the possibility to write the target property;
- the use of a reactive property compatible with `Bind`.

In the first version we accept direct property-to-property binding. We do not add converters, arithmetic expressions, nested paths or bidirectional binding.

### Reactive conditions in templates

The abbreviated boolean form becomes valid:
```xml
@when $owner.IsMouseOver
{
    Background = "#252B36";
}
```
It is semantically equivalent to the old explicit form for the true boolean case. The parser must not invent a false `@if` in the AST; the emitter validates that the observed source is boolean.

The existing form remains valid for values ​​with several branches:
```xml
@when Status
{
    @if value == "Ready"
    {
        Foreground = "Green";
    }

    @if value == "Failed"
    {
        Foreground = "Red";
    }
}
```
Conditional values ​​take precedence over template binding as long as the condition is active. When the condition becomes false, the conditional value is removed and the binding to `$owner.Background` becomes visible again.

### Names and parts of templates

- `Name="Bd"` from an inline template does not generate a field on the external code-behind.
- Each such name is registered through `ComponentTemplateContext.RequirePart`.
- Parts are available on `ComponentTemplateInstance.Parts`.
- The namespace of the template parts is separated from the namespace of the external document.
- Duplicate names in the same template produce a diagnosis.
- The same template can be instantiated several times without the parts of an instance overlapping each other.
Intentional exception: The content generated for the root of a `UserControl` paired preserves the current behavior for the named members of that UserControl. Controls with inline templates nested in that content, however, use the isolated parts space.

## Aspects and precedent

The Aspect syntax remains the existing one:
```xml
<Aspect TargetType="TextBlock">
    ...
</Aspect>

<Aspect Name="TitleBarButton" TargetType="Button">
    ...
</Aspect>
```
- `Target` describes the target type.
- `Name` identifies a referable Aspect.
- A Skin without `Name` remains the default Skin for the type.
- The element reference remains `Aspect="$TitleBarButton"`.
- `MarkupAspectResource.Name` and the existing internal model remain unchanged.
- The implementation of `@template` does not produce any incompatible changes in the Aspect API.

The precedence order for `ComponentTemplateProperty` is:

1. Default layout for type;
2. Aspect referenced by `Aspect="$Name"`;
3. Local aspect declared by `<Button.Aspect>`;
4. `@template` declared directly on Control.

A source with a larger precedent completely replaces the previous template. The properties `@default` and the values ​​`@when` from Aspect remain active; only the value `ComponentTemplate` is overwritten.

A template declared in an Aspect is compiled only once in the scope of the resource and is stored as a value for `Control.ComponentTemplateProperty`. For an inline Aspect, the value is applied by `ElementAspect` at the `LocalAspectBase` level. For a direct `@template`, the generator uses the normal local setter.

## The proposed architecture

### 1. AST of directives

We extend `UiMarkupDirectiveParser` with:
```csharp
internal sealed record DirectiveTemplateNode(
    XElement Root,
    SourceLocation Source) : DirectiveNode;
```
`DirectiveWhenNode` must be able to represent separately:

- explicit branches `@if`;
- a direct boolean body.

The parser receives an explicit context of capabilities, not a sequence of hard-to-follow bools. For example:
```csharp
[Flags]
internal enum DirectiveContentKind
{
    Elements = 1,
    Assignments = 2,
    Templates = 4
}
```
Nesting rules are validated in the parser where they are strictly grammatical. Type eligibility and property compatibility remain in the semantic phase.

### 2. The semantic model of the template

We introduce a small internal model, separate from the raw XML:
```csharp
internal sealed record TemplateDeclaration(
    XElement Root,
    ITypeSymbol OwnerType,
    string GeneratedName,
    TemplateOrigin Origin,
    SourceLocation Source);
```
`TemplateOrigin` distinguishes only the cases necessary for issuance and precedent:

- `AspectResource`;
- `InlineAspect`;
- `DirectElement`;
- `PairedUserControlRoot`.

We don't build an unnecessary extensible hierarchy and we don't move the runtime logic into the generator.

### 3. Issue context

`GenerationScope` receives a stack of contexts for templates. The current context contains:

- variable `ComponentTemplateContext<TControl>`;
- the expression of the owner;
- the symbol of the owner type;
- the namespace of the parties;
- the indicator that the current emission is in a template;
- the current visual element used by `$self`.
A reusable helper is needed for the temporary change of buffers `currentLines` and `currentPostLines`. The existing code in the conditional emitter already does this; we extract it in a safe scope and reuse it for templates, including nested templates.

### 4. Issuing a direct template

For a normal Button, the generated form will be equivalent to:
```csharp
button.ComponentTemplate = new ComponentTemplate<Button>(
    "Inline.Button.<locatie-determinista>",
    templateContext0 =>
    {
        Border border0 = new();
        templateContext0.RequirePart("Bd", border0);
        templateContext0.Bind(
            Control.BackgroundProperty,
            border0,
            Control.BackgroundProperty);
        return border0;
    });
```
The name of the template is deterministic, based on the type, origin and stable position in the document. We do not use GUIDs, so that the generated source and snapshots remain clean.

Lambda is not forced `static`: event handlers and generated resources may need the code-behind instance. The generator can issue `static` only when it proves that there are no captures; this optimization is not necessary in the first implementation.

### 5. Integration with the property emitter

`EmitProperty` remains responsible for existing literal values. We add a template-aware branch for expressions `$owner.Property` and, where relevant, `$self.Property`.

The solution must use the existing semantic infrastructure:

- `ResolveElementTypeSymbol`;
- `ResolvePropertyOwnerType`;
- `FindPropertySpec`;
- `IsOrDerivesFrom`.

We do not add lists of known types or properties manually.

### 6. Integration with the reactive emitter

`UiMarkupReactiveEmitter` receives the explicit resolution of the observed source:

- `$owner` -> the owner of the template;
- `$self` -> the current element;
- unqualified in template -> owner;
- unqualified outside the template -> current element;
- `$DataContext` and existing resources -> current behavior.

The subscriptions created for `@when` in templates must be owned by the template instance and removed when it is replaced or removed. We do not accept an implementation that works visually, but leaves subscriptions hanging after it.

### 7. Paired UserControl and Window

For a paired root `UserControl`:

- `@template` provides the body of `__CernealaGeneratedTemplate` already created by the generator;
- no additional assignment is issued to `ComponentTemplate`;
- `$owner` is the instance of that UserControl;
- there cannot be a visual child directly on the wrapper at the same time, because both would define the root of the template;
- nested controls behave normally.

For a `Window` root:

- `Window` can receive a local `ComponentTemplate` like any other Control;
- the direct visual child continues to be `Window.Content`;
- the two can coexist;
- the template must explicitly design the content if it wants to display it.

## Targeted files

### Generator

- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`
  - AST for `@template`;
  - direct boolean body for `@when`;
  - grammatical rules and nesting.
- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- keeping the contract Aspect `Name`/`Target`;
  - semantic template model;
  - validation `Control`;
  - issuing `ComponentTemplate<TControl>`;
  - template binding;
  - previous Aspect/direct;
  - named parts and nested scopes.
- `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`
  - `$owner`, `$self` and Boolean abbreviation;
  - correct lifecycle for the conditions in the template.
- `Cerneala.SourceGen/UiMarkupUserControlGenerator.cs`
  - special semantics for the paired root.
- `Cerneala.SourceGen/UiMarkupWindowGenerator.cs`
  - local template on Windows and coexistence with Content.

### Runtime

- `Cerneala/UI/Markup/MarkupAspectResource.cs`
  - no API renaming;
  - adjustments only if they are necessary for storing the modern template in Aspect.
- `Cerneala/UI/Styling/ElementAspect.cs`
  - only the strictly necessary adjustments for the `ComponentTemplateProperty` value; a new template abstraction is not introduced.
- Infrastructure `ComponentTemplate*`
  - changes only if the lifecycle tests prove that the subscriptions or parts are not cleaned correctly.

### Playground and documentation

- `Playground/Cerneala.Playground/MainWindow.crn`
  - keeping Aspects with `Name`/`Target`;
  - a real example of `Button` with `@template`, hover and `ContentPresenter`.
- `docs/aspect-system.md`
  - the new syntax and the previous one.
- `docs/getting-started.md`
  - minimal example of direct templates.
- `docs/developer-preview-scope.md`
  - explicit capabilities and limitations.
- API documentation for `MarkupAspectResource`, only if the template support requires clarification; `Name` remains unchanged.

## Implementation stages

### Stage 1: RED tests for grammar and contract

- [ ] Add tests for parsing a valid `@template`.
- [ ] Add tests for `@when` with direct boolean body.
- [ ] Add test for a `@when` from templates with multiple branches `@if`.
- [ ] Add tests for zero, one and more roots.
- [ ] Add tests for duplicate templates and conditional templates.
- [ ] Add test that proves that `StackPanel` is semantically rejected.
- [ ] Add test that proves that a custom Control discovered through Roslyn is supported.
- [ ] Confirm that the tests fail for the reason expected before the production code.

### Stage 2: parser and AST

- [ ] Add `DirectiveTemplateNode`.
- [ ] Replaces the current combination of boolean flags with a readable context of allowed content.
- [ ] Extends `DirectiveWhenNode` for direct boolean body.
- [ ] Keeps its shape with compatible `@if`.
- [ ] Issue grammatical errors with the correct XML position.
- [ ] Run the parser tests and reindex the solution.

### Stage 3: preserving the Aspect contract

- [ ] Keeps the reading of `Name` and `Target` attributes without syntax changes.
- [ ] Keep `MarkupAspectResource.Name` and the existing internal models.
- [ ] Add regression tests for Default Layout and Named Layout with current syntax.
- [ ] Confirm that adding a `@template` does not change the `Aspect="$Name"` solution.
- [ ] Re-indexes the solution after each coherent group of changes.

### Stage 4: issuing the minimal local template

- [ ] Detects and extracts `@template` before issuing normal content.
- [ ] Validates semantically that the owner derives from `Control`.
- [ ] Enter `TemplateDeclaration` and the broadcast context.
- [ ] Extract the safe helper for generated code buffers.
- [ ] Issue `ComponentTemplate<TControl>` with deterministic name and a root `UIElement`.
- [ ] Allows normal Content next to the directive without the directive becoming Content.
- [ ] Supports nested local templates.
- [ ] Inspect the generated source for stable and readable code.

### Stage 5: `$owner`, `$self` and template bindings

- [ ] Reserve the identifiers `$owner` and `$self`.
- [ ] Semantically resolves the owner and target properties.
- [ ] Issue `ComponentTemplateContext.Bind` for attributes `$owner.Property`.
- [ ] Add `$self.Property` support where the expression is allowed.
- [ ] Reports diagnosis for non-existent property, incompatible type or non-writable target.
- [ ] Keeps the lexical lookup for `$ResourceName`.
- [ ] Do not add default conversions or nested paths.

### Stage 6: state reagents in templates

- [ ] Extends the reactive plane with the owner and the current element.
- [ ] Implements `@when $owner.BoolProperty`.
- [ ] Implements the shorthand `@when BoolProperty` in templates.
- [ ] Implements `@when $self.BoolProperty`.
- [ ] Check the return from the conditional value to template binding.
- [ ] Checks the detachment of subscriptions when the template is replaced or reapplied.

### Stage 7: templates in Aspect

- [ ] Allows a single `@template` in the body of an Aspect.
- [ ] Compiles the template in the lexical scope of the resource.
- [ ] Stores the value in `Control.ComponentTemplateProperty` through the existing Aspect mechanism.
- [ ] Apply the previous documented order.
- [ ] Confirm that the template override does not remove `@default` or `@when` from the Aspect.
- [ ] Allows inline `<Control.Aspect>` templates as well.

### Stage 8: names and parts

- [ ] Register `Name` through `RequirePart` for normal inline templates.
- [ ] Detects duplicates in the same template.
- [ ] Does not emit external code-behind fields for template parts.
- [ ] Checks the isolation of the parts between two instances of the same template.
- [ ] Keeps the current contract for paired UserControl members.

### Stage 9: special roots
- [ ] Integrates the `@template` body of a paired UserControl into the existing generated template.
- [ ] Reports the conflict between that body and a direct visual child of the wrapper.
- [ ] Allows `@template` on Windows without removing the normal Content.
- [ ] Check `$owner`, event handlers and resources in both cases.

### Stage 10: Playground and documentation

- [ ] Keep and check the Playground `Name`/`Target` syntax.
- [ ] Add a demonstration Button with direct templates, content and hover.
- [ ] Documents syntax, scope, precedent and limitations.
- [ ] Includes examples for templates directly, Default Layout, Named Layout and Inline Layout.
- [ ] It explicitly says that `StackPanel` is not templatable because it does not derive from `Control`.
- [ ] Regenerates `FileTree.md` if the structure of the documentation has changed.

## Test matrix

### Source generator

- [ ] Button with `@template` emits `ComponentTemplate<Button>` and the correct root.
- [ ] The directive is not treated as Content.
- [ ] Template and `Content` attribute can coexist.
- [ ] Template and normal Content child can coexist.
- [ ] `$owner.Background` issues binding and follows the change of owner.
- [ ] `$owner.Content` explicitly feeds `ContentPresenter`.
- [ ] `@when $owner.IsMouseOver` applies and removes the conditional value.
- [ ] A single `@when` accepts several `@if` branches, each with its own assignments and conditional elements.
- [ ] `@when IsMouseOver` uses the owner in templates.
- [ ] `@when $self.IsMouseOver` uses the current part.
- [ ] `$InkDim` continues to solve the lexical resource.
- [ ] `Name="Bd"` becomes part, not outer field.
- [ ] A nested Control can have its own `@template`.
- [ ] A default Layout can provide templates.
- [ ] A Skin with `Name` can provide templates.
- [ ] `<Button.Aspect>` can provide templates.
- [ ] The direct template wins over the template in Aspect.
- [ ] `@default` and `@when` from Aspect remain applied after override.
- [ ] Paired UserControl uses a single generated template.
- [ ] Window keeps the normal Content with local template.
- [ ] Generated code compiles without warnings.

### Diagnostics

- [ ] `@template` on `StackPanel`.
- [ ] Two `@template` on the same Control.
- [ ] Template without root.
- [ ] Template with two roots.
- [ ] Text or assignment directly to the root of the template.
- [ ] Template declared in a conditional.
- [ ] `$owner.UnknownProperty`.
- [ ] `$self.UnknownProperty`.
- [ ] Binding with incompatible types.
- [ ] Duplicate party name.
- [ ] Conflict on the paired UserControl root.
- [ ] An Aspect with `Name`/`Target` continues to compile and resolve correctly.
Grammatical errors may continue to use existing diagnostics for invalid directives. For the semantic template contract, we introduce a dedicated diagnosis, for example `CERNEALAUI012`, with a specific message and exact location. We do not cram all cases into a generic "invalid template" type message.

### Runtime and lifecycle

- [ ] Changing the owner property updates the linked part.
- [ ] Replacing the template detaches the bindings of the old instance.
- [ ] Re-applying the template does not duplicate subscriptions.
- [ ] The parts of the old court are not accessible after the replacement.
- [ ] Two controls with the same template have independent part dictionaries.
- [ ] The reactive condition returns to the binding value after deactivation.
- [ ] The local source `ComponentTemplate` wins over the Aspect sources according to the previous one.

## Final check

1. Run the targeted tests of the parser and the generator.
2. Run the runtime tests for `ComponentTemplate`, binding and lifecycle.
3. Run the entire suite with `dotnet test Cerneala.slnx --no-restore`.
4. Run the complete build without warnings or errors.
5. Run the formatter in verification mode.
6. Inspect the generated source for the Playground examples.
7. Start the Playground and check manually:
   - the content of the Button;
   - the hover;
   - change of owner properties;
   - opening several windows;
   - reapplying/replacing the template.
8. Run `git diff --check`.
9. Regenerates `FileTree.md`.
10. Reindex `Cerneala.slnx` with RoslynIndexer and confirm that the index is healthy.

## Non-targets for the first version

- Templates on any `UIElement` that do not derive from `Control`.
- Template switching from `@when` or `@if`.
- The structural combination of two templates.
- `ContentPresenter` or slot generated by default.
- Binding converters.
- Bidirectional binding.
- Horses like `$owner.User.Profile.Name`.
- Arbitrary expressions in attributes.
- Direct access `$PartName.Property` between the parts of the template.
- New triggers, animations or visual states.
- A new parallel runtime class with `ComponentTemplate`.

## Risks and measures

### Wrong scope for expressions

The biggest semantic risk is that an unqualified expression accidentally notices the visual part instead of the control. The template context must make this choice explicitly, and the tests for `$owner`, `$self` and the unqualified form must exist separately.

### Reactive subscriptions left after templates

A template can be re-instantiated many times. All bindings and conditions created by the factory must be owned and removed together with `ComponentTemplateInstance`. The replacement and reapplication tests are blocking for going.

### Names transformed into global fields
If the names in the templates use the usual code-behind mechanism, two instances will be overwritten. Normal templates must use `Parts` exclusively; the paired UserControl exception remains limited and explicitly tested.

### Regressions in Content

The directive must be removed from the child stream before the existing rules for Button, Border, Panel and Window. The tests must cover both the Content attribute and the direct visual child.

### Regressions in the Aspect contract

`@template` must be added without changing `Name`, `Target`, `Aspect="$Name"` or existing runtime metadata. The regression tests for Aspect are blocking, because here we are not doing renovation with the sledgehammer in an API that already works.

## Acceptance criteria

The implementation is ready only when:

- the direct syntax and that in Aspect produce the same modern `ComponentTemplate`;
- any semantically discovered Custom Control can use `@template`;
- a non-Control receives a clear diagnosis;
- `$owner`, `$self`, resources and conditions have a deterministic scope;
- The content is kept and designed only explicitly;
- the previous Aspect/direct is proven by tests;
- the parties are isolated per instance;
- replacing the template does not leave bindings or old subscriptions;
- The Playground demonstrates the end-to-end flow;
- the documentation and all examples keep the `Name`/`Target` contract;
- the build and the entire suite of tests are clean.

## Evidence of implementation

- `dotnet build Cerneala.slnx --no-restore`: 0 warnings, 0 errors.
- `dotnet test Cerneala.slnx --no-restore`: 1570 runtime/documentation tests and 92 source-generator tests, all green.
- `dotnet format Cerneala.slnx --no-restore --verify-no-changes`: clean.
- `git diff --check`: clean.
- The Playground generated source contains `ComponentTemplate<Button>`, `RequirePart`, owner bindings and `RegisterLifetime` for the reactive conditions.
- The compiled playground starts, creates the native window `Cerneala generator playground` and remains responsive.
- RoslynIndexer reports valid index, without dirty files or warnings.
