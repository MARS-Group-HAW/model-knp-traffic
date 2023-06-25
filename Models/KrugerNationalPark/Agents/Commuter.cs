using System;
using System.Collections.Generic;
using System.Linq;
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
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents;

/// <summary>
/// The <see cref="Commuter"/> agent starts at a KNP Gate and travels to a Rest camp to work for some amount of time.
/// </summary>
/// <remarks>Configurable via and spawned by <see cref="CommuterScheduler"/> (see scheduler CSV file).</remarks>
public class Commuter : IAgent<KnpRoadNetwork>, ICarSteeringCapable
{
    #region Initialization

    /// <summary>Initialization routine of the <see cref="Commuter"/>.</summary>
    /// <remarks>
    /// Includes state initialization, vehicle acquisition, positioning in the environment, and route finding.
    /// </remarks>
    /// <param name="layer">Reference to the <see cref="KnpRoadNetwork"/> on which the <see cref="Commuter"/> lives.
    /// </param>
    public void Init(KnpRoadNetwork layer)
    {
        // 1. State initialization
        State = CommuterState.GoingToWork;
        _knpRoadNetwork = layer;

        TripsCollection = new TripsCollection(_knpRoadNetwork.Context);

        // 2. Vehicle acquisition
        Car = _knpRoadNetwork.EntityManager.Create<KnpCar>("type", "Golf");
        Car.Environment = _knpRoadNetwork.Environment;

        // 3. Positioning in the environment
        var sourcePos = GenerateSourcePosition();
        Position = sourcePos.X != 0d && sourcePos.Y != 0d
            ? sourcePos
            : PointsOfInterest.GetRandomPoiPositionOfType(PoiType.KnpGate);

        Car.TryEnterDriver(this, out var handle);

        // 4. Route finding
        _originNode = _knpRoadNetwork.Environment.NearestNode(Position, SpatialModalityType.CarDriving);
        _knpRoadNetwork.Environment.Insert(Car, _originNode);

        var targetPos = GenerateTargetPosition();
        _workplaceNode = _knpRoadNetwork.Environment.NearestNode(targetPos, SpatialModalityType.CarDriving);

        handle.Route = _knpRoadNetwork.Environment.FindRoute(_originNode, _workplaceNode);
        VehicleHandle = handle;
    }

    #endregion

    #region Tick

    /// <summary>Behaviour routine of the <see cref="Commuter"/>.</summary>
    /// <remarks>
    /// Includes travel to place of work, remaining at place of work for some time, and returning to KNP entry point.
    /// </remarks>
    public void Tick()
    {
        if (VehicleHandle.Route.GoalReached)
        {
            if (State == CommuterState.GoingToWork)
            {
                // we reached our working destination
                State = CommuterState.Working;
                _arrivalTime = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();
                _departureTime = _arrivalTime.AddMinutes(WorkDuration);
            }
            else if (State == CommuterState.Working && _departureTime
                         .Subtract(_knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
            {
                // finished working -> go back to origin gate
                State = CommuterState.GoingHome;
                VehicleHandle.Route = _knpRoadNetwork.Environment.FindRoute(_workplaceNode, _originNode);
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
    /// Gets a random position from <see cref="SourceGeometry"/> or, if not provided, obtains the source position based
    /// on the provided <see cref="SourceName"/>.
    /// </summary>
    /// <returns>Position of source of trip</returns>
    private Position GenerateSourcePosition()
    {
        return SourceGeometry is not null
            ? GetRandomPositionFromGeometry(SourceGeometry)
            : PointsOfInterest.GetPoiPositionOfNameAndType(SourceName, SourceType);
    }

    /// <summary>
    /// Generates a target position using <see cref="TargetName"/> and <see cref="TargetType"/> or
    /// <see cref="TargetGeometry"/>.
    /// </summary>
    /// <remarks>Alternatively, if neither <see cref="TargetName"/> nor <see cref="TargetGeometry"/> is
    /// provided, obtains a random destination position within a fixed driving distance.</remarks>
    /// <returns>Position of destination of trip</returns>
    private Position GenerateTargetPosition()
    {
        Position targetPos;

        // If TargetName and TargetType is provided, use it to determine a target position.
        if (TargetName is not null && TargetType is not null)
        {
            targetPos = PointsOfInterest.GetPoiPositionOfNameAndType(TargetName, TargetType);
            // TODO: Add a distance check. If targetName in CSV is too far away from sourceName, choose different target
        }
        // If TargetGeometry is provided, use it to determine a target position.
        else if (TargetGeometry is not null)
        {
            var poisOfTypesInGeometry =
                PointsOfInterest.GetKnpPoisOfTypeInGeometry(new List<string> { PoiType.RestCamp }, TargetGeometry);
            targetPos = GetRandomKnpPoiFromEnumerable(poisOfTypesInGeometry).Position;
        }
        else
        {
            // Destination is undefined. Therefore, randomly choose a rest camp that is within 1.5 hours from source.
            // TODO: replace magic number (timeLimit)
            // TODO: add option to provide TargetGeometry but no SourceGeometry?
            // TODO: refactor KnpPoi.GetDestinationPois() into PoiLayer so that layer is single point of entry for information related to KnpPois
            var nearestPoi = PointsOfInterest.GetNearestKnpPoi(Position);
            var availableDestinations =
                nearestPoi.GetDestinationPois(1.5 * 3600, new List<string> { PoiType.RestCamp });
            var numberOfPotentialDestinations = availableDestinations.Count;
            var destinationIndex = new Random().Next(numberOfPotentialDestinations);
            targetPos = availableDestinations[destinationIndex].Poi.Position;
        }

        return targetPos;
    }

    /// <summary>Gets a ransom POI from the given list of POIs.</summary>
    /// <param name="knpPois">The given list of POIs</param>
    /// <returns>A randomly picked POI</returns>
    private static KnpPoi GetRandomKnpPoiFromEnumerable(IEnumerable<KnpPoi> knpPois)
    {
        var knpPoisList = knpPois.ToList();
        var poiCount = knpPoisList.Count;
        var poiIndex = new Random().Next(poiCount);
        return knpPoisList[poiIndex];
    }

    /// <summary>Gets a random Position from the given geometry.</summary>
    /// <param name="geometry">The given geometry</param>
    /// <returns>A random position from the given geometry</returns>
    // TODO move this method to KNPRoadNetwork?
    private static Position GetRandomPositionFromGeometry(Geometry geometry)
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

    /// <summary>Current state of the <see cref="Commuter"/> (<see cref="CommuterState"/>).</summary>
    private CommuterState State { get; set; }

    /// <summary>
    /// Node of the <see cref="KnpRoadNetwork"/> that is the initial position of the <see cref="Commuter"/>.
    /// </summary>
    /// <remarks>This is typically a KNP Gate.</remarks>
    private ISpatialNode _originNode;

    /// <summary>
    /// Node of the <see cref="KnpRoadNetwork"/> that is the work position of the <see cref="Commuter"/>.
    /// </summary>
    /// <remarks>This is typically a Rest camp.</remarks>
    private ISpatialNode _workplaceNode;

    /// <summary>Name of the POI at which the trip of the <see cref="Commuter"/> begins.</summary>
    [PropertyDescription(Name = "sourceName")]
    public string SourceName { get; set; }

    /// <summary>Type of the POI specified in <see cref="SourceName"/>.</summary>
    [PropertyDescription(Name = "sourceType")]
    public string SourceType { get; set; }

#nullable enable
    /// <summary>
    /// Geometry that contains positions of POIs at which the trip of the <see cref="Commuter"/> can begin.
    /// </summary>
    /// <remarks>The format should be a WKT geometry
    /// <see href="https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry">Wikipedia</see> that
    /// encodes geospatial locations that are within the KNP. An example of a point geometry:
    /// POINT (31.482268 -24.979422).
    /// </remarks>
    [PropertyDescription(Name = "sourceGeometry")]
    public Geometry? SourceGeometry { get; set; }

    /// <summary>Name of the POI at which the trip of the <see cref="Commuter"/> ends.</summary>
    [PropertyDescription(Name = "targetName")]
    public string? TargetName { get; set; }

    /// <summary>Type of the POI specified in <see cref="TargetName"/>.</summary>
    [PropertyDescription(Name = "targetType")]
    public string? TargetType { get; set; }

    /// <summary>
    /// Geometry that contains positions of POIs at which the trip of the <see cref="Commuter"/> can end.
    /// </summary>
    /// <remarks>The format should be a WKT geometry
    /// <see href="https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry">Wikipedia</see> that
    /// encodes geospatial locations that are within the KNP. An example of a point geometry:
    /// POINT (31.482268 -24.979422).
    /// </remarks>
    [PropertyDescription(Name = "targetGeometry")]
    public Geometry? TargetGeometry { get; set; }
#nullable disable

    /// <summary>Time point at which the <see cref="Commuter"/> arrives at <see cref="_workplaceNode"/>.</summary>
    private DateTime _arrivalTime;

    /// <summary>
    /// Amount of time, in minutes, that the <see cref="Commuter"/> spends at <see cref="_workplaceNode"/>.
    /// </summary>
    [PropertyDescription(Name = "workDuration")]
    public double WorkDuration { get; set; }

    /// <summary>
    /// Time point at which the <see cref="Commuter"/> departs from <see cref="_workplaceNode"/>.
    /// </summary>
    private DateTime _departureTime;

    /// <summary>Reference to the <see cref="KnpRoadNetwork"/>, which holds the road network of the KNP.</summary>
    private KnpRoadNetwork _knpRoadNetwork;

    /// <summary>Reference to the <see cref="PointsOfInterest"/> layer, which holds the POIs of the KNP.</summary>
    public PointsOfInterest PointsOfInterest { get; set; }

    /// <summary>Position of the <see cref="Commuter"/> on the <see cref="KnpRoadNetwork"/>.</summary>
    /// <remarks>Defined in terms of the position of the <see cref="Car"/> entity of the <see cref="Commuter"/>.
    /// </remarks>
    public Position Position
    {
        get => Car.Position;
        set => Car.Position = value;
    }

    /// <summary>Unique identifier of the <see cref="Commuter"/>.</summary>
    public Guid ID { get; set; }

    /// <summary>Reference to the <see cref="UnregisterHandle"/> of the <see cref="KnpRoadNetwork"/>.</summary>
    /// <remarks>Can be used to unregister agents from the simulation context.</remarks>
    [PropertyDescription]
    public UnregisterAgent UnregisterHandle { get; set; }

    /// <summary>
    /// Reference to the <see cref="CarSteeringHandle"/> to interact with the <see cref="Car"/> of the
    /// <see cref="Commuter"/>.
    /// </summary>
    public CarSteeringHandle VehicleHandle { get; set; }

    /// <summary>Flag that indicates if the <see cref="Commuter"/> is capable of overtaking in traffic.</summary>
    public bool OvertakingActivated { get; set; }

    /// <summary>
    /// Flag that indicates if the <see cref="Commuter"/> is currently braking its <see cref="Car"/> entity.
    /// </summary>
    public bool BrakingActivated { get; set; }

    /// <summary>Reference to the <see cref="Car"/> entity of the <see cref="Commuter"/>.</summary>
    public Car Car { get; set; }

    /// <summary>
    /// Flag that indicates of the <see cref="Commuter"/> is currently driving its <see cref="Car"/> entity.
    /// </summary>
    public bool CurrentlyCarDriving => true;

    /// <summary>
    /// Velocity, in meters per second, of the <see cref="Car"/> entity of the <see cref="Commuter"/>.
    /// </summary>
    public double CarVelocity { get; set; }

    /// <summary>
    /// Collection of temporally ordered TripPositions that encode the travel trajectory of the <see cref="Commuter"/>.
    /// </summary>
    public TripsCollection TripsCollection { get; set; }

    #endregion
}