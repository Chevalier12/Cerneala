# IElementHost Interface

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/IElementHost.cs`

Represents an object that exposes the `UIRoot` of an element tree.

```csharp
public interface IElementHost
```

## Examples

```csharp
using Cerneala.UI.Elements;

UIRoot root = new(100, 100);
IElementHost host = root;
Console.WriteLine(host.Root == root);
// True
```

## Remarks

`UIRoot` is the only Cerneala implementation. It implements `Root` explicitly and returns itself.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Root` | `UIRoot` | Gets the root of the hosted element tree. |

## Applies to

`UIRoot`.

## See also

- `Cerneala.UI.Elements.UIRoot`
