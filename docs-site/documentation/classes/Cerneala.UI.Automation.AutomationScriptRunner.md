# AutomationScriptRunner Class

## Definition
Namespace: `Cerneala.UI.Automation`
Assembly/Project: `Cerneala`
Source: `UI/Automation/AutomationScriptRunner.cs`

Executes JSON automation steps against an `AutomationSession`.

```csharp
public static class AutomationScriptRunner
```

## Examples
```json
{
  "steps": [
    { "action": "click", "automationId": "editor" },
    { "action": "pressKey", "key": "A", "modifiers": ["Control"] },
    { "action": "sendText", "text": "replacement" },
    { "action": "screenshot", "path": "capture.png" }
  ]
}
```

Set `CERNEALA_AUTOMATION_SCRIPT` to the JSON file path to run it automatically against the first matching rendered native window. Set `CERNEALA_AUTOMATION_WINDOW_TITLE` when a process creates multiple windows and the script must target one exact title. The native runtime waits for a committed retained frame before executing the script and writes failures to `<script-path>.error.txt`.

## Remarks
Targets can use either `automationId` or `xpath`. Supported actions are `click`, `pressKey`, `sendText`, and `screenshot`. Relative screenshot paths resolve beside the script file. Screenshot steps use the session provider, which is `Window.SaveScreenshot` for native window sessions.

## Methods
| Name | Description |
| --- | --- |
| `RunFile(AutomationSession, string)` | Loads and executes a JSON script file. |
| `RunJson(AutomationSession, string, string?)` | Executes JSON with an optional base directory. |

## Applies to
Opt-in deterministic application automation.
