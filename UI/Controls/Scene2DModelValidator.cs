using Cerneala.Drawing;
using System.Globalization;
using System.Numerics;

namespace Cerneala.UI.Controls;

public enum Scene2DDiagnosticSeverity
{
    Warning,
    Error,
    Fatal,
    Unsupported
}

public sealed record Scene2DDiagnostic(
    string Code,
    Scene2DDiagnosticSeverity Severity,
    string Message,
    string FilePath = "",
    string JsonPath = "$");

public sealed class Scene2DValidationOptions
{
    public int MaxDiagnostics { get; init; } = 128;
    public int MaxCells { get; init; } = 1_048_576;
    public int MaxChunks { get; init; } = 65_536;
    public int MaxLayers { get; init; } = 4_096;
    public int MaxEntities { get; init; } = 65_536;
}

public sealed class Scene2DValidationResult
{
    internal Scene2DValidationResult(bool success, Scene2DDiagnostic[] diagnostics, bool truncated, Scene2DDiagnostic? firstFailure)
    {
        Success = success;
        Diagnostics = Array.AsReadOnly(diagnostics);
        DiagnosticsTruncated = truncated;
        FirstFailure = firstFailure;
    }

    public bool Success { get; }
    public IReadOnlyList<Scene2DDiagnostic> Diagnostics { get; }
    public bool DiagnosticsTruncated { get; }
    internal Scene2DDiagnostic? FirstFailure { get; }
}

public static class Scene2DModelValidator
{
    private const string DiagnosticKey = "Cerneala.Scene2D.DiagnosticCode";
    internal const int MaximumCells = 1_048_576;
    internal const int MaximumChunks = 65_536;
    internal const int MaximumLayers = 4_096;
    internal const int MaximumShapePoints = 4_096;
    internal const int MaximumEntities = 65_536;
    internal const int MaximumExpandedTileColliders = 65_536;

    public static uint ParseCollisionBits(object value)
    {
        if (value is uint bits) { return bits; }
        if (value is int intValue && intValue >= 0) { return (uint)intValue; }
        if (value is long longValue && longValue >= 0 && longValue <= uint.MaxValue) { return (uint)longValue; }
        if (value is ulong ulongValue && ulongValue <= uint.MaxValue) { return (uint)ulongValue; }
        if (value is string text)
        {
            bool hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            if (uint.TryParse(hex ? text.AsSpan(2) : text.AsSpan(),
                hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out uint parsed))
            {
                return parsed;
            }
        }
        throw Diagnostic(new ArgumentException("Collision layer/mask must be an unsigned 32-bit integer or decimal/0x string.", nameof(value)), "SCN2D009");
    }

    public static Scene2DDiagnostic? GetDiagnostic(Exception exception, string filePath = "", string jsonPath = "$")
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Data[DiagnosticKey] is string code
            ? BoundDiagnosticText(new Scene2DDiagnostic(code, Scene2DDiagnosticSeverity.Error, exception.Message, filePath, jsonPath))
            : null;
    }

    public static Scene2DValidationResult Validate(
        TileMap2DModel model,
        IReadOnlyDictionary<string, DrawSize> atlasSizes,
        Scene2DValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(atlasSizes);
        Scene2DDiagnosticCollector diagnostics = new(options);
        ValidateMap(model, atlasSizes, diagnostics, "$");
        return diagnostics.Complete();
    }

    public static Scene2DValidationResult Validate(Scene2DDocument document, Scene2DValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        Scene2DDiagnosticCollector diagnostics = new(options);
        Dictionary<string, DrawSize> atlases = new(StringComparer.Ordinal);
        foreach (Scene2DAsset asset in document.Assets)
        {
            if (diagnostics.StopIfFull()) { return diagnostics.Complete(); }
            if (asset is null) { diagnostics.Error("SCN2D010", "Assets cannot contain null.", "$.assets"); }
            else if (!atlases.TryAdd(asset.ResourceId.Key, asset.Size))
            {
                diagnostics.Error("SCN2D015", $"Atlas resource ID '{asset.ResourceId.Key}' is duplicated.", "$.assets");
            }
        }
        HashSet<string> ids = new(StringComparer.Ordinal);
        long cells = 0, chunks = 0, layers = 0, entities = 0;
        for (int index = 0; index < document.Levels.Count; index++)
        {
            if (diagnostics.StopIfFull()) { break; }
            Scene2DLevel level = document.Levels[index];
            string path = $"$.levels[{index}]";
            if (level is null) { diagnostics.Error("SCN2D005", "Levels cannot contain null.", path); continue; }
            if (!ids.Add(level.Id)) { diagnostics.Error("SCN2D015", $"Level ID '{level.Id}' is duplicated.", path); }
            layers += level.TileMap.Layers.Count;
            entities += level.Entities.Count + (long)level.Promotions.Count;
            foreach (TileLayer2DModel layer in level.TileMap.Layers)
            {
                chunks += layer.Chunks.Count;
                foreach (TileChunk2D chunk in layer.Chunks) { cells += chunk.Tiles.Count; }
            }
            if (layers > diagnostics.Options.MaxLayers || chunks > diagnostics.Options.MaxChunks ||
                cells > diagnostics.Options.MaxCells || entities > diagnostics.Options.MaxEntities)
            {
                diagnostics.Error("SCN2D013", "The document exceeds its aggregate layer, chunk, cell or entity budget.", path);
                break;
            }
            ValidateMap(level.TileMap, atlases, diagnostics, path + ".tileMap");
            // Level construction already validates its immutable associations
            // and placed geometry. Do not rescan them when only asset/budget
            // information is added by the enclosing document.
        }
        return diagnostics.Complete();
    }

    internal static void ValidateLevel(Scene2DLevel level, Scene2DDiagnosticCollector diagnostics, string path)
    {
        Dictionary<string, TileLayer2DModel> layersById = level.TileMap.Layers.ToDictionary(layer => layer.Id, StringComparer.Ordinal);
        foreach (TileLayer2DModel layer in level.TileMap.Layers)
        {
            foreach (TileChunk2D chunk in layer.Chunks)
            {
                if (diagnostics.StopIfFull()) { return; }
                try { ValidateChunkGeometry(level.TileMap.TileSize, chunk, layer.Offset, level.WorldOffset); }
                catch (ArgumentException error) { diagnostics.Add(GetDiagnostic(error)! with { JsonPath = path + ".worldOffset" }); }
            }
        }
        HashSet<string> entities = new(StringComparer.Ordinal);
        long entityColliders = 0;
        for (int index = 0; index < level.Entities.Count; index++)
        {
            if (diagnostics.StopIfFull()) { return; }
            Scene2DEntity entity = level.Entities[index];
            string entityPath = $"{path}.entities[{index}]";
            if (entity is null) { diagnostics.Error("SCN2D008", "Entities cannot contain null.", entityPath); continue; }
            entityColliders += entity.Colliders.Count;
            if (entityColliders > MaximumExpandedTileColliders)
            {
                diagnostics.Error("SCN2D013", "The level exceeds its entity collider descriptor budget.", entityPath);
                return;
            }
            if (!entities.Add(entity.Id)) { diagnostics.Error("SCN2D015", $"Entity ID '{entity.Id}' is duplicated.", entityPath); }
            if (!layersById.TryGetValue(entity.LayerId, out TileLayer2DModel? entityLayer))
            {
                diagnostics.Error("SCN2D015", $"Entity layer '{entity.LayerId}' does not exist.", entityPath + ".layerId");
            }
            else
            {
                Matrix3x2 placement = Matrix3x2.CreateRotation(entity.Rotation) * Matrix3x2.CreateTranslation(
                    entity.Position.X + entityLayer!.Offset.X + level.WorldOffset.X,
                    entity.Position.Y + entityLayer.Offset.Y + level.WorldOffset.Y);
                foreach (TileColliderDescriptor2D collider in entity.Colliders)
                {
                    if (diagnostics.StopIfFull()) { return; }
                    try { collider.ValidateGeometry(placement); }
                    catch (ArgumentException error) { diagnostics.Add(GetDiagnostic(error)! with { JsonPath = entityPath + ".colliders" }); }
                }
            }
        }
        Dictionary<string, Dictionary<TileCoordinate2D, TileCell2D?>> requested = new(StringComparer.Ordinal);
        foreach (TilePromotion2D promotion in level.Promotions)
        {
            if (promotion is null) { continue; }
            if (!requested.TryGetValue(promotion.Cell.LayerId, out Dictionary<TileCoordinate2D, TileCell2D?>? cells))
            {
                requested.Add(promotion.Cell.LayerId, cells = new());
            }
            cells.TryAdd(promotion.Cell.Coordinate, null);
        }
        // Scan requested layers once, retaining only the sparse requested cells.
        // This bounds validation by map cells + promotions, not their product.
        foreach ((string layerId, Dictionary<TileCoordinate2D, TileCell2D?> cells) in requested)
        {
            if (!layersById.TryGetValue(layerId, out TileLayer2DModel? layer)) { continue; }
            foreach (TileChunk2D chunk in layer.Chunks)
            {
                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCoordinate2D coordinate = new(chunk.Origin.X + index % chunk.Width, chunk.Origin.Y + index / chunk.Width);
                    if (cells.ContainsKey(coordinate)) { cells[coordinate] = chunk.Tiles[index]; }
                }
            }
        }
        HashSet<TileCellKey2D> promotions = new();
        for (int index = 0; index < level.Promotions.Count; index++)
        {
            if (diagnostics.StopIfFull()) { return; }
            TilePromotion2D promotion = level.Promotions[index];
            string promotionPath = $"{path}.promotions[{index}]";
            if (promotion is null || !promotions.Add(promotion.Cell))
            {
                diagnostics.Error("SCN2D012", "Promotion is null or its address is duplicated.", promotionPath);
                continue;
            }
            if (requested[promotion.Cell.LayerId][promotion.Cell.Coordinate] is not TileCell2D cell ||
                !level.TileMap.TryResolveTile(promotion.TileId ?? cell.TileId, out _, out _))
            {
                diagnostics.Error("SCN2D012", "Promotion must address an existing cell and resolve a positive tile ID (an empty cell requires an override).", promotionPath);
            }
        }
    }

    internal static void ThrowIfInvalid(Scene2DValidationResult result, string parameter)
    {
        if (!result.Success)
        {
            Scene2DDiagnostic first = result.FirstFailure!;
            throw Diagnostic(new ArgumentException(first.Message, parameter), first.Code);
        }
    }

    internal static void ValidateChunkGeometry(DrawSize tileSize, TileChunk2D chunk, DrawPoint layerOffset, DrawPoint worldOffset = default)
    {
        try
        {
            DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)chunk.Origin.X * tileSize.Width + layerOffset.X + worldOffset.X), nameof(chunk));
            DrawArgument.ThrowIfNotValidPixelCoordinate((float)((double)chunk.Origin.Y * tileSize.Height + layerOffset.Y + worldOffset.Y), nameof(chunk));
            DrawArgument.ThrowIfNotValidPixelCoordinate((float)(((long)chunk.Origin.X + chunk.Width) * (double)tileSize.Width + layerOffset.X + worldOffset.X), nameof(chunk));
            DrawArgument.ThrowIfNotValidPixelCoordinate((float)(((long)chunk.Origin.Y + chunk.Height) * (double)tileSize.Height + layerOffset.Y + worldOffset.Y), nameof(chunk));
            DrawArgument.ThrowIfNegativeOrNotValidPixelSize(chunk.Width * tileSize.Width, nameof(chunk));
            DrawArgument.ThrowIfNegativeOrNotValidPixelSize(chunk.Height * tileSize.Height, nameof(chunk));
        }
        catch (ArgumentException error) { throw Diagnostic(error, "SCN2D014"); }
    }

    internal static void ValidateMap(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize> atlasSizes,
        Scene2DDiagnosticCollector diagnostics, string path, bool requireAllAtlases = true)
    {
        long chunks = 0;
        long cells = 0;
        foreach (TileLayer2DModel layer in model.Layers)
        {
            chunks += layer.Chunks.Count;
            foreach (TileChunk2D chunk in layer.Chunks) { cells += chunk.Tiles.Count; }
        }
        if (model.Layers.Count > diagnostics.Options.MaxLayers || chunks > diagnostics.Options.MaxChunks || cells > diagnostics.Options.MaxCells)
        {
            diagnostics.Error("SCN2D013", "The model exceeds the configured layer, chunk or cell budget.", path);
            return;
        }
        for (int setIndex = 0; setIndex < model.TileSets.Count; setIndex++)
        {
            if (diagnostics.StopIfFull()) { return; }
            TileSet2D set = model.TileSets[setIndex];
            string setPath = $"{path}.tileSets[{setIndex}]";
            if (!atlasSizes.TryGetValue(set.AtlasResourceId.Key, out DrawSize size))
            {
                if (requireAllAtlases)
                {
                    diagnostics.Error("SCN2D010", $"Atlas '{set.AtlasResourceId.Key}' is not declared.", setPath + ".atlasResourceId");
                }
                continue;
            }
            if (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width <= 0 || size.Height <= 0)
            {
                diagnostics.Error("SCN2D007", "Atlas dimensions must be finite and positive.", setPath);
                continue;
            }
            for (int tileIndex = 0; tileIndex < set.Tiles.Count; tileIndex++)
            {
                if (diagnostics.StopIfFull()) { return; }
                DrawRect source = set.Tiles[tileIndex].SourceRect;
                if (source.X < 0 || source.Y < 0 ||
                    (double)source.X + source.Width > size.Width || (double)source.Y + source.Height > size.Height)
                {
                    diagnostics.Error("SCN2D007", $"Tile {set.Tiles[tileIndex].Id} source rectangle exceeds atlas '{set.AtlasResourceId.Key}'.",
                        $"{setPath}.tiles[{tileIndex}].sourceRect");
                }
            }
        }
    }

    internal static T Diagnostic<T>(T exception, string code) where T : Exception
    {
        exception.Data[DiagnosticKey] = code;
        return exception;
    }

    internal static Scene2DDiagnostic BoundDiagnosticText(Scene2DDiagnostic diagnostic)
    {
        static string Limit(string value) => value.Length <= 4096 ? value : string.Concat(value.AsSpan(0, 4093), "...");
        if (diagnostic.Message.Length <= 4096 && diagnostic.FilePath.Length <= 4096 && diagnostic.JsonPath.Length <= 4096) { return diagnostic; }
        return diagnostic with { Message = Limit(diagnostic.Message), FilePath = Limit(diagnostic.FilePath), JsonPath = Limit(diagnostic.JsonPath) };
    }

    internal static IReadOnlyList<Vector2> ParseShapePoints(string? value, int minimum, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Diagnostic(new ArgumentException("Collider points cannot be empty.", nameof(value)), "SCN2D008");
        }
        if (value.Length > MaximumShapePoints * 96)
        {
            throw Diagnostic(new ArgumentException("Collider point text exceeds its size limit.", nameof(value)), "SCN2D013");
        }
        string[] tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > MaximumShapePoints)
        {
            throw Diagnostic(new ArgumentException("Collider point count exceeds its size limit.", nameof(value)), "SCN2D013");
        }
        if (tokens.Length < minimum || tokens.Length > maximum)
        {
            throw Diagnostic(new ArgumentException($"Collider requires between {minimum} and {maximum} points.", nameof(value)), "SCN2D008");
        }
        Vector2[] parsed = new Vector2[tokens.Length];
        for (int index = 0; index < tokens.Length; index++)
        {
            string[] coordinates = tokens[index].Split(',');
            if (coordinates.Length != 2 ||
                !float.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.IsFinite(x) || !float.IsFinite(y))
            {
                throw Diagnostic(new ArgumentException("Collider points require finite invariant 'x,y' coordinates.", nameof(value)), "SCN2D008");
            }
            parsed[index] = new Vector2(x, y);
            try
            {
                DrawArgument.ThrowIfNotValidPixelCoordinate(x, nameof(value));
                DrawArgument.ThrowIfNotValidPixelCoordinate(y, nameof(value));
            }
            catch (ArgumentException error) { throw Diagnostic(error, "SCN2D008"); }
        }
        return Array.AsReadOnly(parsed);
    }

    internal static T[] CopyBounded<T>(IEnumerable<T> source, int maximum, string parameter, string code = "SCN2D013")
    {
        ArgumentNullException.ThrowIfNull(source, parameter);
        List<T> values = new();
        foreach (T value in source)
        {
            if (values.Count == maximum)
            {
                throw Diagnostic(new ArgumentException($"'{parameter}' exceeds its maximum count of {maximum}.", parameter), code);
            }
            values.Add(value);
        }
        return values.ToArray();
    }
}

public sealed class Scene2DDiagnosticCollector
{
    private readonly List<Scene2DDiagnostic> diagnostics = new();
    private bool success = true;
    private bool truncated;
    private Scene2DDiagnostic? firstFailure;

    public Scene2DDiagnosticCollector(int maxDiagnostics = 128)
        : this(new Scene2DValidationOptions { MaxDiagnostics = maxDiagnostics })
    {
    }

    internal Scene2DDiagnosticCollector(Scene2DValidationOptions? options)
    {
        Options = options ?? new();
        if (Options.MaxDiagnostics <= 0 || Options.MaxCells <= 0 || Options.MaxChunks <= 0 ||
            Options.MaxLayers <= 0 || Options.MaxEntities <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Validation budgets must be positive.");
        }
    }

    internal Scene2DValidationOptions Options { get; }

    internal void Error(string code, string message, string path) =>
        Add(new Scene2DDiagnostic(code, Scene2DDiagnosticSeverity.Error, message, JsonPath: path));

    public void Add(Scene2DDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        diagnostic = Scene2DModelValidator.BoundDiagnosticText(diagnostic);
        success &= diagnostic.Severity == Scene2DDiagnosticSeverity.Warning;
        if (diagnostic.Severity != Scene2DDiagnosticSeverity.Warning) { firstFailure ??= diagnostic; }
        if (diagnostics.Count < Options.MaxDiagnostics) { diagnostics.Add(diagnostic); }
        else { truncated = true; }
    }

    internal bool StopIfFull()
    {
        if (firstFailure is null || diagnostics.Count < Options.MaxDiagnostics) { return false; }
        truncated = true;
        return true;
    }

    public Scene2DValidationResult Complete() => new(success, diagnostics.ToArray(), truncated, firstFailure);
}
