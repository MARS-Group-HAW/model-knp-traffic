using System;
using System.Collections.Generic;
using System.IO;
using KrugerNationalPark.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Xunit;

namespace KrugerNationalParkTests.Travel
{
    public class PoiTests
    {
        [Fact]
        public void getDestinationPOIsTest()
        {
            var layer = new PoiLayer();
            layer.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    File = Path.Combine("resources", "pois.geojson")
                }
            }, null, null);

            var p = new Position(31.484812, -24.980938); // Start at kruger Gate
            var knpPoi = layer.Nearest(p);

            // no results for 0s time (be aware of Crocodile Bridge, two POIs on same spot. one gate, one camp
            var results1 = knpPoi.GetDestinationPois(0);
            Assert.Equal(0, results1.Count);

            // Kurger gate has reachable within 1h / 3600s: 1 Trail camp, 2 Rest camp, 2 Gates

            var results2 = knpPoi.GetDestinationPois(3600.0);
            Assert.Equal(5, results2.Count);
            
            var results3 = knpPoi.GetDestinationPois(3600.0, new List<String> { "Rest camp", "Gate" });
            Assert.Equal(4, results3.Count);
            
            var results4 = knpPoi.GetDestinationPois(3600.0, new List<String> { "Rest camp" });
            Assert.Equal(2, results4.Count);

            var results5 = knpPoi.GetDestinationPois(3600.0, new List<String> { "Gate" });
            Assert.Equal(2, results5.Count);

        }
    }
}