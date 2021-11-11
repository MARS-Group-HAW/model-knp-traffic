using System.IO;
using System.Linq;
using KrugerNationalPark.Agents;
using Mars.Components.Environments;
using Mars.Interfaces;
using Mars.Interfaces.Model;
using SOHDomain.Graph;
using SOHTests;
using SOHTests.Commons.Agent;
using Xunit;

namespace KrugerNationalParkTests.Travel
{
    public class Braking
    {



        [Fact]
        public void test()
        {
            const double speed = 30 / 3.6;

            var graph = new SpatialGraphEnvironment(ResourcesConstants.RingNetwork);
            graph.Edges.Values.First().MaxSpeed = speed; // set speed limit

            var context = SimulationContext.Start2020InSeconds;

            var visitor = new Tourist();
            
            
            var mediator = new SpatialGraphMediatorLayer();
            mediator.Environment = environment;

            var layer = new VisitorTravelerLayer();
            layer.SpatialGraphMediatorLayer = mediator;
            
            
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