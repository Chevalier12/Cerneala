# AspectPackageDiagnostic Record

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectCatalog.cs`

Represents diagnostic metadata for an aspect package included in an `AspectCatalog`.

```csharp
public sealed record AspectPackageDiagnostic(string Name);
```

Inheritance:
`object` -> `AspectPackageDiagnostic`

## Examples

Inspect package diagnostics from a built catalog:

```csharp
using Cerneala.UI.Aspect;

AspectCatalog catalog = new AspectRegistry()
    .Register(AspectPackage.Create("App"))
    .BuildCatalog();

foreach (AspectPackageDiagnostic package in catalog.PackageDiagnostics)
{
    string packageName = package.Name;
}
```

## Remarks

`AspectPackageDiagnostic` is created when `AspectRegistry.BuildCatalog()` merges registered packages into an `AspectCatalog`. The catalog adds one diagnostic entry per registered `AspectPackage`, using the package's `Name`.

Package diagnostics preserve registry order because `AspectCatalog.FromPackages` iterates packages in the order stored by `AspectRegistry`. They are exposed through `AspectCatalog.PackageDiagnostics` for tooling and inspection; the record does not contain rules, tokens, templates, or validation details.

## Constructors

| Name | Description |
| --- | --- |
| `AspectPackageDiagnostic(string Name)` | Initializes a diagnostic entry with the aspect package name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the package name copied from the source `AspectPackage`. |

## Applies to

Cerneala UI aspect catalogs built from `AspectRegistry`.

## See also

- `AspectCatalog`
- `AspectPackage`
- `AspectRegistry`
