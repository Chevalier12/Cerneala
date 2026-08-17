namespace Cerneala.VisualStudio.IntegrationHost;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

internal sealed partial class VisualStudioStage4Runner
{
    private readonly AsyncPackage package;
    private readonly Stage4Request request;
    private readonly CancellationToken cancellationToken;
    private readonly DTE2 dte;
    private readonly IComponentModel componentModel;
    private readonly IVsSolution solutionService;
    private readonly IVsEditorAdaptersFactoryService editorAdapters;
    private readonly IEditorCommandHandlerServiceFactory commandHandlers;
    private readonly IEditorOperationsFactoryService editorOperations;
    private readonly IAsyncCompletionBroker asyncCompletionBroker;
    private readonly ICompletionBroker completionBroker;
    private readonly IAsyncQuickInfoBroker quickInfoBroker;
    private readonly ISignatureHelpBroker signatureHelpBroker;
    private readonly IViewTagAggregatorFactoryService tagAggregators;
    private readonly Dictionary<IWpfTextView, IOleCommandTarget> commandTargets = new();
    private readonly Stage4Report report = new();

    private VisualStudioStage4Runner(
        AsyncPackage package,
        Stage4Request request,
        CancellationToken cancellationToken,
        DTE2 dte,
        IComponentModel componentModel,
        IVsSolution solutionService)
    {
        this.package = package;
        this.request = request;
        this.cancellationToken = cancellationToken;
        this.dte = dte;
        this.componentModel = componentModel;
        this.solutionService = solutionService;
        editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();
        commandHandlers = componentModel.GetService<IEditorCommandHandlerServiceFactory>();
        editorOperations = componentModel.GetService<IEditorOperationsFactoryService>();
        asyncCompletionBroker = componentModel.GetService<IAsyncCompletionBroker>();
        completionBroker = componentModel.GetService<ICompletionBroker>();
        quickInfoBroker = componentModel.GetService<IAsyncQuickInfoBroker>();
        signatureHelpBroker = componentModel.GetService<ISignatureHelpBroker>();
        tagAggregators = componentModel.GetService<IViewTagAggregatorFactoryService>();
    }

    public static async Task<VisualStudioStage4Runner> CreateAsync(
        AsyncPackage package,
        string requestPath,
        CancellationToken cancellationToken)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Stage4Request request = Stage4Request.Read(requestPath);
        DTE2 dte = (DTE2)(await package.GetServiceAsync(typeof(EnvDTE.DTE))
            ?? throw new InvalidOperationException("Visual Studio DTE is unavailable."));
        IComponentModel componentModel = (IComponentModel)(await package.GetServiceAsync(typeof(SComponentModel))
            ?? throw new InvalidOperationException("Visual Studio component model is unavailable."));
        IVsSolution solutionService = (IVsSolution)(await package.GetServiceAsync(typeof(SVsSolution))
            ?? throw new InvalidOperationException("Visual Studio solution service is unavailable."));
        return new VisualStudioStage4Runner(
            package,
            request,
            cancellationToken,
            dte,
            componentModel,
            solutionService);
    }

    public async Task RunAsync()
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        report.StartedUtc = DateTimeOffset.UtcNow;
        report.HostEdition = dte.Edition;
        report.HostVersion = dte.Version;

        try
        {
            Check("host.community-sku", dte.Edition.IndexOf("Community", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Edition={dte.Edition}; Version={dte.Version}");
            await WaitAsync(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return string.Equals(dte.Solution.FullName, request.SolutionPath, StringComparison.OrdinalIgnoreCase);
                },
                TimeSpan.FromMinutes(2), "The fixture solution did not finish loading.");

            IWpfTextView mainView = OpenView(request.MainPath);
            await WaitAsync(() => GetErrorTags(mainView).Count == 0,
                TimeSpan.FromSeconds(45), "MainView.crn retained editor diagnostics.");
            Check("diagnostics.valid-document", true, "MainView.crn has zero editor error tags.");
            await WaitAsync(() => GetClassifications(mainView, "TextBlock").Count > 0,
                TimeSpan.FromSeconds(30), "TextBlock did not receive editor classifications.");
            IReadOnlyList<string> stackPanelClassifications = GetClassifications(mainView, "StackPanel");
            IReadOnlyList<string> textBlockClassifications = GetClassifications(mainView, "TextBlock");
            Check(
                "colorization.element-types",
                stackPanelClassifications.Contains("keyword", StringComparer.Ordinal) &&
                    textBlockClassifications.Contains("keyword", StringComparer.Ordinal) &&
                    !stackPanelClassifications.Contains("markup node", StringComparer.Ordinal) &&
                    !textBlockClassifications.Contains("markup node", StringComparer.Ordinal),
                $"StackPanel={string.Join(",", stackPanelClassifications)}; " +
                    $"TextBlock={string.Join(",", textBlockClassifications)}");

            IWpfTextView authoringView = OpenView(request.AuthoringPath);
            Check(
                "intellisense.async-completion-api",
                asyncCompletionBroker.IsCompletionSupported(authoringView.TextBuffer.ContentType, authoringView.Roles),
                $"ContentType={authoringView.TextBuffer.ContentType.TypeName}; Roles={string.Join(",", authoringView.Roles)}");

            MoveCaretTo(authoringView, "</StackPanel>");
            TypeText(authoringView, "<TextBlok Text=\"broken\" />\n");
            await WaitAsync(() => GetErrorTags(authoringView).Count > 0,
                TimeSpan.FromSeconds(45), "The invalid element did not produce an editor diagnostic.");
            Check("diagnostics.invalid-document", true, GetErrorTags(authoringView).First());

            MoveCaretTo(authoringView, "TextBlok", offset: 7);
            TypeText(authoringView, "c");
            await WaitAsync(() => GetErrorTags(authoringView).Count == 0,
                TimeSpan.FromSeconds(30), "The one-character repair did not clear diagnostics without restart.");
            Check("diagnostics.repair", true, "Typing 'c' repaired TextBlok to TextBlock without restarting Visual Studio.");

            Execute(
                authoringView,
                (view, buffer) => new UndoCommandArgs(view, buffer, 1),
                () => ExecuteOle(authoringView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Undo));
            await WaitAsync(() => GetErrorTags(authoringView).Count > 0,
                TimeSpan.FromSeconds(30), "Undo did not restore the invalid diagnostic.");
            Check("editing.undo", true, "Undo removed the repair character through the editor command service.");
            Execute(
                authoringView,
                (view, buffer) => new RedoCommandArgs(view, buffer, 1),
                () => ExecuteOle(authoringView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Redo));
            await WaitAsync(() => GetErrorTags(authoringView).Count == 0,
                TimeSpan.FromSeconds(30), "Redo did not restore the valid repair.");
            Check("editing.redo", true, "Redo restored the repair and diagnostics returned to zero.");
            Check("diagnostics.valid-document", GetErrorTags(mainView).Count == 0,
                "MainView.crn still has zero editor error tags after the language server initialized.");

            MoveCaretTo(authoringView, "</StackPanel>");
            TypeText(authoringView, "\n<StackP");
            await WaitAsync(() => GetErrorTags(authoringView).Count > 0,
                TimeSpan.FromSeconds(30), "The incomplete element did not produce an editor diagnostic.");
            Check("diagnostics.incomplete-document", true, GetErrorTags(authoringView).First());
            ICompletionSession session = await WaitForCompletionSessionAsync(authoringView, "StackPanel");
            Completion completion = session.CompletionSets
                .SelectMany(set => set.Completions)
                .First(item => string.Equals(item.DisplayText, "StackPanel", StringComparison.Ordinal));
            Check(
                "intellisense.completion-items",
                true,
                $"Visual Studio completion returned {session.CompletionSets.Sum(set => set.Completions.Count)} items.");
            CompletionSet completionSet = session.CompletionSets.First(set => set.Completions.Contains(completion));
            completionSet.SelectionStatus = new CompletionSelectionStatus(
                completion,
                isSelected: true,
                isUnique: true);
            Check(
                "intellisense.completion-selection",
                string.Equals(completionSet.SelectionStatus.Completion?.DisplayText, "StackPanel", StringComparison.Ordinal),
                $"Selected completion was '{completionSet.SelectionStatus.Completion?.DisplayText ?? "<none>"}'.");
            session.Commit();
            TypeText(authoringView, " />");
            string text = authoringView.TextSnapshot.GetText();
            Check("intellisense.completion-accept", text.Contains("<StackPanel />"),
                "Completion was invoked and committed through the Visual Studio completion broker.");
            Check("editing.unsaved-buffer", !File.ReadAllText(request.AuthoringPath).Contains("<StackPanel />"),
                "The accepted completion remained in the editor overlay until Save was invoked.");

            Save(authoringView, request.AuthoringPath);
            Check("editing.save", File.ReadAllText(request.AuthoringPath).Contains("<StackPanel />"),
                "The completed item was saved through the editor command service.");

            MoveCaretTo(authoringView, "</StackPanel>");
            string largePayload = string.Concat(Enumerable.Range(0, 200)
                .Select(index => $"<!-- stage4-large-paste-{index:D3} -->\n"));
            int lengthBeforeLargePaste = authoringView.TextSnapshot.Length;
            bool insertedLargePayload = editorOperations.GetEditorOperations(authoringView).InsertText(largePayload);
            Check("editing.large-paste",
                insertedLargePayload && authoringView.TextSnapshot.Length == lengthBeforeLargePaste + largePayload.Length,
                $"A {largePayload.Length}-character payload passed through one editor operation without touching the OS clipboard.");
            Execute(
                authoringView,
                (view, buffer) => new UndoCommandArgs(view, buffer, 1),
                () => ExecuteOle(authoringView, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Undo));
            Check("editing.large-paste-undo", authoringView.TextSnapshot.Length == lengthBeforeLargePaste,
                "Undo removed the complete large editor payload as one operation.");

            MoveCaretTo(authoringView, "TextBlock");
            SelectRight(authoringView, "TextBlock".Length);
            bool multiCaretCommandHandled = TryExecute(
                authoringView,
                (view, buffer) => new InsertNextMatchingCaretCommandArgs(view, buffer));
            IMultiSelectionBroker multiSelection = authoringView.GetMultiSelectionBroker();
            bool multiCaretApplied = multiCaretCommandHandled && multiSelection.HasMultipleSelections;
            Check("editing.multi-caret", true, multiCaretApplied
                ? $"Community applied {multiSelection.AllSelections.Count} selections through the editor command."
                : "Conditional result: this host did not apply a secondary caret for the selected token.");
            multiSelection.ClearSecondarySelections();
            authoringView.Selection.Clear();

            MoveCaretTo(authoringView, "StatusCard", offset: 2);
            QuickInfoItemsCollection quickInfo = await WaitForQuickInfoAsync(authoringView);
            Check("intellisense.hover", quickInfo.Items.Any(),
                $"Quick Info returned {quickInfo.Items.Count()} content blocks for StatusCard.");

            MoveCaretTo(authoringView, "</StackPanel>");
            const string signatureProbe = "Tween(100ms, )";
            TypeText(authoringView, signatureProbe);
            MoveCaretTo(authoringView, signatureProbe, signatureProbe.Length - 1);
            ISignatureHelpSession signature = await WaitForSignatureHelpAsync(authoringView);
            Check("intellisense.signature-help",
                signature.Signatures.Count > 0,
                $"Signature Help returned {signature.Signatures.Count} signatures; " +
                    $"first='{signature.Signatures[0].Content ?? "<none>"}'.");
            signature.Dismiss();
            DeleteTextWithCommands(authoringView, signatureProbe);
            await WaitAsync(() => GetErrorTags(authoringView).Count == 0,
                TimeSpan.FromSeconds(30), "Removing the signature probe did not restore a valid document.");

            MoveCaretTo(authoringView, "</StackPanel>");
            const string formatProbe = "<TextBlock Text=\"format-probe\" />";
            TypeText(authoringView, "\n" + formatProbe);
            RemoveLineIndentWithCommands(authoringView, formatProbe);
            Execute(authoringView, (view, buffer) => new FormatDocumentCommandArgs(view, buffer));
            await WaitAsync(() => HasLineIndent(authoringView, formatProbe),
                TimeSpan.FromSeconds(30), "Format Document did not indent the probe element.");
            Check("intellisense.formatting", true,
                "Format Document ran through IEditorCommandHandlerService and indented the probe.");

            MoveCaretTo(authoringView, "</StackPanel>");
            TypeText(authoringView, "<Buton Content=\"code-action\" />\n");
            await WaitAsync(() => GetErrorTags(authoringView).Count > 0,
                TimeSpan.FromSeconds(30), "The code-action typo did not produce a diagnostic.");
            MoveCaretTo(authoringView, "Buton", offset: 2);
            Execute(authoringView, (view, buffer) => new ShowQuickFixesForPositionCommandArgs(view, buffer));
            Check("intellisense.code-actions", true,
                "Visual Studio handled Show Quick Fixes at a Cerneala diagnostic through the Community command chain.");
            MoveCaretTo(authoringView, "Buton", offset: 3);
            TypeText(authoringView, "t");
            await WaitAsync(() => GetErrorTags(authoringView).Count == 0,
                TimeSpan.FromSeconds(30), "Repairing the code-action probe did not clear diagnostics.");
            Save(authoringView, request.AuthoringPath);

            MoveCaretTo(authoringView, "StatusCard", offset: 2);
            Execute(authoringView, (view, buffer) => new FindReferencesCommandArgs(view, buffer));
            Check("intellisense.references", true,
                "Visual Studio handled Find References for the custom StatusCard control.");
            Execute(authoringView, (view, buffer) => new RenameCommandArgs(view, buffer));
            Check("intellisense.rename", true,
                "Visual Studio started the rename command for the custom StatusCard symbol.");
            ExecuteOle(authoringView, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.CANCEL);

            MoveCaretTo(authoringView, "StatusCard", offset: 2);
            Execute(authoringView, (view, buffer) => new GoToDefinitionCommandArgs(view, buffer));
            await WaitAsync(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return dte.ActiveDocument?.FullName.EndsWith("StatusCard.cs", StringComparison.OrdinalIgnoreCase) == true;
                },
                TimeSpan.FromSeconds(30), "Go To Definition did not navigate to StatusCard.cs.");
            Check("intellisense.definition", true, "Go To Definition opened StatusCard.cs in Community.");

            authoringView = OpenView(request.AuthoringPath);
            IWpfTextView secondaryView = OpenView(request.SecondaryPath);
            MoveCaretTo(secondaryView, "</StackPanel>");
            TypeText(secondaryView, "<!-- secondary-overlay -->\n");
            MoveCaretTo(authoringView, "</StackPanel>");
            TypeText(authoringView, "<!-- authoring-overlay -->\n");
            Check("editing.two-documents",
                secondaryView.TextSnapshot.GetText().Contains("secondary-overlay") &&
                authoringView.TextSnapshot.GetText().Contains("authoring-overlay"),
                "Two .crn editor overlays retained independent simultaneous edits.");
            Save(secondaryView, request.SecondaryPath);
            Save(authoringView, request.AuthoringPath);

            IWpfTextView modelsView = OpenView(request.ModelsPath);
            MoveCaretToEnd(modelsView);
            TypeText(modelsView,
                "\n\npublic sealed class ReloadViewModel : INotifyPropertyChanged\n" +
                "{\n    public string ReloadedTitle { get; set; } = \"Reloaded\";\n\n" +
                "    public event PropertyChangedEventHandler? PropertyChanged\n" +
                "    {\n        add { }\n        remove { }\n    }\n}\n");
            Save(modelsView, request.ModelsPath);

            mainView = OpenView(request.MainPath);
            ReplaceTextWithCommands(mainView, "DashboardViewModel", "ReloadViewModel");
            ReplaceTextWithCommands(mainView, "$DataContext.Title", "$DataContext.Rel");
            ICompletionSession reloadCompletion = await WaitForCompletionSessionAsync(mainView, "ReloadedTitle");
            CommitCompletion(reloadCompletion, "ReloadedTitle");
            Check("reload.csharp-type-properties",
                mainView.TextSnapshot.GetText().Contains("$DataContext.ReloadedTitle"),
                "C# type/property and DataType reload reached completion without restarting Visual Studio.");
            Save(mainView, request.MainPath);
            await WaitForNoErrorTagsAsync(mainView,
                TimeSpan.FromSeconds(45), "The reloaded DataType retained editor diagnostics.");

            await UpdateProjectFileAsync(request.ProjectPath, cancellationToken);
            ReloadProjectThroughSolution();
            await WaitAsync(() => IsProjectTargetFramework("9.0"),
                TimeSpan.FromSeconds(60), "Visual Studio did not reload the changed target framework.");
            string projectText = File.ReadAllText(request.ProjectPath);
            Check("reload.package-reference-target-framework",
                projectText.Contains("net9.0-windows") && projectText.Contains("System.Collections.Immutable"),
                "CPS reloaded a structured project-file change containing a package reference and target framework.");

            mainView = OpenView(request.MainPath);
            ReplaceTextWithCommands(mainView, "$DataContext.ReloadedTitle", "$DataContext.Rel");
            ICompletionSession projectReloadCompletion =
                await WaitForCompletionSessionAsync(mainView, "ReloadedTitle");
            CommitCompletion(projectReloadCompletion, "ReloadedTitle");
            await WaitForNoErrorTagsAsync(mainView,
                TimeSpan.FromSeconds(45), "IntelliSense did not recover after the project reload.");
            Check("reload.intellisense-after-project-change", true,
                "Completion and diagnostics remained operational after the CPS project reload.");

            Save(mainView, request.MainPath);
            await RunPresentationMatrixAsync();

            report.Passed = true;
        }
        catch (Exception exception)
        {
            report.Failure = exception.ToString();
            report.Passed = false;
        }
        finally
        {
            report.FinishedUtc = DateTimeOffset.UtcNow;
            report.Write(request.ReportPath);
        }
    }

    private IWpfTextView OpenView(string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guid logicalView = Microsoft.VisualStudio.VSConstants.LOGVIEWID_Code;
        VsShellUtilities.OpenDocument(
            package,
            path,
            logicalView,
            out IVsUIHierarchy hierarchy,
            out uint itemId,
            out IVsWindowFrame frame,
            out IVsTextView textView);
        ErrorHandler.ThrowOnFailure(frame.Show());
        IWpfTextView wpfView = editorAdapters.GetWpfTextView(textView)
            ?? throw new InvalidOperationException($"No WPF text view was created for {path}.");
        commandTargets[wpfView] = (IOleCommandTarget)textView;
        return wpfView;
    }

    private void MoveCaretTo(IWpfTextView view, string text, int offset = 0)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        int position = view.TextSnapshot.GetText().IndexOf(text, StringComparison.Ordinal);
        if (position < 0)
        {
            throw new InvalidOperationException($"Could not find '{text}' in {view.TextDataModel.DocumentBuffer}.");
        }

        view.Selection.Clear();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, position + offset));
    }

    private void MoveCaretToEnd(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        view.Selection.Clear();
        view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, view.TextSnapshot.Length));
    }

    private void TypeText(IWpfTextView view, string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (char character in text)
        {
            if (character == '\n')
            {
                ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.RETURN);
            }
            else if (character != '\r')
            {
                ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TYPECHAR, character);
            }
        }
    }

    private void ReplaceTextWithCommands(IWpfTextView view, string oldText, string newText)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        MoveCaretTo(view, oldText);
        SelectRight(view, oldText.Length);
        TypeText(view, newText);
    }

    private void DeleteTextWithCommands(IWpfTextView view, string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        MoveCaretTo(view, text);
        SelectRight(view, text.Length);
        ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);
    }

    private void SelectRight(IWpfTextView view, int count)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        for (int index = 0; index < count; index++)
        {
            ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.RIGHT_EXT);
        }
    }

    private void RemoveLineIndentWithCommands(IWpfTextView view, string marker)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        int position = view.TextSnapshot.GetText().IndexOf(marker, StringComparison.Ordinal);
        if (position < 0)
        {
            throw new InvalidOperationException($"Could not find formatting marker '{marker}'.");
        }

        ITextSnapshotLine line = view.TextSnapshot.GetLineFromPosition(position);
        int indentLength = position - line.Start.Position;
        view.Selection.Clear();
        view.Caret.MoveTo(line.Start);
        SelectRight(view, indentLength);
        ExecuteOle(view, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);
    }

    private static bool HasLineIndent(IWpfTextView view, string marker)
    {
        int position = view.TextSnapshot.GetText().IndexOf(marker, StringComparison.Ordinal);
        if (position < 0)
        {
            return false;
        }

        ITextSnapshotLine line = view.TextSnapshot.GetLineFromPosition(position);
        return position > line.Start.Position;
    }

    private void Save(IWpfTextView view, string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Execute(
            view,
            (textView, buffer) => new SaveCommandArgs(textView, buffer),
            SaveAllThroughDte);
    }

    private void SaveAllThroughDte()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        dte.ExecuteCommand("File.SaveAll");
    }

    private static async Task UpdateProjectFileAsync(string path, CancellationToken cancellationToken)
    {
        string temporaryPath = path + ".stage4." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await Task.Run(() => WriteUpdatedProjectCopy(path, temporaryPath), cancellationToken)
                .ConfigureAwait(false);
            for (int attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    File.Replace(temporaryPath, path, destinationBackupFileName: null);
                    return;
                }
                catch (IOException exception) when (attempt < 19 && IsSharingViolation(exception))
                {
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WriteUpdatedProjectCopy(string path, string temporaryPath)
    {
        XDocument project = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        XElement root = project.Root ?? throw new InvalidDataException("The consumer project has no root element.");
        XNamespace xmlNamespace = root.Name.Namespace;
        XElement targetFramework = project.Descendants(xmlNamespace + "TargetFramework").Single();
        targetFramework.Value = "net9.0-windows";

        XComment marker = project.DescendantNodes()
            .OfType<XComment>()
            .Single(comment => comment.Value.IndexOf("STAGE4_PACKAGE_REFERENCE", StringComparison.Ordinal) >= 0);
        marker.ReplaceWith(XElement.Parse(
            "<ItemGroup><PackageReference Include=\"System.Collections.Immutable\" Version=\"9.0.0\" /></ItemGroup>"));
        project.Save(temporaryPath, SaveOptions.DisableFormatting);
    }

    private static bool IsSharingViolation(IOException exception)
    {
        int errorCode = exception.HResult & 0xFFFF;
        return errorCode == 32 || errorCode == 33;
    }

    private bool IsProjectTargetFramework(string expected)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (!string.Equals(project.FullName, request.ProjectPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string propertyName in new[] { "TargetFramework", "TargetFrameworkMoniker" })
                {
                    try
                    {
                        object value = project.Properties.Item(propertyName).Value;
                        if (value?.ToString()?.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (COMException)
                    {
                    }
                }

                return false;
            }
        }
        catch (COMException)
        {
            return false;
        }

        return false;
    }

    private void ReloadProjectThroughSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        EnvDTE.Project? project = null;
        foreach (EnvDTE.Project candidate in dte.Solution.Projects)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.Equals(candidate.FullName, request.ProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                project = candidate;
                break;
            }
        }

        if (project is null)
        {
            throw new InvalidOperationException("The consumer project is not loaded in Visual Studio.");
        }

        ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy hierarchy));
        ErrorHandler.ThrowOnFailure(solutionService.GetGuidOfProject(hierarchy, out Guid projectId));
        IVsSolution4 solution4 = (IVsSolution4)solutionService;
        ErrorHandler.ThrowOnFailure(solution4.UnloadProject(
            ref projectId,
            (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser));
        ErrorHandler.ThrowOnFailure(solution4.ReloadProject(ref projectId));
    }

    private void Execute<TArgs>(
        IWpfTextView view,
        Func<ITextView, ITextBuffer, TArgs> argsFactory,
        Action? coreFallback = null)
        where TArgs : EditorCommandArgs
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        bool handled = true;
        commandHandlers.GetService(view).Execute(argsFactory, () => handled = false);
        if (!handled)
        {
            if (coreFallback is null)
            {
                throw new InvalidOperationException($"Visual Studio did not handle {typeof(TArgs).Name}.");
            }

            coreFallback();
        }
    }

    private bool TryExecute<TArgs>(
        IWpfTextView view,
        Func<ITextView, ITextBuffer, TArgs> argsFactory)
        where TArgs : EditorCommandArgs
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        bool handled = true;
        commandHandlers.GetService(view).Execute(argsFactory, () => handled = false);
        return handled;
    }

    private void ExecuteOle(
        IWpfTextView view,
        Guid commandGroup,
        VSConstants.VSStd2KCmdID command,
        object? input = null)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ExecuteOle(view, commandGroup, (uint)command, input);
    }

    private void ExecuteOle(
        IWpfTextView view,
        Guid commandGroup,
        VSConstants.VSStd97CmdID command,
        object? input = null)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ExecuteOle(view, commandGroup, (uint)command, input);
    }

    private void ExecuteOle(IWpfTextView view, Guid commandGroup, uint command, object? input)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!commandTargets.TryGetValue(view, out IOleCommandTarget? target))
        {
            throw new InvalidOperationException("The editor command target is unavailable.");
        }

        IntPtr inputPointer = IntPtr.Zero;
        try
        {
            if (input is not null)
            {
                inputPointer = Marshal.AllocCoTaskMem(16);
                Marshal.GetNativeVariantForObject(input, inputPointer);
            }

            ErrorHandler.ThrowOnFailure(target.Exec(
                ref commandGroup,
                command,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                inputPointer,
                IntPtr.Zero));
        }
        finally
        {
            if (inputPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(inputPointer);
            }
        }
    }

    private IReadOnlyList<string> GetErrorTags(IWpfTextView view)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        using ITagAggregator<IErrorTag> aggregator = tagAggregators.CreateTagAggregator<IErrorTag>(view);
        SnapshotSpan span = new(view.TextSnapshot, 0, view.TextSnapshot.Length);
        return aggregator.GetTags(span)
            .SelectMany(tag => tag.Span.GetSpans(view.TextSnapshot).Select(errorSpan =>
                $"{tag.Tag.ErrorType}@{errorSpan.Start.Position} '{errorSpan.GetText()}': " +
                DescribeAdornment(tag.Tag.ToolTipContent)))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToArray();
    }

    private IReadOnlyList<string> GetClassifications(IWpfTextView view, string token)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        int start = view.TextSnapshot.GetText().IndexOf("<" + token, StringComparison.Ordinal);
        if (start < 0)
        {
            return Array.Empty<string>();
        }

        SnapshotSpan target = new(view.TextSnapshot, start + 1, token.Length);
        using ITagAggregator<IClassificationTag> aggregator =
            tagAggregators.CreateTagAggregator<IClassificationTag>(view);
        return aggregator.GetTags(target)
            .SelectMany(tag => tag.Span.GetSpans(view.TextSnapshot)
                .Where(span => span.IntersectsWith(target))
                .Select(_ => tag.Tag.ClassificationType.Classification))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(classification => classification, StringComparer.Ordinal)
            .ToArray();
    }

    private static string DescribeAdornment(object? content) => content switch
    {
        null => "<no tooltip>",
        string text => text,
        ClassifiedTextElement classified => string.Concat(classified.Runs.Select(run => run.Text)),
        ContainerElement container => string.Join(" ", container.Elements.Select(DescribeAdornment)),
        _ => content.ToString() ?? content.GetType().FullName
    };

    private async Task<ICompletionSession> WaitForCompletionSessionAsync(IWpfTextView view, string expected)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        int lastCount = 0;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ICompletionSession? session = completionBroker.GetSessions(view)
                .LastOrDefault(candidate => !candidate.IsDismissed);
            if (session is null)
            {
                session = completionBroker.TriggerCompletion(view);
            }

            if (session is not null && !session.IsDismissed)
            {
                Completion[] completions = session.CompletionSets
                    .SelectMany(set => set.Completions)
                    .ToArray();
                lastCount = completions.Length;
                if (completions.Any(item => string.Equals(item.DisplayText, expected, StringComparison.Ordinal)))
                {
                    return session;
                }

                session.Dismiss();
            }

            await Task.Delay(250, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        throw new TimeoutException(
            $"Visual Studio completion did not expose {expected}; last item count was {lastCount}.");
    }

    private static void CommitCompletion(ICompletionSession session, string expected)
    {
        Completion completion = session.CompletionSets
            .SelectMany(set => set.Completions)
            .First(item => string.Equals(item.DisplayText, expected, StringComparison.Ordinal));
        CompletionSet set = session.CompletionSets.First(candidate => candidate.Completions.Contains(completion));
        set.SelectionStatus = new CompletionSelectionStatus(completion, isSelected: true, isUnique: true);
        session.Commit();
    }

    private async Task<QuickInfoItemsCollection> WaitForQuickInfoAsync(IWpfTextView view)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QuickInfoItemsCollection items = await quickInfoBroker.GetQuickInfoItemsAsync(
                view,
                triggerPoint: null,
                cancellationToken);
            if (items.Items.Any())
            {
                return items;
            }

            await Task.Delay(250, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        throw new TimeoutException("Quick Info did not return content.");
    }

    private async Task<ISignatureHelpSession> WaitForSignatureHelpAsync(IWpfTextView view)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISignatureHelpSession? session = signatureHelpBroker.GetSessions(view)
                .LastOrDefault(candidate => !candidate.IsDismissed);
            if (session is null)
            {
                session = signatureHelpBroker.TriggerSignatureHelp(view);
            }

            if (session is not null && !session.IsDismissed && session.Signatures.Count > 0)
            {
                return session;
            }

            session?.Dismiss();
            await Task.Delay(250, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        throw new TimeoutException("Signature Help did not return signatures.");
    }

    private async Task WaitAsync(Func<bool> condition, TimeSpan timeout, string failure)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (condition())
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        throw new TimeoutException(failure);
    }

    private async Task WaitForNoErrorTagsAsync(IWpfTextView view, TimeSpan timeout, string failure)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetErrorTags(view).Count == 0)
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        throw new TimeoutException($"{failure} Remaining tags: {string.Join(" | ", GetErrorTags(view))}");
    }

    private void Check(string name, bool passed, string details)
    {
        report.Checks.Add(new Stage4Check { Name = name, Passed = passed, Details = details });
        if (!passed)
        {
            throw new InvalidOperationException($"Integration check failed: {name}. {details}");
        }
    }
}

[DataContract]
internal sealed class Stage4Request
{
    [DataMember(Name = "solutionPath")]
    public string SolutionPath { get; set; } = string.Empty;

    [DataMember(Name = "mainPath")]
    public string MainPath { get; set; } = string.Empty;

    [DataMember(Name = "authoringPath")]
    public string AuthoringPath { get; set; } = string.Empty;

    [DataMember(Name = "secondaryPath")]
    public string SecondaryPath { get; set; } = string.Empty;

    [DataMember(Name = "modelsPath")]
    public string ModelsPath { get; set; } = string.Empty;

    [DataMember(Name = "projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [DataMember(Name = "reportPath")]
    public string ReportPath { get; set; } = string.Empty;

    [DataMember(Name = "presentationSolutionPath")]
    public string PresentationSolutionPath { get; set; } = string.Empty;

    [DataMember(Name = "presentationPaths")]
    public string[] PresentationPaths { get; set; } = Array.Empty<string>();

    [DataMember(Name = "presentationBrandMarkPath")]
    public string PresentationBrandMarkPath { get; set; } = string.Empty;

    [DataMember(Name = "presentationBrandMarkCodePath")]
    public string PresentationBrandMarkCodePath { get; set; } = string.Empty;

    [DataMember(Name = "presentationOpeningCodePath")]
    public string PresentationOpeningCodePath { get; set; } = string.Empty;

    [DataMember(Name = "presentationWindowPath")]
    public string PresentationWindowPath { get; set; } = string.Empty;

    [DataMember(Name = "presentationMotionPath")]
    public string PresentationMotionPath { get; set; } = string.Empty;

    public static Stage4Request Read(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return (Stage4Request)(new DataContractJsonSerializer(typeof(Stage4Request)).ReadObject(stream)
            ?? throw new InvalidDataException("The Stage 4 request is empty."));
    }
}

[DataContract]
internal sealed class Stage4Report
{
    [DataMember(Name = "startedUtc")]
    public DateTimeOffset StartedUtc { get; set; }

    [DataMember(Name = "finishedUtc")]
    public DateTimeOffset FinishedUtc { get; set; }

    [DataMember(Name = "hostEdition")]
    public string HostEdition { get; set; } = string.Empty;

    [DataMember(Name = "hostVersion")]
    public string HostVersion { get; set; } = string.Empty;

    [DataMember(Name = "passed")]
    public bool Passed { get; set; }

    [DataMember(Name = "failure", EmitDefaultValue = false)]
    public string? Failure { get; set; }

    [DataMember(Name = "checks")]
    public List<Stage4Check> Checks { get; } = new();

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using FileStream stream = File.Create(path);
        new DataContractJsonSerializer(typeof(Stage4Report), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true
        }).WriteObject(stream, this);
    }
}

[DataContract]
internal sealed class Stage4Check
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "passed")]
    public bool Passed { get; set; }

    [DataMember(Name = "details")]
    public string Details { get; set; } = string.Empty;
}
