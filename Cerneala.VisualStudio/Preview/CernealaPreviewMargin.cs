namespace Cerneala.VisualStudio.Preview;

using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

internal sealed class CernealaPreviewMargin : IWpfTextViewMargin
{
    public const string TopMarginName = "Cerneala.LivePreview.Top";
    public const string LeftMarginName = "Cerneala.LivePreview.Left";
    public const string BottomMarginName = "Cerneala.LivePreview.Bottom";

    private readonly FrameworkElement visualElement;
    private readonly IDisposable content;
    private readonly PreviewMarginPlacement placement;
    private bool disposed;

    public CernealaPreviewMargin(
        IWpfTextView textView,
        CernealaPreviewSession session,
        PreviewMarginPlacement placement)
    {
        this.placement = placement;
        content = placement == PreviewMarginPlacement.Bottom
            ? new CernealaPreviewModeBar(session)
            : new CernealaPreviewSurface(textView, session, placement);
        visualElement = (FrameworkElement)content;
    }

    public bool Enabled => !disposed;

    public double MarginSize => placement switch
    {
        PreviewMarginPlacement.Top => visualElement.ActualHeight,
        PreviewMarginPlacement.Left => visualElement.ActualWidth,
        _ => visualElement.ActualHeight
    };

    public FrameworkElement VisualElement
    {
        get
        {
            ThrowIfDisposed();
            return visualElement;
        }
    }

    public ITextViewMargin? GetTextViewMargin(string marginName)
    {
        ThrowIfDisposed();
        string ownName = placement switch
        {
            PreviewMarginPlacement.Top => TopMarginName,
            PreviewMarginPlacement.Left => LeftMarginName,
            _ => BottomMarginName
        };
        return string.Equals(marginName, ownName, StringComparison.OrdinalIgnoreCase) ? this : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        content.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CernealaPreviewMargin));
        }
    }
}

internal enum PreviewMarginPlacement
{
    Top,
    Left,
    Bottom
}
