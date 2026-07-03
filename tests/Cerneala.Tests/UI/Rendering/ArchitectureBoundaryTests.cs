namespace Cerneala.Tests.UI.Rendering;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void UiRenderingDoesNotReferenceConcreteBackends()
    {
        string renderingRoot = FindRepositoryPath("UI", "Rendering");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "MonoGameDrawingBackend"
        ];

        foreach (string file in Directory.EnumerateFiles(renderingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiControlsDoNotReferenceConcreteBackends()
    {
        string controlsRoot = FindRepositoryPath("UI", "Controls");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(controlsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Section16ControlsDoNotReferenceConcreteBackends()
    {
        string controlsRoot = FindRepositoryPath("UI", "Controls");
        string[] section16Files =
        [
            Path.Combine(controlsRoot, "Primitives", "RangeBase.cs"),
            Path.Combine(controlsRoot, "Primitives", "Thumb.cs"),
            Path.Combine(controlsRoot, "Primitives", "Track.cs"),
            Path.Combine(controlsRoot, "Primitives", "ScrollBar.cs"),
            Path.Combine(controlsRoot, "ScrollBarVisibility.cs"),
            Path.Combine(controlsRoot, "IScrollInfo.cs"),
            Path.Combine(controlsRoot, "ScrollContentPresenter.cs"),
            Path.Combine(controlsRoot, "ScrollViewer.cs"),
            Path.Combine(controlsRoot, "Slider.cs"),
            Path.Combine(controlsRoot, "ProgressBar.cs"),
            Path.Combine(controlsRoot, "RadioButton.cs"),
            Path.Combine(controlsRoot, "Label.cs"),
            Path.Combine(controlsRoot, "ToolTip.cs"),
            Path.Combine(controlsRoot, "PopupRoot.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in section16Files)
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiControlTemplateApisDoNotReferenceConcreteBackends()
    {
        string controlsRoot = FindRepositoryPath("UI", "Controls");
        string[] templateFiles =
        [
            "ControlTemplate.cs",
            "ControlTemplate{TControl}.cs",
            "TemplateContext.cs",
            "TemplateInstance.cs",
            "TemplateBinding{T}.cs",
            "TemplatePartAttribute.cs",
            "ContentPresenter.cs",
            "ItemsPresenter.cs",
            "ItemsPanelTemplate.cs",
            "DataTemplate.cs",
            "DataTemplate{T}.cs"
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string templateFile in templateFiles)
        {
            string text = File.ReadAllText(Path.Combine(controlsRoot, templateFile));
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiTextDoesNotReferenceConcreteBackends()
    {
        string textRoot = FindRepositoryPath("UI", "Text");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(textRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiResourcesCoreDoesNotReferenceConcreteBackends()
    {
        string resourcesRoot = FindRepositoryPath("UI", "Resources");
        string monoGameResourcesRoot = Path.Combine(resourcesRoot, "MonoGame");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(resourcesRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.StartsWith(monoGameResourcesRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiStylingDoesNotReferenceConcreteBackends()
    {
        string stylingRoot = FindRepositoryPath("UI", "Styling");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(stylingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiPlatformAbstractionsDoNotReferenceConcreteBackends()
    {
        string platformRoot = FindRepositoryPath("UI", "Platform");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "System.Windows.Forms.Clipboard",
            "Windows.UI",
            "Microsoft.UI"
        ];

        foreach (string file in Directory.EnumerateFiles(platformRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiDiagnosticsDoesNotReferenceConcreteBackends()
    {
        string diagnosticsRoot = FindRepositoryPath("UI", "Diagnostics");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "MonoGameDrawingBackend"
        ];

        foreach (string file in Directory.EnumerateFiles(diagnosticsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void MonoGameImageLoadingIsAdapterScoped()
    {
        string resourcesRoot = FindRepositoryPath("UI", "Resources");
        string monoGameResourcesRoot = Path.Combine(resourcesRoot, "MonoGame");

        foreach (string file in Directory.EnumerateFiles(resourcesRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (text.Contains("Texture2D", StringComparison.Ordinal) ||
                text.Contains("SpriteBatch", StringComparison.Ordinal))
            {
                Assert.StartsWith(monoGameResourcesRoot, file, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void UiDrawingDoesNotReferenceRetainedRendering()
    {
        string drawingRoot = FindRepositoryPath("UI", "Drawing");

        foreach (string file in Directory.EnumerateFiles(drawingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            Assert.DoesNotContain("Cerneala.UI.Rendering", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RetainedRenderCache", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ElementRenderCache", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RenderQueueProcessor", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DrawCommandListPoolDeferralIsDocumentedConsistently()
    {
        string renderingRoot = FindRepositoryPath("UI", "Rendering");
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));
        string spec = File.ReadAllText(FindRepositoryPath("openspec", "specs", "retained-rendering-cache", "spec.md"));

        Assert.False(File.Exists(Path.Combine(renderingRoot, "DrawCommandListPool.cs")));
        Assert.Contains("DrawCommandListPool.cs", roadmap, StringComparison.Ordinal);
        Assert.Contains("deferred", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DrawCommandListPool is deferred", spec, StringComparison.Ordinal);
    }

    [Fact]
    public void TextServicesRoadmapCompletionIsDocumented()
    {
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));

        Assert.Contains("- [x] A retained `TextBlock` measures text using the existing Skia/HarfBuzz pipeline through higher-level text services.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `UI/Text/TextRenderer.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Re-rendering unchanged text reuses cached text layout and retained render commands.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void ResourceServicesRoadmapCompletionIsDocumented()
    {
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));

        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Rendering/ResourceRenderDependencyTests.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` — covers resource and control backend boundaries.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Retained render caches include resource dependency identity/version in staleness checks.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Core resources and controls do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] MonoGame image loading is adapter-scoped under `UI/Resources/MonoGame`.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Section16RoadmapCompletionIsDocumentedConsistently()
    {
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));

        Assert.Contains("- [x] `UI/Controls/Primitives/RangeBase.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` - covers section 16 backend boundaries.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] 16. Add scrolling/range controls.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Section17ControlsAndVirtualizationDoNotReferenceConcreteBackends()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "UI", "Controls", "ItemsControl.cs"),
            Path.Combine(root, "UI", "Controls", "ItemCollection.cs"),
            Path.Combine(root, "UI", "Controls", "ItemContainerGenerator.cs"),
            Path.Combine(root, "UI", "Controls", "ItemContainerRecyclePool.cs"),
            Path.Combine(root, "UI", "Controls", "ItemsPresenter.cs"),
            Path.Combine(root, "UI", "Controls", "SelectionModel.cs"),
            Path.Combine(root, "UI", "Controls", "SelectionModel{T}.cs"),
            Path.Combine(root, "UI", "Controls", "Primitives", "Selector.cs"),
            Path.Combine(root, "UI", "Controls", "ListBox.cs"),
            Path.Combine(root, "UI", "Controls", "ListBoxItem.cs"),
            Path.Combine(root, "UI", "Controls", "ComboBox.cs"),
            Path.Combine(root, "UI", "Controls", "TabControl.cs"),
            Path.Combine(root, "UI", "Controls", "TabItem.cs"),
            Path.Combine(root, "UI", "Layout", "Panels", "VirtualizingStackPanel.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "VirtualizationContext.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "RealizationWindow.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Section17RoadmapDoesNotClaimCompletionWhenDedicatedPublicControlTestsAreMissing()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string[] requiredDedicatedTests =
        [
            "tests/Cerneala.Tests/Controls/ItemContainerRecyclePoolTests.cs",
            "tests/Cerneala.Tests/Controls/ComboBoxTests.cs",
            "tests/Cerneala.Tests/Controls/TabControlTests.cs",
            "tests/Cerneala.Tests/Controls/TabItemTests.cs"
        ];

        string[] missingDedicatedTests = requiredDedicatedTests
            .Where(testPath => !File.Exists(Path.Combine(root, testPath.Replace('/', Path.DirectorySeparatorChar))))
            .ToArray();

        if (missingDedicatedTests.Length == 0)
        {
            Assert.Contains("- [x] 17. Add items, selection, and virtualization.", roadmap, StringComparison.Ordinal);
            return;
        }

        Assert.DoesNotContain("- [x] 17. Add items, selection, and virtualization.", roadmap, StringComparison.Ordinal);
        foreach (string missingDedicatedTest in missingDedicatedTests)
        {
            Assert.Contains($"- [ ] `{missingDedicatedTest}`", roadmap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Section18DataApisDoNotReferenceConcreteBackends()
    {
        string dataRoot = FindRepositoryPath("UI", "Data");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(dataRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Section18RoadmapCompletionIsDocumentedConsistently()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string[] requiredFiles =
        [
            "UI/Data/ObservableValue{T}.cs",
            "UI/Data/ObservableList{T}.cs",
            "UI/Data/IObservableList{T}.cs",
            "UI/Data/PropertyAdapter{TOwner,TValue}.cs",
            "UI/Data/Binding.cs",
            "UI/Data/Binding{T}.cs",
            "UI/Data/BindingMode.cs",
            "UI/Data/IValueConverter{TIn,TOut}.cs",
            "UI/Data/CollectionView{T}.cs",
            "UI/Data/SortDescription{T}.cs",
            "UI/Data/FilterPredicate{T}.cs",
            "UI/Data/StringPropertyPath.cs",
            "tests/Cerneala.Tests/UI/Data/ObservableValueTests.cs",
            "tests/Cerneala.Tests/UI/Data/ObservableListTests.cs",
            "tests/Cerneala.Tests/UI/Data/TypedBindingTests.cs",
            "tests/Cerneala.Tests/UI/Data/CollectionViewTests.cs",
            "tests/Cerneala.Tests/UI/Data/StringPropertyPathTests.cs"
        ];

        foreach (string requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, requiredFile.Replace('/', Path.DirectorySeparatorChar))), requiredFile);
            Assert.Contains($"- [x] `{requiredFile}`", roadmap, StringComparison.Ordinal);
        }

        Assert.Contains("- [x] 18. Add typed data observation and binding-light APIs.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Section19DiagnosticsRoadmapCompletionIsDocumentedConsistently()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string[] requiredFiles =
        [
            "UI/Diagnostics/FrameDiagnostics.cs",
            "UI/Diagnostics/LayoutDiagnostics.cs",
            "UI/Diagnostics/RenderDiagnostics.cs",
            "UI/Diagnostics/InputDiagnostics.cs",
            "UI/Diagnostics/DirtyTreeDumper.cs",
            "UI/Diagnostics/ElementTreeDumper.cs",
            "UI/Diagnostics/RenderCacheDumper.cs",
            "UI/Diagnostics/RoutedEventTrace.cs",
            "UI/Diagnostics/StyleTrace.cs",
            "UI/Diagnostics/DebugOverlay.cs",
            "UI/Diagnostics/DebugAdorner.cs",
            "Playground/Cerneala.Playground/Samples/DiagnosticsSample.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/FrameDiagnosticsTests.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/DirtyTreeDumperTests.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/ElementTreeDumperTests.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/RenderCacheDumperTests.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/RoutedEventTraceTests.cs",
            "tests/Cerneala.Tests/UI/Diagnostics/StyleTraceTests.cs"
        ];

        foreach (string requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, requiredFile.Replace('/', Path.DirectorySeparatorChar))), requiredFile);
            Assert.Contains($"- [x] `{requiredFile}`", roadmap, StringComparison.Ordinal);
        }

        Assert.Contains("- [x] Developers can see per-frame measure/arrange/render-cache counts.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Developers can dump which elements are dirty and why.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Developers can trace routed event paths.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Developers can inspect style sources for a property value.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] 19. Add diagnostics/devtools overlays and tree/cache dumpers.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Section20TextEditingRoadmapCompletionIsDocumentedConsistently()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string openSpecSegment = "open" + "spec";
        string changesSegment = "chang" + "es";
        string tasks = File.ReadAllText(Path.Combine(root, openSpecSegment, changesSegment, "add-text-editing-ime", "tasks.md"));
        string[] requiredFiles =
        [
            "UI/Controls/TextBoxBase.cs",
            "UI/Controls/TextBox.cs",
            "UI/Controls/PasswordBox.cs",
            "UI/Text/TextDocument.cs",
            "UI/Text/TextCaret.cs",
            "UI/Text/TextSelection.cs",
            "UI/Text/TextEditor.cs",
            "UI/Text/TextCompositionManager.cs",
            "UI/Text/TextCompositionState.cs",
            "UI/Text/UndoRedoStack.cs",
            "UI/Text/ClipboardAdapter.cs",
            "UI/Platform/ITextInputPlatform.cs",
            "tests/Cerneala.Tests/Controls/TextBoxTests.cs",
            "tests/Cerneala.Tests/Controls/PasswordBoxTests.cs",
            "tests/Cerneala.Tests/UI/Text/TextEditorTests.cs",
            "tests/Cerneala.Tests/UI/Text/TextCompositionManagerTests.cs",
            "tests/Cerneala.Tests/UI/Text/UndoRedoStackTests.cs"
        ];

        foreach (string requiredFile in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, requiredFile.Replace('/', Path.DirectorySeparatorChar))), requiredFile);
            Assert.Contains($"- [x] `{requiredFile}`", roadmap, StringComparison.Ordinal);
        }

        Assert.Contains("- [x] 4.5 Run `openspec validate add-text-editing-ime --strict` and `dotnet test`.", tasks, StringComparison.Ordinal);
        Assert.Contains("- [x] 20. Add text editing and IME composition.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeTestsDoNotDependOnActiveOpenSpecChanges()
    {
        string testsRoot = FindRepositoryPath("tests", "Cerneala.Tests");
        string openSpecSegment = "open" + "spec";
        string changesSegment = "chang" + "es";
        string activeChangePathPattern = string.Join("\"" + ", " + "\"", openSpecSegment, changesSegment);
        string activeChangeSlashPattern = string.Join("/", openSpecSegment, changesSegment);

        foreach (string file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            Assert.DoesNotContain(activeChangePathPattern, text, StringComparison.Ordinal);
            Assert.DoesNotContain(activeChangeSlashPattern, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Section20TextEditingApisDoNotReferenceConcreteBackendsOrNativeTextApis()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "UI", "Controls", "TextBoxBase.cs"),
            Path.Combine(root, "UI", "Controls", "TextBox.cs"),
            Path.Combine(root, "UI", "Controls", "PasswordBox.cs"),
            Path.Combine(root, "UI", "Text", "TextDocument.cs"),
            Path.Combine(root, "UI", "Text", "TextCaret.cs"),
            Path.Combine(root, "UI", "Text", "TextSelection.cs"),
            Path.Combine(root, "UI", "Text", "TextEditor.cs"),
            Path.Combine(root, "UI", "Text", "TextCompositionManager.cs"),
            Path.Combine(root, "UI", "Text", "TextCompositionState.cs"),
            Path.Combine(root, "UI", "Text", "UndoRedoStack.cs"),
            Path.Combine(root, "UI", "Text", "ClipboardAdapter.cs"),
            Path.Combine(root, "UI", "Platform", "ITextInputPlatform.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "System.Windows.Forms.Clipboard",
            "System.Windows.Clipboard",
            "System.Windows.Input.InputMethod",
            "Windows.UI",
            "Microsoft.UI",
            "TextServicesFramework",
            "ImmGet",
            "ImmSet",
            "HIMC"
        ];

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    private static string FindRepositoryPath(params string[] segments)
    {
        string repositoryRoot = FindRepositoryRoot();
        string candidate = Path.Combine(new[] { repositoryRoot }.Concat(segments).ToArray());

        if (Directory.Exists(candidate) || File.Exists(candidate))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException($"Could not find repository path: {Path.Combine(segments)}");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
