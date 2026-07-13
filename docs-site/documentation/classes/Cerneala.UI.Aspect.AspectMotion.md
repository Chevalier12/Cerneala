# AspectMotion Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectMotion.cs`

Stores motion metadata for an aspect declaration.

```csharp
public sealed class AspectMotion
```

Inheritance:
`object` -> `AspectMotion`

## Examples

Attach motion metadata to an aspect declaration:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectDeclaration declaration = new(
    Control.BorderBrushProperty,
    AspectValue<Brush?>.Literal(new SolidColorBrush(new Color(99, 102, 241))),
    new AspectMotion(Control.BorderBrushProperty, "motion.fast", AspectMotionSource.State));
```

Use the same motion token for multiple declarations in a rule:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectRuleSet hoverRule = new(
    "card hover",
    AspectLayer.Runtime,
    new AspectTarget(typeof(Border), conditions: [AspectCondition.State(AspectState.Hover)]),
    [
        new AspectDeclaration(
            Control.BackgroundProperty,
            AspectValue<Brush?>.Literal(new SolidColorBrush(new Color(238, 242, 255))),
            new AspectMotion(Control.BackgroundProperty, "motion.normal")),
        new AspectDeclaration(
            Control.BorderBrushProperty,
            AspectValue<Brush?>.Literal(new SolidColorBrush(new Color(99, 102, 241))),
            new AspectMotion(Control.BorderBrushProperty, "motion.normal"))
    ],
    declarationOrder: 0);
```

## Remarks

`AspectMotion` identifies the `UiProperty` whose aspect value should transition, the theme motion token used for that transition, and the aspect source categories for which the transition is enabled.

When `AspectEngine.Apply` changes an attached element, it resolves `TokenName` through `ThemeMotionTokens` on the supplied `ThemeProvider` and applies the property mutation inside a Motion transaction. The property must be registered as animatable. Without an attached root or a theme provider, the value is applied immediately.

The winning declaration controls motion when a rule becomes active. When a conditional declaration stops winning, its motion metadata controls the transition back to the newly resolved value or to the property's lower-precedence value. This gives state rules symmetric enter and exit transitions without duplicating motion metadata on the base declaration.

The constructor requires a non-null `UiProperty` and a non-empty, non-whitespace token name. The `Source` value defaults to `AspectMotionSource.All`, which combines base, state, variant, and data aspect sources.

`AspectMotionSource` is a `[Flags]` enum, so sources can be combined when a motion token should apply to more than one aspect source category. UI-property and predicate conditions are state-driven and therefore use the `State` category.

## Constructors

| Name | Description |
| --- | --- |
| `AspectMotion(UiProperty property, string tokenName, AspectMotionSource source = AspectMotionSource.All)` | Initializes motion metadata for `property` using `tokenName` and the optional source filter. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Property` | `UiProperty` | Gets the UI property associated with the motion metadata. |
| `TokenName` | `string` | Gets the motion token name. |
| `Source` | `AspectMotionSource` | Gets the source flags that identify which aspect source categories can contribute the motion metadata. |

## Related Enum Values

| Name | Value | Description |
| --- | --- | --- |
| `AspectMotionSource.None` | `0` | No aspect motion source. |
| `AspectMotionSource.Base` | `1 << 0` | Base aspect source. |
| `AspectMotionSource.State` | `1 << 1` | State aspect source. |
| `AspectMotionSource.Variant` | `1 << 2` | Variant aspect source. |
| `AspectMotionSource.Data` | `1 << 3` | Data aspect source. |
| `AspectMotionSource.All` | `Base | State | Variant | Data` | All defined aspect motion sources. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectMotion(UiProperty, string, AspectMotionSource)` | `ArgumentNullException` | `property` is `null`. |
| `AspectMotion(UiProperty, string, AspectMotionSource)` | `ArgumentException` | `tokenName` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI aspect declarations and resolved aspect values that carry optional motion metadata.

## See also

- `AspectDeclaration`
- `ResolvedAspectValue`
- `AspectEngine`
- `AspectToken`
- `Cerneala.UI.Core.UiProperty`
