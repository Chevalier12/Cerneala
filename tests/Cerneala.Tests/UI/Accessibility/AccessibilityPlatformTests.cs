using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class AccessibilityPlatformTests
{
    [Fact]
    public void PlatformBoundaryReceivesSemanticsTreeSnapshot()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new Button { Content = "Publish" });
        SemanticsTree tree = new SemanticsProvider().Build(root);
        RecordingAccessibilityPlatform platform = new();

        platform.Publish(tree);

        Assert.Same(tree, platform.PublishedTree);
        SemanticsNode button = Assert.Single(platform.PublishedTree!.Root.Children);
        Assert.Equal(SemanticsRole.Button, button.Role);
        Assert.Equal("Publish", button.Name);
    }

    private sealed class RecordingAccessibilityPlatform : IAccessibilityPlatform
    {
        public SemanticsTree? PublishedTree { get; private set; }

        public void Publish(SemanticsTree tree)
        {
            PublishedTree = tree;
        }
    }
}
