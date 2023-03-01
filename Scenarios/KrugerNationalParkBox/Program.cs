using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Layers;
using Mars.Common.Core.Collections;
using Mars.Common.Core.Logging;
using Mars.Components.Environments;
using Mars.Components.Starter;
using Mars.Core.Simulation.Entities;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using SOHDomain.Graph;

namespace KrugerNationalParkBox;

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

        // First register each layer at the runtime system
        /*
        description.AddLayer<RasterTempLayer>();
        description.AddLayer<RasterFenceLayer>();
        description.AddLayer<RasterShadeLayer>();
        description.AddLayer<RasterVegetationLayer>();
        description.AddLayer<VectorWaterLayer>();
        description.AddLayer<ElephantLayer>();
        description.AddLayer<KnpStreetLayer>(new[] {typeof( ISpatialGraphLayer)} ); // Straßennetz im KNP
        */

        description.AddLayer<SpatialGraphMediatorLayer>(new[] { typeof(ISpatialGraphLayer) });

        description.AddLayer<VisitorTravelerLayer>();

        description.AddLayer<TrafficLayer>();

        //description.AddLayer<KnpStreetLayer>(); // Straßennetzt im KNP

        description.AddLayer<PoiLayer>(); // Camps and Gates

        description.AddLayer<VisitorSchedulingLayer>();
        description.AddLayer<CommuterSchedulingLayer>();
        description.AddLayer<ProducerSchedulingLayer>();

        // Second register the agent types with their respective layer type
        description.AddAgent<Visitor, VisitorTravelerLayer>();
        description.AddAgent<Commuter, VisitorTravelerLayer>();

        //description.AddAgent<Elephant, ElephantLayer>();
        description.AddAgent<EventProducer, VisitorTravelerLayer>();

        description.AddEntity<KnpCar>();

        // Starting up
        SimulationWorkflowState result;

        if (args != null)
        {
            if (args.Any(s => s.Equals("-l")))
            {
                LoggerFactory.SetLogLevel(LogLevel.Info);
                LoggerFactory.ActivateConsoleLogging();
            }

            SimulationConfig simConfig;
            string file;

            if (args.Any(s => s.Equals("-sm")))
            {
                var index = args.IndexOf(s => s == "-sm");
                file = File.ReadAllText(args[index + 1]);
                simConfig = SimulationConfig.Deserialize(file);

                Console.WriteLine(simConfig.Serialize());
            }
            else
            {
                simConfig = GenerateSimConfig();
            }

            var starter = SimulationStarter.Start(description, simConfig);

            result = starter.Run();
        }

        watch.Stop();
        Console.WriteLine($"Simulation finished and last {watch.Elapsed}");
    }

    private static SimulationConfig GenerateSimConfig()
    {
        var start = new DateTime(2019, 1, 1, 6, 0, 00);
        var end = start + TimeSpan.FromHours(12);
        return new SimulationConfig
        {
            Globals =
            {
                StartPoint = start,
                EndPoint = end,
                DeltaTUnit = TimeSpanUnit.Seconds,
                OutputTarget = OutputTargetType.Csv,
                CsvOptions =
                {
                    Delimiter = ",",
                    NumberFormat = "G"
                },
                ShowConsoleProgress = false,
            },
            LayerMappings = new List<LayerMapping>
            {
                new()
                {
                    Name = nameof(TrafficLayer),
                    File = "resources/knp_raster_1111m.asc"
                },
                new()
                {
                    Name = nameof(RasterTempLayer),
                    File = "resources/RCP8.5_2010_2050_temp.zip"
                },
                new()
                {
                    Name = nameof(RasterFenceLayer),
                    File = "resources/gis_raster_border.zip"
                },
                new()
                {
                    Name = nameof(RasterShadeLayer),
                    File = "resources/gis_raster_shade.zip"
                },
                new()
                {
                    Name = nameof(RasterVegetationLayer),
                    File = "resources/gis_raster_biomass_ts.zip"
                },
                new()
                {
                    Name = nameof(VectorWaterLayer),
                    File = "resources/merged_waters_fixed_with_fence_buffer.geojson"
                },
                new()
                {
                    Name = nameof(VisitorTravelerLayer),
                },
                new()
                {
                    Name = nameof(SpatialGraphMediatorLayer),
                    Inputs = new List<Input>
                    {
                        new()
                        {
                            //File = "resources/knp_graph.geojson",
                            File = "resources/roads_all_2019_inferred.geojson",
                            //File = "resources/roads_all_2019_public.geojson",
                            InputConfiguration = new InputConfiguration
                            {
                                Modalities = new HashSet<SpatialModalityType> { SpatialModalityType.CarDriving },
                                IsBiDirectedImport = true,
                                //NodeToleranceInMeter = 20,
                                //NodeIntegrationKind = NodeIntegrationKind.LinkNode,
                                //GeometryAsNodesEnabled = false // alle punkte als node -> langsam
                            }
                        }
                    }
                },
                new()
                {
                    Name = nameof(PoiLayer),
                    File = "resources/pois_inferred.geojson"
                },
                new()
                {
                    Name = nameof(VisitorSchedulingLayer),
                    File = "resources/VisitorScheduler_debug_1.csv"
                },
                new()
                {
                    Name = nameof(CommuterSchedulingLayer),
                    File = "resources/CommScheduler_noDest.csv"
                },
                new()
                {
                    Name = nameof(ProducerSchedulingLayer),
                    File = "resources/ProducerScheduler.csv"
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
                    Name = nameof(Visitor),
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
                    },
                    IndividualMapping = new List<IndividualMapping>
                    {
                        new ()
                        {
                            ParameterName = "WriteRouteAsGeoJSON",
                            Value = false,
                        }
                    }
                },
                new AgentMapping
                {
                    Name = nameof(Commuter),
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
                },
                new AgentMapping
                {
                    Name = nameof(EventProducer),
                    Outputs = new List<Output>
                    {
                        new()
                        {
                            OutputTarget = OutputTargetType.Csv
                        }
                    }
                }
            }
        };
    }
}