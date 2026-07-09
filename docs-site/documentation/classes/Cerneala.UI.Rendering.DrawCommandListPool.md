# DrawCommandListPool Class

## Definition
Namespace: `Cerneala.UI.Rendering`  
Assembly/Project: `Cerneala`  
Source: `UI/Rendering/DrawCommandListPool.cs`

Retains reusable `DrawCommandList` instances for rendering code that wants to reduce per-frame command-list allocations.

```csharp
public sealed class DrawCommandListPool
```

Inheritance:  
`object` -> `DrawCommandListPool`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Rendering;

DrawCommandListPool pool = new(maxRetained: 4);

DrawCommandList commands = pool.Rent();
commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 16), DrawColor.White));

pool.Return(commands);

DrawCommandList reused = pool.Rent();
// The list returned to the pool was cleared before it became available again.
Console.WriteLine(reused.Count); // 0
```

## Remarks

`DrawCommandListPool` owns a bounded set of available `DrawCommandList` objects. `Rent` returns a retained list when one is available; otherwise, it creates a new `DrawCommandList`.

`Return` requires a non-null command list, clears it, and stores it only while `AvailableCount` is less than `MaxRetained`. If the pool is already full, the returned list is cleared but not retained. A pool created with `maxRetained: 0` accepts returned lists but never keeps them for reuse.

The class does not expose synchronization or ownership tracking. Callers should return only lists they rented and should not use a list after returning it to the pool.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandListPool(int maxRetained = 32)` | Initializes a pool that retains up to `maxRetained` available command lists. Throws `ArgumentOutOfRangeException` when `maxRetained` is negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MaxRetained` | `int` | Gets the maximum number of returned command lists the pool keeps for later reuse. |
| `AvailableCount` | `int` | Gets the number of command lists currently retained and available to rent. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Rent()` | `DrawCommandList` | Returns an available command list from the pool, or creates a new one when the pool is empty. |
| `Return(DrawCommandList commands)` | `void` | Clears `commands` and retains it when the pool has capacity. Throws `ArgumentNullException` when `commands` is null. |

## Applies to

Rendering infrastructure in the `Cerneala` project.

## See also

- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
