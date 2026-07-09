# DefaultAspectTokens.Spacing Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Provides the built-in `Thickness` aspect tokens used for default Cerneala UI spacing values.

```csharp
public static class DefaultAspectTokens
{
    public static class Spacing
}
```

Inheritance:
`object` -> `DefaultAspectTokens.Spacing`

## Examples

Read the default control padding from the environment created by `DefaultAspectPackage`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Spacing.ControlPadding, out Thickness padding))
{
    // padding is new Thickness(8).
}
```

Use the spacing token as an aspect value reference:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectDeclaration declaration = new(
    Control.PaddingProperty,
    DefaultAspectTokens.Spacing.ControlPadding.Ref());
```

## Remarks

`DefaultAspectTokens.Spacing` groups the framework's built-in spacing tokens. Each field is an `AspectToken<Thickness>` created with `AspectToken.Thickness(string)`, so token values use `Cerneala.UI.Layout.Thickness`.

The class defines token identities only. Default values are assigned by `DefaultAspectPackage.Create()` when building the package token set and by `DefaultAspectPackage.CreateEnvironment()` when creating an `AspectEnvironment`.

The default package sets `ControlPadding` to `new Thickness(8)` in both the default package and the default environment. The default package tests assert that `DefaultAspectTokens.Spacing.ControlPadding` is registered as a core semantic token.

## Fields

| Name | Type | Token Name | Default Value | Description |
| --- | --- | --- | --- | --- |
| `ControlPadding` | `AspectToken<Thickness>` | `spacing.control-padding` | `new Thickness(8)` | Identifies the default spacing token for control padding values. |

## Applies to

Cerneala UI default aspect packages, aspect environments, token references, and theme-layer spacing resolution.

## See also

- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Layout.Thickness`
- `Cerneala.UI.Controls.Control`
