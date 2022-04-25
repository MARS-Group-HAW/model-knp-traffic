using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Options;
using System.Text.Json;
using KrugerNationalPark.Misc;
using Microsoft.VisualBasic.FileIO;


namespace KrugerNationalParkBox
{
    public static class GetRouteTimings
    {
        public static void Timings()
        {
            // Build Spatial graph env.
            ISpatialGraphEnvironment spatialGraphEnvironment = new SpatialGraphEnvironment(new SpatialGraphOptions
            {
                GraphImports = new List<Input>
                {
                    new()
                    {
                        File = "resources/roads_all_2019_inferred.geojson",
                        InputConfiguration = new InputConfiguration
                        {
                            IsBiDirectedImport = true,
                            Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving}
                        }
                    }
                }
            });
            
            // load POIs
            var pois = new List<Poi>();
            
            
            var path = @"./resources/pois.csv";
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    string[] fields = csvParser.ReadFields();
                    var poi = new Poi
                    {
                        Name = fields[0],
                        Type = fields[1],
                        Access = fields[2],
                        Position = new Position(Convert.ToDouble(fields[3], CultureInfo.InvariantCulture),
                            Convert.ToDouble(fields[4], CultureInfo.InvariantCulture))
                    };
                    pois.Add(poi);
                }
            }
            
            var routeInfoList = new List<OriginPoco>();
            
            for (var i = 0; i < pois.Count; i++)
            {
                var originPoi = pois[i];
                var originNode = spatialGraphEnvironment.NearestNode(originPoi.Position);
                var timings = new List<DestinationPoco>();

                Console.WriteLine("Origin: " + originPoi.Name + " (" + originPoi.Type + ")");

                if (originNode == null)
                {
                    Console.WriteLine("No NearestNode (origin)");
                    continue;
                }

                for (var j = 0; j < pois.Count; j++)
                {
                    var destinationPoi = pois[j];
                    var destinationNode = spatialGraphEnvironment.NearestNode(destinationPoi.Position);

                    if (destinationNode == null)
                    {
                        Console.WriteLine("No NearestNode (destination)");
                        continue;
                    }
                    
                    if (destinationPoi.Position.Equals(originPoi.Position)) continue;
                    
                    var route = spatialGraphEnvironment.FindRoute(originNode, destinationNode);

                    if (route == null)
                    {
                        Console.WriteLine("No route for: " + originPoi.Name + " -> " + destinationPoi.Name);
                        continue;
                    }
                    
                    var edgeStops = route.Stops;
                    
                    var tripTime = 0.0; // in seconds
                    var tripLength = route.RouteLength;

                    for (var k = 0; k < route.Count; k++)
                    {
                        var edge = edgeStops[k].Edge;

                        // edges might not have a set MaxSpeed, we use a defualt of 40 km/h.
                        // San Parks (https://www.sanparks.org/parks/kruger/tourism/code.php):
                        // > Stick to the speed limit! All general rules of the road apply within the Kruger National
                        // > Park. The speed limit is 50 km/h on tar roads and 40 km/h on gravel roads. Please note
                        // > that not all roads are accessible to caravans.
                        var maxSpeed = 11.11111111111111; // 40 km/h
                        if (edge.MaxSpeed > 0)
                        {
                            maxSpeed = edge.MaxSpeed;
                        }
                        
                        tripTime += edge.Length / maxSpeed;
                    }

                    var routeInfoPoco = new DestinationPoco
                    {
                        Poi = destinationPoi,
                        Duration = tripTime,
                        Length = tripLength
                    };
                    
                    timings.Add(routeInfoPoco);
                }

                var originPoco = new OriginPoco
                {
                    Poi = originPoi,
                    Destinations = timings
                };

                routeInfoList.Add(originPoco);
            }

            File.WriteAllText("./resources/pois.json", JsonSerializer.Serialize(routeInfoList));
        }
    }
}