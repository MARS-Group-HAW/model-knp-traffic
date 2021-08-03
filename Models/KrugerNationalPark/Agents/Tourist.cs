using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Components.Agents.Trips;
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
    public class Tourist : IAgent<StreetLayer>, ICarSteeringCapable, ITripSavingAgent
    {
        #region Properties

        public POILayer PoiLayer { get; set; }
        public Guid ID { get; set; }
        public int StableId { get; }

        private static Random rng = new Random(); 

        /// <summary>
        /// State of the tourist (driving around, looking at wildlife, ...)
        /// </summary>
        public TouristState State { get; set; }

        /// <summary>
        /// The start time and end time of the agent's tour
        /// </summary>
        private DateTime _startTime;

        /// <summary>
        /// TimeStamp if the the time alle Camps close and the tourist needs to be home.
        /// </summary>
        private DateTime _endTime;

        private ISpatialNode _originNode;

        /// <summary>
        /// During tick() we need to check before entering a new edge, if we have enough time to get home (before _endTime).
        /// To notice if entered a new edge / passed a node we keep track of the edge and compare it each tick to detect
        /// a new edge.
        /// </summary>
        private ISpatialEdge _edgeFromPreviousTick;
        
        /// <summary>
        /// Start point of the tourist (can be Camp, or gate).
        /// Example: POINT (31.482268 -24.979422)
        /// </summary>
        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        private Geometry TargetGeometry { get; set; }
        
        /// <summary>
        /// A queue containing one DateTime object for each node of the agent's node
        /// A node's DateTime object specifies the time as of which the agent needs to start driving home when it reaches this node
        /// </summary>
        private readonly Queue<DateTime> _edgeTimings = new();

        /// <summary>
        /// Keep track if the tourist is on its way home -> no longer stop for animals
        /// </summary>
        private bool _goingHome;

        /// <summary>
        /// start time of animal sighting
        /// </summary>
        private DateTime _arrivalTime;
        
        /// <summary>
        /// time to start driving after an animal sighting
        /// </summary>
        private DateTime _departureTime;
        
        /// <summary>
        /// Reference to the object positioned before our agent to trigger braking.
        /// </summary>
        private KnpCar _animalSighting;

        // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
        // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
        /// <summary>
        /// The distance from the agent's current position at which the agent should stop upon an animal sighting (achieved by placing a virtual car on the road)
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
        public bool CurrentlyCarDriving => true;
        public double CarVelocity { get; set; }

        private CarSteeringHandle VehicleHandle { get; set; }

        public TripsCollection TripsCollection { get; set; }

        public int ElephantCounter { set; get; }

        private readonly HashSet<Guid> _knownElephants;

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



            var p1 = new Position(31.4447138, -24.9883233);
            var p2 = new Position(31.4277741, -25.0153934);
            var n1 = layer.StreetEnvironment.NearestNode(p1);
            var n2 = layer.StreetEnvironment.NearestNode(p2);
            
            var rt1 = _streetLayer.StreetEnvironment.FindRoute(n1, n2);
            var rt2 = _streetLayer.StreetEnvironment.FindRoute(n2, n1);

            // Random walk with time constraint without "destination"
            //handle.Route = FindRoute(_originNode);
            //VehicleHandle = handle;
            
            // tourist determines destination
            var sourcePoi = PoiLayer.Nearest(Position);
            
            var availableDestinations = sourcePoi.getDestinationPOIs(3600, new List<String> { "Rest camp", "Gate" });

            var l = availableDestinations.Count;
            var rnd = new Random();
            var i = rnd.Next(0, l);
            var destinationPoco = availableDestinations[i];

            
            var destNode = layer.StreetEnvironment.NearestNode(destinationPoco.Position);

            
   
            handle.Route = FindRoute2(_originNode, destNode, 3600);
            VehicleHandle = handle;
            
            // search all gates/camps inside time constrainaed
            // -> bsp: gib mir alle gates die von meinem punkt aus innerhalb von 2h erreichbar sind

        }

        private Route FindRoute2(ISpatialNode start, ISpatialNode goal, double timeLimit)
        {
            var node = start;
            var lastEdge = node.OutgoingEdges.Values.ToList()[0];

            var oldFrom = lastEdge.From;

            var rt = new Route();

            // build route with edges until return time is reached
            do
            {
                var outEdges = node.OutgoingEdges.Values.ToList();

                // TODO: die lastEdge scheint keine OutGoing edge der "Nächsten" node zu sein. 
                // das ist uns unklar und nicht erwartungskonform!

                // tripTime == 0 -> erster durchlauf, keine kante entfernen
                // outEdges.Count == 1 -> kein andere option als den selben weg zurückzufahren 
                if (rt.Count > 0  && outEdges.Count != 1)
                {
                    // find edge, leading back to the last origin
                    var count2 = outEdges.Count;

                    for (var i = 0; i < count2; i++)
                    {
                        var e = outEdges[i];
                        if (e.To.Equals(oldFrom))
                        {
                            outEdges.Remove(e);
                            break;
                        }
                    }
                
                
                    //outEdges.Remove(lastEdge);
                }

                var rnd = new Random(); 
                
                outEdges = outEdges.OrderBy(item => rnd.Next()).ToList();

                var count = outEdges.Count;
                    
                for (var i = 0; i < count; i++)
                {
                    lastEdge = outEdges[i];
                    oldFrom = lastEdge.From;
                    
                    var edgeDuration =  (lastEdge.Length / lastEdge.MaxSpeed);
                    
                    // edge leads to this node
                    // from this node we have to be able to reach out goal within the time limit
                    var targetNode = lastEdge.To;
                    var tmpRoute = _streetLayer.StreetEnvironment.FindRoute(targetNode, goal);
                    var duration = GetRouteDuration(tmpRoute);

                    if ((duration + edgeDuration) < timeLimit)
                    {
                        // route edge is Okay to drive on
                        rt.Add(lastEdge);
                        node = lastEdge.To;
                        timeLimit -= edgeDuration;
                        break;
                    }
                }
                
            } while (!node.Equals(goal));

            return rt;
        }

        public void Shuffle<T>(IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }

        private double GetRouteDuration(Route rt)
        {
            double duration = 0.0;
            
            foreach (var edgeStop in rt)
            {
                var edge = edgeStop.Edge;
                // TODO: MARS method is broken (see next line for correct calculation)
                //tripTime += lastEdge.TravelTime; 
                duration += edge.Length / edge.MaxSpeed;
            }
            return duration;
        }

        
        #endregion

        #region Tick

        public void Tick()
        {
            // @todo: random in range
            const int lookDuration = 15; // in minutes
            
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
                        _animalSighting.StreetLayer = _streetLayer;

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
            TripsCollection.Add(Position);
        }

        #endregion

        #region Methods

        public void ElephantAhead(Elephant elephant)
        {
            if (_knownElephants.Add(elephant.ID))
            {
                ElephantCounter += 1;
            }
        }

        public void Notify(PassengerMessage passengerMessage)
        {
        }

        #endregion
    }
}