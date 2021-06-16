using System;
using System.Collections.Generic;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Import;
using Mars.Interfaces.Model.Options;

namespace KrugerNationalParkStarter
{


    public static class GetRouteTimings
    {
        public static void Timings()
        {
            
            ISpatialGraphEnvironment se = new SpatialGraphEnvironment(new SpatialGraphOptions
            {
                GraphImports = new List<Source>
                {
                    new()
                    {
                        File = "resources/knp_graph.graphml",
                        InputConfiguration = new InputConfiguration
                        {
                            IsBiDirectedImport = true,
                            Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving}
                        }
                    }
                },
                NodeIndex = true
            });
            

            Position p1 = new Position(31.45122, -25.42001);
            ISpatialNode n1 = se.NearestNode(p1);
            
            Position p2 = new Position(31.59263, -24.98984);
            ISpatialNode n2 = se.NearestNode(p1);



            Route rt = se.FindRoute(n1, n2);
            var tripTime = 0.0; // in seconds
            var tripLength = 0.0;

            List<EdgeStop> edgeStops = rt.Stops;

            tripLength = rt.RouteLength;
            
            
            
            for (int i = 0; i < rt.Count; i++)
            {
                var edge = edgeStops[i].Edge;
                tripTime += edge.Length / edge.MaxSpeed;
            }

            Console.WriteLine(tripTime);
            Console.WriteLine(tripLength);

        }
    }
}