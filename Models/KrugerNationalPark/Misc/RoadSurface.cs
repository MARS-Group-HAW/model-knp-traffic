using KrugerNationalPark.Layers;

namespace KrugerNationalPark.Misc;

/// <summary>
/// The <see cref="RoadSurface"/> struct contains the road surface types of road segments of the
/// <see cref="KnpRoadNetwork"/>.
/// </summary>
/// <remarks>Based on the KNP road network dataset provided by SANParks.</remarks>
public struct RoadSurface
{
    public const string Graded = "Graded";
    public const string Gravel = "Gravel";
    public const string Disused = "Disused";
    public const string Rehab = "Rehab";
    public const string Tar = "Tar";
    public const string TwoTrack = "Two Track";
}