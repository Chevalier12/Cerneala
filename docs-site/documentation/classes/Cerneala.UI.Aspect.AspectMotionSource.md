# AspectMotionSource Enum

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectMotion.cs`

Specifies which kinds of aspect declarations may use an `AspectMotion` transition.

```csharp
[Flags]
public enum AspectMotionSource
```

## Examples

Restrict a transition to declarations driven by state conditions:

```csharp
AspectMotion motion = new(
    Control.BackgroundProperty,
    "motion.fast",
    AspectMotionSource.State);
```

Enable the same transition for variant and data-driven declarations:

```csharp
AspectMotion motion = new(
    UIElement.OpacityProperty,
    "motion.normal",
    AspectMotionSource.Variant | AspectMotionSource.Data);
```

## Remarks

`AspectEngine` classifies an unconditional declaration as `Base`. State conditions, UI-property conditions, and predicate conditions are classified as `State`; variant conditions as `Variant`; and data-context conditions as `Data`. Compound conditions can produce multiple source flags.

The engine starts a Motion transaction only when the declaration's classification intersects the `AspectMotion.Source` mask. `All` is the default mask used by `AspectMotion`.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | Disables the motion for every aspect source. |
| `Base` | `1 << 0` | Matches unconditional declarations. |
| `State` | `1 << 1` | Matches state, UI-property, and predicate-driven declarations. |
| `Variant` | `1 << 2` | Matches variant-driven declarations. |
| `Data` | `1 << 3` | Matches data-context-driven declarations. |
| `All` | `Base | State | Variant | Data` | Matches every supported aspect source category. |

## Applies to

Cerneala UI aspect declarations and Motion integration.

## See also

- `AspectMotion`
- `AspectDeclaration`
- `AspectCondition`
- `AspectEngine`
