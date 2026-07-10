using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.Playground;

public partial class MainWindow : Window<MainWindowViewModel>
{
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
        Foreground = DrawColor.Black;
        Background = new DrawColor(255, 214, 102);
        Padding = new Cerneala.UI.Layout.Thickness(8, 4, 8, 4);
    }
}

public enum ShowcaseMode
{
    Idle,
    Running,
    Complete
}
