# GameBootstrap Class

## Definition
Namespace: `Cerneala`
Assembly/Project: `Cerneala`
Source: `GameBootstrap.cs`

Provides backend-agnostic startup defaults.

```csharp
public static class GameBootstrap
```

## Examples

```csharp
using Cerneala;
using Cerneala.Drawing;

Color clearColor = GameBootstrap.CreateDefaultClearColor();
```

## Remarks

`GameBootstrap` centralizes the default Cerneala clear color used by host bootstrap paths. The returned value is the backend-agnostic `Cerneala.Drawing.Color.CornflowerBlue`; each drawing backend is responsible for mapping it to its native color representation.

## Methods

| Name | Description |
| --- | --- |
| `CreateDefaultClearColor()` | Returns the backend-agnostic default clear color. |

## Applies to

Project: `Cerneala`

## See also

- Source: `GameBootstrap.cs`
