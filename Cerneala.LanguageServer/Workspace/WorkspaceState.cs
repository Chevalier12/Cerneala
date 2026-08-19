using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using Cerneala.Language;
using Cerneala.Language.Semantics;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cerneala.LanguageServer.Workspace;

internal sealed class WorkspaceState : IDisposable
{
    private readonly MSBuildWorkspace? workspace;
    private readonly Dictionary<string, ProjectContext[]> owners;
    private int referenceCount = 1;

    private WorkspaceState(
        long revision,
        MSBuildWorkspace? workspace,
        IReadOnlyList<ProjectContext> projects,
        Dictionary<string, ProjectContext[]> owners)
    {
        Revision = revision;
        this.workspace = workspace;
        Projects = projects;
        this.owners = owners;
    }

    public long Revision { get; }

    public IReadOnlyList<ProjectContext> Projects { get; }

    public static WorkspaceState Empty(long revision) => new(
        revision,
        null,
        [],
        new Dictionary<string, ProjectContext[]>(PathComparer.Instance));

    public static async Task<WorkspaceState?> TryLoadBootstrapAsync(
        WorkspaceConfiguration configuration,
        long revision,
        string preferredDocumentPath,
        IServerLogger logger,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string documentPath = PathComparer.Normalize(preferredDocumentPath);
        if (!File.Exists(documentPath))
        {
            return null;
        }

        string? projectPath = FindNearestProject(configuration, documentPath);
        if (projectPath is null)
        {
            return null;
        }

        string projectAssemblyName = ReadAssemblyName(projectPath);
        string? outputAssemblyPath = FindOutputAssembly(
            projectPath,
            projectAssemblyName,
            configuration.Configuration,
            configuration.ActiveTargetFramework);
        if (outputAssemblyPath is null)
        {
            return null;
        }

        long started = Stopwatch.GetTimestamp();
        IReadOnlyList<MetadataReference> references = await LoadMetadataReferencesAsync(
            Path.GetDirectoryName(outputAssemblyPath)!,
            cancellationToken).ConfigureAwait(false);
        if (references.Count == 0)
        {
            return null;
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Cerneala.LanguageServer.Bootstrap",
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        string markup = await File.ReadAllTextAsync(documentPath, cancellationToken).ConfigureAwait(false);
        CernealaDocument document = new(documentPath, SourceText.From(markup, revision));
        ProjectContext context = ProjectContext.CreateBootstrap(
            projectPath,
            InferTargetFramework(outputAssemblyPath, configuration.ActiveTargetFramework),
            projectAssemblyName,
            compilation,
            document,
            revision);
        Dictionary<string, ProjectContext[]> owners = new(PathComparer.Instance)
        {
            [documentPath] = [context]
        };
        logger.Info(
            "workspace.bootstrapLoaded",
            ("revision", revision),
            ("elapsedMs", Stopwatch.GetElapsedTime(started).TotalMilliseconds),
            ("referenceCount", references.Count));
        return new WorkspaceState(revision, null, [context], owners);
    }

    public static async Task<WorkspaceState> LoadAsync(
        WorkspaceConfiguration configuration,
        long revision,
        IServerLogger logger,
        CancellationToken cancellationToken)
    {
        string? workspacePath = configuration.ResolveWorkspacePath();
        if (workspacePath is null)
        {
            return Empty(revision);
        }

        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = configuration.Configuration
        };
        if (!string.IsNullOrWhiteSpace(configuration.ActiveTargetFramework))
        {
            properties["TargetFramework"] = configuration.ActiveTargetFramework;
        }

        MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties);
        workspace.SkipUnrecognizedProjects = true;
        workspace.LoadMetadataForReferencedProjects = false;
        workspace.RegisterWorkspaceFailedHandler(_ => logger.Info("workspace.msbuildDiagnostic"));

        try
        {
            Solution solution;
            string extension = Path.GetExtension(workspacePath);
            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Project project = await workspace.OpenProjectAsync(workspacePath, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                solution = project.Solution;
            }
            else if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                solution = await workspace.OpenSolutionAsync(workspacePath, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException("Unsupported workspace file '" + workspacePath + "'.");
            }

            solution = RemoveAnalyzerReferences(solution);

            List<ProjectContext> contexts = new();
            foreach (IGrouping<string, Project> group in solution.Projects
                .Where(project => project.Language == LanguageNames.CSharp &&
                    project.FilePath is not null &&
                    project.AdditionalDocuments.Any(document =>
                        document.FilePath is not null &&
                        CernealaDocumentPath.IsMarkupFile(document.FilePath)))
                .GroupBy(project => PathComparer.Normalize(project.FilePath!), PathComparer.Instance))
            {
                Project selected = SelectTargetFramework(group, configuration.ActiveTargetFramework);
                ProjectContext? context = await ProjectContext.CreateAsync(selected, revision, cancellationToken)
                    .ConfigureAwait(false);
                if (context is not null)
                {
                    contexts.Add(context);
                }
            }

            Dictionary<string, ProjectContext[]> owners = contexts
                .SelectMany(context => context.DocumentPaths.Select(path => (Path: path, Context: context)))
                .GroupBy(entry => entry.Path, PathComparer.Instance)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => entry.Context)
                        .OrderBy(context => context.ProjectFilePath, StringComparer.Ordinal)
                        .ToArray(),
                    PathComparer.Instance);
            logger.Info("workspace.loaded", ("revision", revision), ("projectCount", contexts.Count));
            return new WorkspaceState(revision, workspace, contexts, owners);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    public ProjectContext[] GetOwners(string path) =>
        owners.TryGetValue(path, out ProjectContext[]? value) ? value : [];

    public void Retain() => Interlocked.Increment(ref referenceCount);

    public void Release()
    {
        if (Interlocked.Decrement(ref referenceCount) == 0)
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        foreach (ProjectContext project in Projects)
        {
            project.Dispose();
        }

        workspace?.Dispose();
    }

    private static Project SelectTargetFramework(IEnumerable<Project> projects, string? activeTargetFramework)
    {
        Project[] candidates = projects.OrderBy(project => project.Name, StringComparer.Ordinal).ToArray();
        if (!string.IsNullOrWhiteSpace(activeTargetFramework))
        {
            Project? match = candidates.FirstOrDefault(project =>
                project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    "build_property.TargetFramework",
                    out string? targetFramework) &&
                string.Equals(targetFramework, activeTargetFramework, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return candidates[0];
    }

    private static Solution RemoveAnalyzerReferences(Solution solution)
    {
        foreach (ProjectId projectId in solution.ProjectIds)
        {
            Project? project = solution.GetProject(projectId);
            if (project is not null && project.AnalyzerReferences.Count != 0)
            {
                solution = solution.WithProjectAnalyzerReferences(projectId, []);
            }
        }

        return solution;
    }

    private static string? FindNearestProject(WorkspaceConfiguration configuration, string documentPath)
    {
        string? configuredWorkspace = configuration.ResolveWorkspacePath();
        if (configuredWorkspace is not null &&
            Path.GetExtension(configuredWorkspace).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return PathComparer.Normalize(configuredWorkspace);
        }

        string? root = configuration.RootPath is null
            ? configuredWorkspace is null ? null : Path.GetDirectoryName(configuredWorkspace)
            : Path.GetFullPath(configuration.RootPath);
        DirectoryInfo? directory = new(Path.GetDirectoryName(documentPath)!);
        while (directory is not null)
        {
            string[] candidates = Directory.GetFiles(directory.FullName, "*.csproj", SearchOption.TopDirectoryOnly);
            if (candidates.Length == 1)
            {
                return PathComparer.Normalize(candidates[0]);
            }

            if (candidates.Length > 1)
            {
                string? named = candidates.SingleOrDefault(candidate => string.Equals(
                    Path.GetFileNameWithoutExtension(candidate),
                    directory.Name,
                    StringComparison.OrdinalIgnoreCase));
                return named is null ? null : PathComparer.Normalize(named);
            }

            if (root is not null && PathComparer.Instance.Equals(directory.FullName, root))
            {
                break;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ReadAssemblyName(string projectPath)
    {
        try
        {
            string? configured = XDocument.Load(projectPath)
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "AssemblyName" &&
                    !string.IsNullOrWhiteSpace(element.Value))?
                .Value.Trim();
            return string.IsNullOrWhiteSpace(configured)
                ? Path.GetFileNameWithoutExtension(projectPath)
                : configured!;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return Path.GetFileNameWithoutExtension(projectPath);
        }
    }

    private static string? FindOutputAssembly(
        string projectPath,
        string assemblyName,
        string configuration,
        string? activeTargetFramework)
    {
        string outputRoot = Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", configuration);
        if (!Directory.Exists(outputRoot))
        {
            return null;
        }

        string separator = Path.DirectorySeparatorChar.ToString();
        return Directory.EnumerateFiles(outputRoot, assemblyName + ".dll", SearchOption.AllDirectories)
            .Where(path => !path.Contains(separator + "ref" + separator, StringComparison.OrdinalIgnoreCase) &&
                !path.Contains(separator + "refint" + separator, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => !string.IsNullOrWhiteSpace(activeTargetFramework) &&
                path.Contains(separator + activeTargetFramework + separator, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static async Task<IReadOnlyList<MetadataReference>> LoadMetadataReferencesAsync(
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in Directory.EnumerateFiles(outputDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            paths[Path.GetFileName(path)] = path;
        }

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
        {
            foreach (string path in trustedPlatformAssemblies.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries))
            {
                paths.TryAdd(Path.GetFileName(path), path);
            }
        }

        List<MetadataReference> references = new(paths.Count);
        foreach (string path in paths.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                byte[] image = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                references.Add(MetadataReference.CreateFromImage(
                    ImmutableArray.Create(image),
                    filePath: path));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
            }
        }

        return references;
    }

    private static string? InferTargetFramework(string outputAssemblyPath, string? configuredTargetFramework)
    {
        if (!string.IsNullOrWhiteSpace(configuredTargetFramework))
        {
            return configuredTargetFramework;
        }

        string? directory = Path.GetDirectoryName(outputAssemblyPath);
        return directory is null ? null : Path.GetFileName(directory);
    }
}
