using System.Diagnostics;

namespace RoslynRepoIndexer.Core;

public static class RepositoryDiscovery
{
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
            .Select(file => new CandidateFile(file.Full, file.Relative, new FileInfo(file.Full).Length))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
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
