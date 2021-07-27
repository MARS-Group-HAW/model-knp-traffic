using System;
using System.Collections.Generic;
using System.IO;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Xunit;

namespace KrugerNationalParkTests.Travel
{
    public class FindRoute
    {
        [Fact]
        public void FindRouteTest()
        {
            
            var layer = new StreetLayer();
            layer.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    File = Path.Combine("resources", "knp_graph.graphml")
                }
            }, null, null);


            var t = new Tourist();
            t.Init(layer);


        }
        
    }
}