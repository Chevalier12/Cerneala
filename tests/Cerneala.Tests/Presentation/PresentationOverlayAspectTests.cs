using Cerneala.Presentation;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;
using Cerneala.Tests.UI.Hosting;

namespace Cerneala.Tests.Presentation;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PresentationOverlayAspectTests : IDisposable
{
    public PresentationOverlayAspectTests()
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
    public void ComboBoxDropDownScrollViewerUsesPresentationAspect()
    {
        UIRoot root = new(1650, 1004);
        root.SetResourceProvider(Application.Current!.Resources);
        PrismChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        view.AddLayerForTests();
        root.ProcessFrame();
        ComboBox comboBox = Assert.Single(Descendants(view).OfType<ComboBox>());
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        root.ProcessFrame();

        ScrollViewer scrollViewer = GetDropDownScrollViewer(comboBox);
        Assert.True(scrollViewer.IsVerticalScrollBarVisible);
        AssertUsesPresentationScrollAspect(scrollViewer);
    }

    [Fact]
    public void AspectStudioComboBoxDropDownScrollViewerUsesPresentationAspect()
    {
        UIRoot root = new(1650, 1004);
        root.SetResourceProvider(Application.Current!.Resources);
        AspectChapterView view = new();
        root.VisualChildren.Add(view);
        root.ProcessFrame();
        root.ProcessFrame();

        ComboBox comboBox = Descendants(view).OfType<ComboBox>().First();
        comboBox.IsDropDownOpen = true;
        root.ProcessFrame();
        root.ProcessFrame();

        AssertUsesPresentationScrollAspect(GetDropDownScrollViewer(comboBox));
    }

    private static ScrollViewer GetDropDownScrollViewer(ComboBox comboBox)
    {
        Overlay overlay = Assert.IsType<Overlay>(
            comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"]);
        Border border = Assert.IsType<Border>(overlay.Content);
        return Assert.IsType<ScrollViewer>(border.Child);
    }

    private static void AssertUsesPresentationScrollAspect(ScrollViewer scrollViewer)
    {
        ScrollBar scrollBar = scrollViewer.VerticalScrollBar;
        Assert.NotSame(ScrollViewerTemplates.Default, scrollViewer.ComponentTemplate);
        Assert.NotSame(ScrollBarTemplates.Default, scrollBar.ComponentTemplate);
        Assert.IsType<Track>(scrollBar.ComponentTemplateInstance!.Root);
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
