using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.Playground.Samples;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Playground.Samples;

public sealed class ModernAspectSampleTests
{
    [Fact]
    public void ModernAspectSampleRegistersPackage()
    {
        AspectPackage package = ModernAspectSample.CreatePackage();
        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();

        Assert.Equal("Playground.ModernAspect", package.Name);
        Assert.NotEmpty(catalog.Rules);
        Assert.Contains(catalog.ComponentTemplates, template => template.Template == ButtonTemplates.Modern);
        Assert.True(catalog.TryGetTokenDefault(ModernAspectSample.LiveAccentToken, out _));
    }

    [Fact]
    public void ModernAspectSampleShowsVariantsStatesTokensAndSlots()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Playground", "Cerneala.Playground", "Samples", "ModernAspectSample.cs"));

        Assert.Contains("DefaultAspectTokens.Color.Accent", source, StringComparison.Ordinal);
        Assert.Contains("ButtonVariants.Kind", source, StringComparison.Ordinal);
        Assert.Contains("AspectState.Hover", source, StringComparison.Ordinal);
        Assert.Contains("AspectState.Pressed", source, StringComparison.Ordinal);
        Assert.Contains("AspectState.Focus", source, StringComparison.Ordinal);
        Assert.Contains("ButtonSlots.Content", source, StringComparison.Ordinal);
        Assert.Contains("ContentTemplateRegistry", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ModernAspectSampleDoesNotUseLegacyRuleSheet()
    {
        string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Playground", "Cerneala.Playground", "Samples", "ModernAspectSample.cs"));
        UIElement root = new ModernAspectSample().Build();

        Assert.DoesNotContain("StyleSheet", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StyleRule", source, StringComparison.Ordinal);
        Assert.Contains(DescendantsAndSelf<Button>(root), _ => true);
        Assert.Contains(DescendantsAndSelf<ContentPresenter>(root), _ => true);
    }

    [Fact]
    public void ModernAspectSelectionUsesProvidedPlaygroundFont()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        SampleSelector selector = SampleSelector.CreateDefault(resources, fontId);
        root.VisualChildren.Add(selector.Root);
        selector.SelectSample(selector.Samples.ToList().FindIndex(sample => sample.Name == "Modern Aspect"));
        selector.Root.Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.Subtree,
            "Initial modern aspect sample test frame");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.NotEmpty(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.IsType<SkiaFont>(command.TextRun!.Font));
    }

    [Fact]
    public void ModernAspectSampleAppliesPackageVariantAndShowsDemoSections()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        root.SetResourceProvider(resources);
        root.AspectRegistry.Register(ModernAspectSample.CreatePackage());
        UIElement sampleRoot = new ModernAspectSample(resources, fontId).Build();
        root.VisualChildren.Add(sampleRoot);

        root.ProcessFrame();

        Button primary = DescendantsAndSelf<Button>(sampleRoot)
            .Single(button => button.Content is string content && content.Contains("Primary", StringComparison.Ordinal));
        Assert.Equal(new DrawColor(79, 70, 229), primary.Background);
        Border primaryChrome = Assert.IsType<Border>(primary.ComponentTemplateInstance!.Root);
        Assert.Equal(new DrawColor(79, 70, 229), primaryChrome.Background);
        Assert.Contains(DescendantsAndSelf<Button>(sampleRoot), button => button.Content is string content && content.Contains("Danger", StringComparison.Ordinal));
        Assert.Contains(DescendantsAndSelf<Button>(sampleRoot), button => button.Content is string content && content.Contains("Small", StringComparison.Ordinal));
        Assert.Contains(DescendantsAndSelf<TextBlock>(sampleRoot), block => block.Text.Contains("Hover", StringComparison.Ordinal));
        Assert.Contains(DescendantsAndSelf<TextBlock>(sampleRoot), block => block.Text.Contains("ContentTemplateRegistry", StringComparison.Ordinal));
        Assert.True(DescendantsAndSelf<ContentPresenter>(sampleRoot).Count() >= 2);
    }

    [Fact]
    public void ModernAspectSampleButtonsReactToClickHoverAndPressedState()
    {
        ResourceStore resources = new();
        ResourceId<FontResource> fontId = new("Playground/Body");
        resources.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", 16)));
        UIRoot root = new(800, 600);
        root.SetResourceProvider(resources);
        root.AspectRegistry.Register(ModernAspectSample.CreatePackage());
        UIElement sampleRoot = new ModernAspectSample(resources, fontId).Build();
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();

        Button primary = DescendantsAndSelf<Button>(sampleRoot)
            .Single(button => button.Content is string content && content.Contains("Primary", StringComparison.Ordinal));
        TextBlock status = DescendantsAndSelf<TextBlock>(sampleRoot)
            .Single(block => block.Text.Contains("Button action:", StringComparison.Ordinal));

        primary.Command!.Execute(primary.CommandParameter);

        Assert.Contains("Primary token-backed chrome", status.Text, StringComparison.Ordinal);

        primary.IsPointerOver = true;
        root.ProcessFrame();
        Border chrome = Assert.IsType<Border>(primary.ComponentTemplateInstance!.Root);
        Assert.Equal(new DrawColor(79, 70, 229), chrome.BorderColor);
        Assert.Equal(new Thickness(1), chrome.BorderThickness);

        primary.IsPressed = true;
        root.ProcessFrame();
        Assert.Equal(new DrawColor(67, 56, 202), chrome.Background);
    }

    private static IEnumerable<T> DescendantsAndSelf<T>(UIElement element)
        where T : UIElement
    {
        if (element is T match)
        {
            yield return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            foreach (T descendant in DescendantsAndSelf<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateFiles("Cerneala.slnx").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
