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

/// <summary>The <see cref="OsvTourGuide"/> agent travels between POIs in search for wildlife.</summary>
/// <remarks>Configurable via and spawned by <see cref="OsvTourGuideScheduler"/> (see scheduler CSV file).</remarks>
public class OsvTourGuide : IAgent<KnpRoadNetwork>, ICarSteeringCapable
{
    #region Initialization
    
    /// <summary>Initialization routine of the <see cref="OsvTourGuide"/> agent.</summary>
    /// <remarks>
    /// Includes state initialization, vehicle acquisition, positioning in the environment, and route finding.
    /// </remarks>
    /// <param name="layer">Reference to the <see cref="KnpRoadNetwork"/> on which the <see cref="OsvTourGuide"/> lives.
    /// </param>
    public void Init(KnpRoadNetwork layer)
    {
        // 1. State initialization
        log = LoggerFactory.GetLogger(typeof(OsvTourGuide));

        OsvTourGuideEventComponent = new OsvTourGuideEventComponent(this);
        _knpRoadNetwork = layer;
        State = OsvTourGuideState.Driving;

        TripsCollection = new TripsCollection(layer.Context);

        // 2. Vehicle acquisition
        Car = layer.EntityManager.Create<KnpCar>("type", "Golf");
        Car.Environment = _knpRoadNetwork.Environment;

        // 3. Positioning in the environment
        var sourcePos = GenerateSourcePosition();
        // todo: define distribution to make some OSVTourGuides spawn from gates and others from rest camps (in else case)?
        Position = sourcePos.X != 0d && sourcePos.Y != 0d ? sourcePos : PointsOfInterest.GetRandomPoiPositionOfType(PoiType.RestCamp);
        
        Car.TryEnterDriver(this, out var handle);

        _originNode = _knpRoadNetwork.Environment.NearestNode(Position, SpatialModalityType.CarDriving);
        _knpRoadNetwork.Environment.Insert(Car, _originNode);

        /*var p1 = new Position(31.4447138, -24.9883233);
        var p2 = new Position(31.4277741, -25.0153934);
        var n1 = layer.StreetEnvironment.NearestNode(p1);
        var n2 = layer.StreetEnvironment.NearestNode(p2);
        
        var rt1 = _streetLayer.StreetEnvironment.FindRoute(n1, n2);
        var rt2 = _streetLayer.StreetEnvironment.FindRoute(n2, n1);*/

        // Random walk with time constraint without "destination"
        //handle.Route = FindRoute(_originNode);
        //VehicleHandle = handle;

        // 4. Route finding
        _currentTripOriginPoi = PointsOfInterest.GetNearestKnpPoi(Position);
        _currentTripDestinationPoi = _currentTripOriginPoi;
        
        var destinationNode = _knpRoadNetwork.Environment.NearestNode(_currentTripOriginPoi.Position, SpatialModalityType.CarDriving);

        handle.Route = _knpRoadNetwork.FindOsvRoute(_originNode, destinationNode, 2 * 3600);  // TODO make time variable/configurable
        VehicleHandle = handle;

        // save route to geojson
        if (WriteRouteAsGeoJson)
        {
            var geoJson = handle.Route.ToGeoJson();
            File.WriteAllText("route_" + ID + ".json", geoJson);
        }

        // search all gates/camps inside time constraint
        // -> bsp: gib mir alle gates die von meinem punkt aus innerhalb von 2h erreichbar sind
    }

    #endregion

    #region Tick

    /// <summary>Behaviour routine of the <see cref="OsvTourGuide"/>.</summary>
    /// <remarks>
    /// Includes movement behaviour that alternates between random travel on the <see cref="KnpRoadNetwork"/> and
    /// shortest-path travel to a nearby POI.
    /// </remarks>
    public void Tick()
    {
        // @todo: random in range
        var lookDuration = RandomHelper.Random.NextInteger(5, 15);; // in minutes

        // we are driving around and waiting for an animal sighting event
        // todo: on the qy home should we prevent looking for animals?

        if (State == OsvTourGuideState.Braking)
        {
            if (Car.Velocity == 0)
            {
                // we are at a stand now, start timer to remove AnimalSighting "car"
                _wildlifeSightingStartTime = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();
                _wildlifeSightingEndTime = _wildlifeSightingStartTime.AddMinutes(lookDuration);
                State = OsvTourGuideState.Looking;
            }
        }
        else if (State == OsvTourGuideState.Looking)
        {
            // TODO validate logic; in simulation, it appears like they are constantly braking
            if (_wildlifeSightingEndTime.Subtract(_knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
            {
                Car.Driver.BrakingActivated = false;
                State = OsvTourGuideState.Driving;
            }
        }

        if (VehicleHandle.Route.GoalReached)
        {
            if (_departureTimePoi is null)
            {
                _arrivalTimePoi = _knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault();
                _departureTimePoi = _arrivalTimePoi?.AddMinutes(120);  // TODO make 120 variable/configurable
                //Console.WriteLine($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} OSVTourGuide arrived at {currentDestinationPoi.Poi.Name}");

                Console.WriteLine(
                    $"{_knpRoadNetwork.Context.CurrentTick},{ID},arrived,{_currentTripOriginPoi.Name},{_currentTripDestinationPoi.Name}");

                //log.LogInfo($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} OSVTourGuide arrived at {currentDestinationPoi.Poi.Name}");
            }
            else
            {
                if (_departureTimePoi?.Subtract(_knpRoadNetwork.Context.CurrentTimePoint.GetValueOrDefault())
                        .TotalMinutes < 0)
                {
                    // pause vorbei!

                    var sourcePoi = PointsOfInterest.GetNearestKnpPoi(_currentTripDestinationPoi.Position);
                    var sourceNode = _knpRoadNetwork.Environment.NearestNode(Position, SpatialModalityType.CarDriving);

                    VehicleHandle.Route = _knpRoadNetwork.FindOsvRoute(sourceNode, sourceNode, 2 * 3600);  // TODO make time variable/configurable

                    //Console.WriteLine($"{ID} {_sgmLayer.Context.CurrentTimePoint.GetValueOrDefault()} OSVTourGuide goes back to {destinationPoco.Poi.Name}");

                    Console.WriteLine(
                        $"{_knpRoadNetwork.Context.CurrentTick},{ID},leave,{sourcePoi.Name},{sourcePoi.Name}");

                    if (WriteRouteAsGeoJson)
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

        // todo: make dynamic
        if (TrafficJamGrid.IsInRaster(Position) && CarVelocity == 0 && State != OsvTourGuideState.Looking)
        {

            _trafficJamDurationCounter += 1;

            if (_trafficJamDurationCounter > 60)
            {
                TrafficJamGrid[Position] += 1;
                _trafficJamDurationCounter = 0;
            }
        }
        
        if (SightingsGrid.IsInRaster(Position) && State == OsvTourGuideState.Looking)
        {
            SightingsGrid[Position] += 1;
        }
    }
    
    #endregion

    #region Properties

    /// <summary>Number of seconds that the <see cref="OsvTourGuide"/> agent has spent in current traffic jam.</summary>
    private int _trafficJamDurationCounter;

    /// <summary>Number of wildlife sightings events received by the <see cref="OsvTourGuide"/> agent.</summary>
    public int EventReceived { get; set; }
    
    /// <summary>
    /// Number of wildlife sightings events that are potentially relevant for the <see cref="OsvTourGuide"/> agent.
    /// </summary>
    public int EventPossibleRelevant { get; set; }
    
    /// <summary>
    /// Number of wildlife sightings events that are actually perceived by the <see cref="OsvTourGuide"/> agent.
    /// </summary>
    public int EventsHandled { get; set; }

    /// <summary>Reference to the <see cref="UnregisterHandle"/> of the <see cref="KnpRoadNetwork"/>.</summary>
    /// <remarks>Can be used to unregister agents from the simulation context.</remarks>
    [PropertyDescription]
    public UnregisterAgent UnregisterHandle { get; set; }

    /// <summary>
    /// Component for handling wildlife sighting events on behalf of the <see cref="OsvTourGuide"/> agent.
    /// </summary>
    public IEventComponent OsvTourGuideEventComponent { get; set; }

    /// <summary>Reference to the <see cref="PointsOfInterest"/> layer of the environment.</summary>
    public PointsOfInterest PointsOfInterest { get; set; }
    
    /// <summary>Unique identifier of the <see cref="OsvTourGuide"/> agent.</summary>
    public Guid ID { get; set; }

    /// <summary>Current state of the <see cref="OsvTourGuide"/> (<see cref="OsvTourGuideState"/>).</summary>
    public OsvTourGuideState State { get; set; }
    
    /// <summary> Logger provided by the MARS Framework to track activity of the <see cref="OsvTourGuide"/>.</summary>
    private ILogger log;
    
    /// <summary>
    /// Node of the <see cref="KnpRoadNetwork"/> that is the initial position of the <see cref="OsvTourGuide"/>.
    /// </summary>
    private ISpatialNode _originNode;

    /// <summary>Name of the POI at which the first trip of the <see cref="OsvTourGuide"/> begins.</summary>
    [PropertyDescription(Name = "sourceName")]
    public string SourceName { get; set; }

    /// <summary>Type of the POI specified in <see cref="SourceName"/>.</summary>
    [PropertyDescription(Name = "sourceType")]
    public string SourceType { get; set; }
    
#nullable enable
    /// <summary>
    /// Geometry that contains positions of POIs at which the first trip of the <see cref="OsvTourGuide"/> can begin.
    /// </summary>
    /// <remarks>The format should be a WKT geometry
    /// <see href="https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry">Wikipedia</see> that
    /// encodes geospatial locations that are within the KNP. An example of a point geometry:
    /// POINT (31.482268 -24.979422).
    /// </remarks>
    [PropertyDescription(Name = "sourceGeometry")]
    public Geometry? SourceGeometry { get; set; }
    
    /// <summary>Name of the POI at which the first trip of the <see cref="OsvTourGuide"/> ends.</summary>
    [PropertyDescription(Name = "targetName")]
    public string? TargetName { get; set; }
    
    /// <summary>Type of the POI specified in <see cref="TargetName"/>.</summary>
    [PropertyDescription(Name = "targetType")]
    public string? TargetType { get; set; }

    /// <summary>
    /// Geometry that contains positions of POIs at which the trip of the <see cref="OsvTourGuide"/> can end.
    /// </summary>
    /// <remarks>The format should be a WKT geometry
    /// <see href="https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry">Wikipedia</see> that
    /// encodes geospatial locations that are within the KNP. An example of a point geometry:
    /// POINT (31.482268 -24.979422).
    /// </remarks>
    [PropertyDescription(Name = "targetGeometry")]
    public Geometry? TargetGeometry { get; set; }
#nullable disable

    /// <summary>Time point at which the <see cref="OsvTourGuide"/> starts observing a wildlife sighting.</summary>
    private DateTime _wildlifeSightingStartTime;

    /// <summary>Time point at which the <see cref="OsvTourGuide"/> stops observing a wildlife sighting.</summary>
    private DateTime _wildlifeSightingEndTime;

    /// <summary>Time point at which the <see cref="OsvTourGuide"/> arrives at a POI.</summary>
    private DateTime? _arrivalTimePoi;

    /// <summary>Time point at which the <see cref="OsvTourGuide"/> departs from a POI.</summary>
    private DateTime? _departureTimePoi;

    // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
    // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
    /// <summary>
    /// Distance from the current position of the <see cref="OsvTourGuide"/> at which the agent should stop due to a wildlife
    /// sighting.
    /// </summary>
    /// <remarks>This is achieved by temporarily placing a virtual car on the road.</remarks>
    public const double InsertAnimalSightingDistanceAhead = 33.0;

    /// <summary><see cref="KnpPoi"/> that is the initial position of the <see cref="OsvTourGuide"/>.</summary>
    /// <remarks>This is based on the value of <see cref="SourceName"/>.</remarks>
    private KnpPoi _currentTripOriginPoi;
    
    /// <summary><see cref="KnpPoi"/> that is the initial destination of the <see cref="OsvTourGuide"/>.</summary>
    /// <remarks>This is based on the value of <see cref="TargetName"/>.</remarks>
    private KnpPoi _currentTripDestinationPoi;

    /// <summary>Reference to the <see cref="Car"/> entity of the <see cref="OsvTourGuide"/>.</summary>
    public Car Car { get; set; }

    /// <summary>Position of the <see cref="OsvTourGuide"/> on the <see cref="KnpRoadNetwork"/>.</summary>
    /// <remarks>Defined in terms of the position of the <see cref="Car"/> entity of the <see cref="OsvTourGuide"/>.
    /// </remarks>
    public Position Position
    {
        get => Car.Position;
        set => Car.Position = value;
    }
    
    /// <summary>
    /// Flag that indicates if the travel trajectory of the <see cref="OsvTourGuide"/> should be written to a GeoJSON
    /// file.
    /// </summary>
    [PropertyDescription(Name = "WriteRouteAsGeoJSON")]
    public bool WriteRouteAsGeoJson { get; set; }

    /// <summary>Flag that indicates if the <see cref="OsvTourGuide"/> is capable of overtaking in traffic.</summary>
    public bool OvertakingActivated { get; }
    
    /// <summary>
    /// Flag that indicates if the <see cref="OsvTourGuide"/> is currently braking its <see cref="Car"/> entity.
    /// </summary>
    public bool BrakingActivated { get; set; }
    
    /// <summary>
    /// Flag that indicates of the <see cref="OsvTourGuide"/> is currently driving its <see cref="Car"/> entity.
    /// </summary>
    public bool CurrentlyCarDriving => true;
    
    /// <summary>
    /// Velocity, in meters per second, of the <see cref="Car"/> entity of the <see cref="OsvTourGuide"/>.
    /// </summary>
    public double CarVelocity { get; set; }

    /// <summary>
    /// Reference to the <see cref="CarSteeringHandle"/> to interact with the <see cref="Car"/> of the
    /// <see cref="OsvTourGuide"/>.
    /// </summary>
    public CarSteeringHandle VehicleHandle { get; set; }

    /// <summary>
    /// Collection of temporally ordered TripPositions that encode the travel trajectory of the
    /// <see cref="OsvTourGuide"/>.
    /// </summary>
    public TripsCollection TripsCollection { get; set; }

    /// <summary>Reference to the <see cref="KnpRoadNetwork"/>, which holds the road network of the KNP.</summary>
    private KnpRoadNetwork _knpRoadNetwork;

    /// <summary>
    /// Reference to the <see cref="TrafficGrid"/>, which tracks the cumulative movement density on the
    /// <see cref="KnpRoadNetwork"/>. 
    /// </summary>
    [PropertyDescription]
    public TrafficGrid TrafficGrid { get; set; }

    /// <summary>
    /// Reference to the <see cref="TrafficJamGrid"/>, which tracks the location and cumulative duration of traffic jams
    /// on the <see cref="KnpRoadNetwork"/>.
    /// </summary>
    [PropertyDescription]
    public TrafficJamGrid TrafficJamGrid { get; set; }
    
    /// <summary>
    /// Reference to the <see cref="SightingsGrid"/>, which tracks the location and cumulative duration of wildlife
    /// sightings on the <see cref="KnpRoadNetwork"/>.
    /// </summary>
    [PropertyDescription]
    public SightingsGrid SightingsGrid { get; set; }
    
    #endregion

    #region Methods
    
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
    
    /// <summary>
    /// Gets a random position from <see cref="SourceGeometry"/> or, if not provided, obtains the source position based
    /// on the provided <see cref="SourceName"/>.
    /// </summary>
    /// <returns>Position of source of trip</returns>
    private Position GenerateSourcePosition()
    {
        // TODO replace GetRandomPositionFromGeometry() with call to POILayer where POI of type in geometry is selected
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

        // If TargetName is provided, use it to determine target position
        if (TargetName is not null && TargetType is not null)
        {
            targetPos = PointsOfInterest.GetPoiPositionOfNameAndType(TargetName, TargetType);
            // TODO: Add a distance check. If targetName in CSV is too far away from sourceName, choose different target
        }
        // If TargetGeometry is provided, use it to choose a target position
        else if (TargetGeometry is not null)
        {
            // TODO replace GetRandomPositionFromGeometry() with call to POILayer where POI of type in geometry is selected
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
            targetPos = _currentTripDestinationPoi.Position;
        }

        return targetPos;
    }

    public void Notify(PassengerMessage passengerMessage)
    {
    }

    #endregion
}