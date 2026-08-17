# CernealaPackage Class

## Definition

Namespace: `Cerneala.VisualStudio`

Assembly/Project: `Cerneala.VisualStudio`

Source: `Cerneala.VisualStudio/CernealaPackage.cs`

Provides the Visual Studio package entry point for Cerneala editor commands and troubleshooting output.

```csharp
public sealed class CernealaPackage : Microsoft.VisualStudio.Shell.AsyncPackage
```

Inheritance:

`Object` -> `Package` -> `AsyncPackage` -> `CernealaPackage`

## Examples

Use the package identifier when code or registration metadata must refer to the Cerneala package:

```csharp
Guid packageId = new(CernealaPackage.PackageGuidString);
```

Visual Studio creates the package. Applications and extensions should not instantiate it directly.

## Remarks

The package supports background loading and is registered without an automatic solution-load rule. Visual Studio loads it when the `Cerneala: Restart Language Server` command is invoked. Initialization creates the `Cerneala` output pane and registers the command handler.

The `.crn` content type is a separate MEF registration and does not require this package to load when a Cerneala document is opened.

## Fields

| Name | Description |
| --- | --- |
| `PackageGuidString` | Gets the stable GUID string used to register the Visual Studio package. |

## Applies to

Visual Studio Community 18.x on Windows; .NET Framework 4.7.2 extension host.
