# AspectBehavior Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectBehavior.cs`

Defines a target-typed, non-style sidecar that `AspectProcessor` attaches for each matching element visible to an `AspectPackage`.

```csharp
public sealed class AspectBehavior
```

Inheritance:
`object` -> `AspectBehavior`

## Examples

Add a disposable event sidecar to a package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

AspectBehavior clickBehavior = new(typeof(Button), element =>
{
    Button button = (Button)element;
    RoutedEventHandler handler = (_, _) => System.Console.WriteLine("Button clicked");
    button.Click += handler;
    return new EventSubscription(() => button.Click -= handler);
});

AspectPackage package = AspectPackage.Create("App.ButtonBehavior")
    .Components(components => components.AddBehavior(clickBehavior));

sealed class EventSubscription(Action unsubscribe) : IDisposable
{
    private Action? unsubscribe = unsubscribe;

    public void Dispose() => Interlocked.Exchange(ref unsubscribe, null)?.Invoke();
}
```

## Remarks

`AspectBehavior` is for lifecycle-owned behavior that is not part of Aspect value resolution, such as generated event, Motion, presence, layout, scroll, drag, gesture, or observation sidecars. It does not contain declarations and does not perform cascade or property-store writes on behalf of Aspect styling.

The constructor requires a `TargetType` derived from `UIElement` and a non-null attach factory. `AspectProcessor` filters behaviors by `TargetType`, invokes the factory once for each matching visible behavior occurrence, and retains the returned lifetime. A `null` lifetime is allowed and still counts as an attached behavior.

When a package is replaced, becomes invisible, or its element detaches, the processor disposes the associated lifetime. Reprocessing an unchanged catalog does not attach the same behavior occurrence again. The attach operation is processor-owned; this class intentionally exposes no public `Attach` method.

## Constructors

| Name | Description |
| --- | --- |
| `AspectBehavior(Type targetType, Func<UIElement, IDisposable?> attach)` | Creates a behavior for elements assignable to `targetType` using the supplied attach factory. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TargetType` | `Type` | Gets the required `UIElement` target type. Derived element types also match. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `targetType` or `attach` is `null`. |
| Constructor | `ArgumentException` | `targetType` does not derive from `UIElement`. |

## Applies to

Cerneala Aspect package sidecars synchronized by `AspectProcessor`.

## See also

- `AspectPackage`
- `ComponentAspectBuilder`
- `AspectCatalog`
- `AspectProcessor`
- `ElementAspect`
