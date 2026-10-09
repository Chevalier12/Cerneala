using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Buttons;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectTemplateCatalogIntegrationTests
{
    [Fact]
    public void ControlResolvesNamedComponentTemplateFromRootCatalog()
    {
        Border generated = new();
        ComponentTemplate<Button> template = new("App.Button", _ => generated);
        Button button = new() { ComponentTemplateKey = "App.Button" };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), template))));

        root.AspectProcessor.Process(button);

        Assert.Same(generated, button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void LaterPackageReplacesNamedComponentTemplateWithSameOwnerSpecificity()
    {
        Border firstRoot = new();
        Border secondRoot = new();
        ComponentTemplate<Button> first = new("First", _ => firstRoot);
        ComponentTemplate<Button> second = new("Second", _ => secondRoot);
        Button button = new() { ComponentTemplateKey = "App.Button" };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("First")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), first))));
        root.AspectProcessor.Process(button);

        root.AspectRegistry.Register(AspectPackage.Create("Second")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), second))));
        root.AspectProcessor.Process(button);

        Assert.Same(secondRoot, button.ComponentTemplateInstance!.Root);
        Assert.Null(firstRoot.LogicalParent);
    }

    [Fact]
    public void ContentPresenterFallsBackToContentTemplatesFromRootCatalog()
    {
        ContentTemplate<string> template = new(
            "App.String",
            key: "studio",
            priority: 0,
            _ => new Border());
        ContentPresenter presenter = new()
        {
            Content = "Aspect",
            ContentTemplateKey = "studio"
        };
        UIRoot root = RootWith(presenter);
        root.AspectRegistry.Register(AspectPackage.Create("Content")
            .Content(content => content.Add(
                new ContentTemplateDefinition("App.String", typeof(string), "studio", template))));

        root.AspectProcessor.Process(presenter);

        Assert.IsType<Border>(presenter.PresentedChild);
    }

    [Fact]
    public void ContentTemplateInsideComponentReceivesTemplateOwnerVariants()
    {
        AspectSlot<Button, ContentPresenter> slot = AspectSlot.For<Button, ContentPresenter>("Content");
        AspectVariantSet? observedVariants = null;
        object? observedOwner = null;
        ContentTemplate<string> contentTemplate = new(
            "App.String",
            key: null,
            priority: 0,
            context =>
            {
                observedVariants = context.Variants;
                observedOwner = context.Owner;
                return new Border();
            });
        ComponentTemplate<Button> componentTemplate = new("App.Button", context =>
        {
            ContentPresenter presenter = new();
            context.RegisterSlot(slot, presenter);
            context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
            return presenter;
        });
        Button button = new()
        {
            Content = "Aspect",
            ComponentTemplateKey = "App.Button"
        };
        button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Primary);
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Button", typeof(Button), componentTemplate)))
            .Content(content => content.Add(
                new ContentTemplateDefinition("App.String", typeof(string), key: null, contentTemplate))));

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Same(button, observedOwner);
        Assert.NotNull(observedVariants);
        Assert.True(observedVariants!.TryGet(ButtonVariants.Kind, out ButtonKind kind));
        Assert.Equal(ButtonKind.Primary, kind);
    }

    [Fact]
    public void SlotRuleUsesTemplateOwnerVariantsAndTargetsRegisteredElement()
    {
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border chrome = new();
        ComponentTemplate<Button> template = new("App.Button", context =>
        {
            context.RegisterSlot(slot, chrome);
            return chrome;
        });
        AspectRuleSet slotRule = new AspectRuleSetBuilder(
            "button.primary.chrome",
            AspectLayer.App,
            new AspectTarget(
                typeof(Border),
                slot,
                [
                    AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Primary),
                    AspectCondition.State(AspectState.Hover)
                ]),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent), "chrome.background")
            .Build();
        Button button = new() { ComponentTemplateKey = "App.Button" };
        button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Primary);
        button.IsPointerOver = true;
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components =>
            {
                components.AddTemplate(new ComponentTemplateDefinition("App.Button", typeof(Button), template));
                components.AddRule(slotRule);
            }));

        root.AspectProcessor.Process(button);
        root.AspectProcessor.Process(chrome);

        Assert.Same(accent, chrome.Background);
        AspectDiagnostics.Snapshot diagnostics = root.Detective.CaptureAspect(chrome);
        Assert.Equal(slot, diagnostics.ResolvedAspect!.Dependencies.Slot);
        Assert.Contains(ButtonVariants.Kind, diagnostics.ResolvedAspect.Dependencies.Variants);
        Assert.Contains(AspectState.Hover, diagnostics.ResolvedAspect.Dependencies.States);
    }

    [Theory]
    [InlineData("hover", false)]
    [InlineData("hover", true)]
    [InlineData("pressed", false)]
    [InlineData("pressed", true)]
    [InlineData("focus", false)]
    [InlineData("focus", true)]
    [InlineData("focus-within", false)]
    [InlineData("focus-within", true)]
    [InlineData("disabled", false)]
    [InlineData("disabled", true)]
    public void SlotRuleFollowsOwnerStateAfterInitialFramesWithoutRebuildingTemplate(
        string stateName, bool initiallyActive)
    {
        (AspectState state, UiProperty<bool> property) = stateName switch
        {
            "hover" => (AspectState.Hover, UIElement.IsPointerOverProperty),
            "pressed" => (AspectState.Pressed, Button.IsPressedProperty),
            "focus" => (AspectState.Focus, UIElement.IsKeyboardFocusedProperty),
            "focus-within" => (AspectState.FocusWithin, UIElement.IsKeyboardFocusWithinProperty),
            "disabled" => (AspectState.Disabled, UIElement.IsEnabledProperty),
            _ => throw new ArgumentOutOfRangeException(nameof(stateName))
        };
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border chrome = new();
        int templateCreations = 0;
        ComponentTemplate<Button> template = new("App.Button", context =>
        {
            templateCreations++;
            context.RegisterSlot(slot, chrome);
            return chrome;
        });
        AspectRuleSet slotRule = new AspectRuleSetBuilder(
            "chrome.state",
            AspectLayer.App,
            new AspectTarget(typeof(Border), slot, [AspectCondition.State(state)]),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent))
            .Build();
        Button button = new() { ComponentTemplateKey = "App.Button" };
        SetState(initiallyActive);
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components =>
        {
            components.AddTemplate(new ComponentTemplateDefinition("App.Button", typeof(Button), template));
            components.AddRule(slotRule);
        }));
        root.ProcessFrame();
        root.ProcessFrame();
        ComponentTemplateInstance instance = button.ComponentTemplateInstance!;
        Assert.Same(initiallyActive ? accent : null, chrome.Background);
        Assert.Contains(state, root.AspectProcessor.Engine.GetDependencies(chrome).States);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            SetState(!initiallyActive);
            root.ProcessFrame();
            root.ProcessFrame();
            Assert.Same(initiallyActive ? null : accent, chrome.Background);

            SetState(initiallyActive);
            root.ProcessFrame();
            root.ProcessFrame();
            Assert.Same(initiallyActive ? accent : null, chrome.Background);
        }

        Assert.Same(instance, button.ComponentTemplateInstance);
        Assert.Equal(1, templateCreations);
        Assert.False(root.ProcessFrame().HasWork);

        Border replacement = new();
        button.ComponentTemplate = new ComponentTemplate<Button>("Replacement", context =>
        {
            context.RegisterSlot(slot, replacement);
            return replacement;
        });
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Same(initiallyActive ? accent : null, replacement.Background);
        SetState(!initiallyActive);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Same(initiallyActive ? null : accent, replacement.Background);
        Assert.Null(chrome.Root);
        Assert.Null(chrome.Background);
        Assert.Empty(root.AspectProcessor.Engine.GetDependencies(chrome).States);
        Assert.False(root.ProcessFrame().HasWork);

        void SetState(bool active) => button.SetValue(property, state == AspectState.Disabled ? !active : active);
    }

    [Fact]
    public async Task SlotRuleFollowsOwnerHoverThroughServoInput()
    {
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border chrome = new() { IsHitTestVisible = false };
        ComponentTemplate<Button> template = new("App.Button", context =>
        {
            context.RegisterSlot(slot, chrome);
            return chrome;
        });
        Button button = new() { ComponentTemplateKey = "App.Button", Width = 100, Height = 40 };
        Border outside = new() { Width = 100, Height = 40 };
        ServoApi.SetId(button, "button");
        ServoApi.SetId(outside, "outside");
        StackPanel panel = new();
        panel.VisualChildren.Add(button);
        panel.VisualChildren.Add(outside);
        UIRoot root = RootWith(panel);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components =>
        {
            components.AddTemplate(new ComponentTemplateDefinition("App.Button", typeof(Button), template));
            components.AddRule(new AspectRuleSetBuilder(
                "chrome.hover", AspectLayer.App,
                new AspectTarget(typeof(Border), slot, [AspectCondition.State(AspectState.Hover)]),
                declarationOrder: 0)
                .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent))
                .Build());
        }));
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        ServoApi servo = new(host);
        await servo.HoverAsync(ServoTarget.ById("outside"));
        await servo.WaitForIdleAsync();
        Assert.False(button.IsPointerOver);
        Assert.Null(chrome.Background);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            await servo.HoverAsync(ServoTarget.ById("button"));
            await servo.WaitForIdleAsync();
            Assert.True(button.IsPointerOver);
            Assert.Same(accent, chrome.Background);

            await servo.HoverAsync(ServoTarget.ById("outside"));
            await servo.WaitForIdleAsync();
            Assert.False(button.IsPointerOver);
            Assert.Null(chrome.Background);
        }
    }

    [Fact]
    public void OwnerStateChangeQueuesOnlySlotsDependingOnTheChangedState()
    {
        AspectSlot<Button, Border> hoverSlot = AspectSlot.For<Button, Border>("HoverChrome");
        AspectSlot<Button, Border> pressedSlot = AspectSlot.For<Button, Border>("PressedChrome");
        AspectSlot<Button, Border> staticSlot = AspectSlot.For<Button, Border>("StaticChrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        ComponentTemplate<Button> template = new("App.Button", context =>
        {
            StackPanel panel = new();
            foreach (AspectSlot slot in new AspectSlot[] { hoverSlot, pressedSlot, staticSlot })
            {
                Border part = new();
                context.RegisterSlot(slot, part);
                panel.VisualChildren.Add(part);
            }
            return panel;
        });
        Button button = new() { ComponentTemplateKey = "App.Button" };
        Button sibling = new() { ComponentTemplateKey = "App.Button" };
        UIRoot root = RootWith(button);
        root.LogicalChildren.Add(sibling);
        root.VisualChildren.Add(sibling);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components =>
        {
            components.AddTemplate(new ComponentTemplateDefinition("App.Button", typeof(Button), template));
            components.AddRule(Rule(hoverSlot, AspectState.Hover));
            components.AddRule(Rule(pressedSlot, AspectState.Pressed));
            components.AddRule(Rule(staticSlot));
        }));
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.False(root.ProcessFrame().HasWork);
        Border hover = (Border)button.ComponentTemplateInstance!.Slots[hoverSlot];
        Border pressed = (Border)button.ComponentTemplateInstance.Slots[pressedSlot];
        Border staticChrome = (Border)button.ComponentTemplateInstance.Slots[staticSlot];
        Border siblingHover = (Border)sibling.ComponentTemplateInstance!.Slots[hoverSlot];
        Assert.Same(accent, staticChrome.Background);

        button.IsPointerOver = true;
        Assert.Equal(1, root.ProcessFrame().AspectElements);
        Assert.Equal(1, root.ProcessFrame().AspectElements);
        Assert.Same(accent, hover.Background);
        Assert.Null(pressed.Background);
        Assert.Null(siblingHover.Background);
        Assert.False(root.ProcessFrame().HasWork);

        button.IsPressed = true;
        Assert.Equal(1, root.ProcessFrame().AspectElements);
        Assert.Equal(1, root.ProcessFrame().AspectElements);
        Assert.Same(accent, pressed.Background);
        Assert.False(root.ProcessFrame().HasWork);

        button.Invalidate(InvalidationFlags.Aspect, "Unchanged owner state");
        Assert.Equal(1, root.ProcessFrame().AspectElements);
        Assert.False(root.ProcessFrame().HasWork);

        // Detach clears owner snapshots and part dependencies; reattach resolves afresh.
        root.VisualChildren.Remove(button);
        root.LogicalChildren.Remove(button);
        button.IsPointerOver = false;
        button.IsPressed = false;
        root.LogicalChildren.Add(button);
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Null(hover.Background);
        Assert.Null(pressed.Background);
        button.IsPointerOver = true;
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Same(accent, hover.Background);
        Assert.Null(siblingHover.Background);
        Assert.False(root.ProcessFrame().HasWork);

        AspectRuleSet Rule(AspectSlot slot, AspectState? state = null) => new AspectRuleSetBuilder(
            slot.Name, AspectLayer.App,
            new AspectTarget(typeof(Border), slot, state is null ? [] : [AspectCondition.State(state)]),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent))
            .Build();
    }

    [Fact]
    public void ReplacingTemplateRemovesSlotContextFromDetachedElements()
    {
        AspectSlot<Button, Border> slot = AspectSlot.For<Button, Border>("Chrome");
        SolidColorBrush accent = new(new Color(77, 240, 255));
        Border oldChrome = new();
        ComponentTemplate<Button> first = new("First", context =>
        {
            context.RegisterSlot(slot, oldChrome);
            return oldChrome;
        });
        ComponentTemplate<Button> second = new("Second", _ => new Border());
        AspectRuleSet slotRule = new AspectRuleSetBuilder(
            "button.chrome",
            AspectLayer.App,
            new AspectTarget(typeof(Border), slot),
            declarationOrder: 0)
            .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(accent))
            .Build();
        Button button = new() { ComponentTemplate = first };
        UIRoot root = RootWith(button);
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddRule(slotRule)));
        root.AspectProcessor.Process(oldChrome);
        Assert.Same(accent, oldChrome.Background);

        button.ComponentTemplate = second;
        root.AspectProcessor.Process(oldChrome);

        Assert.Null(oldChrome.Background);
        Assert.Null(oldChrome.LogicalParent);
    }

    private static UIRoot RootWith(UIElement child)
    {
        UIRoot root = new();
        root.LogicalChildren.Add(child);
        root.VisualChildren.Add(child);
        return root;
    }
}
