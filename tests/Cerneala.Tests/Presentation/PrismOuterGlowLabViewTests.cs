using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Presentation;
using Cerneala.UI;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windows;
using Cerneala.Tests.UI.Hosting;

namespace Cerneala.Tests.Presentation;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PrismOuterGlowLabViewTests : IDisposable
{
    public PrismOuterGlowLabViewTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
        WindowApplicationRuntime runtime = WindowApplicationRuntime.CurrentOrDefault;
        App app = new();
        app.Install(runtime);
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void ViewOwnsTheOuterGlowAttachmentWithoutThePrismChapter()
    {
        UIRoot root = new(1280, 800);
        PrismOuterGlowLabView lab = new();
        root.VisualChildren.Add(lab);
        root.ProcessFrame();

        Assert.Null(lab.Instance);

        var instance = lab.AttachOuterGlow();
        root.ProcessFrame();

        Assert.Same(instance, lab.Instance);
        Assert.Equal(
            PrismStyleId.OuterGlow,
            Assert.Single(instance.GetLayerState(PrismOuterGlowLabView.LayerId).Styles).Style);
        Assert.Contains(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.BeginPrism);

        lab.ResetPrism();
        root.ProcessFrame();

        Assert.Null(lab.Instance);
        Assert.DoesNotContain(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.BeginPrism);
    }

    [Fact]
    public void ViewCanIsolateMotionBlurWithoutThePrismChapter()
    {
        UIRoot root = new(1280, 800);
        PrismOuterGlowLabView lab = new();
        root.VisualChildren.Add(lab);
        root.ProcessFrame();

        var instance = lab.AttachMotionBlur();
        root.ProcessFrame();

        Assert.Same(instance, lab.Instance);
        Assert.Equal(
            PrismFilterId.MotionBlur,
            Assert.Single(instance.GetLayerState(PrismOuterGlowLabView.LayerId).Filters).Filter);
        Assert.Contains(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.BeginPrism);

        lab.ResetPrism();
        root.ProcessFrame();

        Assert.Null(lab.Instance);
        Assert.DoesNotContain(
            root.RetainedRenderer.Commit(root),
            command => command.Kind == DrawCommandKind.BeginPrism);
    }
}
