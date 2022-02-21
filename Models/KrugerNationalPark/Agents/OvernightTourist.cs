using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents
{
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public class OvernightTourist : IAgent<StreetLayer>, ICarSteeringCapable
    {
        #region Tick

        public void Tick()
        {
            // @todo: random in range
            const int lookDuration = 15; // in minutes

            // keep track of our timings and start way home when latest return is reached.
            if (!_goingHome)
            {
                var currentEdge = VehicleHandle.Route[0].Edge;

                if (!currentEdge.Equals(_edgeFromPreviousTick))
                {
                    var currentTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    var lastOkTimeForEdge = _edgeTimings.Dequeue();

                    _edgeFromPreviousTick = currentEdge;

                    if (lastOkTimeForEdge.Subtract(currentTime).TotalSeconds <= 0)
                    {
                        // Go home with Fastest route
                        VehicleHandle.Route = _streetLayer.StreetEnvironment.FindRoute(currentEdge.From, _originNode);
                        _goingHome = true;
                    }
                }
            }

            // we are driving around and wait for an anima sighting event
            // todo: on the qy home should we prevent looking for animals?
            if (State == TouristState.Driving)
            {
                var rnd = new Random();

                //if (false)
                if (rnd.NextDouble() > 0.9999) // @todo: what number is good, or layer with probabilities?
                    //if (_streetLayer.Context.CurrentTick == 1400)                
                {
                    // 1. determine our position
                    var remainingDistance = VehicleHandle.RemainingDistanceOnEdge;

                    // if the next intersection is closer than our break distance, 
                    // don't look for the animal and keep driving
                    // @todo: this removed the hassle of determining the next edge and position the car there,
                    //        but maybe this is better for us anyway? discuss!
                    if (remainingDistance > InsertAnimalSightingDistanceAhead)
                    {
                        // 2. Create our car to force braking
                        _animalSighting = _streetLayer.EntityManager.Create<KnpCar>("type", "Golf");
                        _animalSighting.Environment = _streetLayer.StreetEnvironment;
                        _animalSighting.KnpStreetLayer = _streetLayer;

                        var edge = VehicleHandle.Route[0].Edge; // <- current edge of our car

                        // 3. insert our baking trigger into the graph
                        // @todo: we should check if between our position and the pos where we insert the car the road is empty
                        // -> so we don't block an commuter ahead of us e.g.
                        _streetLayer.StreetEnvironment.Insert(_animalSighting, edge,
                            Car.PositionOnCurrentEdge + InsertAnimalSightingDistanceAhead);

                        // 4. enter braking state 
                        State = TouristState.Braking;
                    }
                }
            }
            else if (State == TouristState.Braking)
            {
                if (Car.Velocity == 0)
                {
                    // we are at a stand now, start timer to remove AnimalSighting "car"
                    _arrivalTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    _departureTime = _arrivalTime.AddMinutes(lookDuration);
                    State = TouristState.Looking;
                }
            }
            else if (State == TouristState.Looking)
            {
                //@todo : logik valdieiren, in der simulkation sah es so aus lob die dauernd bremsen
                if (_departureTime.Subtract(_streetLayer.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
                {
                    _streetLayer.StreetEnvironment.Remove(_animalSighting);
                    State = TouristState.Driving;
                }
            }

            // Always call Move, since braking is "handled" by the AnimalSighting car ahead
            VehicleHandle.Move();

            CarVelocity = Car.Velocity;
        }

        #endregion

        #region Properties

        public Guid ID { get; set; }

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
        private KnpCar _animalSighting;

        // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
        // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
        /// <summary>
        ///     The distance from the agent's current position at which the agent should stop upon an animal sighting (achieved by
        ///     placing a virtual car on the road)
        /// </summary>
        private const double InsertAnimalSightingDistanceAhead = 33.0;

        private StreetLayer _streetLayer;

        public Car Car { get; set; }

        public Position Position
        {
            get => Car.Position;
            set => Car.Position = value;
        }

        public bool OvertakingActivated { get; }
        public bool BrakingActivated { get; }
        public bool CurrentlyCarDriving => true;
        public double CarVelocity { get; set; }

        private CarSteeringHandle VehicleHandle { get; set; }

        public int ElephantCounter { set; get; }

        private HashSet<Guid> _knownElephants;

        #endregion

        #region Initialization

        public void Init(StreetLayer layer)
        {
            _streetLayer = layer;
            ElephantCounter = 0;
            State = TouristState.Driving;

            _startTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();

            // TODO: Parameterisierung aus CSV oder Dynamik mit +/- Random Wert, um Varianz im Tourist-Verhalten abzubilden
            _endTime = new DateTime(_startTime.Year, _startTime.Month, _startTime.Day, 10, 0, 0);

            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.KnpStreetLayer = layer;
            Car = car;

            // todo: Source is a Point, no Random needed? 
            Position = SourceGeometry.RandomPositionFromGeometry();
            car.TryEnterDriver(this, out var handle);

            _originNode = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, _originNode);

            _knownElephants = new HashSet<Guid>();
            // @todo: OverNight Konzeption
            // - initialisierung identisch /(wie obemn)
            // - radius? -> ereichbare camps ermitteln


            handle.Route = FindRoute(_originNode);
            VehicleHandle = handle;
        }

        /// <summary>
        ///     Find a random route starting from start, that has the max duration of half difference from the
        ///     current time and endTime (so we have the time to go home).
        ///     Keeps track of the driving duration in _edgeTimings.
        /// </summary>
        /// <param name="start">Start position for route</param>
        /// <returns></returns>
        private Route FindRoute(ISpatialNode start)
        {
            var node = start;
            var tripTime = 0.0; // in seconds

            // determine delta between current start time and max end time
            // divide by 2 to make sure we have the same time to drive home - > point of no return 
            var returnTime = Convert.ToDouble(_endTime.Subtract(_startTime).TotalSeconds / 2);

            var lastEdge = node.OutgoingEdges.Values.ToList()[0];

            var rt = new Route();

            // build route with edges until return time is reached
            do
            {
                var outEdges = node.OutgoingEdges.Values.ToList();

                // TODO: die lastEdge scheint keine OutGoing edge der "Nächsten" node zu sein. 
                // das ist uns unklar und nicht erwartungskonform!

                // tripTime == 0 -> erster durchlauf, keine kante entfernen
                // outEdges.Count == 1 -> kein andere option als den selben weg zurückzufahren 
                if (tripTime != 0 && outEdges.Count != 1) outEdges.Remove(lastEdge);

                var count = outEdges.Count;
                var rnd = new Random();
                var i = rnd.Next(0, count);

                lastEdge = outEdges[i];

                // TODO: MARS method is broken (see next line for correct calculation)
                //tripTime += lastEdge.TravelTime; 
                tripTime += lastEdge.Length / lastEdge.MaxSpeed;

                // zeit der schließung - Zeitspanne von diesem node nach hause 
                // => beim Abfahren der route, darf dieser punkt nicht nach dieser uhrzeit übertreten werden.
                var latestTimeToReachHomeFromThisNode = _endTime.Subtract(TimeSpan.FromSeconds(tripTime));
                _edgeTimings.Enqueue(latestTimeToReachHomeFromThisNode);

                rt.Add(lastEdge);

                node = outEdges[i].To;
            } while (tripTime < returnTime);

            return rt;
        }

        #endregion

        #region Methods

        public void ElephantAhead(Elephant elephant)
        {
            if (_knownElephants.Add(elephant.ID)) ElephantCounter += 1;
        }

        public void Notify(PassengerMessage passengerMessage)
        {
        }

        #endregion
    }
}