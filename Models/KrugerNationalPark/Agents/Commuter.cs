using System;
using System.Collections.Generic;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Core.Data.Wrapper.Memory;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Graph;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents;

/// <summary>
///     Commuter starts at a KNP gate and travels to a KNP rest camp for a specified amount of time (work duration).
///     Configurable in CSV file used for CommuterSchedulingLayer.
///     Spawned by CommuterSchedulingLayer.
/// </summary>
public class Commuter : IAgent<VisitorTravelerLayer>, ICarSteeringCapable
{
    #region Initialization

    public void Init(VisitorTravelerLayer layer)
    {
        Console.WriteLine(SourceName);
        var testPoi = PoiLayer.GetPositionFromName(SourceName);
        // Console.WriteLine(testPoi.Name);
            
        State = CommuterState.GoingToWork;
        _sgmLayer = layer.SpatialGraphMediatorLayer;

        TripsCollection = new TripsCollection(layer.Context);

        var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
        car.Environment = _sgmLayer.Environment;
        Car = car;

        Position = SourceGeometry.RandomPositionFromGeometry();

        car.TryEnterDriver(this, out var handle);

        Position targetPos;
            
        Console.WriteLine(TargetGeometry);
            
        if (TargetGeometry is not null)
        {
            // From given MULTIPOINT Geometry, get a random POINT
            // todo: RandomPositionFromGeometry() doesnt seem random for MULTIPOINTs?!
            var target = TargetGeometry.Coordinates;
            var numberOfPotentialDestinations = target.Length;
            var randomIndex = new Random().Next(numberOfPotentialDestinations);
            var targetCor = target[randomIndex];
            targetPos = Position.CreatePosition(targetCor.X, targetCor.Y);
        }
        else
        {
            // Destination is undefined. Therefore, randomly choose a rest camp that is within 1.5 hours from source
            var sourcePoi = PoiLayer.Nearest(Position);
            var availableDestinations = sourcePoi.GetDestinationPois(1.5 * 3600, new List<string> { "Rest camp" });
            var numberOfPotentialDestinations = availableDestinations.Count;
            var destinationIndex = new Random().Next(numberOfPotentialDestinations);
            targetPos = availableDestinations[destinationIndex].Poi.Position;
        }

        OriginNode = _sgmLayer.Environment.NearestNode(Position, SpatialModalityType.CarDriving);
        _sgmLayer.Environment.Insert(car, OriginNode);

        // for the StreetEnvironment we need a SpatialNode, not a Position.
        // -> get nearest Node to chosen target position
        WorkplaceNode = _sgmLayer.Environment.NearestNode(targetPos, SpatialModalityType.CarDriving);

        handle.Route = _sgmLayer.Environment.FindRoute(OriginNode, WorkplaceNode);
        VehicleHandle = handle;
    }

    #endregion

    #region Tick

    public void Tick()
    {
        if (VehicleHandle.Route.GoalReached)
        {
            if (State == CommuterState.GoingToWork)
            {
                // we reached our working destination
                State = CommuterState.Working;
                _arrivalTime = _sgmLayer.Context.CurrentTimePoint.GetValueOrDefault();
                _departureTime = _arrivalTime.AddMinutes(WorkDuration);
            }
            else if (State == CommuterState.Working && _departureTime
                         .Subtract(_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
            {
                // finished working -> go back to origin gate
                State = CommuterState.GoingHome;
                VehicleHandle.Route = _sgmLayer.Environment.FindRoute(WorkplaceNode, OriginNode);
            }
            else if (State == CommuterState.GoingHome)
            {
                // 1. remove car from graph
                //_streetLayer.StreetEnvironment.Remove(Car);

                // 2. unregister agent, it will no longer receive any tick() calls
                // this agent reached it's goal and is no longer relevant in the sim context so we can remove it.
                //UnregisterHandle.Invoke(_streetLayer, this);
            }
        }
        else
        {
            // agent calls its movement handle (associated with its car) to perform a movement
            VehicleHandle.Move();

            // TODO: can this be moved into the else block?
            CarVelocity = Car.Velocity;
            TripsCollection.Add(Position);
        }
    }

    #endregion

    #region Methods

    public void Notify(PassengerMessage passengerMessage)
    {
    }

    #endregion

    #region Properties

    public Guid ID { get; set; }
    public int StableId { get; }

    private CommuterState State { get; set; }

    /// <summary>
    ///     Needed fo "removing" the agent and preventing further tick() call to it.
    /// </summary>
    [PropertyDescription]
    public UnregisterAgent UnregisterHandle { get; set; }

    private ISpatialNode OriginNode;
    private ISpatialNode WorkplaceNode;

    [PropertyDescription(Name = "sourceName")]
    public string SourceName { get; set; }
        
    /// <summary>
    ///     Format: WKT Point (`POINT (31.482268 -24.979422)`).
    /// </summary>
    [PropertyDescription(Name = "source")]
    public Geometry SourceGeometry { get; set; }

#nullable enable
    /// <summary>
    ///     WKT Multipoint with variable amount of target points. On is chosen by random.
    ///     Example: "MULTIPOINT (31.53493 -25.460457, 31.591958 -24.994678)"
    /// </summary>
    [PropertyDescription(Name = "destination")]
    public Geometry? TargetGeometry { get; set; }
#nullable disable

    /// <summary>
    ///     Duration of work at the camp, in minutes.
    /// </summary>
    [PropertyDescription(Name = "workDuration")]
    public double WorkDuration { get; set; }

    /// <summary>
    ///     The agent's arrival time at work, and departure time from work (each in hours)
    /// </summary>
    private DateTime _arrivalTime;

    private DateTime _departureTime;

    /// <summary>
    ///     The agent's reference to the KNP traffic network
    /// </summary>
    private SpatialGraphMediatorLayer _sgmLayer;

    /// <summary>
    ///     The agent's reference to the KNP POI layer, which holds gates, rest camps, and other POIs in the KNP
    /// </summary>
    public POILayer PoiLayer { get; set; }

    /// <summary>
    ///     Position of our car/agent on the map.
    /// </summary>
    public Position Position
    {
        get => Car.Position;
        set => Car.Position = value;
    }

    public CarSteeringHandle VehicleHandle { get; set; }

    public bool OvertakingActivated { get; set; }
    public bool BrakingActivated { get; set; }
    public Car Car { get; set; }
    public bool CurrentlyCarDriving => true;

    public double CarVelocity { get; set; }

    public TripsCollection TripsCollection { get; set; }

    #endregion
}