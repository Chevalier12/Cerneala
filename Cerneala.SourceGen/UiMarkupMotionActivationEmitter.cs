using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private void EmitMotionActivations(
            MarkupElement element,
            string variable,
            AspectResource aspect,
            bool bindToElementAspect)
        {
            if (!resolvedMotionAspects.TryGetValue((aspect, element), out ResolvedMotionAspect? resolved))
            {
                return;
            }

            string sessionName = "motionSession" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            currentPostLines.Add(
                "global::System.IDisposable " + sessionName +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachMotionSession(" + variable +
                (bindToElementAspect ? ", " + variable + ".Aspect!" : string.Empty) + ");");
            if (templateEmissionContexts.Count > 0)
            {
                currentPostLines.Add(templateEmissionContexts.Peek().ContextVariable + ".RegisterLifetime(" + sessionName + ");");
            }

            foreach (ResolvedMotionAnimation animation in resolved.Animations)
            {
                if (animation.Stagger is not null)
                {
                    EmitMotionStaggerActivation(animation, sessionName);
                    continue;
                }

                List<string> starts = [];
                foreach (ResolvedMotionProperty property in animation.Properties)
                {
                    string targetCode = EmitMotionTargetCode(property.Target, variable);
                    string typeCode = GetMotionTypeCode(property.Property.ValueType);
                    bool hasFrom = property.Source is not null && property.Source.Value is not MotionCurrentValueSyntax;
                    string fromCode = hasFrom
                        ? EmitMotionValue(property.Source!.Value, property.Property, targetCode, animation.Parameters)
                        : "default(" + typeCode + ")!";
                    bool toCurrent = property.Destination.Value is MotionCurrentValueSyntax;
                    bool hasBinding = TryEmitMotionBinding(
                        property.Destination.Value,
                        property.Property,
                        property.Target,
                        targetCode,
                        out string observationCode,
                        out string bindingModeCode,
                        out string projectionCode);
                    string toCode = toCurrent || hasBinding
                        ? "default(" + typeCode + ")!"
                        : EmitMotionValue(property.Destination.Value, property.Property, targetCode, animation.Parameters);
                    string specCode = property.SpecVariable ?? "null";
                    if (property.Keyframes is not null)
                    {
                        specCode = "motionKeyframesSpec" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                        nextReactiveId++;
                        string framesCode = string.Join(", ", property.Keyframes.Frames.Select(frame =>
                            "new global::Cerneala.UI.Motion.Specs.MotionKeyframe<" + typeCode + ">(" +
                            frame.Offset.ToString("R", CultureInfo.InvariantCulture) + "f, " +
                            EmitMotionValue(frame.Value, property.Property, targetCode, animation.Parameters) + ", " +
                            frame.EasingCode + ", " + (frame.Hold ? "true" : "false") + ")"));
                        currentPostLines.Add(
                            "global::Cerneala.UI.Motion.Specs.MotionSpec<" + typeCode + "> " + specCode +
                            " = new global::Cerneala.UI.Motion.Specs.KeyframesSpec<" + typeCode + ">(" +
                            "new global::Cerneala.UI.Motion.Specs.MotionKeyframe<" + typeCode + ">[] { " + framesCode + " }, " +
                            BuildDurationExpression(property.Keyframes.Duration) + ");");
                    }
                    string optionsCode = EmitMotionOptions(animation.Syntax.Options, animation.Parameters);
                    if (hasBinding)
                    {
                        starts.Add(property.Target.Prism is null
                            ? "global::Cerneala.UI.Markup.GeneratedMarkup.StartBoundMotionProperty(" + sessionName + ", " +
                                targetCode + ", " + property.Property.PropertyCode + ", " +
                                (hasFrom ? "true" : "false") + ", " + fromCode + ", " +
                                observationCode + ", " + bindingModeCode + ", " + projectionCode + ", " +
                                specCode + ", " + optionsCode + ")"
                            : EmitBoundPrismMotionStart(
                                sessionName,
                                property,
                                targetCode,
                                hasFrom,
                                fromCode,
                                observationCode,
                                bindingModeCode,
                                projectionCode,
                                specCode,
                                optionsCode));
                    }
                    else
                    {
                        starts.Add(property.Target.Prism is null
                            ? "global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionProperty(" + sessionName + ", " +
                                targetCode + ", " + property.Property.PropertyCode + ", " +
                                (hasFrom ? "true" : "false") + ", " + fromCode + ", " +
                                (toCurrent ? "true" : "false") + ", " + toCode + ", " + specCode + ", " + optionsCode + ")"
                            : EmitPrismMotionStart(
                                sessionName,
                                property,
                                targetCode,
                                hasFrom,
                                fromCode,
                                toCurrent,
                                toCode,
                                specCode,
                                optionsCode));
                    }
                }

                currentPostLines.Add(
                    "global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution> " + animation.FactoryName +
                    " = () => global::Cerneala.UI.Markup.MarkupMotionExecution.Parallel(" +
                    string.Join(", ", starts.Select(start => "() => global::Cerneala.UI.Markup.MarkupMotionExecution.From(" + start + ")")) + ");");
                currentPostLines.Add(
                    "global::System.Action " + animation.ExecutionName +
                    " = () => global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionExecution(" + sessionName +
                    ", " + animation.FactoryName + ");");
            }

            foreach (ResolvedMotionSet set in resolved.Sets)
            {
                List<string> assignments = [];
                foreach (ResolvedMotionSetProperty property in set.Properties)
                {
                    string targetCode = EmitMotionTargetCode(property.Target, variable);
                    string valueCode = EmitMotionValue(
                        property.Syntax.Value,
                        property.Property,
                        targetCode,
                        set.Parameters);
                    assignments.Add(property.Target.Prism is null
                        ? targetCode + ".SetValue(" +
                            property.Property.PropertyCode + ", " +
                            valueCode + ");"
                        : EmitPrismMotionSet(
                            property,
                            targetCode,
                            valueCode));
                }

                currentPostLines.Add(
                    "global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution> " + set.FactoryName +
                    " = () => { " + string.Join(" ", assignments) +
                    " return global::Cerneala.UI.Markup.MarkupMotionExecution.Parallel(); };");
                currentPostLines.Add(
                    "global::System.Action " + set.ExecutionName +
                    " = () => global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionExecution(" + sessionName +
                    ", " + set.FactoryName + ");");
            }

            foreach (ResolvedMotionComposition composition in resolved.Compositions)
            {
                if (composition.HandleName is null)
                {
                    string method = composition.Syntax!.Kind == MotionCompositionKind.Parallel ? "Parallel" : "Sequence";
                    currentPostLines.Add(
                        "global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution> " + composition.FactoryName +
                        " = () => global::Cerneala.UI.Markup.MarkupMotionExecution." + method + "(" +
                        string.Join(", ", composition.ChildFactoryNames) + ");");
                }
                else
                {
                    currentPostLines.Add(
                        "global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution> " + composition.FactoryName +
                        " = () => global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionExecution(" + sessionName + ", " +
                        Literal(composition.HandleName) + ", " + composition.ChildFactoryNames[0] + ");");
                }

                string startCall = composition.HandleName is null
                    ? "global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionExecution(" + sessionName +
                        ", " + composition.FactoryName + ")"
                    : composition.FactoryName + "()";
                currentPostLines.Add(
                    "global::System.Action " + composition.ExecutionName + " = () => " + startCall + ";");
            }

            foreach (ResolvedMotionCancelCommand command in resolved.CancelCommands)
            {
                string call = "global::Cerneala.UI.Markup.GeneratedMarkup.CancelMotionExecution(" +
                    sessionName + ", " + Literal(command.HandleName) + ")";
                currentPostLines.Add("global::System.Action " + command.ActionName + " = () => " + call + ";");
            }

            foreach (ResolvedMotionEventTrigger trigger in resolved.EventTriggers)
            {
                string handlerName = "motionEventHandler" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                INamedTypeSymbol delegateType = (INamedTypeSymbol)trigger.EventSymbol.Type;
                IMethodSymbol invoke = delegateType.DelegateInvokeMethod!;
                string parameters = string.Join(", ", invoke.Parameters.Select((_, index) => "eventArg" + index.ToString(CultureInfo.InvariantCulture)));
                string calls = string.Join(" ", trigger.ExecutionNames.Select(name => name + "();"));
                currentPostLines.Add(
                    delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " " + handlerName +
                    " = (" + parameters + ") => { " + calls + " };");

                currentPostLines.Add(
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AddMotionTrigger(" + sessionName +
                    ", () => " + variable + "." + trigger.EventSymbol.Name + " += " + handlerName +
                    ", () => " + variable + "." + trigger.EventSymbol.Name + " -= " + handlerName + ");");
            }

            EmitMotionScrolls(element, variable, aspect, sessionName);
            EmitMotionDrag(element, variable, aspect, sessionName);
            EmitMotionGesturePress(element, variable, aspect, sessionName);
        }

        private void EmitMotionScrolls(MarkupElement element, string variable, AspectResource aspect, string sessionName)
        {
            if (!resolvedMotionScrolls.TryGetValue((aspect, element), out IReadOnlyList<ResolvedMotionScroll>? scrolls))
            {
                return;
            }

            foreach (ResolvedMotionScroll scroll in scrolls)
            {
                string id = nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                string timelineName = "motionScrollTimeline" + id;
                string handlerName = "motionScrollHandler" + id;
                string sourceCode = ReferenceEquals(scroll.SourceElement, element)
                    ? variable
                    : CreateIdentifier(scroll.SourceElement.Attribute("Name")!.Value);
                currentPostLines.Add("global::Cerneala.UI.Motion.Input.ScrollTimeline? " + timelineName + " = null;");
                currentPostLines.Add(
                    "global::System.EventHandler<global::Cerneala.UI.Controls.ScrollChangedEventArgs> " + handlerName +
                    " = (sender, args) => " + timelineName + "?.Update();");

                List<string> attachLines = [timelineName + " = global::Cerneala.UI.Motion.MotionExtensions.Motion(" + sourceCode + ").ScrollTimeline();", timelineName + ".Update();"];
                List<string> detachLines = [sourceCode + ".ScrollChanged -= " + handlerName + ";"];
                for (int index = 0; index < scroll.Properties.Count; index++)
                {
                    ResolvedMotionScrollProperty property = scroll.Properties[index];
                    string bindingName = "motionScrollBinding" + id + "_" + index.ToString(CultureInfo.InvariantCulture);
                    currentPostLines.Add("global::Cerneala.UI.Motion.Input.ScrollMotionBinding<float>? " + bindingName + " = null;");
                    string progress = scroll.Syntax.Axis == MotionScrollAxis.Vertical ? "Progress" : "HorizontalProgress";
                    string mapping = timelineName + "." + progress + ".Map(" +
                        property.Syntax.From.ToString("R", CultureInfo.InvariantCulture) + "f, " +
                        property.Syntax.To.ToString("R", CultureInfo.InvariantCulture) + "f)" +
                        (scroll.Syntax.AllowLayout ? ".AllowLayout()" : string.Empty);
                    string targetCode = EmitMotionTargetCode(property.Target, variable);
                    attachLines.Add(bindingName + " = " + mapping + ";");
                    attachLines.Add("global::Cerneala.UI.Motion.MotionExtensions.Motion(" + targetCode + ").Animate(" + property.Property.PropertyCode + ").Bind(" + bindingName + ");");
                    detachLines.Add(bindingName + "?.Dispose();");
                    detachLines.Add(bindingName + " = null;");
                }

                attachLines.Add(sourceCode + ".ScrollChanged += " + handlerName + ";");
                detachLines.Add(timelineName + " = null;");
                currentPostLines.Add(
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AddMotionTrigger(" + sessionName +
                    ", () => { " + string.Join(" ", attachLines) + " }, () => { " + string.Join(" ", detachLines) + " });");
            }
        }

        private void EmitMotionDrag(MarkupElement element, string variable, AspectResource aspect, string sessionName)
        {
            if (!resolvedMotionDrags.TryGetValue((aspect, element), out ResolvedMotionDrag? drag))
            {
                return;
            }

            string id = nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            string controllerName = "motionDragController" + id;
            string nowName = "motionDragNow" + id;
            string downHandler = "motionDragDownHandler" + id;
            string moveHandler = "motionDragMoveHandler" + id;
            string upHandler = "motionDragUpHandler" + id;
            string captureLostHandler = "motionDragCaptureLostHandler" + id;
            currentPostLines.Add("global::Cerneala.UI.Motion.Input.DragMotionController? " + controllerName + " = null;");
            currentPostLines.Add(
                "global::System.Func<global::System.TimeSpan> " + nowName +
                " = () => global::System.TimeSpan.FromSeconds((double)global::System.Diagnostics.Stopwatch.GetTimestamp() / global::System.Diagnostics.Stopwatch.Frequency);");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + downHandler +
                " = (sender, args) => { if (" + controllerName + " is not null && args is global::Cerneala.UI.Input.MouseButtonEventArgs mouseArgs) " +
                controllerName + ".Begin(mouseArgs.X, mouseArgs.Y, " + nowName + "()); };");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + moveHandler +
                " = (sender, args) => { if (" + controllerName + "?.State == global::Cerneala.UI.Motion.Input.PointerMotionState.Dragging && args is global::Cerneala.UI.Input.MouseEventArgs mouseArgs) " +
                controllerName + ".Move(mouseArgs.X, mouseArgs.Y, " + nowName + "()); };");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + upHandler +
                " = (sender, args) => { if (" + controllerName + "?.State == global::Cerneala.UI.Motion.Input.PointerMotionState.Dragging) " +
                controllerName + ".End(" + drag.ReleaseSpec + "); };");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + captureLostHandler +
                " = (sender, args) => { if (" + controllerName + "?.State == global::Cerneala.UI.Motion.Input.PointerMotionState.Dragging) " +
                controllerName + ".PointerCaptureLost(" + drag.ReleaseSpec + "); };");

            string attach = controllerName + " = global::Cerneala.UI.Motion.MotionExtensions.Motion(" + variable + ").Drag(); " +
                variable + ".MouseLeftButtonDown += " + downHandler + "; " +
                variable + ".MouseMove += " + moveHandler + "; " +
                variable + ".MouseLeftButtonUp += " + upHandler + "; " +
                variable + ".LostMouseCapture += " + captureLostHandler + ";";
            string detach = variable + ".MouseLeftButtonDown -= " + downHandler + "; " +
                variable + ".MouseMove -= " + moveHandler + "; " +
                variable + ".MouseLeftButtonUp -= " + upHandler + "; " +
                variable + ".LostMouseCapture -= " + captureLostHandler + "; " +
                controllerName + "?.Dispose(); " + controllerName + " = null;";
            currentPostLines.Add(
                "global::Cerneala.UI.Markup.GeneratedMarkup.AddMotionTrigger(" + sessionName +
                ", () => { " + attach + " }, () => { " + detach + " });");
        }

        private void EmitMotionGesturePress(MarkupElement element, string variable, AspectResource aspect, string sessionName)
        {
            if (!resolvedMotionGesturePresses.TryGetValue((aspect, element), out ResolvedMotionGesturePress? gesture))
            {
                return;
            }

            string id = nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            string controllerName = "motionGestureController" + id;
            string downHandler = "motionGestureDownHandler" + id;
            string upHandler = "motionGestureUpHandler" + id;
            string captureLostHandler = "motionGestureCaptureLostHandler" + id;
            currentPostLines.Add("global::Cerneala.UI.Motion.Input.GestureMotionController? " + controllerName + " = null;");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + downHandler +
                " = (sender, args) => { if (" + controllerName + " is not null && " + controllerName +
                ".State != global::Cerneala.UI.Motion.Input.PointerMotionState.Pressed) " + controllerName +
                ".PointerPressed(" + gesture.Spec + "); };");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + upHandler +
                " = (sender, args) => { if (" + controllerName + "?.State == global::Cerneala.UI.Motion.Input.PointerMotionState.Pressed) " +
                controllerName + ".PointerReleased(" + gesture.Spec + "); };");
            currentPostLines.Add(
                "global::Cerneala.UI.Input.RoutedEventHandler " + captureLostHandler +
                " = (sender, args) => { if (" + controllerName + "?.State == global::Cerneala.UI.Motion.Input.PointerMotionState.Pressed) " +
                controllerName + ".PointerReleased(" + gesture.Spec + "); };");

            string attach = controllerName + " = global::Cerneala.UI.Motion.MotionExtensions.Motion(" + variable + ").Gestures(); " +
                variable + ".MouseLeftButtonDown += " + downHandler + "; " +
                variable + ".MouseLeftButtonUp += " + upHandler + "; " +
                variable + ".LostMouseCapture += " + captureLostHandler + ";";
            string detach = variable + ".MouseLeftButtonDown -= " + downHandler + "; " +
                variable + ".MouseLeftButtonUp -= " + upHandler + "; " +
                variable + ".LostMouseCapture -= " + captureLostHandler + "; " +
                controllerName + "?.Dispose(); " + controllerName + " = null;";
            currentPostLines.Add(
                "global::Cerneala.UI.Markup.GeneratedMarkup.AddMotionTrigger(" + sessionName +
                ", () => { " + attach + " }, () => { " + detach + " });");
        }

        private void EmitMotionPresence(MarkupElement element, string variable, AspectResource aspect)
        {
            if (!resolvedMotionPresences.TryGetValue((aspect, element), out ResolvedMotionPresence? presence))
            {
                return;
            }

            currentLines.Add(
                "if (" + variable + ".IsAttached) throw new global::System.InvalidOperationException(\"@presence must be applied before the element is attached.\");");
            currentLines.Add(
                variable + ".Presence = global::Cerneala.UI.Motion.Presence.PresenceOptions.FadeAndScale(" +
                presence.EnterSpec + ", " + presence.ExitSpec + ", " +
                (presence.ExcludeInputWhileExiting ? "true" : "false") + ");");
        }

        private void EmitMotionLayout(MarkupElement element, string variable, AspectResource aspect)
        {
            if (!resolvedMotionLayouts.TryGetValue((aspect, element), out ResolvedMotionLayout? layout))
            {
                return;
            }

            currentLines.Add(
                "if (" + variable + ".IsAttached) throw new global::System.InvalidOperationException(\"@layout must be applied before the element is attached.\");");
            currentLines.Add(variable + ".LayoutMotionId = " + layout.IdExpression + ";");
            currentLines.Add(
                variable + ".LayoutMotion = global::Cerneala.UI.Motion.Layout.LayoutMotionOptions.Spring(" + layout.Spec + ");");
        }

        private void EmitMotionStaggerActivation(ResolvedMotionAnimation animation, string sessionName)
        {
            string suffix = nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            string snapshotName = "motionStaggerItems" + suffix;
            string staggerName = "motionStagger" + suffix;
            string factoriesName = "motionStaggerFactories" + suffix;
            string indexName = "motionStaggerIndex" + suffix;
            string itemName = "motionStaggerItem" + suffix;
            string delayName = "motionStaggerDelay" + suffix;
            string collectionName = CreateIdentifier(animation.StaggerTarget!.Attribute("Name")!.Value);
            List<string> starts = [];

            foreach (ResolvedMotionProperty property in animation.Properties)
            {
                string typeCode = GetMotionTypeCode(property.Property.ValueType);
                bool hasFrom = property.Source is not null && property.Source.Value is not MotionCurrentValueSyntax;
                string fromCode = hasFrom
                    ? EmitMotionValue(property.Source!.Value, property.Property, itemName, animation.Parameters)
                    : "default(" + typeCode + ")!";
                bool toCurrent = property.Destination.Value is MotionCurrentValueSyntax;
                bool hasBinding = TryEmitMotionBinding(
                    property.Destination.Value,
                    property.Property,
                    property.Target,
                    itemName,
                    out string observationCode,
                    out string bindingModeCode,
                    out string projectionCode);
                string toCode = toCurrent || hasBinding
                    ? "default(" + typeCode + ")!"
                    : EmitMotionValue(property.Destination.Value, property.Property, itemName, animation.Parameters);
                string tweenCode = "((global::Cerneala.UI.Motion.Specs.TweenSpec<" + typeCode + ">)" + property.SpecVariable + ")";
                string delayedSpecCode = tweenCode + ".WithDelay(" + tweenCode + ".Delay + " + delayName + ")";
                string optionsCode = EmitMotionOptions(animation.Syntax.Options, animation.Parameters);
                string start = hasBinding
                    ? "global::Cerneala.UI.Markup.GeneratedMarkup.StartBoundMotionProperty(" + sessionName + ", " +
                        itemName + ", " + property.Property.PropertyCode + ", " +
                        (hasFrom ? "true" : "false") + ", " + fromCode + ", " +
                        observationCode + ", " + bindingModeCode + ", " + projectionCode + ", " +
                        delayedSpecCode + ", " + optionsCode + ")"
                    : "global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionProperty(" + sessionName + ", " +
                        itemName + ", " + property.Property.PropertyCode + ", " +
                        (hasFrom ? "true" : "false") + ", " + fromCode + ", " +
                        (toCurrent ? "true" : "false") + ", " + toCode + ", " + delayedSpecCode + ", " + optionsCode + ")";
                starts.Add("() => global::Cerneala.UI.Markup.MarkupMotionExecution.From(" + start + ")");
            }

            string factoryBody =
                "() => { " +
                "global::System.Collections.Generic.List<global::Cerneala.UI.Elements.UIElement> " + snapshotName +
                " = new global::System.Collections.Generic.List<global::Cerneala.UI.Elements.UIElement>(" + collectionName + ".VisualChildren); " +
                "global::Cerneala.UI.Motion.Core.MotionStagger " + staggerName +
                " = new global::Cerneala.UI.Motion.Core.MotionStagger(" + BuildDurationExpression(animation.Stagger!.Each) + "); " +
                "global::System.Collections.Generic.List<global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution>> " + factoriesName +
                " = new global::System.Collections.Generic.List<global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution>>(" + snapshotName + ".Count); " +
                "for (int " + indexName + " = 0; " + indexName + " < " + snapshotName + ".Count; " + indexName + "++) { " +
                "global::Cerneala.UI.Elements.UIElement " + itemName + " = " + snapshotName + "[" + indexName + "]; " +
                "global::System.TimeSpan " + delayName + " = " + staggerName + ".GetDelay(" + indexName + "); " +
                factoriesName + ".Add(() => global::Cerneala.UI.Markup.MarkupMotionExecution.Parallel(" + string.Join(", ", starts) + ")); } " +
                "return global::Cerneala.UI.Markup.MarkupMotionExecution.Parallel(" + factoriesName + ".ToArray()); }";
            currentPostLines.Add(
                "global::System.Func<global::Cerneala.UI.Markup.MarkupMotionExecution> " + animation.FactoryName + " = " + factoryBody + ";");
            currentPostLines.Add(
                "global::System.Action " + animation.ExecutionName +
                " = () => global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionExecution(" + sessionName +
                ", " + animation.FactoryName + ");");
        }

        private string EmitMotionValue(
            MotionValueSyntax value,
            PropertySpec property,
            string targetCode,
            MotionClipInvocationContext? parameters)
        {
            if (value is MotionConditionalValueSyntax conditional)
            {
                return "(" + EmitMotionCondition(conditional.Condition, targetCode) + " ? " +
                    EmitMotionValue(conditional.WhenTrue, property, targetCode, parameters) + " : " +
                    EmitMotionValue(conditional.WhenFalse, property, targetCode, parameters) + ")";
            }

            if (value is MotionCurrentValueSyntax)
            {
                return "default(" + GetMotionTypeCode(property.ValueType) + ")!";
            }

            MotionAtomValueSyntax atom = (MotionAtomValueSyntax)value;
            if (parameters is not null && parameters.Values.TryGetValue(atom.Text.Trim(), out ResolvedMotionParameterValue? parameter))
            {
                return parameter.ValueCode!;
            }

            GeneratedExpression? expression = ParseDirectiveValue(
                null,
                property.Name,
                atom.Text,
                property,
                atom.Location.Source,
                targetCode);
            if (expression is null)
            {
                return "default(" + GetMotionTypeCode(property.ValueType) + ")!";
            }

            return property.ValueKind == MarkupValueKind.Integer
                ? "(" + GetMotionTypeCode(property.ValueType) + ")(" + expression.Code + ")"
                : expression.Code;
        }

        private bool TryEmitMotionBinding(
            MotionValueSyntax value,
            PropertySpec property,
            ResolvedMotionTarget target,
            string targetCode,
            out string observationCode,
            out string modeCode,
            out string projectionCode)
        {
            observationCode = string.Empty;
            modeCode = string.Empty;
            projectionCode = string.Empty;
            if (value is not MotionAtomValueSyntax atom)
            {
                return false;
            }

            string text = atom.Text.Trim();
            BindingTokenParseResult tokenResult = ParseBindingToken(text, 0);
            MarkupBindingToken? token = tokenResult.Token;
            if (token is null || tokenResult.Length != text.Length || token.ModeOffset < 0)
            {
                return false;
            }

            BindingResolutionContext context = new(
                targetCode,
                target.Element.Name.LocalName,
                ReferenceEquals(target.Element, document.Root),
                templateEmissionContexts.Count == 0 ? null : templateEmissionContexts.Peek(),
                validateClrObservability: true);
            BindingSourceDescriptor? source = ResolveBindingSource(
                context,
                token.Path,
                atom.Location.Source,
                atom.Location.Source);
            if (source is null || !ValidateBindingCompatibility(source, property, token.Mode, context, atom.Location.Source))
            {
                return false;
            }

            observationCode = BuildObservationExpression(source);
            modeCode = BindingModeCode(token.Mode);
            projectionCode = ProjectionCode(source, property);
            return true;
        }

        private static string EmitMotionCondition(DirectiveExpression expression, string targetCode)
        {
            return expression switch
            {
                DirectiveSourceExpression source => source.Text.StartsWith("$self.", StringComparison.Ordinal)
                    ? targetCode + "." + source.Text.Substring(6)
                    : targetCode + "." + source.Text,
                DirectiveLiteralExpression literal => literal.Text,
                DirectiveGroupExpression group => "(" + EmitMotionCondition(group.Inner, targetCode) + ")",
                DirectiveComparisonExpression comparison =>
                    EmitMotionCondition(comparison.Left, targetCode) + " " + comparison.Comparator + " " +
                    EmitMotionCondition(comparison.Right, targetCode),
                DirectiveLogicalExpression logical =>
                    "(" + EmitMotionCondition(logical.Left, targetCode) +
                    (logical.Operator == DirectiveLogicalOperator.And ? " && " : " || ") +
                    EmitMotionCondition(logical.Right, targetCode) + ")",
                _ => "false"
            };
        }

        private static string EmitMotionOptions(
            IReadOnlyList<MotionOptionSyntax> options,
            MotionClipInvocationContext? parameters)
        {
            string retarget = ResolveMotionOptionValue(options, "retarget", parameters, "Restart", useCode: false);
            string hold = ResolveMotionOptionValue(options, "holdOnComplete", parameters, "true", useCode: true);
            string debugName = ResolveMotionOptionValue(options, "debugName", parameters, "null", useCode: true);
            return "new global::Cerneala.UI.Motion.Properties.MotionPropertyStartOptions { RetargetMode = " +
                "global::Cerneala.UI.Motion.Specs.RetargetMode." + retarget +
                ", HoldOnComplete = " + hold + ", DebugName = " + debugName + " }";
        }

        private static string ResolveMotionOptionValue(
            IReadOnlyList<MotionOptionSyntax> options,
            string name,
            MotionClipInvocationContext? parameters,
            string fallback,
            bool useCode)
        {
            if (options.FirstOrDefault(option => option.Name == name)?.Value is not MotionAtomValueSyntax atom)
            {
                return fallback;
            }

            string text = atom.Text.Trim();
            if (parameters is not null && parameters.Values.TryGetValue(text, out ResolvedMotionParameterValue? parameter))
            {
                return useCode ? parameter.ValueCode! : parameter.RawText.Trim('"');
            }

            return name == "holdOnComplete" ? text.ToLowerInvariant() : text;
        }

        private string EmitMotionTargetCode(ResolvedMotionTarget target, string selfVariable)
        {
            if (target.Prism?.ElementCode is string prismElementCode)
            {
                return prismElementCode;
            }

            string ownerCode = target.Kind switch
            {
                ResolvedMotionTargetKind.Self or ResolvedMotionTargetKind.SelfPart => selfVariable,
                ResolvedMotionTargetKind.Named or ResolvedMotionTargetKind.NamedPart => CreateIdentifier(target.OwnerName!),
                ResolvedMotionTargetKind.Owner or ResolvedMotionTargetKind.OwnerPart => templateEmissionContexts.Peek().OwnerVariable,
                _ => throw new InvalidOperationException("Unsupported resolved Motion target.")
            };
            if (target.Kind is not (ResolvedMotionTargetKind.SelfPart or ResolvedMotionTargetKind.NamedPart or ResolvedMotionTargetKind.OwnerPart))
            {
                return ownerCode;
            }

            INamedTypeSymbol partType = ResolveElementTypeSymbol(target.Element.Name.LocalName)!;
            string partTypeCode = partType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return "((" + partTypeCode + ")" + ownerCode + ".ComponentTemplateInstance!.Parts[" + Literal(target.PartName!) + "])";
        }

    }
}
