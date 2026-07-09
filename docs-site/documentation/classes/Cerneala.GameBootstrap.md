# GameBootstrap Class

## Definition
Namespace: `Cerneala`
Assembly/Project: `Cerneala`
Source: `GameBootstrap.cs`

Provides startup helper APIs for the game host.

```csharp
public static class GameBootstrap
```

## Examples

```csharp
using Cerneala;
using Microsoft.Xna.Framework;

Color clearColor = GameBootstrap.CreateDefaultClearColor();
```

## Remarks

`GameBootstrap` currently centralizes the default MonoGame clear color used by the host bootstrap path. The returned value is `Color.CornflowerBlue`.

## Methods

| Name | Description |
| --- | --- |
| `CreateDefaultClearColor()` | Returns the default clear color for the game host. |

## Applies to

Project: `Cerneala`

## See also

- Source: `GameBootstrap.cs`
