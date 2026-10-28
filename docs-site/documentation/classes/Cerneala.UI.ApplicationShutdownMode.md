# ApplicationShutdownMode Enum

## Definition
Namespace: `Cerneala.UI`
Assembly/Project: `Cerneala`
Source: `UI/ApplicationShutdownMode.cs`

Specifies when closing windows automatically shuts down an application.

```csharp
public enum ApplicationShutdownMode
```

## Examples
```csharp
app.ShutdownMode = ApplicationShutdownMode.OnMainWindowClose;
```

## Fields
| Name | Description |
| --- | --- |
| `OnLastWindowClose` | Shuts down after the last runtime window closes successfully. |
| `OnMainWindowClose` | Shuts down when the window designated by `Application.MainWindow` closes successfully. |
| `OnExplicitShutdown` | Keeps the application alive until `Application.Shutdown` is called or hosting stops it. |

## Remarks
A cancelled `Window.Closing` event does not satisfy any automatic shutdown policy. A close whose `Window.Closed` handler throws does count: the window is closed, so the policy runs, for example `OnLastWindowClose` raises `Application.Exit` when no window is left, and then the handler's exception propagates from `Window.Close`. Reassigning `MainWindow` changes which future close is observed by `OnMainWindowClose`.

## Applies to
Desktop application hosting.
