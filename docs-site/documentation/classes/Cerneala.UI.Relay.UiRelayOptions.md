# UiRelayOptions Class

## Definition
Namespace: `Cerneala.UI.Relay`

Assembly/Project: `Cerneala`

Source: `UI/Relay/UiRelayOptions.cs`

Configures the deterministic callback budget used by a `UiRelay` drain.

```csharp
public sealed class UiRelayOptions
```

Inheritance:
`object` -> `UiRelayOptions`

## Examples

Configure a maximum of 256 callbacks per update:

```csharp
using Cerneala.UI.Relay;

UiRelayOptions options = new()
{
    MaxCallbacksPerUpdate = 256
};
```

## Remarks

The default budget is 1,024 callbacks per update. A numeric budget keeps drain behavior deterministic and prevents a callback that reposts work from consuming an unbounded frame.

`MaxCallbacksPerUpdate` must be greater than zero. Relay construction copies the configured value; later updates do not read mutable option state.

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `MaxCallbacksPerUpdate` | `int` | `1024` | Gets or initializes the maximum number of callbacks eligible for one drain. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MaxCallbacksPerUpdate` | `ArgumentOutOfRangeException` | The initialized value is zero or negative. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Relay.UiRelay`
- `UI/Relay/UiRelayOptions.cs`
