using System.Collections.Generic;
using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectUnificationContractTests
{
    [Fact]
    public void MatchingRuleEvaluatesPredicateExactlyOnce()
    {
        int calls = 0;
        AspectRuleSet rule = Rule(
            "single-evaluation",
            typeof(Button),
            AspectCondition.Predicate("count", _ =>
            {
                calls++;
                return true;
            }));

        new AspectEngine().Resolve(new Button(), Catalog("Single", rule), new AspectEnvironment("test"));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void NonMatchingTargetDoesNotEvaluatePredicateOrCaptureItsDependency()
    {
        int calls = 0;
        AspectRuleSet rule = Rule(
            "wrong-type",
            typeof(TextBlock),
            AspectCondition.Predicate("must-not-run", _ =>
            {
                calls++;
                return true;
            }));

        ResolvedAspect resolved = new AspectEngine().Resolve(
            new Button(),
            Catalog("WrongType", rule),
            new AspectEnvironment("test"));

        Assert.Equal(0, calls);
        Assert.Empty(resolved.Dependencies.Data);
        Assert.Empty(resolved.Dependencies.Properties);
        Assert.Empty(resolved.Dependencies.States);
        Assert.Empty(resolved.Dependencies.Variants);
    }

    [Fact]
    public void TemplateSlotRejectsElementOutsideDeclaredTargetType()
    {
        TemplateSlotMap slots = new();
        AspectSlot<Button, TextBlock> slot = AspectSlot.For<Button, TextBlock>("Content");

        Assert.Throws<ArgumentException>(() => slots.Register(slot, new Border()));
    }

    [Fact]
    public void SlotFactoriesAndMatchingEnforceOwnerAndTargetTypes()
    {
        Assert.Throws<ArgumentException>(() => AspectSlot.For<string, TextBlock>("invalid-owner"));
        Assert.Throws<ArgumentException>(() => AspectSlot.For<Button, string>("invalid-target"));

        AspectSlot<Button, TextBlock> slot = AspectSlot.For<Button, TextBlock>("Content");
        AspectTarget target = new(typeof(TextBlock), slot);
        AspectMatchContext wrongOwner = new(
            new TextBlock(),
            ownerComponent: new Border(),
            slotPath: new AspectSlotPath(slot));
        ComponentTemplateContext context = new(
            new Border(),
            new AspectEnvironment("wrong-owner"));

        Assert.False(target.Matches(wrongOwner));
        Assert.Throws<ArgumentException>(() => context.RegisterSlot(slot, new TextBlock()));
    }

    [Fact]
    public void ControlRejectsVariantKeyOwnedByDifferentControlType()
    {
        Border border = new();
        AspectVariantKey<Button, string> key = AspectVariantKey.For<Button, string>("kind");

        Assert.Throws<ArgumentException>(() => border.SetAspectVariant(key, "primary"));
        Assert.Throws<ArgumentException>(() => AspectVariantKey.For<string, string>("invalid-owner"));
    }

    [Fact]
    public void RegistryPackageSnapshotCannotBeMutatedThroughPublicSurface()
    {
        AspectRegistry registry = new();
        registry.Register(AspectPackage.Create("First"));
        int version = registry.Version;
        AspectCatalog catalog = registry.BuildCatalog();
        IList<AspectPackage> packages = Assert.IsAssignableFrom<IList<AspectPackage>>(registry.Packages);

        Assert.Throws<NotSupportedException>(() => packages.Add(AspectPackage.Create("Injected")));
        Assert.Equal(version, registry.Version);
        Assert.Same(catalog, registry.BuildCatalog());
        Assert.Single(registry.Packages);
    }

    [Fact]
    public void PackageAndCatalogCollectionsAreImmutableSnapshots()
    {
        AspectRuleSet original = Rule("original", typeof(Button));
        AspectPackage package = AspectPackage.Create("Immutable")
            .Components(components => components.AddRule(original));
        IList<AspectRuleSet> packageRules = Assert.IsAssignableFrom<IList<AspectRuleSet>>(package.Rules);
        AspectRuleSet replacement = Rule("replacement", typeof(Button));
        AspectBehavior behavior = new(typeof(Button), _ => null);

        Assert.Throws<NotSupportedException>(() => packageRules[0] = replacement);

        AspectPackage behaviorPackage = AspectPackage.Create("Behavior")
            .Components(components => components.AddBehavior(behavior));
        IList<AspectBehavior> packageBehaviors = Assert.IsAssignableFrom<IList<AspectBehavior>>(behaviorPackage.Behaviors);
        Assert.Throws<NotSupportedException>(() => packageBehaviors.Add(behavior));

        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        IList<AspectRuleSet> catalogRules = Assert.IsAssignableFrom<IList<AspectRuleSet>>(catalog.Rules);
        IDictionary<AspectToken, AspectValue> tokenDefaults =
            Assert.IsAssignableFrom<IDictionary<AspectToken, AspectValue>>(catalog.TokenDefaults);

        Assert.Throws<NotSupportedException>(() => catalogRules.Add(replacement));
        AspectToken<float> injected = AspectToken.Float("injected");
        Assert.Throws<NotSupportedException>(() =>
            tokenDefaults.Add(injected, AspectValue<float>.Literal(1)));
        Assert.Single(package.Rules);
        Assert.Single(catalog.Rules);
        Assert.Empty(catalog.TokenDefaults);
    }

    [Fact]
    public void PackageBehaviorAttachesOnceAndIsDisposedOnReplacementAndDetach()
    {
        int firstAttach = 0;
        int firstDispose = 0;
        int secondAttach = 0;
        int secondDispose = 0;
        AspectBehavior first = new(typeof(Button), _ =>
        {
            firstAttach++;
            return new CallbackLifetime(() => firstDispose++);
        });
        AspectBehavior second = new(typeof(Button), _ =>
        {
            secondAttach++;
            return new CallbackLifetime(() => secondDispose++);
        });
        Border scope = new();
        scope.Resources["Behavior"] = BehaviorPackage("First", first);
        Button button = new();
        scope.Child = button;
        UIRoot root = new();
        root.VisualChildren.Add(scope);

        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(1, firstAttach);
        Assert.Equal(0, firstDispose);

        scope.Resources["Behavior"] = BehaviorPackage("Second", second);
        root.ProcessFrame();
        Assert.Equal(1, firstDispose);
        Assert.Equal(1, secondAttach);

        scope.Child = null;
        root.ProcessFrame();
        Assert.Equal(1, secondDispose);
    }

    [Fact]
    public void DetachingWithAThrowingPackageBehaviorLifetimeDisposesEveryLifetimeOnceAndClearsAspectState()
    {
        List<string> events = [];
        int[] counts = new int[3];
        SolidColorBrush brush = new(Color.Red);
        AspectPackage package = AspectPackage.Create("A").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "A.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            for (int behaviorIndex = 0; behaviorIndex < 3; behaviorIndex++)
            {
                int i = behaviorIndex;
                components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
                {
                    int n = ++counts[i];
                    events.Add($"create B{i}#{n}");
                    return new RecordingLifetime(() =>
                    {
                        events.Add($"dispose B{i}#{n}");
                        if (i == 2 && n == 1)
                        {
                            throw new InvalidOperationException("B2 dispose failed");
                        }
                    });
                }));
            }
        });
        UIRoot root = new();
        root.AspectRegistry.Register(package);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Same(brush, button.Background);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(button));

        Assert.Equal("B2 dispose failed", failure.Message);
        Assert.Equal(
            ["create B0#1", "create B1#1", "create B2#1", "dispose B2#1", "dispose B1#1", "dispose B0#1"],
            events);
        Assert.NotSame(brush, button.Background);

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Equal(["create B0#2", "create B1#2", "create B2#2"], events[6..]);
        Assert.Same(brush, button.Background);

        root.VisualChildren.Remove(button);
        Assert.Equal(["dispose B2#2", "dispose B1#2", "dispose B0#2"], events[9..]);
    }

    [Fact]
    public void ReplacingAPackageBehaviorWhoseLifetimeThrowsKeepsTheNewLifetimeAndDisposesTheOldOnce()
    {
        List<string> events = [];
        int xCount = 0;
        int yCount = 0;
        AspectBehavior x = new(typeof(Border), _ =>
        {
            int n = ++xCount;
            events.Add($"create X#{n}");
            return new RecordingLifetime(() =>
            {
                events.Add($"dispose X#{n}");
                if (n == 1)
                {
                    throw new InvalidOperationException("X dispose failed");
                }
            });
        });
        AspectBehavior y = new(typeof(Border), _ =>
        {
            int n = ++yCount;
            events.Add($"create Y#{n}");
            return new RecordingLifetime(() => events.Add($"dispose Y#{n}"));
        });
        UIRoot root = new();
        root.AspectRegistry.Register(BehaviorPackage("X", x));
        Border border = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(BehaviorPackage("Y", y), notify: false);
        root.AspectRegistry.Unregister("X");
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.Equal("X dispose failed", failure.Message);
        root.ProcessFrame();
        root.VisualChildren.Remove(border);
        Assert.Equal(["create X#1", "create Y#1", "dispose X#1", "dispose Y#1"], events);
    }

    [Fact]
    public void FailedPackageBehaviorAttachKeepsItsExceptionWhenARolledBackLifetimeThrows()
    {
        int rollbackDisposals = 0;
        AspectBehavior attached = new(typeof(Border), _ => new RecordingLifetime(() =>
        {
            rollbackDisposals++;
            throw new InvalidOperationException("rollback dispose failed");
        }));
        AspectBehavior failing = new(typeof(Border), _ => throw new InvalidOperationException("attach failed"));
        UIRoot root = new();
        Border border = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(AspectPackage.Create("Failing").Components(components =>
        {
            components.AddBehavior(attached);
            components.AddBehavior(failing);
        }));
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.Equal("attach failed", failure.Message);
        Assert.Equal(1, rollbackDisposals);
    }

    [Fact]
    public void RuleAndTargetCopyCallerOwnedCollections()
    {
        List<AspectCondition> conditions =
        [
            AspectCondition.Predicate("stable", _ => true)
        ];
        List<AspectDeclaration> declarations =
        [
            new AspectDeclaration(
                Control.BackgroundProperty,
                AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
        ];
        AspectTarget target = new(typeof(Button), conditions: conditions);
        AspectRuleSet rule = new("stable", AspectLayer.App, target, declarations, 0);

        conditions.Clear();
        declarations.Clear();

        Assert.Single(target.Conditions);
        Assert.Single(rule.Declarations);
    }

    [Fact]
    public void CatalogDiagnosticsKeepOriginalPackageAfterSharedRulesBuildAnotherCatalog()
    {
        AspectRuleSet shared = Rule("shared", typeof(Button));
        AspectCatalog firstCatalog = Catalog("First", shared);
        Button first = new();
        AspectEngine firstEngine = new();
        firstEngine.Apply(first, firstCatalog, new AspectEnvironment("first"));
        Assert.Equal("First", Assert.Single(firstEngine.GetDiagnostics(first).ResolutionSteps).PackageName);

        AspectCatalog secondCatalog = Catalog("Second", shared);
        Button second = new();
        AspectEngine secondEngine = new();
        secondEngine.Apply(second, secondCatalog, new AspectEnvironment("second"));
        firstEngine.Apply(first, firstCatalog, new AspectEnvironment("first-again"));

        Assert.Equal("First", Assert.Single(firstEngine.GetDiagnostics(first).ResolutionSteps).PackageName);
        Assert.Equal("Second", Assert.Single(secondEngine.GetDiagnostics(second).ResolutionSteps).PackageName);
    }

    [Fact]
    public void ElementAspectUsesCanonicalEngineSourceAndQueuesIncrementalMutation()
    {
        UIRoot root = new();
        Button button = new();
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, new SolidColorBrush(Color.White))]);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.Aspect = aspect;
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        Assert.True(aspect.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black)));
        var update = root.ProcessFrame();
        var idle = root.ProcessFrame();

        Assert.True(update.AspectElements > 0);
        Assert.False(idle.HasWork);
    }

    [Fact]
    public void ApplicationAspectUsesCanonicalEngineSource()
    {
        Application application = new();
        SolidColorBrush brush = new(new Color(12, 34, 56));
        application.Resources["ApplicationAspect"] = Package("Application", brush);
        UIRoot root = new();
        root.SetResourceProvider(application.Resources);
        Button button = new();

        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void ScopedAspectAppearsInCanonicalEngineDiagnostics()
    {
        UIRoot root = new();
        Border scope = new();
        scope.Resources["ScopedAspect"] = Package(
            "Scoped",
            new SolidColorBrush(Color.White));
        Button button = new();
        scope.Child = button;

        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        Assert.Contains(
            root.Detective.CaptureAspect(button).ResolutionSteps,
            step => string.Equals(step.PackageName, "Scoped", StringComparison.Ordinal));
    }

    [Fact]
    public void RuntimeCascadeComposesRootApplicationScopesAndElementAspect()
    {
        SolidColorBrush rootBrush = new(new Color(10, 0, 0));
        SolidColorBrush applicationBrush = new(new Color(20, 0, 0));
        SolidColorBrush outerBrush = new(new Color(30, 0, 0));
        SolidColorBrush innerBrush = new(new Color(40, 0, 0));
        SolidColorBrush elementBrush = new(new Color(50, 0, 0));
        UIRoot root = new();
        root.AspectRegistry.Register(Package("Root", rootBrush));
        Application application = new();
        application.Resources["Application"] = Package("Application", applicationBrush);
        root.SetResourceProvider(application.Resources);
        Border outer = new();
        outer.Resources["Outer"] = Package("Outer", outerBrush);
        Border inner = new();
        inner.Resources["Inner"] = Package("Inner", innerBrush);
        Button button = new()
        {
            Aspect = new ElementAspect(
                [new ElementAspectValue(Control.BackgroundProperty, elementBrush)])
        };
        inner.Child = button;
        outer.Child = inner;
        root.VisualChildren.Add(outer);

        root.ProcessFrame();
        Assert.Same(elementBrush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = null;
        root.ProcessFrame();
        Assert.Same(innerBrush, button.Background);

        inner.Resources.Remove("Inner");
        root.ProcessFrame();
        Assert.Same(outerBrush, button.Background);

        outer.Resources.Remove("Outer");
        root.ProcessFrame();
        Assert.Same(applicationBrush, button.Background);

        application.Resources.Remove("Application");
        root.ProcessFrame();
        Assert.Same(rootBrush, button.Background);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void ReplacingScopedPackageRecomputesOnlyThroughAspectQueueAndReturnsIdle()
    {
        UIRoot root = new();
        Border scope = new();
        SolidColorBrush first = new(new Color(1, 2, 3));
        SolidColorBrush second = new(new Color(4, 5, 6));
        scope.Resources["Scoped"] = Package("Scoped.First", first);
        Button button = new();
        scope.Child = button;
        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        scope.Resources["Scoped"] = Package("Scoped.Second", second);
        var update = root.ProcessFrame();
        var idle = root.ProcessFrame();

        Assert.Same(second, button.Background);
        Assert.True(update.AspectElements > 0);
        Assert.False(idle.HasWork);
    }

    [Fact]
    public void InnerScopeTokenOverridesOuterScopeWithoutLeakingToSibling()
    {
        AspectToken<Brush?> token = AspectToken.Create<Brush?>("scope.background");
        SolidColorBrush outerBrush = new(new Color(11, 22, 33));
        SolidColorBrush innerBrush = new(new Color(44, 55, 66));
        AspectPackage outerPackage = AspectPackage.Create("Outer.Token")
            .Tokens(tokens => tokens.Set(token, outerBrush))
            .Components(components => components.AddRule(new AspectRuleSet(
                "token.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, token.Ref())],
                0)));
        AspectPackage innerPackage = AspectPackage.Create("Inner.Token")
            .Tokens(tokens => tokens.Set(token, innerBrush));
        Border outer = new();
        outer.Resources["Outer"] = outerPackage;
        StackPanel children = new();
        Border inner = new();
        inner.Resources["Inner"] = innerPackage;
        Button innerButton = new();
        inner.Child = innerButton;
        Button sibling = new();
        children.VisualChildren.Add(inner);
        children.VisualChildren.Add(sibling);
        outer.Child = children;
        UIRoot root = new();
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        Assert.Same(innerBrush, innerButton.Background);
        Assert.Same(outerBrush, sibling.Background);
    }

    [Fact]
    public void SharedElementAspectInvalidatesAllConsumersAndSurvivesDetachReattach()
    {
        SolidColorBrush first = new(new Color(10, 20, 30));
        SolidColorBrush second = new(new Color(40, 50, 60));
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, first)]);
        UIRoot root = new();
        Button firstButton = new() { Aspect = aspect };
        Button secondButton = new() { Aspect = aspect };
        root.VisualChildren.Add(firstButton);
        root.VisualChildren.Add(secondButton);
        root.ProcessFrame();

        Assert.True(aspect.SetValue(Control.BackgroundProperty, second));
        var sharedUpdate = root.ProcessFrame();

        Assert.True(sharedUpdate.AspectElements >= 2);
        Assert.Same(second, firstButton.Background);
        Assert.Same(second, secondButton.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, firstButton.GetValueSource(Control.BackgroundProperty));

        root.VisualChildren.Remove(secondButton);
        root.ProcessFrame();
        Assert.True(aspect.SetValue(Control.BackgroundProperty, first));
        root.ProcessFrame();
        root.VisualChildren.Add(secondButton);
        root.ProcessFrame();

        Assert.Same(first, secondButton.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, secondButton.GetValueSource(Control.BackgroundProperty));
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void LongRunningSessionDoesNotExhaustCompositeCatalogVersions()
    {
        SolidColorBrush first = new(new Color(10, 20, 30));
        SolidColorBrush second = new(new Color(40, 50, 60));
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, first)]);
        UIRoot root = new();
        Button[] buttons = [new() { Aspect = aspect }, new() { Aspect = aspect }, new() { Aspect = aspect }];
        foreach (Button button in buttons)
        {
            root.VisualChildren.Add(button);
        }

        root.ProcessFrame();

        // Each element attach and each consumer of a changed shared ElementAspect composes
        // its catalog again. A composition must not consume a number from a finite space,
        // or ~2^31 valid compositions in a long-running session make ProcessFrame throw:
        // a composed catalog keeps the registry version it was built from.
        for (int round = 0; round < 3; round++)
        {
            SolidColorBrush next = round % 2 == 0 ? second : first;
            Assert.True(aspect.SetValue(Control.BackgroundProperty, next));
            root.ProcessFrame();

            Assert.All(buttons, button =>
            {
                Assert.Same(next, button.Background);
                Assert.Equal(
                    root.AspectRegistry.Version,
                    root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);
            });
        }
    }

    [Fact]
    public void ScopedPackageProvidesComponentTemplateThroughCanonicalCatalog()
    {
        Border generatedRoot = new();
        Border replacementRoot = new();
        ComponentTemplate<Button> template = new("Scoped.Template", _ => generatedRoot);
        ComponentTemplate<Button> replacement = new("Scoped.Template.Replacement", _ => replacementRoot);
        AspectPackage package = AspectPackage.Create("Scoped.Templates")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Template", typeof(Button), template)));
        Border scope = new();
        scope.Resources["Templates"] = package;
        Button button = new() { ComponentTemplateKey = "Scoped.Template" };
        scope.Child = button;
        UIRoot root = new();
        root.VisualChildren.Add(scope);

        root.ProcessFrame();

        Assert.Same(generatedRoot, button.ComponentTemplateInstance!.Root);

        scope.Resources["Templates"] = AspectPackage.Create("Scoped.Templates.Replacement")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Template", typeof(Button), replacement)))
            .Build();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Same(replacementRoot, button.ComponentTemplateInstance!.Root);
        Assert.Null(generatedRoot.LogicalParent);
        Assert.Null(generatedRoot.VisualParent);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void NamedAspectAppearsInCanonicalEngineDiagnostics()
    {
        ElementAspect named = new(
            "Named",
            typeof(Button),
            [new ElementAspectValue(
                Control.BackgroundProperty,
                new SolidColorBrush(Color.White))]);
        UIRoot root = new();
        Button button = new() { Aspect = named };
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Contains(
            root.Detective.CaptureAspect(button).ResolutionSteps,
            step => string.Equals(step.PackageName, "Named", StringComparison.Ordinal));
    }

    [Fact]
    public void LegacyMarkupAspectRuntimeTypeIsAbsent()
    {
        Type? legacyType = typeof(UIElement).Assembly.GetType(
            "Cerneala.UI.Markup.MarkupAspectResource",
            throwOnError: false);

        Assert.Null(legacyType);
    }

    private static AspectCatalog Catalog(string packageName, params AspectRuleSet[] rules)
    {
        AspectPackage package = AspectPackage.Create(packageName)
            .Components(components =>
            {
                foreach (AspectRuleSet rule in rules)
                {
                    components.AddRule(rule);
                }
            });
        return new AspectRegistry().Register(package).BuildCatalog();
    }

    private static AspectPackage Package(string packageName, Brush brush)
    {
        return AspectPackage.Create(packageName)
            .Components(components => components.AddRule(new AspectRuleSet(
                packageName + ".button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(
                    Control.BackgroundProperty,
                    AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0)));
    }

    private static AspectPackage BehaviorPackage(string packageName, AspectBehavior behavior)
    {
        return AspectPackage.Create(packageName)
            .Components(components => components.AddBehavior(behavior));
    }

    private static AspectRuleSet Rule(
        string name,
        Type targetType,
        params AspectCondition[] conditions)
    {
        return new AspectRuleSet(
            name,
            AspectLayer.App,
            new AspectTarget(targetType, conditions: conditions),
            [new AspectDeclaration(
                Control.BackgroundProperty,
                AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))],
            declarationOrder: 0);
    }

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }

    // Runs the callback on every Dispose call, so a second disposal is visible.
    private sealed class RecordingLifetime(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
