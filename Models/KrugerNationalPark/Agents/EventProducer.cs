using System;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;

namespace KrugerNationalPark.Agents
{
    public class EventProducer : IAgent<VisitorTravelerLayer>
    {
        private VisitorTravelerLayer _travellerLayer;

        private EventsCollection EventsCollection;

        #region Initialization

        public void Init(VisitorTravelerLayer layer)
        {
            _travellerLayer = layer;
            EventsCollection = new EventsCollection();
        }

        #endregion Initialization

        #region Tick

        public void Tick()
        {
            var random = new Random();
            if (random.NextDouble() >= 0.5)
            {
                var eventStartTime = _travellerLayer.Context.CurrentTimePoint.GetValueOrDefault();
                var node = _travellerLayer.SpatialGraphMediatorLayer.Environment.GetRandomNode();
                var edges = node.OutgoingEdges;

                foreach (var edge in edges.Values)
                    if (edge.Length > 100)
                    {
                        // put event on a random POINT of the given edge, so they are not always on 
                        // actual nodes of the graph
                        var rndEdgePosI = new Random();
                        var i = rndEdgePosI.Next(edge.Geometry.Length);
                        var pos = edge.Geometry[i];

                        var e = new KnpEvent(pos, eventStartTime);

                        // todo: this is just a random radius to show on kepler.gl, refactor so agents actually use it
                        e.Radius = (int) (random.NextDouble() * 1000);
                        MarsEventHandler.Instance.Invoke(e);

                        EventsCollection.Add(e);
                        // calling the log function for ALL events makes the sim really slow
                        //EventsCollection.TearDown();
                    }
            }
        }

        #endregion Tick

        #region Properties

        public Guid ID { get; set; }

        #endregion Properties

        #region Methods

        #endregion Methods
    }
}