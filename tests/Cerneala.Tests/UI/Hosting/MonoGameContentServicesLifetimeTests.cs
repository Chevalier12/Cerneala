using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Resources;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.UI.Hosting;

public sealed class MonoGameContentServicesLifetimeTests
{
    [Fact]
    public void MonoGameContentServicesDisposesOwnedImageCache()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage image = new(16, 8);
        loader.SetImage("logo.png", image);
        MonoGameContentServices services = CreateContentServices(loader);
        ImageResourceCache cache = GetRequiredImageCache(services);
        cache.Resolve(new ImageResource("logo.png"));

        IDisposable disposable = Assert.IsAssignableFrom<IDisposable>(services);
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public void MonoGameUiHostDisposeDisposesContentOwnedResourcesOnce()
    {
        RecordingImageLoader loader = new();
        DisposableTestImage image = new(16, 8);
        loader.SetImage("logo.png", image);
        MonoGameContentServices services = CreateContentServices(loader);
        GetRequiredImageCache(services).Resolve(new ImageResource("logo.png"));
        MonoGameUiHost host = CreateHostWithoutGpu(new UIRoot(), services);

        host.Dispose();
        host.Dispose();

        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public void ReplacingRootReattachesContentServicesToNewRoot()
    {
        RecordingImageLoader loader = new();
        MonoGameContentServices services = CreateContentServices(loader);
        IImageLoader expectedLoader = GetRequiredImageLoader(services);
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        MonoGameUiHost host = CreateHostWithoutGpu(firstRoot, services);

        Assert.Same(expectedLoader, firstRoot.ImageLoader);
        Assert.NotNull(firstRoot.ImageResourceCache);

        host.SetRoot(secondRoot);

        Assert.Same(expectedLoader, secondRoot.ImageLoader);
        Assert.NotNull(secondRoot.ImageResourceCache);
    }

    private static MonoGameUiHost CreateHostWithoutGpu(UIRoot root, MonoGameContentServices services)
    {
        return new MonoGameUiHost(new MonoGameUiHostOptions
        {
            SpriteBatch = CreateUninitialized<SpriteBatch>(),
            WhitePixel = CreateUninitialized<Texture2D>(),
            Root = root,
            Viewport = new UiViewport(100, 100),
            ContentServices = services
        });
    }

    private static MonoGameContentServices CreateContentServices(IImageLoader loader)
    {
        foreach (ConstructorInfo constructor in typeof(MonoGameContentServices).GetConstructors())
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (!parameters.Any(parameter => parameter.ParameterType == typeof(IImageLoader)))
            {
                continue;
            }

            object?[] arguments = parameters.Select(parameter =>
            {
                if (parameter.ParameterType == typeof(IImageLoader))
                {
                    return loader;
                }

                return parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }).ToArray();

            return (MonoGameContentServices)constructor.Invoke(arguments);
        }

        MonoGameContentServices services = new();
        TrySetImageLoader(services, loader);
        return services;
    }

    private static void TrySetImageLoader(MonoGameContentServices services, IImageLoader loader)
    {
        PropertyInfo? property = typeof(MonoGameContentServices).GetProperty(
            "ImageLoader",
            BindingFlags.Instance | BindingFlags.Public);

        if (property?.CanWrite == true && property.PropertyType.IsAssignableFrom(typeof(IImageLoader)))
        {
            property.SetValue(services, loader);
        }
    }

    private static IImageLoader GetRequiredImageLoader(MonoGameContentServices services)
    {
        PropertyInfo? property = typeof(MonoGameContentServices).GetProperty(
            "ImageLoader",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.True(property is not null, "MonoGameContentServices must expose a public ImageLoader property.");
        Assert.True(
            typeof(IImageLoader).IsAssignableFrom(property.PropertyType),
            "MonoGameContentServices.ImageLoader must expose the backend-neutral IImageLoader abstraction.");

        return Assert.IsAssignableFrom<IImageLoader>(property.GetValue(services));
    }

    private static ImageResourceCache GetRequiredImageCache(MonoGameContentServices services)
    {
        PropertyInfo? property = typeof(MonoGameContentServices).GetProperty(
            "ImageResourceCache",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.True(property is not null, "MonoGameContentServices must expose a public ImageResourceCache property.");
        Assert.True(
            typeof(ImageResourceCache).IsAssignableFrom(property.PropertyType),
            "MonoGameContentServices.ImageResourceCache must expose the backend-neutral image cache.");

        return Assert.IsType<ImageResourceCache>(property.GetValue(services));
    }

    private static T CreateUninitialized<T>()
        where T : class
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public IDrawImage Load(string path)
        {
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }
    }

    private sealed class DisposableTestImage(int width, int height) : IDrawImage, IDisposable
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
