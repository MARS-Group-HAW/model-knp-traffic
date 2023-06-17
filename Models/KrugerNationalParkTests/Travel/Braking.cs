using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc.Events;
using Mars.Common.Core.Collections.HashStructures;
using Mars.Components.Environments;
using Mars.Components.Services.Events;
using Mars.Components.Starter;
using Mars.Core.Data.Wrapper.Memory;
using Mars.Core.Simulation.Entities;
using Mars.Interfaces;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Converters;
using ServiceStack;
using SOHDomain.Graph;
using SOHTests;
using SOHTests.Commons.Agent;
using Xunit;
using Feature = ServiceStack.Feature;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalParkTests.Travel
{
    public class Braking
    {



        [Fact]
        public void brakeOnEvent()
        {
            var description = new ModelDescription();
            
            description.AddLayer<SpatialGraphMediatorLayer>(new[] {typeof(ISpatialGraphLayer)});
            description.AddLayer<KnpRoadNetwork>();   
            description.AddLayer<PointsOfInterest>();
            description.AddLayer<VisitorScheduler>();
            
            description.AddAgent<Visitor, KnpRoadNetwork>();
            
            description.AddEntity<KnpCar>();
            
            var start = new DateTime(2019, 1, 1, 6, 0, 00);
            var end = start + TimeSpan.FromHours(1);

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
                    new LayerMapping
                    {
                        Name = nameof(KnpRoadNetwork)
                    },
                    new LayerMapping
                    {
                        Name = nameof(SpatialGraphMediatorLayer),
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                File = "resources/networks/drive_graph_veddeler_damm.geojson",
                                //File = "resources/networks/line.geojson",
                                //File = "resources/roads_all_2019_public.geojson",
                                InputConfiguration = new InputConfiguration
                                {
                                    Modalities = new HashSet<SpatialModalityType>{ SpatialModalityType.CarDriving },
                                    IsBiDirectedImport = true
                                }
                            }
                        }
                    },
                    new LayerMapping
                    {
                        Name = nameof(PointsOfInterest),
                        File = "resources/pois.geojson"
                    },
                    new LayerMapping
                    {
                        Name = nameof(VisitorScheduler),
                        File = "resources/TouristScheduler_brakeOnEvent.csv"
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
            
            var simulation = SimulationStarter.Build(description, simConfig);
            simulation.PrepareSimulation(description, simConfig);
            var eventsCollection = new EventsCollection();
            
            SimulationWorkflowState result = null;
            for (var i = 0; i < 3600; i++) {

                if (i == 200)
                {
                    var pos = new Position(x:  9.991003079370975, y: 53.52606263568983);
                    var e = new KnpEvent(pos, start);
                    e.Radius = (int) 50;
                    MarsEventHandler.Instance.Invoke(e);
            
                    eventsCollection.Add(e);
                }

                result = simulation.StepSimulation();
            }
            
            eventsCollection.TearDown();
            simulation.Dispose(); // explicitly call to create trips.geojson
        }

        [Fact]
        public void test()
        {
            const double speed = 30 / 3.6;

            var graph = new SpatialGraphEnvironment(ResourcesConstants.RingNetwork);
            graph.Edges.Values.First().MaxSpeed = speed; // set speed limit

            var context = SimulationContext.Start2020InSeconds;

            var visitor = new Visitor();
            
            
            /* TODO review this (commented out during refactoring of KnpRoadNetwork : AbstractLayer to KnpRoadNetwork : SpatialGraphMediatorLayer
            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;
            */

            var layer = new KnpRoadNetwork();
            // layer.SpatialGraphMediatorLayer = mediator;
            
            
            var driver = new InfiniteSteeringDriver(context, 0, graph, 0, speed)
            {
                //set start speed
                Car =
                {
                    Velocity = speed
                }
            };

            Assert.False(driver.BrakingActivated);

            const int tickToBrake = 10;
            var tickWhenStopped = -1;
            var distanceBeforeBrake = -1d;

            for (var tick = 0; tick < 50; tick++, context.UpdateStep())
            {
                var velocityLastTick = driver.Velocity;
                driver.Tick();

                switch (tick)
                {
                    case tickToBrake:
                        driver.BrakingActivated = true;
                        distanceBeforeBrake = driver.PositionOnCurrentEdge;
                        break;
                    case > tickToBrake:
                        Assert.True(driver.BrakingActivated);
                        Assert.True(driver.Velocity <= velocityLastTick);
                        
                        if (tickWhenStopped < 0 && driver.Velocity == 0) tickWhenStopped = tick;
                        break;
                }
            }

            Assert.Equal(0.0, driver.Velocity);

            var brakingTime = tickWhenStopped - tickToBrake;
            Assert.Equal(2, brakingTime);

            var brakingDistance = driver.PositionOnCurrentEdge - distanceBeforeBrake;
            Assert.InRange(brakingDistance, 3, 4);
        }
        
        
        
        
        
        
        
        [Fact]
        public void TestBrakingOnLongStreetWithEvent()
        {

            /* var graph = new SpatialGraphEnvironment(Path.Combine("resources", "networks",  "hamburg_south_graph_filtered.geojson"));

            
            var env = new SpatialGraphMediatorLayer();
            
            env.
            
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
            edge23.MaxSpeed = 10; */
            
        }
    }
}