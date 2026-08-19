namespace Cerneala.VisualStudio.Preview;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(IWpfTextViewMarginProvider))]
[Name(CernealaPreviewMargin.TopMarginName)]
[MarginContainer(PredefinedMarginNames.Top)]
[Order(After = PredefinedMarginNames.OuterTextViewTopBoundaryMargin)]
[ContentType(CernealaContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class CernealaPreviewTopMarginProvider : IWpfTextViewMarginProvider
{
    private readonly ITextDocumentFactoryService documentFactory;

    [ImportingConstructor]
    public CernealaPreviewTopMarginProvider(ITextDocumentFactoryService documentFactory)
    {
        this.documentFactory = documentFactory;
    }

    public IWpfTextViewMargin CreateMargin(
        IWpfTextViewHost wpfTextViewHost,
        IWpfTextViewMargin marginContainer)
    {
        CernealaPreviewSession session = GetSession(wpfTextViewHost.TextView, documentFactory);
        return new CernealaPreviewMargin(
            wpfTextViewHost.TextView,
            session,
            PreviewMarginPlacement.Top);
    }

    internal static CernealaPreviewSession GetSession(
        IWpfTextView textView,
        ITextDocumentFactoryService documentFactory) =>
        textView.Properties.GetOrCreateSingletonProperty(
            () => new CernealaPreviewSession(textView, documentFactory));
}

[Export(typeof(IWpfTextViewMarginProvider))]
[Name(CernealaPreviewMargin.LeftMarginName)]
[MarginContainer(PredefinedMarginNames.Left)]
[Order(Before = PredefinedMarginNames.LineNumber)]
[ContentType(CernealaContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class CernealaPreviewLeftMarginProvider : IWpfTextViewMarginProvider
{
    private readonly ITextDocumentFactoryService documentFactory;

    [ImportingConstructor]
    public CernealaPreviewLeftMarginProvider(ITextDocumentFactoryService documentFactory)
    {
        this.documentFactory = documentFactory;
    }

    public IWpfTextViewMargin CreateMargin(
        IWpfTextViewHost wpfTextViewHost,
        IWpfTextViewMargin marginContainer)
    {
        CernealaPreviewSession session = CernealaPreviewTopMarginProvider.GetSession(
            wpfTextViewHost.TextView,
            documentFactory);
        return new CernealaPreviewMargin(
            wpfTextViewHost.TextView,
            session,
            PreviewMarginPlacement.Left);
    }
}

[Export(typeof(IWpfTextViewMarginProvider))]
[Name(CernealaPreviewMargin.BottomMarginName)]
[MarginContainer(PredefinedMarginNames.Bottom)]
[Order(Before = PredefinedMarginNames.HorizontalScrollBar)]
[ContentType(CernealaContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class CernealaPreviewBottomMarginProvider : IWpfTextViewMarginProvider
{
    private readonly ITextDocumentFactoryService documentFactory;

    [ImportingConstructor]
    public CernealaPreviewBottomMarginProvider(ITextDocumentFactoryService documentFactory)
    {
        this.documentFactory = documentFactory;
    }

    public IWpfTextViewMargin CreateMargin(
        IWpfTextViewHost wpfTextViewHost,
        IWpfTextViewMargin marginContainer)
    {
        CernealaPreviewSession session = CernealaPreviewTopMarginProvider.GetSession(
            wpfTextViewHost.TextView,
            documentFactory);
        return new CernealaPreviewMargin(
            wpfTextViewHost.TextView,
            session,
            PreviewMarginPlacement.Bottom);
    }
}
