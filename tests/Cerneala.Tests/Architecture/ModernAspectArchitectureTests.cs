using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Architecture;

public sealed class ModernAspectArchitectureTests
{
    [Fact]
    public void DefaultThemeDoesNotCreateLegacyRuleSheetForRuntimePath()
    {
        UIRoot root = new();
        Button button = new();
        root.VisualChildren.Add(button);

        root.ProcessFrame();

        Assert.Contains(root.AspectRegistry.Packages, package => package.Name == "Default");
        Assert.Equal(new Color(255, 255, 255), button.Background);
    }

    [Fact]
    public void RootExposesAspectProcessorAsCanonicalProperty()
    {
        UIRoot root = new();

        Assert.NotNull(root.AspectProcessor);
    }

    [Fact]
    public void LegacyStyleRuntimeSurfaceIsRemoved()
    {
        MemberInfo[] rootMembers = typeof(UIRoot)
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(member => member.DeclaringType == typeof(UIRoot))
            .ToArray();

        Assert.DoesNotContain(rootMembers, member => member.Name.Contains("StyleSheet", StringComparison.Ordinal));
        Assert.DoesNotContain(rootMembers, member => member.Name.Contains("StyleProcessor", StringComparison.Ordinal));
        Assert.DoesNotContain(rootMembers, member => member.Name.Contains("legacyStyle", StringComparison.OrdinalIgnoreCase));

        AssertNoProductionReferences("SetStyleSheet");
        AssertNoProductionReferences("StyleSheet");
        AssertNoProductionReferences("StyleRule");
        AssertNoProductionReferences("StyleApplicator");
        AssertNoProductionReferences("StyleProcessor");
        AssertNoProductionReferences("StyleMotion");
        AssertNoProductionReferences("PseudoClass");
        AssertNoProductionReferences("VisualStateRule");
        AssertNoProductionReferences("StyleQueue");
        AssertNoProductionReferences("AffectsStyle");
        AssertNoProductionReferences("FramePhase.Style");
        AssertNoProductionReferences("InvalidationFlags.Style");
        AssertNoProductionReferences("UiPropertyValueSource.StyleBase");
        AssertNoProductionReferences("UiPropertyValueSource.StyleVisualState");
        AssertNoProductionReferences("TextRunStyle");
    }

    [Fact]
    public void AspectValueSourcesHaveDocumentedPrecedence()
    {
        Assert.True(UiPropertyValueSource.Local > UiPropertyValueSource.Animation);
        Assert.True(UiPropertyValueSource.Animation > UiPropertyValueSource.LocalAspectConditional);
        Assert.True(UiPropertyValueSource.LocalAspectConditional > UiPropertyValueSource.LocalAspectBase);
        Assert.True(UiPropertyValueSource.LocalAspectBase > UiPropertyValueSource.AspectVisualState);
        Assert.True(UiPropertyValueSource.Animation > UiPropertyValueSource.AspectVisualState);
        Assert.True(UiPropertyValueSource.AspectVisualState > UiPropertyValueSource.AspectBase);
        Assert.True(UiPropertyValueSource.AspectBase > UiPropertyValueSource.TemplateBinding);
        Assert.True(UiPropertyValueSource.TemplateBinding > UiPropertyValueSource.Inherited);
        Assert.True(UiPropertyValueSource.Inherited > UiPropertyValueSource.Default);
    }

    [Fact]
    public void RemovedTemplateTypesStayRemoved()
    {
        string[] removedTypeNames =
        [
            "Control" + "Template",
            "Data" + "Template",
            "Template" + "Context",
            "Template" + "Instance",
            "Control" + "TemplateAdapter",
            "Data" + "TemplateAdapter"
        ];
        Type[] templateTypes = typeof(Control).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Cerneala.UI.Controls.Templates", StringComparison.Ordinal) == true)
            .ToArray();

        foreach (string removedTypeName in removedTypeNames)
        {
            Assert.DoesNotContain(templateTypes, type => string.Equals(type.Name.Split('`')[0], removedTypeName, StringComparison.Ordinal));
        }

    }

    [Fact]
    public void RemovedTemplateMembersStayRemoved()
    {
        Assert.Null(typeof(Control).GetProperty("Template"));
        Assert.Null(typeof(Control).GetProperty("Template" + "Instance"));
        Assert.Null(typeof(ContentPresenter).GetProperty("Modern" + "ContentTemplate"));
    }

    [Fact]
    public void NoObsoleteAspectOrTemplateTypesRemain()
    {
        Type[] publicTypes = typeof(AspectToken).Assembly.GetTypes()
            .Where(type =>
                (type.Namespace?.StartsWith("Cerneala.UI.Aspect", StringComparison.Ordinal) == true ||
                 type.Namespace?.StartsWith("Cerneala.UI.Controls.Templates", StringComparison.Ordinal) == true) &&
                type.IsPublic)
            .ToArray();

        Assert.DoesNotContain(publicTypes, type => type.GetCustomAttribute<ObsoleteAttribute>() is not null);
        MemberInfo[] declaredMembers = publicTypes
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .ToArray();

        Assert.DoesNotContain(declaredMembers, member => member.GetCustomAttribute<ObsoleteAttribute>() is not null);
    }

    private static void AssertNoProductionReferences(string symbolName)
    {
        Assert.Empty(FindProductionReferences(symbolName).Order(StringComparer.Ordinal));
    }

    private static string[] FindProductionReferences(string symbolName)
    {
        string root = FindRepositoryRoot();
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(symbolName, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
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
