using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers
{
    public class PoiLayer : VectorLayer<KnpPoi>
    {
        public override bool InitLayer(LayerInitData layerInitData,
            RegisterAgent registerAgentHandle = null,
            UnregisterAgent unregisterAgentHandle = null)
        {
            base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);


            return true;
        }
    }
}