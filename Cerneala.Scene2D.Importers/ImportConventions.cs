using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Importers;

// Shared authoring conventions, independent of the source file format.
internal sealed class ImportConventions(ImportContext context)
{
    private int colliderCount;

    internal void Validate(IReadOnlyDictionary<string, object?> properties)
    {
        if (properties.TryGetValue("CernealaRole", out object? role) && role is not ("Metadata" or "Spawn" or "Collider" or "Promote"))
        { context.Fail("SCN2D004", "Unknown CernealaRole convention."); }
        if (properties.TryGetValue("CollisionLayer", out object? layer)) { _ = Scene2DModelValidator.ParseCollisionBits(layer!); }
        if (properties.TryGetValue("CollisionMask", out object? mask)) { _ = Scene2DModelValidator.ParseCollisionBits(mask!); }
        if (properties.TryGetValue("IsTrigger", out object? trigger) && trigger is not bool) { context.Fail("SCN2D016", "IsTrigger must be Boolean."); }
    }

    internal string Text(IReadOnlyDictionary<string, object?> properties, string name, string fallback)
    {
        if (!properties.TryGetValue(name, out object? value)) { return fallback; }
        if (value is not string) { context.Fail("SCN2D016", $"'{name}' must be a string."); }
        return (string)value!;
    }

    internal TilePromotion2D Promotion(IReadOnlyDictionary<string, object?> properties)
    {
        string layer = properties.TryGetValue("TileLayer", out object? value) && value is string or long
            ? Convert.ToString(value, CultureInfo.InvariantCulture)! : "";
        int Coordinate(string name)
        {
            if (!properties.TryGetValue(name, out object? number) || number is not long integer || integer < int.MinValue || integer > int.MaxValue)
            { context.Fail("SCN2D012", $"Promotion requires an Int32 '{name}'."); }
            return (int)(long)number!;
        }
        if (string.IsNullOrWhiteSpace(layer)) { context.Fail("SCN2D012", "Promotion requires a stable TileLayer."); }
        int? tile = properties.ContainsKey("TileId") ? Coordinate("TileId") : null;
        return new(new(layer, Coordinate("TileX"), Coordinate("TileY")), tile, properties);
    }

    internal List<TileColliderDescriptor2D> Colliders(string id, string role, string shape, DrawSize size,
        string points, IReadOnlyDictionary<string, object?> properties, int maxColliders = 4096)
    {
        Validate(properties);
        List<TileColliderDescriptor2D> result = new();
        if (role != "Collider") { return result; }
        if (points.Length > 393_216) { context.Fail("SCN2D013", "Shape point text exceeds the core limit."); }
        string[] vertices = shape == "Polyline" ? points.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) : [];
        int count = shape == "Polyline" ? Math.Max(0, vertices.Length - 1) : shape == "Point" ? 0 : 1;
        if (count > maxColliders || count > 65_536 - colliderCount)
        { context.Fail("SCN2D013", "Collider descriptors exceed the per-owner or aggregate import budget."); }
        colliderCount += count;
        uint layer = properties.TryGetValue("CollisionLayer", out object? layerValue) ? Scene2DModelValidator.ParseCollisionBits(layerValue!) : 1;
        uint mask = properties.TryGetValue("CollisionMask", out object? maskValue) ? Scene2DModelValidator.ParseCollisionBits(maskValue!) : uint.MaxValue;
        bool trigger = properties.TryGetValue("IsTrigger", out object? triggerValue) && (bool)triggerValue!;
        TileColliderDescriptor2D Descriptor(TileColliderShape2D kind, Matrix3x2 transform, string text = "0,0 1,0 0,1") =>
            new(kind, transform, width: size.Width, height: size.Height, radius: 1, points: text,
                collisionLayer: layer, collisionMask: mask, isTrigger: trigger, debugIdentity: id, properties: properties);
        switch (shape)
        {
            case "Box": result.Add(Descriptor(TileColliderShape2D.Box, Matrix3x2.Identity)); break;
            case "Ellipse": result.Add(Descriptor(TileColliderShape2D.Circle,
                Matrix3x2.CreateScale(size.Width / 2, size.Height / 2) * Matrix3x2.CreateTranslation(size.Width / 2, size.Height / 2))); break;
            case "Polygon": result.Add(Descriptor(TileColliderShape2D.Polygon, Matrix3x2.Identity, points)); break;
            case "Polyline":
                for (int index = 1; index < vertices.Length; index++)
                { result.Add(Descriptor(TileColliderShape2D.Segment, Matrix3x2.Identity, vertices[index - 1] + " " + vertices[index])); }
                break;
            default: context.Fail("SCN2D008", "A collider requires supported non-point geometry."); break;
        }
        return result;
    }
}
