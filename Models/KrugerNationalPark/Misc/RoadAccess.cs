using KrugerNationalPark.Layers;

namespace KrugerNationalPark.Misc;

/// <summary>
/// The <see cref="RoadAccess"/> struct contains the road access types of road segments of the
/// <see cref="KnpRoadNetwork"/>.
/// </summary>
/// <remarks>Based on the KNP road network dataset provided by SANParks.</remarks>
public struct RoadAccess
{
    public const string Private = "Private";
    public const string Public = "Public";
    public const string Staff = "Staff";
}
