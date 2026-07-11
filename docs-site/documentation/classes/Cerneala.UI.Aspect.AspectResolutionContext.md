# AspectResolutionContext Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectResolutionContext.cs`

Carries the element, token environment, active states, variants, and optional theme provider used while aspect values resolve.

```csharp
public sealed class AspectResolutionContext
```

Inheritance:
`object` -> `AspectResolutionContext`

## Examples

Resolve an aspect token reference against an environment:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<Color> accentToken = AspectToken.Color("app.accent");
AspectEnvironment environment = new("root");
environment.Set(accentToken, Color.White);

AspectResolutionContext context = new(new Button(), environment);

object? value = accentToken.Ref().Resolve(context);
```

Create a context for a computed aspect value that reads the current element state:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectValue<string> labelValue = AspectValue<string>.Computed(
    context => context.States.Contains(AspectState.Hover) ? "hover" : "normal",
    []);

AspectResolutionContext context = new(
    new Button(),
    new AspectEnvironment("preview"),
    AspectStateSet.Empty.Add(AspectState.Hover));

object? resolved = labelValue.Resolve(context);
```

## Remarks

`AspectResolutionContext` is the per-resolution input object passed to `AspectValue.Resolve`. `AspectEngine.Resolve` builds one from the element being resolved, the supplied `AspectEnvironment`, the element's current `AspectStateSet`, optional variants, and optional `ThemeProvider`.

The constructor requires non-null `element` and `environment` arguments. When `states` or `variants` are omitted, they are normalized to `AspectStateSet.Empty` and `AspectVariantSet.Empty`, so computed aspect values can read `States` and `Variants` without null checks.

Token-backed aspect values use `Environment` to look up their token value. Computed aspect values receive the whole context and can inspect `Element`, `States`, `Variants`, `Environment`, or `ThemeProvider` as needed. The context does not resolve tokens by itself; resolution is performed by `AspectValue` implementations.

`AspectProcessor` also uses this context while synchronizing catalog token defaults, passing the root, default environment, empty states and variants, and the root theme provider.

## Constructors

| Name | Description |
| --- | --- |
| `AspectResolutionContext(UIElement element, AspectEnvironment environment, AspectStateSet? states = null, AspectVariantSet? variants = null, ThemeProvider? themeProvider = null)` | Initializes a resolution context for an element and aspect environment. Throws `ArgumentNullException` when `element` or `environment` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | Gets the UI element whose aspect value is being resolved. |
| `Environment` | `AspectEnvironment` | Gets the token environment used by token-backed aspect values. |
| `States` | `AspectStateSet` | Gets the active aspect states available to computed aspect values. |
| `ThemeProvider` | `ThemeProvider?` | Gets the optional theme provider supplied for the resolution pass. |
| `Variants` | `AspectVariantSet` | Gets the variant values available to computed aspect values. |

## Applies to

Cerneala UI aspect value resolution, aspect engine resolution, template token resolution, and token default synchronization.

## See also

- `Cerneala.UI.Aspect.AspectValue`
- `Cerneala.UI.Aspect.AspectValue<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.AspectMatchContext`
