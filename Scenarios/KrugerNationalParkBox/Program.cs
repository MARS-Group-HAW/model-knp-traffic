using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using Mars.Common.Core.Collections;
using Mars.Common.Core.Logging;
using Mars.Common.Core.Logging.Enums;
using Mars.Components.Starter;
using Mars.Core.Simulation.Entities;
using Mars.Interfaces.Model;
using SOHDomain.Graph;

namespace KrugerNationalParkBox
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            
            // only one binary output per project is possible -> use argument to trigger
            // execution of POI timings script. Use `$ dotnet run -poi` to run.
            if (args.Any(s => s.Equals("-poi")))
            {
                GetRouteTimings.Timings();
                return;
            }


            // Build model...
            var watch = Stopwatch.StartNew();
            var description = new ModelDescription();

            // Turning logger on or off
            LoggerFactory.SetLogLevel(LogLevel.Info);

            // First register each layer at the runtime system
            description.AddLayer<RasterTempLayer>();
            description.AddLayer<RasterFenceLayer>();
            description.AddLayer<RasterShadeLayer>();
            description.AddLayer<RasterVegetationLayer>();
            description.AddLayer<VectorWaterLayer>();
            description.AddLayer<ElephantLayer>();
            
            //             description.AddLayer<KnpStreetLayer>(new[] {typeof( ISpatialGraphLayer)} ); // Straßennetzt im KNP

            description.AddLayer<KnpStreetLayer>( ); // Straßennetzt im KNP
            description.AddLayer<POILayer>(); // Camps and Gates
            
            description.AddLayer<TouristSchedulingLayer>();
            description.AddLayer<CommuterSchedulingLayer>();

            // Second register the agent types with their respective layer type
            var a = description.AddAgent<Tourist, KnpStreetLayer>();
            var aa = description.AddAgent<Commuter, KnpStreetLayer>();
            var b = description.AddAgent<Elephant, ElephantLayer>();
            
            var c = description.AddEntity<KnpCar>();

            var a_out = a.OutputProperties;
            var aa_out = aa.OutputProperties;
            var b_out = b.OutputProperties;
            var c_out = c.OutputProperties;
            
            // Starting up
            SimulationWorkflowState result = null;
            if (args != null)
            {
                if (args.Any(s => s.Equals("-l")))
                {
                    LoggerFactory.SetLogLevel(LogLevel.Info);
                    LoggerFactory.ActivateConsoleLogging();
                }

                string file;
                if (args.Any(s => s.Equals("-sm")))
                {
                    var index = args.IndexOf(s => s == "-sm");
                    file = File.ReadAllText(args[index + 1]);
                }
                else
                {
                    file = File.ReadAllText("config.json");
                }

                var simConfig = SimulationConfig.Deserialize(file);
                var starter = SimulationStarter.Start(description, simConfig);
                result = starter.Run();
            }
            
            watch.Stop();
            Console.WriteLine($"Simulation finished and last {watch.Elapsed}");
        }
    }
}