using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Common.IO;
using Mars.Components.Environments;
using Mars.Components.Services.Events;
using Mars.Components.Starter;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using NuGet.Frameworks;
using ServiceStack.Text;
using SOHDomain.Graph;
using Xunit;

namespace KrugerNationalParkTests.Travel
{
    public class FindRoute
    {

        [Fact]
        public void TestFindRouteWithAccessAttribute()
        {
            var environment = new SpatialGraphEnvironment();

            var node1 = environment.AddNode(1, 1);
            var node2 = environment.AddNode(2, 1);
            var node3 = environment.AddNode(2, 2);
            var node4 = environment.AddNode(1, 2);
            var node5 = environment.AddNode(3, 2);

            Assert.Equal(1, node1.Position.X);
            Assert.Equal(1, node1.Position.Y);

            // 1        2
            // O---10-->O
            // |        |
            // 5        10
            // |        |
            // \/       \/
            // O---10-->O---10-->0
            // 3        4        5

            var visitorAttritbutes = new Dictionary<string, object>();
            visitorAttritbutes.Add("access", "Public");

            var osvAttritbutes = new Dictionary<string, object>();
            osvAttritbutes.Add("access", "Staff");
            

            var edge12 = environment.AddEdge(node1, node2, 10, visitorAttritbutes, SpatialModalityType.CarDriving);
            var edge24 = environment.AddEdge(node2, node4, 10, visitorAttritbutes, SpatialModalityType.CarDriving);
            var edge13 = environment.AddEdge(node1, node3, 5, osvAttritbutes , SpatialModalityType.CarDriving);
            var edge34 = environment.AddEdge(node3, node4, 10, osvAttritbutes, SpatialModalityType.CarDriving);
            environment.AddEdge(node4, node5, 10, osvAttritbutes, SpatialModalityType.CarDriving);
            
            // test that visitor takes the longer route, and not the shorter OSV / Staff route
            var route = environment.FindRoute(node1, node4, (_, edge, _) => edge.Length,
                edge => ((String) edge.Attributes["access"] == "Public"));
            
            Assert.NotNull(route);
            Assert.Equal(2, route.Count);
            Assert.False(route.GoalReached);
            Assert.Equal(edge12, route[0].Edge);
            Assert.Equal(edge24, route[1].Edge);
            
            // test that OSV takes the correct route
            String[] allowed = {"Public", "Staff"};
            var route2 = environment.FindRoute(node1, node4, (_, edge, _) => edge.Length,
                edge => (allowed.Contains((String) edge.Attributes["access"])));
            
            Assert.NotNull(route2);
            Assert.Equal(2, route2.Count);
            Assert.False(route2.GoalReached);
            Assert.Equal(edge13, route2[0].Edge);
            Assert.Equal(edge34, route2[1].Edge);
            
            
            // Visitor can't reach node 5
            var noRoute = environment.FindRoute(node1, node5, (_, edge, _) => edge.Length,
                edge => ((String) edge.Attributes["access"] == "Public"));

            Assert.Null(noRoute);
        }
        
        
                [Fact]
        public void TestFindRouteWithAccessAttributeOnVisitorLayer()
        {
            var environment = new SpatialGraphEnvironment();
            
            var node1 = environment.AddNode(0, 1);
            var node2 = environment.AddNode(1, 1);
            var node3 = environment.AddNode(0, 0);
            var node4 = environment.AddNode(1, 0);
            var node5 = environment.AddNode(2, 0);
            
            // 1   v    2
            // O---10-->O
            // |        |
            // 10 o      10 v
            // |        |
            // \/   o   \/   o
            // O<--10-->O---10-->0
            // 3        4        5

            var visitorAttritbutes = new Dictionary<string, object>();
            visitorAttritbutes.Add("access", "Public");

            var osvAttritbutes = new Dictionary<string, object>();
            osvAttritbutes.Add("access", "Staff");
            
            var privateAttritbutes = new Dictionary<string, object>();
            privateAttritbutes.Add("access", "Private");


            var edge12 = environment.AddEdge(node1, node2, 10, visitorAttritbutes, SpatialModalityType.CarDriving);
            edge12.MaxSpeed = 10;
            var edge24 = environment.AddEdge(node2, node4, 10, visitorAttritbutes, SpatialModalityType.CarDriving);
            edge24.MaxSpeed = 10;
            var edge13 = environment.AddEdge(node1, node3, 10, osvAttritbutes , SpatialModalityType.CarDriving);
            edge13.MaxSpeed = 1;
            var edge34 = environment.AddEdge(node3, node4, 10, osvAttritbutes, SpatialModalityType.CarDriving);
            edge34.MaxSpeed = 10;
            var edge43 = environment.AddEdge(node4, node3, 10, osvAttritbutes, SpatialModalityType.CarDriving);
            edge43.MaxSpeed = 10;
            var edge45 = environment.AddEdge(node4, node5, 10, osvAttritbutes, SpatialModalityType.CarDriving);
            edge45.MaxSpeed = 10;
            var edge14 = environment.AddEdge(node1, node4, 10, privateAttritbutes, SpatialModalityType.CarDriving);
            edge14.MaxSpeed = 10;
            
            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;

            
            // test that visitor takes the longer route, and not the shorter OSV / Staff route
            var route = layer.FindOsvRoute(node1, node5, 5);
            AssertEdgeAccess(route, new String[] {"Public", "Staff"});

            var geoJson = SpatialGraphHelper.ToGeoJson(route);
            File.WriteAllText("TestFindRouteWithAccessAttributeOnVisitorLayer.geojson", geoJson);
            
            // test that visitor takes the longer route, and not the shorter OSV / Staff route
            var route2 = layer.FindVisitorRoute(node1, node4, 2);
            AssertEdgeAccess(route2, new String[] {"Public"});
            
            Assert.Throws<ArgumentException>(() => layer.FindOsvRoute(node1, node4, 1));

            Assert.Throws<ArgumentException>(() => layer.FindVisitorRoute(node1, node4, 1));
        }

        private void AssertEdgeAccess(Route rt, String[] access)
        {
            for (int i = 0; i < rt.Count; i++)
            {
                var edge = rt[i].Edge;
                Assert.Contains(edge.Attributes["access"], access);
            }
        }
        
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

            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;
            
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

            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;


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

            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;

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

            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;


            var geoJson = SpatialGraphHelper.ToGeoJson(environment);
            File.WriteAllText("environment.geojson", geoJson);


            var rt = layer.FindRoute(node1, node4, 3600);
        }



        
        [Fact]
        public void TestCreateMultipleRandomRoutesOnKNPGraph()
        {
            var mediator = new SpatialGraphMediatorLayer();
            mediator.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    Inputs = new List<Input>
                    {
                        new Input
                        {
                            File = Path.Combine("resources", "knp_graph.geojson"),
                            InputConfiguration = new InputConfiguration
                            {
                                Modalities = new HashSet<SpatialModalityType>{SpatialModalityType.CarDriving},
                                IsBiDirectedImport = true
                            }
                        }
                    }
                }
            }, null, null);


            var p1 = new Position(31.484812, -24.980938); // Kruger Gate
            var p2 = new Position(31.8938518629925, -25.3581762165958); // Crocodile Bridge
            var n1 = mediator.Environment.NearestNode(p1);
            var n2 = mediator.Environment.NearestNode(p2);
            
            // loop, never stops, points
            // are not reachable in 1h
            //var rt1 = layer.FindRoute(n1, n2, 3600);
            
            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;
            
            
            for (var i = 0; i < 10; i++)
            {
                var rt1 = layer.FindRoute(n1, n1, 21600);

                var geoJson = SpatialGraphHelper.ToGeoJson(rt1);
                File.WriteAllText("FindRouteTest_" + i + ".geojson", geoJson);
            }


            // Random walk with time constraint without "destination"
            //handle.Route = FindRoute(_originNode);
            //VehicleHandle = handle;
        }

/*
        [Fact]
        public void FindRouteTest()
        {
            var description = new ModelDescription();
            description.AddLayer<KnpStreetLayer>();
            description.AddLayer<POILayer>();
            description.AddLayer<VisitorSchedulingLayer>();

            description.AddAgent<Visitor, KnpStreetLayer>();

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
                        Name = nameof(VisitorSchedulingLayer),
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
                        Name = nameof(Visitor), InstanceCount = 1,
                        Outputs = new List<Output>
                        {
                            new()
                            {
                                OutputTarget = OutputTargetType.Trips,
                                OutputConfiguration = new OutputConfiguration()
                                {
                                    TripsDiscriminatorFields = new[] { "ActiveCapability" }
                                }
                            },
                            new()
                            {
                                OutputTarget = OutputTargetType.Csv
                            }
                        }
                    }
                }
            };

            File.WriteAllText("simConfig.json", simConfig.Serialize());
            

            var result = SimulationStarter.Start(description, simConfig).Run();


            /*var description = new ModelDescription();

            description.AddLayer<StreetLayer>();
            description.AddAgent<Visitor, StreetLayer>();

            
            var layer = new StreetLayer();
            layer.InitLayer(new LayerInitData
            {
                LayerInitConfig =
                {
                    File = Path.Combine("resources", "knp_graph.geojson")
                }
            }, null, null);

            var t = new Visitor();
            t.Init(layer);*/
        //}
        

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