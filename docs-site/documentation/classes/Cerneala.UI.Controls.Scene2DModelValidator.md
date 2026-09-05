# Scene2DModelValidator Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Validates scene data without loading images, opening files, or creating scene nodes.

```csharp
public static class Scene2DModelValidator
```

## Examples

```csharp
Scene2DValidationResult result = Scene2DModelValidator.Validate(
    model,
    new Dictionary<string, DrawSize> { ["Atlas"] = new DrawSize(32, 16) });
if (!result.Success)
{
    foreach (Scene2DDiagnostic diagnostic in result.Diagnostics)
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
}
```

## Remarks

Model constructors enforce structural invariants before publication. They retain their argument-exception categories and attach stable diagnostic information. Importers use those same constructors and `GetDiagnostic`; they do not implement a second geometry validator. `Validate` additionally checks atlas references and source rectangles against supplied dimensions, document identities, associations, promotions, and configured aggregate budgets.

Validation is deterministic in collection order. Retention stops at `MaxDiagnostics`, but omitted errors still make `Success` false. After a known failure fills the retained budget, validation may stop inspecting remaining data and report truncation. Message, file and JSON-path strings are limited to 4,096 UTF-16 characters. Validation does not mutate a model, allocate GPU resources, install collision adapters, or inspect the filesystem.

### Construction limits

Each chunk/tileset is limited to 1,048,576 cells/definitions; a layer has at most 65,536 chunks; a map has at most 4,096 layers/tilesets. A level has at most 65,536 entities and 65,536 promotion references. A document has at most 4,096 levels/assets. A shape or descriptor collection has at most 4,096 points/descriptors. Point text is limited to 393,216 UTF-16 characters before tokenization.

A map additionally caps aggregate definitions and cells at 1,048,576 each and aggregate chunks at 65,536, including repeated references to shared components. A map permits at most 65,536 expanded tile collider descriptors, counted before horizontal box coalescing. A level permits at most 65,536 entity collider descriptors in total. This prevents small repeated input from materializing an unbounded collision tree. Collection enumeration is bounded; chunk dimensions are checked before enumeration and excess cell enumeration stops at the first extra cell.

Geometry must fit the existing drawing coordinate range (±2,000,000,000 scene units, sizes at most 2,000,000,000), in addition to finite-number checks. Polygon cross products must remain finite and nondegenerate. The collider epsilon is 0.00001 scene units. These are rejection rules, not clamping or approximation.

These per-component construction caps are independent of the configurable aggregate validation budgets. Raising an aggregate budget does not remove a component cap. Arbitrary objects stored in opaque property dictionaries are not recursively validated or cloned.

### Stable model codes

| Code | Meaning |
| --- | --- |
| `SCN2D003` | Invalid publication/document schema version. |
| `SCN2D004` | Unsupported shape, role, or flip bits. |
| `SCN2D005` | Invalid dimensions, chunk size/count, or bounds. |
| `SCN2D006` | Invalid/unresolved tile ID. |
| `SCN2D007` | Invalid atlas dimensions or source rectangle. |
| `SCN2D008` | Invalid collider geometry. |
| `SCN2D009` | Invalid collision bitset. |
| `SCN2D010` | Invalid/unresolved asset reference. |
| `SCN2D011` | Chunk overlap. |
| `SCN2D012` | Invalid or duplicate promotion. |
| `SCN2D013` | Resource/work limit exceeded. |
| `SCN2D014` | Nonfinite/out-of-range geometry or opacity. |
| `SCN2D015` | Duplicate, empty, or unresolved identity. |

## Methods

| Name | Description |
| --- | --- |
| `Validate(TileMap2DModel, IReadOnlyDictionary<string, DrawSize>, Scene2DValidationOptions?)` | Validates a programmatic map against atlas resource-key dimensions. |
| `Validate(Scene2DDocument, Scene2DValidationOptions?)` | Validates the complete core document and its aggregate budgets. |
| `GetDiagnostic(Exception, string, string)` | Returns a diagnostic for an annotated core validation exception, or null for an unrelated exception. Caller-supplied file and JSON path are retained within the text limit; defaults are empty file and `$`. |
| `ParseCollisionBits(object)` | Accepts uint, nonnegative in-range int/long/ulong, or an unsigned decimal/`0x` string; returns all 32 bits, including zero. Rejects fractional, negative, malformed, and out-of-range values with `SCN2D009`. |

## See also

- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
- [Scene2DValidationOptions](Cerneala.UI.Controls.Scene2DValidationOptions.md)
