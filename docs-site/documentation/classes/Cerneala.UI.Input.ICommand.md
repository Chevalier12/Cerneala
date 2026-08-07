# ICommand Interface
## Definition
Namespace: `Cerneala.UI.Input`
Assembly/Project: `Cerneala`
Source: `UI/Input/ICommand.cs`
Provides the `Cerneala.UI.Input.ICommand` API surface.
```csharp
public interface ICommand
```

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanExecute(object? parameter)` | `bool` | Gets whether the command can execute for the supplied parameter. |
| `Execute(object? parameter)` | `void` | Executes the command for the supplied parameter. |
## Remarks
This page is generated from the repository API index so the documentation surface stays aligned with the source tree.
## Applies to
Cerneala UI runtime and framework API consumers.
