using System;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;

namespace KrugerNationalPark.Agents;

/// <summary>
/// The <see cref="EventProducer"/> creates temporary wildlife sighting events (<see cref="KnpEvent"/>) along the road
/// segments of the <see cref="KnpRoadNetwork"/>.
/// </summary>
/// <remarks>Configurable via and spawned by <see cref="EventProducerScheduler"/> (see scheduler CSV file).</remarks>
public class EventProducer : IAgent<KnpRoadNetwork>
{
    #region Initialization

    /// <summary>Initialization routine of the <see cref="EventProducer"/> agent.</summary>
    /// <param name="layer">Reference to the <see cref="KnpRoadNetwork"/> on which the <see cref="Commuter"/>
    /// lives.</param>
    public void Init(KnpRoadNetwork layer)
    {
        _travellerLayer = layer;
        _eventsCollection = new EventsCollection();
    }

    #endregion Initialization

    #region Tick

    /// <summary>Behaviour routine of the <see cref="EventProducer"/>.</summary>
    /// <remarks>
    /// Includes creation of <see cref="KnpEvent"/> instances at random locations of the <see cref="KnpRoadNetwork"/>.
    /// </remarks>
    public void Tick()
    {
        var random = new Random();
        if (random.NextDouble() >= 0.9)
        {
            var eventStartTime = _travellerLayer.Context.CurrentTimePoint.GetValueOrDefault();
            var node = _travellerLayer.Environment.GetRandomNode();
            var edges = node.OutgoingEdges;

            foreach (var edge in edges.Values)
                if (edge.Length > 100)
                {
                    // put event on a random POINT of the given edge, so they are not always on 
                    // actual nodes of the graph
                    var rndEdgePosI = new Random();
                    var i = rndEdgePosI.Next(edge.Geometry.Length);
                    var pos = edge.Geometry[i];

                    var e = new KnpEvent(pos, eventStartTime)
                    {
                        // todo: this is just a random radius to show on kepler.gl, refactor so agents actually use it
                        Radius = (int) (random.NextDouble() * 1000)
                    };

                    MarsEventHandler.Instance.Invoke(e);

                    _eventsCollection.Add(e);
                    // calling the log function for ALL events makes the sim really slow
                    //_eventsCollection.TearDown();
                }
        }
    }

    #endregion Tick

    #region Properties

    /// <summary>Unique identifier of the <see cref="Visitor"/> agent.</summary>
    public Guid ID { get; set; }
        
    /// <summary>Reference to the <see cref="KnpRoadNetwork"/>, which holds the road network of the KNP.</summary>
    private KnpRoadNetwork _travellerLayer;

    /// <summary>Collection of produced <see cref="KnpEvent"/> instances.</summary>
    private EventsCollection _eventsCollection;

    #endregion Properties
}