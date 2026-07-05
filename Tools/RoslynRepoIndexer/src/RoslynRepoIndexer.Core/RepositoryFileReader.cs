namespace RoslynRepoIndexer.Core;

public sealed class RepositoryFileReader
{
    public RepositoryFileReadResult Read(string repoRoot, string filePath, IndexerConfig config)
    {
        var file = LoadFile(repoRoot, filePath, config);
        return new RepositoryFileReadResult(
            file.RelativePath,
            RepositoryDiscovery.NormalizeRoot(repoRoot),
            file.Language,
            file.LineCount,
            file.SizeBytes,
            file.ContentHash,
            file.LastModifiedUtc,
            file.IsIndexed,
            file.Content);
    }

    public RepositoryPartialFileReadResult ReadRange(string repoRoot, string filePath, IndexerConfig config, int startLine, int endLine)
    {
        if (startLine < 1 || endLine < 1)
        {
            throw new RepositoryFileReadException("Line numbers are 1-based and must be greater than zero.");
        }

        if (startLine > endLine)
        {
            throw new RepositoryFileReadException("startLine cannot be greater than endLine.");
        }

        var file = LoadFile(repoRoot, filePath, config);
        if (startLine > file.LineCount)
        {
            throw new RepositoryFileReadException("startLine cannot be greater than the file line count.");
        }

        var clampedEndLine = Math.Min(endLine, file.LineCount);
        var selected = SelectLines(file.Lines, startLine, clampedEndLine);
        return CreatePartial(file, "range", startLine, clampedEndLine, selected, null, null);
    }

    public RepositoryPartialFileReadResult ReadAround(string repoRoot, string filePath, IndexerConfig config, int targetLine, int context)
    {
        if (targetLine < 1)
        {
            throw new RepositoryFileReadException("Line numbers are 1-based and must be greater than zero.");
        }

        if (context < 0)
        {
            throw new RepositoryFileReadException("--context must be a non-negative line count.");
        }

        var file = LoadFile(repoRoot, filePath, config);
        if (targetLine > file.LineCount)
        {
            throw new RepositoryFileReadException("targetLine cannot be greater than the file line count.");
        }

        var startLine = Math.Max(1, targetLine - context);
        var endLine = Math.Min(file.LineCount, targetLine + context);
        var selected = SelectLines(file.Lines, startLine, endLine);
        return CreatePartial(file, "around", startLine, endLine, selected, targetLine, context);
    }

    private static RepositoryPartialFileReadResult CreatePartial(
        LoadedRepositoryFile file,
        string selectionMode,
        int startLine,
        int endLine,
        string content,
        int? targetLine,
        int? context)
    {
        return new RepositoryPartialFileReadResult(
            file.RelativePath,
            file.RepoRoot,
            file.Language,
            file.LineCount,
            file.SizeBytes,
            file.ContentHash,
            file.LastModifiedUtc,
            file.IsIndexed,
            selectionMode,
            startLine,
            endLine,
            endLine - startLine + 1,
            content,
            targetLine,
            context);
    }

    private static LoadedRepositoryFile LoadFile(string repoRoot, string filePath, IndexerConfig config)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new RepositoryFileReadException("Missing file path.");
        }

        var normalizedRoot = RepositoryDiscovery.NormalizeRoot(repoRoot);
        var fullPath = ResolvePathInsideRepo(normalizedRoot, filePath, out var relativePath);
        if (Directory.Exists(fullPath))
        {
            throw new RepositoryFileReadException($"Path '{relativePath}' points to a directory, not a file.");
        }

        if (!File.Exists(fullPath))
        {
            throw new RepositoryFileReadException($"File '{relativePath}' does not exist.");
        }

        var info = new FileInfo(fullPath);
        if (info.Length > config.MaxTextFileBytes)
        {
            throw new RepositoryFileReadException($"File '{relativePath}' exceeds maxTextFileBytes ({config.MaxTextFileBytes} bytes).");
        }

        if (BinaryFileDetector.IsBinaryFile(fullPath))
        {
            throw new RepositoryFileReadException($"File '{relativePath}' appears to be binary and cannot be read as text.");
        }

        var content = NormalizeLineEndings(File.ReadAllText(fullPath));
        var lines = SplitLogicalLines(content);
        var contentHash = "sha256:" + DocumentHasher.HashText(content);
        return new LoadedRepositoryFile(
            normalizedRoot,
            relativePath,
            DetectLanguage(relativePath),
            lines.Length,
            info.Length,
            contentHash,
            new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            IsIndexed(normalizedRoot, relativePath),
            content,
            lines);
    }

    private static string ResolvePathInsideRepo(string repoRoot, string filePath, out string relativePath)
    {
        var candidate = Path.IsPathFullyQualified(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(repoRoot, filePath));

        if (!IsInsideRoot(repoRoot, candidate))
        {
            throw new RepositoryFileReadException($"Path '{filePath}' is outside the repository root.");
        }

        relativePath = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repoRoot, candidate));
        return candidate;
    }

    private static bool IsInsideRoot(string repoRoot, string fullPath)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(repoRoot, fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), comparison))
        {
            return true;
        }

        var rootWithSeparator = repoRoot.EndsWith(Path.DirectorySeparatorChar)
            ? repoRoot
            : repoRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(rootWithSeparator, comparison);
    }

    private static string NormalizeLineEndings(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static string[] SplitLogicalLines(string content)
    {
        if (content.Length == 0)
        {
            return Array.Empty<string>();
        }

        var withoutFinalTerminator = content.EndsWith('\n') ? content[..^1] : content;
        return withoutFinalTerminator.Length == 0 ? Array.Empty<string>() : withoutFinalTerminator.Split('\n');
    }

    private static string SelectLines(string[] lines, int startLine, int endLine)
        => string.Join('\n', lines.Skip(startLine - 1).Take(endLine - startLine + 1));

    private static string DetectLanguage(string relativePath)
    {
        var extension = Path.GetExtension(relativePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".csproj" or ".slnx" or ".xml" => "xml",
            ".json" => "json",
            ".md" or ".markdown" => "markdown",
            ".yml" or ".yaml" => "yaml",
            ".ps1" => "powershell",
            ".sh" => "shell",
            _ => "text"
        };
    }

    private static bool IsIndexed(string repoRoot, string relativePath)
    {
        if (!IndexStore.Exists(repoRoot))
        {
            return false;
        }

        try
        {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return IndexStore.Read(repoRoot).Documents.Any(document => string.Equals(document.RelativePath, relativePath, comparison));
        }
        catch
        {
            return false;
        }
    }

    private sealed record LoadedRepositoryFile(
        string RepoRoot,
        string RelativePath,
        string Language,
        int LineCount,
        long SizeBytes,
        string ContentHash,
        DateTimeOffset LastModifiedUtc,
        bool IsIndexed,
        string Content,
        string[] Lines);
}
