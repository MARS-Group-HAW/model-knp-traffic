using System;
using System.Collections.Generic;
using System.IO;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using Mars.Components.Environments;
using Mars.Components.Starter;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Xunit;

namespace KrugerNationalParkTests.Travel
{
    public class FindRoute
    {
        [Fact]
        public void TestTimeLimit()
        {
            // based on: AStarPathSearchTests.cs::TestFindFastestRoute()
            var environment = new SpatialGraphEnvironment();

            var node1 = environment.AddNode(1, 1);
            var node2 = environment.AddNode(2, 1);
            var node3 = environment.AddNode(3, 1);

            Assert.Equal(1, node1.Position.X);
            Assert.Equal(1, node1.Position.Y);

            // n1 --- 50m w/ 10m/s---> n2 --- 50m w/ 10m/s---> n3
            //  < ------- 5s -------- > < ------- 5s -------- >

            var edge12 = environment.AddEdge(node1, node2, 50);
            edge12.MaxSpeed = 10;
            var edge23 = environment.AddEdge(node2, node3, 50);
            edge23.MaxSpeed = 10;

            var layer = new StreetLayer();
            layer.StreetEnvironment = environment;

            var rt = layer.FindRoute(node1, node3, 10);
        }

        [Fact]
        public void FindRouteTest()
        {
            var description = new ModelDescription();
            description.AddLayer<StreetLayer>();
            description.AddLayer<POILayer>();
            description.AddLayer<TouristSchedulingLayer>();
            
            description.AddAgent<Tourist, StreetLayer>();
            
            description.AddEntity<KnpCar>();
            
            
            var start = new DateTime(2019, 1, 1, 6, 0, 00);
            var end = start + TimeSpan.FromHours(4);
            
            var simConfig = new SimulationConfig
            {
                Globals =
                {
                    StartPoint = start,
                    EndPoint = end,
                    DeltaTUnit = TimeSpanUnit.Seconds,
                    OutputTarget = OutputTargetType.Csv
                },
                LayerMappings = new List<LayerMapping>
                {
                    new()
                    {
                        Name = nameof(StreetLayer),
                        File = Path.Combine("resources", "knp_graph.geojson")
                    },
                    new()
                    {
                        Name = nameof(POILayer),
                        File = Path.Combine("resources", "pois.geojson")
                    },
                    new()
                    {
                        Name = nameof(TouristSchedulingLayer),
                        File = Path.Combine("resources", "TouristScheduler_FindRoute.csv")
                    }
                },
                EntityMappings = new List<EntityMapping>
                {
                    new()
                    {
                        Name = "KnpCar",
                        File = Path.Combine("resources", "car.csv")
                    }
                },
                AgentMappings =
                {
                    new AgentMapping
                    {
                        Name = nameof(Tourist), InstanceCount = 1
                    }
                }
            };

            var result = SimulationStarter.Start(description, simConfig).Run();


            
            
            /*var description = new ModelDescription();

            description.AddLayer<StreetLayer>();
            description.AddAgent<Tourist, StreetLayer>();

            
            var layer = new StreetLayer();
            layer.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    File = Path.Combine("resources", "knp_graph.geojson")
                }
            }, null, null);

            var t = new Tourist();
            t.Init(layer);*/


        }
        
    }
}