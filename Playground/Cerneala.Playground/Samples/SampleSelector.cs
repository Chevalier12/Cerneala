#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.Playground.Samples.UserControlShowcase;
using GridLength = Cerneala.UI.Layout.Panels.GridLength;
using LayoutGrid = Cerneala.UI.Layout.Panels.Grid;
using PanelOrientation = Cerneala.UI.Layout.Orientation;
using RowDefinition = Cerneala.UI.Layout.Panels.RowDefinition;

namespace Cerneala.Playground.Samples;

public interface IPlaygroundSample
{
    string Name { get; }

    UIElement Build();
}

public sealed class SampleSelector
{
    private readonly ContentControl activeSampleHost;
    private readonly PlaygroundText text;
    private readonly InvalidationStatsOverlay statsOverlay;
    private readonly List<IPlaygroundSample> samples;
    private readonly ReadOnlyCollection<IPlaygroundSample> readOnlySamples;

    public SampleSelector(IEnumerable<IPlaygroundSample> samples, IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        ArgumentNullException.ThrowIfNull(samples);

        this.samples = samples.ToList();
        if (this.samples.Count == 0)
        {
            throw new ArgumentException("At least one playground sample is required.", nameof(samples));
        }

        readOnlySamples = this.samples.AsReadOnly();
        text = new PlaygroundText(resourceProvider, fontResourceId);
        activeSampleHost = new ContentControl();
        statsOverlay = new InvalidationStatsOverlay(resourceProvider, fontResourceId);
        Root = BuildRoot();
        SelectSample(0);
    }

    public static SampleSelector CreateDefault(
        IResourceProvider? resourceProvider = null,
        ResourceId<FontResource>? fontResourceId = null,
        ResourceId<ImageResource>? imageResourceId = null)
    {
        return new SampleSelector(new IPlaygroundSample[]
        {
            new RetainedAppSample(resourceProvider, fontResourceId, imageResourceId),
            new RetainedButtonSample(resourceProvider, fontResourceId),
            new LayoutSample(resourceProvider, fontResourceId),
            new TextSample(resourceProvider, fontResourceId),
            new DiagnosticsSample(resourceProvider, fontResourceId),
            new RuntimePreviewSample(resourceProvider, fontResourceId, imageResourceId),
            new AuthoringAppSample(resourceProvider, fontResourceId),
            new GettingStartedSample(resourceProvider, fontResourceId),
            new MotionSample(resourceProvider, fontResourceId),
            new LayoutMotionSample(resourceProvider, fontResourceId),
            new PresenceMotionSample(resourceProvider, fontResourceId),
            new ScrollMotionSample(resourceProvider, fontResourceId),
            new ModernAspectSample(resourceProvider, fontResourceId),
            new UserControlMarkupSample(),
            new AspectMotionSample(resourceProvider, fontResourceId)
        }, resourceProvider, fontResourceId);
    }

    public IReadOnlyList<IPlaygroundSample> Samples => readOnlySamples;

    public int ActiveIndex { get; private set; }

    public IPlaygroundSample ActiveSample => samples[ActiveIndex];

    public UIElement Root { get; }

    public UIElement? ActiveElement => activeSampleHost.Content as UIElement;

    public string StatsText => statsOverlay.Text;

    public void SelectSample(int index)
    {
        if (index < 0 || index >= samples.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Sample index must refer to an existing sample.");
        }

        ActiveIndex = index;
        activeSampleHost.Content = samples[index].Build();
        activeSampleHost.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Measure | Cerneala.UI.Invalidation.InvalidationFlags.Render, "Playground sample changed");
    }

    public void UpdateFrame(UiFrame? frame)
    {
        statsOverlay.Update(frame);
    }

    private LayoutGrid BuildRoot()
    {
        LayoutGrid root = new()
        {
            Margin = new Thickness(24, 20, 24, 0)
        };
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));

        StackPanel header = BuildHeader();
        LayoutGrid.SetRow(statsOverlay.Root, 1);
        LayoutGrid.SetRow(activeSampleHost, 2);

        root.VisualChildren.Add(header);
        root.VisualChildren.Add(statsOverlay.Root);
        root.VisualChildren.Add(activeSampleHost);
        return root;
    }

    private StackPanel BuildHeader()
    {
        StackPanel header = new()
        {
            Margin = new Thickness(32, 24, 32, 0),
            Orientation = PanelOrientation.Horizontal
        };

        for (int index = 0; index < samples.Count; index++)
        {
            int capturedIndex = index;
            header.VisualChildren.Add(new Button
            {
                Content = text.Create(samples[index].Name, 14, new DrawColor(28, 35, 48)),
                Padding = new Thickness(12, 8, 12, 8),
                Command = new ActionCommand(_ => SelectSample(capturedIndex))
            });
        }

        return header;
    }
}
