using System.Collections.Generic;
using System.IO;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Import;
using Xunit;

namespace KrugerNationalParkTests.SpatialGraph
{
    public class GraphCompleteTest
    {
        [Fact]
        public void TestOneWayImport()
        {
            var path = Path.Combine("resources", "knp_graph.geojson");
            Assert.True(File.Exists(path));
            
            var environment = new SpatialGraphEnvironment(new Source
            {
                File = path,
                InputConfiguration = new InputConfiguration
                {
                    Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving},
                    IsBiDirectedImport = true
                }
            });
            
            foreach (var n1 in environment.Nodes)
            {
                foreach (var n2 in environment.Nodes)
                {
                    var rt = environment.FindRoute(n1, n2);
                    Assert.NotNull(rt);
                }
            }
            
            
            
        }
    }
}