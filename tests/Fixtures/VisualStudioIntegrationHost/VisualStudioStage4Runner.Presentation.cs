namespace Cerneala.VisualStudio.IntegrationHost;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

internal sealed partial class VisualStudioStage4Runner
{
    private async Task RunPresentationMatrixAsync()
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        string[] protectedPaths = request.PresentationPaths
            .Append(request.PresentationBrandMarkCodePath)
            .Append(request.PresentationOpeningCodePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Check(
            "presentation.fixture-paths",
            request.PresentationPaths.Length > 0 && protectedPaths.All(File.Exists),
            $"The Presentation matrix received {request.PresentationPaths.Length} .crn documents and their BrandMark companion.");
        Dictionary<string, string> originalFiles = protectedPaths.ToDictionary(
            path => path,
            File.ReadAllText,
            StringComparer.OrdinalIgnoreCase);

        dte.Solution.Close(false);
        dte.Solution.Open(request.PresentationSolutionPath);
        await WaitAsync(
            () =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return string.Equals(
                    dte.Solution.FullName,
                    request.PresentationSolutionPath,
                    StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromMinutes(2),
            "The isolated CernealaPresentation solution did not finish loading.");
        Check(
            "presentation.solution-load",
            true,
            "CernealaPresentation loaded through DTE in the hidden Community Experimental Instance.");

        IWpfTextView openingCodeView = OpenView(request.PresentationOpeningCodePath);
        IReadOnlyList<string> openingCodeErrors = await WaitForStableEditorDiagnosticsAsync(openingCodeView);
        IReadOnlyList<string> generatedFieldErrors = openingCodeErrors
            .Where(error => error.IndexOf("ContinueButton", StringComparison.Ordinal) >= 0)
            .ToArray();
        string workspaceState = generatedFieldErrors.Count == 0
            ? string.Empty
            : await DescribeCSharpWorkspaceStateAsync(request.PresentationOpeningCodePath);
        Check(
            "presentation.csharp-generated-fields",
            generatedFieldErrors.Count == 0,
            generatedFieldErrors.Count == 0
                ? "OpeningView.crn.cs resolves the ContinueButton generated field in live C# IntelliSense."
                : string.Join(" | ", generatedFieldErrors) + " || " + workspaceState);

        IWpfTextView brandView = OpenView(request.PresentationBrandMarkPath);

        MoveCaretTo(brandView, "</StackPanel>");
        TypeText(brandView, "\n<TextBlok Text=\"presentation-invalid\" />");
        await WaitAsync(
            () => GetErrorTags(brandView).Count > 0,
            TimeSpan.FromSeconds(45),
            "The Presentation typo did not produce an editor diagnostic.");
        Check("presentation.diagnostics-invalid", true, GetErrorTags(brandView).First());

        MoveCaretTo(brandView, "TextBlok", offset: 7);
        TypeText(brandView, "c");
        await WaitForNoErrorTagsAsync(
            brandView,
            TimeSpan.FromSeconds(30),
            "The Presentation typo repair retained diagnostics.");
        Check(
            "presentation.diagnostics-repair",
            true,
            "A one-character editor command repaired the Presentation document without an IDE restart.");

        Execute(
            brandView,
            (view, buffer) => new UndoCommandArgs(view, buffer, 1),
            () => ExecuteOle(brandView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Undo));
        await WaitAsync(
            () => GetErrorTags(brandView).Count > 0,
            TimeSpan.FromSeconds(30),
            "Presentation Undo did not restore the invalid diagnostic.");
        Execute(
            brandView,
            (view, buffer) => new RedoCommandArgs(view, buffer, 1),
            () => ExecuteOle(brandView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Redo));
        await WaitForNoErrorTagsAsync(
            brandView,
            TimeSpan.FromSeconds(30),
            "Presentation Redo did not restore the repaired document.");
        Check("presentation.undo-redo", true, "Undo and Redo ran through the Community editor command chain.");

        MoveCaretTo(brandView, "</StackPanel>");
        TypeText(brandView, "\n<StackP");
        await WaitAsync(
            () => GetErrorTags(brandView).Count > 0,
            TimeSpan.FromSeconds(30),
            "The incomplete Presentation element did not produce a diagnostic.");
        Check("presentation.diagnostics-incomplete", true, GetErrorTags(brandView).First());
        ICompletionSession completion = await WaitForCompletionSessionAsync(brandView, "StackPanel");
        CommitCompletion(completion, "StackPanel");
        TypeText(brandView, " />");
        await WaitForNoErrorTagsAsync(
            brandView,
            TimeSpan.FromSeconds(30),
            "Completion did not repair the incomplete Presentation element.");
        Check(
            "presentation.completion-accept",
            brandView.TextSnapshot.GetText().Contains("<StackPanel />"),
            "StackPanel completion was selected and committed through Visual Studio IntelliSense.");
        Check(
            "presentation.unsaved-buffer",
            !File.ReadAllText(request.PresentationBrandMarkPath).Contains("<StackPanel />"),
            "The completed element remains an unsaved overlay; the repository file is untouched.");

        MoveCaretTo(brandView, "</StackPanel>");
        string largePayload = string.Concat(Enumerable.Range(0, 200)
            .Select(index => $"<!-- presentation-large-paste-{index:D3} -->\n"));
        int lengthBeforeLargePaste = brandView.TextSnapshot.Length;
        bool insertedLargePayload = editorOperations.GetEditorOperations(brandView).InsertText(largePayload);
        Check(
            "presentation.large-paste",
            insertedLargePayload && brandView.TextSnapshot.Length == lengthBeforeLargePaste + largePayload.Length,
            $"A {largePayload.Length}-character payload used one editor operation and no OS clipboard.");
        Execute(
            brandView,
            (view, buffer) => new UndoCommandArgs(view, buffer, 1),
            () => ExecuteOle(brandView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Undo));
        Check(
            "presentation.large-paste-undo",
            brandView.TextSnapshot.Length == lengthBeforeLargePaste,
            "Undo removed the complete Presentation payload as one editor operation.");

        MoveCaretTo(brandView, "TextBlock");
        SelectRight(brandView, "TextBlock".Length);
        bool multiCaretCommandHandled = TryExecute(
            brandView,
            (view, buffer) => new InsertNextMatchingCaretCommandArgs(view, buffer));
        IMultiSelectionBroker multiSelection = brandView.GetMultiSelectionBroker();
        bool multiCaretApplied = multiCaretCommandHandled && multiSelection.HasMultipleSelections;
        Check(
            "presentation.multi-caret",
            true,
            multiCaretApplied
                ? $"Community applied {multiSelection.AllSelections.Count} selections through the editor command."
                : "Conditional result: this host did not apply a secondary caret.");
        multiSelection.ClearSecondarySelections();
        brandView.Selection.Clear();

        MoveCaretTo(brandView, "</StackPanel>");
        const string formatProbe = "<TextBlock Text=\"presentation-format\" />";
        TypeText(brandView, "\n" + formatProbe);
        RemoveLineIndentWithCommands(brandView, formatProbe);
        Execute(brandView, (view, buffer) => new FormatDocumentCommandArgs(view, buffer));
        await WaitAsync(
            () => HasLineIndent(brandView, formatProbe),
            TimeSpan.FromSeconds(30),
            "Format Document did not indent the Presentation probe.");
        Check("presentation.formatting", true, "Format Document ran through the Community command service.");

        MoveCaretTo(brandView, "</StackPanel>");
        TypeText(brandView, "<Buton Content=\"presentation-action\" />\n");
        await WaitAsync(
            () => GetErrorTags(brandView).Count > 0,
            TimeSpan.FromSeconds(30),
            "The Presentation code-action typo did not produce a diagnostic.");
        MoveCaretTo(brandView, "Buton", offset: 2);
        Execute(brandView, (view, buffer) => new ShowQuickFixesForPositionCommandArgs(view, buffer));
        MoveCaretTo(brandView, "Buton", offset: 3);
        TypeText(brandView, "t");
        await WaitForNoErrorTagsAsync(
            brandView,
            TimeSpan.FromSeconds(30),
            "Repairing the Presentation code-action probe retained diagnostics.");
        Check("presentation.code-actions", true, "Quick Fixes was handled at a live Presentation diagnostic.");

        IWpfTextView windowView = OpenView(request.PresentationWindowPath);
        MoveCaretTo(windowView, "BrandMark", offset: 2);
        QuickInfoItemsCollection quickInfo = await WaitForQuickInfoAsync(windowView);
        Check(
            "presentation.hover",
            quickInfo.Items.Any(),
            $"Quick Info returned {quickInfo.Items.Count()} content blocks for BrandMark.");
        Execute(windowView, (view, buffer) => new FindReferencesCommandArgs(view, buffer));
        Check("presentation.references", true, "Find References was handled for the Presentation custom control.");
        Execute(windowView, (view, buffer) => new RenameCommandArgs(view, buffer));
        Check("presentation.rename", true, "Rename started for the Presentation custom control.");
        ExecuteOle(windowView, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.CANCEL);

        MoveCaretTo(windowView, "BrandMark", offset: 2);
        Execute(windowView, (view, buffer) => new GoToDefinitionCommandArgs(view, buffer));
        await WaitAsync(
            () =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                string? activePath = dte.ActiveDocument?.FullName;
                return string.Equals(activePath, request.PresentationBrandMarkCodePath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(activePath, request.PresentationBrandMarkPath, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(30),
            "Go To Definition did not navigate from PresentationWindow to BrandMark.");
        Check(
            "presentation.definition",
            true,
            $"Go To Definition opened {Path.GetFileName(dte.ActiveDocument.FullName)} in Community.");

        IWpfTextView motionView = OpenView(request.PresentationMotionPath);
        MoveCaretTo(motionView, "</UserControl>");
        const string signatureProbe = "Tween(100ms, )";
        TypeText(motionView, signatureProbe);
        MoveCaretTo(motionView, signatureProbe, signatureProbe.Length - 1);
        ISignatureHelpSession signature = await WaitForSignatureHelpAsync(motionView);
        Check(
            "presentation.signature-help",
            signature.Signatures.Count > 0,
            $"Signature Help returned {signature.Signatures.Count} signatures in MotionChapterView.crn.");
        signature.Dismiss();
        DeleteTextWithCommands(motionView, signatureProbe);
        await WaitForNoErrorTagsAsync(
            motionView,
            TimeSpan.FromSeconds(30),
            "Removing the Presentation signature probe retained diagnostics.");

        MoveCaretTo(brandView, "</StackPanel>");
        TypeText(brandView, "<!-- presentation-brand-overlay -->\n");
        MoveCaretTo(motionView, "</UserControl>");
        TypeText(motionView, "<!-- presentation-motion-overlay -->\n");
        Check(
            "presentation.two-documents",
            brandView.TextSnapshot.GetText().Contains("presentation-brand-overlay") &&
                motionView.TextSnapshot.GetText().Contains("presentation-motion-overlay"),
            "Two Presentation documents retained independent unsaved editor overlays.");

        Dictionary<string, IWpfTextView> presentationViews = request.PresentationPaths.ToDictionary(
            path => path,
            OpenView,
            StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> falseErrors = await WaitForStablePresentationDiagnosticsAsync(presentationViews);
        Check(
            "presentation.zero-false-errors",
            falseErrors.Count == 0,
            falseErrors.Count == 0
                ? $"All {presentationViews.Count} valid CernealaPresentation documents have zero editor error tags."
                : string.Join(" | ", falseErrors));

        string[] changedFiles = originalFiles
            .Where(pair => !string.Equals(File.ReadAllText(pair.Key), pair.Value, StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToArray();
        Check(
            "presentation.repository-files-unchanged",
            changedFiles.Length == 0,
            changedFiles.Length == 0
                ? "The complete Presentation matrix stayed in unsaved overlays and left repository files byte-for-byte unchanged."
                : $"Unexpected disk changes: {string.Join(", ", changedFiles)}");
    }

    private async Task<IReadOnlyList<string>> WaitForStablePresentationDiagnosticsAsync(
        IReadOnlyDictionary<string, IWpfTextView> views)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        DateTime? zeroSince = null;
        IReadOnlyList<string> lastErrors = Array.Empty<string>();
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastErrors = views
                .SelectMany(pair => GetErrorTags(pair.Value)
                    .Select(error => $"{Path.GetFileName(pair.Key)}: {error}"))
                .ToArray();
            if (lastErrors.Count == 0)
            {
                zeroSince ??= DateTime.UtcNow;
                if (DateTime.UtcNow - zeroSince.Value >= TimeSpan.FromSeconds(3))
                {
                    return lastErrors;
                }
            }
            else
            {
                zeroSince = null;
            }

            await Task.Delay(250, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        return lastErrors;
    }

    private async Task<IReadOnlyList<string>> WaitForStableEditorDiagnosticsAsync(IWpfTextView view)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
        DateTime stableSince = DateTime.UtcNow;
        string previousSnapshot = string.Empty;
        IReadOnlyList<string> lastErrors = Array.Empty<string>();
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastErrors = GetErrorTags(view);
            string currentSnapshot = string.Join("\n", lastErrors.OrderBy(error => error, StringComparer.Ordinal));
            if (!string.Equals(currentSnapshot, previousSnapshot, StringComparison.Ordinal))
            {
                previousSnapshot = currentSnapshot;
                stableSince = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow - stableSince >= TimeSpan.FromSeconds(5))
            {
                return lastErrors;
            }

            await Task.Delay(250, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        return lastErrors;
    }

    private async Task<string> DescribeCSharpWorkspaceStateAsync(string documentPath)
    {
        Type? workspaceType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type =>
                type.Name.IndexOf("VisualStudioWorkspace", StringComparison.OrdinalIgnoreCase) >= 0 &&
                type.GetProperty("CurrentSolution") is not null);
        if (workspaceType is null)
        {
            return "VisualStudioWorkspace type was not available.";
        }

        MethodInfo getService = typeof(Microsoft.VisualStudio.ComponentModelHost.IComponentModel)
            .GetMethods()
            .Single(method => method.Name == "GetService" && method.IsGenericMethodDefinition)
            .MakeGenericMethod(workspaceType);
        object? workspace = getService.Invoke(componentModel, null);
        object? solution = workspace is null ? null : GetPropertyValue(workspace, "CurrentSolution");
        IEnumerable projects = (solution is null ? null : GetPropertyValue(solution, "Projects")) as IEnumerable
            ?? Array.Empty<object>();
        object? project = projects.Cast<object>().FirstOrDefault(candidate =>
        {
            IEnumerable documents = (IEnumerable)(GetPropertyValue(candidate, "Documents")
                ?? Array.Empty<object>());
            return documents.Cast<object>().Any(document =>
            {
                string? filePath = GetPropertyValue(document, "FilePath") as string;
                return filePath is not null && string.Equals(
                    Path.GetFullPath(filePath),
                    Path.GetFullPath(documentPath),
                    StringComparison.OrdinalIgnoreCase);
            });
        });
        if (project is null)
        {
            string projectNames = string.Join(",", projects.Cast<object>()
                .Select(candidate => GetPropertyValue(candidate, "Name") as string));
            return $"The project containing {Path.GetFileName(documentPath)} was absent from VisualStudioWorkspace; " +
                $"available projects=[{projectNames}].";
        }

        IEnumerable additionalDocuments = (IEnumerable)(GetPropertyValue(project, "AdditionalDocuments")
            ?? Array.Empty<object>());
        IEnumerable analyzerReferences = (IEnumerable)(GetPropertyValue(project, "AnalyzerReferences")
            ?? Array.Empty<object>());
        string[] analyzers = analyzerReferences.Cast<object>()
            .Select(reference => GetPropertyValue(reference, "FullPath") as string
                ?? reference.ToString()
                ?? "<unknown>")
            .Where(path => path.IndexOf("Cerneala", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(Path.GetFileName)
            .ToArray();

        MethodInfo getCompilationAsync = project.GetType().GetMethod("GetCompilationAsync", new[] { typeof(System.Threading.CancellationToken) })
            ?? throw new InvalidOperationException("Roslyn Project.GetCompilationAsync was unavailable.");
        Task compilationTask = (Task)getCompilationAsync.Invoke(project, new object[] { cancellationToken });
        await compilationTask;
        object? compilation = GetPropertyValue(compilationTask, "Result");
        IEnumerable syntaxTrees = (compilation is null ? null : GetPropertyValue(compilation, "SyntaxTrees")) as IEnumerable
            ?? Array.Empty<object>();
        object[] trees = syntaxTrees.Cast<object>().ToArray();
        string[] generatedTrees = trees
            .Select(tree => GetPropertyValue(tree, "FilePath") as string ?? string.Empty)
            .Where(path => path.IndexOf("Cerneala.SourceGen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("UiMarkupGenerator", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(Path.GetFileName)
            .ToArray();
        MethodInfo? getDiagnostics = compilation?.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
                method.Name == "GetDiagnostics" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(System.Threading.CancellationToken));
        IEnumerable diagnostics = (IEnumerable)(getDiagnostics?.Invoke(compilation, new object[] { cancellationToken })
            ?? Array.Empty<object>());
        string[] cernealaDiagnostics = diagnostics.Cast<object>()
            .Where(diagnostic => (GetPropertyValue(diagnostic, "Id") as string)?
                .StartsWith("CERNEALA", StringComparison.OrdinalIgnoreCase) == true)
            .Select(diagnostic => diagnostic.ToString() ?? "<unknown>")
            .ToArray();

        return $"AdditionalDocuments={additionalDocuments.Cast<object>().Count()}; " +
            $"CernealaAnalyzers=[{string.Join(",", analyzers)}]; " +
            $"SyntaxTrees={trees.Length}; GeneratedTrees=[{string.Join(",", generatedTrees)}]; " +
            $"CernealaDiagnostics=[{string.Join(" | ", cernealaDiagnostics)}].";
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private static object? GetPropertyValue(object instance, string propertyName) =>
        instance.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property =>
                string.Equals(property.Name, propertyName, StringComparison.Ordinal) &&
                property.GetIndexParameters().Length == 0)
            ?.GetValue(instance);
}
