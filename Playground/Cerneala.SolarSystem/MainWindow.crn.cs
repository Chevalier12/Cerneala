using System.Numerics;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Rendering;

namespace Cerneala.SolarSystem;

/// <summary>
/// The only behaviour markup cannot express: a scalable simulation clock, orbits placed from mean
/// longitudes, and a camera (zoom about the cursor, drag to pan, follow the selected body).
/// Everything visual, including hover and selection states, is declared in MainWindow.crn.
/// </summary>
public partial class MainWindow : Window<SolarSystemViewModel>, ITimeSensitiveRenderElement
{
    private const double ViewWidth = 1360;
    private const double ViewHeight = 765;
    private const double WorldCentre = 1200;
    private const double OverviewZoom = 0.355;
    private const double MinZoom = 0.3;
    private const double MaxZoom = 9;
    private const double InfoCardShift = 180;

    private sealed record Planet(BodyInfo Info, UIElement Orbit, UIElement Body, TextBlock Label, Button Hit, double Radius, double Size)
    {
        public double X { get; set; } = WorldCentre;
        public double Y { get; set; } = WorldCentre;
    }

    private Planet[]? planets;
    private double cameraX = WorldCentre;
    private double cameraY = WorldCentre;
    private double zoom = 2.6;
    private double targetX = WorldCentre;
    private double targetY = WorldCentre;
    private double targetZoom = OverviewZoom;
    private string? follow;
    private bool dragging;
    private bool dragMoved;
    private Vector2 dragOrigin;
    private (double X, double Y) dragCamera;

    public bool UpdateRenderTime(TimeSpan frameTime)
    {
        if (planets is null)
        {
            Initialise();
        }

        double seconds = Math.Min(frameTime.TotalSeconds, 0.1);
        if (seconds <= 0)
        {
            return false;
        }

        ViewModel.Advance(frameTime);
        PlaceBodies();
        MoveCamera(seconds);
        ApplyCamera();
        return true;
    }

    private void Initialise()
    {
        BodyInfo Info(string name) => SolarSystemViewModel.Bodies.First(body => body.Name == name);
        planets =
        [
            new(Info("Mercury"), MercuryOrbit, MercuryBody, MercuryLabel, MercuryHit, 150, 10),
            new(Info("Venus"), VenusOrbit, VenusBody, VenusLabel, VenusHit, 205, 18),
            new(Info("Earth"), EarthOrbit, EarthBody, EarthLabel, EarthHit, 265, 19),
            new(Info("Mars"), MarsOrbit, MarsBody, MarsLabel, MarsHit, 330, 13),
            new(Info("Jupiter"), JupiterOrbit, JupiterBody, JupiterLabel, JupiterHit, 540, 52),
            new(Info("Saturn"), SaturnOrbit, SaturnBody, SaturnLabel, SaturnHit, 690, 44),
            new(Info("Uranus"), UranusOrbit, UranusBody, UranusLabel, UranusHit, 840, 28),
            new(Info("Neptune"), NeptuneOrbit, NeptuneBody, NeptuneLabel, NeptuneHit, 980, 27),
        ];
        ViewModel.SelectionChanged += OnSelectionChanged;
    }

    private void PlaceBodies()
    {
        double days = ViewModel.DaysSinceJ2000;
        double earthRotation = 0;
        foreach (Planet planet in planets!)
        {
            // Screen y points down, so a counter-clockwise orbit seen from the north is a negative rotation.
            double rotation = -ViewModel.MeanLongitudeRadians(planet.Info);
            planet.Orbit.Rotation = (float)rotation;
            planet.Body.Rotation = (float)-rotation;
            planet.X = WorldCentre + planet.Radius * Math.Cos(rotation);
            planet.Y = WorldCentre + planet.Radius * Math.Sin(rotation);
            if (planet.Info.Name == "Earth")
            {
                earthRotation = rotation;
            }
        }

        double moonRotation = -2 * Math.PI * days / 27.3217;
        MoonOrbit.Rotation = (float)(moonRotation - earthRotation);
        Belt.Rotation = (float)(-2 * Math.PI * days / 1680);
    }

    private void MoveCamera(double seconds)
    {
        if (follow is not null && !dragging)
        {
            (double x, double y) = PositionOf(follow);
            targetX = x + InfoCardShift / Math.Max(targetZoom, MinZoom);
            targetY = y;
        }

        double rate = follow is null ? 4.0 : 6.0;
        double blend = 1 - Math.Exp(-seconds * rate);
        cameraX += (targetX - cameraX) * blend;
        cameraY += (targetY - cameraY) * blend;
        double zoomBlend = 1 - Math.Exp(-seconds * 2.6);
        zoom = Math.Exp(Math.Log(zoom) + (Math.Log(targetZoom) - Math.Log(zoom)) * zoomBlend);
    }

    private void ApplyCamera()
    {
        double offsetX = ViewWidth / 2 - zoom * cameraX;
        double offsetY = ViewHeight / 2 - zoom * cameraY;
        Universe.Scale = (float)zoom;
        Universe.TranslateX = (float)offsetX;
        Universe.TranslateY = (float)offsetY;

        // Parallax: deeper layers drift less as the camera moves across the system.
        double panX = cameraX - WorldCentre;
        double panY = cameraY - WorldCentre;
        PlaceLayer(StarsFar, panX, panY, 0.02);
        PlaceLayer(Nebula, panX, panY, 0.04);
        PlaceLayer(StarsMid, panX, panY, 0.07);
        PlaceLayer(StarsNear, panX, panY, 0.12);

        foreach (Planet planet in planets!)
        {
            PlaceLabel(planet.Label, offsetX + zoom * planet.X, offsetY + zoom * (planet.Y + planet.Size / 2) + 9);
            PlaceHit(planet.Hit, offsetX + zoom * planet.X, offsetY + zoom * planet.Y, Math.Max(40, zoom * planet.Size + 24));
        }

        PlaceLabel(SunLabel, offsetX + zoom * WorldCentre, offsetY + zoom * (WorldCentre + 66) + 12);
        PlaceHit(SunHit, offsetX + zoom * WorldCentre, offsetY + zoom * WorldCentre, Math.Max(56, zoom * 140));
        PlaceGhosts(offsetX + zoom * WorldCentre, offsetY + zoom * WorldCentre);
    }

    private static void PlaceLayer(UIElement layer, double panX, double panY, double depth)
    {
        layer.TranslateX = (float)((ViewWidth - 3200) / 2 - panX * depth);
        layer.TranslateY = (float)((ViewHeight - 2000) / 2 - panY * depth);
    }

    private static void PlaceHit(Button hit, double x, double y, double size)
    {
        // Layout positioning (not a render transform) keeps the hit area where input and Servo expect it.
        hit.Width = (float)size;
        hit.Height = (float)size;
        Cerneala.UI.Layout.Panels.Canvas.SetLeft(hit, (float)(x - size / 2));
        Cerneala.UI.Layout.Panels.Canvas.SetTop(hit, (float)(y - size / 2));
    }

    private static void PlaceLabel(TextBlock label, double x, double y)
    {
        label.TranslateX = (float)(x - label.DesiredSize.Width / 2);
        label.TranslateY = (float)y;
    }

    private void PlaceGhosts(double sunX, double sunY)
    {
        double toCentreX = ViewWidth / 2 - sunX;
        double toCentreY = ViewHeight / 2 - sunY;
        double distance = Math.Sqrt(toCentreX * toCentreX + toCentreY * toCentreY);
        bool sunInFrame = sunX > -60 && sunX < ViewWidth + 60 && sunY > -60 && sunY < ViewHeight + 60;
        double strength = sunInFrame ? Math.Clamp(distance / 260, 0, 1) * Math.Clamp(1.4 - zoom * 0.25, 0.25, 1) : 0;
        PlaceGhost(GhostA, sunX + toCentreX * 0.55, sunY + toCentreY * 0.55, 80, strength * 0.9);
        PlaceGhost(GhostB, sunX + toCentreX * 1.2, sunY + toCentreY * 1.2, 140, strength);
        PlaceGhost(GhostC, sunX + toCentreX * 1.5, sunY + toCentreY * 1.5, 24, strength);
        PlaceGhost(GhostD, sunX + toCentreX * 1.95, sunY + toCentreY * 1.95, 80, strength * 0.7);
    }

    private static void PlaceGhost(UIElement ghost, double x, double y, double size, double opacity)
    {
        ghost.TranslateX = (float)(x - size / 2);
        ghost.TranslateY = (float)(y - size / 2);
        ghost.Opacity = (float)Math.Clamp(opacity, 0, 1);
    }

    private (double X, double Y) PositionOf(string name)
    {
        Planet? planet = planets!.FirstOrDefault(candidate => candidate.Info.Name == name);
        return planet is null ? (WorldCentre, WorldCentre) : (planet.X, planet.Y);
    }

    private void OnSelectionChanged(object? sender, EventArgs args)
    {
        BodyInfo? selected = ViewModel.Selected;
        if (selected is null)
        {
            follow = null;
            targetZoom = OverviewZoom;
            targetX = WorldCentre;
            targetY = WorldCentre;
            return;
        }

        follow = selected.Name;
        targetZoom = selected.FocusZoom;
    }

    private void OnWheel(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not MouseWheelEventArgs wheel)
        {
            return;
        }

        double nextZoom = Math.Clamp(targetZoom * Math.Exp(wheel.Delta / 120.0 * 0.2), MinZoom, MaxZoom);
        if (follow is null)
        {
            // Keep the world point under the cursor fixed while zooming.
            Vector2 cursor = wheel.GetPosition(Viewport);
            double fromCentreX = cursor.X - ViewWidth / 2;
            double fromCentreY = cursor.Y - ViewHeight / 2;
            double worldX = targetX + fromCentreX / targetZoom;
            double worldY = targetY + fromCentreY / targetZoom;
            targetX = worldX - fromCentreX / nextZoom;
            targetY = worldY - fromCentreY / nextZoom;
        }

        targetZoom = nextZoom;
        args.Handled = true;
    }

    private void OnPointerDown(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not MouseEventArgs mouse)
        {
            return;
        }

        dragging = true;
        dragMoved = false;
        dragOrigin = mouse.GetPosition(Viewport);
        dragCamera = (cameraX, cameraY);
    }

    private void OnPointerMove(UiElementId sender, RoutedEventArgs args)
    {
        if (!dragging || args is not MouseEventArgs mouse)
        {
            return;
        }

        Vector2 delta = mouse.GetPosition(Viewport) - dragOrigin;
        if (!dragMoved && delta.Length() < 4)
        {
            return;
        }

        dragMoved = true;
        follow = null;
        targetX = cameraX = dragCamera.X - delta.X / zoom;
        targetY = cameraY = dragCamera.Y - delta.Y / zoom;
    }

    private void OnPointerUp(UiElementId sender, RoutedEventArgs args)
    {
        dragging = false;
    }
}
