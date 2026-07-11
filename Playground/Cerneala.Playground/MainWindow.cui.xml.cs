using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.Playground;

public partial class MainWindow : Window<MainWindowViewModel>
{
    private int openedWindowBatch;

    private void OnSourceInitialized(object? sender, EventArgs args)
    {
        LifecycleText.Text = "SourceInitialized: HWND-ul nativ exista";
    }

    private void OnInitialized(object? sender, EventArgs args)
    {
        LifecycleText.Text = "Initialized: arborele este compus";
    }

    private void OnLoaded(UiElementId sender, RoutedEventArgs args)
    {
        LifecycleText.Text = $"Loaded: score={ViewModel.Score}, user={ViewModel.UserName}";
        if (string.Equals(Environment.GetEnvironmentVariable("CERNEALA_OPEN_TEXT_ORACLE"), "1", StringComparison.Ordinal))
        {
            ShowTextOracleWindow();
        }
    }

    private void OnUnloaded(UiElementId sender, RoutedEventArgs args)
    {
        LifecycleText.Text = "Unloaded";
    }

    private void OnDataContextChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        LifecycleText.Text = $"DataContextChanged: {args.NewValue?.GetType().Name ?? "null"}";
    }

    private void OnActivated(object? sender, EventArgs args)
    {
        WindowEventText.Text = "Window event: Activated";
    }

    private void OnDeactivated(object? sender, EventArgs args)
    {
        WindowEventText.Text = "Window event: Deactivated";
    }

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        WindowEventText.Text = "Window event: Closing";
    }

    private void OnClosed(object? sender, EventArgs args)
    {
    }

    private void OnStateChanged(object? sender, EventArgs args)
    {
        WindowEventText.Text = $"Window event: StateChanged -> {WindowState}";
    }

    private void OnLocationChanged(object? sender, EventArgs args)
    {
        WindowEventText.Text = $"Window event: LocationChanged -> {Left:0}, {Top:0}";
    }

    private void OnContentRendered(object? sender, EventArgs args)
    {
        WindowEventText.Text = "Window event: ContentRendered";
    }

    private void OnPrimaryClick(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.Score++;
        ViewModel.ShowAdvanced = !ViewModel.ShowAdvanced;
        ViewModel.Mode = ViewModel.Score >= ViewModel.TargetScore
            ? ShowcaseMode.Complete
            : ShowcaseMode.Running;
    }

    private void OnReplaceDetailsClick(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.Details = ViewModel.Details is null
            ? new ShowcaseDetails(true)
            : new ShowcaseDetails(!ViewModel.Details.IsHealthy);
        ViewModel.UserName = ViewModel.UserName == "Ada" ? "Zoe" : "Ada";
        ViewModel.TargetScore += 2;
    }

    private void OnReplaceContextClick(UiElementId sender, RoutedEventArgs args)
    {
        DataContext = new MainWindowViewModel(new ShowcaseSeed(
            "Lin",
            score: 1,
            targetScore: 6,
            new ShowcaseDetails(false)));
    }

    private void OnPinStatusClick(UiElementId sender, RoutedEventArgs args)
    {
        StatusText.Text = "Valoare Local din code-behind: bate MarkupConditional";
    }

    private void OnToggleWindowStateClick(UiElementId sender, RoutedEventArgs args)
    {
        WindowState = WindowState == WindowState.Normal
            ? WindowState.Maximized
            : WindowState.Normal;
    }

    private void OnOpenWindowsClick(UiElementId sender, RoutedEventArgs args)
    {
        int batch = ++openedWindowBatch;
        for (int index = 1; index <= 3; index++)
        {
            Window window = new()
            {
                Title = $"Test window {batch}.{index}",
                Width = 480,
                Height = 300,
                MinWidth = 320,
                MinHeight = 200,
                Left = Left + (index * 48),
                Top = Top + (index * 48),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Owner = this,
                Background = new Color(20, 28, 38),
                BorderColor = new Color(82, 96, 113),
                BorderThickness = new Cerneala.UI.Layout.Thickness(1),
                Padding = new Cerneala.UI.Layout.Thickness(20),
                Content = new TextBlock
                {
                    Text = $"Fereastra {index} din setul {batch}",
                    FontFamily = "Segoe UI",
                    FontSize = 20,
                    Foreground = new Color(245, 245, 245),
                    Background = new Color(30, 38, 51),
                    Padding = new Cerneala.UI.Layout.Thickness(16)
                }
            };

            window.Show();
        }
    }

    private void OnOpenTextOracleClick(UiElementId sender, RoutedEventArgs args)
    {
        ShowTextOracleWindow();
    }

    private void ShowTextOracleWindow()
    {
        string fontFamily = Environment.GetEnvironmentVariable("CERNEALA_TEXT_ORACLE_FONT") ?? "Arial";
        string screenshotName = Environment.GetEnvironmentVariable("CERNEALA_TEXT_ORACLE_SCREENSHOT") ?? "cerneala-text.png";
        string screenshotPath = Path.GetFullPath(Path.Combine("artifacts", "visual-oracles", screenshotName));
        Window window = new()
        {
            Title = "Cerneala text oracle",
            Width = 320,
            Height = 160,
            MinWidth = 320,
            MinHeight = 160,
            MaxWidth = 320,
            MaxHeight = 160,
            Left = Left + 96,
            Top = Top + 96,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ResizeMode = ResizeMode.NoResize,
            Owner = this,
            Background = Color.White,
            BorderColor = Color.Transparent,
            BorderThickness = Cerneala.UI.Layout.Thickness.Zero,
            Padding = Cerneala.UI.Layout.Thickness.Zero,
            Content = new TextBlock
            {
                Text = "Hello world!",
                FontFamily = fontFamily,
                FontSize = 16,
                Foreground = Color.Black,
                HorizontalAlignment = Cerneala.UI.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Cerneala.UI.Layout.VerticalAlignment.Center
            }
        };

        EventHandler? capture = null;
        capture = (_, _) =>
        {
            window.ContentRendered -= capture;
            window.SaveScreenshot(screenshotPath);
            IDrawFont font = new SystemFontSource().LoadFont(fontFamily, 16);
            DrawTextRun run = new(font, "Hello world!", 16);
            using SkiaSharp.SKFont skiaFont = new(((SkiaFont)font).Typeface, 16)
            {
                LinearMetrics = true,
                Hinting = SkiaSharp.SKFontHinting.Full,
                Subpixel = true,
                Edging = SkiaSharp.SKFontEdging.SubpixelAntialias,
                BaselineSnap = true
            };
            SkiaSharp.SKFontMetrics skiaMetrics = skiaFont.Metrics;
            TextShaper.Default.TryMeasureLineHeight(run, out float lineHeight);
            TextShaper.Default.TryMeasureBaseline(run, out float baseline);
            TextMeasureResult measurement = TextMeasurer.Default.Measure(
                "Hello world!",
                new TextAspect(fontFamily, 16),
                float.PositiveInfinity);
            TextBlock textBlock = (TextBlock)window.Content!;
            File.WriteAllLines(Path.ChangeExtension(screenshotPath, ".metrics.txt"),
            [
                $"Bounds={textBlock.ArrangedBounds}",
                $"DesiredSize={textBlock.DesiredSize}",
                $"Measure.Width={measurement.Size.Width:R}",
                $"Measure.Height={measurement.Size.Height:R}",
                $"LineHeight={lineHeight:R}",
                $"Baseline={baseline:R}",
                $"Skia.Ascent={skiaMetrics.Ascent:R}",
                $"Skia.Descent={skiaMetrics.Descent:R}",
                $"Skia.Leading={skiaMetrics.Leading:R}"
            ]);
        };
        window.ContentRendered += capture;

        window.Show();
    }

    private void OnAdvancedClick(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.Mode = ShowcaseMode.Complete;
        ViewModel.IsReady = true;
    }

    private void OnBadgeMouseEnter(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.IsReady = true;
    }

    private void OnPrimaryEnabledChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        LifecycleText.Text = $"PrimaryButton.IsEnabledChanged: {args.NewValue}";
    }
}

public static class App
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ShowcaseSeed(
            "Ada",
            score: 2,
            targetScore: 10,
            new ShowcaseDetails(true)));
    }
}

public sealed class ShowcaseSeed
{
    public ShowcaseSeed(string userName, int score, int targetScore, ShowcaseDetails? details)
    {
        UserName = userName;
        Score = score;
        TargetScore = targetScore;
        Details = details;
    }

    public string UserName { get; }

    public int Score { get; }

    public int TargetScore { get; }

    public ShowcaseDetails? Details { get; }
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string userName;
    private int score;
    private int targetScore;
    private bool isReady;
    private bool showAdvanced;
    private ShowcaseMode mode;
    private ShowcaseDetails? details;

    public MainWindowViewModel(ShowcaseSeed seed)
    {
        userName = seed.UserName;
        score = seed.Score;
        targetScore = seed.TargetScore;
        details = seed.Details;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string UserName
    {
        get => userName;
        set => Set(ref userName, value);
    }

    public int Score
    {
        get => score;
        set => Set(ref score, value);
    }

    public int TargetScore
    {
        get => targetScore;
        set => Set(ref targetScore, value);
    }

    public bool IsReady
    {
        get => isReady;
        set => Set(ref isReady, value);
    }

    public bool ShowAdvanced
    {
        get => showAdvanced;
        set => Set(ref showAdvanced, value);
    }

    public ShowcaseMode Mode
    {
        get => mode;
        set => Set(ref mode, value);
    }

    public ShowcaseDetails? Details
    {
        get => details;
        set => Set(ref details, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ShowcaseDetails : INotifyPropertyChanged
{
    private bool isHealthy;

    public ShowcaseDetails(bool isHealthy)
    {
        this.isHealthy = isHealthy;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsHealthy
    {
        get => isHealthy;
        set
        {
            if (isHealthy == value)
            {
                return;
            }

            isHealthy = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHealthy)));
        }
    }
}

public sealed class ShowcaseBadge : TextBlock
{
    public ShowcaseBadge()
    {
        Text = "Custom tag rezolvat semantic din namespace-ul code-behind";
        FontFamily = "Consolas";
        FontSize = 12;
        Foreground = Color.Black;
        Background = new Color(255, 214, 102);
        Padding = new Cerneala.UI.Layout.Thickness(8, 4, 8, 4);
    }
}

public enum ShowcaseMode
{
    Idle,
    Running,
    Complete
}
