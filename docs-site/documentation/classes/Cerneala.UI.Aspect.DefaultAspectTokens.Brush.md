# DefaultAspectTokens.Brush Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Groups the built-in semantic brush tokens used by control chrome.

```csharp
public static class DefaultAspectTokens.Brush
```

## Examples

Use the surface brush in an aspect declaration:

```csharp
AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    DefaultAspectTokens.Brush.Surface.Ref());
```

## Remarks

The tokens carry nullable `Brush` values, so packages can supply solid, gradient, image, drawing, or visual brushes. `DefaultAspectPackage` supplies solid brushes for all four built-in values.

## Fields

| Name | Type | Token Name | Default Value |
| --- | --- | --- | --- |
| `Background` | `AspectToken<Brush?>` | `brush.background` | `new SolidColorBrush(new Color(248, 250, 252))` |
| `Surface` | `AspectToken<Brush?>` | `brush.surface` | `new SolidColorBrush(new Color(255, 255, 255))` |
| `Border` | `AspectToken<Brush?>` | `brush.border` | `new SolidColorBrush(new Color(148, 163, 184))` |
| `Foreground` | `AspectToken<Brush?>` | `brush.foreground` | `new SolidColorBrush(new Color(28, 35, 48))` |

## Applies to

Cerneala UI aspect packages, aspect environments, and control chrome rules.
