using System;
using System.Collections.Generic;
using System.IO;
using Mars.Common.IO;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Import;
using Mars.Interfaces.Model.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using KrugerNationalPark.Misc;

namespace KrugerNationalParkStarter
{


    public static class GetRouteTimings
    {
        public static void Timings()
        {
            // Build Spatial graph env.
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
            
            
            // load POIs
            String header ="";
            var pois = new List<Position>();
            var lines = new List<String>();
            
            String fileOutput =  "";
            
            using(var reader = new StreamReader(@"./camp_waypoints.csv"))
            {
                var skip = true;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');

                    if (skip)
                    {
                        header = line +  ";RouteInfoList";
                        skip = false;
                        continue;
                    }

                    Position p = new Position(Convert.ToDouble(values[2]), Convert.ToDouble(values[3]));
                    pois.Add(p);
                    lines.Add(line);
                }
            }

            fileOutput += header + "\n";

            for (int i = 0; i < pois.Count; i++)
            {
                Position p1 = pois[i];
                ISpatialNode n1 = se.NearestNode(p1);

                //var timings = new List<(Position, double, double)>();
                var timings = new List<RouteInfoPOCO>();  
                    
                    /*
                     * {
                     *  p1 => [
                     *      [p1, p2,  duration, length],
                     *      [p1, 2p2',  duration, length],
                     *      [p1, p2'', duration, length]
                        ]
                     * }
                     *
                     * 
                     */
                
                for (int j = 0; j < pois.Count; j++)
                {
                    Position p2 = pois[j];
                    ISpatialNode n2 = se.NearestNode(p2);

                    if (p1.Equals(p2))
                    {
                        continue;
                    }
                    
                    Route rt = se.FindRoute(n1, n2);
                    List<EdgeStop> edgeStops = rt.Stops;
                    
                    var tripTime = 0.0; // in seconds
                    var tripLength = 0.0;
                    tripLength = rt.RouteLength;

                    for (int k = 0; k < rt.Count; k++)
                    {
                        var edge = edgeStops[k].Edge;
                        tripTime += edge.Length / edge.MaxSpeed;
                    }

                    var routeInfoPoco = new RouteInfoPOCO();
                    routeInfoPoco.Origin = p1; 
                    routeInfoPoco.Destination = p2;
                    routeInfoPoco.Duration = tripTime;
                    routeInfoPoco.Length = tripLength;
                    
                    timings.Add(routeInfoPoco);
                }

                RouteInfoListPOCO rilp = new RouteInfoListPOCO();
                rilp.RouteInfoList = timings;
                
                lines[i] += ';'  + JsonSerializer.Serialize(rilp);
                fileOutput += lines[i]  + "\n";
                //Console.WriteLine(lines[i]);
            }
            
            File.WriteAllText("./camp_waypoints_with_routeinfolist.csv",  fileOutput);
            
            
            
            /* Position p1 = new Position(31.45122, -25.42001);
            ISpatialNode n1 = se.NearestNode(p1);
            
            Position p2 = new Position(31.59263, -24.98984);
            ISpatialNode n2 = se.NearestNode(p2);



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
            Console.WriteLine(tripLength); */

        }
    }
}