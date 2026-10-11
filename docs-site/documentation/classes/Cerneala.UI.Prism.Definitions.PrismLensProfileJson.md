# PrismLensProfileJson Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileJson.cs`

Reads and writes validated Prism lens profiles as strict JSON.

```csharp
public static class PrismLensProfileJson
```

## Examples

```csharp
using System.IO;
using Cerneala.UI.Prism.Definitions;

using FileStream input = File.OpenRead("lens-profile.json");
PrismLensProfileResource profile = PrismLensProfileJson.Load(input);

using FileStream output = File.Create("fitted-profile.json");
PrismLensProfileJson.Save(output, profile);
```

## Remarks

JSON property names use camel case. Unknown properties are rejected so stale
or misspelled optical data does not silently alter rendering. Streams remain
open after loading or saving.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Parse(string)` | `PrismLensProfileResource` | Parses a JSON string. |
| `Load(Stream)` | `PrismLensProfileResource` | Loads UTF-8 JSON from a stream. |
| `Serialize(PrismLensProfileResource, bool)` | `string` | Serializes a profile, indented by default. |
| `Save(Stream, PrismLensProfileResource, bool)` | `void` | Writes UTF-8 JSON to a stream. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Parse` | `ArgumentNullException` | `json` is `null`. |
| `Parse` | `ArgumentException` | `json` is empty or whitespace. |
| `Load` | `ArgumentNullException` | `stream` is `null`. |
| `Serialize` | `ArgumentNullException` | `profile` is `null`. |
| `Save` | `ArgumentNullException` | `stream` or `profile` is `null`. |
| `Parse`, `Load` | `JsonException` | The JSON is malformed, is the literal `null`, contains a property that the profile format does not define, or describes a profile that the resource constructors reject (for example an empty ghost list). |

## See also

- [PrismLensProfileResource](Cerneala.UI.Prism.Definitions.PrismLensProfileResource.md)
- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
