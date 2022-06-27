using System;
using System.Collections.Generic;
using System.Text.Json;
using KrugerNationalPark.Misc;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers;

public class KnpPoi : IVectorFeature
{
    public TripOrigin infoList;

    public Position Position { get; private set; }

    public string Name { get; private set; }

    public string Type { get; private set; }

    public VectorStructuredData VectorStructured { get; private set; }

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

    public void Update(VectorStructuredData data)
    {
        throw new NotImplementedException();
    }

    // TODO: Move this to PoiLayer (to encapsulate KnpPoi a little more)?
    /// <summary>
    ///     Returns a list of POIs that satisfy the given travel time constraint and POI type constraint
    /// </summary>
    /// <param name="timeLimit"></param> maximum amount of travel time in seconds
    /// <param name="allowedTypes"></param> types of POIs that are requested as travel destinations
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
}