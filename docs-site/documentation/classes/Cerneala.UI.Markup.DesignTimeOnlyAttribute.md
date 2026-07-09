# DesignTimeOnlyAttribute Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/DesignTimeOnlyAttribute.cs`

Represents metadata that can mark a class or property as design-time-only.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class DesignTimeOnlyAttribute : Attribute
```

Inheritance:
`Object` -> `Attribute` -> `DesignTimeOnlyAttribute`

Attributes:
`AttributeUsageAttribute` with `AttributeTargets.Class`, `AttributeTargets.Property`, and `Inherited = true`.

## Examples

```csharp
using Cerneala.UI.Markup;

[DesignTimeOnly]
public sealed class PreviewOnlyControl
{
    [DesignTimeOnly]
    public string? PreviewLabel { get; set; }
}
```

## Remarks

`DesignTimeOnlyAttribute` is a marker attribute. It stores no additional data and does not perform filtering, validation, serialization, or runtime behavior by itself.

The attribute can be applied to classes and properties. Because its usage metadata sets `Inherited` to `true`, reflection code that requests inherited attributes can observe the marker through inheritance where .NET attribute inheritance rules apply.

The source declares no repository-specific consumers for this attribute in the indexed C# code, so any design-time behavior depends on code that explicitly checks for this metadata.

## Constructors

| Name | Description |
| --- | --- |
| `DesignTimeOnlyAttribute()` | Initializes a new marker attribute instance. |

## Applies to

`Cerneala` markup metadata for classes and properties.

## See also

- `ContentPropertyAttribute`
