using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc;

/// <summary>
///     The properties of a POI
///     TODO: properties are null when class name is not "Poi"
/// </summary>
public class Poi
{
    /// <summary>
    ///     The name of a POI
    /// </summary>
    public string Name { get; set; }
        
    /// <summary>
    ///     The type of a POI
    /// </summary>
    public string Type { get; set; }
        
    /// <summary>
    ///     The access restriction level of a POI
    /// </summary>
    public string Access { get; set; }
        
    /// <summary>
    ///     The geospatial position of a POI
    /// </summary>
    public Position Position { get; set; }
}