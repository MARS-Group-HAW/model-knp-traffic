using System;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;

namespace KrugerNationalPark.Agents;

public class EventProducer : IAgent<KnpRoadNetwork>
{
    #region Initialization

    public void Init(KnpRoadNetwork layer)
    {
        _travellerLayer = layer;
        _eventsCollection = new EventsCollection();
    }

    #endregion Initialization

    #region Tick

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

    public Guid ID { get; set; }
        
    private KnpRoadNetwork _travellerLayer;

    private EventsCollection _eventsCollection;

    #endregion Properties

    #region Methods

    #endregion Methods
}