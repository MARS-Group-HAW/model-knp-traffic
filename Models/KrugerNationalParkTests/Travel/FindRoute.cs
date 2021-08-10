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
        public void TestTimeLimitAB()
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

            var layer = new KnpStreetLayer();
            layer.StreetEnvironment = environment;

            // n1 -> n2 in time
            var rt1 = layer.FindRoute(node1, node3, 10);
            Assert.Equal(2, rt1.Count);
            Assert.Equal(100, rt1.RouteLength);
            Assert.True(GetRouteDuration(rt1) <= 10, "Route took more seconds than limit");

            
            // n1 -> n2 not enough time
            Assert.Throws<ArgumentException>(() => layer.FindRoute(node1, node3, 9));
        }
        
        [Fact]
        public void TestTimeLimitAATriangle()
        {
            // based on: AStarPathSearchTests.cs::TestFindFastestRoute()
            var environment = new SpatialGraphEnvironment();

            var node1 = environment.AddNode(1, 1);
            var node2 = environment.AddNode(2, 1);
            var node3 = environment.AddNode(3, 1);
            
            var node4 = environment.AddNode(2, 2);

            Assert.Equal(1, node1.Position.X);
            Assert.Equal(1, node1.Position.Y);

            //
            //                         n4
            //                    /    |
            //        25m w/ 5m/s     25m w/ 5m/s
            //    /                    |
            // n1 <-- 25m w/ 5m/s ---> n2 <-- 150m w/ 10m/s---> n3
            //  < ------- 5s -------- > < ------- 15s -------- >

            var edge12 = environment.AddEdge(node1, node2, 25);
            edge12.MaxSpeed = 5;
            var edge21 = environment.AddEdge(node2, node1, 25);
            edge21.MaxSpeed = 5;
            var edge23 = environment.AddEdge(node2, node3, 50);
            edge23.MaxSpeed = 10;
            var edge32 = environment.AddEdge(node3, node2, 50);
            edge32.MaxSpeed = 10;
            
            var edge24 = environment.AddEdge(node2, node4, 25);
            edge24.MaxSpeed = 5;
            var edge41 = environment.AddEdge(node4, node1, 25);
            edge41.MaxSpeed = 5;
            
            var layer = new KnpStreetLayer();
            layer.StreetEnvironment = environment;

            
            // n1 -> n1 
            var rt2 = layer.FindRoute(node1, node1, 15);
            Assert.Equal(3, rt2.Count);
            Assert.Equal(75, rt2.RouteLength);
            Assert.True(GetRouteDuration(rt2) <= 15, "Route took more seconds than limit");
        }

        [Fact]
        public void TestTimeLimitAASingle()
        {
            // based on: AStarPathSearchTests.cs::TestFindFastestRoute()
            var environment = new SpatialGraphEnvironment();

            var node1 = environment.AddNode(1, 1);
            var node2 = environment.AddNode(2, 1);
            var node3 = environment.AddNode(3, 1);

            Assert.Equal(1, node1.Position.X);
            Assert.Equal(1, node1.Position.Y);

            // n1 <-- 25m w/ 5m/s ---> n2 <-- 50m w/ 10m/s---> n3
            //  < ------- 5s -------- > < ------- 5s -------- >

            var edge12 = environment.AddEdge(node1, node2, 25);
            edge12.MaxSpeed = 5;
            var edge21 = environment.AddEdge(node2, node1, 25);
            edge21.MaxSpeed = 5;
            var edge23 = environment.AddEdge(node2, node3, 50);
            edge23.MaxSpeed = 10;
            var edge32 = environment.AddEdge(node3, node2, 50);
            edge32.MaxSpeed = 10;
            
            var layer = new KnpStreetLayer();
            layer.StreetEnvironment = environment;

            
            // n1 -> n2 in time
            var rt1 = layer.FindRoute(node1, node3, 10);
            Assert.Equal(2, rt1.Count);
            Assert.Equal(75, rt1.RouteLength);
            Assert.True(GetRouteDuration(rt1) <= 10, "Route took more seconds than limit");

            // n1 -> n2 not enough time
            Assert.Throws<ArgumentException>(() => layer.FindRoute(node1, node3, 9));
            
            // n1 -> n1 
            var rt2 = layer.FindRoute(node1, node1, 10);
            Assert.Equal(2, rt2.Count);
            Assert.Equal(50, rt2.RouteLength);
            Assert.True(GetRouteDuration(rt2) <= 10, "Route took more seconds than limit");

            // n1 -> n1 
            var rt3 = layer.FindRoute(node1, node1, 20);
            Assert.Equal(4, rt3.Count);
            Assert.Equal(150, rt3.RouteLength);
            Assert.True(GetRouteDuration(rt1) <= 20, "Route took more seconds than limit");
        }

                
        [Fact]
        public void TestOneDirection()
        {
            // based on: AStarPathSearchTests.cs::TestFindFastestRoute()
            var environment = new SpatialGraphEnvironment();

            var node1 = environment.AddNode(1, 1);
            var node2 = environment.AddNode(2, 1);
            var node3 = environment.AddNode(2, 2);
            var node4 = environment.AddNode(1, 2);


            Assert.Equal(1, node1.Position.X);
            Assert.Equal(1, node1.Position.Y);

            // 1        2
            // O---10-->O
            // |        |
            // 5        10
            // |        |
            // \/       \/
            // O---30-->O
            // 3        4

            var edge12 = environment.AddEdge(node1, node2, 1);
            edge12.MaxSpeed = 10;
            var edge24 = environment.AddEdge(node2, node4, 1);
            edge24.MaxSpeed = 10;

            var edge13 = environment.AddEdge(node1, node3, 1);
            edge13.MaxSpeed = 5;
            var edge34 = environment.AddEdge(node3, node4, 1);
            edge34.MaxSpeed = 30;

            var layer = new KnpStreetLayer();
            layer.StreetEnvironment = environment;


            var geoJson = SpatialGraphHelper.ToGeoJson(environment);
            File.WriteAllText("environment.geojson", geoJson);


            var rt = layer.FindRoute(node1, node4, 3600);
        }
        

        [Fact]
        public void TestCreateMultipleRandomRoutesOnKNPGraph()
        {
            var layer = new KnpStreetLayer();
            layer.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    File = Path.Combine("resources", "knp_graph.geojson")
                }
            }, null, null);



            var p1 = new Position(31.484812, -24.980938); // Kruger Gate
            var p2 = new Position(31.8938518629925, -25.3581762165958); // Crocodile Bridge
            var n1 = layer.StreetEnvironment.NearestNode(p1);
            var n2 = layer.StreetEnvironment.NearestNode(p2);

            // loop, never stops, points are not reachable in 1h
            //var rt1 = layer.FindRoute(n1, n2, 3600);

            for (var i = 0; i < 10; i++)
            {
                var rt1 = layer.FindRoute(n1, n1, 21600);

                var geoJson = SpatialGraphHelper.ToGeoJson(rt1);
                File.WriteAllText("FindRouteTest_"+i+".geojson", geoJson);
            }
            


            // Random walk with time constraint without "destination"
            //handle.Route = FindRoute(_originNode);
            //VehicleHandle = handle;

           
        }
        
        
        
        [Fact]
        public void FindRouteTest()
        {
            var description = new ModelDescription();
            description.AddLayer<KnpStreetLayer>();
            description.AddLayer<POILayer>();
            description.AddLayer<TouristSchedulingLayer>();
            
            description.AddAgent<Tourist, KnpStreetLayer>();
            
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
                        Name = nameof(KnpStreetLayer),
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
        
        private double GetRouteDuration(Route rt)
        {
            double duration = 0.0;

            if (rt is null)
            {
                return duration;
            }
            
            foreach (var edgeStop in rt)
            {
                var edge = edgeStop.Edge;
                // TODO: MARS method is broken (see next line for correct calculation)
                //tripTime += lastEdge.TravelTime; 
                duration += edge.Length / edge.MaxSpeed;
            }
            return duration;
        }
    }
}