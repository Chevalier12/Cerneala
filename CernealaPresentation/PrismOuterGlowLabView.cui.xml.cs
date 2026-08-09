using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Presentation;

public partial class PrismOuterGlowLabView : UserControl
{
    internal static readonly PrismNodeId LayerId = new(1);

    private IDisposable? prismLifetime;

    internal UIElement Target => GlowTarget;

    internal PrismInstance? Instance { get; private set; }

    internal PrismInstance AttachOuterGlow() => Attach(
        new PrismLayerDefinition(
            LayerId,
            "LabTarget",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]),
        "ACTIVE / OUTER GLOW ATTACHED");

    internal PrismInstance AttachMotionBlur() => Attach(
        new PrismLayerDefinition(
            LayerId,
            "LabTarget",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)]),
        "ACTIVE / MOTION BLUR ATTACHED");

    internal void ResetPrism()
    {
        prismLifetime?.Dispose();
        prismLifetime = null;
        Instance = null;
        GlowTarget.Invalidate(InvalidationFlags.Render, "Prism effect lab attachment reset");
        LabStatus.Text = "READY / NO PRISM ATTACHMENT";
    }

    protected override void OnDetached()
    {
        ResetPrism();
        base.OnDetached();
    }

    private PrismInstance Attach(PrismLayerDefinition layer, string status)
    {
        if (Instance is not null)
        {
            return Instance;
        }

        Instance = new PrismInstance(new PrismCompositionDefinition("PrismEffectLab", [layer]));
        prismLifetime = GeneratedMarkup.AttachPrism(GlowTarget, () => Instance);
        GlowTarget.Invalidate(InvalidationFlags.Render, "Prism effect lab attachment changed");
        LabStatus.Text = status;
        return Instance;
    }
}
