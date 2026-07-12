using System.Security.Cryptography;
using System.Text;

namespace RoslynRepoIndexer.Core;

internal static class BinaryIndexCodec
{
    private const string StringsMagic = "RISTR001";
    private const string DocumentsMagic = "RIDOC001";
    private const string SymbolsMagic = "RISYM001";
    private const string ReferencesMagic = "RIREF001";
    private const string TokensMagic = "RITOK001";
    private const int ChecksumLength = 32;

    public static void Write(string directory, IndexSnapshot snapshot)
    {
        var strings = StringTable.Create(snapshot);
        WriteTable(Path.Combine(directory, "strings.bin"), StringsMagic, snapshot.Manifest.GenerationId, strings.Values.Count, writer =>
        {
            foreach (var value in strings.Values)
            {
                WriteRawString(writer, value);
            }
        });
        WriteTable(Path.Combine(directory, "documents.bin"), DocumentsMagic, snapshot.Manifest.GenerationId, snapshot.Documents.Count, writer => WriteDocuments(writer, strings, snapshot.Documents));
        WriteTable(Path.Combine(directory, "symbols.bin"), SymbolsMagic, snapshot.Manifest.GenerationId, snapshot.Symbols.Count, writer => WriteSymbols(writer, strings, snapshot.Symbols));
        WriteTable(Path.Combine(directory, "references.bin"), ReferencesMagic, snapshot.Manifest.GenerationId, snapshot.References.Count, writer => WriteReferences(writer, strings, snapshot.References));
        WriteTable(Path.Combine(directory, "tokens.bin"), TokensMagic, snapshot.Manifest.GenerationId, snapshot.Tokens.Count, writer => WriteTokens(writer, strings, snapshot.Tokens));
    }

    public static IndexSnapshot Read(string directory, IndexManifest manifest)
    {
        var stringPayload = ReadTable(Path.Combine(directory, "strings.bin"), StringsMagic, manifest.GenerationId, out var stringCount);
        var strings = ReadStrings(stringPayload, stringCount);
        var documentPayload = ReadTable(Path.Combine(directory, "documents.bin"), DocumentsMagic, manifest.GenerationId, out var documentCount);
        var symbolPayload = ReadTable(Path.Combine(directory, "symbols.bin"), SymbolsMagic, manifest.GenerationId, out var symbolCount);
        var referencePayload = ReadTable(Path.Combine(directory, "references.bin"), ReferencesMagic, manifest.GenerationId, out var referenceCount);
        var tokenPayload = ReadTable(Path.Combine(directory, "tokens.bin"), TokensMagic, manifest.GenerationId, out var tokenCount);
        return new IndexSnapshot(
            manifest,
            ReadDocuments(documentPayload, strings, documentCount),
            ReadSymbols(symbolPayload, strings, symbolCount),
            ReadReferences(referencePayload, strings, referenceCount),
            ReadTokens(tokenPayload, strings, tokenCount));
    }

    public static void ValidateHeaders(string directory, IndexManifest manifest)
    {
        ValidateHeader(Path.Combine(directory, "strings.bin"), StringsMagic, manifest.GenerationId);
        ValidateHeader(Path.Combine(directory, "documents.bin"), DocumentsMagic, manifest.GenerationId);
        ValidateHeader(Path.Combine(directory, "symbols.bin"), SymbolsMagic, manifest.GenerationId);
        ValidateHeader(Path.Combine(directory, "references.bin"), ReferencesMagic, manifest.GenerationId);
        ValidateHeader(Path.Combine(directory, "tokens.bin"), TokensMagic, manifest.GenerationId);
    }

    internal static byte[] EncodeSegment(
        IReadOnlyList<DocumentEntry> documents,
        IReadOnlyList<SymbolEntry> symbols,
        IReadOnlyList<ReferenceEntry> references,
        IReadOnlyList<TokenPosting> tokens)
    {
        var strings = StringTable.Create(documents, symbols, references, tokens);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write7BitEncodedInt(strings.Values.Count);
        foreach (var value in strings.Values) WriteRawString(writer, value);
        writer.Write7BitEncodedInt(documents.Count); WriteDocuments(writer, strings, documents);
        writer.Write7BitEncodedInt(symbols.Count); WriteSymbols(writer, strings, symbols);
        writer.Write7BitEncodedInt(references.Count); WriteReferences(writer, strings, references);
        writer.Write7BitEncodedInt(tokens.Count); WriteTokens(writer, strings, tokens);
        writer.Flush();
        return stream.ToArray();
    }

    internal static BinarySegmentRows DecodeSegment(byte[] payload)
    {
        using var reader = Reader(payload);
        var stringCount = reader.Read7BitEncodedInt();
        if (stringCount < 0) throw new InvalidDataException("Segment string count is invalid.");
        var strings = new string[stringCount + 1];
        strings[0] = string.Empty;
        for (var i = 1; i <= stringCount; i++) strings[i] = ReadRawString(reader);
        var documents = ReadDocumentsFrom(reader, strings, ReadCount(reader));
        var symbols = ReadSymbolsFrom(reader, strings, ReadCount(reader));
        var references = ReadReferencesFrom(reader, strings, ReadCount(reader));
        var tokens = ReadTokensFrom(reader, strings, ReadCount(reader));
        EnsureConsumed(reader);
        return new BinarySegmentRows(documents, symbols, references, tokens);
    }

    private static void WriteDocuments(BinaryWriter writer, StringTable strings, IReadOnlyList<DocumentEntry> rows)
    {
        foreach (var row in rows)
        {
            strings.Write(writer, row.DocumentId); strings.Write(writer, row.ProjectId); strings.Write(writer, row.RelativePath); strings.Write(writer, row.ProjectName); strings.Write(writer, row.Language);
            writer.Write(row.IsCSharp); writer.Write(row.IsGenerated); writer.Write(row.IsNonCSharpText);
            writer.Write7BitEncodedInt64(row.LengthBytes); writer.Write(row.LastWriteUtc.Ticks); writer.Write(row.LastWriteUtc.Offset.Ticks);
            strings.Write(writer, row.ContentHash); strings.Write(writer, row.DeclarationHash); writer.Write7BitEncodedInt(row.LineCount);
        }
    }

    private static DocumentEntry[] ReadDocuments(byte[] payload, string[] strings, int count)
    {
        using var reader = Reader(payload);
        var rows = ReadDocumentsFrom(reader, strings, count);
        EnsureConsumed(reader); return rows;
    }

    private static DocumentEntry[] ReadDocumentsFrom(BinaryReader reader, string[] strings, int count)
    {
        var rows = new DocumentEntry[count];
        for (var i = 0; i < count; i++)
        {
            rows[i] = new DocumentEntry(S(strings, reader), NS(strings, reader), S(strings, reader), NS(strings, reader), S(strings, reader), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.Read7BitEncodedInt64(), new DateTimeOffset(reader.ReadInt64(), TimeSpan.FromTicks(reader.ReadInt64())), S(strings, reader), S(strings, reader), reader.Read7BitEncodedInt());
        }
        return rows;
    }

    private static void WriteSymbols(BinaryWriter writer, StringTable strings, IReadOnlyList<SymbolEntry> rows)
    {
        foreach (var row in rows)
        {
            strings.Write(writer, row.SymbolId); strings.Write(writer, row.DocumentId); strings.Write(writer, row.ProjectId); strings.Write(writer, row.Kind); strings.Write(writer, row.Name); strings.Write(writer, row.MetadataName); strings.Write(writer, row.FullyQualifiedName); strings.Write(writer, row.ContainerName); strings.Write(writer, row.Signature); strings.Write(writer, row.Accessibility);
            strings.WriteList(writer, row.Modifiers); strings.Write(writer, row.Path);
            WriteInt(writer, row.Line); WriteInt(writer, row.Column); WriteInt(writer, row.EndLine); WriteInt(writer, row.EndColumn); WriteInt(writer, row.SpanStart); WriteInt(writer, row.SpanLength);
            writer.Write(row.IsDefinition); writer.Write(row.IsPartial); strings.WriteList(writer, row.ParameterTypes); strings.Write(writer, row.ReturnType); strings.Write(writer, row.ProjectName); strings.Write(writer, row.SymbolKey); strings.WriteList(writer, row.BaseTypeIds); strings.WriteList(writer, row.InterfaceTypeIds); strings.Write(writer, row.OverriddenSymbolId);
        }
    }

    private static SymbolEntry[] ReadSymbols(byte[] payload, string[] strings, int count)
    {
        using var reader = Reader(payload);
        var rows = ReadSymbolsFrom(reader, strings, count);
        EnsureConsumed(reader); return rows;
    }

    private static SymbolEntry[] ReadSymbolsFrom(BinaryReader reader, string[] strings, int count)
    {
        var rows = new SymbolEntry[count];
        for (var i = 0; i < count; i++)
        {
            rows[i] = new SymbolEntry(S(strings, reader), S(strings, reader), NS(strings, reader), S(strings, reader), S(strings, reader), S(strings, reader), S(strings, reader), NS(strings, reader), S(strings, reader), S(strings, reader), SL(strings, reader), S(strings, reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), reader.ReadBoolean(), reader.ReadBoolean(), SL(strings, reader), NS(strings, reader), NS(strings, reader), NS(strings, reader), SL(strings, reader), SL(strings, reader), NS(strings, reader));
        }
        return rows;
    }

    private static void WriteReferences(BinaryWriter writer, StringTable strings, IReadOnlyList<ReferenceEntry> rows)
    {
        foreach (var row in rows)
        {
            strings.Write(writer, row.ReferenceId); strings.Write(writer, row.SymbolId); strings.Write(writer, row.DocumentId); strings.Write(writer, row.ProjectId); strings.Write(writer, row.ReferencedName); strings.Write(writer, row.Path);
            WriteInt(writer, row.Line); WriteInt(writer, row.Column); WriteInt(writer, row.EndLine); WriteInt(writer, row.EndColumn); WriteInt(writer, row.SpanStart); WriteInt(writer, row.SpanLength);
            strings.Write(writer, row.ProjectName); strings.Write(writer, row.ReferenceKind); strings.Write(writer, row.ContainingSymbolId); writer.Write(row.IsInvocation);
        }
    }

    private static ReferenceEntry[] ReadReferences(byte[] payload, string[] strings, int count)
    {
        using var reader = Reader(payload);
        var rows = ReadReferencesFrom(reader, strings, count);
        EnsureConsumed(reader); return rows;
    }

    private static ReferenceEntry[] ReadReferencesFrom(BinaryReader reader, string[] strings, int count)
    {
        var rows = new ReferenceEntry[count];
        for (var i = 0; i < count; i++)
        {
            rows[i] = new ReferenceEntry(S(strings, reader), S(strings, reader), S(strings, reader), NS(strings, reader), S(strings, reader), S(strings, reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), ReadInt(reader), NS(strings, reader), S(strings, reader), NS(strings, reader), reader.ReadBoolean());
        }
        return rows;
    }

    private static void WriteTokens(BinaryWriter writer, StringTable strings, IReadOnlyList<TokenPosting> rows)
    {
        foreach (var row in rows)
        {
            strings.Write(writer, row.Token); strings.Write(writer, row.Path); WriteInt(writer, row.Line); WriteInt(writer, row.Column); strings.Write(writer, row.Field); strings.Write(writer, row.Weight); strings.Write(writer, row.ProjectName); strings.Write(writer, row.DocumentId);
        }
    }

    private static TokenPosting[] ReadTokens(byte[] payload, string[] strings, int count)
    {
        using var reader = Reader(payload);
        var rows = ReadTokensFrom(reader, strings, count);
        EnsureConsumed(reader); return rows;
    }

    private static TokenPosting[] ReadTokensFrom(BinaryReader reader, string[] strings, int count)
    {
        var rows = new TokenPosting[count];
        for (var i = 0; i < count; i++)
        {
            rows[i] = new TokenPosting(S(strings, reader), S(strings, reader), ReadInt(reader), ReadInt(reader), S(strings, reader), S(strings, reader), NS(strings, reader), NS(strings, reader));
        }
        return rows;
    }

    private static void WriteTable(string path, string magic, string generationId, int rowCount, Action<BinaryWriter> writePayload)
    {
        using var payloadStream = new MemoryStream();
        using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
        {
            writePayload(payloadWriter);
        }
        var payload = payloadStream.ToArray();
        var checksum = SHA256.HashData(payload);
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes(magic));
        writer.Write(IndexManifest.CurrentSchemaVersion);
        WriteRawString(writer, generationId);
        writer.Write(rowCount);
        writer.Write(payload.Length);
        writer.Write(checksum);
        writer.Write(payload);
    }

    private static byte[] ReadTable(string path, string magic, string generationId, out int rowCount)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        var actualMagic = Encoding.ASCII.GetString(reader.ReadBytes(8));
        if (!string.Equals(actualMagic, magic, StringComparison.Ordinal)) throw Corrupt(path, "invalid magic");
        if (reader.ReadInt32() != IndexManifest.CurrentSchemaVersion) throw Corrupt(path, "schema mismatch");
        if (!string.Equals(ReadRawString(reader), generationId, StringComparison.Ordinal)) throw Corrupt(path, "generation mismatch");
        rowCount = reader.ReadInt32();
        var payloadLength = reader.ReadInt32();
        if (rowCount < 0 || payloadLength < 0 || payloadLength > stream.Length) throw Corrupt(path, "invalid lengths");
        var checksum = reader.ReadBytes(ChecksumLength);
        var payload = reader.ReadBytes(payloadLength);
        if (checksum.Length != ChecksumLength || payload.Length != payloadLength || stream.Position != stream.Length) throw Corrupt(path, "truncated or trailing data");
        if (!CryptographicOperations.FixedTimeEquals(checksum, SHA256.HashData(payload))) throw Corrupt(path, "checksum mismatch");
        return payload;
    }

    private static void ValidateHeader(string path, string magic, string generationId)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        if (!string.Equals(Encoding.ASCII.GetString(reader.ReadBytes(8)), magic, StringComparison.Ordinal)) throw Corrupt(path, "invalid magic");
        if (reader.ReadInt32() != IndexManifest.CurrentSchemaVersion) throw Corrupt(path, "schema mismatch");
        if (!string.Equals(ReadRawString(reader), generationId, StringComparison.Ordinal)) throw Corrupt(path, "generation mismatch");
        if (reader.ReadInt32() < 0) throw Corrupt(path, "invalid row count");
        var payloadLength = reader.ReadInt32();
        if (payloadLength < 0 || stream.Position + ChecksumLength + payloadLength != stream.Length) throw Corrupt(path, "truncated or trailing data");
    }

    private static string[] ReadStrings(byte[] payload, int count)
    {
        using var reader = Reader(payload);
        var values = new string[count + 1];
        values[0] = string.Empty;
        for (var i = 1; i <= count; i++) values[i] = ReadRawString(reader);
        EnsureConsumed(reader); return values;
    }

    private static void WriteRawString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write7BitEncodedInt(bytes.Length); writer.Write(bytes);
    }

    private static string ReadRawString(BinaryReader reader)
    {
        var length = reader.Read7BitEncodedInt();
        if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid string length.");
        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }

    private static void WriteInt(BinaryWriter writer, int value)
    {
        if (value < 0) throw new InvalidDataException("Negative index values are not supported.");
        writer.Write7BitEncodedInt(value);
    }
    private static int ReadInt(BinaryReader reader) => reader.Read7BitEncodedInt();
    private static BinaryReader Reader(byte[] payload) => new(new MemoryStream(payload, writable: false), Encoding.UTF8, leaveOpen: false);
    private static string S(string[] strings, BinaryReader reader) => Get(strings, reader.Read7BitEncodedInt(), nullable: false)!;
    private static string? NS(string[] strings, BinaryReader reader) => Get(strings, reader.Read7BitEncodedInt(), nullable: true);
    private static string[] SL(string[] strings, BinaryReader reader)
    {
        var count = reader.Read7BitEncodedInt(); var values = new string[count];
        for (var i = 0; i < count; i++) values[i] = S(strings, reader);
        return values;
    }
    private static string? Get(string[] strings, int id, bool nullable)
    {
        if (id == 0 && nullable) return null;
        if (id <= 0 || id >= strings.Length) throw new InvalidDataException("String table ID is out of range.");
        return strings[id];
    }
    private static void EnsureConsumed(BinaryReader reader)
    {
        if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Binary table contains trailing data.");
    }
    private static int ReadCount(BinaryReader reader)
    {
        var count = reader.Read7BitEncodedInt();
        if (count < 0) throw new InvalidDataException("Segment row count is invalid.");
        return count;
    }
    private static InvalidDataException Corrupt(string path, string reason) => new($"Binary index table '{Path.GetFileName(path)}' is corrupt: {reason}.");

    private sealed class StringTable
    {
        private readonly Dictionary<string, int> ids;
        private StringTable(string[] values)
        {
            Values = values;
            ids = values.Select((value, index) => (value, id: index + 1)).ToDictionary(x => x.value, x => x.id, StringComparer.Ordinal);
        }
        public IReadOnlyList<string> Values { get; }
        public void Write(BinaryWriter writer, string? value) => writer.Write7BitEncodedInt(value is null ? 0 : ids[value]);
        public void WriteList(BinaryWriter writer, IReadOnlyList<string> values)
        {
            writer.Write7BitEncodedInt(values.Count); foreach (var value in values) Write(writer, value);
        }
        public static StringTable Create(IndexSnapshot snapshot)
            => Create(snapshot.Documents, snapshot.Symbols, snapshot.References, snapshot.Tokens);

        public static StringTable Create(
            IReadOnlyList<DocumentEntry> documents,
            IReadOnlyList<SymbolEntry> symbols,
            IReadOnlyList<ReferenceEntry> references,
            IReadOnlyList<TokenPosting> tokens)
        {
            var values = new HashSet<string>(StringComparer.Ordinal);
            void Add(string? value) { if (value is not null) values.Add(value); }
            void AddMany(IEnumerable<string> list) { foreach (var value in list) Add(value); }
            foreach (var x in documents) { Add(x.DocumentId); Add(x.ProjectId); Add(x.RelativePath); Add(x.ProjectName); Add(x.Language); Add(x.ContentHash); Add(x.DeclarationHash); }
            foreach (var x in symbols) { Add(x.SymbolId); Add(x.DocumentId); Add(x.ProjectId); Add(x.Kind); Add(x.Name); Add(x.MetadataName); Add(x.FullyQualifiedName); Add(x.ContainerName); Add(x.Signature); Add(x.Accessibility); AddMany(x.Modifiers); Add(x.Path); AddMany(x.ParameterTypes); Add(x.ReturnType); Add(x.ProjectName); Add(x.SymbolKey); AddMany(x.BaseTypeIds); AddMany(x.InterfaceTypeIds); Add(x.OverriddenSymbolId); }
            foreach (var x in references) { Add(x.ReferenceId); Add(x.SymbolId); Add(x.DocumentId); Add(x.ProjectId); Add(x.ReferencedName); Add(x.Path); Add(x.ProjectName); Add(x.ReferenceKind); Add(x.ContainingSymbolId); }
            foreach (var x in tokens) { Add(x.Token); Add(x.Path); Add(x.Field); Add(x.Weight); Add(x.ProjectName); Add(x.DocumentId); }
            return new StringTable(values.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
    }
}

internal sealed record BinarySegmentRows(DocumentEntry[] Documents, SymbolEntry[] Symbols, ReferenceEntry[] References, TokenPosting[] Tokens);
