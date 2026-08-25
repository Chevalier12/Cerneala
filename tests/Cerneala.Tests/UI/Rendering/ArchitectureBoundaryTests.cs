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
            "Microsoft.Xna",
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
            "Microsoft.Xna"
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
    public void UiMediaDoesNotReferenceConcreteBackends()
    {
        string mediaRoot = FindRepositoryPath("UI", "Media");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna",
            "MonoGameDrawingBackend"
        ];

        foreach (string file in Directory.EnumerateFiles(mediaRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void LegacyUiAnimationFolderIsRemoved()
    {
        string root = FindRepositoryRoot();

        Assert.False(Directory.Exists(Path.Combine(root, "UI", "Animation")));
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
            "Microsoft.Xna"
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
            Path.Combine("Templates", "ComponentTemplate.cs"),
            Path.Combine("Templates", "ComponentTemplateContext.cs"),
            Path.Combine("Templates", "ComponentTemplateInstance.cs"),
            Path.Combine("Templates", "TemplateBinding{T}.cs"),
            Path.Combine("Templates", "TemplatePartAttribute.cs"),
            "ContentPresenter.cs",
            "ItemsPresenter.cs",
            Path.Combine("Templates", "ContentTemplate.cs"),
            Path.Combine("Templates", "ContentTemplateContext.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna"
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
            "Microsoft.Xna"
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
            "Microsoft.Xna"
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
    public void UiThemingDoesNotReferenceConcreteBackends()
    {
        string themingRoot = FindRepositoryPath("UI", "Theming");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna"
        ];

        foreach (string file in Directory.EnumerateFiles(themingRoot, "*.cs", SearchOption.AllDirectories))
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
            "Microsoft.Xna",
            "System.Windows.Forms.Clipboard",
            "System.Windows.Automation",
            "Windows.UI",
            "Microsoft.UI",
            "NSAccessibility"
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
    public void AccessibilityPlatformBoundaryCoversNativeAccessibilityApis()
    {
        string testText = File.ReadAllText(FindRepositoryPath("tests", "Cerneala.Tests", "UI", "Rendering", "ArchitectureBoundaryTests.cs"));
        int methodStart = testText.IndexOf("public void UiPlatformAbstractionsDoNotReferenceConcreteBackends()", StringComparison.Ordinal);
        Assert.NotEqual(-1, methodStart);
        int nextFact = testText.IndexOf("    [Fact]", methodStart + 1, StringComparison.Ordinal);
        Assert.NotEqual(-1, nextFact);
        string methodText = testText[methodStart..nextFact];

        Assert.Contains("\"System.Windows.Automation\"", methodText, StringComparison.Ordinal);
        Assert.Contains("\"NSAccessibility\"", methodText, StringComparison.Ordinal);
    }

    [Fact]
    public void UiAccessibilityDoesNotReferenceConcreteBackendsOrNativeAccessibilityApis()
    {
        string accessibilityRoot = FindRepositoryPath("UI", "Accessibility");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna",
            "System.Windows.Automation",
            "Windows.UI",
            "Microsoft.UI",
            "NSAccessibility"
        ];

        foreach (string file in Directory.EnumerateFiles(accessibilityRoot, "*.cs", SearchOption.AllDirectories))
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
                text.Contains("Microsoft.Xna", StringComparison.Ordinal))
            {
                Assert.StartsWith(monoGameResourcesRoot, file, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void UiDrawingDoesNotReferenceRetainedRendering()
    {
        string drawingRoot = FindRepositoryPath("Drawing");

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
    public void Section17ControlsAndVirtualizationDoNotReferenceConcreteBackends()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "UI", "Controls", "ItemsControl.cs"),
            Path.Combine(root, "UI", "Controls", "Items", "ItemCollection.cs"),
            Path.Combine(root, "UI", "Controls", "Items", "ItemContainerGenerator.cs"),
            Path.Combine(root, "UI", "Controls", "Items", "ItemContainerRecyclePool.cs"),
            Path.Combine(root, "UI", "Controls", "ItemsPresenter.cs"),
            Path.Combine(root, "UI", "Controls", "Selection", "SelectionModel.cs"),
            Path.Combine(root, "UI", "Controls", "Selection", "SelectionModel{T}.cs"),
            Path.Combine(root, "UI", "Controls", "Primitives", "Selector.cs"),
            Path.Combine(root, "UI", "Controls", "ListBox.cs"),
            Path.Combine(root, "UI", "Controls", "ListBoxItem.cs"),
            Path.Combine(root, "UI", "Controls", "ComboBox.cs"),
            Path.Combine(root, "UI", "Controls", "TabControl.cs"),
            Path.Combine(root, "UI", "Controls", "TabItem.cs"),
            Path.Combine(root, "UI", "Layout", "Panels", "VirtualizingStackPanel.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "IItemsVirtualizingPanel.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "ItemsVirtualizationViewport.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "VirtualizationContext.cs"),
            Path.Combine(root, "UI", "Layout", "Virtualization", "RealizationWindow.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna"
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
    public void Section18DataApisDoNotReferenceConcreteBackends()
    {
        string dataRoot = FindRepositoryPath("UI", "Data");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna"
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
    public void RuntimeTestsDoNotDependOnActiveLegacySpecChanges()
    {
        string testsRoot = FindRepositoryPath("tests", "Cerneala.Tests");
        string legacySpecSegment = "open" + "spec";
        string changesSegment = "chang" + "es";
        string[] forbiddenPatterns =
        [
            string.Join("\"" + ", " + "\"", legacySpecSegment, changesSegment),
            string.Join("/", legacySpecSegment, changesSegment),
            string.Join("\\", legacySpecSegment, changesSegment)
        ];
        string[] testDependencyExtensions =
        [
            ".cs",
            ".csproj",
            ".props",
            ".targets",
            ".runsettings"
        ];

        foreach (string file in Directory.EnumerateFiles(testsRoot, "*", SearchOption.AllDirectories)
            .Where(file => testDependencyExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !HasPathSegment(file, "bin") && !HasPathSegment(file, "obj")))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenPattern in forbiddenPatterns)
            {
                Assert.DoesNotContain(forbiddenPattern, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void RuntimeLegacySpecDependencyBoundaryCoversProjectLevelTestFiles()
    {
        string testText = File.ReadAllText(FindRepositoryPath("tests", "Cerneala.Tests", "UI", "Rendering", "ArchitectureBoundaryTests.cs"));
        int methodStart = testText.IndexOf("public void RuntimeTestsDoNotDependOnActiveLegacySpecChanges()", StringComparison.Ordinal);
        Assert.NotEqual(-1, methodStart);
        int nextFact = testText.IndexOf("    [Fact]", methodStart + 1, StringComparison.Ordinal);
        Assert.NotEqual(-1, nextFact);
        string methodText = testText[methodStart..nextFact];

        Assert.Contains(".csproj", methodText, StringComparison.Ordinal);
        Assert.Contains(".props", methodText, StringComparison.Ordinal);
        Assert.Contains(".targets", methodText, StringComparison.Ordinal);
        Assert.Contains(".runsettings", methodText, StringComparison.Ordinal);
    }

    [Fact]
    public void Section20TextEditingApisDoNotReferenceConcreteBackendsOrNativeTextApis()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "UI", "Controls", "TextBox.cs"),
            Path.Combine(root, "UI", "Controls", "PasswordBox.cs"),
            Path.Combine(root, "UI", "Controls", "ITextInputHost.cs"),
            Path.Combine(root, "UI", "Controls", "TextInputCore.cs"),
            Path.Combine(root, "UI", "Controls", "TextInputPolicy.cs"),
            Path.Combine(root, "UI", "Controls", "TextInputViewport.cs"),
            Path.Combine(root, "UI", "Text", "TextDocument.cs"),
            Path.Combine(root, "UI", "Text", "TextCaret.cs"),
            Path.Combine(root, "UI", "Text", "TextSelection.cs"),
            Path.Combine(root, "UI", "Text", "TextEditor.cs"),
            Path.Combine(root, "UI", "Text", "TextEditorSnapshot.cs"),
            Path.Combine(root, "UI", "Text", "TextEditingController.cs"),
            Path.Combine(root, "UI", "Text", "TextCompositionManager.cs"),
            Path.Combine(root, "UI", "Text", "TextCompositionState.cs"),
            Path.Combine(root, "UI", "Text", "UndoRedoStack.cs"),
            Path.Combine(root, "UI", "Platform", "ITextInputPlatform.cs")
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "Microsoft.Xna",
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

    private static bool HasPathSegment(string path, string segment)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains(segment, StringComparer.OrdinalIgnoreCase);
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
