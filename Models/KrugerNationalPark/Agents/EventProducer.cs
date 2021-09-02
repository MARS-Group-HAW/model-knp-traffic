using System;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;

namespace KrugerNationalPark.Agents
{
    public class EventProducer : IAgent<KnpStreetLayer>
    {
        private KnpStreetLayer _streetLayer;

        #region Initialization

        public void Init(KnpStreetLayer layer)
        {
            _streetLayer = layer;
        }

        #endregion Initialization

        #region Tick

        public void Tick()
        {
            var random = new Random();
            if (random.NextDouble() >= 0.5)
            {
                var eventStartTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                var node = _streetLayer.StreetEnvironment.GetRandomNode();
                MarsEventHandler.Instance.Invoke(new KnpEvent(node.Position, eventStartTime));
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