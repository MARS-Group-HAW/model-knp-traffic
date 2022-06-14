using System.Collections.Generic;

namespace KrugerNationalPark.Misc;

public class TripOrigin
{
    public Poi Poi { get; set; }
    public List<TripDestination> Destinations { get; set; }
}