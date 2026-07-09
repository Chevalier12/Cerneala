# ValueMixerRegistry Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/ValueMixerRegistry.cs`

Stores `ValueMixer<T>` instances by value type and resolves typed or untyped mixers for motion interpolation.

```csharp
public sealed class ValueMixerRegistry
```

Inheritance:
`object` -> `ValueMixerRegistry`

## Examples

Register the built-in mixers and resolve the mixer for a supported value type:

```csharp
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<double> mixer = registry.Resolve<double>();
double halfway = mixer.Mix(0d, 10d, 0.5f);
```

Register a custom mixer for a custom value type:

```csharp
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.Register(new CustomValueMixer());

if (registry.TryResolve<CustomValue>(out ValueMixer<CustomValue> mixer))
{
    CustomValue current = mixer.Mix(new CustomValue(0), new CustomValue(10), 0.25f);
}

public readonly record struct CustomValue(float Amount);

public sealed class CustomValueMixer : ValueMixer<CustomValue>
{
    public override CustomValue Mix(CustomValue from, CustomValue to, float progress)
    {
        return new CustomValue(from.Amount + ((to.Amount - from.Amount) * progress));
    }
}
```

## Remarks

`ValueMixerRegistry` is the lookup table used by the motion system when a `MotionValue<T>` or motion spec needs interpolation behavior for a value type. `MotionSystem` creates a root-owned registry, registers the built-in mixers, and passes that registry to its `MotionGraph`. The one-argument `MotionGraph` constructor also creates a registry and registers the same built-ins.

Registration is type-based. `Register<T>` stores the supplied `ValueMixer<T>` under `typeof(T)`, replacing any mixer previously registered for that exact type. Generic resolution returns `ValueMixer<T>`, while untyped resolution returns `IValueMixer`.

`TryResolve` methods return `false` when the registry has no mixer for the requested type. `Resolve` methods throw an `InvalidOperationException` instead. The overloads that accept `propertyName` include that property name in the exception message when it is not null or whitespace, which helps diagnose missing mixers for motion-driven UI properties.

`RegisterBuiltIns` registers mixers for `float`, `double`, `DrawColor`, `Thickness`, `DrawPoint`, `DrawSize`, `DrawRect`, and `Transform`.

## Constructors

| Name | Description |
| --- | --- |
| `ValueMixerRegistry()` | Creates an empty mixer registry. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Register<T>(ValueMixer<T> mixer)` | `void` | Registers `mixer` for `T`, replacing any existing mixer for the same value type. |
| `TryResolve<T>(out ValueMixer<T> mixer)` | `bool` | Attempts to resolve the typed mixer registered for `T`. |
| `Resolve<T>()` | `ValueMixer<T>` | Resolves the typed mixer registered for `T`, or throws when no mixer is registered. |
| `Resolve<T>(string? propertyName)` | `ValueMixer<T>` | Resolves the typed mixer registered for `T`, or throws a missing-mixer exception that can include a property name. |
| `TryResolve(Type valueType, out IValueMixer mixer)` | `bool` | Attempts to resolve the untyped mixer registered for `valueType`. |
| `Resolve(Type valueType, string? propertyName = null)` | `IValueMixer` | Resolves the untyped mixer registered for `valueType`, or throws a missing-mixer exception that can include a property name. |
| `RegisterBuiltIns()` | `void` | Registers the built-in mixers used by the default motion system. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Register<T>(ValueMixer<T> mixer)` | `ArgumentNullException` | `mixer` is `null`. |
| `TryResolve(Type valueType, out IValueMixer mixer)` | `ArgumentNullException` | `valueType` is `null`. |
| `Resolve(Type valueType, string? propertyName = null)` | `ArgumentNullException` | `valueType` is `null`. |
| `Resolve<T>()`, `Resolve<T>(string? propertyName)`, `Resolve(Type, string?)` | `InvalidOperationException` | No mixer is registered for the requested value type. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionGraph`
