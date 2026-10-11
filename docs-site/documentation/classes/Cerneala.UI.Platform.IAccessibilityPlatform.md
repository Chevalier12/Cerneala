# IAccessibilityPlatform Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IAccessibilityPlatform.cs`

Receives semantics trees to publish to a platform accessibility API.

```csharp
public interface IAccessibilityPlatform
```

## Examples

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Platform;

IPlatformServices services = new PlatformServices(Accessibility: new CountingAccessibility());
Console.WriteLine(services.Accessibility is not null);
// True

sealed class CountingAccessibility : IAccessibilityPlatform
{
    public int PublishCount { get; private set; }

    public void Publish(SemanticsTree tree) => PublishCount++;
}
```

## Remarks

This is an optional seam in `IPlatformServices`. No Cerneala code calls `Publish`, and no Cerneala platform supplies an implementation, including the SDL platform. An application that wants to publish semantics calls `Publish` itself with a `SemanticsTree`. `Detective` reports only whether a publisher is present.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Publish(SemanticsTree tree)` | `void` | Publishes `tree` to the platform. |

## Applies to

`IPlatformServices.Accessibility`.

## See also

- `Cerneala.UI.Accessibility.SemanticsTree`
- `Cerneala.UI.Platform.IPlatformServices`
