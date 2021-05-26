using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Components.Agents.Trips;
using Mars.Numerics;
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
        private StreetLayer _streetLayer;

        public DateTime StartTime; // when the tourist starts his tour

        public DateTime endTime;

        public double drivingEdgeTime = 0.0;
            
        //private Dictionary<ISpatialEdge, DateTime> _edgeTimings =  new Dictionary<ISpatialEdge, DateTime>();
        
        private Queue<DateTime> _edgeTimings =  new Queue<DateTime>();


        private ISpatialEdge _lastEdge;
        
        public ISpatialNode OriginNode;
        
        private bool _goingHome = false;
        
        public DateTime ArrivalTime; // start of "sighting"
        public DateTime DepartureTime; // time to start driving after a sightinh
        private KnpCar AnimalSighting;

        // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
        // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
        private double _insertAnimalSightingDistanceAhead = 33.0;
        
        private HashSet<Guid> KnownElephants = new HashSet<Guid>();
        
        public void Init(StreetLayer layer)
        {

            _streetLayer = layer;
            ElephantCounter = 0;
            State = TouristState.Driving;
            
            StartTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
            
            // @todo: parameterisierung aus CSV oder dynamik mit +/- random wert um varianz im tourist verhalten bazubilden
            endTime = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, 10, 0, 0);
            
            //Console.WriteLine("Tourist init");

            TripsCollection = new TripsCollection(layer.Context);
            
            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.StreetLayer = layer;
            Car = car;
            Car.Mass = MyMass;
            
            // todo: Source is a Point, no Random needed? 
            Position = SourceGeometry.RandomPositionFromGeometry();
            car.TryEnterDriver(this, out var handle);

            // From given MULTIPOINT Geometry get a random POINT
            // RandomPositionFromGeometry() doesnt seem random for MULTIPOINTs?!
            var target = TargetGeometry.Coordinates;
            var length = target.Length;
            Random rnd = new Random();
            var index = rnd.Next((int) length);
            var targetCor = target[index];
            var targetPos = Position.CreatePosition(targetCor.X, targetCor.Y);

            OriginNode = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, OriginNode);
            
            // for the StreetEnvironment we need an SpatialNode, not a Position. 
            // -> get nearest Node to chosen target position
            //var goal = layer.StreetEnvironment.GetRandomNode();
            var goal = layer.StreetEnvironment.NearestNode(targetPos);
            
            //handle.Route = layer.StreetEnvironment.FindRoute(OriginNode, goal);
            
            handle.Route = findRoute(OriginNode);
            
            VehicleHandle = handle;
            
            



        }

        /*
         *
findRoute() {
	
	returnTime = AvailableTime / 2
	tripTime = 0
	
	do {
		// random connected node, that is not the origin
		n = nextNode() 
		Route.add(n)
		tripTime += timeToNode(n)
	} while(tripTime < returnTime )

}

         */
        
        
        private Route findRoute(ISpatialNode start)
        {
            var node = start;
            var tripTime = 0.0; // in seconds
            
            // determine delta between current start time and max end time
            // divide by 2 to make sure we have the same time to drive home - > point of no return 
            var returnTime = Convert.ToDouble(endTime.Subtract(StartTime).TotalSeconds / 2);
            
            ISpatialEdge lastEdge = node.OutgoingEdges.Values.ToList()[0];


            
            var rt = new Route();
            
            do
            {
                var outEdges = node.OutgoingEdges.Values.ToList();
                
                
                // @todo: die lastEdge scheint kein3 OutGoing edge der "Nächsten" node zu sein. 
                // das ist uns unklar und nicht erwartungskonform
                
                // tripTime == 0 -> erster durchlauf, keine kante entfernen
                // outEdges.Count == 1 -> kein andere option als den selben weg zurückzufahren 
                if (tripTime != 0 && outEdges.Count != 1)
                {
                    outEdges.Remove(lastEdge);
                }

                var count = outEdges.Count;
                var rnd = new Random();
                var i = rnd.Next(0, count);
                
                lastEdge = outEdges[i];
                
                //tripTime += lastEdge.TravelTime; // broken 
                tripTime += lastEdge.Length / lastEdge.MaxSpeed;

                //StartTime.AddSeconds(tripTime);

                // zeit der schließung - TZeitspanne von diesem node nach hause 
                // => beim abfahren der route, darf dieser punkt nicht nach dieser uhrzeut übertreten werden.
                var x = endTime.Subtract(TimeSpan.FromSeconds(tripTime));

                /*if (!_edgeTimings.ContainsKey(lastEdge))
                {
                    _edgeTimings.Add(lastEdge, x);
                }*/
                
                _edgeTimings.Enqueue(x);

                rt.Add(lastEdge);
                
  
                
                node = outEdges[i].To;

            } while (tripTime < returnTime);
            
            return rt;
        }
        
        
        
        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        public Geometry TargetGeometry { get; set; }
        
        [PropertyDescription(Name = "my_mass")]
        public double MyMass { get; set; }
        
        public double CarVelocity { get; set; }

        /// <summary>
        /// State of the tourist (driving around, looking at wildlife, ...)
        /// </summary>
        public TouristState State { get; set; }
        
        public int ElephantCounter { set; get; }
        
        public CarSteeringHandle VehicleHandle { get; set; }

        public void Tick()
        {
            //Console.WriteLine("Tourist position: " + Position);

            // just for debugging: what is the distance to nearest elephant:
            //var enumerable2 = _touristLayer.ElephantLayer.Environment.Explore(Position);
            //var elephant2 = enumerable2.FirstOrDefault();
            //var distanceElephant = Distance.Haversine(elephant2.Position.PositionArray, Position.PositionArray);
            //Console.WriteLine("Distance to nearest elephant:" + distanceElephant);
            
            // Look for nearest elephant for counting
            
            /* var enumerable = _touristLayer.ElephantLayer.Environment.Explore(Position, 300, 1);
            var elephant = enumerable.FirstOrDefault();
            if (elephant != null)
            {
                if (KnownElephants.Add(elephant.ID))
                {
                    ElephantCounter += 1;
                }
            } */

            var LookDuration = 15;

            if (!_goingHome)
            {
                var currentEdge = VehicleHandle.Route[0].Edge;

                if (!currentEdge.Equals(_lastEdge))
                {
                    var currentTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    var lastOkTimeForEdge = _edgeTimings.Dequeue();
                    
                    _lastEdge = currentEdge;
                    
                    if (lastOkTimeForEdge.Subtract(currentTime).TotalSeconds <= 0)
                    {
                        // Go home with Fastest route
                        VehicleHandle.Route = _streetLayer.StreetEnvironment.FindRoute(currentEdge.From, OriginNode);
                        _goingHome = true;
                    }
                }
            }
            
            // we are driving around and wait for an anima sighting event
            if (State == TouristState.Driving)
            {

                // throw dice...
                Random rnd = new Random();

                
                //if (false)
                if (rnd.NextDouble() > 0.999999)
                //if (_streetLayer.Context.CurrentTick == 1400)                
                {
                    // 1. determine our position
                    var remainingDistance = VehicleHandle.RemainingDistanceOnEdge;
                    
                    // if the next intersection is closer than our break distance, 
                    // don't look for the animal and keep driving
                    // @todo: this removed the hassly of determining the next edge and position the car there,
                    //        but maybe this is better for us anyway? discuss!
                    if (remainingDistance > _insertAnimalSightingDistanceAhead)
                    {
                        // 2. Create our car to force braking
                        AnimalSighting = _streetLayer.EntityManager.Create<KnpCar>("type", "Golf");
                        AnimalSighting.Environment = _streetLayer.StreetEnvironment;
                        AnimalSighting.StreetLayer = _streetLayer;

                        var edge = VehicleHandle.Route[0].Edge; // <- current edge of our car
                        
                        // 3. insert our baking trigger into the graph
                        // @todo: we should check if between our position and the pos where we insert the car the road is empty
                        // -> so we don't block an commuter ahead of us e.g.
                        _streetLayer.StreetEnvironment.Insert(AnimalSighting, edge,
                            Car.PositionOnCurrentEdge + _insertAnimalSightingDistanceAhead);
                        
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
                    ArrivalTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    DepartureTime = ArrivalTime.AddMinutes(LookDuration);
                    State = TouristState.Looking;
                }
            } else if (State == TouristState.Looking)
            {
                //@todo : logik valdieiren, in der simulkation sah es so aus lob die dauernd bremsen
                if (DepartureTime.Subtract(_streetLayer.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
                {
                    _streetLayer.StreetEnvironment.Remove(AnimalSighting);
                    State = TouristState.Driving;
                }
            }
            
            
            // Always call Move, since braking is "handled" by the AnimalSighting car ahead
            VehicleHandle.Move(); // -> will call our HandleCustom in 

            CarVelocity = Car.Velocity;
            TripsCollection.Add(Position);
        }
        
        public Guid ID { get; set; }
        
        public void ElephantAhead(Elephant elephant)
        {
            if (KnownElephants.Add(elephant.ID))
            {
                ElephantCounter += 1;
            }
        }

        public Position Position
        {
            get => Car.Position;
            set => Car.Position = value;
        }
        
        public void Notify(PassengerMessage passengerMessage)
        {
            
        }

        public bool OvertakingActivated { get; }
        public Car Car { get; set; }
        public bool CurrentlyCarDriving => true;
        public int StableId { get; }

        public TripsCollection TripsCollection { get; set; }
    }
}