using Cerneala.UI.Accessibility;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.UI.Platform;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void PlatformServicesCanBePartiallyAvailable()
    {
        PlatformServices services = PlatformServices.Empty;

        Assert.Null(services.Clipboard);
        Assert.Null(services.Cursor);
        Assert.Null(services.FileDialogs);
        Assert.Null(services.TextInput);
        Assert.Null(services.Dpi);
        Assert.Null(services.Accessibility);
    }

    [Fact]
    public void PlatformServicesExposeKnownSeams()
    {
        FakeClipboard clipboard = new();
        FakeCursor cursor = new();
        FakeFileDialogs fileDialogs = new();
        FakeTextInput textInput = new(clipboard);
        FakeDpi dpi = new();
        FakeAccessibility accessibility = new();

        IPlatformServices services = new PlatformServices(
            clipboard,
            cursor,
            fileDialogs,
            textInput,
            dpi,
            accessibility);

        Assert.Same(clipboard, services.Clipboard);
        Assert.Same(cursor, services.Cursor);
        Assert.Same(fileDialogs, services.FileDialogs);
        Assert.Same(textInput, services.TextInput);
        Assert.Same(clipboard, services.TextInput!.Clipboard);
        Assert.Same(dpi, services.Dpi);
        Assert.Same(accessibility, services.Accessibility);
    }

    [Fact]
    public void FileDialogOptionsSnapshotFilters()
    {
        List<FileDialogFilter> filters =
        [
            new("Images", ["png"])
        ];

        FileDialogOptions options = new(Filters: filters);

        filters.Add(new("Text", ["txt"]));

        FileDialogFilter filter = Assert.Single(options.Filters!);
        Assert.Equal("Images", filter.Name);
    }

    [Fact]
    public void FileDialogFilterSnapshotsExtensions()
    {
        List<string> extensions = ["png"];

        FileDialogFilter filter = new("Images", extensions);

        extensions.Add("jpg");

        string extension = Assert.Single(filter.Extensions);
        Assert.Equal("png", extension);
    }

    [Fact]
    public void FileDialogFilterRejectsMissingRequiredValues()
    {
        Assert.ThrowsAny<ArgumentException>(() => new FileDialogFilter(null!, ["png"]));
        Assert.Throws<ArgumentNullException>(() => new FileDialogFilter("Images", null!));
    }

    private sealed class FakeClipboard : IClipboard
    {
        private string? text;

        public bool HasText => text is not null;

        public string? GetText() => text;

        public void SetText(string text) => this.text = text;
    }

    private sealed class FakeCursor : ICursorService
    {
        public CursorShape Current { get; private set; }

        public void SetCursor(CursorShape shape) => Current = shape;
    }

    private sealed class FakeFileDialogs : IFileDialogService
    {
        public string? OpenFile(FileDialogOptions options) => null;

        public string? SaveFile(FileDialogOptions options) => null;
    }

    private sealed class FakeTextInput : ITextInputPlatform
    {
        public FakeTextInput(IClipboard? clipboard = null)
        {
            Clipboard = clipboard;
        }

        public IClipboard? Clipboard { get; }

        public bool SupportsIme => false;
    }

    private sealed class FakeDpi : IDpiProvider
    {
        public float Scale => 1;

        public float DpiX => 96;

        public float DpiY => 96;
    }

    private sealed class FakeAccessibility : IAccessibilityPlatform
    {
        public void Publish(SemanticsTree tree)
        {
        }
    }
}
