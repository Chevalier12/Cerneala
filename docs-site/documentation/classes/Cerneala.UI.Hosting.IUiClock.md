# IUiClock Interface

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/IUiClock.cs`

Supplies the time that has passed since the previous frame, for a `UiHost` that is not given one directly.

```csharp
public interface IUiClock
```

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

FixedClock clock = new();
UiHost host = new(new UiHostOptions { Root = new UIRoot(100, 100), Clock = clock });
InputFrame noInput = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.Empty,
    Array.Empty<TextInputSnapshotEvent>());

UiFrame first = host.Update(noInput);
UiFrame second = host.Update(noInput, elapsedTime: TimeSpan.FromMilliseconds(5));

Console.WriteLine($"{first.ElapsedTime.TotalMilliseconds} {second.ElapsedTime.TotalMilliseconds} {clock.Calls}");
// 16 5 1

sealed class FixedClock : IUiClock
{
    public int Calls { get; private set; }

    public TimeSpan GetElapsedTime()
    {
        Calls++;
        return TimeSpan.FromMilliseconds(16);
    }
}
```

The second update passes `elapsedTime`, so the clock is not called for it.

## Remarks

Set the clock through `UiHostOptions.Clock` or `UiHost.Clock`. Cerneala has no built-in implementation.

`UiHost.Update` picks the frame time in this order:

1. The `elapsedTime` argument, when it is not `null`.
2. `Clock.GetElapsedTime()`, when a clock is set.
3. `TimeSpan.Zero`.

`GetElapsedTime` is called at most once per update. The frame time is passed to `TimeSensitiveRenderInvalidator` (elements whose rendering depends on time, such as a blinking caret), to input dispatch through `ElementInputBridge`, and to the returned `UiFrame.ElapsedTime`.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `GetElapsedTime()` | `TimeSpan` | Returns the time that has passed since the previous frame. |

## Applies to

`UiHost.Clock` and `UiHostOptions.Clock`.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.UiHostOptions`
- `Cerneala.UI.Hosting.UiFrame`
