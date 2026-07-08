# Aspect System

Aspect is Cerneala's typed runtime design system. It owns semantic tokens, component rules, slots, variants, states, component templates, content templates, and diagnostics.

## Runtime Path

`UIRoot` creates an `AspectRegistry`, registers `DefaultAspectPackage.Create()`, and runs `AspectProcessor` during the style phase. A legacy style sheet can still be set explicitly for compatibility, but the default path is `AspectPackage` -> `AspectCatalog` -> `AspectEngine`.

```csharp
UIRoot root = new(800, 600);
root.AspectRegistry.Register(AppAspectPackage.Create());
```

## Tokens

Tokens are typed values:

```csharp
public static readonly AspectToken<DrawColor> Accent =
    AspectToken.Color("app.accent");
```

Rules use `AspectRef.To(Accent)` so diagnostics can report token dependencies and resolution.

## Variants And States

Variants describe component modes with typed keys. States describe runtime interaction.

```csharp
new AspectTarget(
    typeof(Button),
    conditions:
    [
        AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Primary),
        AspectCondition.State(AspectState.Hover)
    ]);
```

## Slots

Component templates register slots so aspects can target generated parts without string selector parsing.

```csharp
context.RegisterSlot(ButtonSlots.Content, presenter);
```

## Component Templates

Component templates build retained controls and expose slots, required parts, bindings, and token bindings. Prefer `ComponentTemplate` for new control chrome.

```csharp
button.ComponentTemplate = ButtonTemplates.Modern;
```

## Content Templates

Content templates live in a `ContentTemplateRegistry` and resolve by explicit key, predicate priority, exact type, nearest assignable type, then registration order.

```csharp
ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<UserCard>(
    "user-card",
    key: null,
    priority: 10,
    context => new TextBlock { Text = context.Data.Name }));
```

## Diagnostics

Use `AspectTrace` and `AspectDiagnostics` to inspect winning declarations, rejected declarations, layer, specificity, package origin, slots, variants, states, and token traces.
