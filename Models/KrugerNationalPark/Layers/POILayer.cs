using System.Collections.Generic;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Import;
using Mars.Interfaces.Model.Options;

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
    }
}