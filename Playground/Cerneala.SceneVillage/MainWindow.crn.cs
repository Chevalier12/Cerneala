using Cerneala.UI.Controls;
using Cerneala.UI.Input;

namespace Cerneala.SceneVillage;

public partial class MainWindow : Window
{
    private StressPreset selectedPreset = StressPreset.Static;
    private int selectedCount;
    private int displayedRealizedCount = -1;

    private void OnPreviewKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs key)
        {
            return;
        }

        if (GameSurface.SetMovementKey(key.Key, true))
        {
            args.Handled = true;
        }
        else if (key.Key == InputKey.R)
        {
            GameSurface.ResetPlayer();
            args.Handled = true;
        }
    }

    private void OnPreviewKeyUp(UiElementId sender, RoutedEventArgs args)
    {
        if (args is KeyEventArgs key && GameSurface.SetMovementKey(key.Key, false))
        {
            args.Handled = true;
        }
    }

    private void OnDeactivated(object? sender, EventArgs args)
    {
        GameSurface.ClearMovementKeys();
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        RefreshStatus();
    }

    private void OnStaticClick(UiElementId sender, RoutedEventArgs args) => SelectPreset(StressPreset.Static);

    private void OnAnimatedClick(UiElementId sender, RoutedEventArgs args) => SelectPreset(StressPreset.Animated);

    private void OnCollisionClick(UiElementId sender, RoutedEventArgs args) => SelectPreset(StressPreset.Collision);

    private void OnCountZeroClick(UiElementId sender, RoutedEventArgs args) => SelectCount(0);

    private void OnCountHundredClick(UiElementId sender, RoutedEventArgs args) => SelectCount(100);

    private void OnCountThousandClick(UiElementId sender, RoutedEventArgs args) => SelectCount(1_000);

    private void OnCountTenThousandClick(UiElementId sender, RoutedEventArgs args) => SelectCount(10_000);

    private void OnFieldClick(UiElementId sender, RoutedEventArgs args) => GameSurface.GoToStressField();

    private void OnVillageClick(UiElementId sender, RoutedEventArgs args) => GameSurface.ResetPlayer();

    private void SelectPreset(StressPreset preset)
    {
        selectedPreset = preset;
        GameSurface.SetStress(selectedPreset, selectedCount);
        RefreshStatus(force: true);
    }

    private void SelectCount(int count)
    {
        selectedCount = count;
        GameSurface.SetStress(selectedPreset, selectedCount);
        RefreshStatus(force: true);
    }

    private void RefreshStatus(bool force = false)
    {
        int realized = GameSurface.RealizedStressCount;
        if (!force && realized == displayedRealizedCount)
        {
            return;
        }

        displayedRealizedCount = realized;
        StatusText.Text = $"{VillageStress.Label(selectedPreset)} — requested {selectedCount:N0} / realized {realized:N0}";
    }
}
