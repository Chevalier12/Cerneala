using System.IO.Compression;
using System.Xml.Linq;

namespace Cerneala.Tests.VisualStudio;

public sealed class VisualStudioPackageTests
{
    private static readonly XNamespace Vsix = "http://schemas.microsoft.com/developer/vsx-schema/2011";

    [Fact]
    public void ManifestTargetsOnlyCommunity18AndDeclaresStableIdentity()
    {
        XDocument manifest = XDocument.Load(Path.Combine(ProjectDirectory(), "source.extension.vsixmanifest"));
        XElement identity = Assert.Single(manifest.Descendants(Vsix + "Identity"));
        Assert.Equal("Cerneala.Cerneala.VisualStudio", (string?)identity.Attribute("Id"));
        Assert.Equal("Cerneala", (string?)identity.Attribute("Publisher"));
        Assert.Equal("|%CurrentProject%;GetVsixVersion|", (string?)identity.Attribute("Version"));

        XElement target = Assert.Single(manifest.Descendants(Vsix + "InstallationTarget"));
        Assert.Equal("Microsoft.VisualStudio.Community", (string?)target.Attribute("Id"));
        Assert.Equal("[18.0,19.0)", (string?)target.Attribute("Version"));
        Assert.Equal("amd64", target.Element(Vsix + "ProductArchitecture")?.Value);

        XElement prerequisite = Assert.Single(manifest.Descendants(Vsix + "Prerequisite"));
        Assert.Equal("Microsoft.VisualStudio.Component.CoreEditor", (string?)prerequisite.Attribute("Id"));
        Assert.Equal("[18.0,19.0)", (string?)prerequisite.Attribute("Version"));
    }

    [Fact]
    public void VsixContainsHostServerGrammarConfigurationAndAssets()
    {
        string expectedVersion = ProjectVersion();
        string vsixPath = FindVsix();
        using ZipArchive package = ZipFile.OpenRead(vsixPath);
        HashSet<string> entries = package.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Cerneala.VisualStudio.dll", entries);
        Assert.Contains("Cerneala.pkgdef", entries);
        Assert.Contains("Cerneala.VisualStudio.pkgdef", entries);
        Assert.Contains("Grammars/cerneala.tmLanguage.json", entries);
        Assert.Contains("language-configuration.json", entries);
        Assert.Contains("Assets/cerneala.png", entries);
        Assert.Contains("LICENSE", entries);
        Assert.Contains("THIRD-PARTY-NOTICES.txt", entries);
        Assert.Contains(entries, entry => entry.Equals(
            $"Server/{expectedVersion}/Cerneala.LanguageServer.exe",
            StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Equals(
            $"Server/{expectedVersion}/coreclr.dll",
            StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Equals(
            $"PreviewHost/{expectedVersion}/Cerneala.PreviewHost.exe",
            StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Equals(
            $"PreviewHost/{expectedVersion}/coreclr.dll",
            StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, entry => entry.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));

        ZipArchiveEntry manifestEntry = Assert.Single(package.Entries.Where(entry => entry.FullName.Equals(
            "extension.vsixmanifest",
            StringComparison.OrdinalIgnoreCase)));
        using Stream manifestStream = manifestEntry.Open();
        XDocument packagedManifest = XDocument.Load(manifestStream);
        XElement packagedIdentity = Assert.Single(packagedManifest.Descendants(Vsix + "Identity"));
        Assert.Equal(expectedVersion, (string?)packagedIdentity.Attribute("Version"));
    }

    [Fact]
    public void CrnRegistrationDoesNotClaimXmlOrCompoundExtensions()
    {
        string registration = File.ReadAllText(Path.Combine(ProjectDirectory(), "Cerneala.pkgdef"));
        Assert.Contains("\"crn\"=dword:00000064", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("\"xml\"=", registration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cui.xml", registration, StringComparison.OrdinalIgnoreCase);

        string contentType = File.ReadAllText(Path.Combine(ProjectDirectory(), "CernealaContentType.cs"));
        Assert.Contains("[FileExtension(\".crn\")]", contentType, StringComparison.Ordinal);
        Assert.DoesNotContain(".xml", contentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PackageRegistrationIsGeneratedWithoutAnAutoLoadRule()
    {
        string vsixPath = FindVsix();
        using ZipArchive package = ZipFile.OpenRead(vsixPath);
        ZipArchiveEntry registration = Assert.Single(package.Entries.Where(entry => entry.FullName.Equals(
            "Cerneala.VisualStudio.pkgdef",
            StringComparison.OrdinalIgnoreCase)));
        using StreamReader reader = new(registration.Open());
        string content = reader.ReadToEnd();

        Assert.Contains("f7d79e1c-8074-46ec-80ca-79347f6d896a", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cerneala.VisualStudio.dll", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AutoLoadPackages", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LivePreviewUsesWpfStyleDesignerSurfaceAroundTheStandardEditor()
    {
        string provider = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewMarginProvider.cs"));
        string margin = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewMargin.cs"));
        string modeBar = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewModeBar.cs"));
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));

        Assert.Contains(": IWpfTextViewMarginProvider", provider, StringComparison.Ordinal);
        Assert.Contains("[ContentType(CernealaContentType.Name)]", provider, StringComparison.Ordinal);
        Assert.Contains("[TextViewRole(PredefinedTextViewRoles.Document)]", provider, StringComparison.Ordinal);
        Assert.Contains("[MarginContainer(PredefinedMarginNames.Top)]", provider, StringComparison.Ordinal);
        Assert.Contains("[MarginContainer(PredefinedMarginNames.Left)]", provider, StringComparison.Ordinal);
        Assert.Contains("[MarginContainer(PredefinedMarginNames.Bottom)]", provider, StringComparison.Ordinal);
        Assert.Contains("GetOrCreateSingletonProperty", provider, StringComparison.Ordinal);
        Assert.Contains("BottomMarginName", margin, StringComparison.Ordinal);
        Assert.Contains("new CernealaPreviewModeBar", margin, StringComparison.Ordinal);
        Assert.Contains("new CernealaPreviewSurface", margin, StringComparison.Ordinal);
        Assert.Contains("PreviewViewMode.Design", modeBar, StringComparison.Ordinal);
        Assert.Contains("PreviewViewMode.Split", modeBar, StringComparison.Ordinal);
        Assert.Contains("PreviewViewMode.Code", modeBar, StringComparison.Ordinal);
        Assert.Contains("PreviewSplitOrientation.Horizontal", modeBar, StringComparison.Ordinal);
        Assert.Contains("PreviewSplitOrientation.Vertical", modeBar, StringComparison.Ordinal);
        Assert.Contains("GridSplitter", surface, StringComparison.Ordinal);
        Assert.Contains("CreateViewportHandle", surface, StringComparison.Ordinal);
        Assert.Contains("ViewportResizeAxis.Width", surface, StringComparison.Ordinal);
        Assert.Contains("ViewportResizeAxis.Height", surface, StringComparison.Ordinal);
        Assert.Contains("ViewportResizeAxis.Both", surface, StringComparison.Ordinal);
        Assert.Contains("Key.Space", surface, StringComparison.Ordinal);
        Assert.Contains("MouseButton.Middle", surface, StringComparison.Ordinal);
        Assert.Contains("TranslatePoint", surface, StringComparison.Ordinal);
        Assert.Contains("12.5%", surface, StringComparison.Ordinal);
        Assert.Contains("800%", surface, StringComparison.Ordinal);
        Assert.Contains("Show actual size", surface, StringComparison.Ordinal);
        Assert.Contains("Fit all", surface, StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewViewportResizeHandlesUseWpfDesignerChrome()
    {
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));

        Assert.Contains("CreateWpfResizeHandleTemplate", surface, StringComparison.Ordinal);
        Assert.Contains("Background = Brushes.Transparent", surface, StringComparison.Ordinal);
        Assert.Contains("handle.Template = CreateWpfResizeHandleTemplate(axis)", surface, StringComparison.Ordinal);
        Assert.Contains("CreateHandleLine", surface, StringComparison.Ordinal);
        Assert.Contains("CreateHandleGrip", surface, StringComparison.Ordinal);
        Assert.Contains("Panel.SetZIndex(handle, 10)", surface, StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewDropdownsUseThePreviewToolbarChrome()
    {
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));
        string chrome = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewChrome.cs"));

        Assert.Contains("CernealaPreviewChrome.ConfigureComboBox(zoomInput", surface, StringComparison.Ordinal);
        Assert.Contains("CernealaPreviewChrome.ConfigureComboBox(refreshRateInput", surface, StringComparison.Ordinal);
        Assert.Contains("PART_EditableTextBox", chrome, StringComparison.Ordinal);
        Assert.Contains("PART_Popup", chrome, StringComparison.Ordinal);
        Assert.Contains("ComboBoxItem.ForegroundProperty, TextBrush", chrome, StringComparison.Ordinal);
        Assert.Contains("ComboBoxItem.BackgroundProperty, SurfaceBrush", chrome, StringComparison.Ordinal);
        Assert.DoesNotContain("Brushes.Black", chrome, StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewZoomSelectionCommitsTheSelectedValueImmediately()
    {
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));

        Assert.Contains(
            "CommitZoom(zoomInput.SelectedItem as string)",
            surface,
            StringComparison.Ordinal);
        Assert.Contains(
            "selectedValue ?? zoomInput.Text",
            surface,
            StringComparison.Ordinal);
        Assert.Contains(
            "CommitRefreshRate(refreshRateInput.SelectedItem as string)",
            surface,
            StringComparison.Ordinal);
        Assert.Contains(
            "selectedValue ?? refreshRateInput.Text",
            surface,
            StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewRefreshCapIsEditablePerSession()
    {
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));
        string session = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSession.cs"));

        Assert.Contains("refreshRateInput", surface, StringComparison.Ordinal);
        Assert.Contains("15", surface, StringComparison.Ordinal);
        Assert.Contains("30", surface, StringComparison.Ordinal);
        Assert.Contains("60", surface, StringComparison.Ordinal);
        Assert.Contains("120", surface, StringComparison.Ordinal);
        Assert.Contains("SetRefreshRateLimit", surface, StringComparison.Ordinal);
        Assert.Contains("RefreshRateLimit", session, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromMilliseconds(1000d / RefreshRateLimit)", session, StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewPacesTheNextFrameFromCaptureCompletion()
    {
        string session = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSession.cs"));

        Assert.Contains("DispatcherPriority.Render", session, StringComparison.Ordinal);
        Assert.Contains("animationFrameStartedTimestamp", session, StringComparison.Ordinal);
        Assert.Contains("ScheduleAnimationFrame", session, StringComparison.Ordinal);
        Assert.Contains("remainingTicks <= 0", session, StringComparison.Ordinal);
        Assert.Contains("Stopwatch.Frequency / RefreshRateLimit", session, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "private void OnAnimationTick(object? sender, EventArgs args) =>",
            session,
            StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewReusesItsWpfFrameSurface()
    {
        string session = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSession.cs"));

        Assert.Contains("WriteableBitmap? frameBuffer", session, StringComparison.Ordinal);
        Assert.Contains("frameBuffer.WritePixels", session, StringComparison.Ordinal);
        Assert.DoesNotContain("BitmapSource.Create", session, StringComparison.Ordinal);
        Assert.Contains(
            "if (updateStatus)",
            session,
            StringComparison.Ordinal);
    }

    [Fact]
    public void LivePreviewShowsACompilationLoadingOverlay()
    {
        string surface = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSurface.cs"));
        string session = File.ReadAllText(Path.Combine(
            ProjectDirectory(),
            "Preview",
            "CernealaPreviewSession.cs"));

        Assert.Contains("Loading...", surface, StringComparison.Ordinal);
        Assert.Contains("Might take a while (or not).", surface, StringComparison.Ordinal);
        Assert.Contains("session.IsLoading", surface, StringComparison.Ordinal);
        Assert.Contains("public bool IsLoading", session, StringComparison.Ordinal);
        Assert.Contains("BuildLoadingOverlay", surface, StringComparison.Ordinal);
        Assert.Contains("DoubleAnimation", surface, StringComparison.Ordinal);
        Assert.Contains("RepeatBehavior.Forever", surface, StringComparison.Ordinal);
        Assert.Contains("SetLoadingState(session.IsLoading)", surface, StringComparison.Ordinal);
    }

    private static string FindVsix()
    {
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string path = Path.Combine(
            ProjectDirectory(),
            "bin",
            configuration,
            "net472",
            "Cerneala.VisualStudio.vsix");
        Assert.True(File.Exists(path), $"Expected VSIX '{path}' was not built.");
        return path;
    }

    private static string ProjectDirectory() => Path.Combine(RepositoryRoot(), "Cerneala.VisualStudio");

    private static string ProjectVersion()
    {
        XDocument project = XDocument.Load(Path.Combine(ProjectDirectory(), "Cerneala.VisualStudio.csproj"));
        return Assert.Single(project.Descendants("Version")).Value;
    }

    internal static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
