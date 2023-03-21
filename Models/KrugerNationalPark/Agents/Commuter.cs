using System;
using System.Collections.Generic;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
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
        State = CommuterState.GoingToWork;
        _sgmLayer = layer.SpatialGraphMediatorLayer;

        TripsCollection = new TripsCollection(_sgmLayer.Context);

        var car = _sgmLayer.EntityManager.Create<KnpCar>("type", "Golf");
        car.Environment = _sgmLayer.Environment;
        Car = car;

        var sourcePos = GenerateSourcePosition();
        Position = sourcePos.X != 0d && sourcePos.Y != 0d ? sourcePos : PoiLayer.GetRandomPoiPositionOfType(PoiType.KnpGate);

        car.TryEnterDriver(this, out var handle);

        _originNode = _sgmLayer.Environment.NearestNode(Position, SpatialModalityType.CarDriving);
        _sgmLayer.Environment.Insert(car, _originNode);

        // The StreetEnvironment requires a SpatialNode, not a Position. Get nearest Node to chosen target position.
        var targetPos = GenerateTargetPosition();
        _workplaceNode = _sgmLayer.Environment.NearestNode(targetPos, SpatialModalityType.CarDriving);

        handle.Route = _sgmLayer.Environment.FindRoute(_originNode, _workplaceNode);
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
                VehicleHandle.Route = _sgmLayer.Environment.FindRoute(_workplaceNode, _originNode);
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
            CarVelocity = Car.Velocity;
            TripsCollection.Add(Position);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a random position from SourceGeometry or, if not provided, obtains the source position based on
    ///     the provided <value>SourceName</value>
    /// </summary>
    /// <returns>Position of source of trip</returns>
    private Position GenerateSourcePosition()
    {
        return SourceGeometry is not null
            ? GetRandomPositionFromGeometry(SourceGeometry)
            : PoiLayer.GetPositionFromNameAndType(SourceName, SourceType);
    }

    /// <summary>
    ///     Generates a target position from TargetName or TargetGeometry. Alternatively, if neither TargetName nor
    ///     TargetGeometry is provided, obtains a random destination position within a fixed driving distance
    /// </summary>
    /// <returns>Position of destination of trip</returns>
    private Position GenerateTargetPosition()
    {
        Position targetPos;

        // If TargetName is provided, use it to determine target position
        if (TargetName is not null)
        {
            targetPos = PoiLayer.GetPositionFromNameAndType(TargetName, TargetType);
            // TODO: Add a distance check. If targetName in CSV is too far away from sourceName, choose different target
        }
        // If TargetGeometry is provided, use it to choose a target position
        else if (TargetGeometry is not null)
        {
            targetPos = GetRandomPositionFromGeometry(TargetGeometry);
        }
        else
        {
            // Destination is undefined. Therefore, randomly choose a rest camp that is within 1.5 hours from source
            // TODO: replace magic number (timeLimit)
            // TODO: add option to provide TargetGeometry but no SourceGeometry?
            // TODO: refactor KnpPoi.GetDestinationPois() into PoiLayer so that layer is single point of entry for information related to KnpPois
            var nearestPoi = PoiLayer.GetNearestKnpPoi(Position);
            var availableDestinations =
                nearestPoi.GetDestinationPois(1.5 * 3600, new List<string> { PoiType.RestCamp });
            var numberOfPotentialDestinations = availableDestinations.Count;
            var destinationIndex = new Random().Next(numberOfPotentialDestinations);
            targetPos = availableDestinations[destinationIndex].Poi.Position;
        }

        return targetPos;
    }

    /// <summary>
    ///     Returns a random Position from the given geometry
    /// </summary>
    /// <param name="geometry">The given geometry</param>
    /// <returns>A random position from the given geometry</returns>
    private Position GetRandomPositionFromGeometry(Geometry geometry)
    {
        var geometryCoords = geometry.Coordinates;
        var numberOfPotentialPositions = geometryCoords.Length;
        var randomIndex = new Random().Next(numberOfPotentialPositions);
        var chosenCoords = geometryCoords[randomIndex];
        return Position.CreatePosition(chosenCoords.X, chosenCoords.Y);
    }

    public void Notify(PassengerMessage passengerMessage)
    {
    }

    #endregion

    #region Properties and Fields

    /// <summary>
    ///     Current state of the Commuter
    /// </summary>
    private CommuterState State { get; set; }

    /// <summary>
    ///     The node of the SGE that represents the Commuter's origin (typically a KNP gate)
    /// </summary>
    private ISpatialNode _originNode;

    /// <summary>
    ///     The node of the SGE that represents the Commuter's workplace (typically a KNP rest camp)
    /// </summary>
    private ISpatialNode _workplaceNode;

    /// <summary>
    ///     The name of the POI at which the Commuter's trip begins
    /// </summary>
    [PropertyDescription(Name = "sourceName")]
    public string SourceName { get; set; }

    /// <summary>
    ///     The type of the POI at which the Commuter's trip begins
    /// </summary>
    [PropertyDescription(Name = "sourceType")]
    public string SourceType { get; set; }

    /// <summary>
    ///     The geometry that contains positions of POIs at which the Commuter's trip can begin
    ///     Format: WKT Geometry
    ///     Example: POINT (31.482268 -24.979422)
    /// </summary>
    [PropertyDescription(Name = "source")]
    public Geometry SourceGeometry { get; set; }

#nullable enable
    /// <summary>
    ///     The name of the POI at which the Commuter's trip ends
    /// </summary>
    [PropertyDescription(Name = "targetName")]
    public string? TargetName { get; set; }
    
    /// <summary>
    ///     The type of the POI at which the Commuter's trip ends
    /// </summary>
    [PropertyDescription(Name = "targetType")]
    public string? TargetType { get; set; }
    
    /// <summary>
    ///     The geometry that contains positions of POIs at which the Commuter's trip can end
    ///     Format: WKT geometry
    ///     Example: MULTIPOINT (31.53493 -25.460457, 31.591958 -24.994678)
    /// </summary>
    [PropertyDescription(Name = "destination")]
    public Geometry? TargetGeometry { get; set; }
#nullable disable

    /// <summary>
    ///     Arrival time at work, in hours
    /// </summary>
    private DateTime _arrivalTime;

    /// <summary>
    ///     Work duration at workplace, in minutes
    /// </summary>
    [PropertyDescription(Name = "workDuration")]
    public double WorkDuration { get; set; }

    /// <summary>
    ///     Departure time from work, in hours
    /// </summary>
    private DateTime _departureTime;

    /// <summary>
    ///     Reference to the KNP traffic network
    /// </summary>
    private SpatialGraphMediatorLayer _sgmLayer;

    /// <summary>
    ///     Reference to the KNP POI layer, which holds gates, rest camps, and other POIs in the KNP
    /// </summary>
    public PoiLayer PoiLayer { get; set; }

    /// <summary>
    ///     Position of car/agent on the map
    /// </summary>
    public Position Position
    {
        get => Car.Position;
        set => Car.Position = value;
    }

    public Guid ID { get; set; }
    public int StableId { get; }

    [PropertyDescription] public UnregisterAgent UnregisterHandle { get; set; }
    public CarSteeringHandle VehicleHandle { get; set; }
    public bool OvertakingActivated { get; set; }
    public bool BrakingActivated { get; set; }
    public Car Car { get; set; }
    public bool CurrentlyCarDriving => true;
    public double CarVelocity { get; set; }
    public TripsCollection TripsCollection { get; set; }

    #endregion
}