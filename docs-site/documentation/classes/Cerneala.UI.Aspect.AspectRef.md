# AspectRef Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectRef.cs`

Provides a short static helper for creating token-backed `AspectValue<T>` instances.

```csharp
public static class AspectRef
```

## Examples

Reference an aspect token from an aspect declaration:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");

AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    AspectRef.To(accent));
```

Use the reference in a rule while defining the token default in the same package:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<DrawColor> surface = AspectToken.Color("card.surface");

AspectPackage package = AspectPackage.Create("Cards")
    .Tokens(tokens => tokens.Set(surface, DrawColor.White))
    .Components(components => components.AddRule(new AspectRuleSet(
        "card.surface",
        AspectLayer.App,
        new AspectTarget(typeof(ContentControl)),
        [new AspectDeclaration(Control.BackgroundProperty, AspectRef.To(surface))],
        declarationOrder: 0)));
```

## Remarks

`AspectRef` is a convenience wrapper around `AspectValue<T>.Token(AspectToken<T>)`. It is useful when an aspect declaration should resolve its value from the current `AspectEnvironment` instead of storing a literal value.

The returned value reports the supplied token as its only dependency through `AspectValue.Dependencies`. During resolution, `AspectValue<T>.Resolve(AspectResolutionContext)` reads that token from `context.Environment`; if the token is not present, resolution throws an `InvalidOperationException`.

`AspectToken<T>.Ref()` provides the same token-backed value shape as `AspectRef.To(token)`. Use whichever form keeps the declaration easier to read.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `To<T>(AspectToken<T> token)` | `AspectValue<T>` | Creates an aspect value that resolves from `token`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `To<T>(AspectToken<T>)` | `ArgumentNullException` | `token` is `null`. |

## Applies to

Cerneala UI aspect declarations and token-backed aspect value resolution.

## See also

- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectDeclaration`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectResolutionContext`
