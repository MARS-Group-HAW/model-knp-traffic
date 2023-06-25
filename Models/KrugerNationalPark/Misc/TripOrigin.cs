using System.Collections.Generic;

namespace KrugerNationalPark.Misc;

/// <summary>
/// A <see cref="TripOrigin"/> contains information the potential destinations that can be reached from it.
/// </summary>
public class TripOrigin
{
    /// <summary>Information about this <see cref="TripOrigin"/>.</summary>
    public Poi Poi { get; set; }
    
    /// <summary>List of <see cref="TripDestination"/> objects pertaining to this <see cref="TripOrigin"/>.</summary>
    public List<TripDestination> Destinations { get; set; }
}