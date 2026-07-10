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

The source generator exposes the same runtime contract through `@template`. It is available on every markup element whose resolved type derives from `Control`; it is not available on layout-only elements such as `StackPanel`.

```xml
<Button Content="Close" Background="Transparent">
    @template
    {
        <Border Name="Chrome" Background="$owner.Background">
            @when $owner.IsMouseOver
            {
                Background = "#252B36";
            }

            <ContentPresenter Content="$owner.Content"
                              HorizontalAlignment="Center"
                              VerticalAlignment="Center" />
        </Border>
    }
</Button>
```

`$owner` is the templated Control. `$self` is the current visual element when used by a reactive directive. An unqualified observed property inside a template is shorthand for the owner property. Template parts declared with `Name` are isolated per `ComponentTemplateInstance` and are exposed through `ComponentTemplateInstance.Parts`.

Content projection is explicit. A template displays `ContentControl.Content` only when it declares a `ContentPresenter` bound to `$owner.Content`.

## Markup Aspects

Markup keeps the existing `Name`/`Target` contract. An Aspect can provide defaults, reactive values, and one modern component template:

```xml
<Aspect Name="GhostButton" Target="Button">
    @default
    {
        Background = "Transparent";
        Foreground = $Ink;
    }

    @template
    {
        <Border Background="$owner.Background">
            <ContentPresenter Content="$owner.Content" />
        </Border>
    }
</Aspect>
```

Template precedence, from lowest to highest, is: unnamed Aspect, named Aspect, inline `<Button.Aspect>`, then a direct `@template` on the element. Templates replace each other; they are never structurally merged. Aspect defaults and conditions remain active when a higher-precedence template wins.

A single `@when` may contain multiple `@if` branches. Boolean sources also support a direct shorthand body:

```xml
@when $owner.IsEnabled
{
    @if value == True { Background = "White"; }
    @if value == False { Background = "Black"; }
}
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
# Accesarea partilor unui template

O proprietate observabila a unei parti din template poate fi folosita in `@when` si ca operand in `@if`:

```xml
@when $scrollViewer.parts.$contentPresenter.IsEnabled
```

Forma este `$control.parts.$part.Property`. `parts` este un segment semantic rezervat si se scrie lowercase. Controlul si partea trebuie sa fie declarate local, iar proprietatea finala trebuie sa fie o `UiProperty` descoperita semantic. Observatia se reconecteaza automat cand `ComponentTemplate` este inlocuit.
