using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Misc;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Layers;

/// <summary>
/// The <see cref="PointsOfInterest"/> holds the POIs of the KNP (<see cref="KnpPoi"/>) to and from which
/// agents can travel.
/// </summary>
/// <remarks>Requires appropriate vector data for initialization. Configurable via config.json.</remarks>
public class PointsOfInterest : VectorLayer<KnpPoi>
{
    #region Initialization

    /// <summary>Initialization routine of the <see cref="PointsOfInterest"/> layer.</summary>
    /// <param name="layerInitData">Initialization data passed to the <see cref="PointsOfInterest"/> layer.</param>
    /// <param name="registerAgentHandle">A handle for registering agents to the simulation context.</param>
    /// <param name="unregisterAgentHandle">A handle for unregistering agents from the simulation context.</param>
    /// <returns>A boolean that indicates if initialization was successful</returns>
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgentHandle = null)
    {
        var initSuccessful = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

        return initSuccessful;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a random <see cref="KnpPoi"/> of the given type (<see cref="PoiType"/>) that is in the given geometry.
    /// </summary>
    /// <param name="poiTypes">The given <see cref="PoiType"/>.</param>
    /// <param name="geometry">The given geometry.</param>
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

    /// <summary>Gets all <see cref="KnpPoi"/> objects of the given <see cref="PoiType"/>s.</summary>
    /// <param name="poiTypes">The given types</param>
    /// <returns>A list containing the matching <see cref="KnpPoi"/> objects.</returns>
    private IEnumerable<KnpPoi> FindAllPoisOfType(IEnumerable<string> poiTypes)
    {
        return Features.Where(feature => poiTypes.Contains((string)feature.VectorStructured.Attributes["type"]))
            .Cast<KnpPoi>().ToList();
    }

    /// <summary>Gets all <see cref="KnpPoi"/> objects that are in the given geometry.</summary>
    /// <param name="geometry">The given geometry.</param>
    /// <returns></returns>
    private IEnumerable<KnpPoi> FindAllPoisInGeometry(Geometry geometry)
    {
        return (from KnpPoi knpPoi in Features
            let knpPoiPositionAsPoint = new Point(knpPoi.Position.X, knpPoi.Position.Y)
            where geometry.Contains(knpPoiPositionAsPoint)
            select knpPoi).ToList();
    }

    /// <summary>
    /// Gets the <see cref="KnpPoi"/> with the geospatial position that is nearest to the given position.
    /// </summary>
    /// <param name="position">The given position</param>
    /// <returns>The <see cref="KnpPoi"/></returns>
    public KnpPoi GetNearestKnpPoi(Position position)
    {
        return Nearest(position.PositionArray);
    }

    /// <summary>
    /// Returns the position of a <see cref="KnpPoi"/>, given its name and <see cref="PoiType"/>.
    /// </summary>
    /// <param name="poiName">The name of the POI</param>
    /// <param name="poiType">The type of the POI (<see cref="PoiType"/></param>)
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

    /// <summary>
    /// Gets the position of a randomly selected <see cref="KnpPoi"/> that is of the given type.
    /// </summary>
    /// <param name="poiType">The given <see cref="PoiType"/>.</param>
    /// <returns>The position of the <see cref="KnpPoi"/>.</returns>
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