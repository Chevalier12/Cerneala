using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoslynRepoIndexer.Core;

internal static class SegmentedIndexCodec
{
    private const string Magic = "RISEG003";
    private const string DescriptorFileName = "segments.json";
    private const int ChecksumLength = 32;

    public static SegmentPersistenceSummary Write(
        string indexRoot,
        string generationDirectory,
        IndexSnapshot snapshot,
        string? previousGenerationDirectory,
        IReadOnlySet<string>? dirtyDocumentIds)
    {
        var pool = Path.Combine(indexRoot, "segments");
        Directory.CreateDirectory(pool);
        var documentGroups = snapshot.Documents
            .GroupBy(document => document.DocumentId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.ToArray())
            .ToArray();
        var descriptors = new DocumentSegmentDescriptor[documentGroups.Length];
        var written = 0;
        var reused = 0;
        long totalBytes = 0;
        var symbolsByDocument = snapshot.Symbols.ToLookup(symbol => symbol.DocumentId, StringComparer.Ordinal);
        var referencesByDocument = snapshot.References.ToLookup(reference => reference.DocumentId, StringComparer.Ordinal);
        var tokensByDocument = snapshot.Tokens
            .Where(token => token.DocumentId is not null)
            .ToLookup(token => token.DocumentId!, StringComparer.Ordinal);
        var unownedTokens = snapshot.Tokens
            .Where(token => token.DocumentId is null)
            .ToLookup(token => (token.Path, token.ProjectName));
        var previousDescriptors = previousGenerationDirectory is not null && File.Exists(Path.Combine(previousGenerationDirectory, DescriptorFileName))
            ? ReadDescriptors(previousGenerationDirectory).ToDictionary(descriptor => descriptor.DocumentId, StringComparer.Ordinal)
            : new Dictionary<string, DocumentSegmentDescriptor>(StringComparer.Ordinal);

        Parallel.For(
            0,
            documentGroups.Length,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 16) },
            index =>
        {
            var documentGroup = documentGroups[index];
            var documentId = documentGroup[0].DocumentId;
            if (dirtyDocumentIds is not null &&
                !dirtyDocumentIds.Contains(documentId) &&
                previousDescriptors.TryGetValue(documentId, out var previousDescriptor))
            {
                var previousPath = Path.Combine(pool, previousDescriptor.FileName);
                if (TryGetChecksumFromFileName(previousDescriptor.FileName, out var previousChecksum) &&
                    HasValidSegmentEnvelope(previousPath, previousChecksum) &&
                    new FileInfo(previousPath).Length == previousDescriptor.Bytes)
                {
                    descriptors[index] = previousDescriptor;
                    Interlocked.Increment(ref reused);
                    Interlocked.Add(ref totalBytes, previousDescriptor.Bytes);
                    return;
                }
            }

            var documentIds = documentGroup.Select(document => document.DocumentId).ToHashSet(StringComparer.Ordinal);
            var documentKeys = documentGroup.Select(document => (document.RelativePath, document.ProjectName)).Distinct().ToArray();
            var segmentDocuments = documentGroup.OrderBy(document => document.RelativePath, StringComparer.Ordinal).ThenBy(document => document.ProjectName, StringComparer.Ordinal).ToArray();
            var segmentSymbols = documentIds.SelectMany(id => symbolsByDocument[id]).OrderBy(symbol => symbol.SpanStart).ThenBy(symbol => symbol.SymbolId, StringComparer.Ordinal).ToArray();
            var segmentReferences = documentIds.SelectMany(id => referencesByDocument[id]).OrderBy(reference => reference.SpanStart).ThenBy(reference => reference.ReferenceId, StringComparer.Ordinal).ToArray();
            var segmentTokens = documentIds.SelectMany(id => tokensByDocument[id]).Concat(documentKeys.SelectMany(key => unownedTokens[key])).OrderBy(token => token.Token, StringComparer.Ordinal).ThenBy(token => token.Line).ThenBy(token => token.Column).ToArray();
            var raw = BinaryIndexCodec.EncodeSegment(segmentDocuments, segmentSymbols, segmentReferences, segmentTokens);
            var checksum = SHA256.HashData(raw);
            var segmentId = Convert.ToHexString(checksum).ToLowerInvariant();
            var fileName = segmentId + ".bin";
            var path = Path.Combine(pool, fileName);
            if (File.Exists(path) && !HasValidSegmentEnvelope(path, checksum))
            {
                File.Delete(path);
            }
            if (!File.Exists(path))
            {
                WriteSegmentAtomic(path, raw, checksum);
                Interlocked.Increment(ref written);
            }
            else
            {
                Interlocked.Increment(ref reused);
            }

            var length = new FileInfo(path).Length;
            Interlocked.Add(ref totalBytes, length);
            var first = documentGroup[0];
            descriptors[index] = new DocumentSegmentDescriptor(first.DocumentId, first.ProjectName, first.RelativePath, fileName, length);
        });

        File.WriteAllText(Path.Combine(generationDirectory, DescriptorFileName), JsonSerializer.Serialize(descriptors, JsonOptions.Compact));
        return new SegmentPersistenceSummary(descriptors.Length, written, reused, totalBytes);
    }

    public static IndexSnapshot Read(string indexRoot, string generationDirectory, IndexManifest manifest)
    {
        var descriptors = ReadDescriptors(generationDirectory);
        if (descriptors.Count != manifest.SegmentCount)
        {
            throw new InvalidDataException($"Segment descriptor count {descriptors.Count} does not match manifest count {manifest.SegmentCount}.");
        }

        var segments = new DocumentSegment[descriptors.Count];
        try
        {
            Parallel.For(
                0,
                descriptors.Count,
                new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 16) },
                index => segments[index] = ReadSegment(Path.Combine(indexRoot, "segments", descriptors[index].FileName)));
        }
        catch (AggregateException ex)
        {
            throw new InvalidDataException("One or more document segments are corrupt.", ex.Flatten().InnerExceptions.First());
        }
        return new IndexSnapshot(
            manifest,
            segments.SelectMany(segment => segment.Documents).ToArray(),
            segments.SelectMany(segment => segment.Symbols).ToArray(),
            segments.SelectMany(segment => segment.References).ToArray(),
            segments.SelectMany(segment => segment.Tokens).ToArray());
    }

    public static void ValidateDescriptor(string indexRoot, string generationDirectory, IndexManifest manifest)
    {
        var descriptors = ValidateDescriptorMetadataCore(generationDirectory, manifest);
        foreach (var descriptor in descriptors)
        {
            var path = Path.GetFullPath(Path.Combine(indexRoot, "segments", descriptor.FileName));
            var pool = Path.GetFullPath(Path.Combine(indexRoot, "segments")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(pool, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) || new FileInfo(path).Length != descriptor.Bytes)
            {
                throw new InvalidDataException($"Segment '{descriptor.FileName}' is missing or has an invalid length.");
            }
        }
    }

    public static void ValidateDescriptorMetadata(string generationDirectory, IndexManifest manifest)
    {
        _ = ValidateDescriptorMetadataCore(generationDirectory, manifest);
    }

    public static IReadOnlySet<string> ReadReferencedSegmentFiles(string generationDirectory)
        => ReadDescriptors(generationDirectory).Select(descriptor => descriptor.FileName).ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<DocumentSegmentDescriptor> ReadDescriptors(string generationDirectory)
    {
        var path = Path.Combine(generationDirectory, DescriptorFileName);
        try
        {
            return JsonSerializer.Deserialize<DocumentSegmentDescriptor[]>(File.ReadAllText(path), JsonOptions.Compact)
                   ?? throw new InvalidDataException("Segment descriptor is invalid.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Segment descriptor contains invalid JSON.", ex);
        }
    }

    private static IReadOnlyList<DocumentSegmentDescriptor> ValidateDescriptorMetadataCore(string generationDirectory, IndexManifest manifest)
    {
        var descriptors = ReadDescriptors(generationDirectory);
        if (descriptors.Count != manifest.SegmentCount)
        {
            throw new InvalidDataException("Segment descriptor count does not match the manifest.");
        }

        var fileNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Bytes <= 0 ||
                descriptor.FileName.Length != 68 ||
                !descriptor.FileName.EndsWith(".bin", StringComparison.Ordinal) ||
                descriptor.FileName[..64].Any(character => !Uri.IsHexDigit(character) || char.IsUpper(character)) ||
                !fileNames.Add(descriptor.FileName))
            {
                throw new InvalidDataException("Segment descriptor contains invalid metadata.");
            }
        }

        return descriptors;
    }

    private static void WriteSegmentAtomic(string path, byte[] raw, byte[] checksum)
    {
        var compressed = Compress(raw);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = File.Create(temp))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(Encoding.ASCII.GetBytes(Magic));
                writer.Write(IndexManifest.CurrentSchemaVersion);
                writer.Write(raw.Length);
                writer.Write(compressed.Length);
                writer.Write(checksum);
                writer.Write(SHA256.HashData(compressed));
                writer.Write(compressed);
            }
            File.Move(temp, path, overwrite: false);
        }
        catch (IOException) when (File.Exists(path))
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static DocumentSegment ReadSegment(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != Magic) throw new InvalidDataException($"Segment '{Path.GetFileName(path)}' has invalid magic.");
        if (reader.ReadInt32() != IndexManifest.CurrentSchemaVersion) throw new InvalidDataException($"Segment '{Path.GetFileName(path)}' has an incompatible schema.");
        var rawLength = reader.ReadInt32();
        var compressedLength = reader.ReadInt32();
        var checksum = reader.ReadBytes(ChecksumLength);
        var compressedChecksum = reader.ReadBytes(ChecksumLength);
        if (rawLength < 0 || compressedLength < 0 || checksum.Length != ChecksumLength || compressedChecksum.Length != ChecksumLength || stream.Position + compressedLength != stream.Length) throw new InvalidDataException($"Segment '{Path.GetFileName(path)}' is truncated.");
        var compressed = reader.ReadBytes(compressedLength);
        if (!CryptographicOperations.FixedTimeEquals(compressedChecksum, SHA256.HashData(compressed))) throw new InvalidDataException($"Segment '{Path.GetFileName(path)}' compressed checksum mismatch.");
        var raw = Decompress(compressed, rawLength);
        if (!CryptographicOperations.FixedTimeEquals(checksum, SHA256.HashData(raw))) throw new InvalidDataException($"Segment '{Path.GetFileName(path)}' checksum mismatch.");
        var expectedName = Convert.ToHexString(checksum).ToLowerInvariant() + ".bin";
        if (!string.Equals(Path.GetFileName(path), expectedName, StringComparison.Ordinal)) throw new InvalidDataException("Segment filename does not match its content hash.");
        var rows = BinaryIndexCodec.DecodeSegment(raw);
        return new DocumentSegment(rows.Documents, rows.Symbols, rows.References, rows.Tokens);
    }

    private static bool HasValidSegmentEnvelope(string path, byte[] expectedChecksum)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
            if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != Magic || reader.ReadInt32() != IndexManifest.CurrentSchemaVersion)
            {
                return false;
            }

            var rawLength = reader.ReadInt32();
            var compressedLength = reader.ReadInt32();
            var checksum = reader.ReadBytes(ChecksumLength);
            var compressedChecksum = reader.ReadBytes(ChecksumLength);
            return rawLength >= 0 &&
                   compressedLength >= 0 &&
                   checksum.Length == ChecksumLength &&
                   compressedChecksum.Length == ChecksumLength &&
                   stream.Position + compressedLength == stream.Length &&
                   CryptographicOperations.FixedTimeEquals(checksum, expectedChecksum);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryGetChecksumFromFileName(string fileName, out byte[] checksum)
    {
        checksum = Array.Empty<byte>();
        if (fileName.Length != 68 || !fileName.EndsWith(".bin", StringComparison.Ordinal)) return false;
        try
        {
            checksum = Convert.FromHexString(fileName[..64]);
            return checksum.Length == ChecksumLength;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Compress(byte[] raw)
        => raw;

    private static byte[] Decompress(byte[] compressed, int expectedLength)
    {
        if (compressed.Length == expectedLength) return compressed;
        using var input = new MemoryStream(compressed, writable: false);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(expectedLength);
        brotli.CopyTo(output);
        var raw = output.ToArray();
        if (raw.Length != expectedLength) throw new InvalidDataException("Segment decompressed length is invalid.");
        return raw;
    }

    private sealed record DocumentSegment(DocumentEntry[] Documents, SymbolEntry[] Symbols, ReferenceEntry[] References, TokenPosting[] Tokens);
    private sealed record DocumentSegmentDescriptor(string DocumentId, string? ProjectName, string RelativePath, string FileName, long Bytes);
}

public sealed record SegmentPersistenceSummary(int SegmentCount, int SegmentsWritten, int SegmentsReused, long SegmentBytes);
