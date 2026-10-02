using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.InvestorReel;

/// <summary>
/// Navigation between the theme picker and the reels. Each viewing gets a new reel instance, so
/// every chapter starts from its declared first frame.
/// </summary>
public partial class ReelWindow : Window<ShellViewModel>
{
    private void OnSourceInitialized(object? sender, EventArgs args)
    {
        ViewModel.PlayRequested += (_, theme) => Play(theme);
        ViewModel.HomeRequested += (_, _) => ShowPicker();
        ViewModel.ExitRequested += (_, _) => Close();
    }

    private void Play(ReelTheme theme)
    {
        ReelHost.Content = CreateReel(theme);
        Picker.Visibility = Visibility.Collapsed;
    }

    private void ShowPicker()
    {
        ReelHost.Content = null;
        Picker.Visibility = Visibility.Visible;
    }

    private static UIElement CreateReel(ReelTheme theme) => theme switch
    {
        ReelTheme.Cyberpunk => new CyberReel(),
        ReelTheme.Brutalist => new BrutalReel(),
        ReelTheme.Sakura => new SakuraReel(),
        _ => new InkReel(),
    };
}
