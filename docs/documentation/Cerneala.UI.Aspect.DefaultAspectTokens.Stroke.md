# DefaultAspectTokens.Stroke Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Groups the built-in aspect tokens for default stroke and border thickness values.

```csharp
public static class DefaultAspectTokens.Stroke
```

Inheritance:
`object` -> `DefaultAspectTokens.Stroke`

## Examples

Read the default control border thickness token from the default aspect environment:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Layout;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Stroke.ControlBorderThickness, out Thickness thickness))
{
    // thickness is new Thickness(1) in the default environment.
}
```

Use the default stroke token as the value source for a control border thickness declaration:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectDeclaration declaration = new(
    Control.BorderThicknessProperty,
    DefaultAspectTokens.Stroke.ControlBorderThickness.Ref());
```

## Remarks

`DefaultAspectTokens.Stroke` is a static token group. It defines token identities only; it does not store stroke values by itself. Values are supplied by an `AspectPackage`, an `AspectEnvironment`, or another token source.

`DefaultAspectPackage.Create()` registers `ControlBorderThickness` with `new Thickness(1)`. `DefaultAspectPackage.CreateEnvironment()` sets the same value in the returned `AspectEnvironment`.

The token name is the semantic string `stroke.control-border-thickness`. The default package uses `ControlBorderThickness.Ref()` in its `Button` base rule to resolve `Control.BorderThicknessProperty` from the active aspect environment.

`Control.BorderThicknessProperty` itself defaults to `Thickness.Zero`, affects measure and render, and validates that every side is finite and non-negative. The stroke token supplies the default package value used by aspect resolution; it is separate from the control property's own metadata default.

## Fields

| Name | Type | Token Name | Default Value | Description |
| --- | --- | --- | --- | --- |
| `ControlBorderThickness` | `AspectToken<Thickness>` | `stroke.control-border-thickness` | `new Thickness(1)` | Identifies the semantic border thickness token used by the default aspect package and environment for controls. |

## Applies to

Cerneala UI aspect packages and aspect environments that resolve default control border thickness values.

## See also

- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Layout.Thickness`
