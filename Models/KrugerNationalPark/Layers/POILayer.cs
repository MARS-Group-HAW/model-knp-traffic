using System.Linq;
using Mars.Common.Core;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers
{
    public class POILayer : VectorLayer<KnpPoi>
    {
        public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle,
            UnregisterAgent unregisterAgentHandle)
        {
            base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

            return true;
        }

        public KnpPoi Nearest(Position position)
        {
            return Explore(position.PositionArray).FirstOrDefault();
        }
    }
}