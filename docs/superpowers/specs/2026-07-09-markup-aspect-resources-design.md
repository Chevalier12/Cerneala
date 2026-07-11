# Markup Aspect Resources Design

## Context

Cerneala currently has a small XML-based UI markup source generator that consumes `.cui.xml` additional files and emits code-first factories. The supported element/property surface is intentionally narrow and does not include aspect resources or aspect references.

This design adds authoring syntax for aspect resources in markup without exposing low-level variant mechanics in the markup surface.

## Goals

- Allow markup authors to declare reusable aspect resources.
- Allow a type-wide default aspect for all elements of a given type.
- Allow a named aspect to be applied explicitly to an element.
- Use one reference identity concept: `Name`.
- Preserve a clear cascade order that is easy to reason about.
- Keep invalid references and target mismatches as generator errors.

## Non-Goals

- Do not expose `AspectVariantKey` or `Variant.Kind` style syntax in markup.
- Do not add arbitrary C# execution inside markup.
- Do not support implicit fallback when an aspect type does not match the target element.
- Do not expand the full Cerneala aspect engine surface in this first pass.

## Markup Syntax

Aspect resources are declared inside a top-level `Resources` element.

```xml
<Resources>
  <SolidColorBrush Name="TextColor" Color="#1E293B" />
  <SolidColorBrush Name="PulseColor" Color="#FF5D73" />

  <Aspect Target="TextBlock">
    @default
    {
      FontFamily = "Segoe UI";
      FontSize = 14;
      Foreground = $TextColor;
    }
  </Aspect>

  <Aspect Name="KickerText" Target="TextBlock">
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

Elements may also declare a `Name` for reference purposes:

```xml
<TextBlock Name="KickerLabel" Aspect="$KickerText" Text="HELLO" />
```

`Aspect Target="TextBlock"` without a `Name` defines the implicit default aspect for every `TextBlock` in the document.

`Aspect Name="KickerText" Target="TextBlock"` defines a named aspect resource. It is not applied automatically. Elements opt into it with `Aspect="$KickerText"`.

`SolidColorBrush Name="PulseColor"` defines a reusable brush resource. Property values inside aspect declaration bodies can reference it with `$PulseColor`.

`$Identifier` is the general markup reference syntax. It refers to a `Name`; the consuming context decides which named target kinds are valid.

For example, `Aspect="$KickerText"` requires the referenced symbol to be an `Aspect` resource. `Foreground = $PulseColor;` requires the referenced symbol to be assignable or coercible to the `Foreground` property type.

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

- An `Aspect` resource is missing `Target`.
- A named resource or element has an empty `Name`.
- Two unnamed aspects have the same `Target` in the same document.
- Two named items use the same `Name` in the same document.
- A `$Identifier` reference cannot be resolved to a `Name`.
- A `$Identifier` reference resolves to a symbol that is not valid for the consuming context.
- An element references an aspect whose `Target` does not match the element type.
- An aspect declaration references a resource that cannot be assigned or coerced to the target property type.
- An aspect declaration assigns an unsupported property for its target type.
- An aspect declaration value cannot be parsed using the same typed rules as element attributes.
- A `SolidColorBrush` resource has an invalid or missing `Color`.

Target mismatch example:

```xml
<Resources>
  <Aspect Name="KickerText" Target="TextBlock">
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

- Unnamed type defaults apply to all matching elements in the document.
- Elements and resources share one document-level `Name` namespace.
- `$Identifier` references resolve only through that `Name` namespace.
- Nested `Resources` declarations are rejected with a clear diagnostic in the first implementation.

## Generation Model

The generator should parse resources before emitting elements. For each element:

1. Emit the element construction.
2. Apply declarations from the unnamed default aspect for that element type.
3. Apply declarations from the named aspect referenced by `Aspect="$Name"`, if present.
4. Apply normal element attributes and content.
5. Emit children using the existing child rules.

The generated code should continue to use public typed properties directly where possible, matching the current generator style.

The generator should also build a document symbol table before emitting element references:

- Element `Name` entries identify generated element variables.
- Resource `Name` entries identify generated resource values or aspect resources.
- Duplicate `Name` identifiers are diagnostics.

## Resource Declarations

The first implementation supports `SolidColorBrush` resources:

```xml
<SolidColorBrush Name="PulseColor" Color="#FF5D73" />
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

References use `$Name`:

```text
Foreground = $PulseColor;
```

References are resolved by declaration context. For example, `Foreground = $PulseColor` targets `Control.ForegroundProperty`, whose runtime type is `Color`. If `PulseColor` is a named `SolidColorBrush`, the generator uses its solid color value for `Color` properties.

For a `Brush` property, the same reference would resolve to the `SolidColorBrush` resource itself. For a `Color` property, only resources with a solid color are accepted. A gradient brush or incompatible resource used for a `Color` property is a generator error.

Named element references use the same `$Name` syntax, but only contexts that accept an element reference may consume them. A reference never silently becomes a string.

## Tests

Add focused source generator tests for:

- An unnamed `Aspect Target="TextBlock"` applying to every `TextBlock`.
- A named aspect applying after the type default.
- Local attributes overriding named aspect values.
- Element `Name` registration.
- Resource `Name` registration.
- `$Identifier` resolving to a named resource.
- `$Identifier` resolving to a named element in a context that accepts element references.
- Unknown `$Identifier` diagnostic.
- Duplicate `Name` diagnostic across elements/resources.
- Aspect target mismatch diagnostic.
- Duplicate default aspect diagnostic.
- Unsupported property inside an aspect diagnostic.
- `SolidColorBrush` resource declaration.
- Invalid `SolidColorBrush.Color` diagnostic.
- Resource reference from an aspect declaration to a `SolidColorBrush`.
- Solid brush resource coerced to `Color` for `Control.Foreground`.
- Incompatible resource reference diagnostic.
- Nested `Resources` rejected diagnostic.
