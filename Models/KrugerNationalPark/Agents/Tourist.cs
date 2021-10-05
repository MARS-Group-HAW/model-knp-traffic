using System;
using System.Collections.Generic;
using System.IO;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using KrugerNationalPark.Misc.Events;
using Mars.Common;
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

namespace KrugerNationalPark.Agents
{
    public class Tourist : IAgent<KnpStreetLayer>, ICarSteeringCapable
    {
        #region Initialization

        /// <summary>
        ///     Needed fo "removing" the agent and preventing further tick() call to it.
        /// </summary>
        [PropertyDescription]
        public UnregisterAgent UnregisterHandle { get; set; }

        public void Init(KnpStreetLayer layer)
        {
            KnpEventComponent = new KnpEventComponent(this);
            _streetLayer = layer;
            ElephantCounter = 0;
            State = TouristState.Driving;

            _startTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();

            // TODO: Parameterisierung aus CSV oder Dynamik mit +/- Random Wert, um Varianz im Tourist-Verhalten abzubilden
            _endTime = new DateTime(_startTime.Year, _startTime.Month, _startTime.Day, 10, 0, 0);

            TripsCollection = new TripsCollection(layer.Context);

            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.StreetLayer = layer;
            Car = car;

            // todo: Source is a Point, no Random needed? 
            Position = SourceGeometry.RandomPositionFromGeometry();
            car.TryEnterDriver(this, out var handle);

            _originNode = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, _originNode);


            /*var p1 = new Position(31.4447138, -24.9883233);
            var p2 = new Position(31.4277741, -25.0153934);
            var n1 = layer.StreetEnvironment.NearestNode(p1);
            var n2 = layer.StreetEnvironment.NearestNode(p2);
            
            var rt1 = _streetLayer.StreetEnvironment.FindRoute(n1, n2);
            var rt2 = _streetLayer.StreetEnvironment.FindRoute(n2, n1);*/

            // Random walk with time constraint without "destination"
            //handle.Route = FindRoute(_originNode);
            //VehicleHandle = handle;

            // tourist determines destination
            var sourcePoi = PoiLayer.Nearest(Position);

            var availableDestinations = sourcePoi.getDestinationPOIs(3600, new List<string> {"Rest camp", "Gate"});

            var l = availableDestinations.Count;
            var rnd = new Random();
            var i = rnd.Next(0, l);
            var destinationPoco = availableDestinations[i];


            var destinationNode = layer.StreetEnvironment.NearestNode(destinationPoco.Position);


            handle.Route = _streetLayer.FindRoute(_originNode, _originNode, 3600);
            VehicleHandle = handle;

            // save route to geojson
            var geoJson = handle.Route.ToGeoJson();
            File.WriteAllText("route_" + ID + ".json", geoJson);


            // search all gates/camps inside time constrainaed
            // -> bsp: gib mir alle gates die von meinem punkt aus innerhalb von 2h erreichbar sind
        }

        public IEventComponent KnpEventComponent { get; set; }

        #endregion

        #region Tick

        public void Tick()
        {
            // @todo: random in range
            const int lookDuration = 15; // in minutes

            // we are driving around and wait for an anima sighting event
            // todo: on the qy home should we prevent looking for animals?

            // just temp code for logging agent internals 
            // -> track if we have an virtual car before us so we need to brake
            HasAnimalSighting = 0;
            SightingEventCarVelocity = 0;
            if (_animalSighting is not null)
            {
                SightingEventCarVelocity = (int) Math.Round(_animalSighting.Velocity, 0);
                HasAnimalSighting = 1;
            }

            if (State == TouristState.Braking)
            {
                if (Car.Velocity == 0)
                {
                    // we are at a stand now, start timer to remove AnimalSighting "car"
                    _arrivalTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    _departureTime = _arrivalTime.AddMinutes(lookDuration);
                    State = TouristState.Looking;


                    _tmpRoute = VehicleHandle.Route;
                    VehicleHandle.Route = null;
                }
            }
            else if (State == TouristState.Looking)
            {
                //@todo : logik valdieiren, in der simulkation sah es so aus lob die dauernd bremsen
                if (_departureTime.Subtract(_streetLayer.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
                {
                    _streetLayer.StreetEnvironment.Remove(_animalSighting);
                    _animalSighting = null;
                    State = TouristState.Driving;

                    VehicleHandle.Route = _tmpRoute;
                }
            }


            // Always call Move, since braking is "handled" by the AnimalSighting car ahead
            VehicleHandle.Move();

            CarVelocity = Car.Velocity;
            CarVelocityInt =
                (int) Math.Round(Car.Velocity, 0); // todo: save it into an int, double is broken in csv writer
            TripsCollection.Add(Position);
        }

        public int HasAnimalSighting { get; set; }

        public int SightingEventCarVelocity { get; set; }

        public int CarVelocityInt { get; set; }

        #endregion

        #region Properties

        public POILayer PoiLayer { get; set; }
        public Guid ID { get; set; }
        public int StableId { get; }

        private static Random rng = new();

        /// <summary>
        ///     State of the tourist (driving around, looking at wildlife, ...)
        /// </summary>
        public TouristState State { get; set; }

        /// <summary>
        ///     The start time and end time of the agent's tour
        /// </summary>
        private DateTime _startTime;

        /// <summary>
        ///     TimeStamp if the the time alle Camps close and the tourist needs to be home.
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
        ///     Start point of the tourist (can be Camp, or gate).
        ///     Example: POINT (31.482268 -24.979422)
        /// </summary>
        [PropertyDescription(Name = "source")]
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        private Geometry TargetGeometry { get; set; }

        /// <summary>
        ///     A queue containing one DateTime object for each node of the agent's node
        ///     A node's DateTime object specifies the time as of which the agent needs to start driving home when it reaches this
        ///     node
        /// </summary>
        private readonly Queue<DateTime> _edgeTimings = new();

        /// <summary>
        ///     Keep track if the tourist is on its way home -> no longer stop for animals
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
        ///     Reference to the object positioned before our agent to trigger braking.
        /// </summary>
        public KnpCar _animalSighting;

        // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
        // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
        /// <summary>
        ///     The distance from the agent's current position at which the agent should stop upon an animal sighting (achieved by
        ///     placing a virtual car on the road)
        /// </summary>
        public const double InsertAnimalSightingDistanceAhead = 33.0;

        public KnpStreetLayer _streetLayer;

        public Car Car { get; set; }

        public Position Position
        {
            get => Car.Position;
            set => Car.Position = value;
        }

        public bool OvertakingActivated { get; }
        public bool CurrentlyCarDriving => true;
        public double CarVelocity { get; set; }

        public CarSteeringHandle VehicleHandle { get; set; }

        public TripsCollection TripsCollection { get; set; }

        public int ElephantCounter { set; get; }

        private readonly HashSet<Guid> _knownElephants;
        public Route _tmpRoute;

        #endregion

        #region Methods

        public void ElephantAhead(Elephant elephant)
        {
            if (_knownElephants.Add(elephant.ID)) ElephantCounter += 1;
        }

        public void Notify(PassengerMessage passengerMessage)
        {
        }

        public int EventReceived { get; set; }
        public int EventPossibleRelevant { get; set; }
        public int EventHandled { get; set; }

        #endregion
    }
}