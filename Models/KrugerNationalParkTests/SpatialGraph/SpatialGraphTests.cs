using System;
using System.Collections.Generic;
using System.IO;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Xunit;


namespace KrugerNationalParkTests.SpatialGraph
{
    public class SpatialGraphTests
    {
        [Fact]
        public void TestOneWayImport2()
        {
            var path = Path.Combine("resources", "kruger_drive_loop_test.geojson");
            Assert.True(File.Exists(path));
            
            var environment = new SpatialGraphEnvironment(new Input
            {
                File = path,
                InputConfiguration = new InputConfiguration
                {
                    Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving},
                    IsBiDirectedImport = true
                }
            });
            
            Assert.Equal(4, environment.Edges.Count);
        }
        

        [Fact]
        public void TestOneWayImport()
        {
            var path = Path.Combine("resources", "kruger_drive_loop_test.geojson");
            Assert.True(File.Exists(path));
            
            
            var environment1 = new SpatialGraphEnvironment(new Input
            {
                File = path,
                InputConfiguration = new InputConfiguration
                {
                    Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving},
                    IsBiDirectedImport = false
                }
            });
            
            Assert.Equal(3, environment1.Edges.Count);

            var n1 = environment1.NodesMap[1];
            var n2 = environment1.NodesMap[2];

            var rt = environment1.FindRoute(n1, n2);
            
            
            File.WriteAllText("kruger_drive_loop_test_export_nodbidirectional.geojson", SpatialGraphHelper.ToGeoJson(environment1));

            
            var environment = new SpatialGraphEnvironment(new Input
            {
                File = path,
                InputConfiguration = new InputConfiguration
                {
                    Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving},
                    IsBiDirectedImport = true
                }
            });
            

            File.WriteAllText("kruger_drive_loop_test_export.geojson", SpatialGraphHelper.ToGeoJson(environment));
            
            Assert.Equal(4, environment.Edges.Count);
            
            Console.WriteLine("hello");
        }
        
    }
}