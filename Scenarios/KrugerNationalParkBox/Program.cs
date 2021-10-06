using System;
using System.Collections.Generic;
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
using Mars.Interfaces.Environments;
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

            
            description.AddLayer<SpatialGraphMediatorLayer>(new[] {typeof(ISpatialGraphLayer)});
            
            
            description.AddLayer<VisitorTravelerLayer>();
            
            
            //description.AddLayer<KnpStreetLayer>(); // Straßennetzt im KNP
            
            description.AddLayer<POILayer>(); // Camps and Gates

            description.AddLayer<TouristSchedulingLayer>();
            description.AddLayer<CommuterSchedulingLayer>();
            description.AddLayer<ProducerSchedulingLayer>();

            // Second register the agent types with their respective layer type
            description.AddAgent<Tourist, VisitorTravelerLayer>();
            description.AddAgent<Commuter, VisitorTravelerLayer>();
            
            description.AddAgent<Elephant, ElephantLayer>();
            description.AddAgent<EventProducer, VisitorTravelerLayer>();

            description.AddEntity<KnpCar>();

            // Starting up
            SimulationWorkflowState result = null;

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
                    simConfig = SimConfig();
                }


                var starter = SimulationStarter.Start(description, simConfig);

                result = starter.Run();
            }


            watch.Stop();
            Console.WriteLine($"Simulation finished and last {watch.Elapsed}");
        }

        public static SimulationConfig SimConfig()
        {
            var start = new DateTime(2019, 1, 1, 6, 0, 00);
            var end = start + TimeSpan.FromHours(4);
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
                        Delimiter = ";",
                        NumberFormat = "en-EN"
                    },
                    ShowConsoleProgress = false,
                    EnableSimpleVisualization = false
                },
                LayerMappings = new List<LayerMapping>
                {
                    new LayerMapping
                    {
                        Name = nameof(RasterTempLayer),
                        File = "resources/RCP8.5_2010_2050_temp.zip"
                    },
                    new LayerMapping
                    {
                        Name = nameof(RasterFenceLayer),
                        File = "resources/gis_raster_border.zip"
                    },
                    new LayerMapping
                    {
                        Name = nameof(RasterShadeLayer),
                        File = "resources/gis_raster_shade.zip"
                    },
                    new LayerMapping
                    {
                        Name = nameof(RasterVegetationLayer),
                        File = "resources/gis_raster_biomass_ts.zip"
                    },
                    new LayerMapping
                    {
                        Name = nameof(VectorWaterLayer),
                        File = "resources/merged_waters_fixed_with_fence_buffer.geojson"
                    },
                    
                    new LayerMapping
                    {
                        Name = nameof(VisitorTravelerLayer),
                    },
                    new LayerMapping
                    {
                        Name = nameof(SpatialGraphMediatorLayer),
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                File = "resources/knp_graph.geojson",
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
                        Name = nameof(POILayer),
                        File = "resources/pois.geojson"
                    },
                    new LayerMapping
                    {
                        Name = nameof(TouristSchedulingLayer),
                        File = "resources/TouristScheduler_debug_1.csv"
                    },
                    new LayerMapping
                    {
                        Name = nameof(CommuterSchedulingLayer),
                        File = "resources/_emptyScheduler.csv"
                    },
                    new LayerMapping
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
                        Name = nameof(Tourist),
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
}