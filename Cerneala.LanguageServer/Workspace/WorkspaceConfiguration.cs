namespace Cerneala.LanguageServer.Workspace;

internal sealed record WorkspaceConfiguration(
    string? RootPath,
    string? SolutionPath,
    string? ActiveTargetFramework,
    string Configuration,
    bool WatchFileSystem = true)
{
    public static WorkspaceConfiguration Create(
        string? rootUri,
        string? solutionPath,
        string? activeTargetFramework,
        string? configuration)
    {
        string? rootPath = null;
        if (!string.IsNullOrWhiteSpace(rootUri) &&
            Uri.TryCreate(rootUri, UriKind.Absolute, out Uri? uri) &&
            uri.IsFile)
        {
            rootPath = Path.GetFullPath(uri.LocalPath);
        }

        return new WorkspaceConfiguration(
            rootPath,
            solutionPath,
            activeTargetFramework,
            string.IsNullOrWhiteSpace(configuration) ? "Debug" : configuration);
    }

    public string? ResolveWorkspacePath()
    {
        if (!string.IsNullOrWhiteSpace(SolutionPath))
        {
            string candidate = Path.IsPathRooted(SolutionPath)
                ? SolutionPath
                : Path.Combine(RootPath ?? Directory.GetCurrentDirectory(), SolutionPath);
            return Path.GetFullPath(candidate);
        }

        if (RootPath is null || !Directory.Exists(RootPath))
        {
            return null;
        }

        foreach (string pattern in new[] { "*.slnx", "*.sln", "*.csproj" })
        {
            string[] candidates = Directory.GetFiles(RootPath, pattern, SearchOption.TopDirectoryOnly);
            if (candidates.Length == 1)
            {
                return Path.GetFullPath(candidates[0]);
            }

            if (candidates.Length > 1)
            {
                return null;
            }
        }

        return null;
    }
}
