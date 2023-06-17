using System;
using System.Collections.Generic;
using System.Linq;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Layers;

public class PointsOfInterest : VectorLayer<KnpPoi>
{
    #region Initialization

    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgentHandle = null)
    {
        base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

        return true;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Returns a random KnpPoi of the given type that is located within the given geometry
    /// </summary>
    /// <param name="poiTypes"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    public IEnumerable<KnpPoi> GetKnpPoisOfTypeInGeometry(IEnumerable<string> poiTypes, Geometry geometry)
    {
        // TODO test this
        // TODO what to do if either collection is empty?
        var poisOfTypes = FindAllPoisOfType(poiTypes);
        var poisInGeometry = FindAllPoisInGeometry(geometry);
        var poisOfTypeInGeometry = poisOfTypes.Intersect(poisInGeometry);

        return poisOfTypeInGeometry;
    }

    /// <summary>
    ///     Returns all KnpPois of the given types
    /// </summary>
    /// <param name="poiTypes">The given types</param>
    /// <returns>A list containing the matching KnpPois</returns>
    private IEnumerable<KnpPoi> FindAllPoisOfType(IEnumerable<string> poiTypes)
    {
        return Features.Where(feature => poiTypes.Contains((string)feature.VectorStructured.Attributes["type"])).Cast<KnpPoi>().ToList();
    }

    private IEnumerable<KnpPoi> FindAllPoisInGeometry(Geometry geometry)
    {
        return (from KnpPoi knpPoi in Features let knpPoiPositionAsPoint = new Point(knpPoi.Position.X, knpPoi.Position.Y) where geometry.Contains(knpPoiPositionAsPoint) select knpPoi).ToList();
    }

    /// <summary>
    ///     Returns the KnpPoi with the nearest geospatial position to the given position
    /// </summary>
    /// <param name="position">The given position</param>
    /// <returns>The KnpPoi with the nearest geospatial position to the given position</returns>
    public KnpPoi GetNearestKnpPoi(Position position)
    {
        return Nearest(position.PositionArray);
    }

    /// <summary>
    ///     Returns the position of a POI, given the name and type of the POI
    /// </summary>
    /// <param name="poiName">The name of the POI for which the position is requested</param>
    /// <param name="poiType">The type of the POI for which the position is requested</param>
    public Position GetPoiPositionOfNameAndType(string poiName, string poiType)
    {
        var poiPosition = new Position();

        foreach (var feature in Features)
        {
            if ((string)feature.VectorStructured.Attributes["name"] == poiName &&
                (string)feature.VectorStructured.Attributes["type"] == poiType)
            {
                poiPosition = ((KnpPoi)feature).Position;
                break;
            }
        }

        return poiPosition;
    }

    public Position GetRandomPoiPositionOfType(string poiType)
    {
        var pois = new List<Position>();

        foreach (var feature in Features)
        {
            if ((string)feature.VectorStructured.Attributes["type"] == poiType)
            {
                pois.Add(((KnpPoi)feature).Position);
            }
        }

        var randomPoiIndex = new Random().Next(pois.Count);
        return pois[randomPoiIndex];
    }

    #endregion
}