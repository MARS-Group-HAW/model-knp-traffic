using System;
using System.Collections.Generic;
using System.IO;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using KrugerNationalPark.Misc.Events;
using Mars.Common.Core.Logging;
using Mars.Common.Core.Random;
using Mars.Components.Environments;
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

public class Visitor : IAgent<KnpRoadNetwork>, ICarSteeringCapable
{
    #region Initialization
        
    public void Init(KnpRoadNetwork layer)
    {
        log = LoggerFactory.GetLogger(typeof(Visitor));

        VisitorEventComponent = new KnpEventComponent(this);
        _knpRoadNetwork = layer;
        State = VisitorState.Driving;

        _startTime = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();

        // TODO: Parameterisierung aus CSV oder Dynamik mit +/- Random Wert, um Varianz im Visitor-Verhalten abzubilden
        _endTime = new DateTime(_startTime.Year, _startTime.Month, _startTime.Day, 10, 0, 0);

        TripsCollection = new TripsCollection(layer.Context);

        var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
        car.Environment = _knpRoadNetwork.Environment;
        Car = car;

        var sourcePos = GenerateSourcePosition();
        // todo: define distribution to make some visitors spawn from gates and others from rest camps (in else case)?
        Position = sourcePos.X != 0d && sourcePos.Y != 0d ? sourcePos : PointsOfInterest.GetRandomPoiPositionOfType(PoiType.KnpGate);
        
        car.TryEnterDriver(this, out var handle);

        _originNode = _knpRoadNetwork.Environment.NearestNode(Position, SpatialModalityType.CarDriving);
        _knpRoadNetwork.Environment.Insert(car, _originNode);


        /*var p1 = new Position(31.4447138, -24.9883233);
        var p2 = new Position(31.4277741, -25.0153934);
        var n1 = layer.StreetEnvironment.NearestNode(p1);
        var n2 = layer.StreetEnvironment.NearestNode(p2);
        
        var rt1 = _streetLayer.StreetEnvironment.FindRoute(n1, n2);
        var rt2 = _streetLayer.StreetEnvironment.FindRoute(n2, n1);*/

        // Random walk with time constraint without "destination"
        //handle.Route = FindRoute(_originNode);
        //VehicleHandle = handle;

        // Visitor determines destination
        _currentTripOriginPoi = PointsOfInterest.GetNearestKnpPoi(Position);

        var targetPos = GenerateTargetPosition();
        var destinationNode = _knpRoadNetwork.Environment.NearestNode(targetPos, SpatialModalityType.CarDriving);

        handle.Route = _knpRoadNetwork.FindRoute(_originNode, destinationNode, 4 * 3600);
        VehicleHandle = handle;

        // save route to geojson
        if (WriteRouteAsGeoJSON)
        {
            var geoJson = handle.Route.ToGeoJson();
            File.WriteAllText("route_" + ID + ".json", geoJson);
        }

        // search all gates/camps inside time constraint
        // -> bsp: gib mir alle gates die von meinem punkt aus innerhalb von 2h erreichbar sind
    }

    #endregion

    #region Tick

    public void Tick()
    {
        // @todo: random in range
        int lookDuration = RandomHelper.Random.NextInteger(5, 15);; // in minutes

        // we are driving around and waiting for an animal sighting event
        // todo: on the qy home should we prevent looking for animals?

        if (State == VisitorState.Braking)
        {
            if (Car.Velocity == 0)
            {
                // we are at a stand now, start timer to remove AnimalSighting "car"
                _arrivalTime = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();
                _departureTime = _arrivalTime.AddMinutes(lookDuration);
                State = VisitorState.Looking;
            }
        }
        else if (State == VisitorState.Looking)
        {
            //@todo : logik validieren, in der simulation sah es so aus lob die dauernd bremsen
            if (_departureTime.Subtract(_knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
            {
                Car.Driver.BrakingActivated = false;
                State = VisitorState.Driving;
            }
        }

        if (VehicleHandle.Route.GoalReached)
        {
            if (_departureTimePoi == null)
            {
                _arrivalTimePoi = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();
                _departureTimePoi = _arrivalTimePoi?.AddMinutes(30);
                //Console.WriteLine($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} Visitor arrived at {currentDestinationPoi.Poi.Name}");

                Console.WriteLine(
                    $"{_knpRoadNetwork.Context.CurrentTick},{ID},arrived,{_currentTripOriginPoi.Name},{_currentTripDestinationPoi.Poi.Name}");


                //log.LogInfo($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} Visitor arrived at {currentDestinationPoi.Poi.Name}");
            }
            else
            {
                if (_departureTimePoi?.Subtract(_knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault())
                        .TotalMinutes < 0)
                {
                    // pause vorbei!

                    var sourcePoi = PointsOfInterest.GetNearestKnpPoi(_currentTripDestinationPoi.Poi.Position);
                    var sourceNode = _knpRoadNetwork.Environment.NearestNode(Position, SpatialModalityType.CarDriving);

                    var availableDestinations =
                        sourcePoi.GetDestinationPois(4 * 3600, new List<string> { PoiType.KnpGate });

                    var l = availableDestinations.Count;
                    var rnd = new Random();
                    var i = rnd.Next(0, l);
                    var destinationPoco = availableDestinations[i];
                    var destinationNode = _knpRoadNetwork.Environment.NearestNode(destinationPoco.Poi.Position,
                        SpatialModalityType.CarDriving);


                    VehicleHandle.Route = _knpRoadNetwork.FindRoute(sourceNode, destinationNode, 4 * 3600);

                    //Console.WriteLine($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} Visitor goes back to {destinationPoco.Poi.Name}");


                    Console.WriteLine(
                        $"{_knpRoadNetwork.Context.CurrentTick},{ID},leave,{sourcePoi.Name},{destinationPoco.Poi.Name}");

                    if (WriteRouteAsGeoJSON)
                    {
                        var geoJson = VehicleHandle.Route.ToGeoJson();
                        File.WriteAllText("route_back_" + ID + ".json", geoJson);
                    }
                }
            }
        }

        // Always call Move, since braking is "handled" by the AnimalSighting car ahead
        VehicleHandle.Move();
        
        
        CarVelocity = Car.Velocity;
        TripsCollection.Add(Position);
        
        if (TrafficGrid.IsInRaster(Position))
        {
            TrafficGrid[Position] += 1;
        }

        // todo: 3 dynamisch machen
        if (TrafficJamGrid.IsInRaster(Position) && CarVelocity == 0 && State != VisitorState.Looking)
        {

            _reallyJam += 1;

            if (_reallyJam > 60)
            {
                TrafficJamGrid[Position] += 1;
                _reallyJam = 0;
            }
        }
        
        if (SightingsGrid.IsInRaster(Position) && State == VisitorState.Looking)
        {
            SightingsGrid[Position] += 1;
        }
    }

    private int _reallyJam = 0;

    #endregion

    #region Properties

    public int EventReceived { get; set; }
    public int EventPossibleRelevant { get; set; }
    public int EventHandled { get; set; }

    [PropertyDescription(Name = "RoadSurfacePreferences")]
    public Dictionary<object, double> RoadSurfacePreferences { get; set; }
    
    [PropertyDescription(Name = "RoadAccessPermissions")]
    public Dictionary<object, bool> RoadAccessPermissions { get; set; }
    
    /// <summary>
    ///     Needed fo "removing" the agent and preventing further tick() call to it.
    /// </summary>
    [PropertyDescription]
    public UnregisterAgent UnregisterHandle { get; set; }

    public IEventComponent VisitorEventComponent { get; set; }

    public PointsOfInterest PointsOfInterest { get; set; }
    public Guid ID { get; set; }
    public int StableId { get; }

    private static Random rng = new();

    public int HasAnimalSighting { get; set; }

    public int SightingEventCarVelocity { get; set; }

    /// <summary>
    ///     State of the visitor (driving around, looking at wildlife, ...)
    /// </summary>
    public VisitorState State { get; set; }
    
    private ILogger log;

    /// <summary>
    ///     The start time and end time of the agent's tour
    /// </summary>
    private DateTime _startTime;

    /// <summary>
    ///     TimeStamp if the the time alle Camps close and the visitor needs to be home.
    /// </summary>
    private DateTime _endTime;

    private ISpatialNode _originNode;

    /// <summary>
    ///     During tick() we need to check before entering a new edge, if we have enough time to get home (before _endTime).
    ///     To notice if entered a new edge / passed a node we keep track of the edge and compare it each tick to detect
    ///     a new edge.
    /// </summary>
    private ISpatialEdge _edgeFromPreviousTick;

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
    ///     Geometry that is or contains the trip origin of the visitor.
    ///     Example: POINT (31.482268 -24.979422)
    /// </summary>
    [PropertyDescription(Name = "sourceGeometry")]
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
    ///     Geometry that is or contains the trip destination of the visitor.
    ///     Example: MULTIPOINT (31.28163172149577 -25.0153934)
    /// </summary>
    [PropertyDescription(Name = "targetGeometry")]
    public Geometry? TargetGeometry { get; set; }
#nullable disable
    
    /// <summary>
    ///     A queue containing one DateTime object for each node of the agent's node
    ///     A node's DateTime object specifies the time as of which the agent needs to start driving home when it reaches this
    ///     node
    /// </summary>
    private readonly Queue<DateTime> _edgeTimings = new();

    /// <summary>
    ///     Keep track if the visitor is on its way home -> no longer stop for animals
    /// </summary>
    private bool _goingHome;

    /// <summary>
    ///     start time of animal sighting
    /// </summary>
    private DateTime _arrivalTime;

    /// <summary>
    ///     time to start driving after an animal sighting
    /// </summary>
    private DateTime _departureTime;

    /// <summary>
    ///     start time of break/pause/etc at some poi
    /// </summary>
    private DateTime? _arrivalTimePoi = null;

    /// <summary>
    ///     time to start driving after a stop at a poi
    /// </summary>
    private DateTime? _departureTimePoi;

    // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
    // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
    /// <summary>
    ///     The distance from the agent's current position at which the agent should stop upon an animal sighting (achieved by
    ///     placing a virtual car on the road)
    /// </summary>
    public const double InsertAnimalSightingDistanceAhead = 33.0;

    private KnpPoi _currentTripOriginPoi;
    private TripDestination _currentTripDestinationPoi;

    public Car Car { get; set; }

    public Position Position
    {
        get => Car.Position;
        set => Car.Position = value;
    }
    
    [PropertyDescription(Name = "WriteRouteAsGeoJSON")]
    public bool WriteRouteAsGeoJSON { get; set; }

    public bool OvertakingActivated { get; }
    public bool BrakingActivated { get; set; }
    public bool CurrentlyCarDriving => true;
    public double CarVelocity { get; set; }

    public CarSteeringHandle VehicleHandle { get; set; }

    public TripsCollection TripsCollection { get; set; }

    private KnpRoadNetwork _knpRoadNetwork;

    [PropertyDescription]
    public TrafficGrid TrafficGrid { get; set; }

    [PropertyDescription]
    public TrafficJamGrid TrafficJamGrid { get; set; }
    
    [PropertyDescription]
    public SightingsGrid SightingsGrid { get; set; }
    
    #endregion

    #region Methods
    
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
    
    /// <summary>
    ///     Gets a random position from SourceGeometry or, if not provided, obtains the source position based on
    ///     the provided <value>SourceName</value>
    /// </summary>
    /// <returns>Position of source of trip</returns>
    private Position GenerateSourcePosition()
    {
        return SourceGeometry is not null
            ? GetRandomPositionFromGeometry(SourceGeometry)
            : PointsOfInterest.GetPoiPositionOfNameAndType(SourceName, SourceType);
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
        if (TargetName is not null && TargetType is not null)
        {
            // TODO _currentTripDestinationPoi is not set here; might lead to NullPointerException layer
            targetPos = PointsOfInterest.GetPoiPositionOfNameAndType(TargetName, TargetType);
            // TODO: Add a distance check. If targetName in CSV is too far away from sourceName, choose different target
        }
        // If TargetGeometry is provided, use it to choose a target position
        else if (TargetGeometry is not null)
        {
            // TODO _currentTripDestinationPoi is not set here; might lead to NullPointerException layer
            targetPos = GetRandomPositionFromGeometry(TargetGeometry);
        }
        else
        {
            // Destination is undefined. Therefore, randomly choose a rest camp that is within 1.5 hours from source
            // TODO: replace magic number (timeLimit)
            // TODO: add option to provide TargetGeometry but no SourceGeometry?
            var availableDestinations = _currentTripOriginPoi.GetDestinationPois(4 * 3600, new List<string> { PoiType.RestCamp });
            var numberOfPotentialDestinations = availableDestinations.Count;
            var destinationIndex = new Random().Next(0, numberOfPotentialDestinations);
            _currentTripDestinationPoi = availableDestinations[destinationIndex];
            targetPos = _currentTripDestinationPoi.Poi.Position;
        }

        return targetPos;
    }

    public void Notify(PassengerMessage passengerMessage)
    {
    }

    #endregion
}