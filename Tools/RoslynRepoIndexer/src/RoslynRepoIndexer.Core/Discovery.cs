using System.Diagnostics;

namespace RoslynRepoIndexer.Core;

public static class RepositoryDiscovery
{
    public static string? TryComputeGitStateFingerprint(string repoRoot)
    {
        if (!Directory.Exists(Path.Combine(repoRoot, ".git")))
        {
            return null;
        }

        try
        {
            var head = ReadGitHead(repoRoot);
            var status = RunGit(repoRoot, "-c", "core.quotepath=false", "status", "--porcelain=v1", "--untracked-files=all");
            if (string.IsNullOrWhiteSpace(head))
            {
                return null;
            }

            var entries = new List<string> { "HEAD|" + head };
            foreach (var line in status.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var path = line.Length > 3 ? line[3..] : string.Empty;
                var renameSeparator = path.IndexOf(" -> ", StringComparison.Ordinal);
                if (renameSeparator >= 0)
                {
                    path = path[(renameSeparator + 4)..];
                }

                path = NormalizeRelative(path.Trim('"'));
                var fullPath = Path.Combine(repoRoot, path);
                var state = "deleted";
                if (File.Exists(fullPath))
                {
                    var info = new FileInfo(fullPath);
                    state = $"{info.Length}|{info.LastWriteTimeUtc.Ticks}";
                }
                entries.Add(line[..Math.Min(2, line.Length)] + "|" + path + "|" + state);
            }

            return ConfigLoader.HashText(string.Join('\n', entries.OrderBy(entry => entry, StringComparer.Ordinal)));
        }
        catch
        {
            return null;
        }
    }

    public static RepositoryRoot FindRoot(string? startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath ?? Directory.GetCurrentDirectory()));
        if (File.Exists(current.FullName))
        {
            current = current.Parent ?? throw new DirectoryNotFoundException("Cannot detect repository root from file path.");
        }

        for (var directory = current; directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return new RepositoryRoot(NormalizeRoot(directory.FullName), RepositoryRootKind.Git);
            }
        }

        for (var directory = current; directory is not null; directory = directory.Parent)
        {
            if (Directory.EnumerateFiles(directory.FullName, "*.sln").Any()
                || Directory.EnumerateFiles(directory.FullName, "*.slnx").Any()
                || Directory.EnumerateFiles(directory.FullName, "*.csproj").Any())
            {
                return new RepositoryRoot(NormalizeRoot(directory.FullName), RepositoryRootKind.WorkspaceFile);
            }
        }

        throw new InvalidOperationException("No repository root found. Expected .git, .sln, .slnx or .csproj in current path ancestors.");
    }

    public static IReadOnlyList<CandidateFile> EnumerateCandidateFiles(string repoRoot, IndexerConfig config)
    {
        var files = TryGitLsFiles(repoRoot);
        if (files.Count == 0)
        {
            files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(repoRoot, path))
                .ToList();
        }

        return files
            .Select(relative => NormalizeRelative(relative))
            .Where(relative => !IsExcluded(relative, config))
            .Select(relative => new { Relative = relative, Full = Path.Combine(repoRoot, relative) })
            .Where(file => File.Exists(file.Full))
            .Select(file =>
            {
                var info = new FileInfo(file.Full);
                return new CandidateFile(file.Full, file.Relative, info.Length, info.LastWriteTimeUtc);
            })
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<CandidateFile> EnumerateCandidateFilesFromFileSystem(string repoRoot, IndexerConfig config)
    {
        var files = new List<CandidateFile>();
        var pending = new Stack<string>();
        pending.Push(repoRoot);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var info = new DirectoryInfo(child);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0
                    || config.ExcludeDirectories.Any(excluded => string.Equals(info.Name, excluded, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                {
                    continue;
                }

                pending.Push(child);
            }

            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var relative = NormalizeRelative(Path.GetRelativePath(repoRoot, path));
                if (IsExcluded(relative, config))
                {
                    continue;
                }

                if (!config.IncludeNonCSharpText && !IsCSharpOrWorkspaceTrigger(path))
                {
                    continue;
                }

                var info = new FileInfo(path);
                files.Add(new CandidateFile(path, relative, info.Length, info.LastWriteTimeUtc));
            }
        }

        return files.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray();
    }

    private static bool IsCSharpOrWorkspaceTrigger(string path)
    {
        var extension = Path.GetExtension(path);
        var fileName = Path.GetFileName(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".props", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".targets", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("global.json", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("NuGet.config", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("Directory.Build.targets", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExcluded(string relativePath, IndexerConfig config)
    {
        var segments = NormalizeRelative(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (segments.Any(segment => config.ExcludeDirectories.Any(ex => string.Equals(segment, ex, pathComparison))))
        {
            return true;
        }

        return config.ExcludeFileSuffixes.Any(suffix => relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeRoot(string path)
        => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string NormalizeRelative(string path)
        => path.Replace('\\', '/').TrimStart('/');

    private static List<string> TryGitLsFiles(string repoRoot)
    {
        if (!Directory.Exists(Path.Combine(repoRoot, ".git")))
        {
            return new List<string>();
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo("git")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }.WithArguments("ls-files", "-co", "--exclude-standard"));
            if (process is null)
            {
                return new List<string>();
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            if (process.ExitCode != 0)
            {
                return new List<string>();
            }

            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string RunGit(string repoRoot, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }.WithArguments(arguments));
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start git.");
        }

        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(3000) || process.ExitCode != 0)
        {
            throw new InvalidOperationException("Git state query failed.");
        }

        return output;
    }

    private static string ReadGitHead(string repoRoot)
    {
        var gitPath = Path.Combine(repoRoot, ".git");
        var gitDirectory = Directory.Exists(gitPath)
            ? gitPath
            : ResolveGitDirectory(repoRoot, gitPath);
        var headPath = Path.Combine(gitDirectory, "HEAD");
        var head = File.ReadAllText(headPath).Trim();
        if (!head.StartsWith("ref: ", StringComparison.Ordinal))
        {
            return head;
        }

        var reference = head[5..];
        var looseReferencePath = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseReferencePath))
        {
            return File.ReadAllText(looseReferencePath).Trim();
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (File.Exists(packedRefsPath))
        {
            foreach (var line in File.ReadLines(packedRefsPath))
            {
                if (!line.StartsWith('#') && line.EndsWith(" " + reference, StringComparison.Ordinal))
                {
                    return line[..line.IndexOf(' ')];
                }
            }
        }

        return head;
    }

    private static string ResolveGitDirectory(string repoRoot, string gitFilePath)
    {
        var content = File.ReadAllText(gitFilePath).Trim();
        const string prefix = "gitdir: ";
        if (!content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid .git file.");
        }

        return Path.GetFullPath(Path.Combine(repoRoot, content[prefix.Length..]));
    }
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo info, params string[] args)
    {
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        return info;
    }
}

public static class WorkspaceDiscovery
{
    public static IReadOnlyList<WorkspaceInput> Discover(string repoRoot, IndexerConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Solution))
        {
            var path = Path.GetFullPath(Path.Combine(repoRoot, config.Solution));
            return File.Exists(path) ? new[] { new WorkspaceInput(path, Path.GetExtension(path).TrimStart('.')) } : Array.Empty<WorkspaceInput>();
        }

        var rootSolutions = Directory.EnumerateFiles(repoRoot, "*.sln").Concat(Directory.EnumerateFiles(repoRoot, "*.slnx")).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (rootSolutions.Length > 0)
        {
            return rootSolutions.Select(path => new WorkspaceInput(path, Path.GetExtension(path).TrimStart('.'))).ToArray();
        }

        var allSolutions = Directory.EnumerateFiles(repoRoot, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(repoRoot, "*.slnx", SearchOption.AllDirectories))
            .Where(path => !RepositoryDiscovery.IsExcluded(Path.GetRelativePath(repoRoot, path), config))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        if (allSolutions.Length > 0)
        {
            return allSolutions.Select(path => new WorkspaceInput(path, Path.GetExtension(path).TrimStart('.'))).ToArray();
        }

        return Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !RepositoryDiscovery.IsExcluded(Path.GetRelativePath(repoRoot, path), config))
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(path => new WorkspaceInput(path, "csproj"))
            .ToArray();
    }
}
