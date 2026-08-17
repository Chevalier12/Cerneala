using Cerneala.Language;
using Cerneala.LanguageServer.Logging;
using Microsoft.CodeAnalysis;
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
}
