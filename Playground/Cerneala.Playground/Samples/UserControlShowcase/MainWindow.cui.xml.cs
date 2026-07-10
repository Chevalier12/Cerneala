#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.Playground.Samples.UserControlShowcase;

public partial class MainWindow : UserControl<MainWindowViewModel>
{
    private void OnInitialized(object? sender, EventArgs args)
    {
        LifecycleText.Text = "Lifecycle: Initialized";
    }

    private void OnLoaded(UiElementId sender, RoutedEventArgs args)
    {
        LifecycleText.Text = $"Lifecycle: Loaded, score={ViewModel.Score}";
    }

    private void OnUnloaded(UiElementId sender, RoutedEventArgs args)
    {
        LifecycleText.Text = "Lifecycle: Unloaded";
    }

    private void OnDataContextChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        LifecycleText.Text = $"DataContext changed: {args.NewValue?.GetType().Name ?? "null"}";
    }

    private void OnPrimaryClick(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.Score++;
        ViewModel.ShowAdvanced = !ViewModel.ShowAdvanced;
        ViewModel.Mode = ViewModel.Score >= ViewModel.TargetScore
            ? ShowcaseMode.Complete
            : ShowcaseMode.Running;
    }

    private void OnSecondaryClick(UiElementId sender, RoutedEventArgs args)
    {
        ViewModel.Details = ViewModel.Details is null
            ? new ShowcaseDetails(isHealthy: true)
            : new ShowcaseDetails(!ViewModel.Details.IsHealthy);
        ViewModel.UserName = ViewModel.UserName == "Ada" ? "Zoe" : "Ada";
        ViewModel.TargetScore += 2;
    }

    private void OnPinStatusClick(UiElementId sender, RoutedEventArgs args)
    {
        StatusText.Text = "Pinned from code-behind; Local beats MarkupConditional";
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
        LifecycleText.Text = $"Primary IsEnabled changed to {args.NewValue}";
    }
}

public sealed class ShowcaseBadge : TextBlock
{
    public ShowcaseBadge()
    {
        Text = "Custom tag resolved from the code-behind namespace";
        FontFamily = "Consolas";
        FontSize = 12;
        Foreground = DrawColor.Black;
        Background = new DrawColor(255, 214, 102);
        Padding = new Cerneala.UI.Layout.Thickness(8, 4, 8, 4);
    }
}

public sealed class UserControlMarkupSample : IPlaygroundSample
{
    public string Name => "Markup UserControl";

    public Cerneala.UI.Elements.UIElement Build()
    {
        MainWindowViewModel viewModel = new(
            userName: "Ada",
            score: 2,
            targetScore: 10,
            details: new ShowcaseDetails(isHealthy: true));
        return new MainWindow(viewModel);
    }
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

    public MainWindowViewModel(string userName, int score, int targetScore, ShowcaseDetails? details)
    {
        this.userName = userName;
        this.score = score;
        this.targetScore = targetScore;
        this.details = details;
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

public enum ShowcaseMode
{
    Idle,
    Running,
    Complete
}
