# Markup Aspect Resources Design

## Context

Cerneala currently has a small XML-based UI markup source generator that consumes `.cui.xml` additional files and emits code-first factories. The supported element/property surface is intentionally narrow and does not include aspect resources or aspect references.

This design adds authoring syntax for aspect resources in markup without exposing low-level variant mechanics in the markup surface.

## Goals

- Allow markup authors to declare reusable aspect resources.
- Allow a type-wide default aspect for all elements of a given type.
- Allow a named aspect to be applied explicitly to an element.
- Preserve a clear cascade order that is easy to reason about.
- Keep invalid references and type mismatches as generator errors.

## Non-Goals

- Do not expose `AspectVariantKey` or `Variant.Kind` style syntax in markup.
- Do not add arbitrary C# execution inside markup.
- Do not support implicit fallback when an aspect type does not match the target element.
- Do not expand the full Cerneala aspect engine surface in this first pass.

## Markup Syntax

Aspect resources are declared inside a top-level `Resources` element.

```xml
<Resources>
  <SolidColorBrush Key="TextColor" Color="#1E293B" />
  <SolidColorBrush Key="PulseColor" Color="#FF5D73" />

  <Aspect Type="TextBlock">
    @default
    {
      FontFamily = "Segoe UI";
      FontSize = 14;
      Foreground = $TextColor;
    }
  </Aspect>

  <Aspect Key="KickerText" Type="TextBlock">
    @default
    {
      FontFamily = "Consolas";
      FontSize = 12;
      Foreground = $PulseColor;
      Margin = "0,0,0,12";
    }
  </Aspect>
</Resources>

<TextBlock Aspect="$KickerText" Text="HELLO" />
```

`Aspect Type="TextBlock"` without a `Key` defines the implicit default aspect for every `TextBlock` in the document.

`Aspect Key="KickerText" Type="TextBlock"` defines a named aspect resource. It is not applied automatically. Elements opt into it with `Aspect="$KickerText"`.

`SolidColorBrush Key="PulseColor"` defines a reusable brush resource. Property values inside aspect declaration bodies can reference it with `$PulseColor`.

`Aspect="$KickerText"` resolves only against named `Aspect` resources. Declaration values such as `Foreground = $PulseColor;` resolve only against non-aspect resources. The same `$` prefix is intentionally context-sensitive.

## Cascade

The cascade order is:

1. Type default aspect, if one exists for the element type.
2. Explicit named aspect from the element `Aspect` attribute, if present.
3. Local element attributes and text/content assignments.

For example:

```xml
<TextBlock Aspect="$KickerText" FontSize="20" Text="HELLO" />
```

The final value resolution is:

- `FontFamily = "Consolas"` from `KickerText`.
- `FontSize = 20` from the local element attribute.
- `Foreground = $PulseColor` from `KickerText`.
- `Margin = "0,0,0,12"` from `KickerText`.
- `Text = "HELLO"` from the local element attribute.

Local element attributes always win over aspect declarations.

## Validation

The generator reports an error when:

- An `Aspect` resource is missing `Type`.
- A named aspect has an empty `Key`.
- Two keyless aspects target the same `Type` in the same document.
- Two keyed resources have the same `Key` in the same document.
- An element references an unknown aspect key.
- An element references an aspect whose `Type` does not match the element type.
- An aspect declaration references an unknown non-aspect resource key.
- An aspect declaration references a resource that cannot be assigned or coerced to the target property type.
- An aspect declaration assigns an unsupported property for its target type.
- An aspect declaration value cannot be parsed using the same typed rules as element attributes.
- A `SolidColorBrush` resource has an invalid or missing `Color`.

Type mismatch example:

```xml
<Resources>
  <Aspect Key="KickerText" Type="TextBlock">
    @default
    {
      FontSize = 12;
    }
  </Aspect>
</Resources>

<Button Aspect="$KickerText" />
```

This is a generator error because `KickerText` targets `TextBlock`, not `Button`.

## Scope

The first implementation should support document-level resources. Scoped/nested `Resources` can be added later if needed, but the syntax should not prevent it.

For document-level resources:

- Keyless type defaults apply to all matching elements in the document.
- Keyed resource names are unique within the document, across both named aspects and non-aspect resources.
- Named aspect references use `$KeyName`.
- Nested `Resources` declarations are rejected with a clear diagnostic in the first implementation.

## Generation Model

The generator should parse resources before emitting elements. For each element:

1. Emit the element construction.
2. Apply declarations from the keyless default aspect for that element type.
3. Apply declarations from the named aspect referenced by `Aspect="$Key"`, if present.
4. Apply normal element attributes and content.
5. Emit children using the existing child rules.

The generated code should continue to use public typed properties directly where possible, matching the current generator style.

## Resource Declarations

The first implementation supports `SolidColorBrush` resources:

```xml
<SolidColorBrush Key="PulseColor" Color="#FF5D73" />
```

`Color` accepts hex color syntax:

- `#RRGGBB`, where alpha defaults to `FF`.
- `#AARRGGBB`, where alpha is explicit.

The generated representation should use the existing `Cerneala.UI.Media.SolidColorBrush` type.

## Aspect Declaration Values

String literals use quotes:

```text
FontFamily = "Consolas";
Margin = "0,0,0,12";
```

Numbers use invariant culture:

```text
FontSize = 12;
```

Resource references use `$Name`:

```text
Foreground = $PulseColor;
```

Resource references are resolved by declaration context. For example, `Foreground = $PulseColor` targets `Control.ForegroundProperty`, whose runtime type is `DrawColor`. If `PulseColor` is a `SolidColorBrush`, the generator uses its solid color value for `DrawColor` properties.

For a `Brush` property, the same reference would resolve to the `SolidColorBrush` resource itself. For a `DrawColor` property, only resources with a solid color are accepted. A gradient brush or incompatible resource used for a `DrawColor` property is a generator error.

Resource references never silently become strings.

## Tests

Add focused source generator tests for:

- A keyless `Aspect Type="TextBlock"` applying to every `TextBlock`.
- A named aspect applying after the type default.
- Local attributes overriding named aspect values.
- Unknown aspect key diagnostic.
- Unknown non-aspect resource key diagnostic.
- Aspect type mismatch diagnostic.
- Duplicate default aspect diagnostic.
- Duplicate keyed resource diagnostic.
- Unsupported property inside an aspect diagnostic.
- `SolidColorBrush` resource declaration.
- Invalid `SolidColorBrush.Color` diagnostic.
- Resource reference from an aspect declaration to a `SolidColorBrush`.
- Solid brush resource coerced to `DrawColor` for `Control.Foreground`.
- Incompatible resource reference diagnostic.
- Nested `Resources` rejected diagnostic.
