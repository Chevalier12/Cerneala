# IElementChildHost Interface

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/IElementChildHost.cs`

Represents an object that exposes a collection of generated visual children.

```csharp
public interface IElementChildHost
```

## Examples

```csharp
using Cerneala.UI.Elements;

GeneratedHost host = new();
host.GeneratedVisualChildren.Add(new UIElement());
Console.WriteLine(host.GeneratedVisualChildren.Count);
// 1

sealed class GeneratedHost : UIElement, IElementChildHost
{
    public UIElementCollection GeneratedVisualChildren => VisualChildren;
}
```

## Remarks

No Cerneala type implements this interface, and no Cerneala code reads it. Implementing it has no effect on layout, rendering, or input.

`UIElementCollection` has no public constructor, so an implementation returns a collection that an element already owns, such as `VisualChildren`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GeneratedVisualChildren` | `UIElementCollection` | Gets the collection that holds the generated visual children. |

## See also

- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Elements.UIElement`
