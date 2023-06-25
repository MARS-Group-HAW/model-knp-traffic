using System;
using System.Collections.Generic;
using System.Text.Json;
using KrugerNationalPark.Misc;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers;

/// <summary>
/// A <see cref="KnpPoi"/> represents a POI in the KNP (e.g., KNP Gate, Rest camp, etc.).
/// </summary>
public class KnpPoi : IVectorFeature
{
    #region Initialization

    /// <summary>Initialization routine of the <see cref="KnpPoi"/>.</summary>
    /// <param name="layer">The layer that holds the <see cref="KnpPoi"/>.</param>
    /// <param name="data">The data available for initializing the <see cref="KnpPoi"/>.</param>
    public void Init(ILayer layer, VectorStructuredData data)
    {
        var centroid = data.Geometry.Centroid;
        Position = Position.CreatePosition(centroid.X, centroid.Y);

        VectorStructured = data;

        Name = VectorStructured.Data["name"].ToString();
        Type = VectorStructured.Data["type"].ToString();

        // load timings form json into structure
        // todo: this could probably be hinted directly in the GeoJSON Properties for JObject?
        var list = VectorStructured.Data["routeList"].ToString();
        infoList = JsonSerializer.Deserialize<TripOrigin>(list);
    }

    #endregion

    #region Methods

    public void Update(VectorStructuredData data)
    {
        throw new NotImplementedException();
    }

    // TODO: Move this to PoiLayer (to encapsulate KnpPoi a little more)?
    /// <summary>
    /// Gets the <see cref="KnpPoi"/> objects that satisfy the given travel time constraint and POI type constraint.
    /// </summary>
    /// <param name="timeLimit">The maximum amount of travel time in seconds.</param>
    /// <param name="allowedTypes">Types of POIs that are allowed as travel destinations. <see cref="PoiType"/>.</param>
    /// <returns></returns>
    public List<TripDestination> GetDestinationPois(double timeLimit, List<string> allowedTypes = null)
    {
        List<TripDestination> results = new();

        foreach (var dest in infoList.Destinations)
        {
            // Exclude POIs that exceed the travel time limit
            if (dest.Duration > timeLimit) continue;

            // If specific types of POIs are requested, exclude POIs of different type
            if (allowedTypes is not null && !allowedTypes.Contains(dest.Poi.Type)) continue;

            results.Add(dest);
        }

        return results;
    }

    #endregion

    #region Properties

    /// <summary>Collection of travel distances and durations to all other <see cref="KnpPoi"/>.</summary>
    public TripOrigin infoList;

    /// <summary>Position (latitude, longitude) of the <see cref="KnpPoi"/>.</summary>
    public Position Position { get; private set; }

    /// <summary>Name of the <see cref="KnpPoi"/>.</summary>
    public string Name { get; private set; }

    /// <summary>Type of the <see cref="KnpPoi"/> (<see cref="PoiType"/>).</summary>
    public string Type { get; private set; }

    /// <summary>Data passed to the <see cref="KnpPoi"/> upon initialization.</summary>
    public VectorStructuredData VectorStructured { get; set; }

    #endregion
}