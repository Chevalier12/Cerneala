using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.UI.Input;

namespace Cerneala.SolarSystem;

/// <summary>Reference data for one body. Orbital values are mean elements; positions are circular approximations.</summary>
public sealed record BodyInfo(
    string Name,
    string Kind,
    string Distance,
    string Year,
    string Day,
    string Radius,
    string Moons,
    string Temperature,
    string Fact,
    double PeriodDays,
    double MeanLongitudeJ2000Degrees,
    float FocusZoom);

/// <summary>
/// Simulation state the markup binds to: the clock, the playback speed and the selected body.
/// The window owns the per-frame tick and the camera; this type owns no rendering.
/// </summary>
public sealed class SolarSystemViewModel : INotifyPropertyChanged
{
    private static readonly DateTime J2000 = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static readonly IReadOnlyList<BodyInfo> Bodies =
    [
        new("Sun", "G-type main-sequence star", "Centre of the system", "—", "25 days at the equator", "696,340 km", "8 planets", "5,500 °C at the surface", "Holds 99.8% of all the mass in the solar system.", 0, 0, 1.25f),
        new("Mercury", "Terrestrial planet", "0.39 AU · 57.9 million km", "88 days", "58.6 days", "2,440 km", "None", "167 °C mean", "One solar day on Mercury lasts longer than its year.", 87.969, 252.25, 6.5f),
        new("Venus", "Terrestrial planet", "0.72 AU · 108.2 million km", "224.7 days", "243 days, retrograde", "6,052 km", "None", "464 °C mean", "The hottest planet: its thick carbon-dioxide air traps the heat.", 224.701, 181.98, 5f),
        new("Earth", "Terrestrial planet", "1.00 AU · 149.6 million km", "365.25 days", "23.9 hours", "6,371 km", "1", "15 °C mean", "The only world known to host life.", 365.256, 100.46, 5f),
        new("Mars", "Terrestrial planet", "1.52 AU · 227.9 million km", "687 days", "24.6 hours", "3,390 km", "2", "−65 °C mean", "Home to Olympus Mons, the tallest volcano known.", 686.980, 355.45, 5.5f),
        new("Jupiter", "Gas giant", "5.20 AU · 778.5 million km", "11.9 years", "9.9 hours", "69,911 km", "95+", "−110 °C at the cloud tops", "The Great Red Spot is a storm wider than Earth.", 4332.59, 34.40, 2.6f),
        new("Saturn", "Gas giant", "9.54 AU · 1.43 billion km", "29.4 years", "10.7 hours", "58,232 km", "146+", "−140 °C at the cloud tops", "Less dense than water; its rings are mostly ice.", 10759.22, 49.94, 2.4f),
        new("Uranus", "Ice giant", "19.2 AU · 2.87 billion km", "84 years", "17.2 hours, retrograde", "25,362 km", "27+", "−195 °C at the cloud tops", "Tilted by 98°, it rolls around the Sun on its side.", 30688.5, 313.23, 3.6f),
        new("Neptune", "Ice giant", "30.1 AU · 4.50 billion km", "165 years", "16.1 hours", "24,622 km", "16", "−200 °C at the cloud tops", "Its winds are the fastest measured in the solar system.", 60182.0, 304.88, 3.6f),
    ];

    private readonly DateTime start = DateTime.UtcNow.Date;
    private double elapsedDays;
    private double daysPerSecond = 1;
    private BodyInfo? selected;
    private string dateText = "";
    private string offsetText = "";

    public SolarSystemViewModel()
    {
        SelectCommand = new ActionCommand(parameter => Select(parameter as string));
        ClearSelectionCommand = new ActionCommand(_ => Select(null));
        SetSpeedCommand = new ActionCommand(parameter => SetSpeed(parameter as string));
        UpdateClockText();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when the selection changes; the window responds by moving the camera.</summary>
    public event EventHandler? SelectionChanged;

    public ICommand SelectCommand { get; }

    public ICommand ClearSelectionCommand { get; }

    public ICommand SetSpeedCommand { get; }

    /// <summary>Days since J2000.0 at the current simulated instant.</summary>
    public double DaysSinceJ2000 => (start - J2000).TotalDays + elapsedDays;

    public BodyInfo? Selected => selected;

    public bool HasSelection => selected is not null;

    public string SelectedName => selected?.Name ?? "";
    public string SelectedKind => selected?.Kind ?? "";
    public string SelectedDistance => selected?.Distance ?? "";
    public string SelectedYear => selected?.Year ?? "";
    public string SelectedDay => selected?.Day ?? "";
    public string SelectedRadius => selected?.Radius ?? "";
    public string SelectedMoons => selected?.Moons ?? "";
    public string SelectedTemperature => selected?.Temperature ?? "";
    public string SelectedFact => selected?.Fact ?? "";

    public bool IsSunSelected => selected?.Name == "Sun";
    public bool IsMercurySelected => selected?.Name == "Mercury";
    public bool IsVenusSelected => selected?.Name == "Venus";
    public bool IsEarthSelected => selected?.Name == "Earth";
    public bool IsMarsSelected => selected?.Name == "Mars";
    public bool IsJupiterSelected => selected?.Name == "Jupiter";
    public bool IsSaturnSelected => selected?.Name == "Saturn";
    public bool IsUranusSelected => selected?.Name == "Uranus";
    public bool IsNeptuneSelected => selected?.Name == "Neptune";

    public bool IsPaused => daysPerSecond == 0;
    public bool IsSpeedDay => daysPerSecond == 1;
    public bool IsSpeedWeek => daysPerSecond == 7;
    public bool IsSpeedMonth => daysPerSecond == 30;
    public bool IsSpeedYear => daysPerSecond == 365;

    public string DateText => dateText;

    public string OffsetText => offsetText;

    /// <summary>Advances the simulated clock by real elapsed time scaled by the playback speed.</summary>
    public void Advance(TimeSpan realElapsed)
    {
        if (daysPerSecond == 0)
        {
            return;
        }

        elapsedDays += realElapsed.TotalSeconds * daysPerSecond;
        UpdateClockText();
    }

    /// <summary>Mean heliocentric longitude of a body in radians at the current instant (circular orbit).</summary>
    public double MeanLongitudeRadians(BodyInfo body)
    {
        if (body.PeriodDays <= 0)
        {
            return 0;
        }

        double degrees = body.MeanLongitudeJ2000Degrees + 360.0 * DaysSinceJ2000 / body.PeriodDays;
        return degrees * Math.PI / 180.0;
    }

    private void Select(string? name)
    {
        BodyInfo? next = name is null ? null : Bodies.FirstOrDefault(body => body.Name == name);
        if (ReferenceEquals(next, selected))
        {
            // Re-selecting resumes following after the user has panned away.
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        selected = next;
        Raise(nameof(Selected), nameof(HasSelection), nameof(SelectedName), nameof(SelectedKind), nameof(SelectedDistance),
            nameof(SelectedYear), nameof(SelectedDay), nameof(SelectedRadius), nameof(SelectedMoons), nameof(SelectedTemperature),
            nameof(SelectedFact), nameof(IsSunSelected), nameof(IsMercurySelected), nameof(IsVenusSelected), nameof(IsEarthSelected),
            nameof(IsMarsSelected), nameof(IsJupiterSelected), nameof(IsSaturnSelected), nameof(IsUranusSelected), nameof(IsNeptuneSelected));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetSpeed(string? value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double next) ||
            next == daysPerSecond)
        {
            return;
        }

        daysPerSecond = next;
        Raise(nameof(IsPaused), nameof(IsSpeedDay), nameof(IsSpeedWeek), nameof(IsSpeedMonth), nameof(IsSpeedYear));
    }

    private void UpdateClockText()
    {
        DateTime now = start.AddDays(elapsedDays);
        string nextDate = now.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        int days = (int)Math.Floor(elapsedDays);
        string nextOffset = days switch
        {
            0 => "today",
            1 => "today + 1 day",
            _ => $"today + {days:N0} days",
        };
        if (nextDate != dateText)
        {
            dateText = nextDate;
            Raise(nameof(DateText));
        }

        if (nextOffset != offsetText)
        {
            offsetText = nextOffset;
            Raise(nameof(OffsetText));
        }
    }

    private void Raise(params string[] names)
    {
        foreach (string name in names)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
