using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc;

/// <summary>The properties of a <see cref="Poi"/>.</summary>
public class Poi
{
    /// <summary>The name of the <see cref="Poi"/>.</summary>
    public string Name { get; set; }
        
    /// <summary>The type of the <see cref="Poi"/>.</summary>
    public string Type { get; set; }
        
    /// <summary>The access restriction of the <see cref="Poi"/>.</summary>
    public string Access { get; set; }
        
    /// <summary>The geospatial position of the <see cref="Poi"/>.</summary>
    public Position Position { get; set; }
}