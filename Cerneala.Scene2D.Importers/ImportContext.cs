using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Importers;

// A context belongs to one synchronous import. The caller owns a stable, trusted
// root directory; content cannot choose network access or enlarge any budget.
internal sealed class ImportContext : IDisposable
{
    private readonly List<JsonDocument> documents = new();
    private readonly HashSet<string> warnedFiles = new(StringComparer.Ordinal);
    private long totalBytes;
    private int files;
    private long cells, chunks, layers, entities;

    internal ImportContext(Scene2DImportOptions options)
    {
        if (options.MaxFileBytes <= 0 || options.MaxTotalBytes <= 0 || options.MaxFiles <= 0 ||
            options.MaxJsonDepth <= 0 || options.MaxCells <= 0 || options.MaxChunks <= 0 ||
            options.MaxLayers <= 0 || options.MaxEntities <= 0 || options.MaxPoints <= 0 || options.MaxDiagnostics <= 0)
        { throw new ArgumentOutOfRangeException(nameof(options), "Import budgets must be positive."); }
        Options = options;
        Diagnostics = new(options.MaxDiagnostics);
    }

    internal Scene2DImportOptions Options { get; }
    internal Scene2DDiagnosticCollector Diagnostics { get; }
    internal string Root { get; private set; } = "";
    internal string File { get; set; } = "";
    internal string Path { get; set; } = "$";
    internal Scene2DValidationOptions ValidationOptions => new()
    {
        MaxCells = Options.MaxCells, MaxChunks = Options.MaxChunks, MaxLayers = Options.MaxLayers,
        MaxEntities = Options.MaxEntities, MaxDiagnostics = Options.MaxDiagnostics
    };

    internal string Initialize(string file)
    {
        File = file;
        string full = System.IO.Path.GetFullPath(file);
        Root = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(
            Options.AssetRootDirectory ?? System.IO.Path.GetDirectoryName(full)!));
        CheckLocalAbsolute(full);
        CheckLocalAbsolute(Root);
        CheckContained(full);
        File = full;
        return full;
    }

    internal string Resolve(string sourceFile, string reference)
    {
        string local = reference.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(local) || local.StartsWith('/') || local.Contains(':') || local.Contains('\0') ||
            System.IO.Path.IsPathRooted(local))
        { Fail("SCN2D010", "External references must be local relative paths."); }
        string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourceFile)!, local));
        CheckContained(full);
        return full;
    }

    internal string Relative(string full) => System.IO.Path.GetRelativePath(Root, full).Replace('\\', '/');

    private void CheckLocalAbsolute(string full)
    {
        // In particular reject UNC/device paths even when supplied as the root.
        if (full.StartsWith("\\\\", StringComparison.Ordinal) || full.StartsWith("//", StringComparison.Ordinal) ||
            full.Contains('\0') || full.AsSpan(OperatingSystem.IsWindows() ? 2 : 0).Contains(':'))
        { Fail("SCN2D010", "Only local filesystem roots and files are accepted."); }
    }

    private void CheckContained(string full)
    {
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string prefix = System.IO.Path.EndsInDirectorySeparator(Root) ? Root : Root + System.IO.Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, comparison) || string.Equals(full, Root, comparison))
        { Fail("SCN2D010", "The resolved file must be below the configured asset root."); }
        string current = Root;
        foreach (string component in System.IO.Path.GetRelativePath(Root, full).Split(System.IO.Path.DirectorySeparatorChar))
        {
            current = System.IO.Path.Combine(current, component);
            try
            {
                if ((System.IO.File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                { Fail("SCN2D010", "Reparse points below the asset root are prohibited."); }
            }
            catch (FileNotFoundException) { break; }
            catch (DirectoryNotFoundException) { break; }
        }
    }

    internal void RequireFile(string full)
    {
        CheckContained(full);
        using FileStream stream = System.IO.File.Open(full, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    internal JsonElement Load(string full)
    {
        File = full;
        Path = "$";
        CheckContained(full);
        if (++files > Options.MaxFiles) { Fail("SCN2D013", "JSON file count exceeds the import budget."); }
        using FileStream stream = System.IO.File.Open(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        long available = Math.Min(Options.MaxFileBytes, Options.MaxTotalBytes - totalBytes);
        if (stream.Length > available || stream.Length > int.MaxValue)
        { Fail("SCN2D013", "JSON bytes exceed the per-file or aggregate import budget."); }
        using MemoryStream buffer = new();
        byte[] block = new byte[8192];
        int read;
        while ((read = stream.Read(block)) > 0)
        {
            if (buffer.Length + read > available) { Fail("SCN2D013", "JSON bytes exceed the import budget."); }
            buffer.Write(block, 0, read);
        }
        totalBytes += buffer.Length;
        byte[] bytes = buffer.ToArray();
        // Check depth independently of exception-message wording. JSON syntax is
        // still validated by the framework reader and the owned document below.
        Utf8JsonReader reader = new(bytes, new JsonReaderOptions { MaxDepth = int.MaxValue });
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray && reader.CurrentDepth >= Options.MaxJsonDepth)
            { Fail("SCN2D013", "JSON nesting exceeds the import depth budget."); }
        }
        JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = Options.MaxJsonDepth });
        documents.Add(document);
        CheckDuplicates(document.RootElement, new(), new());
        return document.RootElement;
    }

    private void CheckDuplicates(JsonElement value, List<HashSet<string>> scopes, List<JsonPathPart> path)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            // Reuse one name set per nesting level. A point-heavy JSON file must
            // not allocate a set and a full diagnostic path for every vertex.
            while (scopes.Count <= path.Count) { scopes.Add(new(StringComparer.Ordinal)); }
            HashSet<string> names = scopes[path.Count];
            names.Clear();
            foreach (JsonProperty property in value.EnumerateObject())
            {
                string name = property.Name;
                path.Add(new(name, 0));
                if (!names.Add(name))
                {
                    Path = "$" + string.Concat(path.Select(part => part.Name is null
                        ? "[" + part.Index.ToString(CultureInfo.InvariantCulture) + "]" : "." + part.Name));
                    Fail("SCN2D015", "Duplicate JSON member.");
                }
                CheckDuplicates(property.Value, scopes, path);
                path.RemoveAt(path.Count - 1);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                path.Add(new(null, index++));
                CheckDuplicates(item, scopes, path);
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    private readonly record struct JsonPathPart(string? Name, int Index);

    internal void CountCells(long count)
    {
        if (count <= 0 || count > int.MaxValue / 4) { Fail("SCN2D005", "Cell dimensions are invalid or exceed the addressable decoded size."); }
        cells += count;
        if (cells > Options.MaxCells) { Fail("SCN2D013", "Decoded cells exceed the import budget."); }
    }

    internal void CountChunk() { if (++chunks > Options.MaxChunks) { Fail("SCN2D013", "Chunk count exceeds the import budget."); } }
    internal void CountLayer() { if (++layers > Options.MaxLayers) { Fail("SCN2D013", "Layer count exceeds the import budget."); } }
    internal void CountEntity() { if (++entities > Options.MaxEntities) { Fail("SCN2D013", "Entity count exceeds the import budget."); } }

    internal void Fields(JsonElement value, string mapped, string metadata = "", string editor = "",
        Dictionary<string, object?>? properties = null)
    {
        Object(value);
        HashSet<string> accepted = new(mapped.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        HashSet<string> retained = new(metadata.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        HashSet<string> ignored = new(editor.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        string path = Path;
        foreach (JsonProperty field in value.EnumerateObject())
        {
            Path = path + "." + field.Name;
            if (accepted.Contains(field.Name)) { continue; }
            if (retained.Contains(field.Name))
            {
                if (properties is null) { throw new InvalidOperationException("Metadata requires an owning property bag."); }
                properties["$" + field.Name] = field.Value.Clone();
            }
            else if (ignored.Contains(field.Name))
            {
                if (warnedFiles.Add(File))
                { Diagnostics.Add(new("SCN2D017", Scene2DDiagnosticSeverity.Warning, "Known editor-only fields are not used at runtime.", File, Path)); }
            }
            else { Fail("SCN2D004", $"Field '{field.Name}' is outside the supported v1 subset."); }
        }
        Path = path;
    }

    internal JsonElement Required(JsonElement value, string name)
    {
        Object(value);
        if (!value.TryGetProperty(name, out JsonElement field)) { Path += "." + name; Fail("SCN2D002", $"Required field '{name}' is missing."); }
        return field;
    }

    internal void Object(JsonElement value) { if (value.ValueKind != JsonValueKind.Object) { Fail("SCN2D002", "Expected a JSON object."); } }
    internal JsonElement.ArrayEnumerator Array(JsonElement value)
    { if (value.ValueKind != JsonValueKind.Array) { Fail("SCN2D002", "Expected a JSON array."); } return value.EnumerateArray(); }
    internal string Text(JsonElement value)
    { if (value.ValueKind != JsonValueKind.String) { Fail("SCN2D002", "Expected a string."); } return value.GetString()!; }
    internal string Text(JsonElement value, string name, string fallback = "") => value.TryGetProperty(name, out JsonElement field) ? Text(field) : fallback;
    internal int Int(JsonElement value, string code = "SCN2D002")
    { if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number)) { Fail(code, "Expected an Int32 integer."); } return value.GetInt32(); }
    internal int Int(JsonElement value, string name, int fallback, string code = "SCN2D002") => value.TryGetProperty(name, out JsonElement field) ? Int(field, code) : fallback;
    internal float Number(JsonElement value)
    { if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out float number) || !float.IsFinite(number)) { Fail("SCN2D014", "Expected a finite single-precision number."); } return value.GetSingle(); }
    internal float Number(JsonElement value, string name, float fallback = 0) => value.TryGetProperty(name, out JsonElement field) ? Number(field) : fallback;
    internal bool Boolean(JsonElement value)
    { if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) { Fail("SCN2D016", "Expected a Boolean value."); } return value.GetBoolean(); }
    internal bool Boolean(JsonElement value, string name, bool fallback = false) => value.TryGetProperty(name, out JsonElement field) ? Boolean(field) : fallback;

    internal void Expect(JsonElement value, string name, string expected)
    { if (Text(value, name, expected) != expected) { Path += "." + name; Fail("SCN2D004", $"Only '{expected}' is supported for '{name}'."); } }
    internal void Expect(JsonElement value, string name, float expected)
    { if (Number(value, name, expected) != expected) { Path += "." + name; Fail("SCN2D004", $"Only {expected.ToString(CultureInfo.InvariantCulture)} is supported for '{name}'."); } }

    internal void Record(Exception error)
    {
        if (error is ImportFailure) { return; }
        Scene2DDiagnostic? core = Scene2DModelValidator.GetDiagnostic(error, File, Path);
        if (core is not null) { Diagnostics.Add(core); return; }
        string code = error switch
        {
            JsonException => "SCN2D002",
            UnauthorizedAccessException or IOException => "SCN2D001",
            OverflowException => "SCN2D014",
            ArgumentException => "SCN2D014",
            _ => throw new InvalidOperationException("Unexpected importer failure.", error)
        };
        Diagnostics.Add(new(code, code == "SCN2D001" ? Scene2DDiagnosticSeverity.Fatal : Scene2DDiagnosticSeverity.Error,
            error.Message, File, error is JsonException json ? json.Path ?? Path : Path));
    }

    [DoesNotReturn]
    internal void Fail(string code, string message)
    {
        Scene2DDiagnosticSeverity severity = code switch
        {
            "SCN2D001" or "SCN2D003" or "SCN2D010" => Scene2DDiagnosticSeverity.Fatal,
            "SCN2D004" => Scene2DDiagnosticSeverity.Unsupported,
            _ => Scene2DDiagnosticSeverity.Error
        };
        Diagnostics.Add(new(code, severity, message, File, Path));
        throw new ImportFailure();
    }

    public void Dispose() { foreach (JsonDocument document in documents) { document.Dispose(); } }
}

internal sealed class ImportFailure : Exception;
