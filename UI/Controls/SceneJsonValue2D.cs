using System.Text.Json;

namespace Cerneala.UI.Controls;

/// <summary>Detached JSON metadata with reference identity, not structural or reflective equality.</summary>
public sealed class SceneJsonValue2D
{
    public SceneJsonValue2D(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("Scene JSON metadata requires a defined JSON value.", nameof(value));
        }
        Value = value.Clone();
    }

    /// <summary>Gets JSON content independent of the document passed to the constructor.</summary>
    public JsonElement Value { get; }
}
