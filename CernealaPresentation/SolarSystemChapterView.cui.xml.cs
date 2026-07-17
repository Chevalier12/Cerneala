using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Media;

namespace Cerneala.Presentation;

public partial class SolarSystemChapterView : UserControl
{
    private const float MinimumZoom = 0.7f;
    private const float MaximumZoom = 3.4f;
    private const float ZoomStep = 1.12f;

    private static readonly PlanetCardData Mercury = new(
        "Mercur", "Planeta terestra", "57,9 mil. km", "4.879 km", "88 zile", "MercuryGradient");
    private static readonly PlanetCardData Venus = new(
        "Venus", "Planeta terestra", "108,2 mil. km", "12.104 km", "225 zile", "VenusGradient");
    private static readonly PlanetCardData Earth = new(
        "Pamant", "Planeta terestra", "149,6 mil. km", "12.742 km", "365 zile", "EarthGradient");
    private static readonly PlanetCardData Mars = new(
        "Marte", "Planeta terestra", "227,9 mil. km", "6.779 km", "687 zile", "MarsGradient");
    private static readonly PlanetCardData Jupiter = new(
        "Jupiter", "Gigant gazos", "778,5 mil. km", "139.820 km", "11,9 ani", "JupiterGradient");
    private static readonly PlanetCardData Saturn = new(
        "Saturn", "Gigant gazos", "1,43 mld. km", "116.460 km", "29,5 ani", "SaturnGradient");
    private static readonly PlanetCardData Uranus = new(
        "Uranus", "Gigant de gheata", "2,87 mld. km", "50.724 km", "84 ani", "UranusGradient");
    private static readonly PlanetCardData Neptune = new(
        "Neptun", "Gigant de gheata", "4,50 mld. km", "49.244 km", "164,8 ani", "NeptuneGradient");

    private void OnMouseWheel(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not MouseWheelEventArgs wheelArgs || wheelArgs.Delta == 0)
        {
            return;
        }

        float zoomFactor = wheelArgs.Delta > 0 ? ZoomStep : 1 / ZoomStep;
        float zoom = Math.Clamp(SolarScene.ScaleX * zoomFactor, MinimumZoom, MaximumZoom);
        SolarScene.ScaleX = zoom;
        SolarScene.ScaleY = zoom;
        args.Handled = true;
    }

    private void OnMercuryClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Mercury, args);

    private void OnVenusClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Venus, args);

    private void OnEarthClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Earth, args);

    private void OnMarsClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Mars, args);

    private void OnJupiterClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Jupiter, args);

    private void OnSaturnClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Saturn, args);

    private void OnUranusClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Uranus, args);

    private void OnNeptuneClicked(UiElementId sender, RoutedEventArgs args) => SelectPlanet(Neptune, args);

    private void SelectPlanet(PlanetCardData planet, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left })
        {
            return;
        }

        SelectPlanet(planet);
        args.Handled = true;
    }

    private void SelectPlanet(PlanetCardData planet)
    {
        ObjectNameText.Text = planet.Name;
        ObjectTypeText.Text = planet.Type;
        DistanceText.Text = planet.Distance;
        DiameterText.Text = planet.Diameter;
        OrbitalYearText.Text = planet.OrbitalYear;

        if (Resources.TryGetResource(planet.BrushResource, out Brush swatchBrush))
        {
            PlanetSwatchGlow.Fill = swatchBrush;
            PlanetSwatchCore.Fill = swatchBrush;
        }
    }

    private readonly record struct PlanetCardData(
        string Name,
        string Type,
        string Distance,
        string Diameter,
        string OrbitalYear,
        string BrushResource);
}
