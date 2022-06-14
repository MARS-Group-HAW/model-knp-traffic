namespace KrugerNationalPark.Misc;

public class TripDestination
{
    public Poi Poi { get; set; }

    /// <summary>
    ///     Duration (in seconds) to reach this POI from the origin
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    ///     Distance (in meters) to reach this POI from the origin
    /// </summary>
    public double Length { get; set; }
}