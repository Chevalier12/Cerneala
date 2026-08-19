using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.PreviewHost;

internal sealed class PreviewCompiler : IDisposable
{
    private readonly object projectsGate = new();
    private readonly Dictionary<string, Task<LoadedProject>> projects = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task> warmups = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool prewarmBuildOutput;
    private readonly string shadowRoot = Path.Combine(
        Path.GetTempPath(),
        "Cerneala",
        "PreviewHost",
        Environment.ProcessId.ToString(),
        "analyzers");

    private bool disposed;

    internal PreviewCompiler(bool prewarmBuildOutput = true)
    {
        this.prewarmBuildOutput = prewarmBuildOutput;
    }

    public async Task<PreviewCompilation> CompileAsync(
        string documentPath,
        string source,
        CancellationToken cancellationToken = default)
    {
        Stopwatch total = Stopwatch.StartNew();
        string fullDocumentPath = Path.GetFullPath(documentPath);
        string projectPath = FindOwningProject(fullDocumentPath);
        if (TryUseCurrentBuildOutput(fullDocumentPath, projectPath, source, total, out PreviewCompilation? built))
        {
            return built!;
        }

        LoadedProject loaded = await GetProjectAsync(projectPath, cancellationToken).ConfigureAwait(false);
        Project project = loaded.Project;
        DocumentId additionalDocumentId = project.AdditionalDocumentIds.FirstOrDefault(id =>
            string.Equals(project.Solution.GetAdditionalDocument(id)?.FilePath, fullDocumentPath, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"'{fullDocumentPath}' is not an AdditionalFile in '{projectPath}'.");
        Solution overlay = project.Solution.WithAdditionalDocumentText(
            additionalDocumentId,
            SourceText.From(source),
            PreservationMode.PreserveIdentity);
        project = overlay.GetProject(project.Id)
            ?? throw new InvalidOperationException("The preview project disappeared while applying the editor buffer.");

        Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Roslyn could not compile '{projectPath}'.");

        using MemoryStream assembly = new();
        EmitResult emit = compilation.Emit(assembly, cancellationToken: cancellationToken);
        if (!emit.Success)
        {
            throw new PreviewCompilationException(string.Join(
                Environment.NewLine,
                emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(FormatDiagnostic)));
        }
        loaded.Project = project;

        string targetTypeName = await ResolveTargetTypeNameAsync(project, fullDocumentPath, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, string> referencePaths = compilation.References
            .OfType<PortableExecutableReference>()
            .Where(reference => !string.IsNullOrWhiteSpace(reference.FilePath))
            .Select(reference => reference.FilePath!)
            .Where(File.Exists)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (Project solutionProject in project.Solution.Projects)
        {
            if (!string.IsNullOrWhiteSpace(solutionProject.OutputFilePath) && File.Exists(solutionProject.OutputFilePath))
            {
                referencePaths[Path.GetFileNameWithoutExtension(solutionProject.OutputFilePath)] = solutionProject.OutputFilePath;
            }
        }
        total.Stop();
        return new PreviewCompilation(
            assembly.ToArray(),
            compilation.AssemblyName ?? Path.GetFileNameWithoutExtension(projectPath),
            targetTypeName,
            ResolveRuntimeDirectory(project, projectPath),
            referencePaths,
            total.Elapsed);
    }

    public void Dispose()
    {
        Task<LoadedProject>[] projectTasks;
        Task[] warmupTasks;
        lock (projectsGate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            projectTasks = projects.Values.ToArray();
            warmupTasks = warmups.Values.ToArray();
            projects.Clear();
            warmups.Clear();
        }

        Task cleanup = CleanupProjectsAsync(projectTasks, warmupTasks);
        if (cleanup.IsCompleted)
        {
            cleanup.GetAwaiter().GetResult();
        }
    }

    private async Task<LoadedProject> GetProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        Task<LoadedProject> loadTask;
        lock (projectsGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!projects.TryGetValue(projectPath, out loadTask!))
            {
                loadTask = LoadProjectAsync(projectPath);
                projects.Add(projectPath, loadTask);
            }
        }

        try
        {
            return await loadTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (loadTask.IsFaulted || loadTask.IsCanceled)
        {
            lock (projectsGate)
            {
                if (projects.TryGetValue(projectPath, out Task<LoadedProject>? current) &&
                    ReferenceEquals(current, loadTask))
                {
                    projects.Remove(projectPath);
                }
            }

            throw;
        }
    }

    private async Task<LoadedProject> LoadProjectAsync(string projectPath)
    {
        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = "Debug",
            ["DesignTimeBuild"] = "true",
            ["BuildingInsideVisualStudio"] = "true",
            ["SkipCompilerExecution"] = "true",
            ["ProvideCommandLineArgs"] = "true"
        };
        MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties);
        List<string> failures = [];
        workspace.RegisterWorkspaceFailedHandler(args => failures.Add(args.Diagnostic.Message));
        Project project = await workspace.OpenProjectAsync(projectPath, cancellationToken: CancellationToken.None)
            .ConfigureAwait(false);
        project = ShadowCopyAnalyzers(project);
        if (failures.Any(message => message.Contains("Failure", StringComparison.OrdinalIgnoreCase)))
        {
            workspace.Dispose();
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }

        return new LoadedProject(workspace, project);
    }

    private bool TryUseCurrentBuildOutput(
        string documentPath,
        string projectPath,
        string source,
        Stopwatch total,
        out PreviewCompilation? compilation)
    {
        compilation = null;
        if (!File.Exists(documentPath) ||
            !string.Equals(File.ReadAllText(documentPath), source, StringComparison.Ordinal))
        {
            return false;
        }

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        string assemblyName = ReadAssemblyName(projectPath);
        string? outputPath = FindCurrentBuildOutput(projectDirectory, assemblyName);
        if (outputPath is null || !IsBuildOutputCurrent(projectDirectory, outputPath))
        {
            return false;
        }

        string outputDirectory = Path.GetDirectoryName(outputPath)!;
        Dictionary<string, string> referencePaths = Directory
            .EnumerateFiles(outputDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        string actualAssemblyName = AssemblyName.GetAssemblyName(outputPath).Name ?? assemblyName;
        total.Stop();
        compilation = new PreviewCompilation(
            File.ReadAllBytes(outputPath),
            actualAssemblyName,
            Path.GetFileNameWithoutExtension(documentPath),
            outputDirectory,
            referencePaths,
            total.Elapsed);
        if (prewarmBuildOutput && File.Exists(Path.ChangeExtension(outputPath, ".deps.json")))
        {
            BeginWarmUp(projectPath);
        }
        return true;
    }

    internal Task PrepareProjectAsync(string projectPath)
    {
        projectPath = Path.GetFullPath(projectPath);
        lock (projectsGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!warmups.TryGetValue(projectPath, out Task? warmup))
            {
                warmup = WarmProjectCoreAsync(projectPath);
                warmups.Add(projectPath, warmup);
            }

            return warmup;
        }
    }

    private void BeginWarmUp(string projectPath)
    {
        Task warmup = PrepareProjectAsync(projectPath);
        _ = warmup.ContinueWith(
            task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task WarmProjectCoreAsync(string projectPath)
    {
        LoadedProject loaded = await GetProjectAsync(projectPath, CancellationToken.None).ConfigureAwait(false);
        _ = await loaded.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task CleanupProjectsAsync(
        IReadOnlyList<Task<LoadedProject>> projectTasks,
        IReadOnlyList<Task> warmupTasks)
    {
        try
        {
            await Task.WhenAll(projectTasks.Cast<Task>().Concat(warmupTasks)).ConfigureAwait(false);
        }
        catch
        {
        }

        foreach (Task<LoadedProject> projectTask in projectTasks)
        {
            if (projectTask.Status == TaskStatus.RanToCompletion)
            {
                projectTask.Result.Workspace.Dispose();
            }
        }

        try
        {
            if (Directory.Exists(shadowRoot))
            {
                Directory.Delete(shadowRoot, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ReadAssemblyName(string projectPath)
    {
        XDocument document = XDocument.Load(projectPath, LoadOptions.None);
        string? assemblyName = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "AssemblyName")?
            .Value
            .Trim();
        return string.IsNullOrWhiteSpace(assemblyName)
            ? Path.GetFileNameWithoutExtension(projectPath)
            : assemblyName!;
    }

    private static string? FindCurrentBuildOutput(string projectDirectory, string assemblyName)
    {
        string debugDirectory = Path.Combine(projectDirectory, "bin", "Debug");
        if (!Directory.Exists(debugDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(debugDirectory, assemblyName + ".dll", SearchOption.AllDirectories)
            .Where(path => !IsReferenceAssemblyPath(path))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool IsBuildOutputCurrent(string projectDirectory, string outputPath)
    {
        DateTime outputWriteTime = File.GetLastWriteTimeUtc(outputPath);
        foreach (string inputPath in EnumerateBuildInputs(projectDirectory))
        {
            if (File.GetLastWriteTimeUtc(inputPath) > outputWriteTime)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> EnumerateBuildInputs(string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            string extension = Path.GetExtension(file);
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".crn", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".targets", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }

        foreach (string child in Directory.EnumerateDirectories(directory))
        {
            string name = Path.GetFileName(child);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                name.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string file in EnumerateBuildInputs(child))
            {
                yield return file;
            }
        }
    }

    private static bool IsReferenceAssemblyPath(string path)
    {
        string normalized = path.Replace('/', '\\');
        return normalized.Contains("\\ref\\", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("\\refint\\", StringComparison.OrdinalIgnoreCase);
    }

    private Project ShadowCopyAnalyzers(Project project)
    {
        if (project.AnalyzerReferences.Count == 0)
        {
            return project;
        }

        string projectShadow = Path.Combine(shadowRoot, project.Id.Id.ToString("N"));
        Directory.CreateDirectory(projectShadow);
        ShadowAnalyzerAssemblyLoader loader = new();
        List<AnalyzerReference> references = [];
        foreach (AnalyzerReference reference in project.AnalyzerReferences)
        {
            if (reference is not AnalyzerFileReference fileReference || !File.Exists(fileReference.FullPath))
            {
                references.Add(reference);
                continue;
            }

            string sourceDirectory = Path.GetDirectoryName(fileReference.FullPath)!;
            string analyzerDirectory = Path.Combine(projectShadow, references.Count.ToString());
            Directory.CreateDirectory(analyzerDirectory);
            foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory))
            {
                string extension = Path.GetExtension(sourceFile);
                if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    string destination = Path.Combine(analyzerDirectory, Path.GetFileName(sourceFile));
                    File.Copy(sourceFile, destination, overwrite: true);
                    if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        loader.AddDependencyLocation(destination);
                    }
                }
            }

            string shadowPath = Path.Combine(analyzerDirectory, Path.GetFileName(fileReference.FullPath));
            references.Add(new AnalyzerFileReference(shadowPath, loader));
        }

        return project.WithAnalyzerReferences(references);
    }

    private static async Task<string> ResolveTargetTypeNameAsync(
        Project project,
        string documentPath,
        CancellationToken cancellationToken)
    {
        string companionPath = documentPath + ".cs";
        Document? companion = project.Documents.FirstOrDefault(document =>
            string.Equals(document.FilePath, companionPath, StringComparison.OrdinalIgnoreCase));
        if (companion is not null)
        {
            SyntaxNode? root = await companion.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SemanticModel? model = await companion.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is not null && model is not null)
            {
                string expectedName = Path.GetFileNameWithoutExtension(documentPath);
                INamedTypeSymbol? symbol = root.DescendantNodes()
                    .Select(node => model.GetDeclaredSymbol(node, cancellationToken))
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault(candidate => candidate.Name == expectedName);
                if (symbol is not null)
                {
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                }
            }
        }

        string fileName = Path.GetFileNameWithoutExtension(documentPath);
        return string.IsNullOrWhiteSpace(project.DefaultNamespace)
            ? fileName
            : project.DefaultNamespace + "." + fileName;
    }

    private static string FindOwningProject(string documentPath)
    {
        DirectoryInfo? directory = new FileInfo(documentPath).Directory;
        while (directory is not null)
        {
            string[] projects = Directory.GetFiles(directory.FullName, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projects.Length == 1)
            {
                return projects[0];
            }
            if (projects.Length > 1)
            {
                string expected = Path.Combine(directory.FullName, directory.Name + ".csproj");
                return projects.FirstOrDefault(path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase))
                    ?? projects[0];
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No owning .csproj could be found for the preview document.", documentPath);
    }

    private static string ResolveRuntimeDirectory(Project project, string projectPath)
    {
        string? outputDirectory = string.IsNullOrWhiteSpace(project.OutputFilePath)
            ? null
            : Path.GetDirectoryName(project.OutputFilePath);
        return !string.IsNullOrWhiteSpace(outputDirectory) && Directory.Exists(outputDirectory)
            ? outputDirectory
            : Path.GetDirectoryName(projectPath)!;
    }

    private static string FormatDiagnostic(Diagnostic diagnostic)
    {
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        string location = span.IsValid
            ? $"{Path.GetFileName(span.Path)}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})"
            : "preview";
        return $"{location}: {diagnostic.Id}: {diagnostic.GetMessage()}";
    }

    private sealed class LoadedProject(MSBuildWorkspace workspace, Project project)
    {
        public MSBuildWorkspace Workspace { get; } = workspace;

        public Project Project { get; set; } = project;
    }

    private sealed class ShadowAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly Dictionary<string, string> dependencies = new(StringComparer.OrdinalIgnoreCase);

        public void AddDependencyLocation(string fullPath)
        {
            dependencies[Path.GetFileNameWithoutExtension(fullPath)] = Path.GetFullPath(fullPath);
        }

        public Assembly LoadFromPath(string fullPath)
        {
            ResolveEventHandler resolver = (_, args) =>
            {
                AssemblyName name = new(args.Name);
                return name.Name is not null && dependencies.TryGetValue(name.Name, out string? path)
                    ? Assembly.LoadFrom(path)
                    : null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            try
            {
                return Assembly.LoadFrom(Path.GetFullPath(fullPath));
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }
    }
}

internal sealed record PreviewCompilation(
    byte[] AssemblyImage,
    string AssemblyName,
    string TargetTypeName,
    string ProjectDirectory,
    IReadOnlyDictionary<string, string> ReferencePaths,
    TimeSpan CompileTime);

internal sealed class PreviewCompilationException(string message) : Exception(message);
