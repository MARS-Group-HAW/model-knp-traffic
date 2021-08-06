using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using KrugerNationalPark.Agents;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Import;
using Mars.Interfaces.Model.Options;

namespace KrugerNationalPark.Layers
{
    /// <summary>
    ///     This class represents the agent layer implementation for the <see cref="KnpCar" /> and keeps references
    ///     to all other required layer e.g., the <see cref="ElephantLayer" />
    /// </summary>
    public class StreetLayer : AbstractLayer
    {
        public StreetLayer()
        {

        }
        
        
        public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle,
            UnregisterAgent unregisterAgentHandle)
        {
            base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

            StreetEnvironment = new SpatialGraphEnvironment(new SpatialGraphOptions
            {
                GraphImports = new List<Source>
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


            // Export StreetLayer as GeoJSON
            //var geoJson = SpatialGraphHelper.ToGeoJson(StreetEnvironment);
            //File.WriteAllText("streetLayer.geojson", geoJson);

            return true;
        }
        
        public ISpatialGraphEnvironment StreetEnvironment { get; set; }
    }
}