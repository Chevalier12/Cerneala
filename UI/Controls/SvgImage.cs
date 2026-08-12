using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public class SvgImage : Image
{
    public static readonly UiProperty<string?> SourcePathProperty = UiProperty<string?>.Register(
        nameof(SourcePath),
        typeof(SvgImage),
        new UiPropertyMetadata<string?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    private IDrawImage? loadedImage;

    public string? SourcePath
    {
        get => GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        ReloadSource();
    }

    protected override void OnDetached()
    {
        ReleaseLoadedImage();
        base.OnDetached();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SourcePathProperty) && Root is not null)
        {
            ReloadSource();
        }
    }

    private void ReloadSource()
    {
        ReleaseLoadedImage();
        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            return;
        }

        IImageLoader? loader = Root?.ImageLoader;
        if (loader is null)
        {
            return;
        }

        string resolvedPath = ResolvePath(SourcePath);
        using MemoryStream rasterized = new(SvgRasterizer.Rasterize(resolvedPath), writable: false);
        loadedImage = loader.Load(rasterized)
            ?? throw new InvalidOperationException("Image loader returned a null image for the rasterized SVG.");
        Source = loadedImage;
    }

    private void ReleaseLoadedImage()
    {
        IDrawImage? previous = loadedImage;
        loadedImage = null;
        if (ReferenceEquals(Source, previous))
        {
            Source = null;
        }

        if (previous is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return Path.GetFullPath(path);
        }

        string workingDirectoryPath = Path.GetFullPath(path);
        return File.Exists(workingDirectoryPath)
            ? workingDirectoryPath
            : Path.GetFullPath(path, AppContext.BaseDirectory);
    }
}
