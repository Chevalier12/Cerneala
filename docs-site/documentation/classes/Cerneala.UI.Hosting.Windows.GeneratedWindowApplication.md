# GeneratedWindowApplication Class

## Definition
Namespace: `Cerneala.UI.Hosting.Windows`  
Assembly/Project: `Cerneala`  
Source: `UI/Hosting/Windows/GeneratedWindowApplication.cs`

Runs or hosts the main window described by generated startup metadata.

```csharp
public static class GeneratedWindowApplication
```

## Examples
```csharp
GeneratedWindowApplication.RegisterStartup(descriptor);
```

## Methods
| Name | Description |
| --- | --- |
| `RegisterStartup` | Registers the one generated startup descriptor. |
| `Run` | Builds services and runs the main window standalone. |

## Remarks
Only one distinct startup descriptor may be registered. Hosted pumping and reset methods are internal; `Run` disposes the runtime after the standalone window exits.

## Applies to
Windows desktop application hosting.
