using System;
using System.Collections.Generic;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers;

public class PoiLayer : VectorLayer<KnpPoi>
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
    public Position GetPositionFromNameAndType(string poiName, string poiType)
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