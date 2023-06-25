namespace KrugerNationalPark.Misc;

/// <summary>
/// A <see cref="TripDestination"/> contains information about the distance and duration from another <see cref="Poi"/>
/// to this destination.
/// </summary>
public class TripDestination
{
    /// <summary>Information about this <see cref="TripDestination"/>.</summary>
    public Poi Poi { get; set; }

    /// <summary>Duration (in seconds) to reach this POI from another <see cref="Poi"/>.</summary>
    public double Duration { get; set; }

    /// <summary>Distance (in meters) to reach this <see cref="Poi"/> from another <see cref="Poi"/>.</summary>
    public double Length { get; set; }
}