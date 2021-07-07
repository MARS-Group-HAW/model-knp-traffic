using System.Collections.Generic;
using KrugerNationalPark.Agents;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Options;

namespace KrugerNationalPark.Layers
{
    /// <summary>
    ///     This class represents the agent layer implementation for the <see cref="KnpCar" /> and keeps references
    ///     to all other required layer e.g., the <see cref="ElephantLayer" />
    /// </summary>
    public class StreetLayer : AbstractLayer
    {
        public StreetLayer(ElephantLayer elephantLayer)
        {
            ElephantLayer = elephantLayer;
        }
        
        public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
            UnregisterAgent unregisterAgent = null)
        {
            base.InitLayer(layerInitData, registerAgentHandle, unregisterAgent);

            StreetEnvironment = new SpatialGraphEnvironment(new SpatialGraphOptions
            {
                GraphImports = new List<Input>
                {
                    new()
                    {
                        File = layerInitData.LayerInitConfig.File,
                        InputConfiguration = new InputConfiguration
                        {
                            IsBiDirectedImport = true,
                            Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving}
                        }
                    }
                },
                NodeIndex = true
            });
            
            return true;
        }

        

        [PropertyDescription]
        public ElephantLayer ElephantLayer { get; }
        
        public ISpatialGraphEnvironment StreetEnvironment { get; set; }
        
    }
}