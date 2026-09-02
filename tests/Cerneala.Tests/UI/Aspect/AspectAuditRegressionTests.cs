using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectAuditRegressionTests
{
    [Fact]
    public void RemovingTemporaryRuleRestoresFrameworkAspectDefault()
    {
        UIRoot root = new();
        TextBox textBox = new();
        root.VisualChildren.Add(textBox);
        root.AspectProcessor.Process(textBox);
        Thickness frameworkPadding = new(4, 2, 4, 2);
        Assert.Equal(frameworkPadding, textBox.Padding);

        AspectPackage temporary = AspectPackage.Create("temporary-padding")
            .Components(components => components.AddRule(new AspectRuleSet(
                "temporary-padding",
                AspectLayer.App,
                new AspectTarget(typeof(TextBox)),
                [new AspectDeclaration(Control.PaddingProperty, AspectValue<Thickness>.Literal(new Thickness(20)))],
                0)));

        root.AspectRegistry.Register(temporary);
        root.AspectProcessor.Process(textBox);
        Assert.Equal(new Thickness(20), textBox.Padding);

        Assert.True(root.AspectRegistry.Unregister("temporary-padding"));
        root.AspectProcessor.Process(textBox);

        Assert.Equal(frameworkPadding, textBox.Padding);
        Assert.Equal(UiPropertyValueSource.Default, textBox.GetValueSource(Control.PaddingProperty));
    }

    [Fact]
    public void RootProcessorRecomputesDynamicPropertyDependencyWithoutStaticAspectFlag()
    {
        UIRoot root = new();
        Border border = new();
        root.AspectRegistry.Register(AspectPackage.Create("property-dependency")
            .Components(components => components.AddRule(new AspectRuleSet(
                "opacity-condition",
                AspectLayer.App,
                new AspectTarget(
                    typeof(Border),
                    conditions: [AspectCondition.Property(UIElement.OpacityProperty).Is(0.5f)]),
                [new AspectDeclaration(UIElement.WidthProperty, AspectValue<float>.Literal(123f))],
                0))));
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        border.Opacity = 0.5f;

        Assert.True(root.AspectQueue.HasWork);
        var update = root.ProcessFrame();
        Assert.True(update.AspectElements > 0);
        Assert.Equal(123f, border.Width);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void ElementAspectBehaviorFollowsAttachDetachAndReattachLifecycle()
    {
        int attaches = 0;
        int disposes = 0;
        ElementAspect aspect = new(
            "lifecycle",
            typeof(Button),
            [],
            [],
            _ =>
            {
                attaches++;
                return new CallbackLifetime(() => disposes++);
            });
        Button button = new() { Aspect = aspect };
        UIRoot root = new();

        Assert.Equal(0, attaches);
        root.VisualChildren.Add(button);
        Assert.Equal(1, attaches);

        root.VisualChildren.Remove(button);
        Assert.Equal(1, disposes);

        root.VisualChildren.Add(button);
        Assert.Equal(2, attaches);

        button.Aspect = null;
        Assert.Equal(2, disposes);
    }

    [Fact]
    public void PublicResolvedValuesCannotCorruptEngineCleanup()
    {
        AspectPackage package = AspectPackage.Create("snapshot")
            .Components(components => components.AddRule(new AspectRuleSet(
                "opacity",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.25f))],
                0)));
        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        AspectEngine engine = new();
        Button button = new();
        ResolvedAspect resolved = engine.Apply(button, catalog, new AspectEnvironment("snapshot")).ResolvedAspect;
        IDictionary<UiProperty, ResolvedAspectValue> values =
            Assert.IsAssignableFrom<IDictionary<UiProperty, ResolvedAspectValue>>(resolved.Values);

        Assert.Throws<NotSupportedException>(() => values.Clear());
        engine.Clear(button);

        Assert.Equal(1f, button.Opacity);
        Assert.Equal(UiPropertyValueSource.Default, button.GetValueSource(UIElement.OpacityProperty));
    }

    [Fact]
    public void PublicAspectCollectionsAreStableReadOnlySnapshots()
    {
        List<UiProperty> properties = [UIElement.OpacityProperty];
        AspectDependencySet dependencySet = new(properties: properties);
        properties.Clear();
        Assert.Single(dependencySet.Properties);
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<UiProperty>>(dependencySet.Properties).Clear());

        List<AspectConditionDependency> dependencies =
        [
            new(AspectConditionDependencyKind.UiProperty, Property: UIElement.OpacityProperty)
        ];
        AspectConditionResult conditionResult = new(true, dependencies, "opacity");
        dependencies.Clear();
        Assert.Single(conditionResult.Dependencies);
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<AspectConditionDependency>>(conditionResult.Dependencies).Clear());

        List<AspectTokenTrace> tokenTraces =
        [
            new(ButtonTokens.Background, "test", null, null)
        ];
        AspectDiagnostics.Snapshot diagnostics = new(tokenTraces: tokenTraces);
        tokenTraces.Clear();
        Assert.Single(diagnostics.TokenTraces);
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<AspectTokenTrace>>(diagnostics.TokenTraces).Clear());
    }

    [Fact]
    public void RejectedElementAspectAssignmentIsAtomic()
    {
        ElementAspect valid = new(
            "valid",
            typeof(Button),
            [new ElementAspectValue(UIElement.OpacityProperty, 0.5f)]);
        ElementAspect invalid = new(
            "text-only",
            typeof(TextBlock),
            [new ElementAspectValue(TextBlock.TextProperty, "invalid")]);
        Button button = new() { Aspect = valid };

        Assert.Throws<InvalidOperationException>(() => button.Aspect = invalid);

        Assert.Same(valid, button.Aspect);
    }

    [Fact]
    public void ElementAspectRejectsIncompatibleConditionalProperty()
    {
        AspectConditionKey key = new("active");
        ElementAspect invalid = new(
            "invalid-conditional-property",
            typeof(Button),
            [],
            [
                new ElementAspectCondition(
                    key,
                    [new ElementAspectValue(TextBlock.TextProperty, "invalid")],
                    0)
            ]);
        Button button = new();

        Assert.Throws<InvalidOperationException>(() => button.Aspect = invalid);
        Assert.Null(button.Aspect);
    }

    [Fact]
    public void EngineRejectsDeclarationIncompatibleWithTargetElement()
    {
        AspectPackage package = AspectPackage.Create("invalid-property")
            .Components(components => components.AddRule(new AspectRuleSet(
                "invalid-property",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(TextBlock.TextProperty, AspectValue<string>.Literal("invalid"))],
                0)));
        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        Button button = new();

        Assert.Throws<InvalidOperationException>(() =>
            new AspectEngine().Apply(button, catalog, new AspectEnvironment("invalid-property")));
        Assert.Equal(string.Empty, button.GetValue(TextBlock.TextProperty));
        Assert.Equal(UiPropertyValueSource.Default, button.GetValueSource(TextBlock.TextProperty));
    }

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }
}
