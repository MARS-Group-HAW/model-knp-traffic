using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using Mars.Common.Core.Collections;
using Mars.Common.Core.Logging;
using Mars.Components.Environments;
using Mars.Components.Starter;
using Mars.Interfaces.Model;
using SOHDomain.Graph;

namespace KrugerNationalParkBox;

public static class Program
{
    public static void Main(string[] args)
    {
        var currentWorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (currentWorkingDirectory != null)
        {
            Directory.SetCurrentDirectory(currentWorkingDirectory);
        }

        // only one binary output per project is possible -> use argument to trigger
        // execution of POI timings script. Use `$ dotnet run -poi` to run.
        if (args.Any(s => s.Equals("-poi")))
        {
            GetRouteTimings.Timings();
            return;
        }

        // only one binary output per project is possible -> use argument to trigger
        // prepare san park graph as geojson layer, this takes quite some time 
        // since we infer graph intersections
        if (args.Any(s => s.Equals("-infergraph")))
        {
            var input = new Input
            {
                File = "resources/roads_all_2019.geojson",
                //File = "resources/moreIntersectionsAfterImport2.geojson",
                InputConfiguration = new InputConfiguration
                    { InferNodesOnEdgeIntersections = true, IsBiDirectedImport = true }
            };
            var graph = new SpatialGraphEnvironment(input);

            Console.WriteLine("Inferring is finished, saving now...");
            var json = graph.ToGeoJson(); // <- richtig? 
            File.WriteAllText("resources/roads_all_2019_inferred.geojson", json);
            //System.IO.File.WriteAllText("resources/moreIntersectionsAfterImport2_inferred.geojson", json);

            return;
        }

        // Build model...
        var watch = Stopwatch.StartNew();
        var description = new ModelDescription();

        // Turning logger on or off
        LoggerFactory.SetLogLevel(LogLevel.Info);

        // description.AddLayer<SpatialGraphMediatorLayer>();

        description.AddLayer<KnpRoadNetwork>(new[] { typeof(ISpatialGraphLayer) });

        description.AddLayer<TrafficGrid>();
        description.AddLayer<TrafficJamGrid>();
        description.AddLayer<SightingsGrid>();

        description.AddLayer<PointsOfInterest>(); // KNP POIs, including gates, rest camps, etc. (see PoiTypes.cs)

        description.AddLayer<VisitorScheduler>();
        description.AddLayer<CommuterScheduler>();
        description.AddLayer<OsvTourGuideScheduler>();
        description.AddLayer<EventProducerScheduler>();

        // Second register the agent types with their respective layer type
        description.AddAgent<Visitor, KnpRoadNetwork>();
        description.AddAgent<Commuter, KnpRoadNetwork>();
        description.AddAgent<OsvTourGuide, KnpRoadNetwork>();

        description.AddAgent<EventProducer, KnpRoadNetwork>();

        description.AddEntity<KnpCar>();

        
        // Starting up
        if (args.Any(s => s.Equals("-l")))
        {
            LoggerFactory.SetLogLevel(LogLevel.Info);
            LoggerFactory.ActivateConsoleLogging();
        }
        
        // load sim config, fall back to config.json if none is given
        /*var file = "config.json";
        if (args.Any(s => s.Equals("-sm")))
        {
            var index = args.IndexOf(s => s == "-sm");
            file = File.ReadAllText(args[index + 1]);
        }
        */
        // $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)}/config.json"
        var file = File.ReadAllText("config.json");
        
        var simConfig = SimulationConfig.Deserialize(file);
        var starter = SimulationStarter.Start(description, simConfig);
        var result = starter.Run();
        
        watch.Stop();
        Console.WriteLine($"Simulation finished and last {watch.Elapsed}");
    }
}