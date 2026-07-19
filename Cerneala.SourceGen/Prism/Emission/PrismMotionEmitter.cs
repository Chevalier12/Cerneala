using System;
using System.Globalization;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private string EmitPrismMotionStart(
            string sessionName,
            ResolvedMotionProperty property,
            string targetCode,
            bool hasFrom,
            string fromCode,
            bool toCurrent,
            string toCode,
            string specCode,
            string optionsCode)
        {
            ResolvedPrismMotionTarget prism = property.Target.Prism
                ?? throw new InvalidOperationException(
                    "A Prism Motion start requires a resolved Prism target.");
            return
                "global::Cerneala.UI.Markup.GeneratedMarkup.StartPrismMotionProperty(" +
                sessionName + ", " +
                targetCode + ", " +
                prism.PropertyId.ToString(CultureInfo.InvariantCulture) + ", " +
                EmitPrismMotionGetter(prism) + ", " +
                EmitPrismMotionSetter(prism) + ", " +
                (prism.IsDiscrete ? "true" : "false") + ", " +
                (hasFrom ? "true" : "false") + ", " +
                fromCode + ", " +
                (toCurrent ? "true" : "false") + ", " +
                toCode + ", " +
                specCode + ", " +
                optionsCode + ")";
        }

        private string EmitPrismMotionSet(
            ResolvedMotionSetProperty property,
            string targetCode,
            string valueCode)
        {
            ResolvedPrismMotionTarget prism = property.Target.Prism
                ?? throw new InvalidOperationException(
                    "A Prism Motion set requires a resolved Prism target.");
            return
                "global::Cerneala.UI.Markup.GeneratedMarkup.SetPrismMotionProperty(" +
                targetCode + ", " +
                EmitPrismMotionGetter(prism) + ", " +
                EmitPrismMotionSetter(prism) + ", " +
                valueCode + ");";
        }

        private string EmitPrismMotionGetter(
            ResolvedPrismMotionTarget target)
        {
            PrismMotionAccessor accessor = target.Accessors[0];
            string read = EmitPrismMotionAccessorRead(
                "prismInstance",
                accessor);
            if (target.ValueType == BoundPrismValueType.Integer &&
                accessor.StorageType == BoundPrismValueType.Number)
            {
                read = "(int)(" + read + ")";
            }

            return "static prismInstance => " + read;
        }

        private string EmitPrismMotionSetter(
            ResolvedPrismMotionTarget target)
        {
            string body = string.Empty;
            foreach (PrismMotionAccessor accessor in target.Accessors)
            {
                string value = "prismValue";
                if (target.ValueType == BoundPrismValueType.Integer &&
                    accessor.StorageType == BoundPrismValueType.Number)
                {
                    value = "(float)prismValue";
                }

                body += EmitPrismMotionAccessorWrite(
                    "prismInstance",
                    accessor,
                    value) + " ";
            }

            return "static (prismInstance, prismValue) => { " + body + "}";
        }

        private string EmitPrismMotionAccessorRead(
            string instanceCode,
            PrismMotionAccessor accessor)
        {
            string state = EmitPrismMotionState(instanceCode, accessor);
            if (!accessor.UsesCatalogKey)
            {
                return state + "." + accessor.Schema.Name;
            }

            return
                "global::Cerneala.UI.Markup.GeneratedMarkup.GetPrism" +
                PrismMotionOperationName(accessor.Kind) +
                PrismMotionStorageName(accessor.StorageType) +
                "(" + state + ", " +
                accessor.EntryStableId.ToString(CultureInfo.InvariantCulture) +
                ", " +
                accessor.Slot.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private string EmitPrismMotionAccessorWrite(
            string instanceCode,
            PrismMotionAccessor accessor,
            string valueCode)
        {
            string state = EmitPrismMotionState(instanceCode, accessor);
            if (!accessor.UsesCatalogKey)
            {
                return state + "." + accessor.Schema.Name + " = " + valueCode + ";";
            }

            return
                "global::Cerneala.UI.Markup.GeneratedMarkup.SetPrism" +
                PrismMotionOperationName(accessor.Kind) +
                PrismMotionStorageName(accessor.StorageType) +
                "(" + state + ", " +
                accessor.EntryStableId.ToString(CultureInfo.InvariantCulture) +
                ", " +
                accessor.Slot.ToString(CultureInfo.InvariantCulture) +
                ", " + valueCode + ");";
        }

        private static string EmitPrismMotionState(
            string instanceCode,
            PrismMotionAccessor accessor)
        {
            string node = instanceCode + "." + (accessor.Node.Kind switch
            {
                PrismContainerKind.Layer =>
                    "GetLayerState(" + PrismMotionNodeId(accessor.Node.Id) + ")",
                PrismContainerKind.Group =>
                    "GetGroupState(" + PrismMotionNodeId(accessor.Node.Id) + ")",
                PrismContainerKind.Backdrop =>
                    "GetBackdropState(" + PrismMotionNodeId(accessor.Node.Id) + ")",
                _ => throw new InvalidOperationException(
                    "Unsupported Prism Motion node kind.")
            });
            return accessor.Kind switch
            {
                PrismMotionAccessorKind.Node => node,
                PrismMotionAccessorKind.Filter =>
                    node + ".Filters[" +
                    accessor.OperationIndex.ToString(CultureInfo.InvariantCulture) +
                    "]",
                PrismMotionAccessorKind.Style =>
                    node + ".Styles[" +
                    accessor.OperationIndex.ToString(CultureInfo.InvariantCulture) +
                    "]",
                PrismMotionAccessorKind.Mask => node + ".Mask!",
                _ => throw new InvalidOperationException(
                    "Unsupported Prism Motion accessor kind.")
            };
        }

        private static string PrismMotionNodeId(int id) =>
            "new global::Cerneala.UI.Prism.Definitions.PrismNodeId(" +
            id.ToString(CultureInfo.InvariantCulture) + ")";

        private static string PrismMotionOperationName(
            PrismMotionAccessorKind kind) =>
            kind switch
            {
                PrismMotionAccessorKind.Filter => "Filter",
                PrismMotionAccessorKind.Style => "Style",
                _ => throw new InvalidOperationException(
                    "Only Prism filters and styles use catalog parameter bridges.")
            };

        private static string PrismMotionStorageName(
            BoundPrismValueType type) =>
            type switch
            {
                BoundPrismValueType.Boolean => "Boolean",
                BoundPrismValueType.Integer => "Integer",
                BoundPrismValueType.Number => "Number",
                BoundPrismValueType.Color => "Color",
                BoundPrismValueType.Vector => "Vector",
                BoundPrismValueType.Symbol => "Integer",
                BoundPrismValueType.Resource => "Resource",
                _ => throw new InvalidOperationException(
                    "Unsupported Prism Motion storage type.")
            };
    }
}
