namespace Cerneala.VisualStudio.Preview;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.VisualStudio.Text.Editor;

internal sealed class CernealaPreviewSurface : Grid, IDisposable
{
    private const double SplitterSize = 5;
    private const double ToolbarHeight = 31;
    private const double LoadingTrackWidth = 220;
    private const double LoadingSweepWidth = 64;
    private static readonly Brush DesignerSelectionBrush = new SolidColorBrush(Color.FromRgb(86, 156, 255));
    private readonly IWpfTextView textView;
    private readonly CernealaPreviewSession session;
    private readonly PreviewMarginPlacement placement;
    private readonly Border designerFrame = new();
    private readonly ScrollViewer scroller = new();
    private readonly Grid artboard = new();
    private readonly Image previewImage = new();
    private readonly Grid loadingOverlay = new();
    private readonly Border loadingSweep = new();
    private readonly TranslateTransform loadingSweepTransform = new();
    private readonly TextBlock error = new();
    private readonly TextBlock viewportSize = new();
    private readonly TextBox widthInput = new();
    private readonly TextBox heightInput = new();
    private readonly ComboBox zoomInput = new();
    private readonly ComboBox refreshRateInput = new();
    private double totalExtent = 760;
    private double effectiveZoom = 1;
    private Point? panOrigin;
    private double panHorizontalOffset;
    private double panVerticalOffset;
    private bool releasingMouseCapture;
    private bool loadingAnimationRunning;
    private bool disposed;

    public CernealaPreviewSurface(
        IWpfTextView textView,
        CernealaPreviewSession session,
        PreviewMarginPlacement placement)
    {
        this.textView = textView;
        this.session = session;
        this.placement = placement;

        Background = CernealaPreviewChrome.SurfaceBrush;
        BuildVisualTree();
        session.Changed += OnSessionChanged;
        textView.VisualElement.SizeChanged += OnEditorSizeChanged;
        Loaded += OnLoaded;
        ApplySessionState();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        StopLoadingAnimation();
        session.Changed -= OnSessionChanged;
        textView.VisualElement.SizeChanged -= OnEditorSizeChanged;
        Loaded -= OnLoaded;
    }

    private void BuildVisualTree()
    {
        designerFrame.Child = BuildDesignerFrame();
        if (placement == PreviewMarginPlacement.Top)
        {
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(SplitterSize) });
            Children.Add(designerFrame);
            GridSplitter splitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
            splitter.DragDelta += (_, args) =>
                session.SetHorizontalExtent(Math.Max(220, ActualHeight + args.VerticalChange));
            SetRow(splitter, 1);
            Children.Add(splitter);
        }
        else
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SplitterSize) });
            Children.Add(designerFrame);
            GridSplitter splitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
            splitter.DragDelta += (_, args) =>
                session.SetVerticalExtent(Math.Max(320, ActualWidth + args.HorizontalChange));
            SetColumn(splitter, 1);
            Children.Add(splitter);
        }
    }

    private UIElement BuildDesignerFrame()
    {
        designerFrame.Background = CernealaPreviewChrome.SurfaceBrush;
        designerFrame.BorderBrush = CernealaPreviewChrome.BorderBrush;
        designerFrame.BorderThickness = placement == PreviewMarginPlacement.Top
            ? new Thickness(0, 0, 0, 1)
            : new Thickness(0, 0, 1, 0);

        Grid frame = new();
        frame.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        frame.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ToolbarHeight) });
        frame.Children.Add(BuildArtboardSurface());
        UIElement toolbar = BuildArtboardToolbar();
        SetRow(toolbar, 1);
        frame.Children.Add(toolbar);
        return frame;
    }

    private UIElement BuildArtboardSurface()
    {
        Grid surface = new() { Background = CernealaPreviewChrome.Brush(24, 24, 24) };
        scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scroller.CanContentScroll = false;
        scroller.PanningMode = PanningMode.None;
        scroller.Background = CernealaPreviewChrome.Brush(24, 24, 24);
        scroller.Padding = new Thickness(28);
        scroller.SizeChanged += (_, _) => UpdateArtboard();
        scroller.PreviewMouseWheel += OnPreviewMouseWheel;

        artboard.Background = Brushes.Black;
        artboard.HorizontalAlignment = HorizontalAlignment.Center;
        artboard.VerticalAlignment = VerticalAlignment.Center;
        artboard.SnapsToDevicePixels = true;
        previewImage.Effect = new DropShadowEffect
        {
            BlurRadius = 14,
            ShadowDepth = 3,
            Opacity = 0.55,
            Color = Colors.Black
        };

        previewImage.Stretch = Stretch.Fill;
        previewImage.Focusable = true;
        previewImage.SnapsToDevicePixels = true;
        previewImage.MouseDown += OnPreviewMouseDown;
        previewImage.MouseMove += OnPreviewMouseMove;
        previewImage.MouseUp += OnPreviewMouseUp;
        previewImage.MouseLeave += OnPreviewMouseLeave;
        previewImage.LostMouseCapture += OnPreviewLostMouseCapture;
        previewImage.TextInput += OnPreviewTextInput;
        previewImage.KeyDown += OnPreviewKeyDown;
        previewImage.KeyUp += OnPreviewKeyUp;
        previewImage.LostKeyboardFocus += OnPreviewLostKeyboardFocus;
        artboard.Children.Add(previewImage);

        artboard.Children.Add(CreateViewportHandle(
            ViewportResizeAxis.Width,
            Cursors.SizeWE));
        artboard.Children.Add(CreateViewportHandle(
            ViewportResizeAxis.Height,
            Cursors.SizeNS));
        artboard.Children.Add(CreateViewportHandle(
            ViewportResizeAxis.Both,
            Cursors.SizeNWSE));

        Border sizeBadge = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8),
            Padding = new Thickness(7, 3, 7, 3),
            Background = CernealaPreviewChrome.Brush(45, 45, 48),
            BorderBrush = CernealaPreviewChrome.AccentBrush,
            BorderThickness = new Thickness(1),
            Child = viewportSize,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        viewportSize.Foreground = CernealaPreviewChrome.TextBrush;
        viewportSize.FontSize = 11;
        viewportSize.Tag = sizeBadge;
        artboard.Children.Add(sizeBadge);

        scroller.Content = artboard;
        surface.Children.Add(scroller);

        surface.Children.Add(BuildLoadingOverlay());

        error.HorizontalAlignment = HorizontalAlignment.Center;
        error.VerticalAlignment = VerticalAlignment.Center;
        error.Margin = new Thickness(24);
        error.TextWrapping = TextWrapping.Wrap;
        error.TextAlignment = TextAlignment.Center;
        error.Foreground = CernealaPreviewChrome.Brush(244, 135, 113);
        error.MaxWidth = 720;
        error.Visibility = Visibility.Collapsed;
        surface.Children.Add(error);
        return surface;
    }

    private UIElement BuildLoadingOverlay()
    {
        loadingOverlay.Background = CernealaPreviewChrome.Brush(24, 24, 24);
        loadingOverlay.IsHitTestVisible = false;
        loadingOverlay.Visibility = Visibility.Collapsed;

        StackPanel content = new()
        {
            Width = 280,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid accentRail = new()
        {
            Width = 156,
            Height = 3,
            Margin = new Thickness(0, 0, 0, 18),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        accentRail.ColumnDefinitions.Add(new ColumnDefinition());
        accentRail.ColumnDefinitions.Add(new ColumnDefinition());
        accentRail.ColumnDefinitions.Add(new ColumnDefinition());
        AddAccentSegment(accentRail, 0, CernealaPreviewChrome.Brush(65, 221, 235));
        AddAccentSegment(accentRail, 1, CernealaPreviewChrome.Brush(255, 52, 157));
        AddAccentSegment(accentRail, 2, CernealaPreviewChrome.Brush(186, 255, 47));
        content.Children.Add(accentRail);

        content.Children.Add(new TextBlock
        {
            Text = "Loading...",
            Foreground = CernealaPreviewChrome.TextBrush,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = "Might take a while (or not).",
            Foreground = CernealaPreviewChrome.MutedTextBrush,
            FontSize = 12,
            Margin = new Thickness(0, 7, 0, 0),
            TextAlignment = TextAlignment.Center
        });

        Grid track = new()
        {
            Width = LoadingTrackWidth,
            Height = 3,
            Margin = new Thickness(0, 22, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = CernealaPreviewChrome.Brush(45, 45, 48),
            ClipToBounds = true
        };
        loadingSweep.Width = LoadingSweepWidth;
        loadingSweep.Height = 3;
        loadingSweep.HorizontalAlignment = HorizontalAlignment.Left;
        loadingSweep.Background = CernealaPreviewChrome.Brush(65, 221, 235);
        loadingSweep.RenderTransform = loadingSweepTransform;
        track.Children.Add(loadingSweep);
        content.Children.Add(track);

        loadingOverlay.Children.Add(content);
        return loadingOverlay;
    }

    private static void AddAccentSegment(Grid rail, int column, Brush brush)
    {
        Border segment = new() { Background = brush };
        SetColumn(segment, column);
        rail.Children.Add(segment);
    }

    private void SetLoadingState(bool isLoading)
    {
        loadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        if (isLoading)
        {
            StartLoadingAnimation();
        }
        else
        {
            StopLoadingAnimation();
        }
    }

    private void StartLoadingAnimation()
    {
        if (loadingAnimationRunning)
        {
            return;
        }

        loadingAnimationRunning = true;
        loadingSweepTransform.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation
            {
                From = -LoadingSweepWidth,
                To = LoadingTrackWidth,
                Duration = TimeSpan.FromSeconds(1.1),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });
    }

    private void StopLoadingAnimation()
    {
        if (!loadingAnimationRunning)
        {
            return;
        }

        loadingAnimationRunning = false;
        loadingSweepTransform.BeginAnimation(TranslateTransform.XProperty, null);
        loadingSweepTransform.X = -LoadingSweepWidth;
    }

    private UIElement BuildArtboardToolbar()
    {
        Border border = new()
        {
            Background = CernealaPreviewChrome.ToolbarBrush,
            BorderBrush = CernealaPreviewChrome.BorderBrush,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
        DockPanel panel = new() { LastChildFill = true, Margin = new Thickness(6, 0, 6, 0) };

        StackPanel zoom = new() { Orientation = Orientation.Horizontal };
        ConfigureZoomInput();
        zoom.Children.Add(zoomInput);
        Button zoomOut = CernealaPreviewChrome.Button("-", "Zoom out");
        zoomOut.Click += (_, _) => session.SetZoom(CurrentZoom() / 1.25);
        zoom.Children.Add(zoomOut);
        Button zoomIn = CernealaPreviewChrome.Button("+", "Zoom in");
        zoomIn.Click += (_, _) => session.SetZoom(CurrentZoom() * 1.25);
        zoom.Children.Add(zoomIn);
        Button actualSize = CernealaPreviewChrome.Button("1:1", "Show actual size", 34);
        actualSize.Click += (_, _) => session.SetZoom(1);
        zoom.Children.Add(actualSize);
        Button fit = CernealaPreviewChrome.Button("Fit", "Fit all", 34);
        fit.Click += (_, _) => session.SetFitToSurface();
        zoom.Children.Add(fit);
        zoom.Children.Add(CernealaPreviewChrome.Label("FPS"));
        ConfigureRefreshRateInput();
        zoom.Children.Add(refreshRateInput);
        DockPanel.SetDock(zoom, Dock.Left);
        panel.Children.Add(zoom);

        StackPanel viewport = new() { Orientation = Orientation.Horizontal };
        viewport.Children.Add(CernealaPreviewChrome.Label("Viewport"));
        ConfigureDimensionInput(widthInput);
        ConfigureDimensionInput(heightInput);
        viewport.Children.Add(widthInput);
        viewport.Children.Add(CernealaPreviewChrome.Label("x"));
        viewport.Children.Add(heightInput);
        DockPanel.SetDock(viewport, Dock.Right);
        panel.Children.Add(viewport);

        TextBlock hint = CernealaPreviewChrome.Label("Ctrl+wheel zoom  |  Middle-drag pan");
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.TextTrimming = TextTrimming.CharacterEllipsis;
        panel.Children.Add(hint);
        border.Child = panel;
        return border;
    }

    private Thumb CreateViewportHandle(
        ViewportResizeAxis axis,
        Cursor cursor)
    {
        (double width, double height, HorizontalAlignment horizontal, VerticalAlignment vertical, Thickness margin) = axis switch
        {
            ViewportResizeAxis.Width => (18, 52, HorizontalAlignment.Right, VerticalAlignment.Center, new Thickness(0, 0, -9, 0)),
            ViewportResizeAxis.Height => (52, 18, HorizontalAlignment.Center, VerticalAlignment.Bottom, new Thickness(0, 0, 0, -9)),
            _ => (22, 22, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, -11, -11))
        };

        Thumb handle = new()
        {
            Tag = axis,
            Width = width,
            Height = height,
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Margin = margin,
            Cursor = cursor,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ToolTip = axis == ViewportResizeAxis.Width
                ? "Resize preview width"
                : axis == ViewportResizeAxis.Height
                    ? "Resize preview height"
                    : "Resize preview viewport"
        };
        handle.Template = CreateWpfResizeHandleTemplate(axis);
        Panel.SetZIndex(handle, 10);
        handle.DragStarted += OnViewportResizeStarted;
        handle.DragDelta += OnViewportResize;
        handle.DragCompleted += OnViewportResizeCompleted;
        return handle;
    }

    private static ControlTemplate CreateWpfResizeHandleTemplate(ViewportResizeAxis axis)
    {
        FrameworkElementFactory root = new(typeof(Grid));
        root.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

        if (axis == ViewportResizeAxis.Width)
        {
            root.AppendChild(CreateHandleLine(1, 24, HorizontalAlignment.Center, VerticalAlignment.Center));
            root.AppendChild(CreateHandleLine(9, 1, HorizontalAlignment.Right, VerticalAlignment.Center));
        }
        else if (axis == ViewportResizeAxis.Height)
        {
            root.AppendChild(CreateHandleLine(24, 1, HorizontalAlignment.Center, VerticalAlignment.Center));
            root.AppendChild(CreateHandleLine(1, 9, HorizontalAlignment.Center, VerticalAlignment.Bottom));
        }
        else
        {
            root.AppendChild(CreateHandleLine(11, 1, HorizontalAlignment.Right, VerticalAlignment.Center));
            root.AppendChild(CreateHandleLine(1, 11, HorizontalAlignment.Center, VerticalAlignment.Bottom));
        }

        root.AppendChild(CreateHandleGrip());
        return new ControlTemplate(typeof(Thumb)) { VisualTree = root };
    }

    private static FrameworkElementFactory CreateHandleLine(
        double width,
        double height,
        HorizontalAlignment horizontal,
        VerticalAlignment vertical)
    {
        FrameworkElementFactory line = new(typeof(Border));
        line.SetValue(FrameworkElement.WidthProperty, width);
        line.SetValue(FrameworkElement.HeightProperty, height);
        line.SetValue(FrameworkElement.HorizontalAlignmentProperty, horizontal);
        line.SetValue(FrameworkElement.VerticalAlignmentProperty, vertical);
        line.SetValue(Border.BackgroundProperty, DesignerSelectionBrush);
        return line;
    }

    private static FrameworkElementFactory CreateHandleGrip()
    {
        FrameworkElementFactory grip = new(typeof(Border));
        grip.SetValue(FrameworkElement.WidthProperty, 5d);
        grip.SetValue(FrameworkElement.HeightProperty, 5d);
        grip.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        grip.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grip.SetValue(Border.BackgroundProperty, Brushes.White);
        grip.SetValue(Border.BorderBrushProperty, DesignerSelectionBrush);
        grip.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        return grip;
    }

    private static GridSplitter CreateSplitter(GridResizeDirection direction, Cursor cursor) => new()
    {
        ResizeDirection = direction,
        ResizeBehavior = GridResizeBehavior.PreviousAndNext,
        ShowsPreview = false,
        Cursor = cursor,
        Background = CernealaPreviewChrome.BorderBrush,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private static void ConfigureDimensionInput(TextBox input)
    {
        CernealaPreviewChrome.ConfigureTextBox(input, 50);
        input.HorizontalContentAlignment = HorizontalAlignment.Right;
    }

    private void ConfigureZoomInput()
    {
        CernealaPreviewChrome.ConfigureComboBox(zoomInput, 66);
        foreach (string value in new[] { "12.5%", "25%", "50%", "75%", "100%", "150%", "200%", "400%", "800%" })
        {
            zoomInput.Items.Add(value);
        }

        zoomInput.SelectionChanged += (_, _) =>
            CommitZoom(zoomInput.SelectedItem as string);
        zoomInput.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                CommitZoom();
                args.Handled = true;
            }
        };
        zoomInput.LostKeyboardFocus += (_, _) => CommitZoom();
        widthInput.KeyDown += OnDimensionKeyDown;
        heightInput.KeyDown += OnDimensionKeyDown;
        widthInput.LostKeyboardFocus += (_, _) => CommitViewportSize();
        heightInput.LostKeyboardFocus += (_, _) => CommitViewportSize();
    }

    private void ConfigureRefreshRateInput()
    {
        CernealaPreviewChrome.ConfigureComboBox(refreshRateInput, 50);
        foreach (string value in new[] { "15", "30", "60", "120" })
        {
            refreshRateInput.Items.Add(value);
        }

        refreshRateInput.SelectionChanged += (_, _) =>
            CommitRefreshRate(refreshRateInput.SelectedItem as string);
        refreshRateInput.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                CommitRefreshRate();
                args.Handled = true;
            }
        };
        refreshRateInput.LostKeyboardFocus += (_, _) => CommitRefreshRate();
    }

    private void OnDimensionKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Enter)
        {
            CommitViewportSize();
            args.Handled = true;
        }
    }

    private void CommitViewportSize()
    {
        int width = ParseDimension(widthInput.Text, session.ViewportWidth);
        int height = ParseDimension(heightInput.Text, session.ViewportHeight);
        session.SetViewportSize(width, height, immediate: true);
    }

    private void CommitZoom(string? selectedValue = null)
    {
        string text = (selectedValue ?? zoomInput.Text)?.Trim().TrimEnd('%') ?? string.Empty;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
        {
            session.SetZoom(percent / 100);
        }
    }

    private void CommitRefreshRate(string? selectedValue = null)
    {
        if (int.TryParse(
            (selectedValue ?? refreshRateInput.Text)?.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int framesPerSecond))
        {
            session.SetRefreshRateLimit(framesPerSecond);
        }
        else
        {
            refreshRateInput.Text = session.RefreshRateLimit.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs args) => ApplySessionState();

    private void ApplySessionState()
    {
        if (disposed)
        {
            return;
        }

        ApplyLayout();
        previewImage.Source = session.Frame;
        SetLoadingState(session.IsLoading);
        error.Text = session.Error ?? string.Empty;
        error.Visibility = session.Error is null ? Visibility.Collapsed : Visibility.Visible;
        if (!widthInput.IsKeyboardFocusWithin)
        {
            widthInput.Text = session.ViewportWidth.ToString(CultureInfo.InvariantCulture);
        }

        if (!heightInput.IsKeyboardFocusWithin)
        {
            heightInput.Text = session.ViewportHeight.ToString(CultureInfo.InvariantCulture);
        }

        if (!zoomInput.IsKeyboardFocusWithin)
        {
            zoomInput.Text = session.FitToSurface ? "Fit" : $"{session.ZoomFactor * 100:F0}%";
        }

        if (!refreshRateInput.IsKeyboardFocusWithin)
        {
            refreshRateInput.Text = session.RefreshRateLimit.ToString(CultureInfo.InvariantCulture);
        }

        viewportSize.Text = $"{session.ViewportWidth} x {session.ViewportHeight}";
        UpdateArtboard();
    }

    private void ApplyLayout()
    {
        if (placement == PreviewMarginPlacement.Top)
        {
            bool show = session.Mode == PreviewViewMode.Design ||
                session.Mode == PreviewViewMode.Split &&
                session.Orientation == PreviewSplitOrientation.Horizontal;
            Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            Height = show ? ResolveHorizontalExtent() : 0;
            RowDefinitions[1].Height = show && session.Mode == PreviewViewMode.Split
                ? new GridLength(SplitterSize)
                : new GridLength(0);
            return;
        }

        bool showVertical = session.Mode == PreviewViewMode.Split &&
            session.Orientation == PreviewSplitOrientation.Vertical;
        Visibility = showVertical ? Visibility.Visible : Visibility.Collapsed;
        Width = showVertical ? ResolveVerticalExtent() : 0;
        ColumnDefinitions[1].Width = showVertical
            ? new GridLength(SplitterSize)
            : new GridLength(0);
    }

    private double ResolveHorizontalExtent()
    {
        if (session.Mode == PreviewViewMode.Design)
        {
            return Math.Max(280, totalExtent - 1);
        }

        double requested = session.HorizontalExtent > 0
            ? session.HorizontalExtent
            : Math.Max(280, Math.Min(620, totalExtent * 0.58));
        return Math.Max(220, Math.Min(Math.Max(220, totalExtent - 100), requested));
    }

    private double ResolveVerticalExtent()
    {
        double requested = session.VerticalExtent > 0
            ? session.VerticalExtent
            : Math.Max(420, Math.Min(820, totalExtent * 0.56));
        return Math.Max(320, Math.Min(Math.Max(320, totalExtent - 180), requested));
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        totalExtent = placement == PreviewMarginPlacement.Top
            ? Math.Max(totalExtent, ActualHeight + textView.VisualElement.ActualHeight)
            : Math.Max(totalExtent, ActualWidth + textView.VisualElement.ActualWidth);
        ApplySessionState();
        if (placement == PreviewMarginPlacement.Top)
        {
            session.Start();
        }
    }

    private void OnEditorSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (Visibility != Visibility.Visible)
        {
            return;
        }

        double nextTotal = placement == PreviewMarginPlacement.Top
            ? ActualHeight + args.NewSize.Height
            : ActualWidth + args.NewSize.Width;
        if (Math.Abs(nextTotal - totalExtent) < 4)
        {
            return;
        }

        totalExtent = Math.Max(1, nextTotal);
        ApplyLayout();
        UpdateArtboard();
    }

    private void OnViewportResizeStarted(object sender, DragStartedEventArgs args)
    {
        if (viewportSize.Tag is Border badge)
        {
            badge.Visibility = Visibility.Visible;
        }
    }

    private void OnViewportResize(object sender, DragDeltaEventArgs args)
    {
        ViewportResizeAxis axis = (ViewportResizeAxis)((Thumb)sender).Tag;
        double zoom = Math.Max(0.001, effectiveZoom);
        int width = session.ViewportWidth;
        int height = session.ViewportHeight;
        if (axis is ViewportResizeAxis.Width or ViewportResizeAxis.Both)
        {
            width += (int)Math.Round(args.HorizontalChange / zoom);
        }

        if (axis is ViewportResizeAxis.Height or ViewportResizeAxis.Both)
        {
            height += (int)Math.Round(args.VerticalChange / zoom);
        }

        session.SetViewportSize(width, height);
    }

    private void OnViewportResizeCompleted(object sender, DragCompletedEventArgs args)
    {
        if (viewportSize.Tag is Border badge)
        {
            badge.Visibility = Visibility.Collapsed;
        }

        session.SetViewportSize(session.ViewportWidth, session.ViewportHeight, immediate: true);
    }

    private void UpdateArtboard()
    {
        if (Visibility != Visibility.Visible)
        {
            return;
        }

        effectiveZoom = session.FitToSurface ? CalculateFitZoom() : session.ZoomFactor;
        artboard.Width = Math.Max(1, session.ViewportWidth * effectiveZoom);
        artboard.Height = Math.Max(1, session.ViewportHeight * effectiveZoom);
        previewImage.Width = artboard.Width;
        previewImage.Height = artboard.Height;
    }

    private double CalculateFitZoom()
    {
        double availableWidth = Math.Max(1, scroller.ActualWidth - 64);
        double availableHeight = Math.Max(1, scroller.ActualHeight - 64);
        return Math.Max(0.125, Math.Min(8, Math.Min(
            availableWidth / session.ViewportWidth,
            availableHeight / session.ViewportHeight)));
    }

    private double CurrentZoom() => session.FitToSurface ? effectiveZoom : session.ZoomFactor;

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs args)
    {
        previewImage.Focus();
        if (args.ChangedButton == MouseButton.Middle)
        {
            panOrigin = args.GetPosition(scroller);
            panHorizontalOffset = scroller.HorizontalOffset;
            panVerticalOffset = scroller.VerticalOffset;
            previewImage.Cursor = Cursors.Hand;
            previewImage.CaptureMouse();
            args.Handled = true;
            return;
        }

        if (!TryGetLogicalPoint(args, out Point point) ||
            ToPreviewButton(args.ChangedButton) is not string button)
        {
            return;
        }

        previewImage.CaptureMouse();
        session.SetPointerButton(point.X, point.Y, button, isDown: true);
        args.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs args)
    {
        if (panOrigin is not Point origin)
        {
            if (TryGetLogicalPoint(args, out Point point))
            {
                session.MovePointer(point.X, point.Y);
                args.Handled = true;
            }

            return;
        }

        Point current = args.GetPosition(scroller);
        Vector delta = current - origin;
        scroller.ScrollToHorizontalOffset(panHorizontalOffset - delta.X);
        scroller.ScrollToVerticalOffset(panVerticalOffset - delta.Y);
        args.Handled = true;
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (panOrigin is not null && args.ChangedButton == MouseButton.Middle)
        {
            EndPan();
            args.Handled = true;
            return;
        }

        if (!TryGetLogicalPoint(args, out Point point) ||
            ToPreviewButton(args.ChangedButton) is not string button)
        {
            return;
        }

        session.SetPointerButton(point.X, point.Y, button, isDown: false);
        if (!AnyPreviewMouseButtonPressed())
        {
            ReleasePreviewMouseCapture();
        }

        args.Handled = true;
    }

    private void OnPreviewMouseLeave(object sender, MouseEventArgs args)
    {
        if (!previewImage.IsMouseCaptured)
        {
            session.LeavePointer();
        }
    }

    private void OnPreviewLostMouseCapture(object sender, MouseEventArgs args)
    {
        if (releasingMouseCapture)
        {
            return;
        }

        if (panOrigin is not null)
        {
            panOrigin = null;
            previewImage.Cursor = Cursors.Arrow;
        }
        else
        {
            session.ResetInput();
        }
    }

    private void EndPan()
    {
        panOrigin = null;
        previewImage.Cursor = Cursors.Arrow;
        ReleasePreviewMouseCapture();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            if (TryGetLogicalPoint(args, out Point point))
            {
                session.ScrollPointer(point.X, point.Y, args.Delta);
                args.Handled = true;
            }

            return;
        }

        Point cursor = args.GetPosition(scroller);
        Point anchor = args.GetPosition(artboard);
        double logicalX = anchor.X / Math.Max(0.001, effectiveZoom);
        double logicalY = anchor.Y / Math.Max(0.001, effectiveZoom);
        session.SetZoom(CurrentZoom() * (args.Delta > 0 ? 1.1 : 1 / 1.1));
        UpdateLayout();
        Point origin = artboard.TranslatePoint(new Point(0, 0), scroller);
        scroller.ScrollToHorizontalOffset(
            scroller.HorizontalOffset + origin.X + logicalX * effectiveZoom - cursor.X);
        scroller.ScrollToVerticalOffset(
            scroller.VerticalOffset + origin.Y + logicalY * effectiveZoom - cursor.Y);
        args.Handled = true;
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return;
        }

        args.Handled = true;
        session.SendText(args.Text);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
        string? key = ToPreviewKey(args.Key == Key.System ? args.SystemKey : args.Key);
        if (key is null)
        {
            return;
        }

        args.Handled = true;
        session.SetKeyState(key, isDown: true);
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs args)
    {
        string? key = ToPreviewKey(args.Key == Key.System ? args.SystemKey : args.Key);
        if (key is null)
        {
            return;
        }

        args.Handled = true;
        session.SetKeyState(key, isDown: false);
    }

    private void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args) =>
        session.ResetInput();

    private bool TryGetLogicalPoint(MouseEventArgs args, out Point point)
    {
        if (previewImage.ActualWidth <= 0 || previewImage.ActualHeight <= 0)
        {
            point = default;
            return false;
        }

        Point imagePoint = args.GetPosition(previewImage);
        point = new Point(
            imagePoint.X * session.ViewportWidth / previewImage.ActualWidth,
            imagePoint.Y * session.ViewportHeight / previewImage.ActualHeight);
        return true;
    }

    private void ReleasePreviewMouseCapture()
    {
        if (!previewImage.IsMouseCaptured)
        {
            return;
        }

        releasingMouseCapture = true;
        try
        {
            previewImage.ReleaseMouseCapture();
        }
        finally
        {
            releasingMouseCapture = false;
        }
    }

    private static bool AnyPreviewMouseButtonPressed() =>
        Mouse.LeftButton == MouseButtonState.Pressed ||
        Mouse.RightButton == MouseButtonState.Pressed ||
        Mouse.XButton1 == MouseButtonState.Pressed ||
        Mouse.XButton2 == MouseButtonState.Pressed;

    private static string? ToPreviewButton(MouseButton button) => button switch
    {
        MouseButton.Left => "Left",
        MouseButton.Right => "Right",
        MouseButton.XButton1 => "XButton1",
        MouseButton.XButton2 => "XButton2",
        _ => null
    };

    private static int ParseDimension(string text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;

    private static string? ToPreviewKey(Key key)
    {
        if (key == Key.Return) return "Enter";
        if (key is Key.Back or Key.Tab or Key.Escape or Key.Space or
            Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or Key.PageUp or Key.PageDown or
            Key.End or Key.Home or Key.Left or Key.Up or Key.Right or Key.Down or
            Key.Insert or Key.Delete ||
            key is >= Key.D0 and <= Key.D9 ||
            key is >= Key.A and <= Key.Z ||
            key is >= Key.F1 and <= Key.F12)
        {
            return key.ToString();
        }

        return null;
    }

    private enum ViewportResizeAxis
    {
        Width,
        Height,
        Both
    }
}
